using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.Messages;
using MarcusW.VncClient.Protocol.SecurityTypes;

namespace MarcusW.VncClient.Protocol.Implementation
{
    /// <inhertitdoc />
    public class ProtocolState : IRfbProtocolState
    {
        private readonly RfbConnectionContext _context;

        private readonly StateValue<RfbProtocolVersion> _protocolVersionValue = new StateValue<RfbProtocolVersion>(RfbProtocolVersion.Unknown);

        private readonly StateValue<ISecurityType?> _usedSecurityTypeValue = new StateValue<ISecurityType?>(null);

        private readonly StateValue<IImmutableDictionary<byte, IMessage>> _usedMessagesValue =
            new StateValue<IImmutableDictionary<byte, IMessage>>(ImmutableDictionary<byte, IMessage>.Empty);

        private readonly StateValue<IImmutableDictionary<int, IEncodingType>> _usedEncodingTypesValue =
            new StateValue<IImmutableDictionary<int, IEncodingType>>(ImmutableDictionary<int, IEncodingType>.Empty);

        private readonly StateValue<FrameSize> _framebufferSizeValue = new StateValue<FrameSize>(FrameSize.Zero);

        private readonly StateValue<PixelFormat> _framebufferFormatValue = new StateValue<PixelFormat>(PixelFormat.Unknown);

        private readonly StateValue<string?> _desktopNameValue = new StateValue<string?>(null);

        private readonly StateValue<bool> _continuousUpdatesEnabledValue = new StateValue<bool>(false);

        /// <summary>
        /// Gets or sets the used protocol version.
        /// </summary>
        public RfbProtocolVersion ProtocolVersion
        {
            get => _protocolVersionValue.Value;
            set
            {
                _protocolVersionValue.Value = value;
                _context.ConnectionDetails.SetProtocolVersion(value);
            }
        }

        /// <summary>
        /// Gets or sets the security type that was negotiated during handshake.
        /// </summary>
        public ISecurityType? UsedSecurityType
        {
            get => _usedSecurityTypeValue.Value;
            set
            {
                _usedSecurityTypeValue.Value = value;
                _context.ConnectionDetails.SetUsedSecurityType(value);
            }
        }

        /// <summary>
        /// Gets or sets the messages that are known to be supported by both sides.
        /// </summary>
        public IImmutableDictionary<byte, IMessage> UsedMessages
        {
            get => _usedMessagesValue.Value;
            set
            {
                _usedMessagesValue.Value = value;
                _context.ConnectionDetails.SetUsedMessages(value);
            }
        }

        /// <summary>
        /// Gets or sets the encoding types that are either known to be supported by both sides, or at least safe to use anyway.
        /// </summary>
        public IImmutableDictionary<int, IEncodingType> UsedEncodingTypes
        {
            get => _usedEncodingTypesValue.Value;
            set
            {
                _usedEncodingTypesValue.Value = value;
                _context.ConnectionDetails.SetUsedEncodingTypes(value);
            }
        }

        /// <summary>
        /// Gets or sets the current framebuffer size.
        /// </summary>
        public FrameSize FramebufferSize
        {
            get => _framebufferSizeValue.Value;
            set
            {
                _framebufferSizeValue.Value = value;
                _context.ConnectionDetails.SetFramebufferSize(value);
            }
        }

        /// <summary>
        /// Gets or sets the current framebuffer format.
        /// </summary>
        public PixelFormat FramebufferFormat
        {
            get => _framebufferFormatValue.Value;
            set
            {
                _framebufferFormatValue.Value = value;
                _context.ConnectionDetails.SetFramebufferFormat(value);
            }
        }

        /// <summary>
        /// Gets or sets the current desktop name.
        /// </summary>
        public string? DesktopName
        {
            get => _desktopNameValue.Value;
            set
            {
                _desktopNameValue.Value = value;
                _context.ConnectionDetails.SetDesktopName(value);
            }
        }

        /// <summary>
        /// Gets or sets whether continuous updates are currently enabled.
        /// </summary>
        public bool ContinuousUpdatesEnabled
        {
            get => _continuousUpdatesEnabledValue.Value;
            set => _continuousUpdatesEnabledValue.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolState"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public ProtocolState(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public void Prepare()
        {
            // Initialize UsedMessages with all standard messages that need to be supported by the server by definition
            UsedMessages = _context.SupportedMessages.Where(entry => entry.Value.IsStandardMessage).ToImmutableDictionary();

            // Initialize UsedEncodingTypes with all encoding types that don't require a confirmation by the server
            UsedEncodingTypes = _context.SupportedEncodingTypes.Where(entry => entry.Value.RequiresConfirmation).ToImmutableDictionary();
        }

        /// <summary>
        /// Marks a message as known to be supported by both sides.
        /// </summary>
        /// <param name="id">The message id.</param>
        public void MarkMessageAsUsed(byte id)
        {
            if (UsedMessages.ContainsKey(id))
                return;

            Debug.Assert(_context.SupportedMessages != null, "_context.SupportedMessages != null");
            if (!_context.SupportedMessages.ContainsKey(id))
                throw new ArgumentException($"Unknown message id: {id}", nameof(id));
            IMessage message = _context.SupportedMessages[id];

            UsedMessages = UsedMessages.Add(id, message);
        }

        /// <summary>
        /// Marks an encoding type as known to be supported by both sides.
        /// </summary>
        /// <param name="id">The encoding type id.</param>
        public void MarkEncodingTypeAsUsed(int id)
        {
            if (UsedEncodingTypes.ContainsKey(id))
                return;

            Debug.Assert(_context.SupportedEncodingTypes != null, "_context.SupportedEncodingTypes != null");
            if (!_context.SupportedEncodingTypes.ContainsKey(id))
                throw new ArgumentException($"Unknown encoding type id: {id}", nameof(id));
            IEncodingType encodingType = _context.SupportedEncodingTypes[id];

            UsedEncodingTypes = UsedEncodingTypes.Add(id, encodingType);
        }

        private class StateValue<T>
        {
            private readonly object _lockObject = new object();
            private T _value;

            public T Value
            {
                get
                {
                    lock (_lockObject)
                        return _value;
                }
                set
                {
                    lock (_lockObject)
                        _value = value;
                }
            }

            public StateValue(T initialValue)
            {
                _value = initialValue;
            }
        }
    }
}
