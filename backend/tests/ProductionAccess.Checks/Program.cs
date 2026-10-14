using System.Reflection;
using System.Security.Claims;
using Grimorio.API.Authorization;
using Grimorio.API.Controllers;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Inventory.Commands;
using Grimorio.Application.Features.Inventory.Queries;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

var checks = 0;
void Check(bool value)
{
    if (!value) throw new Exception($"Production access check {checks + 1} failed.");
    checks++;
}
var branchId = Guid.NewGuid();
ClaimsPrincipal User(string? role, params string[] permissions) => new(new ClaimsIdentity(
    new[] { new Claim(AppConstants.Claims.BranchId, branchId.ToString()) }
        .Concat(role is null ? [] : new[] { new Claim(ClaimTypes.Role, role) })
        .Concat(permissions.Select(p => new Claim(AppConstants.Claims.Permissions, p))), "checks"));
var cashier = User(AppConstants.Roles.Cashier,
    AppConstants.Permissions.InventoryProductionCreate, AppConstants.Permissions.InventoryProductionRecipesView);
var admin = User(AppConstants.Roles.Admin);
var employee = User(AppConstants.Roles.Kitchen, AppConstants.Permissions.InventoryProductionRecipesView);
var supervisor = User(AppConstants.Roles.Supervisor, AppConstants.Permissions.InventoryProductionRecipesRestrictedView);
using var services = new ServiceCollection().AddLogging()
    .AddAuthorization(ProductionAuthorization.AddPolicies).BuildServiceProvider();
var authorization = services.GetRequiredService<IAuthorizationService>();
foreach (var policy in new[] { ProductionAuthorization.RegisterPolicy, ProductionAuthorization.HistoryPolicy })
{
    Check((await authorization.AuthorizeAsync(cashier, null, policy)).Succeeded);
    Check((await authorization.AuthorizeAsync(admin, null, policy)).Succeeded);
    Check(!(await authorization.AuthorizeAsync(employee, null, policy)).Succeeded);
    Check(!(await authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, policy)).Succeeded);
}
Check(!ProductionAuthorization.CanAccessRestrictedRecipes(cashier));
Check(!ProductionAuthorization.CanAccessRestrictedRecipes(supervisor));
Check(ProductionAuthorization.CanAccessRestrictedRecipes(admin));
Check((await authorization.AuthorizeAsync(User(AppConstants.Roles.Warehouse,
    AppConstants.Permissions.InventoryMovementsCreate), null, ProductionAuthorization.RegisterPolicy)).Succeeded);
Check((await authorization.AuthorizeAsync(User(AppConstants.Roles.Warehouse,
    AppConstants.Permissions.InventoryMovementsView), null, ProductionAuthorization.HistoryPolicy)).Succeeded);
Check(typeof(InventoryController).GetMethod(nameof(InventoryController.UpsertProductionRecipe))!
    .GetCustomAttributes<AuthorizeAttribute>().Any(a => a.Policy == "AdminOnly"));

foreach (var user in new[] { cashier, admin })
{
    var mediator = DispatchProxy.Create<IMediator, RecordingMediator>();
    var recorder = (RecordingMediator)(object)mediator;
    var controller = new InventoryController(mediator, null!)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } },
    };
    var restricted = user.IsInRole(AppConstants.Roles.Admin);
    await controller.GetProductionRecipes(true);
    Check(recorder.Requests.OfType<GetProductionRecipesQuery>().Last().IncludeRestricted == restricted);
    await controller.GetProductionRecipeByArticle(Guid.NewGuid());
    Check(recorder.Requests.OfType<GetProductionRecipeByArticleQuery>().Last().IncludeRestricted == restricted);
    await controller.GetProductionOrders(null, null, null, null);
    Check(recorder.Requests.OfType<GetProductionOrdersQuery>().Last().IncludeRestricted == restricted);
    await controller.RegisterProduction(new RegisterProductionDto { ProductionRecipeId = Guid.NewGuid(), OutputQuantity = 1 });
    Check(recorder.Requests.OfType<RegisterProductionCommand>().Last().IncludeRestrictedRecipes == restricted);
    var result = (OkObjectResult)await controller.GetProductionOptions();
    var articles = (List<InventoryArticleDto>)result.Value!.GetType().GetProperty("Articles")!.GetValue(result.Value)!;
    Check(articles.Count == (restricted ? 2 : 1));
    if (!restricted) Check(articles.Single().Id == RecordingMediator.GeneralArticleId);
    Check(recorder.Requests.All(r => (Guid)r.GetType().GetProperty("BranchId")!.GetValue(r)! == branchId));
}
Console.WriteLine($"Production access checks passed: {checks}. No database or SRI used.");

public class RecordingMediator : DispatchProxy
{
    public static readonly Guid GeneralArticleId = Guid.NewGuid();
    public List<object> Requests { get; } = [];
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        var request = args![0]!;
        Requests.Add(request);
        object? result = request switch
        {
            GetProductionRecipesQuery => new List<ProductionRecipeDto> { new() { OutputArticleId = GeneralArticleId } },
            GetProductionRecipeByArticleQuery => null,
            GetProductionOrdersQuery => new List<ProductionOrderDto>(),
            RegisterProductionCommand => new ProductionOrderDto(),
            GetInventoryArticlesQuery => new List<InventoryArticleDto> { new() { Id = GeneralArticleId }, new() { Id = Guid.NewGuid() } },
            GetWarehousesQuery => new List<WarehouseDto>(),
            GetMeasurementUnitsQuery => new List<MeasurementUnitDto>(),
            GetUnitConversionsQuery => new List<UnitConversionDto>(),
            _ => throw new Exception($"Unexpected request {request.GetType().Name}."),
        };
        return typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(method!.ReturnType.GenericTypeArguments[0]).Invoke(null, [result]);
    }
}
