using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Protocol.Services.Communication;
using MarcusW.VncClient.Protocol.Services.Connection;
using MarcusW.VncClient.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MarcusW.VncClient.Tests
{
    public class RfbConnectionTests
    {
        private readonly Mock<ITcpConnector> _tcpConnectorMock;
        private readonly Mock<IRfbMessageReceiver> _messageReceiverMock;

        private readonly Mock<IRfbProtocolImplementation> _protocolMock;

        public RfbConnectionTests()
        {
            _tcpConnectorMock = new Mock<ITcpConnector>();
            _messageReceiverMock = new Mock<IRfbMessageReceiver>();

            _protocolMock = new Mock<IRfbProtocolImplementation>();
            _protocolMock.Setup(p => p.CreateTcpConnector()).Returns(_tcpConnectorMock.Object);
            _protocolMock.Setup(p => p.CreateMessageReceiver(It.IsAny<RfbConnection>()))
                .Returns(_messageReceiverMock.Object);
        }

        [Fact]
        public async Task Updates_ConnectionState_On_Connect_Reconnect_Close()
        {
            var connectParams = new ConnectParameters {
                ReconnectDelay = TimeSpan.FromSeconds(1),
                MaxReconnectAttempts = 1
            };

            var rfbConnection = new RfbConnection(_protocolMock.Object, new NullLoggerFactory(), connectParams);

            Assert.Equal(ConnectionState.Uninitialized, rfbConnection.ConnectionState);

            // Start connection.
            await Assert.PropertyChangedAsync(rfbConnection, nameof(rfbConnection.ConnectionState),
                () => rfbConnection.StartAsync());
            Assert.Equal(ConnectionState.Connected, rfbConnection.ConnectionState);

            // Receive loop should have been started.
            _messageReceiverMock.Verify(receiver => receiver.StartReceiveLoop());

            // Status should update when connection is interrupted
            Assert.PropertyChanged(rfbConnection, nameof(rfbConnection.ConnectionState), () => {
                // Let's simulate a failure
                _messageReceiverMock.Raise(receiver => receiver.Failed += null,
                    new BackgroundThreadFailedEventArgs(new Exception("Shit happens.")));
            });
            Assert.Equal(ConnectionState.Interrupted, rfbConnection.ConnectionState);

            // Reconnect should succeed after 1 second.
            await Assert.PropertyChangedAsync(rfbConnection, nameof(rfbConnection.ConnectionState),
                () => Task.Delay(TimeSpan.FromSeconds(1.5)));
            Assert.Equal(ConnectionState.Connected, rfbConnection.ConnectionState);

            // Close connection.
            await Assert.PropertyChangedAsync(rfbConnection, nameof(rfbConnection.ConnectionState),
                () => rfbConnection.CloseAsync());
            Assert.Equal(ConnectionState.Closed, rfbConnection.ConnectionState);
        }

        [Fact]
        public async Task Throws_When_Initial_Connect_Fails()
        {
            var connectParams = new ConnectParameters();

            // Make the initial connect fail.
            _tcpConnectorMock
                .Setup(c => c.ConnectAsync(It.IsAny<IPEndPoint>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Throws<TimeoutException>();

            var rfbConnection = new RfbConnection(_protocolMock.Object, new NullLoggerFactory(), connectParams);

            // Start should throw.
            await Assert.ThrowsAsync<TimeoutException>(() => rfbConnection.StartAsync());

            // Connection should still be uninitialized
            Assert.Equal(ConnectionState.Uninitialized, rfbConnection.ConnectionState);
        }

        // TODO: Test reconnect limit
    }
}
