using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure;

/// <summary>
/// Extensiones de inyección de dependencias para la capa de infraestructura.
/// Configura DbContext, repositorios, y otros servicios de infraestructura.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configura DbContext con PostgreSQL
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<GrimorioDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                }
            );
            // En desarrollo, habilita logs detallados de EF Core
            var isDevelopment = configuration["ASPNETCORE_ENVIRONMENT"] == "Development";
            if (isDevelopment)
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });

        return services;
    }
}
