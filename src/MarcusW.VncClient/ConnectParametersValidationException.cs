using System;
using JetBrains.Annotations;

namespace MarcusW.VncClient;

[PublicAPI]
public class ConnectParametersValidationException : Exception
{
    public ConnectParametersValidationException() { }

    public ConnectParametersValidationException(string? message) : base(message) { }

    public ConnectParametersValidationException(string? message, Exception? innerException) : base(message,
        innerException) { }
}
