using System;
using JetBrains.Annotations;

namespace MarcusW.VncClient.Protocol;

[PublicAPI]
public class HandshakeFailedException : RfbProtocolException
{
    public HandshakeFailedException() { }

    public HandshakeFailedException(string? message) : base(message) { }

    public HandshakeFailedException(string? message, Exception? innerException) : base(message, innerException) { }
}
