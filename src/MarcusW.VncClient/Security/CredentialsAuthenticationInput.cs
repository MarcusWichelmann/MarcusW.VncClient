using System;

namespace MarcusW.VncClient.Security
{
    /// <summary>
    /// Contains the input data that was requested for a password authentication.
    /// </summary>
    public class CredentialsAuthenticationInput : IAuthenticationInput
    {
        /// <summary>
        /// Gets the requested username.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Gets the requested password.
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="CredentialsAuthenticationInput"/>.
        /// </summary>
        /// <param name="password">The requested password.</param>
        public CredentialsAuthenticationInput(string username, string password)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Password = password ?? throw new ArgumentNullException(nameof(password));
        }
    }
}
