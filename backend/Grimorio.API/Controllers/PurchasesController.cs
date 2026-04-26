using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Purchases.Commands;
using Grimorio.Application.Features.Purchases.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/purchases")]
[Authorize]
public class PurchasesController : ControllerBase
{
    private readonly IMediator _mediator;
    private Guid BranchId => Guid.Parse(User.FindFirst("branchId")?.Value ?? Guid.Empty.ToString());

    public PurchasesController(IMediator mediator) => _mediator = mediator;

    // â”€â”€ Suppliers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpGet("proveedores")]
    public async Task<IActionResult> GetSuppliers([FromQuery] bool? activeOnly, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSuppliersQuery { BranchId = BranchId, ActiveOnly = activeOnly }, ct);
        return Ok(result);
    }

    [HttpPost("proveedores")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateSupplierCommand
        {
            BranchId = BranchId, Name = dto.Name, TaxId = dto.TaxId,
            Phone = dto.Phone, Email = dto.Email, Address = dto.Address, ContactName = dto.ContactName,
        }, ct);
        return Ok(result);
    }

    [HttpPut("proveedores/{id:guid}")]
    public async Task<IActionResult> UpdateSupplier(Guid id, [FromBody] UpdateSupplierDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateSupplierCommand
        {
            Id = id, BranchId = BranchId, Name = dto.Name, TaxId = dto.TaxId,
            Phone = dto.Phone, Email = dto.Email, Address = dto.Address,
            ContactName = dto.ContactName, IsActive = dto.IsActive,
        }, ct);
        return Ok(result);
    }

    [HttpDelete("proveedores/{id:guid}")]
    public async Task<IActionResult> DeleteSupplier(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteSupplierCommand { Id = id, BranchId = BranchId }, ct);
        return NoContent();
    }

    // â”€â”€ Ã“rdenes de compra â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpGet("ordenes")]
    public async Task<IActionResult> GetOrders([FromQuery] string? status, [FromQuery] Guid? supplierId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPurchaseOrdersQuery { BranchId = BranchId, Status = status, SupplierId = supplierId }, ct);
        return Ok(result);
    }

    [HttpGet("ordenes/{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPurchaseOrderDetailQuery { Id = id, BranchId = BranchId }, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("ordenes")]
    public async Task<IActionResult> CreateOrder([FromBody] CreatePurchaseOrderDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreatePurchaseOrderCommand
        {
            BranchId = BranchId, SupplierId = dto.SupplierId, ExpectedAt = dto.ExpectedAt,
            Notes = dto.Notes, DestinationWarehouseId = dto.DestinationWarehouseId, Items = dto.Items,
        }, ct);
        return Ok(result);
    }

    [HttpPut("ordenes/{id:guid}")]
    public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] UpdatePurchaseOrderDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdatePurchaseOrderCommand
        {
            Id = id, BranchId = BranchId, SupplierId = dto.SupplierId, ExpectedAt = dto.ExpectedAt,
            Notes = dto.Notes, DestinationWarehouseId = dto.DestinationWarehouseId, Items = dto.Items,
        }, ct);
        return Ok(result);
    }

    [HttpPost("ordenes/{id:guid}/enviar")]
    public async Task<IActionResult> SendOrder(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new SendPurchaseOrderCommand { Id = id, BranchId = BranchId }, ct);
        return Ok(result);
    }

    [HttpPost("ordenes/{id:guid}/recibir")]
    public async Task<IActionResult> ReceiveOrder(Guid id, [FromBody] ReceivePurchaseOrderDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new ReceivePurchaseOrderCommand
        {
            Id = id, BranchId = BranchId, WarehouseId = dto.WarehouseId, Items = dto.Items,
        }, ct);
        return Ok(result);
    }

    [HttpPost("ordenes/{id:guid}/cancelar")]
    public async Task<IActionResult> CancelOrder(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelPurchaseOrderCommand { Id = id, BranchId = BranchId }, ct);
        return Ok(result);
    }

    [HttpDelete("ordenes/{id:guid}")]
    public async Task<IActionResult> DeleteOrder(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeletePurchaseOrderCommand { Id = id, BranchId = BranchId }, ct);
        return NoContent();
    }
}

