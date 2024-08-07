using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Security;

namespace MarcusW.VncClient.Protocol.Implementation.SecurityTypes;

/// <summary>
///     A security type which uses DES for a simple password authentication but does not provide any further transport
///     encryption.
/// </summary>
/// <remarks>
///     Initializes a new instance of the <see cref="VncAuthenticationSecurityType" />.
/// </remarks>
/// <param name="context">The connection context.</param>
public class VncAuthenticationSecurityType(RfbConnectionContext context) : ISecurityType
{
    /// <inhertitdoc />
    public byte Id => (byte)WellKnownSecurityType.VncAuthentication;

    /// <inhertitdoc />
    public string Name => "VncAuth";

    /// <inhertitdoc />
    public int Priority => 10;

    /// <inhertitdoc />
    public async Task<AuthenticationResult> AuthenticateAsync(IAuthenticationHandler? authenticationHandler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authenticationHandler);

        cancellationToken.ThrowIfCancellationRequested();

        ITransport transport = context.Transport
            ?? throw new InvalidOperationException("Cannot access transport for authentication.");

        // Read challenge
        ReadOnlyMemory<byte> challengeBytes =
            await transport.Stream.ReadAllAsync(16, cancellationToken).ConfigureAwait(false);

        // Request password input
        PasswordAuthenticationInput input = await authenticationHandler
            .ProvideAuthenticationInputAsync(context.Connection, this, new PasswordAuthenticationInputRequest())
            .ConfigureAwait(false);
        ReadOnlyMemory<byte> passwordBytes = Encoding.UTF8.GetBytes(input.Password);

        // Calculate response
        ReadOnlyMemory<byte> response = CreateChallengeResponse(challengeBytes, passwordBytes);

        // Send response
        Debug.Assert(response.Length == 16, "response.Length == 16");
        await transport.Stream.WriteAsync(response, cancellationToken).ConfigureAwait(false);

        return new();
    }

    /// <inheritdoc />
    public Task ReadServerInitExtensionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    // Inspired by https://github.com/quamotion/remoteviewing/blob/8c7cfeab90064d5bc68d9ab83313ddd12703c462/RemoteViewing/Vnc/VncPasswordChallenge.cs#L91
    private static ReadOnlyMemory<byte> CreateChallengeResponse(ReadOnlyMemory<byte> challenge,
        ReadOnlyMemory<byte> password)
    {
        // The first 8 characters/bytes of the password are the DES key (padded with zeros if shorter)
        var key = new byte[8];
        password[..Math.Min(key.Length, password.Length)].CopyTo(key);

        // Reverse bit order of all byes in key
        for (var i = 0; i < key.Length; i++)
        {
            byte value = key[i];
            byte newValue = 0;
            for (var offset = 0; offset < 8; offset++)
            {
                if ((value & (0b1 << offset)) != 0)
                {
                    newValue |= (byte)(0b10000000 >> offset);
                }
            }

            key[i] = newValue;
        }

        // Initialize encryptor with key
        using var desProvider = DES.Create();
        desProvider.Key = key;
        desProvider.Mode = CipherMode.ECB;
        using ICryptoTransform encryptor = desProvider.CreateEncryptor();

        // Encrypt challenge with key
        var response = new byte[16];
        encryptor.TransformBlock(challenge.ToArray(), 0, challenge.Length, response, 0);

        return response;
    }
}
