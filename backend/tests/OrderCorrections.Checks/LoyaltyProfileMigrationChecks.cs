using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

internal static class LoyaltyProfileMigrationChecks
{
    internal static async Task Run(GrimorioDbContext db, Action<bool, string> check)
    {
        if (await db.Database.CanConnectAsync() && (await db.Database.GetAppliedMigrationsAsync()).Any()) return;
        // Test upgrade of a real pre-change card before the suite creates its regular fixtures.
        await db.GetService<IMigrator>().MigrateAsync("20261020234831_AddMarketingLoyalty");
        var branch = new Branch { Name = "Legacy loyalty upgrade", Code = "LEGACY-LOYALTY" };
        var customer = new Customer { BranchId = branch.Id, Name = "Legacy Member", Email = "legacy@checks.invalid", TaxIdType = TaxIdType.Passport, TaxId = "LEGACY-LOYALTY" };
        db.AddRange(branch, customer); await db.SaveChangesAsync();
        var memberId = Guid.NewGuid(); var token = new string('a', 64); var now = DateTime.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO marketing."Members" ("Id", "BranchId", "CustomerId", "CardCode", "PublicToken", "IsActive", "AcceptedAt", "MarketingConsent", "CreatedBy")
            VALUES ({memberId}, {branch.Id}, {customer.Id}, 'CF-LEGACY-LOYALTY', {token}, TRUE, {now}, FALSE, {Guid.Empty})
            """);
        await db.Database.MigrateAsync();
        var member = await db.LoyaltyMembers.AsNoTracking().SingleAsync(m => m.Id == memberId);
        check(member.Name == customer.Name && member.Email == customer.Email && member.CustomerId == customer.Id
            && member.CardCode == "CF-LEGACY-LOYALTY" && member.PublicToken == token && member.Phone == null,
            "Upgrade preserves existing card identity, customer link and contact name without inventing phone numbers");
        await db.LoyaltyMembers.Where(m => m.Id == memberId).ExecuteDeleteAsync();
        await db.Customers.Where(c => c.Id == customer.Id).ExecuteDeleteAsync();
        await db.Branches.Where(b => b.Id == branch.Id).ExecuteDeleteAsync();
        db.ChangeTracker.Clear();
    }
}
