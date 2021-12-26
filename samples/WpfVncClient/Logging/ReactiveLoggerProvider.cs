using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace WpfVncClient.Logging;

public class ReactiveLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, ReactiveLogger> _loggers = new();
    private readonly ReactiveLog _reactiveLog;

    public ReactiveLoggerProvider(ReactiveLog reactiveLog)
    {
        _reactiveLog = reactiveLog;
    }

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name => new ReactiveLogger(name, _reactiveLog));

    public void Dispose()
    {
        _loggers.Clear();
    }
}
