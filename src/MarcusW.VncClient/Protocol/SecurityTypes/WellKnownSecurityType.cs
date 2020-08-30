using System.Diagnostics.CodeAnalysis;

namespace MarcusW.VncClient.Protocol.SecurityTypes
{
    /// <summary>
    /// The well known security types and their IDs.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum WellKnownSecurityType : byte
    {
        Invalid = 0,
        None = 1,
        VncAuthentication = 2,
        RA2 = 5,
        RA2ne = 6,
        Tight = 16,
        Ultra = 17,
        TLS = 18,
        VeNCrypt = 19,
        SASL = 20,
        MD5 = 21,
        XVP = 22,
        SecureTunnel = 23,
        IntegratedSSH = 24
    }
}
