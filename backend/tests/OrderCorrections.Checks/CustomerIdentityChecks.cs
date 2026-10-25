using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Infrastructure.Features.Billing;
using Grimorio.Infrastructure.Features.Billing.Commands;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

internal static class CustomerIdentityChecks
{
    public static async Task Run()
    {
        var connection = Environment.GetEnvironmentVariable("CORRECTION_CHECKS_CONNECTION") ?? throw new Exception("Set CORRECTION_CHECKS_CONNECTION.");
        ChecksDatabaseTarget.Validate(new NpgsqlConnectionStringBuilder(connection));
        GrimorioDbContext Db() => new(new DbContextOptionsBuilder<GrimorioDbContext>().UseNpgsql(connection).Options);
        var count = 0;
        void Check(bool pass, string label) { if (!pass) throw new Exception(label); count++; }
        string Cedula(string prefix)
        {
            var sum = prefix.Select((c, i) => (c - '0') * (i % 2 == 0 ? 2 : 1)).Sum(d => d > 9 ? d - 9 : d);
            return prefix + (10 - sum % 10) % 10;
        }
        var ci = Cedula("171234567");
        Check(CustomerIdentity.Validate(ci, "FinalConsumer").Type == TaxIdType.Cedula, "Detect cedula from default type");
        Check(CustomerIdentity.Validate(ci + "001", "Cedula").Type == TaxIdType.Ruc, "Detect misclassified RUC");
        Check(CustomerIdentity.Validate("1793189906001", "Ruc").Type == TaxIdType.Ruc, "Modern private RUC is not rejected by modulo 11");
        Check(CustomerIdentity.Validate("1760001550001", "Ruc").Type == TaxIdType.Ruc, "Public RUC");
        Check(CustomerIdentity.ValidCedula(Cedula("304123456")), "Consular cedula");
        Check(CustomerIdentity.ValidCedula(Cedula("010123456")), "Leading zero preserved");
        Check(CustomerIdentity.Validate(" ab-123 ", "Passport").Number == "AB123", "Passport normalization");
        Check(CustomerIdentity.Validate("", "FinalConsumer").Number == null, "Anonymous final consumer");
        foreach (var invalid in new[] { (ci[..9] + (ci[9] == '0' ? '1' : '0'), "Cedula"),
            ("0000000000", "Cedula"), (Cedula("176123456"), "Cedula"), (Cedula("300123456"), "Cedula"),
            (ci + "000", "Ruc"), (ci + "001", "undefined"), ("123", "Cedula"), ("", "Ruc"), ("a", "Passport") })
        {
            try { CustomerIdentity.Validate(invalid.Item1, invalid.Item2); throw new Exception("Invalid ID accepted"); }
            catch (InvalidOperationException) { Check(true, "Reject invalid identity"); }
        }
        var branch = new Branch { Name = "Customer identity checks", Code = "CI" + Guid.NewGuid().ToString("N")[..12] };
        var other = new Branch { Name = "Other customer branch", Code = "CO" + Guid.NewGuid().ToString("N")[..12] };
        string before;
        await using (var db = Db())
        {
            await db.Database.MigrateAsync();
            before = JsonSerializer.Serialize(await db.OrderPayments.AsNoTracking().OrderBy(p => p.Id).Select(p => new { p.Id, p.CustomerId }).ToListAsync());
            db.AddRange(branch, other); await db.SaveChangesAsync();
        }
        async Task<CustomerDto> Create(string number, Guid? target = null, string type = "FinalConsumer")
        {
            await using var db = Db();
            return await new CreateCustomerHandler(db).Handle(new CreateCustomerCommand {
                BranchId = target ?? branch.Id, Name = "Customer checks", TaxId = number, TaxIdType = type,
            }, default);
        }
        async Task Reject(Func<Task> action, string label)
        {
            try { await action(); throw new Exception(label); }
            catch (InvalidOperationException) { Check(true, label); }
        }
        var first = await Create(ci);
        Check(first.TaxId == ci && first.TaxIdType == "Cedula", "Creation stores canonical identity");
        await Reject(async () => { await Create(ci); }, "Reject duplicate");
        await Reject(async () => { await Create(ci[..2] + "-" + ci[2..]); }, "Reject formatted duplicate");
        await Reject(async () => { await Create(ci, type: "Passport"); }, "Do not evade duplicate check with a different ID type");
        await using (var db = Db())
        {
            var updated = await new UpdateCustomerHandler(db).Handle(new UpdateCustomerCommand {
                Id = first.Id, BranchId = branch.Id, Name = "Renamed customer", TaxId = ci, TaxIdType = "Ruc", IsActive = false,
            }, default);
            Check(updated.TaxIdType == "Cedula" && !updated.IsActive, "Own identity may be edited and auto-classified");
        }
        await Reject(async () => { await Create(ci); }, "Inactive customer still prevents duplicates");
        Check((await Create(ci, other.Id)).Id != first.Id, "Identity isolated by branch");
        var second = await Create(Cedula("091234567"));
        await Reject(async () => {
            await using var db = Db();
            await new UpdateCustomerHandler(db).Handle(new UpdateCustomerCommand {
                Id = second.Id, BranchId = branch.Id, Name = "Second", TaxId = ci, TaxIdType = "Cedula", IsActive = true,
            }, default);
        }, "Update cannot take another customer's identity");
        var legacyRuc = ci + "001";
        await using (var db = Db())
        {
            db.Customers.Add(new Customer { BranchId = branch.Id, Name = "Legacy RUC", TaxId = legacyRuc, TaxIdType = TaxIdType.Cedula });
            await db.SaveChangesAsync();
        }
        await Reject(async () => { await Create(legacyRuc); }, "Legacy misclassified RUC prevents duplicates");
        var concurrentId = Cedula("111234567");
        async Task<bool> ConcurrentCreate()
        {
            try { await Create(concurrentId); return true; } catch (InvalidOperationException) { return false; }
        }
        var results = await Task.WhenAll(ConcurrentCreate(), ConcurrentCreate());
        Check(results.Count(success => success) == 1, "Concurrent registration creates exactly one customer");
        await using (var db = Db())
        {
            Check(await db.Customers.CountAsync(c => c.BranchId == branch.Id && c.TaxId == concurrentId) == 1, "No duplicate persisted");
            Check(before == JsonSerializer.Serialize(await db.OrderPayments.AsNoTracking().OrderBy(p => p.Id).Select(p => new { p.Id, p.CustomerId }).ToListAsync()),
                "Historical sales keep their customer assignments");
        }
        Console.WriteLine($"Customer identity checks passed: {count}. Isolated Docker PostgreSQL; no historical sales modified.");
    }
}
