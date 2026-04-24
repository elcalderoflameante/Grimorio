using Grimorio.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Grimorio.API.Authorization;

/// <summary>
/// Bypasea cualquier política si el usuario tiene rol Administrador.
/// </summary>
public class AdminBypassHandler : AuthorizationHandler<IAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        IAuthorizationRequirement requirement)
    {
        if (context.User.IsInRole(AppConstants.Roles.Admin))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
