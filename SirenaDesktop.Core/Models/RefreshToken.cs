namespace SirenaDesktop.Core.Models;

public class RefreshToken
{
    public Guid TokenNahuy { get; set; } = Guid.Empty;
    public DateTime Expiry { get; set; }
    public Guid UserId { get; set; }
}