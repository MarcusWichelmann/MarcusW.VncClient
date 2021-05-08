using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MarcusW.VncClient.Protocol.Implementation.Native
{
    /// <summary>
    /// Native bindings for the TurboJPEG library.
    /// </summary>
    public static class TurboJpeg
    {
        // *** CREDITS ***
        // To make the dependency on libturbojpeg optional, I decided to not reference the TurboJpegWrapper NuGet package and instead add the few required methods here.
        // The code in this class is based on: https://github.com/quamotion/AS.TurboJpegWrapper - License: https://github.com/quamotion/AS.TurboJpegWrapper/blob/master/LICENSE.txt
        // ***************

        private const string LibraryName = "turbojpeg";

        /// <summary>
        /// Gets whether the library is available on the current system.
        /// </summary>
        public static bool IsAvailable { get; }

#if NETSTANDARD2_0
        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string dllToLoad);
#endif

        // Static constructor to check for library availability.
        static TurboJpeg()
        {
#if NETSTANDARD2_0
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                IsAvailable = LoadLibrary(LibraryName + ".dll") != IntPtr.Zero;
            }
#else
            IsAvailable = NativeLibrary.TryLoad(LibraryName, typeof(TurboJpeg).Assembly, null, out _);
#endif
        }

        /// <summary>
        /// Create a TurboJPEG decompressor instance.
        /// </summary>
        /// <returns>A handle to the newly created instance, or <see langword="null"/> if an error occurred (see <see cref="GetLastError"/>).</returns>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tjInitDecompress")]
        public static extern IntPtr InitDecompressorInstance();

        /// <summary>
        /// Destroy a TurboJPEG compressor, decompressor, or transformer instance.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <returns>0 if successful, or -1 if an error occurred (see <see cref="GetLastError"/>).</returns>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tjDestroy")]
        public static extern int DestroyInstance(IntPtr handle);

        /// <summary>
        /// Retrieve information about a JPEG image without decompressing it.
        /// </summary>
        /// <param name="handle">A handle to a TurboJPEG decompressor or transformer instance.</param>
        /// <param name="jpegBuf">Pointer to a buffer containing a JPEG image. This buffer is not modified.</param>
        /// <param name="jpegSize">Size of the JPEG image (in bytes).</param>
        /// <param name="width">An integer variable that will receive the width (in pixels) of the JPEG image.</param>
        /// <param name="height">An integer variable that will receive the height (in pixels) of the JPEG image.</param>
        /// <param name="subsampling">An integer variable that will receive the level of chrominance subsampling used when the JPEG image was compressed.</param>
        /// <param name="colorspace">An integer variable that will receive one of the JPEG colorspace constants, indicating the colorspace of the JPEG image.</param>
        /// <returns>0 if successful, or -1 if an error occurred (see <see cref="TjGetErrorStr"/>).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DecompressHeader(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, out int width, out int height, out int subsampling, out int colorspace)
        {
            return IntPtr.Size switch {
                4 => DecompressHeader_x86(handle, jpegBuf, (uint)jpegSize, out width, out height, out subsampling, out colorspace),
                8 => DecompressHeader_x64(handle, jpegBuf, jpegSize, out width, out height, out subsampling, out colorspace),
                _ => throw new InvalidOperationException("Unsupported plattform architecture.")
            };
        }

        /// <summary>
        /// Decompress a JPEG image to an RGB, grayscale, or CMYK image.
        /// </summary>
        /// <param name="handle">A handle to a TurboJPEG decompressor or transformer instance.</param>
        /// <param name="jpegBuf">Pointer to a buffer containing the JPEG image to decompress. This buffer is not modified.</param>
        /// <param name="jpegSize">Size of the JPEG image (in bytes).</param>
        /// <param name="dstBuf">Pointer to an image buffer that will receive the decompressed image.</param>
        /// <param name="width">
        /// Desired width (in pixels) of the destination image.
        /// If this is different than the width of the JPEG image being decompressed, then TurboJPEG will use scaling in the JPEG decompressor to generate the largest possible
        /// image that will fit within the desired width. If <paramref name="width"/> is set to 0, then only the height will be considered when determining the scaled image size.
        /// </param>
        /// <param name="pitch">
        /// Bytes per line in the destination image.
        /// Normally, this is <c>scaledWidth * tjPixelSize[pixelFormat]</c> if the decompressed image is unpadded, else <c>TJPAD(scaledWidth * tjPixelSize[pixelFormat])</c> if each
        /// line of the decompressed image is padded to the nearest 32-bit boundary, as is the case for Windows bitmaps.
        /// You can also be clever and use the pitch parameter to skip lines, etc.
        /// Setting this parameter to 0 is the equivalent of setting it to <c>scaledWidth* tjPixelSize[pixelFormat]</c>.
        /// </param>
        /// <param name="height">
        /// Desired height (in pixels) of the destination image.
        /// If this is different than the height of the JPEG image being decompressed, then TurboJPEG will use scaling in the JPEG decompressor to generate the largest possible
        /// image that will fit within the desired height. If <paramref name="height"/> is set to 0, then only the width will be considered when determining the scaled image size.
        /// </param>
        /// <param name="pixelFormat">Pixel format of the destination image (see <see cref="TurboJpegPixelFormat"/>).</param>
        /// <param name="flags">The bitwise OR of one or more of the <see cref="TurboJpegFlags"/>.</param>
        /// <returns>0 if successful, or -1 if an error occurred (see <see cref="GetLastError"/>).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Decompress(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, IntPtr dstBuf, int width, int pitch, int height, int pixelFormat, int flags)
        {
            return IntPtr.Size switch {
                4 => Decompress_x86(handle, jpegBuf, (uint)jpegSize, dstBuf, width, pitch, height, pixelFormat, flags),
                8 => Decompress_x64(handle, jpegBuf, jpegSize, dstBuf, width, pitch, height, pixelFormat, flags),
                _ => throw new InvalidOperationException("Unsupported plattform architecture.")
            };
        }

        /// <summary>
        /// Returns a descriptive error message explaining why the last command failed.
        /// </summary>
        /// <returns>An error message.</returns>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tjGetErrorStr")]
        public static extern IntPtr GetLastError();

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tjDecompressHeader3")]
        private static extern int DecompressHeader_x86(IntPtr handle, IntPtr jpegBuf, uint jpegSize, out int width, out int height, out int jpegSubsamp, out int jpegColorspace);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tjDecompressHeader3")]
        private static extern int DecompressHeader_x64(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, out int width, out int height, out int jpegSubsamp, out int jpegColorspace);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tjDecompress2")]
        private static extern int Decompress_x86(IntPtr handle, IntPtr jpegBuf, uint jpegSize, IntPtr dstBuf, int width, int pitch, int height, int pixelFormat, int flags);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tjDecompress2")]
        private static extern int Decompress_x64(IntPtr handle, IntPtr jpegBuf, ulong jpegSize, IntPtr dstBuf, int width, int pitch, int height, int pixelFormat, int flags);
    }
}
