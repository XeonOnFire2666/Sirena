using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SirenaAPI.Logic.Entities;
using SirenaAPI.Models;

namespace SirenaAPI.Infrastructure.BackgroundJobs;

public class TodoRolloverBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<TodoRolloverBackgroundService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<TodoRolloverBackgroundService> _logger = logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RolloverOverdueTasks(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TodoRollover] Ошибка при переносе просроченных задач");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task RolloverOverdueTasks(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SirenaDbContext>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Ищем воркспейсы из ПРОШЛОГО, в которых есть не удаленные задачи.
        // ОБЯЗАТЕЛЬНО .Include(w => w.Todos), иначе они не загрузятся.
        var pastWorkspaces = await db.Workspaces
            .Include(w => w.Todos)
            .Where(w => !w.IsDeleted && w.Date < today)
            .ToListAsync(ct);

        var changedCount = 0;

        foreach (var oldWs in pastWorkspaces)
        {
            // Отбираем невыполненные задачи
            var overdueTodos = oldWs.Todos
                .Where(t => !t.IsCompleted && !t.IsDeleted)
                .ToList();

            if (overdueTodos.Count == 0)
                continue;

            // 2. Проверяем, есть ли уже у юзера идентичный воркспейс на СЕГОДНЯ
            var targetWs = await db.Workspaces
                .Include(w => w.Todos)
                .FirstOrDefaultAsync(w =>
                    w.UserId == oldWs.UserId &&
                    w.Name == oldWs.Name &&
                    w.WorkspaceTypeId == oldWs.WorkspaceTypeId &&
                    w.Date == today &&
                    !w.IsDeleted, ct);

            // 3. Если нет, создаем автоматически "идентичный воркспейс"
            if (targetWs == null)
            {
                targetWs = Workspace.Create(
                    Guid.NewGuid(),
                    oldWs.UserId,
                    oldWs.WorkspaceTypeId,
                    oldWs.Name,
                    oldWs.ColorHex,
                    today,
                    new List<TodoItem>(),
                    false,
                    DateTime.UtcNow);

                db.Workspaces.Add(targetWs);
            }

            // 4. Переносим задачи
            foreach (var todo in overdueTodos)
            {
                // Физически переносим в новый воркспейс
                todo.WorkspaceId = targetWs.Id;

                // Переносим даты на сегодня, но оставляем изначальное время (часы, минуты)
                if (todo.StartTime.HasValue)
                {
                    todo.StartTime = new DateTime(today.Year, today.Month, today.Day,
                        todo.StartTime.Value.Hour, todo.StartTime.Value.Minute, 0, DateTimeKind.Utc);
                }
                if (todo.EndTime.HasValue)
                {
                    todo.EndTime = new DateTime(today.Year, today.Month, today.Day,
                        todo.EndTime.Value.Hour, todo.EndTime.Value.Minute, 0, DateTimeKind.Utc);
                }

                todo.UpdatedAt = DateTime.UtcNow;

                targetWs.Todos.Add(todo);
                oldWs.Todos.Remove(todo); // Убираем из вчерашнего воркспейса!

                changedCount++;
            }

            // Обновляем метки для синхронизации Desktop'а
            oldWs.UpdatedAt = DateTime.UtcNow;
            targetWs.UpdatedAt = DateTime.UtcNow;
        }

        if (changedCount > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("[TodoRollover] Перенесено задач на сегодня в новые пространства: {Count}", changedCount);
        }
    }
}