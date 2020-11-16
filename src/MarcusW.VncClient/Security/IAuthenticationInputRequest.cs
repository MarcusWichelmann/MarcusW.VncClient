namespace MarcusW.VncClient.Security
{
    /// <summary>
    /// Represents a request for input data needed for authentication.
    /// </summary>
    /// <typeparam name="TInput">The type of the requested input.</typeparam>
    public interface IAuthenticationInputRequest<TInput> where TInput : class, IAuthenticationInput { }
}
