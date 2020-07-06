using System;
using Avalonia.Platform;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient.Avalonia.Adapters.Rendering
{
    /// <inheritdoc />
    public class AvaloniaFramebufferReference : IFramebufferReference
    {
        private ILockedFramebuffer? _lockedFramebuffer;
        private readonly Action _invalidateVisual;

        /// <inheritdoc />
        public IntPtr Address
            => _lockedFramebuffer?.Address ?? throw new ObjectDisposedException(nameof(AvaloniaFramebufferReference));

        /// <inheritdoc />
        public FrameSize Size
            => Conversions.GetFrameSize(_lockedFramebuffer?.Size
                ?? throw new ObjectDisposedException(nameof(AvaloniaFramebufferReference)));

        /// <inheritdoc />
        public int RowBytes
            => _lockedFramebuffer?.RowBytes ?? throw new ObjectDisposedException(nameof(AvaloniaFramebufferReference));

        /// <inheritdoc />
        public TargetFramebufferFormat Format
            => Conversions.GetTargetFramebufferFormat(_lockedFramebuffer?.Format
                ?? throw new ObjectDisposedException(nameof(AvaloniaFramebufferReference)));

        /// <inheritdoc />
        public double HorizontalDpi
            => _lockedFramebuffer?.Dpi.X ?? throw new ObjectDisposedException(nameof(AvaloniaFramebufferReference));

        /// <inheritdoc />
        public double VerticalDpi
            => _lockedFramebuffer?.Dpi.Y ?? throw new ObjectDisposedException(nameof(AvaloniaFramebufferReference));

        internal AvaloniaFramebufferReference(ILockedFramebuffer lockedFramebuffer, Action invalidateVisual)
        {
            _lockedFramebuffer = lockedFramebuffer;
            _invalidateVisual = invalidateVisual;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ILockedFramebuffer? lockedFramebuffer = _lockedFramebuffer;
            _lockedFramebuffer = null;

            if (lockedFramebuffer == null)
                return;

            lockedFramebuffer.Dispose();

            // Dispose gets called, when rendering is finished, so invalidate the visual now
            _invalidateVisual();
        }
    }
}
