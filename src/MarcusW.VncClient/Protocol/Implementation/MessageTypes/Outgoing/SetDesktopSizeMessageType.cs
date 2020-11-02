using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing
{
    /// <summary>
    /// A message type for sending <see cref="SetDesktopSizeMessage"/>s.
    /// </summary>
    public class SetDesktopSizeMessageType : IOutgoingMessageType
    {
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownOutgoingMessageType.SetDesktopSize;

        /// <inheritdoc />
        public string Name => "SetDesktopSize";

        /// <inheritdoc />
        public bool IsStandardMessageType => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetDesktopSizeMessageType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public SetDesktopSizeMessageType(RfbConnectionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public void WriteToTransport(IOutgoingMessage<IOutgoingMessageType> message, ITransport transport, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (!(message is SetDesktopSizeMessage setDesktopSizeMessage))
                throw new ArgumentException($"Message is no {nameof(SetDesktopSizeMessage)}.", nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            // Execute the mutation
            (Size size, IImmutableSet<Screen> layout) = setDesktopSizeMessage.MutationFunc.Invoke(_state.RemoteFramebufferSize, _state.RemoteFramebufferLayout);

            // Calculate message size
            int messageSize = 2 + 2 * sizeof(ushort) + 2 + layout.Count * (sizeof(uint) + 4 * sizeof(ushort) + sizeof(uint));

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
    /// A message for updating the remote framebuffer size and layout.
    /// </summary>
    public class SetDesktopSizeMessage : IOutgoingMessage<SetDesktopSizeMessageType>
    {
        /// <summary>
        /// Represents the method that mutates a remote framebuffer size and layout.
        /// </summary>
        /// <param name="size">The current size.</param>
        /// <param name="layout">The current layout.</param>
        public delegate (Size size, IImmutableSet<Screen> layout) MutationFuncDelegate(Size size, IImmutableSet<Screen> layout);

        /// <summary>
        /// Gets the function that mutates the current remote framebuffer size and layout.
        /// It will be called by the sender thread right before the message gets sent.
        /// </summary>
        public MutationFuncDelegate MutationFunc { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetDesktopSizeMessage"/>.
        /// </summary>
        /// <param name="mutationFunc">
        /// The function that mutates the current remote framebuffer size and layout.
        /// It will be called by the sender thread right before the message gets sent.
        /// </param>
        public SetDesktopSizeMessage(MutationFuncDelegate mutationFunc)
        {
            MutationFunc = mutationFunc ?? throw new ArgumentNullException(nameof(mutationFunc));
        }

        /// <inheritdoc />
        public string? GetParametersOverview() => $"MutationFunc: {MutationFunc}";
    }
}
