using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class ChecksDbContextFactory : IDesignTimeDbContextFactory<GrimorioDbContext>
{
    public GrimorioDbContext CreateDbContext(string[] args) => Create();
    public static GrimorioDbContext Create() => new(new DbContextOptionsBuilder<GrimorioDbContext>()
        .UseNpgsql("Host=127.0.0.1;Port=15440;Database=attendance_checks;Username=checks;Password=checks").Options);
}
