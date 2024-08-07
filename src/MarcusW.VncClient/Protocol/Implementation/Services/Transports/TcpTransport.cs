using System.IO;
using System.Net.Sockets;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Transports;

/// <summary>
///     A transport which provides a stream for communication over a plain TCP connection.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="TcpTransport" />.
/// </remarks>
/// <param name="tcpClient">The tcp client.</param>
public sealed class TcpTransport(TcpClient tcpClient) : ITransport
{
    /// <inhertitdoc />
    public Stream Stream => tcpClient.GetStream();

    /// <inhertitdoc />
    public bool IsEncrypted => false;

    /// <inhertitdoc />
    public void Dispose()
    {
        tcpClient.Dispose();
    }
}
