using System;

namespace MarcusW.VncClient.Protocol
{
    public class UnexpectedDataException : RfbProtocolException
    {
        public UnexpectedDataException() { }

        public UnexpectedDataException(string? message) : base(message) { }

        public UnexpectedDataException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
