using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace MarcusW.VncClient.Protocol.Implementation.SecurityTypes
{
    /// <summary>
    /// A Stream wrapper that provides AES-EAX encryption and decryption for RA2 security type.
    /// </summary>
    /// <remarks>
    /// Each message is framed as: [2-byte length][encrypted message][16-byte MAC]
    /// The 2-byte length is associated data, and a 16-byte little-endian counter is the nonce.
    /// Uses ArrayPool to minimize GC pressure from per-frame allocations, which is critical
    /// for memory-constrained devices like Raspberry Pi 3.
    /// </remarks>
    internal class AesEaxStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly byte[] _clientSessionKey;
        private readonly byte[] _serverSessionKey;
        private ulong _writeCounter;
        private ulong _readCounter;
        private const int MacSize = 16;
        private const int NonceSize = 16;

        // Diagnostic counters for debug logging
        private long _totalReads;
        private long _totalWrites;
        private long _totalBytesDecrypted;
        private long _totalBytesEncrypted;

        // Read buffer for incoming messages - uses ArrayPool to reduce GC pressure
        private byte[] _readBuffer = Array.Empty<byte>();
        private bool _readBufferFromPool;
        private int _readPosition;
        private int _readLength;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public AesEaxStream(Stream baseStream, byte[] clientSessionKey, byte[] serverSessionKey)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            
            if (clientSessionKey == null)
                throw new ArgumentNullException(nameof(clientSessionKey));
            if (serverSessionKey == null)
                throw new ArgumentNullException(nameof(serverSessionKey));
            if (clientSessionKey.Length != 16)
                throw new ArgumentException("Client session key must be 16 bytes", nameof(clientSessionKey));
            if (serverSessionKey.Length != 16)
                throw new ArgumentException("Server session key must be 16 bytes", nameof(serverSessionKey));

            // Make copies of the keys to prevent external clearing
            _clientSessionKey = new byte[16];
            _serverSessionKey = new byte[16];
            Array.Copy(clientSessionKey, _clientSessionKey, 16);
            Array.Copy(serverSessionKey, _serverSessionKey, 16);

            _writeCounter = 0;
            _readCounter = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException();

            // If we have data in the read buffer, return it
            if (_readPosition < _readLength)
            {
                int available = _readLength - _readPosition;
                int toCopy = Math.Min(count, available);
                Array.Copy(_readBuffer, _readPosition, buffer, offset, toCopy);
                _readPosition += toCopy;
                return toCopy;
            }

            // Return the previous read buffer to the pool before reading a new frame
            ReturnReadBuffer();

            // Read message length (2 bytes, big-endian)
            Span<byte> lengthBytes = stackalloc byte[2];
            int lengthRead = 0;
            while (lengthRead < 2)
            {
                int read = _baseStream.Read(lengthBytes.Slice(lengthRead));
                if (read == 0)
                    return 0; // End of stream
                lengthRead += read;
            }

            ushort messageLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBytes);

            // Read encrypted message + MAC using a pooled buffer
            int ciphertextLength = messageLength + MacSize;
            byte[] ciphertext = ArrayPool<byte>.Shared.Rent(ciphertextLength);
            try
            {
                int totalRead = 0;
                while (totalRead < ciphertextLength)
                {
                    int read = _baseStream.Read(ciphertext, totalRead, ciphertextLength - totalRead);
                    if (read == 0)
                        throw new EndOfStreamException("Unexpected end of stream while reading AES-EAX message");
                    totalRead += read;
                }

                // Prepare associated data (2-byte length) as local buffer
                byte[] lengthAd = new byte[] { lengthBytes[0], lengthBytes[1] };

                // Decrypt with AES-EAX into a pooled buffer
                int plaintextLength = DecryptMessagePooled(ciphertext, ciphertextLength, lengthAd, _serverSessionKey, _readCounter,
                    out byte[] plaintextBuffer);
                _readBuffer = plaintextBuffer;
                _readBufferFromPool = true;
                _readLength = plaintextLength;
                _readPosition = 0;
                _readCounter++;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(ciphertext);
            }

            _totalReads++;
            _totalBytesDecrypted += _readLength;
            Debug.WriteLine($"[AesEaxStream] Read: decrypted frame #{_readCounter} ({_readLength} bytes). TotalReads={_totalReads}, TotalDecrypted={_totalBytesDecrypted}");

            // Return as much as requested
            int bytesToReturn = Math.Min(count, _readLength);
            Array.Copy(_readBuffer, 0, buffer, offset, bytesToReturn);
            _readPosition = bytesToReturn;
            return bytesToReturn;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException();

            // Prepare associated data (2-byte length) as local buffer
            byte[] lengthBuffer = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(lengthBuffer, (ushort)count);

            // Encrypt with AES-EAX using pooled output buffer
            int ciphertextLength = EncryptMessagePooled(buffer, offset, count, lengthBuffer, _clientSessionKey, _writeCounter,
                out byte[] ciphertext);
            _writeCounter++;
            _totalWrites++;
            _totalBytesEncrypted += count;
            Debug.WriteLine($"[AesEaxStream] Write: encrypted frame #{_writeCounter} ({count} bytes plaintext -> {ciphertextLength} bytes ciphertext). TotalWrites={_totalWrites}, TotalEncrypted={_totalBytesEncrypted}");

            try
            {
                // Write: [2-byte length][encrypted message + MAC]
                _baseStream.Write(lengthBuffer, 0, 2);
                _baseStream.Write(ciphertext, 0, ciphertextLength);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(ciphertext);
            }
        }

        /// <summary>
        /// Encrypts plaintext using AES-EAX mode, returning a pooled buffer.
        /// Caller MUST return the buffer to ArrayPool when done.
        /// </summary>
        private static int EncryptMessagePooled(byte[] plaintext, int plaintextOffset, int plaintextLength,
            byte[] associatedData, byte[] key, ulong counter, out byte[] ciphertextBuffer)
        {
            // Allocate nonce per-call (16 bytes - negligible) to avoid thread-safety issues
            // between concurrent Read (decrypt) and Write (encrypt) operations
            byte[] nonce = new byte[NonceSize];
            BinaryPrimitives.WriteUInt64LittleEndian(nonce, counter);

            // Initialize EAX cipher
            var cipher = new EaxBlockCipher(new AesEngine());
            cipher.Init(true, new AeadParameters(new KeyParameter(key), MacSize * 8, nonce, associatedData));

            // Encrypt into a pooled buffer
            int outputSize = cipher.GetOutputSize(plaintextLength);
            ciphertextBuffer = ArrayPool<byte>.Shared.Rent(outputSize);
            int len = cipher.ProcessBytes(plaintext, plaintextOffset, plaintextLength, ciphertextBuffer, 0);
            len += cipher.DoFinal(ciphertextBuffer, len);

            return len;
        }

        /// <summary>
        /// Decrypts ciphertext using AES-EAX mode, returning a pooled buffer.
        /// Caller MUST return the buffer to ArrayPool when done.
        /// </summary>
        private static int DecryptMessagePooled(byte[] ciphertext, int ciphertextLength,
            byte[] associatedData, byte[] key, ulong counter, out byte[] plaintextBuffer)
        {
            // Allocate nonce per-call (16 bytes - negligible) to avoid thread-safety issues
            // between concurrent Read (decrypt) and Write (encrypt) operations
            byte[] nonce = new byte[NonceSize];
            BinaryPrimitives.WriteUInt64LittleEndian(nonce, counter);

            // Initialize EAX cipher
            var cipher = new EaxBlockCipher(new AesEngine());
            cipher.Init(false, new AeadParameters(new KeyParameter(key), MacSize * 8, nonce, associatedData));

            // Decrypt into a pooled buffer
            int outputSize = cipher.GetOutputSize(ciphertextLength);
            plaintextBuffer = ArrayPool<byte>.Shared.Rent(outputSize);
            int len = cipher.ProcessBytes(ciphertext, 0, ciphertextLength, plaintextBuffer, 0);
            try
            {
                len += cipher.DoFinal(plaintextBuffer, len);
            }
            catch (Exception ex)
            {
                ArrayPool<byte>.Shared.Return(plaintextBuffer);
                throw new InvalidOperationException("AES-EAX decryption failed - MAC verification error", ex);
            }

            return len;
        }

        /// <summary>
        /// Returns the current read buffer to the pool if it was rented.
        /// </summary>
        private void ReturnReadBuffer()
        {
            if (_readBufferFromPool && _readBuffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(_readBuffer);
            }
            _readBuffer = Array.Empty<byte>();
            _readBufferFromPool = false;
            _readPosition = 0;
            _readLength = 0;
        }

        public override void Flush() => _baseStream.Flush();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Don't dispose the base stream - it's managed by the base transport
                Array.Clear(_clientSessionKey, 0, _clientSessionKey.Length);
                Array.Clear(_serverSessionKey, 0, _serverSessionKey.Length);

                // Return the read buffer to the pool before clearing
                ReturnReadBuffer();
            }
            base.Dispose(disposing);
        }
    }
}
