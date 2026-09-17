using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Represents a VNC color map for indexed color pixel formats.
    /// </summary>
    public class ColorMap : IReadOnlyList<ColorMapEntry>
    {
        private readonly ColorMapEntry[] _entries;

        /// <summary>
        /// Gets an empty color map.
        /// </summary>
        public static readonly ColorMap Empty = new(Array.Empty<ColorMapEntry>());

        /// <summary>
        /// Gets the number of entries in the color map.
        /// </summary>
        public int Count => _entries.Length;

        /// <summary>
        /// Gets the color map entry at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the entry to get.</param>
        /// <returns>The color map entry at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
        public ColorMapEntry this[int index]
        {
            get
            {
                if (index < 0 || index >= _entries.Length)
                    throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range.");
                return _entries[index];
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorMap"/> class.
        /// </summary>
        /// <param name="entries">The color map entries.</param>
        /// <exception cref="ArgumentNullException">Thrown when entries is null.</exception>
        public ColorMap(IReadOnlyList<ColorMapEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            _entries = new ColorMapEntry[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                _entries[i] = entries[i];
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorMap"/> class.
        /// </summary>
        /// <param name="entries">The color map entries array.</param>
        /// <exception cref="ArgumentNullException">Thrown when entries is null.</exception>
        internal ColorMap(ColorMapEntry[] entries)
        {
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        /// <summary>
        /// Gets the color for the specified index, with bounds checking.
        /// </summary>
        /// <param name="index">The color index.</param>
        /// <returns>The color at the specified index, or black if the index is out of bounds.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ColorMapEntry GetEntryOrDefault(int index)
        {
            if (index < 0 || index >= _entries.Length)
                return new ColorMapEntry(0, 0, 0); // Return black for out-of-bounds indices
            return _entries[index];
        }

        /// <summary>
        /// Converts an indexed pixel value to a true color pixel using this color map.
        /// </summary>
        /// <param name="pixelValue">The indexed pixel value.</param>
        /// <param name="targetFormat">The target pixel format.</param>
        /// <returns>The converted true color pixel.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ConvertIndexedPixel(uint pixelValue, in PixelFormat targetFormat)
        {
            var entry = GetEntryOrDefault((int)pixelValue);
            return entry.ToPixel(targetFormat);
        }

        /// <summary>
        /// Creates a copy of this color map with updated entries.
        /// </summary>
        /// <param name="firstColor">The index of the first color to update.</param>
        /// <param name="newEntries">The new color entries.</param>
        /// <returns>A new color map with the updated entries.</returns>
        public ColorMap WithUpdatedEntries(ushort firstColor, IReadOnlyList<ColorMapEntry> newEntries)
        {
            if (newEntries == null)
                throw new ArgumentNullException(nameof(newEntries));

            // Determine the required size for the new color map
            int requiredSize = Math.Max(_entries.Length, firstColor + newEntries.Count);
            var newColorMap = new ColorMapEntry[requiredSize];

            // Copy existing entries
            Array.Copy(_entries, 0, newColorMap, 0, _entries.Length);

            // Update with new entries
            for (int i = 0; i < newEntries.Count; i++)
            {
                newColorMap[firstColor + i] = newEntries[i];
            }

            return new ColorMap(newColorMap);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the color map entries.
        /// </summary>
        /// <returns>An enumerator for the color map entries.</returns>
        public IEnumerator<ColorMapEntry> GetEnumerator()
        {
            foreach (var entry in _entries)
                yield return entry;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public override string ToString() => $"ColorMap with {Count} entries";
    }
}
