using System.Security.Cryptography;
using Grimorio.API.Services;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Operations;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grimorio.API.Controllers;

/// <summary>Operational assignments, private employee evidence and daily responsibility sheets.</summary>
[ApiController, Route("api/operations/responsibilities"), Authorize]
public class ResponsibilitiesController(IMediator mediator, IWebHostEnvironment environment) : ControllerBase
{
    private Guid Branch => Guid.TryParse(User.FindFirst(AppConstants.Claims.BranchId)?.Value, out var id) && id != Guid.Empty
        ? id : throw new BadHttpRequestException("La sesion no tiene sucursal.", StatusCodes.Status401Unauthorized);
    private bool CanSupervise => User.IsInRole(AppConstants.Roles.Admin)
        || User.HasClaim(AppConstants.Claims.Permissions, AppConstants.Permissions.OperationsResponsibilitiesView);
    private string EvidenceRoot => Path.GetFullPath(Environment.GetEnvironmentVariable("RESPONSIBILITY_EVIDENCE_PATH")
        ?? Path.Combine(environment.ContentRootPath, "private", "responsibilities"));

    [HttpGet("catalog"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesView)]
    public async Task<IActionResult> Catalog(CancellationToken ct) => Ok(await mediator.Send(new GetResponsibilityCatalogQuery(Branch), ct));

    [HttpPost("areas"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesManage)]
    public Task<IActionResult> CreateArea(ResponsibilityNamedDto dto, CancellationToken ct) { dto.Id = Guid.Empty; return SaveNamed("areas", dto, ct); }
    [HttpPut("areas/{id:guid}"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesManage)]
    public Task<IActionResult> UpdateArea(Guid id, ResponsibilityNamedDto dto, CancellationToken ct) { dto.Id = id; return SaveNamed("areas", dto, ct); }
    [HttpPost("positions"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesManage)]
    public Task<IActionResult> CreatePosition(ResponsibilityNamedDto dto, CancellationToken ct) { dto.Id = Guid.Empty; return SaveNamed("positions", dto, ct); }
    [HttpPut("positions/{id:guid}"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesManage)]
    public Task<IActionResult> UpdatePosition(Guid id, ResponsibilityNamedDto dto, CancellationToken ct) { dto.Id = id; return SaveNamed("positions", dto, ct); }
    private Task<IActionResult> SaveNamed(string kind, ResponsibilityNamedDto dto, CancellationToken ct) => Run(async () => await mediator.Send(new SaveResponsibilityNamedCommand(Branch, kind, dto), ct));

    [HttpPost("tasks"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesManage)]
    public Task<IActionResult> CreateTask(ResponsibilityTaskDto dto, CancellationToken ct) { dto.Id = Guid.Empty; return SaveTask(dto, ct); }
    [HttpPut("tasks/{id:guid}"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesManage)]
    public Task<IActionResult> UpdateTask(Guid id, ResponsibilityTaskDto dto, CancellationToken ct) { dto.Id = id; return SaveTask(dto, ct); }
    private Task<IActionResult> SaveTask(ResponsibilityTaskDto dto, CancellationToken ct) => Run(async () => await mediator.Send(new SaveResponsibilityTaskCommand(Branch, dto), ct));
    [HttpPost("assignments"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesManage)]
    public Task<IActionResult> CreateAssignment(ResponsibilityAssignmentDto dto, CancellationToken ct) { dto.Id = Guid.Empty; return SaveAssignment(dto, ct); }
    [HttpPut("assignments/{id:guid}"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesManage)]
    public Task<IActionResult> UpdateAssignment(Guid id, ResponsibilityAssignmentDto dto, CancellationToken ct) { dto.Id = id; return SaveAssignment(dto, ct); }
    private Task<IActionResult> SaveAssignment(ResponsibilityAssignmentDto dto, CancellationToken ct) => Run(async () => await mediator.Send(new SaveResponsibilityAssignmentCommand(Branch, dto), ct));

    [HttpGet("day"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesView)]
    public Task<IActionResult> Day([FromQuery] DateOnly? date, CancellationToken ct) => Run(async () => await mediator.Send(new GetResponsibilityDayQuery(Branch, date, false), ct));
    [HttpGet("my-day"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesExecute)]
    public Task<IActionResult> MyDay(CancellationToken ct) => Run(async () => await mediator.Send(new GetResponsibilityDayQuery(Branch, null, true), ct));
    [HttpPost("activities/{id:guid}/complete"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesExecute)]
    public Task<IActionResult> Complete(Guid id, CompleteResponsibilityDto dto, CancellationToken ct) => Run(async () => await mediator.Send(new CompleteResponsibilityCommand(Branch, id, dto), ct));
    [HttpGet("activities/{id:guid}"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesExecute)]
    public Task<IActionResult> Activity(Guid id, CancellationToken ct) => Run(async () => await mediator.Send(new GetMyResponsibilityActivityQuery(Branch, id), ct));
    [HttpDelete("captures/{id:guid}"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesExecute)]
    public Task<IActionResult> CancelCapture(Guid id, CancellationToken ct) => Run(async () => { await mediator.Send(new CancelResponsibilityCaptureCommand(Branch, id), ct); return new { saved = true }; });
    [HttpPost("activities/{id:guid}/transfer"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesManage)]
    public Task<IActionResult> Transfer(Guid id, TransferResponsibilityDto dto, CancellationToken ct) => Run(async () => { await mediator.Send(new TransferResponsibilityCommand(Branch, id, dto), ct); return new { saved = true }; });
    [HttpPost("activities/{id:guid}/captures"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesExecute)]
    public Task<IActionResult> Capture(Guid id, [FromQuery] string phase, CancellationToken ct) => Run(async () => await mediator.Send(new CreateResponsibilityCaptureCommand(Branch, id, phase), ct));

    [HttpPut("captures/{id:guid}/photo"), Authorize(Policy = AppConstants.Permissions.OperationsResponsibilitiesExecute)]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid id, [FromForm] IFormFile photo, [FromForm] DateTime capturedAtUtc, CancellationToken ct)
    {
        if (photo.Length is <= 0 or > MenuImageProcessor.MaxUploadBytes) return BadRequest(new { message = "La foto debe tener entre 1 byte y 10 MB." });
        string? path = null;
        try
        {
            await using var input = photo.OpenReadStream();
            var image = await MenuImageProcessor.ProcessAsync(input, ct);
            var key = $"{Guid.NewGuid():N}.webp"; Directory.CreateDirectory(EvidenceRoot); path = Path.Combine(EvidenceRoot, key);
            await System.IO.File.WriteAllBytesAsync(path, image.Detail, ct);
            await mediator.Send(new StoreResponsibilityEvidenceCommand(Branch, id, capturedAtUtc, key,
                Convert.ToHexString(SHA256.HashData(image.Detail)).ToLowerInvariant(), image.Detail.Length), ct);
            path = null;
            return Ok(new { saved = true });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return BadRequest(new { message = ex.Message }); }
        finally { if (path != null && System.IO.File.Exists(path)) System.IO.File.Delete(path); }
    }

    [HttpGet("evidence/{id:guid}"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Evidence(Guid id, CancellationToken ct)
    {
        if (!CanSupervise && !User.HasClaim(AppConstants.Claims.Permissions, AppConstants.Permissions.OperationsResponsibilitiesExecute)) return Forbid();
        try
        {
            var key = await mediator.Send(new GetResponsibilityEvidenceQuery(Branch, id, CanSupervise), ct);
            if (key is null || Path.GetFileName(key) != key) return NotFound();
            var path = Path.Combine(EvidenceRoot, key);
            if (!System.IO.File.Exists(path)) return NotFound();
            Response.Headers.XContentTypeOptions = "nosniff";
            return PhysicalFile(path, "image/webp");
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    private async Task<IActionResult> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
