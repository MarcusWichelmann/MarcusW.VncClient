using System;
using System.Collections.Concurrent;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Avalonia.Adapters.Logging
{
    /// <summary>
    /// Provider for the <see cref="AvaloniaLogger"/> logging adapter.
    /// </summary>
    public class AvaloniaLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, AvaloniaLogger> _loggers =
            new ConcurrentDictionary<string, AvaloniaLogger>();

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            if (categoryName == null)
                throw new ArgumentNullException(nameof(categoryName));

            return _loggers.GetOrAdd(categoryName, loggerName => new AvaloniaLogger(categoryName));
        }

        public void Dispose() { }
    }
}
