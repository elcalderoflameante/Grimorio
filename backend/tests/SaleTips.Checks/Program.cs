using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Grimorio.API.Controllers;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Application.Features.Finance;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.Finance;
using Grimorio.Infrastructure.Persistence;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var connection = Environment.GetEnvironmentVariable("TIPS_CHECKS_CONNECTION") ?? throw new Exception("Set TIPS_CHECKS_CONNECTION.");
var target = new NpgsqlConnectionStringBuilder(connection);
if (target.Host != "127.0.0.1" || target.Port != 15432 || target.Database != "checks" || target.Username != "checks")
    throw new Exception("Only dedicated localhost:15432 checks database is allowed.");
var checks = 0;
void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Logging.ClearProviders();
const string secret = "sale-tips-checks-secret-only-never-use-in-production-1234567890";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
builder.Services.AddDbContext<GrimorioDbContext>(options => options.UseNpgsql(connection));
builder.Services.AddMediatR(options => options.RegisterServicesFromAssembly(typeof(SaleTipHandlers).Assembly));
builder.Services.AddControllers().AddApplicationPart(typeof(SaleTipsController).Assembly);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuerSigningKey = true, IssuerSigningKey = signingKey,
        ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true,
    });
builder.Services.AddAuthorization(options => {
    options.AddPolicy("Billing.Cash.View", policy => policy.RequireClaim("permission", "view"));
    options.AddPolicy("Billing.Cash.Charge", policy => policy.RequireClaim("permission", "charge"));
});
await using var app = builder.Build();
app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
var branch = new Branch { Name = "Tips checks", Code = "TIPS" };
var otherBranch = new Branch { Name = "Other", Code = "OTHER" };
var user = new User { BranchId = branch.Id, FirstName = "Checks", LastName = "Cashier", Email = "tips@checks.invalid", PasswordHash = "checks" };
var register = new CashRegister { BranchId = branch.Id, Name = "Checks", Code = "CHECKS" };
var session = new CashSession { BranchId = branch.Id, CashRegisterId = register.Id, OpenedBy = user.Id, OpeningBalance = 10 };
var method = new PaymentMethodConfig { Name = "Cash", IsCash = true };
Order SaleOrder(int number, Guid branchId) => new() { BranchId = branchId, Number = number, Type = OrderType.Takeout,
    Status = OrderStatus.Delivered, Total = 20, Subtotal = 20, PaidAt = DateTime.UtcNow };
var orders = Enumerable.Range(1, 4).Select(n => SaleOrder(n, branch.Id)).Append(SaleOrder(1, otherBranch.Id)).ToArray();
var payments = orders.Select(o => new OrderPayment { BranchId = o.BranchId, OrderId = o.Id, OrderAmount = 20,
    CashSessionId = o.BranchId == branch.Id ? session.Id : null }).ToArray();
async Task Read(Func<GrimorioDbContext, Task> action)
{
    await using var scope = app.Services.CreateAsyncScope();
    await action(scope.ServiceProvider.GetRequiredService<GrimorioDbContext>());
}
async Task<T> Send<T>(IRequest<T> req)
{
    await using var scope = app.Services.CreateAsyncScope();
    return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(req);
}
await Read(async db => {
    if (Environment.GetEnvironmentVariable("TIPS_CHECKS_ALLOW_RESET") == "true") await db.Database.EnsureDeletedAsync();
    await db.Database.MigrateAsync();
    Check(!db.Database.HasPendingModelChanges(), "Migration matches model");
    db.AddRange(branch, otherBranch, user, register, session, method);
    db.AddRange(orders); db.AddRange(payments);
    foreach (var payment in payments.Take(4)) db.PaymentLines.Add(new PaymentLine { BranchId = branch.Id,
        OrderPaymentId = payment.Id, PaymentMethodConfigId = method.Id, AmountTendered = 20 });
    await db.SaveChangesAsync();
});
var beforeCash = JsonSerializer.Serialize(await Send(new GetActiveCashSessionQuery { BranchId = branch.Id, UserId = user.Id }));
var beforeSales = JsonSerializer.Serialize(await Send(new GetSalesQuery { BranchId = branch.Id }));
string Token(Guid branchId, bool canCharge = true, bool canView = true)
{
    var claims = new List<Claim> { new(AppConstants.Claims.BranchId, branchId.ToString()), new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(AppConstants.Claims.FirstName, "Checks"), new(AppConstants.Claims.LastName, "Cashier") };
    if (canCharge) claims.Add(new Claim("permission", "charge"));
    if (canView) claims.Add(new Claim("permission", "view"));
    return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddMinutes(15),
        signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)));
}
await app.StartAsync();
try
{
    using var client = new HttpClient { BaseAddress = new Uri(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
    var token = Token(branch.Id);
    async Task<HttpResponseMessage> Request(HttpMethod method, string path, object? body = null, string? credential = null)
    {
        using var req = new HttpRequestMessage(method, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential ?? token);
        if (body != null) req.Content = JsonContent.Create(body);
        return await client.SendAsync(req);
    }
    async Task<SaleTipDto> Save(int index, decimal amount, string method)
    {
        using var response = await Request(HttpMethod.Put, $"/api/sale-tips/{payments[index].Id}", new { amount, method });
        Check(response.StatusCode == HttpStatusCode.OK, $"Save {method}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<SaleTipDto>())!;
    }
    var path = $"/api/sale-tips/{payments[0].Id}";
    using (var absent = await Request(HttpMethod.Get, path))
        Check(absent.StatusCode == HttpStatusCode.OK && await absent.Content.ReadAsStringAsync() == "null", "Absent tip is explicit null");
    using (var denied = await Request(HttpMethod.Put, path, new { amount = 1, method = "Cash" }, Token(branch.Id, false)))
        Check(denied.StatusCode == HttpStatusCode.Forbidden, "Read-only user cannot save tips");
    using (var denied = await Request(HttpMethod.Delete, path, credential: Token(branch.Id, false)))
        Check(denied.StatusCode == HttpStatusCode.Forbidden, "Read-only user cannot delete tips");
    using (var denied = await Request(HttpMethod.Get, "/api/sale-tips", credential: Token(branch.Id, canView: false)))
        Check(denied.StatusCode == HttpStatusCode.Forbidden, "Report requires sales view permission");
    foreach (var amount in new[] { -1m, 0m, 0.001m, 100000000m })
    {
        using var response = await Request(HttpMethod.Put, path, new { amount, method = "Cash" });
        Check(response.StatusCode == HttpStatusCode.BadRequest, "Reject invalid amount");
    }
    foreach (var invalidMethod in new[] { "Other", "cash", "0", "EmployeeConsumption" })
    {
        using var response = await Request(HttpMethod.Put, path, new { amount = 1, method = invalidMethod });
        Check(response.StatusCode == HttpStatusCode.BadRequest, "Reject invalid method");
    }
    using (var missing = await Request(HttpMethod.Put, $"/api/sale-tips/{Guid.NewGuid()}", new { amount = 1, method = "Cash" }))
        Check(missing.StatusCode == HttpStatusCode.NotFound, "Reject nonexistent sale");
    using (var foreign = await Request(HttpMethod.Put, $"/api/sale-tips/{payments[4].Id}", new { amount = 1, method = "Cash" }))
        Check(foreign.StatusCode == HttpStatusCode.NotFound, "Cannot attach to another branch sale");
    var first = await Save(0, 2, "Cash");
    var repeated = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Save(0, 2, "Cash")));
    Check(repeated.All(t => t.Id == first.Id && t.RegisteredAt == first.RegisteredAt), "Concurrent retries preserve one tip and original registration date");
    var updated = await Save(0, 3, "Card");
    Check(updated.Id == first.Id && updated.RegisteredAt == first.RegisteredAt && updated.UpdatedByName == "Checks Cashier", "Edit keeps identity, date and user audit");
    await Save(1, 1.25m, "Cash"); await Save(2, 2.50m, "Transfer");
    await Read(async db => { var old = new Grimorio.Domain.Entities.Finance.SaleTip {
        BranchId = branch.Id, OrderPaymentId = payments[3].Id, Amount = 10, Method = Grimorio.Domain.Entities.Finance.TipMethod.Cash,
        RegisteredAt = DateTime.UtcNow.AddDays(-10), RegisteredByName = "Older" }; db.Add(old); await db.SaveChangesAsync(); });
    var from = Uri.EscapeDataString(DateTime.UtcNow.AddDays(-1).ToString("O"));
    var to = Uri.EscapeDataString(DateTime.UtcNow.AddDays(1).ToString("O"));
    var reportPath = $"/api/sale-tips?from={from}&to={to}&pageSize=1";
    using (var reportResponse = await Request(HttpMethod.Get, reportPath))
    {
        var report = (await reportResponse.Content.ReadFromJsonAsync<SaleTipReportDto>())!;
        Check(report.TotalAmount == 6.75m && report.Count == 3 && report.Items.Count == 1, "Date-filtered report totals cover all pages");
        Check(report.ByMethod.Single(g => g.Method == "Transfer").Amount == 2.50m && report.ByMethod.Count == 3, "Three method totals");
    }
    using (var filtered = await Request(HttpMethod.Get, reportPath + "&method=Card&page=2"))
    {
        var report = (await filtered.Content.ReadFromJsonAsync<SaleTipReportDto>())!;
        Check(report.TotalAmount == 3 && report.Count == 1 && report.Items.Count == 0, "Method filter, totals and pagination");
    }
    using (var invalid = await Request(HttpMethod.Get, $"/api/sale-tips?from={to}&to={from}"))
        Check(invalid.StatusCode == HttpStatusCode.BadRequest, "Reject reversed dates");
    using (var foreign = await Request(HttpMethod.Get, path, credential: Token(otherBranch.Id)))
        Check(foreign.StatusCode == HttpStatusCode.NotFound, "Cannot read another branch tip");
    using (var foreign = await Request(HttpMethod.Delete, path, credential: Token(otherBranch.Id)))
        Check(foreign.StatusCode == HttpStatusCode.NotFound, "Cannot delete another branch tip");
    using (var report = await Request(HttpMethod.Get, "/api/sale-tips", credential: Token(otherBranch.Id)))
        Check((await report.Content.ReadFromJsonAsync<SaleTipReportDto>())!.Count == 0, "Report is branch-isolated");
    using (var deleted = await Request(HttpMethod.Delete, path)) Check(deleted.StatusCode == HttpStatusCode.NoContent, "Delete tip");
    using (var repeatedDelete = await Request(HttpMethod.Delete, path)) Check(repeatedDelete.StatusCode == HttpStatusCode.NoContent, "Delete retry is harmless");
    await Read(async db => Check(await db.SaleTips.IgnoreQueryFilters().AnyAsync(t => t.Id == first.Id && t.IsDeleted && t.DeletedAt != null), "Deleted tip remains in audit history"));
    var replacement = await Save(0, 4, "Cash");
    Check(replacement.Id != first.Id, "Re-register after deletion preserves old audit record");
    Check(JsonSerializer.Serialize(await Send(new GetActiveCashSessionQuery { BranchId = branch.Id, UserId = user.Id })) == beforeCash,
        "Cash balance, payment-method totals, expected cash and sales remain identical");
    Check(JsonSerializer.Serialize(await Send(new GetSalesQuery { BranchId = branch.Id })) == beforeSales, "Sales amounts, receipts and payment lines remain identical");
    await Read(async db => Check(!await db.Expenses.AnyAsync() && !await db.StockMovements.AnyAsync() && !await db.ElectronicDocuments.AnyAsync(),
        "No expense, inventory movement or electronic invoice created by tips"));
    Console.WriteLine($"Sale tip checks passed: {checks}. Isolated PostgreSQL and HTTP, no production services.");
}
finally { await app.StopAsync(); }
