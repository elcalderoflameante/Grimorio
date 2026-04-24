using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.Features.TableService.Commands;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.TableService.Commands;

public class RegisterPushTokenCommandHandler : IRequestHandler<RegisterPushTokenCommand, Unit>
{
    private readonly GrimorioDbContext _context;

    public RegisterPushTokenCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<Unit> Handle(RegisterPushTokenCommand request, CancellationToken cancellationToken)
    {
        var normalizedToken = request.Token.Trim();
        var existingToken = await _context.UserPushTokens
            .FirstOrDefaultAsync(t => t.Token == normalizedToken, cancellationToken);

        if (existingToken == null)
        {
            _context.UserPushTokens.Add(new UserPushToken
            {
                UserId = request.UserId,
                BranchId = request.BranchId,
                Token = normalizedToken,
                Platform = string.IsNullOrWhiteSpace(request.Platform)
                    ? "android"
                    : request.Platform.Trim().ToLowerInvariant(),
                DeviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? null : request.DeviceId.Trim(),
                LastSeenAt = DateTime.UtcNow,
                IsActive = true,
                CreatedBy = request.UserId,
            });
        }
        else
        {
            existingToken.UserId = request.UserId;
            existingToken.BranchId = request.BranchId;
            existingToken.Platform = string.IsNullOrWhiteSpace(request.Platform)
                ? existingToken.Platform
                : request.Platform.Trim().ToLowerInvariant();
            existingToken.DeviceId = string.IsNullOrWhiteSpace(request.DeviceId)
                ? existingToken.DeviceId
                : request.DeviceId.Trim();
            existingToken.LastSeenAt = DateTime.UtcNow;
            existingToken.IsActive = true;
            existingToken.IsDeleted = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public class DeactivatePushTokenCommandHandler : IRequestHandler<DeactivatePushTokenCommand, Unit>
{
    private readonly GrimorioDbContext _context;

    public DeactivatePushTokenCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<Unit> Handle(DeactivatePushTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await _context.UserPushTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token.Trim(), cancellationToken);

        if (existingToken != null)
        {
            existingToken.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
