using Microsoft.EntityFrameworkCore;

namespace SirenaDesktop.Repositories;

public class TodoItemRepository(IDbContextFactory<SirenaDbContext> dbContextFactory) : ITodoItemRepository
{
    private readonly IDbContextFactory<SirenaDbContext> _contextFactory = dbContextFactory;

    public async Task HardDeleteAll(CancellationToken ct = default)
    {
        using var db = await _contextFactory.CreateDbContextAsync(ct);
        await db.TodoItems.ExecuteDeleteAsync(ct);
    }
}