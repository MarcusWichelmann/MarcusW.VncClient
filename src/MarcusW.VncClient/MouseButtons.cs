using System;

namespace MarcusW.VncClient
{
    /// <summary>
    /// The different buttons of a mouse to describe pointer events.
    /// </summary>
    [Flags]
    public enum MouseButtons : byte
    {
        None = 0,
        Left = 1 << 0,
        Middle = 1 << 1,
        Right = 1 << 2,
        WheelUp = 1 << 3,
        WheelDown = 1 << 4,
        WheelLeft = 1 << 5,
        WheelRight = 1 << 6
    }
}
