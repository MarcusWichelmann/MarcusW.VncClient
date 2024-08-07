using System.Threading;
using MarcusW.VncClient.Protocol.MessageTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Incoming;

/// <summary>
///     A message type for receiving a server bell message.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="BellMessageType" />.
/// </remarks>
/// <param name="context">The connection context.</param>
public class BellMessageType(RfbConnectionContext context) : IIncomingMessageType
{
    private readonly ILogger<BellMessageType>
        _logger = context.Connection.LoggerFactory.CreateLogger<BellMessageType>();

    /// <inheritdoc />
    public byte Id => (byte)WellKnownIncomingMessageType.Bell;

    /// <inheritdoc />
    public string Name => "Bell";

    /// <inheritdoc />
    public bool IsStandardMessageType => true;

    /// <inheritdoc />
    public void ReadMessage(ITransport transport, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug("Beep! https://www.youtube.com/watch?v=CZlfbep2LdU");

        context.Connection.OutputHandler?.RingBell();
    }
}
