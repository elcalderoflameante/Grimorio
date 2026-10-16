using Grimorio.Application.Features.Users.Commands;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Users.Commands;

public class ResetUserPasswordCommandHandler(GrimorioDbContext db, IPasswordHashingService passwords)
    : IRequestHandler<ResetUserPasswordCommand, bool>
{
    public async Task<bool> Handle(ResetUserPasswordCommand request, CancellationToken ct)
    {
        var validation = await new ResetUserPasswordCommandValidator().ValidateAsync(request, ct);
        if (!validation.IsValid) throw new InvalidOperationException(validation.Errors[0].ErrorMessage);
        if (request.UserId == request.ActorUserId)
            throw new InvalidOperationException("Cambia tu propia contrasena desde Mi Perfil.");
        var admin = await db.Users.Where(UserAccessPolicy.ActiveAccount).AnyAsync(u => u.Id == request.ActorUserId
            && u.BranchId == request.BranchId && u.UserRoles.Any(role => role.BranchId == request.BranchId && !role.IsDeleted
                && role.Role != null && role.Role.IsActive && !role.Role.IsDeleted
                && role.Role.Name == SharedKernel.Constants.AppConstants.Roles.Admin), ct);
        if (!admin) throw new InvalidOperationException("Solo un administrador activo de esta sucursal puede restablecer contrasenas.");
        var hash = passwords.HashPassword(request.TemporaryPassword);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var changed = await db.Users.Where(u => u.Id == request.UserId && u.BranchId == request.BranchId && !u.IsDeleted)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(u => u.PasswordHash, hash)
                .SetProperty(u => u.MustChangePassword, true)
                .SetProperty(u => u.AuthVersion, u => u.AuthVersion + 1)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow)
                .SetProperty(u => u.UpdatedBy, request.ActorUserId), ct);
        if (changed == 0) throw new InvalidOperationException("Usuario no encontrado en esta sucursal.");
        await db.UserPushTokens.Where(token => token.UserId == request.UserId && token.BranchId == request.BranchId)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.IsActive, false)
                .SetProperty(token => token.UpdatedAt, DateTime.UtcNow).SetProperty(token => token.UpdatedBy, request.ActorUserId), ct);
        await transaction.CommitAsync(ct);
        return true;
    }
}
