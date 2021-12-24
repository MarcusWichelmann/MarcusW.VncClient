using System;
using Microsoft.Extensions.Logging;

namespace WpfVncClient.Logging;

public class ReactiveLogger : ILogger
{
    private readonly string _name;
    private readonly ReactiveLog _reactiveLog;

    public ReactiveLogger(string name, ReactiveLog reactiveLog)
    {
        _name = name;
        _reactiveLog = reactiveLog;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string msg = $"[{eventId.Id,2}: {logLevel,-12}, {timestamp}]: {_name} - {formatter(state, exception)}";

        _reactiveLog.Log(msg);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
}
