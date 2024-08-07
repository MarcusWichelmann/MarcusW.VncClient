using System;
using JetBrains.Annotations;

namespace MarcusW.VncClient.Protocol;

[PublicAPI]
public class UnexpectedEndOfStreamException : RfbProtocolException
{
    public UnexpectedEndOfStreamException() { }

    public UnexpectedEndOfStreamException(string? message) : base(message) { }

    public UnexpectedEndOfStreamException(string? message, Exception? innerException) : base(message,
        innerException) { }
}
