using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Commands;
using Grimorio.Application.Features.Scheduling.Queries;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class SchedulingController : ControllerBase
{
    private readonly IMediator _mediator;

    public SchedulingController(IMediator mediator) => _mediator = mediator;

    // ======================== WorkArea Endpoints ========================

    [HttpGet("work-areas")]
    public async Task<IActionResult> GetWorkAreas([FromQuery] Guid branchId)
    {
        var result = await _mediator.Send(new GetWorkAreasQuery { BranchId = branchId });
        return Ok(result);
    }

    [HttpGet("work-areas/{id}")]
    public async Task<IActionResult> GetWorkAreaById(Guid id)
    {
        var result = await _mediator.Send(new GetWorkAreaByIdQuery { Id = id });
        return Ok(result);
    }

    [HttpPost("work-areas")]
    public async Task<IActionResult> CreateWorkArea([FromBody] CreateWorkAreaCommand command)
    {
        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        command.BranchId = branchId;
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetWorkAreaById), new { id = result.Id }, result);
    }

    [HttpPut("work-areas/{id}")]
    public async Task<IActionResult> UpdateWorkArea(Guid id, [FromBody] UpdateWorkAreaCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("work-areas/{id}")]
    public async Task<IActionResult> DeleteWorkArea(Guid id)
    {
        var result = await _mediator.Send(new DeleteWorkAreaCommand { Id = id });
        return Ok(result);
    }

    // ======================== WorkRole Endpoints ========================

    [HttpGet("work-roles")]
    public async Task<IActionResult> GetWorkRoles([FromQuery] Guid? workAreaId = null)
    {
        var result = await _mediator.Send(new GetWorkRolesQuery { WorkAreaId = workAreaId });
        return Ok(result);
    }

    [HttpGet("work-roles/{id}")]
    public async Task<IActionResult> GetWorkRoleById(Guid id)
    {
        var result = await _mediator.Send(new GetWorkRoleByIdQuery { Id = id });
        return Ok(result);
    }

    [HttpPost("work-roles")]
    public async Task<IActionResult> CreateWorkRole([FromBody] CreateWorkRoleCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetWorkRoleById), new { id = result.Id }, result);
    }

    [HttpPut("work-roles/{id}")]
    public async Task<IActionResult> UpdateWorkRole(Guid id, [FromBody] UpdateWorkRoleCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("work-roles/{id}")]
    public async Task<IActionResult> DeleteWorkRole(Guid id)
    {
        var result = await _mediator.Send(new DeleteWorkRoleCommand { Id = id });
        return Ok(result);
    }

    // ======================== EmployeeWorkRole Endpoints ========================

    [HttpGet("employees/{employeeId}/work-roles")]
    public async Task<IActionResult> GetEmployeeWorkRoles(Guid employeeId)
    {
        var result = await _mediator.Send(new GetEmployeeWorkRolesQuery { EmployeeId = employeeId });
        return Ok(result);
    }

    [HttpPost("employees/{employeeId}/work-roles")]
    public async Task<IActionResult> AssignWorkRolesToEmployee(Guid employeeId, [FromBody] AssignWorkRolesToEmployeeCommand command)
    {
        command.EmployeeId = employeeId;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("employees/{employeeId}/work-roles/{workRoleId}")]
    public async Task<IActionResult> RemoveWorkRoleFromEmployee(Guid employeeId, Guid workRoleId)
    {
        var result = await _mediator.Send(new RemoveWorkRoleFromEmployeeCommand { EmployeeId = employeeId, WorkRoleId = workRoleId });
        return Ok(result);
    }

    // ======================== ShiftTemplate Endpoints ========================

    [HttpGet("shift-templates")]
    public async Task<IActionResult> GetShiftTemplates([FromQuery] Guid branchId, [FromQuery] DayOfWeek? dayOfWeek = null)
    {
        var result = await _mediator.Send(new GetShiftTemplatesQuery { BranchId = branchId, DayOfWeek = dayOfWeek });
        return Ok(result);
    }

    [HttpGet("shift-templates/{id}")]
    public async Task<IActionResult> GetShiftTemplateById(Guid id)
    {
        var result = await _mediator.Send(new GetShiftTemplateByIdQuery { Id = id });
        return Ok(result);
    }

    [HttpPost("shift-templates")]
    public async Task<IActionResult> CreateShiftTemplate([FromBody] CreateShiftTemplateCommand command)
    {
        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        command.BranchId = branchId;
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetShiftTemplateById), new { id = result.Id }, result);
    }

    [HttpPut("shift-templates/{id}")]
    public async Task<IActionResult> UpdateShiftTemplate(Guid id, [FromBody] UpdateShiftTemplateCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("shift-templates/{id}")]
    public async Task<IActionResult> DeleteShiftTemplate(Guid id)
    {
        var result = await _mediator.Send(new DeleteShiftTemplateCommand { Id = id });
        return Ok(result);
    }

    // ======================== ShiftAssignment Endpoints ========================

    [HttpGet("shifts")]
    public async Task<IActionResult> GetMonthlyShifts([FromQuery] Guid branchId, [FromQuery] int year, [FromQuery] int month)
    {
        var result = await _mediator.Send(new GetMonthlyShiftsQuery { BranchId = branchId, Year = year, Month = month });
        return Ok(result);
    }

    [HttpGet("shifts/employee/{employeeId}")]
    public async Task<IActionResult> GetEmployeeMonthlyShifts(Guid employeeId, [FromQuery] int year, [FromQuery] int month)
    {
        var result = await _mediator.Send(new GetEmployeeMonthlyShiftsQuery { EmployeeId = employeeId, Year = year, Month = month });
        return Ok(result);
    }

    [HttpGet("shifts/{id}")]
    public async Task<IActionResult> GetShiftAssignmentById(Guid id)
    {
        var result = await _mediator.Send(new GetShiftAssignmentByIdQuery { Id = id });
        return Ok(result);
    }

    [HttpGet("shifts/date/{date}")]
    public async Task<IActionResult> GetShiftAssignmentsByDate(DateTime date, [FromQuery] Guid branchId)
    {
        var result = await _mediator.Send(new GetShiftAssignmentsByDateQuery { BranchId = branchId, Date = date.Date });
        return Ok(result);
    }

    [HttpPost("shifts")]
    public async Task<IActionResult> CreateShiftAssignment([FromBody] CreateShiftAssignmentCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetShiftAssignmentById), new { id = result.Id }, result);
    }

    [HttpDelete("shifts/{id}")]
    public async Task<IActionResult> DeleteShiftAssignment(Guid id)
    {
        var result = await _mediator.Send(new DeleteShiftAssignmentCommand { Id = id });
        return Ok(result);
    }

    // ======================== EmployeeAvailability Endpoints ========================

    [HttpGet("employees/{employeeId}/availability")]
    public async Task<IActionResult> GetEmployeeAvailability(Guid employeeId, [FromQuery] int? month = null, [FromQuery] int? year = null)
    {
        var result = await _mediator.Send(new GetEmployeeAvailabilityQuery { EmployeeId = employeeId, Month = month, Year = year });
        return Ok(result);
    }

    [HttpPost("employees/{employeeId}/availability")]
    public async Task<IActionResult> AddEmployeeAvailability(Guid employeeId, [FromBody] AddEmployeeAvailabilityCommand command)
    {
        command.EmployeeId = employeeId;
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetEmployeeAvailability), new { employeeId = result.EmployeeId }, result);
    }

    [HttpDelete("availability/{id}")]
    public async Task<IActionResult> RemoveEmployeeAvailability(Guid id)
    {
        var result = await _mediator.Send(new RemoveEmployeeAvailabilityCommand { Id = id });
        return Ok(result);
    }

    // ======================== Generate Shifts ========================

    [HttpPost("shifts/generate")]
    public async Task<IActionResult> GenerateMonthlyShifts([FromBody] GenerateMonthlyShiftsCommand command)
    {
        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        command.BranchId = branchId;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    // ======================== Schedule Configuration Endpoints ========================

    [HttpGet("configuration")]
    public async Task<IActionResult> GetScheduleConfiguration([FromQuery] Guid branchId)
    {
        var result = await _mediator.Send(new GetScheduleConfigurationQuery { BranchId = branchId });
        return Ok(result);
    }

    [HttpPost("configuration")]
    public async Task<IActionResult> CreateScheduleConfiguration([FromBody] CreateScheduleConfigurationCommand command)
    {
        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        command.BranchId = branchId;
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetScheduleConfiguration), new { branchId = result.BranchId }, result);
    }

    [HttpPut("configuration/{id}")]
    public async Task<IActionResult> UpdateScheduleConfiguration(Guid id, [FromBody] UpdateScheduleConfigurationCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}

