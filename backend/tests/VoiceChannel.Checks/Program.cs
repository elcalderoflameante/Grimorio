using Grimorio.API.Hubs;
using Grimorio.API.Services.Voice;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

var checks = 0;
void Check(bool condition, string scenario) { if (!condition) throw new Exception(scenario); checks++; }
var media = new FakeMedia();
var hub = new FakeHub();
var clock = new FakeClock();
var channels = new VoiceChannelService(media, hub, clock);
var branch = Guid.NewGuid();
await channels.JoinAsync(branch, "a", "Ana");
await channels.JoinAsync(branch, "b", "Bar");
var results = await Task.WhenAll(channels.AcquireAsync(branch, "a"), channels.AcquireAsync(branch, "b"));
Check(results.Count(x => x != null) == 1, "Only one simultaneous requester gets the floor");
var owner = results[0] != null ? "a" : "b";
var other = owner == "a" ? "b" : "a";
var lease = results.First(x => x != null)!;
Check(media.Publishers.Count == 1, "Only one media publishing permission");
Check(!await channels.RenewAsync(branch, other, lease), "Another participant cannot renew ownership");
await channels.ReleaseAsync(branch, other, lease);
Check(media.Publishers.Count == 1, "Another participant cannot release ownership");
await channels.ReleaseAsync(branch, owner, lease);
Check(media.Publishers.Count == 0, "Release revokes media before freeing channel");
var next = (await channels.AcquireAsync(branch, other))!;
await channels.ReleaseAsync(branch, owner, lease);
Check(media.Publishers.Count == 1, "A delayed release cannot end a newer turn");
clock.Advance(9);
await channels.SweepAsync(NullLogger.Instance);
Check(media.Publishers.Count == 0, "Lost heartbeat automatically revokes publication");
next = (await channels.AcquireAsync(branch, other))!;
for (var i = 0; i < 5; i++) { clock.Advance(5); Check(await channels.RenewAsync(branch, other, next), "Renew valid turn"); }
clock.Advance(6);
Check(!await channels.RenewAsync(branch, other, next), "Maximum transmission cannot be extended");
Check(media.Publishers.Count == 0, "Maximum transmission revokes media");
next = (await channels.AcquireAsync(branch, owner))!;
media.FailRevoke = true;
try { await channels.ReleaseAsync(branch, owner, next); } catch (HttpRequestException) { }
Check(await channels.AcquireAsync(branch, other) == null, "Failed revoke must not permit overlap");
media.FailRevoke = false;
await channels.ReleaseAsync(branch, owner, next);
Check(media.Publishers.Count == 0, "Retry revoke recovers channel");
media.FailGrantAfterApplying = true;
try { await channels.AcquireAsync(branch, owner); } catch (HttpRequestException) { }
Check(await channels.AcquireAsync(branch, other) == null, "Ambiguous grant retains ownership");
clock.Advance(9);
await channels.SweepAsync(NullLogger.Instance);
Check(media.Publishers.Count == 0, "Ambiguous grant is eventually revoked");
media.FailGrantAfterApplying = false;
var otherBranch = Guid.NewGuid();
await channels.JoinAsync(otherBranch, "c", "Caja");
Check(await channels.AcquireAsync(otherBranch, "c") != null, "Branches have independent channels");
try { await channels.AcquireAsync(branch, "c"); throw new Exception("Cross-branch request accepted"); }
catch (HubException) { checks++; }
await channels.LeaveAsync(otherBranch, "c");
Check(media.Publishers.Count == 0, "Disconnect revokes publisher");
Console.WriteLine($"Voice channel checks passed: {checks}.");

sealed class FakeClock : TimeProvider
{
    private DateTimeOffset _now = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => _now;
    public void Advance(int seconds) => _now = _now.AddSeconds(seconds);
}
sealed class FakeMedia : IVoiceMediaClient
{
    public string PublicUrl => "ws://localhost:7880";
    public HashSet<string> Publishers { get; } = [];
    public bool FailRevoke, FailGrantAfterApplying;
    public string CreateListenerToken(string room, string identity, string name) => "listener-only";
    public Task RemoveAsync(string room, string identity) { Publishers.Remove($"{room}/{identity}"); return Task.CompletedTask; }
    public Task SetPublisherAsync(string room, string identity, bool enabled)
    {
        if (!enabled && FailRevoke) throw new HttpRequestException("Simulated media failure");
        if (enabled) Publishers.Add($"{room}/{identity}"); else Publishers.Remove($"{room}/{identity}");
        if (enabled && FailGrantAfterApplying) throw new HttpRequestException("Response lost after grant");
        return Task.CompletedTask;
    }
}
sealed class FakeHub : IHubContext<VoiceHub>
{
    public IHubClients Clients { get; } = new FakeClients();
    public IGroupManager Groups => throw new NotSupportedException();
}
sealed class FakeClients : IHubClients, IClientProxy
{
    public IClientProxy All => this;
    public IClientProxy AllExcept(IReadOnlyList<string> ids) => this;
    public IClientProxy Client(string id) => this;
    public IClientProxy Clients(IReadOnlyList<string> ids) => this;
    public IClientProxy Group(string name) => this;
    public IClientProxy GroupExcept(string name, IReadOnlyList<string> ids) => this;
    public IClientProxy Groups(IReadOnlyList<string> names) => this;
    public IClientProxy User(string id) => this;
    public IClientProxy Users(IReadOnlyList<string> ids) => this;
    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
