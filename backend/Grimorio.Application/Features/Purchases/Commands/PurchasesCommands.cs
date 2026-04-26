using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Purchases.Commands;

// ── Suppliers ───────────────────────────────────────────────────────────

public class CreateSupplierCommand : IRequest<SupplierDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? ContactName { get; set; }
}

public class UpdateSupplierCommand : IRequest<SupplierDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? ContactName { get; set; }
    public bool IsActive { get; set; }
}

public class DeleteSupplierCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

// ── Órdenes de compra ─────────────────────────────────────────────────────

public class CreatePurchaseOrderCommand : IRequest<PurchaseOrderDto>
{
    public Guid BranchId { get; set; }
    public Guid SupplierId { get; set; }
    public DateTime? ExpectedAt { get; set; }
    public string? Notes { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public List<PurchaseOrderItemInputDto> Items { get; set; } = [];
}

public class UpdatePurchaseOrderCommand : IRequest<PurchaseOrderDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid SupplierId { get; set; }
    public DateTime? ExpectedAt { get; set; }
    public string? Notes { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public List<PurchaseOrderItemInputDto> Items { get; set; } = [];
}

public class SendPurchaseOrderCommand : IRequest<PurchaseOrderDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

public class ReceivePurchaseOrderCommand : IRequest<PurchaseOrderDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid WarehouseId { get; set; }
    public List<ReceptionItemDto> Items { get; set; } = [];
}

public class CancelPurchaseOrderCommand : IRequest<PurchaseOrderDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

public class DeletePurchaseOrderCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}
