using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Grimorio.API.Hubs;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.POS;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

public static class TableRequestChecksServer
{
    public static async Task Run()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Configuration.Sources.Clear(); builder.Configuration.AddInMemoryCollection();
        builder.WebHost.UseUrls("http://127.0.0.1:5198");
        builder.Services.AddAuthentication("Fixture").AddScheme<AuthenticationSchemeOptions, FixtureAuth>("Fixture", _ => {});
        builder.Services.AddAuthorization();
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://127.0.0.1:5173", "http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
        builder.Services.AddSingleton<Connections>();
        builder.Services.AddSignalR(o => o.AddFilter<Connections>());
        var app = builder.Build(); app.UseCors(); app.UseAuthentication(); app.UseAuthorization();
        app.MapHub<TableServiceHub>("/hubs/table-service");
        app.MapGet("/fixture/health", () => Results.Ok());
        app.MapGet("/fixture/connections", (Connections connections) => connections.Items.Values.Select(c => c.User?.FindFirst("BranchId")?.Value).Where(v => v != null));
        app.MapPost("/fixture/notify", async (TableServiceRequestDto request, HttpContext http, IHubContext<TableServiceHub> hub) => {
            if (http.User.FindFirst("BranchId")?.Value != request.BranchId.ToString()) return Results.Forbid();
            await hub.Clients.Group(TableServiceHub.GetBranchGroup(request.BranchId)).SendAsync(
                request.Status == TableServiceRequestStatus.Pending ? TableServiceHub.NewRequestEvent : TableServiceHub.RequestUpdatedEvent, request);
            return Results.Ok();
        }).RequireAuthorization();
        app.MapPost("/fixture/disconnect", (HttpContext http, Connections connections) => {
            foreach (var connection in connections.Items.Values.Where(c => c.User?.FindFirst("BranchId")?.Value == http.User.FindFirst("BranchId")?.Value)) connection.Abort();
            return Results.Ok();
        }).RequireAuthorization();
        await app.RunAsync();
    }

    private sealed class FixtureAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var token = Request.Query["access_token"].FirstOrDefault() ?? Request.Headers.Authorization.ToString().Replace("Bearer ", "");
            var branch = token switch { "branch-a" => "11111111-1111-4111-8111-111111111111", "branch-b" => "22222222-2222-4222-8222-222222222222", _ => null };
            return Task.FromResult(branch == null ? AuthenticateResult.NoResult() : AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity([new Claim("BranchId", branch)], Scheme.Name)), Scheme.Name)));
        }
    }

    private sealed class Connections : IHubFilter
    {
        public ConcurrentDictionary<string, HubCallerContext> Items { get; } = new();
        public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next) {
            Items[context.Context.ConnectionId] = context.Context; await next(context);
        }
        public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? error, Func<HubLifetimeContext, Exception?, Task> next) {
            Items.TryRemove(context.Context.ConnectionId, out _); await next(context, error);
        }
    }
}
