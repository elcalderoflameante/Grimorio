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

    [Authorize(Policy = "Billing.PaymentMethods.View")]
    [HttpGet("metodos-pago")]
    public async Task<IActionResult> GetPaymentMethods([FromQuery] bool activeOnly = true)
        => Ok(await _mediator.Send(new GetPaymentMethodsQuery { ActiveOnly = activeOnly }));

    [Authorize(Policy = "Billing.PaymentMethods.Manage")]
    [HttpPost("metodos-pago")]
    public async Task<IActionResult> CreatePaymentMethod([FromBody] CreatePaymentMethodConfigDto dto)
    {
        var result = await _mediator.Send(new CreatePaymentMethodCommand
        {
            Name = dto.Name, Color = dto.Color,
            IsCash = dto.IsCash, IsCard = dto.IsCard, SortOrder = dto.SortOrder,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Billing.PaymentMethods.Manage")]
    [HttpPut("metodos-pago/{id:guid}")]
    public async Task<IActionResult> UpdatePaymentMethod(Guid id, [FromBody] UpdatePaymentMethodConfigDto dto)
    {
        var result = await _mediator.Send(new UpdatePaymentMethodCommand
        {
            Id = id, Name = dto.Name, Color = dto.Color,
            IsCash = dto.IsCash, IsCard = dto.IsCard,
            IsActive = dto.IsActive, SortOrder = dto.SortOrder,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Billing.PaymentMethods.Manage")]
    [HttpDelete("metodos-pago/{id:guid}")]
    public async Task<IActionResult> DeletePaymentMethod(Guid id)
    {
        await _mediator.Send(new DeletePaymentMethodCommand { Id = id });
        return NoContent();
    }

    [Authorize(Policy = "Billing.PaymentMethods.View")]
    [HttpGet("bancos-tarjeta")]
    public async Task<IActionResult> GetCardBanks([FromQuery] bool activeOnly = true)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetCardBanksQuery { BranchId = branchId, ActiveOnly = activeOnly }));
    }

    [Authorize(Policy = "Billing.PaymentMethods.Manage")]
    [HttpPost("bancos-tarjeta")]
    public async Task<IActionResult> CreateCardBank([FromBody] CreateCardBankDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CreateCardBankCommand
        {
            BranchId = branchId, Name = dto.Name, SortOrder = dto.SortOrder,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Billing.PaymentMethods.Manage")]
    [HttpPut("bancos-tarjeta/{id:guid}")]
    public async Task<IActionResult> UpdateCardBank(Guid id, [FromBody] UpdateCardBankDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpdateCardBankCommand
        {
            Id = id, BranchId = branchId,
            Name = dto.Name, IsActive = dto.IsActive, SortOrder = dto.SortOrder,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Billing.PaymentMethods.Manage")]
    [HttpDelete("bancos-tarjeta/{id:guid}")]
    public async Task<IActionResult> DeleteCardBank(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteCardBankCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    // â”€â”€ Cajas / estaciones â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Authorize(Policy = "Billing.CashRegisters.View")]
    [HttpGet("cajas")]
    public async Task<IActionResult> GetCashRegisters([FromQuery] bool activeOnly = true)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetCashRegistersQuery { BranchId = branchId, ActiveOnly = activeOnly }));
    }

    [Authorize(Policy = "Billing.CashRegisters.Manage")]
    [HttpPost("cajas")]
    public async Task<IActionResult> CreateCashRegister([FromBody] CreateCashRegisterDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CreateCashRegisterCommand
        {
            BranchId = branchId,
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Billing.CashRegisters.Manage")]
    [HttpPut("cajas/{id:guid}")]
    public async Task<IActionResult> UpdateCashRegister(Guid id, [FromBody] UpdateCashRegisterDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpdateCashRegisterCommand
        {
            Id = id,
            BranchId = branchId,
            Name = dto.Name,
            Code = dto.Code,
            Description = dto.Description,
            IsActive = dto.IsActive,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Billing.CashRegisters.Manage")]
    [HttpDelete("cajas/{id:guid}")]
    public async Task<IActionResult> DeleteCashRegister(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteCashRegisterCommand { Id = id, BranchId = branchId });
        return NoContent();
    }

    // ── Sesión activa ─────────────────────────────────────────────────────────

    [Authorize(Policy = "Billing.Cash.View")]
    [HttpGet("sesion-activa")]
    public async Task<IActionResult> GetActiveSession()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _mediator.Send(new GetActiveCashSessionQuery { BranchId = branchId, UserId = userId });
        return result is null ? NotFound() : Ok(result);
    }

    // ── Historial ─────────────────────────────────────────────────────────────

    [Authorize(Policy = "Billing.Cash.View")]
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

    [Authorize(Policy = "Billing.Cash.View")]
    [HttpGet("sesiones/{id:guid}")]
    public async Task<IActionResult> GetSession(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetCashSessionDetailQuery { Id = id, BranchId = branchId });
        return result is null ? NotFound() : Ok(result);
    }

    // ── Abrir / Cerrar ────────────────────────────────────────────────────────

    [Authorize(Policy = "Billing.Cash.Open")]
    [HttpPost("abrir")]
    public async Task<IActionResult> OpenSession([FromBody] OpenCashSessionDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var userName = BuildUserName();

        var result = await _mediator.Send(new OpenCashSessionCommand
        {
            BranchId = branchId, UserId = userId,
            CashRegisterId = dto.CashRegisterId,
            UserName = userName, OpeningBalance = dto.OpeningBalance,
        });
        return Ok(result);
    }

    [Authorize(Policy = "Billing.Cash.Close")]
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

    [Authorize(Policy = "Billing.Cash.View")]
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

    [Authorize(Policy = "Billing.Cash.View")]
    [HttpGet("ventas/rentabilidad")]
    public async Task<IActionResult> GetSalesProfitability(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? cashRegisterId)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetSalesProfitabilityQuery
        {
            BranchId = branchId,
            FromUtc = from,
            ToUtc = to,
            CashRegisterId = cashRegisterId,
        }));
    }

    [Authorize(Policy = "Billing.Cash.View")]
    [HttpGet("ordenes/{orderId:guid}/pagos")]
    public async Task<IActionResult> GetOrderPayments(Guid orderId)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetOrderPaymentsQuery { OrderId = orderId, BranchId = branchId }));
    }

    [Authorize(Policy = "Billing.Cash.Charge")]
    [HttpPost("cobrar/{orderId:guid}")]
    public async Task<IActionResult> PayOrder(Guid orderId, [FromBody] AddOrderPaymentDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _mediator.Send(new PayOrderCommand
        {
            OrderId = orderId, BranchId = branchId, UserId = userId,
            OrderAmount = dto.OrderAmount,
            DocumentType = dto.DocumentType,
            CustomerId = dto.CustomerId, CashSessionId = dto.CashSessionId,
            Lines = dto.Lines.Select(l => new PaymentLineCommand
            {
                MethodId = l.MethodId, AmountTendered = l.AmountTendered,
                CardPaymentType = l.CardPaymentType,
                CardBankId = l.CardBankId,
                CardBrand = l.CardBrand,
                AuthorizationNumber = l.AuthorizationNumber,
            }).ToList(),
            Items = dto.Items.Select(i => new PaymentItemCommand
            {
                OrderItemId = i.OrderItemId,
                Quantity = i.Quantity,
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
