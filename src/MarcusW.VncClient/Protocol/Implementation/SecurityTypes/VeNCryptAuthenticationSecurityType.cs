using MarcusW.VncClient.Protocol.Implementation.Services.Transports;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.SecurityTypes
{

    public class VeNCryptAuthenticationSecurityType : ISecurityType
    {
        public byte Id => 19;
        public string Name => "VeNCrypt";
        public int Priority => 100;

        private readonly RfbConnectionContext _context;

        public VeNCryptAuthenticationSecurityType(RfbConnectionContext context)
        {
            _context = context;
        }
        public async Task<AuthenticationResult> AuthenticateAsync(IAuthenticationHandler handler, CancellationToken cancellationToken = default)
        {
            var stream = _context.Transport.Stream;

            // Step 1: Receive server VeNCrypt version (2 bytes: major, minor)
            byte[] serverVersion = await ReadExactAsync(stream, 2, cancellationToken).ConfigureAwait(false);
            byte serverMajor = serverVersion[0];
            byte serverMinor = serverVersion[1];
            // Removed VeNCrypt server version debug logging for production use

            // Step 2: Choose client version (max supported 0.2)
            byte clientMajor = 0;
            byte clientMinor = 2;

            if (serverMajor < clientMajor || (serverMajor == clientMajor && serverMinor < clientMinor))
            {
                clientMajor = serverMajor;
                clientMinor = serverMinor;
            }

            // Send chosen VeNCrypt version (2 bytes)
            await stream.WriteAsync(new byte[] { clientMajor, clientMinor }, 0, 2, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            if (clientMajor == 0 && clientMinor == 0)
                throw new InvalidOperationException("No supported VeNCrypt version.");

            // Step 3: Receive server success/failure (1 byte)
            int serverAccept = stream.ReadByte();
            if (serverAccept != 0)
                throw new InvalidOperationException("VeNCrypt version rejected by server.");

            // Step 4: Depending on version, read subtype list

            if (clientMajor == 0 && clientMinor == 1)
            {
                // Version 0.1: server sends 1-byte count, then subtypes (U8s)
                int subtypeCount = stream.ReadByte();
                if (subtypeCount <= 0)
                    throw new InvalidOperationException("No VeNCrypt subtypes.");

                var subtypes = new List<byte>();
                for (int i = 0; i < subtypeCount; i++)
                {
                    int st = stream.ReadByte();
                    if (st < 0) throw new IOException("Unexpected end of stream.");
                    subtypes.Add((byte)st);
                }

                // Choose preferred subtype from subtypes (choose highest preference)
                byte chosenSubtype = ChoosePreferredSubtype0_1(subtypes);
                if (chosenSubtype == 0)
                    throw new InvalidOperationException("No compatible VeNCrypt subtype found.");

                // Send chosen subtype (1 byte)
                await stream.WriteAsync(new[] { chosenSubtype }, 0, 1, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Continue with chosen subtype
                return await HandleSubtype0_1Async(chosenSubtype, handler, stream, cancellationToken).ConfigureAwait(false);
            }
            else if (clientMajor == 0 && clientMinor == 2)
            {
                // Version 0.2: server sends 1-byte count, then subtypes (U32s)
                int subtypeCount = stream.ReadByte();
                if (subtypeCount <= 0)
                    throw new InvalidOperationException("No VeNCrypt subtypes.");

                var subtypes = new List<uint>();
                for (int i = 0; i < subtypeCount; i++)
                {
                    byte[] subtypeBytes = await ReadExactAsync(stream, 4, cancellationToken).ConfigureAwait(false);
                    uint st = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(subtypeBytes, 0));
                    subtypes.Add(st);
                }

                uint chosenSubtype = ChoosePreferredSubtype0_2(subtypes);
                if (chosenSubtype == 0)
                    throw new InvalidOperationException("No compatible VeNCrypt subtype found.");

                // Send chosen subtype (4 bytes)
                byte[] chosenBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)chosenSubtype));
                await stream.WriteAsync(chosenBytes, 0, 4, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Read 1-byte accept flag
                int acceptFlag = stream.ReadByte();
                if (acceptFlag != 0x01)
                    throw new InvalidOperationException("Server rejected chosen VeNCrypt subtype");

                // Log the chosen subtype for debugging
                var logger = _context.Connection.LoggerFactory.CreateLogger(typeof(VeNCryptAuthenticationSecurityType));
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    // VeNCrypt subtype selection debug logging removed for production
                }
                
                // Continue with chosen subtype
                return await HandleSubtype0_2Async(chosenSubtype, handler, stream, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported VeNCrypt version {clientMajor}.{clientMinor}");
            }
        }

        private byte ChoosePreferredSubtype0_1(List<byte> subtypes)
        {
            // Prefer subtypes in this order for version 0.1:
            byte[] preferred = { 25, 24, 23, 22, 21, 20, 19 };
            foreach (var p in preferred)
                if (subtypes.Contains(p))
                    return p;
            return 0;
        }

        private uint ChoosePreferredSubtype0_2(List<uint> subtypes)
        {
            // Prefer subtypes in this order for version 0.2:
            uint[] preferred = { 262, 261, 260, 259, 258, 257, 256 };
            foreach (var p in preferred)
                if (subtypes.Contains(p))
                    return p;
            return 0;
        }
        
        private string GetSubtypeMeaning(uint subtype)
        {
            return subtype switch
            {
                256 => "Plain (no encryption)",
                257 => "TLSNone (TLS, no auth)",
                258 => "TLSVnc (TLS + VNC auth)",
                259 => "TLSPlain (TLS + plain auth)",
                260 => "X509None (X509, no auth)",
                261 => "X509Vnc (X509 + VNC auth)",
                262 => "X509Plain (X509 + plain auth)",
                _ => $"Unknown subtype {subtype}"
            };
        }

        private async Task<AuthenticationResult> HandleSubtype0_1Async(byte subtype, IAuthenticationHandler handler, Stream stream, CancellationToken cancellationToken)
        {
            switch (subtype)
            {
                case 19: // Plain
                case 22: // TLSPlain
                case 25: // X509Plain
                    return await SendPlainCredentialsAsync(handler, stream, cancellationToken).ConfigureAwait(false);
                // Add TLS or X509 handling here if needed, but for 0.1 these subtypes are rare/obsolete
                default:
                    throw new InvalidOperationException($"Unsupported VeNCrypt 0.1 subtype {subtype}");
            }
        }

        private async Task<AuthenticationResult> HandleSubtype0_2Async(uint subtype, IAuthenticationHandler handler, Stream stream, CancellationToken cancellationToken)
        {
            // If TLS or X509 subtype, wrap stream in SslStream accordingly
            switch (subtype)
            {
                case 256: // Plain
                    return await SendPlainCredentialsAsync(handler, stream, cancellationToken).ConfigureAwait(false);

                case 257: // TLSNone
                case 258: // TLSVnc
                case 259: // TLSPlain
                    stream = await StartTlsAsync(handler, stream, cancellationToken).ConfigureAwait(false);

                    if (subtype == 259) // TLSPlain
                        return await SendPlainCredentialsAsync(handler, stream, cancellationToken).ConfigureAwait(false);
                    else
                        return new AuthenticationResult();

                case 260: // X509None
                case 261: // X509Vnc
                case 262: // X509Plain
                    stream = await StartTlsWithClientCertAsync(handler, stream, cancellationToken).ConfigureAwait(false);

                    if (subtype == 262) // X509Plain
                        return await SendPlainCredentialsAsync(handler, stream, cancellationToken).ConfigureAwait(false);
                    else
                        return new AuthenticationResult();

                default:
                    throw new InvalidOperationException($"Unsupported VeNCrypt 0.2 subtype {subtype}");
            }
        }

        private async Task<Stream> StartTlsAsync(IAuthenticationHandler handler, Stream stream, CancellationToken cancellationToken)
        {
            var sslStream = new SslStream(stream, leaveInnerStreamOpen: true,
                userCertificateValidationCallback: (sender, cert, chain, errors) => true);

            await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions {
                TargetHost = GetTargetHostName(),
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
            }, cancellationToken).ConfigureAwait(false);

            if (!sslStream.IsAuthenticated || !sslStream.IsEncrypted)
                throw new AuthenticationException("TLS authentication failed.");

            return sslStream;
        }

        private async Task<Stream> StartTlsWithClientCertAsync(IAuthenticationHandler handler, Stream stream, CancellationToken cancellationToken)
        {
            try
            {
                var sslStream = new SslStream(stream, leaveInnerStreamOpen: true,
                    userCertificateValidationCallback: (sender, cert, chain, errors) => true);

                // Assume input provides cert path or cert itself; adjust accordingly:
                //var certs = new X509CertificateCollection();
                //certs.Add(new X509Certificate2("c:\\temp\\client.pfx", "")); // example, replace!

                await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions {
                    TargetHost = GetTargetHostName(),
                    ClientCertificates = null, //certs,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                }, cancellationToken).ConfigureAwait(false);

                if (!sslStream.IsAuthenticated || !sslStream.IsEncrypted)
                    throw new AuthenticationException("TLS client cert authentication failed.");

                return sslStream;
            } catch (Exception ex)
            {
                // Handle specific exceptions if needed
                throw new AuthenticationException("TLS client certificate authentication failed.", ex);
            }
        }

        private async Task<AuthenticationResult> SendPlainCredentialsAsync(IAuthenticationHandler handler, Stream stream, CancellationToken cancellationToken)
        {
            var input = await handler.ProvideAuthenticationInputAsync(_context.Connection, this, new CredentialsAuthenticationInputRequest()).ConfigureAwait(false);

            var usernameBytes = Encoding.ASCII.GetBytes(input.Username);
            var passwordBytes = Encoding.ASCII.GetBytes(input.Password);
            
            var logger = _context.Connection.LoggerFactory.CreateLogger(typeof(VeNCryptAuthenticationSecurityType));
            if (logger.IsEnabled(LogLevel.Debug))
            {
                // VeNCrypt credentials debug logging removed for production
            }

            using var ms = new MemoryStream();

            // Write 4-byte username length (big endian)
            ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(usernameBytes.Length)), 0, 4);
            // Write 4-byte password length (big endian)
            ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(passwordBytes.Length)), 0, 4);

            // Write username bytes
            ms.Write(usernameBytes, 0, usernameBytes.Length);
            // Write password bytes
            ms.Write(passwordBytes, 0, passwordBytes.Length);

            byte[] payload = ms.ToArray();
            
            if (logger.IsEnabled(LogLevel.Debug))
            {
                // VeNCrypt payload debug logging removed for production
            }

            await stream.WriteAsync(payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            // Read 4-byte SecurityResult (U32) per RFB specification
            byte[] resultBuf = await ReadExactAsync(stream, 4, cancellationToken).ConfigureAwait(false);
            uint result = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(resultBuf, 0));

            if (result == 0)
            {
                logger.LogInformation("VeNCrypt authentication completed successfully - creating SSL transport");
                ITransport transport = new SSLTransport(stream);
                return new AuthenticationResult(transport, false);
            }
            else
            {
                logger.LogError("VeNCrypt authentication failed with SecurityResult: {ResultCode}", result);

                string errorMessage = "VeNCrypt Plain authentication failed.";
                try
                {
                    byte[] reasonLenBuf = await ReadExactAsync(stream, 4, cancellationToken).ConfigureAwait(false);
                    uint reasonLen = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(reasonLenBuf, 0));

                    if (reasonLen > 0 && reasonLen < 4096)
                    {
                        byte[] reasonBuf = await ReadExactAsync(stream, (int)reasonLen, cancellationToken).ConfigureAwait(false);
                        string reason = Encoding.UTF8.GetString(reasonBuf).Trim();
                        logger.LogError("VeNCrypt server error: '{Reason}'", reason);
                        errorMessage = $"VeNCrypt authentication failed: {reason}";
                    }
                }
                catch (InvalidOperationException) { throw; }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to read VeNCrypt error reason from server");
                }

                throw new InvalidOperationException(errorMessage);
            }
        }


        public Task ReadServerInitExtensionAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Gets the target hostname for TLS certificate validation.
        /// </summary>
        /// <returns>The hostname to use for TLS validation.</returns>
        private string GetTargetHostName()
        {
            // Try to get hostname from TCP transport parameters
            if (_context.Connection.Parameters.TransportParameters is TcpTransportParameters tcpParams)
            {
                return tcpParams.Host;
            }

            // Fallback to generic hostname
            return "vnc-server";
        }

        private async Task<byte[]> ReadExactAsync(Stream stream, int length, CancellationToken cancellationToken)
        {
            var buffer = new byte[length];
            int offset = 0;

            while (offset < length)
            {
                int read = await stream.ReadAsync(buffer, offset, length - offset, cancellationToken);
                if (read == 0)
                    throw new IOException("Unexpected end of stream.");
                offset += read;
            }

            return buffer;
        }

    }
}
