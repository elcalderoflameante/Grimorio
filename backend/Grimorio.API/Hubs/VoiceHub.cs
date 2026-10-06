using System.Security.Claims;
using Grimorio.API.Services.Voice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Grimorio.SharedKernel.Constants;

namespace Grimorio.API.Hubs;

[Authorize(Policy = "Voice.Access")]
public sealed class VoiceHub(VoiceChannelService channels, ILogger<VoiceHub> logger) : Hub
{
    private bool CanTransmit => Context.User?.FindFirst(AppConstants.Claims.ClientType)?.Value
        != AppConstants.ClientTypes.Kds;

    private Guid Branch => Guid.TryParse(Context.User?.FindFirst("BranchId")?.Value, out var id)
        ? id : throw new HubException("La sesión no tiene una sucursal válida.");

    public async Task<VoiceSession> Join()
    {
        var branch = Branch;
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out _)) throw new HubException("Sesión inválida.");
        var name = $"{Context.User?.FindFirst("FirstName")?.Value} {Context.User?.FindFirst("LastName")?.Value}".Trim();
        await Groups.AddToGroupAsync(Context.ConnectionId, VoiceChannelService.Group(branch));
        try { return await channels.JoinAsync(
            branch,
            Context.ConnectionId,
            name.Length > 0 ? name : "Personal",
            CanTransmit); }
        catch (InvalidOperationException ex) { throw new HubException(ex.Message); }
    }

    public Task<string?> Acquire() => channels.AcquireAsync(Branch, Context.ConnectionId);
    public Task<bool> Renew(string lease) => channels.RenewAsync(Branch, Context.ConnectionId, lease);
    public Task Release(string lease) => channels.ReleaseAsync(Branch, Context.ConnectionId, lease);
    public async Task Leave()
    {
        await channels.LeaveAsync(Branch, Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, VoiceChannelService.Group(Branch));
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try { await channels.LeaveAsync(Branch, Context.ConnectionId); }
        catch (Exception ex) { logger.LogWarning(ex, "Voice disconnect cleanup will be retried by lease expiry"); }
        await base.OnDisconnectedAsync(exception);
    }
}
