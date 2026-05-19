using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Billing;
using Grimorio.Infrastructure.Persistence;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Text;
using System.Text.Json;

namespace Grimorio.Infrastructure.Services.Email;

public class SmtpEmailService : IEmailService
{
    private readonly GrimorioDbContext _db;
    private readonly IDataProtector _protector;
    private readonly ILogger<SmtpEmailService> _log;

    public SmtpEmailService(
        GrimorioDbContext db,
        IDataProtectionProvider dp,
        ILogger<SmtpEmailService> log)
    {
        _db = db;
        _protector = dp.CreateProtector("SmtpPassword");
        _log = log;
    }

    public async Task SendInvoiceAsync(
        Guid branchId,
        string toEmail,
        string toName,
        string numeroFactura,
        string razonSocial,
        decimal importeTotal,
        byte[] ridePdf,
        string? signedXml = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return;

        var (config, password) = await LoadConfigAsync(branchId, ct);
        if (config == null || !config.IsActive) return;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        var emailTemplate = await LoadInvoiceTemplateAsync(branchId, ct);
        message.Subject = (emailTemplate?.EmailSubject ?? "Factura Electrónica {numeroFactura} — {razonSocial}")
            .Replace("{numeroFactura}", numeroFactura)
            .Replace("{razonSocial}", razonSocial);
        var safeNumero = numeroFactura.Replace("-", "");
        var body = new BodyBuilder
        {
            HtmlBody = BuildTemplatedHtml(emailTemplate, toName, numeroFactura, razonSocial, importeTotal),
        };
        body.Attachments.Add($"RIDE-{safeNumero}.pdf", ridePdf, new ContentType("application", "pdf"));
        if (!string.IsNullOrWhiteSpace(signedXml))
        {
            var xmlBytes = System.Text.Encoding.UTF8.GetBytes(signedXml);
            body.Attachments.Add($"Factura-{safeNumero}.xml", xmlBytes, new ContentType("application", "xml"));
        }
        message.Body = body.ToMessageBody();

        await SendAsync(config.Host, config.Port, config.EnableSsl, config.Username, password, message, ct);

        _log.LogInformation("Factura {NumeroFactura} enviada por correo a {Email}", numeroFactura, toEmail);
    }

    public async Task SendTestEmailAsync(Guid branchId, string toEmail, CancellationToken ct = default)
    {
        var (config, password) = await LoadConfigAsync(branchId, ct);
        if (config == null)
            throw new InvalidOperationException("No hay configuración SMTP para esta sucursal.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Prueba de conexión SMTP — Grimorio";
        message.Body = new TextPart("html")
        {
            Text = "<p>Correo de prueba enviado correctamente desde <strong>Grimorio</strong>.</p>" +
                   "<p>La configuración SMTP está funcionando.</p>"
        };

        await SendAsync(config.Host, config.Port, config.EnableSsl, config.Username, password, message, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(Domain.Entities.Billing.SmtpConfig? config, string password)> LoadConfigAsync(
        Guid branchId, CancellationToken ct)
    {
        var config = await _db.SmtpConfigs
            .FirstOrDefaultAsync(x => x.BranchId == branchId && !x.IsDeleted, ct);

        if (config == null) return (null, string.Empty);

        string password;
        try { password = _protector.Unprotect(config.PasswordEncrypted); }
        catch { password = string.Empty; }

        return (config, password);
    }

    private static async Task SendAsync(
        string host, int port, bool enableSsl,
        string username, string password,
        MimeMessage message, CancellationToken ct)
    {
        using var client = new SmtpClient();

        var secureOption = enableSsl
            ? SecureSocketOptions.StartTlsWhenAvailable
            : SecureSocketOptions.None;

        await client.ConnectAsync(host, port, secureOption, ct);

        if (!string.IsNullOrWhiteSpace(username))
            await client.AuthenticateAsync(username, password, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }

    private async Task<InvoiceTemplate?> LoadInvoiceTemplateAsync(Guid branchId, CancellationToken ct)
    {
        return await _db.InvoiceTemplates
            .FirstOrDefaultAsync(x => x.BranchId == branchId && !x.IsDeleted, ct);
    }

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static string BuildTemplatedHtml(
        InvoiceTemplate? template,
        string toName, string numeroFactura, string razonSocial, decimal importeTotal)
    {
        List<EmailBlockDto>? blocks = null;
        if (template != null && !string.IsNullOrWhiteSpace(template.EmailBlocksJson))
        {
            try { blocks = JsonSerializer.Deserialize<List<EmailBlockDto>>(template.EmailBlocksJson, JsonOpts); }
            catch { /* usa defaults */ }
        }

        if (blocks == null || blocks.Count == 0)
            return BuildDefaultHtml(toName, numeroFactura, razonSocial, importeTotal);

        var sb = new StringBuilder();
        sb.Append("""<!DOCTYPE html><html lang="es"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0"></head>""");
        sb.Append("""<body style="margin:0;padding:0;background:#f5f5f5;font-family:Arial,sans-serif;">""");
        sb.Append("""<table width="100%" cellpadding="0" cellspacing="0" style="background:#f5f5f5;padding:32px 0;"><tr><td align="center">""");
        sb.Append("""<table width="600" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08);">""");

        foreach (var block in blocks.Where(b => b.Visible))
        {
            switch (block.Type)
            {
                case "header":
                    var bg = string.IsNullOrWhiteSpace(block.BgColor) ? "#1677ff" : block.BgColor;
                    var title = block.Title ?? razonSocial;
                    var subtitle = block.Subtitle ?? "Factura Electrónica";
                    sb.Append($"""<tr><td style="background:{bg};padding:24px 32px;"><h1 style="margin:0;color:#fff;font-size:22px;">{System.Net.WebUtility.HtmlEncode(title)}</h1><p style="margin:4px 0 0;color:rgba(255,255,255,.75);font-size:13px;">{System.Net.WebUtility.HtmlEncode(subtitle)}</p></td></tr>""");
                    break;
                case "greeting":
                    var greetText = (block.Text ?? "Estimado/a {nombreCliente},")
                        .Replace("{nombreCliente}", toName)
                        .Replace("{razonSocial}", razonSocial);
                    sb.Append($"""<tr><td style="padding:24px 32px 0;"><p style="margin:0;color:#333;font-size:15px;">{System.Net.WebUtility.HtmlEncode(greetText)}</p></td></tr>""");
                    break;
                case "message":
                    var msgText = (block.Text ?? "")
                        .Replace("{nombreCliente}", toName)
                        .Replace("{numeroFactura}", numeroFactura)
                        .Replace("{razonSocial}", razonSocial);
                    sb.Append($"""<tr><td style="padding:12px 32px;"><p style="margin:0;color:#555;font-size:14px;line-height:1.6;">{System.Net.WebUtility.HtmlEncode(msgText)}</p></td></tr>""");
                    break;
                case "invoice_summary":
                    sb.Append($"""<tr><td style="padding:12px 32px;"><table width="100%" cellpadding="12" cellspacing="0" style="background:#f9f9f9;border-radius:6px;"><tr><td style="color:#888;font-size:13px;">Número de factura</td><td style="color:#222;font-size:14px;font-weight:bold;text-align:right;">{System.Net.WebUtility.HtmlEncode(numeroFactura)}</td></tr><tr style="border-top:1px solid #eee;"><td style="color:#888;font-size:13px;">Importe total</td><td style="font-size:16px;font-weight:bold;text-align:right;color:#1677ff;">${importeTotal:F2}</td></tr></table></td></tr>""");
                    break;
                case "legal_note":
                    var legal = (block.Text ?? "Este documento tiene validez legal ante el SRI del Ecuador.")
                        .Replace("{nombreCliente}", toName);
                    sb.Append($"""<tr><td style="padding:8px 32px;"><p style="margin:0;color:#888;font-size:12px;line-height:1.6;">{System.Net.WebUtility.HtmlEncode(legal)}</p></td></tr>""");
                    break;
                case "footer":
                    var footerText = block.Text ?? "Generado por Grimorio";
                    sb.Append($"""<tr><td style="background:#f9f9f9;padding:16px 32px;text-align:center;"><p style="margin:0;color:#bbb;font-size:11px;">{System.Net.WebUtility.HtmlEncode(footerText)}</p></td></tr>""");
                    break;
            }
        }

        sb.Append("</table></td></tr></table></body></html>");
        return sb.ToString();
    }

    private static string BuildDefaultHtml(
        string toName, string numeroFactura, string razonSocial, decimal importeTotal)
    {
        return $"""
        <!DOCTYPE html><html lang="es">
        <head><meta charset="UTF-8"></head>
        <body style="margin:0;padding:0;background:#f5f5f5;font-family:Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f5f5f5;padding:32px 0;">
            <tr><td align="center">
              <table width="600" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08);">
                <tr><td style="background:#1677ff;padding:24px 32px;">
                  <h1 style="margin:0;color:#fff;font-size:22px;">{System.Net.WebUtility.HtmlEncode(razonSocial)}</h1>
                  <p style="margin:4px 0 0;color:#d0e8ff;font-size:13px;">Factura Electrónica</p>
                </td></tr>
                <tr><td style="padding:32px;">
                  <p style="margin:0 0 16px;color:#333;font-size:15px;">Estimado/a <strong>{System.Net.WebUtility.HtmlEncode(toName)}</strong>,</p>
                  <p style="margin:0 0 24px;color:#555;font-size:14px;line-height:1.6;">Adjunto encontrará el RIDE de su factura autorizada por el SRI Ecuador.</p>
                  <table width="100%" cellpadding="12" cellspacing="0" style="background:#f9f9f9;border-radius:6px;margin-bottom:24px;">
                    <tr><td style="color:#888;font-size:13px;">Número de factura</td><td style="color:#222;font-size:14px;font-weight:bold;text-align:right;">{System.Net.WebUtility.HtmlEncode(numeroFactura)}</td></tr>
                    <tr style="border-top:1px solid #eee;"><td style="color:#888;font-size:13px;">Importe total</td><td style="color:#1677ff;font-size:16px;font-weight:bold;text-align:right;">${importeTotal:F2}</td></tr>
                  </table>
                  <p style="margin:0;color:#888;font-size:12px;line-height:1.6;">Este documento tiene validez legal ante el SRI del Ecuador.</p>
                </td></tr>
                <tr><td style="background:#f9f9f9;padding:16px 32px;text-align:center;">
                  <p style="margin:0;color:#bbb;font-size:11px;">Generado por Grimorio</p>
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body></html>
        """;
    }
}
