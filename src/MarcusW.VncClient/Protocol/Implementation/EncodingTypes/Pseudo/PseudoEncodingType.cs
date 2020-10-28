using System.IO;
using MarcusW.VncClient.Protocol.EncodingTypes;

namespace MarcusW.VncClient.Protocol.Implementation.EncodingTypes.Pseudo
{
    /// <summary>
    /// Base class for <see cref="IPseudoEncodingType"/> implementations.
    /// </summary>
    public abstract class PseudoEncodingType : IPseudoEncodingType
    {
        /// <inheritdoc />
        public abstract int Id { get; }

        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public virtual int Priority => int.MinValue; // Doesn't really matter for pseudo encodings.

        /// <inheritdoc />
        public abstract bool GetsConfirmed { get; }

        /// <inheritdoc />
        public abstract void ReadPseudoEncoding(Stream transportStream, Rectangle rectangle);
    }
}
