// -----------------------------------------------------------------------
// <copyright file="UIElement.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Avalonia
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Avalonia.Input;
    using Avalonia.Media;

    public class UIElement : Visual, IInputElement
    {
        public static readonly DependencyProperty FocusableProperty =
            DependencyProperty.Register(
                "Focusable",
                typeof(bool),
                typeof(UIElement));

        public static readonly DependencyProperty IsKeyboardFocusedProperty =
            DependencyProperty.Register(
                "IsKeyboardFocused",
                typeof(bool),
                typeof(UIElement));

        public static readonly DependencyProperty IsMouseOverProperty =
            DependencyProperty.Register(
                "IsMouseOver",
                typeof(bool),
                typeof(UIElement));

        public static readonly DependencyProperty IsMouseCapturedProperty =
            DependencyProperty.Register(
                "IsMouseCaptured",
                typeof(bool),
                typeof(UIElement));

        public static readonly DependencyProperty OpacityProperty =
            DependencyProperty.Register(
                "Opacity",
                typeof(double),
                typeof(UIElement),
                new PropertyMetadata(1.0));

        public static readonly DependencyProperty SnapsToDevicePixelsProperty =
            DependencyProperty.Register(
                "SnapsToDevicePixels",
                typeof(bool),
                typeof(UIElement));

        public static readonly RoutedEvent GotKeyboardFocusEvent =
            Keyboard.GotKeyboardFocusEvent.AddOwner(typeof(UIElement));

        public static readonly RoutedEvent LostKeyboardFocusEvent =
            Keyboard.LostKeyboardFocusEvent.AddOwner(typeof(UIElement));

        public static readonly RoutedEvent KeyDownEvent =
            Keyboard.KeyDownEvent.AddOwner(typeof(UIElement));

        public static readonly RoutedEvent MouseEnterEvent =
            Mouse.MouseEnterEvent.AddOwner(typeof(UIElement));

        public static readonly RoutedEvent MouseLeaveEvent =
            Mouse.MouseLeaveEvent.AddOwner(typeof(UIElement));

        public static readonly RoutedEvent MouseLeftButtonDownEvent =
            Mouse.MouseLeftButtonDownEvent.AddOwner(typeof(UIElement));

        public static readonly RoutedEvent MouseLeftButtonUpEvent =
            Mouse.MouseLeftButtonUpEvent.AddOwner(typeof(UIElement));

        public static readonly RoutedEvent MouseMoveEvent =
            Mouse.MouseMoveEvent.AddOwner(typeof(UIElement));

        public static readonly RoutedEvent TextInputEvent =
            TextCompositionManager.TextInputEvent.AddOwner(typeof(UIElement));

        private bool measureCalled;
        private Size previousMeasureSize;
        private Dictionary<RoutedEvent, List<Delegate>> eventHandlers = new Dictionary<RoutedEvent, List<Delegate>>();

        public UIElement()
        {
            this.IsMeasureValid = true;
            this.IsArrangeValid = true;

            this.AddHandler(GotKeyboardFocusEvent, (KeyboardFocusChangedEventHandler)((s, e) => this.OnGotKeyboardFocus(e)));
            this.AddHandler(LostKeyboardFocusEvent, (KeyboardFocusChangedEventHandler)((s, e) => this.OnLostKeyboardFocus(e)));
            this.AddHandler(MouseEnterEvent, (MouseEventHandler)((s, e) => this.OnMouseEnter(e)));
            this.AddHandler(MouseLeaveEvent, (MouseEventHandler)((s, e) => this.OnMouseLeave(e)));
            this.AddHandler(MouseLeftButtonDownEvent, (MouseButtonEventHandler)((s, e) => this.OnMouseLeftButtonDown(e)));
            this.AddHandler(MouseLeftButtonUpEvent, (MouseButtonEventHandler)((s, e) => this.OnMouseLeftButtonUp(e)));
            this.AddHandler(MouseMoveEvent, (MouseEventHandler)((s, e) => this.OnMouseMove(e)));
            this.AddHandler(TextInputEvent, (TextCompositionEventHandler)((s, e) => this.OnTextInput(e)));
        }

        public event KeyEventHandler KeyDown
        {
            add { this.AddHandler(KeyDownEvent, value); }
            remove { this.RemoveHandler(KeyDownEvent, value); }
        }

        public event MouseEventHandler MouseEnter
        {
            add { this.AddHandler(MouseEnterEvent, value); }
            remove { this.RemoveHandler(MouseEnterEvent, value); }
        }

        public event MouseEventHandler MouseLeave
        {
            add { this.AddHandler(MouseLeaveEvent, value); }
            remove { this.RemoveHandler(MouseLeaveEvent, value); }
        }

        public event MouseButtonEventHandler MouseLeftButtonDown
        {
            add { this.AddHandler(MouseLeftButtonDownEvent, value); }
            remove { this.RemoveHandler(MouseLeftButtonDownEvent, value); }
        }

        public event MouseButtonEventHandler MouseLeftButtonUp
        {
            add { this.AddHandler(MouseLeftButtonUpEvent, value); }
            remove { this.RemoveHandler(MouseLeftButtonUpEvent, value); }
        }

        public event MouseEventHandler MouseMove
        {
            add { this.AddHandler(MouseMoveEvent, value); }
            remove { this.RemoveHandler(MouseMoveEvent, value); }
        }

        public Size DesiredSize { get; set; }

        public bool IsMeasureValid { get; private set; }

        public bool IsArrangeValid { get; private set; }

        public bool IsVisible 
        { 
            get
            {
                // TODO: Implement visibility.
                return true;
            }
        }

        public Size RenderSize { get; private set; }

        public bool Focusable
        {
            get { return (bool)this.GetValue(FocusableProperty); }
            set { this.SetValue(FocusableProperty, value); }
        }

        public bool IsKeyboardFocused
        {
            get { return (bool)this.GetValue(IsKeyboardFocusedProperty); }
        }

        public bool IsMouseOver
        {
            get { return (bool)this.GetValue(IsMouseOverProperty); }
        }

        public bool IsMouseCaptured
        {
            get { return (bool)this.GetValue(IsMouseCapturedProperty); }
            private set { this.SetValue(IsMouseCapturedProperty, value); }
        }

        public double Opacity 
        {
            get { return (double)this.GetValue(OpacityProperty); }
            set { this.SetValue(OpacityProperty, value); }
        }

        // TODO: Actually implement this.
        public bool SnapsToDevicePixels
        {
            get { return (bool)this.GetValue(SnapsToDevicePixelsProperty); }
            set { this.SetValue(SnapsToDevicePixelsProperty, value); }
        }

        public void CaptureMouse()
        {
            Mouse.Capture(this);
            this.IsMouseCaptured = true;
        }

        public void ReleaseMouseCapture()
        {
            if (Mouse.Captured == this)
            {
                Mouse.Capture(null);
                this.IsMouseCaptured = false;
            }
        }

        public void Measure(Size availableSize)
        {
            this.measureCalled = true;
            this.previousMeasureSize = availableSize;
            this.DesiredSize = this.MeasureCore(availableSize);
            this.IsMeasureValid = true;
            this.IsArrangeValid = false;
        }

        public void Arrange(Rect finalRect)
        {
            if (!this.measureCalled || !this.IsMeasureValid)
            {
                this.Measure(this.measureCalled ? this.previousMeasureSize : finalRect.Size);
            }

            this.ArrangeCore(finalRect);
            this.IsArrangeValid = true;
        }

        public void InvalidateMeasure()
        {
            this.IsMeasureValid = false;
            LayoutManager.Instance.QueueMeasure(this);
        }

        public void InvalidateArrange()
        {
            this.IsArrangeValid = false;
            LayoutManager.Instance.QueueArrange(this);
        }

        public void InvalidateVisual()
        {
            this.InvalidateArrange();
        }

        public void UpdateLayout()
        {
            Size size = this.RenderSize;
            this.Measure(size);
            this.Arrange(new Rect(size));
        }

        public IInputElement InputHitTest(Point point)
        {
            Rect bounds = new Rect(this.RenderSize);

            if (bounds.Contains(point))
            {
                foreach (UIElement child in VisualTreeHelper.GetChildren(this).OfType<UIElement>())
                {
                    Point offsetPoint = point - child.VisualOffset;
                    IInputElement hit = child.InputHitTest(offsetPoint);

                    if (hit != null)
                    {
                        return hit;
                    }
                }

                return this;
            }
            else
            {
                return null;
            }
        }

        public void AddHandler(RoutedEvent routedEvent, Delegate handler)
        {
            if (routedEvent == null)
            {
                throw new ArgumentNullException("routedEvent");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            List<Delegate> delegates;

            if (!this.eventHandlers.TryGetValue(routedEvent, out delegates))
            {
                delegates = new List<Delegate>();
                this.eventHandlers.Add(routedEvent, delegates);
            }

            delegates.Add(handler);
        }

        public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
        {
            if (routedEvent == null)
            {
                throw new ArgumentNullException("routedEvent");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            List<Delegate> delegates;

            if (this.eventHandlers.TryGetValue(routedEvent, out delegates))
            {
                delegates.Remove(handler);
            }
        }

        public void RaiseEvent(RoutedEventArgs e)
        {
            if (e.RoutedEvent != null)
            {
                switch (e.RoutedEvent.RoutingStrategy)
                {
                    case RoutingStrategy.Bubble:
                        this.BubbleEvent(e);
                        break;
                    case RoutingStrategy.Direct:
                        this.RaiseEventImpl(e);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        internal override Rect GetHitTestBounds()
        {
            return new Rect((Point)this.VisualOffset, this.RenderSize);
        }

        protected internal virtual void OnRender(DrawingContext drawingContext)
        {
        }

        protected virtual Size MeasureCore(Size availableSize)
        {
            return new Size();
        }

        protected virtual void ArrangeCore(Rect finalRect)
        {
            this.VisualOffset = (Vector)finalRect.TopLeft;
            this.RenderSize = finalRect.Size;
        }

        protected virtual void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
        }

        protected virtual void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
        }

        protected virtual void OnMouseEnter(MouseEventArgs e)
        {
        }

        protected virtual void OnMouseLeave(MouseEventArgs e)
        {
        }

        protected virtual void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
        }

        protected virtual void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
        }

        protected virtual void OnMouseMove(MouseEventArgs e)
        {
        }

        protected virtual void OnTextInput(TextCompositionEventArgs e)
        {
        }

        private void BubbleEvent(RoutedEventArgs e)
        {
            UIElement target = this;

            while (target != null)
            {
                target.RaiseEventImpl(e);
                target = VisualTreeHelper.GetAncestor<UIElement>(target);
            }
        }

        private void RaiseEventImpl(RoutedEventArgs e)
        {
            List<Delegate> delegates;

            if (this.eventHandlers.TryGetValue(e.RoutedEvent, out delegates))
            {
                foreach (Delegate handler in delegates)
                {
                    // TODO: Implement the Handled stuff.
                    handler.DynamicInvoke(this, e);
                }
            }
        }
    }
}
