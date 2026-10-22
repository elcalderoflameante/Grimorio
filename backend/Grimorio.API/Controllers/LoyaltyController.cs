using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Marketing;
using Grimorio.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Grimorio.API.Controllers;

[ApiController, Route("api/marketing/loyalty"), Authorize]
public class LoyaltyController(IMediator mediator) : ControllerBase
{
    private Guid Branch => Guid.TryParse(User.FindFirst(AppConstants.Claims.BranchId)?.Value, out var id) && id != Guid.Empty
        ? id : throw new BadHttpRequestException("La sesion no tiene una sucursal valida.", StatusCodes.Status401Unauthorized);

    [HttpGet("program"), Authorize(Policy = AppConstants.Permissions.MarketingLoyaltyView)]
    public async Task<IActionResult> Program(CancellationToken ct) => Ok(await mediator.Send(new GetLoyaltyProgramQuery(Branch), ct));

    [HttpPut("program"), Authorize(Policy = AppConstants.Permissions.MarketingLoyaltyManage)]
    public async Task<IActionResult> Save(LoyaltyProgramDto dto, CancellationToken ct) {
        try { return Ok(await mediator.Send(new SaveLoyaltyProgramCommand(Branch, dto), ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("members"), Authorize(Policy = AppConstants.Permissions.MarketingLoyaltyView)]
    public async Task<IActionResult> Members([FromQuery] string? search, CancellationToken ct) => Ok(await mediator.Send(new GetLoyaltyMembersQuery(Branch, search), ct));

    [HttpGet("lookup"), Authorize(Policy = AppConstants.Permissions.MarketingLoyaltyUse)]
    public async Task<IActionResult> Lookup([FromQuery] string? search, CancellationToken ct) => Ok((await mediator.Send(new GetLoyaltyMembersQuery(Branch, search), ct))
        .Where(m => m.IsActive).Select(m => new { m.Id, m.CustomerName, m.Phone, m.CardCode, m.IsActive, m.Visits, m.Rewards }));

    [HttpPost("members"), Authorize(Policy = AppConstants.Permissions.MarketingLoyaltyEnroll)]
    public async Task<IActionResult> Enroll(EnrollLoyaltyMemberDto dto, CancellationToken ct) {
        if (dto.CustomerId.HasValue && !CanViewCustomers()) return Forbid();
        try { return Ok(await mediator.Send(new EnrollLoyaltyMemberCommand(Branch, dto), ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("members/{id:guid}"), Authorize(Policy = AppConstants.Permissions.MarketingLoyaltyEnroll)]
    public async Task<IActionResult> UpdateMember(Guid id, LoyaltyMemberProfileDto dto, CancellationToken ct) {
        if (!CanViewCustomers()) {
            var existing = await mediator.Send(new GetLoyaltyMemberQuery(Branch, id), ct);
            if (existing is null) return NotFound();
            if (existing.Member.CustomerId != dto.CustomerId) return Forbid();
        }
        try { return Ok(await mediator.Send(new UpdateLoyaltyMemberCommand(Branch, id, dto), ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    private bool CanViewCustomers() => User.IsInRole(AppConstants.Roles.Admin)
        || User.HasClaim(AppConstants.Claims.Permissions, AppConstants.Permissions.BillingCustomersView);

    [HttpGet("members/{id:guid}"), Authorize(Policy = AppConstants.Permissions.MarketingLoyaltyView)]
    public async Task<IActionResult> Member(Guid id, CancellationToken ct) {
        var result = await mediator.Send(new GetLoyaltyMemberQuery(Branch, id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("summary"), Authorize(Policy = AppConstants.Permissions.MarketingLoyaltyView)]
    public async Task<IActionResult> Summary(CancellationToken ct) => Ok(await mediator.Send(new GetLoyaltySummaryQuery(Branch), ct));

    [HttpGet("card/{token}"), AllowAnonymous, EnableRateLimiting("loyalty-public"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Card(string token, CancellationToken ct) {
        var result = await mediator.Send(new GetPublicLoyaltyCardQuery(token), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
