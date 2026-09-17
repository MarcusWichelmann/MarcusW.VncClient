using System;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing
{
    /// <summary>
    /// A message for requesting a pixel format change.
    /// </summary>
    public class SetPixelFormatMessage : IOutgoingMessage<SetPixelFormatMessageType>
    {
        /// <summary>
        /// Gets the pixel format to set.
        /// </summary>
        public PixelFormat PixelFormat { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetPixelFormatMessage"/>.
        /// </summary>
        /// <param name="pixelFormat">The pixel format to set.</param>
        public SetPixelFormatMessage(PixelFormat pixelFormat)
        {
            PixelFormat = pixelFormat;
        }

        /// <inheritdoc />
        public string? GetParametersOverview() => $"PixelFormat: {PixelFormat}";

        /// <inheritdoc />
        public override string ToString()
        {
            return $"SetPixelFormat({PixelFormat})";
        }
    }
}
