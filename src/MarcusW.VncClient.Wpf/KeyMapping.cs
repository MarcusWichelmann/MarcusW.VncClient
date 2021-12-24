using System.Windows.Input;

namespace MarcusW.VncClient.Wpf;

/// <summary>
///     Provides methods for mapping keys.
/// </summary>
public static class KeyMapping
{
    /// <summary>
    ///     Maps a printable char to a <see cref="KeySymbol" />.
    /// </summary>
    /// <param name="c">The char.</param>
    /// <returns>The X key symbol.</returns>
    public static KeySymbol GetSymbolFromChar(char c)
    {
        if (c >= ' ' && c <= '~')
        {
            return KeySymbol.space + (c - ' ');
        }

        return (KeySymbol)(0x1000000 | c);
    }

    /// <summary>
    ///     Maps an Avalonia <see cref="Key" /> to a <see cref="KeySymbol" />.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="includePrintable">True, if printable chars should be included in the mapping, false otherwise.</param>
    /// <returns>The X key symbol.</returns>
    public static KeySymbol GetSymbolFromKey(Key key, bool includePrintable = true)
    {
        KeySymbol keySymbol = key switch {
            Key.Cancel     => KeySymbol.Cancel,
            Key.Back       => KeySymbol.BackSpace,
            Key.Tab        => KeySymbol.Tab,
            Key.LineFeed   => KeySymbol.Linefeed,
            Key.Clear      => KeySymbol.Clear,
            Key.Return     => KeySymbol.Return,
            Key.Pause      => KeySymbol.Pause,
            Key.CapsLock   => KeySymbol.Caps_Lock,
            Key.Escape     => KeySymbol.Escape,
            Key.Prior      => KeySymbol.Prior,
            Key.PageDown   => KeySymbol.Page_Down,
            Key.End        => KeySymbol.End,
            Key.Home       => KeySymbol.Home,
            Key.Left       => KeySymbol.Left,
            Key.Up         => KeySymbol.Up,
            Key.Right      => KeySymbol.Right,
            Key.Down       => KeySymbol.Down,
            Key.Select     => KeySymbol.Select,
            Key.Print      => KeySymbol.Print,
            Key.Execute    => KeySymbol.Execute,
            Key.Insert     => KeySymbol.Insert,
            Key.Delete     => KeySymbol.Delete,
            Key.Help       => KeySymbol.Help,
            Key.LWin       => KeySymbol.Super_L,
            Key.RWin       => KeySymbol.Super_R,
            Key.Apps       => KeySymbol.Menu,
            Key.F1         => KeySymbol.F1,
            Key.F2         => KeySymbol.F2,
            Key.F3         => KeySymbol.F3,
            Key.F4         => KeySymbol.F4,
            Key.F5         => KeySymbol.F5,
            Key.F6         => KeySymbol.F6,
            Key.F7         => KeySymbol.F7,
            Key.F8         => KeySymbol.F8,
            Key.F9         => KeySymbol.F9,
            Key.F10        => KeySymbol.F10,
            Key.F11        => KeySymbol.F11,
            Key.F12        => KeySymbol.F12,
            Key.F13        => KeySymbol.F13,
            Key.F14        => KeySymbol.F14,
            Key.F15        => KeySymbol.F15,
            Key.F16        => KeySymbol.F16,
            Key.F17        => KeySymbol.F17,
            Key.F18        => KeySymbol.F18,
            Key.F19        => KeySymbol.F19,
            Key.F20        => KeySymbol.F20,
            Key.F21        => KeySymbol.F21,
            Key.F22        => KeySymbol.F22,
            Key.F23        => KeySymbol.F23,
            Key.F24        => KeySymbol.F24,
            Key.NumLock    => KeySymbol.Num_Lock,
            Key.Scroll     => KeySymbol.Scroll_Lock,
            Key.LeftShift  => KeySymbol.Shift_L,
            Key.RightShift => KeySymbol.Shift_R,
            Key.LeftCtrl   => KeySymbol.Control_L,
            Key.RightCtrl  => KeySymbol.Control_R,
            Key.LeftAlt    => KeySymbol.Alt_L,
            Key.RightAlt   => KeySymbol.Alt_R,
            var _          => KeySymbol.Null,
        };

        if (keySymbol == KeySymbol.Null && includePrintable)
        {
            keySymbol = key switch {
                Key.Space    => KeySymbol.space,
                Key.A        => KeySymbol.a,
                Key.B        => KeySymbol.b,
                Key.C        => KeySymbol.c,
                Key.D        => KeySymbol.d,
                Key.E        => KeySymbol.e,
                Key.F        => KeySymbol.f,
                Key.G        => KeySymbol.g,
                Key.H        => KeySymbol.h,
                Key.I        => KeySymbol.i,
                Key.J        => KeySymbol.j,
                Key.K        => KeySymbol.k,
                Key.L        => KeySymbol.l,
                Key.M        => KeySymbol.m,
                Key.N        => KeySymbol.n,
                Key.O        => KeySymbol.o,
                Key.P        => KeySymbol.p,
                Key.Q        => KeySymbol.q,
                Key.R        => KeySymbol.r,
                Key.S        => KeySymbol.s,
                Key.T        => KeySymbol.t,
                Key.U        => KeySymbol.u,
                Key.V        => KeySymbol.v,
                Key.W        => KeySymbol.w,
                Key.X        => KeySymbol.x,
                Key.Y        => KeySymbol.y,
                Key.Z        => KeySymbol.z,
                Key.NumPad0  => KeySymbol.KP_0,
                Key.NumPad1  => KeySymbol.KP_1,
                Key.NumPad2  => KeySymbol.KP_2,
                Key.NumPad3  => KeySymbol.KP_3,
                Key.NumPad4  => KeySymbol.KP_4,
                Key.NumPad5  => KeySymbol.KP_5,
                Key.NumPad6  => KeySymbol.KP_6,
                Key.NumPad7  => KeySymbol.KP_7,
                Key.NumPad8  => KeySymbol.KP_8,
                Key.NumPad9  => KeySymbol.KP_9,
                Key.Multiply => KeySymbol.KP_Multiply,
                Key.Add      => KeySymbol.KP_Add,
                Key.Subtract => KeySymbol.KP_Subtract,
                Key.Decimal  => KeySymbol.KP_Decimal,
                Key.Divide   => KeySymbol.KP_Divide,
                Key.D1       => KeySymbol.XK_1,
                Key.D2       => KeySymbol.XK_2,
                Key.D3       => KeySymbol.XK_3,
                Key.D4       => KeySymbol.XK_4,
                Key.D5       => KeySymbol.XK_5,
                Key.D6       => KeySymbol.XK_6,
                Key.D7       => KeySymbol.XK_7,
                Key.D8       => KeySymbol.XK_8,
                Key.D9       => KeySymbol.XK_9,
                Key.D0       => KeySymbol.XK_0,
                var _        => KeySymbol.Null,
            };
        }

        return keySymbol;
    }
}
