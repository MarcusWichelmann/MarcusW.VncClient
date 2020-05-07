using System;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// Provides data for an event that is raised when a background thread failes.
    /// </summary>
    public class BackgroundThreadFailedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the exception that describes the failure.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundThreadFailedEventArgs"/>.
        /// </summary>
        /// <param name="exception">The exception that describes the failure.</param>
        public BackgroundThreadFailedEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }
}
