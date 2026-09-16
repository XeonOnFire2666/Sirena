using SirenaDesktop.Core.Models;

namespace SirenaDesktop.Application.Contracts;

public record DayStatsResponce(
    bool IsUnauthorizedError,
    bool IsError,
    string message = null,
    DayStats Stats = null,
    List<DayStats> MoreSlatts = null);

public interface IDayStatsApiCaller
{
    Task<DayStatsResponce> GetFromMobile(Guid userId, DateOnly date);
}