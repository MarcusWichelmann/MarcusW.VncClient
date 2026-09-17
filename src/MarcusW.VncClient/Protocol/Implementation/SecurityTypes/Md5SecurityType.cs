using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Security;
using MarcusW.VncClient.Utils;

namespace MarcusW.VncClient.Protocol.Implementation.SecurityTypes
{
    /// <summary>
    /// A security type that uses MD5 hash authentication for password verification.
    /// This provides basic password authentication using MD5 hashing but no transport encryption.
    /// </summary>
    public class Md5SecurityType : ISecurityType
    {
        private readonly RfbConnectionContext _context;

        /// <inheritdoc />
        public byte Id => (byte)WellKnownSecurityType.MD5;

        /// <inheritdoc />
        public string Name => "MD5";

        /// <inheritdoc />
        public int Priority => 15; // Higher than VNC auth due to better hashing, but lower than encrypted methods

        /// <summary>
        /// Initializes a new instance of the <see cref="Md5SecurityType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public Md5SecurityType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc />
        public async Task<AuthenticationResult> AuthenticateAsync(IAuthenticationHandler authenticationHandler, CancellationToken cancellationToken = default)
        {
            if (authenticationHandler == null)
                throw new ArgumentNullException(nameof(authenticationHandler));

            cancellationToken.ThrowIfCancellationRequested();

            ITransport transport = _context.Transport ?? throw new InvalidOperationException("Cannot access transport for authentication.");

            // Read the challenge from the server (typically 16 bytes)
            ReadOnlyMemory<byte> challengeBytes = await transport.Stream.ReadAllAsync(16, cancellationToken).ConfigureAwait(false);

            // Request password input from the authentication handler
            PasswordAuthenticationInput input = await authenticationHandler
                .ProvideAuthenticationInputAsync(_context.Connection, this, new PasswordAuthenticationInputRequest()).ConfigureAwait(false);

            // Calculate MD5 hash response
            ReadOnlyMemory<byte> response = CreateMd5Response(challengeBytes, input.Password);

            // Send the response to the server (MD5 hash is 16 bytes)
            await transport.Stream.WriteAsync(response, cancellationToken).ConfigureAwait(false);

            return new AuthenticationResult();
        }

        /// <inheritdoc />
        public Task ReadServerInitExtensionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <summary>
        /// Creates an MD5 hash response from the challenge and password.
        /// </summary>
        /// <param name="challenge">The challenge received from the server.</param>
        /// <param name="password">The password provided by the user.</param>
        /// <returns>The MD5 hash response to send to the server.</returns>
        private static ReadOnlyMemory<byte> CreateMd5Response(ReadOnlyMemory<byte> challenge, string password)
        {
            if (string.IsNullOrEmpty(password))
                password = string.Empty;

            // Convert password to bytes using UTF-8 encoding
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            // Create MD5 hasher
            using var md5 = MD5.Create();

            // Combine challenge and password for hashing
            // The typical approach is to hash the concatenation of challenge + password
            var combinedData = new byte[challenge.Length + passwordBytes.Length];
            challenge.CopyTo(combinedData.AsMemory(0, challenge.Length));
            passwordBytes.CopyTo(combinedData, challenge.Length);

            // Calculate MD5 hash
            byte[] hash = md5.ComputeHash(combinedData);

            // Clear sensitive data
            Array.Clear(passwordBytes, 0, passwordBytes.Length);
            Array.Clear(combinedData, 0, combinedData.Length);

            return hash;
        }
    }
}
