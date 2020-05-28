using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Services.Handshaking
{
    /// <inheritdoc />
    public class RfbHandshaker : IRfbHandshaker
    {
        private readonly Stream _stream;
        private readonly ILogger<RfbHandshaker> _logger;

        private readonly byte[] _readBuffer = new byte[32];

        internal RfbHandshaker(RfbConnectionContext context) : this(context.Stream,
            context.Connection.LoggerFactory.CreateLogger<RfbHandshaker>()) { }

        // For uint testing only
        internal RfbHandshaker(Stream stream, ILogger<RfbHandshaker> logger)
        {
            _stream = stream;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<HandshakeResult> DoHandshakeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RfbProtocolVersion protocolVersion =
                await ReadProtocolVersionAsync(cancellationToken).ConfigureAwait(false);

            return new HandshakeResult(protocolVersion);
        }

        public async Task<RfbProtocolVersion> ReadProtocolVersionAsync(CancellationToken cancellationToken = default)
        {
            // The protocol version info always consists of 12 bytes
            ReadOnlyMemory<byte> bytes = await ReadBytesAsync(12, cancellationToken).ConfigureAwait(false);
            string protocolVersionString = Encoding.ASCII.GetString(bytes.Span).TrimEnd('\n');

            return protocolVersionString switch {
                "RFB 003.003" => RfbProtocolVersion.RFB_3_3,
                "RFB 003.005" => RfbProtocolVersion.RFB_3_3, // Interpret as 3.3
                "RFB 003.007" => RfbProtocolVersion.RFB_3_7,
                "RFB 003.008" => RfbProtocolVersion.RFB_3_8,
                _             => throw new UnexpectedDataException($"Unexpected RFB version {protocolVersionString}.")
            };
        }

        // Helper method to read a chunk of bytes from the stream. Not thread-safe!
        private async Task<ReadOnlyMemory<byte>> ReadBytesAsync(int numBytes,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(numBytes <= _readBuffer.Length, "numBytes <= _readBuffer.Length");

            cancellationToken.ThrowIfCancellationRequested();

            var bytesRead = 0;
            do
            {
                int read = await _stream.ReadAsync(_readBuffer, bytesRead, numBytes - bytesRead, cancellationToken)
                    .ConfigureAwait(false);
                if (read == 0)
                    throw new UnexpectedEndOfStreamException(
                        $"Stream reached its end while trying to read {numBytes} bytes.");

                bytesRead += read;
            }
            while (bytesRead < numBytes);

            return _readBuffer.AsMemory(0, numBytes);
        }
    }
}
