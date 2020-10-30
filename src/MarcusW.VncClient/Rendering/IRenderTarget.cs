using System.Collections.Immutable;

namespace MarcusW.VncClient.Rendering
{
    /// <summary>
    /// Provides access to a target framebuffer for rendering frames.
    /// </summary>
    /// <remarks>
    /// This is an abstraction around the different framebuffer access methods for rendering backends like Avalonia (Skia, Direct2D, ...), WPF and others.
    /// </remarks>
    public interface IRenderTarget
    {
        /// <summary>
        /// Locks the native framebuffer in memory and returns a reference to it. The returned object should be disposed after rendering is finished.
        /// Must only be used by one thread at a time.
        /// The framebuffer should be re-created in case the <paramref name="size"/> changes.
        /// </summary>
        /// <param name="size">The required frame size.</param>
        /// <param name="layout">The screens that are contained by the frame (if supported).</param>
        /// <returns>The framebuffer reference.</returns>
        IFramebufferReference GrabFramebufferReference(Size size, IImmutableSet<Screen> layout);
    }
}
