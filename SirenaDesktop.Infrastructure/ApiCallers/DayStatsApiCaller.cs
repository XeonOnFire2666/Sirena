using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Core.Models;

namespace SirenaDesktop.ApiCallers;

public record CreateDayStatsDto(
    List<ActionTag> Tags,
    Guid UserId,
    DateOnly Day,
    double StudyUnits = 0,
    double GitUnits = 0,
    string? LastScoredCommitSha = null,
    int? ProductivityPercent = null);

public record UpdateDayStatsDto(
    List<ActionTag> NewTags,
    Guid UserId,
    DateOnly Day,
    double StudyUnits = 0,
    double GitUnits = 0,
    string? LastScoredCommitSha = null,
    int? ProductivityPercent = null);

public class DayStatsApiCaller(HttpClient client, AuthTokens authTokens) : IDayStatsApiCaller
{
    private readonly AuthTokens _authTokens = authTokens;

    private readonly HttpClient _client = client;
    private readonly string host = "https://sirenaapi.onrender.com";

    public async Task<DayStatsResponce> GetFromMobile(Guid userId, DateOnly date)
    {
        try
        {
            var url = host + $"/stats/getbydatemobile?userId={userId}&date={date}";

            var request = await _authTokens.WithAuth(HttpMethod.Get, url);
            var responce = await _client.SendAsync(request);

            if (responce.StatusCode == HttpStatusCode.Unauthorized)
                return new DayStatsResponce(true, false);
            if (responce.StatusCode == HttpStatusCode.BadRequest)
                return new DayStatsResponce(false, true, "Bad request");
            if (responce.StatusCode == HttpStatusCode.InternalServerError)
                return new DayStatsResponce(false, true);

            responce.EnsureSuccessStatusCode();

            var stats = JsonSerializer.Deserialize<DayStats>(await responce.Content.ReadAsStreamAsync(),
                JsonSerializerOptions.Web);
            return new DayStatsResponce(false, false, Stats: stats);
        }
        catch (HttpRequestException ex)
        {
            return new DayStatsResponce(false, true, ex.Message);
        }
        catch (Exception ex)
        {
            return new DayStatsResponce(false, true, ex.Message);
        }
    }

    public async Task<DayStatsResponce> GetByDate(DateOnly day, Guid userId)
    {
        try
        {
            var url = host + $"/stats/getbydate?date={day}&userId={userId}";
            var request = await _authTokens.WithAuth(HttpMethod.Get, url);

            var responce = await _client.SendAsync(request);

            if (responce.StatusCode == HttpStatusCode.Unauthorized)
                return new DayStatsResponce(true, false);
            if (responce.StatusCode == HttpStatusCode.BadRequest)
                return new DayStatsResponce(false, true, "Bad request");
            if (responce.StatusCode == HttpStatusCode.InternalServerError)
                return new DayStatsResponce(false, true);

            responce.EnsureSuccessStatusCode();

            var stats = JsonSerializer.Deserialize<DayStats>(await responce.Content.ReadAsStreamAsync(),
                JsonSerializerOptions.Web);
            return new DayStatsResponce(false, false, Stats: stats);
        }
        catch (HttpRequestException ex)
        {
            return new DayStatsResponce(false, true, ex.Message);
        }
        catch (Exception ex)
        {
            return new DayStatsResponce(false, true, ex.Message);
        }
    }

    public async Task<DayStatsResponce> CreateDayStats(CreateDayStatsDto dto)
    {
        try
        {
            var url = host + "/stats/create";
            var request = await _authTokens.WithAuth(HttpMethod.Post, url, JsonContent.Create(dto));
            var responce = await _client.SendAsync(request);

            if (responce.StatusCode == HttpStatusCode.Unauthorized)
                return new DayStatsResponce(true, false, "Unauthorized");
            if (responce.StatusCode == HttpStatusCode.BadRequest)
                return new DayStatsResponce(false, true, "Bad request");
            if (responce.StatusCode == HttpStatusCode.InternalServerError)
                return new DayStatsResponce(false, true, "Server error");

            return new DayStatsResponce(false, false);
        }
        catch (HttpRequestException ex)
        {
            return new DayStatsResponce(false, true, ex.Message);
        }
        catch (Exception ex)
        {
            return new DayStatsResponce(false, true, ex.Message);
        }
    }

    public async Task<DayStatsResponce> UpdateDayStats(UpdateDayStatsDto dto)
    {
        try
        {
            var url = host + "/stats/update";
            var request = await _authTokens.WithAuth(HttpMethod.Post, url, JsonContent.Create(dto));
            var responce = await _client.SendAsync(request);

            if (responce.StatusCode == HttpStatusCode.Unauthorized)
                return new DayStatsResponce(true, false, "Unauthorized");
            if (responce.StatusCode == HttpStatusCode.BadRequest)
                return new DayStatsResponce(false, true, "Bad request");
            if (responce.StatusCode == HttpStatusCode.InternalServerError)
                return new DayStatsResponce(false, true, "Server error");

            return new DayStatsResponce(false, false);
        }
        catch (HttpRequestException ex)
        {
            return new DayStatsResponce(false, true, ex.Message);
        }
        catch (Exception ex)
        {
            return new DayStatsResponce(false, true, ex.Message);
        }
    }

    public async Task<DayStatsResponce> GetForAllTime(Guid userId)
    {
        try
        {
            var url = host + $"/stats/getall?userId={userId}";
            var request = await _authTokens.WithAuth(HttpMethod.Get, url);
            var responce = await _client.SendAsync(request);

            if (responce.StatusCode == HttpStatusCode.Unauthorized)
                return new DayStatsResponce(true, false, "Unauthorized");
            if (responce.StatusCode == HttpStatusCode.BadRequest)
                return new DayStatsResponce(false, true, "Bad request");
            if (responce.StatusCode == HttpStatusCode.InternalServerError)
                return new DayStatsResponce(false, true, "Server error");

            var moreStats = JsonSerializer.Deserialize<List<DayStats>>(await responce.Content.ReadAsStreamAsync(),
                JsonSerializerOptions.Web);
            return new DayStatsResponce(false, false, MoreSlatts: moreStats);
        }
        catch (HttpRequestException ex)
        {
            return new DayStatsResponce(false, true, ex.Message);
        }
        catch (Exception ex)
        {
            return new DayStatsResponce(false, true, ex.Message);
        }
    }
}