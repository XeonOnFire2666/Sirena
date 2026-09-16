using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.ApiCallers;

public record TodoDtoOut(
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
    List<TodoDtoOut> Todos,
    DateTime UpdatedAt
);

public record UpdateWorkspaceDto(
    Guid Id,
    Guid UserId,
    string WorkspaceTypeId,
    string Name,
    string ColorHex,
    DateOnly Date,
    List<TodoDtoOut> Todos,
    bool IsDeleted,
    DateTime UpdatedAt
);

public record WorkspaceResponse(
    bool IsUnauthorizedError,
    bool IsError,
    string? Message = null,
    List<Workspace>? Workspaces = null);

public class WorkspaceApiCaller(HttpClient client, AuthTokens authTokens)
{
    private readonly AuthTokens _authTokens = authTokens;
    private readonly HttpClient _client = client;
    private readonly string host = "https://sirenaapi.onrender.com";

    public async Task<WorkspaceResponse> GetAll(Guid userId)
    {
        try
        {
            var url = host + $"/workspace/getall?userId={userId}";
            var request = await _authTokens.WithAuth(HttpMethod.Get, url);
            var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new WorkspaceResponse(true, false);
            if (!response.IsSuccessStatusCode)
                return new WorkspaceResponse(false, true, await response.Content.ReadAsStringAsync());

            var workspaces = JsonSerializer.Deserialize<List<Workspace>>(await response.Content.ReadAsStreamAsync(),
                JsonSerializerOptions.Web);
            return new WorkspaceResponse(false, false, Workspaces: workspaces);
        }
        catch (Exception ex)
        {
            return new WorkspaceResponse(false, true, ex.Message);
        }
    }

    public async Task<WorkspaceResponse> Delete(Guid id)
    {
        try
        {
            var url = host + $"/workspace/delete?userId={id}";
            var request = await _authTokens.WithAuth(HttpMethod.Delete, url);
            var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new WorkspaceResponse(true, false);
            if (!response.IsSuccessStatusCode)
                return new WorkspaceResponse(false, true, await response.Content.ReadAsStringAsync());

            return new WorkspaceResponse(false, false);
        }
        catch (Exception ex)
        {
            return new WorkspaceResponse(false, true, ex.Message);
        }
    }

    public async Task<WorkspaceResponse> Create(CreateWorkspaceDto dto)
    {
        try
        {
            var url = host + "/workspace/create";
            var request = await _authTokens.WithAuth(HttpMethod.Post, url, JsonContent.Create(dto));
            var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new WorkspaceResponse(true, false);
            if (!response.IsSuccessStatusCode)
                return new WorkspaceResponse(false, true, await response.Content.ReadAsStringAsync());

            return new WorkspaceResponse(false, false);
        }
        catch (Exception ex)
        {
            return new WorkspaceResponse(false, true, ex.Message);
        }
    }

    public async Task<WorkspaceResponse> Update(UpdateWorkspaceDto dto)
    {
        try
        {
            var url = host + "/workspace/update";
            var request = await _authTokens.WithAuth(HttpMethod.Post, url, JsonContent.Create(dto));
            var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new WorkspaceResponse(true, false);
            if (!response.IsSuccessStatusCode)
                return new WorkspaceResponse(false, true, await response.Content.ReadAsStringAsync());

            return new WorkspaceResponse(false, false);
        }
        catch (Exception ex)
        {
            return new WorkspaceResponse(false, true, ex.Message);
        }
    }
}