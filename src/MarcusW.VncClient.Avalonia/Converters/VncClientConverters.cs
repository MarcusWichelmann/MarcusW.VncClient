using Avalonia.Data.Converters;
using MarcusW.VncClient.Protocol;

namespace MarcusW.VncClient.Avalonia.Converters
{
    /// <summary>
    /// Provides a set of useful <see cref="IValueConverter"/>s for working with the vnc client library.
    /// </summary>
    public static class VncClientConverters
    {
        /// <summary>
        /// A value converter that returns a readable string for a given <see cref="RfbProtocolVersion"/>.
        /// </summary>
        public static readonly IValueConverter RfbProtocolVersionToString = new FuncValueConverter<RfbProtocolVersion, string>(v => v.ToReadableString());

        /// <summary>
        /// A value converter that returns a short string representation for a given <see cref="PixelFormat"/>.
        /// </summary>
        public static readonly IValueConverter PixelFormatToShortString = new FuncValueConverter<PixelFormat, string>(f => f.ToShortString());
    }
}
