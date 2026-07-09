using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.POS.Commands;

// ── Estaciones ────────────────────────────────────────────────────────────

public class CreateWorkStationCommand : IRequest<WorkStationDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class UpdateWorkStationCommand : IRequest<WorkStationDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class DeleteWorkStationCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

// ── Mesas (posición en mapa) ──────────────────────────────────────────────

public class UpdateTablePositionCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public int PosX { get; set; }
    public int PosY { get; set; }
}

// ── Órdenes ───────────────────────────────────────────────────────────────

public class CreateOrderCommand : IRequest<OrderDto>
{
    public Guid BranchId { get; set; }
    public Guid? WaiterId { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid? TableId { get; set; }
    public string? CustomerName { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = [];
}

public class CreateDirectSaleCommand : IRequest<OrderDto>
{
    public Guid BranchId { get; set; }
    public Guid? CashierId { get; set; }
    public string? CustomerName { get; set; }
    public string? Notes { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = [];
}

public class UpdateOrderItemsCommand : IRequest<OrderDto>
{
    public Guid OrderId { get; set; }
    public Guid BranchId { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = [];
}

public class ConfirmOrderCommand : IRequest<OrderDto>
{
    public Guid OrderId { get; set; }
    public Guid BranchId { get; set; }
}

public class DeliverOrderCommand : IRequest<OrderDto>
{
    public Guid OrderId { get; set; }
    public Guid BranchId { get; set; }
}

public class CancelOrderCommand : IRequest<OrderDto>
{
    public Guid OrderId { get; set; }
    public Guid BranchId { get; set; }
}

public class CancelOrderItemCommand : IRequest<OrderDto>
{
    public Guid OrderItemId { get; set; }
    public Guid BranchId { get; set; }
}

public class UpdateOrderItemNotesCommand : IRequest<OrderItemDto>
{
    public Guid OrderItemId { get; set; }
    public Guid BranchId { get; set; }
    public string? Notes { get; set; }
}

public class SetOrderItemStatusCommand : IRequest<OrderItemDto>
{
    public Guid OrderItemId { get; set; }
    public Guid BranchId { get; set; }
    public string Status { get; set; } = string.Empty;
}
