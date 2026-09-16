namespace SirenaAPI.Models;

public class User(Guid id, string name, string email, string googleId, string pictureUrl)
{
    public Guid Id { get; private set; } = id;
    public string GoogleId { get; private set; } = googleId;
    public string Name { get; set; } = name;
    public string Email { get; set; } = email;
    public string PictureUrl { get; set; } = pictureUrl;

    public static User Create(string username, string email, string googleId, string pictureUrl)
    {
        return new User(Guid.NewGuid(), username, email, googleId, pictureUrl);
    }
 }