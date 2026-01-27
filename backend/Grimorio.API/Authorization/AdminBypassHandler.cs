using Microsoft.AspNetCore.Authorization;

namespace Grimorio.API.Authorization;

/// <summary>
/// Handler personalizado que bypasea cualquier política si el usuario tiene rol "Administrador".
/// Evita repetir el check de Admin en cada política.
/// </summary>
public class AdminBypassHandler : AuthorizationHandler<IAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        IAuthorizationRequirement requirement)
    {
        // Si el usuario tiene rol Administrador, pasa todas las políticas
        if (context.User.IsInRole("Administrador"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Si no es Admin, deja que otras políticas/handlers decidan
        return Task.CompletedTask;
    }
}
