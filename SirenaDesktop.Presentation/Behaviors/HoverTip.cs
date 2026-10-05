using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;

namespace SirenaDesktop.Presentation.Behaviors;

public static class HoverTip
{
    public static readonly AttachedProperty<object?> TipProperty =
        AvaloniaProperty.RegisterAttached<Control, object?>("Tip", typeof(HoverTip));

    public static readonly AttachedProperty<int> ShowDelayProperty =
        AvaloniaProperty.RegisterAttached<Control, int>("ShowDelay", typeof(HoverTip), 500);

    public static readonly AttachedProperty<int> HideDelayProperty =
        AvaloniaProperty.RegisterAttached<Control, int>("HideDelay", typeof(HoverTip), 150);

    public static readonly AttachedProperty<PlacementMode> PlacementProperty =
        AvaloniaProperty.RegisterAttached<Control, PlacementMode>("Placement", typeof(HoverTip), PlacementMode.Right);

    public static readonly AttachedProperty<double> HorizontalOffsetProperty =
        AvaloniaProperty.RegisterAttached<Control, double>("HorizontalOffset", typeof(HoverTip), 8.0);

    public static readonly AttachedProperty<double> VerticalOffsetProperty =
        AvaloniaProperty.RegisterAttached<Control, double>("VerticalOffset", typeof(HoverTip), 0.0);

    public static object? GetTip(Control c) => c.GetValue(TipProperty);
    public static void SetTip(Control c, object? v) => c.SetValue(TipProperty, v);

    public static int GetShowDelay(Control c) => c.GetValue(ShowDelayProperty);
    public static void SetShowDelay(Control c, int v) => c.SetValue(ShowDelayProperty, v);

    public static int GetHideDelay(Control c) => c.GetValue(HideDelayProperty);
    public static void SetHideDelay(Control c, int v) => c.SetValue(HideDelayProperty, v);

    public static PlacementMode GetPlacement(Control c) => c.GetValue(PlacementProperty);
    public static void SetPlacement(Control c, PlacementMode v) => c.SetValue(PlacementProperty, v);

    public static double GetHorizontalOffset(Control c) => c.GetValue(HorizontalOffsetProperty);
    public static void SetHorizontalOffset(Control c, double v) => c.SetValue(HorizontalOffsetProperty, v);

    public static double GetVerticalOffset(Control c) => c.GetValue(VerticalOffsetProperty);
    public static void SetVerticalOffset(Control c, double v) => c.SetValue(VerticalOffsetProperty, v);

    private sealed class State
    {
        public Popup? Popup;
        public DispatcherTimer? ShowTimer;
        public DispatcherTimer? HideTimer;
    }

    private static readonly ConditionalWeakTable<Control, State> States = new();
    private static State GetState(Control c) => States.GetOrCreateValue(c);

    static HoverTip()
    {
        TipProperty.Changed.AddClassHandler<Control>(OnTipChanged);
    }

    private static void OnTipChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        var had = e.OldValue is not null;
        var has = e.NewValue is not null;

        if (has && !had)
        {
            control.PointerEntered += OnPointerEntered;
            control.PointerExited += OnPointerExited;
            control.DetachedFromVisualTree += OnDetached;
        }
        else if (!has && had)
        {
            control.PointerEntered -= OnPointerEntered;
            control.PointerExited -= OnPointerExited;
            control.DetachedFromVisualTree -= OnDetached;
            Close(control);
        }
    }

    private static void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control c) Close(c);
    }

    private static void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not Control c) return;
        var state = GetState(c);
        CancelTimer(ref state.HideTimer);
        CancelTimer(ref state.ShowTimer);

        var delay = Math.Max(0, GetShowDelay(c));
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(delay) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            state.ShowTimer = null;
            Show(c);
        };
        state.ShowTimer = timer;
        timer.Start();
    }

    private static void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is not Control c) return;
        var state = GetState(c);
        CancelTimer(ref state.ShowTimer);
        if (state.Popup is not null)
            StartHideTimer(c);
    }

    private static void StartHideTimer(Control c)
    {
        var state = GetState(c);
        CancelTimer(ref state.HideTimer);

        var delay = Math.Max(0, GetHideDelay(c));
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(delay) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            state.HideTimer = null;
            Close(c);
        };
        state.HideTimer = timer;
        timer.Start();
    }

    private static void CancelTimer(ref DispatcherTimer? timer)
    {
        if (timer is null) return;
        timer.Stop();
        timer = null;
    }

    private static void Show(Control c)
    {
        var tip = GetTip(c);
        if (tip is null) return;

        var state = GetState(c);
        if (state.Popup is not null)
        {
            state.Popup.IsOpen = true;
            return;
        }

        var content = new Border { Classes = { "hoverTip" } };
        content.Child = new ContentControl { Content = tip };

        var popup = new Popup
        {
            PlacementTarget = c,
            Placement = GetPlacement(c),
            HorizontalOffset = GetHorizontalOffset(c),
            VerticalOffset = GetVerticalOffset(c),
            IsLightDismissEnabled = false,
            Child = content,
        };

        content.PointerEntered += (_, _) =>
        {
            var s = GetState(c);
            CancelTimer(ref s.HideTimer);
        };
        content.PointerExited += (_, _) => StartHideTimer(c);

        state.Popup = popup;
        popup.IsOpen = true;
    }

    private static void Close(Control c)
    {
        var state = GetState(c);
        CancelTimer(ref state.ShowTimer);
        CancelTimer(ref state.HideTimer);

        if (state.Popup is not null)
        {
            state.Popup.IsOpen = false;
            state.Popup = null;
        }
    }
}