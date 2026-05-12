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

    // ── Medios de pago ────────────────────────────────────────────────────────

    [HttpGet("metodos-pago")]
    public async Task<IActionResult> GetPaymentMethods([FromQuery] bool activeOnly = true)
        => Ok(await _mediator.Send(new GetPaymentMethodsQuery { ActiveOnly = activeOnly }));

    [HttpPost("metodos-pago")]
    public async Task<IActionResult> CreatePaymentMethod([FromBody] CreatePaymentMethodConfigDto dto)
    {
        var result = await _mediator.Send(new CreatePaymentMethodCommand
        {
            Name = dto.Name, Color = dto.Color, IsCash = dto.IsCash, SortOrder = dto.SortOrder,
        });
        return Ok(result);
    }

    [HttpPut("metodos-pago/{id:guid}")]
    public async Task<IActionResult> UpdatePaymentMethod(Guid id, [FromBody] UpdatePaymentMethodConfigDto dto)
    {
        var result = await _mediator.Send(new UpdatePaymentMethodCommand
        {
            Id = id, Name = dto.Name, Color = dto.Color,
            IsCash = dto.IsCash, IsActive = dto.IsActive, SortOrder = dto.SortOrder,
        });
        return Ok(result);
    }

    [HttpDelete("metodos-pago/{id:guid}")]
    public async Task<IActionResult> DeletePaymentMethod(Guid id)
    {
        await _mediator.Send(new DeletePaymentMethodCommand { Id = id });
        return NoContent();
    }

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
        var userName = BuildUserName();

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
        var userName = BuildUserName();

        var result = await _mediator.Send(new CloseCashSessionCommand
        {
            Id = id, BranchId = branchId, UserId = userId, UserName = userName,
            ActualCash = dto.ActualCash, Notes = dto.Notes,
        });
        return Ok(result);
    }

    // ── Ventas realizadas ─────────────────────────────────────────────────────

    [HttpGet("ventas")]
    public async Task<IActionResult> GetSales(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int pageSize = 100)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetSalesQuery
        {
            BranchId = branchId, FromUtc = from, ToUtc = to, PageSize = pageSize,
        }));
    }

    // ── Cobro de orden ────────────────────────────────────────────────────────

    [HttpGet("ordenes/{orderId:guid}/pagos")]
    public async Task<IActionResult> GetOrderPayments(Guid orderId)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetOrderPaymentsQuery { OrderId = orderId, BranchId = branchId }));
    }

    [HttpPost("cobrar/{orderId:guid}")]
    public async Task<IActionResult> PayOrder(Guid orderId, [FromBody] AddOrderPaymentDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new PayOrderCommand
        {
            OrderId = orderId, BranchId = branchId,
            OrderAmount = dto.OrderAmount,
            DocumentType = dto.DocumentType,
            CustomerId = dto.CustomerId, CashSessionId = dto.CashSessionId,
            Lines = dto.Lines.Select(l => new PaymentLineCommand
            {
                MethodId = l.MethodId, AmountTendered = l.AmountTendered,
            }).ToList(),
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

    private string BuildUserName()
    {
        var firstName = User.FindFirst("FirstName")?.Value ?? string.Empty;
        var lastName = User.FindFirst("LastName")?.Value ?? string.Empty;
        var full = $"{firstName} {lastName}".Trim();
        return string.IsNullOrEmpty(full) ? "Usuario" : full;
    }
}
