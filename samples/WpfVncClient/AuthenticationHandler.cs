using System;
using System.Threading.Tasks;
using MarcusW.VncClient;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Security;

namespace WpfVncClient.Services;

public class AuthenticationHandler : IAuthenticationHandler
{
    public string? Password { get; set; }

    /// <inhertitdoc />
    public async Task<TInput> ProvideAuthenticationInputAsync<TInput>(RfbConnection connection,
        ISecurityType securityType, IAuthenticationInputRequest<TInput> request)
        where TInput : class, IAuthenticationInput
    {
        if (typeof(TInput) == typeof(PasswordAuthenticationInput))
        {
            string? password = Password ?? string.Empty;

            // TODO: Implement canceling of authentication input requests instead of passing an empty password!

            return (TInput)Convert.ChangeType(new PasswordAuthenticationInput(password), typeof(TInput));
        }

        throw new InvalidOperationException(
            "The authentication input request is not supported by the interactive authentication handler.");
    }
}
