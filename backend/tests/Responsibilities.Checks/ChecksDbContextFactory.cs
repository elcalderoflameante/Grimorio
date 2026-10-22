using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

public class ChecksDbContextFactory : IDesignTimeDbContextFactory<GrimorioDbContext>
{
    public GrimorioDbContext CreateDbContext(string[] args) => Create();
    public static GrimorioDbContext Create(Grimorio.Application.Abstractions.ICurrentUserContext? user = null)
    {
        var connection = Environment.GetEnvironmentVariable("RESPONSIBILITY_CHECKS_CONNECTION")
            ?? "Host=127.0.0.1;Port=15439;Database=responsibilities_checks;Username=checks;Password=checks";
        var target = new NpgsqlConnectionStringBuilder(connection);
        if (target.Host != "127.0.0.1" || target.Port != 15439 || target.Database != "responsibilities_checks" || target.Username != "checks")
            throw new InvalidOperationException("Only the dedicated localhost:15439 responsibilities_checks database is permitted.");
        return new GrimorioDbContext(new DbContextOptionsBuilder<GrimorioDbContext>().UseNpgsql(connection).Options, user);
    }
}
