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
    }
}
