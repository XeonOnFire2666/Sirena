using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntimeGenAI;
using SirenaDesktop.Application.Contracts.Ai;

namespace SirenaDesktop.Infrastructure.AI;

public sealed class LocalAiVideoAnalysis : IDisposable
{
    private const string SystemPrompt =
        "You rate videos by their title for how educational and useful their content looks. " +
        "This covers ANY topic: programming, science, math, history, languages, health, finance, DIY, cooking, " +
        "psychology, business, crafts, tech, or any other field Ч as long as the video teaches something or gives real value. " +
        "High score (8-10) = tutorials, how-to guides, lectures, explainers, documentaries, deep-dive analysis, " +
        "smart podcasts, skill-building videos, and any content that clearly teaches or informs. " +
        "Low score (1-3) = pure entertainment, memes, shorts, vlogs, gossip, drama, clickbait, or low-effort content " +
        "with no real educational value. " +
        "Judge each title only by how useful and educational it looks Ч the specific topic does not matter. " +
        "You will receive a numbered list of titles. " +
        "Return ONLY a JSON array of integers (1 to 10), one score per title, in the SAME ORDER as given. " +
        "Do not repeat the titles. Do not add explanations or markdown. " +
        "Example input:\n1. \"How to learn Python in 30 days\"\n2. \"Funny cat compilation\"\n" +
        "Example output:\n[9, 2]";

    private readonly object _initLock = new();
    private readonly ILogger<LocalAiVideoAnalysis>? _logger;

    private readonly string _modelDirectory;

    private readonly Dictionary<string, int> _savedVideoTopics = [];
    private readonly object _topicsLock = new();
    private volatile bool _initialized;

    private Model? _model;
    private Tokenizer? _tokenizer;

    public LocalAiVideoAnalysis(string modelDirectory, ILogger<LocalAiVideoAnalysis>? logger = null)
    {
        _modelDirectory = modelDirectory;
        _logger = logger;
    }

    public void Dispose()
    {
        _tokenizer?.Dispose();
        _model?.Dispose();
    }

    public int GetAllTime()
    {
        lock (_topicsLock)
        {
            return _savedVideoTopics.Values.Sum();
        }
    }

    public void Upsert(string topic, int seconds)
    {
        lock (_topicsLock)
        {
            ref var existing = ref CollectionsMarshal.GetValueRefOrAddDefault(_savedVideoTopics, topic, out _);
            existing += seconds;
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_initLock)
        {
            if (_initialized) return;

            if (!Directory.Exists(_modelDirectory))
                throw new DirectoryNotFoundException(
                    $"Model directory not found: {_modelDirectory}. " +
                    "Download the ONNX model files first.");

            _model = new Model(_modelDirectory);
            _tokenizer = new Tokenizer(_model);
            _initialized = true;
        }
    }

    public async Task<VideoAnalysisResult> AnalyzeAndFlushAsync(CancellationToken ct = default)
    {
        Dictionary<string, int> snapshotSeconds;
        lock (_topicsLock)
        {
            if (_savedVideoTopics.Count == 0)
                return VideoAnalysisResult.Empty;

            snapshotSeconds = new Dictionary<string, int>(_savedVideoTopics);
        }

        var titles = snapshotSeconds.Keys.ToList();
        var minutesList = titles
            .Select(t => Math.Max(1, snapshotSeconds[t] / 60))
            .ToList();

        var scores = await GetScoresInOrderAsync(titles, ct).ConfigureAwait(false);

        var items = new List<ScoredVideo>(titles.Count);
        for (var i = 0; i < titles.Count; i++)
        {
            var score = i < scores.Count ? scores[i] : 5;
            items.Add(new ScoredVideo(titles[i], minutesList[i], score));
        }

        lock (_topicsLock)
        {
            foreach (var key in snapshotSeconds.Keys) _savedVideoTopics.Remove(key);
        }

        const double neutralScore = 5.5;
        const double scoreRange = 4.5;

        var studyUnits = items.Sum(i => i.Minutes * ((i.Score - neutralScore) / scoreRange));

        return new VideoAnalysisResult(items, studyUnits, true);
    }

    private async Task<List<int>> GetScoresInOrderAsync(List<string> titles, CancellationToken ct)
    {
        if (titles.Count == 0) return [];

        var raw = await GenerateRawAsync(titles, ct).ConfigureAwait(false);
        return ExtractScoresInOrder(raw, titles.Count);
    }

    public async Task<string> GenerateRawAsync(List<string> titles, CancellationToken ct = default)
    {
        EnsureInitialized();

        var numbered = string.Join("\n", titles.Select((t, i) => $"{i + 1}. \"{t}\""));
        var userPrompt = $"¬идео:\n{numbered}";

        var chatPrompt =
            $"<|im_start|>system\n{SystemPrompt}<|im_end|>\n" +
            $"<|im_start|>user\n{userPrompt}<|im_end|>\n" +
            $"<|im_start|>assistant\n";

        return await Task.Run(() => RunGeneration(chatPrompt, titles.Count, ct), ct).ConfigureAwait(false);
    }

    private string RunGeneration(string chatPrompt, int itemCount, CancellationToken ct)
    {
        var sequences = _tokenizer!.Encode(chatPrompt);

        var promptLength = sequences[0].Length;

        var maxNewTokens = 40 + itemCount * 6;

        using var generatorParams = new GeneratorParams(_model!);
        generatorParams.SetSearchOption("max_length", maxNewTokens + promptLength);
        generatorParams.SetSearchOption("temperature", 0.1);
        generatorParams.SetSearchOption("top_p", 0.9);

        using var generator = new Generator(_model!, generatorParams);
        generator.AppendTokenSequences(sequences);

        var sb = new StringBuilder();
        using var tokenizerStream = _tokenizer.CreateStream();

        var openBrackets = 0;
        var sawArrayStart = false;
        var generatedTokens = 0;

        while (!generator.IsDone())
        {
            ct.ThrowIfCancellationRequested();
            generator.GenerateNextToken();
            generatedTokens++;

            var tokenId = generator.GetSequence(0)[^1];
            var piece = tokenizerStream.Decode(tokenId);
            sb.Append(piece);

            foreach (var c in piece)
                if (c == '[')
                {
                    sawArrayStart = true;
                    openBrackets++;
                }
                else if (c == ']' && sawArrayStart)
                {
                    openBrackets--;
                }

            if (sawArrayStart && openBrackets <= 0)
                break;

            if (generatedTokens >= maxNewTokens)
                break;
        }

        _logger?.LogDebug("RAW output ({Tokens} tokens, promptLen={PromptLen}): [{Raw}]",
            generatedTokens, promptLength, sb.ToString());

        if (!sawArrayStart)
            _logger?.LogWarning(
                "Model produced no JSON array at all within {Tokens} tokens. Raw: [{Raw}]",
                generatedTokens, sb.ToString());

        return sb.ToString();
    }

    private List<int> ExtractScoresInOrder(string rawOutput, int expectedCount)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(rawOutput))
        {
            _logger?.LogWarning("Empty model output, cannot extract scores.");
            return result;
        }

        try
        {
            var arrStart = rawOutput.IndexOf('[');
            var arrEnd = rawOutput.IndexOf(']', arrStart < 0 ? 0 : arrStart);

            if (arrEnd < 0)
                arrEnd = rawOutput.LastIndexOf(']');

            if (arrStart >= 0 && arrEnd > arrStart)
            {
                var candidate = rawOutput[arrStart..(arrEnd + 1)];
                using var doc = JsonDocument.Parse(candidate);

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        var score = el.ValueKind switch
                        {
                            JsonValueKind.Number when el.TryGetInt32(out var n) => n,
                            JsonValueKind.String when int.TryParse(el.GetString(), out var p) => p,
                            JsonValueKind.Object when el.TryGetProperty("score", out var scoreEl) =>
                                scoreEl.ValueKind switch
                                {
                                    JsonValueKind.Number when scoreEl.TryGetInt32(out var n2) => n2,
                                    JsonValueKind.String when int.TryParse(scoreEl.GetString(), out var p2) => p2,
                                    _ => (int?)null
                                },
                            _ => null
                        };

                        if (score.HasValue)
                            result.Add(ClampScore(score.Value));
                    }

                if (result.Count > 0)
                    return result;
            }
        }
        catch (JsonException ex)
        {
            _logger?.LogDebug(ex, "Model output was not a valid JSON array, falling back to regex. Raw: {Raw}",
                rawOutput);
        }

        var matches = Regex.Matches(rawOutput, @"-?\d+");
        foreach (Match m in matches)
        {
            if (result.Count >= expectedCount) break;
            if (int.TryParse(m.Value, out var s))
                result.Add(ClampScore(s));
        }

        if (result.Count == 0)
            _logger?.LogWarning("Failed to extract any scores from model output: {Raw}", rawOutput);
        else if (result.Count < expectedCount)
            _logger?.LogWarning(
                "Extracted only {Got}/{Expected} scores from model output, remaining will default to neutral. Raw: {Raw}",
                result.Count, expectedCount, rawOutput);

        return result;
    }

    private static int ClampScore(int value)
    {
        return Math.Clamp(value, 1, 10);
    }
}