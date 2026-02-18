using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Queries;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Scheduling.Queries;

public class GetSpecialDateTemplatesQueryHandler : IRequestHandler<GetSpecialDateTemplatesQuery, List<SpecialDateTemplateDto>>
{
    private readonly GrimorioDbContext _context;

    public GetSpecialDateTemplatesQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<SpecialDateTemplateDto>> Handle(GetSpecialDateTemplatesQuery request, CancellationToken cancellationToken)
    {
        return await _context.SpecialDateTemplates
            .Where(t => t.SpecialDateId == request.SpecialDateId && !t.IsDeleted)
            .Include(t => t.WorkArea)
            .Include(t => t.WorkRole)
            .OrderBy(t => t.StartTime)
            .Select(t => new SpecialDateTemplateDto
            {
                Id = t.Id,
                SpecialDateId = t.SpecialDateId,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                BreakDuration = t.BreakDuration,
                LunchDuration = t.LunchDuration,
                WorkAreaId = t.WorkAreaId,
                WorkAreaName = t.WorkArea!.Name,
                WorkRoleId = t.WorkRoleId,
                WorkRoleName = t.WorkRole!.Name,
                RequiredCount = t.RequiredCount,
                Notes = t.Notes
            }).ToListAsync(cancellationToken);
    }
}

public class GetSpecialDateTemplateByIdQueryHandler : IRequestHandler<GetSpecialDateTemplateByIdQuery, SpecialDateTemplateDto?>
{
    private readonly GrimorioDbContext _context;

    public GetSpecialDateTemplateByIdQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<SpecialDateTemplateDto?> Handle(GetSpecialDateTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.SpecialDateTemplates
            .Where(t => t.Id == request.Id && !t.IsDeleted)
            .Include(t => t.WorkArea)
            .Include(t => t.WorkRole)
            .Select(t => new SpecialDateTemplateDto
            {
                Id = t.Id,
                SpecialDateId = t.SpecialDateId,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                BreakDuration = t.BreakDuration,
                LunchDuration = t.LunchDuration,
                WorkAreaId = t.WorkAreaId,
                WorkAreaName = t.WorkArea!.Name,
                WorkRoleId = t.WorkRoleId,
                WorkRoleName = t.WorkRole!.Name,
                RequiredCount = t.RequiredCount,
                Notes = t.Notes
            }).FirstOrDefaultAsync(cancellationToken);
    }
}
