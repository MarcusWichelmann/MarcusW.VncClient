using System;
using System.Threading;
using MarcusW.VncClient.Protocol.MessageTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Incoming
{
    /// <summary>
    /// Defines the xvp message codes from server to client.
    /// </summary>
    public enum XvpServerMessageCode : byte
    {
        /// <summary>
        /// The server could not perform the requested xvp operation.
        /// </summary>
        Fail = 0,

        /// <summary>
        /// The server supports the xvp extension and is ready to receive commands.
        /// </summary>
        Init = 1
    }

    /// <summary>
    /// A message type for receiving xvp server messages (VM power control responses).
    /// </summary>
    public class XvpServerMessageType : IIncomingMessageType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<XvpServerMessageType> _logger;
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownIncomingMessageType.XvpServer;

        /// <inheritdoc />
        public string Name => "XvpServer";

        /// <inheritdoc />
        public bool IsStandardMessageType => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="XvpServerMessageType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public XvpServerMessageType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<XvpServerMessageType>();
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public void ReadMessage(ITransport transport, CancellationToken cancellationToken = default)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));

            cancellationToken.ThrowIfCancellationRequested();

            // Read 3 bytes: padding + version + message code
            Span<byte> buffer = stackalloc byte[3];
            transport.Stream.ReadAll(buffer, cancellationToken);

            byte version = buffer[1];
            var messageCode = (XvpServerMessageCode)buffer[2];

            switch (messageCode)
            {
                case XvpServerMessageCode.Init:
                    _logger.LogDebug("Server supports xvp extension version {Version}", version);
                    _state.ServerSupportsXvp = true;
                    _state.XvpVersion = version;
                    // Mark the xvp server message type as used
                    _state.EnsureMessageTypeIsMarkedAsUsed(this);
                    break;

                case XvpServerMessageCode.Fail:
                    _logger.LogWarning("Server reported xvp operation failed (version {Version})", version);
                    _context.Connection.OutputHandler?.HandleXvpOperationFailed();
                    break;

                default:
                    _logger.LogWarning("Received unknown xvp message code: {MessageCode}", messageCode);
                    break;
            }
        }
    }
}
