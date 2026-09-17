using System;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Provides data for the <see cref="RfbConnection.ConnectionStateChanged"/> event.
    /// </summary>
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the previous connection state.
        /// </summary>
        public ConnectionState PreviousState { get; }

        /// <summary>
        /// Gets the current connection state.
        /// </summary>
        public ConnectionState CurrentState { get; }

        /// <summary>
        /// Gets the reason for the state change, if available.
        /// </summary>
        public string? Reason { get; }

        /// <summary>
        /// Gets the exception that caused the state change, if any.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets the current reconnect attempt count.
        /// This is 0 for the initial connection and increments with each reconnection attempt.
        /// </summary>
        public int ReconnectAttempt { get; }

        /// <summary>
        /// Gets whether this state change was triggered by a manual action
        /// (such as calling <see cref="RfbConnection.CloseAsync"/> or <see cref="RfbConnection.ForceReconnectAsync"/>).
        /// </summary>
        public bool IsManualAction { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousState">The previous connection state.</param>
        /// <param name="currentState">The current connection state.</param>
        /// <param name="reason">The reason for the state change.</param>
        /// <param name="exception">The exception that caused the state change, if any.</param>
        /// <param name="reconnectAttempt">The reconnect attempt count.</param>
        /// <param name="isManualAction">Whether this state change was triggered by a manual action.</param>
        public ConnectionStateChangedEventArgs(
            ConnectionState previousState,
            ConnectionState currentState,
            string? reason = null,
            Exception? exception = null,
            int reconnectAttempt = 0,
            bool isManualAction = false)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            Reason = reason;
            Exception = exception;
            ReconnectAttempt = reconnectAttempt;
            IsManualAction = isManualAction;
        }
    }
}
