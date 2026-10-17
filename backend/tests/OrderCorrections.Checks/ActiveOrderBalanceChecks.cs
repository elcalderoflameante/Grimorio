using Grimorio.Application.Features.POS.Queries;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.POS.Queries;
using Grimorio.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

static class ActiveOrderBalanceChecks
{
    public static async Task Run(IServiceProvider services, Action<bool, string> check)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();
        var branch = new Branch { Name = "Active balance checks", Code = "BAL" };
        var other = new Branch { Name = "Other balance checks", Code = "BAL2" };
        db.AddRange(branch, other);
        var orders = Enum.GetValues<OrderStatus>().Select((status, index) => new Order {
            BranchId = branch.Id, Number = index + 1, Type = OrderType.Takeout, Status = status, Total = 20,
        }).ToList();
        var partial = orders.Single(o => o.Status == OrderStatus.Ready);
        partial.Payments.Add(new OrderPayment { BranchId = branch.Id, OrderId = partial.Id, OrderAmount = 7.25m });
        partial.Payments.Add(new OrderPayment { BranchId = branch.Id, OrderId = partial.Id, OrderAmount = 2.50m });
        partial.Payments.Add(new OrderPayment { BranchId = branch.Id, OrderId = partial.Id, OrderAmount = 9, IsDeleted = true });
        var overpaid = new Order { BranchId = branch.Id, Number = 7, Type = OrderType.Delivery, Status = OrderStatus.Delivered, Total = 5 };
        overpaid.Payments.Add(new OrderPayment { BranchId = branch.Id, OrderId = overpaid.Id, OrderAmount = 6 });
        db.AddRange(orders);
        db.AddRange(overpaid,
            new Order { BranchId = branch.Id, Number = 8, Total = 100, PaidAt = DateTime.UtcNow },
            new Order { BranchId = branch.Id, Number = 9, Total = 100, IsDeleted = true },
            new Order { BranchId = other.Id, Number = 1, Total = 100 });
        await db.SaveChangesAsync();

        var result = await new GetActiveOrderSummariesQueryHandler(db).Handle(new() { BranchId = branch.Id }, default);
        check(result.Count == 6, "Active summaries exclude cancelled, paid, deleted and foreign orders");
        check(result.Single(o => o.Id == partial.Id).PendingPaymentTotal == 10.25m, "Pending balance subtracts all active partial payments, not deleted payments");
        check(result.Single(o => o.Id == partial.Id).Total == 20, "Summary keeps original order total unchanged");
        check(result.Single(o => o.Id == overpaid.Id).PendingPaymentTotal == 0, "Pending balance never becomes negative");
        check(result.Single(o => o.Status == "Confirmed").PendingPaymentTotal == 20, "Unpaid order retains full balance");
        check(result.Any(o => o.Status == "Draft"), "Shared summaries retain drafts for non-table order management");
        check(result.Where(o => o.Status != "Draft").Sum(o => o.PendingPaymentTotal) == 70.25m, "Confirmed and later states total excludes draft balance");
    }
}
