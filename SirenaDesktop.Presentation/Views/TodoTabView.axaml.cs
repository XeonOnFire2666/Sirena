using Avalonia.Controls;
using Avalonia.Input;
using SirenaDesktop.ViewModels;

namespace SirenaDesktop.Views;

public partial class DayPlanTabView : UserControl
{
    private const double WideBreakpoint = 1080;

    public DayPlanTabView()
    {
        InitializeComponent();
        SizeChanged += (_, e) => LayoutRoot.Classes.Set("wide", e.NewSize.Width >= WideBreakpoint);
        WeekCard.PointerWheelChanged += OnWeekWheel;
    }

    private void OnWeekWheel(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not TodoTabViewModel vm)
        {
            return;
        }

        var command = e.Delta.Y > 0 ? vm.PrevWeekCommand : vm.NextWeekCommand;
        if (command.CanExecute(null))
        {
            command.Execute(null);
        }

        e.Handled = true;
    }
}