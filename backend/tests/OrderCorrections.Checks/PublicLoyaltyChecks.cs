using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Marketing;
using Grimorio.Infrastructure.Features.Marketing;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

internal class FakeLoyaltyCodeSender : ILoyaltyCodeSender
{
    internal string Code = "";
    internal bool Fail;
    internal int Calls;
    internal string MessageId = "";
    public Task<string> Send(string phone, string code, CancellationToken ct) {
        Calls++; Code = code;
        if (Fail) throw new HttpRequestException("Provider failure with secret data");
        MessageId = "wamid." + Guid.NewGuid();
        return Task.FromResult(MessageId);
    }
}

internal static class PublicLoyaltyChecks
{
    internal static async Task Run(IServiceProvider services, HttpClient client, LoyaltyWhatsAppOptions config, Guid branch, Action<bool, string> check) {
        client.DefaultRequestHeaders.Authorization = null;
        var sender = (FakeLoyaltyCodeSender)services.GetRequiredService<ILoyaltyCodeSender>();
        async Task Read(Func<GrimorioDbContext, Task> action) {
            await using var scope = services.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<GrimorioDbContext>());
        }
        const string baseUrl = "/api/marketing/loyalty/public";
        async Task<HttpResponseMessage> Request(string purpose = "Enroll", string phone = "0998880001") =>
            await client.PostAsJsonAsync(baseUrl + "/code", new RequestLoyaltyCodeDto { Phone = phone, Purpose = purpose });
        async Task<LoyaltyCodeSentDto> Start(string purpose = "Enroll", string phone = "0998880001") {
            var response = await Request(purpose, phone); response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<LoyaltyCodeSentDto>())!;
        }
        Task<HttpResponseMessage> Verify(LoyaltyCodeSentDto c, string? code = null, bool accepts = true, string? terms = PublicLoyaltyService.TermsVersion) =>
            client.PostAsJsonAsync(baseUrl + "/verify", new VerifyLoyaltyCodeDto { ChallengeId = c.ChallengeId, Code = code ?? sender.Code,
                Name = "Nuevo Miembro", Email = "new@checks.invalid", BirthDay = 29, BirthMonth = 2, AcceptsProgram = accepts, TermsVersion = terms });
        async Task Age() => await Read(db => db.LoyaltyPhoneChallenges.Where(c => c.BranchId == branch)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.CreatedAt, DateTime.UtcNow.AddMinutes(-2))));

        check(!(await client.GetFromJsonAsync<PublicLoyaltyInfoDto>(baseUrl + "/program"))!.Enabled, "Public enrollment disabled by default");
        check((await Request()).StatusCode == HttpStatusCode.BadRequest && sender.Calls == 0, "Disabled mode never sends");
        config.Enabled = true; config.PublicBranchId = branch; config.TestPhoneNumberId = "123456";
        config.AccessToken = "test-not-real"; config.AuthenticationTemplate = "test_authentication";
        config.CodeHashKey = new string('k', 40); config.AllowedTestPhones = ["0998880001", "0998880002", "0998880003", "0998880004", "0998880005"];
        config.AppSecret = new string('s', 40); config.WebhookVerifyToken = new string('v', 40);
        await Read(async db => { var p = await db.LoyaltyPrograms.SingleAsync(p => p.BranchId == branch); p.IsActive = true; await db.SaveChangesAsync(); });
        config.Mode = "Production";
        check(!config.Ready && (await Request()).StatusCode == HttpStatusCode.BadRequest, "Production mode cannot accidentally activate public sends");
        config.Mode = "Test";
        check((await client.GetFromJsonAsync<PublicLoyaltyInfoDto>(baseUrl + "/program"))!.Enabled, "Configured active test program available");
        check((await Request(phone: "0999999999")).StatusCode == HttpStatusCode.BadRequest && sender.Calls == 0, "Allowlist enforced before provider call");
        var c = await Start(); var correct = sender.Code;
        check(correct.Length == 6 && correct.All(char.IsAsciiDigit), "Cryptographically generated six digit code");
        await Read(async db => {
            var saved = await db.LoyaltyPhoneChallenges.SingleAsync(x => x.Id == c.ChallengeId);
            check(saved.CodeHash != correct && saved.CodeHash.Length == 64 && saved.Sent, "Only keyed code hash persisted, never plaintext");
            check(!await db.LoyaltyMembers.AnyAsync(m => m.Phone == "+593998880001"), "Sending a code creates no member");
        });
        check((await Request()).StatusCode == HttpStatusCode.BadRequest && sender.Calls == 1, "Per-phone cooldown persists in PostgreSQL");
        check((await Verify(c, correct == "000000" ? "000001" : "000000")).StatusCode == HttpStatusCode.BadRequest, "Wrong code rejected");
        await Read(async db => check((await db.LoyaltyPhoneChallenges.SingleAsync(x => x.Id == c.ChallengeId)).Attempts == 1, "Failed attempts committed despite HTTP error"));
        check((await Verify(c, accepts: false)).StatusCode == HttpStatusCode.BadRequest, "Acceptance required for new member");
        check((await Verify(c, terms: "stale")).StatusCode == HttpStatusCode.BadRequest, "Stale program conditions rejected");
        var verified = await Verify(c); verified.EnsureSuccessStatusCode();
        var access = (await verified.Content.ReadFromJsonAsync<PublicLoyaltyAccessDto>())!;
        check(access.PublicToken.Length == 64, "Valid code opens private card");
        Guid memberId = Guid.Empty;
        await Read(async db => {
            var member = await db.LoyaltyMembers.SingleAsync(m => m.Phone == "+593998880001"); memberId = member.Id;
            check(member.PhoneVerifiedAt.HasValue && member.ProgramTermsVersion == PublicLoyaltyService.TermsVersion
                && member.CustomerId == null && !member.MarketingConsent && member.BirthDay == 29, "Minimal verified profile, optional birthday, no fiscal customer or marketing opt-in");
            check(!await db.LoyaltyVisits.AnyAsync(v => v.MemberId == member.Id), "Enrollment never awards a visit");
        });
        check((await Verify(c)).StatusCode == HttpStatusCode.BadRequest, "Successful code cannot be reused");
        await Age(); var recovery = await Start("Recover"); var recovered = await Verify(recovery, accepts: false);
        recovered.EnsureSuccessStatusCode();
        check((await recovered.Content.ReadFromJsonAsync<PublicLoyaltyAccessDto>())!.PublicToken == access.PublicToken, "Recovery returns same card only after verification");
        await Read(async db => {
            check(await db.LoyaltyMembers.CountAsync(m => m.Phone == "+593998880001") == 1, "Recovery never duplicates member");
            var member = await db.LoyaltyMembers.SingleAsync(m => m.Id == memberId);
            check(member.Name == "Nuevo Miembro" && !member.MarketingConsent, "Recovery does not overwrite profile or advertising consent");
        });
        await Age(); var old = await Start(); await Age(); var newer = await Start();
        check((await Verify(old)).StatusCode == HttpStatusCode.BadRequest, "Resend invalidates previous code");
        await Read(db => db.LoyaltyPhoneChallenges.Where(x => x.Id == newer.ChallengeId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAt, DateTime.UtcNow.AddSeconds(-1))));
        check((await Verify(newer)).StatusCode == HttpStatusCode.BadRequest, "Expired code cannot recover card");
        await Age(); await Start(); await Age();
        check((await Request()).StatusCode == HttpStatusCode.BadRequest, "Daily send limit cannot be bypassed by resend or recovery");

        var attempts = await Start(phone: "0998880002"); correct = sender.Code;
        for (var i = 0; i < 5; i++) check((await Verify(attempts, correct == "000000" ? "000001" : "000000")).StatusCode == HttpStatusCode.BadRequest, "Wrong verification rejected");
        check((await Verify(attempts, correct)).StatusCode == HttpStatusCode.BadRequest, "Correct code rejected after five wrong attempts");
        var notMember = await Start("Recover", "0998880003");
        check((await Verify(notMember)).StatusCode == HttpStatusCode.BadRequest, "Recovery of unknown phone creates no member");
        sender.Fail = true; var failed = await Request(phone: "0998880004"); sender.Fail = false;
        check(failed.StatusCode == HttpStatusCode.BadRequest && !(await failed.Content.ReadAsStringAsync()).Contains("secret data"), "Provider failure is sanitized");
        await Read(async db => { var unsent = await db.LoyaltyPhoneChallenges.SingleAsync(x => x.Phone == "+593998880004");
            check(!unsent.Sent && (await Verify(new(unsent.Id, unsent.ExpiresAt, 60))).StatusCode == HttpStatusCode.BadRequest, "Unconfirmed send cannot be verified"); });

        var concurrentCode = await Start(phone: "0998880005"); correct = sender.Code;
        var results = await Task.WhenAll(Verify(concurrentCode, correct), Verify(concurrentCode, correct));
        check(results.Count(r => r.IsSuccessStatusCode) == 1, "Concurrent verification consumes a code exactly once");
        check((await client.GetAsync("/api/integrations/whatsapp/webhook?hub.mode=subscribe&hub.verify_token=wrong&hub.challenge=123")).StatusCode == HttpStatusCode.Forbidden, "Webhook verification rejects wrong token");
        check(await client.GetStringAsync($"/api/integrations/whatsapp/webhook?hub.mode=subscribe&hub.verify_token={config.WebhookVerifyToken}&hub.challenge=123") == "123", "Webhook responds to Meta challenge");
        var messageId = sender.MessageId;
        byte[] Body(string status) => JsonSerializer.SerializeToUtf8Bytes(new { @object = "whatsapp_business_account", entry = new[] { new { changes = new[] {
            new { value = new { metadata = new { phone_number_id = config.TestPhoneNumberId }, statuses = new[] { new { id = messageId, status } } } },
        } } } });
        async Task<HttpResponseMessage> Webhook(byte[] bytes, bool signed = true) {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/integrations/whatsapp/webhook") { Content = new ByteArrayContent(bytes) };
            req.Content.Headers.ContentType = new("application/json");
            if (signed) req.Headers.Add("X-Hub-Signature-256", "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(config.AppSecret), bytes)).ToLowerInvariant());
            return await client.SendAsync(req);
        }
        check((await Webhook(Body("read"), false)).StatusCode == HttpStatusCode.Forbidden, "Unsigned webhook rejected");
        check((await Webhook(Body("read"))).IsSuccessStatusCode && (await Webhook(Body("sent"))).IsSuccessStatusCode, "Signed delivery callbacks accepted");
        await Read(async db => check((await db.LoyaltyPhoneChallenges.SingleAsync(x => x.MessageId == messageId)).DeliveryStatus == "read", "Out-of-order callbacks never regress delivery status"));
        check((await Webhook(Encoding.UTF8.GetBytes("not json"))).StatusCode == HttpStatusCode.BadRequest, "Malformed signed JSON rejected");

        var handler = new CaptureWhatsAppHandler();
        var adapter = new WhatsAppLoyaltyCodeSender(new HttpClient(handler), Microsoft.Extensions.Options.Options.Create(config));
        await adapter.Send("+593998880001", "123456", default);
        using var sent = JsonDocument.Parse(handler.Body!);
        var template = sent.RootElement.GetProperty("template");
        check(template.GetProperty("components")[0].GetProperty("parameters")[0].GetProperty("text").GetString() == "123456"
            && template.GetProperty("components")[1].GetProperty("sub_type").GetString() == "url", "Authentication template sends body code and copy-code button");
        check(handler.Uri!.Host == "graph.facebook.com" && handler.Bearer == config.AccessToken, "WhatsApp adapter uses official API and Authorization header");
    }

    private class CaptureWhatsAppHandler : HttpMessageHandler
    {
        internal string? Body, Bearer; internal Uri? Uri;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
            Body = await request.Content!.ReadAsStringAsync(ct); Bearer = request.Headers.Authorization?.Parameter; Uri = request.RequestUri;
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { messages = new[] { new { id = "wamid.test" } } }) };
        }
    }
}
