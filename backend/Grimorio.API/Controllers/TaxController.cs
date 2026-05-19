using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Billing.Commands;
using Grimorio.Application.Features.Billing.Queries;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaxController : ControllerBase
{
    private readonly IMediator _mediator;
    public TaxController(IMediator mediator) => _mediator = mediator;

    // ── Tarifas de IVA ────────────────────────────────────────────────────────

    [HttpGet("tarifas")]
    public async Task<IActionResult> GetTaxRates([FromQuery] bool activeOnly = false)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetTaxRatesQuery { BranchId = branchId, ActiveOnly = activeOnly }));
    }

    [HttpPost("tarifas")]
    public async Task<IActionResult> CreateTaxRate([FromBody] UpsertTaxRateDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CreateTaxRateCommand
        {
            BranchId = branchId, Name = dto.Name, Percentage = dto.Percentage,
            SriCode = dto.SriCode, IsDefault = dto.IsDefault, IsActive = dto.IsActive,
        });
        return Ok(result);
    }

    [HttpPut("tarifas/{id:guid}")]
    public async Task<IActionResult> UpdateTaxRate(Guid id, [FromBody] UpsertTaxRateDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpdateTaxRateCommand
        {
            Id = id, BranchId = branchId, Name = dto.Name, Percentage = dto.Percentage,
            SriCode = dto.SriCode, IsDefault = dto.IsDefault, IsActive = dto.IsActive,
        });
        return Ok(result);
    }

    [HttpDelete("tarifas/{id:guid}")]
    public async Task<IActionResult> DeleteTaxRate(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteTaxRateCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    private bool TryGetBranchId(out Guid branchId)
    {
        var claim = User.FindFirst(AppConstants.Claims.BranchId)?.Value;
        return Guid.TryParse(claim, out branchId);
    }
}
