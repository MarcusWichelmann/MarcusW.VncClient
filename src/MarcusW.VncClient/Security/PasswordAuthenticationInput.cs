using System;

namespace MarcusW.VncClient.Security
{
    /// <summary>
    /// Contains the input data that was requested for a password authentication.
    /// </summary>
    public class PasswordAuthenticationInput : IAuthenticationInput<PasswordAuthenticationInputRequest>
    {
        /// <summary>
        /// Gets the requested password.
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="PasswordAuthenticationInput"/>.
        /// </summary>
        /// <param name="password">The requested password.</param>
        public PasswordAuthenticationInput(string password)
        {
            Password = password ?? throw new ArgumentNullException(nameof(password));
        }
    }
}
