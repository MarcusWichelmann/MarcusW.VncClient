using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.MessageTypes;
using MarcusW.VncClient.Protocol.SecurityTypes;

namespace MarcusW.VncClient.Protocol.Implementation
{
    /// <inhertitdoc />
    public class ProtocolState : IRfbProtocolState
    {
        private readonly RfbConnectionContext _context;

        private readonly StateValue<RfbProtocolVersion> _protocolVersionValue = new StateValue<RfbProtocolVersion>(RfbProtocolVersion.Unknown);

        private readonly StateValue<ISecurityType?> _usedSecurityTypeValue = new StateValue<ISecurityType?>(null);

        private readonly StateValue<IImmutableSet<IMessageType>> _usedMessageTypesValue = new StateValue<IImmutableSet<IMessageType>>(ImmutableHashSet<IMessageType>.Empty);

        private readonly StateValue<IImmutableSet<IEncodingType>> _usedEncodingTypesValue = new StateValue<IImmutableSet<IEncodingType>>(ImmutableHashSet<IEncodingType>.Empty);

        private readonly StateValue<Size> _remoteFramebufferSizeValue = new StateValue<Size>(Size.Zero);

        private readonly StateValue<PixelFormat> _remoteFramebufferFormatValue = new StateValue<PixelFormat>(PixelFormat.Unknown);

        private readonly StateValue<string?> _desktopNameValue = new StateValue<string?>(null);

        private readonly StateValue<bool> _serverSupportsFencesValue = new StateValue<bool>(false);
        private readonly StateValue<bool> _serverSupportsContinuousUpdatesValue = new StateValue<bool>(false);

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
        /// Gets or sets the message types that are known to be supported by both sides.
        /// </summary>
        public IImmutableSet<IMessageType> UsedMessageTypes
        {
            get => _usedMessageTypesValue.Value;
            set
            {
                _usedMessageTypesValue.Value = value;
                _context.ConnectionDetails.SetUsedMessageTypes(value);
            }
        }

        /// <summary>
        /// Gets or sets the encoding types that are known to be supported by both sides.
        /// </summary>
        public IImmutableSet<IEncodingType> UsedEncodingTypes
        {
            get => _usedEncodingTypesValue.Value;
            set
            {
                _usedEncodingTypesValue.Value = value;
                _context.ConnectionDetails.SetUsedEncodingTypes(value);
            }
        }

        /// <summary>
        /// Gets or sets the current remote framebuffer size.
        /// </summary>
        /// <remarks>
        /// After the initialization is done, this property should only be written by received messages/pseudo-encodings
        /// because its value might get locally cached to improve message processing performance.
        /// </remarks>
        public Size RemoteFramebufferSize
        {
            get => _remoteFramebufferSizeValue.Value;
            set
            {
                _remoteFramebufferSizeValue.Value = value;
                _context.ConnectionDetails.SetRemoteFramebufferSize(value);
            }
        }

        /// <summary>
        /// Gets or sets the current remote framebuffer format.
        /// </summary>
        /// <remarks>
        /// After the initialization is done, this property should only be written by received messages/pseudo-encodings
        /// because its value might get locally cached to improve message processing performance.
        /// </remarks>
        public PixelFormat RemoteFramebufferFormat
        {
            get => _remoteFramebufferFormatValue.Value;
            set
            {
                _remoteFramebufferFormatValue.Value = value;
                _context.ConnectionDetails.SetRemoteFramebufferFormat(value);
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
        /// Gets or sets whether the server supports fences.
        /// </summary>
        public bool ServerSupportsFences
        {
            get => _serverSupportsFencesValue.Value;
            set => _serverSupportsFencesValue.Value = value;
        }

        /// <summary>
        /// Gets or sets whether the server supports continuous updates.
        /// </summary>
        public bool ServerSupportsContinuousUpdates
        {
            get => _serverSupportsContinuousUpdatesValue.Value;
            set => _serverSupportsContinuousUpdatesValue.Value = value;
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
        public virtual void Prepare()
        {
            // Initialize UsedMessageTypes with all standard messages that need to be supported by the server by definition
            UsedMessageTypes = _context.SupportedMessageTypes.Where(mt => mt.IsStandardMessageType).ToImmutableHashSet();

            // Initialize UsedEncodingTypes with all encoding types that don't get confirmed by the server
            UsedEncodingTypes = _context.SupportedEncodingTypes.Where(et => !et.GetsConfirmed).ToImmutableHashSet();
        }

        /// <summary>
        /// Marks the given message type as being supported by both sides, if that not already happened.
        /// </summary>
        /// <param name="messageType">The message type instance, if already at hand.</param>
        /// <typeparam name="TMessageType">The type of the message type.</typeparam>
        public void MarkMessageTypeAsUsed<TMessageType>(TMessageType? messageType = null) where TMessageType : class, IMessageType
        {
            // Lookup instance if not already available
            messageType ??= _context.FindMessageType<TMessageType>();
            if (messageType == null)
                throw new InvalidOperationException($"Could not find {typeof(TMessageType).Name} in supported message types collection.");

            // Mark as used
            MarkMessageTypeAsUsedInternal(messageType);
        }

        /// <summary>
        /// Marks the given message type as being supported by both sides, if that not already happened.
        /// </summary>
        /// <param name="id">The id of the message type.</param>
        public void MarkMessageTypeAsUsed(byte id)
        {
            // Lookup instance
            IMessageType messageType = _context.FindMessageType(id);

            // Mark as used
            MarkMessageTypeAsUsedInternal(messageType);
        }

        /// <summary>
        /// Marks the given encoding type as being supported by both sides, if that not already happened.
        /// </summary>
        /// <param name="encodingType">The encoding type instance, if already at hand.</param>
        /// <typeparam name="TEncodingType">The type of the encoding type.</typeparam>
        public void MarkEncodingTypeAsUsed<TEncodingType>(TEncodingType? encodingType = null) where TEncodingType : class, IEncodingType
        {
            // Lookup instance if not already available
            encodingType ??= _context.FindEncodingType<TEncodingType>();
            if (encodingType == null)
                throw new InvalidOperationException($"Could not find {typeof(TEncodingType).Name} in supported encoding types collection.");

            // Mark as used
            MarkEncodingTypeAsUsedInternal(encodingType);
        }

        /// <summary>
        /// Marks the given encoding type as being supported by both sides, if that not already happened.
        /// </summary>
        /// <param name="id">The id of the encoding type.</param>
        public void MarkEncodingTypeAsUsed(int id)
        {
            // Lookup instance
            IEncodingType encodingType = _context.FindEncodingType(id);

            // Mark as used
            MarkEncodingTypeAsUsedInternal(encodingType);
        }

        private void MarkMessageTypeAsUsedInternal(IMessageType messageType)
        {
            IImmutableSet<IMessageType> usedMessageTypes = UsedMessageTypes;

            // Update used message types only if it changes to avoid unnecessary change events.
            if (!usedMessageTypes.Contains(messageType))
                UsedMessageTypes = usedMessageTypes.Add(messageType);
        }

        private void MarkEncodingTypeAsUsedInternal(IEncodingType encodingType)
        {
            IImmutableSet<IEncodingType> usedEncodingTypes = UsedEncodingTypes;

            // Update used encoding types only if it changes to avoid unnecessary change events.
            if (!usedEncodingTypes.Contains(encodingType))
                UsedEncodingTypes = usedEncodingTypes.Add(encodingType);
        }

        protected class StateValue<T>
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
