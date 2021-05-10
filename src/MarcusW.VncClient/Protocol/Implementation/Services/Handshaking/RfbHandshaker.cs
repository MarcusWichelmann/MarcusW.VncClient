using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Protocol.Services;
using MarcusW.VncClient.Utils;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Handshaking
{
    /// <inheritdoc />
    public class RfbHandshaker : IRfbHandshaker
    {
        private readonly RfbConnectionContext _context;
        private readonly ProtocolState _state;
        private readonly ILogger<RfbHandshaker> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RfbHandshaker"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public RfbHandshaker(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _state = context.GetState<ProtocolState>();
            _logger = context.Connection.LoggerFactory.CreateLogger<RfbHandshaker>();
        }

        /// <inheritdoc />
        public async Task<ITransport?> DoHandshakeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Doing protocol handshake...");

            ITransport currentTransport = _context.Transport ?? throw new InvalidOperationException("Cannot do handshake before a transport has been created.");

            // Negotiate the protocol version that both sides will use
            RfbProtocolVersion protocolVersion = await NegotiateProtocolVersionAsync(currentTransport, cancellationToken).ConfigureAwait(false);
            _state.ProtocolVersion = protocolVersion;

            // Negotiate which security type will be used
            ISecurityType usedSecurityType = await NegotiateSecurityTypeAsync(currentTransport, cancellationToken).ConfigureAwait(false);
            _state.UsedSecurityType = usedSecurityType;

            // Execute authentication
            _logger.LogDebug("Negotiated security type: {name}({id}). Authenticating...", usedSecurityType.Name, usedSecurityType.Id);
            AuthenticationResult authenticationResult = await usedSecurityType
                .AuthenticateAsync(_context.Connection.Parameters.AuthenticationHandler, cancellationToken).ConfigureAwait(false);

            // When a tunnel was built, use that transport for further communication
            if (authenticationResult.TunnelTransport != null)
                currentTransport = authenticationResult.TunnelTransport;

            if (authenticationResult.ExpectSecurityResult)
            {
                // Read the security result
                ReadOnlyMemory<byte> securityResultBytes = await currentTransport.Stream.ReadAllAsync(4, cancellationToken).ConfigureAwait(false);
                uint securityResult = BinaryPrimitives.ReadUInt32BigEndian(securityResultBytes.Span);

                // Authentication failed?
                if (securityResult > 0)
                {
                    // From version 3.8 onwards the server sends a reason
                    if (protocolVersion >= RfbProtocolVersion.RFB_3_8)
                    {
                        string reason = await ReadFailureReasonAsync(currentTransport, cancellationToken).ConfigureAwait(false);
                        throw new HandshakeFailedException($"Authentication failed: {reason}");
                    }

                    // There is no reason
                    throw new HandshakeFailedException("Authentication failed" + (securityResult == 2 ? " because of too many attempts." : "."));
                }
            }

            return authenticationResult.TunnelTransport;
        }

        private async Task<RfbProtocolVersion> NegotiateProtocolVersionAsync(ITransport transport, CancellationToken cancellationToken = default)
        {
            // Read maximum supported server protocol version
            RfbProtocolVersion serverProtocolVersion = await ReadProtocolVersionAsync(transport, cancellationToken).ConfigureAwait(false);

            // Select used protocol version
            RfbProtocolVersion clientProtocolVersion;
            if (serverProtocolVersion == RfbProtocolVersion.Unknown)
            {
                clientProtocolVersion = RfbProtocolVersions.LatestSupported;
                _logger.LogDebug("Supported server protocol version is unknown, too new? Trying latest protocol version {clientProtocolVersion}.",
                    clientProtocolVersion.ToReadableString());
            }
            else if (serverProtocolVersion > RfbProtocolVersions.LatestSupported)
            {
                clientProtocolVersion = RfbProtocolVersions.LatestSupported;
                _logger.LogDebug("Supported server protocol version {serverProtocolVersion} is too new. Requesting latest version supported by the client.",
                    serverProtocolVersion.ToReadableString());
            }
            else
            {
                clientProtocolVersion = serverProtocolVersion;
                _logger.LogDebug("Server supports protocol version {serverProtocolVersion}. Choosing that as the highest one that's supported by both sides.",
                    serverProtocolVersion.ToReadableString());
            }

            // Send selected protocol version
            await SendProtocolVersionAsync(transport, clientProtocolVersion, cancellationToken).ConfigureAwait(false);

            return clientProtocolVersion;
        }

        private async Task<ISecurityType> NegotiateSecurityTypeAsync(ITransport transport, CancellationToken cancellationToken = default)
        {
            ISecurityType? usedSecurityType;

            if (_state.ProtocolVersion == RfbProtocolVersion.RFB_3_3)
            {
                // Read the security type id that was decided by the server (is 0 if the connection/handshake failed)
                ReadOnlyMemory<byte> securityTypeIdBytes = await transport.Stream.ReadAllAsync(4, cancellationToken).ConfigureAwait(false);
                uint securityTypeId = BinaryPrimitives.ReadUInt32BigEndian(securityTypeIdBytes.Span);
                if (securityTypeId == (int)WellKnownSecurityType.Invalid)
                {
                    string reason = await ReadFailureReasonAsync(transport, cancellationToken).ConfigureAwait(false);
                    throw new HandshakeFailedException($"Handshake failed. Server reported: {reason}");
                }

                // The Id is received as an UInt32, but only values 0-255 are valid because protocol >=3.7 doesn't support any higher values
                Debug.Assert(securityTypeId <= byte.MaxValue, "securityTypeId <= byte.MaxValue");
                var id = (byte)securityTypeId;

                // Search security type
                Debug.Assert(_context.SupportedSecurityTypes != null, "_context.SupportedSecurityTypes != null");
                usedSecurityType = _context.SupportedSecurityTypes.FirstOrDefault(st => st.Id == id);
                if (usedSecurityType == null)
                    throw new HandshakeFailedException($"Server decided on the used security type, but no security type for the ID {id} is known.");

                return usedSecurityType;
            }

            // Read number of security types (is 0 if the connection/handshake failed)
            byte numberOfSecurityTypes = (await transport.Stream.ReadAllAsync(1, cancellationToken).ConfigureAwait(false)).Span[0];
            if (numberOfSecurityTypes == 0)
            {
                string reason = await ReadFailureReasonAsync(transport, cancellationToken).ConfigureAwait(false);
                throw new HandshakeFailedException($"Handshake failed. Server reported: {reason}");
            }

            // Read the security types supported by the server and find all security types that are known by our protocol implementation and supported by the server
            byte[] securityTypeIds = (await transport.Stream.ReadAllAsync(numberOfSecurityTypes, cancellationToken).ConfigureAwait(false)).ToArray();
            Debug.Assert(_context.SupportedSecurityTypes != null, "_context.SupportedSecurityTypes != null");
            IEnumerable<ISecurityType> usableSecurityTypes = _context.SupportedSecurityTypes.Where(st => securityTypeIds.Contains(st.Id));

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Server supports the following security types: {securityTypeIds}", string.Join(", ", securityTypeIds));

            // Select the one with the hightest priority (hopefully the best one!)
            usedSecurityType = usableSecurityTypes.OrderByDescending(st => st.Priority).FirstOrDefault();
            if (usedSecurityType == null)
                throw new HandshakeFailedException("Could not negotiate any common security types between the server and the client.");

            // Tell the server, which security type was chosen
            _logger.LogDebug("Informing server about chosen security type: {name}({id})", usedSecurityType.Name, usedSecurityType.Id);
            await transport.Stream.WriteAsync(new[] { usedSecurityType.Id }, 0, 1, cancellationToken).ConfigureAwait(false);

            return usedSecurityType;
        }

        private async Task<RfbProtocolVersion> ReadProtocolVersionAsync(ITransport transport, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Reading protocol version...");

            // The protocol version info always consists of 12 bytes
            ReadOnlyMemory<byte> bytes = await transport.Stream.ReadAllAsync(12, cancellationToken).ConfigureAwait(false);
#if NETSTANDARD2_0
            string protocolVersionString = Encoding.ASCII.GetString(bytes.Span.ToArray()).TrimEnd('\n');
#else
            string protocolVersionString = Encoding.ASCII.GetString(bytes.Span).TrimEnd('\n');
#endif

            RfbProtocolVersion protocolVersion = RfbProtocolVersions.GetFromStringRepresentation(protocolVersionString);
            if (protocolVersion == RfbProtocolVersion.Unknown)
                _logger.LogWarning("Unknown protocol version {protocolVersionString}.", protocolVersionString);

            return protocolVersion;
        }

        private async Task SendProtocolVersionAsync(ITransport transport, RfbProtocolVersion protocolVersion, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending protocol version {protocolVersion}...", protocolVersion.ToReadableString());

            string protocolVersionString = protocolVersion.GetStringRepresentation() + '\n';
            byte[] bytes = Encoding.ASCII.GetBytes(protocolVersionString);
            Debug.Assert(bytes.Length == 12, "bytes.Length == 12");

            await transport.Stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string> ReadFailureReasonAsync(ITransport transport, CancellationToken cancellationToken = default)
        {
            ReadOnlyMemory<byte> reasonLengthBytes = await transport.Stream.ReadAllAsync(4, cancellationToken).ConfigureAwait(false);
            uint reasonLength = BinaryPrimitives.ReadUInt32BigEndian(reasonLengthBytes.Span);

            ReadOnlyMemory<byte> reasonStringBytes = await transport.Stream.ReadAllAsync((int)reasonLength, cancellationToken).ConfigureAwait(false);
#if NETSTANDARD2_0
            string reasonString = Encoding.UTF8.GetString(reasonStringBytes.Span.ToArray());
#else
            string reasonString = Encoding.UTF8.GetString(reasonStringBytes.Span);
#endif

            return reasonString;
        }
    }
}
