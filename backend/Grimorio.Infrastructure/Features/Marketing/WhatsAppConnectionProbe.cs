using System.Net.Http.Headers;
using System.Text.Json;

namespace Grimorio.Infrastructure.Features.Marketing;

public record WhatsAppConnectionResult(string Phone, string Name, string Template);

public static class WhatsAppConnectionProbe
{
    // Read-only checks: this never registers, migrates, subscribes, or sends a message.
    public static async Task<WhatsAppConnectionResult> Check(HttpClient http, LoyaltyWhatsAppOptions settings, CancellationToken ct) {
        if (settings.MissingConfiguration().Any(key => key != "BusinessAppCoexistenceConfirmed") || settings.Mode != "Production")
            throw new InvalidOperationException("Completa la configuracion de produccion antes de comprobar Meta.");
        using var numbers = await Read(http, settings, $"{settings.BusinessAccountId}/phone_numbers?fields=id,display_phone_number,verified_name,code_verification_status&limit=100", ct);
        var phone = Data(numbers).EnumerateArray().FirstOrDefault(p => Text(p, "id") == settings.PhoneNumberId);
        var display = Text(phone, "display_phone_number");
        var normalized = LoyaltyContact.NormalizePhone(display.StartsWith('+') ? display : "+" + display);
        if (normalized is null || normalized != LoyaltyContact.NormalizePhone(settings.BusinessPhone))
            throw new InvalidOperationException("El Phone Number ID no corresponde al numero esperado en la cuenta de WhatsApp configurada.");
        if (Text(phone, "code_verification_status") != "VERIFIED")
            throw new InvalidOperationException("Meta no confirma la verificacion de ese numero. Revisa su registro sin desconectar WhatsApp Business.");
        using var templates = await Read(http, settings, $"{settings.BusinessAccountId}/message_templates?name={Uri.EscapeDataString(settings.AuthenticationTemplate)}&fields=name,status,category,language,components&limit=100", ct);
        var template = Data(templates).EnumerateArray().FirstOrDefault(t => Text(t, "name") == settings.AuthenticationTemplate
            && Text(t, "language") == settings.TemplateLanguage);
        if (Text(template, "status") != "APPROVED" || Text(template, "category") != "AUTHENTICATION" || !HasCopyCode(template))
            throw new InvalidOperationException("Se necesita una plantilla AUTHENTICATION aprobada en el idioma configurado con boton Copy code.");
        return new(normalized, Text(phone, "verified_name"), settings.AuthenticationTemplate);
    }

    private static bool HasCopyCode(JsonElement template) {
        if (!template.TryGetProperty("components", out var components) || components.ValueKind != JsonValueKind.Array) return false;
        return components.EnumerateArray().Any(c => Text(c, "type") == "BUTTONS" && c.TryGetProperty("buttons", out var buttons)
            && buttons.ValueKind == JsonValueKind.Array && buttons.GetArrayLength() > 0
            && Text(buttons[0], "type") == "OTP" && Text(buttons[0], "otp_type") == "COPY_CODE");
    }
    private static JsonElement Data(JsonDocument document) => document.RootElement.ValueKind == JsonValueKind.Object
        && document.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array ? data
        : throw new InvalidOperationException("Meta devolvio una respuesta no valida para comprobar la configuracion.");
    private static string Text(JsonElement value, string field) => value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty(field, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString()! : string.Empty;
    private static async Task<JsonDocument> Read(HttpClient http, LoyaltyWhatsAppOptions settings, string path, CancellationToken ct) {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://graph.facebook.com/{settings.GraphVersion}/{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("No se pudo consultar Meta. Revisa el token y los permisos de la cuenta; no se enviaron mensajes.");
        try { return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct)); }
        catch (JsonException) { throw new InvalidOperationException("Meta devolvio una respuesta no valida."); }
    }
}
