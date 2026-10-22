using Grimorio.Application.Features.Operations;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.API.Services;

/// <summary>Materializes daily obligations even when employees have not opened the app.</summary>
public class ResponsibilityScheduleWorker(IServiceScopeFactory scopes, ILogger<ResponsibilityScheduleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var branches = await db.Branches.Where(x => x.IsActive).Select(x => x.Id).ToListAsync(stoppingToken);
                foreach (var branch in branches)
                    await mediator.Send(new GenerateResponsibilitiesCommand(branch), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogError(error, "Could not generate operational responsibilities"); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
