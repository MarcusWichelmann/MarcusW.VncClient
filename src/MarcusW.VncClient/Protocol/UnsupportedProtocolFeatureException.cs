using System;

namespace MarcusW.VncClient.Protocol
{
    public class UnsupportedProtocolFeatureException : RfbProtocolException
    {
        public UnsupportedProtocolFeatureException() { }

        public UnsupportedProtocolFeatureException(string? message) : base(message) { }

        public UnsupportedProtocolFeatureException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
