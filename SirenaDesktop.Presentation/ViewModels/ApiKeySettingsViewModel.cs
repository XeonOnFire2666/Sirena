using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using SirenaDesktop.Application.Contracts.Ai;
using SirenaDesktop.Infrastructure.AI;
using SirenaDesktop.Services;

public sealed partial class ApiKeySettingsViewModel : ObservableObject, IDisposable
{
    private const int SaveDebounceMs = 500;

    private readonly IApiKeyStore _apiKeyStore;
    private readonly IGeminiAiProvider _geminiCaller;

    [ObservableProperty] private string? _apiKeyInput;

    private CancellationTokenSource? _debounceCts;
    private bool _disposed;

    [ObservableProperty] private bool _isInvalid;

    [ObservableProperty] private bool _isSaved;

    private bool _isSyncingFromStore;

    [ObservableProperty] private bool _isValidating;

    [ObservableProperty] private string? _statusMessage;

    public ApiKeySettingsViewModel(IApiKeyStore apiKeyStore, IGeminiAiProvider geminiCaller)
    {
        _apiKeyStore = apiKeyStore;
        _geminiCaller = geminiCaller;

        _isSyncingFromStore = true;
        ApiKeyInput = _apiKeyStore.CurrentKey;
        IsSaved = _apiKeyStore.HasKey;
        _isSyncingFromStore = false;

        _apiKeyStore.ApiKeyChanged += OnApiKeyChangedExternally;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _apiKeyStore.ApiKeyChanged -= OnApiKeyChangedExternally;
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _disposed = true;
    }

    partial void OnApiKeyInputChanged(string? value)
    {
        if (_isSyncingFromStore)
            return;

        IsSaved = false;
        IsInvalid = false;
        StatusMessage = null;

        ScheduleSave(value);
    }

    private void ScheduleSave(string? value)
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = DebounceValidateAndSaveAsync(value, token);
    }

    private async Task DebounceValidateAndSaveAsync(string? value, CancellationToken ct)
    {
        try
        {
            await Task.Delay(SaveDebounceMs, ct).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (ct.IsCancellationRequested)
            return;

        var sanitized = value?.Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            _apiKeyStore.DeleteKey();
            IsSaved = false;
            IsInvalid = false;
            StatusMessage = null;
            return;
        }

        IsValidating = true;
        StatusMessage = "Validating key...";

        ApiKeyValidationResult validation;
        try
        {
            validation = await _geminiCaller.ValidateApiKeyAsync(sanitized, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            if (!ct.IsCancellationRequested)
                IsValidating = false;
        }

        if (ct.IsCancellationRequested)
            return;

        switch (validation)
        {
            case ApiKeyValidationResult.Valid:
                var saved = _apiKeyStore.SaveKey(sanitized);
                IsSaved = saved;
                IsInvalid = false;
                StatusMessage = saved ? "Key saved" : "Failed to save key";
                if (!saved)
                    Debug.WriteLine(
                        "[ApiKeySettingsViewModel] Failed to save API key to Credential Manager.");
                break;

            case ApiKeyValidationResult.Invalid:
                IsSaved = false;
                IsInvalid = true;
                StatusMessage = "Invalid key";
                break;

            case ApiKeyValidationResult.NetworkError:
                IsSaved = false;
                IsInvalid = false;
                StatusMessage = "Failed to validate key (network error). Please try again.";
                break;
        }
    }

    private void OnApiKeyChangedExternally(object? sender, string? newKey)
    {
        _isSyncingFromStore = true;
        ApiKeyInput = newKey;
        IsSaved = !string.IsNullOrWhiteSpace(newKey);
        IsInvalid = false;
        StatusMessage = null;
        _isSyncingFromStore = false;
    }
}