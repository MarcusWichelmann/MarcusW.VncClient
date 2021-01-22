using System;
using System.ComponentModel;
using Avalonia.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Avalonia.Adapters.Logging
{
    /// <summary>
    /// Logging implementation that forwards any log output to Avalonias own logging sinks.
    /// </summary>
    public class AvaloniaLogger : ILogger
    {
        private const string AreaName = "VncClient";

        private readonly string _categoryName;

        internal AvaloniaLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, [NotNull] Func<TState, Exception?, string> formatter)
        {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            LogEventLevel? logEventLevel = GetLogEventLevel(logLevel);
            if (logEventLevel == null)
                return;

            if (!Logger.TryGet(logEventLevel.Value, AreaName, out ParametrizedLogger outLogger))
                return;

            string message = $"{_categoryName}: {formatter(state, exception)}";

            if (exception != null)
                message += Environment.NewLine + exception + Environment.NewLine;

            outLogger.Log(this, message);
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            LogEventLevel? logEventLevel = GetLogEventLevel(logLevel);
            return logEventLevel != null && Logger.IsEnabled(logEventLevel.Value, AreaName);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Please note that scopes are not supported by this logger.
        /// </remarks>
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        private LogEventLevel? GetLogEventLevel(LogLevel logLevel)
            => logLevel switch {
                LogLevel.Trace       => LogEventLevel.Verbose,
                LogLevel.Debug       => LogEventLevel.Debug,
                LogLevel.Information => LogEventLevel.Information,
                LogLevel.Warning     => LogEventLevel.Warning,
                LogLevel.Error       => LogEventLevel.Error,
                LogLevel.Critical    => LogEventLevel.Fatal,
                LogLevel.None        => null,
                _                    => throw new InvalidEnumArgumentException(nameof(logLevel), (int)logLevel, typeof(LogLevel))
            };

        /// <summary>
        /// Represents an empty logging scope without any logic.
        /// </summary>
        public class NullScope : IDisposable
        {
            /// <summary>
            /// Gets the default instance of the <see cref="NullScope"/>.
            /// </summary>
            public static NullScope Instance { get; } = new NullScope();

            private NullScope() { }

            /// <inheritdoc />
            public void Dispose() { }
        }
    }
}
