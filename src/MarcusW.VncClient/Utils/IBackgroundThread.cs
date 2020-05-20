using System;
using MarcusW.VncClient.Protocol;

namespace MarcusW.VncClient.Utils
{
    /// <summary>
    /// Describes a background thread.
    /// </summary>
    public interface IBackgroundThread : IDisposable
    {
        /// <summary>
        /// Occurs when the background thread fails.
        /// </summary>
        event EventHandler<BackgroundThreadFailedEventArgs>? Failed;
    }
}
