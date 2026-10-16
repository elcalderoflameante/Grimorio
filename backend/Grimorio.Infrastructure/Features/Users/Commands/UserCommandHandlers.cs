using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Users.Commands;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using Grimorio.Infrastructure.Features.Users;
using Npgsql;

namespace Grimorio.Infrastructure.Features.Users.Commands;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly GrimorioDbContext _context;
    private readonly IPasswordHashingService _passwordHashingService;

    public CreateUserCommandHandler(GrimorioDbContext context, IPasswordHashingService passwordHashingService)
    {
        _context = context;
        _passwordHashingService = passwordHashingService;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Dto.Email && !u.IsDeleted, cancellationToken);

        if (existingUser != null)
            throw new InvalidOperationException("Ya existe un usuario con ese email.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Dto.Email,
            PasswordHash = _passwordHashingService.HashPassword(request.Dto.Password),
            FirstName = request.Dto.FirstName,
            LastName = request.Dto.LastName,
            IsActive = true,
            BranchId = request.BranchId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        user.Employee = await UserEmployeeLink.ValidateAsync(_context, user.BranchId, user.Id,
            request.Dto.EmployeeId, null, user.IsActive, cancellationToken);
        user.EmployeeId = user.Employee?.Id;

        _context.Users.Add(user);
        try { await _context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: "23505", ConstraintName: "IX_Users_EmployeeId" })
        { throw new InvalidOperationException("El empleado ya tiene un usuario vinculado.", ex); }
        await transaction.CommitAsync(cancellationToken);

        return new UserDto
        {
            Id = user.Id,
            EmployeeId = user.EmployeeId,
            EmployeeName = user.Employee == null ? null : $"{user.Employee.FirstName} {user.Employee.LastName}",
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            HasKdsPin = false,
            Roles = new List<string>(),
            RoleDetails = new List<UserRoleDto>()
        };
    }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly GrimorioDbContext _context;

    public UpdateUserCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.BranchId == request.BranchId && !u.IsDeleted, cancellationToken);

        if (user == null)
            throw new InvalidOperationException("Usuario no encontrado.");

        var employeeId = request.Dto.EmployeeLinkProvided ? request.Dto.EmployeeId : user.EmployeeId;
        user.Employee = await UserEmployeeLink.ValidateAsync(_context, user.BranchId, user.Id,
            employeeId, user.EmployeeId, request.Dto.IsActive, cancellationToken);
        user.EmployeeId = employeeId;
        user.FirstName = request.Dto.FirstName;
        user.LastName = request.Dto.LastName;
        user.IsActive = request.Dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = Guid.Empty;

        try { await _context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: "23505", ConstraintName: "IX_Users_EmployeeId" })
        { throw new InvalidOperationException("El empleado ya tiene un usuario vinculado.", ex); }
        await transaction.CommitAsync(cancellationToken);

        return new UserDto
        {
            Id = user.Id,
            EmployeeId = user.EmployeeId,
            EmployeeName = user.Employee == null ? null : $"{user.Employee.FirstName} {user.Employee.LastName}",
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            HasKdsPin = user.KdsPinHash != null && user.KdsPinHash != "",
            Roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList(),
            RoleDetails = user.UserRoles.Select(ur => new UserRoleDto
            {
                RoleId = ur.RoleId,
                RoleName = ur.Role!.Name
            }).ToList()
        };
    }
}

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeleteUserCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

        if (user == null)
            throw new InvalidOperationException("Usuario no encontrado.");

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class AssignRolesToUserCommandHandler : IRequestHandler<AssignRolesToUserCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public AssignRolesToUserCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(AssignRolesToUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

        if (user == null)
            throw new InvalidOperationException("Usuario no encontrado.");

        var branchId = request.BranchId;
        if (branchId == Guid.Empty)
            throw new InvalidOperationException("BranchId requerido para asignar roles.");

        if (user.BranchId == Guid.Empty)
        {
            user.BranchId = branchId;
        }
        else if (user.BranchId != branchId)
        {
            throw new InvalidOperationException("El usuario pertenece a otra rama.");
        }

        await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var roleId in request.RoleIds)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted, cancellationToken);

            if (role == null)
            {
                throw new InvalidOperationException($"Rol con ID {roleId} no encontrado.");
            }

            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = roleId,
                BranchId = role.BranchId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };

            _context.UserRoles.Add(userRole);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly GrimorioDbContext _context;
    private readonly IPasswordHashingService _passwordHashingService;

    public ChangePasswordCommandHandler(GrimorioDbContext context, IPasswordHashingService passwordHashingService)
    {
        _context = context;
        _passwordHashingService = passwordHashingService;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var validation = await new Application.Features.Users.Validators.ChangePasswordCommandValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) throw new InvalidOperationException(validation.Errors[0].ErrorMessage);
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

        if (user == null)
            throw new InvalidOperationException("Usuario no encontrado.");

        // Verificar que la contraseña actual sea correcta
        if (!_passwordHashingService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("La contraseña actual es incorrecta.");

        // Validar que la nueva contraseña sea diferente
        if (request.NewPassword == request.CurrentPassword)
            throw new InvalidOperationException("La nueva contraseña debe ser diferente a la actual.");

        // Actualizar la contraseña
        var hash = _passwordHashingService.HashPassword(request.NewPassword);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var changed = await _context.Users.Where(u => u.Id == user.Id && u.AuthVersion == user.AuthVersion && !u.IsDeleted)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(u => u.PasswordHash, hash)
                .SetProperty(u => u.MustChangePassword, false)
                .SetProperty(u => u.AuthVersion, u => u.AuthVersion + 1)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow)
                .SetProperty(u => u.UpdatedBy, user.Id), cancellationToken);
        if (changed == 0) throw new InvalidOperationException("La cuenta cambio durante la solicitud. Inicia sesion nuevamente.");
        await _context.UserPushTokens.Where(token => token.UserId == user.Id && token.BranchId == user.BranchId)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.IsActive, false)
                .SetProperty(token => token.UpdatedAt, DateTime.UtcNow).SetProperty(token => token.UpdatedBy, user.Id), cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }
}

public class SetKdsPinCommandHandler : IRequestHandler<SetKdsPinCommand, bool>
{
    private readonly GrimorioDbContext _context;
    private readonly IPasswordHashingService _passwordHashingService;

    public SetKdsPinCommandHandler(GrimorioDbContext context, IPasswordHashingService passwordHashingService)
    {
        _context = context;
        _passwordHashingService = passwordHashingService;
    }

    public async Task<bool> Handle(SetKdsPinCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

        if (user == null)
            throw new InvalidOperationException("Usuario no encontrado.");

        var pin = request.Pin?.Trim();
        if (string.IsNullOrEmpty(pin))
        {
            user.KdsPinHash = null;
        }
        else
        {
            if (pin.Length != 4 || !pin.All(char.IsDigit))
                throw new InvalidOperationException("El PIN KDS debe tener 4 dígitos.");

            user.KdsPinHash = _passwordHashingService.HashPassword(pin);
        }

        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

