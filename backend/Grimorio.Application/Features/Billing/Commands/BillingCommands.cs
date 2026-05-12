using Grimorio.Application.DTOs;
using MediatR;

namespace Grimorio.Application.Features.Billing.Commands;

// ── TaxRate ───────────────────────────────────────────────────────────────────

public class CreateTaxRateCommand : IRequest<TaxRateDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public string SriCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateTaxRateCommand : IRequest<TaxRateDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public string SriCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public class DeleteTaxRateCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

// ── BranchTaxConfig ───────────────────────────────────────────────────────────

public class UpsertBranchTaxConfigCommand : IRequest<BranchTaxConfigDto>
{
    public Guid BranchId { get; set; }
    public string Ruc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string CodigoEstablecimiento { get; set; } = "001";
    public string PuntoEmision { get; set; } = "001";
    public string Ambiente { get; set; } = "1";
}

// ── PaymentMethodConfig ───────────────────────────────────────────────────────

public class CreatePaymentMethodCommand : IRequest<PaymentMethodConfigDto>
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#1677ff";
    public bool IsCash { get; set; }
    public int SortOrder { get; set; }
}

public class UpdatePaymentMethodCommand : IRequest<PaymentMethodConfigDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#1677ff";
    public bool IsCash { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class DeletePaymentMethodCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

// ── Customers ─────────────────────────────────────────────────────────────────

public class CreateCustomerCommand : IRequest<CustomerDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string TaxIdType { get; set; } = "FinalConsumer";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class UpdateCustomerCommand : IRequest<CustomerDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string TaxIdType { get; set; } = "FinalConsumer";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}

public class DeleteCustomerCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

// ── CashSession ───────────────────────────────────────────────────────────────

public class OpenCashSessionCommand : IRequest<CashSessionDto>
{
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
}

public class CloseCashSessionCommand : IRequest<CashSessionDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal ActualCash { get; set; }
    public string? Notes { get; set; }
}

// ── Payment ───────────────────────────────────────────────────────────────────

public class PaymentLineCommand
{
    public Guid MethodId { get; set; }
    public decimal AmountTendered { get; set; }
}

public class PayOrderCommand : IRequest<OrderPaymentDto>
{
    public Guid OrderId { get; set; }
    public Guid BranchId { get; set; }
    public decimal OrderAmount { get; set; }
    public string DocumentType { get; set; } = "NotaDeVenta";
    public Guid? CustomerId { get; set; }
    public Guid? CashSessionId { get; set; }
    public List<PaymentLineCommand> Lines { get; set; } = [];
}
