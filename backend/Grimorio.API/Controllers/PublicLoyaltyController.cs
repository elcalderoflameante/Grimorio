using System.Text.Json;
using Grimorio.Application.DTOs;
using Grimorio.Infrastructure.Features.Marketing;
using Grimorio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Grimorio.API.Controllers;

[ApiController, Route("api/marketing/loyalty/public"), AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting("loyalty-enrollment")]
public class PublicLoyaltyController(PublicLoyaltyService service) : ControllerBase
{
    [HttpGet("program")]
    public async Task<IActionResult> Info(CancellationToken ct) => Ok(await service.Info(ct));

    [HttpPost("code"), RequestSizeLimit(4096)]
    public async Task<IActionResult> RequestCode(RequestLoyaltyCodeDto dto, CancellationToken ct) {
        try { return Ok(await service.Request(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("verify"), RequestSizeLimit(4096)]
    public async Task<IActionResult> Verify(VerifyLoyaltyCodeDto dto, CancellationToken ct) {
        try { return Ok(await service.Verify(dto, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController, Route("api/integrations/whatsapp/webhook"), AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class WhatsAppWebhookController(IOptions<LoyaltyWhatsAppOptions> options, GrimorioDbContext db) : ControllerBase
{
    [HttpGet]
    public IActionResult Challenge([FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token, [FromQuery(Name = "hub.challenge")] string? challenge) {
        var expected = options.Value.WebhookVerifyToken;
        return mode == "subscribe" && expected.Length >= 32 && token is not null && challenge is { Length: <= 256 }
            && LoyaltyCodeSecurity.Equal(expected, token) ? Content(challenge, "text/plain") : StatusCode(403);
    }

    [HttpPost, RequestSizeLimit(262144)]
    public async Task<IActionResult> Receive(CancellationToken ct) {
        using var buffer = new MemoryStream(); await Request.Body.CopyToAsync(buffer, ct);
        var body = buffer.ToArray();
        if (!LoyaltyCodeSecurity.ValidSignature(body, Request.Headers["X-Hub-Signature-256"].ToString(), options.Value.AppSecret))
            return StatusCode(403);
        try {
            using var payload = JsonDocument.Parse(body);
            var root = payload.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("object", out var kind)
                || kind.GetString() != "whatsapp_business_account" || !root.TryGetProperty("entry", out var entries)
                || entries.ValueKind != JsonValueKind.Array) return BadRequest();
            foreach (var entry in entries.EnumerateArray()) {
                if (!entry.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array) continue;
                foreach (var change in changes.EnumerateArray()) {
                    if (!change.TryGetProperty("value", out var value) || !value.TryGetProperty("metadata", out var metadata)
                        || !metadata.TryGetProperty("phone_number_id", out var phoneId)
                        || phoneId.GetString() != options.Value.SenderId
                        || !value.TryGetProperty("statuses", out var statuses) || statuses.ValueKind != JsonValueKind.Array) continue;
                    foreach (var status in statuses.EnumerateArray()) {
                        if (!status.TryGetProperty("id", out var id) || !status.TryGetProperty("status", out var state)) continue;
                        var messageId = id.GetString(); var delivery = state.GetString();
                        if (string.IsNullOrEmpty(messageId) || messageId.Length > 512
                            || delivery is not ("sent" or "delivered" or "read" or "failed")) continue;
                        // Atomic monotonic updates tolerate duplicate/out-of-order notifications.
                        var query = db.LoyaltyPhoneChallenges.Where(c => c.BranchId == options.Value.PublicBranchId && c.MessageId == messageId);
                        query = delivery switch {
                            "read" => query,
                            "delivered" => query.Where(c => c.DeliveryStatus != "read"),
                            "failed" => query.Where(c => c.DeliveryStatus != "read" && c.DeliveryStatus != "delivered"),
                            _ => query.Where(c => c.DeliveryStatus == "accepted"),
                        };
                        await query.ExecuteUpdateAsync(s => s.SetProperty(c => c.DeliveryStatus, delivery), ct);
                    }
                }
            }
            return Ok();
        } catch (Exception ex) when (ex is JsonException or InvalidOperationException) { return BadRequest(); }
    }
}
