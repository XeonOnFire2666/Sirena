using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SirenaDesktop.ApiCallers;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Application.Contracts.Ai;
using SirenaDesktop.Application.Contracts.Ai.Procent;
using SirenaDesktop.Application.Contracts.Git;
using SirenaDesktop.Application.Contracts.User.Tokens.LogicInterfaces;
using SirenaDesktop.Application.Services;
using SirenaDesktop.Infrastructure.AI;
using SirenaDesktop.Infrastructure.Auth;
using SirenaDesktop.Infrastructure.BackgroundWorkers;
using SirenaDesktop.Infrastructure.Git;
using SirenaDesktop.Infrastructure.Services;
using SirenaDesktop.Notifications;
using SirenaDesktop.Repositories;
using SirenaDesktop.Services;
using SirenaDesktop.Sync;
using SirenaDesktop.ViewModels;
using SirenaDesktop.Win_API;

namespace SirenaDesktop;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var modelDir = Path.Combine(AppContext.BaseDirectory, "Models", "SmolLM2");

        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, builder) =>
            {
                builder.SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", false, true);
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                services.AddSingleton<IAuthSignalService, AuthSignalService>();
                services.AddSingleton(configuration);
                services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
                services.AddSingleton<ITokenContainer, TokenContainer>();
                services.AddSingleton<TokensService>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<JwtTokenContainer>();
                services.AddSingleton<WinAPI>();
                services.AddSingleton<AuthTokens>();
                services.AddSingleton<IGitSettingsService, GitSettingsService>();
                services.AddSingleton<Func<LoginViewModel>>(s => () => s.GetRequiredService<LoginViewModel>());
                services.AddSingleton<Func<MainMenuViewModel>>(s => () => s.GetRequiredService<MainMenuViewModel>());
                services.AddSingleton<DoHConnectionFactory>();
                services.AddSingleton<IVideoWatchAccumulator, VideoWatchAccumulator>();
                services.AddSingleton<IApiKeyStore, ApiKeyStore>();

                services.AddHttpClient<UserApiCaller>(client => { client.Timeout = TimeSpan.FromSeconds(100); });

                services.AddHttpClient<DayStatsApiCaller>(client => { client.Timeout = TimeSpan.FromSeconds(100); });

                services.AddHttpClient<WorkspaceApiCaller>(client => { client.Timeout = TimeSpan.FromSeconds(100); });

                services.AddHttpClient<IGeminiAiProvider, GeminiAiProviderApiCaller>(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                    })
                    .ConfigurePrimaryHttpMessageHandler(sp =>
                    {
                        var dohFactory = sp.GetRequiredService<DoHConnectionFactory>();

                        return new SocketsHttpHandler
                        {
                            ConnectCallback = dohFactory.ConnectCallback,
                            ConnectTimeout = TimeSpan.FromSeconds(10),
                            PooledConnectionLifetime = TimeSpan.FromMinutes(15)
                        };
                    });

                services.AddHostedService<DataCollectorBackgroundWorker>();
                services.AddHostedService<DayStatsApiBackgroundWorker>();

                services.AddTransient<IUserRepository, UserRepository>();
                services.AddTransient<UserService>();
                services.AddTransient<IDayStatsRepository, DayStatsRepository>();
                services.AddTransient<DayStatsService>();
                services.AddTransient<IStatsExistsFlagRepository, StatsExistsFlagRepository>();
                services.AddTransient<StatsExistsFlagService>();
                services.AddTransient<IDayStatsApiCaller>(sp => sp.GetRequiredService<DayStatsApiCaller>());

                services.AddTransient<IWorkspaceRepository, WorkspaceRepository>();
                services.AddTransient<IWorkspaceService, WorkspaceService>();
                services.AddTransient<ITodoItemRepository, TodoItemRepository>();
                services.AddSingleton<IGitProductivityAnalyzer, GitProductivityAnalyzer>();
                services.AddTransient<TodoItemService>();
                services.AddTransient<ProductivityPercent>();
                services.AddTransient(sp =>
                    new TodoGitScoringService(
                        sp.GetRequiredService<IGitProductivityAnalyzer>(),
                        () => sp.GetRequiredService<IGitSettingsService>().GetTrackedRepoPath(),
                        points => points / 10.0,
                        sp.GetRequiredService<DayStatsService>(),
                        sp.GetService<ILogger<TodoGitScoringService>>()));
                services.AddTransient<ITaskUnitsCalculator, TaskUnitsCalculator>();

                services.AddTransient<AppTagMapRepository>();
                services.AddTransient<TokenRefreshService>();
                services.AddTransient<ITokenRefreshService>(sp => sp.GetRequiredService<TokenRefreshService>());
                services.AddTransient<IJwtTokenContainer, JwtTokenContainer>();
                services.AddTransient<IHydrationFlagRepository, HydrationFlagRepository>();
                services.AddTransient<WorkspaceDbService>();
                services.AddTransient<StreakNotificationService>();
                services.AddTransient<HydrationFlagService>();
                services.AddTransient<IPendingChangeRepository, PendingChangeRepository>();

                services.AddTransient<HomeTabViewModel>();
                services.AddTransient<TimerTabViewModel>();
                services.AddTransient<TodoTabViewModel>();
                services.AddTransient<ActivityTabViewModel>();
                services.AddTransient<VideoStudyAnalysisService>();
                services.AddTransient<GitSettingsViewModel>();

                services.AddTransient<LoginViewModel>();
                services.AddTransient<MainMenuViewModel>();
                services.AddTransient<MainWindow>();
                services.AddTransient<DataSyncService>();

                services.AddDbContextFactory<SirenaDbContext>();
            })
            .Build();

        await Task.Run(async () => await host.StartAsync());

        var appWindow = host.Services.GetRequiredService<MainWindow>();

        appWindow.Show();
    }
}