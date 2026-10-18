using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Sfnt;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// The 'ankr' table — AAT anchor points
//
// Self-contained apart from the font's glyph count, which the lookup table needs to
// enumerate its covered glyphs.
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>ankr</c> table: named anchor points in the coordinate space of specific glyphs, independent of the control points used to render them.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Anchor points are consumed by the <c>kerx</c> table, which uses them as reference positions for contextual kerning rather than as outline data.</description></item>
/// <item><description>Each accented glyph's anchor points live in a variable-length entry in the glyph data region; a standard AAT lookup table maps glyph indices to the entry's offset within that region.</description></item>
/// <item><description>Zero is a valid offset into the glyph data region, so glyphs without anchor points must not appear in the lookup at all; absence from the lookup is what signals "no anchor points".</description></item>
/// <item><description>Anchor-point coordinates are signed 16-bit values in glyph-design units, matching the <c>glyf</c> coordinate space rather than the advance-width space.</description></item>
/// <item><description>All multi-byte reads use the sfnt default of big-endian.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html"><c>ankr</c> table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AnkrGlyphEntry"/>
/// <seealso cref="AnchorPoint"/>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
public sealed record AnkrTable : IFontTable<AnkrTable>, IRecord<AnkrTable>
{
    /// <summary>The only version defined by the current specification.</summary>
    /// <value>The constant <c>0</c>.</value>
    /// <seealso cref="Version"/>
    public const ushort CurrentVersion = 0;

    /// <summary>The only flags value defined by the current specification.</summary>
    /// <value>The constant <c>0</c>. The parser preserves non-zero flag words rather than rejecting them; a diagnostics analyzer is the appropriate place to flag them.</value>
    /// <seealso cref="Flags"/>
    public const ushort CurrentFlags = 0;

    /// <summary>The lookup-table offset the specification currently mandates.</summary>
    /// <value>The constant <c>0x0000000C</c>.</value>
    /// <remarks>The value is informational. The parser honors whatever offset the header declares, so a future specification revision with a different offset still parses correctly.</remarks>
    /// <seealso cref="Header.LookupTableOffset"/>
    public const uint ExpectedLookupTableOffset = 0x0000000C;

    /// <inheritdoc/>
    public static Tag Tag => "ankr";

    /// <summary>Gets the table version.</summary>
    /// <value>The version read from the header. Always <see cref="CurrentVersion"/>; the parser rejects any other value.</value>
    /// <seealso cref="CurrentVersion"/>
    public ushort Version { get; init; }

    /// <summary>Gets the raw flags word from the header.</summary>
    /// <value>The <c>flags</c> field as stored. The specification defines it as all-zero; the parser preserves the value for round-tripping and diagnostics.</value>
    /// <seealso cref="CurrentFlags"/>
    public ushort Flags { get; init; }

    /// <summary>Gets the table-relative byte offset of the glyph data region.</summary>
    /// <value>The byte position, from the start of the <c>ankr</c> table, of the first <see cref="AnkrGlyphEntry"/>.</value>
    /// <remarks>Values in <see cref="GlyphToEntryLookup"/> are offsets into this region, so a glyph's entry begins at <c>GlyphDataTableOffset + lookupValue</c>.</remarks>
    /// <seealso cref="GlyphToEntryLookup"/>
    /// <seealso cref="AnkrGlyphEntry"/>
    public uint GlyphDataTableOffset { get; init; }

    /// <summary>Gets the lookup that maps glyph indices to entry offsets.</summary>
    /// <value>A standard AAT lookup whose value for glyph <c>i</c> is a byte offset into the glyph data region. Glyphs without anchor points are not covered by the lookup.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Coverage is determined by presence in the lookup, not by a sentinel value, because zero is a valid offset into the glyph data region.</description></item>
    /// <item><description>Two glyphs may share one lookup value; the parser deduplicates by offset so their entries are stored once in <see cref="EntriesByOffset"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="EntriesByOffset"/>
    /// <seealso cref="GetAnchorPoints(int)"/>
    /// <seealso cref="LookupTable"/>
    public required LookupTable GlyphToEntryLookup { get; init; }

    /// <summary>Gets the glyph data entries referenced by the lookup, keyed by their offset within the glyph data region.</summary>
    /// <value>One <see cref="AnchorPoint"/> array per distinct referenced offset. Multiple glyphs may share one entry.</value>
    /// <seealso cref="AnkrGlyphEntry"/>
    /// <seealso cref="GetAnchorPoints(int)"/>
    public required IReadOnlyDictionary<ushort, AnchorPoint[]> EntriesByOffset { get; init; }

    /// <summary>Gets the anchor points for a glyph.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The anchor-point array for the glyph, or <see langword="null"/> when the glyph has no anchor points.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>A <see langword="null"/> result means the glyph does not appear in the lookup — either it has no anchor points at all, or it sits outside the lookup's covered range.</description></item>
    /// <item><description>The method returns <see langword="null"/> rather than throwing when a malformed table contains a lookup value that is not a valid entry offset.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="TryGetAnchorPoints(int, out AnchorPoint[])"/>
    /// <seealso cref="EntriesByOffset"/>
    public AnchorPoint[]? GetAnchorPoints(int glyphId) =>
        TryGetAnchorPoints(glyphId, out var points) ? points : null;

    /// <summary>Attempts to get the anchor points for a glyph.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <param name="points">When this method returns <see langword="true"/>, contains the anchor-point array; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the glyph is covered by the lookup and its entry is present; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Distinguishes an absent glyph from a covered glyph whose entry happens to be an empty array, which the specification does not define but a malformed table could produce.</remarks>
    /// <seealso cref="GetAnchorPoints(int)"/>
    /// <seealso cref="GlyphToEntryLookup"/>
    public bool TryGetAnchorPoints(int glyphId, out AnchorPoint[]? points)
    {
        if (!GlyphToEntryLookup.TryGetValue(glyphId, out uint raw) || raw > ushort.MaxValue)
        {
            points = null;
            return false;
        }

        return EntriesByOffset.TryGetValue((ushort)raw, out points);
    }

    static AnkrTable IRecord<AnkrTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
        {
            throw new InvalidDataException(
                $"'ankr'.version is {header.Version}, expected {CurrentVersion}.");
        }

        int numGlyphs = context is SfntFontFace face
            ? face.GetTable<MaxpTable>().NumGlyphs
            : throw new InvalidDataException(
                "'ankr' requires an SFNT font face context to obtain the glyph count.");

        LookupTable lookup = cursor.Source.ParseRecordAt<LookupTable>(
            header.LookupTableOffset, new LookupTable.Context(numGlyphs));

        var distinctOffsets = new HashSet<ushort>();
        for (int glyph = 0; glyph < numGlyphs; glyph++)
        {
            if (lookup.TryGetValue(glyph, out uint raw) && raw <= ushort.MaxValue)
                distinctOffsets.Add((ushort)raw);
        }

        var entriesByOffset = new Dictionary<ushort, AnchorPoint[]>(distinctOffsets.Count);
        foreach (ushort offset in distinctOffsets)
        {
            long entryOffset = header.GlyphDataTableOffset + offset;
            entriesByOffset[offset] = cursor.Source.ParseRecordAt<AnkrGlyphEntry>(entryOffset).Points;
        }

        return new()
        {
            Version = header.Version,
            Flags = header.Flags,
            GlyphDataTableOffset = header.GlyphDataTableOffset,
            GlyphToEntryLookup = lookup,
            EntriesByOffset = entriesByOffset,
        };
    }

    /// <summary>The 12-byte <c>ankr</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The first two fields are <c>UInt16</c> and the last two are <c>UInt32</c>, so the reversal is field-by-field rather than a single word-span pass.</description></item>
    /// <item><description>Both offsets are measured from the start of the <c>ankr</c> table.</description></item>
    /// <item><description>Consumed at parse time and not retained on <see cref="AnkrTable"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AnkrTable"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0.</summary>
        /// <value>The constant <see cref="CurrentVersion"/>.</value>
        /// <seealso cref="CurrentVersion"/>
        /// <seealso cref="AnkrTable.Version"/>
        public ushort Version;               // +0

        /// <summary>The raw flags word at byte offset 2.</summary>
        /// <value>The on-disk <c>flags</c> field; defined as zero, preserved by the parser for round-tripping and diagnostics.</value>
        /// <seealso cref="CurrentFlags"/>
        /// <seealso cref="AnkrTable.Flags"/>
        public ushort Flags;                 // +2

        /// <summary>The table-relative byte offset of the lookup table at byte offset 4.</summary>
        /// <value>The position of the <see cref="LookupTable"/> that maps glyph indices to entry offsets. Currently always <see cref="ExpectedLookupTableOffset"/>; the parser honors whatever value is present.</value>
        /// <seealso cref="ExpectedLookupTableOffset"/>
        /// <seealso cref="AnkrTable.GlyphToEntryLookup"/>
        public uint LookupTableOffset;       // +4

        /// <summary>The table-relative byte offset of the glyph data region at byte offset 8.</summary>
        /// <value>The position of the first <see cref="AnkrGlyphEntry"/>.</value>
        /// <seealso cref="AnkrTable.GlyphDataTableOffset"/>
        public uint GlyphDataTableOffset;    // +8

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>The struct mixes 16-bit and 32-bit fields, so the reversal is field-by-field rather than a single word-span pass.</remarks>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(value.Version),
            Flags = BinaryPrimitives.ReverseEndianness(value.Flags),
            LookupTableOffset = BinaryPrimitives.ReverseEndianness(value.LookupTableOffset),
            GlyphDataTableOffset = BinaryPrimitives.ReverseEndianness(value.GlyphDataTableOffset),
        };
    }
}

/// <summary>One glyph's anchor-point entry: a 32-bit count followed by that many <see cref="AnchorPoint"/> records.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The entry's on-disk size is <c>4 + count × 4</c> bytes; the count is self-describing, so no context is required to parse an entry.</description></item>
/// <item><description>Entries are addressed by a byte offset that the enclosing <see cref="LookupTable"/> supplies, so this type is constructed through <see cref="Source.ParseRecordAt{T}(long, object?)"/> rather than as a contiguous array.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">glyph data entry</see> section of the <c>ankr</c> table description.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AnkrTable"/>
/// <seealso cref="AnchorPoint"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
public sealed record AnkrGlyphEntry : IRecord<AnkrGlyphEntry>
{
    /// <summary>Gets the anchor points for this glyph, in the order they appear in the table.</summary>
    /// <value>One <see cref="AnchorPoint"/> per declared anchor point. The array length equals the on-disk <c>numPoints</c> field.</value>
    /// <remarks>Anchor-point order is meaningful for consumers that reference points by index, such as a <c>kerx</c> subtable that names a specific anchor for each contextual rule.</remarks>
    /// <seealso cref="AnchorPoint"/>
    public required AnchorPoint[] Points { get; init; }

    /// <summary>Gets the number of anchor points in the entry.</summary>
    /// <value>The length of <see cref="Points"/>.</value>
    /// <seealso cref="Points"/>
    public int Count => Points.Length;

    static AnkrGlyphEntry IRecord<AnkrGlyphEntry>.Parse(ref Cursor cursor, object? context)
    {
        uint count = cursor.ReadUInt32();
        if (count > int.MaxValue)
        {
            throw new InvalidDataException(
                $"'ankr' glyph entry declares {count} anchor points, which exceeds the supported maximum.");
        }

        return new()
        {
            Points = cursor.ReadEndianReversibleStructArray<AnchorPoint>((int)count),
        };
    }
}

/// <summary>A single anchor point: a signed 16-bit x-coordinate and a signed 16-bit y-coordinate in glyph-design units.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The coordinates match the <c>glyf</c> outline coordinate space, not the advance-width space; a consumer that needs to combine them with advance widths must apply the same transformation the outline uses.</description></item>
/// <item><description>Fixed-layout record, 4 bytes on disk. Implements <see cref="IEndianReversibleStruct{T}"/> so a glyph entry's point array is read in one bulk call.</description></item>
/// <item><description>Both fields are <c>Int16</c>, so the reversal is a single word-span pass over the struct reinterpreted as two 16-bit words.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AnkrGlyphEntry"/>
/// <seealso cref="AnkrTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AnchorPoint : IEndianReversibleStruct<AnchorPoint>
{
    /// <summary>The x-coordinate at byte offset 0.</summary>
    /// <value>A signed 16-bit value in glyph-design units.</value>
    public short X;  // +0

    /// <summary>The y-coordinate at byte offset 2.</summary>
    /// <value>A signed 16-bit value in glyph-design units.</value>
    public short Y;  // +2

    /// <summary>Reverses the byte order of both 16-bit fields in one pass.</summary>
    /// <param name="value">The value whose fields are to be reversed.</param>
    /// <returns>A new anchor point with both fields reversed.</returns>
    /// <remarks>The struct is entirely <c>Int16</c> fields, so it is reinterpreted as a span of two 16-bit words and reversed with a single <see cref="BinaryPrimitives.ReverseEndianness(ReadOnlySpan{short}, Span{short})"/> call.</remarks>
    public static AnchorPoint ReverseEndianness(AnchorPoint value)
    {
        Span<short> words = MemoryMarshal.CreateSpan(ref value.X, 2);
        BinaryPrimitives.ReverseEndianness(words, words);
        return value;
    }
}
