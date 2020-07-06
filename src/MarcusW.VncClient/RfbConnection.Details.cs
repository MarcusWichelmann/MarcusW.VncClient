using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Rendering;

namespace MarcusW.VncClient
{
    public partial class RfbConnection
    {
        private readonly object _framebufferSizeLock = new object();
        private FrameSize _framebufferSize = FrameSize.Zero;

        private readonly object _framebufferFormatLock = new object();
        private PixelFormat _framebufferFormat = PixelFormat.Unknown;

        private readonly object _desktopNameLock = new object();
        private string _desktopName = "Unknown";

        private readonly object _protocolVersionLock = new object();
        private RfbProtocolVersion _protocolVersion = RfbProtocolVersion.Unknown;

        /// <summary>
        /// Gets the current size of the remote view.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public FrameSize FramebufferSize
        {
            get => GetWithLock(ref _framebufferSize, _framebufferSizeLock);
            internal set => RaiseAndSetIfChangedWithLockAndDisposedCheck(ref _framebufferSize, value, _framebufferSizeLock);
        }

        /// <summary>
        /// Gets the current format of the remote view.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public PixelFormat FramebufferFormat
        {
            get => GetWithLock(ref _framebufferFormat, _framebufferFormatLock);
            internal set => RaiseAndSetIfChangedWithLockAndDisposedCheck(ref _framebufferFormat, value, _framebufferFormatLock);
        }

        /// <summary>
        /// Gets the current name of the remote desktop.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public string DesktopName
        {
            get => GetWithLock(ref _desktopName, _desktopNameLock);
            internal set => RaiseAndSetIfChangedWithLockAndDisposedCheck(ref _desktopName, value, _desktopNameLock);
        }

        /// <summary>
        /// Gets the version of the protocol used for remote communication.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public RfbProtocolVersion ProtocolVersion
        {
            get => GetWithLock(ref _protocolVersion, _protocolVersionLock);
            internal set => RaiseAndSetIfChangedWithLockAndDisposedCheck(ref _protocolVersion, value, _protocolVersionLock);
        }
    }
}
