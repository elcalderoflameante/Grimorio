using MediatR;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Queries;
using Grimorio.Infrastructure.Persistence;

namespace Grimorio.Infrastructure.Features.Scheduling.Queries;

public class GetSpecialDatesQueryHandler : IRequestHandler<GetSpecialDatesQuery, List<SpecialDateDto>>
{
    private readonly GrimorioDbContext _context;

    public GetSpecialDatesQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<SpecialDateDto>> Handle(GetSpecialDatesQuery request, CancellationToken cancellationToken)
    {
        return await _context.SpecialDates
            .Where(sd => sd.BranchId == request.BranchId && !sd.IsDeleted)
            .OrderBy(sd => sd.Date)
            .Select(sd => new SpecialDateDto
            {
                Id = sd.Id,
                BranchId = sd.BranchId,
                Date = sd.Date,
                Name = sd.Name,
                Description = sd.Description
            })
            .ToListAsync(cancellationToken);
    }
}

public class GetSpecialDateByIdQueryHandler : IRequestHandler<GetSpecialDateByIdQuery, SpecialDateDto?>
{
    private readonly GrimorioDbContext _context;

    public GetSpecialDateByIdQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<SpecialDateDto?> Handle(GetSpecialDateByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.SpecialDates
            .Where(sd => sd.Id == request.Id && !sd.IsDeleted)
            .Select(sd => new SpecialDateDto
            {
                Id = sd.Id,
                BranchId = sd.BranchId,
                Date = sd.Date,
                Name = sd.Name,
                Description = sd.Description
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public class GetSpecialDateByDateQueryHandler : IRequestHandler<GetSpecialDateByDateQuery, SpecialDateDto?>
{
    private readonly GrimorioDbContext _context;

    public GetSpecialDateByDateQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<SpecialDateDto?> Handle(GetSpecialDateByDateQuery request, CancellationToken cancellationToken)
    {
        return await _context.SpecialDates
            .Where(sd => sd.BranchId == request.BranchId && sd.Date.Date == request.Date.Date && !sd.IsDeleted)
            .Select(sd => new SpecialDateDto
            {
                Id = sd.Id,
                BranchId = sd.BranchId,
                Date = sd.Date,
                Name = sd.Name,
                Description = sd.Description
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
