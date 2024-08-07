using JetBrains.Annotations;

namespace MarcusW.VncClient.Skia;

[Flags]
[PublicAPI]
public enum KeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Meta = 8,
}
