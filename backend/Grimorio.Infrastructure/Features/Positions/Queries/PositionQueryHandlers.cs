using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Positions.Queries;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Domain.Entities.Organization;

namespace Grimorio.Infrastructure.Features.Positions.Queries;

/// <summary>
/// Handler para GetPositionQuery.
/// Obtiene una posición por ID.
/// </summary>
public class GetPositionQueryHandler : IRequestHandler<GetPositionQuery, PositionDto>
{
    private readonly GrimorioDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetPositionQueryHandler(GrimorioDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<PositionDto> Handle(GetPositionQuery request, CancellationToken cancellationToken)
    {
        var position = await _dbContext.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PositionId && p.BranchIdParent == request.BranchId, cancellationToken);

        if (position == null)
            return null;

        return _mapper.Map<PositionDto>(position);
    }
}

/// <summary>
/// Handler para GetPositionsQuery.
/// Obtiene todas las posiciones de una rama con paginación.
/// </summary>
public class GetPositionsQueryHandler : IRequestHandler<GetPositionsQuery, PaginatedPositionsResult>
{
    private readonly GrimorioDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetPositionsQueryHandler(GrimorioDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<PaginatedPositionsResult> Handle(GetPositionsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Positions
            .AsNoTracking()
            .Where(p => p.BranchIdParent == request.BranchId);

        if (request.OnlyActive)
            query = query.Where(p => p.IsActive);

        var totalCount = await query.CountAsync(cancellationToken);

        var positions = await query
            .OrderBy(p => p.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedPositionsResult
        {
            Items = _mapper.Map<List<PositionDto>>(positions),
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
