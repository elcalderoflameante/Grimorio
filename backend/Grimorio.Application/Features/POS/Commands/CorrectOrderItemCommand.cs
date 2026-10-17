using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.POS.Commands;

public class CorrectOrderItemDto
{
    public Guid RequestId { get; set; }
    public bool Replace { get; set; }
    public string Disposition { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public List<CreateModifierSelectionDto> ModifierSelections { get; set; } = [];
}

public record CorrectOrderItemCommand(Guid BranchId, Guid OrderItemId, Guid ActorUserId, CorrectOrderItemDto Data) : IRequest<OrderDto>;
