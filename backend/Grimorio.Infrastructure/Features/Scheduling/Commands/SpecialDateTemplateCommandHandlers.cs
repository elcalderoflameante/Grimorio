using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Commands;
using Grimorio.Domain.Entities.Scheduling;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Scheduling.Commands;

public class CreateSpecialDateTemplateCommandHandler : IRequestHandler<CreateSpecialDateTemplateCommand, SpecialDateTemplateDto>
{
    private readonly GrimorioDbContext _context;

    public CreateSpecialDateTemplateCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<SpecialDateTemplateDto> Handle(CreateSpecialDateTemplateCommand request, CancellationToken cancellationToken)
    {
        var specialDate = await _context.SpecialDates
            .FirstOrDefaultAsync(sd => sd.Id == request.SpecialDateId && !sd.IsDeleted, cancellationToken);

        if (specialDate == null)
            throw new InvalidOperationException("Día especial no encontrado.");

        var workArea = await _context.WorkAreas
            .FirstOrDefaultAsync(wa => wa.Id == request.WorkAreaId && !wa.IsDeleted, cancellationToken);

        if (workArea == null)
            throw new InvalidOperationException("Área de trabajo no encontrada.");

        var workRole = await _context.WorkRoles
            .FirstOrDefaultAsync(wr => wr.Id == request.WorkRoleId && !wr.IsDeleted, cancellationToken);

        if (workRole == null)
            throw new InvalidOperationException("Rol de trabajo no encontrado.");

        var template = new SpecialDateTemplate
        {
            Id = Guid.NewGuid(),
            SpecialDateId = request.SpecialDateId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            BreakDuration = request.BreakDuration,
            LunchDuration = request.LunchDuration,
            WorkAreaId = request.WorkAreaId,
            WorkRoleId = request.WorkRoleId,
            RequiredCount = request.RequiredCount,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };

        _context.SpecialDateTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return new SpecialDateTemplateDto
        {
            Id = template.Id,
            SpecialDateId = template.SpecialDateId,
            StartTime = template.StartTime,
            EndTime = template.EndTime,
            BreakDuration = template.BreakDuration,
            LunchDuration = template.LunchDuration,
            WorkAreaId = template.WorkAreaId,
            WorkAreaName = workArea.Name,
            WorkRoleId = template.WorkRoleId,
            WorkRoleName = workRole.Name,
            RequiredCount = template.RequiredCount,
            Notes = template.Notes
        };
    }
}

public class UpdateSpecialDateTemplateCommandHandler : IRequestHandler<UpdateSpecialDateTemplateCommand, SpecialDateTemplateDto>
{
    private readonly GrimorioDbContext _context;

    public UpdateSpecialDateTemplateCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<SpecialDateTemplateDto> Handle(UpdateSpecialDateTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _context.SpecialDateTemplates
            .Include(t => t.WorkArea)
            .Include(t => t.WorkRole)
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken);

        if (template == null)
            throw new InvalidOperationException("Plantilla de día especial no encontrada.");

        template.StartTime = request.StartTime;
        template.EndTime = request.EndTime;
        template.BreakDuration = request.BreakDuration;
        template.LunchDuration = request.LunchDuration;
        template.WorkAreaId = request.WorkAreaId;
        template.WorkRoleId = request.WorkRoleId;
        template.RequiredCount = request.RequiredCount;
        template.Notes = request.Notes;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);

        return new SpecialDateTemplateDto
        {
            Id = template.Id,
            SpecialDateId = template.SpecialDateId,
            StartTime = template.StartTime,
            EndTime = template.EndTime,
            BreakDuration = template.BreakDuration,
            LunchDuration = template.LunchDuration,
            WorkAreaId = template.WorkAreaId,
            WorkAreaName = template.WorkArea!.Name,
            WorkRoleId = template.WorkRoleId,
            WorkRoleName = template.WorkRole!.Name,
            RequiredCount = template.RequiredCount,
            Notes = template.Notes
        };
    }
}

public class DeleteSpecialDateTemplateCommandHandler : IRequestHandler<DeleteSpecialDateTemplateCommand, bool>
{
    private readonly GrimorioDbContext _context;

    public DeleteSpecialDateTemplateCommandHandler(GrimorioDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteSpecialDateTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _context.SpecialDateTemplates
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken);

        if (template == null)
            throw new InvalidOperationException("Plantilla de día especial no encontrada.");

        template.IsDeleted = true;
        template.DeletedAt = DateTime.UtcNow;
        template.DeletedBy = Guid.Empty;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
