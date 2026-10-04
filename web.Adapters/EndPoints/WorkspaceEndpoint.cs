using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SirenaAPI.Application.Services;
using SirenaAPI.Logic.Entities;
using SirenaAPI.Logic.Exceptions;
using System.Security.Claims;

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

public record CreateWorkspaceDto(
    Guid Id,
    Guid UserId,
    string WorkspaceTypeId,
    string Name,
    string ColorHex,
    DateOnly Date,
    List<TodoDto> Todos,
    DateTime UpdatedAt);

public record UpdateWorkspaceDto(
    Guid Id,
    Guid UserId,
    string WorkspaceTypeId,
    string Name,
    string ColorHex,
    DateOnly Date,
    List<TodoDto> Todos,
    bool IsDeleted,
    DateTime UpdatedAt);

public static class WorkspaceEndpoint
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/workspace/getall", GetAll).RequireAuthorization();
        app.MapPost("/workspace/create", CreateWorkspace).RequireAuthorization();
        app.MapPut("/workspace/upsert/{id:guid}", Upsert).RequireAuthorization();
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

    private static async Task<IResult> GetAll(ClaimsPrincipal user, WorkspaceService service, CancellationToken ct)
    {
        var userId = GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        try
        {
            var workspaces = await service.GetAll(userId.Value, ct);
            return Results.Ok(workspaces);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static async Task<IResult> Upsert(
        Guid id,
        UpdateWorkspaceDto dto,
        ClaimsPrincipal user,
        WorkspaceService service,
        CancellationToken ct)
    {
        if (id != dto.Id)
            return Results.BadRequest(new { error = "Route id does not match body id" });

        return await SaveAsync(
            Build(dto.Id, dto.UserId, dto.WorkspaceTypeId, dto.Name, dto.ColorHex, dto.Date,
                dto.Todos, dto.IsDeleted, dto.UpdatedAt),
            user, service, ct);
    }

    private static async Task<IResult> SaveAsync(
        Workspace workspace,
        ClaimsPrincipal user,
        WorkspaceService service,
        CancellationToken ct)
    {
        var requesterId = GetUserId(user);
        if (requesterId is null) return Results.Unauthorized();

        try
        {
            await service.Upsert(workspace, requesterId.Value, ct);
            return Results.Ok();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (WorkspaceConflictException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static Workspace Build(
        Guid id,
        Guid userId,
        string workspaceTypeId,
        string name,
        string colorHex,
        DateOnly date,
        List<TodoDto> todos,
        bool isDeleted,
        DateTime updatedAt) =>
        Workspace.Create(
            id,
            userId,
            workspaceTypeId,
            name,
            colorHex,
            date,
            MapTodos(id, todos),
            isDeleted,
            updatedAt);


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