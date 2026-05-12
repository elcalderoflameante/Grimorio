using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Purchases.Queries;

public class GetSuppliersQuery : IRequest<List<SupplierDto>>
{
    public Guid BranchId { get; set; }
    public bool? ActiveOnly { get; set; }
}

public class GetPurchasesQuery : IRequest<List<PurchaseDto>>
{
    public Guid BranchId { get; set; }
    public string? Status { get; set; }
    public Guid? SupplierId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class GetPurchaseDetailQuery : IRequest<PurchaseDto?>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}
