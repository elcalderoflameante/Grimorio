using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

internal static class CashSessionReviewChecks
{
    internal static async Task Run(IServiceProvider services, HttpClient client, string adminToken, string deniedToken,
        Guid branchId, Guid foreignBranchId, Action<bool, string> check)
    {
        async Task<T> Send<T>(IRequest<T> query)
        {
            await using var scope = services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(query);
        }
        var cashier = new User { BranchId = branchId, FirstName = "Cashier", LastName = "Review", Email = "review@checks.invalid", PasswordHash = "checks" };
        var register = new CashRegister { BranchId = branchId, Name = "Review register", Code = "REV" };
        var foreignRegister = new CashRegister { BranchId = foreignBranchId, Name = "Other branch register", Code = "REV" };
        var open = new CashSession { BranchId = branchId, CashRegisterId = register.Id, OpenedBy = cashier.Id,
            OpenedByName = "Cashier Review", OpeningBalance = 23, OpenedAt = DateTime.UtcNow.AddDays(-10) };
        var other = new CashSession { BranchId = foreignBranchId, CashRegisterId = foreignRegister.Id, OpenedBy = Guid.NewGuid() };
        var closed = new CashSession { BranchId = branchId, CashRegisterId = register.Id, OpenedBy = cashier.Id,
            Status = CashSessionStatus.Closed, ClosedAt = DateTime.UtcNow };
        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();
            db.AddRange(cashier, register, foreignRegister, open, other, closed);
            await db.SaveChangesAsync();
        }
        var history = await Send(new GetCashSessionsQuery { BranchId = branchId, PageSize = 1 });
        check(history.Count == 1 && history.All(s => s.Id != open.Id), "Normal cash history keeps its recent-session limit");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/cash/sesiones?openOnly=true&pageSize=1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var response = await client.SendAsync(request);
        var sessions = await response.Content.ReadFromJsonAsync<List<CashSessionDto>>();
        check(response.StatusCode == HttpStatusCode.OK && sessions!.Any(s => s.Id == open.Id), "Administrator can review old open cashier sessions beyond recent-history limit");
        check(sessions!.All(s => s.Status == "Open" && s.Id != other.Id && s.Id != closed.Id), "Open cash review excludes closed sessions and other branches");
        var reviewed = sessions!.Single(s => s.Id == open.Id);
        check(reviewed.OpenedByName == "Cashier Review" && reviewed.CashRegisterCode == "REV" && reviewed.ExpectedCash == 23,
            "Cash review exposes cashier identity, register and balance");
        var own = await Send(new GetActiveCashSessionQuery { BranchId = branchId, UserId = cashier.Id });
        check(own?.Id == open.Id, "Review does not change cashier active-session ownership");
        check(await Send(new GetCashSessionDetailQuery { BranchId = foreignBranchId, Id = open.Id }) == null,
            "Cash session detail remains isolated by branch");
        using var deniedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/cash/sesiones?openOnly=true");
        deniedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", deniedToken);
        using var deniedResponse = await client.SendAsync(deniedRequest);
        check(deniedResponse.StatusCode == HttpStatusCode.Forbidden, "Cash review endpoint retains its view authorization policy");
        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();
            var session = await db.CashSessions.SingleAsync(s => s.Id == open.Id);
            session.Status = CashSessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        var refreshed = await Send(new GetCashSessionsQuery { BranchId = branchId, OpenOnly = true });
        check(refreshed.All(s => s.Id != open.Id), "Closing a cashier session removes it from open cash review on refresh");
    }
}
