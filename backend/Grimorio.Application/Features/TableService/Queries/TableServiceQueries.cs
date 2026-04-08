using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.POS;
using MediatR;

namespace Grimorio.Application.Features.TableService.Queries;

public class GetRestaurantTablesQuery : IRequest<List<RestaurantTableDto>>
{
    public Guid BranchId { get; set; }
}

public class GetRestaurantTableByTokenQuery : IRequest<PublicTableInfoDto?>
{
    public string Token { get; set; } = string.Empty;
}

public class GetTableServiceRequestsQuery : IRequest<List<TableServiceRequestDto>>
{
    public Guid BranchId { get; set; }
    public TableServiceRequestStatus? Status { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
