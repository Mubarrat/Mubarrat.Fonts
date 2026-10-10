using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Sfnt;

/// <summary>
/// Represents the SFNT table directory, mapping table tags to their records.
/// </summary>
public sealed record SfntTableDirectory : IReadOnlyDictionary<Tag, SfntTableRecord>, IReadOnlyDictionary<Tag, IFontTableEntry>, IRecord<SfntTableDirectory>
{
    /// <summary>
    /// The SFNT version marker for TrueType outlines.
    /// </summary>
    public const uint SfntVersionTrueType = 0x00010000;

    /// <summary>
    /// The SFNT version marker for CFF outlines.
    /// </summary>
    public const uint SfntVersionCff = 0x4F54544F;

    private readonly Dictionary<Tag, SfntTableRecord> _records;

    /// <summary>
    /// Initializes a new instance of the <see cref="SfntTableDirectory"/> class.
    /// </summary>
    /// <param name="sfntVersion">The SFNT version marker.</param>
    /// <param name="records">The records indexed by their table tags.</param>
    public SfntTableDirectory(uint sfntVersion, Dictionary<Tag, SfntTableRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        SfntVersion = sfntVersion;
        _records = records;
    }

    /// <summary>
    /// Gets the SFNT version marker.
    /// </summary>
    public uint SfntVersion { get; }

    /// <inheritdoc />
    public int Count => _records.Count;

    /// <inheritdoc />
    public IEnumerable<Tag> Keys => _records.Keys;

    /// <inheritdoc />
    public IEnumerable<SfntTableRecord> Values => _records.Values;

    /// <summary>
    /// Gets the table records in this directory.
    /// </summary>
    public IReadOnlyCollection<SfntTableRecord> Records => _records.Values;

    IEnumerable<IFontTableEntry> IReadOnlyDictionary<Tag, IFontTableEntry>.Values => (IEnumerable<IFontTableEntry>)Values;

    IFontTableEntry IReadOnlyDictionary<Tag, IFontTableEntry>.this[Tag key] => _records[key];

    /// <inheritdoc />
    public SfntTableRecord this[Tag key] => _records[key];

    /// <inheritdoc />
    public bool ContainsKey(Tag key) => _records.ContainsKey(key);

    /// <inheritdoc />
    public bool TryGetValue(Tag key, [MaybeNullWhen(false)] out SfntTableRecord value) => _records.TryGetValue(key, out value);

    bool IReadOnlyDictionary<Tag, IFontTableEntry>.TryGetValue(Tag key, [MaybeNullWhen(false)] out IFontTableEntry value)
    {
        if (_records.TryGetValue(key, out SfntTableRecord record))
        {
            value = record;
            return true;
        }
        value = null;
        return false;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<Tag, SfntTableRecord>> GetEnumerator() => _records.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<KeyValuePair<Tag, IFontTableEntry>> IEnumerable<KeyValuePair<Tag, IFontTableEntry>>.GetEnumerator() => _records.Select(static pair => new KeyValuePair<Tag, IFontTableEntry>(pair.Key, pair.Value)).GetEnumerator();

    /// <summary>
    /// Represents the 12-byte SFNT offset table header.
    /// </summary>
    /// <remarks>
    /// <see cref="SearchRange"/>, <see cref="EntrySelector"/>, and
    /// <see cref="RangeShift"/> are precomputed search hints derived from
    /// <see cref="NumTables"/>. They are required in the serialized header
    /// but are not used for dictionary-based lookup.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>
        /// The SFNT version marker.
        /// </summary>
        public uint SfntVersion;

        /// <summary>
        /// The number of table records.
        /// </summary>
        public ushort NumTables;

        /// <summary>
        /// The maximum number of bytes searched by the original binary-search algorithm.
        /// </summary>
        public ushort SearchRange;

        /// <summary>
        /// The log base two of the largest power of two not exceeding the table count.
        /// </summary>
        public ushort EntrySelector;

        /// <summary>
        /// The number of bytes beyond the largest power-of-two search range.
        /// </summary>
        public ushort RangeShift;

        /// <inheritdoc />
        public static Header ReverseEndianness(Header value) => new()
        {
            SfntVersion = BinaryPrimitives.ReverseEndianness(value.SfntVersion),
            NumTables = BinaryPrimitives.ReverseEndianness(value.NumTables),
            SearchRange = BinaryPrimitives.ReverseEndianness(value.SearchRange),
            EntrySelector = BinaryPrimitives.ReverseEndianness(value.EntrySelector),
            RangeShift = BinaryPrimitives.ReverseEndianness(value.RangeShift),
        };
    }

    /// <summary>
    /// Parses the SFNT offset table and its table records from the current cursor position.
    /// </summary>
    static SfntTableDirectory IRecord<SfntTableDirectory>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new SfntTableDirectory(header.SfntVersion, cursor.ReadBigEndianStructArray<SfntTableRecord>(header.NumTables).ToDictionary(static record => record.Tag));
    }
}
