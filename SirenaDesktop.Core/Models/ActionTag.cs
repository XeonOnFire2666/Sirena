namespace SirenaDesktop.Core.Models;

public class ActionTag
{
    protected ActionTag()
    {
    }

    public ActionTag(Guid id, Guid tagId, DateTime dateTime, string appTitle, string domain, int recordedMinutes,
        string platform, string colorHex)
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

    public Guid Id { get; set; }
    public Guid TagId { get; set; }

    public DateTime DateTime { get; set; }
    public string AppTitle { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public int RecordedMinutes { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#FFFFFF";

    public static ActionTag Create(Guid id, Guid tagId, DateTime dateTime, string appTitle, string domain,
        int recordedMinutes, string platform, string colorHex)
    {
        return new ActionTag(id, tagId, dateTime, appTitle, domain, recordedMinutes, platform, colorHex);
    }
}