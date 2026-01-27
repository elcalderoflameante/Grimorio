using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Employees.Commands;
using Grimorio.Application.Features.Employees.Queries;
using System.Security.Claims;

namespace Grimorio.API.Controllers;

/// <summary>
/// Controlador para gestionar empleados.
/// Todos los endpoints requieren autenticación.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmployeesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtiene un empleado por su ID.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "RRHH.ViewEmployees")]
    public async Task<IActionResult> GetEmployee(Guid id)
    {
        var branchId = User.FindFirst("BranchId");
        if (branchId == null || !Guid.TryParse(branchId.Value, out var parsedBranchId))
            return Unauthorized("BranchId no válido en el token.");

        var query = new GetEmployeeQuery
        {
            EmployeeId = id,
            BranchId = parsedBranchId
        };

        var result = await _mediator.Send(query);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Obtiene todos los empleados de la rama del usuario.
    /// Soporta paginación.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RRHH.ViewEmployees")]
    public async Task<IActionResult> GetEmployees([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var branchId = User.FindFirst("BranchId");
        if (branchId == null || !Guid.TryParse(branchId.Value, out var parsedBranchId))
            return Unauthorized("BranchId no válido en el token.");

        var query = new GetEmployeesQuery
        {
            BranchId = parsedBranchId,
            OnlyActive = true,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Crea un nuevo empleado.
    /// Requiere permiso: RRHH.CreateEmployees
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RRHH.CreateEmployees")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var branchId = User.FindFirst("BranchId");
        if (branchId == null || !Guid.TryParse(branchId.Value, out var parsedBranchId))
            return Unauthorized("BranchId no válido en el token.");

        var command = new CreateEmployeeCommand
        {
            BranchId = parsedBranchId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            IdentificationNumber = dto.IdentificationNumber,
            PositionId = dto.PositionId,
            HireDate = dto.HireDate
        };

        try
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetEmployee), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al crear empleado.", error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un empleado existente.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RRHH.UpdateEmployees")]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var branchId = User.FindFirst("BranchId");
        if (branchId == null || !Guid.TryParse(branchId.Value, out var parsedBranchId))
            return Unauthorized("BranchId no válido en el token.");

        var command = new UpdateEmployeeCommand
        {
            EmployeeId = id,
            BranchId = parsedBranchId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            PositionId = dto.PositionId,
            TerminationDate = dto.TerminationDate,
            IsActive = dto.IsActive
        };

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
            return StatusCode(500, new { message = "Error al actualizar empleado.", error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina (soft delete) un empleado.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RRHH.DeleteEmployees")]
    public async Task<IActionResult> DeleteEmployee(Guid id)
    {
        var branchId = User.FindFirst("BranchId");
        if (branchId == null || !Guid.TryParse(branchId.Value, out var parsedBranchId))
            return Unauthorized("BranchId no válido en el token.");

        var command = new DeleteEmployeeCommand
        {
            EmployeeId = id,
            BranchId = parsedBranchId
        };

        try
        {
            var result = await _mediator.Send(command);
            return Ok(new { message = "Empleado eliminado correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar empleado.", error = ex.Message });
        }
    }
}
