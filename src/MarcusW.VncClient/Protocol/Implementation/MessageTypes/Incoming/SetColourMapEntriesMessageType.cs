using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MarcusW.VncClient.Protocol.MessageTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Incoming
{
    /// <summary>
    /// A message type for receiving and updating the server's color map.
    /// </summary>
    public class SetColourMapEntriesMessageType : IIncomingMessageType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<SetColourMapEntriesMessageType> _logger;
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownIncomingMessageType.SetColourMapEntries;

        /// <inheritdoc />
        public string Name => "SetColourMapEntries";

        /// <inheritdoc />
        public bool IsStandardMessageType => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetColourMapEntriesMessageType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public SetColourMapEntriesMessageType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<SetColourMapEntriesMessageType>();
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public void ReadMessage(ITransport transport, CancellationToken cancellationToken = default)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));

            cancellationToken.ThrowIfCancellationRequested();

            Stream transportStream = transport.Stream;

            // Read header: 1 byte padding, 2 bytes first-color, 2 bytes number-of-colors
            Span<byte> header = stackalloc byte[5];
            transportStream.ReadAll(header, cancellationToken);
            
            ushort firstColor = BinaryPrimitives.ReadUInt16BigEndian(header[1..3]);
            ushort numberOfColors = BinaryPrimitives.ReadUInt16BigEndian(header[3..5]);

            _logger.LogDebug("Received SetColourMapEntries: first={FirstColor}, count={NumberOfColors}", firstColor, numberOfColors);

            // Read color map entries (each entry is 6 bytes: 2 bytes red, 2 bytes green, 2 bytes blue)
            int colorDataLength = numberOfColors * 6;
            byte[] colorData = new byte[colorDataLength];
            transportStream.ReadAll(colorData, cancellationToken);

            // Parse color entries
            var colorEntries = new List<ColorMapEntry>(numberOfColors);
            for (int i = 0; i < numberOfColors; i++)
            {
                int offset = i * 6;
                ushort red = BinaryPrimitives.ReadUInt16BigEndian(colorData.AsSpan(offset, 2));
                ushort green = BinaryPrimitives.ReadUInt16BigEndian(colorData.AsSpan(offset + 2, 2));
                ushort blue = BinaryPrimitives.ReadUInt16BigEndian(colorData.AsSpan(offset + 4, 2));
                colorEntries.Add(new ColorMapEntry(red, green, blue));
            }

            // Update the color map in protocol state
            var currentColorMap = _state.RemoteFramebufferColorMap;
            var updatedColorMap = currentColorMap.WithUpdatedEntries(firstColor, colorEntries);
            _state.RemoteFramebufferColorMap = updatedColorMap;

            _logger.LogDebug("Updated color map with {Count} entries starting at index {FirstColor}", numberOfColors, firstColor);
        }
    }
}
