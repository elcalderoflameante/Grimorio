using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Infrastructure.Features.Billing.Commands;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Billing.Queries;

public class GetCustomersHandler : IRequestHandler<GetCustomersQuery, List<CustomerDto>>
{
    private readonly GrimorioDbContext _db;
    public GetCustomersHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<CustomerDto>> Handle(GetCustomersQuery req, CancellationToken ct)
    {
        var query = _db.Customers.Where(c => c.BranchId == req.BranchId && !c.IsDeleted);
        if (req.ActiveOnly == true) query = query.Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(s) ||
                (c.TaxId != null && c.TaxId.Contains(s)));
        }
        var list = await query.OrderBy(c => c.Name).ToListAsync(ct);
        return list.Select(BillingMapper.MapCustomer).ToList();
    }
}

public class GetActiveCashSessionHandler : IRequestHandler<GetActiveCashSessionQuery, CashSessionDto?>
{
    private readonly GrimorioDbContext _db;
    public GetActiveCashSessionHandler(GrimorioDbContext db) => _db = db;

    public async Task<CashSessionDto?> Handle(GetActiveCashSessionQuery req, CancellationToken ct)
    {
        var session = await _db.CashSessions
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.BranchId == req.BranchId && s.Status == CashSessionStatus.Open && !s.IsDeleted, ct);
        return session == null ? null : BillingMapper.MapSession(session);
    }
}

public class GetCashSessionsHandler : IRequestHandler<GetCashSessionsQuery, List<CashSessionDto>>
{
    private readonly GrimorioDbContext _db;
    public GetCashSessionsHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<CashSessionDto>> Handle(GetCashSessionsQuery req, CancellationToken ct)
    {
        var query = _db.CashSessions
            .Include(s => s.Payments)
            .Where(s => s.BranchId == req.BranchId && !s.IsDeleted);

        if (req.FromUtc.HasValue) query = query.Where(s => s.OpenedAt >= req.FromUtc.Value);
        if (req.ToUtc.HasValue) query = query.Where(s => s.OpenedAt <= req.ToUtc.Value);

        var sessions = await query
            .OrderByDescending(s => s.OpenedAt)
            .Take(req.PageSize)
            .ToListAsync(ct);

        return sessions.Select(BillingMapper.MapSession).ToList();
    }
}

public class GetCashSessionDetailHandler : IRequestHandler<GetCashSessionDetailQuery, CashSessionDto?>
{
    private readonly GrimorioDbContext _db;
    public GetCashSessionDetailHandler(GrimorioDbContext db) => _db = db;

    public async Task<CashSessionDto?> Handle(GetCashSessionDetailQuery req, CancellationToken ct)
    {
        var session = await _db.CashSessions
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == req.Id && s.BranchId == req.BranchId && !s.IsDeleted, ct);
        return session == null ? null : BillingMapper.MapSession(session);
    }
}
