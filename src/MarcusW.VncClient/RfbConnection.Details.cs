using System;
using System.Collections.Immutable;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.MessageTypes;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient
{
    public partial class RfbConnection
    {
        // The properties in this class serve only the purpose to inform the API consumer about some connection details.
        // Classes that are part of the protocol implementation should have their own state somewhere else and not have
        // to use below properties to avoid unnecessary locking.
        //
        // Reference types use volatile for lock-free atomic read/write.
        // Value types (structs, primitives) that are larger than pointer size use per-property locks
        // to prevent tearing during concurrent reads/writes.

        private readonly object _protocolVersionLock = new object();
        private RfbProtocolVersion _protocolVersion = RfbProtocolVersion.Unknown;

        // Reference types: volatile is sufficient for atomic read/write
        private volatile ISecurityType? _usedSecurityType;
        private volatile IImmutableSet<IMessageType> _usedMessageTypes = ImmutableHashSet<IMessageType>.Empty;
        private volatile IImmutableSet<IEncodingType> _usedEncodingTypes = ImmutableHashSet<IEncodingType>.Empty;
        private volatile IImmutableSet<Screen> _remoteFramebufferLayout = ImmutableHashSet<Screen>.Empty;
        private volatile string? _desktopName;

        // Value types: require locks for atomic read/write (structs may be larger than pointer size)
        private readonly object _remoteFramebufferSizeLock = new object();
        private Size _remoteFramebufferSize = Size.Zero;

        private readonly object _remoteFramebufferFormatLock = new object();
        private PixelFormat _remoteFramebufferFormat = PixelFormat.Unknown;

        // bool is atomically accessible, but we use volatile for memory ordering
        private volatile bool _desktopIsResizable;
        private volatile bool _continuousUpdatesEnabled;

        /// <summary>
        /// Gets the version of the protocol used for remote communication.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public RfbProtocolVersion ProtocolVersion
        {
            get => GetWithLock(ref _protocolVersion, _protocolVersionLock);
            internal set => RaiseAndSetIfChangedWithLock(ref _protocolVersion, value, _protocolVersionLock);
        }

        /// <summary>
        /// Gets the security type that was used for authenticating and securing the connection.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public ISecurityType? UsedSecurityType
        {
            get => _usedSecurityType;
            internal set
            {
                if (_disposed) throw new ObjectDisposedException(nameof(RfbConnection));
                if (ReferenceEquals(_usedSecurityType, value)) return;
                _usedSecurityType = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the message types that are currently used by this connection.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public IImmutableSet<IMessageType> UsedMessageTypes
        {
            get => _usedMessageTypes;
            internal set
            {
                if (_disposed) throw new ObjectDisposedException(nameof(RfbConnection));
                if (ReferenceEquals(_usedMessageTypes, value)) return;
                _usedMessageTypes = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the encoding types that are currently used by this connection.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public IImmutableSet<IEncodingType> UsedEncodingTypes
        {
            get => _usedEncodingTypes;
            internal set
            {
                if (_disposed) throw new ObjectDisposedException(nameof(RfbConnection));
                if (ReferenceEquals(_usedEncodingTypes, value)) return;
                _usedEncodingTypes = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the current size of the remote view.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public Size RemoteFramebufferSize
        {
            get => GetWithLock(ref _remoteFramebufferSize, _remoteFramebufferSizeLock);
            internal set => RaiseAndSetIfChangedWithLock(ref _remoteFramebufferSize, value, _remoteFramebufferSizeLock);
        }

        /// <summary>
        /// Gets the current format of the remote view.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public PixelFormat RemoteFramebufferFormat
        {
            get => GetWithLock(ref _remoteFramebufferFormat, _remoteFramebufferFormatLock);
            internal set => RaiseAndSetIfChangedWithLock(ref _remoteFramebufferFormat, value, _remoteFramebufferFormatLock);
        }

        /// <summary>
        /// Gets the current layout of the remote view.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public IImmutableSet<Screen> RemoteFramebufferLayout
        {
            get => _remoteFramebufferLayout;
            internal set
            {
                if (_disposed) throw new ObjectDisposedException(nameof(RfbConnection));
                if (ReferenceEquals(_remoteFramebufferLayout, value)) return;
                _remoteFramebufferLayout = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the current name of the remote desktop.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public string? DesktopName
        {
            get => _desktopName;
            internal set
            {
                if (_disposed) throw new ObjectDisposedException(nameof(RfbConnection));
                if (string.Equals(_desktopName, value, StringComparison.Ordinal)) return;
                _desktopName = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets whether the connection allows client-side desktop size changes.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public bool DesktopIsResizable
        {
            get => _desktopIsResizable;
            internal set
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(RfbConnection));
                if (_desktopIsResizable == value)
                    return;
                _desktopIsResizable = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the current state of the continuous update protocol feature.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public bool ContinuousUpdatesEnabled
        {
            get => _continuousUpdatesEnabled;
            internal set
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(RfbConnection));
                if (_continuousUpdatesEnabled == value)
                    return;
                _continuousUpdatesEnabled = value;
                NotifyPropertyChanged();
            }
        }
    }
}
