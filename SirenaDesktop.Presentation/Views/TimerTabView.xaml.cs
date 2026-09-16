using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using SirenaDesktop.ViewModels;

namespace SirenaDesktop.Views;

public partial class TimerTabView : UserControl
{
    private const double CollapsedWidth = 40;
    private const double CollapsedHoverWidth = 46;        
    private const double ExpandedWidth = 300;

    private readonly TimerTabViewModel? _viewModel;

    public TimerTabView()
    {
        InitializeComponent();

        _viewModel = new TimerTabViewModel();
        DataContext = _viewModel;

        _viewModel.IsHistoryCollapsed = true;
        HistoryPanelColumn.Width = new GridLength(CollapsedWidth);

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        RailButton.MouseEnter += OnRailMouseEnter;
        RailButton.MouseLeave += OnRailMouseLeave;
    }

    private void OnRailMouseEnter(object sender, MouseEventArgs e)
    {
        if (_viewModel is null || !_viewModel.IsHistoryCollapsed) return;
        AnimateColumnWidth(CollapsedHoverWidth, TimeSpan.FromSeconds(0.12), true);
    }

    private void OnRailMouseLeave(object sender, MouseEventArgs e)
    {
        if (_viewModel is null || !_viewModel.IsHistoryCollapsed)
            return;

        AnimateColumnWidth(CollapsedWidth, TimeSpan.FromSeconds(0.12), true);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TimerTabViewModel.IsHistoryCollapsed))
            return;

        var collapse = _viewModel!.IsHistoryCollapsed;
        AnimateColumnWidth(collapse ? CollapsedWidth : ExpandedWidth,
            TimeSpan.FromSeconds(collapse ? 0.16 : 0.2),
            false);
    }

    private void AnimateColumnWidth(double toWidth, TimeSpan duration, bool forHover)
    {
        var fromWidth = HistoryPanelColumn.Width.Value;

        var animation = new GridLengthAnimation
        {
            From = new GridLength(fromWidth),
            To = new GridLength(toWidth),
            Duration = new Duration(duration),
            EasingFunction = new BackEase
            {
                EasingMode = EasingMode.EaseOut,
                Amplitude = forHover ? 0.3 : 0.5
            }
        };

        HistoryPanelColumn.BeginAnimation(ColumnDefinition.WidthProperty, animation);
    }
}

public class TimeSpanToMinutesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan ts)
            return $"{(int)Math.Round(ts.TotalMinutes)}m";
        return "0m";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class GridLengthAnimation : AnimationTimeline
{
    public static readonly DependencyProperty FromProperty =
        DependencyProperty.Register(nameof(From), typeof(GridLength), typeof(GridLengthAnimation));

    public static readonly DependencyProperty ToProperty =
        DependencyProperty.Register(nameof(To), typeof(GridLength), typeof(GridLengthAnimation));

    public override Type TargetPropertyType => typeof(GridLength);

    public GridLength From
    {
        get => (GridLength)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    public GridLength To
    {
        get => (GridLength)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    public IEasingFunction? EasingFunction { get; set; }

    protected override Freezable CreateInstanceCore()
    {
        return new GridLengthAnimation();
    }

    public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue,
        AnimationClock clock)
    {
        if (!clock.CurrentProgress.HasValue)
            return From;

        var progress = clock.CurrentProgress.Value;
        if (EasingFunction is not null)
            progress = EasingFunction.Ease(progress);

        var fromValue = From.Value;
        var toValue = To.Value;
        var current = fromValue + (toValue - fromValue) * progress;

        return new GridLength(Math.Max(0, current));
    }
}