namespace MarcusW.VncClient.Security
{
    /// <summary>
    /// Represents a response for a <see cref="IAuthenticationInputRequest"/>.
    /// </summary>
    /// <typeparam name="TRequest">The type of the input request.</typeparam>
    public interface IAuthenticationInput<TRequest> where TRequest : class, IAuthenticationInputRequest { }
}
