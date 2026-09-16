namespace SirenaDesktop.Application.Contracts.Ai;

public interface IGeminiAiProvider
{
    Task<ApiKeyValidationResult> ValidateApiKeyAsync(
        string apiKey, CancellationToken ct = default);

    Task<VideoAnalysisResult> AnalyzeAsync(
        IReadOnlyDictionary<string, int> videoMinutes,
        CancellationToken ct = default);
}