using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Enums;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.API.Services;

public sealed class AttendanceKioskAuthenticator
{
    public const string KioskIdHeader = "X-Grimorio-Kiosk-Id";
    public const string ApiKeyHeader = "X-Grimorio-Kiosk-Key";
    private readonly GrimorioDbContext _context;
    private readonly IPasswordHashingService _passwordHashing;

    public AttendanceKioskAuthenticator(GrimorioDbContext context, IPasswordHashingService passwordHashing)
    {
        _context = context;
        _passwordHashing = passwordHashing;
    }

    public async Task<AttendanceKioskDevice?> AuthenticateAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Headers[KioskIdHeader].FirstOrDefault(), out var kioskId)) return null;
        var apiKey = request.Headers[ApiKeyHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        var kiosk = await _context.AttendanceKioskDevices.FirstOrDefaultAsync(
            x => x.Id == kioskId && x.Status == KioskDeviceStatus.Active && !x.IsDeleted,
            cancellationToken);
        if (kiosk is null || !_passwordHashing.VerifyPassword(apiKey, kiosk.ApiKeyHash)) return null;

        kiosk.LastSeenAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return kiosk;
    }
}
