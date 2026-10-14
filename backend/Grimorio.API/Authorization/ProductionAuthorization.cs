using System.Security.Claims;
using Grimorio.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Grimorio.API.Authorization;

public static class ProductionAuthorization
{
    public const string RegisterPolicy = "Inventory.Production.Register";
    public const string HistoryPolicy = "Inventory.Production.History";

    public static bool CanAccessRestrictedRecipes(ClaimsPrincipal user) =>
        user.IsInRole(AppConstants.Roles.Admin);

    public static void AddPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(RegisterPolicy, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
            CanAccessRestrictedRecipes(context.User) || context.User.FindAll(AppConstants.Claims.Permissions).Any(c =>
                c.Value == AppConstants.Permissions.InventoryProductionCreate ||
                c.Value == AppConstants.Permissions.InventoryMovementsCreate)));
        options.AddPolicy(HistoryPolicy, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
            CanAccessRestrictedRecipes(context.User) || context.User.FindAll(AppConstants.Claims.Permissions).Any(c =>
                c.Value == AppConstants.Permissions.InventoryProductionCreate ||
                c.Value == AppConstants.Permissions.InventoryMovementsView)));
    }
}
