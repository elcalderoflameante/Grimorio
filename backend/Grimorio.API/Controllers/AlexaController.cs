using System.Security.Cryptography;
using System.Text;
using Grimorio.API.Hubs;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.POS.Commands;
using Grimorio.Application.Features.POS.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/alexa")]
public class AlexaController : ControllerBase
{
    private const string IntegrationKeyHeader = "X-Grimorio-Alexa-Key";
    private readonly IConfiguration _configuration;
    private readonly IHubContext<KitchenHub> _kitchenHub;
    private readonly IMediator _mediator;

    public AlexaController(
        IConfiguration configuration,
        IHubContext<KitchenHub> kitchenHub,
        IMediator mediator)
    {
        _configuration = configuration;
        _kitchenHub = kitchenHub;
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpPost("kitchen-command")]
    public async Task<IActionResult> ProcessKitchenCommand([FromBody] AlexaKitchenCommandDto dto)
    {
        if (!IsAuthorizedIntegrationRequest())
            return Unauthorized(new { message = "Alexa integration key invalida." });

        if (dto.BranchId == Guid.Empty)
            return BadRequest(new { message = "BranchId es requerido." });

        var result = await _mediator.Send(new ProcessAlexaKitchenCommand
        {
            BranchId = dto.BranchId,
            RawText = dto.RawText,
            Action = dto.Action,
            TableCode = dto.TableCode,
            OrderNumber = dto.OrderNumber,
            ItemText = dto.ItemText,
            AllItems = dto.AllItems,
        });

        if (result.Success)
            await NotifyKitchenAsync(result.Items);

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("order-repeat")]
    public async Task<IActionResult> RepeatOrder([FromBody] AlexaOrderRepeatRequestDto dto)
    {
        if (!IsAuthorizedIntegrationRequest())
            return Unauthorized(new { message = "Alexa integration key invalida." });

        if (dto.BranchId == Guid.Empty)
            return BadRequest(new { message = "BranchId es requerido." });

        var result = await _mediator.Send(new GetAlexaOrderRepeatQuery
        {
            BranchId = dto.BranchId,
            TableCode = dto.TableCode,
            OrderNumber = dto.OrderNumber,
            StationText = dto.StationText,
            ExcludeStationText = dto.ExcludeStationText,
        });

        return Ok(result);
    }

    private bool IsAuthorizedIntegrationRequest()
    {
        var expectedKey = _configuration["Alexa:KitchenCommandKey"];
        if (string.IsNullOrWhiteSpace(expectedKey)) return false;

        var providedKey = Request.Headers[IntegrationKeyHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedKey)) return false;

        var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);
        var providedBytes = Encoding.UTF8.GetBytes(providedKey);
        return expectedBytes.Length == providedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    private async Task NotifyKitchenAsync(IEnumerable<OrderItemDto> items)
    {
        foreach (var item in items.Where(i => i.StationId.HasValue))
        {
            await _kitchenHub.Clients
                .Group(KitchenHub.GetStationGroup(item.StationId!.Value))
                .SendAsync(KitchenHub.ItemUpdatedEvent, new
                {
                    orderItemId = item.Id,
                    orderId = item.OrderId,
                    status = item.Status,
                });
        }
    }
}
