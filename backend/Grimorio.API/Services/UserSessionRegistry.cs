using System.Collections.Concurrent;
using System.Security.Claims;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using Microsoft.AspNetCore.SignalR;

namespace Grimorio.API.Services;

public sealed class UserSessionRegistry
{
    private readonly ConcurrentDictionary<string, (Guid UserId, Action Abort)> _connections = new();

    public void Register(string connectionId, Guid userId, Action abort) => _connections[connectionId] = (userId, abort);
    public void Remove(string connectionId) => _connections.TryRemove(connectionId, out _);
    public void Revoke(Guid userId)
    {
        foreach (var entry in _connections.Where(entry => entry.Value.UserId == userId))
            if (_connections.TryRemove(entry.Key, out var connection)) connection.Abort();
    }
}

public sealed class UserSessionHubFilter(UserSessionRegistry sessions) : IHubFilter
{
    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var principal = context.Context.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            sessions.Register(context.Context.ConnectionId, userId, context.Context.Abort);
            var branchId = Guid.Parse(principal.FindFirstValue("BranchId")!);
            var version = int.TryParse(principal.FindFirstValue("auth_version"), out var value) ? value : 0;
            var db = context.ServiceProvider.GetRequiredService<GrimorioDbContext>();
            if (!await UserAccessPolicy.HasCurrentSessionAsync(db, userId, branchId, version, false, context.Context.ConnectionAborted))
            {
                sessions.Remove(context.Context.ConnectionId);
                context.Context.Abort();
                throw new HubException("La sesion ya no es valida. Inicia sesion nuevamente.");
            }
        }
        try { await next(context); }
        catch { sessions.Remove(context.Context.ConnectionId); throw; }
    }

    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        sessions.Remove(context.Context.ConnectionId);
        await next(context, exception);
    }
}
