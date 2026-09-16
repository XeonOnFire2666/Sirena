using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SirenaAPI.Application.Services;
using SirenaAPI.Logic.Entities;

namespace SirenaAPI.Adapters.EndPoints;
public record TodoDto(
    Guid Id,
    string Title,
    DateTime? StartTime,
    DateTime? EndTime,
    DateTime? CompleteDate,
    bool IsCompleted,
    bool IsDeleted,
    DateTime UpdatedAt
);

public record CreateWorkspaceDto(Guid Id, Guid UserId, string WorkspaceTypeId, string Name, string ColorHex, DateOnly Date, List<TodoDto> Todos, DateTime UpdatedAt);
public record UpdateWorkspaceDto(Guid Id, Guid UserId, string WorkspaceTypeId, string Name, string ColorHex, DateOnly Date, List<TodoDto> Todos, bool IsDeleted, DateTime UpdatedAt);

public static class WorkspaceEndpoint
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/workspace/getall", GetAll).RequireAuthorization();
        app.MapPost("/workspace/create", CreateWorkspace).RequireAuthorization();
        app.MapPost("/workspace/update", UpdateWorkspace).RequireAuthorization();
        app.MapDelete("/workspace/delete", DeleteWorkspace).RequireAuthorization();
        return app;
    }
    private static List<TodoItem> MapTodos(Guid workspaceId, List<TodoDto> todos) =>
        todos.Select(t => TodoItem.Create(
            t.Id,
            workspaceId,
            t.Title,
            t.StartTime,
            t.EndTime,
            t.CompleteDate,
            t.IsCompleted,
            t.IsDeleted,
            t.UpdatedAt
        )).ToList();

    private static async Task<IResult> GetAll([FromQuery] Guid userId, WorkspaceService service, CancellationToken ct = default)
    {
        try
        {
            var workspaces = await service.GetAll(userId, ct);
            return Results.Ok(workspaces);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> CreateWorkspace(CreateWorkspaceDto dto, WorkspaceService service, CancellationToken ct = default)
    {
        try
        {
            var workspace = Workspace.Create(
                dto.Id,
                dto.UserId,
                dto.WorkspaceTypeId,
                dto.Name,
                dto.ColorHex,
                dto.Date,
                MapTodos(dto.Id, dto.Todos),
                isDeleted: false,
                dto.UpdatedAt
            );
            await service.Add(workspace, ct);
            return Results.Ok();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpdateWorkspace(UpdateWorkspaceDto dto, WorkspaceService service, CancellationToken ct = default)
    {
        try
        {
            var workspace = Workspace.Create(
                dto.Id,
                dto.UserId,
                dto.WorkspaceTypeId,
                dto.Name,
                dto.ColorHex,
                dto.Date,
                MapTodos(dto.Id, dto.Todos),
                dto.IsDeleted,
                dto.UpdatedAt
            );
            await service.Update(workspace, ct);
            return Results.Ok();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteWorkspace(Guid id, WorkspaceService service, CancellationToken ct = default)
    {
        try
        {
            await service.Delete(id, ct);
            return Results.Ok();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}