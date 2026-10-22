using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Grimorio.Infrastructure.Features.Marketing;

public class LoyaltyWhatsAppOptions
{
    public bool Enabled { get; set; }
    public string Mode { get; set; } = "Test";
    public Guid PublicBranchId { get; set; }
    public string TestPhoneNumberId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string AuthenticationTemplate { get; set; } = string.Empty;
    public string TemplateLanguage { get; set; } = "es";
    public string GraphVersion { get; set; } = "v25.0";
    public string[] AllowedTestPhones { get; set; } = [];
    public string CodeHashKey { get; set; } = string.Empty;
    public string WebhookVerifyToken { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public bool Ready => Enabled && Mode == "Test" && PublicBranchId != Guid.Empty
        && TestPhoneNumberId.Length > 0 && TestPhoneNumberId.All(char.IsAsciiDigit)
        && !string.IsNullOrWhiteSpace(AccessToken) && !string.IsNullOrWhiteSpace(AuthenticationTemplate)
        && CodeHashKey.Length >= 32 && AllowedTestPhones.Any(p => LoyaltyContact.NormalizePhone(p) != null)
        && System.Text.RegularExpressions.Regex.IsMatch(GraphVersion, @"^v\d{1,2}\.0$");
}

public interface ILoyaltyCodeSender
{
    Task<string> Send(string phone, string code, CancellationToken ct);
}

public class WhatsAppLoyaltyCodeSender(HttpClient http, Microsoft.Extensions.Options.IOptions<LoyaltyWhatsAppOptions> options) : ILoyaltyCodeSender
{
    public async Task<string> Send(string phone, string code, CancellationToken ct) {
        var o = options.Value;
        if (!o.Ready || !o.AllowedTestPhones.Any(p => LoyaltyContact.NormalizePhone(p) == phone))
            throw new InvalidOperationException("El envio de prueba no esta habilitado para este telefono.");
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://graph.facebook.com/{o.GraphVersion}/{o.TestPhoneNumberId}/messages");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", o.AccessToken);
        request.Content = JsonContent.Create(new {
            messaging_product = "whatsapp", to = phone.TrimStart('+'), type = "template",
            template = new { name = o.AuthenticationTemplate, language = new { code = o.TemplateLanguage },
                components = new object[] {
                    new { type = "body", parameters = new[] { new { type = "text", text = code } } },
                    new { type = "button", sub_type = "url", index = "0", parameters = new[] { new { type = "text", text = code } } },
                } },
        });
        using var response = await http.SendAsync(request, ct);
        // Provider errors can contain personal data and credentials; never return or log their body.
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("No pudimos enviar el codigo por WhatsApp. Intenta mas tarde.");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (json.RootElement.ValueKind != JsonValueKind.Object || !json.RootElement.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array
            || messages.GetArrayLength() == 0 || messages[0].ValueKind != JsonValueKind.Object || !messages[0].TryGetProperty("id", out var messageId)
            || messageId.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException("WhatsApp no confirmo el envio.");
        var id = messageId.GetString();
        return !string.IsNullOrEmpty(id) && id.Length <= 512 ? id : throw new InvalidOperationException("WhatsApp no confirmo el envio.");
    }
}

public static class LoyaltyCodeSecurity
{
    public static string Hash(Guid id, string code, string key) => Convert.ToHexString(
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes($"{id:N}:{code}")));

    public static bool Equal(string expected, string supplied) => CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(supplied));

    public static bool ValidSignature(byte[] body, string signature, string appSecret) =>
        !string.IsNullOrEmpty(appSecret) && Equal("sha256=" + Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), body)).ToLowerInvariant(), signature);
}
