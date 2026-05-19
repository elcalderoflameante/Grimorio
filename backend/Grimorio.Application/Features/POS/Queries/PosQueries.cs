using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.POS.Queries;

public class GetWorkStationsQuery : IRequest<List<WorkStationDto>>
{
    public Guid BranchId { get; set; }
}

public class GetOrdersQuery : IRequest<List<OrderDto>>
{
    public Guid BranchId { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public Guid? TableId { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

public class GetActiveOrderSummariesQuery : IRequest<List<ActiveOrderSummaryDto>>
{
    public Guid BranchId { get; set; }
}

public class GetOrderDetailQuery : IRequest<OrderDto?>
{
    public Guid OrderId { get; set; }
    public Guid BranchId { get; set; }
}

public class GetItemsByStationQuery : IRequest<List<StationItemDto>>
{
    public Guid StationId { get; set; }
    public Guid BranchId { get; set; }
}

// Ítems completados (Ready) de la estación en la fecha indicada
public class GetCompletedStationItemsQuery : IRequest<List<StationItemDto>>
{
    public Guid StationId { get; set; }
    public Guid BranchId { get; set; }
    public DateOnly Date { get; set; }
}
