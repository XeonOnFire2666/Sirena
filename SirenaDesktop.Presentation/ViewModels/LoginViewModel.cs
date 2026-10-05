using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Contracts.User;
using SirenaDesktop.Services;

namespace SirenaDesktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IGoogleAuthService _authService;
    private readonly IAuthSignalService _authSignal;
    private readonly TokensService _tokensService;
    private readonly UserService _userService;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public LoginViewModel(IGoogleAuthService authService, TokensService tokensService, UserService userService,
        IAuthSignalService authSignal)
    {
        _authService = authService;
        _tokensService = tokensService;
        _userService = userService;
        _authSignal = authSignal;
    }

    public event EventHandler? LoginSuccess;
    public event EventHandler? LoginFailure;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task GoogleLoginAsync()
    {
        IsBusy = true;
        try
        {
            StatusMessage = "Connecting...";
            var tokensFromBackend = await _authService.AuthorizeAsync();

            var user = await _userService.Get();
            if (user == null) await _userService.Create(tokensFromBackend.User);

            if (tokensFromBackend.AccessToken == null)
            {
                StatusMessage = "Sign-in was canceled";
                LoginFailure?.Invoke(this, EventArgs.Empty);
                return;
            }

            var result = _tokensService.AddTokens(
                new TokenResponse(tokensFromBackend.RefreshToken, tokensFromBackend.AccessToken));

            if (result.Success)
            {
                StatusMessage = "Authorization successful";
                _authSignal.SignalLoginSuccess();
                LoginSuccess?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                StatusMessage = result.Message;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanLogin() => !IsBusy;
}