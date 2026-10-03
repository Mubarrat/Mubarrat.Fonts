using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType;

/// <summary>
/// The sfnt table directory: maps 4-byte tags to <see cref="TableRecord"/> entries describing
/// where each table lives in the font stream.
/// </summary>
public sealed record TableDirectory : IRecord<TableDirectory>
{
    /// <summary>TrueType outlines (0x00010000).</summary>
    public const uint SfntVersionTrueType = 0x00010000;

    /// <summary>CFF outlines ("OTTO").</summary>
    public const uint SfntVersionCff = 0x4F54544F;

    private readonly Dictionary<Tag, TableRecord> _records;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableDirectory"/> class.
    /// </summary>
    /// <param name="sfntVersion">The sfnt version marker of the font.</param>
    /// <param name="records">The table records in the directory.</param>
    public TableDirectory(uint sfntVersion, Dictionary<Tag, TableRecord> records)
    {
        SfntVersion = sfntVersion;
        _records = records;
    }

    /// <summary>Gets the sfnt version marker of the font.</summary>
    public uint SfntVersion { get; }

    /// <summary>Gets the number of tables in the directory.</summary>
    public int Count => _records.Count;

    /// <summary>Gets all table tags present in the directory.</summary>
    public IReadOnlyCollection<Tag> Tags => _records.Keys;

    /// <summary>Gets all table records in the directory.</summary>
    public IReadOnlyCollection<TableRecord> Records => _records.Values;

    /// <summary>Returns the record for <paramref name="tag"/>, or throws if the table is absent.</summary>
    /// <exception cref="InvalidDataException">No table with the given tag is present.</exception>
    public TableRecord GetRequired(Tag tag) =>
        _records.TryGetValue(tag, out var record)
            ? record
            : throw new InvalidDataException($"Font does not contain a '{tag}' table.");

    /// <summary>Attempts to get the record for <paramref name="tag"/>.</summary>
    public bool TryGet(Tag tag, out TableRecord record) => _records.TryGetValue(tag, out record);

    /// <summary>Returns true if a table with the given tag is present.</summary>
    public bool Contains(Tag tag) => _records.ContainsKey(tag);

    /// <summary>
    /// The 12-byte sfnt offset table header. Blittable, no padding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SearchRange"/>, <see cref="EntrySelector"/>, and <see cref="RangeShift"/>
    /// are a precomputed binary-search hint from the original Apple TrueType design. They
    /// are fully derivable from <see cref="NumTables"/>:
    /// </para>
    /// <code>
    /// p             = 2 ^ floor(log2(numTables))   // largest power of 2 ≤ numTables
    /// searchRange   = p * 16                        // 16 = sizeof(TableRecord)
    /// entrySelector = floor(log2(numTables))
    /// rangeShift    = (numTables - p) * 16
    /// </code>
    /// <para>
    /// They are required to be present on disk but play no role in lookup here: records
    /// are keyed by <see cref="Tag"/> in a <see cref="Dictionary{TKey, TValue}"/>, which
    /// is O(1) and does not consult them. They are read to keep the struct bit-accurate
    /// and to advance the cursor, but are not surfaced on <see cref="TableDirectory"/>.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>
        /// The sfnt version marker of the font. Either <see cref="SfntVersionTrueType"/> or <see cref="SfntVersionCff"/>.
        /// </summary>
        public uint SfntVersion;     // +0
        /// <summary>
        /// The number of tables in the directory. Must be nonzero.
        /// </summary>
        public ushort NumTables;     // +4
        /// <summary>
        /// The maximum number of bytes that can be searched in a binary search of the table records.
        /// </summary>
        public ushort SearchRange;   // +6
        /// <summary>
        /// The log base 2 of the maximum number of table records that can be searched in a binary search.
        /// </summary>
        public ushort EntrySelector; // +8
        /// <summary>
        /// The number of bytes that must be skipped to reach the first table record in a binary search.
        /// </summary>
        public ushort RangeShift;    // +10

        /// <inheritdoc/>
        public static Header ReverseEndianness(Header v) => new()
        {
            SfntVersion = BinaryPrimitives.ReverseEndianness(v.SfntVersion),
            NumTables = BinaryPrimitives.ReverseEndianness(v.NumTables),
            SearchRange = BinaryPrimitives.ReverseEndianness(v.SearchRange),
            EntrySelector = BinaryPrimitives.ReverseEndianness(v.EntrySelector),
            RangeShift = BinaryPrimitives.ReverseEndianness(v.RangeShift),
        };
    }

    /// <summary>
    /// Reads the sfnt offset table and the table directory that follows it, starting at
    /// the current position of <paramref name="cursor"/>.
    /// </summary>
    static TableDirectory IRecord<TableDirectory>.Parse(ref Cursor cursor, object? context)
    {
        Header h = cursor.ReadBigEndianStruct<Header>();
        return new TableDirectory(
            h.SfntVersion,
            cursor.ReadBigEndianStructArray<TableRecord>(h.NumTables).ToDictionary(static r => r.Tag, static r => r));
    }
}
