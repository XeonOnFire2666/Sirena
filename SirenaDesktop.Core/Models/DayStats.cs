using System.Text.Json.Serialization;

namespace SirenaDesktop.Core.Models;

public class DayStats
{
    public DayStats(Guid id, Guid userId, List<ActionTag> tags, DateOnly date)
    {
        Id = id;
        UserId = userId;
        Tags = tags;
        Date = date;
    }

    [JsonConstructor]
    protected DayStats()
    {
    }

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public List<ActionTag> Tags { get; set; } = [];
    public double StudyUnits { get; set; }
    public double GitUnits { get; set; }
    public HashSet<string> ScoredCommitShas { get; set; } = [];
    public int? ProductivityPercent { get; set; }

    public static DayStats Create(Guid id, Guid userId, DateOnly date, List<ActionTag> tags)
    {
        return new DayStats(id, userId, tags, date);
    }
}