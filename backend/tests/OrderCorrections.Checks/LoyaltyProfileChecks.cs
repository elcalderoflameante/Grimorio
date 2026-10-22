using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Marketing;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Marketing;
using Grimorio.Infrastructure.Features.Marketing;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Services;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

internal static class LoyaltyProfileChecks
{
    internal static async Task Run(IServiceProvider services, HttpClient client, SecurityKey key, string adminToken, string deniedToken,
        Guid branch, Guid foreign, Guid actor, Guid dish, Guid size, Guid flavor, Guid cash, Guid session, Action<bool, string> check)
    {
        async Task Read(Func<GrimorioDbContext, Task> action) {
            await using var scope = services.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<GrimorioDbContext>());
        }
        async Task<T> Send<T>(IRequest<T> command) {
            await using var scope = services.CreateAsyncScope(); return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command);
        }
        async Task Reject(Func<Task> action, string name) {
            try { await action(); } catch (InvalidOperationException) { check(true, name); return; }
            throw new Exception(name);
        }
        foreach (var input in new[] { "0991234567", "+593 99 123 4567", "593991234567", "00593991234567", "(099) 123-4567" })
            check(LoyaltyContact.NormalizePhone(input) == "+593991234567", "Equivalent phone formats normalize to one identifier");
        foreach (var input in new[] { "", "123", "abc0991234567", "+5930991234567", "0991234567 ext 1", "+000123456789", "++593991234567" })
            check(LoyaltyContact.NormalizePhone(input) is null, "Invalid phone format rejected");
        check(LoyaltyContact.NormalizePhone("+1 (202) 555-0123") == "+12025550123", "International phone supported");
        EnrollLoyaltyMemberDto New(string phone = "0970000001") => new() { Name = "Ana Fidelidad", Phone = phone, AcceptsProgram = true };
        await Reject(async () => { var data = New(); data.Phone = null; await Send(new EnrollLoyaltyMemberCommand(branch, data)); }, "Phone required for quick enrollment");
        await Reject(async () => { var data = New(); data.Name = " "; await Send(new EnrollLoyaltyMemberCommand(branch, data)); }, "Name required for quick enrollment");
        await Reject(async () => { var data = New(); data.BirthDay = 31; data.BirthMonth = 4; await Send(new EnrollLoyaltyMemberCommand(branch, data)); }, "Impossible birthday rejected");
        await Reject(async () => { var data = New(); data.BirthDay = 10; await Send(new EnrollLoyaltyMemberCommand(branch, data)); }, "Incomplete birthday rejected");
        await Reject(async () => { var data = New(); data.Email = "not-an-email"; await Send(new EnrollLoyaltyMemberCommand(branch, data)); }, "Invalid optional email rejected");
        var customerCount = 0;
        await Read(async db => customerCount = await db.Customers.CountAsync());
        var quick = await Send(new EnrollLoyaltyMemberCommand(branch, New()));
        check(quick.Member.CustomerId is null && quick.Member.Phone == "+593970000001" && !quick.Member.MarketingConsent,
            "Quick member requires no fiscal data or marketing permission");
        await Read(async db => check(await db.Customers.CountAsync() == customerCount, "No fictitious fiscal customer is created"));
        await Reject(async () => await Send(new EnrollLoyaltyMemberCommand(branch, New("+593970000001"))), "Duplicate normalized phone is not a new member");
        foreach (var search in new[] { "0970000001", "+593 97 000 0001", "970000001", quick.Member.CardCode.ToLowerInvariant() })
            check((await Send(new GetLoyaltyMembersQuery(branch, search))).Single().Id == quick.Member.Id, "Search finds member by local phone, international phone or card");
        var other = await Send(new EnrollLoyaltyMemberCommand(foreign, New()));
        check(other.Member.Id != quick.Member.Id, "Telephone uniqueness is scoped to branch");
        async Task<LoyaltyMemberDetailDto?> ConcurrentEnroll() {
            try { return await Send(new EnrollLoyaltyMemberCommand(branch, New("0970000003"))); }
            catch (InvalidOperationException) { return null; }
        }
        var concurrent = await Task.WhenAll(ConcurrentEnroll(), ConcurrentEnroll());
        check(concurrent.Count(m => m != null) == 1, "Concurrent enrollment cannot duplicate telephone");
        var program = await Send(new GetLoyaltyProgramQuery(branch)); program.ExcludedItemIds = []; program.MinimumSpend = 10;
        await Send(new SaveLoyaltyProgramCommand(branch, program));
        async Task<OrderDto> Order() {
            var order = await Send(new CreateOrderCommand { BranchId = branch, Type = "Delivery", Items = [new() { MenuItemId = dish, Quantity = 1,
                ModifierSelections = [new() { ModifierOptionId = size, Quantity = 1 }, new() { ModifierOptionId = flavor, Quantity = 1 }] }] });
            return await Send(new ConfirmOrderCommand { BranchId = branch, OrderId = order.Id });
        }
        async Task<OrderPaymentDto> Pay(OrderDto order) => await Send(new PayOrderCommand { BranchId = branch, UserId = actor, OrderId = order.Id,
            CashSessionId = session, IdempotencyKey = Guid.NewGuid(), DocumentType = "NotaDeVenta", OrderAmount = order.Total,
            LoyaltyMemberId = quick.Member.Id, Lines = [new() { MethodId = cash, AmountTendered = order.Total }] });
        var delivery = await Order();
        var today = BranchTimeZone.DateFromUtc(DateTime.UtcNow, "America/Guayaquil");
        await Read(async db => {
            for (var day = 1; day <= 4; day++) db.Add(new LoyaltyVisit { BranchId = branch, MemberId = quick.Member.Id,
                OrderId = delivery.Id, BusinessDate = today.AddDays(-day), MinimumSpend = 10, QualifiedSpend = 10 });
            await db.SaveChangesAsync();
        });
        var payment = await Pay(delivery);
        var before = (await Send(new GetLoyaltyMemberQuery(branch, quick.Member.Id)))!;
        check(before.Member.Visits == 5 && before.Rewards.Count == 1, "Delivery payment stamps and rewards member without fiscal customer");
        await Read(async db => check((await db.OrderPayments.SingleAsync(p => p.Id == payment.Id)).CustomerId == null,
            "Loyalty membership does not invent a billing customer for the payment"));
        var fiscal = new Customer { BranchId = branch, Name = "ANA FISCAL", TaxIdType = TaxIdType.Passport, TaxId = "QUICK-FISCAL", Phone = "0960000001" };
        var foreignFiscal = new Customer { BranchId = foreign, Name = "OTHER FISCAL", TaxIdType = TaxIdType.Passport, TaxId = "OTHER-FISCAL" };
        var finalConsumer = new Customer { BranchId = branch, Name = "Final consumer", TaxIdType = TaxIdType.FinalConsumer, TaxId = "9999999999999" };
        await Read(async db => {
            db.AddRange(fiscal, foreignFiscal);
            var existingFinal = await db.Customers.FirstOrDefaultAsync(c => c.BranchId == branch && c.TaxIdType == TaxIdType.FinalConsumer);
            if (existingFinal is not null) finalConsumer = existingFinal; else db.Add(finalConsumer);
            await db.SaveChangesAsync();
        });
        LoyaltyMemberProfileDto Profile() => new() { Name = "Ana Actualizada", Phone = "0970000002", Email = "ana@checks.invalid",
            CustomerId = fiscal.Id, BirthDay = 29, BirthMonth = 2, MarketingConsent = true };
        await Reject(async () => { var p = Profile(); p.CustomerId = foreignFiscal.Id; await Send(new UpdateLoyaltyMemberCommand(branch, quick.Member.Id, p)); }, "Cannot link foreign fiscal customer");
        await Reject(async () => { var p = Profile(); p.CustomerId = finalConsumer.Id; await Send(new UpdateLoyaltyMemberCommand(branch, quick.Member.Id, p)); }, "Cannot link consumer-final fiscal record");
        var linked = await Send(new UpdateLoyaltyMemberCommand(branch, quick.Member.Id, Profile()));
        check(linked.Member.CustomerId == fiscal.Id && linked.Member.BirthDay == 29 && linked.Member.BirthMonth == 2
            && linked.Member.Phone == "+593970000002" && linked.Member.MarketingConsent, "Profile supports new phone, leap-day birthday, email and explicit fiscal link");
        check(linked.Member.Id == before.Member.Id && linked.PublicToken == before.PublicToken && linked.Member.CardCode == before.Member.CardCode
            && linked.Member.Visits == before.Member.Visits && linked.Rewards.Single().Id == before.Rewards.Single().Id,
            "Completing profile preserves card, visits and issued rewards");
        await Read(async db => check((await db.Customers.SingleAsync(c => c.Id == fiscal.Id)).Phone == "0960000001",
            "Loyalty contact update does not rewrite fiscal customer"));
        using (var response = await client.GetAsync($"/api/marketing/loyalty/card/{quick.PublicToken}")) {
            var json = await response.Content.ReadAsStringAsync();
            check(response.IsSuccessStatusCode && !json.Contains("ana@checks.invalid") && !json.Contains("970000002")
                && !json.Contains("birthDay") && !json.Contains("marketingConsent"), "Public card hides telephone, email, birthday and consent");
        }
        var pDuplicate = Profile(); pDuplicate.Phone = "0970000003";
        await Reject(async () => await Send(new UpdateLoyaltyMemberCommand(branch, quick.Member.Id, pDuplicate)), "Cannot take another member's phone");
        var conflict = New("0970000004"); conflict.CustomerId = fiscal.Id;
        await Reject(async () => await Send(new EnrollLoyaltyMemberCommand(branch, conflict)), "Fiscal customer already linked cannot create a second card");
        await Read(async db => { (await db.Customers.SingleAsync(c => c.Id == fiscal.Id)).IsActive = false; await db.SaveChangesAsync(); });
        check(await Send(new GetPublicLoyaltyCardQuery(quick.PublicToken)) != null, "Inactive fiscal record does not disable independent loyalty card");
        await Pay(await Order());
        var optOut = Profile(); optOut.CustomerId = null; optOut.MarketingConsent = false;
        var unlinked = await Send(new UpdateLoyaltyMemberCommand(branch, quick.Member.Id, optOut));
        check(unlinked.Member.CustomerId == null && !unlinked.Member.MarketingConsent && unlinked.Member.Visits == 5,
            "Unlinking fiscal customer and withdrawing publicity consent preserve program participation and visits");
        string EnrollerToken(string permission = AppConstants.Permissions.MarketingLoyaltyEnroll) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims: [
            new Claim(ClaimTypes.NameIdentifier, actor.ToString()), new Claim(AppConstants.Claims.BranchId, branch.ToString()),
            new Claim(AppConstants.Claims.Permissions, permission)],
            expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
        async Task<HttpResponseMessage> Request(HttpMethod method, string path, object data, string token) {
            using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(data) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); return await client.SendAsync(request);
        }
        using (var response = await Request(HttpMethod.Post, "/api/marketing/loyalty/members", New("0970000005"), EnrollerToken()))
            check(response.IsSuccessStatusCode, "Enrollment permission works without fiscal customer permission");
        using (var response = await Request(HttpMethod.Put, $"/api/marketing/loyalty/members/{quick.Member.Id}", optOut, deniedToken))
            check(response.StatusCode == HttpStatusCode.Forbidden, "Updating profile requires enrollment permission");
        var attempt = Profile(); attempt.CustomerId = foreignFiscal.Id;
        using (var response = await Request(HttpMethod.Put, $"/api/marketing/loyalty/members/{quick.Member.Id}", attempt, EnrollerToken()))
            check(response.StatusCode == HttpStatusCode.Forbidden, "Fiscal linking also requires customer consultation permission");
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/api/marketing/loyalty/lookup?search=0970000002")) {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", EnrollerToken(AppConstants.Permissions.MarketingLoyaltyUse));
            using var response = await client.SendAsync(request); var json = await response.Content.ReadAsStringAsync();
            check(response.IsSuccessStatusCode && json.Contains(quick.Member.Id.ToString()) && !json.Contains("birthDay")
                && !json.Contains("email") && !json.Contains("marketingConsent") && !json.Contains("customerId"),
                "Checkout lookup exposes only identification data, not private profile or fiscal link");
        }
    }
}
