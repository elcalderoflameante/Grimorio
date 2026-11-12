using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Grimorio.API.Hubs;
using Grimorio.API.Notifications;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.Application.Features.TableService.Commands;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

var checks = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
var branch = Fixtures.A;
var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
http.HttpContext.User = Fixtures.User("branch-a");
var hub = new RecordingHub();
async Task Publish<T>(IRequest<T> command, T response, bool expected = true, Guid? expectedBranch = null)
{
    var previous = hub.Messages;
    var completed = false;
    hub.AssertCommitted = () => Check(completed, "Signal emitted only after successful handler completion");
    var behavior = new TablesMapRefreshBehavior<IRequest<T>, T>(hub, http, NullLogger<TablesMapRefreshBehavior<IRequest<T>, T>>.Instance);
    var result = await behavior.Handle(command, _ => { completed = true; return Task.FromResult(response); }, default);
    Check(EqualityComparer<T>.Default.Equals(result, response), "Response unchanged");
    Check(hub.Messages == previous + (expected ? 1 : 0), "Expected event coverage");
    if (expected) Check(hub.LastGroup == KitchenHub.GetBranchGroup(expectedBranch ?? branch), "Event scoped to committed branch");
}
var order = new OrderDto { Id = Guid.NewGuid(), TableId = Guid.NewGuid() };
await Publish(new CreateOrderCommand { BranchId = branch }, order);
await Publish(new UpdateOrderItemsCommand { BranchId = branch }, order);
await Publish(new ConfirmOrderCommand { BranchId = branch }, order);
await Publish(new DeliverOrderCommand { BranchId = branch }, order);
await Publish(new CancelOrderCommand { BranchId = branch }, order);
await Publish(new CancelOrderItemCommand { BranchId = branch }, order);
await Publish(new CorrectOrderItemCommand(branch, Guid.NewGuid(), Guid.NewGuid(), new()), order);
await Publish(new RecalculateOrderPromotionsCommand(branch, order.Id), order);
await Publish(new TransferOrderTableCommand(branch, order.Id, Guid.NewGuid(), new()), order);
await Publish(new PayOrderCommand { BranchId = branch }, new OrderPaymentDto());
await Publish(new PublicCreateDraftOrderCommand(), new PublicDraftOrderResultDto { IsNew = true, Notification = new() { BranchId = branch }, Order = order });
await Publish(new PublicCreateDraftOrderCommand(), new PublicDraftOrderResultDto { IsNew = false }, false);
await Publish(new CreateRestaurantTableCommand { BranchId = branch }, new RestaurantTableDto { BranchId = branch });
await Publish(new UpdateRestaurantTableCommand(), new RestaurantTableDto { BranchId = Fixtures.B }, expectedBranch: Fixtures.B);
await Publish(new DeleteRestaurantTableCommand(), true);
await Publish(new DeleteRestaurantTableCommand(), false, false);
await Publish(new GetOrderDetailQuery { BranchId = branch, OrderId = order.Id }, order, false);
await Publish(new CreateOrderCommand { BranchId = branch, Type = "Takeout" }, new OrderDto(), false);
await Publish(new PayOrderCommand(), new OrderPaymentDto(), false);

var failing = new TablesMapRefreshBehavior<PayOrderCommand, OrderPaymentDto>(hub, http, NullLogger<TablesMapRefreshBehavior<PayOrderCommand, OrderPaymentDto>>.Instance);
var beforeFailure = hub.Messages;
try { await failing.Handle(new() { BranchId = branch }, _ => throw new InvalidOperationException("Rejected before commit"), default); throw new Exception("Expected rejected handler"); }
catch (InvalidOperationException) { Check(hub.Messages == beforeFailure, "Failed operation emits no event"); }
hub.Throw = true;
var paid = new OrderPaymentDto { Id = Guid.NewGuid() };
Check(ReferenceEquals(await failing.Handle(new() { BranchId = branch }, _ => Task.FromResult(paid), default), paid), "Transport failure preserves successful payment response");
Console.WriteLine($"{checks} table-map realtime checks passed. No database, SRI, email, OpenAI or production configuration used.");
if (!args.Contains("--serve")) return;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.Sources.Clear();
builder.Configuration.AddInMemoryCollection();
builder.WebHost.UseUrls("http://127.0.0.1:5199");
builder.Services.AddAuthentication("Fixture").AddScheme<AuthenticationSchemeOptions, FixtureAuth>("Fixture", _ => {});
builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://127.0.0.1:5173", "http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<FixtureConnections>();
builder.Services.AddSignalR(o => o.AddFilter<FixtureConnections>());
// KitchenHub's connection lifecycle does not query storage; there is no real DB connection.
builder.Services.AddDbContext<GrimorioDbContext>(o => o.UseNpgsql("Host=127.0.0.1;Port=1;Database=not_used;Username=not_used;Password=not_used"));
builder.Services.AddMediatR(o => {
    o.RegisterServicesFromAssembly(typeof(Fixtures).Assembly);
    o.AddOpenBehavior(typeof(TablesMapRefreshBehavior<,>));
});
var app = builder.Build();
app.UseCors(); app.UseAuthentication(); app.UseAuthorization();
app.MapHub<KitchenHub>("/hubs/kitchen");
app.MapGet("/fixture/health", () => Results.Ok(new { ready = true }));
app.MapGet("/fixture/connections", (FixtureConnections connections) => connections.Contexts.Values
    .Select(c => c.User?.FindFirst("BranchId")?.Value).ToList());
app.MapPost("/fixture/notify", async (IMediator mediator, HttpContext context) => {
    var id = Guid.Parse(context.User.FindFirst("BranchId")!.Value);
    await mediator.Send(new CreateOrderCommand { BranchId = id, TableId = Guid.NewGuid() });
    return Results.Ok();
}).RequireAuthorization();
app.MapPost("/fixture/disconnect", (FixtureConnections connections, HttpContext context) => {
    var id = context.User.FindFirst("BranchId")?.Value;
    foreach (var connection in connections.Contexts.Values.Where(c => c.User?.FindFirst("BranchId")?.Value == id)) connection.Abort();
    return Results.Ok();
}).RequireAuthorization();
await app.RunAsync();

static class Fixtures
{
    public static readonly Guid A = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid B = Guid.Parse("22222222-2222-4222-8222-222222222222");
    public static ClaimsPrincipal User(string token) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, "fixture"), new Claim("BranchId", (token == "branch-a" ? A : B).ToString()),
    ], "Fixture"));
}
sealed class FixtureAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        if (string.IsNullOrEmpty(token)) token = Request.Query["access_token"].ToString();
        return Task.FromResult(token is "branch-a" or "branch-b"
            ? AuthenticateResult.Success(new AuthenticationTicket(Fixtures.User(token), Scheme.Name)) : AuthenticateResult.NoResult());
    }
}
sealed class FixtureOrderHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken ct) => Task.FromResult(new OrderDto { Id = Guid.NewGuid(), TableId = request.TableId });
}
sealed class FixtureConnections : IHubFilter
{
    public ConcurrentDictionary<string, HubCallerContext> Contexts { get; } = new();
    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    { await next(context); Contexts[context.Context.ConnectionId] = context.Context; }
    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? error, Func<HubLifetimeContext, Exception?, Task> next)
    { Contexts.TryRemove(context.Context.ConnectionId, out _); await next(context, error); }
}
sealed class RecordingHub : IHubContext<KitchenHub>, IHubClients, IClientProxy
{
    public int Messages { get; private set; }
    public string? LastGroup { get; private set; }
    public bool Throw { get; set; }
    public Action? AssertCommitted { get; set; }
    IHubClients IHubContext<KitchenHub>.Clients => this;
    IGroupManager IHubContext<KitchenHub>.Groups => throw new NotSupportedException();
    public Task SendCoreAsync(string method, object?[] args, CancellationToken ct = default)
    {
        if (Throw) throw new IOException("Fixture transport failure");
        AssertCommitted?.Invoke();
        if (method != KitchenHub.TablesChangedEvent || args.Length != 1) throw new Exception("Invalid event");
        var properties = args[0]!.GetType().GetProperties();
        if (properties.Length != 1 || properties[0].Name != "branchId") throw new Exception("Only branch invalidation may be sent, no order/customer data");
        Messages++; return Task.CompletedTask;
    }
    public IClientProxy Group(string name) { LastGroup = name; return this; }
    public IClientProxy All => throw new NotSupportedException();
    public IClientProxy AllExcept(IReadOnlyList<string> ids) => throw new NotSupportedException();
    public IClientProxy Client(string id) => throw new NotSupportedException();
    public IClientProxy Clients(IReadOnlyList<string> ids) => throw new NotSupportedException();
    public IClientProxy GroupExcept(string name, IReadOnlyList<string> ids) => throw new NotSupportedException();
    public IClientProxy Groups(IReadOnlyList<string> names) => throw new NotSupportedException();
    public IClientProxy User(string id) => throw new NotSupportedException();
    public IClientProxy Users(IReadOnlyList<string> ids) => throw new NotSupportedException();
}
