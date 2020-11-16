using System;
using System.Threading.Tasks;
using MarcusW.VncClient;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Security;

namespace AvaloniaVncClient.Services
{
    public class InteractiveAuthenticationHandler : IAuthenticationHandler
    {
        // TODO: https://reactiveui.net/docs/handbook/events/

        /// <inhertitdoc />
        public Task<TInput> ProvideAuthenticationInputAsync<TInput>(RfbConnection connection, ISecurityType securityType, IAuthenticationInputRequest<TInput> request)
            where TInput : class, IAuthenticationInput
        {
            if (typeof(TInput) == typeof(PasswordAuthenticationInput))
            {
                // TODO: Request password from user
                return Task.FromResult((TInput)Convert.ChangeType(new PasswordAuthenticationInput("123456"), typeof(TInput)));
            }

            throw new InvalidOperationException("This authentication input request is not supported by this handler.");
        }
    }
}
