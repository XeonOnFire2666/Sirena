using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SirenaDesktop.ViewModels;

namespace SirenaDesktop.Views;

public partial class MainMenuView : UserControl
{
    public MainMenuView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

#if DEBUG
        AddHandler(PointerPressedEvent, OnDiagPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnDiagReleased, RoutingStrategies.Tunnel);
        AddHandler(Button.ClickEvent, OnDiagClick, RoutingStrategies.Bubble);
#endif
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
#if DEBUG
        RemoveHandler(PointerPressedEvent, OnDiagPressed);
        RemoveHandler(PointerReleasedEvent, OnDiagReleased);
        RemoveHandler(Button.ClickEvent, OnDiagClick);
#endif
        base.OnDetachedFromVisualTree(e);
    }

#if DEBUG
    private static void OnDiagPressed(object? sender, PointerPressedEventArgs a)
    {
        var src = a.Source as Visual;
        var button = src?.FindAncestorOfType<Button>(true);
        var chain = string.Join(
            " < ",
            src?.GetVisualAncestors()
                .Take(6)
                .Select(v => $"{v.GetType().Name}#{(v as Control)?.Name}")
            ?? Enumerable.Empty<string>());

        Debug.WriteLine(
            $"[Diag] Pressed " +
            $"src={src?.GetType().Name}#{(src as Control)?.Name} " +
            $"bounds={src?.Bounds} " +
            $"classes={(src as StyledElement)?.Classes} " +
            $"button={button?.GetHashCode()} " +
            $"enabled={button?.IsEffectivelyEnabled} " +
            $"canExec={button?.Command?.CanExecute(button.CommandParameter)} " +
            $"mode={button?.ClickMode} " +
            $"handled={a.Handled}");
        Debug.WriteLine($"[Diag]   chain={chain}");
    }

    private static void OnDiagReleased(object? sender, PointerReleasedEventArgs a)
    {
        var button = (a.Source as Visual)?.FindAncestorOfType<Button>(true);
        Debug.WriteLine(
            $"[Diag] Released button={button?.GetHashCode()} " +
            $"src={a.Source} handled={a.Handled}");
    }

    private static void OnDiagClick(object? sender, RoutedEventArgs a)
    {
        Debug.WriteLine($"[Diag] Click button={a.Source?.GetHashCode()} src={a.Source}");
    }
#endif

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (TopLevel.GetTopLevel(this) is not Window parentWindow)
            return;

        parentWindow.MinWidth = 600;
        parentWindow.MinHeight = 400;

        parentWindow.Width = 800;
        parentWindow.Height = 1000;

        parentWindow.MaxWidth = 1600;
        parentWindow.MaxHeight = 900;

        var screen = parentWindow.Screens?.ScreenFromWindow(parentWindow) ?? parentWindow.Screens?.Primary;
        if (screen is not null)
        {
            var workArea = screen.WorkingArea;
            var scaling = screen.Scaling;
            var width = parentWindow.Width;
            var height = parentWindow.Height;

            var widthPx = width * scaling;
            var heightPx = height * scaling;

            int posX = workArea.X + (int)((workArea.Width - widthPx) / 2);
            int posY = workArea.Y + (int)((workArea.Height - heightPx) / 2);

            parentWindow.Position = new PixelPoint(posX, posY);
        }
    }

    private void NavTabs_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainMenuViewModel { IsSettingsDialogOpen: true } vm)
            vm.CloseSettingsCommand.Execute(null);
    }

    private void ProfileButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainMenuViewModel vm)
            return;

        if (!vm.IsSettingsDialogOpen)
            vm.OpenSettingsCommand.Execute(null);

        Dispatcher.UIThread.Post(SettingsPage.ScrollToAccount, DispatcherPriority.Background);
    }

    private void StreakOverlay_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainMenuViewModel vm && ReferenceEquals(e.Source, sender))
            vm.CloseStreakCelebrationCommand.Execute(null);
    }
}