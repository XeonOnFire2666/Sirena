using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SirenaDesktop.Application.Contracts;
using SirenaDesktop.Contracts.User;
using SirenaDesktop.Services;

namespace SirenaDesktop.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly IGoogleAuthService _authService;
    private readonly IAuthSignalService _authSignal;
    private readonly TokensService _tokensService;
    private readonly UserService _userService;

    private string _statusMessage = string.Empty;

    public LoginViewModel(IGoogleAuthService authService, TokensService tokensService, UserService userService,
        IAuthSignalService authSignal)
    {
        _authService = authService;
        _tokensService = tokensService;
        _userService = userService;
        _authSignal = authSignal;

        GoogleLoginCommand = new RelayCommand(async _ => await ExecuteGoogleLogin());
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public ICommand GoogleLoginCommand { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? LoginSuccess;
    public event EventHandler? LoginFailure;

    private async Task ExecuteGoogleLogin()
    {
        StatusMessage = "Connecting...";
        var tokensFromBackend = await _authService.AuthorizeAsync();

        var user = await _userService.Get();
        if (user == null) await _userService.Create(tokensFromBackend.User);

        if (tokensFromBackend.AccessToken == null)
        {
            StatusMessage = "Service authorization error";
            LoginFailure?.Invoke(this, EventArgs.Empty);
            return;
        }

        var result =
            _tokensService.AddTokens(new TokenResponse(tokensFromBackend.RefreshToken, tokensFromBackend.AccessToken));

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

    protected void OnPropertyChanged([CallerMemberName] string name = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;

    public RelayCommand(Action<object> execute)
    {
        _execute = execute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
        return true;
    }

    public void Execute(object parameter)
    {
        _execute(parameter!);
    }
}