using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// ankr — Anchor Point Table (Apple Advanced Typography)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>ankr</c> table: named anchor points inside a glyph's coordinate space, used with <c>kerx</c> to position glyphs relative to one another independent of the control points that draw them.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is an Apple Advanced Typography (AAT) table and is not part of OpenType. Apple documents it in the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual, <c>ankr</c> table</see>, which states that it is supported on OS X 10.9 and later and on iOS 7.0 and later.</description></item>
/// <item><description>An anchor point is a point in the glyph's coordinate space that is independent of the outlines: it can sit anywhere the designer chooses, so two glyphs can be aligned by matching an anchor point of one to an anchor point of the other.</description></item>
/// <item><description>The twelve-byte <see cref="Header"/> declares two offsets from the start of the table: the lookup table and the glyph data table. The version is a 16-bit field that is set to zero, and the lookup table conventionally begins at <see cref="DefaultLookupTableOffset"/>, which is <c>0x0000000C</c> — immediately after the header, because the header is twelve bytes.</description></item>
/// <item><description>The lookup value for a glyph is a 16-bit offset from the start of the glyph data table to that glyph's anchor point entry, which is a 32-bit point count followed by that many x and y coordinate pairs. Because zero is a valid offset into the glyph data table, a glyph that has no anchor points must not appear in the lookup table at all.</description></item>
/// <item><description>The 32-bit count at the start of a glyph data entry is read as an unsigned value and bounds-checked against the table; the coordinate pairs are signed 16-bit values in font design units.</description></item>
/// <item><description>A format 0 lookup is an untrimmed array with no declared length, so supply a <see cref="FontFace"/> as the parse context when the table may use it: the glyph count is then taken from <c>maxp.numGlyphs</c>, which is what the shared lookup dispatcher expects.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AnchorPoint"/>
/// <seealso cref="GlyphAnchorPoints"/>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record AnkrTable : IFontTable<AnkrTable>
{
    /// <summary>Gets the AAT table tag <c>ankr</c>.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
    public static Tag Tag => "ankr";

    /// <summary>Gets the table version, which the specification requires to be zero.</summary>
    /// <remarks>Unlike the other Apple Advanced Typography tables, <c>ankr</c> declares its version as a 16-bit field rather than as a 16.16 fixed-point number.</remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the flags field, which is currently unused and set to zero.</summary>
    /// <remarks>The field is preserved rather than validated; a consumer round-tripping the table needs it.</remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
    public ushort Flags { get; init; }

    /// <summary>Gets the offset of the lookup table from the start of the table.</summary>
    /// <remarks>The specification notes that this value is currently always <see cref="DefaultLookupTableOffset"/>; the declared value is preserved rather than assumed.</remarks>
    /// <seealso cref="DefaultLookupTableOffset"/>
    /// <seealso cref="Lookup"/>
    public uint LookupTableOffset { get; init; }

    /// <summary>Gets the offset of the glyph data table from the start of the table.</summary>
    /// <remarks>Lookup values are offsets from the start of this region, not from the start of the <c>ankr</c> table.</remarks>
    /// <seealso cref="GetAnchorPoints(int)"/>
    public uint GlyphDataTableOffset { get; init; }

    /// <summary>Gets the lookup table that maps glyph indices onto offsets to glyph data entries, relative to the start of the glyph data table.</summary>
    /// <seealso cref="LookupTable"/>
    /// <seealso cref="GetAnchorPoints(int)"/>
    public LookupTable Lookup { get; init; } = null!;

    /// <summary>Gets the anchor points of every glyph the lookup table covers, in ascending glyph order.</summary>
    /// <remarks>The <c>0xFFFF</c> entry that a binary-searched lookup conventionally ends with is omitted, because no glyph data entry can be attributed to it.</remarks>
    /// <seealso cref="GetAnchorPoints(int)"/>
    /// <seealso cref="Count"/>
    public IReadOnlyList<GlyphAnchorPoints> Glyphs { get; init; } = [];

    /// <summary>Gets the number of glyphs that have anchor points.</summary>
    /// <seealso cref="Glyphs"/>
    public int Count => Glyphs.Count;

    /// <summary>Gets the anchor points of a glyph.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <returns>The glyph's anchor points, or an empty list when the table gives the glyph none.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>An empty list has two possible causes that the parsed table cannot distinguish: the glyph has no entry in the lookup table, or its entry declares a point count of zero.</description></item>
    /// <item><description>Use <see cref="TryGetAnchorPoints(int, out IReadOnlyList{AnchorPoint})"/> to tell the two apart.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Glyphs"/>
    /// <seealso cref="AnchorPoint"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
    public IReadOnlyList<AnchorPoint> GetAnchorPoints(int glyphIndex)
    {
        if (PointsByGlyph.TryGetValue(glyphIndex, out IReadOnlyList<AnchorPoint>? points))
            return points;

        return [];
    }

    /// <summary>Tries to get the anchor points of a glyph.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <param name="anchorPoints">When this method returns <see langword="true"/>, contains the glyph's anchor points; otherwise, an empty list.</param>
    /// <returns><see langword="true"/> when the glyph has an entry in the lookup table; otherwise, <see langword="false"/>.</returns>
    /// <remarks>The glyph is reported as having anchor points even when its entry declares a count of zero, because the entry exists and the glyph data table holds data for it.</remarks>
    /// <seealso cref="GetAnchorPoints(int)"/>
    public bool TryGetAnchorPoints(int glyphIndex, out IReadOnlyList<AnchorPoint> anchorPoints)
    {
        if (PointsByGlyph.TryGetValue(glyphIndex, out IReadOnlyList<AnchorPoint>? points))
        {
            anchorPoints = points;
            return true;
        }

        anchorPoints = [];
        return false;
    }

    /// <summary>The offset of the lookup table the specification currently requires, <c>0x0000000C</c>: twelve bytes, immediately after the header.</summary>
    /// <seealso cref="LookupTableOffset"/>
    public const uint DefaultLookupTableOffset = 0x0000000C;

    /// <summary>Size, in bytes, of the fixed-layout <see cref="Header"/>.</summary>
    /// <remarks>The lookup table conventionally begins at this offset, which is why <see cref="DefaultLookupTableOffset"/> is twelve.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="DefaultLookupTableOffset"/>
    public const int HeaderSize = 12;

    /// <summary>Gets the anchor points of a glyph, indexed by glyph index for constant-time lookup.</summary>
    /// <remarks>Built alongside <see cref="Glyphs"/> so that a query does not have to scan the list.</remarks>
    /// <seealso cref="GetAnchorPoints(int)"/>
    private IReadOnlyDictionary<int, IReadOnlyList<AnchorPoint>> PointsByGlyph { get; init; } =
        new Dictionary<int, IReadOnlyList<AnchorPoint>>();

    /// <summary>The twelve-byte fixed-layout <c>ankr</c> table header.</summary>
    /// <remarks>Fields are stored in big-endian order at the offsets defined by the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html"><c>ankr</c> table specification</see>.</remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0, set to zero.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
        public ushort Version;              // +0

        /// <summary>The flags field at byte offset 2, currently unused and set to zero.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
        public ushort Flags;                // +2

        /// <summary>The offset of the lookup table from the start of the table at byte offset 4.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
        public uint LookupTableOffset;      // +4

        /// <summary>The offset of the glyph data table from the start of the table at byte offset 8.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
        public uint GlyphDataTableOffset;   // +8

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
            LookupTableOffset = BinaryPrimitives.ReverseEndianness(v.LookupTableOffset),
            GlyphDataTableOffset = BinaryPrimitives.ReverseEndianness(v.GlyphDataTableOffset),
        };
    }

    /// <summary>One anchor point of a glyph.</summary>
    /// <remarks>The coordinates are signed 16-bit values in the font's design units and are independent of the glyph's control points, so the point need not coincide with any outline point.</remarks>
    /// <seealso cref="AnkrTable.Glyphs"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct AnchorPoint : IEndianReversibleStruct<AnchorPoint>
    {
        /// <summary>The horizontal coordinate at byte offset 0, in font design units.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
        public short X; // +0

        /// <summary>The vertical coordinate at byte offset 2, in font design units.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
        public short Y; // +2

        /// <summary>Reverses the byte order of both fields in an <see cref="AnchorPoint"/>.</summary>
        public static AnchorPoint ReverseEndianness(AnchorPoint v) => new()
        {
            X = BinaryPrimitives.ReverseEndianness(v.X),
            Y = BinaryPrimitives.ReverseEndianness(v.Y),
        };
    }

    /// <summary>The anchor points of one glyph, as stored in the table.</summary>
    /// <remarks>Anchor points are ordered as the font stored them; the order is significant to <c>kerx</c>, which addresses them by index.</remarks>
    /// <seealso cref="AnkrTable.Glyphs"/>
    /// <seealso cref="AnchorPoint"/>
    public sealed record GlyphAnchorPoints
    {
        /// <summary>Gets the glyph index the record describes.</summary>
        public int GlyphIndex { get; init; }

        /// <summary>Gets the glyph's anchor points, in the order the font stored them.</summary>
        /// <seealso cref="AnchorPoint"/>
        public IReadOnlyList<AnchorPoint> Points { get; init; } = [];
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the <c>ankr</c> table.</param>
    /// <param name="context">A <see cref="FontFace"/> so that a format 0 lookup can be sized by <c>maxp.numGlyphs</c>, or <see langword="null"/>; a plain <see cref="int"/> glyph count is also accepted.</param>
    /// <returns>The parsed <c>ankr</c> table.</returns>
    /// <exception cref="InvalidDataException">The version is not 0, or a glyph data entry declares an impossible anchor point count.</exception>
    /// <exception cref="EndOfStreamException">The header, the lookup table, or a glyph data entry extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The table's version must be zero, as the specification requires.</description></item>
    /// <item><description>The lookup table is read at its declared offset rather than assumed to follow the header, so that a font deviating from the conventional <c>0x0000000C</c> still parses.</description></item>
    /// <item><description>A lookup value of zero is valid for this table, because the values are offsets from the start of the glyph data table; such a glyph is read like any other.</description></item>
    /// <item><description>A format 0 lookup declares no length of its own, so the glyph count reaches it through <paramref name="context"/>; with no usable context it is read to the end of the table data, which is wrong here because the glyph data table follows the lookup.</description></item>
    /// </list>
    /// </remarks>
    static AnkrTable IRecord<AnkrTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version != 0)
            throw new InvalidDataException($"'ankr'.version is {header.Version}, expected 0.");

        Cursor lookupCursor = cursor.Source.CreateOffsetCursor(header.LookupTableOffset);
        LookupTable lookup = LookupTable.Parse(ref lookupCursor, GetGlyphCountContext(context));

        var entries = new List<GlyphAnchorPoints>();
        var records = new Dictionary<uint, IReadOnlyList<AnchorPoint>>();

        foreach (int glyph in EnumerateCoveredGlyphs(lookup))
        {
            if (glyph == BinSrchHeader.EndOfSearch) continue;
            if (!lookup.TryGetValue(glyph, out uint offset)) continue;

            if (!records.TryGetValue(offset, out IReadOnlyList<AnchorPoint>? points))
            {
                points = ReadAnchorPoints(cursor.Source, (long)header.GlyphDataTableOffset + offset);
                records[offset] = points;
            }

            entries.Add(new GlyphAnchorPoints { GlyphIndex = glyph, Points = points });
        }

        var byGlyph = new Dictionary<int, IReadOnlyList<AnchorPoint>>(entries.Count);
        foreach (GlyphAnchorPoints glyph in entries)
            byGlyph[glyph.GlyphIndex] = glyph.Points;

        return new AnkrTable
        {
            Version = header.Version,
            Flags = header.Flags,
            LookupTableOffset = header.LookupTableOffset,
            GlyphDataTableOffset = header.GlyphDataTableOffset,
            Lookup = lookup,
            Glyphs = entries,
            PointsByGlyph = byGlyph,
        };
    }

    /// <summary>Reads one glyph data entry: a 32-bit point count followed by that many coordinate pairs.</summary>
    /// <param name="source">The table-scoped source.</param>
    /// <param name="entryOffset">The absolute offset of the entry, which the caller has already resolved against the glyph data table.</param>
    /// <returns>The anchor points, in the order the font stored them.</returns>
    /// <exception cref="InvalidDataException">The declared point count cannot be a valid array length.</exception>
    /// <exception cref="EndOfStreamException">The coordinate array extends past the end of the table data.</exception>
    /// <remarks>The count is a 32-bit field, so it is checked against the maximum array length before the coordinates are read; a count that large is a corrupt table rather than a font with that many anchor points.</remarks>
    private static AnchorPoint[] ReadAnchorPoints(Source source, long entryOffset)
    {
        Cursor cursor = source.CreateOffsetCursor(entryOffset);
        uint numPoints = cursor.ReadUInt32();

        if (numPoints > int.MaxValue)
        {
            throw new InvalidDataException(
                $"'ankr' glyph data entry declares {numPoints} anchor points, which is not a valid count.");
        }

        return cursor.ReadBigEndianStructArray<AnchorPoint>((int)numPoints);
    }

    /// <summary>Enumerates every glyph index a lookup table can answer for.</summary>
    /// <param name="lookup">The lookup table to walk.</param>
    /// <returns>The glyph indices the lookup covers, in table order.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Formats 0, 8, and 10 are dense, so the walk is the glyph range the lookup covers.</description></item>
    /// <item><description>Formats 2 and 4 declare contiguous segments and format 6 a sorted list of entries, so those walks stop at the glyphs that have an explicit entry.</description></item>
    /// <item><description>The <c>0xFFFF</c> termination unit that the segment and entry formats conventionally end with is yielded like any other glyph; callers filter it out.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LookupTable"/>
    private static IEnumerable<int> EnumerateCoveredGlyphs(LookupTable lookup)
    {
        if (lookup is LookupTable2 bySegment)
        {
            foreach (LookupSegment segment in bySegment.Segments)
                for (int glyph = segment.FirstGlyph; glyph <= segment.LastGlyph; glyph++)
                    yield return glyph;

            yield break;
        }

        if (lookup is LookupTable4 bySegmentArray)
        {
            foreach (LookupSegment4 segment in bySegmentArray.Segments)
                for (int glyph = segment.FirstGlyph; glyph <= segment.LastGlyph; glyph++)
                    yield return glyph;

            yield break;
        }

        if (lookup is LookupTable6 byEntry)
        {
            foreach (LookupEntry entry in byEntry.Entries)
                yield return entry.Glyph;

            yield break;
        }

        if (lookup is LookupTable8 byTrimmedArray)
        {
            for (int i = 0; i < byTrimmedArray.Count; i++)
                yield return byTrimmedArray.FirstGlyph + i;

            yield break;
        }

        if (lookup is LookupTable10 byExtendedArray)
        {
            for (int i = 0; i < byExtendedArray.Count; i++)
                yield return byExtendedArray.FirstGlyph + i;

            yield break;
        }

        for (int glyph = 0; glyph < lookup.Count; glyph++)
            yield return glyph;
    }

    /// <summary>Resolves the value a nested lookup table needs in order to read a length-less format 0 array.</summary>
    /// <param name="context">The context handed to <see cref="IRecord{T}.Parse(ref Cursor, object?)"/>: a <see cref="FontFace"/>, an <see cref="int"/> glyph count, or <see langword="null"/>.</param>
    /// <returns>The font's glyph count as an <see cref="int"/> when it can be obtained, or <see langword="null"/> so that the lookup is read to the end of the table data.</returns>
    /// <remarks>A format 0 lookup is a bare array with no declared length, so it must be told how many glyphs the font has. A <see cref="FontFace"/> context supplies the count from <c>maxp.numGlyphs</c>; an <see cref="int"/> context is taken as that count directly. The count is optional because a table that never uses a format 0 lookup does not need it.</remarks>
    /// <seealso cref="LookupTable.Parse(ref Cursor, object?)"/>
    private static object? GetGlyphCountContext(object? context) => context switch
    {
        int glyphCount => glyphCount,
        FontFace face when face.Directory.ContainsKey(MaxpTable.Tag) => face.GetTable<MaxpTable>().NumGlyphs,
        _ => null,
    };
}
