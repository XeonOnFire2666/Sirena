using System;
using System.Collections;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace SirenaDesktop.Presentation.Behaviors
{
    public static class DragReorder
    {
        public static readonly AttachedProperty<bool> IsEnabledProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(DragReorder));

        public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);

        public static readonly AttachedProperty<string?> GroupProperty =
            AvaloniaProperty.RegisterAttached<Control, string?>("Group", typeof(DragReorder));

        public static string? GetGroup(Control control) => control.GetValue(GroupProperty);
        public static void SetGroup(Control control, string? value) => control.SetValue(GroupProperty, value);

        public static readonly AttachedProperty<int> HoldMillisecondsProperty =
            AvaloniaProperty.RegisterAttached<Control, int>("HoldMilliseconds", typeof(DragReorder), 140);

        public static int GetHoldMilliseconds(Control control) => control.GetValue(HoldMillisecondsProperty);
        public static void SetHoldMilliseconds(Control control, int value) => control.SetValue(HoldMillisecondsProperty, value);

        static DragReorder()
        {
            IsEnabledProperty.Changed.AddClassHandler<Control>((control, e) =>
            {
                if (e.NewValue is true)
                    Attach(control);
                else
                    Detach(control);
            });
        }

        public static void EnsureAttached(Control control)
        {
            if (GetIsEnabled(control) && control.GetValue(StateProperty) is null)
                Attach(control);
        }

        private static void Attach(Control control)
        {
            if (control is Panel { Background: null } panel)
                panel.Background = Brushes.Transparent;

            var state = new DragState(control);
            control.SetValue(StateProperty, state);

            control.AddHandler(InputElement.PointerPressedEvent, state.OnPointerPressed,
                RoutingStrategies.Bubble, handledEventsToo: true);
            control.AddHandler(InputElement.PointerMovedEvent, state.OnPointerMoved,
                RoutingStrategies.Bubble, handledEventsToo: true);
            control.AddHandler(InputElement.PointerReleasedEvent, state.OnPointerReleased,
                RoutingStrategies.Bubble, handledEventsToo: true);
            control.AddHandler(InputElement.PointerCaptureLostEvent, state.OnPointerCaptureLost,
                RoutingStrategies.Bubble, handledEventsToo: true);
        }

        private static void Detach(Control control)
        {
            if (control.GetValue(StateProperty) is DragState state)
            {
                control.RemoveHandler(InputElement.PointerPressedEvent, state.OnPointerPressed);
                control.RemoveHandler(InputElement.PointerMovedEvent, state.OnPointerMoved);
                control.RemoveHandler(InputElement.PointerReleasedEvent, state.OnPointerReleased);
                control.RemoveHandler(InputElement.PointerCaptureLostEvent, state.OnPointerCaptureLost);
                state.Dispose();
            }
        }

        private static readonly AttachedProperty<DragState?> StateProperty =
            AvaloniaProperty.RegisterAttached<Control, DragState?>("State", typeof(DragReorder));

        private sealed class DragState : IDisposable
        {
            private const double Stiffness = 210.0;
            private const double Damping = 26.0;
            private const double SettleVelocity = 2.0;
            private const double SettleDistance = 0.4;
            private const double DragSlop = 6.0;

            private readonly Control _card;
            private Point _pointerDownScreenPos;
            private DispatcherTimer? _holdTimer;
            private bool _isDragging;
            private bool _isPrimed;
            private IPointer? _pointer;

            private TranslateTransform? _translate;
            private Point _targetOffset;
            private Point _currentOffset;
            private Point _velocity;

            private Point _origin;
            private ItemsControl? _itemsControl;
            private Panel? _itemsPanel;

            private bool _renderLoopRunning;

            private Control? _startContainer;
            private Control? _pendingTargetContainer;
            private Control? _pendingTargetCard;
            private int _startIndex = -1;
            private int _pendingIndex = -1;

            private readonly System.Collections.Generic.List<(Control control, bool original)> _unclippedAncestors = new();

            public DragState(Control card)
            {
                _card = card;
            }

            public void OnPointerPressed(object? sender, PointerPressedEventArgs e)
            {
                if (_isDragging || _isPrimed) return;
                if (!e.GetCurrentPoint(_card).Properties.IsLeftButtonPressed) return;

                _isPrimed = true;
                _pointer = e.Pointer;
                _pointerDownScreenPos = e.GetPosition(null);

                var hold = GetHoldMilliseconds(_card);
                _holdTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(hold) };
                _holdTimer.Tick += (_, _) =>
                {
                    _holdTimer?.Stop();
                    if (_isPrimed) BeginDrag(e.GetPosition(null));
                };
                _holdTimer.Start();
            }

            public void OnPointerMoved(object? sender, PointerEventArgs e)
            {
                if (!_isPrimed && !_isDragging) return;

                var screenPos = e.GetPosition(null);

                if (_isPrimed && !_isDragging)
                {
                    var dist = Distance(screenPos, _pointerDownScreenPos);
                    if (dist > DragSlop)
                    {
                        _holdTimer?.Stop();
                        BeginDrag(screenPos);
                    }
                    else
                    {
                        return;
                    }
                }

                if (_isDragging)
                {
                    UpdateTarget(screenPos);
                    e.Handled = true;
                }
            }

            public void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
            {
                CancelPrime();
                if (_isDragging) EndDrag(commit: true);
            }

            public void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
            {
                CancelPrime();
                if (_isDragging) EndDrag(commit: false);
            }

            private void CancelPrime()
            {
                _isPrimed = false;
                _holdTimer?.Stop();
                _holdTimer = null;
            }

            private static double Distance(Point a, Point b)
            {
                var dx = a.X - b.X;
                var dy = a.Y - b.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            private void BeginDrag(Point screenPos)
            {
                _isPrimed = false;
                _isDragging = true;

                _itemsControl = _card.FindAncestorOfType<ItemsControl>();
                _itemsPanel = _itemsControl?.ItemsPanelRoot as Panel;

                _startContainer = _itemsPanel is not null
                    ? FindContainerForCard(_card, _itemsPanel)
                    : null;

                _startIndex = (_itemsPanel is not null && _startContainer is not null)
                    ? _itemsPanel.Children.IndexOf(_startContainer)
                    : -1;

                _pendingIndex = _startIndex;
                _pendingTargetContainer = null;
                _pendingTargetCard = null;

                _translate = new TranslateTransform();
                _card.RenderTransform = _translate;
                _card.RenderTransformOrigin = RelativePoint.Center;
                _card.ZIndex = 1000;

                _card.Classes.Add("dragging");

                DisableAncestorClipping();

                _origin = screenPos;
                _targetOffset = default;
                _currentOffset = default;
                _velocity = default;

                _pointer?.Capture(_card);

                StartRenderLoop();
            }

            private void DisableAncestorClipping()
            {
                _unclippedAncestors.Clear();
                var current = _card.GetVisualParent();
                while (current is not null)
                {
                    if (current is Control c && c.ClipToBounds)
                    {
                        _unclippedAncestors.Add((c, true));
                        c.ClipToBounds = false;
                    }
                    current = current.GetVisualParent();
                }
            }

            private void RestoreAncestorClipping()
            {
                foreach (var (control, original) in _unclippedAncestors)
                    control.ClipToBounds = original;

                _unclippedAncestors.Clear();
            }

            private void UpdateTarget(Point screenPos)
            {
                _targetOffset = new Point(screenPos.X - _origin.X, screenPos.Y - _origin.Y);
                UpdatePendingSlot();
            }

            private void StartRenderLoop()
            {
                if (_renderLoopRunning) return;

                var topLevel = TopLevel.GetTopLevel(_card);
                if (topLevel is null) return;

                _renderLoopRunning = true;

                var last = TimeSpan.MinValue;

                void OnFrame(TimeSpan time)
                {
                    if (_translate is null && !_isDragging)
                    {
                        _renderLoopRunning = false;
                        return;
                    }

                    if (last == TimeSpan.MinValue)
                        last = time;

                    var dt = Math.Min((time - last).TotalSeconds, 1.0 / 30.0);
                    last = time;

                    StepSpring(dt);

                    if (_translate is not null || _isDragging)
                        topLevel.RequestAnimationFrame(OnFrame);
                    else
                        _renderLoopRunning = false;
                }

                topLevel.RequestAnimationFrame(OnFrame);
            }

            private void StepSpring(double dt)
            {
                if (_translate is null) return;
                if (dt <= 0) return;

                StepAxis(ref _currentOffset, ref _velocity, _targetOffset, dt);

                _translate.X = _currentOffset.X;
                _translate.Y = _currentOffset.Y;

                if (!_isDragging)
                {
                    var dist = Distance(_currentOffset, _targetOffset);
                    var speed = Math.Sqrt(_velocity.X * _velocity.X + _velocity.Y * _velocity.Y);
                    if (dist < SettleDistance && speed < SettleVelocity)
                    {
                        _translate.X = _targetOffset.X;
                        _translate.Y = _targetOffset.Y;
                        FinishRelease();
                    }
                }
            }

            private void StepAxis(ref Point current, ref Point velocity, Point target, double dt)
            {
                var (nx, vx) = StepOne(current.X, velocity.X, target.X, dt);
                var (ny, vy) = StepOne(current.Y, velocity.Y, target.Y, dt);
                current = new Point(nx, ny);
                velocity = new Point(vx, vy);
            }

            private static (double pos, double vel) StepOne(double cur, double vel, double tgt, double dt)
            {
                var displacement = cur - tgt;
                var springAccel = -Stiffness * displacement;
                var dampAccel = -Damping * vel;
                var accel = springAccel + dampAccel;
                var newVel = vel + accel * dt;
                var newPos = cur + newVel * dt;
                return (newPos, newVel);
            }

            private void UpdatePendingSlot()
            {
                if (_itemsPanel is null) return;

                var cardCenter = _card.TranslatePoint(
                    new Point(_card.Bounds.Width / 2, _card.Bounds.Height / 2), _itemsPanel);
                if (cardCenter is null) return;

                Control? bestContainer = null;
                Control? bestCard = null;
                double bestDist = double.MaxValue;

                foreach (var child in _itemsPanel.Children)
                {
                    if (child is not Control otherContainer) continue;
                    if (ReferenceEquals(otherContainer, _startContainer)) continue;

                    var otherCard = FindCardInContainer(otherContainer);
                    if (otherCard is null) continue;
                    if (GetGroup(otherCard) != GetGroup(_card)) continue;

                    var otherCenter = otherCard.TranslatePoint(
                        new Point(otherCard.Bounds.Width / 2, otherCard.Bounds.Height / 2), _itemsPanel);
                    if (otherCenter is null) continue;

                    var d = Distance(cardCenter.Value, otherCenter.Value);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestContainer = otherContainer;
                        bestCard = otherCard;
                    }
                }

                if (_pendingTargetCard is not null && !ReferenceEquals(_pendingTargetCard, bestCard))
                    _pendingTargetCard.Classes.Remove("drop-target");

                if (bestContainer is null || bestCard is null)
                {
                    _pendingTargetContainer = null;
                    _pendingTargetCard = null;
                    _pendingIndex = _startIndex;
                    return;
                }

                var threshold = Math.Min(bestContainer.Bounds.Width, bestContainer.Bounds.Height) * 0.5;
                if (bestDist > threshold)
                {
                    bestCard.Classes.Remove("drop-target");
                    _pendingTargetContainer = null;
                    _pendingTargetCard = null;
                    _pendingIndex = _startIndex;
                    return;
                }

                _pendingTargetContainer = bestContainer;
                _pendingTargetCard = bestCard;
                _pendingIndex = _itemsPanel.Children.IndexOf(bestContainer);
                bestCard.Classes.Add("drop-target");
            }

            private void EndDrag(bool commit)
            {
                _isDragging = false;
                _pointer?.Capture(null);

                _pendingTargetCard?.Classes.Remove("drop-target");

                if (commit
                    && _itemsControl?.ItemsSource is IList list
                    && _startIndex >= 0
                    && _pendingIndex >= 0
                    && _pendingIndex != _startIndex
                    && _startIndex < list.Count
                    && _pendingIndex < list.Count)
                {
                    var item = list[_startIndex];
                    list.RemoveAt(_startIndex);
                    list.Insert(_pendingIndex, item);
                }

                _pendingTargetContainer = null;
                _pendingTargetCard = null;
                _startContainer = null;
                _startIndex = -1;
                _pendingIndex = -1;

                RestoreAncestorClipping();

                _targetOffset = default;
                _card.Classes.Remove("dragging");
            }

            private void FinishRelease()
            {
                _renderLoopRunning = false;

                _card.ZIndex = 0;
                _card.RenderTransform = null;
                _translate = null;
            }

            public void Dispose()
            {
                _holdTimer?.Stop();
                _renderLoopRunning = false;
            }

            private static Control? FindContainerForCard(Control card, Panel itemsPanel)
            {
                Visual? current = card;
                while (current is not null)
                {
                    var parent = current.GetVisualParent();
                    if (parent is null) return null;
                    if (ReferenceEquals(parent, itemsPanel))
                        return current as Control;
                    current = parent;
                }
                return null;
            }

            private static Control? FindCardInContainer(Control container)
            {
                if (GetIsEnabled(container)) return container;

                foreach (var descendant in container.GetVisualDescendants().OfType<Control>())
                {
                    if (GetIsEnabled(descendant)) return descendant;
                }

                return null;
            }
        }
    }
}