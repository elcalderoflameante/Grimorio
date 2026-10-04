using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Grimorio.API.Services.Voice;

public sealed class LiveKitVoiceClient(IHttpClientFactory clients, IOptions<VoiceOptions> options) : IVoiceMediaClient
{
    private readonly VoiceOptions _options = options.Value;
    public string PublicUrl => _options.PublicUrl;

    private void EnsureConfigured()
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey) ||
            Encoding.UTF8.GetByteCount(_options.ApiSecret) < 32 ||
            !Uri.TryCreate(_options.PublicUrl, UriKind.Absolute, out var publicUrl) ||
            publicUrl.Scheme is not ("ws" or "wss") ||
            !Uri.TryCreate(_options.InternalUrl, UriKind.Absolute, out var internalUrl) ||
            internalUrl.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("Walkie-talkie no configurado en esta instalación.");
    }

    private string Token(string identity, Dictionary<string, object> grant, string? name = null)
    {
        EnsureConfigured();
        var now = DateTime.UtcNow;
        var payload = new JwtPayload(_options.ApiKey, null, null, now.AddSeconds(-5), now.AddMinutes(2), now)
        {
            ["sub"] = identity,
            ["video"] = grant,
        };
        if (name != null) payload["name"] = name;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.ApiSecret)), SecurityAlgorithms.HmacSha256);
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(new JwtHeader(credentials), payload));
    }

    public string CreateListenerToken(string room, string identity, string name) => Token(identity, new()
    {
        ["room"] = room, ["roomJoin"] = true, ["canSubscribe"] = true,
        ["canPublish"] = false, ["canPublishData"] = false,
        ["canPublishSources"] = new[] { "microphone" },
    }, name);

    public Task SetPublisherAsync(string room, string identity, bool enabled) => CallAsync("UpdateParticipant", room, new
    {
        room, identity,
        permission = new { canSubscribe = true, canPublish = enabled, canPublishData = false, canPublishSources = new[] { 2 } },
    }, missingIsSuccess: !enabled);

    public Task RemoveAsync(string room, string identity) =>
        CallAsync("RemoveParticipant", room, new { room, identity }, missingIsSuccess: true);

    private async Task CallAsync(string method, string room, object body, bool missingIsSuccess)
    {
        var token = Token("grimorio-voice-server", new() { ["room"] = room, ["roomAdmin"] = true });
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_options.InternalUrl.TrimEnd('/')}/twirp/livekit.RoomService/{method}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(body);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var response = await clients.CreateClient().SendAsync(request, timeout.Token);
        if (missingIsSuccess && response.StatusCode == HttpStatusCode.NotFound)
        {
            // Only a Twirp not_found is proof of absence; a proxy 404 is not.
            try
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
                if (json.RootElement.TryGetProperty("code", out var code) && code.GetString() == "not_found") return;
            }
            catch (JsonException) { }
        }
        response.EnsureSuccessStatusCode();
    }
}
