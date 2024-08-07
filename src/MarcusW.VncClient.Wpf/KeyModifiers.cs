using System;
using JetBrains.Annotations;

namespace MarcusW.VncClient.Wpf;

[Flags]
[PublicAPI]
internal enum KeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Meta = 8,
}
