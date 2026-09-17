using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using MarcusW.VncClient.Output;
using MarcusW.VncClient.Protocol.EncodingTypes;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// A pseudo encoding type to receive changes of the remote desktop name.
    /// </summary>
    public class DesktopNameEncodingType : PseudoEncodingType
    {
        private readonly RfbConnectionContext _context;
        private readonly ILogger<DesktopNameEncodingType> _logger;
        private readonly ProtocolState _state;

        /// <inheritdoc />
        public override int Id => (int)WellKnownEncodingType.DesktopName;

        /// <inheritdoc />
        public override string Name => "DesktopName";

        /// <inheritdoc />
        public override bool GetsConfirmed => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="DesktopNameEncodingType"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public DesktopNameEncodingType(RfbConnectionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = context.Connection.LoggerFactory.CreateLogger<DesktopNameEncodingType>();
            _state = context.GetState<ProtocolState>();
        }

        /// <inheritdoc />
        public override void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle)
        {
            // According to the RFB spec, x-position, y-position, width, and height must be zero
            // for this pseudo-rectangle, but we don't enforce this for compatibility.

            // Read 4-byte name length (big-endian)
            Span<byte> lengthBuffer = stackalloc byte[4];
            transportStream.ReadAll(lengthBuffer);
            uint nameLength = BinaryPrimitives.ReadUInt32BigEndian(lengthBuffer);

            // Sanity check for name length
            if (nameLength > 4096)
            {
                _logger.LogWarning("Received desktop name is too long ({NameLength}). Skipping...", nameLength);
                transportStream.SkipAll((int)nameLength);
                return;
            }

            string desktopName;
            if (nameLength == 0)
            {
                desktopName = string.Empty;
            }
            else
            {
                // Read the UTF-8 encoded name string
                byte[] nameBuffer = ArrayPool<byte>.Shared.Rent((int)nameLength);
                try
                {
                    Span<byte> nameSpan = nameBuffer.AsSpan(0, (int)nameLength);
                    transportStream.ReadAll(nameSpan);
                    desktopName = Encoding.UTF8.GetString(nameSpan);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(nameBuffer);
                }
            }

            // Update the protocol state
            string? previousName = _state.DesktopName;
            if (desktopName != previousName)
            {
                _state.DesktopName = desktopName;
                _logger.LogDebug("Desktop name changed from '{PreviousName}' to '{NewName}'", previousName, desktopName);

                // Notify the output handler
                _context.Connection.OutputHandler?.HandleDesktopNameChange(desktopName);
            }
        }
    }
}
