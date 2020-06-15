using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using MarcusW.VncClient.Protocol;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// The different versions of the RFB protocol.
    /// </summary>
    /// <remarks>
    /// See: https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#protocolversion
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
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
        public static RfbProtocolVersion Latest = RfbProtocolVersion.RFB_3_8;

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
        /// Returns a string representation of a given protocol version.
        /// </summary>
        /// <param name="protocolVersion">The protocol version.</param>
        /// <returns>A string.</returns>
        public static string GetStringRepresentation(this RfbProtocolVersion protocolVersion)
            => protocolVersion switch {
                RfbProtocolVersion.Unknown => "Unknown",
                RfbProtocolVersion.RFB_3_3 => "RFB 003.003",
                RfbProtocolVersion.RFB_3_7 => "RFB 003.007",
                RfbProtocolVersion.RFB_3_8 => "RFB 003.008",
                _                          => throw new InvalidEnumArgumentException(nameof(protocolVersion), (int)protocolVersion, typeof(RfbProtocolVersion))
            };
    }
}
