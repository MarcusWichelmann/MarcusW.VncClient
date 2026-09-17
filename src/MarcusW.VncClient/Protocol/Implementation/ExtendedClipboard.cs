using System;

namespace MarcusW.VncClient.Protocol.Implementation
{
    /// <summary>
    /// Defines the clipboard format flags for the Extended Clipboard extension.
    /// </summary>
    [Flags]
    public enum ExtendedClipboardFormat : uint
    {
        /// <summary>
        /// No formats.
        /// </summary>
        None = 0,

        /// <summary>
        /// Plain UTF-8 text with CRLF line endings.
        /// </summary>
        Text = 1 << 0,

        /// <summary>
        /// Microsoft Rich Text Format.
        /// </summary>
        Rtf = 1 << 1,

        /// <summary>
        /// Microsoft HTML clipboard fragments.
        /// </summary>
        Html = 1 << 2,

        /// <summary>
        /// Microsoft Device Independent Bitmap v5.
        /// </summary>
        Dib = 1 << 3,

        /// <summary>
        /// Files (reserved, not yet defined).
        /// </summary>
        Files = 1 << 4
    }

    /// <summary>
    /// Defines the clipboard action flags for the Extended Clipboard extension.
    /// </summary>
    [Flags]
    public enum ExtendedClipboardAction : uint
    {
        /// <summary>
        /// No action.
        /// </summary>
        None = 0,

        /// <summary>
        /// Capabilities message - indicates which formats and actions are supported.
        /// </summary>
        Caps = 1 << 24,

        /// <summary>
        /// Request clipboard data for the specified formats.
        /// </summary>
        Request = 1 << 25,

        /// <summary>
        /// Request a new notify message indicating available formats.
        /// </summary>
        Peek = 1 << 26,

        /// <summary>
        /// Notify which formats are available on the remote side.
        /// </summary>
        Notify = 1 << 27,

        /// <summary>
        /// Provide clipboard data for the specified formats.
        /// </summary>
        Provide = 1 << 28
    }

    /// <summary>
    /// Represents extended clipboard capabilities.
    /// </summary>
    public class ExtendedClipboardCapabilities
    {
        /// <summary>
        /// Gets or sets the supported formats.
        /// </summary>
        public ExtendedClipboardFormat SupportedFormats { get; set; }

        /// <summary>
        /// Gets or sets the supported actions.
        /// </summary>
        public ExtendedClipboardAction SupportedActions { get; set; }

        /// <summary>
        /// Gets or sets the maximum size for text format (0 = unlimited).
        /// </summary>
        public uint MaxTextSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum size for RTF format (0 = unlimited).
        /// </summary>
        public uint MaxRtfSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum size for HTML format (0 = unlimited).
        /// </summary>
        public uint MaxHtmlSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum size for DIB format (0 = unlimited).
        /// </summary>
        public uint MaxDibSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum size for files format (0 = unlimited).
        /// </summary>
        public uint MaxFilesSize { get; set; }

        /// <summary>
        /// Default client capabilities.
        /// </summary>
        public static ExtendedClipboardCapabilities DefaultClient => new()
        {
            SupportedFormats = ExtendedClipboardFormat.Text | ExtendedClipboardFormat.Rtf | ExtendedClipboardFormat.Html,
            SupportedActions = ExtendedClipboardAction.Caps | ExtendedClipboardAction.Request |
                               ExtendedClipboardAction.Peek | ExtendedClipboardAction.Notify | ExtendedClipboardAction.Provide,
            MaxTextSize = 0, // Force notify mode
            MaxRtfSize = 0,
            MaxHtmlSize = 0,
            MaxDibSize = 0,
            MaxFilesSize = 0
        };
    }

    /// <summary>
    /// Represents extended clipboard data for multiple formats.
    /// </summary>
    public class ExtendedClipboardData
    {
        /// <summary>
        /// Gets or sets the available formats.
        /// </summary>
        public ExtendedClipboardFormat AvailableFormats { get; set; }

        /// <summary>
        /// Gets or sets the plain text content (UTF-8 with null terminator).
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the RTF content.
        /// </summary>
        public byte[]? Rtf { get; set; }

        /// <summary>
        /// Gets or sets the HTML content.
        /// </summary>
        public string? Html { get; set; }

        /// <summary>
        /// Gets or sets the DIB content.
        /// </summary>
        public byte[]? Dib { get; set; }
    }
}
