using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.API.Services;

public sealed class SriInvoiceRetryBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(2);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SriInvoiceRetryBackgroundService> _logger;

    public SriInvoiceRetryBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SriInvoiceRetryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(PollInterval, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingInvoicesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar facturas electrónicas pendientes.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingInvoicesAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var retryBefore = DateTime.UtcNow.Subtract(RetryDelay);

        var pending = await db.ElectronicDocuments
            .AsNoTracking()
            .Where(d => !d.IsDeleted
                && (d.Status == ElectronicDocumentStatus.Pending || d.Status == ElectronicDocumentStatus.Sent)
                && (d.SentAt == null || d.SentAt <= retryBefore))
            .OrderBy(d => d.CreatedAt)
            .Select(d => new { d.Id, d.BranchId })
            .Take(10)
            .ToListAsync(ct);

        foreach (var document in pending)
        {
            try
            {
                await mediator.Send(new RetryElectronicInvoiceCommand
                {
                    DocumentId = document.Id,
                    BranchId = document.BranchId,
                }, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo reprocesar la factura electrónica pendiente {DocumentId}.",
                    document.Id);
            }
        }
    }
}
