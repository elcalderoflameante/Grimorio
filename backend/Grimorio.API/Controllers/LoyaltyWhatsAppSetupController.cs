using Grimorio.Infrastructure.Features.Marketing;
using Grimorio.Infrastructure.Persistence;
using Grimorio.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Grimorio.API.Controllers;

[ApiController, Route("api/marketing/loyalty/whatsapp"), Authorize(Policy = AppConstants.Permissions.MarketingLoyaltyManage)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class LoyaltyWhatsAppSetupController(IOptions<LoyaltyWhatsAppOptions> options, GrimorioDbContext db, IHttpClientFactory clients) : ControllerBase
{
    private bool WrongBranch => options.Value.PublicBranchId != Guid.Empty
        && User.FindFirst(AppConstants.Claims.BranchId)?.Value != options.Value.PublicBranchId.ToString();

    [HttpGet("setup")]
    public async Task<IActionResult> Setup(CancellationToken ct) {
        if (WrongBranch) return Forbid();
        var o = options.Value;
        var missing = o.MissingConfiguration();
        var programActive = await db.LoyaltyPrograms.AnyAsync(p => p.BranchId == o.PublicBranchId && p.IsActive, ct);
        return Ok(new {
            o.Mode, o.Enabled, configured = missing.Count == 0, ready = o.Ready && programActive, programActive,
            restricted = o.IsRestricted, o.DailySendLimit, o.BusinessPhone, o.AuthenticationTemplate, o.TemplateLanguage,
            o.BusinessAppCoexistenceConfirmed, missing,
            canCheckConnection = o.Mode == "Production" && missing.All(k => k == "BusinessAppCoexistenceConfirmed"),
            webhookUrl = LoyaltyWhatsAppOptions.PublicUrl(o.ErpAppUrl, "/api/integrations/whatsapp/webhook"),
            enrollmentUrl = LoyaltyWhatsAppOptions.PublicUrl(o.PublicAppUrl, "/fidelizacion"),
            privacyUrl = LoyaltyWhatsAppOptions.PublicUrl(o.PublicAppUrl, "/fidelizacion/privacidad"),
            deletionUrl = LoyaltyWhatsAppOptions.PublicUrl(o.PublicAppUrl, "/fidelizacion/eliminar-datos"),
            developerUrl = LoyaltyWhatsAppOptions.IsId(o.MetaAppId) ? $"https://developers.facebook.com/apps/{o.MetaAppId}/dashboard/" : "https://developers.facebook.com/apps/",
            managerUrl = "https://business.facebook.com/wa/manage/home/",
        });
    }

    [HttpPost("check"), EnableRateLimiting("loyalty-enrollment")]
    public async Task<IActionResult> Check(CancellationToken ct) {
        if (WrongBranch) return Forbid();
        try {
            using var http = clients.CreateClient(); http.Timeout = TimeSpan.FromSeconds(12);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(15));
            var result = await WhatsAppConnectionProbe.Check(http, options.Value, timeout.Token);
            return Ok(new { success = true, result.Phone, result.Name, result.Template,
                message = "Meta confirma el numero esperado y la plantilla aprobada. No se envio ningun mensaje. Confirma aparte coexistencia, publicacion y pagos." });
        } catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException) {
            return BadRequest(new { message = "No se pudo completar la consulta a Meta. No se enviaron mensajes ni se modifico el numero." });
        }
    }
}
