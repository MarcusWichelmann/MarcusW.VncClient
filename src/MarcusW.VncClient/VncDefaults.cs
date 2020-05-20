using System.Collections.Generic;
using MarcusW.VncClient.Protocol.Encodings;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Provides default values for VNC Client options.
    /// </summary>
    public static class VncDefaults
    {
        /// <summary>
        /// Builds a collection with all RFB encodings that are officially supported by this library.
        /// Feel free to extend the returned enumerable with custom encodings.
        /// </summary>
        /// <returns>The encoding collection.</returns>
        public static IEnumerable<IEncoding> GetEncodingsCollection()
        {
            // TODO: Add encodings
            yield break;
        }
    }
}
