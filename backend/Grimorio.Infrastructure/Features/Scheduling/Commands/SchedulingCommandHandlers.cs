using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Commands;
using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Scheduling.Commands;

public class CreateWorkAreaCommandHandler : IRequestHandler<CreateWorkAreaCommand, WorkAreaDto>
{
    private readonly GrimorioDbContext _context;

    public CreateWorkAreaCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<WorkAreaDto> Handle(CreateWorkAreaCommand request, CancellationToken cancellationToken)
    {
        var workArea = new WorkArea
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Color = request.Color,
            DisplayOrder = request.DisplayOrder,
            BranchId = request.BranchId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        _context.WorkAreas.Add(workArea);
        await _context.SaveChangesAsync(cancellationToken);

        return new WorkAreaDto
        {
            Id = workArea.Id,
            Name = workArea.Name,
            Description = workArea.Description,
            Color = workArea.Color,
            DisplayOrder = workArea.DisplayOrder,
            BranchId = workArea.BranchId,
            WorkRoles = new()
        };
    }
}

public class UpdateWorkAreaCommandHandler : IRequestHandler<UpdateWorkAreaCommand, WorkAreaDto>
{
    private readonly GrimorioDbContext _context;

    public UpdateWorkAreaCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<WorkAreaDto> Handle(UpdateWorkAreaCommand request, CancellationToken cancellationToken)
    {
        var workArea = await _context.WorkAreas
            .Include(w => w.WorkRoles)
            .FirstOrDefaultAsync(w => w.Id == request.Id && !w.IsDeleted, cancellationToken);

        if (workArea == null)
            throw new InvalidOperationException("Área de trabajo no encontrada.");

        workArea.Name = request.Name;
        workArea.Description = request.Description;
        workArea.Color = request.Color;
        workArea.DisplayOrder = request.DisplayOrder;
        workArea.UpdatedAt = DateTime.UtcNow;
        workArea.UpdatedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);

        return new WorkAreaDto
        {
            Id = workArea.Id,
            Name = workArea.Name,
            Description = workArea.Description,
            Color = workArea.Color,
            DisplayOrder = workArea.DisplayOrder,
            BranchId = workArea.BranchId,
            WorkRoles = workArea.WorkRoles
                .Where(wr => !wr.IsDeleted)
                .Select(wr => new WorkRoleDto
                {
                    Id = wr.Id,
                    Name = wr.Name,
                    Description = wr.Description,
                    WorkAreaId = wr.WorkAreaId,
                    FreeDaysPerMonth = wr.FreeDaysPerMonth,
                    DailyHoursTarget = wr.DailyHoursTarget
                }).ToList()
        };
    }
}

public class DeleteWorkAreaCommandHandler : IRequestHandler<DeleteWorkAreaCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeleteWorkAreaCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteWorkAreaCommand request, CancellationToken cancellationToken)
    {
        var workArea = await _context.WorkAreas
            .FirstOrDefaultAsync(w => w.Id == request.Id && !w.IsDeleted, cancellationToken);

        if (workArea == null)
            throw new InvalidOperationException("Área de trabajo no encontrada.");

        workArea.IsDeleted = true;
        workArea.DeletedAt = DateTime.UtcNow;
        workArea.DeletedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class CreateWorkRoleCommandHandler : IRequestHandler<CreateWorkRoleCommand, WorkRoleDto>
{
    private readonly GrimorioDbContext _context;

    public CreateWorkRoleCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<WorkRoleDto> Handle(CreateWorkRoleCommand request, CancellationToken cancellationToken)
    {
        var workArea = await _context.WorkAreas
            .FirstOrDefaultAsync(w => w.Id == request.WorkAreaId && !w.IsDeleted, cancellationToken);

        if (workArea == null)
            throw new InvalidOperationException("Área de trabajo no encontrada.");

        var workRole = new WorkRole
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            WorkAreaId = request.WorkAreaId,
            FreeDaysPerMonth = request.FreeDaysPerMonth,
            DailyHoursTarget = request.DailyHoursTarget,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        _context.WorkRoles.Add(workRole);
        await _context.SaveChangesAsync(cancellationToken);

        return new WorkRoleDto
        {
            Id = workRole.Id,
            Name = workRole.Name,
            Description = workRole.Description,
            WorkAreaId = workRole.WorkAreaId,
            FreeDaysPerMonth = workRole.FreeDaysPerMonth,
            DailyHoursTarget = workRole.DailyHoursTarget
        };
    }
}

public class UpdateWorkRoleCommandHandler : IRequestHandler<UpdateWorkRoleCommand, WorkRoleDto>
{
    private readonly GrimorioDbContext _context;

    public UpdateWorkRoleCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<WorkRoleDto> Handle(UpdateWorkRoleCommand request, CancellationToken cancellationToken)
    {
        var workRole = await _context.WorkRoles
            .FirstOrDefaultAsync(wr => wr.Id == request.Id && !wr.IsDeleted, cancellationToken);

        if (workRole == null)
            throw new InvalidOperationException("Rol de trabajo no encontrado.");

        workRole.Name = request.Name;
        workRole.Description = request.Description;
        workRole.FreeDaysPerMonth = request.FreeDaysPerMonth;
        workRole.DailyHoursTarget = request.DailyHoursTarget;
        workRole.UpdatedAt = DateTime.UtcNow;
        workRole.UpdatedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);

        return new WorkRoleDto
        {
            Id = workRole.Id,
            Name = workRole.Name,
            Description = workRole.Description,
            WorkAreaId = workRole.WorkAreaId,
            FreeDaysPerMonth = workRole.FreeDaysPerMonth,
            DailyHoursTarget = workRole.DailyHoursTarget
        };
    }
}

public class DeleteWorkRoleCommandHandler : IRequestHandler<DeleteWorkRoleCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeleteWorkRoleCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteWorkRoleCommand request, CancellationToken cancellationToken)
    {
        var workRole = await _context.WorkRoles
            .FirstOrDefaultAsync(wr => wr.Id == request.Id && !wr.IsDeleted, cancellationToken);

        if (workRole == null)
            throw new InvalidOperationException("Rol de trabajo no encontrado.");

        workRole.IsDeleted = true;
        workRole.DeletedAt = DateTime.UtcNow;
        workRole.DeletedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
