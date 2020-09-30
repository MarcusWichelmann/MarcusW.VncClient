using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Threading;
using MarcusW.VncClient.Output;
using MarcusW.VncClient.Protocol.MessageTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Incoming
{
    /// <summary>
    /// A message type for receiving updates about the cut buffer (clipboard) of the server.
    /// </summary>
    public class ServerCutTextMessageType : IIncomingMessageType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<ServerCutTextMessageType> _logger;
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownIncomingMessageType.ServerCutText;

        /// <inheritdoc />
        public string Name => "ServerCutText";

        /// <inheritdoc />
        public bool IsStandardMessageType => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerCutTextMessageType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public ServerCutTextMessageType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<ServerCutTextMessageType>();
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public void ReadMessage(ITransport transport, CancellationToken cancellationToken = default)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));

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
            IOutputHandler? outputHandler = _context.Connection.OutputHandler;
            if (outputHandler == null && !_logger.IsEnabled(LogLevel.Debug))
                skip = true;

            // Skip?
            if (skip)
            {
                transportStream.SkipAll((int)textLength, cancellationToken);
                return;
            }

            StringBuilder stringBuilder = new StringBuilder((int)textLength);
            Encoding latin1Encoding = Encoding.GetEncoding("ISO-8859-1");

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

                        int read = transportStream.Read(bytesToRead < bufferSpan.Length ? bufferSpan.Slice(0, bytesToRead) : bufferSpan);
                        if (read == 0)
                            throw new UnexpectedEndOfStreamException("Stream reached its end while trying to read the server cut text.");

                        stringBuilder.Append(latin1Encoding.GetString(bufferSpan.Slice(0, read)));

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
}
