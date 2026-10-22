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
        ChecksDatabaseTarget.Validate(target);
        return new GrimorioDbContext(new DbContextOptionsBuilder<GrimorioDbContext>().UseNpgsql(connection).Options);
    }
}

internal static class ChecksDatabaseTarget
{
    internal static void Validate(NpgsqlConnectionStringBuilder target) {
        var legacy = target.Port == 15432 && target.Database == "checks" && target.Username == "checks";
        var sharedContainer = target.Port == 5432 && target.Database == "grimorio_checks" && target.Username == "grimorio_checks";
        if (target.Host != "127.0.0.1" || !(legacy || sharedContainer))
            throw new InvalidOperationException("Only the isolated localhost checks database with its dedicated checks user is allowed.");
    }
}
