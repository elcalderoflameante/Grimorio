namespace Grimorio.API.Services.Voice;

public interface IVoiceMediaClient
{
    string PublicUrl { get; }
    string CreateListenerToken(string room, string identity, string name);
    Task SetPublisherAsync(string room, string identity, bool enabled);
    Task RemoveAsync(string room, string identity);
}
