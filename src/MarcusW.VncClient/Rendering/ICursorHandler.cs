namespace MarcusW.VncClient.Rendering
{
    /// <summary>
    /// Provides methods for handling cursor updates from the server.
    /// </summary>
    /// <remarks>
    /// Implement this interface to support local cursor rendering, which significantly
    /// improves perceived performance over slow network connections.
    /// </remarks>
    public interface ICursorHandler
    {
        /// <summary>
        /// Updates the cursor shape with pixel data and a bitmask.
        /// </summary>
        /// <param name="hotspotX">The X coordinate of the cursor hotspot.</param>
        /// <param name="hotspotY">The Y coordinate of the cursor hotspot.</param>
        /// <param name="width">The width of the cursor in pixels.</param>
        /// <param name="height">The height of the cursor in pixels.</param>
        /// <param name="pixelData">The cursor pixel data in the current pixel format.</param>
        /// <param name="bitmask">The cursor bitmask (1 bit per pixel, 1 = visible).</param>
        /// <param name="pixelFormat">The pixel format of the cursor data.</param>
        void UpdateCursor(int hotspotX, int hotspotY, int width, int height, byte[] pixelData, byte[] bitmask, PixelFormat pixelFormat);

        /// <summary>
        /// Updates the cursor shape with RGBA pixel data (premultiplied alpha).
        /// </summary>
        /// <param name="hotspotX">The X coordinate of the cursor hotspot.</param>
        /// <param name="hotspotY">The Y coordinate of the cursor hotspot.</param>
        /// <param name="width">The width of the cursor in pixels.</param>
        /// <param name="height">The height of the cursor in pixels.</param>
        /// <param name="rgbaData">The cursor pixel data in RGBA format with premultiplied alpha.</param>
        void UpdateCursorWithAlpha(int hotspotX, int hotspotY, int width, int height, byte[] rgbaData);

        /// <summary>
        /// Updates the cursor shape with a two-color cursor.
        /// </summary>
        /// <param name="hotspotX">The X coordinate of the cursor hotspot.</param>
        /// <param name="hotspotY">The Y coordinate of the cursor hotspot.</param>
        /// <param name="width">The width of the cursor in pixels.</param>
        /// <param name="height">The height of the cursor in pixels.</param>
        /// <param name="primaryColor">The primary (foreground) color RGB.</param>
        /// <param name="secondaryColor">The secondary (background) color RGB.</param>
        /// <param name="bitmap">The cursor bitmap (1 = primary color, 0 = secondary color).</param>
        /// <param name="bitmask">The cursor bitmask (1 = visible).</param>
        void UpdateXCursor(int hotspotX, int hotspotY, int width, int height, byte[] primaryColor, byte[] secondaryColor, byte[] bitmap, byte[] bitmask);

        /// <summary>
        /// Hides the cursor.
        /// </summary>
        void HideCursor();
    }
}
