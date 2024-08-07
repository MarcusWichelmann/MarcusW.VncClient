using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Threading;
using MarcusW.VncClient.Output;
using MarcusW.VncClient.Protocol.MessageTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Incoming;

/// <summary>
///     A message type for receiving updates about the cut buffer (clipboard) of the server.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="ServerCutTextMessageType" />.
/// </remarks>
/// <param name="context">The connection context.</param>
public class ServerCutTextMessageType(RfbConnectionContext context) : IIncomingMessageType
{
    private readonly ILogger<ServerCutTextMessageType> _logger =
        context.Connection.LoggerFactory.CreateLogger<ServerCutTextMessageType>();

    /// <inheritdoc />
    public byte Id => (byte)WellKnownIncomingMessageType.ServerCutText;

    /// <inheritdoc />
    public string Name => "ServerCutText";

    /// <inheritdoc />
    public bool IsStandardMessageType => true;

    /// <inheritdoc />
    public void ReadMessage(ITransport transport, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Stream transportStream = transport.Stream;

        // Read 7 header bytes (first 3 bytes are padding)
        Span<byte> header = stackalloc byte[7];
        transportStream.ReadAll(header, cancellationToken);
        uint textLength = BinaryPrimitives.ReadUInt32BigEndian(header[3..]);

        var skip = false;

        // Is the text too long?
        if (textLength > 256 * 1024)
        {
            _logger.LogWarning("Received cut text is too long ({textLength}). Ignoring...", textLength);
            skip = true;
        }

        // Skip the received bytes when we can't do anything with it anyway.
        IOutputHandler? outputHandler = context.Connection.OutputHandler;
        if (outputHandler == null && !_logger.IsEnabled(LogLevel.Debug))
        {
            skip = true;
        }

        // Skip?
        if (skip)
        {
            transportStream.SkipAll((int)textLength, cancellationToken);
            return;
        }

        var stringBuilder = new StringBuilder((int)textLength);
        var latin1Encoding = Encoding.GetEncoding("ISO-8859-1");

        if (textLength > 0)
        {
            // Read cut text
            byte[] buffer = ArrayPool<byte>.Shared.Rent(Math.Min(1024, (int)textLength));
            Span<byte> bufferSpan = buffer;
            try
            {
                var bytesToRead = (int)textLength;
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int read = transportStream.Read(bytesToRead < bufferSpan.Length
                        ? bufferSpan[..bytesToRead]
                        : bufferSpan);
                    if (read == 0)
                    {
                        throw new UnexpectedEndOfStreamException(
                            "Stream reached its end while trying to read the server cut text.");
                    }

                    stringBuilder.Append(latin1Encoding.GetString(bufferSpan[..read]));

                    bytesToRead -= read;
                }
                while (bytesToRead > 0);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        _logger.LogDebug("Received server cut text of length {length}.", stringBuilder.Length);

        outputHandler?.HandleServerClipboardUpdate(stringBuilder.ToString());
    }
}
