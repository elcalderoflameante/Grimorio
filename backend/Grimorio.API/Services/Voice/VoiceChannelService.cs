using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Grimorio.API.Hubs;

namespace Grimorio.API.Services.Voice;

public record VoiceParticipant(string Identity, string Name);
public record VoiceState(long Revision, VoiceParticipant[] Participants, VoiceParticipant? Speaker, string? LeaseId);
public record VoiceSession(string Url, string Token, string Identity, VoiceState State);

// Single API instance, matching the current CloudCone deployment. Room epoch
// prevents old media permissions surviving an API restart into the new channel.
public sealed class VoiceChannelService(IVoiceMediaClient media, IHubContext<VoiceHub> hub, TimeProvider time)
{
    private sealed class Channel(string room)
    {
        public string Room { get; } = room;
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public Dictionary<string, VoiceParticipant> Participants { get; } = [];
        public HashSet<string> Departed { get; } = [];
        public string? Owner;
        public string? Lease;
        public DateTimeOffset Deadline;
        public DateTimeOffset Maximum;
        public long Revision;
    }

    private readonly ConcurrentDictionary<Guid, Channel> _channels = new();
    private readonly string _epoch = Guid.NewGuid().ToString("N");
    public static string Group(Guid branch) => $"voice:{branch:N}";
    private Channel Get(Guid branch) => _channels.GetOrAdd(branch, id => new($"walkie-{id:N}-{_epoch}"));
    private static VoiceState State(Channel c) => new(c.Revision, c.Participants.Values.ToArray(),
        c.Owner != null && c.Participants.TryGetValue(c.Owner, out var owner) ? owner : null, c.Lease);
    private Task Broadcast(Guid branch, Channel c)
    {
        c.Revision++;
        return hub.Clients.Group(Group(branch)).SendAsync("voice:state", State(c));
    }

    public async Task<VoiceSession> JoinAsync(Guid branch, string identity, string name)
    {
        var c = Get(branch);
        await c.Gate.WaitAsync();
        try
        {
            if (c.Participants.Count >= 12 && !c.Participants.ContainsKey(identity))
                throw new HubException("El canal alcanzó su límite de participantes.");
            var token = media.CreateListenerToken(c.Room, identity, name);
            c.Participants[identity] = new(identity, name);
            await Broadcast(branch, c);
            return new(media.PublicUrl, token, identity, State(c));
        }
        finally { c.Gate.Release(); }
    }

    public async Task<string?> AcquireAsync(Guid branch, string identity)
    {
        var c = Get(branch);
        await c.Gate.WaitAsync();
        try
        {
            if (!c.Participants.ContainsKey(identity)) throw new HubException("Primero conecta el walkie-talkie.");
            await ExpireAsync(branch, c);
            if (c.Owner != null) return null;
            c.Owner = identity;
            c.Lease = Guid.NewGuid().ToString("N");
            c.Maximum = time.GetUtcNow().AddSeconds(30);
            c.Deadline = time.GetUtcNow().AddSeconds(8);
            // Record ownership before the RPC: a lost response may have enabled
            // publishing. Retain the lease until revocation succeeds.
            await media.SetPublisherAsync(c.Room, identity, true);
            await Broadcast(branch, c);
            return c.Lease;
        }
        finally { c.Gate.Release(); }
    }

    public async Task<bool> RenewAsync(Guid branch, string identity, string lease)
    {
        var c = Get(branch);
        await c.Gate.WaitAsync();
        try
        {
            await ExpireAsync(branch, c);
            if (c.Owner != identity || c.Lease != lease) return false;
            var next = time.GetUtcNow().AddSeconds(8);
            c.Deadline = next < c.Maximum ? next : c.Maximum;
            return true;
        }
        finally { c.Gate.Release(); }
    }

    public async Task ReleaseAsync(Guid branch, string identity, string lease)
    {
        var c = Get(branch);
        await c.Gate.WaitAsync();
        try
        {
            if (c.Owner == identity && c.Lease == lease) await RevokeAsync(branch, c);
        }
        finally { c.Gate.Release(); }
    }

    public async Task LeaveAsync(Guid branch, string identity)
    {
        var c = Get(branch);
        await c.Gate.WaitAsync();
        try
        {
            c.Departed.Add(identity);
            if (c.Owner == identity)
            {
                c.Deadline = time.GetUtcNow();
                await RevokeAsync(branch, c);
            }
            if (c.Participants.Remove(identity)) await Broadcast(branch, c);
            await media.RemoveAsync(c.Room, identity);
            c.Departed.Remove(identity);
        }
        finally { c.Gate.Release(); }
    }

    private async Task RevokeAsync(Guid branch, Channel c)
    {
        await media.SetPublisherAsync(c.Room, c.Owner!, false);
        c.Owner = null;
        c.Lease = null;
        await Broadcast(branch, c);
    }
    private Task ExpireAsync(Guid branch, Channel c) => c.Owner != null && time.GetUtcNow() >= c.Deadline
        ? RevokeAsync(branch, c) : Task.CompletedTask;

    public async Task SweepAsync(ILogger logger)
    {
        foreach (var (branch, c) in _channels)
        {
            if (!await c.Gate.WaitAsync(0)) continue;
            try
            {
                await ExpireAsync(branch, c);
                foreach (var identity in c.Departed.ToArray())
                {
                    await media.RemoveAsync(c.Room, identity);
                    c.Departed.Remove(identity);
                    if (c.Participants.Remove(identity)) await Broadcast(branch, c);
                }
            }
            catch (Exception ex) { logger.LogWarning(ex, "Voice lease revocation failed; channel remains blocked"); }
            finally { c.Gate.Release(); }
        }
    }
}

public sealed class VoiceLeaseWorker(VoiceChannelService channels, ILogger<VoiceLeaseWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try { while (await timer.WaitForNextTickAsync(stoppingToken)) await channels.SweepAsync(logger); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
