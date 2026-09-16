namespace SirenaDesktop.Core.Models;

public class AppTagMap
{
    protected AppTagMap()
    {
    }

    public AppTagMap(Guid id, Guid userId, string trackKey, Guid tagId, string colorHex)
    {
        Id = id;
        UserId = userId;
        TrackKey = trackKey;
        TagId = tagId;
        ColorHex = colorHex;
    }

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TrackKey { get; set; } = string.Empty;
    public Guid TagId { get; set; }
    public string ColorHex { get; set; } = "#808080";

    public static AppTagMap Create(Guid userId, string trackKey, string colorHex)
    {
        return new AppTagMap(Guid.NewGuid(), userId, trackKey, Guid.NewGuid(), colorHex);
    }
}