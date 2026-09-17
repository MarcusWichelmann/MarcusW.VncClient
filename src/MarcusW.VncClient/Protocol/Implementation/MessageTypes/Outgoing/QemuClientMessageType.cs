using System;
using System.Buffers.Binary;
using System.Threading;
using MarcusW.VncClient.Protocol.MessageTypes;

namespace MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing
{
    /// <summary>
    /// Defines the QEMU client message subtypes.
    /// </summary>
    public enum QemuClientMessageSubtype : byte
    {
        /// <summary>
        /// Extended key event with keysym and keycode.
        /// </summary>
        ExtendedKeyEvent = 0,

        /// <summary>
        /// Audio control message.
        /// </summary>
        Audio = 1
    }

    /// <summary>
    /// A message type for sending QEMU client messages.
    /// </summary>
    public class QemuClientMessageType : IOutgoingMessageType
    {
        /// <inheritdoc />
        public byte Id => (byte)WellKnownOutgoingMessageType.QemuClient;

        /// <inheritdoc />
        public string Name => "QemuClient";

        /// <inheritdoc />
        public bool IsStandardMessageType => false;

        /// <inheritdoc />
        public void WriteToTransport(IOutgoingMessage<IOutgoingMessageType> message, ITransport transport, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));

            cancellationToken.ThrowIfCancellationRequested();

            switch (message)
            {
                case QemuExtendedKeyEventMessage keyMessage:
                    WriteExtendedKeyEvent(keyMessage, transport);
                    break;
                case QemuAudioMessage audioMessage:
                    WriteAudioMessage(audioMessage, transport);
                    break;
                default:
                    throw new ArgumentException($"Unknown QEMU message type: {message.GetType().Name}", nameof(message));
            }
        }

        private void WriteExtendedKeyEvent(QemuExtendedKeyEventMessage message, ITransport transport)
        {
            // Message format:
            // 1 byte - message-type (255)
            // 1 byte - submessage-type (0)
            // 2 bytes - down-flag (U16)
            // 4 bytes - keysym (U32)
            // 4 bytes - keycode (U32)
            Span<byte> buffer = stackalloc byte[12];
            buffer[0] = Id;
            buffer[1] = (byte)QemuClientMessageSubtype.ExtendedKeyEvent;
            BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], message.DownFlag ? (ushort)1 : (ushort)0);
            BinaryPrimitives.WriteUInt32BigEndian(buffer[4..], message.Keysym);
            BinaryPrimitives.WriteUInt32BigEndian(buffer[8..], message.Keycode);

            transport.Stream.Write(buffer);
        }

        private void WriteAudioMessage(QemuAudioMessage message, ITransport transport)
        {
            // Audio enable/disable:
            // 1 byte - message-type (255)
            // 1 byte - submessage-type (1)
            // 2 bytes - operation (0=disable, 1=enable, 2=set format)

            if (message.Operation == QemuAudioOperation.SetFormat)
            {
                // Set format message:
                // 1 byte - message-type (255)
                // 1 byte - submessage-type (1)
                // 2 bytes - operation (2)
                // 1 byte - sample-format
                // 1 byte - nchannels
                // 4 bytes - frequency (U32)
                Span<byte> buffer = stackalloc byte[10];
                buffer[0] = Id;
                buffer[1] = (byte)QemuClientMessageSubtype.Audio;
                BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], (ushort)message.Operation);
                buffer[4] = (byte)message.SampleFormat;
                buffer[5] = message.Channels;
                BinaryPrimitives.WriteUInt32BigEndian(buffer[6..], message.Frequency);
                transport.Stream.Write(buffer);
            }
            else
            {
                Span<byte> buffer = stackalloc byte[4];
                buffer[0] = Id;
                buffer[1] = (byte)QemuClientMessageSubtype.Audio;
                BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], (ushort)message.Operation);
                transport.Stream.Write(buffer);
            }
        }
    }

    /// <summary>
    /// A message for sending QEMU extended key events with keysym and keycode.
    /// </summary>
    public class QemuExtendedKeyEventMessage : IOutgoingMessage<QemuClientMessageType>
    {
        /// <summary>
        /// Gets whether the key is pressed (true) or released (false).
        /// </summary>
        public bool DownFlag { get; }

        /// <summary>
        /// Gets the X11 keysym value.
        /// </summary>
        public uint Keysym { get; }

        /// <summary>
        /// Gets the XT keycode (scancode).
        /// </summary>
        public uint Keycode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QemuExtendedKeyEventMessage"/>.
        /// </summary>
        /// <param name="downFlag">Whether the key is pressed.</param>
        /// <param name="keysym">The X11 keysym value.</param>
        /// <param name="keycode">The XT keycode (scancode).</param>
        public QemuExtendedKeyEventMessage(bool downFlag, uint keysym, uint keycode)
        {
            DownFlag = downFlag;
            Keysym = keysym;
            Keycode = keycode;
        }

        /// <inheritdoc />
        public string? GetParametersOverview() => $"Down: {DownFlag}, Keysym: 0x{Keysym:X}, Keycode: 0x{Keycode:X}";
    }

    /// <summary>
    /// Defines the QEMU audio operations.
    /// </summary>
    public enum QemuAudioOperation : ushort
    {
        /// <summary>
        /// Disable audio capture.
        /// </summary>
        Disable = 0,

        /// <summary>
        /// Enable audio capture.
        /// </summary>
        Enable = 1,

        /// <summary>
        /// Set the audio sample format.
        /// </summary>
        SetFormat = 2
    }

    /// <summary>
    /// Defines the QEMU audio sample formats.
    /// </summary>
    public enum QemuAudioSampleFormat : byte
    {
        /// <summary>
        /// Unsigned 8-bit.
        /// </summary>
        U8 = 0,

        /// <summary>
        /// Signed 8-bit.
        /// </summary>
        S8 = 1,

        /// <summary>
        /// Unsigned 16-bit.
        /// </summary>
        U16 = 2,

        /// <summary>
        /// Signed 16-bit.
        /// </summary>
        S16 = 3,

        /// <summary>
        /// Unsigned 32-bit.
        /// </summary>
        U32 = 4,

        /// <summary>
        /// Signed 32-bit.
        /// </summary>
        S32 = 5
    }

    /// <summary>
    /// A message for controlling QEMU audio.
    /// </summary>
    public class QemuAudioMessage : IOutgoingMessage<QemuClientMessageType>
    {
        /// <summary>
        /// Gets the audio operation.
        /// </summary>
        public QemuAudioOperation Operation { get; }

        /// <summary>
        /// Gets the sample format (only used with SetFormat operation).
        /// </summary>
        public QemuAudioSampleFormat SampleFormat { get; }

        /// <summary>
        /// Gets the number of channels (1=mono, 2=stereo, only used with SetFormat).
        /// </summary>
        public byte Channels { get; }

        /// <summary>
        /// Gets the sample frequency in Hz (only used with SetFormat).
        /// </summary>
        public uint Frequency { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QemuAudioMessage"/> for enable/disable.
        /// </summary>
        /// <param name="operation">The audio operation (Enable or Disable).</param>
        public QemuAudioMessage(QemuAudioOperation operation)
        {
            if (operation == QemuAudioOperation.SetFormat)
                throw new ArgumentException("Use the other constructor for SetFormat operation.", nameof(operation));

            Operation = operation;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QemuAudioMessage"/> for setting format.
        /// </summary>
        /// <param name="sampleFormat">The sample format.</param>
        /// <param name="channels">The number of channels (1 or 2).</param>
        /// <param name="frequency">The sample frequency in Hz.</param>
        public QemuAudioMessage(QemuAudioSampleFormat sampleFormat, byte channels, uint frequency)
        {
            if (channels != 1 && channels != 2)
                throw new ArgumentException("Channels must be 1 (mono) or 2 (stereo).", nameof(channels));

            Operation = QemuAudioOperation.SetFormat;
            SampleFormat = sampleFormat;
            Channels = channels;
            Frequency = frequency;
        }

        /// <inheritdoc />
        public string? GetParametersOverview() => Operation == QemuAudioOperation.SetFormat
            ? $"Operation: {Operation}, Format: {SampleFormat}, Channels: {Channels}, Frequency: {Frequency}Hz"
            : $"Operation: {Operation}";
    }
}
