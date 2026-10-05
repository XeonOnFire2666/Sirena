using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using SirenaDesktop.Application.Contracts.Ai;
using SirenaDesktop.Services;

namespace SirenaDesktop.Infrastructure.AI;

public class GeminiAiProviderApiCaller : IDisposable, IGeminiAiProvider
{
    private const string ApiBase = "https://generativelanguage.googleapis.com/v1beta/models";
    private const string Model = "gemini-3.1-flash-lite";
    private const double NeutralScore = 5.5;
    private const double ScoreRange = 4.5;
    private const double Multiplier = 2.0;

    private const string SystemPrompt = """
                                            You are a strict productivity analyst. You are given a numbered list of YouTube videos
                                            that the user watched while supposedly working on a task.

                                            For EACH video in the list, rate its educational, professional, or objectively useful
                                            value as an integer from 0 to 10, where:

                                              0-2   — pure entertainment, distraction, or time-wasting content
                                                      (e.g. memes, gaming, pranks, reaction videos, celebrity gossip,
                                                      music videos, comedy skits, vlogs with no instructional value);
                                              3-4   — mostly non-productive content with only marginal, incidental value
                                                      (e.g. casual commentary, entertainment with occasional relevant facts);
                                              5-6   — neutral or mixed content: has some informational or professional
                                                      relevance, but is not clearly instructional or work-related
                                                      (e.g. general news, loosely related industry chatter, light how-to
                                                      content with low depth);
                                              7-8   — clearly educational or professional content with solid learning value
                                                      (e.g. tutorials, technical explanations, documentaries, lectures,
                                                      skill-building content directly or indirectly relevant to work);
                                              9-10  — highly focused, in-depth educational/professional/training content
                                                      directly useful for skill development or the task at hand
                                                      (e.g. in-depth technical tutorials, expert talks, structured courses,
                                                      deep-dive explainers).

                                            Be strict and objective: default to a LOWER score when a video's purpose is
                                            primarily to entertain, and only assign a HIGH score when the content clearly
                                            teaches, informs, or builds a professional/technical skill. Do not be lenient —
                                            ambiguous or borderline content should lean toward the lower end of its bracket.

                                            Return ONLY a JSON array of integers, in the SAME order and SAME length
                                            as the input video list. No explanations, no markdown, no extra text — JSON array only.
                                        """;

    private readonly IApiKeyStore _apiKeyStore;
    private readonly HttpClient _http;
    private string? _apiKey;

    public GeminiAiProviderApiCaller(HttpClient httpClient, IApiKeyStore apiKeyStore)
    {
        _http = httpClient;
        _apiKeyStore = apiKeyStore;

        _apiKey = _apiKeyStore.CurrentKey;
        _apiKeyStore.ApiKeyChanged += OnApiKeyChanged;
    }

    public void Dispose()
    {
        _apiKeyStore.ApiKeyChanged -= OnApiKeyChanged;
    }

    public async Task<ApiKeyValidationResult> ValidateApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return ApiKeyValidationResult.Invalid;

        apiKey = SanitizeApiKey(apiKey);

        if (!IsAsciiOnly(apiKey))
        {
            Debug.WriteLine("[GeminiAI] Key contains non-ASCII characters (copy-paste garbage) — considered invalid.");
            return ApiKeyValidationResult.Invalid;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, ApiBase);
        request.Headers.Add("x-goog-api-key", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GeminiAI] Network error during key validation: {ex}");
            return ApiKeyValidationResult.NetworkError;
        }

        if (response.IsSuccessStatusCode)
            return ApiKeyValidationResult.Valid;

        if (response.StatusCode is HttpStatusCode.BadRequest
            or HttpStatusCode.Unauthorized
            or HttpStatusCode.Forbidden)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            Debug.WriteLine($"[GeminiAI] Key rejected ({(int)response.StatusCode}): {body}");
            return ApiKeyValidationResult.Invalid;
        }

        Debug.WriteLine($"[GeminiAI] Unexpected status during key validation: {(int)response.StatusCode}");
        return ApiKeyValidationResult.NetworkError;
    }

    public async Task<VideoAnalysisResult> AnalyzeAsync(
        IReadOnlyDictionary<string, int> videoMinutes,
        CancellationToken ct = default)
    {
        if (videoMinutes.Count == 0) return VideoAnalysisResult.Empty;

        var titles = videoMinutes.Keys.ToList();
        var minutesList = titles.Select(t => Math.Max(1, videoMinutes[t])).ToList();

        var scores = await GetScoresInOrderAsync(titles, ct).ConfigureAwait(false);

        if (scores is null)
            return VideoAnalysisResult.Failed;

        var items = new List<ScoredVideo>(titles.Count);
        for (var i = 0; i < titles.Count; i++)
        {
            var score = i < scores.Count ? scores[i] : 5;
            items.Add(new ScoredVideo(titles[i], minutesList[i], score));
        }

        var studyUnits = items.Sum(i => i.Minutes * ((i.Score - NeutralScore) / ScoreRange) * Multiplier);

        return new VideoAnalysisResult(items, studyUnits, true);
    }

    private void OnApiKeyChanged(object? sender, string? newKey)
    {
        _apiKey = newKey is null ? null : SanitizeApiKey(newKey);
    }

    private string RequireApiKey()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException(
                "Gemini API key is not set. Specify it in the application settings.");

        var key = SanitizeApiKey(_apiKey);

        if (!IsAsciiOnly(key))
            throw new InvalidOperationException(
                "Gemini API key contains invalid characters. Make sure the key was copied without trailing spaces or extra characters.");

        return key;
    }

    private async Task<List<int>?> GetScoresInOrderAsync(List<string> titles, CancellationToken ct)
    {
        var numberedList = string.Join("\n", titles.Select((t, i) => $"{i + 1}. \"{t}\""));

        var requestBody = new
        {
            systemInstruction = new { parts = new[] { new { text = SystemPrompt } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = numberedList } } } },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "ARRAY",
                    items = new { type = "INTEGER" }
                }
            }
        };

        var url = $"{ApiBase}/{Model}:generateContent";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-goog-api-key", RequireApiKey());
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        Debug.WriteLine($"[GeminiAI] -> POST {url} | videos={titles.Count}");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GeminiAI] Network error during request: {ex}");
            return null;
        }

        var raw = await response.Content.ReadAsStringAsync(ct);
        Debug.WriteLine($"[GeminiAI] <- HTTP {(int)response.StatusCode} {response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            Debug.WriteLine($"[GeminiAI] Error body: {raw}");
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            using var inner = JsonDocument.Parse(text!);
            var scores = inner.RootElement.EnumerateArray()
                .Select(e => e.GetInt32())
                .ToList();

            if (scores.Count != titles.Count)
                Debug.WriteLine($"[GeminiAI] Warning: model returned {scores.Count} scores instead of {titles.Count}. " +
                                "Missing videos will receive a neutral score of 5.");

            return scores;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GeminiAI] Error parsing response: {ex}\nRAW: {raw}");
            return null;
        }
    }

    private static string SanitizeApiKey(string apiKey)
    {
        return apiKey.Trim().Trim('\u200B', '\uFEFF');
    }

    private static bool IsAsciiOnly(string s)
    {
        return s.All(c => c < 128);
    }
}


