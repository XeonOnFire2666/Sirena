namespace SirenaDesktop.Core.Models;

public class CommitScore
{
    public string Sha { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int FilesChanged { get; set; }
    public int LinesChanged { get; set; }
    public bool HasTests { get; set; }
    public int BasePoints { get; set; }
    public int VolumePoints { get; set; }
    public int AtomicityPoints { get; set; }
    public int TestsPoints { get; set; }
    public int RegularityPoints { get; set; }
    public int DumpPenalty { get; set; }
    public int BuildFailPenalty { get; set; }

    public int TotalPoints =>
        BasePoints + VolumePoints + AtomicityPoints + TestsPoints + RegularityPoints + DumpPenalty + BuildFailPenalty;
}