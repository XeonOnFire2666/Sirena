using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SirenaAPI.Application.Services;
using SirenaAPI.Logic.Entities;

namespace SirenaAPI.Adapters.EndPoints;

public static class DayStatsEndpoint
{
    public static IEndpointRouteBuilder MapDayStatsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/stats/getall", GetAll).RequireAuthorization();
        app.MapGet("stats/getbydate", GetByDate).RequireAuthorization();
        app.MapGet("/stats/getalldesktop", GetAllFromDesktop).RequireAuthorization();
        app.MapGet("/stats/getallmobile", GetAllFromMobile).RequireAuthorization();
        app.MapGet("/stats/getbydatemobile", GetByDateFromMobile).RequireAuthorization();
        app.MapGet("/stats/getbydatedesktop", GetByDateFromDesktop).RequireAuthorization();

        app.MapPost("stats/update", UpdateStats).RequireAuthorization();

        app.MapPost("stats/create", CreateDayStats).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> GetAll([FromQuery] Guid userId, DayStatsService service, HttpContext context)
    {
        var stats = await service.GetAll(userId);
        return Results.Ok(stats);
    }

    private static async Task<IResult> GetByDateFromMobile([FromQuery] Guid userId, DateOnly date, DayStatsService service, HttpContext context)
    {
        var stats = await service.GetByDateFromMobile(userId, date);
        return Results.Ok(stats);
    }

    private static async Task<IResult> GetByDateFromDesktop([FromQuery] Guid userId, DateOnly date, DayStatsService service, HttpContext context)
    {
        var stats = await service.GetByDateFromDesktop(userId, date);
        return Results.Ok(stats);
    }

    private static async Task<IResult> GetAllFromDesktop([FromQuery] Guid userId, DayStatsService service, HttpContext context)
    {
        var stats = await service.GetAllFromDesktop(userId);
        return Results.Ok(stats);
    }

    private static async Task<IResult> GetAllFromMobile([FromQuery] Guid userId, DayStatsService service, HttpContext context)
    {
        var stats = await service.GetAllFromMobile(userId);
        return Results.Ok(stats);
    }

    public static async Task<IResult> GetByDate([FromQuery] DateOnly? date, [FromQuery] Guid? userId, DayStatsService service, CancellationToken ct = default)
    {
        var stats = await service.GetByDate(userId: userId, date: date, ct);
        return Results.Ok(stats);
    }

    public static async Task<IResult> UpdateStats(UpdateDayStatsDto dto, DayStatsService service, CancellationToken ct = default)
    {
        try
        {
            await service.UpsertStats(dto, ct);
            return Results.Ok();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    public static async Task<IResult> CreateDayStats(CreateDayStatsDto stats, DayStatsService service, CancellationToken ct = default)
    {
        try
        {
            var entity = DayStats.Create(stats.UserId, stats.Day, stats.Tags);
            entity.StudyUnits = stats.StudyUnits;
            entity.GitUnits = stats.GitUnits;
            entity.LastScoredCommitSha = stats.LastScoredCommitSha;
            entity.ProductivityPercent = stats.ProductivityPercent;

            await service.CreateDayStats(entity, ct);
            return Results.Ok();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}