using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using MarcusW.VncClient.Protocol.Services;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Communication
{
    /// <inhertitdoc />
    public sealed class ZLibInflater : IZLibInflater
    {
        private MemoryStream? _memoryStream;
        private DeflateStream? _inflateStream;

        private volatile bool _disposed;

        /// <inheritdoc />
        public Stream ReadAndInflate(Stream source, int sourceLength, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (_disposed)
                throw new ObjectDisposedException(nameof(ZLibInflater));

            cancellationToken.ThrowIfCancellationRequested();

            // The data must be read and buffered in a memory stream, before passing it to the inflate stream.
            // This seems to be necessary to limit the inflate stream in the amount of bytes it has access to,
            // so it doesn't read more bytes than wanted.
            // Inspiration taken from: https://github.com/quamotion/remoteviewing/blob/926d2baf8de446252fdc3a59054d0af51cdb065d/RemoteViewing/Vnc/VncClient.Framebuffer.cs#L208

            // Create a new memory stream, if necessary.
            _memoryStream ??= new MemoryStream(sourceLength);

            // Write the source data to the memory stream to buffer it
            _memoryStream.Position = 0;
            source.CopyAllTo(_memoryStream, sourceLength, cancellationToken);
            _memoryStream.SetLength(sourceLength);
            _memoryStream.Position = 0;

            // Create a new inflate stream, if necessary.
            if (_inflateStream == null)
            {
                // Skip the two bytes of the ZLib header (see RFC1950)
                if (sourceLength < 2)
                    throw new InvalidOperationException("Inflater cannot be initialized with less than two bytes.");
                _memoryStream.Position = 2;

                _inflateStream = new DeflateStream(_memoryStream, CompressionMode.Decompress, false);
            }

            return _inflateStream;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _memoryStream?.Dispose();
            _inflateStream?.Dispose();

            _disposed = true;
        }
    }
}
