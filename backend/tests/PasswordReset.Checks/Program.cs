using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Grimorio.API.Controllers;
using Grimorio.API.Hubs;
using Grimorio.API.Services;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.Auth.Commands;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using Grimorio.SharedKernel.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var connection = Environment.GetEnvironmentVariable("PASSWORD_CHECKS_CONNECTION") ?? throw new Exception("Set PASSWORD_CHECKS_CONNECTION.");
var target = new NpgsqlConnectionStringBuilder(connection);
if (target.Host != "127.0.0.1" || target.Port != 15432 || target.Database != "checks" || target.Username != "checks")
    throw new Exception("Only the dedicated localhost:15432 checks database is allowed.");
var count = 0;
void Check(bool value, string name) { if (!value) throw new Exception(name); count++; }
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Logging.ClearProviders();
const string key = "password-reset-checks-only-256-bit-secret-never-use-in-production";
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> {
    ["JwtSettings:SecretKey"] = key, ["JwtSettings:Issuer"] = "Checks", ["JwtSettings:Audience"] = "Checks",
});
builder.Services.AddDbContext<GrimorioDbContext>(options => options.UseNpgsql(connection));
builder.Services.AddMediatR(options => options.RegisterServicesFromAssembly(typeof(LoginUserCommandHandler).Assembly));
builder.Services.AddScoped<IPasswordHashingService, PasswordHashingService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddSingleton<UserSessionRegistry>();
builder.Services.AddSignalR(options => options.AddFilter<UserSessionHubFilter>());
builder.Services.AddControllers().AddApplicationPart(typeof(UsersController).Assembly);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = true, ValidIssuer = "Checks", ValidateAudience = true, ValidAudience = "Checks",
        ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
    };
    options.Events = new JwtBearerEvents { OnTokenValidated = UserSessionValidation.ValidateAsync };
});
builder.Services.AddAuthorization(options => options.AddPolicy("Admin.Users.View", policy => policy.RequireRole(AppConstants.Roles.Admin)));
await using var app = builder.Build();
app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/probe", () => "ok").RequireAuthorization();
app.MapHub<KitchenHub>("/hubs/kitchen");

var passwords = new PasswordHashingService();
var branch = new Branch { Name = "Password checks", Code = "PW" };
var foreignBranch = new Branch { Name = "Other checks", Code = "PW2" };
User Account(string email, Guid branchId) => new() { BranchId = branchId, Email = email, FirstName = "Checks", LastName = "User",
    PasswordHash = passwords.HashPassword("Original-pass-123"), KdsPinHash = passwords.HashPassword("1234") };
var admin = Account("admin@checks.invalid", branch.Id);
var employee = Account("employee@checks.invalid", branch.Id);
var foreign = Account("foreign@checks.invalid", foreignBranch.Id);
var adminRole = new Role { BranchId = branch.Id, Name = AppConstants.Roles.Admin, IsActive = true };
var role = new Role { BranchId = branch.Id, Name = "Cocina", IsActive = true };
var kitchen = new Permission { Code = AppConstants.Permissions.PosKitchenView, IsActive = true };
var station = new WorkStation { BranchId = branch.Id, Name = "Checks" };
var foreignStation = new WorkStation { BranchId = foreignBranch.Id, Name = "Other" };
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();
    if (Environment.GetEnvironmentVariable("PASSWORD_CHECKS_ALLOW_RESET") == "true")
        await db.Database.EnsureDeletedAsync();
    await db.Database.MigrateAsync();
    Check(!db.Database.HasPendingModelChanges(), "Migration matches model");
    db.AddRange(branch, foreignBranch, admin, employee, foreign, adminRole, role, kitchen, station, foreignStation);
    db.UserRoles.AddRange(new UserRole { BranchId = branch.Id, UserId = admin.Id, RoleId = adminRole.Id },
        new UserRole { BranchId = branch.Id, UserId = employee.Id, RoleId = role.Id });
    db.RolePermissions.Add(new RolePermission { BranchId = branch.Id, RoleId = role.Id, PermissionId = kitchen.Id });
    db.UserPushTokens.Add(new UserPushToken { BranchId = branch.Id, UserId = employee.Id, Token = "checks-device-token" });
    await db.SaveChangesAsync();
}
await app.StartAsync();
try
{
    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    using var client = new HttpClient { BaseAddress = new Uri(address) };
    async Task<HttpResponseMessage> Request(HttpMethod method, string path, string? token = null, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body != null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }
    async Task<AuthResponse> Login(string email, string password)
    {
        using var response = await Request(HttpMethod.Post, "/api/auth/login", body: new { email, password });
        Check(response.IsSuccessStatusCode, "Password login succeeds");
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }
    async Task<User> ReadUser(Guid id)
    {
        using var scope = app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<GrimorioDbContext>().Users.AsNoTracking().SingleAsync(u => u.Id == id);
    }
    async Task CheckStatus(HttpStatusCode expected, string path, string? token = null, object? body = null)
    {
        using var response = await Request(body == null ? HttpMethod.Get : HttpMethod.Post, path, token, body);
        Check(response.StatusCode == expected, $"{path}: expected {expected}, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }
    var adminSession = await Login(admin.Email, "Original-pass-123");
    var previous = await Login(employee.Email, "Original-pass-123");
    await CheckStatus(HttpStatusCode.OK, "/probe", previous.AccessToken);
    var legacyToken = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer: "Checks", audience: "Checks",
        claims: [new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString()), new Claim("BranchId", branch.Id.ToString())],
        expires: DateTime.UtcNow.AddHours(1), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)));
    await CheckStatus(HttpStatusCode.OK, "/probe", legacyToken);
    await CheckStatus(HttpStatusCode.Unauthorized, $"/api/users/{employee.Id}/reset-password", body: new { temporaryPassword = "Temporary-pass-123" });
    await CheckStatus(HttpStatusCode.Forbidden, $"/api/users/{admin.Id}/reset-password", previous.AccessToken, new { temporaryPassword = "Temporary-pass-123" });
    await CheckStatus(HttpStatusCode.BadRequest, $"/api/users/{admin.Id}/reset-password", adminSession.AccessToken, new { temporaryPassword = "Temporary-pass-123" });
    await CheckStatus(HttpStatusCode.BadRequest, $"/api/users/{foreign.Id}/reset-password", adminSession.AccessToken, new { temporaryPassword = "Temporary-pass-123" });
    await CheckStatus(HttpStatusCode.BadRequest, $"/api/users/{employee.Id}/reset-password", adminSession.AccessToken, new { temporaryPassword = "short" });
    Check((await ReadUser(employee.Id)).AuthVersion == 0, "Rejected operations do not modify credentials");

    using var socket = new ClientWebSocket();
    socket.Options.SetRequestHeader("Authorization", "Bearer " + previous.AccessToken);
    await socket.ConnectAsync(new Uri(address.Replace("http://", "ws://") + "/hubs/kitchen"), CancellationToken.None);
    await socket.SendAsync(Encoding.UTF8.GetBytes("{\"protocol\":\"json\",\"version\":1}\u001e"), WebSocketMessageType.Text, true, CancellationToken.None);
    var buffer = new byte[4096];
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
    await socket.ReceiveAsync(buffer, timeout.Token);
    await Task.Delay(50);
    var disconnected = false;
    var pendingReceive = socket.ReceiveAsync(buffer, timeout.Token);
    await CheckStatus(HttpStatusCode.OK, $"/api/users/{employee.Id}/reset-password", adminSession.AccessToken, new { temporaryPassword = "Temporary-pass-123" });
    try
    {
        var received = await pendingReceive;
        while (received.MessageType != WebSocketMessageType.Close) received = await socket.ReceiveAsync(buffer, timeout.Token);
        disconnected = true;
    }
    catch (WebSocketException) { disconnected = true; }
    Check(disconnected, "Password reset aborts an existing kitchen WebSocket");

    var reset = await ReadUser(employee.Id);
    Check(reset.MustChangePassword && reset.AuthVersion == 1 && reset.UpdatedBy == admin.Id, "Reset state, version and actor audit saved atomically");
    Check(reset.PasswordHash != "Temporary-pass-123" && passwords.VerifyPassword("Temporary-pass-123", reset.PasswordHash), "Temporary password hashed");
    using (var scope = app.Services.CreateScope())
        Check(!await scope.ServiceProvider.GetRequiredService<GrimorioDbContext>().UserPushTokens.AnyAsync(token => token.UserId == employee.Id && token.IsActive), "Previous device push notifications revoked");
    await CheckStatus(HttpStatusCode.Unauthorized, "/probe", previous.AccessToken);
    await CheckStatus(HttpStatusCode.Unauthorized, "/probe", legacyToken);
    await CheckStatus(HttpStatusCode.Unauthorized, "/api/auth/login", body: new { email = employee.Email, password = "Original-pass-123" });
    await CheckStatus(HttpStatusCode.Unauthorized, "/api/auth/kds/login", body: new { branchId = branch.Id, userId = employee.Id, pin = "1234" });
    var temporary = await Login(employee.Email, "Temporary-pass-123");
    Check(temporary.MustChangePassword, "Temporary login identifies required password change");
    await CheckStatus(HttpStatusCode.Unauthorized, "/probe", temporary.AccessToken);
    await CheckStatus(HttpStatusCode.Unauthorized, "/api/users", temporary.AccessToken);
    await CheckStatus(HttpStatusCode.Unauthorized, $"/api/users/{admin.Id}/change-password", temporary.AccessToken, new { currentPassword = "Temporary-pass-123", newPassword = "Private-pass-123" });
    await CheckStatus(HttpStatusCode.BadRequest, $"/api/users/{employee.Id}/change-password", temporary.AccessToken, new { currentPassword = "wrong", newPassword = "Private-pass-123" });
    await CheckStatus(HttpStatusCode.BadRequest, $"/api/users/{employee.Id}/change-password", temporary.AccessToken, new { currentPassword = "Temporary-pass-123", newPassword = "short" });
    await CheckStatus(HttpStatusCode.OK, $"/api/users/{employee.Id}/change-password", temporary.AccessToken, new { currentPassword = "Temporary-pass-123", newPassword = "Private-pass-123" });
    var changed = await ReadUser(employee.Id);
    Check(!changed.MustChangePassword && changed.AuthVersion == 2 && passwords.VerifyPassword("Private-pass-123", changed.PasswordHash), "Private password clears required change and revokes temporary session");
    await CheckStatus(HttpStatusCode.Unauthorized, "/probe", temporary.AccessToken);
    await CheckStatus(HttpStatusCode.Unauthorized, "/api/auth/login", body: new { email = employee.Email, password = "Temporary-pass-123" });
    var current = await Login(employee.Email, "Private-pass-123");
    Check(!current.MustChangePassword, "Normal login no longer requires change");
    await CheckStatus(HttpStatusCode.OK, "/probe", current.AccessToken);
    await CheckStatus(HttpStatusCode.OK, "/api/auth/kds/login", body: new { branchId = branch.Id, userId = employee.Id, pin = "1234" });

    await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => CheckStatus(HttpStatusCode.OK,
        $"/api/users/{employee.Id}/reset-password", adminSession.AccessToken, new { temporaryPassword = "Another-temp-123" })));
    Check((await ReadUser(employee.Id)).AuthVersion == 4, "Concurrent resets atomically increment session version");
    var last = await Login(employee.Email, "Another-temp-123");
    var results = await Task.WhenAll(new[] { "Private-one-123", "Private-two-123" }.Select(password => Request(HttpMethod.Post,
        $"/api/users/{employee.Id}/change-password", last.AccessToken, new { currentPassword = "Another-temp-123", newPassword = password })));
    Check(results.Count(r => r.IsSuccessStatusCode) == 1, "Only one concurrent password change can succeed");
    foreach (var result in results) result.Dispose();
    Check((await ReadUser(employee.Id)).AuthVersion == 5, "Password change compare-and-swap prevents overwriting concurrent changes");
    Check((await ReadUser(foreign.Id)).AuthVersion == 0, "Other branch unchanged");
    await CheckStatus(HttpStatusCode.Forbidden, $"/api/users/{employee.Id}/change-password", adminSession.AccessToken,
        new { currentPassword = "Private-one-123", newPassword = "Other-pass-123" });
    Console.WriteLine($"PASS {count} password reset checks: real HTTP/JWT, PostgreSQL migration, role/branch isolation, temporary access, PIN restrictions, WebSocket revocation and concurrency.");
}
finally { await app.StopAsync(); }
