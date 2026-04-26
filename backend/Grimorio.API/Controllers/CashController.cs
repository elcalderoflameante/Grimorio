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
public class CashController : ControllerBase
{
    private readonly IMediator _mediator;
    public CashController(IMediator mediator) => _mediator = mediator;

    // ── Sesión activa ─────────────────────────────────────────────────────────

    [HttpGet("sesion-activa")]
    public async Task<IActionResult> GetActiveSession()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetActiveCashSessionQuery { BranchId = branchId });
        return result is null ? NotFound() : Ok(result);
    }

    // ── Historial ─────────────────────────────────────────────────────────────

    [HttpGet("sesiones")]
    public async Task<IActionResult> GetSessions(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int pageSize = 30)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetCashSessionsQuery
        {
            BranchId = branchId, FromUtc = from, ToUtc = to, PageSize = pageSize,
        }));
    }

    [HttpGet("sesiones/{id:guid}")]
    public async Task<IActionResult> GetSession(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetCashSessionDetailQuery { Id = id, BranchId = branchId });
        return result is null ? NotFound() : Ok(result);
    }

    // ── Abrir / Cerrar ────────────────────────────────────────────────────────

    [HttpPost("abrir")]
    public async Task<IActionResult> OpenSession([FromBody] OpenCashSessionDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? User.FindFirst("name")?.Value ?? "Usuario";

        var result = await _mediator.Send(new OpenCashSessionCommand
        {
            BranchId = branchId, UserId = userId,
            UserName = userName, OpeningBalance = dto.OpeningBalance,
        });
        return Ok(result);
    }

    [HttpPost("sesiones/{id:guid}/cerrar")]
    public async Task<IActionResult> CloseSession(Guid id, [FromBody] CloseCashSessionDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? User.FindFirst("name")?.Value ?? "Usuario";

        var result = await _mediator.Send(new CloseCashSessionCommand
        {
            Id = id, BranchId = branchId, UserId = userId, UserName = userName,
            ActualCash = dto.ActualCash, Notes = dto.Notes,
        });
        return Ok(result);
    }

    // ── Cobro de orden ────────────────────────────────────────────────────────

    [HttpPost("cobrar/{orderId:guid}")]
    public async Task<IActionResult> PayOrder(Guid orderId, [FromBody] PayOrderDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new PayOrderCommand
        {
            OrderId = orderId, BranchId = branchId,
            Method = dto.Method, AmountPaid = dto.AmountPaid,
            CustomerId = dto.CustomerId, CashSessionId = dto.CashSessionId,
        });
        return Ok(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetBranchId(out Guid branchId)
    {
        var claim = User.FindFirst(AppConstants.Claims.BranchId)?.Value;
        return Guid.TryParse(claim, out branchId);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out userId);
    }
}
