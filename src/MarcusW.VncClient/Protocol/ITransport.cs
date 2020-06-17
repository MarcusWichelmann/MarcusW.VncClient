using System;
using System.IO;

namespace MarcusW.VncClient.Protocol
{
    /// <summary>
    /// Represents an open transport through which bytes to/from the server can be sent and received.
    /// </summary>
    /// <remarks>
    /// You can build a <see cref="ITransport"/> which contains another transport to implement tunnel protocols.
    /// </remarks>
    public interface ITransport : IDisposable
    {
        /// <summary>
        /// Gets the stream for sending and receiving bytes.
        /// </summary>
        Stream Stream { get; }

        /// <summary>
        /// Gets whether the data on this transport gets encrypted.
        /// </summary>
        bool IsEncrypted { get; }
    }
}
