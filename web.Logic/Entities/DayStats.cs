using System.Text.Json.Serialization;

namespace SirenaAPI.Logic.Entities;

public class DayStats
{
    public DayStats(Guid userId, List<ActionTag> tags, DateOnly date)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Tags = tags;
        Date = date;
    }

    [JsonConstructor]
    protected DayStats()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public List<ActionTag> Tags { get; set; }

    public double StudyUnits { get; set; }
    public double GitUnits { get; set; }
    public string? LastScoredCommitSha { get; set; }
    public int? ProductivityPercent { get; set; }

    public static DayStats Create(Guid userId, DateOnly date, List<ActionTag> tags)
    {
        return new DayStats(userId, tags, date);
    }
}