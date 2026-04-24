using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Commands;
using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Scheduling.Commands;

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
            Name = request.Name,
            Description = request.Description,
            WorkAreaId = request.WorkAreaId,
        };

        _context.WorkRoles.Add(workRole);
        await _context.SaveChangesAsync(cancellationToken);

        return new WorkRoleDto
        {
            Id = workRole.Id,
            Name = workRole.Name,
            Description = workRole.Description,
            WorkAreaId = workRole.WorkAreaId
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

        await _context.SaveChangesAsync(cancellationToken);

        return new WorkRoleDto
        {
            Id = workRole.Id,
            Name = workRole.Name,
            Description = workRole.Description,
            WorkAreaId = workRole.WorkAreaId
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

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
