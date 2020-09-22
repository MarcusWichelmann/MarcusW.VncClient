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
        public Task<IAuthenticationInput<TRequest>> ProvideAuthenticationInputAsync<TRequest>(RfbConnection connection, ISecurityType securityType, TRequest request)
            where TRequest : class, IAuthenticationInputRequest
        {
            if (request is PasswordAuthenticationInputRequest)
            {
                // TODO: Request password from user
                return Task.FromResult((IAuthenticationInput<TRequest>)new PasswordAuthenticationInput("123456"));
            }

            throw new InvalidOperationException("This authentication input request is not supported by this handler.");
        }
    }
}