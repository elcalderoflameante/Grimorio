using Microsoft.AspNetCore.SignalR;

namespace Grimorio.API.Hubs;

public class TableServiceHub : Hub
{
    public const string NewRequestEvent = "tableService:new-request";
    public const string RequestUpdatedEvent = "tableService:request-updated";

    public static string GetBranchGroup(Guid branchId) => $"branch:{branchId}";
    public static string GetPublicTableGroup(Guid tableId) => $"public-table:{tableId}";

    public async Task JoinPublicTable(Guid tableId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetPublicTableGroup(tableId));
    }

    public override async Task OnConnectedAsync()
    {
        var branchClaim = Context.User?.FindFirst("BranchId")?.Value;
        if (branchClaim != null && Guid.TryParse(branchClaim, out var branchId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetBranchGroup(branchId));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var branchClaim = Context.User?.FindFirst("BranchId")?.Value;
        if (branchClaim != null && Guid.TryParse(branchClaim, out var branchId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetBranchGroup(branchId));
        }

        await base.OnDisconnectedAsync(exception);
    }
}
