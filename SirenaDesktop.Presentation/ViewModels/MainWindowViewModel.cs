using System.ComponentModel;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Repositories;
using SirenaDesktop.Services;
using static System.Windows.Application;


namespace SirenaDesktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IAuthSignalService _authSignal;
    private readonly IPendingChangeRepository _changeRepository;
    private readonly Dispatcher _dispatcher;
    private readonly JwtSecurityTokenHandler _jwtHandler = new();
    private readonly IJwtTokenContainer _jwtTokenContainer;
    private readonly Func<LoginViewModel> _loginViewModelFactory;
    private readonly Func<MainMenuViewModel> _mainMenuViewModelFactory;
    private readonly ITokenRefreshService _refreshService;
    private readonly TodoItemService _todoItemService;
    private readonly TokensService _tokensService;
    private readonly UserService _userService;
    private readonly IWorkspaceService _workspaceService;

    [ObservableProperty] private object _currentViewModel;

    [ObservableProperty] private bool _isNavigationLoading;

    public MainWindowViewModel(
        TokensService tokensService,
        IJwtTokenContainer jwtTokenContainer,
        UserService userService,
        IAuthSignalService authSignal,
        ITokenRefreshService refreshService,
        Func<LoginViewModel> loginViewModelFactory,
        Func<MainMenuViewModel> mainMenuViewModelFactory,
        StatsExistsFlagService statsExistsFlagService,
        IWorkspaceService workspaceService,
        IPendingChangeRepository changeRepository,
        TodoItemService todoItemService)
    {
        _userService = userService;
        _tokensService = tokensService;
        _jwtTokenContainer = jwtTokenContainer;
        _authSignal = authSignal;
        _refreshService = refreshService;
        _loginViewModelFactory = loginViewModelFactory;
        _mainMenuViewModelFactory = mainMenuViewModelFactory;
        _workspaceService = workspaceService;
        _changeRepository = changeRepository;
        _dispatcher = Current.Dispatcher;

        _currentViewModel = new SplashViewModel();
        _ = InitializeAsync();
        _todoItemService = todoItemService;
    }

    private async Task InitializeAsync()
    {
        try
        {
            await CheckSessionAsync();
        }
        catch (Exception)
        {
            await NavigateToLoginAsync();
        }
    }

    public async Task NavigateToMainMenuAsync()
    {
        await _dispatcher.InvokeAsync(() =>
        {
            IsNavigationLoading = true;
            var mainMenuVM = _mainMenuViewModelFactory();
            mainMenuVM.LogoutRequested += OnUserLoggedOut;
            CurrentViewModel = mainMenuVM;
            IsNavigationLoading = false;
        });
    }

    private async void OnUserLoggedOut(object? sender, EventArgs e)
    {
        if (sender is MainMenuViewModel mainMenuVM)
        {
            mainMenuVM.LogoutRequested -= OnUserLoggedOut;
            mainMenuVM.Dispose();
        }

        await ExecuteLogoutAsync();
    }

    private async Task ExecuteLogoutAsync()
    {
        _authSignal.Reset();
        try
        {
            await _userService.Delete();
            await _workspaceService.ClearAllWorkspaces();
            await _todoItemService.DeleteAll();
            await _changeRepository.RemoveAll();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DB Error]: {ex}");
        }


        _tokensService.DeleteTokens();
        await NavigateToLoginAsync();
    }

    public async Task CheckSessionAsync()
    {
        var tokens = _tokensService.GetTokens();
        if (tokens?.Success is false || tokens?.Response?.AccessToken == null)
        {
            await NavigateToLoginAsync();
            return;
        }

        var exp = GetExpFromToken(tokens.Response.AccessToken);
        if (exp <= DateTime.UtcNow)
        {
            if (tokens.Response.RefreshToken == null)
            {
                await NavigateToLoginAsync();
                return;
            }
            
            var refreshed = await _refreshService.TryRefreshAsync();

            if (!refreshed)
            {
                Debug.WriteLine("[CheckSession]: Refresh failed, logging out");
                await ExecuteLogoutAsync();
                return;
            }
        }

        _authSignal.SignalLoginSuccess();
        await NavigateToMainMenuAsync();
    }

    private DateTime GetExpFromToken(string token)
    {
        try
        {
            var jwtToken = _jwtHandler.ReadJwtToken(token);
            var exp = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (exp != null) return DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp)).UtcDateTime;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Token Error]: {ex}");
        }

        return DateTime.MinValue;
    }

    public async Task NavigateToLoginAsync()
    {
        await _dispatcher.InvokeAsync(() =>
        {
            var loginVM = _loginViewModelFactory();
            loginVM.LoginSuccess += OnUserLoggedIn;
            CurrentViewModel = loginVM;
        });
    }

    public void NavigateToLogin()
    {
        if (_dispatcher.CheckAccess())
            NavigateToLoginInternal();
        else
            _dispatcher.InvokeAsync(NavigateToLoginInternal);
    }

    private void NavigateToLoginInternal()
    {
        var loginVM = _loginViewModelFactory();
        loginVM.LoginSuccess += OnUserLoggedIn;
        CurrentViewModel = loginVM;
    }

    private async void OnUserLoggedIn(object? sender, EventArgs e)
    {
        if (sender is LoginViewModel loginVM) loginVM.LoginSuccess -= OnUserLoggedIn;

        _authSignal.SignalLoginSuccess();
        await NavigateToMainMenuAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}