namespace MarcusW.VncClient
{
    /// <summary>
    /// The different JPEG subsampling levels.
    /// </summary>
    public enum JpegSubsamplingLevel : int
    {
        /// <summary>
        /// Chrominance components are sent for every pixel in the source image.
        /// </summary>
        None = 0,

        /// <summary>
        /// Chrominance components are sent for every fourth pixel in the source image.
        /// This would typically be implemented using 4:2:0 subsampling (2X subsampling in both X and Y directions), but it could also be implemented using 4:1:1
        /// subsampling (4X subsampling in the X direction.)
        /// </summary>
        ChrominanceSubsampling4X = 1,

        /// <summary>
        /// 2X chrominance subsampling. Chrominance components are sent for every other pixel in the source image.
        /// This would typically be implemented using 4:2:2 subsampling (2X subsampling in the X direction.)
        /// </summary>
        ChrominanceSubsampling2X = 2,

        /// <summary>
        /// All chrominance components in the source image are discarded.
        /// </summary>
        Grayscale = 3,

        /// <summary>
        /// Chrominance components are sent for every 8th pixel in the source image.
        /// This would typically be implemented using 4:1:0 subsampling (4X subsampling in the X direction and 2X subsampling in the Y direction.)
        /// </summary>
        ChrominanceSubsampling8X = 4,

        /// <summary>
        /// Chrominance components are sent for every 16th pixel in the source image.
        /// This would typically be implemented using 4X subsampling in both X and Y directions.
        /// </summary>
        ChrominanceSubsampling16X = 5
    }
}
