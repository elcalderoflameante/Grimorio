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
    public string? ContribuyenteEspecial { get; set; }
    public bool ObligadoContabilidad { get; set; }
    public long SecuencialInicial { get; set; } = 1;
}

// ── Factura Electrónica ───────────────────────────────────────────────────────

public class GenerateElectronicInvoiceCommand : IRequest<ElectronicDocumentDto>
{
    public Guid OrderPaymentId { get; set; }
    public Guid BranchId { get; set; }
}

public class RetryElectronicInvoiceCommand : IRequest<ElectronicDocumentDto>
{
    public Guid DocumentId { get; set; }
    public Guid BranchId { get; set; }
}

// ── SRI Certificado ───────────────────────────────────────────────────────────

public class UploadSriCertificateCommand : IRequest<SriCertificateStatusDto>
{
    public Guid BranchId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public byte[] CertificateBytes { get; set; } = [];
    public string Password { get; set; } = string.Empty;
}

public class DeleteSriCertificateCommand : IRequest<bool>
{
    public Guid BranchId { get; set; }
}

// ── SmtpConfig ────────────────────────────────────────────────────────────────

public class UpsertSmtpConfigCommand : IRequest<SmtpConfigDto>
{
    public Guid BranchId { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class TestSmtpConnectionCommand : IRequest<bool>
{
    public Guid BranchId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
}

// ── PaymentMethodConfig ───────────────────────────────────────────────────────

public class CreatePaymentMethodCommand : IRequest<PaymentMethodConfigDto>
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#1677ff";
    public bool IsCash { get; set; }
    public bool IsCard { get; set; }
    public int SortOrder { get; set; }
}

public class UpdatePaymentMethodCommand : IRequest<PaymentMethodConfigDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#1677ff";
    public bool IsCash { get; set; }
    public bool IsCard { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class DeletePaymentMethodCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class CreateCardBankCommand : IRequest<CardBankDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class UpdateCardBankCommand : IRequest<CardBankDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class DeleteCardBankCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
}

public class CreateCashRegisterCommand : IRequest<CashRegisterDto>
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateCashRegisterCommand : IRequest<CashRegisterDto>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class DeleteCashRegisterCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
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
    public Guid CashRegisterId { get; set; }
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
    public string? CardPaymentType { get; set; }
    public Guid? CardBankId { get; set; }
    public string? CardBrand { get; set; }
    public string? AuthorizationNumber { get; set; }
}

public class PayOrderCommand : IRequest<OrderPaymentDto>
{
    public Guid OrderId { get; set; }
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }
    public decimal OrderAmount { get; set; }
    public string DocumentType { get; set; } = "NotaDeVenta";
    public Guid? CustomerId { get; set; }
    public Guid? CashSessionId { get; set; }
    public List<PaymentLineCommand> Lines { get; set; } = [];
    public List<PaymentItemCommand> Items { get; set; } = [];
}

public class PaymentItemCommand
{
    public Guid OrderItemId { get; set; }
    public decimal Quantity { get; set; }
}

// ── InvoiceTemplate ───────────────────────────────────────────────────────────

public class UpsertInvoiceTemplateCommand : IRequest<InvoiceTemplateDto>
{
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }
    public string? LogoBase64 { get; set; }
    public string PrimaryColor { get; set; } = "#1677ff";
    public string AccentColor { get; set; } = "#e6f4ff";
    public List<PdfBlockDto> PdfBlocks { get; set; } = [];
    public string EmailSubject { get; set; } = "Factura Electrónica {numeroFactura} — {razonSocial}";
    public List<EmailBlockDto> EmailBlocks { get; set; } = [];
}

public class GenerateInvoicePreviewPdfCommand : IRequest<byte[]>
{
    public Guid BranchId { get; set; }
    public UpsertInvoiceTemplateDto Template { get; set; } = new();
}
