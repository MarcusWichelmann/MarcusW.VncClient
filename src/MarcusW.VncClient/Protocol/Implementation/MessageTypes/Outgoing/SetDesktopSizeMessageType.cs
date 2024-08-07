using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;

/// <summary>
///     A message type for sending <see cref="SetDesktopSizeMessage" />s.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="SetDesktopSizeMessageType" />.
/// </remarks>
/// <param name="context">The connection context.</param>
public class SetDesktopSizeMessageType(RfbConnectionContext context) : IOutgoingMessageType
{
    private readonly ProtocolState _state = context.GetState<ProtocolState>();

    /// <inheritdoc />
    public byte Id => (byte)WellKnownOutgoingMessageType.SetDesktopSize;

    /// <inheritdoc />
    public string Name => "SetDesktopSize";

    /// <inheritdoc />
    public bool IsStandardMessageType => false;

    /// <inheritdoc />
    public void WriteToTransport(IOutgoingMessage<IOutgoingMessageType> message, ITransport transport,
        CancellationToken cancellationToken = default)
    {
        if (message is not SetDesktopSizeMessage setDesktopSizeMessage)
        {
            throw new ArgumentException($"Message is no {nameof(SetDesktopSizeMessage)}.", nameof(message));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Execute the mutation
        (Size size, IImmutableSet<Screen> layout) =
            setDesktopSizeMessage.MutationFunc.Invoke(_state.RemoteFramebufferSize, _state.RemoteFramebufferLayout);

        // Calculate message size
        int messageSize = 2 + 2 * sizeof(ushort) + 2
            + layout.Count * (sizeof(uint) + 4 * sizeof(ushort) + sizeof(uint));

        // Allocate buffer
        Span<byte> buffer = messageSize <= 1024 ? stackalloc byte[messageSize] : new byte[messageSize];

        // Message header
        buffer[0] = Id;
        buffer[1] = 0; // Padding

        // Size
        BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], (ushort)size.Width);
        BinaryPrimitives.WriteUInt16BigEndian(buffer[4..], (ushort)size.Height);

        // Number of screens
        buffer[6] = (byte)layout.Count;
        buffer[7] = 0; // Padding

        // Screens
        var bufferPosition = 8;
        foreach (Screen screen in layout)
        {
            BinaryPrimitives.WriteUInt32BigEndian(buffer[bufferPosition..], screen.Id);
            bufferPosition += sizeof(int);

            BinaryPrimitives.WriteUInt16BigEndian(buffer[bufferPosition..], (ushort)screen.Rectangle.Position.X);
            bufferPosition += sizeof(ushort);

            BinaryPrimitives.WriteUInt16BigEndian(buffer[bufferPosition..], (ushort)screen.Rectangle.Position.Y);
            bufferPosition += sizeof(ushort);

            BinaryPrimitives.WriteUInt16BigEndian(buffer[bufferPosition..], (ushort)screen.Rectangle.Size.Width);
            bufferPosition += sizeof(ushort);

            BinaryPrimitives.WriteUInt16BigEndian(buffer[bufferPosition..], (ushort)screen.Rectangle.Size.Height);
            bufferPosition += sizeof(ushort);

            BinaryPrimitives.WriteUInt32BigEndian(buffer[bufferPosition..], screen.Flags);
            bufferPosition += sizeof(int);
        }

        Debug.Assert(bufferPosition == messageSize, "bufferPosition == messageSize");

        // Write buffer to stream
        transport.Stream.Write(buffer);
    }
}

/// <summary>
///     A message for updating the remote framebuffer size and layout.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="SetDesktopSizeMessage" />.
/// </remarks>
/// <param name="mutationFunc">
///     The function that mutates the current remote framebuffer size and layout.
///     It will be called by the sender thread right before the message gets sent.
/// </param>
public class SetDesktopSizeMessage(SetDesktopSizeMessage.MutationFuncDelegate mutationFunc)
    : IOutgoingMessage<SetDesktopSizeMessageType>
{
    /// <summary>
    ///     Represents the method that mutates a remote framebuffer size and layout.
    /// </summary>
    /// <param name="size">The current size.</param>
    /// <param name="layout">The current layout.</param>
    public delegate (Size size, IImmutableSet<Screen> layout) MutationFuncDelegate(Size size,
        IImmutableSet<Screen> layout);

    /// <summary>
    ///     Gets the function that mutates the current remote framebuffer size and layout.
    ///     It will be called by the sender thread right before the message gets sent.
    /// </summary>
    public MutationFuncDelegate MutationFunc { get; } = mutationFunc;

    /// <inheritdoc />
    public string GetParametersOverview() => $"MutationFunc: {MutationFunc}";
}
