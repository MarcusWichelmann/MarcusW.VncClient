using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Protocol.Implementation;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient;

/// <summary>
///     Client for the RFB protocol which allows connecting to remote VNC servers.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="VncClient" />.
/// </remarks>
/// <param name="loggerFactory">The logger factory implementation that should be used for creating new loggers.</param>
/// <param name="protocolImplementation">The <see cref="IRfbProtocolImplementation" /> that should be used.</param>
public class VncClient(ILoggerFactory loggerFactory, IRfbProtocolImplementation protocolImplementation)
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="VncClient" />.
    /// </summary>
    /// <param name="loggerFactory">The logger factory implementation that should be used for creating new loggers.</param>
    public VncClient(ILoggerFactory loggerFactory) : this(loggerFactory, new DefaultImplementation()) { }

    /// <summary>
    ///     Tries to connect to a VNC server and initializes a new connection object.
    /// </summary>
    /// <param name="parameters">The connect parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An initialized <see cref="RfbConnection" /> instance.</returns>
    public async Task<RfbConnection> ConnectAsync(ConnectParameters parameters,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Validate and freeze the parameters so they are immutable from now on
        parameters.ValidateAndFreezeRecursively();

        var rfbConnection = new RfbConnection(protocolImplementation, loggerFactory, parameters);
        await rfbConnection.StartAsync(cancellationToken).ConfigureAwait(false);
        return rfbConnection;
    }
}
