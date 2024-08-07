using System.Threading;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.MessageTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Incoming;

/// <summary>
///     A message type for processing the end of a continuous updates phase.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="EndOfContinuousUpdatesMessageType" />.
/// </remarks>
/// <param name="context">The connection context.</param>
public class EndOfContinuousUpdatesMessageType(RfbConnectionContext context) : IIncomingMessageType
{
    private readonly ILogger<EndOfContinuousUpdatesMessageType> _logger =
        context.Connection.LoggerFactory.CreateLogger<EndOfContinuousUpdatesMessageType>();

    private readonly ProtocolState _state = context.GetState<ProtocolState>();

    /// <inheritdoc />
    public byte Id => (byte)WellKnownIncomingMessageType.EndOfContinuousUpdates;

    /// <inheritdoc />
    public string Name => "EndOfContinuousUpdates";

    /// <inheritdoc />
    public bool IsStandardMessageType => false;

    /// <inheritdoc />
    public void ReadMessage(ITransport transport, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Did we just learn that the server supports continuous updates?
        if (!_state.ServerSupportsContinuousUpdates)
        {
            _logger.LogDebug("Server supports the continuous updates extension.");

            // Mark the encoding and message type as used
            _state.EnsureEncodingTypeIsMarkedAsUsed<IPseudoEncodingType>(null,
                (int)WellKnownEncodingType.ContinuousUpdates);
            _state.EnsureMessageTypeIsMarkedAsUsed<IOutgoingMessageType>(null,
                (byte)WellKnownOutgoingMessageType.EnableContinuousUpdates);

            _state.ServerSupportsContinuousUpdates = true;
        }

        // Continuous updates have ended
        _state.ContinuousUpdatesEnabled = false;
    }
}
