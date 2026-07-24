using Grimorio.Application.DTOs;
using Grimorio.Application.Features.TableService.Commands;
using Grimorio.Application.Features.TableService.Queries;
using Grimorio.API.Hubs;
using Grimorio.API.Notifications;
using Grimorio.Domain.Entities.POS;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TableServiceController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHubContext<TableServiceHub> _hubContext;
    private readonly IFcmPushNotificationService _fcmPushNotificationService;

    public TableServiceController(
        IMediator mediator,
        IHubContext<TableServiceHub> hubContext,
        IFcmPushNotificationService fcmPushNotificationService)
    {
        _mediator = mediator;
        _hubContext = hubContext;
        _fcmPushNotificationService = fcmPushNotificationService;
    }

    [Authorize(Policy = "POS.Tables.View")]
    [HttpGet("tables")]
    public async Task<IActionResult> GetTables([FromQuery] Guid? branchId)
    {
        var effectiveBranchId = branchId;
        if (!effectiveBranchId.HasValue)
        {
            if (!TryGetBranchId(out var tokenBranchId)) return Unauthorized();
            effectiveBranchId = tokenBranchId;
        }
        var result = await _mediator.Send(new GetRestaurantTablesQuery { BranchId = effectiveBranchId.Value });
        return Ok(result);
    }

    [Authorize(Policy = "POS.Tables.Manage")]
    [HttpPost("tables")]
    public async Task<IActionResult> CreateTable([FromBody] CreateRestaurantTableCommand command)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        command.BranchId = branchId;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [Authorize(Policy = "POS.Tables.Manage")]
    [HttpPut("tables/{id:guid}")]
    public async Task<IActionResult> UpdateTable(Guid id, [FromBody] UpdateRestaurantTableCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [Authorize(Policy = "POS.Tables.Manage")]
    [HttpPost("tables/{id:guid}/regenerate-token")]
    public async Task<IActionResult> RegenerateTableToken(Guid id)
    {
        var result = await _mediator.Send(new RegenerateRestaurantTableTokenCommand { Id = id });
        return Ok(result);
    }

    [Authorize(Policy = "POS.Tables.Manage")]
    [HttpDelete("tables/{id:guid}")]
    public async Task<IActionResult> DeleteTable(Guid id)
    {
        await _mediator.Send(new DeleteRestaurantTableCommand { Id = id });
        return Ok(new { message = "Mesa eliminada correctamente." });
    }

    [Authorize(Policy = "POS.TableRequests.View")]
    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests([FromQuery] TableServiceRequestStatus? status = null)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        var result = await _mediator.Send(new GetTableServiceRequestsQuery
        {
            BranchId = branchId,
            Status = status,
            FromUtc = DateTime.UtcNow.AddHours(-24),
        });

        return Ok(result);
    }

    [Authorize(Policy = "POS.TableRequests.Update")]
    [HttpPost("requests/{id:guid}/take")]
    public async Task<IActionResult> TakeRequest(Guid id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized("UserId no válido en el token.");

        var userName = GetUserDisplayName();

        var result = await _mediator.Send(new TakeTableServiceRequestCommand
        {
            RequestId = id,
            UserId = userId,
            UserName = userName,
        });

        await _hubContext.Clients
            .Group(TableServiceHub.GetBranchGroup(result.BranchId))
            .SendAsync(TableServiceHub.RequestUpdatedEvent, result);

        await _hubContext.Clients
            .Group(TableServiceHub.GetPublicTableGroup(result.RestaurantTableId))
            .SendAsync(TableServiceHub.RequestUpdatedEvent, result);

        return Ok(result);
    }

    [Authorize(Policy = "POS.TableRequests.Update")]
    [HttpPost("requests/{id:guid}/status")]
    public async Task<IActionResult> SetRequestStatus(Guid id, [FromBody] SetStatusBody body)
    {
        var result = await _mediator.Send(new SetTableServiceRequestStatusCommand
        {
            RequestId = id,
            Status = body.Status,
        });

        await _hubContext.Clients
            .Group(TableServiceHub.GetBranchGroup(result.BranchId))
            .SendAsync(TableServiceHub.RequestUpdatedEvent, result);

        await _hubContext.Clients
            .Group(TableServiceHub.GetPublicTableGroup(result.RestaurantTableId))
            .SendAsync(TableServiceHub.RequestUpdatedEvent, result);

        return Ok(result);
    }

    [HttpPost("push-token")]
    public async Task<IActionResult> RegisterPushToken([FromBody] PushTokenBody body)
    {
        if (string.IsNullOrWhiteSpace(body.Token))
            return BadRequest("Token requerido.");

        if (!TryGetUserId(out var userId))
            return Unauthorized("UserId no válido en el token.");

        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        await _mediator.Send(new RegisterPushTokenCommand
        {
            UserId = userId,
            BranchId = branchId,
            Token = body.Token,
            Platform = body.Platform,
            DeviceId = body.DeviceId,
        });

        return Ok(new { message = "Push token registrado." });
    }

    [HttpDelete("push-token")]
    public async Task<IActionResult> RemovePushToken([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest("Token requerido.");

        if (!TryGetUserId(out var userId))
            return Unauthorized("UserId no válido en el token.");

        await _mediator.Send(new DeactivatePushTokenCommand
        {
            UserId = userId,
            Token = token,
        });

        return Ok(new { message = "Push token desactivado." });
    }

    [AllowAnonymous]
    [HttpGet("public/table/{token}")]
    public async Task<IActionResult> GetPublicTableInfo(string token)
    {
        var result = await _mediator.Send(new GetRestaurantTableByTokenQuery { Token = token });
        if (result == null)
            return NotFound(new { message = "Mesa no encontrada." });
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("public/table/{token}/active-request")]
    public async Task<IActionResult> GetPublicActiveRequest(string token)
    {
        var result = await _mediator.Send(new GetActivePublicTableRequestQuery { TableToken = token });
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("public/table/{token}/menu")]
    public async Task<IActionResult> GetPublicTableMenu(string token)
    {
        try
        {
            var result = await _mediator.Send(new GetPublicTableMenuQuery { TableToken = token });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("public/table/{token}/order")]
    public async Task<IActionResult> GetActivePublicTableOrder(string token)
    {
        var result = await _mediator.Send(new GetActivePublicTableOrderQuery { TableToken = token });
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("public/request/{id:guid}")]
    public async Task<IActionResult> GetPublicRequestStatus(Guid id)
    {
        var result = await _mediator.Send(new GetPublicRequestStatusQuery { RequestId = id });
        if (result == null)
            return NotFound(new { message = "Solicitud no encontrada." });
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("public/request")]
    public async Task<IActionResult> CreatePublicRequest([FromBody] PublicCreateTableServiceRequestDto body)
    {
        var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(new PublicCreateTableServiceRequestCommand
        {
            TableToken = body.TableToken,
            Type = body.Type,
            CustomMessage = body.CustomMessage,
            ClientFingerprint = body.ClientFingerprint,
            SourceIp = sourceIp,
        });

        await _hubContext.Clients
            .Group(TableServiceHub.GetBranchGroup(result.BranchId))
            .SendAsync(TableServiceHub.NewRequestEvent, result);

        await _hubContext.Clients
            .Group(TableServiceHub.GetPublicTableGroup(result.RestaurantTableId))
            .SendAsync(TableServiceHub.RequestUpdatedEvent, result);

        await _fcmPushNotificationService.SendNewTableRequestAsync(result);

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("public/order-draft")]
    public async Task<IActionResult> CreatePublicDraftOrder([FromBody] PublicCreateDraftOrderDto body)
    {
        var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        PublicDraftOrderResultDto result;
        try
        {
            result = await _mediator.Send(new PublicCreateDraftOrderCommand
            {
                TableToken = body.TableToken,
                Notes = body.Notes,
                Items = body.Items,
                SourceIp = sourceIp,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        await _hubContext.Clients
            .Group(TableServiceHub.GetBranchGroup(result.Notification.BranchId))
            .SendAsync(TableServiceHub.NewRequestEvent, result.Notification);

        await _fcmPushNotificationService.SendNewTableRequestAsync(result.Notification);

        return Ok(result);
    }

    private bool TryGetBranchId(out Guid branchId)
    {
        branchId = Guid.Empty;
        var branchClaim = User.FindFirst(AppConstants.Claims.BranchId)?.Value;
        return branchClaim != null && Guid.TryParse(branchClaim, out branchId);
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var userClaim = User.FindFirst(AppConstants.Claims.UserId)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst(AppConstants.Claims.NameIdentifier)?.Value;

        return userClaim != null && Guid.TryParse(userClaim, out userId);
    }

    private string GetUserDisplayName()
    {
        var firstName = User.FindFirst(AppConstants.Claims.FirstName)?.Value;
        var lastName = User.FindFirst(AppConstants.Claims.LastName)?.Value;
        var email = User.FindFirst(AppConstants.Claims.Email)?.Value ?? User.Identity?.Name;

        var fullName = $"{firstName} {lastName}".Trim();
        return !string.IsNullOrWhiteSpace(fullName)
            ? fullName
            : (string.IsNullOrWhiteSpace(email) ? "Usuario" : email);
    }

    public class SetStatusBody
    {
        public TableServiceRequestStatus Status { get; set; }
    }

    public class PushTokenBody
    {
        public string Token { get; set; } = string.Empty;
        public string Platform { get; set; } = "android";
        public string? DeviceId { get; set; }
    }
}
