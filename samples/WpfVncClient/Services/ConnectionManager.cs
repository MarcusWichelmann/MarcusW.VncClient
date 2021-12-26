using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient;
using Microsoft.Extensions.Logging;

namespace WpfVncClient.Services;

public class ConnectionManager
{
    private readonly VncClient _vncClient;

    public ConnectionManager(ILoggerFactory loggerFactory)
    {
        // Create and populate default logger factory for logging to Avalonia logging sinks
        _vncClient = new VncClient(loggerFactory);
    }

    public Task<RfbConnection> ConnectAsync(ConnectParameters parameters, CancellationToken cancellationToken = default)
    {
        // Uncomment for debugging/visualization purposes
        //parameters.RenderFlags |= RenderFlags.VisualizeRectangles;

        return _vncClient.ConnectAsync(parameters, cancellationToken);
    }
}
