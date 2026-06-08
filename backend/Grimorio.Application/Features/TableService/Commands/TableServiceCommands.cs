using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.POS;
using MediatR;

namespace Grimorio.Application.Features.TableService.Commands;

public class CreateRestaurantTableCommand : IRequest<RestaurantTableDto>
{
    public Guid BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Area { get; set; }
    public int Capacity { get; set; } = 2;
}

public class UpdateRestaurantTableCommand : IRequest<RestaurantTableDto>
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Area { get; set; }
    public int Capacity { get; set; } = 2;
    public bool IsActive { get; set; } = true;
}

public class RegenerateRestaurantTableTokenCommand : IRequest<RestaurantTableDto>
{
    public Guid Id { get; set; }
}

public class DeleteRestaurantTableCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class PublicCreateTableServiceRequestCommand : IRequest<TableServiceRequestDto>
{
    public string TableToken { get; set; } = string.Empty;
    public TableServiceRequestType Type { get; set; }
    public string? CustomMessage { get; set; }
    public string? ClientFingerprint { get; set; }
    public string? SourceIp { get; set; }
}

public class TakeTableServiceRequestCommand : IRequest<TableServiceRequestDto>
{
    public Guid RequestId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class SetTableServiceRequestStatusCommand : IRequest<TableServiceRequestDto>
{
    public Guid RequestId { get; set; }
    public TableServiceRequestStatus Status { get; set; }
}
