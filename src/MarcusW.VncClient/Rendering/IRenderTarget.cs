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
        /// </summary>
        /// <param name="size">The required frame size.</param>
        /// <returns>The framebuffer reference.</returns>
        IFramebufferReference GrabFramebufferReference(Size size);
    }
}
