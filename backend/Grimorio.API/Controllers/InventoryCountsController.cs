using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Inventory;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grimorio.API.Controllers;

/// <summary>Staff counts and supervisor decisions. No endpoint adjusts inventory.</summary>
[ApiController, Route("api/inventory/conteos"), Authorize]
public class InventoryCountsController(IMediator mediator) : ControllerBase
{
    private Guid Branch => Guid.TryParse(User.FindFirst(AppConstants.Claims.BranchId)?.Value, out var id) && id != Guid.Empty
        ? id : throw new BadHttpRequestException("La sesion no tiene sucursal.", 401);

    [HttpGet("configuracion"), Authorize(Policy = AppConstants.Permissions.InventoryCountsManage)]
    public Task<IActionResult> Catalog(CancellationToken ct) => Run(() => mediator.Send(new GetInventoryCountCatalogQuery(Branch), ct));

    [HttpPost("areas"), Authorize(Policy = AppConstants.Permissions.InventoryCountsManage)]
    public Task<IActionResult> CreateArea(InventoryCountAreaDto dto, CancellationToken ct)
    { dto.Id = Guid.Empty; return Run(() => mediator.Send(new SaveInventoryCountAreaCommand(Branch, dto), ct)); }

    [HttpPut("areas/{id:guid}"), Authorize(Policy = AppConstants.Permissions.InventoryCountsManage)]
    public Task<IActionResult> UpdateArea(Guid id, InventoryCountAreaDto dto, CancellationToken ct)
    { dto.Id = id; return Run(() => mediator.Send(new SaveInventoryCountAreaCommand(Branch, dto), ct)); }

    [HttpGet("mis-pendientes"), Authorize(Policy = AppConstants.Permissions.InventoryCountsExecute)]
    public Task<IActionResult> Mine(CancellationToken ct) => Run(() => mediator.Send(new GetMyInventoryCountsQuery(Branch), ct));

    [HttpPost("areas/{id:guid}/iniciar"), Authorize(Policy = AppConstants.Permissions.InventoryCountsExecute)]
    public Task<IActionResult> Start(Guid id, CancellationToken ct) => Run(() => mediator.Send(new StartInventoryCountCommand(Branch, id), ct));

    [HttpGet("mis-conteos/{id:guid}"), Authorize(Policy = AppConstants.Permissions.InventoryCountsExecute)]
    public Task<IActionResult> MineDetail(Guid id, CancellationToken ct) => Run(() => mediator.Send(new GetStaffInventoryCountQuery(Branch, id, true), ct));

    [HttpPut("{id:guid}/lineas/{lineId:guid}"), Authorize(Policy = AppConstants.Permissions.InventoryCountsExecute)]
    public Task<IActionResult> SaveLine(Guid id, Guid lineId, SaveInventoryCountLineDto dto, CancellationToken ct) =>
        Run(() => mediator.Send(new SaveInventoryCountLineCommand(Branch, id, lineId, dto), ct));

    [HttpPost("{id:guid}/enviar"), Authorize(Policy = AppConstants.Permissions.InventoryCountsExecute)]
    public Task<IActionResult> Submit(Guid id, CancellationToken ct) => Run(() => mediator.Send(new SubmitInventoryCountCommand(Branch, id), ct));

    [HttpGet, Authorize(Policy = AppConstants.Permissions.InventoryCountsView)]
    public Task<IActionResult> List([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? status, CancellationToken ct) =>
        Run(() => mediator.Send(new ListStaffInventoryCountsQuery(Branch, from, to, status), ct));

    [HttpGet("{id:guid}"), Authorize(Policy = AppConstants.Permissions.InventoryCountsView)]
    public Task<IActionResult> Detail(Guid id, CancellationToken ct) => Run(() => mediator.Send(new GetStaffInventoryCountQuery(Branch, id, false), ct));

    [HttpPost("{id:guid}/revisar"), Authorize(Policy = AppConstants.Permissions.InventoryCountsReview)]
    public Task<IActionResult> Review(Guid id, ReviewInventoryCountDto dto, CancellationToken ct) => Run(() => mediator.Send(new ReviewInventoryCountCommand(Branch, id, dto), ct));

    private async Task<IActionResult> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
