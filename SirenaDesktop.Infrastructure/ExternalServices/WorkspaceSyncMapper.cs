using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Application.Contracts.Todo;
using SirenaDesktop.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SirenaDesktop.Infrastructure.ExternalServices;

public static class WorkspaceSyncMapper
{
    public static UpsertWorkspaceDto ToUpsert(Workspace ws) =>
        new(
            ws.Id,
            ws.UserId,
            ws.WorkspaceTypeId,
            ws.Name,
            ws.ColorHex,
            ws.Date,
            ws.Todos.Select(t => new TodoDtoOut(
                t.Id,
                t.Title,
                t.StartTime,
                t.EndTime,
                t.CompleteDate,
                t.IsCompleted,
                t.IsDeleted,
                t.UpdatedAt)).ToList(),
            ws.IsDeleted,
            ws.UpdatedAt);
}