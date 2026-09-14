using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Purchases.Commands;
using Grimorio.Application.Features.Purchases.Queries;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Xml.Linq;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/purchases")]
[Authorize]
public class PurchasesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PurchasesController(IMediator mediator) => _mediator = mediator;

    // ── Suppliers ──────────────────────────────────────────────────────────────

    [Authorize(Policy = "Purchases.Suppliers.View")]
    [HttpGet("proveedores")]
    public async Task<IActionResult> GetSuppliers([FromQuery] bool? activeOnly, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new GetSuppliersQuery { BranchId = branchId, ActiveOnly = activeOnly }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Purchases.Suppliers.Manage")]
    [HttpPost("proveedores")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDto dto, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new CreateSupplierCommand
        {
            BranchId = branchId, Name = dto.Name, TaxId = dto.TaxId,
            Phone = dto.Phone, Email = dto.Email, Address = dto.Address,
            ContactName = dto.ContactName,
        }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Purchases.Suppliers.Manage")]
    [HttpPut("proveedores/{id:guid}")]
    public async Task<IActionResult> UpdateSupplier(Guid id, [FromBody] UpdateSupplierDto dto, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new UpdateSupplierCommand
        {
            Id = id, BranchId = branchId, Name = dto.Name, TaxId = dto.TaxId,
            Phone = dto.Phone, Email = dto.Email, Address = dto.Address,
            ContactName = dto.ContactName, IsActive = dto.IsActive,
        }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Purchases.Suppliers.Manage")]
    [HttpDelete("proveedores/{id:guid}")]
    public async Task<IActionResult> DeleteSupplier(Guid id, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        await _mediator.Send(new DeleteSupplierCommand { Id = id, BranchId = branchId }, ct);
        return NoContent();
    }

    // ── Compras directas ───────────────────────────────────────────────────────

    [Authorize(Policy = "Purchases.Orders.View")]
    [HttpGet("compras")]
    public async Task<IActionResult> GetPurchases(
        [FromQuery] string? status, [FromQuery] Guid? supplierId,
        [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo,
        CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new GetPurchasesQuery
        {
            BranchId = branchId, Status = status, SupplierId = supplierId,
            DateFrom = dateFrom, DateTo = dateTo,
        }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Purchases.Orders.View")]
    [HttpGet("compras/{id:guid}")]
    public async Task<IActionResult> GetPurchase(Guid id, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new GetPurchaseDetailQuery { Id = id, BranchId = branchId }, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = "Purchases.Orders.Create")]
    [HttpPost("compras")]
    public async Task<IActionResult> CreatePurchase([FromBody] CreatePurchaseDto dto, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new CreatePurchaseCommand
        {
            BranchId = branchId,
            DocumentType = dto.DocumentType,
            DocumentNumber = dto.DocumentNumber,
            DocumentDate = dto.DocumentDate,
            AccessKey = dto.AccessKey,
            AuthorizationNumber = dto.AuthorizationNumber,
            AuthorizationDate = dto.AuthorizationDate,
            Environment = dto.Environment,
            EmissionType = dto.EmissionType,
            SupplierCommercialName = dto.SupplierCommercialName,
            SupplierMatrixAddress = dto.SupplierMatrixAddress,
            SupplierBranchAddress = dto.SupplierBranchAddress,
            SupplierSpecialTaxpayerNumber = dto.SupplierSpecialTaxpayerNumber,
            SupplierObligatedAccounting = dto.SupplierObligatedAccounting,
            PaymentMethodSriCode = dto.PaymentMethodSriCode,
            PaymentMethodName = dto.PaymentMethodName,
            PaymentAmount = dto.PaymentAmount,
            Ice = dto.Ice,
            Irbpnr = dto.Irbpnr,
            Tip = dto.Tip,
            XmlFileUrl = dto.XmlFileUrl,
            PdfFileUrl = dto.PdfFileUrl,
            SupplierId = dto.SupplierId,
            Notes = dto.Notes,
            DestinationWarehouseId = dto.DestinationWarehouseId,
            Items = dto.Items,
        }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Purchases.Orders.Update")]
    [HttpPut("compras/{id:guid}")]
    public async Task<IActionResult> UpdatePurchase(Guid id, [FromBody] UpdatePurchaseDto dto, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new UpdatePurchaseCommand
        {
            Id = id, BranchId = branchId,
            DocumentType = dto.DocumentType,
            DocumentNumber = dto.DocumentNumber,
            DocumentDate = dto.DocumentDate,
            AccessKey = dto.AccessKey,
            AuthorizationNumber = dto.AuthorizationNumber,
            AuthorizationDate = dto.AuthorizationDate,
            Environment = dto.Environment,
            EmissionType = dto.EmissionType,
            SupplierCommercialName = dto.SupplierCommercialName,
            SupplierMatrixAddress = dto.SupplierMatrixAddress,
            SupplierBranchAddress = dto.SupplierBranchAddress,
            SupplierSpecialTaxpayerNumber = dto.SupplierSpecialTaxpayerNumber,
            SupplierObligatedAccounting = dto.SupplierObligatedAccounting,
            PaymentMethodSriCode = dto.PaymentMethodSriCode,
            PaymentMethodName = dto.PaymentMethodName,
            PaymentAmount = dto.PaymentAmount,
            Ice = dto.Ice,
            Irbpnr = dto.Irbpnr,
            Tip = dto.Tip,
            XmlFileUrl = dto.XmlFileUrl,
            PdfFileUrl = dto.PdfFileUrl,
            SupplierId = dto.SupplierId,
            Notes = dto.Notes,
            DestinationWarehouseId = dto.DestinationWarehouseId,
            Items = dto.Items,
        }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Purchases.Orders.Cancel")]
    [HttpPost("compras/{id:guid}/anular")]
    public async Task<IActionResult> AnularPurchase(Guid id, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new AnularPurchaseCommand { Id = id, BranchId = branchId }, ct);
        return Ok(result);
    }

    [Authorize(Policy = "Purchases.Orders.Delete")]
    [HttpDelete("compras/{id:guid}")]
    public async Task<IActionResult> DeletePurchase(Guid id, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        await _mediator.Send(new DeletePurchaseCommand { Id = id, BranchId = branchId }, ct);
        return NoContent();
    }

    [Authorize(Policy = "Purchases.Orders.Create")]
    [HttpPost("compras/importar-xml")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportPurchaseXml([FromForm] IFormFile file, CancellationToken ct)
    {
        if (!TryGetBranchId(out _)) return Unauthorized("BranchId no valido en el token.");
        if (file.Length == 0) return BadRequest("Archivo XML vacio.");

        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".xml", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Solo se permiten archivos XML.");

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var xml = await reader.ReadToEndAsync(ct);
        var preview = PurchaseInvoiceXmlParser.Parse(xml);
        preview.XmlFileUrl = await SavePurchaseDocumentAsync(file.FileName, "xml", xml, ct);
        return Ok(preview);
    }

    [Authorize(Policy = "Purchases.Orders.Create")]
    [HttpPost("compras/adjuntos")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPurchaseAttachment([FromForm] IFormFile file, CancellationToken ct)
    {
        if (!TryGetBranchId(out _)) return Unauthorized("BranchId no valido en el token.");
        if (file.Length == 0) return BadRequest("Archivo vacio.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".pdf" and not ".xml")
            return BadRequest("Solo se permiten archivos PDF o XML.");

        var fileUrl = await SavePurchaseDocumentAsync(file.FileName, extension.TrimStart('.'), file, ct);
        return Ok(new PurchaseAttachmentDto
        {
            FileUrl = fileUrl,
            FileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
        });
    }

    private bool TryGetBranchId(out Guid branchId)
    {
        var claim = User.FindFirst(AppConstants.Claims.BranchId)?.Value;
        return Guid.TryParse(claim, out branchId) && branchId != Guid.Empty;
    }

    private async Task<string> SavePurchaseDocumentAsync(string originalFileName, string type, IFormFile file, CancellationToken ct)
    {
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var relativeFolder = Path.Combine("uploads", "purchase-documents", DateTime.UtcNow.ToString("yyyyMMdd"));
        var folder = Path.Combine(webRoot, relativeFolder);
        Directory.CreateDirectory(folder);

        var safeName = BuildSafeFileName(originalFileName, type);
        var path = Path.Combine(folder, safeName);
        await using var fs = System.IO.File.Create(path);
        await file.CopyToAsync(fs, ct);
        return "/" + Path.Combine(relativeFolder, safeName).Replace('\\', '/');
    }

    private async Task<string> SavePurchaseDocumentAsync(string originalFileName, string type, string content, CancellationToken ct)
    {
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var relativeFolder = Path.Combine("uploads", "purchase-documents", DateTime.UtcNow.ToString("yyyyMMdd"));
        var folder = Path.Combine(webRoot, relativeFolder);
        Directory.CreateDirectory(folder);

        var safeName = BuildSafeFileName(originalFileName, type);
        var path = Path.Combine(folder, safeName);
        await System.IO.File.WriteAllTextAsync(path, content, ct);
        return "/" + Path.Combine(relativeFolder, safeName).Replace('\\', '/');
    }

    private static string BuildSafeFileName(string originalFileName, string extension)
    {
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        var safeBase = new string(baseName.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-').ToArray());
        safeBase = string.IsNullOrWhiteSpace(safeBase) ? "documento" : safeBase[..Math.Min(safeBase.Length, 80)];
        return $"{Guid.NewGuid():N}-{safeBase}.{extension}";
    }
}

internal static class PurchaseInvoiceXmlParser
{
    public static PurchaseInvoiceImportDto Parse(string xml)
    {
        var outer = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var authorization = outer.Descendants("autorizacion").FirstOrDefault();
        var authorizationNumber = authorization?.Element("numeroAutorizacion")?.Value.Trim();
        var authorizationDate = ParseDate(authorization?.Element("fechaAutorizacion")?.Value);
        var voucherXml = authorization?.Element("comprobante")?.Value;

        var doc = string.IsNullOrWhiteSpace(voucherXml)
            ? outer
            : XDocument.Parse(voucherXml, LoadOptions.PreserveWhitespace);

        var infoTrib = doc.Descendants("infoTributaria").FirstOrDefault()
            ?? throw new InvalidOperationException("El XML no contiene infoTributaria.");
        var infoFactura = doc.Descendants("infoFactura").FirstOrDefault()
            ?? throw new InvalidOperationException("El XML no contiene infoFactura.");

        var establishment = Value(infoTrib, "estab");
        var emissionPoint = Value(infoTrib, "ptoEmi");
        var sequential = Value(infoTrib, "secuencial");

        var result = new PurchaseInvoiceImportDto
        {
            DocumentType = 1,
            DocumentNumber = string.Join("-", new[] { establishment, emissionPoint, sequential }.Where(x => !string.IsNullOrWhiteSpace(x))),
            DocumentDate = ParseDate(Value(infoFactura, "fechaEmision")),
            AccessKey = Value(infoTrib, "claveAcceso"),
            AuthorizationNumber = authorizationNumber,
            AuthorizationDate = authorizationDate,
            Environment = MapEnvironment(Value(infoTrib, "ambiente")),
            EmissionType = MapEmissionType(Value(infoTrib, "tipoEmision")),
            SupplierTaxId = Value(infoTrib, "ruc"),
            SupplierName = Value(infoTrib, "razonSocial"),
            SupplierCommercialName = Value(infoTrib, "nombreComercial"),
            SupplierMatrixAddress = Value(infoTrib, "dirMatriz"),
            SupplierBranchAddress = Value(infoFactura, "dirEstablecimiento"),
            SupplierSpecialTaxpayerNumber = Value(infoFactura, "contribuyenteEspecial"),
            SupplierObligatedAccounting = ParseBool(Value(infoFactura, "obligadoContabilidad")),
            PaymentMethodSriCode = infoFactura.Descendants("pago").Select(x => Value(x, "formaPago")).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
            PaymentAmount = infoFactura.Descendants("pago").Select(x => ParseDecimal(Value(x, "total"))).FirstOrDefault(x => x > 0),
            Subtotal = ParseDecimal(Value(infoFactura, "totalSinImpuestos")),
            DiscountTotal = ParseDecimal(Value(infoFactura, "totalDescuento")),
            Tip = ParseDecimal(Value(infoFactura, "propina")),
            Total = ParseDecimal(Value(infoFactura, "importeTotal")),
        };

        result.PaymentMethodName = MapPaymentMethod(result.PaymentMethodSriCode);

        foreach (var tax in infoFactura.Descendants("totalImpuesto"))
        {
            var code = Value(tax, "codigo");
            var percentageCode = Value(tax, "codigoPorcentaje");
            var taxableBase = ParseDecimal(Value(tax, "baseImponible"));
            var amount = ParseDecimal(Value(tax, "valor"));

            if (code == "2")
            {
                if (amount > 0) { result.TaxableBase15 += taxableBase; result.Iva15 += amount; }
                else if (percentageCode == "0") result.TaxableBase0 += taxableBase;
                else if (percentageCode == "6" || percentageCode == "7") result.TaxableBaseExempt += taxableBase;
                else if (percentageCode == "5") result.TaxableBaseNotSubject += taxableBase;
            }
            else if (code == "3") result.Ice += amount;
            else if (code == "5") result.Irbpnr += amount;
        }

        result.Items = doc.Descendants("detalle").Select(detail =>
        {
            var tax = detail.Descendants("impuesto").FirstOrDefault();
            return new PurchaseInvoiceImportItemDto
            {
                SupplierMainCode = Value(detail, "codigoPrincipal"),
                SupplierAuxCode = Value(detail, "codigoAuxiliar"),
                SupplierDescription = Value(detail, "descripcion") ?? string.Empty,
                AdditionalDetail = string.Join(" | ", detail.Descendants("detAdicional")
                    .Select(x => $"{x.Attribute("nombre")?.Value}: {x.Attribute("valor")?.Value}")
                    .Where(x => !string.IsNullOrWhiteSpace(x))),
                Quantity = ParseDecimal(Value(detail, "cantidad")),
                UnitPrice = ParseDecimal(Value(detail, "precioUnitario")),
                DiscountAmount = ParseDecimal(Value(detail, "descuento")),
                TaxPercentage = ParseDecimal(Value(tax, "tarifa")),
                TaxAmount = ParseDecimal(Value(tax, "valor")),
                TotalPrice = ParseDecimal(Value(detail, "precioTotalSinImpuesto")) + ParseDecimal(Value(tax, "valor")),
            };
        }).ToList();

        return result;
    }

    private static string? Value(XElement? parent, string name) =>
        parent?.Elements().FirstOrDefault(x => x.Name.LocalName == name)?.Value.Trim();

    private static decimal ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0m;

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string[] formats = ["dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy H:mm:ss", "yyyy-MM-ddTHH:mm:ssK"];
        return DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var result)
            || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result)
            ? result
            : null;
    }

    private static bool? ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().Equals("SI", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("SÍ", StringComparison.OrdinalIgnoreCase);
    }

    private static string? MapEnvironment(string? value) => value switch
    {
        "1" => "PRUEBAS",
        "2" => "PRODUCCION",
        _ => value,
    };

    private static string? MapEmissionType(string? value) => value switch
    {
        "1" => "NORMAL",
        _ => value,
    };

    private static string? MapPaymentMethod(string? code) => code switch
    {
        "01" => "SIN UTILIZACION DEL SISTEMA FINANCIERO",
        "15" => "COMPENSACION DE DEUDAS",
        "16" => "TARJETA DE DEBITO",
        "17" => "DINERO ELECTRONICO",
        "18" => "TARJETA PREPAGO",
        "19" => "TARJETA DE CREDITO",
        "20" => "OTROS CON UTILIZACION DEL SISTEMA FINANCIERO",
        "21" => "ENDOSO DE TITULOS",
        _ => null,
    };
}
