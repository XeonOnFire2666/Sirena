using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.EntityFrameworkCore;
using SirenaAPI;
using SirenaAPI.Adapters.EndPoints;
using SirenaAPI.Adapters.Repository;
using SirenaAPI.API;
using SirenaAPI.Application.Services;
using SirenaAPI.EndPoints;
using SirenaAPI.Infrastructure;
using SirenaAPI.Infrastructure.BackgroundJobs;
using SirenaAPI.Infrastructure.Repository;
using SirenaAPI.Interfaces;
using SirenaAPI.Jwt;
using SirenaAPI.Logic.Entities;
using SirenaAPI.Models;
using SirenaAPI.Repository;
using SirenaAPI.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5203");
var services = builder.Services;

services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));
services.Configure<RealGoogleOptions>(builder.Configuration.GetSection("RealGoogleOptions"));
services.AddApiAuthentication(builder.Configuration);

// --- Базовые сервисы ---
services.AddScoped<IJwtProvider, JwtProvider>();
services.AddScoped<IUserRepository<User>, UserRepository>();
services.AddScoped<IDayStatsRepository<DayStats>, DayStatsRepositoriy>();
services.AddScoped<ITokenRepository<RefreshToken>, RefreshTokensRepository>();
services.AddScoped<UserService>();
services.AddScoped<DayStatsService>();

// --- Новые репозитории и сервисы для Workspace и TodoItems ---
// Обрати внимание: мы регистрируем новые интерфейсы на новые реализации
services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
services.AddScoped<ITodoItemRepository, TodoItemRepository>();
services.AddScoped<TodoItemService>();
services.AddScoped<WorkspaceService>();

services.AddDbContext<SirenaDbContext>();
services.AddAuthorization();
services.AddOpenApi();
services.AddControllers();
services.AddMemoryCache();

services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new UtcDateTimeConverter());
});

services.AddHostedService<TodoRolloverBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    try
    {
        var context = serviceProvider.GetRequiredService<SirenaDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.MapOpenApi();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapUsersEndpoints();
app.MapDayStatsEndpoints();
app.MapWorkspaceEndpoints();
app.MapControllers();

app.Run();