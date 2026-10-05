using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using SirenaDesktop.ViewModels;
using SukiUI.Controls;

namespace SirenaDesktop;

public partial class MainWindow : SukiWindow
{
    private static readonly Geometry MaximizeGeometry = Geometry.Parse("M0,0 H10 V10 H0 Z");
    private static readonly Geometry RestoreGeometry = Geometry.Parse("M2,0 H10 V8 M0,2 H8 V10 H0 Z");

    private PixelPoint _restorePosition;
    private Size _restoreSize;
    private bool _hasRestoreBounds;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty)
        {
            MaximizeIcon.Data = WindowState == WindowState.Maximized
                ? RestoreGeometry
                : MaximizeGeometry;
        }
    }

    private void RememberNormalBounds()
    {
        if (WindowState != WindowState.Normal || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        _restoreSize = new Size(Bounds.Width, Bounds.Height);
        _restorePosition = Position;
        _hasRestoreBounds = true;
    }

    private void RestoreNormalBounds()
    {
        if (!_hasRestoreBounds || WindowState != WindowState.Normal)
        {
            return;
        }

        Width = _restoreSize.Width;
        Height = _restoreSize.Height;
        Position = _restorePosition;
    }

    private void DragArea_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void DragArea_DoubleTapped(object? sender, TappedEventArgs e)
    {
        ToggleMaximize();
    }

    private void Minimize_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object? sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleMaximize()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            Dispatcher.UIThread.Post(RestoreNormalBounds);
        }
        else
        {
            RememberNormalBounds();
            WindowState = WindowState.Maximized;
        }
    }
}