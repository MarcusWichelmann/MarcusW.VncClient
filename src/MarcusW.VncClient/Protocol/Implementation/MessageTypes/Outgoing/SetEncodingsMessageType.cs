using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing
{
    /// <summary>
    /// A message type for sending <see cref="SetEncodingsMessage"/>s.
    /// </summary>
    public class SetEncodingsMessageType : IOutgoingMessageType
    {
        /// <inheritdoc />
        public byte Id => (byte)WellKnownOutgoingMessageType.SetEncodings;

        /// <inheritdoc />
        public string Name => "SetEncodings";

        /// <inheritdoc />
        public bool IsStandardMessageType => true;

        /// <inheritdoc />
        public void WriteToTransport(IOutgoingMessage<IOutgoingMessageType> message, ITransport transport, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (!(message is SetEncodingsMessage setEncodingsMessage))
                throw new ArgumentException($"Message is no {nameof(SetEncodingsMessage)}.", nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            // Get encoding types
            IImmutableSet<IEncodingType> encodingTypes = setEncodingsMessage.SupportedEncodingTypes;
            if (encodingTypes.Count > ushort.MaxValue)
                throw new InvalidOperationException("Maximum number of encoding types exceeded.");
            var encodingTypesCount = (ushort)encodingTypes.Count;

            // Order encoding types by priority
            IEnumerable<IEncodingType> orderedEncodingTypes = encodingTypes.OrderByDescending(et => et.Priority);

            // Calculate message size
            int messageSize = 2 + sizeof(ushort) + encodingTypesCount * sizeof(int);

            // Allocate buffer
            Span<byte> buffer = messageSize <= 1024 ? stackalloc byte[messageSize] : new byte[messageSize];

            // Message header
            buffer[0] = Id;
            buffer[1] = 0; // Padding

            // Number of encodings
            BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], encodingTypesCount);

            // Encoding type ids
            var bufferPosition = 4;
            foreach (IEncodingType encodingType in orderedEncodingTypes)
            {
                BinaryPrimitives.WriteInt32BigEndian(buffer[bufferPosition..], encodingType.Id);
                bufferPosition += sizeof(int);
            }

            Debug.Assert(bufferPosition == messageSize, "bufferPosition == messageSize");

            // Write buffer to stream
            transport.Stream.Write(buffer);
        }
    }

    /// <summary>
    /// A message declaring which encoding types the client supports.
    /// </summary>
    public class SetEncodingsMessage : IOutgoingMessage<SetEncodingsMessageType>
    {
        /// <summary>
        /// Gets the encoding types collection that will be reported to the server.
        /// </summary>
        public IImmutableSet<IEncodingType> SupportedEncodingTypes { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetEncodingsMessage"/>.
        /// </summary>
        /// <param name="supportedEncodingTypes">The encoding types collection that will be reported to the server.</param>
        public SetEncodingsMessage(IImmutableSet<IEncodingType> supportedEncodingTypes)
        {
            SupportedEncodingTypes = supportedEncodingTypes ?? throw new ArgumentNullException(nameof(supportedEncodingTypes));
        }

        /// <inheritdoc />
        public string? GetParametersOverview() => $"SupportedEncodingTypes: {string.Join(", ", SupportedEncodingTypes.Select(et => et.Name))}";
    }
}
