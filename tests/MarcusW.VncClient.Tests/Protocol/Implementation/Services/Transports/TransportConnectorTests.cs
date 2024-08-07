using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Protocol.Implementation.Services.Transports;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MarcusW.VncClient.Tests.Protocol.Implementation.Services.Transports;

public class TcpConnectorTests : IDisposable
{
    // Some endpoint which always drops the SYNs (Sorry Google :P)
    private readonly TcpTransportParameters _droppingEndpoint = new() {
        Host = "8.8.8.8",
        Port = 1,
    };

    private readonly TcpTransportParameters _testEndpoint;
    private readonly TcpListener _testServer;

    public TcpConnectorTests()
    {
        // Create a testing server (Port is chosen automatically)
        _testServer = new(IPAddress.IPv6Loopback, 0);
        _testServer.Start();

        // Get the chosen port
        var serverEndpoint = (IPEndPoint)_testServer.LocalEndpoint;
        _testEndpoint = new() {
            Host = IPAddress.IPv6Loopback.ToString(),
            Port = serverEndpoint.Port,
        };
    }

    public void Dispose()
    {
        _testServer.Stop();
    }

    [Fact]
    public async Task Connects_Successfully()
    {
        var connector = new TransportConnector(new() {
            TransportParameters = _testEndpoint,
            ConnectTimeout = Timeout.InfiniteTimeSpan,
        }, new NullLogger<TransportConnector>());
        Task<ITransport> connectTask = connector.ConnectAsync();

        // Accept client
        using TcpClient client = await _testServer.AcceptTcpClientAsync();

        // Connect should succeed
        (await connectTask).Dispose();
    }

    [Fact]
    public async Task Throws_On_Cancel()
    {
        using var cts = new CancellationTokenSource();

        var connector = new TransportConnector(new() {
            TransportParameters = _droppingEndpoint,
            ConnectTimeout = Timeout.InfiniteTimeSpan,
        }, new NullLogger<TransportConnector>());
        Task<ITransport> connectTask = connector.ConnectAsync(cts.Token);

        // Task should still be alive
        Assert.False(connectTask.IsCompleted);

        // Cancel connect
        cts.CancelAfter(100);

        // Connect should throw
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connectTask);
    }

    [Fact]
    public async Task Throws_On_Timeout()
    {
        var connector = new TransportConnector(new() {
            TransportParameters = _droppingEndpoint,
            ConnectTimeout = TimeSpan.FromSeconds(1),
        }, new NullLogger<TransportConnector>());
        Task<ITransport> connectTask = connector.ConnectAsync();

        // Connect should throw
        await Assert.ThrowsAsync<TimeoutException>(() => connectTask);
    }
}
