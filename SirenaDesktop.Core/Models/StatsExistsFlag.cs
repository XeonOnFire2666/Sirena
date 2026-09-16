namespace SirenaDesktop.Core.Models;

public class PendingChanges
{
    public PendingChanges(bool flag, Guid userId, DateOnly day)
    {
        IsExists = flag;
        UserId = userId;
        DateOnly = day;
    }

    protected PendingChanges()
    {
    }

    public bool IsExists { get; set; }
    public DateOnly DateOnly { get; set; }
    public Guid UserId { get; set; }

    public static PendingChanges Create(bool flag, Guid userId, DateOnly day)
    {
        return new PendingChanges(flag, userId, day);
    }
}