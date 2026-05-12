using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Infrastructure.Features.Billing.Commands;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.Infrastructure.Features.Billing.Queries;

public class GetTaxRatesHandler : IRequestHandler<GetTaxRatesQuery, List<TaxRateDto>>
{
    private readonly GrimorioDbContext _db;
    public GetTaxRatesHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<TaxRateDto>> Handle(GetTaxRatesQuery req, CancellationToken ct)
    {
        var query = _db.TaxRates.Where(t => t.BranchId == req.BranchId && !t.IsDeleted);
        if (req.ActiveOnly) query = query.Where(t => t.IsActive);
        var list = await query.OrderBy(t => t.Percentage).ToListAsync(ct);
        return list.Select(t => new TaxRateDto
        {
            Id = t.Id, Name = t.Name, Percentage = t.Percentage,
            SriCode = t.SriCode, IsDefault = t.IsDefault, IsActive = t.IsActive,
        }).ToList();
    }
}

public class GetBranchTaxConfigHandler : IRequestHandler<GetBranchTaxConfigQuery, BranchTaxConfigDto?>
{
    private readonly GrimorioDbContext _db;
    public GetBranchTaxConfigHandler(GrimorioDbContext db) => _db = db;

    public async Task<BranchTaxConfigDto?> Handle(GetBranchTaxConfigQuery req, CancellationToken ct)
    {
        var cfg = await _db.BranchTaxConfigs.FirstOrDefaultAsync(c => c.BranchId == req.BranchId && !c.IsDeleted, ct);
        if (cfg == null) return null;
        return BillingMapper.MapBranchTaxConfig(cfg);
    }
}

public class GetPaymentMethodsHandler : IRequestHandler<GetPaymentMethodsQuery, List<PaymentMethodConfigDto>>
{
    private readonly GrimorioDbContext _db;
    public GetPaymentMethodsHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<PaymentMethodConfigDto>> Handle(GetPaymentMethodsQuery req, CancellationToken ct)
    {
        var query = _db.PaymentMethodConfigs.Where(m => !m.IsDeleted);
        if (req.ActiveOnly) query = query.Where(m => m.IsActive);
        var list = await query.OrderBy(m => m.SortOrder).ThenBy(m => m.Name).ToListAsync(ct);
        return list.Select(BillingMapper.MapPaymentMethod).ToList();
    }
}

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
            .FirstOrDefaultAsync(s => s.BranchId == req.BranchId && s.Status == CashSessionStatus.Open && !s.IsDeleted, ct);
        if (session == null) return null;

        // Incluir pagos vinculados por FK + pagos huérfanos (sin sesión) hechos desde que se abrió la caja
        var payments = await _db.OrderPayments
            .Include(p => p.Lines).ThenInclude(l => l.Config)
            .Where(p => p.BranchId == req.BranchId && !p.IsDeleted
                && (p.CashSessionId == session.Id
                    || (p.CashSessionId == null && p.PaidAt >= session.OpenedAt)))
            .ToListAsync(ct);

        return BillingMapper.MapSession(session, payments);
    }
}

public class GetCashSessionsHandler : IRequestHandler<GetCashSessionsQuery, List<CashSessionDto>>
{
    private readonly GrimorioDbContext _db;
    public GetCashSessionsHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<CashSessionDto>> Handle(GetCashSessionsQuery req, CancellationToken ct)
    {
        var query = _db.CashSessions
            .Include(s => s.Payments.Where(p => !p.IsDeleted))
                .ThenInclude(p => p.Lines)
                .ThenInclude(l => l.Config)
            .Where(s => s.BranchId == req.BranchId && !s.IsDeleted);

        if (req.FromUtc.HasValue) query = query.Where(s => s.OpenedAt >= req.FromUtc.Value);
        if (req.ToUtc.HasValue) query = query.Where(s => s.OpenedAt <= req.ToUtc.Value);

        var sessions = await query
            .OrderByDescending(s => s.OpenedAt)
            .Take(req.PageSize)
            .ToListAsync(ct);

        return sessions.Select(s => BillingMapper.MapSession(s)).ToList();
    }
}

public class GetCashSessionDetailHandler : IRequestHandler<GetCashSessionDetailQuery, CashSessionDto?>
{
    private readonly GrimorioDbContext _db;
    public GetCashSessionDetailHandler(GrimorioDbContext db) => _db = db;

    public async Task<CashSessionDto?> Handle(GetCashSessionDetailQuery req, CancellationToken ct)
    {
        var session = await _db.CashSessions
            .FirstOrDefaultAsync(s => s.Id == req.Id && s.BranchId == req.BranchId && !s.IsDeleted, ct);
        if (session == null) return null;

        var closedAt = session.ClosedAt ?? DateTime.UtcNow;
        var payments = await _db.OrderPayments
            .Include(p => p.Lines).ThenInclude(l => l.Config)
            .Where(p => p.BranchId == req.BranchId && !p.IsDeleted
                && (p.CashSessionId == session.Id
                    || (p.CashSessionId == null && p.PaidAt >= session.OpenedAt && p.PaidAt <= closedAt)))
            .ToListAsync(ct);

        return BillingMapper.MapSession(session, payments);
    }
}

public class GetOrderPaymentsHandler : IRequestHandler<GetOrderPaymentsQuery, List<OrderPaymentDto>>
{
    private readonly GrimorioDbContext _db;
    public GetOrderPaymentsHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<OrderPaymentDto>> Handle(GetOrderPaymentsQuery req, CancellationToken ct)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Orden no encontrada.");

        var payments = await _db.OrderPayments
            .Include(p => p.Lines).ThenInclude(l => l.Config)
            .Include(p => p.Customer)
            .Where(p => p.OrderId == req.OrderId && !p.IsDeleted)
            .OrderBy(p => p.PaidAt)
            .ToListAsync(ct);

        return payments.Select(p => BillingMapper.MapPayment(p, order.Number, p.Customer)).ToList();
    }
}

public class GetSalesHandler : IRequestHandler<GetSalesQuery, List<OrderPaymentDto>>
{
    private readonly GrimorioDbContext _db;
    public GetSalesHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<OrderPaymentDto>> Handle(GetSalesQuery req, CancellationToken ct)
    {
        var query = _db.OrderPayments
            .Include(p => p.Lines).ThenInclude(l => l.Config)
            .Include(p => p.Customer)
            .Include(p => p.Order).ThenInclude(o => o!.Table)
            .Where(p => p.BranchId == req.BranchId && !p.IsDeleted);

        if (req.FromUtc.HasValue) query = query.Where(p => p.PaidAt >= req.FromUtc.Value);
        if (req.ToUtc.HasValue) query = query.Where(p => p.PaidAt <= req.ToUtc.Value);

        var payments = await query
            .OrderByDescending(p => p.PaidAt)
            .Take(req.PageSize)
            .ToListAsync(ct);

        return payments.Select(p => BillingMapper.MapPayment(
            p,
            p.Order?.Number ?? 0,
            p.Customer,
            p.Order?.Table?.Code,
            p.Order?.Table?.Name,
            p.Order?.Type.ToString()
        )).ToList();
    }
}

// ── SRI Certificado ───────────────────────────────────────────────────────────

public class GetSriCertificateStatusHandler : IRequestHandler<GetSriCertificateStatusQuery, SriCertificateStatusDto>
{
    private readonly GrimorioDbContext _db;
    public GetSriCertificateStatusHandler(GrimorioDbContext db) => _db = db;

    public async Task<SriCertificateStatusDto> Handle(GetSriCertificateStatusQuery req, CancellationToken ct)
    {
        var cert = await _db.SriCertificates
            .FirstOrDefaultAsync(x => x.BranchId == req.BranchId && !x.IsDeleted, ct);
        if (cert == null)
            return new SriCertificateStatusDto { HasCertificate = false };

        return new SriCertificateStatusDto
        {
            HasCertificate = true,
            FileName = cert.FileName,
            ExpiresAt = cert.ExpiresAt,
            UploadedAt = cert.CreatedAt.ToString("o"),
        };
    }
}

// ── Documentos Electrónicos ───────────────────────────────────────────────────

public class GetElectronicDocumentsHandler : IRequestHandler<GetElectronicDocumentsQuery, List<ElectronicDocumentDto>>
{
    private readonly GrimorioDbContext _db;
    public GetElectronicDocumentsHandler(GrimorioDbContext db) => _db = db;

    public async Task<List<ElectronicDocumentDto>> Handle(GetElectronicDocumentsQuery req, CancellationToken ct)
    {
        var query = _db.ElectronicDocuments.Where(d => d.BranchId == req.BranchId && !d.IsDeleted);
        if (req.FromUtc.HasValue) query = query.Where(d => d.CreatedAt >= req.FromUtc.Value);
        if (req.ToUtc.HasValue) query = query.Where(d => d.CreatedAt <= req.ToUtc.Value);
        if (!string.IsNullOrEmpty(req.Status) && Enum.TryParse<ElectronicDocumentStatus>(req.Status, true, out var statusEnum))
            query = query.Where(d => d.Status == statusEnum);

        var docs = await query
            .OrderByDescending(d => d.CreatedAt)
            .Take(req.PageSize)
            .ToListAsync(ct);

        return docs.Select(MapDoc).ToList();
    }

    private static ElectronicDocumentDto MapDoc(ElectronicDocument d) => new()
    {
        Id = d.Id, OrderPaymentId = d.OrderPaymentId,
        ClaveAcceso = d.ClaveAcceso, NumeroFactura = d.NumeroFactura,
        Secuencial = d.Secuencial, Environment = d.Environment,
        Status = d.Status.ToString(),
        TotalSinImpuestos = d.TotalSinImpuestos, TotalDescuento = d.TotalDescuento,
        TotalIva = d.TotalIva, ImporteTotal = d.ImporteTotal,
        NumeroAutorizacion = d.NumeroAutorizacion, FechaAutorizacion = d.FechaAutorizacion,
        ErrorMessage = d.ErrorMessage, SentAt = d.SentAt, RetryCount = d.RetryCount,
        CreatedAt = d.CreatedAt,
        HasRide = d.RidePdf != null && d.RidePdf.Length > 0,
        HasXml = !string.IsNullOrEmpty(d.XmlAuthorized ?? d.XmlSigned),
    };
}

public class GetElectronicDocumentDetailHandler : IRequestHandler<GetElectronicDocumentDetailQuery, ElectronicDocumentDto?>
{
    private readonly GrimorioDbContext _db;
    public GetElectronicDocumentDetailHandler(GrimorioDbContext db) => _db = db;

    public async Task<ElectronicDocumentDto?> Handle(GetElectronicDocumentDetailQuery req, CancellationToken ct)
    {
        var d = await _db.ElectronicDocuments
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct);
        if (d == null) return null;
        return new ElectronicDocumentDto
        {
            Id = d.Id, OrderPaymentId = d.OrderPaymentId,
            ClaveAcceso = d.ClaveAcceso, NumeroFactura = d.NumeroFactura,
            Secuencial = d.Secuencial, Environment = d.Environment,
            Status = d.Status.ToString(),
            TotalSinImpuestos = d.TotalSinImpuestos, TotalDescuento = d.TotalDescuento,
            TotalIva = d.TotalIva, ImporteTotal = d.ImporteTotal,
            NumeroAutorizacion = d.NumeroAutorizacion, FechaAutorizacion = d.FechaAutorizacion,
            ErrorMessage = d.ErrorMessage, SentAt = d.SentAt, RetryCount = d.RetryCount,
            CreatedAt = d.CreatedAt,
            HasRide = d.RidePdf != null && d.RidePdf.Length > 0,
            HasXml = !string.IsNullOrEmpty(d.XmlAuthorized ?? d.XmlSigned),
        };
    }
}
