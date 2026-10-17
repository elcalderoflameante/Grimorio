using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.API.Hubs;

[Authorize]
public class KitchenHub(GrimorioDbContext db) : Hub
{
    public const string NewItemsEvent = "kitchen:new-items";
    public const string ItemUpdatedEvent = "kitchen:item-updated";
    public const string OrderCancelledEvent = "kitchen:order-cancelled";
    public const string OrderTableChangedEvent = "kitchen:order-table-changed";

    public static string GetStationGroup(Guid stationId) => $"kitchen-station:{stationId}";
    public static string GetBranchGroup(Guid branchId) => $"kitchen-branch:{branchId}";

    // La app Android llama a este método para suscribirse a su estación
    public async Task JoinStation(Guid stationId)
    {
        if (!Guid.TryParse(Context.User?.FindFirst("BranchId")?.Value, out var branchId)
            || !await db.WorkStations.AnyAsync(s => s.Id == stationId && s.BranchId == branchId && s.IsActive && !s.IsDeleted))
            throw new HubException("Estacion no disponible en esta sucursal.");
        await Groups.AddToGroupAsync(Context.ConnectionId, GetStationGroup(stationId));
    }

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
