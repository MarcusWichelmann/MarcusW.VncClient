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
        Stream Stream { get; }
    }
}
