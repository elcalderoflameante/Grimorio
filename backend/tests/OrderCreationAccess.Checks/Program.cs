using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Grimorio.API.Authorization;
using Grimorio.API.Controllers;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.IdentityModel.Tokens;

var count = 0;
void Check(bool value, string label) { if (!value) throw new Exception(label); count++; }
var branchId = Guid.NewGuid();
var userId = Guid.NewGuid();
var tableId = Guid.NewGuid();
var menuItemId = Guid.NewGuid();
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Logging.ClearProviders();
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("order-creation-access-checks-only-not-for-production-1234567890"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuerSigningKey = true, IssuerSigningKey = key,
        ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true,
    });
builder.Services.AddAuthorization(options => options.AddPolicy(AppConstants.Permissions.PosOrdersCreate,
    policy => policy.RequireClaim(AppConstants.Claims.Permissions, AppConstants.Permissions.PosOrdersCreate)));
builder.Services.AddSingleton<IAuthorizationHandler, AdminBypassHandler>();
var mediator = DispatchProxy.Create<IMediator, OrderMediator>();
var recorder = (OrderMediator)(object)mediator;
builder.Services.AddSingleton(mediator);
builder.Services.AddSignalR();
builder.Services.AddControllers().AddApplicationPart(typeof(PosController).Assembly);
await using var app = builder.Build();
app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
await app.StartAsync();
try
{
    using var client = new HttpClient { BaseAddress = new Uri(app.Services.GetRequiredService<IServer>()
        .Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
    string Token(string[] roles, bool charge, bool create = true, bool branch = true, string userClaim = ClaimTypes.NameIdentifier)
    {
        var claims = roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();
        claims.Add(new Claim(userClaim, userId.ToString()));
        if (branch) claims.Add(new Claim(AppConstants.Claims.BranchId, branchId.ToString()));
        if (create) claims.Add(new Claim(AppConstants.Claims.Permissions, AppConstants.Permissions.PosOrdersCreate));
        if (charge) claims.Add(new Claim(AppConstants.Claims.Permissions, AppConstants.Permissions.BillingCashCharge));
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
    }
    async Task Request(string label, string? token, string type, Guid? table, HttpStatusCode expected)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/pos/ordenes");
        if (token != null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new CreateOrderDto { Type = type, TableId = table, CustomerName = "Delivery checks",
            DeliveryAddress = "Test address", Items = [new() { MenuItemId = menuItemId, Quantity = 1 }] });
        var before = recorder.Requests.Count;
        using var response = await client.SendAsync(req);
        Check(response.StatusCode == expected, $"{label}: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        if (expected != HttpStatusCode.OK)
        {
            Check(recorder.Requests.Count == before, $"{label}: rejected before creating an order");
            return;
        }
        Check(recorder.Requests.Count == before + 1, $"{label}: exactly one create command");
        var command = recorder.Requests.Last();
        Check(command.BranchId == branchId && command.WaiterId == userId && command.Type == type && command.TableId == table,
            $"{label}: server branch/user and order type preserved");
        Check(command.DeliveryAddress == "Test address" && command.Items.Single().MenuItemId == menuItemId,
            $"{label}: delivery details and items preserved");
    }
    foreach (var type in new[] { "Delivery", "Takeout" })
    {
        await Request("Waiter and cashier", Token([AppConstants.Roles.Waiter, AppConstants.Roles.Cashier], true), type, null, HttpStatusCode.OK);
        await Request("Cashier and waiter reversed", Token([AppConstants.Roles.Cashier, AppConstants.Roles.Waiter], true), type, null, HttpStatusCode.OK);
        await Request("Waiter with explicit charge permission", Token([AppConstants.Roles.Waiter], true), type, null, HttpStatusCode.OK);
        await Request("Waiter and admin", Token([AppConstants.Roles.Waiter, AppConstants.Roles.Admin], false, create: false), type, null, HttpStatusCode.OK);
        await Request("Waiter only", Token([AppConstants.Roles.Waiter], false), type, null, HttpStatusCode.BadRequest);
        await Request("Cashier role without charge permission", Token([AppConstants.Roles.Waiter, AppConstants.Roles.Cashier], false), type, null, HttpStatusCode.BadRequest);
        await Request("Waiter and kitchen", Token([AppConstants.Roles.Waiter, AppConstants.Roles.Kitchen], false), type, null, HttpStatusCode.BadRequest);
    }
    await Request("Waiter at a table", Token([AppConstants.Roles.Waiter], false), "DineIn", tableId, HttpStatusCode.OK);
    await Request("Waiter without a table", Token([AppConstants.Roles.Waiter], false), "DineIn", null, HttpStatusCode.BadRequest);
    await Request("Combined user at a table", Token([AppConstants.Roles.Waiter, AppConstants.Roles.Cashier], true), "DineIn", tableId, HttpStatusCode.OK);
    await Request("Ordinary cashier behavior unchanged", Token([AppConstants.Roles.Cashier], true), "Delivery", null, HttpStatusCode.OK);
    await Request("Legacy UserId attribution preserved", Token([AppConstants.Roles.Waiter], false, userClaim: AppConstants.Claims.UserId), "DineIn", tableId, HttpStatusCode.OK);
    await Request("Subject claim attribution preserved", Token([AppConstants.Roles.Waiter], false, userClaim: "sub"), "DineIn", tableId, HttpStatusCode.OK);
    await Request("Still requires order creation permission", Token([AppConstants.Roles.Waiter, AppConstants.Roles.Cashier], true, create: false), "Delivery", null, HttpStatusCode.Forbidden);
    await Request("Missing branch rejected", Token([AppConstants.Roles.Waiter, AppConstants.Roles.Cashier], true, branch: false), "Delivery", null, HttpStatusCode.Unauthorized);
    await Request("Anonymous rejected", null, "Delivery", null, HttpStatusCode.Unauthorized);
    Console.WriteLine($"Order creation access checks passed: {count}. Real HTTP/JWT, no database, SRI or inventory changes.");
}
finally { await app.StopAsync(); }

public class OrderMediator : DispatchProxy
{
    public List<CreateOrderCommand> Requests { get; } = [];
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (args?[0] is not CreateOrderCommand command) throw new Exception("Unexpected mediator call.");
        Requests.Add(command);
        return Task.FromResult(new OrderDto { Id = Guid.NewGuid(), Type = command.Type, Status = "Draft" });
    }
}
