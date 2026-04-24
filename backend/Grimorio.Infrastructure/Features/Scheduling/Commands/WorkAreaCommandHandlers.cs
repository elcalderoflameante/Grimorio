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
            Name = request.Name,
            Description = request.Description,
            Color = request.Color,
            DisplayOrder = request.DisplayOrder,
            BranchId = request.BranchId,
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
                    WorkAreaId = wr.WorkAreaId
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

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
