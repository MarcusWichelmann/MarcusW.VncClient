using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.Implementation.Services.Transports;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MarcusW.VncClient.Tests.Protocol.Implementation.Services.Transports
{
    public class TcpConnectorTests : IDisposable
    {
        private readonly TcpListener _testServer;
        private readonly IPEndPoint _testEndpoint;

        // Some endpoint which always drops the SYNs (Sorry Google :P)
        private readonly IPEndPoint _droppingEndpoint = new IPEndPoint(IPAddress.Parse("8.8.8.8"), 25623);

        public TcpConnectorTests()
        {
            // Create a testing server (Port is chosen automatically)
            _testServer = new TcpListener(IPAddress.IPv6Loopback, 0);
            _testServer.Start();

            // Get the chosen port
            _testEndpoint = (IPEndPoint)_testServer.LocalEndpoint;
        }

        [Fact]
        public async Task Connects_Successfully()
        {
            var connector = new TransportConnector(new ConnectParameters {
                Endpoint = _testEndpoint,
                ConnectTimeout = Timeout.InfiniteTimeSpan
            }, new NullLogger<TransportConnector>());
            var connectTask = connector.ConnectAsync();

            // Accept client
            using var client = await _testServer.AcceptTcpClientAsync();

            // Connect should succeed
            (await connectTask).Dispose();
        }

        [Fact]
        public async Task Throws_On_Timeout()
        {
            var connector = new TransportConnector(new ConnectParameters {
                Endpoint = _droppingEndpoint,
                ConnectTimeout = TimeSpan.FromSeconds(1)
            }, new NullLogger<TransportConnector>());
            var connectTask = connector.ConnectAsync();

            // Connect should throw
            await Assert.ThrowsAsync<TimeoutException>(() => connectTask);
        }

        [Fact]
        public async Task Throws_On_Cancel()
        {
            using var cts = new CancellationTokenSource();

            var connector = new TransportConnector(new ConnectParameters {
                Endpoint = _droppingEndpoint,
                ConnectTimeout = Timeout.InfiniteTimeSpan
            }, new NullLogger<TransportConnector>());
            var connectTask = connector.ConnectAsync(cts.Token);

            // Task should still be alive
            Assert.False(connectTask.IsCompleted);

            // Cancel connect
            cts.CancelAfter(100);

            // Connect should throw
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connectTask);
        }

        public void Dispose()
        {
            _testServer.Stop();
        }
    }
}
