using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// The different versions of the RFB protocol.
    /// </summary>
    /// <remarks>
    /// See: https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#protocolversion
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "CA1707")]
    public enum RfbProtocolVersion
    {
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// RFB 3.3
        /// </summary>
        RFB_3_3 = 33,

        /// <summary>
        /// RFB 3.7
        /// </summary>
        RFB_3_7 = 37,

        /// <summary>
        /// RFB 3.8
        /// </summary>
        RFB_3_8 = 38
    }

    /// <summary>
    /// Helper methods for <see cref="RfbProtocolVersion"/>.
    /// </summary>
    public static class RfbProtocolVersions
    {
        /// <summary>
        /// The latest supported protocol version.
        /// </summary>
        public static RfbProtocolVersion LatestSupported = RfbProtocolVersion.RFB_3_8;

        /// <summary>
        /// Returns the protocol version for the provided string representation.
        /// </summary>
        /// <param name="protocolVersionString">The protocol version string.</param>
        /// <returns>The <see cref="RfbProtocolVersion"/> or Unknown for invalid input strings.</returns>
        public static RfbProtocolVersion GetFromStringRepresentation(string protocolVersionString)
            => protocolVersionString switch {
                "RFB 003.003" => RfbProtocolVersion.RFB_3_3,
                "RFB 003.005" => RfbProtocolVersion.RFB_3_3, // Interpret as 3.3
                "RFB 003.007" => RfbProtocolVersion.RFB_3_7,
                "RFB 003.008" => RfbProtocolVersion.RFB_3_8,
                _             => RfbProtocolVersion.Unknown
            };
    }

    /// <summary>
    /// Extension methods for <see cref="RfbProtocolVersion"/>.
    /// </summary>
    public static class RfbProtocolVersionExtensions
    {
        /// <summary>
        /// Returns the string representation of a given protocol version.
        /// </summary>
        /// <param name="protocolVersion">The protocol version.</param>
        /// <returns>The string representation defined by the protocol.</returns>
        public static string GetStringRepresentation(this RfbProtocolVersion protocolVersion)
            => protocolVersion switch {
                RfbProtocolVersion.Unknown => throw new InvalidOperationException("Cannot get string representation for unknown protocol version."),
                RfbProtocolVersion.RFB_3_3 => "RFB 003.003",
                RfbProtocolVersion.RFB_3_7 => "RFB 003.007",
                RfbProtocolVersion.RFB_3_8 => "RFB 003.008",
                _                          => throw new InvalidEnumArgumentException(nameof(protocolVersion), (int)protocolVersion, typeof(RfbProtocolVersion))
            };

        /// <summary>
        /// Returns a readable string for a given protocol version.
        /// </summary>
        /// <param name="protocolVersion">The protocol version.</param>
        /// <returns>A readable string.</returns>
        public static string ToReadableString(this RfbProtocolVersion protocolVersion)
            => protocolVersion switch {
                RfbProtocolVersion.Unknown => "Unknown",
                RfbProtocolVersion.RFB_3_3 => "RFB 3.3",
                RfbProtocolVersion.RFB_3_7 => "RFB 3.7",
                RfbProtocolVersion.RFB_3_8 => "RFB 3.8",
                _                          => throw new InvalidEnumArgumentException(nameof(protocolVersion), (int)protocolVersion, typeof(RfbProtocolVersion))
            };
    }
}
