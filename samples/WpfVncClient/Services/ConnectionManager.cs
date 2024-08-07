using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient;
using Microsoft.Extensions.Logging;

namespace WpfVncClient.Services;

public class ConnectionManager(ILoggerFactory loggerFactory)
{
    private readonly VncClient _vncClient = new(loggerFactory);

    public Task<RfbConnection> ConnectAsync(ConnectParameters parameters, CancellationToken cancellationToken = default)
        =>

            // Uncomment for debugging/visualization purposes
            //parameters.RenderFlags |= RenderFlags.VisualizeRectangles;
            _vncClient.ConnectAsync(parameters, cancellationToken);
}
