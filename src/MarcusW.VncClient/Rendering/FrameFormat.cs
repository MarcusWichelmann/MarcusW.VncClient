using System.Diagnostics.CodeAnalysis;

namespace MarcusW.VncClient.Rendering
{
    /// <summary>
    /// The different types of representing the pixels of a frame.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum FrameFormat
    {
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown,

        /// <summary>
        /// 5 bits for red, 6 bits for green and 5 bits for blue
        /// </summary>
        RGB565,

        /// <summary>
        /// 5 bits for blue, 6 bits for green and 5 bits for red
        /// </summary>
        BGR565,

        /// <summary>
        /// 8 bits for red, green and blue
        /// </summary>
        RGB888,

        /// <summary>
        /// 8 bits for glue, green and red
        /// </summary>
        BGR888,

        /// <summary>
        /// 8 bits for red, green, blue and alpha
        /// </summary>
        RGBA8888,

        /// <summary>
        /// 8 bits for glue, green, red and alpha
        /// </summary>
        BGRA8888
    }
}
