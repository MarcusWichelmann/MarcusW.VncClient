using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace MarcusW.VncClient.Protocol.Implementation.SecurityTypes
{
    /// <summary>
    /// A transport wrapper that encrypts and decrypts RFB messages using AES-EAX mode.
    /// </summary>
    /// <remarks>
    /// This is used by RA2 and RA2_256 security types to encrypt all protocol messages
    /// after the initial RSA key exchange. Each message is framed as:
    /// [2-byte length (big-endian)][encrypted message][16-byte MAC]
    /// The 2-byte length is used as associated data, and a 16-byte little-endian counter
    /// is used as the nonce, incrementing from zero for each message.
    /// </remarks>
    internal class AesEaxTransport : ITransport
    {
        private readonly ITransport _baseTransport;
        private readonly AesEaxStream _aesEaxStream;

        public Stream Stream => _aesEaxStream;

        public bool IsEncrypted => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="AesEaxTransport"/>.
        /// </summary>
        /// <param name="baseTransport">The underlying transport.</param>
        /// <param name="clientSessionKey">The 16-byte AES key for encrypting client-to-server messages.</param>
        /// <param name="serverSessionKey">The 16-byte AES key for decrypting server-to-client messages.</param>
        public AesEaxTransport(ITransport baseTransport, byte[] clientSessionKey, byte[] serverSessionKey)
        {
            _baseTransport = baseTransport ?? throw new ArgumentNullException(nameof(baseTransport));

            if (clientSessionKey == null || clientSessionKey.Length != 16)
                throw new ArgumentException("Client session key must be 16 bytes", nameof(clientSessionKey));
            if (serverSessionKey == null || serverSessionKey.Length != 16)
                throw new ArgumentException("Server session key must be 16 bytes", nameof(serverSessionKey));

            _aesEaxStream = new AesEaxStream(_baseTransport.Stream, clientSessionKey, serverSessionKey);
        }

        public void Dispose()
        {
            _aesEaxStream?.Dispose();
        }
    }
}
