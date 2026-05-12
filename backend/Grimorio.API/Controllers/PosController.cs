using Grimorio.Application.DTOs;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/pos")]
[Authorize]
public class PosController : ControllerBase
{
    private readonly IMediator _mediator;

    public PosController(IMediator mediator) => _mediator = mediator;

    // ── Estaciones ──────────────────────────────────────────────────────────

    [HttpGet("estaciones")]
    public async Task<IActionResult> GetStations()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetWorkStationsQuery { BranchId = branchId });
        return Ok(result);
    }

    [HttpPost("estaciones")]
    public async Task<IActionResult> CreateStation([FromBody] CreateWorkStationDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CreateWorkStationCommand
        {
            BranchId = branchId,
            Name = dto.Name,
            Type = dto.Type,
        });
        return Ok(result);
    }

    [HttpPut("estaciones/{id:guid}")]
    public async Task<IActionResult> UpdateStation(Guid id, [FromBody] UpdateWorkStationDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpdateWorkStationCommand
        {
            Id = id,
            BranchId = branchId,
            Name = dto.Name,
            Type = dto.Type,
            IsActive = dto.IsActive,
        });
        return Ok(result);
    }

    [HttpDelete("estaciones/{id:guid}")]
    public async Task<IActionResult> DeleteStation(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteWorkStationCommand { Id = id, BranchId = branchId });
        return Ok(new { message = "Estación eliminada." });
    }

    // ── Posición de mesas ───────────────────────────────────────────────────

    [HttpPatch("tables/{id:guid}/position")]
    public async Task<IActionResult> UpdateTablePosition(Guid id, [FromBody] UpdateTablePositionDto dto)
    {
        await _mediator.Send(new UpdateTablePositionCommand { Id = id, PosX = dto.PosX, PosY = dto.PosY });
        return Ok(new { message = "Posición actualizada." });
    }

    // ── Órdenes ─────────────────────────────────────────────────────────────

    [HttpGet("ordenes")]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] Guid? tableId = null,
        [FromQuery] bool activeOnly = true)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetOrdersQuery
        {
            BranchId = branchId,
            Status = status,
            Type = type,
            TableId = tableId,
            ActiveOnly = activeOnly,
        });
        return Ok(result);
    }

    [HttpGet("ordenes/{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetOrderDetailQuery { OrderId = id, BranchId = branchId });
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("ordenes")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        TryGetUserId(out var userId);

        var result = await _mediator.Send(new CreateOrderCommand
        {
            BranchId = branchId,
            WaiterId = userId == Guid.Empty ? null : userId,
            Type = dto.Type,
            TableId = dto.TableId,
            CustomerName = dto.CustomerName,
            DeliveryAddress = dto.DeliveryAddress,
            Notes = dto.Notes,
            Items = dto.Items,
        });
        return Ok(result);
    }

    [HttpPut("ordenes/{id:guid}/items")]
    public async Task<IActionResult> UpdateItems(Guid id, [FromBody] UpdateOrderItemsDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpdateOrderItemsCommand
        {
            OrderId = id,
            BranchId = branchId,
            Items = dto.Items,
        });
        return Ok(result);
    }

    [HttpPost("ordenes/{id:guid}/confirmar")]
    public async Task<IActionResult> ConfirmOrder(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new ConfirmOrderCommand { OrderId = id, BranchId = branchId });
        return Ok(result);
    }

    [HttpPost("ordenes/{id:guid}/entregar")]
    public async Task<IActionResult> DeliverOrder(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new DeliverOrderCommand { OrderId = id, BranchId = branchId });
        return Ok(result);
    }

    [HttpPost("ordenes/{id:guid}/cancelar")]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CancelOrderCommand { OrderId = id, BranchId = branchId });
        return Ok(result);
    }

    [HttpPatch("orden-items/{id:guid}/estado")]
    public async Task<IActionResult> SetItemEstado(Guid id, [FromBody] SetItemEstadoBody body)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new SetOrderItemStatusCommand
        {
            OrderItemId = id,
            BranchId = branchId,
            Status = body.Estado,
        });
        return Ok(result);
    }

    // ── Monitor de estación ─────────────────────────────────────────────────

    [HttpGet("estaciones/{id:guid}/items")]
    public async Task<IActionResult> GetStationItems(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetItemsByStationQuery { StationId = id, BranchId = branchId });
        return Ok(result);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private bool TryGetBranchId(out Guid branchId)
    {
        branchId = Guid.Empty;
        var claim = User.FindFirst(AppConstants.Claims.BranchId)?.Value;
        return claim != null && Guid.TryParse(claim, out branchId);
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var claim = User.FindFirst(AppConstants.Claims.UserId)?.Value
            ?? User.FindFirst("sub")?.Value;
        return claim != null && Guid.TryParse(claim, out userId);
    }

    public class SetItemEstadoBody { public string Estado { get; set; } = string.Empty; }
    public class UpdateTablePositionDto { public int PosX { get; set; } public int PosY { get; set; } }
}

