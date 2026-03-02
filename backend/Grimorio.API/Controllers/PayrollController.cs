using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Payroll.Commands;
using Grimorio.Application.Features.Payroll.Queries;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IMediator _mediator;

    public PayrollController(IMediator mediator) => _mediator = mediator;

    [HttpGet("configuration")]
    [Authorize(Policy = "RRHH.ViewEmployees")]
    public async Task<IActionResult> GetConfiguration()
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var result = await _mediator.Send(new GetPayrollConfigurationQuery { BranchId = branchId });
        return Ok(result);
    }

    [HttpPut("configuration")]
    [Authorize(Policy = "RRHH.UpdateEmployees")]
    public async Task<IActionResult> UpsertConfiguration([FromBody] CreatePayrollConfigurationDto dto)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var command = new UpsertPayrollConfigurationCommand
        {
            BranchId = branchId,
            IessEmployeeRate = dto.IessEmployeeRate,
            IessEmployerRate = dto.IessEmployerRate,
            IncomeTaxRate = dto.IncomeTaxRate,
            OvertimeRate50 = dto.OvertimeRate50,
            OvertimeRate100 = dto.OvertimeRate100,
            DecimoThirdRate = dto.DecimoThirdRate,
            DecimoFourthRate = dto.DecimoFourthRate,
            ReserveFundRate = dto.ReserveFundRate,
            MonthlyHours = dto.MonthlyHours
        };

        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al guardar configuración de nómina.", error = ex.Message });
        }
    }

    [HttpGet("summary")]
    [Authorize(Policy = "RRHH.ViewEmployees")]
    public async Task<IActionResult> GetSummary([FromQuery] int year, [FromQuery] int month)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var result = await _mediator.Send(new GetPayrollSummaryQuery { BranchId = branchId, Year = year, Month = month });
        return Ok(result);
    }

    [HttpGet("advances")]
    [Authorize(Policy = "RRHH.ViewEmployees")]
    public async Task<IActionResult> GetAdvances([FromQuery] Guid? employeeId, [FromQuery] int? year, [FromQuery] int? month)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var result = await _mediator.Send(new GetPayrollAdvancesQuery
        {
            BranchId = branchId,
            EmployeeId = employeeId,
            Year = year,
            Month = month
        });

        return Ok(result);
    }

    [HttpPost("advances")]
    [Authorize(Policy = "RRHH.UpdateEmployees")]
    public async Task<IActionResult> CreateAdvance([FromBody] CreatePayrollAdvanceDto dto)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var command = new CreatePayrollAdvanceCommand
        {
            BranchId = branchId,
            EmployeeId = dto.EmployeeId,
            Date = dto.Date,
            Amount = dto.Amount,
            Method = dto.Method,
            Notes = dto.Notes
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
            return StatusCode(500, new { message = "Error al registrar avance.", error = ex.Message });
        }
    }

    [HttpDelete("advances/{advanceId}")]
    [Authorize(Policy = "RRHH.UpdateEmployees")]
    public async Task<IActionResult> DeleteAdvance(Guid advanceId)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var command = new DeletePayrollAdvanceCommand
        {
            BranchId = branchId,
            PayrollAdvanceId = advanceId
        };

        try
        {
            await _mediator.Send(command);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar adelanto.", error = ex.Message });
        }
    }

    [HttpGet("consumptions")]
    [Authorize(Policy = "RRHH.ViewEmployees")]
    public async Task<IActionResult> GetConsumptions([FromQuery] Guid? employeeId, [FromQuery] int? year, [FromQuery] int? month)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var result = await _mediator.Send(new GetEmployeeConsumptionsQuery
        {
            BranchId = branchId,
            EmployeeId = employeeId,
            Year = year,
            Month = month
        });

        return Ok(result);
    }

    [HttpPost("consumptions")]
    [Authorize(Policy = "RRHH.UpdateEmployees")]
    public async Task<IActionResult> CreateConsumption([FromBody] CreateEmployeeConsumptionDto dto)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var command = new CreateEmployeeConsumptionCommand
        {
            BranchId = branchId,
            EmployeeId = dto.EmployeeId,
            Date = dto.Date,
            Amount = dto.Amount,
            Notes = dto.Notes
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
            return StatusCode(500, new { message = "Error al registrar consumo.", error = ex.Message });
        }
    }

    [HttpDelete("consumptions/{consumptionId}")]
    [Authorize(Policy = "RRHH.UpdateEmployees")]
    public async Task<IActionResult> DeleteConsumption(Guid consumptionId)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var command = new DeleteEmployeeConsumptionCommand
        {
            BranchId = branchId,
            EmployeeConsumptionId = consumptionId
        };

        try
        {
            await _mediator.Send(command);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar consumo.", error = ex.Message });
        }
    }

    [HttpGet("adjustments")]
    [Authorize(Policy = "RRHH.ViewEmployees")]
    public async Task<IActionResult> GetAdjustments([FromQuery] Guid? employeeId, [FromQuery] int? year, [FromQuery] int? month)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var result = await _mediator.Send(new GetPayrollAdjustmentsQuery
        {
            BranchId = branchId,
            EmployeeId = employeeId,
            Year = year,
            Month = month
        });

        return Ok(result);
    }

    [HttpPost("adjustments")]
    [Authorize(Policy = "RRHH.UpdateEmployees")]
    public async Task<IActionResult> CreateAdjustment([FromBody] CreatePayrollAdjustmentDto dto)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var command = new CreatePayrollAdjustmentCommand
        {
            BranchId = branchId,
            EmployeeId = dto.EmployeeId,
            Date = dto.Date,
            Type = dto.Type,
            Category = dto.Category,
            Hours = dto.Hours,
            Amount = dto.Amount,
            Notes = dto.Notes
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
            return StatusCode(500, new { message = "Error al registrar ajuste.", error = ex.Message });
        }
    }

    [HttpDelete("adjustments/{adjustmentId}")]
    [Authorize(Policy = "RRHH.UpdateEmployees")]
    public async Task<IActionResult> DeleteAdjustment(Guid adjustmentId)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var command = new DeletePayrollAdjustmentCommand
        {
            BranchId = branchId,
            PayrollAdjustmentId = adjustmentId
        };

        try
        {
            await _mediator.Send(command);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al eliminar ajuste.", error = ex.Message });
        }
    }

    [HttpPost("roles/generate")]
    [Authorize(Policy = "RRHH.UpdateEmployees")]
    public async Task<IActionResult> GenerateMonthlyRoles([FromQuery] int year, [FromQuery] int month, [FromQuery] Guid? employeeId = null)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var command = new GenerateMonthlyPayrollRolesCommand
        {
            BranchId = branchId,
            Year = year,
            Month = month,
            EmployeeId = employeeId
        };

        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al generar roles de pago.", error = ex.Message });
        }
    }

    [HttpGet("roles/employee/{employeeId}")]
    [Authorize(Policy = "RRHH.ViewEmployees")]
    public async Task<IActionResult> GetRolesByEmployee(Guid employeeId)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var query = new GetPayrollRolesByEmployeeQuery
        {
            BranchId = branchId,
            EmployeeId = employeeId
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("roles/{roleId}")]
    [Authorize(Policy = "RRHH.ViewEmployees")]
    public async Task<IActionResult> GetRoleDetail(Guid roleId)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var query = new GetPayrollRoleDetailQuery
        {
            BranchId = branchId,
            PayrollRoleId = roleId
        };

        var result = await _mediator.Send(query);
        if (result == null)
            return NotFound(new { message = "Rol de pago no encontrado." });

        return Ok(result);
    }

    [HttpPatch("roles/{roleId}/status")]
    [Authorize(Policy = "RRHH.UpdateEmployees")]
    public async Task<IActionResult> UpdateRoleStatus(Guid roleId, [FromBody] UpdatePayrollRoleStatusDto dto)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var command = new UpdatePayrollRoleStatusCommand
        {
            BranchId = branchId,
            PayrollRoleId = roleId,
            Status = dto.Status
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
            return StatusCode(500, new { message = "Error al actualizar estado del rol de pago.", error = ex.Message });
        }
    }

    private bool TryGetBranchId(out Guid branchId)
    {
        var branchClaim = User.FindFirst("BranchId")?.Value;
        return Guid.TryParse(branchClaim, out branchId);
    }
}
