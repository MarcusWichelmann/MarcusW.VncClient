using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using MarcusW.VncClient.Protocol.Services;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Communication
{
    /// <inhertitdoc />
    public sealed class ZLibInflater : IZLibInflater
    {
        private readonly Dictionary<int, Inflater> _inflaters = new Dictionary<int, Inflater>();

        private volatile bool _disposed;

        /// <inheritdoc />
        public Stream ReadAndInflate(Stream source, int sourceLength, int zlibStreamId = -1, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (_disposed)
                throw new ObjectDisposedException(nameof(ZLibInflater));

            cancellationToken.ThrowIfCancellationRequested();

            bool hasExisting = _inflaters.TryGetValue(zlibStreamId, out Inflater? inflater);

            if (hasExisting)
            {
                // Because we always read the exact size of bytes we need, it sometimes happens, that the inflate stream doesn't realize that all data has been read and therefore
                // reads garbage when the buffer has been refilled. This is fixed by attempting an additional read of 1 byte after all bytes have been read.
                // This read will not return any results, but it solves the problem. Believe me. :D
                // This is also useful to throw a nice exception if somebody fucked up and didn't read the full buffer.
                Span<byte> buf = stackalloc byte[1];
                if (inflater!.DeflateStream.Read(buf) != 0)
                    throw new RfbProtocolException(
                        $"Attempted to refill the zlib inflate stream before all bytes have been read. There was at least one byte pending to read. Stream id: {zlibStreamId}");
            }

            // The data must be read and buffered in a memory stream, before passing it to the inflate stream.
            // This seems to be necessary to limit the inflate stream in the amount of bytes it has access to,
            // so it doesn't read more bytes than wanted.
            // Inspiration taken from: https://github.com/quamotion/remoteviewing/blob/926d2baf8de446252fdc3a59054d0af51cdb065d/RemoteViewing/Vnc/VncClient.Framebuffer.cs#L208
            // TODO: Can this limit somehow be implemented without having to copy all the data? Maybe a custom Stream implementation?

            // Create a new inflater, if necessary
            if (!hasExisting)
            {
                inflater = new Inflater();
                _inflaters.Add(zlibStreamId, inflater);
            }

            // Write the source data to the memory stream to buffer it
            inflater!.MemoryStream.Position = 0;
            source.CopyAllTo(inflater.MemoryStream, sourceLength, cancellationToken);
            inflater.MemoryStream.SetLength(sourceLength);
            inflater.MemoryStream.Position = 0;

            // Skip the two bytes of the ZLib header (see RFC1950) when a new stream was created
            if (!hasExisting)
            {
                if (sourceLength < 2)
                    throw new InvalidOperationException("Inflater cannot be initialized with less than two bytes.");
                inflater.MemoryStream.Position = 2;
            }

            return inflater.DeflateStream;
        }

        /// <inheritdoc />
        public void ResetZlibStream(int id)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ZLibInflater));

            if (_inflaters.TryGetValue(id, out Inflater? inflater))
                inflater.Dispose();
            _inflaters.Remove(id);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (Inflater inflater in _inflaters.Values)
                inflater.Dispose();

            _disposed = true;
        }

        private sealed class Inflater : IDisposable
        {
            public MemoryStream MemoryStream { get; set; }

            public DeflateStream DeflateStream { get; set; }

            public Inflater()
            {
                MemoryStream = new MemoryStream();
                DeflateStream = new DeflateStream(MemoryStream, CompressionMode.Decompress, false);
            }

            public void Dispose()
            {
                MemoryStream.Dispose();
                DeflateStream.Dispose();
            }
        }
    }
}
