using System.Net;
using System.Net.Http.Json;
using Grimorio.Infrastructure.Features.Marketing;
using Microsoft.Extensions.Options;

internal static class ProductionWhatsAppChecks
{
    internal static async Task Run(Action<bool, string> check) {
        var o = new LoyaltyWhatsAppOptions { Enabled = true, Mode = "Production", PublicBranchId = Guid.NewGuid(), PhoneNumberId = "111111",
            TestPhoneNumberId = "222222", BusinessAccountId = "333333", BusinessPhone = "+593964135806", MetaAppId = "444444",
            PublicAppUrl = "https://restaurant.test", ErpAppUrl = "https://erp.restaurant.test", AccessToken = "production-test-token-only",
            AuthenticationTemplate = "caldero_codigo_fidelizacion", TemplateLanguage = "es", CodeHashKey = new string('k', 40),
            AppSecret = new string('s', 40), WebhookVerifyToken = new string('v', 40), BusinessAppCoexistenceConfirmed = true,
            AllowedTestPhones = ["0998880001"] };
        check(o.Ready && o.IsRestricted && !o.CanSendTo("+593998880002"), "Configured production defaults to restricted rollout");
        o.PhoneNumberId = "";
        check(!o.Ready && o.SenderId == "", "Production never falls back to the Meta test sender"); o.PhoneNumberId = "111111";
        o.WebhookVerifyToken = ""; check(!o.Ready, "Production requires webhook verification secret"); o.WebhookVerifyToken = new string('v', 40);
        o.ErpAppUrl = "http://erp.restaurant.test"; check(!o.Ready, "Production requires HTTPS callback domain"); o.ErpAppUrl = "https://erp.restaurant.test";
        o.RestrictRecipients = false; check(o.CanSendTo("+593998880002"), "Unrestricted delivery requires explicit configuration");
        o.Enabled = false;
        var handler = new MetaHandler(o); using var http = new HttpClient(handler);
        var probe = await WhatsAppConnectionProbe.Check(http, o, default);
        check(probe.Phone == o.BusinessPhone && handler.Posts == 0, "Setup probe works before enabling and sends no messages");
        o.Enabled = true;
        var adapter = new WhatsAppLoyaltyCodeSender(http, Options.Create(o));
        await adapter.Send("+593998880002", "123456", default);
        check(handler.Posts == 1 && handler.PostUri!.AbsolutePath.EndsWith("/111111/messages"), "Production sends only using verified real Phone Number ID");
        foreach (var failure in new[] { "wrong-phone", "unverified", "not-approved", "marketing", "wrong-language", "wrong-button", "provider-error" }) {
            handler.Failure = failure; var before = handler.Posts;
            try { await adapter.Send("+593998880002", "123456", default); throw new Exception("Unexpected production send: " + failure); }
            catch (InvalidOperationException ex) { check(handler.Posts == before && !ex.Message.Contains("credential-secret"), "Invalid Meta configuration prevents sending: " + failure); }
        }
        handler.Failure = ""; o.RestrictRecipients = true;
        var reads = handler.Gets;
        try { await adapter.Send("+593998880002", "123456", default); throw new Exception("Allowlist bypass"); }
        catch (InvalidOperationException) { check(handler.Gets == reads, "Recipient blocked before any Meta call during restricted rollout"); }
    }

    private class MetaHandler(LoyaltyWhatsAppOptions settings) : HttpMessageHandler
    {
        internal string Failure = ""; internal int Posts, Gets; internal Uri? PostUri;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
            if (request.Headers.Authorization?.Parameter != settings.AccessToken) throw new Exception("Missing authorization header");
            if (request.Method == HttpMethod.Post) {
                Posts++; PostUri = request.RequestUri;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { messages = new[] { new { id = "wamid.production-test" } } }) });
            }
            Gets++;
            if (Failure == "provider-error") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("credential-secret") });
            object payload;
            if (request.RequestUri!.AbsolutePath.EndsWith("/phone_numbers")) payload = new { data = new[] { new {
                id = settings.PhoneNumberId, display_phone_number = Failure == "wrong-phone" ? "+593991234567" : settings.BusinessPhone,
                verified_name = "El Caldero Flameante", code_verification_status = Failure == "unverified" ? "NOT_VERIFIED" : "VERIFIED" } } };
            else payload = new { data = new[] { new { name = settings.AuthenticationTemplate, status = Failure == "not-approved" ? "PENDING" : "APPROVED",
                category = Failure == "marketing" ? "MARKETING" : "AUTHENTICATION", language = Failure == "wrong-language" ? "en_US" : settings.TemplateLanguage,
                components = new[] { new { type = "BUTTONS", buttons = new[] { new { type = "OTP", otp_type = Failure == "wrong-button" ? "ONE_TAP" : "COPY_CODE" } } } } } } };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(payload) });
        }
    }
}
