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
        /// Gets or sets the encoding types that are either known to be supported by both sides, or at least safe to use anyway.
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
        /// After the initialization is done, this property should only be written by received messages/encoding-types
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
        /// After the initialization is done, this property should only be written by received messages/encoding-types
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
