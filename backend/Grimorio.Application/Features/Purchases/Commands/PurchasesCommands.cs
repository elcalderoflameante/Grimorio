using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Purchases.Commands;

// ── Suppliers ──────────────────────────────────────────────────────────────────

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

// ── Compras directas ──────────────────────────────────────────────────────────

public class CreatePurchaseCommand : IRequest<PurchaseDto>
{
    public Guid BranchId { get; set; }
    public int DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime DocumentDate { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Notes { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public List<PurchaseItemInputDto> Items { get; set; } = [];
}

public class UpdatePurchaseCommand : IRequest<PurchaseDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public int DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime DocumentDate { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Notes { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public List<PurchaseItemInputDto> Items { get; set; } = [];
}

public class AnularPurchaseCommand : IRequest<PurchaseDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

public class DeletePurchaseCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}
