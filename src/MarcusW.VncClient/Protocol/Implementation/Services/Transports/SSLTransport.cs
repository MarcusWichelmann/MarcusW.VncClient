using System;
using System.IO;
using System.Net.Sockets;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Transports
{
    /// <summary>
    /// A transport which provides a stream for communication over a plain TCP connection.
    /// </summary>
    public sealed class SSLTransport : ITransport
    {
        private readonly Stream _stream;

        /// <inhertitdoc />
        public Stream Stream => _stream;

        /// <inhertitdoc />
        public bool IsEncrypted => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpTransport"/>.
        /// </summary>
        /// <param name="tcpClient">The tcp client.</param>
        public SSLTransport(Stream Stream)
        {
            _stream = Stream ?? throw new ArgumentNullException(nameof(Stream));
        }

        /// <inhertitdoc />
        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}
