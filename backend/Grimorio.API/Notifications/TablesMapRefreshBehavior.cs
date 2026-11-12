using Grimorio.API.Hubs;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Application.Features.TableService.Commands;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Grimorio.API.Notifications;

// Publish only after a successful handler, shared by staff, public QR and kiosk entry points.
public class TablesMapRefreshBehavior<TRequest, TResponse>(
    IHubContext<KitchenHub> hub, IHttpContextAccessor http,
    ILogger<TablesMapRefreshBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next(ct);
        var branchId = request switch
        {
            CreateOrderCommand r when response is OrderDto { TableId: not null } => r.BranchId,
            UpdateOrderItemsCommand r when response is OrderDto { TableId: not null } => r.BranchId,
            ConfirmOrderCommand r when response is OrderDto { TableId: not null } => r.BranchId,
            DeliverOrderCommand r when response is OrderDto { TableId: not null } => r.BranchId,
            CancelOrderCommand r when response is OrderDto { TableId: not null } => r.BranchId,
            CancelOrderItemCommand r when response is OrderDto { TableId: not null } => r.BranchId,
            CorrectOrderItemCommand r when response is OrderDto { TableId: not null } => r.BranchId,
            RecalculateOrderPromotionsCommand r when response is OrderDto { TableId: not null } => r.BranchId,
            TransferOrderTableCommand r => r.BranchId,
            PayOrderCommand r => r.BranchId,
            PublicCreateDraftOrderCommand when response is PublicDraftOrderResultDto { IsNew: true } draft => draft.Notification.BranchId,
            CreateRestaurantTableCommand or UpdateRestaurantTableCommand when response is RestaurantTableDto table => table.BranchId,
            DeleteRestaurantTableCommand when response is true => TokenBranch(),
            _ => Guid.Empty,
        };
        if (branchId == Guid.Empty) return response;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await hub.Clients.Group(KitchenHub.GetBranchGroup(branchId))
                .SendAsync(KitchenHub.TablesChangedEvent, new { branchId }, timeout.Token);
        }
        catch (Exception ex)
        {
            // A committed order/payment must not become an apparent failure if realtime is unavailable.
            logger.LogWarning(ex, "Could not notify table map for branch {BranchId}; periodic refresh will recover it", branchId);
        }
        return response;
    }

    private Guid TokenBranch() => Guid.TryParse(http.HttpContext?.User.FindFirst(AppConstants.Claims.BranchId)?.Value, out var id)
        ? id : Guid.Empty;
}
