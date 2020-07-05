using MarcusW.VncClient.Protocol;

namespace MarcusW.VncClient
{
    public partial class RfbConnection
    {
        private readonly object _framebufferSizeLock = new object();
        private FrameSize _framebufferSize = FrameSize.Zero;

        private readonly object _desktopNameLock = new object();
        private string _desktopName = "Unknown";

        private readonly object _protocolVersionLock = new object();
        private RfbProtocolVersion _protocolVersion = RfbProtocolVersion.Unknown;

        public FrameSize FramebufferSize
        {
            get => GetWithLock(ref _framebufferSize, _framebufferSizeLock);
            private set => RaiseAndSetIfChangedWithLockAndDisposedCheck(ref _framebufferSize, value, _framebufferSizeLock);
        }

        public string DesktopName
        {
            get => GetWithLock(ref _desktopName, _desktopNameLock);
            private set => RaiseAndSetIfChangedWithLockAndDisposedCheck(ref _desktopName, value, _desktopNameLock);
        }

        public RfbProtocolVersion ProtocolVersion
        {
            get => GetWithLock(ref _protocolVersion, _protocolVersionLock);
            private set => RaiseAndSetIfChangedWithLockAndDisposedCheck(ref _protocolVersion, value, _protocolVersionLock);
        }
    }
}
