using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class ChecksDbContextFactory : IDesignTimeDbContextFactory<GrimorioDbContext>
{
    public GrimorioDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<GrimorioDbContext>()
        .UseNpgsql("Host=127.0.0.1;Port=5432;Database=grimorio_checks;Username=grimorio_checks;Password=design-only-not-used").Options);
}
