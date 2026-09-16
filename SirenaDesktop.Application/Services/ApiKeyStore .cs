using System.Diagnostics;
using System.Runtime.InteropServices;
using CredentialManagement;

namespace SirenaDesktop.Services;

public sealed class ApiKeyStore : IApiKeyStore
{
    private const string ApiKeyTargetId = "SirenaDesktop_GeminiApiKey";

    private readonly object _lock = new();
    private string? _cachedKey;

    public ApiKeyStore()
    {
        _cachedKey = LoadFromCredentialManager();
    }

    public event EventHandler<string?>? ApiKeyChanged;

    public string? CurrentKey
    {
        get
        {
            lock (_lock)
            {
                return _cachedKey;
            }
        }
    }

    public bool HasKey => !string.IsNullOrWhiteSpace(CurrentKey);

    public bool SaveKey(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        using var credential = new Credential
        {
            Target = ApiKeyTargetId,
            Type = CredentialType.Generic,
            PersistanceType = PersistanceType.LocalComputer,
            Password = apiKey
        };

        var result = credential.Save();

        if (!result)
        {
            var error = Marshal.GetLastWin32Error();
            Debug.WriteLine($"[API KEY SAVE FAILED]: Win32Error={error}");
            return false;
        }

        lock (_lock)
        {
            _cachedKey = apiKey;
        }

        ApiKeyChanged?.Invoke(this, apiKey);
        return true;
    }

    public bool DeleteKey()
    {
        using var credential = new Credential { Target = ApiKeyTargetId };
        var result = credential.Delete();

        if (result)
        {
            lock (_lock)
            {
                _cachedKey = null;
            }

            ApiKeyChanged?.Invoke(this, null);
        }

        return result;
    }

    private static string? LoadFromCredentialManager()
    {
        using var credential = new Credential { Target = ApiKeyTargetId };

        var success = credential.Load();
        return success ? credential.Password : null;
    }
}