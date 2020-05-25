using System;

namespace MarcusW.VncClient.Protocol
{
    public class RfbProtocolUnexpectedDataException : Exception
    {
        public RfbProtocolUnexpectedDataException() { }

        public RfbProtocolUnexpectedDataException(string? message) : base(message) { }

        public RfbProtocolUnexpectedDataException(string? message, Exception? innerException) : base(message,
            innerException) { }
    }
}
