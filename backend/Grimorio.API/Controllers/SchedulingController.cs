using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Scheduling.Commands;
using Grimorio.Application.Features.Scheduling.Queries;

namespace Grimorio.API.Controllers;

/// <summary>
/// Controlador para gestionar programación de horarios.
/// Incluye áreas, roles, plantillas, turnos y disponibilidad.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class SchedulingController : ControllerBase
{
    private readonly IMediator _mediator;

    public SchedulingController(IMediator mediator) => _mediator = mediator;

    // ======================== WorkArea Endpoints ========================

    /// <summary>
    /// Obtiene todas las áreas de trabajo por sucursal.
    /// </summary>
    [HttpGet("work-areas")]
    public async Task<IActionResult> GetWorkAreas([FromQuery] Guid branchId)
    {
        var result = await _mediator.Send(new GetWorkAreasQuery { BranchId = branchId });
        return Ok(result);
    }

    /// <summary>
    /// Obtiene un área de trabajo por ID.
    /// </summary>
    [HttpGet("work-areas/{id}")]
    public async Task<IActionResult> GetWorkAreaById(Guid id)
    {
        var result = await _mediator.Send(new GetWorkAreaByIdQuery { Id = id });
        if (result == null)
            return NotFound(new { message = "Área de trabajo no encontrada." });
        return Ok(result);
    }

    /// <summary>
    /// Crea una nueva área de trabajo.
    /// </summary>
    [HttpPost("work-areas")]
    public async Task<IActionResult> CreateWorkArea([FromBody] CreateWorkAreaCommand command)
    {
        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        command.BranchId = branchId;
        try
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetWorkAreaById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear área de trabajo.", error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un área de trabajo existente.
    /// </summary>
    [HttpPut("work-areas/{id}")]
    public async Task<IActionResult> UpdateWorkArea(Guid id, [FromBody] UpdateWorkAreaCommand command)
    {
        command.Id = id;
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar área de trabajo.", error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un área de trabajo.
    /// </summary>
    [HttpDelete("work-areas/{id}")]
    public async Task<IActionResult> DeleteWorkArea(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteWorkAreaCommand { Id = id });
            return Ok(new { message = "Área de trabajo eliminada correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar área de trabajo.", error = ex.Message });
        }
    }

    // ======================== WorkRole Endpoints ========================

    /// <summary>
    /// Obtiene todos los roles de trabajo, opcionalmente filtrados por área.
    /// </summary>
    [HttpGet("work-roles")]
    public async Task<IActionResult> GetWorkRoles([FromQuery] Guid? workAreaId = null)
    {
        var result = await _mediator.Send(new GetWorkRolesQuery { WorkAreaId = workAreaId });
        return Ok(result);
    }

    /// <summary>
    /// Obtiene un rol de trabajo por ID.
    /// </summary>
    [HttpGet("work-roles/{id}")]
    public async Task<IActionResult> GetWorkRoleById(Guid id)
    {
        var result = await _mediator.Send(new GetWorkRoleByIdQuery { Id = id });
        if (result == null)
            return NotFound(new { message = "Rol de trabajo no encontrado." });
        return Ok(result);
    }

    /// <summary>
    /// Crea un nuevo rol de trabajo.
    /// </summary>
    [HttpPost("work-roles")]
    public async Task<IActionResult> CreateWorkRole([FromBody] CreateWorkRoleCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetWorkRoleById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear rol de trabajo.", error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un rol de trabajo existente.
    /// </summary>
    [HttpPut("work-roles/{id}")]
    public async Task<IActionResult> UpdateWorkRole(Guid id, [FromBody] UpdateWorkRoleCommand command)
    {
        command.Id = id;
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar rol de trabajo.", error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un rol de trabajo.
    /// </summary>
    [HttpDelete("work-roles/{id}")]
    public async Task<IActionResult> DeleteWorkRole(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteWorkRoleCommand { Id = id });
            return Ok(new { message = "Rol de trabajo eliminado correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar rol de trabajo.", error = ex.Message });
        }
    }

    // ======================== EmployeeWorkRole Endpoints ========================

    /// <summary>
    /// Obtiene los roles asignados a un empleado.
    /// </summary>
    [HttpGet("employees/{employeeId}/work-roles")]
    public async Task<IActionResult> GetEmployeeWorkRoles(Guid employeeId)
    {
        var result = await _mediator.Send(new GetEmployeeWorkRolesQuery { EmployeeId = employeeId });
        return Ok(result);
    }

    /// <summary>
    /// Asigna roles de trabajo a un empleado.
    /// </summary>
    [HttpPost("employees/{employeeId}/work-roles")]
    public async Task<IActionResult> AssignWorkRolesToEmployee(Guid employeeId, [FromBody] AssignWorkRolesToEmployeeCommand command)
    {
        command.EmployeeId = employeeId;
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al asignar roles de trabajo.", error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un rol de trabajo de un empleado.
    /// </summary>
    [HttpDelete("employees/{employeeId}/work-roles/{workRoleId}")]
    public async Task<IActionResult> RemoveWorkRoleFromEmployee(Guid employeeId, Guid workRoleId)
    {
        try
        {
            var result = await _mediator.Send(new RemoveWorkRoleFromEmployeeCommand { EmployeeId = employeeId, WorkRoleId = workRoleId });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar rol de trabajo del empleado.", error = ex.Message });
        }
    }

    // ======================== ShiftTemplate Endpoints ========================

    /// <summary>
    /// Obtiene las plantillas de turno por sucursal y opcionalmente por día de semana.
    /// </summary>
    [HttpGet("shift-templates")]
    public async Task<IActionResult> GetShiftTemplates([FromQuery] Guid branchId, [FromQuery] DayOfWeek? dayOfWeek = null)
    {
        var result = await _mediator.Send(new GetShiftTemplatesQuery { BranchId = branchId, DayOfWeek = dayOfWeek });
        return Ok(result);
    }

    /// <summary>
    /// Obtiene una plantilla de turno por ID.
    /// </summary>
    [HttpGet("shift-templates/{id}")]
    public async Task<IActionResult> GetShiftTemplateById(Guid id)
    {
        var result = await _mediator.Send(new GetShiftTemplateByIdQuery { Id = id });
        if (result == null)
            return NotFound(new { message = "Plantilla de turno no encontrada." });
        return Ok(result);
    }

    /// <summary>
    /// Crea una plantilla de turno.
    /// </summary>
    [HttpPost("shift-templates")]
    public async Task<IActionResult> CreateShiftTemplate([FromBody] CreateShiftTemplateCommand command)
    {
        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        command.BranchId = branchId;
        try
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetShiftTemplateById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear plantilla de turno.", error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza una plantilla de turno.
    /// </summary>
    [HttpPut("shift-templates/{id}")]
    public async Task<IActionResult> UpdateShiftTemplate(Guid id, [FromBody] UpdateShiftTemplateCommand command)
    {
        command.Id = id;
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar plantilla de turno.", error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina una plantilla de turno.
    /// </summary>
    [HttpDelete("shift-templates/{id}")]
    public async Task<IActionResult> DeleteShiftTemplate(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteShiftTemplateCommand { Id = id });
            return Ok(new { message = "Plantilla de turno eliminada correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar plantilla de turno.", error = ex.Message });
        }
    }

    // ======================== ShiftAssignment Endpoints ========================

    /// <summary>
    /// Obtiene los turnos del mes por sucursal.
    /// </summary>
    [HttpGet("shifts")]
    public async Task<IActionResult> GetMonthlyShifts([FromQuery] Guid branchId, [FromQuery] int year, [FromQuery] int month)
    {
        var result = await _mediator.Send(new GetMonthlyShiftsQuery { BranchId = branchId, Year = year, Month = month });
        return Ok(result);
    }

    /// <summary>
    /// Obtiene empleados libres en una fecha específica.
    /// </summary>
    [HttpGet("shifts/free-employees")]
    public async Task<IActionResult> GetFreeEmployeesByDate([FromQuery] Guid branchId, [FromQuery] DateTime date)
    {
        var result = await _mediator.Send(new GetFreeEmployeesByDateQuery { BranchId = branchId, Date = date });
        return Ok(result);
    }

    /// <summary>
    /// Obtiene los turnos mensuales de un empleado.
    /// </summary>
    [HttpGet("employees/{employeeId}/shifts")]
    public async Task<IActionResult> GetEmployeeMonthlyShifts(Guid employeeId, [FromQuery] int year, [FromQuery] int month)
    {
        var result = await _mediator.Send(new GetEmployeeMonthlyShiftsQuery { EmployeeId = employeeId, Year = year, Month = month });
        return Ok(result);
    }

    /// <summary>
    /// Obtiene una asignación de turno por ID.
    /// </summary>
    [HttpGet("shifts/{id}")]
    public async Task<IActionResult> GetShiftAssignmentById(Guid id)
    {
        var result = await _mediator.Send(new GetShiftAssignmentByIdQuery { Id = id });
        if (result == null)
            return NotFound(new { message = "Asignación de turno no encontrada." });
        return Ok(result);
    }

    /// <summary>
    /// Obtiene las asignaciones de turno por fecha.
    /// </summary>
    [HttpGet("shifts/by-date")]
    public async Task<IActionResult> GetShiftAssignmentsByDate([FromQuery] DateTime date, [FromQuery] Guid branchId)
    {
        var result = await _mediator.Send(new GetShiftAssignmentsByDateQuery { BranchId = branchId, Date = date.Date });
        return Ok(result);
    }

    /// <summary>
    /// Crea una asignación de turno.
    /// </summary>
    [HttpPost("shifts")]
    public async Task<IActionResult> CreateShiftAssignment([FromBody] CreateShiftAssignmentCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetShiftAssignmentById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear asignación de turno.", error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina una asignación de turno.
    /// </summary>
    [HttpDelete("shifts/{id}")]
    public async Task<IActionResult> DeleteShiftAssignment(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteShiftAssignmentCommand { Id = id });
            return Ok(new { message = "Asignación de turno eliminada correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar asignación de turno.", error = ex.Message });
        }
    }

    // ======================== EmployeeAvailability Endpoints ========================

    /// <summary>
    /// Obtiene la disponibilidad de un empleado.
    /// </summary>
    [HttpGet("employees/{employeeId}/availability")]
    public async Task<IActionResult> GetEmployeeAvailability(Guid employeeId, [FromQuery] int? month = null, [FromQuery] int? year = null)
    {
        var result = await _mediator.Send(new GetEmployeeAvailabilityQuery { EmployeeId = employeeId, Month = month, Year = year });
        return Ok(result);
    }

    /// <summary>
    /// Agrega una fecha de indisponibilidad a un empleado.
    /// </summary>
    [HttpPost("employees/{employeeId}/availability")]
    public async Task<IActionResult> AddEmployeeAvailability(Guid employeeId, [FromBody] AddEmployeeAvailabilityCommand command)
    {
        command.EmployeeId = employeeId;
        try
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetEmployeeAvailability), new { employeeId = result.EmployeeId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al agregar la fecha de indisponibilidad.", error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina una fecha de indisponibilidad de un empleado.
    /// </summary>
    [HttpDelete("employees/{employeeId}/availability/{id}")]
    public async Task<IActionResult> RemoveEmployeeAvailability(Guid employeeId, Guid id)
    {
        try
        {
            var result = await _mediator.Send(new RemoveEmployeeAvailabilityCommand { Id = id, EmployeeId = employeeId });
            return Ok(new { message = "Fecha de indisponibilidad eliminada correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar la fecha de indisponibilidad.", error = ex.Message });
        }
    }

    // ======================== Generate Shifts ========================

    /// <summary>
    /// Genera turnos mensuales según plantillas y disponibilidad.
    /// </summary>
    [HttpPost("shifts/generate")]
    public async Task<IActionResult> GenerateMonthlyShifts([FromBody] GenerateMonthlyShiftsCommand command)
    {
        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        command.BranchId = branchId;
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al generar turnos.", error = ex.Message });
        }
    }

    // ======================== Schedulable Employees ========================

    /// <summary>
    /// Obtiene empleados elegibles para asignación de turnos.
    /// </summary>
    [HttpGet("employees/eligible")]
    public async Task<IActionResult> GetSchedulableEmployees()
    {
        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var result = await _mediator.Send(new GetSchedulableEmployeesQuery { BranchId = branchId });
        return Ok(result);
    }

    // ======================== Schedule Configuration Endpoints ========================

    /// <summary>
    /// Obtiene la configuración de horarios por sucursal.
    /// </summary>
    [HttpGet("configuration")]
    public async Task<IActionResult> GetScheduleConfiguration([FromQuery] Guid branchId)
    {
        var result = await _mediator.Send(new GetScheduleConfigurationQuery { BranchId = branchId });
        return Ok(result);
    }

    /// <summary>
    /// Crea la configuración de horarios.
    /// </summary>
    [HttpPost("configuration")]
    public async Task<IActionResult> CreateScheduleConfiguration([FromBody] CreateScheduleConfigurationCommand command)
    {
        var branchClaim = User.FindFirst("BranchId")?.Value;
        if (branchClaim == null || !Guid.TryParse(branchClaim, out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        command.BranchId = branchId;
        try
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetScheduleConfiguration), new { branchId = result.BranchId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear configuración de horarios.", error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza la configuración de horarios.
    /// </summary>
    [HttpPut("configuration/{id}")]
    public async Task<IActionResult> UpdateScheduleConfiguration(Guid id, [FromBody] UpdateScheduleConfigurationCommand command)
    {
        command.Id = id;
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al actualizar configuración de horarios.", error = ex.Message });
        }
    }
}

