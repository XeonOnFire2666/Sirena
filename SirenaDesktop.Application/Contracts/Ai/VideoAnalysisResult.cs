namespace SirenaDesktop.Application.Contracts.Ai;

public sealed class VideoAnalysisResult
{
    public VideoAnalysisResult(IReadOnlyList<ScoredVideo> items, double studyUnits, bool isSuccess)
    {
        Items = items;
        StudyUnits = studyUnits;
        IsSuccess = isSuccess;
    }

    public IReadOnlyList<ScoredVideo> Items { get; }
    public double StudyUnits { get; }
    public bool IsSuccess { get; }

    public static VideoAnalysisResult Empty { get; } = new([], 0, true);
    public static VideoAnalysisResult Failed { get; } = new([], 0, false);
}