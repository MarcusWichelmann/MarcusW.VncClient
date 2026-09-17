using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Blazor.Adapters.Logging
{
    /// <summary>
    /// Provider for the <see cref="BlazorLoggerProvider"/> logging adapter.
    /// </summary>
    public class BlazorLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, BlazorLogger> _loggers =
            new ConcurrentDictionary<string, BlazorLogger>();

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            if (categoryName == null)
                throw new ArgumentNullException(nameof(categoryName));

            return _loggers.GetOrAdd(categoryName, loggerName => new BlazorLogger(categoryName));
        }

        /// <inheritdoc />
        public void Dispose() { }
    }
}
