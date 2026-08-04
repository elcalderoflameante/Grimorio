using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Branches.Commands;
using Grimorio.Application.Features.Branches.Queries;

namespace Grimorio.API.Controllers;

/// <summary>
/// Controlador para gestionar la sucursal actual.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Inicializa una nueva instancia del controlador de sucursales.
    /// </summary>
    /// <param name="mediator">Instancia de MediatR para enviar comandos.</param>
    public BranchesController(IMediator mediator, IWebHostEnvironment environment)
    {
        _mediator = mediator;
        _environment = environment;
    }

    /// <summary>
    /// Obtiene los datos de la sucursal actual del usuario.
    /// </summary>
    /// <returns>Datos de la sucursal.</returns>
    /// <response code="200">Sucursal encontrada.</response>
    /// <response code="401">BranchId no válido en el token.</response>
    /// <response code="404">Sucursal no encontrada.</response>
    [Authorize(Policy = "Admin.Branch.View")]
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentBranch()
    {
        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var result = await _mediator.Send(new GetCurrentBranchQuery { BranchId = branchId });
        if (result == null)
            return NotFound(new { message = "Sucursal no encontrada." });

        return Ok(result);
    }

    /// <summary>
    /// Actualiza los datos de la sucursal actual.
    /// </summary>
    /// <param name="dto">Datos de la sucursal a actualizar.</param>
    /// <returns>Datos actualizados de la sucursal.</returns>
    /// <response code="200">Sucursal actualizada correctamente.</response>
    /// <response code="400">Solicitud inválida.</response>
    /// <response code="401">BranchId no válido en el token.</response>
    /// <response code="404">Sucursal no encontrada.</response>
    /// <response code="500">Error interno del servidor.</response>
    [Authorize(Policy = "Admin.Branch.Update")]
    [HttpPut("current")]
    public async Task<IActionResult> UpdateCurrentBranch([FromBody] UpdateBranchDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        try
        {
            var command = new UpdateBranchCommand
            {
                BranchId = branchId,
                Name = dto.Name,
                Code = dto.Code,
                IdentificationNumber = dto.IdentificationNumber,
                Address = dto.Address,
                Phone = dto.Phone,
                Email = dto.Email,
                TimeZoneId = dto.TimeZoneId,
                LogoUrl = dto.LogoUrl,
                IsActive = dto.IsActive,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar la sucursal.", error = ex.Message });
        }
    }

    [Authorize(Policy = "Admin.Branch.Update")]
    [HttpPost("current/logo")]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> UploadCurrentBranchLogo([FromForm] IFormFile? image, CancellationToken ct)
    {
        if (image == null || image.Length == 0) return BadRequest("Imagen no válida.");
        if (image.Length > 3 * 1024 * 1024) return BadRequest("La imagen no puede superar 3 MB.");

        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no vÃ¡lido en el token.");

        var oldBranch = await _mediator.Send(new GetCurrentBranchQuery { BranchId = branchId }, ct);
        if (oldBranch == null)
            return NotFound(new { message = "Sucursal no encontrada." });

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowedExtensions.Contains(extension)) return BadRequest("Formato no permitido. Usa JPG, PNG o WEBP.");

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var relativeFolder = Path.Combine("uploads", "branches", branchId.ToString("N"));
        var targetFolder = Path.Combine(webRoot, relativeFolder);
        Directory.CreateDirectory(targetFolder);

        var fileName = $"logo-{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(targetFolder, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await image.CopyToAsync(stream, ct);
        }

        var logoUrl = "/" + Path.Combine(relativeFolder, fileName).Replace('\\', '/');
        var result = await _mediator.Send(new UpdateBranchCommand
        {
            BranchId = branchId,
            Name = oldBranch.Name,
            Code = oldBranch.Code,
            IdentificationNumber = oldBranch.IdentificationNumber,
            Address = oldBranch.Address,
            Phone = oldBranch.Phone,
            Email = oldBranch.Email,
            TimeZoneId = oldBranch.TimeZoneId,
            LogoUrl = logoUrl,
            IsActive = oldBranch.IsActive,
            Latitude = oldBranch.Latitude,
            Longitude = oldBranch.Longitude
        }, ct);

        DeleteLocalLogo(oldBranch.LogoUrl);
        return Ok(result);
    }

    [Authorize(Policy = "Admin.Branch.Update")]
    [HttpDelete("current/logo")]
    public async Task<IActionResult> DeleteCurrentBranchLogo(CancellationToken ct)
    {
        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no vÃ¡lido en el token.");

        var oldBranch = await _mediator.Send(new GetCurrentBranchQuery { BranchId = branchId }, ct);
        if (oldBranch == null)
            return NotFound(new { message = "Sucursal no encontrada." });

        var result = await _mediator.Send(new UpdateBranchCommand
        {
            BranchId = branchId,
            Name = oldBranch.Name,
            Code = oldBranch.Code,
            IdentificationNumber = oldBranch.IdentificationNumber,
            Address = oldBranch.Address,
            Phone = oldBranch.Phone,
            Email = oldBranch.Email,
            TimeZoneId = oldBranch.TimeZoneId,
            LogoUrl = null,
            IsActive = oldBranch.IsActive,
            Latitude = oldBranch.Latitude,
            Longitude = oldBranch.Longitude
        }, ct);

        DeleteLocalLogo(oldBranch.LogoUrl);
        return Ok(result);
    }

    private void DeleteLocalLogo(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl) || !logoUrl.StartsWith("/uploads/branches/", StringComparison.OrdinalIgnoreCase))
            return;

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var relativePath = logoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath));
        var uploadsRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads", "branches"));

        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase)) return;
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
    }
}
