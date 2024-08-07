using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Avalonia.Adapters.Logging;

/// <summary>
///     Provider for the <see cref="AvaloniaLogger" /> logging adapter.
/// </summary>
public class AvaloniaLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, AvaloniaLogger> _loggers = new();

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, _ => new(categoryName));
    }

    /// <inheritdoc />
    public void Dispose() { }
}
