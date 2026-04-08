using Grimorio.Application.DTOs;
using Grimorio.Application.Features.TableService.Queries;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.TableService.Queries;

public class GetRestaurantTablesQueryHandler : IRequestHandler<GetRestaurantTablesQuery, List<RestaurantTableDto>>
{
    private readonly GrimorioDbContext _context;

    public GetRestaurantTablesQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<RestaurantTableDto>> Handle(GetRestaurantTablesQuery request, CancellationToken cancellationToken)
    {
        return await _context.RestaurantTables
            .Where(x => x.BranchId == request.BranchId && !x.IsDeleted)
            .OrderBy(x => x.Code)
            .Select(x => new RestaurantTableDto
            {
                Id = x.Id,
                BranchId = x.BranchId,
                Code = x.Code,
                Name = x.Name,
                Area = x.Area,
                Capacity = x.Capacity,
                PublicToken = x.PublicToken,
                IsActive = x.IsActive,
                PublicUrl = $"/mesa/{x.PublicToken}",
            })
            .ToListAsync(cancellationToken);
    }
}

public class GetRestaurantTableByTokenQueryHandler : IRequestHandler<GetRestaurantTableByTokenQuery, PublicTableInfoDto?>
{
    private readonly GrimorioDbContext _context;

    public GetRestaurantTableByTokenQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<PublicTableInfoDto?> Handle(GetRestaurantTableByTokenQuery request, CancellationToken cancellationToken)
    {
        return await _context.RestaurantTables
            .Where(x => x.PublicToken == request.Token && !x.IsDeleted)
            .Select(x => new PublicTableInfoDto
            {
                TableId = x.Id,
                Code = x.Code,
                Name = x.Name,
                Area = x.Area,
                IsActive = x.IsActive,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public class GetTableServiceRequestsQueryHandler : IRequestHandler<GetTableServiceRequestsQuery, List<TableServiceRequestDto>>
{
    private readonly GrimorioDbContext _context;

    public GetTableServiceRequestsQueryHandler(GrimorioDbContext context) => _context = context;

    public async Task<List<TableServiceRequestDto>> Handle(GetTableServiceRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TableServiceRequests
            .Where(x => x.BranchId == request.BranchId && !x.IsDeleted)
            .Include(x => x.RestaurantTable)
            .AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(x => x.RequestedAt >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(x => x.RequestedAt <= request.ToUtc.Value);
        }

        return await query
            .OrderByDescending(x => x.RequestedAt)
            .Take(250)
            .Select(x => new TableServiceRequestDto
            {
                Id = x.Id,
                BranchId = x.BranchId,
                RestaurantTableId = x.RestaurantTableId,
                TableCode = x.RestaurantTable != null ? x.RestaurantTable.Code : string.Empty,
                TableName = x.RestaurantTable != null ? x.RestaurantTable.Name : string.Empty,
                TableArea = x.RestaurantTable != null ? x.RestaurantTable.Area : null,
                Type = x.Type,
                CustomMessage = x.CustomMessage,
                Status = x.Status,
                RequestedAt = x.RequestedAt,
                TakenAt = x.TakenAt,
                CompletedAt = x.CompletedAt,
                TakenByUserId = x.TakenByUserId,
                TakenByName = x.TakenByName,
            })
            .ToListAsync(cancellationToken);
    }
}
