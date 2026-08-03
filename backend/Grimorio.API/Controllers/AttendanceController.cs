using System.Security.Claims;
using Grimorio.API.Services;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Attendance.Commands;
using Grimorio.Application.Features.Attendance.Queries;
using Grimorio.Domain.Enums;
using Grimorio.Infrastructure.Features.Attendance;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public sealed class AttendanceController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AttendanceKioskAuthenticator _kioskAuthenticator;
    private readonly SFaceBiometricService _biometricService;

    public AttendanceController(IMediator mediator, AttendanceKioskAuthenticator kioskAuthenticator,
        SFaceBiometricService biometricService)
    {
        _mediator = mediator;
        _kioskAuthenticator = kioskAuthenticator;
        _biometricService = biometricService;
    }

    [Authorize(Policy = AppConstants.Permissions.RrhhAttendanceEnroll)]
    [HttpGet("admin/biometrics/status")]
    public IActionResult GetBiometricStatus()
    {
        try
        {
            _biometricService.ValidateModels();
            return Ok(new
            {
                available = true,
                modelVersion = SFaceBiometricService.ModelVersion,
                embeddingDimensions = SFaceBiometricService.EmbeddingDimensions
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { available = false, message = ex.Message });
        }
    }

    [Authorize(Policy = AppConstants.Permissions.RrhhAttendanceEnroll)]
    [HttpGet("admin/facial-enrollments")]
    public async Task<IActionResult> GetFacialEnrollments(CancellationToken cancellationToken)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetFacialEnrollmentsQuery { BranchId = branchId }, cancellationToken));
    }

    [Authorize(Policy = AppConstants.Permissions.RrhhAttendanceView)]
    [HttpGet("admin/clockings")]
    public async Task<IActionResult> GetClockings([FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] Guid? employeeId, CancellationToken cancellationToken)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            DateTime.UtcNow, OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Guayaquil"));
        return Ok(await _mediator.Send(new GetAttendanceAdminRowsQuery
        {
            BranchId = branchId,
            FromDate = from ?? today,
            ToDate = to ?? today,
            EmployeeId = employeeId
        }, cancellationToken));
    }

    [Authorize(Policy = AppConstants.Permissions.RrhhAttendanceView)]
    [HttpGet("admin/clockings/{clockingId:guid}/corrections")]
    public async Task<IActionResult> GetCorrections(Guid clockingId, CancellationToken cancellationToken)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        try
        {
            return Ok(await _mediator.Send(new GetAttendanceCorrectionsQuery
                { BranchId = branchId, EmployeeClockingId = clockingId }, cancellationToken));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [Authorize(Policy = AppConstants.Permissions.RrhhAttendanceManage)]
    [HttpPut("admin/clockings/{clockingId:guid}")]
    public async Task<IActionResult> CorrectClocking(Guid clockingId, [FromBody] CorrectAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetBranchId(out var branchId) || !TryGetUserId(out var userId)) return Unauthorized();
        var command = new CorrectAttendanceCommand
        {
            EmployeeClockingId = clockingId,
            BranchId = branchId,
            CorrectedByUserId = userId,
            ClockInTimeUtc = request.ClockInTimeUtc,
            ClockOutTimeUtc = request.ClockOutTimeUtc,
            BreakStartedAtUtc = request.BreakStartedAtUtc,
            BreakEndedAtUtc = request.BreakEndedAtUtc,
            Reason = request.Reason
        };
        try { return Ok(await _mediator.Send(command, cancellationToken)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [Authorize(Policy = AppConstants.Permissions.RrhhAttendanceEnroll)]
    [HttpPost("admin/employees/{employeeId:guid}/face")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<IActionResult> EnrollEmployeeFace(Guid employeeId, [FromForm] List<IFormFile> samples,
        CancellationToken cancellationToken)
    {
        if (!TryGetBranchId(out var branchId) || !TryGetUserId(out var userId)) return Unauthorized();
        if (samples.Count != 3) return BadRequest(new { message = "Se requieren exactamente tres muestras faciales." });
        try
        {
            return Ok(await _mediator.Send(new EnrollEmployeeFaceCommand
            {
                EmployeeId = employeeId,
                BranchId = branchId,
                EnrolledByUserId = userId,
                Samples = await ReadImages(samples, cancellationToken)
            }, cancellationToken));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (TypeInitializationException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "El servicio biométrico no está disponible. Contacta al administrador." });
        }
    }

    [Authorize(Policy = AppConstants.Permissions.RrhhAttendanceEnroll)]
    [HttpDelete("admin/employees/{employeeId:guid}/face")]
    public async Task<IActionResult> RevokeEmployeeFace(Guid employeeId, CancellationToken cancellationToken)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        try
        {
            await _mediator.Send(new RevokeEmployeeFaceCommand
                { EmployeeId = employeeId, BranchId = branchId }, cancellationToken);
            return Ok(new { message = "Biometría facial revocada correctamente." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [Authorize(Policy = AppConstants.Permissions.RrhhAttendanceView)]
    [HttpGet("admin/kiosks")]
    public async Task<IActionResult> GetKiosks(CancellationToken cancellationToken)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetAttendanceKiosksQuery { BranchId = branchId }, cancellationToken));
    }

    [Authorize(Policy = AppConstants.Permissions.RrhhAttendanceManage)]
    [HttpPost("admin/kiosks")]
    public async Task<IActionResult> RegisterKiosk([FromBody] RegisterKioskCommand command, CancellationToken cancellationToken)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        command.BranchId = branchId;
        try { return Ok(await _mediator.Send(command, cancellationToken)); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [Authorize(Policy = AppConstants.Permissions.RrhhAttendanceManage)]
    [HttpPost("admin/kiosks/{kioskId:guid}/revoke")]
    public async Task<IActionResult> RevokeKiosk(Guid kioskId, CancellationToken cancellationToken)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new RevokeKioskCommand { KioskId = kioskId, BranchId = branchId }, cancellationToken);
        return Ok(new { message = "Kiosco revocado correctamente." });
    }

    [AllowAnonymous]
    [HttpGet("kiosk/ping")]
    public async Task<IActionResult> Ping(CancellationToken cancellationToken)
    {
        var kiosk = await _kioskAuthenticator.AuthenticateAsync(Request, cancellationToken);
        if (kiosk is null) return Unauthorized(new { message = "Credenciales de kiosco inválidas." });
        return Ok(new { kioskId = kiosk.Id, kiosk.Name, kiosk.BranchId, status = kiosk.Status.ToString() });
    }

    [AllowAnonymous]
    [HttpGet("kiosk/employees/{employeeId:guid}/today")]
    public async Task<IActionResult> GetToday(Guid employeeId, CancellationToken cancellationToken)
    {
        var kiosk = await _kioskAuthenticator.AuthenticateAsync(Request, cancellationToken);
        if (kiosk is null) return Unauthorized(new { message = "Credenciales de kiosco inválidas." });
        try
        {
            return Ok(await _mediator.Send(new GetTodayAttendanceStatusQuery
            {
                EmployeeId = employeeId,
                BranchId = kiosk.BranchId
            }, cancellationToken));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [AllowAnonymous]
    [HttpPost("kiosk/identify")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Identify([FromForm] IFormFile image, CancellationToken cancellationToken)
    {
        var kiosk = await _kioskAuthenticator.AuthenticateAsync(Request, cancellationToken);
        if (kiosk is null) return Unauthorized(new { message = "Credenciales de kiosco inválidas." });
        try
        {
            var images = await ReadImages([image], cancellationToken);
            return Ok(await _mediator.Send(new IdentifyEmployeeFaceQuery
                { BranchId = kiosk.BranchId, Image = images[0] }, cancellationToken));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [AllowAnonymous]
    [HttpPost("kiosk/employees/{employeeId:guid}/clock-in")]
    public Task<IActionResult> ClockIn(Guid employeeId, [FromBody] KioskMarkRequest body, CancellationToken cancellationToken) =>
        ExecuteKioskCommand(employeeId, body, new ClockInCommand(), cancellationToken);

    [AllowAnonymous]
    [HttpPost("kiosk/employees/{employeeId:guid}/break/start")]
    public Task<IActionResult> StartBreak(Guid employeeId, [FromBody] KioskMarkRequest body, CancellationToken cancellationToken) =>
        ExecuteKioskCommand(employeeId, body, new StartBreakCommand(), cancellationToken);

    [AllowAnonymous]
    [HttpPost("kiosk/employees/{employeeId:guid}/break/end")]
    public Task<IActionResult> EndBreak(Guid employeeId, [FromBody] KioskMarkRequest body, CancellationToken cancellationToken) =>
        ExecuteKioskCommand(employeeId, body, new EndBreakCommand(), cancellationToken);

    [AllowAnonymous]
    [HttpPost("kiosk/employees/{employeeId:guid}/clock-out")]
    public Task<IActionResult> ClockOut(Guid employeeId, [FromBody] KioskMarkRequest body, CancellationToken cancellationToken) =>
        ExecuteKioskCommand(employeeId, body, new ClockOutCommand(), cancellationToken);

    private async Task<IActionResult> ExecuteKioskCommand<TCommand>(Guid employeeId, KioskMarkRequest body,
        TCommand command, CancellationToken cancellationToken) where TCommand : KioskAttendanceCommand
    {
        var kiosk = await _kioskAuthenticator.AuthenticateAsync(Request, cancellationToken);
        if (kiosk is null) return Unauthorized(new { message = "Credenciales de kiosco inválidas." });
        if (body.Method == AttendanceMethod.Manual)
            return BadRequest(new { message = "Una marcación manual requiere autorización administrativa." });

        command.EmployeeId = employeeId;
        command.KioskDeviceId = kiosk.Id;
        command.Method = body.Method;
        command.EvidencePath = body.EvidencePath;
        try { return Ok(await _mediator.Send(command, cancellationToken)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    private bool TryGetBranchId(out Guid branchId)
    {
        var value = User.FindFirstValue(AppConstants.Claims.BranchId);
        return Guid.TryParse(value, out branchId);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(AppConstants.Claims.UserId)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }

    private static async Task<List<byte[]>> ReadImages(IEnumerable<IFormFile> files,
        CancellationToken cancellationToken)
    {
        var result = new List<byte[]>();
        foreach (var file in files)
        {
            if (file.Length is <= 0 or > SFaceBiometricService.MaximumImageBytes)
                throw new ArgumentException("Cada imagen debe tener un tamaño máximo de 5 MB.");
            await using var stream = new MemoryStream(checked((int)file.Length));
            await file.CopyToAsync(stream, cancellationToken);
            result.Add(stream.ToArray());
        }
        return result;
    }
}

public sealed class KioskMarkRequest
{
    public AttendanceMethod Method { get; set; } = AttendanceMethod.Face;
    public string? EvidencePath { get; set; }
}
