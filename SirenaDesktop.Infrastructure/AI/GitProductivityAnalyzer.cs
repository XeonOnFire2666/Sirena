using System.IO;
using LibGit2Sharp;
using SirenaDesktop.Application.Contracts.Git;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Infrastructure.Git;

public class GitProductivityAnalyzer : IGitProductivityAnalyzer
{
    public List<CommitScore> AnalyzeRange(string repoPath, DateTime from, DateTime to, ISet<string> alreadyScoredShas)
    {
        var results = new List<CommitScore>();

        using var repo = new Repository(repoPath);

        var commitsInRange = repo.Commits
            .Where(c => c.Author.When.UtcDateTime >= from && c.Author.When.UtcDateTime <= to)
            .Where(c => !alreadyScoredShas.Contains(c.Sha))
            .OrderBy(c => c.Author.When)
            .ToList();

        DateTime? prevCommitTime = null;

        foreach (var commit in commitsInRange)
        {
            var score = ScoreCommit(repo, commit);

            if (prevCommitTime.HasValue)
            {
                var gap = commit.Author.When.UtcDateTime - prevCommitTime.Value;
                if (gap.TotalHours < 2)
                    score.RegularityPoints = 13;
            }

            prevCommitTime = commit.Author.When.UtcDateTime;

            results.Add(score);
        }

        return results;
    }

    private CommitScore ScoreCommit(Repository repo, Commit commit)
    {
        var score = new CommitScore
        {
            Sha = commit.Sha,
            Date = commit.Author.When.UtcDateTime
        };

        var parent = commit.Parents.FirstOrDefault();
        var changes = parent == null
            ? repo.Diff.Compare<Patch>(null, commit.Tree)
            : repo.Diff.Compare<Patch>(parent.Tree, commit.Tree);

        score.FilesChanged = changes.Count();
        score.LinesChanged = changes.Sum(c => c.LinesAdded + c.LinesDeleted);
        score.HasTests = changes.Any(c => IsTestFile(c.Path));

        score.BasePoints = 25;

        score.VolumePoints = Math.Min(50, score.LinesChanged / 6);

        if (score.FilesChanged is >= 1 and <= 5)
            score.AtomicityPoints = 13;

        if (score.HasTests)
            score.TestsPoints = 38;

        if (score.FilesChanged > 15 || score.LinesChanged > 500)
            score.DumpPenalty = -10;

        return score;
    }

    private static bool IsTestFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        var normalized = path.Replace('\\', '/');
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < parts.Length - 1; i++)
        {
            var dir = parts[i].ToLowerInvariant();
            if (dir is "test" or "tests" or "spec" or "specs" or "__tests__")
                return true;
        }

        var fileName = parts[^1];
        var lowerFileName = fileName.ToLowerInvariant();

        if (lowerFileName.StartsWith("test_") || lowerFileName.StartsWith("test-") ||
            lowerFileName.StartsWith("spec_") || lowerFileName.StartsWith("spec-") ||
            lowerFileName.Contains(".test.") || lowerFileName.Contains(".spec.") ||
            lowerFileName.Contains("_test.") || lowerFileName.Contains("-test.") ||
            lowerFileName.Contains("_spec.") || lowerFileName.Contains("-spec."))
            return true;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        if (nameWithoutExt.EndsWith("Test") || nameWithoutExt.EndsWith("Tests") ||
            nameWithoutExt.EndsWith("Spec") || nameWithoutExt.EndsWith("Specs"))
            return true;

        return false;
    }
}