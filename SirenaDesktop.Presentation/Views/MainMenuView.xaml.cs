using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using SirenaDesktop.ViewModels;

namespace SirenaDesktop.Views;

public partial class MainMenuView : UserControl
{
    public MainMenuView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        var parentWindow = Window.GetWindow(this);

        if (parentWindow != null)
        {
            parentWindow.MinWidth = 600;
            parentWindow.MinHeight = 400;

            parentWindow.Width = 1000;
            parentWindow.Height = 666;

            parentWindow.MaxWidth = 1600;
            parentWindow.Height = 900;

            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            parentWindow.Left = (screenWidth - parentWindow.Width) / 2;
            parentWindow.Top = (screenHeight - parentWindow.Height) / 2;
        }
    }

    private void StreakOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainMenuViewModel vm && ReferenceEquals(e.OriginalSource, sender))
            vm.CloseStreakCelebrationCommand.Execute(null);
    }

    private void ApiKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainMenuViewModel vm && sender is PasswordBox pb)
            vm.ApiKeySettings.ApiKeyInput = pb.Password;
    }

    private void TaskOrWorkspaceDialogHost_DialogClosing(object sender, DialogClosingEventArgs eventArgs)
    {
    }
}