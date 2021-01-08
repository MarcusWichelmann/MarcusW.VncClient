using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace MarcusW.VncClient.Protocol.Implementation
{
    /// <summary>
    /// Provides some useful methods for working with pixel values and formats.
    /// </summary>
    public static class PixelUtils
    {
        /// <summary>
        /// Counts the number of high bits in the maximum value for a color channel to retrieve how many bits a channel occupies.
        /// </summary>
        /// <param name="maxValue">The maximum value for the color channel.</param>
        /// <returns>The channel depth.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static byte GetChannelDepth(ushort maxValue)
        {
            // If the hardware instruction is available, use this one, because it's WAY faster...
            if (Popcnt.IsSupported)
                return (byte)Popcnt.PopCount(maxValue);

            // Fallback: https://en.wikichip.org/wiki/population_count#Implementations
            // TODO: Probably there is something faster for this special use case, but mostly popcnt will be available anyway.
            byte depth = 0;
            for (; maxValue != 0; maxValue &= (ushort)(maxValue - 1))
                depth++;
            return depth;
        }
    }
}
