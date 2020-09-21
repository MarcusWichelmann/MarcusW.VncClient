using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace MarcusW.VncClient.Protocol.Implementation
{
    /// <summary>
    /// Provides methods for converting a pixel value from one format to another.
    /// </summary>
    public static class PixelConversions
    {
        /// <summary>
        /// Reads pixel data from <paramref name="pixelPtr"/>, converts it to <paramref name="targetFormat"/> and writes it to the target buffer.
        /// </summary>
        /// <param name="pixelPtr">The position of the source pixel data.</param>
        /// <param name="pixelFormat">The format of the source pixel data.</param>
        /// <param name="targetPtr">The position for the target pixel data.</param>
        /// <param name="targetFormat">The format for the target pixel data.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static unsafe void WritePixel(byte* pixelPtr, in PixelFormat pixelFormat, byte* targetPtr, in PixelFormat targetFormat)
        {
            if (!pixelFormat.TrueColor || !targetFormat.TrueColor)
                throw new InvalidOperationException("Pixel formats with color maps cannot be converted.");
            if (pixelFormat.BitsPerPixel > 32 || targetFormat.BitsPerPixel > 32)
                throw new InvalidOperationException("This conversion algorithm doesn't support pixel formats with more than 32bpp.");

            // Try the fast path for 1:1 conversions
            if (WritePixelFastPath(pixelPtr, pixelFormat, targetPtr, targetFormat))
                return;

            // Fast path didn't apply, so use the generic method
            WritePixelGenericPath(pixelPtr, pixelFormat, targetPtr, targetFormat);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static unsafe bool WritePixelFastPath(byte* pixelPtr, in PixelFormat pixelFormat, byte* targetPtr, in PixelFormat targetFormat)
        {
            if (pixelFormat.BigEndian == targetFormat.BigEndian)
            {
                // Simple case: Do both formats have an alpha channel?
                if (pixelFormat.HasAlpha && targetFormat.HasAlpha)
                {
                    // Is the color representation the same?
                    if (pixelFormat.BitsPerPixel == targetFormat.BitsPerPixel && pixelFormat.RedMax == targetFormat.RedMax && pixelFormat.GreenMax == targetFormat.GreenMax
                        && pixelFormat.BlueMax == targetFormat.BlueMax && pixelFormat.AlphaMax == targetFormat.AlphaMax && pixelFormat.RedShift == targetFormat.RedShift
                        && pixelFormat.GreenShift == targetFormat.GreenShift && pixelFormat.BlueShift == targetFormat.BlueShift
                        && pixelFormat.AlphaShift == targetFormat.AlphaShift)
                    {
                        // Do a memcopy and call it a day.
                        Unsafe.CopyBlock(targetPtr, pixelPtr, pixelFormat.BytesPerPixel);
                        return true;
                    }
                }

                // Either the source pixel has an alpha channel and the target doesn't, then ignore it,
                // or only the target pixel has one, then we can just fake it (set it to MaxValue).
                // If both formats don't have one, it's trivial.
                else
                {
                    // Is the color representation the same?
                    if (pixelFormat.BitsPerPixel == targetFormat.BitsPerPixel && pixelFormat.RedMax == targetFormat.RedMax && pixelFormat.GreenMax == targetFormat.GreenMax
                        && pixelFormat.BlueMax == targetFormat.BlueMax && pixelFormat.RedShift == targetFormat.RedShift && pixelFormat.GreenShift == targetFormat.GreenShift
                        && pixelFormat.BlueShift == targetFormat.BlueShift)
                    {
                        // This will memcopy all bits (bpp), even though only depth-bits are actually relevant.
                        // But in case there were left bits for the alpha channel, they will get overwritten now, anyway.
                        Unsafe.CopyBlock(targetPtr, pixelPtr, pixelFormat.BytesPerPixel);

                        // Set the alpha value, if required
                        if (targetFormat.HasAlpha)
                        {
                            switch (targetFormat.BitsPerPixel)
                            {
                                case 32:
                                    Unsafe.Write(targetPtr, Unsafe.AsRef<uint>(targetPtr) | (uint)(targetFormat.AlphaMax << targetFormat.AlphaShift));
                                    break;
                                case 16:
                                    Unsafe.Write(targetPtr, Unsafe.AsRef<ushort>(targetPtr) | (ushort)(targetFormat.AlphaMax << targetFormat.AlphaShift));
                                    break;
                                case 8:
                                    Unsafe.Write(targetPtr, Unsafe.AsRef<byte>(targetPtr) | (byte)(targetFormat.AlphaMax << targetFormat.AlphaShift));
                                    break;
                                default:
                                    Debug.Fail($"Fast path optimization for pixel conversions might not work correctly for strange bpp-values like {targetFormat.BitsPerPixel}.");
                                    break;
                            }
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static unsafe void WritePixelGenericPath(byte* pixelPtr, in PixelFormat pixelFormat, byte* targetPtr, in PixelFormat targetFormat)
        {
            // Read the corresponding bits, ensure it's LE and put them to the right (of the native representation) of a 32bit uint so we can easier work with it.
            uint pixelValue;
            switch (pixelFormat.BitsPerPixel)
            {
                case 32:
                case 24: // Used for ZRLE compressed pixels which consist of only 3 bytes. The fourth read byte will be ignored then.
                    var u32 = Unsafe.AsRef<uint>(pixelPtr);
                    if (pixelFormat.BigEndian)
                        u32 = BinaryPrimitives.ReverseEndianness(u32);
                    pixelValue = u32;
                    break;
                case 16:
                    var u16 = Unsafe.AsRef<ushort>(pixelPtr);
                    if (pixelFormat.BigEndian)
                        u16 = BinaryPrimitives.ReverseEndianness(u16);
                    pixelValue = u16;
                    break;
                case 8:
                    pixelValue = Unsafe.AsRef<byte>(pixelPtr);
                    break;
                default: throw new InvalidOperationException($"Generic pixel conversion doesn't support strange bpp-values like {pixelFormat.BitsPerPixel}.");
            }

            // Variable for the resulting pixel value
            uint targetValue = 0;

            // Copy the color channels
            CopyChannelValue(pixelValue, ref targetValue, pixelFormat.RedMax, pixelFormat.RedShift, targetFormat.RedMax, targetFormat.RedShift);
            CopyChannelValue(pixelValue, ref targetValue, pixelFormat.GreenMax, pixelFormat.GreenShift, targetFormat.GreenMax, targetFormat.GreenShift);
            CopyChannelValue(pixelValue, ref targetValue, pixelFormat.BlueMax, pixelFormat.BlueShift, targetFormat.BlueMax, targetFormat.BlueShift);

            // Copy or fake the alpha channel
            if (pixelFormat.HasAlpha && targetFormat.HasAlpha)
                CopyChannelValue(pixelValue, ref targetValue, pixelFormat.AlphaMax, pixelFormat.AlphaShift, targetFormat.AlphaMax, targetFormat.AlphaShift);
            else if (targetFormat.HasAlpha)
                targetValue |= (uint)(targetFormat.AlphaMax << targetFormat.AlphaShift);

            // Convert the resulting pixel to LE, if required, and write it to the target buffer
            switch (targetFormat.BitsPerPixel)
            {
                case 32:
                    if (targetFormat.BigEndian)
                        targetValue = BinaryPrimitives.ReverseEndianness(targetValue);
                    Unsafe.Write(targetPtr, targetValue);
                    break;
                case 16:
                    var u16 = (ushort)targetValue;
                    if (targetFormat.BigEndian)
                        u16 = BinaryPrimitives.ReverseEndianness(u16);
                    Unsafe.Write(targetPtr, u16);
                    break;
                case 8:
                    Unsafe.Write(targetPtr, (byte)targetValue);
                    break;
                default: throw new InvalidOperationException($"Generic pixel conversion doesn't support strange bpp-values like {targetFormat.BitsPerPixel}.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void CopyChannelValue(uint srcValue, ref uint dstValue, ushort srcMax, byte srcShift, ushort dstMax, byte dstShift)
        {
            // Retrieve channel value from the source
            uint value = (srcValue >> srcShift) & srcMax;

            // Color range conversion needed?
            if (srcMax != dstMax)
            {
                // Calculate channel depth
                byte srcDepth = GetChannelDepth(srcMax);
                byte dstDepth = GetChannelDepth(dstMax);

                // Reduction: Shift the value right so only the most significant bits remain
                if (srcDepth > dstDepth)
                    value >>= srcDepth - dstDepth;

                // Extension: Shift the value left so the remaining bits get the most significance
                else
                    value <<= dstDepth - srcDepth;
            }

            // Add the value to the result
            dstValue |= value << dstShift;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static byte GetChannelDepth(ushort maxValue)
        {
            // If the hardware instruction is available, use this one, because it's WAY faster...
            if (Popcnt.IsSupported)
                return (byte)Popcnt.PopCount(maxValue);

            // Fallback: https://en.wikichip.org/wiki/population_count#Implementations
            byte depth = 0;
            for (; maxValue != 0; maxValue &= (ushort)(maxValue - 1))
                depth++;
            return depth;
        }
    }
}
