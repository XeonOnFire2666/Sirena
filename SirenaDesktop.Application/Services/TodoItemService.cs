using SirenaDesktop.Repositories;

namespace SirenaDesktop.Services;

public class TodoItemService(
    ITodoItemRepository todoRepo)
{
    private readonly ITodoItemRepository _todoRepo = todoRepo;

    public async Task DeleteAll()
    {
        await _todoRepo.HardDeleteAll();
    }
}