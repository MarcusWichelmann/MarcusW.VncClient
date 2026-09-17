using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.Services;
using MarcusW.VncClient.Utils;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Initialization
{
    /// <inhertitdoc />
    public class RfbInitializer : IRfbInitializer
    {
        private readonly RfbConnectionContext _context;
        private readonly ProtocolState _state;
        private readonly ILogger<RfbInitializer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RfbInitializer"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public RfbInitializer(RfbConnectionContext context)
        {
            _context = context;
            _state = context.GetState<ProtocolState>();
            _logger = context.Connection.LoggerFactory.CreateLogger<RfbInitializer>();
        }

        /// <inheritdoc />
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Debug.Assert(_context.Transport != null, "_context.Transport != null");

            // Removed debug logging for production use

            ITransport transport = _context.Transport;

            // Send ClientInit message
            await SendClientInitAsync(transport, cancellationToken).ConfigureAwait(false);

            // Read ServerInit response
            (Size framebufferSize, PixelFormat pixelFormat, string desktopName) = await ReadServerInitAsync(transport, cancellationToken).ConfigureAwait(false);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                // Removed debug logging for production use
            }

            // Check if we need to negotiate a different pixel format
            PixelFormat negotiatedFormat = pixelFormat;
            bool needsNegotiation = false;

            // If server's pixel format is >32bpp or otherwise incompatible, negotiate a suitable one
            if (pixelFormat.BitsPerPixel > 32)
            {
                negotiatedFormat = CreateCompatiblePixelFormat(pixelFormat);
                needsNegotiation = true;
                _logger.LogInformation("Negotiating pixel format from {ServerFormat} to {NegotiatedFormat}", pixelFormat, negotiatedFormat);
            }

            if (needsNegotiation)
            {
                await NegotiatePixelFormatAsync(transport, negotiatedFormat, cancellationToken).ConfigureAwait(false);
            }

            // Update state
            _state.RemoteFramebufferSize = framebufferSize;
            _state.RemoteFramebufferFormat = negotiatedFormat; // Use negotiated format
            _state.RemoteFramebufferLayout = new[] { new Screen(1, new Rectangle(Position.Origin, framebufferSize), 0) }.ToImmutableHashSet();
            _state.DesktopName = desktopName;

            // Some security types extend the ServerInit response and now have the chance to continue reading
            Debug.Assert(_state.UsedSecurityType != null, "_state.UsedSecurityType != null");
            await _state.UsedSecurityType.ReadServerInitExtensionAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task SendClientInitAsync(ITransport transport, CancellationToken cancellationToken = default)
        {
            bool shared = _context.Connection.Parameters.AllowSharedConnection;
            // Removed debug logging for production use

            await transport.Stream.WriteAsync(new[] { (byte)(shared ? 1 : 0) }, cancellationToken).ConfigureAwait(false);
        }

        private async Task<(Size framebufferSize, PixelFormat pixelFormat, string desktopName)> ReadServerInitAsync(ITransport transport,
            CancellationToken cancellationToken = default)
        {
            // Removed debug logging for production use

            // Read the first part of the message of which the length is known
            ReadOnlyMemory<byte> headerBytes = await transport.Stream.ReadAllAsync(24, cancellationToken).ConfigureAwait(false);
            
            // Removed extensive hex dumping debug logging for production use

        // CRITICAL: Check if this is actually an error message from newer WayVNC
        if (IsServerInitErrorMessage(headerBytes.Span))
        {
            _logger.LogError("Detected ServerInit error message from server");
            await HandleServerInitErrorMessage(headerBytes, transport, cancellationToken).ConfigureAwait(false);
            // This will throw an exception, so we won't reach here
            throw new InvalidOperationException("Should not reach this point - HandleServerInitErrorMessage should have thrown an exception");
        }
            
            // Removed hex dump debug logging for production use
            
            Size framebufferSize;
            PixelFormat pixelFormat;
            
            // Standard RFB ServerInit parsing: first 4 bytes = framebuffer size, next 16 bytes = pixel format
            framebufferSize = GetFramebufferSize(headerBytes.Span[..4]);
            pixelFormat = GetPixelFormat(headerBytes.Span[4..20]);
            uint desktopNameLength = BinaryPrimitives.ReadUInt32BigEndian(headerBytes.Span[20..24]);

            // Removed desktop name length debug logging for production use

            // Validate desktop name length - prevent reading excessive amounts of data
            const uint MaxReasonableDesktopNameLength = 1024; // 1KB should be more than enough for any desktop name
            string desktopName;
            
            if (desktopNameLength > MaxReasonableDesktopNameLength)
            {
                _logger.LogWarning("Server reported unreasonably large desktop name length: {DesktopNameLength} bytes. " +
                    "This may indicate corrupted server data. Using default desktop name.", desktopNameLength);
                desktopName = "Unknown Desktop"; // Default fallback name
            }
            else
            {
                try
                {
            // Read desktop name
            ReadOnlyMemory<byte> desktopNameBytes = await transport.Stream.ReadAllAsync((int)desktopNameLength, cancellationToken).ConfigureAwait(false);
                    desktopName = Encoding.UTF8.GetString(desktopNameBytes.Span);
                    // Removed debug logging for production use
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read desktop name from server. Using default name.");
                    desktopName = "Unknown Desktop";
                }
            }

            return (framebufferSize, pixelFormat, desktopName);
        }

        private Size GetFramebufferSize(ReadOnlySpan<byte> headerBytes)
        {
            ushort framebufferWidth = BinaryPrimitives.ReadUInt16BigEndian(headerBytes[..2]);
            ushort framebufferHeight = BinaryPrimitives.ReadUInt16BigEndian(headerBytes[2..4]);
            
            // CRITICAL DEBUG: Show exactly what bytes we're using for framebuffer size (only at Debug level)
            // Removed verbose framebuffer size parsing debug logging for production use

            return new Size(framebufferWidth, framebufferHeight);
        }

        private PixelFormat GetPixelFormat(ReadOnlySpan<byte> headerBytes)
        {
            byte bitsPerPixel = headerBytes[0];
            byte depth = headerBytes[1];
            bool bigEndian = headerBytes[2] != 0;
            bool trueColor = headerBytes[3] != 0;
            ushort redMax = BinaryPrimitives.ReadUInt16BigEndian(headerBytes[4..6]);
            ushort greenMax = BinaryPrimitives.ReadUInt16BigEndian(headerBytes[6..8]);
            ushort blueMax = BinaryPrimitives.ReadUInt16BigEndian(headerBytes[8..10]);
            byte redShift = headerBytes[10]; 
            byte greenShift = headerBytes[11];
            byte blueShift = headerBytes[12];

            // Removed extensive pixel format debug logging for production use

            // Remaining 3 bytes are padding

            // Color maps are now supported - no need to throw an exception for indexed color formats

            // Removed detailed pixel format debug logging for production use
            
            // Check bits per pixel - modern servers may report higher values but we can handle them if they fit in 32-bit space
            if (bitsPerPixel > 64)
                throw new UnexpectedDataException($"The bits per pixel value ({bitsPerPixel}) of the received pixel format is unsupported. Maximum supported is 64bpp.");

            // Get the used bits per channel
            byte redBits = PixelUtils.GetChannelDepth(redMax);
            byte greenBits = PixelUtils.GetChannelDepth(greenMax);
            byte blueBits = PixelUtils.GetChannelDepth(blueMax);

            // For >32bpp formats, check if we can effectively handle them as 32bpp
            byte effectiveBitsPerPixel = bitsPerPixel;
            if (bitsPerPixel > 32)
            {
                // Calculate the minimum bits needed based on the color channel requirements
                byte maxUsedShift = Math.Max(Math.Max(redShift, greenShift), blueShift);
                byte maxChannelBits = Math.Max(Math.Max(redBits, greenBits), blueBits);
                byte minRequiredBits = (byte)(maxUsedShift + maxChannelBits);
                
                if (minRequiredBits <= 32)
                {
                    _logger.LogInformation("Server reported {OriginalBpp}bpp format, but effective color data fits in 32bpp. Using 32bpp for processing.", bitsPerPixel);
                    effectiveBitsPerPixel = 32;
                }
                else
                {
                    _logger.LogWarning("Server reported {BitsPerPixel}bpp format requiring {MinRequiredBits} bits, which exceeds 32bpp support.", bitsPerPixel, minRequiredBits);
                }
            }

            // Removed channel bits calculation debug logging for production use

            // Check the depth value - but be more lenient with modern servers that might report unusual values
            if (redBits + greenBits + blueBits > depth)
            {
                _logger.LogWarning("Server reported inconsistent pixel format: depth={Depth} but color channels require {RequiredBits} bits. " +
                    "This may indicate a non-standard server implementation. Attempting to continue with corrected depth value.", 
                    depth, redBits + greenBits + blueBits);
                
                // For compatibility, we'll adjust the depth to match the actual color requirements
                // This handles servers that report incorrect depth values
                depth = (byte)Math.Max(depth, redBits + greenBits + blueBits);
                _logger.LogInformation("Adjusted pixel format depth to {AdjustedDepth} for compatibility", depth);
            }

            // Removed shift validation debug logging for production use

            // Check the shift values - but be more lenient with servers that report unusual values
            bool hasInvalidShifts = redBits + redShift > bitsPerPixel || greenBits + greenShift > bitsPerPixel || blueBits + blueShift > bitsPerPixel;
            if (hasInvalidShifts)
            {
                _logger.LogWarning("Server reported invalid color shift values. Red: {RedBits}+{RedShift}={RedTotal}, Green: {GreenBits}+{GreenShift}={GreenTotal}, Blue: {BlueBits}+{BlueShift}={BlueTotal}, BPP: {BitsPerPixel}. " +
                    "Attempting to correct shift values for compatibility.", 
                    redBits, redShift, redBits + redShift, greenBits, greenShift, greenBits + greenShift, blueBits, blueShift, blueBits + blueShift, bitsPerPixel);
                
                // Try to correct the shift values - common patterns for RGB formats
                if (redMax == 255 && greenMax == 255 && blueMax == 255 && bitsPerPixel >= 24)
                {
                    // Standard RGB format - assume RGB or BGR layout
                    if (redShift > greenShift && greenShift > blueShift)
                    {
                        // Likely RGB layout
                        redShift = 16; greenShift = 8; blueShift = 0;
                        _logger.LogInformation("Corrected to RGB layout: Red=16, Green=8, Blue=0");
                    }
                    else if (blueShift > greenShift && greenShift > redShift)
                    {
                        // Likely BGR layout  
                        redShift = 0; greenShift = 8; blueShift = 16;
                        _logger.LogInformation("Corrected to BGR layout: Red=0, Green=8, Blue=16");
                    }
                    else
                    {
                        // Default to RGB
                        redShift = 16; greenShift = 8; blueShift = 0;
                        _logger.LogInformation("Defaulted to RGB layout: Red=16, Green=8, Blue=0");
                    }
                }
                else
                {
                    _logger.LogWarning("Could not auto-correct shift values for unusual pixel format. Connection may fail.");
                }
            }

            // Check for overlaps
            uint redMask = (uint)redMax << redShift;
            uint greenMask = (uint)greenMax << greenShift;
            uint blueMask = (uint)blueMax << blueShift;
            // Removed color mask debug logging for production use
                
            uint overlaps = ((redMask & greenMask) | (greenMask & blueMask) | (blueMask & redMask));
            if (overlaps != 0)
            {
                _logger.LogWarning("Server reported overlapping color channels (overlap mask: 0x{OverlapMask:X8}). " +
                    "This may indicate corrupted pixel format data. Attempting to continue anyway.", overlaps);
                // Don't throw - modern servers sometimes send unusual formats that work despite overlaps
            }

            // Generate a short name for this pixel format while following the RFB naming scheme (name describes the native byte order, e.g. 0xRGB).
            string name;
            if (redShift > greenShift && greenShift > blueShift)
            {
                name = $"RGB{redBits}{greenBits}{blueBits}";
                // RGB order detected
            }
            else if (blueShift > greenShift && greenShift > redShift)
            {
                name = $"BGR{blueBits}{greenBits}{redBits}";
                // BGR order detected
            }
            else
            {
                _logger.LogWarning("Server reported unusual pixel format order (Red: {RedShift}, Green: {GreenShift}, Blue: {BlueShift}). " +
                    "This may indicate corrupted data from the server. Using default RGB format for compatibility.", 
                    redShift, greenShift, blueShift);
                    
                // Default to RGB format when server data is corrupted/unusual
                name = $"RGB{redBits}{greenBits}{blueBits}";
                
                // If the data looks completely corrupted (like BPP=0), use standard values
                if (effectiveBitsPerPixel == 0 || redBits + greenBits + blueBits == 0)
                {
                    _logger.LogInformation("Server pixel format data appears completely corrupted. Using standard 24-bit RGB format.");
                    effectiveBitsPerPixel = 32; // Standard 32bpp format
                    depth = 24; // 24-bit color depth
                    redMax = greenMax = blueMax = 255; // 8 bits per channel
                    redShift = 16; greenShift = 8; blueShift = 0; // RGB order
                    redBits = greenBits = blueBits = 8;
                    name = "RGB888";
                }
            }

            // Create pixel format without alpha support.
            // For some pixel formats, servers will send alpha values anyway, but we ignore them because that's how it's described in the protocol.
            // Use effective bits per pixel for compatibility with >32bpp server formats
            return new PixelFormat($"RFB {name}", effectiveBitsPerPixel, depth, bigEndian, trueColor, false, redMax, greenMax, blueMax, 0, redShift, greenShift, blueShift, 0);
        }

        private PixelFormat CreateCompatiblePixelFormat(PixelFormat serverFormat)
        {
            // Create a 32bpp RGB format that's compatible with our rendering pipeline
            // Most modern VNC servers will accept this standard format
            return new PixelFormat(
                "RGB888 32bpp",
                32,      // 32 bits per pixel
                24,      // 24-bit color depth 
                false,   // Little endian
                true,    // True color
                false,   // No alpha
                255,     // Red max (8-bit)
                255,     // Green max (8-bit) 
                255,     // Blue max (8-bit)
                0,       // Alpha max (unused)
                16,      // Red shift (bits 16-23)
                8,       // Green shift (bits 8-15)
                0,       // Blue shift (bits 0-7)
                0        // Alpha shift (unused)
            );
        }

        /// <summary>
        /// Detects if the ServerInit data is actually an error message from newer WayVNC
        /// </summary>
        private bool IsServerInitErrorMessage(ReadOnlySpan<byte> headerBytes)
        {
            try
            {
                // Error message detection debug logging removed for production

                if (headerBytes.Length < 8)
                    return false;

                // Try multiple detection patterns for WayVNC error messages

                // Pattern 1: Look for "Invalid username" directly in the bytes
                string headerText = System.Text.Encoding.UTF8.GetString(headerBytes);
                // Removed debug logging for production use
                
                if (headerText.Contains("Invalid username", StringComparison.OrdinalIgnoreCase))
                {
                    // Detected invalid username in header text
                    return true;
                }

                // Pattern 2: Check for various positions where error message might start
                for (int offset = 4; offset < Math.Min(headerBytes.Length - 4, 12); offset += 2)
                {
                    try
                    {
                        // Try reading length at different offsets
                        if (offset + 2 < headerBytes.Length)
                        {
                            uint potentialLength = BinaryPrimitives.ReadUInt16BigEndian(headerBytes.Slice(offset, 2));
                            // Removed debug logging for production use

                            if (potentialLength > 0 && potentialLength <= 1024 && offset + 2 + potentialLength <= headerBytes.Length)
                            {
                                var messageBytes = headerBytes.Slice(offset + 2, (int)potentialLength);
                                string messageText = System.Text.Encoding.UTF8.GetString(messageBytes);
                                // Message detected at offset

                                // Check if it looks like an error message
                                var errorKeywords = new[] { "Invalid", "Error", "Failed", "Denied", "Unauthorized", "Authentication" };
                                if (errorKeywords.Any(keyword => messageText.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                                {
                                    // Found error keyword in message
                                    return true;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Continue trying other offsets
                        continue;
                    }
                }

                // Pattern 3: Look for ASCII text that looks like error messages in the raw bytes
                for (int i = 0; i < headerBytes.Length - 10; i++)
                {
                    try
                    {
                        // Try to decode 10+ byte sequences as ASCII
                        int maxLen = Math.Min(50, headerBytes.Length - i);
                        var testBytes = headerBytes.Slice(i, maxLen);
                        string testText = System.Text.Encoding.UTF8.GetString(testBytes);
                        
                        if (testText.Contains("Invalid", StringComparison.OrdinalIgnoreCase) || 
                            testText.Contains("username", StringComparison.OrdinalIgnoreCase) ||
                            testText.Contains("Error", StringComparison.OrdinalIgnoreCase))
                        {
                            // Found error-like text at position
                            return true;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                // No error message pattern detected
                return false;
            }
            catch (Exception ex)
            {
                // Error while checking for ServerInit error message
                return false;
            }
        }

        /// <summary>
        /// Handles error messages found in ServerInit data from newer WayVNC
        /// </summary>
        private async Task HandleServerInitErrorMessage(ReadOnlyMemory<byte> headerBytes, ITransport transport, CancellationToken cancellationToken)
        {
            try
            {
                var headerSpan = headerBytes.Span;
                _logger.LogError("Parsing WayVNC error message");
                _logger.LogError("Raw header bytes: {HeaderBytes}", Convert.ToHexString(headerSpan));

                string errorMessage = "Unknown WayVNC error";

                // Try to extract the error message using multiple patterns

                // Pattern 1: Look for "Invalid username" directly
                string headerText = System.Text.Encoding.UTF8.GetString(headerSpan);
                if (headerText.Contains("Invalid username", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract the readable part
                    int startIndex = headerText.IndexOf("Invalid", StringComparison.OrdinalIgnoreCase);
                    if (startIndex >= 0)
                    {
                        errorMessage = headerText.Substring(startIndex).Trim('\0').Trim();
                        _logger.LogError("Extracted error message (pattern 1): '{ErrorMessage}'", errorMessage);
                    }
                }
                else
                {
                    // Pattern 2: Try to find structured error message with length prefix
                    bool foundError = false;
                    for (int offset = 4; offset < Math.Min(headerSpan.Length - 4, 12) && !foundError; offset += 2)
                    {
                        try
                        {
                            if (offset + 2 < headerSpan.Length)
                            {
                                uint potentialLength = BinaryPrimitives.ReadUInt16BigEndian(headerSpan.Slice(offset, 2));
                                
                                if (potentialLength > 0 && potentialLength <= 1024 && offset + 2 + potentialLength <= headerSpan.Length)
                                {
                                    var messageBytes = headerSpan.Slice(offset + 2, (int)potentialLength);
                                    string candidateMessage = System.Text.Encoding.UTF8.GetString(messageBytes).Trim();
                                    
                                    var errorKeywords = new[] { "Invalid", "Error", "Failed", "Denied", "Unauthorized", "Authentication" };
                                    if (errorKeywords.Any(keyword => candidateMessage.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        errorMessage = candidateMessage;
                                        _logger.LogError("Extracted error message (pattern 2, offset {Offset}): '{ErrorMessage}'", offset, errorMessage);
                                        foundError = true;
                                        break;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    // Pattern 3: Scan for any error-like text in the bytes
                    if (!foundError)
                    {
                        for (int i = 0; i < headerSpan.Length - 10; i++)
                        {
                            try
                            {
                                int maxLen = Math.Min(50, headerSpan.Length - i);
                                var testBytes = headerSpan.Slice(i, maxLen);
                                string testText = System.Text.Encoding.UTF8.GetString(testBytes);
                                
                                if (testText.Contains("Invalid", StringComparison.OrdinalIgnoreCase) || 
                                    testText.Contains("Error", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Try to extract a clean error message
                                    string cleanMessage = testText.Trim('\0').Trim();
                                    if (cleanMessage.Length > 5) // Reasonable minimum length
                                    {
                                        errorMessage = cleanMessage.Length > 100 ? cleanMessage.Substring(0, 100) + "..." : cleanMessage;
                                        _logger.LogError("Extracted error message (pattern 3, position {Position}): '{ErrorMessage}'", i, errorMessage);
                                        foundError = true;
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                }

                _logger.LogError("Final WayVNC authentication error: '{ErrorMessage}'", errorMessage);

                throw new InvalidOperationException($"WayVNC post-authentication validation failed: {errorMessage}. " +
                    "The VeNCrypt handshake succeeded, but the server rejected the connection during ServerInit. " +
                    "Verify that the provided username and password are correct and that the user has VNC access permissions on the server.");
                
                // Note: No need to read additional bytes since the error message seems to be contained in the first 24 bytes
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw our specific exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse WayVNC ServerInit error message");
                throw new InvalidOperationException("WayVNC sent an error message during ServerInit, but failed to parse it properly.", ex);
            }
        }

        private Task NegotiatePixelFormatAsync(ITransport transport, PixelFormat pixelFormat, CancellationToken cancellationToken = default)
        {
            // Removed debug logging for production use

            // Create and send SetPixelFormat message
            var setPixelFormatMessage = new Protocol.Implementation.MessageTypes.Outgoing.SetPixelFormatMessage(pixelFormat);
            var messageType = new Protocol.Implementation.MessageTypes.Outgoing.SetPixelFormatMessageType();
            
            messageType.WriteToTransport(setPixelFormatMessage, transport, cancellationToken);
            
            // Removed debug logging for production use
            
            return Task.CompletedTask;
        }
    }
}
