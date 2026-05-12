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

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetBranchTaxConfigQuery { BranchId = branchId });
        return Ok(result);
    }

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
        });
        return Ok(result);
    }

    // ── Certificado .p12 ──────────────────────────────────────────────────────

    [HttpGet("certificado/estado")]
    public async Task<IActionResult> GetCertificateStatus()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetSriCertificateStatusQuery { BranchId = branchId });
        return Ok(result);
    }

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

    [HttpDelete("certificado")]
    public async Task<IActionResult> DeleteCertificate()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteSriCertificateCommand { BranchId = branchId });
        return NoContent();
    }

    // ── Documentos electrónicos ───────────────────────────────────────────────

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

    [HttpGet("documentos/{id:guid}")]
    public async Task<IActionResult> GetDocument(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetElectronicDocumentDetailQuery { Id = id, BranchId = branchId });
        return result is null ? NotFound() : Ok(result);
    }

    // ── Prueba de conectividad con el SRI ─────────────────────────────────────

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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetBranchId(out Guid branchId)
    {
        var claim = User.FindFirst(AppConstants.Claims.BranchId)?.Value;
        return Guid.TryParse(claim, out branchId);
    }
}
