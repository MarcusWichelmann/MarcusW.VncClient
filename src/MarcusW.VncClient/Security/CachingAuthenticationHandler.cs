using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.SecurityTypes;

namespace MarcusW.VncClient.Security
{
    /// <summary>
    /// Decorates a user-provided <see cref="IAuthenticationHandler"/> and caches the provided inputs
    /// so automatic reconnects don't prompt the user for credentials again.
    /// </summary>
    /// <remarks>
    /// The cache is cleared when a handshake fails (see <see cref="Clear"/>), so the user gets prompted
    /// again in case the cached credentials have become invalid (e.g. the server password was changed).
    /// </remarks>
    internal sealed class CachingAuthenticationHandler : IAuthenticationHandler
    {
        private readonly IAuthenticationHandler _innerHandler;
        private readonly bool _cachingEnabled;

        private readonly Dictionary<Type, IAuthenticationInput> _cache = new Dictionary<Type, IAuthenticationInput>();
        private readonly object _cacheLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingAuthenticationHandler"/>.
        /// </summary>
        /// <param name="innerHandler">The user-provided authentication handler to forward requests to.</param>
        /// <param name="cachingEnabled">Whether inputs should be cached for reuse on reconnects.</param>
        public CachingAuthenticationHandler(IAuthenticationHandler innerHandler, bool cachingEnabled)
        {
            _innerHandler = innerHandler ?? throw new ArgumentNullException(nameof(innerHandler));
            _cachingEnabled = cachingEnabled;
        }

        /// <inheritdoc />
        public async Task<TInput> ProvideAuthenticationInputAsync<TInput>(RfbConnection connection, ISecurityType securityType, IAuthenticationInputRequest<TInput> request)
            where TInput : class, IAuthenticationInput
        {
            if (_cachingEnabled)
            {
                lock (_cacheLock)
                {
                    if (_cache.TryGetValue(typeof(TInput), out IAuthenticationInput? cachedInput))
                        return (TInput)cachedInput;
                }
            }

            TInput input = await _innerHandler.ProvideAuthenticationInputAsync(connection, securityType, request).ConfigureAwait(false);

            if (_cachingEnabled)
            {
                lock (_cacheLock)
                    _cache[typeof(TInput)] = input;
            }

            return input;
        }

        /// <summary>
        /// Clears all cached authentication inputs so the next request is forwarded to the user again.
        /// </summary>
        public void Clear()
        {
            lock (_cacheLock)
                _cache.Clear();
        }
    }
}
