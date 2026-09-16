using SirenaDesktop.Core.Models;
using SirenaDesktop.Repositories;

namespace SirenaDesktop.Services;

public class UserService(
    IUserRepository userRepository)
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task Create(User user)
    {
        if (user == null) return;

        var newUser = new User(
            user.Id,
            user.Name,
            user.Email,
            user.GoogleId,
            user.PictureUrl
        );

        await _userRepository.Add(newUser);
    }

    public async Task<User?> GetById(Guid id)
    {
        if (id == Guid.Empty) return null;

        return await _userRepository.GetById(id);
    }

    public async Task<User?> Get()
    {
        return await _userRepository.Get();
    }

    public async Task Delete()
    {
        await _userRepository.Delete();
    }

    public async Task Update(User user)
    {
        if (user == null) return;

        await _userRepository.Update(user);
    }
}