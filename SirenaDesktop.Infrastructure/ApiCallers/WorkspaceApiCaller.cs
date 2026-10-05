using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Application.Contracts.Todo;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.ApiCallers;

public record WorkspaceResponse(
    bool IsUnauthorizedError,
    bool IsError,
    string? Message = null,
    List<Workspace>? Workspaces = null,
    HttpStatusCode? StatusCode = null);

public class WorkspaceApiCaller(HttpClient client, AuthTokens authTokens)
{
    private readonly AuthTokens _authTokens = authTokens;
    private readonly HttpClient _client = client;
    private readonly string host = "https://sirenaapi.onrender.com";

    public async Task<WorkspaceResponse> GetAll(Guid userId)
    {
        try
        {
            var url = $"{host}/workspace/getall?userId={userId}";
            var request = await _authTokens.WithAuth(HttpMethod.Get, url);
            using var response = await _client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return await ToResponse(response);

            var workspaces = await response.Content.ReadFromJsonAsync<List<Workspace>>(JsonSerializerOptions.Web);
            return new WorkspaceResponse(false, false, Workspaces: workspaces, StatusCode: response.StatusCode);
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
            var url = $"{host}/workspace/delete?userId={id}";
            var request = await _authTokens.WithAuth(HttpMethod.Delete, url);
            using var response = await _client.SendAsync(request);
            return await ToResponse(response);
        }
        catch (Exception ex)
        {
            return new WorkspaceResponse(false, true, ex.Message);
        }
    }

    public async Task<WorkspaceResponse> Upsert(UpsertWorkspaceDto dto)
    {
        try
        {
            var url = $"{host}/workspace/upsert/{dto.Id}";
            var request = await _authTokens.WithAuth(HttpMethod.Put, url, JsonContent.Create(dto));
            using var response = await _client.SendAsync(request);
            return await ToResponse(response);
        }
        catch (Exception ex)
        {
            return new WorkspaceResponse(false, true, ex.Message);
        }
    }

    private static async Task<WorkspaceResponse> ToResponse(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return new WorkspaceResponse(true, false, StatusCode: response.StatusCode);

        if (!response.IsSuccessStatusCode)
            return new WorkspaceResponse(false, true, await response.Content.ReadAsStringAsync(),
                StatusCode: response.StatusCode);

        return new WorkspaceResponse(false, false, StatusCode: response.StatusCode);
    }
}