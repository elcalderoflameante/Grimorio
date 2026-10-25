using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Billing.Queries;

public class GetCustomerSalesHandler(GrimorioDbContext db, ISender sender) : IRequestHandler<GetCustomerSalesQuery, CustomerSalesDto?>
{
    public async Task<CustomerSalesDto?> Handle(GetCustomerSalesQuery req, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(c => c.Id == req.CustomerId && c.BranchId == req.BranchId, ct)) return null;
        if (req.Page < 1 || req.PageSize is < 1 or > 100 || req.Page > int.MaxValue / req.PageSize)
            throw new InvalidOperationException("La pagina o el tamano de pagina no es valido.");
        if (req.FromUtc.HasValue && req.ToUtc.HasValue && req.FromUtc > req.ToUtc)
            throw new InvalidOperationException("El rango de fechas no es valido.");
        var query = db.OrderPayments.AsNoTracking().Where(p => p.BranchId == req.BranchId && p.CustomerId == req.CustomerId);
        if (req.FromUtc.HasValue) query = query.Where(p => p.PaidAt >= req.FromUtc.Value);
        if (req.ToUtc.HasValue) query = query.Where(p => p.PaidAt <= req.ToUtc.Value);
        var summary = await query.GroupBy(p => 1)
            .Select(g => new { Count = g.Count(), Amount = g.Sum(p => p.OrderAmount) }).SingleOrDefaultAsync(ct);
        var items = await sender.Send(new GetSalesQuery { BranchId = req.BranchId, CustomerId = req.CustomerId,
            FromUtc = req.FromUtc, ToUtc = req.ToUtc, Page = req.Page, PageSize = req.PageSize }, ct);
        return new CustomerSalesDto { TotalCount = summary?.Count ?? 0, TotalAmount = summary?.Amount ?? 0, Items = items };
    }
}
