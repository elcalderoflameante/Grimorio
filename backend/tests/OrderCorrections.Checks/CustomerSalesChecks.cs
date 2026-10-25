using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.Billing.Queries;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

internal static class CustomerSalesChecks
{
    public static async Task Run()
    {
        var connection = Environment.GetEnvironmentVariable("CORRECTION_CHECKS_CONNECTION") ?? throw new Exception("Set CORRECTION_CHECKS_CONNECTION.");
        ChecksDatabaseTarget.Validate(new NpgsqlConnectionStringBuilder(connection));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddDbContext<GrimorioDbContext>(options => options.UseNpgsql(connection));
        services.AddMediatR(options => options.RegisterServicesFromAssembly(typeof(GetSalesHandler).Assembly));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await using var tx = await db.Database.BeginTransactionAsync();
        var branch = new Branch { Name = "Customer sales checks", Code = "CS" + Guid.NewGuid().ToString("N")[..12] };
        var foreign = new Branch { Name = "Foreign sales checks", Code = "CF" + Guid.NewGuid().ToString("N")[..12] };
        var customer = new Customer { BranchId = branch.Id, Name = "Buyer", TaxId = "1712345675", TaxIdType = TaxIdType.Cedula };
        var duplicate = new Customer { BranchId = branch.Id, Name = "Buyer duplicate", TaxId = customer.TaxId, TaxIdType = TaxIdType.Cedula };
        var other = new Customer { BranchId = foreign.Id, Name = "Foreign buyer", TaxId = customer.TaxId, TaxIdType = TaxIdType.Cedula };
        var empty = new Customer { BranchId = branch.Id, Name = "No sales" };
        var index = 0;
        OrderPayment Payment(Customer? buyer, decimal amount, int day, bool deleted = false)
        {
            var branchId = buyer?.BranchId ?? branch.Id;
            var order = new Order { BranchId = branchId, Number = ++index, Type = OrderType.Takeout, Total = amount };
            return new OrderPayment { BranchId = branchId, CustomerId = buyer?.Id, Order = order, OrderId = order.Id,
                OrderAmount = amount, PaidAt = new DateTime(2026, 10, day, 12, index, 0, DateTimeKind.Utc), IsDeleted = deleted };
        }
        var payments = new[] { Payment(customer, 10, 23), Payment(customer, 20, 24), Payment(customer, 30, 24),
            Payment(duplicate, 99, 24), Payment(other, 200, 24), Payment(null, 300, 24), Payment(customer, 999, 24, true) };
        db.AddRange(branch, foreign, customer, duplicate, other, empty);
        db.OrderPayments.AddRange(payments);
        await db.SaveChangesAsync();
        var count = 0;
        void Check(bool pass, string label) { if (!pass) throw new Exception(label); count++; }
        var page1 = await sender.Send(new GetCustomerSalesQuery { BranchId = branch.Id, CustomerId = customer.Id, PageSize = 2 });
        Check(page1?.TotalCount == 3 && page1.TotalAmount == 60, "Summary covers all matching payments, not only one page");
        Check(page1!.Items.Count == 2 && page1.Items[0].OrderAmount == 30 && page1.Items[1].OrderAmount == 20,
            "Newest matching payments first");
        Check(page1.Items.All(p => p.CustomerId == customer.Id), "Customer ID, not shared identification, determines history");
        var page2 = await sender.Send(new GetCustomerSalesQuery { BranchId = branch.Id, CustomerId = customer.Id, Page = 2, PageSize = 2 });
        Check(page2!.Items.Single().OrderAmount == 10 && page2.TotalAmount == 60, "Older sales remain accessible by pagination");
        var dated = await sender.Send(new GetCustomerSalesQuery { BranchId = branch.Id, CustomerId = customer.Id,
            FromUtc = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc) });
        Check(dated!.TotalCount == 2 && dated.TotalAmount == 50 && dated.Items.Count == 2, "Date filter applies to summary and rows");
        var emptyResult = await sender.Send(new GetCustomerSalesQuery { BranchId = branch.Id, CustomerId = empty.Id });
        Check(emptyResult!.TotalCount == 0 && emptyResult.TotalAmount == 0 && emptyResult.Items.Count == 0, "Empty history");
        Check(await sender.Send(new GetCustomerSalesQuery { BranchId = foreign.Id, CustomerId = customer.Id }) == null, "Cross-branch access denied");
        Check(await sender.Send(new GetCustomerSalesQuery { BranchId = branch.Id, CustomerId = Guid.NewGuid() }) == null, "Missing customer");
        var general = await sender.Send(new GetSalesQuery { BranchId = branch.Id });
        Check(general.Count == 5 && general.Sum(p => p.OrderAmount) == 459, "General sales keep their previous behavior without a customer filter");
        try { await sender.Send(new GetCustomerSalesQuery { BranchId = branch.Id, CustomerId = customer.Id, Page = 0 }); throw new Exception("Invalid page accepted"); }
        catch (InvalidOperationException) { Check(true, "Pagination validated"); }
        Check(db.ChangeTracker.Entries().All(e => e.State == EntityState.Unchanged), "History requests never modify sales or customers");
        await tx.RollbackAsync();
        Console.WriteLine($"Customer sales checks passed: {count}. Existing Docker PostgreSQL; all fixtures rolled back.");
    }
}
