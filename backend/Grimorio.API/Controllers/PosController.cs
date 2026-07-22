using Grimorio.API.Hubs;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Application.Features.POS.Queries;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/pos")]
[Authorize]
public class PosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHubContext<KitchenHub> _kitchenHub;

    public PosController(IMediator mediator, IHubContext<KitchenHub> kitchenHub)
    {
        _mediator = mediator;
        _kitchenHub = kitchenHub;
    }

    // ── Estaciones ──────────────────────────────────────────────────────────

    [Authorize(Policy = "POS.Stations.View")]
    [HttpGet("estaciones")]
    public async Task<IActionResult> GetStations()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetWorkStationsQuery { BranchId = branchId });
        return Ok(result);
    }

    [Authorize(Policy = "POS.Stations.Manage")]
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

    [Authorize(Policy = "POS.Stations.Manage")]
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

    [Authorize(Policy = "POS.Stations.Manage")]
    [HttpDelete("estaciones/{id:guid}")]
    public async Task<IActionResult> DeleteStation(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        await _mediator.Send(new DeleteWorkStationCommand { Id = id, BranchId = branchId });
        return Ok(new { message = "Estación eliminada." });
    }

    // ── Posición de mesas ───────────────────────────────────────────────────

    [Authorize(Policy = "POS.Tables.Manage")]
    [HttpPatch("tables/{id:guid}/position")]
    public async Task<IActionResult> UpdateTablePosition(Guid id, [FromBody] UpdateTablePositionDto dto)
    {
        await _mediator.Send(new UpdateTablePositionCommand { Id = id, PosX = dto.PosX, PosY = dto.PosY });
        return Ok(new { message = "Posición actualizada." });
    }

    // ── Órdenes ─────────────────────────────────────────────────────────────

    [Authorize(Policy = "POS.Orders.View")]
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

    [Authorize(Policy = "POS.Orders.View")]
    [HttpGet("ordenes/activas/resumen")]
    public async Task<IActionResult> GetActiveOrderSummaries()
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        return Ok(await _mediator.Send(new GetActiveOrderSummariesQuery { BranchId = branchId }));
    }

    [Authorize(Policy = "POS.Orders.View")]
    [HttpGet("ordenes/{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetOrderDetailQuery { OrderId = id, BranchId = branchId });
        if (result == null) return NotFound();
        return Ok(result);
    }

    [Authorize(Policy = "POS.Orders.Create")]
    [HttpPost("ordenes")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        if (User.IsInRole(AppConstants.Roles.Waiter) &&
            (!string.Equals(dto.Type, "DineIn", StringComparison.OrdinalIgnoreCase) || !dto.TableId.HasValue))
        {
            return BadRequest(new { message = "Los meseros solo pueden iniciar pedidos asociados a una mesa." });
        }
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

    [Authorize(Policy = "POS.DirectSale.Create")]
    [HttpPost("ventas-directas")]
    public async Task<IActionResult> CreateDirectSale([FromBody] CreateOrderDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        TryGetUserId(out var userId);

        var result = await _mediator.Send(new CreateDirectSaleCommand
        {
            BranchId = branchId,
            CashierId = userId == Guid.Empty ? null : userId,
            CustomerName = dto.CustomerName,
            Notes = dto.Notes,
            Items = dto.Items,
        });

        var confirmedAt = result.ConfirmedAt ?? result.CreatedAt;
        var itemsByStation = result.Items
            .Where(i => i.StationId.HasValue)
            .GroupBy(i => i.StationId!.Value);
        foreach (var group in itemsByStation)
        {
            var payload = group.Select(i => new
            {
                orderItemId = i.Id,
                orderId = result.Id,
                orderNumber = result.Number,
                orderType = result.Type,
                tableCode = result.TableCode,
                customerName = result.CustomerName,
                orderNotes = result.Notes,
                itemName = i.ItemName,
                quantity = i.Quantity,
                notes = i.Notes,
                isTakeout = i.IsTakeout,
                status = i.Status,
                confirmedAt,
                modifierSelections = i.ModifierSelections.Select(s => new
                {
                    groupName = s.GroupName,
                    optionName = s.OptionName,
                    quantity = s.Quantity,
                }),
            });
            await _kitchenHub.Clients
                .Group(KitchenHub.GetStationGroup(group.Key))
                .SendAsync(KitchenHub.NewItemsEvent, payload);
        }

        return Ok(result);
    }

    [Authorize(Policy = "POS.Orders.Update")]
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

        // Notificar a cada estación los ítems nuevos (Pending) del pedido actualizado
        var confirmedAt = result.ConfirmedAt ?? result.CreatedAt;
        var newItemsByStation = result.Items
            .Where(i => i.StationId.HasValue && i.Status == "Pending")
            .GroupBy(i => i.StationId!.Value);
        foreach (var group in newItemsByStation)
        {
            var payload = group.Select(i => new
            {
                orderItemId = i.Id,
                orderId = result.Id,
                orderNumber = result.Number,
                orderType = result.Type,
                tableCode = result.TableCode,
                customerName = result.CustomerName,
                orderNotes = result.Notes,
                itemName = i.ItemName,
                quantity = i.Quantity,
                notes = i.Notes,
                isTakeout = i.IsTakeout,
                status = i.Status,
                confirmedAt,
                modifierSelections = i.ModifierSelections.Select(s => new
                {
                    groupName = s.GroupName,
                    optionName = s.OptionName,
                    quantity = s.Quantity,
                }),
            });
            await _kitchenHub.Clients
                .Group(KitchenHub.GetStationGroup(group.Key))
                .SendAsync(KitchenHub.NewItemsEvent, payload);
        }

        return Ok(result);
    }

    [Authorize(Policy = "POS.Orders.Update")]
    [HttpPost("ordenes/{id:guid}/confirmar")]
    public async Task<IActionResult> ConfirmOrder(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        OrderDto result;
        try
        {
            result = await _mediator.Send(new ConfirmOrderCommand { OrderId = id, BranchId = branchId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        // Notificar a cada estación sus nuevos ítems
        var confirmedAt = result.ConfirmedAt ?? result.CreatedAt;
        var itemsByStation = result.Items
            .Where(i => i.StationId.HasValue)
            .GroupBy(i => i.StationId!.Value);
        foreach (var group in itemsByStation)
        {
            var payload = group.Select(i => new
            {
                orderItemId = i.Id,
                orderId = result.Id,
                orderNumber = result.Number,
                orderType = result.Type,
                tableCode = result.TableCode,
                customerName = result.CustomerName,
                orderNotes = result.Notes,
                itemName = i.ItemName,
                quantity = i.Quantity,
                notes = i.Notes,
                isTakeout = i.IsTakeout,
                status = i.Status,
                confirmedAt,
                modifierSelections = i.ModifierSelections.Select(s => new
                {
                    groupName = s.GroupName,
                    optionName = s.OptionName,
                    quantity = s.Quantity,
                }),
            });
            await _kitchenHub.Clients
                .Group(KitchenHub.GetStationGroup(group.Key))
                .SendAsync(KitchenHub.NewItemsEvent, payload);
        }

        return Ok(result);
    }

    [Authorize(Policy = "POS.Orders.Update")]
    [HttpPost("ordenes/{id:guid}/entregar")]
    public async Task<IActionResult> DeliverOrder(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new DeliverOrderCommand { OrderId = id, BranchId = branchId });
        return Ok(result);
    }

    [Authorize(Policy = "POS.Orders.Cancel")]
    [HttpPost("ordenes/{id:guid}/cancelar")]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CancelOrderCommand { OrderId = id, BranchId = branchId });

        // Notificar a todas las estaciones de la sucursal que la orden fue cancelada
        await _kitchenHub.Clients
            .Group(KitchenHub.GetBranchGroup(branchId))
            .SendAsync(KitchenHub.OrderCancelledEvent, new { orderId = result.Id });

        return Ok(result);
    }

    [Authorize(Policy = "POS.Orders.Cancel")]
    [HttpPost("ordenes/items/{id:guid}/cancelar")]
    [HttpPost("orden-items/{id:guid}/cancelar")]
    public async Task<IActionResult> CancelOrderItem(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new CancelOrderItemCommand { OrderItemId = id, BranchId = branchId });

        await _kitchenHub.Clients
            .Group(KitchenHub.GetBranchGroup(branchId))
            .SendAsync(KitchenHub.ItemUpdatedEvent, new
            {
                orderItemId = id,
                orderId = result.Id,
                status = "Cancelled",
            });

        return Ok(result);
    }

    [Authorize(Policy = "POS.Orders.Update")]
    [HttpPatch("ordenes/items/{id:guid}/observacion")]
    public async Task<IActionResult> UpdateOrderItemNotes(Guid id, [FromBody] UpdateOrderItemNotesDto dto)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new UpdateOrderItemNotesCommand
        {
            OrderItemId = id,
            BranchId = branchId,
            Notes = dto.Notes,
        });

        if (result.StationId.HasValue)
        {
            await _kitchenHub.Clients
                .Group(KitchenHub.GetStationGroup(result.StationId.Value))
                .SendAsync(KitchenHub.ItemUpdatedEvent, new
                {
                    orderItemId = result.Id,
                    orderId = result.OrderId,
                    status = result.Status,
                    notes = result.Notes ?? string.Empty,
                });
        }

        return Ok(result);
    }

    [Authorize(Policy = "POS.Kitchen.Update")]
    [HttpPatch("ordenes/items/{id:guid}/estado")]
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

        // Notificar a la estación correspondiente el cambio de estado
        if (result.StationId.HasValue)
        {
            await _kitchenHub.Clients
                .Group(KitchenHub.GetStationGroup(result.StationId.Value))
                .SendAsync(KitchenHub.ItemUpdatedEvent, new
                {
                    orderItemId = result.Id,
                    orderId = result.OrderId,
                    status = result.Status,
                });
        }

        return Ok(result);
    }

    // ── Monitor de estación ─────────────────────────────────────────────────

    [Authorize(Policy = "POS.Kitchen.View")]
    [HttpGet("estaciones/{id:guid}/items")]
    public async Task<IActionResult> GetStationItems(Guid id)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        var result = await _mediator.Send(new GetItemsByStationQuery { StationId = id, BranchId = branchId });
        return Ok(result);
    }

    [Authorize(Policy = "POS.Kitchen.View")]
    [HttpGet("estaciones/{id:guid}/completados")]
    public async Task<IActionResult> GetCompletedStationItems(Guid id, [FromQuery] DateOnly? date = null)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized();
        // Ecuador es UTC-5 sin DST; calculamos "hoy" en hora local ecuatoriana
        var todayEcuador = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
        var result = await _mediator.Send(new GetCompletedStationItemsQuery
        {
            StationId = id,
            BranchId = branchId,
            Date = date ?? todayEcuador,
        });
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
