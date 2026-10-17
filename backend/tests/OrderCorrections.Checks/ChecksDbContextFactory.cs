using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

public class ChecksDbContextFactory : IDesignTimeDbContextFactory<GrimorioDbContext>
{
    public GrimorioDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("CORRECTION_CHECKS_CONNECTION")
            ?? "Host=127.0.0.1;Port=15432;Database=checks;Username=checks;Password=checks";
        var target = new NpgsqlConnectionStringBuilder(connection);
        if (target.Host != "127.0.0.1" || target.Port != 15432 || target.Database != "checks" || target.Username != "checks")
            throw new InvalidOperationException("Design-time checks context only permits disposable localhost:15432/checks.");
        return new GrimorioDbContext(new DbContextOptionsBuilder<GrimorioDbContext>().UseNpgsql(connection).Options);
    }
}
