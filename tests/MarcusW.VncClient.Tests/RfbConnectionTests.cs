using System;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Protocol.Implementation.SecurityTypes;
using MarcusW.VncClient.Protocol.Services;
using MarcusW.VncClient.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MarcusW.VncClient.Tests
{
    public class RfbConnectionTests
    {
        private readonly Mock<ITransportConnector> _transportConnectorMock;
        private readonly Mock<IRfbHandshaker> _rfbHandshakerMock;
        private readonly Mock<IRfbInitializer> _rfbInitializerMock;
        private readonly Mock<IRfbMessageReceiver> _messageReceiverMock;

        private readonly Mock<IRfbProtocolImplementation> _protocolMock;

        public RfbConnectionTests()
        {
            _transportConnectorMock = new Mock<ITransportConnector>();
            _rfbHandshakerMock = new Mock<IRfbHandshaker>();
            _rfbHandshakerMock.Setup(h => h.DoHandshakeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HandshakeResult(RfbProtocolVersion.RFB_3_8, new NoSecurityType(), null));
            _rfbInitializerMock = new Mock<IRfbInitializer>();
            _rfbInitializerMock.Setup(i => i.InitializeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InitializationResult(FrameSize.Zero, new PixelFormat(), "Desktop"));

            _messageReceiverMock = new Mock<IRfbMessageReceiver>();

            _protocolMock = new Mock<IRfbProtocolImplementation>();
            _protocolMock.Setup(p => p.CreateTransportConnector(It.IsAny<RfbConnectionContext>())).Returns(_transportConnectorMock.Object);
            _protocolMock.Setup(p => p.CreateRfbHandshaker(It.IsAny<RfbConnectionContext>())).Returns(_rfbHandshakerMock.Object);
            _protocolMock.Setup(p => p.CreateRfbInitializer(It.IsAny<RfbConnectionContext>())).Returns(_rfbInitializerMock.Object);
            _protocolMock.Setup(p => p.CreateMessageReceiver(It.IsAny<RfbConnectionContext>())).Returns(_messageReceiverMock.Object);
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
            await Assert.PropertyChangedAsync(rfbConnection, nameof(rfbConnection.ConnectionState), () => rfbConnection.StartAsync());
            Assert.Equal(ConnectionState.Connected, rfbConnection.ConnectionState);

            // Receive loop should have been started.
            _messageReceiverMock.Verify(receiver => receiver.StartReceiveLoop());

            // Status should update when connection is interrupted
            Assert.PropertyChanged(rfbConnection, nameof(rfbConnection.ConnectionState), () => {
                // Let's simulate a failure
                _messageReceiverMock.Raise(receiver => receiver.Failed += null, new BackgroundThreadFailedEventArgs(new Exception("Shit happens.")));
            });
            Assert.Equal(ConnectionState.Interrupted, rfbConnection.ConnectionState);

            // Reconnect should succeed after 1 second.
            await Assert.PropertyChangedAsync(rfbConnection, nameof(rfbConnection.ConnectionState), () => Task.Delay(TimeSpan.FromSeconds(1.5)));
            Assert.Equal(ConnectionState.Connected, rfbConnection.ConnectionState);

            // Close connection.
            await Assert.PropertyChangedAsync(rfbConnection, nameof(rfbConnection.ConnectionState), () => rfbConnection.CloseAsync());
            Assert.Equal(ConnectionState.Closed, rfbConnection.ConnectionState);
        }

        [Fact]
        public async Task Throws_When_Initial_Connect_Fails()
        {
            var connectParams = new ConnectParameters();

            // Make the initial connect fail.
            _transportConnectorMock.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>())).Throws<TimeoutException>();

            var rfbConnection = new RfbConnection(_protocolMock.Object, new NullLoggerFactory(), connectParams);

            // Start should throw.
            await Assert.ThrowsAsync<TimeoutException>(() => rfbConnection.StartAsync());

            // Connection should still be uninitialized
            Assert.Equal(ConnectionState.Uninitialized, rfbConnection.ConnectionState);
        }

        // TODO: Test reconnect limit
    }
}
