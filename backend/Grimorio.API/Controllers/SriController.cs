using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/sri")]
[Authorize]
public class SriController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpClientFactory _http;

    public SriController(IMediator mediator, IHttpClientFactory http)
    {
        _mediator = mediator;
        _http = http;
    }

    // ── Configuración del emisor ──────────────────────────────────────────────

    [Authorize(Policy = "Billing.Sri.View")]
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetBranchTaxConfigQuery { BranchId = branchId });
        return Ok(result);
    }

    [Authorize(Policy = "Billing.Sri.Manage")]
    [HttpPut("config")]
    public async Task<IActionResult> UpsertConfig([FromBody] BranchTaxConfigDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpsertBranchTaxConfigCommand
        {
            BranchId = branchId,
            Ruc = dto.Ruc,
            RazonSocial = dto.RazonSocial,
            NombreComercial = dto.NombreComercial,
            Direccion = dto.Direccion,
            CodigoEstablecimiento = dto.CodigoEstablecimiento,
            PuntoEmision = dto.PuntoEmision,
            Ambiente = dto.Ambiente,
            ContribuyenteEspecial = dto.ContribuyenteEspecial,
            ObligadoContabilidad = dto.ObligadoContabilidad,
            SecuencialInicial = dto.SecuencialInicial,
        });
        return Ok(result);
    }

    // ── Certificado .p12 ──────────────────────────────────────────────────────

    [Authorize(Policy = "Billing.Sri.View")]
    [HttpGet("certificado/estado")]
    public async Task<IActionResult> GetCertificateStatus()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetSriCertificateStatusQuery { BranchId = branchId });
        return Ok(result);
    }

    [Authorize(Policy = "Billing.Sri.Manage")]
    [HttpPost("certificado")]
    [RequestSizeLimit(5 * 1024 * 1024)]  // máximo 5 MB
    public async Task<IActionResult> UploadCertificate(
        [FromForm] IFormFile archivo, [FromForm] string contrasena)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();

        if (archivo == null || archivo.Length == 0)
            return BadRequest("Se debe adjuntar un archivo .p12.");
        if (!archivo.FileName.EndsWith(".p12", StringComparison.OrdinalIgnoreCase))
            return BadRequest("El archivo debe tener extensión .p12.");
        if (string.IsNullOrWhiteSpace(contrasena))
            return BadRequest("La contraseña del certificado es requerida.");

        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms);

        try
        {
            var result = await _mediator.Send(new UploadSriCertificateCommand
            {
                BranchId = branchId,
                FileName = archivo.FileName,
                CertificateBytes = ms.ToArray(),
                Password = contrasena,
            });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Policy = "Billing.Sri.Manage")]
    [HttpDelete("certificado")]
    public async Task<IActionResult> DeleteCertificate()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteSriCertificateCommand { BranchId = branchId });
        return NoContent();
    }

    // ── Documentos electrónicos ───────────────────────────────────────────────

    [Authorize(Policy = "Billing.Sri.View")]
    [HttpGet("documentos")]
    public async Task<IActionResult> GetDocuments(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] string? estado, [FromQuery] int pageSize = 50)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetElectronicDocumentsQuery
        {
            BranchId = branchId, FromUtc = desde, ToUtc = hasta,
            Status = estado, PageSize = pageSize,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Billing.Sri.View")]
    [HttpGet("documentos/{id:guid}")]
    public async Task<IActionResult> GetDocument(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetElectronicDocumentDetailQuery { Id = id, BranchId = branchId });
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = "Billing.Sri.Generate")]
    [HttpPost("documentos/generar/{orderPaymentId:guid}")]
    public async Task<IActionResult> GenerateInvoice(Guid orderPaymentId)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        try
        {
            var result = await _mediator.Send(new GenerateElectronicInvoiceCommand
            {
                OrderPaymentId = orderPaymentId,
                BranchId = branchId,
            });
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    [Authorize(Policy = "Billing.Sri.Generate")]
    [HttpPost("documentos/{id:guid}/reintentar")]
    public async Task<IActionResult> RetryInvoice(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        try
        {
            var result = await _mediator.Send(new RetryElectronicInvoiceCommand { DocumentId = id, BranchId = branchId });
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    [Authorize(Policy = "Billing.Sri.View")]
    [HttpGet("documentos/{id:guid}/ride")]
    public async Task<IActionResult> DownloadRide(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var doc = await _mediator.Send(new GetElectronicDocumentDetailQuery { Id = id, BranchId = branchId });
        if (doc == null) return NotFound();
        if (!doc.HasRide) return BadRequest("El RIDE aún no está disponible.");
        var rawDoc = await _mediator.Send(new GetElectronicDocumentBytesQuery { Id = id, BranchId = branchId });
        if (rawDoc?.RidePdf == null) return NotFound();
        return File(rawDoc.RidePdf, "application/pdf", $"RIDE-{doc.NumeroFactura}.pdf");
    }

    [Authorize(Policy = "Billing.Sri.View")]
    [HttpGet("documentos/{id:guid}/respuesta-sri")]
    public async Task<IActionResult> DownloadRespuestaSri(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var rawDoc = await _mediator.Send(new GetElectronicDocumentBytesQuery { Id = id, BranchId = branchId });
        if (rawDoc == null) return NotFound();
        if (string.IsNullOrEmpty(rawDoc.XmlResponseSri)) return BadRequest("No hay respuesta del SRI almacenada para este documento.");
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawDoc.XmlResponseSri);
        return File(bytes, "application/xml", $"RespuestaSRI-{rawDoc.NumeroFactura}.xml");
    }

    [Authorize(Policy = "Billing.Sri.View")]
    [HttpGet("documentos/{id:guid}/xml")]
    public async Task<IActionResult> DownloadXml(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var rawDoc = await _mediator.Send(new GetElectronicDocumentBytesQuery { Id = id, BranchId = branchId });
        if (rawDoc == null) return NotFound();
        var xml = rawDoc.XmlAuthorized ?? rawDoc.XmlSigned;
        if (xml == null) return BadRequest("XML no disponible.");
        var bytes = System.Text.Encoding.UTF8.GetBytes(xml);
        return File(bytes, "application/xml", $"FE-{rawDoc.NumeroFactura}.xml");
    }

    // ── Configuración SMTP ────────────────────────────────────────────────────

    [Authorize(Policy = "Billing.Sri.View")]
    [HttpGet("smtp")]
    public async Task<IActionResult> GetSmtpConfig()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetSmtpConfigQuery { BranchId = branchId });
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = "Billing.Sri.Manage")]
    [HttpPut("smtp")]
    public async Task<IActionResult> UpsertSmtpConfig([FromBody] UpsertSmtpConfigDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpsertSmtpConfigCommand
        {
            BranchId = branchId,
            Host = dto.Host, Port = dto.Port, Username = dto.Username,
            Password = dto.Password, FromEmail = dto.FromEmail,
            FromName = dto.FromName, EnableSsl = dto.EnableSsl, IsActive = dto.IsActive,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Billing.Sri.Manage")]
    [HttpPost("smtp/test")]
    public async Task<IActionResult> TestSmtpConnection([FromQuery] string toEmail)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(toEmail)) return BadRequest("Se requiere un correo de destino.");
        try
        {
            await _mediator.Send(new TestSmtpConnectionCommand { BranchId = branchId, ToEmail = toEmail });
            return Ok(new { success = true, message = "Correo de prueba enviado correctamente." });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // ── Prueba de conectividad con el SRI ─────────────────────────────────────

    [Authorize(Policy = "Billing.Sri.View")]
    [HttpPost("ping")]
    public async Task<IActionResult> Ping([FromQuery] string ambiente = "1")
    {
        var url = ambiente == "2"
            ? "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl"
            : "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?wsdl";

        try
        {
            using var client = _http.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var response = await client.GetAsync(url);
            return Ok(new
            {
                success = response.IsSuccessStatusCode,
                ambiente = ambiente == "2" ? "Producción" : "Pruebas",
                url,
                statusCode = (int)response.StatusCode,
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                success = false,
                ambiente = ambiente == "2" ? "Producción" : "Pruebas",
                url,
                error = ex.Message,
            });
        }
    }

    // ── Plantilla de factura ──────────────────────────────────────────────────

    [Authorize(Policy = "Billing.Sri.View")]
    [HttpGet("invoice-template")]
    public async Task<IActionResult> GetInvoiceTemplate()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetInvoiceTemplateQuery { BranchId = branchId });
        return Ok(result);
    }

    [Authorize(Policy = "Billing.Sri.Manage")]
    [HttpPut("invoice-template")]
    [RequestSizeLimit(3 * 1024 * 1024)]  // 3 MB (cubre logos en base64)
    public async Task<IActionResult> UpsertInvoiceTemplate([FromBody] UpsertInvoiceTemplateDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _mediator.Send(new UpsertInvoiceTemplateCommand
        {
            BranchId = branchId,
            UserId = userId,
            LogoBase64 = dto.LogoBase64,
            PrimaryColor = dto.PrimaryColor,
            AccentColor = dto.AccentColor,
            PdfBlocks = dto.PdfBlocks,
            EmailSubject = dto.EmailSubject,
            EmailBlocks = dto.EmailBlocks,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Billing.Sri.View")]
    [HttpPost("invoice-template/preview-pdf")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> PreviewInvoicePdf([FromBody] UpsertInvoiceTemplateDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        try
        {
            var pdfBytes = await _mediator.Send(new GenerateInvoicePreviewPdfCommand
            {
                BranchId = branchId,
                Template = dto,
            });
            return File(pdfBytes, "application/pdf", "preview-factura.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetBranchId(out Guid branchId)
    {
        var claim = User.FindFirst(AppConstants.Claims.BranchId)?.Value;
        return Guid.TryParse(claim, out branchId);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out userId);
    }
}
