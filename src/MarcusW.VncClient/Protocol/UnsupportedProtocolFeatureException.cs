using System;
using JetBrains.Annotations;

namespace MarcusW.VncClient.Protocol;

[PublicAPI]
public class UnsupportedProtocolFeatureException : RfbProtocolException
{
    public UnsupportedProtocolFeatureException() { }

    public UnsupportedProtocolFeatureException(string? message) : base(message) { }

    public UnsupportedProtocolFeatureException(string? message, Exception? innerException) : base(message,
        innerException) { }
}
