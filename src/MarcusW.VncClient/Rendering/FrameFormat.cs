using System.Diagnostics.CodeAnalysis;

namespace MarcusW.VncClient.Rendering
{
    /// <summary>
    /// The different types of representing a frame in memory.
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
        /// 8 bits for red, green, blue and alpha
        /// </summary>
        RGBA8888,

        /// <summary>
        /// 8 bits for glue, green, red and alpha
        /// </summary>
        BGRA8888
    }
}
