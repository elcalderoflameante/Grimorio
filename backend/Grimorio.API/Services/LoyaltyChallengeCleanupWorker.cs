using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.API.Services;

public class LoyaltyChallengeCleanupWorker(IServiceScopeFactory scopes, ILogger<LoyaltyChallengeCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            try {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();
                await db.LoyaltyPhoneChallenges.Where(c => c.CreatedAt < DateTime.UtcNow.AddDays(-7)).ExecuteDeleteAsync(stoppingToken);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception) { logger.LogWarning("No se pudieron depurar las verificaciones de fidelizacion. Se reintentara mas tarde."); }
            try { await Task.Delay(TimeSpan.FromHours(12), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }
}
