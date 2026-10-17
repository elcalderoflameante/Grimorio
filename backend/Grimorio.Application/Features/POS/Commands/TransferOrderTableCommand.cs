using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.POS.Commands;

public class TransferOrderTableDto
{
    public Guid RequestId { get; set; }
    public Guid FromTableId { get; set; }
    public Guid ToTableId { get; set; }
}

public record TransferOrderTableCommand(Guid BranchId, Guid OrderId, Guid ActorUserId, TransferOrderTableDto Data) : IRequest<OrderDto>;
