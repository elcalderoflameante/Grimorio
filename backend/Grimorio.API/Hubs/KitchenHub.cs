using Microsoft.AspNetCore.SignalR;

namespace Grimorio.API.Hubs;

public class KitchenHub : Hub
{
    public const string NewItemsEvent = "kitchen:new-items";
    public const string ItemUpdatedEvent = "kitchen:item-updated";
    public const string OrderCancelledEvent = "kitchen:order-cancelled";

    public static string GetStationGroup(Guid stationId) => $"kitchen-station:{stationId}";
    public static string GetBranchGroup(Guid branchId) => $"kitchen-branch:{branchId}";

    // La app Android llama a este método para suscribirse a su estación
    public async Task JoinStation(Guid stationId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, GetStationGroup(stationId));

    public async Task LeaveStation(Guid stationId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetStationGroup(stationId));

    public override async Task OnConnectedAsync()
    {
        var branchClaim = Context.User?.FindFirst("BranchId")?.Value;
        if (branchClaim != null && Guid.TryParse(branchClaim, out var branchId))
            await Groups.AddToGroupAsync(Context.ConnectionId, GetBranchGroup(branchId));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var branchClaim = Context.User?.FindFirst("BranchId")?.Value;
        if (branchClaim != null && Guid.TryParse(branchClaim, out var branchId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetBranchGroup(branchId));
        await base.OnDisconnectedAsync(exception);
    }
}
