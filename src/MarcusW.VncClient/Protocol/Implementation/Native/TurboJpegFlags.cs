using System;

namespace MarcusW.VncClient.Protocol.Implementation.Native
{
    /// <summary>
    /// The different TurboJPEG flags.
    /// </summary>
    [Flags]
    public enum TurboJpegFlags
    {
        /// <summary>
        /// Flags not set
        /// </summary>
        None = 0,

        /// <summary>
        /// The uncompressed source/destination image is stored in bottom-up (Windows, OpenGL) order,
        /// not top-down (X11) order.
        /// </summary>
        BottomUp = 2,

        /// <summary>
        /// When decompressing an image that was compressed using chrominance subsampling,
        /// use the fastest chrominance upsampling algorithm available in the underlying codec.
        /// The default is to use smooth upsampling, which creates a smooth transition between
        /// neighboring chrominance components in order to reduce upsampling artifacts in the decompressed image.
        /// </summary>
        FastUpsample = 256,

        /// <summary>
        /// Disable buffer (re)allocation. If passed to <see cref="TurboJpegImport.TjCompress2"/> or #tjTransform(),
        /// this flag will cause those functions to generate an error
        /// if the JPEG image buffer is invalid or too small rather than attempting to allocate or reallocate that buffer.
        /// This reproduces the behavior of earlier versions of TurboJPEG.
        /// </summary>
        NoRealloc = 1024,

        /// <summary>
        /// Use the fastest DCT/IDCT algorithm available in the underlying codec. The
        /// default if this flag is not specified is implementation-specific. For
        /// example, the implementation of TurboJPEG for libjpeg[-turbo] uses the fast
        /// algorithm by default when compressing, because this has been shown to have
        /// only a very slight effect on accuracy, but it uses the accurate algorithm
        /// when decompressing, because this has been shown to have a larger effect.
        /// </summary>
        FastDct = 2048,

        /// <summary>
        /// Use the most accurate DCT/IDCT algorithm available in the underlying codec.
        /// The default if this flag is not specified is implementation-specific. For
        /// example, the implementation of TurboJPEG for libjpeg[-turbo] uses the fast
        /// algorithm by default when compressing, because this has been shown to have
        /// only a very slight effect on accuracy, but it uses the accurate algorithm
        /// when decompressing, because this has been shown to have a larger effect.
        /// </summary>
        AccurateDct = 4096,
    }
}
