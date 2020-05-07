using System;

namespace MarcusW.VncClient
{
    public class ConnectParametersValidationException : Exception
    {
        public ConnectParametersValidationException() { }

        public ConnectParametersValidationException(string? message) : base(message) { }

        public ConnectParametersValidationException(string? message, Exception? innerException) : base(message,
            innerException) { }
    }
}
