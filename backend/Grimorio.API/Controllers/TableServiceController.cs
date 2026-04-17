using Grimorio.Application.DTOs;
using Grimorio.Application.Features.TableService.Commands;
using Grimorio.Application.Features.TableService.Queries;
using Grimorio.API.Hubs;
using Grimorio.API.Notifications;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.POS;
using Grimorio.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Grimorio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TableServiceController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHubContext<TableServiceHub> _hubContext;
    private readonly IFcmPushNotificationService _fcmPushNotificationService;
    private readonly GrimorioDbContext _dbContext;

    public TableServiceController(
        IMediator mediator,
        IHubContext<TableServiceHub> hubContext,
        IFcmPushNotificationService fcmPushNotificationService,
        GrimorioDbContext dbContext)
    {
        _mediator = mediator;
        _hubContext = hubContext;
        _fcmPushNotificationService = fcmPushNotificationService;
        _dbContext = dbContext;
    }

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables([FromQuery] Guid branchId)
    {
        var result = await _mediator.Send(new GetRestaurantTablesQuery { BranchId = branchId });
        return Ok(result);
    }

    [HttpPost("tables")]
    public async Task<IActionResult> CreateTable([FromBody] CreateRestaurantTableCommand command)
    {
        if (!TryGetBranchId(out var branchId))
            return Unauthorized("BranchId no válido en el token.");

        command.BranchId = branchId;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPut("tables/{id:guid}")]
    public async Task<IActionResult> UpdateTable(Guid id, [FromBody] UpdateRestaurantTableCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("tables/{id:guid}/regenerate-token")]
    public async Task<IActionResult> RegenerateTableToken(Guid id)
    {
        var result = await _mediator.Send(new RegenerateRestaurantTableTokenCommand { Id = id });
        return Ok(result);
    }

    [HttpDelete("tables/{id:guid}")]
    public async Task<IActionResult> DeleteTable(Guid id)
    {
        await _mediator.Send(new DeleteRestaurantTableCommand { Id = id });
        return Ok(new { message = "Mesa eliminada correctamente." });
    }

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

        var normalizedToken = body.Token.Trim();
        var existingToken = await _dbContext.UserPushTokens
            .FirstOrDefaultAsync(t => t.Token == normalizedToken);

        if (existingToken == null)
        {
            _dbContext.UserPushTokens.Add(new UserPushToken
            {
                UserId = userId,
                BranchId = branchId,
                Token = normalizedToken,
                Platform = string.IsNullOrWhiteSpace(body.Platform) ? "android" : body.Platform.Trim().ToLowerInvariant(),
                DeviceId = string.IsNullOrWhiteSpace(body.DeviceId) ? null : body.DeviceId.Trim(),
                LastSeenAt = DateTime.UtcNow,
                IsActive = true,
                CreatedBy = userId,
                UpdatedBy = userId,
            });
        }
        else
        {
            existingToken.UserId = userId;
            existingToken.BranchId = branchId;
            existingToken.Platform = string.IsNullOrWhiteSpace(body.Platform)
                ? existingToken.Platform
                : body.Platform.Trim().ToLowerInvariant();
            existingToken.DeviceId = string.IsNullOrWhiteSpace(body.DeviceId)
                ? existingToken.DeviceId
                : body.DeviceId.Trim();
            existingToken.LastSeenAt = DateTime.UtcNow;
            existingToken.IsActive = true;
            existingToken.IsDeleted = false;
            existingToken.UpdatedAt = DateTime.UtcNow;
            existingToken.UpdatedBy = userId;
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Push token registrado." });
    }

    [HttpDelete("push-token")]
    public async Task<IActionResult> RemovePushToken([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest("Token requerido.");

        if (!TryGetUserId(out var userId))
            return Unauthorized("UserId no válido en el token.");

        var existingToken = await _dbContext.UserPushTokens
            .FirstOrDefaultAsync(t => t.Token == token.Trim());

        if (existingToken != null)
        {
            existingToken.IsActive = false;
            existingToken.UpdatedAt = DateTime.UtcNow;
            existingToken.UpdatedBy = userId;
            await _dbContext.SaveChangesAsync();
        }

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

    private bool TryGetBranchId(out Guid branchId)
    {
        branchId = Guid.Empty;
        var branchClaim = User.FindFirst("BranchId")?.Value;
        return branchClaim != null && Guid.TryParse(branchClaim, out branchId);
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var userClaim = User.FindFirst("UserId")?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        return userClaim != null && Guid.TryParse(userClaim, out userId);
    }

    private string GetUserDisplayName()
    {
        var firstName = User.FindFirst("FirstName")?.Value;
        var lastName = User.FindFirst("LastName")?.Value;
        var email = User.FindFirst("email")?.Value ?? User.Identity?.Name;

        var fullName = $"{firstName} {lastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        return string.IsNullOrWhiteSpace(email) ? "Usuario" : email;
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
