namespace MarcusW.VncClient.Output
{
    /// <summary>
    /// Provides methods for handling output events from the server.
    /// </summary>
    public interface IOutputHandler
    {
        /// <summary>
        /// Handles when the server rings the bell.
        /// </summary>
        void RingBell();

        /// <summary>
        /// Handles when the clipboard content of the server changed.
        /// </summary>
        /// <param name="text">The text in the clipboard buffer.</param>
        void HandleServerClipboardUpdate(string text);

        /// <summary>
        /// Handles when the desktop name of the server changed.
        /// </summary>
        /// <param name="name">The new desktop name.</param>
        void HandleDesktopNameChange(string name) { }

        /// <summary>
        /// Handles when an xvp operation (shutdown/reboot/reset) failed on the server.
        /// </summary>
        void HandleXvpOperationFailed() { }

        /// <summary>
        /// Handles when the server changes the pointer mode between absolute and relative.
        /// </summary>
        /// <param name="relativeMode">True if relative mode is active, false for absolute mode.</param>
        void HandlePointerModeChange(bool relativeMode) { }

        /// <summary>
        /// Handles when the server updates the keyboard LED state.
        /// </summary>
        /// <param name="ledState">The LED state flags.</param>
        void HandleLedStateChange(Protocol.Implementation.EncodingTypes.Pseudo.LedState ledState) { }

        /// <summary>
        /// Handles when the server notifies about available clipboard formats.
        /// </summary>
        /// <param name="availableFormats">The available clipboard formats on the server.</param>
        void HandleExtendedClipboardNotify(Protocol.Implementation.ExtendedClipboardFormat availableFormats) { }

        /// <summary>
        /// Handles when the server provides extended clipboard data.
        /// </summary>
        /// <param name="data">The clipboard data.</param>
        void HandleExtendedClipboardData(Protocol.Implementation.ExtendedClipboardData data) { }

        /// <summary>
        /// Handles when the server requests clipboard data from the client.
        /// </summary>
        /// <param name="requestedFormats">The formats requested by the server.</param>
        void HandleExtendedClipboardRequest(Protocol.Implementation.ExtendedClipboardFormat requestedFormats) { }
    }
}
