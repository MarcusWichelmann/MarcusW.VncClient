using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Platform;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Avalonia.Adapters.Rendering;

/// <inheritdoc />
public sealed class AvaloniaFramebufferReference : IFramebufferReference
{
    private Action? _invalidateVisual;
    private ILockedFramebuffer? _lockedFramebuffer;

    internal AvaloniaFramebufferReference(ILockedFramebuffer lockedFramebuffer, Action invalidateVisual)
    {
        _lockedFramebuffer = lockedFramebuffer;
        _invalidateVisual = invalidateVisual;
    }

    /// <inheritdoc />
    public IntPtr Address => ThrowIf(_lockedFramebuffer!.Address);

    /// <inheritdoc />
    public Size Size => ThrowIf(Conversions.GetSize(_lockedFramebuffer!.Size));

    /// <inheritdoc />
    public PixelFormat Format => ThrowIf(Conversions.GetPixelFormat(_lockedFramebuffer!.Format));

    /// <inheritdoc />
    public double HorizontalDpi => ThrowIf(_lockedFramebuffer!.Dpi.X);

    /// <inheritdoc />
    public double VerticalDpi => ThrowIf(_lockedFramebuffer!.Dpi.Y);

    /// <inheritdoc />
    public void Dispose()
    {
        _lockedFramebuffer?.Dispose();
        _lockedFramebuffer = null;

        // Dispose gets called, when rendering is finished, so invalidate the visual now
        _invalidateVisual?.Invoke();
        _invalidateVisual = null;
    }

    [MemberNotNull(nameof(_lockedFramebuffer))]
    private T ThrowIf<T>(T val)
    {
        ObjectDisposedException.ThrowIf(_lockedFramebuffer == null, typeof(AvaloniaFramebufferReference));
        return val;
    }
}
