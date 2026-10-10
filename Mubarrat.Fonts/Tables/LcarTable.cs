using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// lcar — Ligature Caret Table (Apple Advanced Typography)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>lcar</c> table: the division points inside a ligature at which an application may split the ligature to place a caret.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is an Apple Advanced Typography (AAT) table and is not part of OpenType. Apple documents it in the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">TrueType Reference Manual, <c>lcar</c> table</see>.</description></item>
/// <item><description>Without the table an application can only divide a ligature evenly, which places the caret badly whenever the parts of the ligature have different widths — the <c>w</c> and <c>i</c> of a <c>wi</c> ligature, for example. The table exists so that the font can name the correct split point.</description></item>
/// <item><description>The six-byte <see cref="Header"/> is followed immediately by the lookup table, which has no offset field of its own. The lookup value for a glyph is a 16-bit offset from the start of the <c>lcar</c> table to that glyph's <see cref="LigCaretClassEntry"/> record, not an offset from the start of the lookup table.</description></item>
/// <item><description>The number of partials is, in general, one less than the number of characters composing the ligature: each partial is one division point. In <see cref="FormatDistance"/> a partial is an FUnit distance along the baseline measured from x = 0, so every division point of a multi-part ligature is stored independently; in <see cref="FormatControlPoint"/> a partial is a control point number, whose orthogonal projection onto the baseline is the division point. Control points are defined in the <c>glyf</c> table.</description></item>
/// <item><description>Format 1 exists because a control point can be hinted: its location can be corrected for small pixels-per-em sizes, which a distance cannot.</description></item>
/// <item><description>A lookup value of zero cannot address a record, because the header occupies the first <see cref="HeaderSize"/> bytes of the table. Such a value is therefore read as "no ligature carets" and the glyph is omitted from <see cref="Glyphs"/>; the same is done for the <c>0xFFFF</c> entry that a binary-searched lookup conventionally ends with. Glyphs that share a division point may share one record, which is read once and cached.</description></item>
/// <item><description>Any of the five lookup formats may be used. A format 0 table is an untrimmed array with no declared length, so supply a <see cref="FontFace"/> as the parse context when the table may use it: the glyph count is then taken from <c>maxp.numGlyphs</c>, which is what the shared lookup dispatcher expects.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LigCaretClassEntry"/>
/// <seealso cref="GlyphLigatureCarets"/>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">TrueType Reference Manual: The <c>lcar</c> table</seealso>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
public sealed record LcarTable : IFontTable<LcarTable>
{
    /// <summary>Gets the AAT table tag <c>lcar</c>.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">TrueType Reference Manual: The <c>lcar</c> table</seealso>
    public static Tag Tag => "lcar";

    /// <summary>Gets the table version.</summary>
    /// <remarks>The initial and current version is 1.0; <see cref="MajorVersion"/> must be 1.</remarks>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="MinorVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">TrueType Reference Manual: The <c>lcar</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the major half of <see cref="Version"/>, which must be 1.</summary>
    /// <seealso cref="Version"/>
    /// <seealso cref="MinorVersion"/>
    public ushort MajorVersion => (ushort)Version.IntegerPart;

    /// <summary>Gets the minor half of <see cref="Version"/>.</summary>
    /// <seealso cref="Version"/>
    /// <seealso cref="MajorVersion"/>
    public ushort MinorVersion => Version.FractionPart;

    /// <summary>Gets the table format: <see cref="FormatDistance"/> for FUnit distances, or <see cref="FormatControlPoint"/> for control point numbers.</summary>
    /// <seealso cref="FormatDistance"/>
    /// <seealso cref="FormatControlPoint"/>
    /// <seealso cref="IsControlPointFormat"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">TrueType Reference Manual: The <c>lcar</c> table</seealso>
    public ushort Format { get; init; }

    /// <summary>Gets a value indicating whether the partials are control point numbers rather than FUnit distances.</summary>
    /// <seealso cref="Format"/>
    /// <seealso cref="FormatControlPoint"/>
    public bool IsControlPointFormat => Format == FormatControlPoint;

    /// <summary>Gets the lookup table that maps glyph indices onto offsets to <see cref="LigCaretClassEntry"/> records, relative to the start of the <c>lcar</c> table.</summary>
    /// <seealso cref="LookupTable"/>
    /// <seealso cref="GetEntry(int)"/>
    public LookupTable Lookup { get; init; } = null!;

    /// <summary>Gets the ligature caret records of every glyph the lookup table covers, in ascending glyph order.</summary>
    /// <remarks>Glyphs whose lookup value is zero, and the <c>0xFFFF</c> termination entry, are omitted because neither addresses a <see cref="LigCaretClassEntry"/> record. Glyphs that share a record appear as separate entries holding the same record instance.</remarks>
    /// <seealso cref="GetEntry(int)"/>
    /// <seealso cref="Count"/>
    public IReadOnlyList<GlyphLigatureCarets> Glyphs { get; init; } = [];

    /// <summary>Gets the number of glyphs that carry ligature caret data.</summary>
    /// <seealso cref="Glyphs"/>
    public int Count => Glyphs.Count;

    /// <summary>Gets the ligature caret record of a glyph.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <returns>The glyph's record, or <see langword="null"/> when the table gives the glyph none.</returns>
    /// <seealso cref="GetCarets(int)"/>
    /// <seealso cref="LigCaretClassEntry"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">TrueType Reference Manual: The <c>lcar</c> table</seealso>
    public LigCaretClassEntry? GetEntry(int glyphIndex) =>
        EntriesByGlyph.TryGetValue(glyphIndex, out LigCaretClassEntry? entry) ? entry : null;

    /// <summary>Gets the division points of a glyph's ligature.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <returns>The glyph's partials, or an empty list when the table gives the glyph none.</returns>
    /// <remarks>The values are FUnit distances or control point numbers according to <see cref="Format"/>, and each is one division point, in the order the font stored them.</remarks>
    /// <seealso cref="GetEntry(int)"/>
    /// <seealso cref="LigCaretClassEntry.Partials"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">TrueType Reference Manual: The <c>lcar</c> table</seealso>
    public IReadOnlyList<short> GetCarets(int glyphIndex)
    {
        if (EntriesByGlyph.TryGetValue(glyphIndex, out LigCaretClassEntry? entry))
            return entry.Partials;

        return [];
    }

    /// <summary>Format value declaring that a partial is an FUnit distance along the baseline, measured from x = 0.</summary>
    /// <seealso cref="Format"/>
    /// <seealso cref="FormatControlPoint"/>
    public const ushort FormatDistance = 0;

    /// <summary>Format value declaring that a partial is a control point number.</summary>
    /// <remarks>The control point's orthogonal projection onto the baseline is the division point.</remarks>
    /// <seealso cref="Format"/>
    /// <seealso cref="FormatDistance"/>
    public const ushort FormatControlPoint = 1;

    /// <summary>Size, in bytes, of the fixed-layout <see cref="Header"/>.</summary>
    /// <remarks>The lookup table begins at this offset from the start of the table.</remarks>
    /// <seealso cref="Header"/>
    public const int HeaderSize = 6;

    /// <summary>Gets the ligature caret record of a glyph, indexed by glyph index for constant-time lookup.</summary>
    /// <remarks>Built alongside <see cref="Glyphs"/> so that a query does not have to scan the list.</remarks>
    /// <seealso cref="GetEntry(int)"/>
    private IReadOnlyDictionary<int, LigCaretClassEntry> EntriesByGlyph { get; init; } =
        new Dictionary<int, LigCaretClassEntry>();

    /// <summary>The six-byte fixed-layout <c>lcar</c> table header.</summary>
    /// <remarks>Fields are stored in big-endian order at the offsets defined by the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html"><c>lcar</c> table specification</see>. The lookup table follows this header immediately.</remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">TrueType Reference Manual: The <c>lcar</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0.</summary>
        /// <remarks>1.0 for the initial version.</remarks>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">TrueType Reference Manual: The <c>lcar</c> table</seealso>
        public Fixed Version; // +0

        /// <summary>The table format at byte offset 4: <see cref="FormatDistance"/> or <see cref="FormatControlPoint"/>.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">TrueType Reference Manual: The <c>lcar</c> table</seealso>
        public ushort Format; // +4

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = new Fixed(BinaryPrimitives.ReverseEndianness(v.Version.Bits)),
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
        };
    }

    /// <summary>The ligature division points of one glyph: a count followed by that many partials.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The record is variable-length, so it is read from the offset the lookup table gives rather than as part of an array.</description></item>
    /// <item><description>Its values are 16-bit, matching the specification's <c>int16 partials[count]</c>; there is no 32-bit form.</description></item>
    /// <item><description><see cref="Count"/> is retained because it is part of the stored record, and it always equals <c>Partials.Count</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LcarTable.GetEntry(int)"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">TrueType Reference Manual: The <c>lcar</c> table</seealso>
    public sealed record LigCaretClassEntry
    {
        /// <summary>Gets the number of entries in <see cref="Partials"/>, as declared by the record.</summary>
        /// <remarks>The count is, in general, one less than the number of characters composing the ligature.</remarks>
        /// <seealso cref="Partials"/>
        public ushort Count { get; init; }

        /// <summary>Gets the division points themselves.</summary>
        /// <remarks>Each value is an FUnit distance or a control point number according to <see cref="LcarTable.Format"/>.</remarks>
        /// <seealso cref="Count"/>
        /// <seealso cref="LcarTable.Format"/>
        public IReadOnlyList<short> Partials { get; init; } = [];
    }

    /// <summary>The ligature caret record of one glyph, as stored in the table.</summary>
    /// <remarks>Several glyphs may share one <see cref="LigCaretClassEntry"/> record, in which case their entries hold the same instance.</remarks>
    /// <seealso cref="LcarTable.Glyphs"/>
    /// <seealso cref="LigCaretClassEntry"/>
    public sealed record GlyphLigatureCarets
    {
        /// <summary>Gets the glyph index the record describes.</summary>
        public int GlyphIndex { get; init; }

        /// <summary>Gets the glyph's ligature division points.</summary>
        /// <seealso cref="LigCaretClassEntry"/>
        public LigCaretClassEntry Entry { get; init; } = new();
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the <c>lcar</c> table.</param>
    /// <param name="context">A <see cref="FontFace"/> so that a format 0 lookup can be sized by <c>maxp.numGlyphs</c>, or <see langword="null"/>; a plain <see cref="int"/> glyph count is also accepted.</param>
    /// <returns>The parsed <c>lcar</c> table.</returns>
    /// <exception cref="InvalidDataException">The major version is not 1, or the format is not 0 or 1.</exception>
    /// <exception cref="EndOfStreamException">The header, the lookup table, or a ligature caret record extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The table's major version must be 1 and its format must be 0 or 1.</description></item>
    /// <item><description>Each glyph the lookup covers is resolved to its <see cref="LigCaretClassEntry"/> record once; records shared by several glyphs are read once and cached.</description></item>
    /// <item><description>A format 0 lookup declares no length of its own, so the glyph count reaches it through <paramref name="context"/>; with no usable context it is read to the end of the table data, which is wrong here because the caret records follow the lookup.</description></item>
    /// </list>
    /// </remarks>
    static LcarTable IRecord<LcarTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version.IntegerPart != 1)
        {
            throw new InvalidDataException(
                $"'lcar'.version is 0x{unchecked((uint)header.Version.Bits):X8}, expected a 1.x table.");
        }

        if (header.Format is not (FormatDistance or FormatControlPoint))
            throw new InvalidDataException($"'lcar'.format is {header.Format}, expected 0 or 1.");

        LookupTable lookup = LookupTable.Parse(ref cursor, GetGlyphCountContext(context));

        var entries = new List<GlyphLigatureCarets>();
        var records = new Dictionary<uint, LigCaretClassEntry>();

        foreach (int glyph in EnumerateCoveredGlyphs(lookup))
        {
            if (glyph == BinSrchHeader.EndOfSearch) continue;
            if (!lookup.TryGetValue(glyph, out uint offset) || offset < HeaderSize) continue;

            if (!records.TryGetValue(offset, out LigCaretClassEntry? entry))
            {
                Cursor entryCursor = cursor.Source.CreateOffsetCursor(offset);
                ushort count = entryCursor.ReadUInt16();
                entry = new LigCaretClassEntry
                {
                    Count = count,
                    Partials = entryCursor.ReadInt16Array(count),
                };

                records[offset] = entry;
            }

            entries.Add(new GlyphLigatureCarets { GlyphIndex = glyph, Entry = entry });
        }

        var byGlyph = new Dictionary<int, LigCaretClassEntry>(entries.Count);
        foreach (GlyphLigatureCarets glyph in entries)
            byGlyph[glyph.GlyphIndex] = glyph.Entry;

        return new LcarTable
        {
            Version = header.Version,
            Format = header.Format,
            Lookup = lookup,
            Glyphs = entries,
            EntriesByGlyph = byGlyph,
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
