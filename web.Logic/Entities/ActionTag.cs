namespace SirenaAPI.Logic.Entities;

public class ActionTag
{
    /// <summary>
    /// Physical primary key of this day's row. Unique per (day, app) occurrence.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Stable identifier for a given (user, app/domain) pair. Stays the same
    /// forever across days, so the same app always maps to the same TagId
    /// and therefore the same color on the client.
    /// </summary>
    public Guid TagId { get; set; }

    public DateTime DateTime { get; set; }
    public string AppTitle { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public int RecordedMinutes { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#FFFFFF";

    protected ActionTag() { }

    public ActionTag(Guid id, Guid tagId, DateTime dateTime, string appTitle, string domain, int recordedMinutes, string platform, string colorHex)
    {
        Id = id;
        TagId = tagId;
        DateTime = dateTime;
        AppTitle = appTitle;
        Domain = domain;
        RecordedMinutes = recordedMinutes;
        Platform = platform;
        ColorHex = colorHex;
    }
}
    