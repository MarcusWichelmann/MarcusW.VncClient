using System;

namespace MarcusW.VncClient.Rendering
{
    /// <summary>
    /// Some flags to control how rendering to the target framebuffer should happen.
    /// </summary>
    [Flags]
    public enum RenderFlags
    {
        /// <summary>
        /// The default.
        /// </summary>
        Default = 0,

        /// <summary>
        /// If set, the screen will be updated by received rectangle instead of by frame.
        /// This might improve display latency for the cost of screen tearing and some overhead.
        /// </summary>
        UpdateByRectangle = 1 << 0,

        /// <summary>
        /// If set, the rendered rectangles and the used encoding type will be visualized for debugging purposes.
        /// </summary>
        VisualizeRectangles = 1 << 1
    }
}
