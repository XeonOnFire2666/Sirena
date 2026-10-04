namespace SirenaAPI.Logic.Exceptions;

public sealed class WorkspaceConflictException(string message) : Exception(message);
