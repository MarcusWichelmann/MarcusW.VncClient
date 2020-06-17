using System;
using System.IO;
using System.Net.Sockets;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Transports
{
    /// <summary>
    /// A transport which provides a stream for communication over a plain TCP connection.
    /// </summary>
    public class TcpTransport : ITransport
    {
        private readonly TcpClient _tcpClient;

        /// <inhertitdoc />
        public Stream Stream => _tcpClient.GetStream();

        /// <inhertitdoc />
        public bool IsEncrypted => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpTransport"/>.
        /// </summary>
        /// <param name="tcpClient">The tcp client.</param>
        public TcpTransport(TcpClient tcpClient)
        {
            _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
        }

        /// <inhertitdoc />
        public void Dispose()
        {
            _tcpClient.Dispose();
        }
    }
}
