using System.Security.Claims;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Grimorio.API.Services;

public static class UserSessionValidation
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (!Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !Guid.TryParse(principal?.FindFirstValue("BranchId"), out var branchId))
        {
            context.Fail("La cuenta no es valida.");
            return;
        }
        var versionClaim = principal?.FindFirstValue("auth_version");
        var version = 0;
        if (versionClaim is not null && (!int.TryParse(versionClaim, out version) || version < 0))
        {
            context.Fail("La sesion no es valida.");
            return;
        }
        var request = context.HttpContext.Request;
        var passwordChange = HttpMethods.IsPost(request.Method)
            && string.Equals(request.Path.Value?.TrimEnd('/'), $"/api/users/{userId}/change-password", StringComparison.OrdinalIgnoreCase);
        var db = context.HttpContext.RequestServices.GetRequiredService<GrimorioDbContext>();
        if (!await UserAccessPolicy.HasCurrentSessionAsync(db, userId, branchId, version, passwordChange, request.HttpContext.RequestAborted))
            context.Fail("La sesion expiro o debes cambiar tu contrasena temporal.");
    }
}
