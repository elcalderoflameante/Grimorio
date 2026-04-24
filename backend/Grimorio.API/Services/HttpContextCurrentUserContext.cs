using System.Security.Claims;
using Grimorio.Application.Abstractions;
using Grimorio.SharedKernel.Constants;

namespace Grimorio.API.Services;

public class HttpContextCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var claim = user?.FindFirst(AppConstants.Claims.UserId)
                ?? user?.FindFirst(ClaimTypes.NameIdentifier)
                ?? user?.FindFirst("sub");

            return claim != null && Guid.TryParse(claim.Value, out var userId)
                ? userId
                : Guid.Empty;
        }
    }
}
