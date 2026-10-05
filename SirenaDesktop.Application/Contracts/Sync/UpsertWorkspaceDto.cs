using SirenaDesktop.Application.Contracts.Todo;

namespace SirenaDesktop.Application.Contracts;

public record UpsertWorkspaceDto(
    Guid Id,
    Guid UserId,
    string WorkspaceTypeId,
    string Name,
    string ColorHex,
    DateOnly Date,
    List<TodoDtoOut> Todos,
    bool IsDeleted,
    DateTime UpdatedAt);