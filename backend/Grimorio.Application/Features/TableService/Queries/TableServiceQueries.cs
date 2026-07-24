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

public class GetPublicRequestStatusQuery : IRequest<PublicRequestStatusDto?>
{
    public Guid RequestId { get; set; }
}

public class GetActivePublicTableRequestQuery : IRequest<PublicActiveTableRequestDto?>
{
    public string TableToken { get; set; } = string.Empty;
}

public class GetPublicTableMenuQuery : IRequest<PublicTableMenuDto>
{
    public string TableToken { get; set; } = string.Empty;
}

public class GetActivePublicTableOrderQuery : IRequest<OrderDto?>
{
    public string TableToken { get; set; } = string.Empty;
}
