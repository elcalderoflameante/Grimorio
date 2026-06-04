using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Commands;
using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Scheduling.Commands;

public class CreateShiftTemplateCommandHandler : IRequestHandler<CreateShiftTemplateCommand, ShiftTemplateDto>
{
    private readonly GrimorioDbContext _context;

    public CreateShiftTemplateCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<ShiftTemplateDto> Handle(CreateShiftTemplateCommand request, CancellationToken cancellationToken)
    {
        var workArea = await _context.WorkAreas
            .FirstOrDefaultAsync(w => w.Id == request.WorkAreaId && !w.IsDeleted, cancellationToken);

        if (workArea == null)
            throw new InvalidOperationException("Área de trabajo no encontrada.");

        var workRole = await _context.WorkRoles
            .FirstOrDefaultAsync(wr => wr.Id == request.WorkRoleId && !wr.IsDeleted, cancellationToken);

        if (workRole == null)
            throw new InvalidOperationException("Rol de trabajo no encontrado.");

        var shiftTemplate = new ShiftTemplate
        {
            BranchId = request.BranchId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            BreakDuration = request.BreakDuration,
            LunchDuration = request.LunchDuration,
            WorkAreaId = request.WorkAreaId,
            WorkRoleId = request.WorkRoleId,
            RequiredCount = request.RequiredCount,
            Notes = request.Notes,
        };

        _context.ShiftTemplates.Add(shiftTemplate);
        await _context.SaveChangesAsync(cancellationToken);

        return new ShiftTemplateDto
        {
            Id = shiftTemplate.Id,
            BranchId = shiftTemplate.BranchId,
            DayOfWeek = shiftTemplate.DayOfWeek,
            StartTime = shiftTemplate.StartTime,
            EndTime = shiftTemplate.EndTime,
            BreakDuration = shiftTemplate.BreakDuration,
            LunchDuration = shiftTemplate.LunchDuration,
            WorkAreaId = shiftTemplate.WorkAreaId,
            WorkAreaName = workArea.Name,
            WorkAreaColor = workArea.Color,
            WorkRoleId = shiftTemplate.WorkRoleId,
            WorkRoleName = workRole.Name,
            RequiredCount = shiftTemplate.RequiredCount,
            Notes = shiftTemplate.Notes
        };
    }
}

public class UpdateShiftTemplateCommandHandler : IRequestHandler<UpdateShiftTemplateCommand, ShiftTemplateDto>
{
    private readonly GrimorioDbContext _context;

    public UpdateShiftTemplateCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<ShiftTemplateDto> Handle(UpdateShiftTemplateCommand request, CancellationToken cancellationToken)
    {
        var shiftTemplate = await _context.ShiftTemplates
            .Include(st => st.WorkArea)
            .Include(st => st.WorkRole)
            .FirstOrDefaultAsync(st => st.Id == request.Id && !st.IsDeleted, cancellationToken);

        if (shiftTemplate == null)
            throw new InvalidOperationException("Plantilla de turno no encontrada.");

        shiftTemplate.DayOfWeek = request.DayOfWeek;
        shiftTemplate.StartTime = request.StartTime;
        shiftTemplate.EndTime = request.EndTime;
        shiftTemplate.BreakDuration = request.BreakDuration;
        shiftTemplate.LunchDuration = request.LunchDuration;
        shiftTemplate.RequiredCount = request.RequiredCount;
        shiftTemplate.Notes = request.Notes;

        await _context.SaveChangesAsync(cancellationToken);

        return new ShiftTemplateDto
        {
            Id = shiftTemplate.Id,
            BranchId = shiftTemplate.BranchId,
            DayOfWeek = shiftTemplate.DayOfWeek,
            StartTime = shiftTemplate.StartTime,
            EndTime = shiftTemplate.EndTime,
            BreakDuration = shiftTemplate.BreakDuration,
            LunchDuration = shiftTemplate.LunchDuration,
            WorkAreaId = shiftTemplate.WorkAreaId,
            WorkAreaName = shiftTemplate.WorkArea!.Name,
            WorkAreaColor = shiftTemplate.WorkArea!.Color,
            WorkRoleId = shiftTemplate.WorkRoleId,
            WorkRoleName = shiftTemplate.WorkRole!.Name,
            RequiredCount = shiftTemplate.RequiredCount,
            Notes = shiftTemplate.Notes
        };
    }
}

public class DeleteShiftTemplateCommandHandler : IRequestHandler<DeleteShiftTemplateCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeleteShiftTemplateCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteShiftTemplateCommand request, CancellationToken cancellationToken)
    {
        var shiftTemplate = await _context.ShiftTemplates
            .FirstOrDefaultAsync(st => st.Id == request.Id && !st.IsDeleted, cancellationToken);

        if (shiftTemplate == null)
            throw new InvalidOperationException("Plantilla de turno no encontrada.");

        shiftTemplate.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
