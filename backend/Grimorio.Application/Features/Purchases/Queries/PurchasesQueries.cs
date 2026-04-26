using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Purchases.Queries;

public class GetSuppliersQuery : IRequest<List<SupplierDto>>
{
    public Guid BranchId { get; set; }
    public bool? ActiveOnly { get; set; }
}

public class GetPurchaseOrdersQuery : IRequest<List<PurchaseOrderDto>>
{
    public Guid BranchId { get; set; }
    public string? Status { get; set; }
    public Guid? SupplierId { get; set; }
}

public class GetPurchaseOrderDetailQuery : IRequest<PurchaseOrderDto?>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}
