using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.Services;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Handshaking
{
    /// <inheritdoc />
    public class RfbHandshaker : IRfbHandshaker
    {
        private readonly ITransport _transport;
        private readonly ILogger<RfbHandshaker> _logger;

        private readonly byte[] _readBuffer = new byte[32];

        /// <summary>
        /// Initializes a new instance of the <see cref="RfbHandshaker"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public RfbHandshaker(RfbConnectionContext context) : this((context ?? throw new ArgumentNullException(nameof(context))).Transport,
            context.Connection.LoggerFactory.CreateLogger<RfbHandshaker>()) { }

        // For uint testing only
        internal RfbHandshaker(ITransport transport, ILogger<RfbHandshaker> logger)
        {
            _transport = transport;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<HandshakeResult> DoHandshakeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Doing protocol handshake...");

            // Read maximum supported server protocol version
            RfbProtocolVersion serverProtocolVersion = await ReadProtocolVersionAsync(cancellationToken).ConfigureAwait(false);

            // Select used protocol version
            RfbProtocolVersion clientProtocolVersion;
            if (serverProtocolVersion == RfbProtocolVersion.Unknown)
            {
                clientProtocolVersion = RfbProtocolVersions.Latest;
                _logger.LogDebug($"Supported server protocol version is unknown, too new? Trying latest protocol version {clientProtocolVersion}.");
            }
            else if (serverProtocolVersion > RfbProtocolVersions.Latest)
            {
                clientProtocolVersion = RfbProtocolVersions.Latest;
                _logger.LogDebug($"Supported server protocol version {serverProtocolVersion} is too new. Requesting latest version supported by the client.");
            }
            else
            {
                clientProtocolVersion = serverProtocolVersion;
                _logger.LogDebug($"Server supports protocol version {serverProtocolVersion}. Choosing that as the highest one that's supported by both sides.");
            }

            // Send selected protocol version
            await SendProtocolVersionAsync(clientProtocolVersion, cancellationToken).ConfigureAwait(false);

            return new HandshakeResult(serverProtocolVersion);
        }

        private async Task<RfbProtocolVersion> ReadProtocolVersionAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Reading protocol version...");

            // The protocol version info always consists of 12 bytes
            ReadOnlyMemory<byte> bytes = await ReadBytesAsync(12, cancellationToken).ConfigureAwait(false);
            string protocolVersionString = Encoding.ASCII.GetString(bytes.Span).TrimEnd('\n');

            RfbProtocolVersion protocolVersion = RfbProtocolVersions.GetFromStringRepresentation(Encoding.ASCII.GetString(bytes.Span).TrimEnd('\n'));
            if (protocolVersion == RfbProtocolVersion.Unknown)
                _logger.LogWarning($"Unknown protocol version {protocolVersionString}.");

            return protocolVersion;
        }

        private async Task SendProtocolVersionAsync(RfbProtocolVersion protocolVersion, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug($"Sending protocol version {protocolVersion}...");

            string protocolVersionString = protocolVersion.GetStringRepresentation() + '\n';
            byte[] bytes = Encoding.ASCII.GetBytes(protocolVersionString);
            Debug.Assert(bytes.Length == 12, "bytes.Length == 12");

            await _transport.Stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        }

        // Helper method to read a chunk of bytes from the stream. Not thread-safe!
        private async Task<ReadOnlyMemory<byte>> ReadBytesAsync(int numBytes, CancellationToken cancellationToken = default)
        {
            Debug.Assert(numBytes <= _readBuffer.Length, "numBytes <= _readBuffer.Length");

            cancellationToken.ThrowIfCancellationRequested();

            var bytesRead = 0;
            do
            {
                int read = await _transport.Stream.ReadAsync(_readBuffer, bytesRead, numBytes - bytesRead, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    throw new UnexpectedEndOfStreamException($"Stream reached its end while trying to read {numBytes} bytes.");

                bytesRead += read;
            }
            while (bytesRead < numBytes);

            return _readBuffer.AsMemory(0, numBytes);
        }
    }
}
