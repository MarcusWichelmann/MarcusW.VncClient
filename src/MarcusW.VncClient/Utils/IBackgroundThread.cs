using System;
using MarcusW.VncClient.Protocol;

namespace MarcusW.VncClient.Utils
{
    /// <summary>
    /// Describes a background thread.
    /// </summary>
    internal interface IBackgroundThread : IDisposable
    {
        event EventHandler<BackgroundThreadFailedEventArgs>? Failed;
    }
}
