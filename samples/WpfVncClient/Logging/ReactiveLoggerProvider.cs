using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace WpfVncClient.Logging;

public class ReactiveLoggerProvider(ReactiveLog reactiveLog) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, ReactiveLogger> _loggers = new();
    private readonly ReactiveLog _reactiveLog = reactiveLog;

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name => new(name, _reactiveLog));

    public void Dispose()
    {
        _loggers.Clear();
    }
}
