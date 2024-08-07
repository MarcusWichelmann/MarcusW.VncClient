using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using MarcusW.VncClient.Protocol.MessageTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Incoming;

/// <summary>
///     A message type for receiving a color map.
/// </summary>
/// <remarks>
///     Color maps are currently not supported by this protocol implementation. Therefore the received color map is
///     discarded.
/// </remarks>
/// <remarks>
///     Initializes a new instance of the <see cref="SetColourMapEntriesMessageType" />.
/// </remarks>
/// <param name="context">The connection context.</param>
public class SetColourMapEntriesMessageType(RfbConnectionContext context) : IIncomingMessageType
{
    private readonly ILogger<SetColourMapEntriesMessageType> _logger =
        context.Connection.LoggerFactory.CreateLogger<SetColourMapEntriesMessageType>();

    /// <inheritdoc />
    public byte Id => (byte)WellKnownIncomingMessageType.SetColourMapEntries;

    /// <inheritdoc />
    public string Name => "SetColourMapEntries (unsupported)";

    /// <inheritdoc />
    public bool IsStandardMessageType => true;

    /// <inheritdoc />
    public void ReadMessage(ITransport transport, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Stream transportStream = transport.Stream;

        // Read 5 header bytes (first 1 byte is padding)
        Span<byte> header = stackalloc byte[5];
        transportStream.ReadAll(header, cancellationToken);
        ushort numberOfColors = BinaryPrimitives.ReadUInt16BigEndian(header[3..]);

        // Skip the color map
        transportStream.SkipAll(6 * numberOfColors, cancellationToken);

        _logger.LogDebug("Received and discarded a color map of {numberOfColors} entries.", numberOfColors);
    }
}
