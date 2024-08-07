namespace MarcusW.VncClient.Security;

/// <summary>
///     Contains the input data that was requested for a password authentication.
/// </summary>
/// <remarks>
///     Initializes a new instance of <see cref="PasswordAuthenticationInput" />.
/// </remarks>
/// <param name="password">The requested password.</param>
public class PasswordAuthenticationInput(string password) : IAuthenticationInput
{
    /// <summary>
    ///     Gets the requested password.
    /// </summary>
    public string Password { get; } = password;
}
