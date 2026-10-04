namespace Grimorio.API.Services.Voice;

public sealed class VoiceOptions
{
    public bool Enabled { get; set; }
    public string PublicUrl { get; set; } = "";
    public string InternalUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";
}
