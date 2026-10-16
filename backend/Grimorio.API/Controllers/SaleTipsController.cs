using Grimorio.Application.Features.Finance;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/sale-tips")]
[Authorize(Policy = "Billing.Cash.View")]
public class SaleTipsController(IMediator mediator) : ControllerBase
{
    private Guid BranchId => Guid.TryParse(User.FindFirst(AppConstants.Claims.BranchId)?.Value, out var id) ? id : Guid.Empty;

    [HttpGet("{paymentId:guid}")]
    public async Task<IActionResult> Get(Guid paymentId, CancellationToken ct)
    {
        if (BranchId == Guid.Empty) return Unauthorized();
        return await Send(new GetSaleTipQuery(BranchId, paymentId), ct);
    }

    [HttpGet]
    public async Task<IActionResult> Report([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] string? method, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        if (BranchId == Guid.Empty) return Unauthorized();
        return await Send(new GetSaleTipReportQuery(BranchId, from, to, method, page, pageSize), ct);
    }

    [HttpPut("{paymentId:guid}")]
    [Authorize(Policy = "Billing.Cash.Charge")]
    public async Task<IActionResult> Save(Guid paymentId, [FromBody] SaveSaleTipDto dto, CancellationToken ct)
    {
        if (BranchId == Guid.Empty) return Unauthorized();
        var name = $"{User.FindFirst(AppConstants.Claims.FirstName)?.Value} {User.FindFirst(AppConstants.Claims.LastName)?.Value}".Trim();
        return await Send(new SaveSaleTipCommand(BranchId, paymentId, dto.Amount, dto.Method,
            string.IsNullOrEmpty(name) ? "Usuario" : name), ct);
    }

    [HttpDelete("{paymentId:guid}")]
    [Authorize(Policy = "Billing.Cash.Charge")]
    public async Task<IActionResult> Delete(Guid paymentId, CancellationToken ct)
    {
        if (BranchId == Guid.Empty) return Unauthorized();
        return await Send(new DeleteSaleTipCommand(BranchId, paymentId), ct, noContent: true);
    }

    private async Task<IActionResult> Send<T>(IRequest<T> request, CancellationToken ct, bool noContent = false)
    {
        try
        {
            var result = await mediator.Send(request, ct);
            return noContent ? NoContent() : new JsonResult(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
