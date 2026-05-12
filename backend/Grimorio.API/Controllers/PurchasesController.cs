using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Purchases.Commands;
using Grimorio.Application.Features.Purchases.Queries;
using Grimorio.SharedKernel.Constants;
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

    public PurchasesController(IMediator mediator) => _mediator = mediator;

    // ── Suppliers ──────────────────────────────────────────────────────────────

    [HttpGet("proveedores")]
    public async Task<IActionResult> GetSuppliers([FromQuery] bool? activeOnly, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new GetSuppliersQuery { BranchId = branchId, ActiveOnly = activeOnly }, ct);
        return Ok(result);
    }

    [HttpPost("proveedores")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDto dto, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new CreateSupplierCommand
        {
            BranchId = branchId, Name = dto.Name, TaxId = dto.TaxId,
            Phone = dto.Phone, Email = dto.Email, Address = dto.Address,
            ContactName = dto.ContactName,
        }, ct);
        return Ok(result);
    }

    [HttpPut("proveedores/{id:guid}")]
    public async Task<IActionResult> UpdateSupplier(Guid id, [FromBody] UpdateSupplierDto dto, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new UpdateSupplierCommand
        {
            Id = id, BranchId = branchId, Name = dto.Name, TaxId = dto.TaxId,
            Phone = dto.Phone, Email = dto.Email, Address = dto.Address,
            ContactName = dto.ContactName, IsActive = dto.IsActive,
        }, ct);
        return Ok(result);
    }

    [HttpDelete("proveedores/{id:guid}")]
    public async Task<IActionResult> DeleteSupplier(Guid id, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        await _mediator.Send(new DeleteSupplierCommand { Id = id, BranchId = branchId }, ct);
        return NoContent();
    }

    // ── Compras directas ───────────────────────────────────────────────────────

    [HttpGet("compras")]
    public async Task<IActionResult> GetPurchases(
        [FromQuery] string? status, [FromQuery] Guid? supplierId,
        [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo,
        CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new GetPurchasesQuery
        {
            BranchId = branchId, Status = status, SupplierId = supplierId,
            DateFrom = dateFrom, DateTo = dateTo,
        }, ct);
        return Ok(result);
    }

    [HttpGet("compras/{id:guid}")]
    public async Task<IActionResult> GetPurchase(Guid id, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new GetPurchaseDetailQuery { Id = id, BranchId = branchId }, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("compras")]
    public async Task<IActionResult> CreatePurchase([FromBody] CreatePurchaseDto dto, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new CreatePurchaseCommand
        {
            BranchId = branchId,
            DocumentType = dto.DocumentType,
            DocumentNumber = dto.DocumentNumber,
            DocumentDate = dto.DocumentDate,
            SupplierId = dto.SupplierId,
            Notes = dto.Notes,
            DestinationWarehouseId = dto.DestinationWarehouseId,
            Items = dto.Items,
        }, ct);
        return Ok(result);
    }

    [HttpPut("compras/{id:guid}")]
    public async Task<IActionResult> UpdatePurchase(Guid id, [FromBody] UpdatePurchaseDto dto, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new UpdatePurchaseCommand
        {
            Id = id, BranchId = branchId,
            DocumentType = dto.DocumentType,
            DocumentNumber = dto.DocumentNumber,
            DocumentDate = dto.DocumentDate,
            SupplierId = dto.SupplierId,
            Notes = dto.Notes,
            DestinationWarehouseId = dto.DestinationWarehouseId,
            Items = dto.Items,
        }, ct);
        return Ok(result);
    }

    [HttpPost("compras/{id:guid}/anular")]
    public async Task<IActionResult> AnularPurchase(Guid id, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        var result = await _mediator.Send(new AnularPurchaseCommand { Id = id, BranchId = branchId }, ct);
        return Ok(result);
    }

    [HttpDelete("compras/{id:guid}")]
    public async Task<IActionResult> DeletePurchase(Guid id, CancellationToken ct)
    {
        if (!TryGetBranchId(out var branchId)) return Unauthorized("BranchId no valido en el token.");

        await _mediator.Send(new DeletePurchaseCommand { Id = id, BranchId = branchId }, ct);
        return NoContent();
    }

    private bool TryGetBranchId(out Guid branchId)
    {
        var claim = User.FindFirst(AppConstants.Claims.BranchId)?.Value;
        return Guid.TryParse(claim, out branchId) && branchId != Guid.Empty;
    }
}
