using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Inventory.Commands;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Domain.Entities.Inventory;
using Grimorio.Domain.Entities.Menu;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Features.Billing.Queries;
using Grimorio.Infrastructure.Features.Menu;
using Grimorio.Infrastructure.Features.POS;
using Grimorio.Infrastructure.Features.POS.Commands;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Services.Email;
using Grimorio.Infrastructure.Services.Sri;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.DataProtection;
using Org.BouncyCastle.Pkcs;
using System.Text.Json;

namespace Grimorio.Infrastructure.Features.Billing.Commands;

// ── TaxRate ───────────────────────────────────────────────────────────────────

public class CreateTaxRateHandler : IRequestHandler<CreateTaxRateCommand, TaxRateDto>
{
    private readonly GrimorioDbContext _db;
    public CreateTaxRateHandler(GrimorioDbContext db) => _db = db;

    public async Task<TaxRateDto> Handle(CreateTaxRateCommand req, CancellationToken ct)
    {
        if (req.IsDefault)
            await ClearDefault(req.BranchId, ct);

        var entity = new TaxRate
        {
            BranchId = req.BranchId, Name = req.Name.Trim(),
            Percentage = req.Percentage, SriCode = req.SriCode.Trim(),
            IsDefault = req.IsDefault, IsActive = req.IsActive,
        };
        _db.TaxRates.Add(entity);
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapTaxRate(entity);
    }

    private async Task ClearDefault(Guid branchId, CancellationToken ct)
    {
        var current = await _db.TaxRates.FirstOrDefaultAsync(t => t.BranchId == branchId && t.IsDefault && !t.IsDeleted, ct);
        if (current != null) { current.IsDefault = false; }
    }
}

public class UpdateTaxRateHandler : IRequestHandler<UpdateTaxRateCommand, TaxRateDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateTaxRateHandler(GrimorioDbContext db) => _db = db;

    public async Task<TaxRateDto> Handle(UpdateTaxRateCommand req, CancellationToken ct)
    {
        var entity = await _db.TaxRates.FirstOrDefaultAsync(t => t.Id == req.Id && t.BranchId == req.BranchId && !t.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Tarifa de IVA no encontrada.");

        if (req.IsDefault && !entity.IsDefault)
        {
            var current = await _db.TaxRates.FirstOrDefaultAsync(t => t.BranchId == req.BranchId && t.IsDefault && !t.IsDeleted && t.Id != req.Id, ct);
            if (current != null) current.IsDefault = false;
        }

        entity.Name = req.Name.Trim(); entity.Percentage = req.Percentage;
        entity.SriCode = req.SriCode.Trim(); entity.IsDefault = req.IsDefault;
        entity.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapTaxRate(entity);
    }
}

public class DeleteTaxRateHandler : IRequestHandler<DeleteTaxRateCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteTaxRateHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteTaxRateCommand req, CancellationToken ct)
    {
        var entity = await _db.TaxRates.FirstOrDefaultAsync(t => t.Id == req.Id && t.BranchId == req.BranchId && !t.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Tarifa de IVA no encontrada.");
        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── SmtpConfig ────────────────────────────────────────────────────────────────

public class UpsertSmtpConfigHandler : IRequestHandler<UpsertSmtpConfigCommand, SmtpConfigDto>
{
    private readonly GrimorioDbContext _db;
    private readonly IDataProtector _protector;
    public UpsertSmtpConfigHandler(GrimorioDbContext db, IDataProtectionProvider dp)
    {
        _db = db;
        _protector = dp.CreateProtector("SmtpPassword");
    }

    public async Task<SmtpConfigDto> Handle(UpsertSmtpConfigCommand req, CancellationToken ct)
    {
        var cfg = await _db.SmtpConfigs.FirstOrDefaultAsync(c => c.BranchId == req.BranchId && !c.IsDeleted, ct);
        if (cfg == null)
        {
            cfg = new SmtpConfig { BranchId = req.BranchId };
            _db.SmtpConfigs.Add(cfg);
        }

        cfg.Host = req.Host.Trim();
        cfg.Port = req.Port;
        cfg.Username = req.Username.Trim();
        cfg.FromEmail = req.FromEmail.Trim();
        cfg.FromName = req.FromName.Trim();
        cfg.EnableSsl = req.EnableSsl;
        cfg.IsActive = req.IsActive;

        if (!string.IsNullOrWhiteSpace(req.Password))
            cfg.PasswordEncrypted = _protector.Protect(req.Password);

        await _db.SaveChangesAsync(ct);

        return new SmtpConfigDto
        {
            Host = cfg.Host, Port = cfg.Port, Username = cfg.Username,
            FromEmail = cfg.FromEmail, FromName = cfg.FromName,
            EnableSsl = cfg.EnableSsl, IsActive = cfg.IsActive,
            HasPassword = !string.IsNullOrEmpty(cfg.PasswordEncrypted),
        };
    }
}

public class TestSmtpConnectionHandler : IRequestHandler<TestSmtpConnectionCommand, bool>
{
    private readonly IEmailService _email;
    public TestSmtpConnectionHandler(IEmailService email) => _email = email;

    public async Task<bool> Handle(TestSmtpConnectionCommand req, CancellationToken ct)
    {
        await _email.SendTestEmailAsync(req.BranchId, req.ToEmail, ct);
        return true;
    }
}

// ── BranchTaxConfig ───────────────────────────────────────────────────────────

public class UpsertBranchTaxConfigHandler : IRequestHandler<UpsertBranchTaxConfigCommand, BranchTaxConfigDto>
{
    private readonly GrimorioDbContext _db;
    public UpsertBranchTaxConfigHandler(GrimorioDbContext db) => _db = db;

    public async Task<BranchTaxConfigDto> Handle(UpsertBranchTaxConfigCommand req, CancellationToken ct)
    {
        var cfg = await _db.BranchTaxConfigs.FirstOrDefaultAsync(c => c.BranchId == req.BranchId && !c.IsDeleted, ct);
        if (cfg == null)
        {
            cfg = new BranchTaxConfig { BranchId = req.BranchId };
            _db.BranchTaxConfigs.Add(cfg);
        }
        var ptoChanged = cfg.CodigoEstablecimiento != req.CodigoEstablecimiento.Trim()
                      || cfg.PuntoEmision != req.PuntoEmision.Trim();
        var seqChanged = cfg.SecuencialInicial != req.SecuencialInicial;

        cfg.Ruc = req.Ruc.Trim(); cfg.RazonSocial = req.RazonSocial.Trim();
        cfg.NombreComercial = req.NombreComercial?.Trim();
        cfg.Direccion = req.Direccion.Trim();
        cfg.CodigoEstablecimiento = req.CodigoEstablecimiento.Trim();
        cfg.PuntoEmision = req.PuntoEmision.Trim();
        cfg.Ambiente = req.Ambiente;
        cfg.ContribuyenteEspecial = req.ContribuyenteEspecial?.Trim();
        cfg.ObligadoContabilidad = req.ObligadoContabilidad;
        cfg.SecuencialInicial = Math.Max(1, req.SecuencialInicial);

        // Si cambia el punto de emisión o el secuencial inicial, resetear el contador.
        if (ptoChanged || seqChanged)
            cfg.Secuencial = cfg.SecuencialInicial - 1;

        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapBranchTaxConfig(cfg);
    }
}

// ── SRI Certificado ───────────────────────────────────────────────────────────

public class UploadSriCertificateHandler : IRequestHandler<UploadSriCertificateCommand, SriCertificateStatusDto>
{
    private readonly GrimorioDbContext _db;
    private readonly Microsoft.AspNetCore.DataProtection.IDataProtector _protector;

    public UploadSriCertificateHandler(GrimorioDbContext db,
        Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dp)
    {
        _db = db;
        _protector = dp.CreateProtector("SriCertificate");
    }

    public async Task<SriCertificateStatusDto> Handle(UploadSriCertificateCommand req, CancellationToken ct)
    {
        // Validar que el archivo sea un .p12 válido antes de guardar
        try
        {
            var store = new Org.BouncyCastle.Pkcs.Pkcs12StoreBuilder().Build();
            store.Load(new MemoryStream(req.CertificateBytes), req.Password.ToCharArray());
            var alias = store.Aliases.Cast<string>().FirstOrDefault(a => store.IsKeyEntry(a))
                ?? throw new InvalidOperationException("El archivo .p12 no contiene una clave privada válida.");

            // Leer fecha de expiración del certificado
            var certEntry = store.GetCertificate(alias);
            var expiresAt = certEntry?.Certificate.NotAfter;

            var existing = await _db.SriCertificates
                .FirstOrDefaultAsync(x => x.BranchId == req.BranchId && !x.IsDeleted, ct);

            if (existing != null)
            {
                existing.FileName = req.FileName;
                existing.CertificateEncrypted = _protector.Protect(req.CertificateBytes);
                existing.PasswordEncrypted = _protector.Protect(req.Password);
                existing.ExpiresAt = expiresAt;
            }
            else
            {
                var cert = new SriCertificate
                {
                    BranchId = req.BranchId,
                    FileName = req.FileName,
                    CertificateEncrypted = _protector.Protect(req.CertificateBytes),
                    PasswordEncrypted = _protector.Protect(req.Password),
                    ExpiresAt = expiresAt,
                };
                _db.SriCertificates.Add(cert);
            }

            await _db.SaveChangesAsync(ct);

            return new SriCertificateStatusDto
            {
                HasCertificate = true,
                FileName = req.FileName,
                ExpiresAt = expiresAt,
                UploadedAt = DateTime.UtcNow.ToString("o"),
            };
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                "No se pudo leer el archivo .p12. Verifique que el archivo y la contraseña sean correctos.", ex);
        }
    }
}

public class DeleteSriCertificateHandler : IRequestHandler<DeleteSriCertificateCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteSriCertificateHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteSriCertificateCommand req, CancellationToken ct)
    {
        var cert = await _db.SriCertificates
            .FirstOrDefaultAsync(x => x.BranchId == req.BranchId && !x.IsDeleted, ct);
        if (cert == null) return false;
        cert.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── PaymentMethodConfig ───────────────────────────────────────────────────────

public class CreatePaymentMethodHandler : IRequestHandler<CreatePaymentMethodCommand, PaymentMethodConfigDto>
{
    private readonly GrimorioDbContext _db;
    public CreatePaymentMethodHandler(GrimorioDbContext db) => _db = db;

    public async Task<PaymentMethodConfigDto> Handle(CreatePaymentMethodCommand req, CancellationToken ct)
    {
        var entity = new PaymentMethodConfig
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            Color = req.Color,
            IsCash = req.IsCash,
            IsCard = req.IsCard,
            IsActive = true,
            SortOrder = req.SortOrder,
        };
        _db.PaymentMethodConfigs.Add(entity);
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapPaymentMethod(entity);
    }
}

public class UpdatePaymentMethodHandler : IRequestHandler<UpdatePaymentMethodCommand, PaymentMethodConfigDto>
{
    private readonly GrimorioDbContext _db;
    public UpdatePaymentMethodHandler(GrimorioDbContext db) => _db = db;

    public async Task<PaymentMethodConfigDto> Handle(UpdatePaymentMethodCommand req, CancellationToken ct)
    {
        var entity = await _db.PaymentMethodConfigs
            .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Método de pago no encontrado.");

        entity.Name = req.Name.Trim();
        entity.Color = req.Color;
        entity.IsCash = req.IsCash;
        entity.IsCard = req.IsCard;
        entity.IsActive = req.IsActive;
        entity.SortOrder = req.SortOrder;
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapPaymentMethod(entity);
    }
}

public class DeletePaymentMethodHandler : IRequestHandler<DeletePaymentMethodCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeletePaymentMethodHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeletePaymentMethodCommand req, CancellationToken ct)
    {
        var entity = await _db.PaymentMethodConfigs
            .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Método de pago no encontrado.");

        var inUse = await _db.PaymentLines.AnyAsync(l => l.PaymentMethodConfigId == req.Id, ct);
        if (inUse)
            throw new InvalidOperationException("No se puede eliminar un medio de pago que ya tiene registros de cobro. Desactívalo en su lugar.");

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── Customers ─────────────────────────────────────────────────────────────────

public class CreateCardBankHandler : IRequestHandler<CreateCardBankCommand, CardBankDto>
{
    private readonly GrimorioDbContext _db;
    public CreateCardBankHandler(GrimorioDbContext db) => _db = db;

    public async Task<CardBankDto> Handle(CreateCardBankCommand req, CancellationToken ct)
    {
        var name = req.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Ingresa el nombre del banco.");

        var entity = new CardBank
        {
            Id = Guid.NewGuid(),
            BranchId = req.BranchId,
            Name = name,
            IsActive = true,
            SortOrder = req.SortOrder,
        };
        _db.CardBanks.Add(entity);
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapCardBank(entity);
    }
}

public class UpdateCardBankHandler : IRequestHandler<UpdateCardBankCommand, CardBankDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateCardBankHandler(GrimorioDbContext db) => _db = db;

    public async Task<CardBankDto> Handle(UpdateCardBankCommand req, CancellationToken ct)
    {
        var entity = await _db.CardBanks
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Banco no encontrado.");

        entity.Name = req.Name.Trim();
        entity.IsActive = req.IsActive;
        entity.SortOrder = req.SortOrder;
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapCardBank(entity);
    }
}

public class DeleteCardBankHandler : IRequestHandler<DeleteCardBankCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteCardBankHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteCardBankCommand req, CancellationToken ct)
    {
        var entity = await _db.CardBanks
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Banco no encontrado.");

        var inUse = await _db.PaymentLines.AnyAsync(l => l.CardBankId == req.Id, ct);
        if (inUse) entity.IsActive = false;
        else entity.IsDeleted = true;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class CreateCashRegisterHandler : IRequestHandler<CreateCashRegisterCommand, CashRegisterDto>
{
    private readonly GrimorioDbContext _db;
    public CreateCashRegisterHandler(GrimorioDbContext db) => _db = db;

    public async Task<CashRegisterDto> Handle(CreateCashRegisterCommand req, CancellationToken ct)
    {
        var name = req.Name.Trim();
        var code = req.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Ingresa el nombre de la caja.");
        if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Ingresa el codigo de la caja.");

        var exists = await _db.CashRegisters
            .AnyAsync(x => x.BranchId == req.BranchId && x.Code == code && !x.IsDeleted, ct);
        if (exists) throw new InvalidOperationException("Ya existe una caja con ese codigo.");

        var entity = new CashRegister
        {
            Id = Guid.NewGuid(),
            BranchId = req.BranchId,
            Name = name,
            Code = code,
            Description = req.Description?.Trim(),
            IsActive = true,
        };
        _db.CashRegisters.Add(entity);
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapCashRegister(entity);
    }
}

public class UpdateCashRegisterHandler : IRequestHandler<UpdateCashRegisterCommand, CashRegisterDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateCashRegisterHandler(GrimorioDbContext db) => _db = db;

    public async Task<CashRegisterDto> Handle(UpdateCashRegisterCommand req, CancellationToken ct)
    {
        var entity = await _db.CashRegisters
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Caja no encontrada.");

        var name = req.Name.Trim();
        var code = req.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Ingresa el nombre de la caja.");
        if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Ingresa el codigo de la caja.");

        var exists = await _db.CashRegisters
            .AnyAsync(x => x.Id != req.Id && x.BranchId == req.BranchId && x.Code == code && !x.IsDeleted, ct);
        if (exists) throw new InvalidOperationException("Ya existe una caja con ese codigo.");

        entity.Name = name;
        entity.Code = code;
        entity.Description = req.Description?.Trim();
        entity.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapCashRegister(entity);
    }
}

public class DeleteCashRegisterHandler : IRequestHandler<DeleteCashRegisterCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteCashRegisterHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteCashRegisterCommand req, CancellationToken ct)
    {
        var entity = await _db.CashRegisters
            .FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Caja no encontrada.");

        var hasOpenSession = await _db.CashSessions.AnyAsync(s =>
            s.CashRegisterId == req.Id && s.Status == CashSessionStatus.Open && !s.IsDeleted, ct);
        if (hasOpenSession)
            throw new InvalidOperationException("No se puede eliminar una caja con sesion abierta.");

        var hasHistory = await _db.CashSessions.AnyAsync(s => s.CashRegisterId == req.Id && !s.IsDeleted, ct);
        if (hasHistory) entity.IsActive = false;
        else entity.IsDeleted = true;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly GrimorioDbContext _db;
    public CreateCustomerHandler(GrimorioDbContext db) => _db = db;

    public async Task<CustomerDto> Handle(CreateCustomerCommand req, CancellationToken ct)
    {
        if (!Enum.TryParse<TaxIdType>(req.TaxIdType, out var taxIdType))
            throw new InvalidOperationException($"Tipo de identificación inválido: {req.TaxIdType}");

        var entity = new Customer
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            Name = req.Name.Trim(), TaxId = req.TaxId?.Trim(),
            TaxIdType = taxIdType, Address = req.Address?.Trim(),
            Phone = req.Phone?.Trim(), Email = req.Email?.Trim(),
        };
        _db.Customers.Add(entity);
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapCustomer(entity);
    }
}

public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly GrimorioDbContext _db;
    public UpdateCustomerHandler(GrimorioDbContext db) => _db = db;

    public async Task<CustomerDto> Handle(UpdateCustomerCommand req, CancellationToken ct)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        if (!Enum.TryParse<TaxIdType>(req.TaxIdType, out var taxIdType))
            throw new InvalidOperationException($"Tipo de identificación inválido: {req.TaxIdType}");

        entity.Name = req.Name.Trim();
        entity.TaxId = req.TaxId?.Trim();
        entity.TaxIdType = taxIdType;
        entity.Address = req.Address?.Trim();
        entity.Phone = req.Phone?.Trim();
        entity.Email = req.Email?.Trim();
        entity.IsActive = req.IsActive;
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapCustomer(entity);
    }
}

public class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommand, bool>
{
    private readonly GrimorioDbContext _db;
    public DeleteCustomerHandler(GrimorioDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteCustomerCommand req, CancellationToken ct)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(x => x.Id == req.Id && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");
        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

// ── CashSession ───────────────────────────────────────────────────────────────

public class OpenCashSessionHandler : IRequestHandler<OpenCashSessionCommand, CashSessionDto>
{
    private readonly GrimorioDbContext _db;
    public OpenCashSessionHandler(GrimorioDbContext db) => _db = db;

    public async Task<CashSessionDto> Handle(OpenCashSessionCommand req, CancellationToken ct)
    {
        var register = await _db.CashRegisters
            .FirstOrDefaultAsync(x => x.Id == req.CashRegisterId && x.BranchId == req.BranchId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Caja no encontrada.");

        if (!register.IsActive)
            throw new InvalidOperationException("La caja seleccionada esta inactiva.");

        var openForRegister = await _db.CashSessions
            .AnyAsync(x => x.CashRegisterId == req.CashRegisterId && x.Status == CashSessionStatus.Open && !x.IsDeleted, ct);
        if (openForRegister)
            throw new InvalidOperationException("Esta caja ya tiene una sesion abierta.");

        var openForUser = await _db.CashSessions
            .AnyAsync(x => x.BranchId == req.BranchId && x.OpenedBy == req.UserId && x.Status == CashSessionStatus.Open && !x.IsDeleted, ct);
        if (openForUser)
            throw new InvalidOperationException("Ya tienes una sesion de caja abierta.");

        var session = new CashSession
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            CashRegisterId = req.CashRegisterId,
            CashRegister = register,
            OpenedBy = req.UserId, OpenedByName = req.UserName,
            OpeningBalance = req.OpeningBalance,
            OpenedAt = DateTime.UtcNow,
            Status = CashSessionStatus.Open,
        };
        _db.CashSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return BillingMapper.MapSession(session);
    }
}

public class CloseCashSessionHandler : IRequestHandler<CloseCashSessionCommand, CashSessionDto>
{
    private readonly GrimorioDbContext _db;
    public CloseCashSessionHandler(GrimorioDbContext db) => _db = db;

    public async Task<CashSessionDto> Handle(CloseCashSessionCommand req, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var session = await _db.CashSessions
            .FromSqlInterpolated($"""
                SELECT * FROM billing."CashSessions"
                WHERE "Id" = {req.Id}
                    AND "BranchId" = {req.BranchId}
                    AND "IsDeleted" = false
                FOR UPDATE
                """)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Sesión no encontrada.");

        await _db.Entry(session).Reference(s => s.CashRegister).LoadAsync(ct);
        await _db.Entry(session)
            .Collection(s => s.Payments)
            .Query()
            .Where(p => !p.IsDeleted)
            .Include(p => p.Lines).ThenInclude(l => l.Config)
            .LoadAsync(ct);

        var expenses = await _db.Expenses
            .Include(e => e.PaymentMethodConfig)
            .Where(e => e.BranchId == req.BranchId
                && e.CashSessionId == session.Id
                && e.Status == Domain.Entities.Finance.ExpenseStatus.Registered)
            .ToListAsync(ct);

        if (session.Status != CashSessionStatus.Open)
            throw new InvalidOperationException("La sesión ya está cerrada.");

        if (session.OpenedBy != req.UserId)
            throw new InvalidOperationException("Solo puedes cerrar la caja que abriste con tu usuario.");

        session.ClosedAt = DateTime.UtcNow;
        session.ClosedBy = req.UserId;
        session.ClosedByName = req.UserName;
        session.ActualCash = req.ActualCash;
        session.CloseNotes = req.Notes?.Trim();
        session.Status = CashSessionStatus.Closed;
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return BillingMapper.MapSession(session, null, expenses);
    }
}

// ── Payment ───────────────────────────────────────────────────────────────────

public class PayOrderHandler : IRequestHandler<PayOrderCommand, OrderPaymentDto>
{
    private readonly GrimorioDbContext _db;
    private readonly IMediator _mediator;
    private readonly ILogger<PayOrderHandler> _logger;
    public PayOrderHandler(GrimorioDbContext db, IMediator mediator, ILogger<PayOrderHandler> logger)
    { _db = db; _mediator = mediator; _logger = logger; }

    public async Task<OrderPaymentDto> Handle(PayOrderCommand req, CancellationToken ct)
    {
        if (req.IdempotencyKey == Guid.Empty)
            throw new PaymentRejectedException("La solicitud de cobro no tiene una clave de idempotencia válida.");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        // Serializar cobros de la misma orden. El saldo y las cantidades pagadas deben
        // calcularse después de obtener este bloqueo para impedir cobros concurrentes duplicados.
        _ = await _db.Orders
            .FromSqlInterpolated($"""
                SELECT * FROM pos."Orders"
                WHERE "Id" = {req.OrderId} AND "BranchId" = {req.BranchId} AND NOT "IsDeleted"
                FOR UPDATE
                """)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Orden no encontrada.");

        var order = await _db.Orders
            .Include(o => o.Payments)
            .Include(o => o.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.MenuItem)
            .Include(o => o.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Promotion)
            .Include(o => o.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.TaxRate)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.BranchId == req.BranchId && !o.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Orden no encontrada.");

        var existingPayment = await _db.OrderPayments
            .Include(p => p.Lines).ThenInclude(l => l.Config)
            .Include(p => p.Items).ThenInclude(i => i.OrderItem).ThenInclude(i => i!.MenuItem)
            .Include(p => p.Customer)
            .Include(p => p.CashSession).ThenInclude(s => s!.CashRegister)
            .FirstOrDefaultAsync(p => p.BranchId == req.BranchId
                && p.IdempotencyKey == req.IdempotencyKey
                && !p.IsDeleted, ct);

        if (existingPayment != null)
        {
            EnsureMatchingPayment(existingPayment, req);

            var existingDocument = await _db.ElectronicDocuments
                .FirstOrDefaultAsync(d => d.OrderPaymentId == existingPayment.Id && !d.IsDeleted, ct);

            await transaction.CommitAsync(ct);
            _logger.LogInformation(
                "Se recuperó el cobro existente {PaymentId} para la clave idempotente {IdempotencyKey}.",
                existingPayment.Id,
                req.IdempotencyKey);
            return BillingMapper.MapPayment(
                existingPayment,
                order.Number,
                existingPayment.Customer,
                elDoc: existingDocument);
        }

        if (order.Status == Domain.Entities.POS.OrderStatus.Cancelled)
            throw new PaymentRejectedException("No se puede cobrar una orden cancelada.");

        if (order.Status == Domain.Entities.POS.OrderStatus.Draft)
            throw new PaymentRejectedException("La orden debe estar confirmada antes de cobrarla.");

        if (req.Lines.Count == 0)
            throw new PaymentRejectedException("Se requiere al menos un medio de pago.");

        if (req.OrderAmount <= 0)
            throw new PaymentRejectedException("El monto a cobrar debe ser mayor a cero.");

        // Cargar métodos de pago usados en este cobro
        var methodIds = req.Lines.Select(l => l.MethodId).Distinct().ToList();
        var methods = await _db.PaymentMethodConfigs
            .Where(m => methodIds.Contains(m.Id) && !m.IsDeleted)
            .ToListAsync(ct);

        if (methods.Count != methodIds.Count)
            throw new PaymentRejectedException("Uno o más medios de pago no existen.");

        var methodMap = methods.ToDictionary(m => m.Id);
        var cardLines = req.Lines.Where(l => methodMap[l.MethodId].IsCard).ToList();
        var bankIds = cardLines
            .Select(l => l.CardBankId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var bankMap = bankIds.Count == 0
            ? new Dictionary<Guid, CardBank>()
            : await _db.CardBanks
                .Where(b => bankIds.Contains(b.Id) && b.BranchId == req.BranchId && b.IsActive && !b.IsDeleted)
                .ToDictionaryAsync(b => b.Id, ct);

        foreach (var line in cardLines)
        {
            if (!Enum.TryParse<CardPaymentType>(line.CardPaymentType, true, out _))
                throw new PaymentRejectedException("Selecciona si la tarjeta es de credito o debito.");

            if (!line.CardBankId.HasValue || !bankMap.ContainsKey(line.CardBankId.Value))
                throw new PaymentRejectedException("Selecciona un banco valido para el cobro con tarjeta.");

            if (string.IsNullOrWhiteSpace(line.CardBrand))
                throw new PaymentRejectedException("Selecciona el tipo de tarjeta.");

            if (string.IsNullOrWhiteSpace(line.AuthorizationNumber))
                throw new PaymentRejectedException("Ingresa el numero de autorizacion de la tarjeta.");
        }

        var alreadyPaid = order.Payments.Where(p => !p.IsDeleted).Sum(p => p.OrderAmount);

        var totalTendered = req.Lines.Sum(l => l.AmountTendered);
        if (totalTendered < req.OrderAmount)
            throw new PaymentRejectedException("Los medios de pago no cubren el monto indicado.");

        var totalChange = totalTendered - req.OrderAmount;
        var hasCashLine = req.Lines.Any(l => methodMap[l.MethodId].IsCash);
        if (totalChange > 0 && !hasCashLine)
            throw new PaymentRejectedException("Hay excedente pero no se indicó línea de efectivo para dar vuelto.");

        if (!Enum.TryParse<DocumentType>(req.DocumentType, out var docType))
            throw new PaymentRejectedException($"Tipo de documento inválido: {req.DocumentType}");

        if (docType == DocumentType.Factura && !req.CustomerId.HasValue)
            throw new PaymentRejectedException("La factura requiere un cliente con RUC o cédula.");

        Customer? customer = null;
        if (req.CustomerId.HasValue)
        {
            customer = await _db.Customers.FirstOrDefaultAsync(
                c => c.Id == req.CustomerId.Value && c.BranchId == req.BranchId && !c.IsDeleted, ct)
                ?? throw new KeyNotFoundException("Cliente no encontrado.");
        }

        BranchTaxConfig? invoiceConfig = null;
        if (docType == DocumentType.Factura)
        {
            invoiceConfig = await _db.BranchTaxConfigs
                .FirstOrDefaultAsync(c => c.BranchId == req.BranchId && !c.IsDeleted, ct)
                ?? throw new PaymentRejectedException("Configure los datos del emisor antes de cobrar una factura.");

            var certificate = await _db.SriCertificates
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.BranchId == req.BranchId && !c.IsDeleted, ct)
                ?? throw new PaymentRejectedException("Cargue el certificado .p12 antes de cobrar una factura.");

            if (certificate.ExpiresAt.HasValue && certificate.ExpiresAt.Value <= DateTime.UtcNow)
                throw new PaymentRejectedException("El certificado de firma electronica esta vencido.");
        }

        var selectedItems = req.Items
            .Where(i => i.Quantity > 0)
            .GroupBy(i => i.OrderItemId)
            .Select(g => new PaymentItemCommand { OrderItemId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .ToList();

        var activeOrderItems = order.Items
            .Where(i => !i.IsDeleted && i.Status != OrderItemStatus.Cancelled)
            .ToList();
        var activeOrderItemIds = activeOrderItems.Select(i => i.Id).ToList();
        var paidItems = await _db.OrderPaymentItems
            .Where(i => i.BranchId == req.BranchId
                && activeOrderItemIds.Contains(i.OrderItemId)
                && !i.IsDeleted)
            .AsNoTracking()
            .ToListAsync(ct);
        var paidQuantities = paidItems.GroupBy(i => i.OrderItemId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        var paidTotals = paidItems.GroupBy(i => i.OrderItemId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Total));
        if (activeOrderItems.Any(i => i.Promotion != null)
            && Math.Abs(paidItems.Sum(i => i.Total) - alreadyPaid) > 0.01m)
            throw new PaymentRejectedException("Los cobros anteriores no tienen un detalle compatible. Revisa la orden antes de aplicar precios promocionales al saldo.");

        // En un cobro no dividido, persistir igualmente el detalle exacto de todo lo pendiente.
        // Ese snapshot es la fuente de la factura, el ticket y la trazabilidad del inventario.
        if (selectedItems.Count == 0)
        {
            selectedItems = activeOrderItems
                .Select(i => new PaymentItemCommand
                {
                    OrderItemId = i.Id,
                    Quantity = i.Quantity - paidQuantities.GetValueOrDefault(i.Id),
                })
                .Where(i => i.Quantity > 0)
                .ToList();
        }

        var remainingQuantityBeforePayment = new Dictionary<Guid, decimal>();
        List<OrderPaymentItem> paymentItems = [];
        if (selectedItems.Count > 0)
        {
            var selectedItemIds = selectedItems.Select(i => i.OrderItemId).ToList();
            var orderItemsById = activeOrderItems
                .Where(i => selectedItemIds.Contains(i.Id))
                .ToDictionary(i => i.Id);

            if (orderItemsById.Count != selectedItemIds.Distinct().Count())
                throw new PaymentRejectedException("Uno o mas items seleccionados no pertenecen a la orden o estan cancelados.");

            foreach (var selected in selectedItems)
            {
                var item = orderItemsById[selected.OrderItemId];
                var paidQuantity = paidQuantities.GetValueOrDefault(item.Id);
                var remainingQuantity = item.Quantity - paidQuantity;
                if (selected.Quantity > remainingQuantity)
                    throw new PaymentRejectedException($"La cantidad a cobrar de {item.MenuItem?.Name ?? "item"} supera lo pendiente.");

                remainingQuantityBeforePayment[item.Id] = remainingQuantity;
                var selectedTotal = OrderPaymentPricing.AllocateTotal(
                    OrderPaymentPricing.GetLineTotal(item, cardLines.Count > 0),
                    item.Quantity, paidQuantity, selected.Quantity);
                var netUnitPrice = Math.Round(selectedTotal / selected.Quantity, 4);
                var taxRate = item.TaxRate ?? item.MenuItem?.TaxRate;
                paymentItems.Add(new OrderPaymentItem
                {
                    Id = Guid.NewGuid(),
                    BranchId = req.BranchId,
                    OrderPaymentId = Guid.Empty,
                    OrderItemId = item.Id,
                    ItemCode = item.MenuItem?.InternalCode ?? item.MenuItemId.ToString()[..8],
                    ItemName = item.MenuItem?.Name ?? item.MenuItemId.ToString()[..8],
                    Quantity = selected.Quantity,
                    UnitPrice = netUnitPrice,
                    Total = selectedTotal,
                    TaxRateSriCode = taxRate?.SriCode ?? "6",
                    TaxRatePercentage = taxRate?.Percentage ?? 0m,
                    OrderItem = item,
                });

                if (item.Promotion != null)
                {
                    var pendingTotal = OrderPaymentPricing.AllocateTotal(
                        OrderPaymentPricing.GetLineTotal(item, false), item.Quantity,
                        paidQuantity + selected.Quantity, remainingQuantity - selected.Quantity);
                    OrderPaymentPricing.UpdateItemTotal(item, paidTotals.GetValueOrDefault(item.Id), selectedTotal, pendingTotal);
                }
            }

            var itemsAmount = paymentItems.Sum(i => i.Total);
            if (Math.Abs(itemsAmount - req.OrderAmount) > 0.01m)
                throw new PaymentRejectedException($"El total de items seleccionados ({itemsAmount:F2}) no coincide con el monto a cobrar ({req.OrderAmount:F2}).");
        }

        if (paymentItems.Count == 0)
            throw new PaymentRejectedException("No existen items pendientes para asociar al cobro.");

        PosMapper.RecalculateOrderTotals(order);
        var remaining = order.Total - alreadyPaid;
        if (req.OrderAmount > remaining + 0.01m)
            throw new PaymentRejectedException($"El monto ({req.OrderAmount:F2}) supera el saldo pendiente ({remaining:F2}).");

        var paidAt = DateTime.UtcNow;

        // Si el frontend no envió sesión, auto-asignar la activa del sucursal
        var session = req.CashSessionId.HasValue
            ? await _db.CashSessions.FirstOrDefaultAsync(s =>
                s.Id == req.CashSessionId.Value &&
                s.BranchId == req.BranchId &&
                s.Status == CashSessionStatus.Open &&
                !s.IsDeleted, ct)
            : await _db.CashSessions.FirstOrDefaultAsync(s =>
                s.BranchId == req.BranchId &&
                s.OpenedBy == req.UserId &&
                s.Status == CashSessionStatus.Open &&
                !s.IsDeleted, ct);

        if (session == null)
            throw new PaymentRejectedException("Debes abrir tu caja antes de cobrar.");

        if (session.OpenedBy != req.UserId)
            throw new PaymentRejectedException("No puedes cobrar en una sesion de caja abierta por otro usuario.");

        await _db.CashSessions
            .FromSqlInterpolated($"""
                SELECT * FROM billing."CashSessions"
                WHERE "Id" = {session.Id}
                FOR UPDATE
                """)
            .AsNoTracking()
            .FirstAsync(ct);
        await _db.Entry(session).ReloadAsync(ct);

        if (session.Status != CashSessionStatus.Open)
            throw new PaymentRejectedException("La sesion de caja ya fue cerrada. Abre una nueva caja para cobrar.");

        var payment = new OrderPayment
        {
            Id = Guid.NewGuid(), BranchId = req.BranchId,
            IdempotencyKey = req.IdempotencyKey,
            OrderId = order.Id, CashSessionId = session.Id,
            CustomerId = req.CustomerId, DocumentType = docType,
            OrderAmount = req.OrderAmount, PaidAt = paidAt,
            Customer = customer,
        };
        foreach (var item in paymentItems)
            item.OrderPaymentId = payment.Id;

        var remainingChange = totalChange;
        var lines = req.Lines.Select(l =>
        {
            var method = methodMap[l.MethodId];
            var change = 0m;
            CardPaymentType? cardPaymentType = null;
            Guid? cardBankId = null;
            string? cardBankName = null;
            string? cardBrand = null;
            string? authorizationNumber = null;

            if (method.IsCard)
            {
                Enum.TryParse<CardPaymentType>(l.CardPaymentType, true, out var parsedCardType);
                cardPaymentType = parsedCardType;
                cardBankId = l.CardBankId;
                cardBankName = l.CardBankId.HasValue ? bankMap[l.CardBankId.Value].Name : null;
                cardBrand = l.CardBrand?.Trim();
                authorizationNumber = l.AuthorizationNumber?.Trim();
            }

            if (method.IsCash && remainingChange > 0)
            {
                change = Math.Min(remainingChange, l.AmountTendered);
                remainingChange -= change;
            }
            return new PaymentLine
            {
                Id = Guid.NewGuid(),
                OrderPaymentId = payment.Id,
                PaymentMethodConfigId = l.MethodId,
                AmountTendered = l.AmountTendered,
                Change = change,
                CardPaymentType = cardPaymentType,
                CardBankId = cardBankId,
                CardBankName = cardBankName,
                CardBrand = cardBrand,
                AuthorizationNumber = authorizationNumber,
                Config = method,
            };
        }).ToList();

        payment.Lines = lines;
        payment.Items = paymentItems;
        _db.OrderPayments.Add(payment);

        var newRemaining = remaining - req.OrderAmount;
        var isFullyPaid = newRemaining <= 0.01m;
        if (isFullyPaid)
        {
            order.PaidAt = paidAt;
            order.CustomerId ??= req.CustomerId;
        }

        await _db.SaveChangesAsync(ct);

        if (paymentItems.Count > 0)
            await DeductInventoryForPaymentItemsAsync(paymentItems, remainingQuantityBeforePayment, order.Number, req.BranchId, ct);

        ElectronicDocument? electronicDocument = null;
        if (invoiceConfig != null)
        {
            await _db.BranchTaxConfigs
                .FromSqlInterpolated($"""
                    SELECT * FROM billing."BranchTaxConfigs"
                    WHERE "Id" = {invoiceConfig.Id}
                    FOR UPDATE
                    """)
                .AsNoTracking()
                .FirstAsync(ct);
            await _db.Entry(invoiceConfig).ReloadAsync(ct);

            invoiceConfig.Secuencial += 1;
            var invoiceLines = SriInvoiceLineFactory.Build(payment, order);
            var invoiceTotals = SriInvoiceLineFactory.CalculateTotals(invoiceLines, payment.OrderAmount);
            var localEmissionDate = EcuadorTime.FromUtc(paidAt);
            var accessKey = SriKeyGenerator.Build(
                localEmissionDate,
                invoiceConfig.Ruc,
                invoiceConfig.Ambiente,
                invoiceConfig.CodigoEstablecimiento,
                invoiceConfig.PuntoEmision,
                invoiceConfig.Secuencial);

            electronicDocument = new ElectronicDocument
            {
                Id = Guid.NewGuid(),
                BranchId = req.BranchId,
                OrderPaymentId = payment.Id,
                ClaveAcceso = accessKey,
                NumeroFactura = $"{invoiceConfig.CodigoEstablecimiento.PadLeft(3, '0')}-{invoiceConfig.PuntoEmision.PadLeft(3, '0')}-{invoiceConfig.Secuencial.ToString().PadLeft(9, '0')}",
                Secuencial = invoiceConfig.Secuencial,
                Environment = invoiceConfig.Ambiente,
                Status = ElectronicDocumentStatus.Pending,
                TotalSinImpuestos = invoiceTotals.TotalWithoutTaxes,
                TotalDescuento = 0m,
                TotalIva = invoiceTotals.TotalTax,
                ImporteTotal = invoiceTotals.Total,
                EmissionDate = paidAt,
                OriginalEmissionDate = paidAt,
                EmailStatus = string.IsNullOrWhiteSpace(customer?.Email)
                    ? ElectronicDocumentEmailStatus.Skipped
                    : ElectronicDocumentEmailStatus.Pending,
                EmailRecipient = customer?.Email?.Trim(),
                EmailErrorMessage = string.IsNullOrWhiteSpace(customer?.Email)
                    ? "El cliente no tiene un correo electrónico registrado."
                    : null,
            };
            _db.ElectronicDocuments.Add(electronicDocument);
            await _db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);

        if (electronicDocument != null)
        {
            try
            {
                var processed = await _mediator.Send(new RetryElectronicInvoiceCommand
                {
                    DocumentId = electronicDocument.Id,
                    BranchId = req.BranchId,
                }, ct);
                electronicDocument.Status = Enum.Parse<ElectronicDocumentStatus>(processed.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "El cobro {PaymentId} fue confirmado, pero la factura {DocumentId} quedo pendiente de procesamiento.",
                    payment.Id, electronicDocument.Id);
            }
        }

        return BillingMapper.MapPayment(payment, order.Number, customer, elDoc: electronicDocument);
    }

    private static void EnsureMatchingPayment(OrderPayment payment, PayOrderCommand req)
    {
        if (payment.OrderId != req.OrderId || payment.OrderAmount != req.OrderAmount
            || payment.CustomerId != req.CustomerId || payment.DocumentType.ToString() != req.DocumentType
            || (req.CashSessionId.HasValue && payment.CashSessionId != req.CashSessionId))
            throw new PaymentIdempotencyConflictException();

        var unmatchedLines = payment.Lines.Where(l => !l.IsDeleted).ToList();
        foreach (var line in req.Lines)
        {
            var match = unmatchedLines.FindIndex(l => l.PaymentMethodConfigId == line.MethodId
                && l.AmountTendered == line.AmountTendered
                && string.Equals(l.CardPaymentType?.ToString(), line.CardPaymentType, StringComparison.OrdinalIgnoreCase)
                && l.CardBankId == line.CardBankId
                && l.CardBrand == line.CardBrand?.Trim()
                && l.AuthorizationNumber == line.AuthorizationNumber?.Trim());
            if (match < 0) throw new PaymentIdempotencyConflictException();
            unmatchedLines.RemoveAt(match);
        }
        if (unmatchedLines.Count > 0) throw new PaymentIdempotencyConflictException();

        // Empty items represents the original full-balance request, not today's remaining items.
        if (req.Items.Count == 0) return;
        var requested = req.Items.GroupBy(i => i.OrderItemId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        var paid = payment.Items.Where(i => !i.IsDeleted).GroupBy(i => i.OrderItemId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        if (req.Items.Any(i => i.Quantity <= 0) || requested.Count != paid.Count
            || requested.Any(i => !paid.TryGetValue(i.Key, out var quantity) || quantity != i.Value))
            throw new PaymentIdempotencyConflictException();
    }


    private async Task DeductInventoryForPaymentItemsAsync(
        IReadOnlyCollection<OrderPaymentItem> paymentItems,
        IReadOnlyDictionary<Guid, decimal> remainingQuantityBeforePayment,
        int orderNumber,
        Guid branchId,
        CancellationToken ct)
    {
        var itemIds = paymentItems.Select(i => i.OrderItemId).Distinct().ToList();
        if (itemIds.Count == 0) return;

        var reservations = await _db.StockReservations
            .Where(r => r.BranchId == branchId
                && itemIds.Contains(r.OrderItemId)
                && r.Status == StockReservationStatus.Active
                && !r.IsDeleted)
            .ToListAsync(ct);

        if (reservations.Count == 0) return;

        var reservationsByItem = reservations.GroupBy(r => r.OrderItemId).ToDictionary(g => g.Key, g => g.ToList());
        var now = DateTime.UtcNow;

        foreach (var paymentItem in paymentItems)
        {
            if (!reservationsByItem.TryGetValue(paymentItem.OrderItemId, out var itemReservations))
                continue;

            var remainingBefore = remainingQuantityBeforePayment.GetValueOrDefault(paymentItem.OrderItemId);
            if (remainingBefore <= 0) continue;

            var factor = Math.Min(1m, paymentItem.Quantity / remainingBefore);
            foreach (var reservation in itemReservations)
            {
                var quantityToConsume = reservation.Quantity * factor;
                if (quantityToConsume <= 0) continue;

                await _mediator.Send(new RegisterMovementCommand
                {
                    BranchId = branchId,
                    ArticleId = reservation.ArticleId,
                    WarehouseId = reservation.WarehouseId,
                    Type = MovementType.SaleDeduction,
                    Quantity = quantityToConsume,
                    UnitId = reservation.UnitId,
                    Reference = $"Orden #{orderNumber}",
                }, ct);

                if (quantityToConsume >= reservation.Quantity - 0.0001m)
                {
                    reservation.Status = StockReservationStatus.Consumed;
                    reservation.ConsumedAt = now;
                }
                else
                {
                    reservation.Quantity -= quantityToConsume;
                    reservation.BaseQuantity -= quantityToConsume;
                }
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task DeductInventoryForOrderAsync(Guid orderId, int orderNumber, Guid branchId, CancellationToken ct)
    {
        var reservations = await _db.StockReservations
            .Where(r => r.BranchId == branchId
                && r.OrderId == orderId
                && r.Status == StockReservationStatus.Active
                && !r.IsDeleted)
            .ToListAsync(ct);

        if (reservations.Count > 0)
        {
            foreach (var reservation in reservations)
            {
                try
                {
                    await _mediator.Send(new RegisterMovementCommand
                    {
                        BranchId = branchId,
                        ArticleId = reservation.ArticleId,
                        WarehouseId = reservation.WarehouseId,
                        Type = MovementType.SaleDeduction,
                        Quantity = reservation.Quantity,
                        UnitId = reservation.UnitId,
                        Reference = $"Orden #{orderNumber}",
                    }, ct);

                    reservation.Status = StockReservationStatus.Consumed;
                    reservation.ConsumedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "No se pudo consumir reserva de inventario: reserva {ReservationId}, artículo {ArticleId}, orden #{OrderNumber}",
                        reservation.Id, reservation.ArticleId, orderNumber);
                    throw;
                }
            }

            await _db.SaveChangesAsync(ct);
            return;
        }

        var order = await _db.Orders
            .Include(o => o.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.MenuItem)
                    .ThenInclude(m => m!.Recipe.Where(r => !r.IsDeleted))
                        .ThenInclude(r => r.Article)
                            .ThenInclude(a => a!.BaseUnit)
            .Include(o => o.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.MenuItem)
                    .ThenInclude(m => m!.Recipe.Where(r => !r.IsDeleted))
                        .ThenInclude(r => r.SubRecipe)
                            .ThenInclude(s => s!.Ingredients.Where(i => !i.IsDeleted))
                                .ThenInclude(i => i.Article)
                                    .ThenInclude(a => a!.BaseUnit)
            .Include(o => o.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.MenuItem)
                    .ThenInclude(m => m!.Recipe.Where(r => !r.IsDeleted))
                        .ThenInclude(r => r.SubRecipe)
                            .ThenInclude(s => s!.Ingredients.Where(i => !i.IsDeleted))
                                .ThenInclude(i => i.Unit)
            .Include(o => o.Items.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.MenuItem)
                    .ThenInclude(m => m!.Recipe.Where(r => !r.IsDeleted))
                        .ThenInclude(r => r.Unit)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null) return;

        // Bodega de respaldo: la primera activa de la sucursal
        var fallbackWarehouseId = await _db.Warehouses
            .Where(w => w.BranchId == branchId && !w.IsDeleted && w.IsActive)
            .OrderBy(w => w.CreatedAt)
            .Select(w => w.Id)
            .FirstOrDefaultAsync(ct);

        if (fallbackWarehouseId == default) return;

        var conversions = await _db.UnitConversions
            .AsNoTracking()
            .Where(c => c.BranchId == branchId && !c.IsDeleted)
            .Select(c => new UnitConversionInfo(c.OriginUnitId, c.DestinationUnitId, c.Factor))
            .ToListAsync(ct);

        foreach (var item in order.Items)
        {
            if (item.MenuItem?.Recipe == null) continue;

            foreach (var ingredient in MenuRecipeExpansion.Expand(item.MenuItem, item.Quantity, conversions))
            {
                // Ingrediente variable: usar el artículo elegido por el cliente
                var articleId = ingredient.ArticleId;

                // Bodega con más stock para ese artículo, o la de respaldo
                var warehouseId = await _db.WarehouseStock
                    .Where(ws => ws.ArticleId == articleId && ws.BranchId == branchId && !ws.IsDeleted)
                    .OrderByDescending(ws => ws.Quantity)
                    .Select(ws => ws.WarehouseId)
                    .FirstOrDefaultAsync(ct);

                if (warehouseId == default) warehouseId = fallbackWarehouseId;

                try
                {
                    await _mediator.Send(new RegisterMovementCommand
                    {
                        BranchId = branchId,
                        ArticleId = articleId,
                        WarehouseId = warehouseId,
                        Type = MovementType.SaleDeduction,
                        Quantity = ingredient.BaseQuantity,
                        UnitId = ingredient.BaseUnitId,
                        Reference = $"Orden #{orderNumber}",
                    }, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "No se pudo descontar inventario: artículo {ArticleId}, unidad {UnitId}, orden #{OrderNumber}",
                        articleId, ingredient.BaseUnitId, orderNumber);
                    throw;
                }
            }
        }
    }
}

// ── Factura Electrónica ───────────────────────────────────────────────────────

public class GenerateElectronicInvoiceHandler : IRequestHandler<GenerateElectronicInvoiceCommand, ElectronicDocumentDto>
{
    private readonly GrimorioDbContext _db;
    private readonly IMediator _mediator;

    public GenerateElectronicInvoiceHandler(GrimorioDbContext db, IMediator mediator)
    {
        _db = db; _mediator = mediator;
    }

    public async Task<ElectronicDocumentDto> Handle(GenerateElectronicInvoiceCommand req, CancellationToken ct)
    {
        // ── Cargar datos necesarios ───────────────────────────────────────────
        var payment = await _db.OrderPayments
            .Include(p => p.Lines).ThenInclude(l => l.Config)
            .Include(p => p.Items).ThenInclude(i => i.OrderItem).ThenInclude(i => i!.MenuItem).ThenInclude(i => i!.TaxRate)
            .Include(p => p.Items).ThenInclude(i => i.OrderItem).ThenInclude(i => i!.TaxRate)
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == req.OrderPaymentId && p.BranchId == req.BranchId && !p.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Pago no encontrado.");

        // Cada cobro puede originar un solo comprobante electrónico.
        var existing = await _db.ElectronicDocuments
            .FirstOrDefaultAsync(d => d.OrderPaymentId == req.OrderPaymentId && !d.IsDeleted, ct);
        if (existing != null)
            throw new InvalidOperationException(existing.Status == ElectronicDocumentStatus.Authorized
                ? "Este pago ya tiene una factura electrónica autorizada."
                : "Este pago ya tiene una factura electrónica. Utiliza la opción de reintento.");

        var order = await _db.Orders
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.MenuItem).ThenInclude(i => i!.TaxRate)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.TaxRate)
            .FirstOrDefaultAsync(o => o.Id == payment.OrderId && !o.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Orden no encontrada.");

        var invoiceLines = SriInvoiceLineFactory.Build(payment, order);
        var invoiceTotals = SriInvoiceLineFactory.CalculateTotals(invoiceLines, payment.OrderAmount);

        var config = await _db.BranchTaxConfigs
            .FirstOrDefaultAsync(c => c.BranchId == req.BranchId && !c.IsDeleted, ct)
            ?? throw new InvalidOperationException("Configure los datos del emisor antes de emitir facturas.");

        var sriCert = await _db.SriCertificates
            .FirstOrDefaultAsync(c => c.BranchId == req.BranchId && !c.IsDeleted, ct)
            ?? throw new InvalidOperationException("Cargue el certificado .p12 antes de emitir facturas.");

        var originalEmissionDate = payment.PaidAt;
        var paymentEmissionLocal = EcuadorTime.FromUtc(payment.PaidAt);
        var emissionDate = payment.PaidAt;
        var contingencyReason = req.ContingencyReason?.Trim();
        var isContingencyEmission = false;

        if (req.EmissionDate.HasValue)
        {
            var selectedLocalDate = req.EmissionDate.Value.Date;
            var todayLocalDate = EcuadorTime.FromUtc(DateTime.UtcNow).Date;
            if (selectedLocalDate > todayLocalDate)
                throw new InvalidOperationException("La fecha de emisión no puede ser futura.");

            isContingencyEmission = selectedLocalDate != paymentEmissionLocal.Date;
            if (isContingencyEmission)
            {
                if (string.IsNullOrWhiteSpace(contingencyReason))
                    throw new InvalidOperationException("Ingresa el motivo de contingencia para cambiar la fecha de emisión.");
                if (contingencyReason.Length > 500)
                    throw new InvalidOperationException("El motivo de contingencia no puede superar 500 caracteres.");
            }

            var selectedLocalDateTime = selectedLocalDate.Add(paymentEmissionLocal.TimeOfDay);
            emissionDate = EcuadorTime.ToUtc(selectedLocalDateTime);
        }

        // ── Incrementar secuencial de forma atómica ────────────────────────
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.BranchTaxConfigs
            .FromSqlInterpolated($"""
                SELECT * FROM billing."BranchTaxConfigs"
                WHERE "Id" = {config.Id}
                FOR UPDATE
                """)
            .AsNoTracking()
            .FirstAsync(ct);
        await _db.Entry(config).ReloadAsync(ct);

        // Revalidar dentro del bloqueo por solicitudes concurrentes sobre el mismo pago.
        if (await _db.ElectronicDocuments.AnyAsync(
                d => d.OrderPaymentId == req.OrderPaymentId && !d.IsDeleted, ct))
            throw new InvalidOperationException("Este pago ya tiene una factura electronica. Utiliza la opcion de reintento.");

        config.Secuencial += 1;
        var secuencial = config.Secuencial;

        // ── Generar clave de acceso ────────────────────────────────────────
        var claveAcceso = SriKeyGenerator.Build(
            EcuadorTime.FromUtc(emissionDate),
            config.Ruc, config.Ambiente,
            config.CodigoEstablecimiento, config.PuntoEmision,
            secuencial);

        var numeroFactura = $"{config.CodigoEstablecimiento.PadLeft(3, '0')}-{config.PuntoEmision.PadLeft(3, '0')}-{secuencial.ToString().PadLeft(9, '0')}";

        // ── Crear registro preliminar ──────────────────────────────────────
        var doc = new ElectronicDocument
        {
            BranchId = req.BranchId,
            OrderPaymentId = req.OrderPaymentId,
            ClaveAcceso = claveAcceso,
            NumeroFactura = numeroFactura,
            Secuencial = secuencial,
            Environment = config.Ambiente,
            Status = ElectronicDocumentStatus.Pending,
            TotalSinImpuestos = invoiceTotals.TotalWithoutTaxes,
            TotalDescuento = 0m,
            TotalIva = invoiceTotals.TotalTax,
            ImporteTotal = invoiceTotals.Total,
            EmissionDate = emissionDate,
            OriginalEmissionDate = originalEmissionDate,
            IsContingencyEmission = isContingencyEmission,
            ContingencyReason = isContingencyEmission ? contingencyReason : null,
            ContingencyUserId = isContingencyEmission ? req.UserId : null,
            ContingencyUserName = isContingencyEmission ? req.UserName?.Trim() : null,
            EmailStatus = string.IsNullOrWhiteSpace(payment.Customer?.Email)
                ? ElectronicDocumentEmailStatus.Skipped
                : ElectronicDocumentEmailStatus.Pending,
            EmailRecipient = payment.Customer?.Email?.Trim(),
            EmailErrorMessage = string.IsNullOrWhiteSpace(payment.Customer?.Email)
                ? "El cliente no tiene un correo electrónico registrado."
                : null,
        };
        _db.ElectronicDocuments.Add(doc);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await _mediator.Send(new RetryElectronicInvoiceCommand
        {
            DocumentId = doc.Id,
            BranchId = req.BranchId,
        }, ct);
    }

    internal static ElectronicDocumentDto MapDoc(ElectronicDocument d) => new()
    {
        Id = d.Id, OrderPaymentId = d.OrderPaymentId,
        OrderId = d.OrderPayment?.OrderId ?? Guid.Empty,
        OrderNumber = d.OrderPayment?.Order?.Number ?? 0,
        ClaveAcceso = d.ClaveAcceso, NumeroFactura = d.NumeroFactura,
        Secuencial = d.Secuencial, Environment = d.Environment,
        Status = d.Status.ToString(),
        TotalSinImpuestos = d.TotalSinImpuestos, TotalDescuento = d.TotalDescuento,
        TotalIva = d.TotalIva, ImporteTotal = d.ImporteTotal,
        NumeroAutorizacion = d.NumeroAutorizacion, FechaAutorizacion = d.FechaAutorizacion,
        ErrorMessage = d.ErrorMessage, SentAt = d.SentAt, RetryCount = d.RetryCount,
        CreatedAt = d.CreatedAt,
        EmissionDate = d.EmissionDate,
        OriginalEmissionDate = d.OriginalEmissionDate,
        IsContingencyEmission = d.IsContingencyEmission,
        ContingencyReason = d.ContingencyReason,
        ContingencyUserName = d.ContingencyUserName,
        HasRide = d.RidePdf != null && d.RidePdf.Length > 0,
        HasXml = !string.IsNullOrEmpty(d.XmlAuthorized ?? d.XmlSigned),
        HasXmlResponse = !string.IsNullOrEmpty(d.XmlResponseSri),
        EmailStatus = d.EmailStatus.ToString(),
        EmailRecipient = d.EmailRecipient,
        EmailSentAt = d.EmailSentAt,
        EmailErrorMessage = d.EmailErrorMessage,
        EmailRetryCount = d.EmailRetryCount,
    };
}

public class RetryElectronicInvoiceHandler : IRequestHandler<RetryElectronicInvoiceCommand, ElectronicDocumentDto>
{
    private readonly GrimorioDbContext _db;
    private readonly SriSoapClient _soap;
    private readonly IDataProtectionProvider _dp;
    private readonly IEmailService _email;
    private readonly ILogger<RetryElectronicInvoiceHandler> _log;

    public RetryElectronicInvoiceHandler(
        GrimorioDbContext db, SriSoapClient soap,
        IDataProtectionProvider dp, IEmailService email,
        ILogger<RetryElectronicInvoiceHandler> log)
    {
        _db = db; _soap = soap; _dp = dp; _email = email; _log = log;
    }

    public async Task<ElectronicDocumentDto> Handle(RetryElectronicInvoiceCommand req, CancellationToken ct)
    {
        var processingStartedAt = DateTime.UtcNow;
        var staleProcessingBefore = processingStartedAt.AddMinutes(-5);
        var claimed = await _db.ElectronicDocuments
            .Where(d => d.Id == req.DocumentId && d.BranchId == req.BranchId && !d.IsDeleted
                && d.Status != ElectronicDocumentStatus.Authorized
                && (d.ProcessingStartedAt == null || d.ProcessingStartedAt <= staleProcessingBefore))
            .ExecuteUpdateAsync(setters => setters.SetProperty(d => d.ProcessingStartedAt, processingStartedAt), ct);
        if (claimed == 0)
        {
            var current = await _db.ElectronicDocuments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == req.DocumentId && d.BranchId == req.BranchId && !d.IsDeleted, ct)
                ?? throw new KeyNotFoundException("Documento no encontrado.");
            return GenerateElectronicInvoiceHandler.MapDoc(current);
        }

        ElectronicDocument? doc = null;
        try
        {
            doc = await _db.ElectronicDocuments
                .Include(d => d.OrderPayment).ThenInclude(p => p!.Customer)
                .Include(d => d.OrderPayment).ThenInclude(p => p!.Lines).ThenInclude(l => l.Config)
                .Include(d => d.OrderPayment).ThenInclude(p => p!.Items).ThenInclude(i => i.OrderItem).ThenInclude(i => i!.MenuItem).ThenInclude(i => i!.TaxRate)
                .Include(d => d.OrderPayment).ThenInclude(p => p!.Items).ThenInclude(i => i.OrderItem).ThenInclude(i => i!.TaxRate)
                .FirstAsync(d => d.Id == req.DocumentId && d.BranchId == req.BranchId && !d.IsDeleted, ct);
            await _db.Entry(doc).ReloadAsync(ct);

            var wasRejected = doc.Status == ElectronicDocumentStatus.Rejected;
            var received = doc.Status == ElectronicDocumentStatus.Sent;
            doc.RetryCount++;
            doc.SentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            var result = await SriInvoiceRecovery.ProcessAsync(
                () => _soap.AutorizarComprobanteAsync(doc.ClaveAcceso, doc.Environment, ct),
                async () =>
                {
                    // Uncertain transmissions reuse the exact persisted XML, not current customer/config data.
                    if (string.IsNullOrWhiteSpace(doc.XmlSigned) || wasRejected)
                        await SignAsync(doc, ct);
                    doc.Status = ElectronicDocumentStatus.Pending;
                    doc.ErrorMessage = null;
                    await _db.SaveChangesAsync(ct);
                    var reception = await _soap.ValidarComprobanteAsync(doc.XmlSigned!, doc.Environment, ct);
                    doc.XmlResponseSri = reception.RawXml;
                    return reception;
                },
                async () =>
                {
                    doc.Status = ElectronicDocumentStatus.Sent;
                    await _db.SaveChangesAsync(ct);
                },
                received, wasRejected, ct);

            doc.XmlResponseSri = result.RawXml;
            doc.ErrorMessage = result.Error;
            doc.Status = result.Status switch
            {
                SriRecoveryStatus.Authorized => ElectronicDocumentStatus.Authorized,
                SriRecoveryStatus.Rejected => ElectronicDocumentStatus.Rejected,
                SriRecoveryStatus.Sent => ElectronicDocumentStatus.Sent,
                _ => ElectronicDocumentStatus.Pending,
            };
            if (result.Authorization?.IsAuthorized == true)
            {
                doc.NumeroAutorizacion = result.Authorization.NumeroAutorizacion;
                doc.FechaAutorizacion = result.Authorization.FechaAutorizacion;
                doc.XmlAuthorized = result.Authorization.XmlAuthorizado;
            }
            // Authorization is durable before optional PDF/email side effects.
            await _db.SaveChangesAsync(ct);
            if (doc.Status == ElectronicDocumentStatus.Authorized)
                await CompleteAuthorizedAsync(doc, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (doc == null) throw;
            if (doc.Status != ElectronicDocumentStatus.Authorized)
            {
                if (doc.Status != ElectronicDocumentStatus.Sent)
                    doc.Status = ElectronicDocumentStatus.Pending;
                doc.ErrorMessage = $"Error tecnico; resultado pendiente de verificar: {ex.Message}";
                await _db.SaveChangesAsync(ct);
            }
            _log.LogError(ex, "Error tecnico al procesar factura {DocumentId}; no implica rechazo del SRI.", doc.Id);
        }
        finally
        {
            // Only release our own processing claim, including failures before signing/querying.
            await _db.ElectronicDocuments
                .Where(d => d.Id == req.DocumentId && d.BranchId == req.BranchId
                    && d.ProcessingStartedAt == processingStartedAt)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.ProcessingStartedAt, (DateTime?)null), CancellationToken.None);
            if (doc != null)
            {
                doc.ProcessingStartedAt = null;
                _db.Entry(doc).Property(d => d.ProcessingStartedAt).OriginalValue = null;
                _db.Entry(doc).Property(d => d.ProcessingStartedAt).IsModified = false;
            }
        }
        return GenerateElectronicInvoiceHandler.MapDoc(doc!);
    }

    private async Task SignAsync(ElectronicDocument doc, CancellationToken ct)
    {
        var config = await _db.BranchTaxConfigs
            .FirstOrDefaultAsync(c => c.BranchId == doc.BranchId && !c.IsDeleted, ct)
            ?? throw new InvalidOperationException("Configure los datos del emisor.");
        var numberPrefix = $"{config.CodigoEstablecimiento.PadLeft(3, '0')}-{config.PuntoEmision.PadLeft(3, '0')}-";
        if (config.Ambiente != doc.Environment || !doc.NumeroFactura.StartsWith(numberPrefix, StringComparison.Ordinal)
            || (doc.ClaveAcceso.Length == 49 && doc.ClaveAcceso.Substring(10, 13) != config.Ruc))
            throw new InvalidOperationException("La configuracion fiscal no coincide con el comprobante original. No se modificara su clave ni su secuencial.");

        var certificate = await _db.SriCertificates
            .FirstOrDefaultAsync(c => c.BranchId == doc.BranchId && !c.IsDeleted, ct)
            ?? throw new InvalidOperationException("Cargue el certificado .p12.");
        var payment = doc.OrderPayment ?? throw new InvalidOperationException("Pago no encontrado.");
        var order = await LoadOrderAsync(payment.OrderId, doc.BranchId, ct);
        var lines = SriInvoiceLineFactory.Build(payment, order);
        var totals = SriInvoiceLineFactory.CalculateTotals(lines, payment.OrderAmount);
        doc.TotalSinImpuestos = totals.TotalWithoutTaxes;
        doc.TotalIva = totals.TotalTax;
        doc.ImporteTotal = totals.Total;
        var protector = _dp.CreateProtector("SriCertificate");
        var emissionDate = doc.EmissionDate == default ? payment.PaidAt : doc.EmissionDate;
        var unsignedXml = SriXmlBuilder.Build(new SriInvoiceData(config, payment, order, lines,
            doc.ClaveAcceso, doc.Secuencial, payment.Customer, emissionDate));
        doc.XmlSigned = SriXmlSigner.Sign(unsignedXml,
            protector.Unprotect(certificate.CertificateEncrypted), protector.Unprotect(certificate.PasswordEncrypted));
        await _db.SaveChangesAsync(ct);
    }

    private Task<Order> LoadOrderAsync(Guid orderId, Guid branchId, CancellationToken ct) =>
        _db.Orders
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.MenuItem).ThenInclude(i => i!.TaxRate)
            .Include(o => o.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.TaxRate)
            .FirstAsync(o => o.Id == orderId && o.BranchId == branchId && !o.IsDeleted, ct);

    private async Task CompleteAuthorizedAsync(ElectronicDocument doc, CancellationToken ct)
    {
        try
        {
            var config = await _db.BranchTaxConfigs.FirstAsync(c => c.BranchId == doc.BranchId && !c.IsDeleted, ct);
            var template = await _db.InvoiceTemplates.FirstOrDefaultAsync(t => t.BranchId == doc.BranchId && !t.IsDeleted, ct);
            var payment = doc.OrderPayment!;
            var order = await LoadOrderAsync(payment.OrderId, doc.BranchId, ct);
            var lines = SriInvoiceLineFactory.Build(payment, order);
            doc.RidePdf = RideGenerator.Generate(config, doc, payment, order, lines, payment.Customer,
                GetInvoiceTemplateHandler.BuildDto(template ?? new InvoiceTemplate()));
            await _db.SaveChangesAsync(ct);
            await ElectronicInvoiceEmailDelivery.SendAsync(doc, payment, config, _email, _log, ct);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Factura {DocumentId} autorizada; no se pudo completar el RIDE o correo.", doc.Id);
        }
    }
}

public class ResendElectronicInvoiceEmailHandler : IRequestHandler<ResendElectronicInvoiceEmailCommand, ElectronicDocumentDto>
{
    private readonly GrimorioDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<ResendElectronicInvoiceEmailHandler> _log;

    public ResendElectronicInvoiceEmailHandler(
        GrimorioDbContext db,
        IEmailService email,
        ILogger<ResendElectronicInvoiceEmailHandler> log)
    {
        _db = db;
        _email = email;
        _log = log;
    }

    public async Task<ElectronicDocumentDto> Handle(ResendElectronicInvoiceEmailCommand req, CancellationToken ct)
    {
        var doc = await _db.ElectronicDocuments
            .Include(d => d.OrderPayment).ThenInclude(p => p!.Customer)
            .FirstOrDefaultAsync(d => d.Id == req.DocumentId
                && d.BranchId == req.BranchId
                && !d.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Documento no encontrado.");

        if (doc.Status != ElectronicDocumentStatus.Authorized)
            throw new InvalidOperationException("Solo se pueden enviar por correo facturas autorizadas.");
        if (doc.RidePdf == null || doc.RidePdf.Length == 0)
            throw new InvalidOperationException("El RIDE de la factura no está disponible.");
        if (doc.OrderPayment == null)
            throw new InvalidOperationException("No se encontró el cobro asociado a la factura.");

        var config = await _db.BranchTaxConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.BranchId == req.BranchId && !c.IsDeleted, ct)
            ?? throw new InvalidOperationException("Configure los datos del emisor.");

        await ElectronicInvoiceEmailDelivery.SendAsync(
            doc, doc.OrderPayment, config, _email, _log, ct);
        await _db.SaveChangesAsync(ct);

        return GenerateElectronicInvoiceHandler.MapDoc(doc);
    }
}

internal static class ElectronicInvoiceEmailDelivery
{
    internal static async Task SendAsync(
        ElectronicDocument doc,
        OrderPayment payment,
        BranchTaxConfig config,
        IEmailService email,
        ILogger logger,
        CancellationToken ct)
    {
        var recipient = payment.Customer?.Email?.Trim();
        doc.EmailRecipient = recipient;

        if (string.IsNullOrWhiteSpace(recipient))
        {
            doc.EmailStatus = ElectronicDocumentEmailStatus.Skipped;
            doc.EmailSentAt = null;
            doc.EmailErrorMessage = "El cliente no tiene un correo electrónico registrado.";
            return;
        }

        doc.EmailStatus = ElectronicDocumentEmailStatus.Pending;
        doc.EmailErrorMessage = null;
        doc.EmailRetryCount++;

        try
        {
            if (doc.RidePdf == null || doc.RidePdf.Length == 0)
                throw new InvalidOperationException("El RIDE de la factura no está disponible.");

            await email.SendInvoiceAsync(
                doc.BranchId,
                recipient,
                payment.Customer?.Name ?? "Cliente",
                doc.NumeroFactura,
                config.RazonSocial,
                doc.ImporteTotal,
                doc.RidePdf,
                doc.XmlAuthorized ?? doc.XmlSigned,
                ct);

            doc.EmailStatus = ElectronicDocumentEmailStatus.Sent;
            doc.EmailSentAt = DateTime.UtcNow;
            doc.EmailErrorMessage = null;
        }
        catch (Exception ex)
        {
            doc.EmailStatus = ElectronicDocumentEmailStatus.Failed;
            doc.EmailSentAt = null;
            doc.EmailErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            logger.LogWarning(ex,
                "No se pudo enviar por correo la factura {NumeroFactura} al destinatario {Email}",
                doc.NumeroFactura,
                recipient);
        }
    }
}

internal static class BillingMapper
{
    internal static BranchTaxConfigDto MapBranchTaxConfig(BranchTaxConfig c) => new()
    {
        Id = c.Id, Ruc = c.Ruc, RazonSocial = c.RazonSocial,
        NombreComercial = c.NombreComercial, Direccion = c.Direccion,
        CodigoEstablecimiento = c.CodigoEstablecimiento, PuntoEmision = c.PuntoEmision,
        Ambiente = c.Ambiente, ContribuyenteEspecial = c.ContribuyenteEspecial,
        ObligadoContabilidad = c.ObligadoContabilidad,
        SecuencialInicial = c.SecuencialInicial, Secuencial = c.Secuencial,
    };

    internal static TaxRateDto MapTaxRate(TaxRate t) => new()
    {
        Id = t.Id, Name = t.Name, Percentage = t.Percentage,
        SriCode = t.SriCode, IsDefault = t.IsDefault, IsActive = t.IsActive,
    };

    internal static PaymentMethodConfigDto MapPaymentMethod(PaymentMethodConfig m) => new()
    {
        Id = m.Id, Name = m.Name, Color = m.Color,
        IsCash = m.IsCash, IsCard = m.IsCard, IsActive = m.IsActive, SortOrder = m.SortOrder,
    };

    internal static CardBankDto MapCardBank(CardBank b) => new()
    {
        Id = b.Id, Name = b.Name, IsActive = b.IsActive, SortOrder = b.SortOrder,
    };

    internal static CashRegisterDto MapCashRegister(CashRegister r, bool hasOpenSession = false) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Code = r.Code,
        Description = r.Description,
        IsActive = r.IsActive,
        HasOpenSession = hasOpenSession,
    };

    internal static CustomerDto MapCustomer(Customer c) => new()
    {
        Id = c.Id, Name = c.Name, TaxId = c.TaxId,
        TaxIdType = c.TaxIdType.ToString(), Address = c.Address,
        Phone = c.Phone, Email = c.Email, IsActive = c.IsActive,
    };

    internal static CashSessionDto MapSession(
        CashSession s,
        List<OrderPayment>? payments = null,
        List<Domain.Entities.Finance.Expense>? expenses = null)
    {
        var effectivePayments = payments ?? s.Payments.Where(p => !p.IsDeleted).ToList();
        var allLines = effectivePayments.SelectMany(p => p.Lines).ToList();
        var effectiveExpenses = expenses ?? [];

        var totals = allLines
            .Where(l => l.Config != null)
            .GroupBy(l => l.Config!)
            .Select(g => new PaymentMethodTotalDto
            {
                MethodId = g.Key.Id,
                MethodName = g.Key.Name,
                MethodColor = g.Key.Color,
                IsCash = g.Key.IsCash,
                Total = g.Sum(l => l.AmountTendered - l.Change),
            })
            .OrderBy(t => t.MethodName)
            .ToList();

        var totalCash = totals.Where(t => t.IsCash).Sum(t => t.Total);
        var totalSales = totals.Sum(t => t.Total);
        var totalExpenses = effectiveExpenses.Sum(e => e.Amount);
        var totalCashExpenses = effectiveExpenses
            .Where(e => e.PaymentMethodConfig?.IsCash == true)
            .Sum(e => e.Amount);
        var expectedCash = s.OpeningBalance + totalCash - totalCashExpenses;
        var totalOrders = effectivePayments.Select(p => p.OrderId).Distinct().Count();

        return new CashSessionDto
        {
            Id = s.Id,
            CashRegisterId = s.CashRegisterId,
            CashRegisterName = s.CashRegister?.Name ?? string.Empty,
            CashRegisterCode = s.CashRegister?.Code ?? string.Empty,
            OpenedByName = s.OpenedByName,
            OpeningBalance = s.OpeningBalance, OpenedAt = s.OpenedAt,
            ClosedAt = s.ClosedAt, ClosedByName = s.ClosedByName,
            ActualCash = s.ActualCash, CloseNotes = s.CloseNotes,
            Status = s.Status.ToString(),
            Totals = totals,
            TotalSales = totalSales, TotalOrders = totalOrders,
            TotalExpenses = totalExpenses,
            TotalCashExpenses = totalCashExpenses,
            ExpectedCash = expectedCash,
            CashDifference = s.ActualCash.HasValue ? s.ActualCash.Value - expectedCash : null,
        };
    }

    internal static OrderPaymentDto MapPayment(
        OrderPayment p, int orderNumber, Customer? customer,
        string? tableCode = null, string? orderType = null,
        ElectronicDocument? elDoc = null) => new()
    {
        Id = p.Id, OrderId = p.OrderId, OrderNumber = orderNumber,
        CashSessionId = p.CashSessionId,
        CashRegisterId = p.CashSession?.CashRegisterId,
        CashRegisterName = p.CashSession?.CashRegister?.Name,
        CashRegisterCode = p.CashSession?.CashRegister?.Code,
        CashierName = p.CashSession?.OpenedByName,
        OrderType = orderType,
        CustomerId = p.CustomerId, CustomerName = customer?.Name,
        CustomerTaxId = customer?.TaxId,
        TableCode = tableCode,
        DocumentType = p.DocumentType.ToString(),
        OrderAmount = p.OrderAmount, PaidAt = p.PaidAt,
        Lines = p.Lines.Select(l => new PaymentLineDto
        {
            Id = l.Id,
            MethodId = l.PaymentMethodConfigId,
            MethodName = l.Config?.Name ?? string.Empty,
            MethodColor = l.Config?.Color ?? "#1677ff",
            IsCash = l.Config?.IsCash ?? false,
            IsCard = l.Config?.IsCard ?? false,
            AmountTendered = l.AmountTendered,
            Change = l.Change,
            CardPaymentType = l.CardPaymentType?.ToString(),
            CardBankId = l.CardBankId,
            CardBankName = l.CardBankName,
            CardBrand = l.CardBrand,
            AuthorizationNumber = l.AuthorizationNumber,
        }).ToList(),
        Items = p.Items.Select(i => new OrderPaymentItemDto
        {
            Id = i.Id,
            OrderItemId = i.OrderItemId,
            ItemName = i.ItemName ?? i.OrderItem?.MenuItem?.Name ?? string.Empty,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Total = i.Total,
        }).ToList(),
        ElectronicDocumentId = elDoc?.Id,
        ElectronicDocumentNumber = elDoc?.NumeroFactura,
        ElectronicDocumentStatus = elDoc?.Status.ToString(),
    };
}

// ── UpsertInvoiceTemplateHandler ─────────────────────────────────────────────

public class UpsertInvoiceTemplateHandler : IRequestHandler<UpsertInvoiceTemplateCommand, InvoiceTemplateDto>
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

    private readonly GrimorioDbContext _db;
    public UpsertInvoiceTemplateHandler(GrimorioDbContext db) => _db = db;

    public async Task<InvoiceTemplateDto> Handle(UpsertInvoiceTemplateCommand req, CancellationToken ct)
    {
        var template = await _db.InvoiceTemplates
            .FirstOrDefaultAsync(t => t.BranchId == req.BranchId && !t.IsDeleted, ct);

        if (template == null)
        {
            template = new InvoiceTemplate { BranchId = req.BranchId, CreatedBy = req.UserId };
            _db.InvoiceTemplates.Add(template);
        }
        else
        {
            template.UpdatedAt = DateTime.UtcNow;
            template.UpdatedBy = req.UserId;
        }

        template.LogoBase64 = req.LogoBase64;
        template.PrimaryColor = req.PrimaryColor;
        template.AccentColor = req.AccentColor;
        template.EmailSubject = req.EmailSubject;
        template.PdfBlocksJson = JsonSerializer.Serialize(req.PdfBlocks, JsonOpts);
        template.EmailBlocksJson = JsonSerializer.Serialize(req.EmailBlocks, JsonOpts);

        await _db.SaveChangesAsync(ct);
        return GetInvoiceTemplateHandler.BuildDto(template);
    }
}

// ── GenerateInvoicePreviewPdfHandler ─────────────────────────────────────────

public class GenerateInvoicePreviewPdfHandler : IRequestHandler<GenerateInvoicePreviewPdfCommand, byte[]>
{
    private readonly GrimorioDbContext _db;
    public GenerateInvoicePreviewPdfHandler(GrimorioDbContext db) => _db = db;

    public async Task<byte[]> Handle(GenerateInvoicePreviewPdfCommand req, CancellationToken ct)
    {
        var config = await _db.BranchTaxConfigs
            .FirstOrDefaultAsync(c => c.BranchId == req.BranchId && !c.IsDeleted, ct)
            ?? new BranchTaxConfig
            {
                RazonSocial = "Mi Empresa S.A.",
                NombreComercial = "Mi Negocio",
                Ruc = "1790000000001",
                Direccion = "Av. Principal 123 y Secundaria",
                CodigoEstablecimiento = "001",
                PuntoEmision = "001",
                Ambiente = "1",
                ObligadoContabilidad = true,
            };

        var dto = new InvoiceTemplateDto
        {
            LogoBase64 = req.Template.LogoBase64,
            PrimaryColor = req.Template.PrimaryColor,
            AccentColor = req.Template.AccentColor,
            PdfBlocks = req.Template.PdfBlocks,
            EmailSubject = req.Template.EmailSubject,
            EmailBlocks = req.Template.EmailBlocks,
        };

        return RideGenerator.GeneratePreview(config, dto);
    }
}
