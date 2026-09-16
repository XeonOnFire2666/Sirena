using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Application.Contracts.Git;

public interface IGitProductivityAnalyzer
{
    List<CommitScore> AnalyzeRange(string repoPath, DateTime from, DateTime to, ISet<string> alreadyScoredShas);
}