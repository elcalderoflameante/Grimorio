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
    public string PhoneNumberId { get; set; } = string.Empty;
    public string BusinessAccountId { get; set; } = string.Empty;
    public string BusinessPhone { get; set; } = string.Empty;
    public string MetaAppId { get; set; } = string.Empty;
    public string PublicAppUrl { get; set; } = string.Empty;
    public string ErpAppUrl { get; set; } = string.Empty;
    public bool RestrictRecipients { get; set; } = true;
    public bool BusinessAppCoexistenceConfirmed { get; set; }
    public int DailySendLimit { get; set; } = 100;
    public string AccessToken { get; set; } = string.Empty;
    public string AuthenticationTemplate { get; set; } = string.Empty;
    public string TemplateLanguage { get; set; } = "es";
    public string GraphVersion { get; set; } = "v25.0";
    public string[] AllowedTestPhones { get; set; } = [];
    public string CodeHashKey { get; set; } = string.Empty;
    public string WebhookVerifyToken { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string SenderId => Mode == "Production" ? PhoneNumberId : Mode == "Test" ? TestPhoneNumberId : string.Empty;
    public bool IsRestricted => Mode != "Production" || RestrictRecipients;
    public bool CanSendTo(string phone) => !IsRestricted || AllowedTestPhones.Any(p => LoyaltyContact.NormalizePhone(p) == phone);
    public bool Ready => Enabled && MissingConfiguration().Count == 0;
    public List<string> MissingConfiguration() {
        var missing = new List<string>();
        if (Mode is not ("Test" or "Production")) missing.Add("Mode");
        if (PublicBranchId == Guid.Empty) missing.Add("PublicBranchId");
        if (!IsId(SenderId)) missing.Add(Mode == "Production" ? "PhoneNumberId" : "TestPhoneNumberId");
        if (string.IsNullOrWhiteSpace(AccessToken)) missing.Add("AccessToken");
        if (!System.Text.RegularExpressions.Regex.IsMatch(AuthenticationTemplate, "^[a-z0-9_]{1,512}$")) missing.Add("AuthenticationTemplate");
        if (string.IsNullOrWhiteSpace(TemplateLanguage)) missing.Add("TemplateLanguage");
        if (CodeHashKey.Length < 32) missing.Add("CodeHashKey");
        if (DailySendLimit is < 1 or > 10000) missing.Add("DailySendLimit");
        if (IsRestricted && !AllowedTestPhones.Any(p => LoyaltyContact.NormalizePhone(p) != null)) missing.Add("AllowedTestPhones");
        if (!System.Text.RegularExpressions.Regex.IsMatch(GraphVersion, @"^v\d{1,2}\.0$")) missing.Add("GraphVersion");
        if (Mode == "Production") {
            if (!BusinessAppCoexistenceConfirmed) missing.Add("BusinessAppCoexistenceConfirmed");
            if (!IsId(BusinessAccountId)) missing.Add("BusinessAccountId");
            if (LoyaltyContact.NormalizePhone(BusinessPhone) is null) missing.Add("BusinessPhone");
            if (AppSecret.Length < 32) missing.Add("AppSecret");
            if (WebhookVerifyToken.Length < 32) missing.Add("WebhookVerifyToken");
            if (PublicUrl(PublicAppUrl, "/fidelizacion") is null) missing.Add("PublicAppUrl");
            if (PublicUrl(ErpAppUrl, "/api/integrations/whatsapp/webhook") is null) missing.Add("ErpAppUrl");
        }
        return missing;
    }
    public static bool IsId(string id) => id.Length is > 0 and <= 40 && id.All(char.IsAsciiDigit);
    public static string? PublicUrl(string origin, string path) => Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && uri.Scheme == "https" && !uri.IsLoopback && string.IsNullOrEmpty(uri.UserInfo)
        ? new Uri(new Uri(uri.GetLeftPart(UriPartial.Authority)), path).AbsoluteUri : null;
}

public interface ILoyaltyCodeSender
{
    Task<string> Send(string phone, string code, CancellationToken ct);
}

public class WhatsAppLoyaltyCodeSender(HttpClient http, Microsoft.Extensions.Options.IOptions<LoyaltyWhatsAppOptions> options) : ILoyaltyCodeSender
{
    public async Task<string> Send(string phone, string code, CancellationToken ct) {
        var o = options.Value;
        if (!o.Ready || !o.CanSendTo(phone))
            throw new InvalidOperationException("El envio no esta habilitado para este telefono.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(12)); ct = timeout.Token;
        if (o.Mode == "Production") await WhatsAppConnectionProbe.Check(http, o, ct);
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://graph.facebook.com/{o.GraphVersion}/{o.SenderId}/messages");
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
