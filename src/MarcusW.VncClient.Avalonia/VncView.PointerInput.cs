using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;

namespace MarcusW.VncClient.Avalonia
{
    public partial class VncView
    {
        private Dictionary<IPointer, PointerPoint> _touchPointers = new Dictionary<IPointer, PointerPoint>();

        /// <inheritdoc />
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (e.Handled)
                return;

            // Handle two-finger movements
            if (_touchPointers.Count > 1)
            {

                e.Handled = true;
                return;
            }

            PointerPoint point = e.GetCurrentPoint(this);
            if (HandleMouseEvent(point, Vector.Zero))
                e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (e.Handled)
                return;

            PointerPoint point = e.GetCurrentPoint(this);

            if (e.Pointer.Type == PointerType.Touch)
            {
                if (!_touchPointers.ContainsKey(e.Pointer))
                    _touchPointers.Add(e.Pointer, point);
            }

            if (HandleMouseEvent(point, Vector.Zero))
                e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (e.Handled)
                return;

            PointerPoint point = e.GetCurrentPoint(this);

            if (e.Pointer.Type == PointerType.Touch)
                _touchPointers.Remove(e.Pointer);

            if (HandleMouseEvent(point, Vector.Zero))
                e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            if (e.Handled)
                return;

            PointerPoint point = e.GetCurrentPoint(this);
            if (HandleMouseEvent(point, e.Delta))
                e.Handled = true;
        }

        private bool HandleMouseEvent(PointerPoint pointerPoint, Vector wheelDelta)
        {
            RfbConnection? connection = Connection;
            if (connection == null)
                return false;
            Console.WriteLine(wheelDelta);

            Position position = Conversions.GetPosition(pointerPoint.Position);

            MouseButtons buttonsMask = GetButtonsMask(pointerPoint.Properties);
            MouseButtons wheelMask = GetWheelMask(wheelDelta);

            // For scrolling, set the wheel buttons and remove them quickly after that.
            if (wheelMask != MouseButtons.None)
                connection.EnqueueMessage(new PointerEventMessage(position, buttonsMask | wheelMask));
            connection.EnqueueMessage(new PointerEventMessage(position, buttonsMask));

            return true;
        }

        private MouseButtons GetButtonsMask(PointerPointProperties pointProperties)
        {
            var mask = MouseButtons.None;

            if (pointProperties.IsLeftButtonPressed)
                mask |= MouseButtons.Left;
            if (pointProperties.IsMiddleButtonPressed)
                mask |= MouseButtons.Middle;
            if (pointProperties.IsRightButtonPressed)
                mask |= MouseButtons.Right;

            return mask;
        }

        private MouseButtons GetWheelMask(Vector wheelDelta)
        {
            var mask = MouseButtons.None;

            if (wheelDelta.X > 0)
                mask |= MouseButtons.WheelRight;
            else if (wheelDelta.X < 0)
                mask |= MouseButtons.WheelLeft;

            if (wheelDelta.Y > 0)
                mask |= MouseButtons.WheelUp;
            else if (wheelDelta.Y < 0)
                mask |= MouseButtons.WheelDown;

            return mask;
        }
    }
}
