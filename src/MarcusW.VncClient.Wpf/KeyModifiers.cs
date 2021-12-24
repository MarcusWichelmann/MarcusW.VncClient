using System;

namespace MarcusW.VncClient.Wpf;

[Flags]
internal enum KeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Meta = 8,
}
