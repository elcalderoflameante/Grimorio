using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Inventory.Queries;

public class GetInventoryReconciliationQuery : IRequest<InventoryReconciliationDto>
{
    public Guid BranchId { get; set; }
    public Guid? ArticleId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? Search { get; set; }
    public string? Severity { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetStockMovementTraceQuery : IRequest<StockMovementTraceDto?>
{
    public Guid BranchId { get; set; }
    public Guid MovementId { get; set; }
}
