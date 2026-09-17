using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Blazor.Adapters.Logging
{
    /// <summary>
    /// Logging implementation that forwards any log output to Avalonias own logging sinks.
    /// </summary>
    public class BlazorLogger : ILogger
    {
        private const string AreaName = "VncClient";

        private readonly string _categoryName;

        internal BlazorLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, [NotNull] Func<TState, Exception?, string> formatter)
        {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            string message = $"{_categoryName}: {formatter(state, exception)}";

            if (exception != null)
                message += Environment.NewLine + exception + Environment.NewLine;

            Console.WriteLine(message);
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc />
        /// <remarks>
        /// Please note that scopes are not supported by this logger.
        /// </remarks>
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

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
