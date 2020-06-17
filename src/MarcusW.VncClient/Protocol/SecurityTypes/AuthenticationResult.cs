namespace MarcusW.VncClient.Protocol.SecurityTypes
{
    /// <summary>
    /// Provides information about the outcome of a security type authentication attempt.
    /// </summary>
    public class AuthenticationResult
    {
        /// <summary>
        /// Gets the established tunnel transport that will replace the base transport from now on.
        /// This is <see langword="null"/> when no tunnel was built.
        /// </summary>
        public ITransport? TunnelTransport { get; }

        /// <summary>
        /// Gets whether the server will send a SecurityResult message after authentication.
        /// </summary>
        public bool ExpectSecurityResult { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationResult"/>.
        /// </summary>
        /// <param name="tunnelTransport">The tunnel transport to replace the base transport with or <see langword="null"/> when no tunnel was built.</param>
        /// <param name="expectSecurityResult">True if the server will send a SecurityResult message after authentication, False otherwise.</param>
        public AuthenticationResult(ITransport? tunnelTransport = null, bool expectSecurityResult = true)
        {
            TunnelTransport = tunnelTransport;
            ExpectSecurityResult = expectSecurityResult;
        }
    }
}
