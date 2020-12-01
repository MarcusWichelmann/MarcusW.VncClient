namespace MarcusW.VncClient.Protocol.Implementation.Native
{
    /// <summary>
    /// The different TurboJPEG pixel formats.
    /// </summary>
    public enum TurboJpegPixelFormat
    {
        /// <summary>
        /// RGB pixel format. The red, green, and blue components in the image are
        /// stored in 3-byte pixels in the order R, G, B from lowest to highest byte
        /// address within each pixel.
        /// </summary>
        RGB = 0,

        /// <summary>
        /// BGR pixel format. The red, green, and blue components in the image are
        /// stored in 3-byte pixels in the order B, G, R from lowest to highest byte
        /// address within each pixel.
        /// </summary>
        BGR,

        /// <summary>
        /// RGBX pixel format. The red, green, and blue components in the image are
        /// stored in 4-byte pixels in the order R, G, B from lowest to highest byte
        /// address within each pixel. The X component is ignored when compressing
        /// and undefined when decompressing.
        /// </summary>
        RGBX,

        /// <summary>
        /// BGRX pixel format. The red, green, and blue components in the image are
        /// stored in 4-byte pixels in the order B, G, R from lowest to highest byte
        /// address within each pixel. The X component is ignored when compressing
        /// and undefined when decompressing.
        /// </summary>
        BGRX,

        /// <summary>
        /// XBGR pixel format. The red, green, and blue components in the image are
        /// stored in 4-byte pixels in the order R, G, B from highest to lowest byte
        /// address within each pixel. The X component is ignored when compressing
        /// and undefined when decompressing.
        /// </summary>
        XBGR,

        /// <summary>
        /// XRGB pixel format. The red, green, and blue components in the image are
        /// stored in 4-byte pixels in the order B, G, R from highest to lowest byte
        /// address within each pixel. The X component is ignored when compressing
        /// and undefined when decompressing.
        /// </summary>
        XRGB,

        /// <summary>
        /// Grayscale pixel format. Each 1-byte pixel represents a luminance
        /// (brightness) level from 0 to 255.
        /// </summary>
        Gray,

        /// <summary>
        /// RGBA pixel format. This is the same as <see cref="RGBX"/>, except that when
        /// decompressing, the X component is guaranteed to be 0xFF, which can be
        /// interpreted as an opaque alpha channel.
        /// </summary>
        RGBA,

        /// <summary>
        /// BGRA pixel format. This is the same as <see cref="BGRX"/>, except that when
        /// decompressing, the X component is guaranteed to be 0xFF, which can be
        /// interpreted as an opaque alpha channel.
        /// </summary>
        BGRA,

        /// <summary>
        /// ABGR pixel format. This is the same as <see cref="XBGR"/>, except that when
        /// decompressing, the X component is guaranteed to be 0xFF, which can be
        /// interpreted as an opaque alpha channel.
        /// </summary>
        ABGR,

        /// <summary>
        /// ARGB pixel format. This is the same as <see cref="XRGB"/>, except that when
        /// decompressing, the X component is guaranteed to be 0xFF, which can be
        /// interpreted as an opaque alpha channel.
        /// </summary>
        ARGB,

        /// <summary>
        /// CMYK pixel format. Unlike RGB, which is an additive color model used
        /// primarily for display, CMYK (Cyan/Magenta/Yellow/Key) is a subtractive
        /// color model used primarily for printing. In the CMYK color model, the
        /// value of each color component typically corresponds to an amount of cyan,
        /// magenta, yellow, or black ink that is applied to a white background. In
        /// order to convert between CMYK and RGB, it is necessary to use a color
        /// management system (CMS.) A CMS will attempt to map colors within the
        /// printer's gamut to perceptually similar colors in the display's gamut and
        /// vice versa, but the mapping is typically not 1:1 or reversible, nor can it
        /// be defined with a simple formula. Thus, such a conversion is out of scope
        /// for a codec library. However, the TurboJPEG API allows for compressing
        /// CMYK pixels into a YCCK JPEG image (see #TJCS_YCCK) and decompressing YCCK
        /// JPEG images into CMYK pixels.
        /// </summary>
        CMYK,
    }
}
