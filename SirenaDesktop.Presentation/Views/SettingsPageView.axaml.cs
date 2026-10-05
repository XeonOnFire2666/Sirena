using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using SirenaDesktop.ViewModels;
using System.ComponentModel;

namespace SirenaDesktop.Views;

public partial class SettingsPageView : UserControl
{
    public SettingsPageView()
    {
        InitializeComponent();
    }

    public void ScrollToAccount()
    {
        SettingsScroll.UpdateLayout();

        var point = AccountSection.TranslatePoint(default, ContentPanel);
        if (point is { } p)
            SettingsScroll.Offset = new Vector(0, p.Y);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is INotifyPropertyChanged notifying)
            notifying.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainMenuViewModel.IsTimerSettingsSectionExpanded)) return;
        if (sender is not MainMenuViewModel { IsTimerSettingsSectionExpanded: true }) return;

        Dispatcher.UIThread.Post(ScrollToTimerSection, DispatcherPriority.Loaded);
    }

    private void ScrollToTimerSection()
    {
        var scroll = this.FindControl<ScrollViewer>("SettingsScroll");
        var section = this.FindControl<StackPanel>("TimerSettingsSection");

        if (scroll is null || section is null) return;

        var transform = section.TransformToVisual(scroll);
        if (transform is null) return;

        var offsetY = transform.Value.Transform(new Avalonia.Point(0, 0)).Y + scroll.Offset.Y;
        scroll.Offset = new Avalonia.Vector(scroll.Offset.X, Math.Max(0, offsetY - 16));
    }
}