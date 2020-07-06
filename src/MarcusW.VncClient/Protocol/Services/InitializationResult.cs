using System;

namespace MarcusW.VncClient.Protocol.Services
{
    /// <summary>
    /// Provides information about the outcome of a connection initialization.
    /// </summary>
    public class InitializationResult
    {
        /// <summary>
        /// Gets the received framebuffer size.
        /// </summary>
        public FrameSize FramebufferSize { get; }

        /// <summary>
        /// Gets the received pixel format.
        /// </summary>
        public PixelFormat PixelFormat { get; }

        /// <summary>
        /// Gets the received name of the remote desktop.
        /// </summary>
        public string DesktopName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializationResult"/>.
        /// </summary>
        /// <param name="framebufferSize">The received framebuffer size.</param>
        /// <param name="pixelFormat">The received pixel format.</param>
        /// <param name="desktopName">The received name of the remote desktop.</param>
        public InitializationResult(FrameSize framebufferSize, PixelFormat pixelFormat, string desktopName)
        {
            FramebufferSize = framebufferSize;
            PixelFormat = pixelFormat;
            DesktopName = desktopName ?? throw new ArgumentNullException(nameof(desktopName));
        }
    }
}
