using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using SirenaDesktop.ViewModels;

namespace SirenaDesktop.Views;

public partial class TimerTabView : UserControl
{
    private const double CollapsedWidth = 40;
    private const double CollapsedHoverWidth = 46;
    private const double ExpandedWidth = 300;

    private TimerTabViewModel? _viewModel;

    private ColumnDefinition HistoryPanelColumn => MainGrid.ColumnDefinitions[1];

    public TimerTabView()
    {
        InitializeComponent();

        RailButton.PointerEntered += OnRailPointerEntered;
        RailButton.PointerExited += OnRailPointerExited;

        DataContextChanged += OnDataContextChanged;
    }

    public TimerTabView(TimerTabViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = DataContext as TimerTabViewModel;

        if (_viewModel is null)
            return;

        _viewModel.IsHistoryCollapsed = true;
        HistoryPanelColumn.Width = new GridLength(CollapsedWidth);

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnRailPointerEntered(object? sender, PointerEventArgs e)
    {
        if (_viewModel is null || !_viewModel.IsHistoryCollapsed) return;
        HistoryPanelColumn.Width = new GridLength(CollapsedHoverWidth);
    }

    private void OnRailPointerExited(object? sender, PointerEventArgs e)
    {
        if (_viewModel is null || !_viewModel.IsHistoryCollapsed) return;
        HistoryPanelColumn.Width = new GridLength(CollapsedWidth);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TimerTabViewModel.IsHistoryCollapsed))
            return;

        var collapse = _viewModel!.IsHistoryCollapsed;
        HistoryPanelColumn.Width = new GridLength(collapse ? CollapsedWidth : ExpandedWidth);
    }
}

public class TimeSpanToMinutesConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan ts)
            return $"{(int)Math.Round(ts.TotalMinutes)}m";
        return "0m";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}