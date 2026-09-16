namespace SirenaDesktop.Services;

public interface IApiKeyStore
{
    string? CurrentKey { get; }

    bool HasKey { get; }

    event EventHandler<string?>? ApiKeyChanged;

    bool SaveKey(string apiKey);

    bool DeleteKey();
}