using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.MessageTypes;
using MarcusW.VncClient.Protocol.SecurityTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation
{
    /// <inhertitdoc />
    public class ProtocolState : IRfbProtocolState
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<ProtocolState> _logger;

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
            _logger = context.Connection.LoggerFactory.CreateLogger<ProtocolState>();
        }

        /// <inheritdoc />
        public virtual void Prepare()
        {
            _logger.LogDebug("Initializing sets for used message and encoding types...");

            // Initialize UsedMessageTypes with all standard messages that need to be supported by the server by definition
            Debug.Assert(_context.SupportedMessageTypes != null, "_context.SupportedMessageTypes != null");
            UsedMessageTypes = _context.SupportedMessageTypes.Where(mt => mt.IsStandardMessageType).ToImmutableHashSet();

            // Initialize UsedEncodingTypes with all encoding types that don't get confirmed by the server
            Debug.Assert(_context.SupportedEncodingTypes != null, "_context.SupportedEncodingTypes != null");
            UsedEncodingTypes = _context.SupportedEncodingTypes.Where(et => !et.GetsConfirmed).ToImmutableHashSet();
        }

        /// <summary>
        /// Marks the given message type as being supported by both sides, if that not already happened.
        /// </summary>
        /// <param name="messageType">The message type instance, if already at hand.</param>
        /// <param name="idFilter">An optional filter in case <typeparamref name="TMessageType"/> is not specific enough.</param>
        /// <typeparam name="TMessageType">The type of the message type or a base type like <see cref="IIncomingMessageType"/>.</typeparam>
        public void EnsureMessageTypeIsMarkedAsUsed<TMessageType>(TMessageType? messageType = null, byte? idFilter = null) where TMessageType : class, IMessageType
        {
            // Lookup instance if not already available
            if (messageType == null)
            {
                Debug.Assert(_context.SupportedMessageTypes != null, "_context.SupportedMessageTypes != null");

                IEnumerable<TMessageType> filtered = _context.SupportedMessageTypes.OfType<TMessageType>();
                if (idFilter != null)
                    filtered = filtered.Where(mt => mt.Id == idFilter);

                messageType = filtered.FirstOrDefault();
                if (messageType == null)
                    throw new InvalidOperationException($"Could not find {typeof(TMessageType).Name} (ID filter: {idFilter}) in supported message types collection.");
            }
            else
            {
                // If an instance was already at hand, check that it has the right id.
                if (idFilter != null && messageType.Id != idFilter)
                    throw new ArgumentException("The given message type does not match the id filter.", nameof(messageType));
            }

            // Mark as used
            IImmutableSet<IMessageType> usedMessageTypes = UsedMessageTypes;
            if (!usedMessageTypes.Contains(messageType))
            {
                _logger.LogDebug("Marking message type {messageType} as used...", messageType.Name);
                UsedMessageTypes = usedMessageTypes.Add(messageType);
            }
        }

        /// <summary>
        /// Marks the given encoding type as being supported by both sides, if that not already happened.
        /// </summary>
        /// <param name="encodingType">The encoding type instance, if already at hand.</param>
        /// <param name="idFilter">An optional filter in case <typeparamref name="TEncodingType"/> is not specific enough.</param>
        /// <typeparam name="TEncodingType">The type of the encoding type or a base type like <see cref="IFrameEncodingType"/>.</typeparam>
        public void EnsureEncodingTypeIsMarkedAsUsed<TEncodingType>(TEncodingType? encodingType = null, int? idFilter = null) where TEncodingType : class, IEncodingType
        {
            // Lookup instance if not already available
            if (encodingType == null)
            {
                Debug.Assert(_context.SupportedEncodingTypes != null, "_context.SupportedEncodingTypes != null");

                IEnumerable<TEncodingType> filtered = _context.SupportedEncodingTypes.OfType<TEncodingType>();
                if (idFilter != null)
                    filtered = filtered.Where(et => et.Id == idFilter);

                encodingType = filtered.FirstOrDefault();
                if (encodingType == null)
                    throw new InvalidOperationException($"Could not find {typeof(TEncodingType).Name} (ID filter: {idFilter}) in supported encoding types collection.");
            }
            else
            {
                // If an instance was already at hand, check that it has the right id.
                if (idFilter != null && encodingType.Id != idFilter)
                    throw new ArgumentException("The given encoding type does not match the id filter.", nameof(encodingType));
            }

            // Mark as used
            IImmutableSet<IEncodingType> usedEncodingTypes = UsedEncodingTypes;
            if (!usedEncodingTypes.Contains(encodingType))
            {
                _logger.LogDebug("Marking encoding type {encodingType} as used...", encodingType.Name);
                UsedEncodingTypes = usedEncodingTypes.Add(encodingType);
            }
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
