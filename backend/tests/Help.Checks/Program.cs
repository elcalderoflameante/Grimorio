using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Grimorio.API.Controllers;
using Grimorio.API.Services.Help;
using Grimorio.SharedKernel.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;

var count = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); count++; }
ClaimsPrincipal Person(params string[] permissions) => new(new ClaimsIdentity(permissions.Select(p => new Claim(AppConstants.Claims.Permissions, p)), "Checks"));
var catalog = new HelpCatalog();
var admin = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, AppConstants.Roles.Admin)], "Checks"));
var counter = Person(AppConstants.Permissions.InventoryCountsExecute);
var viewer = Person(AppConstants.Permissions.InventoryCountsView);
var manager = Person(AppConstants.Permissions.InventoryCountsManage);
var none = Person();
var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
foreach (var (id, permission) in new (string, string)[] {
    ("pedidos-operar", "POS.Orders.View"),
    ("pedidos-qr", "POS.TableRequests.View"),
    ("menu-recetas", "Menu.Items.View"),
    ("menu-promociones", "POS.Orders.Update"),
    ("inventario-catalogos", "Inventory.Articles.View"),
    ("inventario-movimientos", "Inventory.Movements.View"),
    ("inventario-produccion", "Inventory.Production.Create"),
    ("compras-registrar", "Purchases.Orders.View"),
    ("clientes-gestionar", "Billing.Customers.View"),
    ("facturacion-documentos", "Billing.Sri.View"),
    ("facturacion-configurar", "Billing.Tax.View"),
    ("finanzas-gastos", "Finance.Expenses.View"),
    ("personal-horarios", "RRHH.Employees.View"),
    ("personal-asistencia", "RRHH.Attendance.View"),
    ("personal-nomina", "RRHH.Payroll.View"),
    ("admin-accesos", "Admin.Users.View"),
    ("operaciones-responsabilidades", "Operations.Responsibilities.View"),
    ("fidelizacion-programa", "Marketing.Loyalty.View"),
    ("apps-meseros", "POS.TableRequests.View"),
    ("apps-estaciones", "POS.Kitchen.View"),
    ("comunicacion-walkie", "POS.Orders.View"),
}) {
    Check(catalog.Find(Person(permission), id) is not null, "Module guide allowed: " + id);
    Check(catalog.Find(none, id) is null, "Module guide denies unrelated user: " + id);
    Check(catalog.Find(anonymous, id) is null, "Module guide denies anonymous user: " + id);
}
Check(catalog.List(admin).Articles.Count == 33, "Admin sees thirty-three operational guides");
var cashier = Person("Billing.Cash.View", "Billing.Cash.Charge");
var corrector = Person("Billing.Cash.View", "Billing.Payments.CorrectMethod", "Billing.PaymentMethods.View");
Check(catalog.Find(cashier, "caja-turno") is not null, "Cash viewer can read shift guide");
Check(catalog.Find(cashier, "ventas-cobrar") is not null, "Charger can read collection guide");
Check(catalog.Find(cashier, "ventas-corregir-pagos") is null, "Cash access does not grant correction guide");
Check(catalog.Find(corrector, "ventas-corregir-pagos") is not null, "Corrector with all prerequisites can read guide");
Check(catalog.Find(Person("Billing.Payments.CorrectMethod"), "ventas-corregir-pagos") is null, "Correction guide requires all permissions");
Check(catalog.Find(counter, "ventas-cobrar") is null, "Inventory permission does not grant payment guide");
Check(catalog.List(corrector, "reclasifica").Articles.Any(a => a.Id == "ventas-corregir-pagos"), "Payment guide body is searchable");
Check(catalog.List(counter, "reclasifica").Articles.Count == 0, "Payment search respects permissions");
if (args is ["--published-api", var apiPath]) {
    var api = System.Reflection.Assembly.LoadFile(Path.GetFullPath(apiPath));
    var resources = api.GetManifestResourceNames().Where(name => name.StartsWith("Grimorio.Help.", StringComparison.Ordinal)).ToHashSet();
    Check(resources.Count == 36, "API packages exactly catalogue, thirty-three guides and two captures");
    foreach (var article in catalog.List(admin).Articles)
        Check(resources.Contains($"Grimorio.Help.articles/{article.Id}.md"), "Packaged article: " + article.Id);
    foreach (var asset in new[] { "inventario-configuracion", "inventario-revision" })
        Check(resources.Contains($"Grimorio.Help.assets/{asset}.png"), "Packaged capture: " + asset);
}
Check(catalog.List(anonymous).Articles.Count == 0, "Anonymous cannot build a search index");
Check(catalog.List(none).Articles.Select(a => a.Id).SequenceEqual(["inicio"]), "Unrelated user only sees introduction");
Check(catalog.Find(counter, "inventario-contar") is not null, "Counter can read counting guide");
Check(catalog.Find(counter, "inventario-configurar") is null, "Counter cannot read configuration");
Check(catalog.Find(counter, "inventario-revisar") is null, "Counter cannot read review");
Check(catalog.Find(viewer, "inventario-revisar") is not null, "Viewer can read review guide");
Check(catalog.Find(viewer, "inventario-configurar") is null, "Viewer cannot read configuration");
Check(catalog.Find(manager, "inventario-configurar") is not null, "Manager can read configuration");
Check(catalog.Find(manager, "inventario-contar") is null, "Manage does not grant Execute");
Check(catalog.Find(anonymous, "inicio") is null, "Direct article access checks authentication");
Check(catalog.List(counter, "RECONTEO").Articles.Any(a => a.Id == "inventario-contar"), "Search includes body content");
Check(catalog.List(admin, "instalacion").Articles.Any(a => a.Id == "inventario-instalar"), "Accent-insensitive search");
Check(catalog.List(counter, "areas frecuencias").Articles.All(a => a.Id != "inventario-configurar"), "Search filters permission before content matching");
Check(catalog.List(admin, "zxqnotfound").Articles.Count == 0, "No matches");
foreach (var path in new[] { "../README", "../../.env", "deploy-cloudcone", "catalog.json", "articles/inicio.md", "INICIO" })
    Check(catalog.Find(admin, path) is null, "Article allowlist: " + path);
foreach (var asset in new[] { "../README", "catalog.json", "inventario-configuracion.png" })
    Check(catalog.Image(admin, asset) is null, "Asset allowlist: " + asset);
Check(catalog.Image(counter, "inventario-configuracion") is null, "Image permission is server-side");
Check(catalog.Image(none, "inventario-revision") is null, "Unrelated user cannot download review capture");
foreach (var asset in new[] { "inventario-configuracion", "inventario-revision" }) {
    var image = catalog.Image(admin, asset)!;
    Check(image.Length > 1000 && image.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }), "Valid embedded PNG: " + asset);
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production", Args = [] });
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Logging.ClearProviders();
builder.Services.AddSingleton<HelpCatalog>();
builder.Services.AddControllers().AddApplicationPart(typeof(HelpController).Assembly);
builder.Services.AddAuthentication("Checks").AddScheme<AuthenticationSchemeOptions, ChecksAuth>("Checks", _ => { });
builder.Services.AddAuthorization();
await using var app = builder.Build();
app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
await app.StartAsync();
try {
    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    using var client = new HttpClient { BaseAddress = new Uri(address) };
    foreach (var path in new[] { "/api/help/articles", "/api/help/articles/inicio", "/api/help/assets/inventario-revision" })
        Check((await client.GetAsync(path)).StatusCode == HttpStatusCode.Unauthorized, "HTTP auth required: " + path);
    client.DefaultRequestHeaders.Add("X-Checks-User", "counter");
    Check((await client.GetAsync("/api/help/articles")).StatusCode == HttpStatusCode.NotFound, "Production defaults disabled");
    builder.Configuration["Help:Enabled"] = "true";
    var index = await client.GetFromJsonAsync<HelpIndex>("/api/help/articles");
    Check(index!.Articles.Any(a => a.Id == "inventario-contar"), "HTTP authorized index");
    Check(index.Articles.All(a => a.Id != "inventario-configurar"), "HTTP restricted index");
    Check((await client.GetAsync("/api/help/articles/inventario-configurar")).StatusCode == HttpStatusCode.NotFound, "HTTP direct access denied");
    Check((await client.GetAsync("/api/help/assets/inventario-configuracion")).StatusCode == HttpStatusCode.NotFound, "HTTP image access denied");
    Check((await client.GetAsync("/api/help/articles?q=" + new string('a', 121))).StatusCode == HttpStatusCode.BadRequest, "Bounded query");
    var response = await client.GetAsync("/api/help/articles/inventario-contar");
    Check(response.Headers.CacheControl?.NoStore == true, "Article cannot enter a shared cache");
    client.DefaultRequestHeaders.Remove("X-Checks-User"); client.DefaultRequestHeaders.Add("X-Checks-User", "admin");
    var picture = await client.GetAsync("/api/help/assets/inventario-configuracion");
    Check(picture.IsSuccessStatusCode && picture.Content.Headers.ContentType?.MediaType == "image/png", "Authenticated image endpoint");
    Check(picture.Headers.CacheControl?.NoStore == true, "Image no-store");
    foreach (var path in new[] { "/docs/manual/catalog.json", "/manual/articles/inicio.md", "/api/help/articles/deploy-cloudcone", "/api/help/assets/deploy-cloudcone" })
        Check((await client.GetAsync(path)).StatusCode == HttpStatusCode.NotFound, "No static/private-document exposure: " + path);
} finally { await app.StopAsync(); }
Console.WriteLine($"{count} help checks passed. No database, real accounts or external services.");

sealed class ChecksAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var user = Request.Headers["X-Checks-User"].ToString();
        if (user is not ("counter" or "admin")) return Task.FromResult(AuthenticateResult.NoResult());
        var claims = user == "admin" ? new[] { new Claim(ClaimTypes.Role, AppConstants.Roles.Admin) }
            : [new Claim(AppConstants.Claims.Permissions, AppConstants.Permissions.InventoryCountsExecute)];
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
    }
}
