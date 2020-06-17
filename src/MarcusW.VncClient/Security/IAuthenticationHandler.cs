using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.SecurityTypes;

namespace MarcusW.VncClient.Security
{
    /// <summary>
    /// Provides handlers for authentication requests.
    /// </summary>
    public interface IAuthenticationHandler
    {
        /// <summary>
        /// Handles an authentication input request and tries to get the requested data from the user or device.
        /// </summary>
        /// <param name="connection">The connection which this request belongs to.</param>
        /// <param name="securityType">The security type which raised the request.</param>
        /// <param name="request">The input request.</param>
        /// <typeparam name="TRequest">The type of the input request.</typeparam>
        /// <returns>The input response.</returns>
        Task<IAuthenticationInput<TRequest>> ProvideAuthenticationInputAsync<TRequest>(RfbConnection connection, ISecurityType securityType, TRequest request)
            where TRequest : class, IAuthenticationInputRequest;
    }
}
