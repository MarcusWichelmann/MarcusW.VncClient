namespace MarcusW.VncClient
{
    /// <summary>
    /// The different states of an active RFB connection.
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        /// Connection has not started yet.
        /// </summary>
        Uninitialized,

        /// <summary>
        /// Connection is established.
        /// </summary>
        Connected,

        /// <summary>
        /// Connection was interrupted. Waiting for reconnect attempt after a short pause.
        /// </summary>
        Interrupted,

        /// <summary>
        /// Reconnect attempt is running.
        /// </summary>
        Reconnecting,

        /// <summary>
        /// Reconnect attempt failed. Waiting for next reconnect attempt after a short pause.
        /// </summary>
        ReconnectFailed,

        /// <summary>
        /// Connection was closed or gave up reconnecting.
        /// </summary>
        Closed
    }
}
