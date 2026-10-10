using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// opbd — Optical Bounds Table (Apple Advanced Typography)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>opbd</c> table: the optical edges of a glyph, used to align the edges of lines of text so that they line up visually rather than by their side bearings.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is an Apple Advanced Typography (AAT) table and is not part of OpenType. Apple documents it in the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual, <c>opbd</c> table</see>.</description></item>
/// <item><description>For each glyph the table stores four values, one per optical edge, in the order left, top, right, and bottom. Each value is the amount by which the glyph must be moved for its optical edge to align; the sign convention is that of the font's coordinate system, so a negative left-side value moves the glyph left or down.</description></item>
/// <item><description>The six-byte <see cref="Header"/> is followed immediately by the lookup table, which has no offset field of its own. The lookup value for a glyph is a 16-bit offset from the start of the <c>opbd</c> table to that glyph's <see cref="OpticalBounds"/> record, not an offset from the start of the lookup table.</description></item>
/// <item><description><see cref="Format"/> distinguishes distance data from control point data. In <see cref="FormatDistance"/> the four values are deltas in FUnits, and a delta of zero declares that the side has no optical bound. In <see cref="FormatControlPoint"/> the four values are control point numbers, and the special value -1 declares that the side has no optical bound.</description></item>
/// <item><description>Format 1 exists because a control point can be hinted: its location can be corrected for small pixels-per-em sizes, which a distance cannot. Control points are defined in the <c>glyf</c> table.</description></item>
/// <item><description>A lookup value of zero cannot address a record, because the header occupies the first <see cref="HeaderSize"/> bytes of the table. Such a value is therefore read as "no optical bounds" and the glyph is omitted from <see cref="Glyphs"/>; the same is done for the <c>0xFFFF</c> entry that a binary-searched lookup conventionally ends with.</description></item>
/// <item><description>Any of the five lookup formats may be used. A format 0 table is an untrimmed array with no declared length, so supply a <see cref="FontFace"/> as the parse context when the table may use it: the glyph count is then taken from <c>maxp.numGlyphs</c>, which is what the shared lookup dispatcher expects.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="OpticalBounds"/>
/// <seealso cref="GlyphOpticalBounds"/>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual: The <c>opbd</c> table</seealso>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
public sealed record OpbdTable : IFontTable<OpbdTable>
{
    /// <summary>Gets the AAT table tag <c>opbd</c>.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual: The <c>opbd</c> table</seealso>
    public static Tag Tag => "opbd";

    /// <summary>Gets the table version.</summary>
    /// <remarks>The current version is 1.0; <see cref="MajorVersion"/> must be 1.</remarks>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="MinorVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual: The <c>opbd</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the major half of <see cref="Version"/>, which must be 1.</summary>
    /// <seealso cref="Version"/>
    /// <seealso cref="MinorVersion"/>
    public ushort MajorVersion => (ushort)Version.IntegerPart;

    /// <summary>Gets the minor half of <see cref="Version"/>.</summary>
    /// <seealso cref="Version"/>
    /// <seealso cref="MajorVersion"/>
    public ushort MinorVersion => Version.FractionPart;

    /// <summary>Gets the table format: <see cref="FormatDistance"/> for delta values in FUnits, or <see cref="FormatControlPoint"/> for control point numbers.</summary>
    /// <seealso cref="FormatDistance"/>
    /// <seealso cref="FormatControlPoint"/>
    /// <seealso cref="IsControlPointFormat"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual: The <c>opbd</c> table</seealso>
    public ushort Format { get; init; }

    /// <summary>Gets a value indicating whether <see cref="Glyphs"/> holds control point numbers rather than distance deltas.</summary>
    /// <seealso cref="Format"/>
    /// <seealso cref="FormatControlPoint"/>
    public bool IsControlPointFormat => Format == FormatControlPoint;

    /// <summary>Gets the lookup table that maps glyph indices onto offsets to <see cref="OpticalBounds"/> records, relative to the start of the <c>opbd</c> table.</summary>
    /// <seealso cref="LookupTable"/>
    /// <seealso cref="GetOpticalBounds(int)"/>
    public LookupTable Lookup { get; init; } = null!;

    /// <summary>Gets the optical bounds of every glyph the lookup table covers, in ascending glyph order.</summary>
    /// <remarks>Glyphs whose lookup value is zero, and the <c>0xFFFF</c> termination entry, are omitted because neither addresses an <see cref="OpticalBounds"/> record.</remarks>
    /// <seealso cref="GetOpticalBounds(int)"/>
    /// <seealso cref="Count"/>
    public IReadOnlyList<GlyphOpticalBounds> Glyphs { get; init; } = [];

    /// <summary>Gets the number of glyphs that carry optical bounds.</summary>
    /// <seealso cref="Glyphs"/>
    public int Count => Glyphs.Count;

    /// <summary>Gets the optical bounds of a glyph.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <returns>The glyph's optical bounds, or <see langword="null"/> when the table gives the glyph none.</returns>
    /// <remarks>Interpret the four values according to <see cref="Format"/>: distance deltas in FUnits, or control point numbers, with zero and -1 respectively meaning "no bound on this side".</remarks>
    /// <seealso cref="Glyphs"/>
    /// <seealso cref="OpticalBounds"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual: The <c>opbd</c> table</seealso>
    public OpticalBounds? GetOpticalBounds(int glyphIndex) =>
        BoundsByGlyph.TryGetValue(glyphIndex, out OpticalBounds bounds) ? bounds : null;

    /// <summary>Format value declaring that the four values are distances in FUnits.</summary>
    /// <remarks>A delta of zero means the side has no optical bound.</remarks>
    /// <seealso cref="Format"/>
    /// <seealso cref="FormatControlPoint"/>
    public const ushort FormatDistance = 0;

    /// <summary>Format value declaring that the four values are control point numbers.</summary>
    /// <remarks>The special value -1 means the side has no optical bound.</remarks>
    /// <seealso cref="Format"/>
    /// <seealso cref="FormatDistance"/>
    public const ushort FormatControlPoint = 1;

    /// <summary>Size, in bytes, of the fixed-layout <see cref="Header"/>.</summary>
    /// <remarks>The lookup table begins at this offset from the start of the table.</remarks>
    /// <seealso cref="Header"/>
    public const int HeaderSize = 6;

    /// <summary>Gets the optical bounds of a glyph, indexed by glyph index for constant-time lookup.</summary>
    /// <remarks>Built alongside <see cref="Glyphs"/> so that a query does not have to scan the list.</remarks>
    /// <seealso cref="GetOpticalBounds(int)"/>
    private IReadOnlyDictionary<int, OpticalBounds> BoundsByGlyph { get; init; } =
        new Dictionary<int, OpticalBounds>();

    /// <summary>The six-byte fixed-layout <c>opbd</c> table header.</summary>
    /// <remarks>Fields are stored in big-endian order at the offsets defined by the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html"><c>opbd</c> table specification</see>. The lookup table follows this header immediately.</remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual: The <c>opbd</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0.</summary>
        /// <remarks>1.0 for the current version.</remarks>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual: The <c>opbd</c> table</seealso>
        public Fixed Version; // +0

        /// <summary>The table format at byte offset 4: <see cref="FormatDistance"/> or <see cref="FormatControlPoint"/>.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual: The <c>opbd</c> table</seealso>
        public ushort Format; // +4

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = new Fixed(BinaryPrimitives.ReverseEndianness(v.Version.Bits)),
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
        };
    }

    /// <summary>The four optical edge values of one glyph, in left, top, right, bottom order.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>In a <see cref="FormatDistance"/> table the values are deltas in FUnits and a value of zero means the side has no optical bound.</description></item>
    /// <item><description>In a <see cref="FormatControlPoint"/> table the values are control point numbers and a value of -1 means the side has no optical bound.</description></item>
    /// <item><description>The sign convention is that of the font's coordinate system: negative values move the glyph left or down, positive values right or up.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="OpbdTable.GetOpticalBounds(int)"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual: The <c>opbd</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct OpticalBounds : IEndianReversibleStruct<OpticalBounds>
    {
        /// <summary>The left-side optical edge at byte offset 0.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual: The <c>opbd</c> table</seealso>
        public short LeftSide; // +0

        /// <summary>The top-side optical edge at byte offset 2.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual: The <c>opbd</c> table</seealso>
        public short TopSide; // +2

        /// <summary>The right-side optical edge at byte offset 4.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual: The <c>opbd</c> table</seealso>
        public short RightSide; // +4

        /// <summary>The bottom-side optical edge at byte offset 6.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual: The <c>opbd</c> table</seealso>
        public short BottomSide; // +6

        /// <summary>Reverses the byte order of every field in an <see cref="OpticalBounds"/>.</summary>
        public static OpticalBounds ReverseEndianness(OpticalBounds v) => new()
        {
            LeftSide = BinaryPrimitives.ReverseEndianness(v.LeftSide),
            TopSide = BinaryPrimitives.ReverseEndianness(v.TopSide),
            RightSide = BinaryPrimitives.ReverseEndianness(v.RightSide),
            BottomSide = BinaryPrimitives.ReverseEndianness(v.BottomSide),
        };
    }

    /// <summary>The optical bounds of one glyph, as stored in the table.</summary>
    /// <remarks>Several glyphs may share one <see cref="OpticalBounds"/> record, in which case their entries hold the same value.</remarks>
    /// <seealso cref="OpbdTable.Glyphs"/>
    /// <seealso cref="OpticalBounds"/>
    public sealed record GlyphOpticalBounds
    {
        /// <summary>Gets the glyph index the record describes.</summary>
        public int GlyphIndex { get; init; }

        /// <summary>Gets the glyph's four optical edge values.</summary>
        /// <seealso cref="OpticalBounds"/>
        public OpticalBounds Bounds { get; init; }
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the <c>opbd</c> table.</param>
    /// <param name="context">A <see cref="FontFace"/> so that a format 0 lookup can be sized by <c>maxp.numGlyphs</c>, or <see langword="null"/>; a plain <see cref="int"/> glyph count is also accepted.</param>
    /// <returns>The parsed <c>opbd</c> table.</returns>
    /// <exception cref="InvalidDataException">The major version is not 1, or the format is not 0 or 1.</exception>
    /// <exception cref="EndOfStreamException">The header, the lookup table, or an optical bounds record extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The table's major version must be 1 and its format must be 0 or 1.</description></item>
    /// <item><description>Each glyph the lookup covers is resolved to its <see cref="OpticalBounds"/> record once; records shared by several glyphs are read once and cached.</description></item>
    /// <item><description>A format 0 lookup declares no length of its own, so the glyph count reaches it through <paramref name="context"/>; with no usable context it is read to the end of the table data, which is wrong here because the bounds records follow the lookup.</description></item>
    /// </list>
    /// </remarks>
    static OpbdTable IRecord<OpbdTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version.IntegerPart != 1)
        {
            throw new InvalidDataException(
                $"'opbd'.version is 0x{unchecked((uint)header.Version.Bits):X8}, expected a 1.x table.");
        }

        if (header.Format is not (FormatDistance or FormatControlPoint))
            throw new InvalidDataException($"'opbd'.format is {header.Format}, expected 0 or 1.");

        LookupTable lookup = LookupTable.Parse(ref cursor, GetGlyphCountContext(context));

        var entries = new List<GlyphOpticalBounds>();
        var records = new Dictionary<uint, OpticalBounds>();

        foreach (int glyph in EnumerateCoveredGlyphs(lookup))
        {
            if (glyph == BinSrchHeader.EndOfSearch) continue;
            if (!lookup.TryGetValue(glyph, out uint offset) || offset < HeaderSize) continue;

            if (!records.TryGetValue(offset, out OpticalBounds bounds))
            {
                Cursor recordCursor = cursor.Source.CreateOffsetCursor(offset);
                bounds = recordCursor.ReadBigEndianStruct<OpticalBounds>();
                records[offset] = bounds;
            }

            entries.Add(new GlyphOpticalBounds { GlyphIndex = glyph, Bounds = bounds });
        }

        var byGlyph = new Dictionary<int, OpticalBounds>(entries.Count);
        foreach (GlyphOpticalBounds entry in entries)
            byGlyph[entry.GlyphIndex] = entry.Bounds;

        return new OpbdTable
        {
            Version = header.Version,
            Format = header.Format,
            Lookup = lookup,
            Glyphs = entries,
            BoundsByGlyph = byGlyph,
        };
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
