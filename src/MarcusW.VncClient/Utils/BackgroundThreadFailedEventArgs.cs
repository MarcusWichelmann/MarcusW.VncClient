using System;

namespace MarcusW.VncClient.Utils;

/// <summary>
///     Provides data for an event that is raised when a background thread failes.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="BackgroundThreadFailedEventArgs" />.
/// </remarks>
/// <param name="exception">The exception that describes the failure.</param>
public class BackgroundThreadFailedEventArgs(Exception exception) : EventArgs
{
    /// <summary>
    ///     Gets the exception that describes the failure.
    /// </summary>
    public Exception Exception { get; } = exception;
}
