using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════
// Table + encoding records
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The <c>cmap</c> table: maps character codes to glyph IDs. Different subtables cover different encodings; each is a <see cref="CmapSubtable"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is a directory of encoding records, each naming a platform/encoding pair and pointing to a subtable. A single font may carry many subtables covering overlapping character sets.</description></item>
/// <item><description>Format 4 covers the Basic Multilingual Plane (U+0000–U+FFFF); format 12 covers all of Unicode; format 14 handles variation sequences. Formats 0, 2, 6, 8, 10, and 13 are defined but rarely encountered in modern fonts.</description></item>
/// <item><description>Lookups return <c>0</c> for unmapped codepoints; glyph ID <c>0</c> is by convention the <c>.notdef</c> glyph. <see cref="TryGetGlyphIdFromAnySubtable(int, out int)"/> distinguishes "mapped to .notdef" from "not mapped at all" by returning <see langword="false"/> when no subtable yields a non-zero ID.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">cmap table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CmapEncodingRecord"/>
/// <seealso cref="CmapSubtable"/>
/// <seealso cref="CmapFormat0"/>
/// <seealso cref="CmapFormat4"/>
/// <seealso cref="CmapFormat12"/>
/// <seealso cref="CmapFormat14"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">OpenType specification: cmap table</seealso>
public sealed record CmapTable : IFontTable<CmapTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">OpenType specification: cmap table</seealso>
    public static Tag Tag => "cmap";

    /// <summary>Gets the table version. Always 0.</summary>
    /// <value>The constant <c>0</c> for a conforming cmap table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap"><c>version</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="EncodingRecords"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">OpenType specification: <c>version</c></seealso>
    public ushort Version { get; init; }

    /// <summary>Gets all encoding records in the order they appear in the table.</summary>
    /// <value>The unranked list of <see cref="CmapEncodingRecord"/> entries, in on-disk order. Use <see cref="RankedSubtables"/> for a preference-ordered view.</value>
    /// <seealso cref="Subtables"/>
    /// <seealso cref="RankedSubtables"/>
    /// <seealso cref="CmapEncodingRecord"/>
    public IReadOnlyList<CmapEncodingRecord> EncodingRecords { get; init; } = [];

    /// <summary>Gets the parsed subtables, in encoding-record order.</summary>
    /// <value>The <see cref="CmapSubtable"/> instances referenced by <see cref="EncodingRecords"/>, in the same order. The two lists are index-parallel.</value>
    /// <seealso cref="EncodingRecords"/>
    /// <seealso cref="RankedSubtables"/>
    /// <seealso cref="CmapSubtable"/>
    public IReadOnlyList<CmapSubtable> Subtables { get; init; } = [];

    /// <summary>Gets the number of subtables.</summary>
    /// <value>The length of <see cref="Subtables"/>.</value>
    /// <seealso cref="Subtables"/>
    public int SubtableCount => Subtables.Count;

    /// <summary>Attempts to resolve <paramref name="codepoint"/> by trying every subtable in the same preference order used by <see cref="GetGlyphId"/>, returning the first nonzero glyph ID.</summary>
    /// <param name="codepoint">The Unicode code point to resolve.</param>
    /// <param name="glyphId">When this method returns, the resolved glyph ID; <c>0</c> on failure.</param>
    /// <returns><see langword="true"/> when a subtable yielded a nonzero glyph ID; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The iteration order is <see cref="RankedSubtables"/>; format 14 subtables are excluded because they do not map individual codepoints.</description></item>
    /// <item><description>Unlike <see cref="GetGlyphId"/>, this method distinguishes "no subtable maps this codepoint" from "the highest-ranked subtable maps it to <c>.notdef</c>". Use this when that distinction matters.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetGlyphId(int)"/>
    /// <seealso cref="RankedSubtables"/>
    public bool TryGetGlyphIdFromAnySubtable(int codepoint, out int glyphId)
    {
        foreach (var sub in RankedSubtables)
        {
            int gid = sub.GetGlyphId(codepoint);
            if (gid != 0)
            {
                glyphId = gid;
                return true;
            }
        }
        glyphId = 0;
        return false;
    }

    /// <summary>Gets the subtables ordered by platform/encoding preference, with format 14 subtables excluded.</summary>
    /// <value>A cached, preference-ordered array of <see cref="CmapSubtable"/> entries. The first element is the most-preferred subtable for a general Unicode lookup.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The array is computed lazily on first access and cached for the lifetime of the table.</description></item>
    /// <item><description>Preference is determined by <see cref="Rank(NamePlatformId, ushort)"/>; lower rank values are preferred.</description></item>
    /// <item><description>Format 14 subtables are excluded because they do not map individual codepoints.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BuildRankedSubtables"/>
    /// <seealso cref="Rank(NamePlatformId, ushort)"/>
    /// <seealso cref="GetGlyphId(int)"/>
    /// <seealso cref="TryGetGlyphIdFromAnySubtable(int, out int)"/>
    public CmapSubtable[] RankedSubtables => field ??= BuildRankedSubtables();

    /// <summary>Builds a fresh preference-ordered array of subtables, excluding format 14.</summary>
    /// <returns>A new array of <see cref="CmapSubtable"/> entries sorted by ascending <see cref="Rank(NamePlatformId, ushort)"/> value.</returns>
    /// <remarks>Called once by <see cref="RankedSubtables"/> to populate the cache. The result is not cached by this method itself; callers that need the cached form should access <see cref="RankedSubtables"/>.</remarks>
    /// <seealso cref="RankedSubtables"/>
    /// <seealso cref="Rank(NamePlatformId, ushort)"/>
    public CmapSubtable[] BuildRankedSubtables()
    {
        var ranked = new List<CmapSubtable>(Subtables.Count);
        foreach (var sub in Subtables)
        {
            if (sub.Format == 14) continue;
            ranked.Add(sub);
        }
        ranked.Sort((a, b) => Rank(a.PlatformId, a.EncodingId)
            .CompareTo(Rank(b.PlatformId, b.EncodingId)));
        return ranked.ToArray();
    }

    /// <summary>Returns a preference rank for a platform/encoding pair; lower values are more preferred.</summary>
    /// <param name="platform">The platform identifier.</param>
    /// <param name="encoding">The platform-specific encoding identifier.</param>
    /// <returns>An integer rank in the range <c>[0, 10]</c>. Lower is more preferred.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Windows full Unicode (platform 3, encoding 10) is preferred over Unicode full repertoire (platform 0, encoding 6) to match the ordering used by most shaping engines.</description></item>
    /// <item><description>Ranks <c>0</c>–<c>8</c> cover the Unicode and Windows platform variants; rank <c>9</c> covers Macintosh Roman; rank <c>10</c> is the catch-all for anything else.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#platform-and-encoding-ids">platform and encoding IDs</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BuildRankedSubtables"/>
    /// <seealso cref="RankedSubtables"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#platform-and-encoding-ids">OpenType specification: platform and encoding IDs</seealso>
    public static int Rank(NamePlatformId platform, ushort encoding) => (platform, encoding) switch
    {
        (NamePlatformId.Windows, 10) => 0,
        (NamePlatformId.Unicode, 6) => 1,
        (NamePlatformId.Unicode, 4) => 2,
        (NamePlatformId.Windows, 1) => 3,
        (NamePlatformId.Unicode, 3) => 4,
        (NamePlatformId.Unicode, 2) => 5,
        (NamePlatformId.Unicode, 1) => 6,
        (NamePlatformId.Unicode, 0) => 7,
        (NamePlatformId.Windows, 0) => 8,
        (NamePlatformId.Macintosh, 0) => 9,
        _ => 10,
    };

    /// <summary>Returns the glyph ID for <paramref name="codepoint"/> using the highest-ranked subtable.</summary>
    /// <param name="codepoint">The Unicode code point to resolve.</param>
    /// <returns>The glyph ID from the highest-ranked subtable, or <c>0</c> when the codepoint is not mapped or no subtable is available.</returns>
    /// <remarks>The lookup uses only the highest-ranked subtable; it does not fall through to lower-ranked subtables on a miss. Use <see cref="TryGetGlyphIdFromAnySubtable(int, out int)"/> to search across subtables.</remarks>
    /// <seealso cref="TryGetGlyphIdFromAnySubtable(int, out int)"/>
    /// <seealso cref="RankedSubtables"/>
    public int GetGlyphId(int codepoint)
    {
        CmapSubtable[] ranked = RankedSubtables;
        return ranked.Length > 0 ? ranked[0].GetGlyphId(codepoint) : 0;
    }

    /// <summary>Returns the glyph ID for the variation sequence, or <c>-1</c> if no format 14 subtable defines it.</summary>
    /// <param name="baseCodepoint">The base character code point.</param>
    /// <param name="variationSelector">The variation selector code point.</param>
    /// <returns>The resolved glyph ID, or <c>-1</c> when the sequence is not defined.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>This method returns on the first format 14 subtable found; it does not continue searching additional format 14 subtables. In practice fonts carry at most one such subtable, so this is not observable.</description></item>
    /// <item><description>Returns <c>-1</c>, not <c>0</c>, to distinguish "sequence undefined" from "sequence maps to <c>.notdef</c>".</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">format 14 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapFormat14"/>
    /// <seealso cref="CmapFormat14.GetVariationGlyphId(int, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
    public int GetVariationGlyphId(int baseCodepoint, int variationSelector)
    {
        foreach (var sub in Subtables.OfType<CmapFormat14>())
            return sub.GetVariationGlyphId(baseCodepoint, variationSelector);
        return -1;
    }

    /// <summary>Returns the subtable matching the platform/encoding pair, or <c>null</c>.</summary>
    /// <param name="platformId">The platform identifier to match.</param>
    /// <param name="encodingId">The platform-specific encoding identifier to match.</param>
    /// <returns>The first <see cref="CmapSubtable"/> whose <see cref="CmapSubtable.PlatformId"/> and <see cref="CmapSubtable.EncodingId"/> match, or <c>null</c>.</returns>
    /// <remarks>The lookup is linear over <see cref="Subtables"/>. When more than one subtable matches — unusual but not forbidden — the first in on-disk order is returned.</remarks>
    /// <seealso cref="Subtables"/>
    /// <seealso cref="EncodingRecords"/>
    /// <seealso cref="CmapSubtable"/>
    public CmapSubtable? GetSubtable(NamePlatformId platformId, ushort encodingId)
    {
        foreach (var sub in Subtables)
        {
            if (sub.PlatformId == platformId && sub.EncodingId == encodingId)
                return sub;
        }
        return null;
    }

    /// <summary>The 4-byte <c>cmap</c> table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by <c>numTables</c> 8-byte encoding records.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">cmap header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="EncodingRecordHeader"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">OpenType specification: cmap header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the table version. Always 0.</summary>
        /// <value>The constant <c>0</c> for a conforming cmap table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap"><c>version</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="NumTables"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">OpenType specification: <c>version</c></seealso>
        public ushort Version;    // +0

        /// <summary>Gets the number of encoding records that follow the header.</summary>
        /// <value>The count of <see cref="EncodingRecordHeader"/> entries in the directory.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap"><c>numTables</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Version"/>
        /// <seealso cref="CmapEncodingRecord"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">OpenType specification: <c>numTables</c></seealso>
        public ushort NumTables;  // +2

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">OpenType specification: cmap header</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            NumTables = BinaryPrimitives.ReverseEndianness(v.NumTables),
        };
    }

    /// <summary>An 8-byte encoding record. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Each record names a platform/encoding pair and the byte offset to its subtable. The offset is measured from the start of the <c>cmap</c> table, not the font file.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">cmap encoding records</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapEncodingRecord"/>
    /// <seealso cref="Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">OpenType specification: cmap encoding records</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct EncodingRecordHeader : IEndianReversibleStruct<EncodingRecordHeader>
    {
        /// <summary>Gets the platform identifier.</summary>
        /// <value>The <see cref="NamePlatformId"/> value identifying the platform this subtable is intended for.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#platform-and-encoding-ids"><c>platformID</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="EncodingId"/>
        /// <seealso cref="SubtableOffset"/>
        /// <seealso cref="NamePlatformId"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#platform-and-encoding-ids">OpenType specification: <c>platformID</c></seealso>
        public ushort PlatformId;       // +0

        /// <summary>Gets the platform-specific encoding identifier.</summary>
        /// <value>The encoding ID, interpreted according to <see cref="PlatformId"/>.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#platform-and-encoding-ids"><c>encodingID</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="PlatformId"/>
        /// <seealso cref="SubtableOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#platform-and-encoding-ids">OpenType specification: <c>encodingID</c></seealso>
        public ushort EncodingId;       // +2

        /// <summary>Gets the offset from the start of the cmap table to the subtable.</summary>
        /// <value>The byte offset of the referenced <see cref="CmapSubtable"/> from the cmap table start.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap"><c>subtableOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="PlatformId"/>
        /// <seealso cref="EncodingId"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">OpenType specification: <c>subtableOffset</c></seealso>
        public uint SubtableOffset;     // +4

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">OpenType specification: cmap encoding records</seealso>
        public static EncodingRecordHeader ReverseEndianness(EncodingRecordHeader v) => new()
        {
            PlatformId = BinaryPrimitives.ReverseEndianness(v.PlatformId),
            EncodingId = BinaryPrimitives.ReverseEndianness(v.EncodingId),
            SubtableOffset = BinaryPrimitives.ReverseEndianness(v.SubtableOffset),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the cmap table.</param>
    /// <param name="context">A <see cref="FontFace"/>. Passed through to the subtable parsers as a <see cref="ParentContext"/>.</param>
    /// <returns>The parsed cmap table with all subtables resolved.</returns>
    /// <exception cref="InvalidDataException">The version is not 0, or a subtable declares a format the parser does not recognise.</exception>
    /// <exception cref="EndOfStreamException">The header, encoding records, or any referenced subtable extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Every encoding record's subtable is parsed eagerly, even for formats the parser does not understand — those produce a <see cref="CmapFormatUnknown"/> placeholder.</description></item>
    /// <item><description>The parse is not recursive: subtables are read directly from the table-scoped source using their own offsets, not through the cursor.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">cmap table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="CmapEncodingRecord"/>
    /// <seealso cref="CmapSubtable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">OpenType specification: cmap table</seealso>
    static CmapTable IRecord<CmapTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.Version != 0)
            throw new InvalidDataException($"'cmap'.version is {header.Version}, expected 0.");

        var records = cursor.ReadBigEndianHeaderRecordArray<CmapEncodingRecord, EncodingRecordHeader>(
            header.NumTables, new ParentContext(cursor.Source));

        return new CmapTable
        {
            Version = header.Version,
            EncodingRecords = records,
            Subtables = Array.ConvertAll(records, record => record.Subtable),
        };
    }
}

/// <summary>An encoding record in the <c>cmap</c> header. The subtable it points to is resolved during parsing.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record pairs a platform/encoding identifier with a resolved <see cref="CmapSubtable"/> instance.</description></item>
/// <item><description>Subtables are parsed eagerly by <see cref="IRecord{T}.Parse"/>; the subtable's own platform and encoding IDs are copied from this record's header.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">cmap encoding records</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CmapTable"/>
/// <seealso cref="CmapSubtable"/>
/// <seealso cref="CmapTable.EncodingRecordHeader"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">OpenType specification: cmap encoding records</seealso>
public sealed record CmapEncodingRecord : IEndianReversibleHeaderRecord<CmapEncodingRecord, CmapTable.EncodingRecordHeader>
{
    /// <summary>Gets the platform ID.</summary>
    /// <value>The <see cref="NamePlatformId"/> identifying the platform this subtable is intended for.</value>
    /// <seealso cref="EncodingId"/>
    /// <seealso cref="Subtable"/>
    /// <seealso cref="NamePlatformId"/>
    public NamePlatformId PlatformId { get; init; }

    /// <summary>Gets the platform-specific encoding ID.</summary>
    /// <value>The encoding ID, interpreted according to <see cref="PlatformId"/>.</value>
    /// <seealso cref="PlatformId"/>
    /// <seealso cref="Subtable"/>
    public ushort EncodingId { get; init; }

    /// <summary>Gets the parsed subtable.</summary>
    /// <value>The <see cref="CmapSubtable"/> referenced by this record's offset, with its own platform and encoding IDs copied from this record.</value>
    /// <seealso cref="PlatformId"/>
    /// <seealso cref="EncodingId"/>
    /// <seealso cref="CmapSubtable"/>
    public CmapSubtable Subtable { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="header">The already-read encoding record header.</param>
    /// <param name="context">A <see cref="ParentContext"/> whose <see cref="IParentContext.ParentSource"/> is the table-scoped source.</param>
    /// <returns>A new record with its subtable resolved.</returns>
    /// <remarks>The <c>subtableOffset</c> is measured from the start of the <c>cmap</c> table; the parent context carries the table-scoped source so the offset resolves correctly.</remarks>
    /// <seealso cref="Subtable"/>
    /// <seealso cref="CmapSubtableContext"/>
    /// <seealso cref="CmapSubtable"/>
    static CmapEncodingRecord IHeaderRecord<CmapEncodingRecord, CmapTable.EncodingRecordHeader>.FromHeader(
        in CmapTable.EncodingRecordHeader header, object? context)
    {
        var platformId = (NamePlatformId)header.PlatformId;
        return new CmapEncodingRecord
        {
            PlatformId = platformId,
            EncodingId = header.EncodingId,
            Subtable = ((ParentContext)context!).ParentSource.ParseRecordAt<CmapSubtable>(
                header.SubtableOffset,
                new CmapSubtableContext(platformId, header.EncodingId)),
        };
    }
}

/// <summary>Context supplied to each <see cref="CmapSubtable"/> parse: the platform and encoding identifiers from the enclosing encoding record, which the subtable stores on itself.</summary>
/// <param name="PlatformId">The platform identifier from the enclosing encoding record.</param>
/// <param name="EncodingId">The encoding identifier from the enclosing encoding record.</param>
/// <remarks>The subtable parsers do not re-read these identifiers from the source; they receive them through this context because the identifiers live in the encoding record, not the subtable.</remarks>
/// <seealso cref="CmapSubtable"/>
/// <seealso cref="CmapEncodingRecord"/>
public record CmapSubtableContext(NamePlatformId PlatformId, ushort EncodingId);

// ═══════════════════════════════════════════════════════════════════════════
// Subtable base and dispatcher
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Base class for all <c>cmap</c> subtable formats.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Nine formats are defined by the specification: 0, 2, 4, 6, 8, 10, 12, 13, and 14. All except 14 map individual codepoints; format 14 maps variation sequences.</description></item>
/// <item><description>The dispatcher reads the format word and routes to the matching concrete type through <see cref="IBaseRecord{TBase}.Parse{TDerived}(ref Cursor, object?)"/>.</description></item>
/// <item><description>Unknown format numbers produce a <see cref="CmapFormatUnknown"/> placeholder rather than throwing; the format number is preserved for diagnostics.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">cmap table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CmapFormat0"/>
/// <seealso cref="CmapFormat2"/>
/// <seealso cref="CmapFormat4"/>
/// <seealso cref="CmapFormat6"/>
/// <seealso cref="CmapFormat8"/>
/// <seealso cref="CmapFormat10"/>
/// <seealso cref="CmapFormat12"/>
/// <seealso cref="CmapFormat13"/>
/// <seealso cref="CmapFormat14"/>
/// <seealso cref="CmapFormatUnknown"/>
/// <seealso cref="CmapSubtableContext"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">OpenType specification: cmap subtable formats</seealso>
public abstract record CmapSubtable : IRecord<CmapSubtable>, IBaseRecord<CmapSubtable>
{
    /// <summary>Gets the subtable format number.</summary>
    /// <value>One of 0, 2, 4, 6, 8, 10, 12, 13, or 14 for defined formats; any other value for a <see cref="CmapFormatUnknown"/> placeholder.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">cmap subtable formats</see> in the OpenType specification.</remarks>
    /// <seealso cref="PlatformId"/>
    /// <seealso cref="EncodingId"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">OpenType specification: cmap subtable formats</seealso>
    public ushort Format { get; init; }

    /// <summary>Gets the platform ID from the parent encoding record.</summary>
    /// <value>The <see cref="NamePlatformId"/> identifying the platform this subtable is intended for.</value>
    /// <remarks>The value is copied from <see cref="CmapEncodingRecord.PlatformId"/> during parse; it is not stored in the subtable's on-disk layout.</remarks>
    /// <seealso cref="Format"/>
    /// <seealso cref="EncodingId"/>
    /// <seealso cref="NamePlatformId"/>
    public NamePlatformId PlatformId { get; init; }

    /// <summary>Gets the encoding ID from the parent encoding record.</summary>
    /// <value>The encoding ID, interpreted according to <see cref="PlatformId"/>.</value>
    /// <remarks>The value is copied from <see cref="CmapEncodingRecord.EncodingId"/> during parse; it is not stored in the subtable's on-disk layout.</remarks>
    /// <seealso cref="Format"/>
    /// <seealso cref="PlatformId"/>
    public ushort EncodingId { get; init; }

    /// <summary>Returns the glyph ID for <paramref name="codepoint"/>, or 0 if the codepoint is not mapped.</summary>
    /// <param name="codepoint">The Unicode code point to resolve.</param>
    /// <returns>The mapped glyph ID, or <c>0</c> when the codepoint is not mapped or the format does not map individual codepoints (format 14).</returns>
    /// <remarks>Glyph ID <c>0</c> is by convention the <c>.notdef</c> glyph; a return of <c>0</c> may indicate either "unmapped" or "explicitly mapped to .notdef".</remarks>
    /// <seealso cref="CmapFormat4.GetGlyphId(int)"/>
    /// <seealso cref="CmapFormat12.GetGlyphId(int)"/>
    /// <seealso cref="CmapTable.TryGetGlyphIdFromAnySubtable(int, out int)"/>
    public abstract int GetGlyphId(int codepoint);

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable.</param>
    /// <param name="context">A <see cref="CmapSubtableContext"/> carrying the platform and encoding identifiers.</param>
    /// <returns>The format-specific subtable.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="CmapSubtableContext"/>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The dispatcher consumes the <c>format</c> word from the cursor, then delegates the remaining bytes to the format-specific parser.</description></item>
    /// <item><description>An unrecognised format number does not throw; the dispatcher produces a <see cref="CmapFormatUnknown"/> placeholder so that a font with one unsupported subtable remains partially usable.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">cmap subtable formats</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapSubtableContext"/>
    /// <seealso cref="CmapFormatUnknown"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap">OpenType specification: cmap subtable formats</seealso>
    static CmapSubtable IRecord<CmapSubtable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not CmapSubtableContext ctx)
            throw new InvalidOperationException(
                $"{nameof(CmapSubtable)}.Parse requires a {nameof(CmapSubtableContext)} context.");

        ushort format = cursor.ReadUInt16();
        return format switch
        {
            0 => IBaseRecord<CmapSubtable>.Parse<CmapFormat0>(ref cursor, context),
            2 => IBaseRecord<CmapSubtable>.Parse<CmapFormat2>(ref cursor, context),
            4 => IBaseRecord<CmapSubtable>.Parse<CmapFormat4>(ref cursor, context),
            6 => IBaseRecord<CmapSubtable>.Parse<CmapFormat6>(ref cursor, context),
            8 => IBaseRecord<CmapSubtable>.Parse<CmapFormat8>(ref cursor, context),
            10 => IBaseRecord<CmapSubtable>.Parse<CmapFormat10>(ref cursor, context),
            12 => IBaseRecord<CmapSubtable>.Parse<CmapFormat12>(ref cursor, context),
            13 => IBaseRecord<CmapSubtable>.Parse<CmapFormat13>(ref cursor, context),
            14 => IBaseRecord<CmapSubtable>.Parse<CmapFormat14>(ref cursor, context),
            _ => new CmapFormatUnknown
            {
                Format = format,
                PlatformId = ctx.PlatformId,
                EncodingId = ctx.EncodingId,
            },
        };
    }
}

/// <summary>Shared helper for retrieving and validating the parse context.</summary>
/// <remarks>File-scoped; not part of the public API. Centralises the context cast so each format parser does not repeat the type check.</remarks>
file static class CmapFormatHelpers
{
    /// <summary>Returns the parse context as a <see cref="CmapSubtableContext"/>, throwing if the cast fails.</summary>
    /// <param name="context">The context passed to the parser.</param>
    /// <returns>The context as a <see cref="CmapSubtableContext"/>.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="CmapSubtableContext"/>.</exception>
    /// <seealso cref="CmapSubtableContext"/>
    public static CmapSubtableContext GetContext(object? context) =>
        context as CmapSubtableContext
        ?? throw new InvalidOperationException(
            $"cmap subtable parse requires a {nameof(CmapSubtableContext)} context.");
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 0 — byte encoding table
// ═══════════════════════════════════════════════════════════════════════════

/// <summary><c>cmap</c> format 0: byte encoding table. A 256-entry lookup that maps 8-bit character codes to glyph IDs.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The subtable covers only the first 256 code points (the Latin-1 range in practice); codepoints above 255 always return <c>0</c>.</description></item>
/// <item><description>The subtable's length is fixed at 262 bytes by the format; the parser validates the declared length against that constant.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-0-byte-encoding-table">format 0 subtable</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CmapSubtable"/>
/// <seealso cref="CmapFormat4"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-0-byte-encoding-table">OpenType specification: format 0</seealso>
public sealed record CmapFormat0 : CmapSubtable, IDerivedRecord<CmapSubtable, CmapFormat0>
{
    /// <summary>Gets the 256-entry glyph ID array.</summary>
    /// <value>An array of 256 bytes; entry <c>i</c> is the glyph ID for character code <c>i</c>.</value>
    /// <seealso cref="GetGlyphId(int)"/>
    public IReadOnlyList<byte> GlyphIdArray { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="codepoint">The code point to resolve.</param>
    /// <returns>The glyph ID for <paramref name="codepoint"/> when it is in <c>[0, 255]</c>; otherwise <c>0</c>.</returns>
    /// <seealso cref="GlyphIdArray"/>
    public override int GetGlyphId(int codepoint) =>
        (uint)codepoint < 256 ? GlyphIdArray[codepoint] : 0;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the format word.</param>
    /// <param name="context">A <see cref="CmapSubtableContext"/> carrying the platform and encoding identifiers.</param>
    /// <returns>The parsed format-0 subtable.</returns>
    /// <exception cref="InvalidDataException">The declared length is not exactly 262 bytes.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="CmapSubtableContext"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-0-byte-encoding-table">format 0 subtable</see> in the OpenType specification.</remarks>
    /// <seealso cref="CmapSubtableContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-0-byte-encoding-table">OpenType specification: format 0</seealso>
    static CmapFormat0 IDerivedRecord<CmapSubtable, CmapFormat0>.Parse(
        ref Cursor cursor, object? context)
    {
        CmapSubtableContext ctx = CmapFormatHelpers.GetContext(context);
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Length != 262)
            throw new InvalidDataException(
                $"'cmap' format 0 subtable length is {header.Length}, expected 262.");

        return new CmapFormat0
        {
            Format = 0,
            PlatformId = ctx.PlatformId,
            EncodingId = ctx.EncodingId,
            GlyphIdArray = cursor.ReadBytes(256),
        };
    }

    /// <summary>The 4-byte format 0 header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-0-byte-encoding-table">format 0 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapFormat0"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-0-byte-encoding-table">OpenType specification: format 0</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the subtable length in bytes. Always 262.</summary>
        /// <value>The constant <c>262</c> for a conforming format-0 subtable.</value>
        /// <seealso cref="Language"/>
        public ushort Length;    // +0

        /// <summary>Gets the language identifier.</summary>
        /// <value>Zero for a language-neutral subtable; otherwise a Macintosh language code.</value>
        /// <seealso cref="Length"/>
        public ushort Language;  // +2

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-0-byte-encoding-table">OpenType specification: format 0</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Length = BinaryPrimitives.ReverseEndianness(v.Length),
            Language = BinaryPrimitives.ReverseEndianness(v.Language),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 2 — high-byte mapping through table
// ═══════════════════════════════════════════════════════════════════════════

/// <summary><c>cmap</c> format 2: high-byte mapping through table. Used for legacy CJK encodings such as Shift-JIS, Big5, and Johab.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The format handles mixed single-byte and double-byte encodings by splitting the code point into a high byte and a low byte, using the high byte to select a subheader and the low byte to index within it.</description></item>
/// <item><description>Subheader 0 covers the single-byte range; other subheaders cover the double-byte ranges for their respective high bytes.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-2-high-byte-mapping-through-table">format 2 subtable</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CmapSubtable"/>
/// <seealso cref="SubHeader"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-2-high-byte-mapping-through-table">OpenType specification: format 2</seealso>
public sealed record CmapFormat2 : CmapSubtable, IDerivedRecord<CmapSubtable, CmapFormat2>
{
    /// <summary>Gets the 256 subHeaderKeys, one per possible high byte. The key divided by 8 is the index into <see cref="SubHeaders"/>.</summary>
    /// <value>An array of 256 <c>uint16</c> values. A value of zero means the code point is single-byte and maps through subheader 0.</value>
    /// <seealso cref="SubHeaders"/>
    /// <seealso cref="SubHeader"/>
    public IReadOnlyList<ushort> SubHeaderKeys { get; init; } = [];

    /// <summary>Gets the subheaders referenced by <see cref="SubHeaderKeys"/>.</summary>
    /// <value>An array of <see cref="SubHeader"/> entries; entry 0 is the single-byte subheader, entries 1..N are the double-byte subheaders.</value>
    /// <seealso cref="SubHeaderKeys"/>
    /// <seealso cref="SubHeader"/>
    public IReadOnlyList<SubHeader> SubHeaders { get; init; } = [];

    /// <summary>Gets the glyph index array referenced by each subheader's <see cref="SubHeader.IdRangeOffset"/>.</summary>
    /// <value>A flat array of <c>uint16</c> glyph index values, offset-addressed by the subheaders.</value>
    /// <seealso cref="SubHeaders"/>
    /// <seealso cref="SubHeader.IdRangeOffset"/>
    public IReadOnlyList<ushort> GlyphIndexArray { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="codepoint">The code point to resolve.</param>
    /// <returns>The mapped glyph ID, or <c>0</c> when the code point is not mapped or the format does not cover it.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Code points above <c>0xFFFF</c> always return <c>0</c>; the format only handles 8-bit and 16-bit encodings.</description></item>
    /// <item><description>The high byte selects a subheader; the low byte is resolved within it either additively or through the glyph index array, depending on <see cref="SubHeader.IdRangeOffset"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-2-high-byte-mapping-through-table">format 2 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SubHeaderKeys"/>
    /// <seealso cref="SubHeaders"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-2-high-byte-mapping-through-table">OpenType specification: format 2</seealso>
    public override int GetGlyphId(int codepoint)
    {
        if ((uint)codepoint > 0xFFFF) return 0;

        int highByte = codepoint >> 8;
        int lowByte = codepoint & 0xFF;

        int subHeaderIndex = SubHeaderKeys[highByte] / 8;

        // subheader 0 is the single-byte subheader. A non-zero high byte that maps
        // there has no two-byte form in this subtable.
        if (subHeaderIndex == 0 && highByte != 0) return 0;
        if (subHeaderIndex >= SubHeaders.Count) return 0;

        SubHeader sh = SubHeaders[subHeaderIndex];
        if (lowByte < sh.FirstCode) return 0;
        int rangeIndex = lowByte - sh.FirstCode;
        if (rangeIndex >= sh.EntryCount) return 0;

        if (sh.IdRangeOffset == 0)
            return (lowByte + sh.IdDelta) & 0xFFFF;

        // The glyphIndexArray index is derived from the idRangeOffset field's own
        // position. That offset is (subHeaderStart + 6) - glyphIndexArrayStart =
        // 3 + 4*(subHeaderIndex - SubHeaders.Count) ushorts.
        int glyphIndex = (sh.IdRangeOffset >> 1) + rangeIndex
                       + 4 * (subHeaderIndex - SubHeaders.Count) + 3;
        if ((uint)glyphIndex >= (uint)GlyphIndexArray.Count) return 0;

        ushort glyphId = GlyphIndexArray[glyphIndex];
        if (glyphId == 0) return 0;
        return (glyphId + sh.IdDelta) & 0xFFFF;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the format word.</param>
    /// <param name="context">A <see cref="CmapSubtableContext"/> carrying the platform and encoding identifiers.</param>
    /// <returns>The parsed format-2 subtable.</returns>
    /// <exception cref="InvalidDataException">The declared length is too short for the number of subheaders.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="CmapSubtableContext"/>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The number of subheaders is derived from the maximum value in the subHeaderKeys array, divided by 8, plus one.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-2-high-byte-mapping-through-table">format 2 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapSubtableContext"/>
    /// <seealso cref="SubHeader"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-2-high-byte-mapping-through-table">OpenType specification: format 2</seealso>
    static CmapFormat2 IDerivedRecord<CmapSubtable, CmapFormat2>.Parse(
        ref Cursor cursor, object? context)
    {
        CmapSubtableContext ctx = CmapFormatHelpers.GetContext(context);

        // The dispatcher consumed the format word; the cursor is at offset 2.
        ushort length = cursor.ReadUInt16();
        cursor.ReadUInt16();   // language, unused

        ushort[] subHeaderKeys = cursor.ReadUInt16Array(256);

        int numSubHeaders = 1;
        for (int i = 0; i < 256; i++)
        {
            int idx = subHeaderKeys[i] / 8 + 1;
            if (idx > numSubHeaders) numSubHeaders = idx;
        }

        SubHeader[] subHeaders = cursor.ReadBigEndianStructArray<SubHeader>(numSubHeaders);

        // Remaining bytes are the glyph index array. The subtable header is 6 bytes,
        // subHeaderKeys is 512, each subHeader is 8.
        int consumed = 6 + 512 + numSubHeaders * 8;
        if (length < consumed)
            throw new InvalidDataException(
                $"'cmap' format 2 subtable length {length} is too short for {numSubHeaders} subheaders.");

        int glyphIndexCount = (length - consumed) / 2;
        ushort[] glyphIndexArray = glyphIndexCount > 0
            ? cursor.ReadUInt16Array(glyphIndexCount)
            : [];

        return new CmapFormat2
        {
            Format = 2,
            PlatformId = ctx.PlatformId,
            EncodingId = ctx.EncodingId,
            SubHeaderKeys = subHeaderKeys,
            SubHeaders = subHeaders,
            GlyphIndexArray = glyphIndexArray,
        };
    }

    /// <summary>A single cmap format 2 subheader. Blittable, size 8.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><see cref="IdRangeOffset"/> is measured in bytes from the location of the field itself, into the glyph index array that follows the subheader array. A value of zero means the mapping is purely additive: <c>glyph = (lowByte + IdDelta) mod 65536</c>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-2-high-byte-mapping-through-table">format 2 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapFormat2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-2-high-byte-mapping-through-table">OpenType specification: format 2</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct SubHeader : IEndianReversibleStruct<SubHeader>
    {
        /// <summary>Gets the first low byte covered by this subheader.</summary>
        /// <value>The lowest low-byte value for which this subheader is authoritative.</value>
        /// <seealso cref="EntryCount"/>
        /// <seealso cref="IdDelta"/>
        /// <seealso cref="IdRangeOffset"/>
        public ushort FirstCode;

        /// <summary>Gets the number of consecutive low bytes covered.</summary>
        /// <value>The count of valid low-byte values starting at <see cref="FirstCode"/>.</value>
        /// <seealso cref="FirstCode"/>
        /// <seealso cref="IdDelta"/>
        /// <seealso cref="IdRangeOffset"/>
        public ushort EntryCount;

        /// <summary>Gets the additive delta applied to the resolved glyph index.</summary>
        /// <value>The value added (mod 65536) to either the low byte or the looked-up glyph index.</value>
        /// <seealso cref="FirstCode"/>
        /// <seealso cref="IdRangeOffset"/>
        public short IdDelta;

        /// <summary>Gets the byte offset to the glyph index array, or 0 for the additive form.</summary>
        /// <value>A byte offset relative to the location of this field, or <c>0</c> when the mapping is purely additive.</value>
        /// <seealso cref="FirstCode"/>
        /// <seealso cref="IdDelta"/>
        public ushort IdRangeOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new subheader with each multi-byte field reversed.</returns>
        /// <remarks>All four fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-2-high-byte-mapping-through-table">OpenType specification: format 2</seealso>
        public static SubHeader ReverseEndianness(SubHeader v) => new()
        {
            FirstCode = BinaryPrimitives.ReverseEndianness(v.FirstCode),
            EntryCount = BinaryPrimitives.ReverseEndianness(v.EntryCount),
            IdDelta = BinaryPrimitives.ReverseEndianness(v.IdDelta),
            IdRangeOffset = BinaryPrimitives.ReverseEndianness(v.IdRangeOffset),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 4 — segment mapping to delta values
// ═══════════════════════════════════════════════════════════════════════════

/// <summary><c>cmap</c> format 4: segment mapping to delta values. The standard subtable for fonts that support only BMP characters (U+0000 to U+FFFF).</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The subtable partitions the BMP into segments, each with a contiguous range of code points mapped to glyphs either additively or through a shared glyph index array.</description></item>
/// <item><description>Segments are stored sorted by end code, with a final sentinel segment ending in <c>0xFFFF</c>; binary search is therefore valid.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-4-segment-mapping-to-delta-values">format 4 subtable</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CmapSubtable"/>
/// <seealso cref="CmapFormat12"/>
/// <seealso cref="Header"/>
/// <seealso cref="FindSegment(IReadOnlyList{ushort}, int)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-4-segment-mapping-to-delta-values">OpenType specification: format 4</seealso>
public sealed record CmapFormat4 : CmapSubtable, IDerivedRecord<CmapSubtable, CmapFormat4>
{
    /// <summary>Gets the number of segments.</summary>
    /// <value>The segment count, derived from <see cref="Header.SegCountX2"/> divided by two.</value>
    /// <seealso cref="EndCodes"/>
    /// <seealso cref="StartCodes"/>
    public int SegCount { get; init; }

    /// <summary>Gets the end codes, one per segment.</summary>
    /// <value>An array of <see cref="SegCount"/> values, sorted ascending. The final entry is always <c>0xFFFF</c>.</value>
    /// <seealso cref="StartCodes"/>
    /// <seealso cref="SegCount"/>
    public IReadOnlyList<ushort> EndCodes { get; init; } = [];

    /// <summary>Gets the start codes, one per segment.</summary>
    /// <value>An array of <see cref="SegCount"/> values, parallel to <see cref="EndCodes"/>.</value>
    /// <seealso cref="EndCodes"/>
    /// <seealso cref="SegCount"/>
    public IReadOnlyList<ushort> StartCodes { get; init; } = [];

    /// <summary>Gets the delta values, one per segment.</summary>
    /// <value>An array of <see cref="SegCount"/> signed deltas, parallel to <see cref="EndCodes"/>.</value>
    /// <seealso cref="EndCodes"/>
    /// <seealso cref="IdRangeOffsets"/>
    public IReadOnlyList<short> IdDeltas { get; init; } = [];

    /// <summary>Gets the range offsets, one per segment.</summary>
    /// <value>An array of <see cref="SegCount"/> byte offsets into <see cref="GlyphIdArray"/>, or <c>0</c> for additive segments. Parallel to <see cref="EndCodes"/>.</value>
    /// <seealso cref="IdDeltas"/>
    /// <seealso cref="GlyphIdArray"/>
    public IReadOnlyList<ushort> IdRangeOffsets { get; init; } = [];

    /// <summary>Gets the glyph ID array referenced by range offsets.</summary>
    /// <value>A flat array of <c>uint16</c> glyph IDs, indexed through <see cref="IdRangeOffsets"/>.</value>
    /// <seealso cref="IdRangeOffsets"/>
    public IReadOnlyList<ushort> GlyphIdArray { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="codepoint">The code point to resolve.</param>
    /// <returns>The mapped glyph ID, or <c>0</c> when the code point is not in any segment or maps to <c>.notdef</c>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Code points above <c>0xFFFF</c> always return <c>0</c>; the format is BMP-only.</description></item>
    /// <item><description>A segment with <see cref="IdRangeOffsets"/> entry <c>0</c> maps additively: <c>glyph = (codepoint + delta) mod 65536</c>.</description></item>
    /// <item><description>A segment with a non-zero range offset looks up the glyph ID from <see cref="GlyphIdArray"/>; a zero result there means unmapped.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FindSegment(IReadOnlyList{ushort}, int)"/>
    /// <seealso cref="IdDeltas"/>
    /// <seealso cref="IdRangeOffsets"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-4-segment-mapping-to-delta-values">OpenType specification: format 4</seealso>
    public override int GetGlyphId(int codepoint)
    {
        if ((uint)codepoint > 0xFFFF) return 0;

        var endCodes = EndCodes;
        var startCodes = StartCodes;
        var idDeltas = IdDeltas;
        var idRangeOffsets = IdRangeOffsets;
        var glyphIdArray = GlyphIdArray;

        int i = FindSegment(endCodes, codepoint);
        if (i >= SegCount) return 0;
        if (codepoint < startCodes[i]) return 0;

        int delta = idDeltas[i];
        ushort rangeOffset = idRangeOffsets[i];

        if (rangeOffset == 0)
            return (codepoint + delta) & 0xFFFF;

        int index = (rangeOffset >> 1) + i - SegCount + (codepoint - startCodes[i]);
        if ((uint)index >= (uint)glyphIdArray.Count) return 0;

        uint glyphId = glyphIdArray[index];
        if (glyphId == 0) return 0;
        return (int)((glyphId + delta) & 0xFFFF);
    }

    /// <summary>Finds the index of the segment whose end code is the smallest value greater than or equal to <paramref name="codepoint"/>.</summary>
    /// <param name="endCodes">The sorted array of segment end codes.</param>
    /// <param name="codepoint">The code point to locate.</param>
    /// <returns>The index of the matching segment, or <c>endCodes.Count</c> when <paramref name="codepoint"/> exceeds every end code.</returns>
    /// <remarks>The search is a binary search over <paramref name="endCodes"/>, which the format requires to be sorted ascending.</remarks>
    /// <seealso cref="EndCodes"/>
    /// <seealso cref="GetGlyphId(int)"/>
    public static int FindSegment(IReadOnlyList<ushort> endCodes, int codepoint)
    {
        int lo = 0, hi = endCodes.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (endCodes[mid] < codepoint) lo = mid + 1;
            else hi = mid;
        }
        return lo;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the format word.</param>
    /// <param name="context">A <see cref="CmapSubtableContext"/> carrying the platform and encoding identifiers.</param>
    /// <returns>The parsed format-4 subtable.</returns>
    /// <exception cref="InvalidDataException">The subtable has zero segments, or the declared length is too short for the segment arrays.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="CmapSubtableContext"/>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The reservedPad field between the end-code and start-code arrays is read and discarded.</description></item>
    /// <item><description>The glyph ID array length is inferred from the difference between the declared subtable length and the fixed-size segment arrays.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-4-segment-mapping-to-delta-values">format 4 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapSubtableContext"/>
    /// <seealso cref="Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-4-segment-mapping-to-delta-values">OpenType specification: format 4</seealso>
    static CmapFormat4 IDerivedRecord<CmapSubtable, CmapFormat4>.Parse(
        ref Cursor cursor, object? context)
    {
        CmapSubtableContext ctx = CmapFormatHelpers.GetContext(context);
        Header header = cursor.ReadBigEndianStruct<Header>();

        int segCount = header.SegCountX2 / 2;
        if (segCount == 0)
            throw new InvalidDataException("'cmap' format 4 subtable has zero segments.");

        ushort[] endCodes = cursor.ReadUInt16Array(segCount);
        cursor.ReadUInt16();   // reservedPad
        ushort[] startCodes = cursor.ReadUInt16Array(segCount);
        short[] idDeltas = cursor.ReadInt16Array(segCount);
        ushort[] idRangeOffsets = cursor.ReadUInt16Array(segCount);

        int headerBytes = 16 + 8 * segCount;
        if (header.Length < headerBytes)
            throw new InvalidDataException(
                $"'cmap' format 4 subtable length {header.Length} is too short for {segCount} segments.");

        int glyphIdArrayLength = (header.Length - headerBytes) / 2;
        ushort[] glyphIdArray = glyphIdArrayLength > 0
            ? cursor.ReadUInt16Array(glyphIdArrayLength)
            : [];

        return new CmapFormat4
        {
            Format = 4,
            PlatformId = ctx.PlatformId,
            EncodingId = ctx.EncodingId,
            SegCount = segCount,
            EndCodes = endCodes,
            StartCodes = startCodes,
            IdDeltas = idDeltas,
            IdRangeOffsets = idRangeOffsets,
            GlyphIdArray = glyphIdArray,
        };
    }

    /// <summary>The 12-byte format 4 header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The <see cref="SearchRange"/>, <see cref="EntrySelector"/>, and <see cref="RangeShift"/> fields are precomputed binary search parameters; modern parsers ignore them and compute the values directly.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-4-segment-mapping-to-delta-values">format 4 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapFormat4"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-4-segment-mapping-to-delta-values">OpenType specification: format 4</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the subtable length in bytes.</summary>
        /// <value>The total byte count of the subtable, including all segment arrays and the glyph ID array.</value>
        /// <seealso cref="SegCountX2"/>
        public ushort Length;         // +0

        /// <summary>Gets the language identifier.</summary>
        /// <value>Zero for a language-neutral subtable; otherwise a Macintosh language code.</value>
        /// <seealso cref="Length"/>
        public ushort Language;       // +2

        /// <summary>Gets twice the segment count.</summary>
        /// <value>An even number; the actual segment count is this value divided by two.</value>
        /// <seealso cref="Length"/>
        /// <seealso cref="SearchRange"/>
        public ushort SegCountX2;     // +4

        /// <summary>Gets the precomputed binary search range: <c>2 × 2^floor(log2(segCount))</c>.</summary>
        /// <value>A precomputed value used by legacy binary search code. Modern parsers ignore it.</value>
        /// <seealso cref="SegCountX2"/>
        /// <seealso cref="EntrySelector"/>
        /// <seealso cref="RangeShift"/>
        public ushort SearchRange;    // +6

        /// <summary>Gets the precomputed binary search entry selector: <c>floor(log2(segCount))</c>.</summary>
        /// <value>A precomputed value used by legacy binary search code. Modern parsers ignore it.</value>
        /// <seealso cref="SearchRange"/>
        /// <seealso cref="RangeShift"/>
        public ushort EntrySelector;  // +8

        /// <summary>Gets the precomputed binary search range shift: <c>2 × segCount − searchRange</c>.</summary>
        /// <value>A precomputed value used by legacy binary search code. Modern parsers ignore it.</value>
        /// <seealso cref="SearchRange"/>
        /// <seealso cref="EntrySelector"/>
        public ushort RangeShift;     // +10

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All six fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-4-segment-mapping-to-delta-values">OpenType specification: format 4</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Length = BinaryPrimitives.ReverseEndianness(v.Length),
            Language = BinaryPrimitives.ReverseEndianness(v.Language),
            SegCountX2 = BinaryPrimitives.ReverseEndianness(v.SegCountX2),
            SearchRange = BinaryPrimitives.ReverseEndianness(v.SearchRange),
            EntrySelector = BinaryPrimitives.ReverseEndianness(v.EntrySelector),
            RangeShift = BinaryPrimitives.ReverseEndianness(v.RangeShift),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 6 — trimmed table mapping
// ═══════════════════════════════════════════════════════════════════════════

/// <summary><c>cmap</c> format 6: trimmed table mapping. A dense array covering a single contiguous range of character codes.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The format is efficient when a font maps a single contiguous range, such as a symbol font or a legacy 8-bit encoding with a small offset.</description></item>
/// <item><description>Code points outside the covered range always return <c>0</c>.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-6-trimmed-table-mapping">format 6 subtable</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CmapSubtable"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-6-trimmed-table-mapping">OpenType specification: format 6</seealso>
public sealed record CmapFormat6 : CmapSubtable, IDerivedRecord<CmapSubtable, CmapFormat6>
{
    /// <summary>Gets the first character code covered.</summary>
    /// <value>The lowest code point in the covered range; the array is indexed by <c>codepoint - FirstCode</c>.</value>
    /// <seealso cref="GlyphIdArray"/>
    /// <seealso cref="GetGlyphId(int)"/>
    public ushort FirstCode { get; init; }

    /// <summary>Gets the glyph ID array, indexed by <c>codepoint - FirstCode</c>.</summary>
    /// <value>An array of <c>uint16</c> glyph IDs covering the contiguous range beginning at <see cref="FirstCode"/>.</value>
    /// <seealso cref="FirstCode"/>
    /// <seealso cref="GetGlyphId(int)"/>
    public IReadOnlyList<ushort> GlyphIdArray { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="codepoint">The code point to resolve.</param>
    /// <returns>The mapped glyph ID, or <c>0</c> when the code point is outside the covered range.</returns>
    /// <seealso cref="FirstCode"/>
    /// <seealso cref="GlyphIdArray"/>
    public override int GetGlyphId(int codepoint)
    {
        int offset = codepoint - FirstCode;
        return (uint)offset < (uint)GlyphIdArray.Count ? GlyphIdArray[offset] : 0;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the format word.</param>
    /// <param name="context">A <see cref="CmapSubtableContext"/> carrying the platform and encoding identifiers.</param>
    /// <returns>The parsed format-6 subtable.</returns>
    /// <exception cref="InvalidDataException">The declared length does not match <c>10 + 2 × entryCount</c>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="CmapSubtableContext"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-6-trimmed-table-mapping">format 6 subtable</see> in the OpenType specification.</remarks>
    /// <seealso cref="CmapSubtableContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-6-trimmed-table-mapping">OpenType specification: format 6</seealso>
    static CmapFormat6 IDerivedRecord<CmapSubtable, CmapFormat6>.Parse(
        ref Cursor cursor, object? context)
    {
        CmapSubtableContext ctx = CmapFormatHelpers.GetContext(context);
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Length != 10 + header.EntryCount * 2)
            throw new InvalidDataException(
                $"'cmap' format 6 subtable length {header.Length} does not match entryCount {header.EntryCount}.");

        return new CmapFormat6
        {
            Format = 6,
            PlatformId = ctx.PlatformId,
            EncodingId = ctx.EncodingId,
            FirstCode = header.FirstCode,
            GlyphIdArray = cursor.ReadUInt16Array(header.EntryCount),
        };
    }

    /// <summary>The 8-byte format 6 header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-6-trimmed-table-mapping">format 6 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapFormat6"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-6-trimmed-table-mapping">OpenType specification: format 6</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the subtable length in bytes.</summary>
        /// <value>The total byte count, equal to <c>10 + 2 × entryCount</c>.</value>
        /// <seealso cref="EntryCount"/>
        public ushort Length;      // +0

        /// <summary>Gets the language identifier.</summary>
        /// <value>Zero for a language-neutral subtable; otherwise a Macintosh language code.</value>
        /// <seealso cref="Length"/>
        public ushort Language;    // +2

        /// <summary>Gets the first code point covered.</summary>
        /// <value>The lowest code point in the covered range.</value>
        /// <seealso cref="EntryCount"/>
        public ushort FirstCode;   // +4

        /// <summary>Gets the number of code points covered.</summary>
        /// <value>The count of consecutive code points starting at <see cref="FirstCode"/>.</value>
        /// <seealso cref="FirstCode"/>
        public ushort EntryCount;  // +6

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All four fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-6-trimmed-table-mapping">OpenType specification: format 6</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Length = BinaryPrimitives.ReverseEndianness(v.Length),
            Language = BinaryPrimitives.ReverseEndianness(v.Language),
            FirstCode = BinaryPrimitives.ReverseEndianness(v.FirstCode),
            EntryCount = BinaryPrimitives.ReverseEndianness(v.EntryCount),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 8 — mixed 16-bit and 32-bit coverage
// ═══════════════════════════════════════════════════════════════════════════

/// <summary><c>cmap</c> format 8: mixed 16-bit and 32-bit coverage. Deprecated by the specification in favour of format 12.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The format uses an 8192-byte <c>is32</c> bit array to mark which BMP code points are 32-bit only, then resolves the code point through a group array shared in layout with formats 12 and 13.</description></item>
/// <item><description>Rarely encountered in practice; format 12 supersedes it for supplementary-plane support.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-8-mixed-16-bit-and-32-bit-coverage">format 8 subtable</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CmapSubtable"/>
/// <seealso cref="CmapFormat12"/>
/// <seealso cref="SequentialMapGroup"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-8-mixed-16-bit-and-32-bit-coverage">OpenType specification: format 8</seealso>
public sealed record CmapFormat8 : CmapSubtable, IDerivedRecord<CmapSubtable, CmapFormat8>
{
    /// <summary>Gets the 8192-byte <c>is32</c> bit array marking which 16-bit codepoints are 32-bit.</summary>
    /// <value>An 8192-byte array interpreted as 65536 bits, one per BMP code point. A set bit means the code point is 32-bit in this subtable.</value>
    /// <seealso cref="Groups"/>
    public IReadOnlyList<byte> Is32 { get; init; } = [];

    /// <summary>Gets the sequential map groups.</summary>
    /// <value>An array of <see cref="SequentialMapGroup"/> entries sorted ascending by <see cref="SequentialMapGroup.StartCharCode"/>.</value>
    /// <seealso cref="Is32"/>
    /// <seealso cref="SequentialMapGroup"/>
    public IReadOnlyList<SequentialMapGroup> Groups { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="codepoint">The code point to resolve.</param>
    /// <returns>The mapped glyph ID, or <c>0</c> when the code point is not in any group.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>For BMP code points, the <c>is32</c> bit must be set; otherwise the code point is treated as unmapped even if a group covers it.</description></item>
    /// <item><description>Supplementary-plane code points bypass the <c>is32</c> check and resolve directly through the group array.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Is32"/>
    /// <seealso cref="Groups"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-8-mixed-16-bit-and-32-bit-coverage">OpenType specification: format 8</seealso>
    public override int GetGlyphId(int codepoint)
    {
        if (codepoint < 0) return 0;

        if ((uint)codepoint <= 0xFFFF)
        {
            int b = codepoint >> 3;
            int bit = codepoint & 7;
            if ((Is32[b] & (1 << bit)) == 0) return 0;
        }

        var groups = Groups;
        int i = CmapGroupSearch.FindGroup(groups, (uint)codepoint);
        if (i < 0) return 0;
        if ((uint)codepoint > groups[i].EndCharCode) return 0;

        return (int)(groups[i].StartGlyphId + ((uint)codepoint - groups[i].StartCharCode));
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the format word.</param>
    /// <param name="context">A <see cref="CmapSubtableContext"/> carrying the platform and encoding identifiers.</param>
    /// <returns>The parsed format-8 subtable.</returns>
    /// <exception cref="InvalidDataException">The declared group count exceeds the safety limit.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="CmapSubtableContext"/>.</exception>
    /// <remarks>The 8192-byte <c>is32</c> array is read unconditionally before the group count.</remarks>
    /// <seealso cref="CmapSubtableContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-8-mixed-16-bit-and-32-bit-coverage">OpenType specification: format 8</seealso>
    static CmapFormat8 IDerivedRecord<CmapSubtable, CmapFormat8>.Parse(ref Cursor cursor, object? context)
    {
        CmapSubtableContext ctx = CmapFormatHelpers.GetContext(context);
        Header header = cursor.ReadBigEndianStruct<Header>();

        byte[] is32 = cursor.ReadBytes(8192);

        uint numGroups = cursor.ReadUInt32();
        if (numGroups > 1_000_000)
            throw new InvalidDataException(
                $"'cmap' format 8 subtable declares {numGroups} groups, which exceeds the safety limit.");

        return new CmapFormat8
        {
            Format = 8,
            PlatformId = ctx.PlatformId,
            EncodingId = ctx.EncodingId,
            Is32 = is32,
            Groups = cursor.ReadBigEndianStructArray<SequentialMapGroup>((int)numGroups),
        };
    }

    /// <summary>The 8-byte format 8 header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is immediately followed by the 8192-byte <c>is32</c> array, then the group count and group array.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-8-mixed-16-bit-and-32-bit-coverage">format 8 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapFormat8"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-8-mixed-16-bit-and-32-bit-coverage">OpenType specification: format 8</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the reserved field. Must be zero.</summary>
        /// <value>The constant <c>0</c> for a conforming subtable.</value>
        /// <seealso cref="Length"/>
        public ushort Reserved;  // +0

        /// <summary>Gets the subtable length in bytes.</summary>
        /// <value>The total byte count of the subtable including the <c>is32</c> array and group data.</value>
        /// <seealso cref="Reserved"/>
        /// <seealso cref="Language"/>
        public uint Length;      // +2

        /// <summary>Gets the language identifier.</summary>
        /// <value>Zero for a language-neutral subtable; otherwise a Macintosh language code.</value>
        /// <seealso cref="Length"/>
        public uint Language;    // +6

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-8-mixed-16-bit-and-32-bit-coverage">OpenType specification: format 8</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Reserved = BinaryPrimitives.ReverseEndianness(v.Reserved),
            Length = BinaryPrimitives.ReverseEndianness(v.Length),
            Language = BinaryPrimitives.ReverseEndianness(v.Language),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 10 — trimmed array
// ═══════════════════════════════════════════════════════════════════════════

/// <summary><c>cmap</c> format 10: trimmed array. A dense 32-bit array covering a single contiguous range of character codes.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The 32-bit counterpart of format 6; supports supplementary-plane code points in a single contiguous range.</description></item>
/// <item><description>Rarely encountered; format 12 supersedes it because it handles multiple ranges.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-10-trimmed-array">format 10 subtable</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CmapSubtable"/>
/// <seealso cref="CmapFormat6"/>
/// <seealso cref="CmapFormat12"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-10-trimmed-array">OpenType specification: format 10</seealso>
public sealed record CmapFormat10 : CmapSubtable, IDerivedRecord<CmapSubtable, CmapFormat10>
{
    /// <summary>Gets the first character code covered.</summary>
    /// <value>The lowest code point in the covered range; the array is indexed by <c>codepoint - StartCharCode</c>.</value>
    /// <seealso cref="Glyphs"/>
    public uint StartCharCode { get; init; }

    /// <summary>Gets the glyph ID array, indexed by <c>codepoint - StartCharCode</c>.</summary>
    /// <value>An array of <c>uint16</c> glyph IDs covering the contiguous range beginning at <see cref="StartCharCode"/>.</value>
    /// <seealso cref="StartCharCode"/>
    public IReadOnlyList<ushort> Glyphs { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="codepoint">The code point to resolve.</param>
    /// <returns>The mapped glyph ID, or <c>0</c> when the code point is outside the covered range.</returns>
    /// <seealso cref="StartCharCode" />
    /// <seealso cref="Glyphs"/>
    public override int GetGlyphId(int codepoint)
    {
        if (codepoint < 0) return 0;
        long offset = (long)codepoint - StartCharCode;
        return (uint)offset < (uint)Glyphs.Count ? Glyphs[(int)offset] : 0;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the format word.</param>
    /// <param name="context">A <see cref="CmapSubtableContext"/> carrying the platform and encoding identifiers.</param>
    /// <returns>The parsed format-10 subtable.</returns>
    /// <exception cref="InvalidDataException">The declared character count exceeds the safety limit.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="CmapSubtableContext"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-10-trimmed-array">format 10 subtable</see> in the OpenType specification.</remarks>
    /// <seealso cref="CmapSubtableContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-10-trimmed-array">OpenType specification: format 10</seealso>
    static CmapFormat10 IDerivedRecord<CmapSubtable, CmapFormat10>.Parse(ref Cursor cursor, object? context)
    {
        CmapSubtableContext ctx = CmapFormatHelpers.GetContext(context);
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.NumChars > 1_000_000)
            throw new InvalidDataException(
                $"'cmap' format 10 subtable declares {header.NumChars} characters, which exceeds the safety limit.");

        return new CmapFormat10
        {
            Format = 10,
            PlatformId = ctx.PlatformId,
            EncodingId = ctx.EncodingId,
            StartCharCode = header.StartCharCode,
            Glyphs = cursor.ReadUInt16Array((int)header.NumChars),
        };
    }

    /// <summary>The 16-byte format 10 header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-10-trimmed-array">format 10 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapFormat10"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-10-trimmed-array">OpenType specification: format 10</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the reserved field. Must be zero.</summary>
        /// <value>The constant <c>0</c> for a conforming subtable.</value>
        /// <seealso cref="Length"/>
        public ushort Reserved;         // +0

        /// <summary>Gets the subtable length in bytes.</summary>
        /// <value>The total byte count including the glyph array.</value>
        /// <seealso cref="Reserved"/>
        /// <seealso cref="NumChars"/>
        public uint Length;             // +2

        /// <summary>Gets the language identifier.</summary>
        /// <value>Zero for a language-neutral subtable; otherwise a Macintosh language code.</value>
        /// <seealso cref="Length"/>
        public uint Language;           // +6

        /// <summary>Gets the first code point covered.</summary>
        /// <value>The lowest code point in the covered range.</value>
        /// <seealso cref="NumChars"/>
        public uint StartCharCode;      // +10

        /// <summary>Gets the number of code points covered.</summary>
        /// <value>The count of consecutive code points starting at <see cref="StartCharCode"/>.</value>
        /// <seealso cref="StartCharCode"/>
        public uint NumChars;           // +14

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All five fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-10-trimmed-array">OpenType specification: format 10</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Reserved = BinaryPrimitives.ReverseEndianness(v.Reserved),
            Length = BinaryPrimitives.ReverseEndianness(v.Length),
            Language = BinaryPrimitives.ReverseEndianness(v.Language),
            StartCharCode = BinaryPrimitives.ReverseEndianness(v.StartCharCode),
            NumChars = BinaryPrimitives.ReverseEndianness(v.NumChars),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Sequential map group + search helper (shared by formats 8, 12, 13)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A contiguous character-code range mapped to glyph IDs. Blittable, size 12.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Shared by formats 8, 12, and 13. The group covers code points from <see cref="StartCharCode"/> through <see cref="EndCharCode"/> inclusive.</description></item>
/// <item><description>Formats 8 and 12 map each code point to a distinct glyph ID (<c>StartGlyphId + (codepoint - StartCharCode)</c>); format 13 maps every code point to <see cref="StartGlyphId"/>.</description></item>
/// <item><description>Groups are sorted ascending by <see cref="StartCharCode"/> and do not overlap.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-12-segmented-coverage">format 12 subtable</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CmapFormat8"/>
/// <seealso cref="CmapFormat12"/>
/// <seealso cref="CmapFormat13"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-12-segmented-coverage">OpenType specification: format 12</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct SequentialMapGroup : IEndianReversibleStruct<SequentialMapGroup>
{
    /// <summary>Gets the first code point in the range (inclusive).</summary>
    /// <value>The lowest code point covered by this group.</value>
    /// <seealso cref="EndCharCode"/>
    /// <seealso cref="StartGlyphId"/>
    public uint StartCharCode;   // +0

    /// <summary>Gets the last code point in the range (inclusive).</summary>
    /// <value>The highest code point covered by this group.</value>
    /// <seealso cref="StartCharCode"/>
    /// <seealso cref="StartGlyphId"/>
    public uint EndCharCode;     // +4

    /// <summary>Gets the first glyph ID in the range.</summary>
    /// <value>The glyph ID assigned to <see cref="StartCharCode"/> in formats 8 and 12, and to every code point in the range in format 13.</value>
    /// <seealso cref="StartCharCode"/>
    /// <seealso cref="EndCharCode"/>
    public uint StartGlyphId;    // +8

    /// <inheritdoc/>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new group with each multi-byte field reversed.</returns>
    /// <remarks>All three fields are <c>uint32</c> and are reversed independently.</remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-12-segmented-coverage">OpenType specification: format 12</seealso>
    public static SequentialMapGroup ReverseEndianness(SequentialMapGroup v) => new()
    {
        StartCharCode = BinaryPrimitives.ReverseEndianness(v.StartCharCode),
        EndCharCode = BinaryPrimitives.ReverseEndianness(v.EndCharCode),
        StartGlyphId = BinaryPrimitives.ReverseEndianness(v.StartGlyphId),
    };
}

/// <summary>Binary search over a sorted sequential map group array.</summary>
/// <remarks>File-scoped; not part of the public API. Shared by formats 8, 12, and 13, all of which store groups in the same sorted order.</remarks>
file static class CmapGroupSearch
{
    /// <summary>Finds the index of the group containing <paramref name="codepoint"/>, or the group immediately before it.</summary>
    /// <param name="groups">The sorted group array.</param>
    /// <param name="codepoint">The code point to locate.</param>
    /// <returns>The index of the highest group whose <see cref="SequentialMapGroup.StartCharCode"/> is less than or equal to <paramref name="codepoint"/>, or <c>-1</c> when the code point precedes every group.</returns>
    /// <remarks>The caller must additionally check <see cref="SequentialMapGroup.EndCharCode"/> to confirm the code point is actually covered by the returned group.</remarks>
    /// <seealso cref="SequentialMapGroup"/>
    public static int FindGroup(IReadOnlyList<SequentialMapGroup> groups, uint codepoint)
    {
        int lo = 0, hi = groups.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            if (groups[mid].StartCharCode <= codepoint) lo = mid + 1;
            else hi = mid - 1;
        }
        return hi;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 12 — segmented coverage
// ═══════════════════════════════════════════════════════════════════════════

/// <summary><c>cmap</c> format 12: segmented coverage. The standard subtable for fonts that support Unicode supplementary-plane characters (U+10000 to U+10FFFF).</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The format maps a sorted array of code point ranges to a contiguous glyph ID range, allowing compact coverage of large Unicode regions.</description></item>
/// <item><description>The layout is shared with format 13; the difference is that format 12 assigns consecutive glyph IDs within a group while format 13 assigns the same glyph ID to every code point.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-12-segmented-coverage">format 12 subtable</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CmapSubtable"/>
/// <seealso cref="CmapFormat13"/>
/// <seealso cref="SequentialMapGroup"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-12-segmented-coverage">OpenType specification: format 12</seealso>
public sealed record CmapFormat12 : CmapSubtable, IDerivedRecord<CmapSubtable, CmapFormat12>
{
    /// <summary>Gets the sequential map groups.</summary>
    /// <value>An array of <see cref="SequentialMapGroup"/> entries sorted ascending by <see cref="SequentialMapGroup.StartCharCode"/>.</value>
    /// <seealso cref="SequentialMapGroup"/>
    /// <seealso cref="GetGlyphId(int)"/>
    public IReadOnlyList<SequentialMapGroup> Groups { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="codepoint">The code point to resolve.</param>
    /// <returns>The mapped glyph ID, or <c>0</c> when the code point is not in any group.</returns>
    /// <remarks>Within a group, the glyph ID is <c>StartGlyphId + (codepoint - StartCharCode)</c>; consecutive code points map to consecutive glyph IDs.</remarks>
    /// <seealso cref="Groups"/>
    public override int GetGlyphId(int codepoint)
    {
        if (codepoint < 0) return 0;

        var groups = Groups;
        int i = CmapGroupSearch.FindGroup(groups, (uint)codepoint);
        if (i < 0) return 0;
        if ((uint)codepoint > groups[i].EndCharCode) return 0;

        return (int)(groups[i].StartGlyphId + ((uint)codepoint - groups[i].StartCharCode));
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the format word.</param>
    /// <param name="context">A <see cref="CmapSubtableContext"/> carrying the platform and encoding identifiers.</param>
    /// <returns>The parsed format-12 subtable.</returns>
    /// <exception cref="InvalidDataException">The declared group count exceeds the safety limit.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="CmapSubtableContext"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-12-segmented-coverage">format 12 subtable</see> in the OpenType specification.</remarks>
    /// <seealso cref="CmapSubtableContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-12-segmented-coverage">OpenType specification: format 12</seealso>
    static CmapFormat12 IDerivedRecord<CmapSubtable, CmapFormat12>.Parse(
        ref Cursor cursor, object? context)
    {
        CmapSubtableContext ctx = CmapFormatHelpers.GetContext(context);
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.NumGroups > 1_000_000)
            throw new InvalidDataException(
                $"'cmap' format 12 subtable declares {header.NumGroups} groups, which exceeds the safety limit.");

        return new CmapFormat12
        {
            Format = 12,
            PlatformId = ctx.PlatformId,
            EncodingId = ctx.EncodingId,
            Groups = cursor.ReadBigEndianStructArray<SequentialMapGroup>((int)header.NumGroups),
        };
    }

    /// <summary>The 12-byte format 12 header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-12-segmented-coverage">format 12 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapFormat12"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-12-segmented-coverage">OpenType specification: format 12</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the reserved field. Must be zero.</summary>
        /// <value>The constant <c>0</c> for a conforming subtable.</value>
        /// <seealso cref="Length"/>
        public ushort Reserved;     // +0

        /// <summary>Gets the subtable length in bytes.</summary>
        /// <value>The total byte count including the group array.</value>
        /// <seealso cref="Reserved"/>
        /// <seealso cref="NumGroups"/>
        public uint Length;         // +2

        /// <summary>Gets the language identifier.</summary>
        /// <value>Zero for a language-neutral subtable; otherwise a Macintosh language code.</value>
        /// <seealso cref="Length"/>
        public uint Language;       // +6

        /// <summary>Gets the number of sequential map groups.</summary>
        /// <value>The count of <see cref="SequentialMapGroup"/> entries in the group array.</value>
        /// <seealso cref="Length"/>
        public uint NumGroups;      // +10

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All four fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-12-segmented-coverage">OpenType specification: format 12</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Reserved = BinaryPrimitives.ReverseEndianness(v.Reserved),
            Length = BinaryPrimitives.ReverseEndianness(v.Length),
            Language = BinaryPrimitives.ReverseEndianness(v.Language),
            NumGroups = BinaryPrimitives.ReverseEndianness(v.NumGroups),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 13 — many-to-one range mappings
// ═══════════════════════════════════════════════════════════════════════════

/// <summary><c>cmap</c> format 13: many-to-one range mappings. Same layout as format 12, but every codepoint in a group maps to a single glyph ID.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Used for last-resort fallback and for fonts that map large ranges of code points to a single glyph, such as a "missing" glyph or a shared ideographic variant.</description></item>
/// <item><description>The on-disk layout is identical to format 12; the difference is entirely in the interpretation of <see cref="SequentialMapGroup.StartGlyphId"/>.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-13-many-to-one-range-mappings">format 13 subtable</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CmapSubtable"/>
/// <seealso cref="CmapFormat12"/>
/// <seealso cref="SequentialMapGroup"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-13-many-to-one-range-mappings">OpenType specification: format 13</seealso>
public sealed record CmapFormat13 : CmapSubtable, IDerivedRecord<CmapSubtable, CmapFormat13>
{
    /// <summary>Gets the sequential map groups.</summary>
    /// <value>An array of <see cref="SequentialMapGroup"/> entries sorted ascending by <see cref="SequentialMapGroup.StartCharCode"/>.</value>
    /// <seealso cref="SequentialMapGroup"/>
    /// <seealso cref="GetGlyphId(int)"/>
    public IReadOnlyList<SequentialMapGroup> Groups { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="codepoint">The code point to resolve.</param>
    /// <returns>The single glyph ID assigned to the containing group, or <c>0</c> when the code point is not in any group.</returns>
    /// <remarks>Every code point within a group maps to <see cref="SequentialMapGroup.StartGlyphId"/>; the code point's offset within the range is not used.</remarks>
    /// <seealso cref="Groups"/>
    public override int GetGlyphId(int codepoint)
    {
        if (codepoint < 0) return 0;

        var groups = Groups;
        int i = CmapGroupSearch.FindGroup(groups, (uint)codepoint);
        if (i < 0) return 0;
        if ((uint)codepoint > groups[i].EndCharCode) return 0;

        return (int)groups[i].StartGlyphId;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the format word.</param>
    /// <param name="context">A <see cref="CmapSubtableContext"/> carrying the platform and encoding identifiers.</param>
    /// <returns>The parsed format-13 subtable.</returns>
    /// <exception cref="InvalidDataException">The declared group count exceeds the safety limit.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="CmapSubtableContext"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-13-many-to-one-range-mappings">format 13 subtable</see> in the OpenType specification.</remarks>
    /// <seealso cref="CmapSubtableContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-13-many-to-one-range-mappings">OpenType specification: format 13</seealso>
    static CmapFormat13 IDerivedRecord<CmapSubtable, CmapFormat13>.Parse(
        ref Cursor cursor, object? context)
    {
        CmapSubtableContext ctx = CmapFormatHelpers.GetContext(context);
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.NumGroups > 1_000_000)
            throw new InvalidDataException(
                $"'cmap' format 13 subtable declares {header.NumGroups} groups, which exceeds the safety limit.");

        return new CmapFormat13
        {
            Format = 13,
            PlatformId = ctx.PlatformId,
            EncodingId = ctx.EncodingId,
            Groups = cursor.ReadBigEndianStructArray<SequentialMapGroup>((int)header.NumGroups),
        };
    }

    /// <summary>The 12-byte format 13 header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header layout is identical to format 12's; the two formats are distinguished only by the format word and the interpretation of <see cref="SequentialMapGroup.StartGlyphId"/>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-13-many-to-one-range-mappings">format 13 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapFormat13"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-13-many-to-one-range-mappings">OpenType specification: format 13</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the reserved field. Must be zero.</summary>
        /// <value>The constant <c>0</c> for a conforming subtable.</value>
        /// <seealso cref="Length"/>
        public ushort Reserved;     // +0

        /// <summary>Gets the subtable length in bytes.</summary>
        /// <value>The total byte count including the group array.</value>
        /// <seealso cref="Reserved"/>
        /// <seealso cref="NumGroups"/>
        public uint Length;         // +2

        /// <summary>Gets the language identifier.</summary>
        /// <value>Zero for a language-neutral subtable; otherwise a Macintosh language code.</value>
        /// <seealso cref="Length"/>
        public uint Language;       // +6

        /// <summary>Gets the number of sequential map groups.</summary>
        /// <value>The count of <see cref="SequentialMapGroup"/> entries in the group array.</value>
        /// <seealso cref="Length"/>
        public uint NumGroups;      // +10

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All four fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-13-many-to-one-range-mappings">OpenType specification: format 13</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Reserved = BinaryPrimitives.ReverseEndianness(v.Reserved),
            Length = BinaryPrimitives.ReverseEndianness(v.Length),
            Language = BinaryPrimitives.ReverseEndianness(v.Language),
            NumGroups = BinaryPrimitives.ReverseEndianness(v.NumGroups),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 14 — Unicode variation sequences
// ═══════════════════════════════════════════════════════════════════════════

/// <summary><c>cmap</c> format 14: Unicode variation sequences. Maps (base character, variation selector) pairs to glyph IDs.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Variation sequences allow a base character to be rendered differently depending on a following variation selector code point (U+FE00–U+FE0F, U+E0100–U+E01EF), commonly used for CJK ideographic variants and emoji presentation.</description></item>
/// <item><description>Format 14 is the only cmap format that does not map individual code points; <see cref="GetGlyphId(int)"/> always returns <c>0</c>. Use <see cref="GetVariationGlyphId(int, int)"/> instead.</description></item>
/// <item><description>Each variation selector record carries two optional sub-tables: a default UVS table listing code points that use the glyph the ordinary cmap already assigns, and a non-default UVS table listing explicit (code point, glyph ID) mappings.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">format 14 subtable</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CmapSubtable"/>
/// <seealso cref="DefaultUVSTable"/>
/// <seealso cref="NonDefaultUVSTable"/>
/// <seealso cref="VariationSelectorRecord"/>
/// <seealso cref="CmapTable.GetVariationGlyphId(int, int)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
public sealed record CmapFormat14 : CmapSubtable, IDerivedRecord<CmapSubtable, CmapFormat14>
{
    /// <summary>Gets the variation selector records.</summary>
    /// <value>An array of <see cref="VariationSelectorRecord"/> entries, one per variation selector supported by this subtable.</value>
    /// <seealso cref="DefaultUVS"/>
    /// <seealso cref="NonDefaultUVS"/>
    /// <seealso cref="VariationSelectorRecord"/>
    public IReadOnlyList<VariationSelectorRecord> Records { get; init; } = [];

    /// <summary>Gets the default UVS tables, parallel to <see cref="Records"/>; entries may be <c>null</c>.</summary>
    /// <value>An array of <see cref="DefaultUVSTable"/> references, one per variation selector, or <c>null</c> where the record declares no default UVS table.</value>
    /// <seealso cref="Records"/>
    /// <seealso cref="NonDefaultUVS"/>
    /// <seealso cref="DefaultUVSTable"/>
    public IReadOnlyList<DefaultUVSTable?> DefaultUVS { get; init; } = [];

    /// <summary>Gets the non-default UVS tables, parallel to <see cref="Records"/>; entries may be <c>null</c>.</summary>
    /// <value>An array of <see cref="NonDefaultUVSTable"/> references, one per variation selector, or <c>null</c> where the record declares no non-default UVS table.</value>
    /// <seealso cref="Records"/>
    /// <seealso cref="DefaultUVS"/>
    /// <seealso cref="NonDefaultUVSTable"/>
    public IReadOnlyList<NonDefaultUVSTable?> NonDefaultUVS { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="codepoint">Ignored.</param>
    /// <returns>Always <c>0</c>.</returns>
    /// <remarks>Format 14 does not map individual code points; use <see cref="GetVariationGlyphId(int, int)"/> to resolve a variation sequence.</remarks>
    /// <seealso cref="GetVariationGlyphId(int, int)"/>
    public override int GetGlyphId(int codepoint) => 0;

    /// <summary>Returns the glyph ID for the variation sequence, or <c>-1</c> if not defined.</summary>
    /// <param name="baseCodepoint">The base character code point.</param>
    /// <param name="variationSelector">The variation selector code point.</param>
    /// <returns>The resolved glyph ID from the non-default UVS table, or <c>0</c> when the sequence is defined in the default UVS table, or <c>-1</c> when the sequence is not defined.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>A non-default mapping takes precedence over a default mapping when both are present.</description></item>
    /// <item><description>Returns <c>-1</c>, not <c>0</c>, to distinguish "sequence undefined" from "sequence maps to <c>.notdef</c>".</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="NonDefaultUVSTable.TryGetGlyphId(int, out int)"/>
    /// <seealso cref="DefaultUVSTable.Contains(int)"/>
    /// <seealso cref="FindSelector(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
    public int GetVariationGlyphId(int baseCodepoint, int variationSelector)
    {
        int i = FindSelector(variationSelector);
        if (i < 0) return -1;

        if (NonDefaultUVS[i] is { } nonDefault && nonDefault.TryGetGlyphId(baseCodepoint, out int gid))
            return gid;

        if (DefaultUVS[i] is { } @default && @default.Contains(baseCodepoint))
            return 0;

        return -1;
    }

    /// <summary>Finds the index of the variation selector record for <paramref name="variationSelector"/>.</summary>
    /// <param name="variationSelector">The variation selector code point to search for.</param>
    /// <returns>The index of the matching record in <see cref="Records"/>, or <c>-1</c> when no record declares that selector.</returns>
    /// <remarks>The lookup is linear over <see cref="Records"/>. The specification does not require the records to be sorted.</remarks>
    /// <seealso cref="Records"/>
    /// <seealso cref="GetVariationGlyphId(int, int)"/>
    public int FindSelector(int variationSelector)
    {
        var records = Records;
        for (int i = 0; i < records.Count; i++)
            if ((int)records[i].VarSelector.Value == variationSelector) return i;
        return -1;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the format word.</param>
    /// <param name="context">A <see cref="CmapSubtableContext"/> carrying the platform and encoding identifiers.</param>
    /// <returns>The parsed format-14 subtable with both UVS tables resolved.</returns>
    /// <exception cref="InvalidDataException">The declared record count exceeds the safety limit.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="CmapSubtableContext"/>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The variation selector records are read first, then each referenced UVS table is parsed by offset relative to the start of the subtable.</description></item>
    /// <item><description>A zero UVS table offset means the table is absent; the corresponding array entry is <c>null</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">format 14 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapSubtableContext"/>
    /// <seealso cref="DefaultUVSTable"/>
    /// <seealso cref="NonDefaultUVSTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
    static CmapFormat14 IDerivedRecord<CmapSubtable, CmapFormat14>.Parse(
        ref Cursor cursor, object? context)
    {
        CmapSubtableContext ctx = CmapFormatHelpers.GetContext(context);
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.NumRecords > 10_000)
            throw new InvalidDataException(
                $"'cmap' format 14 subtable declares {header.NumRecords} variation selectors, which exceeds the safety limit.");

        int n = (int)header.NumRecords;
        VariationSelectorRecord[] records = cursor.ReadBigEndianStructArray<VariationSelectorRecord>(n);

        var defaults = new DefaultUVSTable?[n];
        var nonDefaults = new NonDefaultUVSTable?[n];

        for (int i = 0; i < n; i++)
        {
            if (records[i].DefaultUVSOffset != 0)
                defaults[i] = cursor.Source.ParseRecordAt<DefaultUVSTable>(records[i].DefaultUVSOffset);

            if (records[i].NonDefaultUVSOffset != 0)
                nonDefaults[i] = cursor.Source.ParseRecordAt<NonDefaultUVSTable>(records[i].NonDefaultUVSOffset);
        }

        return new CmapFormat14
        {
            Format = 14,
            PlatformId = ctx.PlatformId,
            EncodingId = ctx.EncodingId,
            Records = records,
            DefaultUVS = defaults,
            NonDefaultUVS = nonDefaults,
        };
    }

    /// <summary>The 8-byte format 14 header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by the variation selector record array, then by the UVS tables referenced by each record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">format 14 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapFormat14"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the subtable length in bytes.</summary>
        /// <value>The total byte count including the record array and all referenced UVS tables.</value>
        /// <seealso cref="NumRecords"/>
        public uint Length;          // +0

        /// <summary>Gets the number of variation selector records.</summary>
        /// <value>The count of <see cref="VariationSelectorRecord"/> entries following the header.</value>
        /// <seealso cref="Length"/>
        /// <seealso cref="VariationSelectorRecord"/>
        public uint NumRecords;      // +4

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint32</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Length = BinaryPrimitives.ReverseEndianness(v.Length),
            NumRecords = BinaryPrimitives.ReverseEndianness(v.NumRecords),
        };
    }

    /// <summary>An 11-byte variation selector record. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Each record names a variation selector code point and carries two offsets — one to a default UVS table, one to a non-default UVS table — either of which may be zero to indicate absence.</description></item>
    /// <item><description>Both UVS table offsets are measured from the start of the format 14 subtable.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">format 14 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapFormat14"/>
    /// <seealso cref="DefaultUVSTable"/>
    /// <seealso cref="NonDefaultUVSTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct VariationSelectorRecord : IEndianReversibleStruct<VariationSelectorRecord>
    {
        /// <summary>Gets the variation selector code point.</summary>
        /// <value>A 24-bit code point in the range U+FE00–U+FE0F or U+E0100–U+E01EF.</value>
        /// <seealso cref="DefaultUVSOffset"/>
        /// <seealso cref="NonDefaultUVSOffset"/>
        public UInt24 VarSelector;             // +0

        /// <summary>Gets the offset to the default UVS table, or zero when absent.</summary>
        /// <value>The byte offset of a <see cref="DefaultUVSTable"/> from the start of the format 14 subtable, or <c>0</c> when the record declares no default table.</value>
        /// <seealso cref="VarSelector"/>
        /// <seealso cref="NonDefaultUVSOffset"/>
        /// <seealso cref="DefaultUVSTable"/>
        public uint DefaultUVSOffset;          // +3

        /// <summary>Gets the offset to the non-default UVS table, or zero when absent.</summary>
        /// <value>The byte offset of a <see cref="NonDefaultUVSTable"/> from the start of the format 14 subtable, or <c>0</c> when the record declares no non-default table.</value>
        /// <seealso cref="VarSelector"/>
        /// <seealso cref="DefaultUVSOffset"/>
        /// <seealso cref="NonDefaultUVSTable"/>
        public uint NonDefaultUVSOffset;       // +7

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new record with each multi-byte field reversed.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>The <see cref="VarSelector"/> field is a 24-bit value and is reversed as a 3-byte value.</description></item>
        /// <item><description>The <see cref="DefaultUVSOffset"/> and <see cref="NonDefaultUVSOffset"/> fields are 32-bit offsets and are reversed independently.</description></item>
        /// </list>
        /// </remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="UInt24.ReverseEndianness(UInt24)"/>
        /// <seealso cref="DefaultUVSOffset"/>
        /// <seealso cref="NonDefaultUVSOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
        public static VariationSelectorRecord ReverseEndianness(VariationSelectorRecord v) => new()
        {
            VarSelector = UInt24.ReverseEndianness(v.VarSelector),
            DefaultUVSOffset = BinaryPrimitives.ReverseEndianness(v.DefaultUVSOffset),
            NonDefaultUVSOffset = BinaryPrimitives.ReverseEndianness(v.NonDefaultUVSOffset),
        };
    }

    /// <summary>
    /// A default UVS table containing Unicode ranges whose glyphs are determined by the
    /// default <c>cmap</c> mapping.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Each range identifies base character code points for which the glyph from the ordinary <c>cmap</c> applies when the associated variation selector is used.</description></item>
    /// <item><description>The ranges are stored in ascending order by <see cref="UnicodeRange.StartUnicodeValue"/>.</description></item>
    /// <item><description>A code point may therefore be located by scanning the ranges until its containing range is found or a later range is reached.</description></item>
    /// <item><description>The table is referenced by a <see cref="VariationSelectorRecord"/> through <see cref="VariationSelectorRecord.DefaultUVSOffset"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">format 14 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapFormat14"/>
    /// <seealso cref="VariationSelectorRecord"/>
    /// <seealso cref="UnicodeRange"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
    public sealed record DefaultUVSTable : IRecord<DefaultUVSTable>
    {
        /// <summary>Gets the Unicode ranges covered by this default UVS table.</summary>
        /// <value>An array of <see cref="UnicodeRange"/> entries sorted ascending by <see cref="UnicodeRange.StartUnicodeValue"/>.</value>
        /// <seealso cref="UnicodeRange"/>
        /// <seealso cref="Contains(int)"/>
        public IReadOnlyList<UnicodeRange> Ranges { get; init; } = [];

        /// <summary>Determines whether a base character is covered by this default UVS table.</summary>
        /// <param name="codepoint">The Unicode code point to test.</param>
        /// <returns><c>true</c> when <paramref name="codepoint"/> is contained in one of the table's ranges; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The ranges are expected to be sorted by <see cref="UnicodeRange.StartUnicodeValue"/>.
        /// The search terminates as soon as a range begins after <paramref name="codepoint"/>.
        /// </remarks>
        /// <seealso cref="Ranges"/>
        /// <seealso cref="UnicodeRange"/>
        public bool Contains(int codepoint)
        {
            var ranges = Ranges;
            for (int i = 0; i < ranges.Count; i++)
            {
                var (start, end) = (ranges[i].StartUnicodeValue, ranges[i].EndUnicodeValue);
                if ((uint)codepoint >= start && (uint)codepoint <= end) return true;
                if ((uint)codepoint < start) return false;
            }

            return false;
        }

        /// <inheritdoc/>
        /// <param name="cursor">Cursor positioned at the beginning of the default UVS table.</param>
        /// <param name="context">Optional parsing context; not required by the default UVS table.</param>
        /// <returns>The parsed default UVS table.</returns>
        /// <exception cref="InvalidDataException">The declared range count exceeds the safety limit.</exception>
        /// <remarks>
        /// The table begins with a 32-bit range count followed by that many
        /// <see cref="UnicodeRange"/> records.
        /// </remarks>
        /// <seealso cref="UnicodeRange"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
        static DefaultUVSTable IRecord<DefaultUVSTable>.Parse(
            ref Cursor cursor, object? context)
        {
            uint numRanges = cursor.ReadUInt32();

            if (numRanges > 100_000)
                throw new InvalidDataException(
                    $"'cmap' format 14 default UVS table declares {numRanges} ranges, which exceeds the safety limit.");

            return new DefaultUVSTable
            {
                Ranges = cursor.ReadBigEndianStructArray<UnicodeRange>((int)numRanges),
            };
        }
    }

    /// <summary>
    /// A non-default UVS table containing explicit mappings from Unicode code points
    /// to glyph IDs.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Each entry explicitly maps a base character code point to a glyph ID for the associated variation selector.</description></item>
    /// <item><description>The mappings are stored in ascending order by <see cref="UVSMapping.UnicodeValue"/>.</description></item>
    /// <item><description>A non-default mapping takes precedence over a default UVS mapping when resolving a variation sequence.</description></item>
    /// <item><description>The table is referenced by a <see cref="VariationSelectorRecord"/> through <see cref="VariationSelectorRecord.NonDefaultUVSOffset"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">format 14 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CmapFormat14"/>
    /// <seealso cref="VariationSelectorRecord"/>
    /// <seealso cref="UVSMapping"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
    public sealed record NonDefaultUVSTable : IRecord<NonDefaultUVSTable>
    {
        /// <summary>Gets the explicit Unicode-to-glyph mappings.</summary>
        /// <value>An array of <see cref="UVSMapping"/> entries sorted ascending by <see cref="UVSMapping.UnicodeValue"/>.</value>
        /// <seealso cref="UVSMapping"/>
        /// <seealso cref="TryGetGlyphId(int, out int)"/>
        public IReadOnlyList<UVSMapping> Mappings { get; init; } = [];

        /// <summary>Attempts to resolve the glyph ID explicitly assigned to a base character.</summary>
        /// <param name="codepoint">The Unicode code point to look up.</param>
        /// <param name="glyphId">When this method returns <c>true</c>, receives the mapped glyph ID; otherwise receives <c>0</c>.</param>
        /// <returns><c>true</c> when an explicit mapping exists for <paramref name="codepoint"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The mappings are expected to be sorted by <see cref="UVSMapping.UnicodeValue"/>.
        /// </remarks>
        /// <seealso cref="Mappings"/>
        /// <seealso cref="UVSMapping"/>
        public bool TryGetGlyphId(int codepoint, out int glyphId)
        {
            var mappings = Mappings;
            for (int i = 0; i < mappings.Count; i++)
            {
                if (mappings[i].UnicodeValue.Value == (uint)codepoint)
                {
                    glyphId = mappings[i].GlyphId;
                    return true;
                }
            }

            glyphId = 0;
            return false;
        }

        /// <inheritdoc/>
        /// <param name="cursor">Cursor positioned at the beginning of the non-default UVS table.</param>
        /// <param name="context">Optional parsing context; not required by the non-default UVS table.</param>
        /// <returns>The parsed non-default UVS table.</returns>
        /// <exception cref="InvalidDataException">The declared mapping count exceeds the safety limit.</exception>
        /// <remarks>
        /// The table begins with a 32-bit mapping count followed by that many
        /// <see cref="UVSMapping"/> records.
        /// </remarks>
        /// <seealso cref="UVSMapping"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
        static NonDefaultUVSTable IRecord<NonDefaultUVSTable>.Parse(
            ref Cursor cursor, object? context)
        {
            uint numMappings = cursor.ReadUInt32();

            if (numMappings > 100_000)
                throw new InvalidDataException(
                    $"'cmap' format 14 non-default UVS table declares {numMappings} mappings, which exceeds the safety limit.");

            return new NonDefaultUVSTable
            {
                Mappings = cursor.ReadBigEndianStructArray<UVSMapping>((int)numMappings),
            };
        }
    }

    /// <summary>
    /// A Unicode range entry in a default UVS table.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The record occupies exactly 4 bytes on disk: a 24-bit starting Unicode value followed by an 8-bit additional-count field.</description></item>
    /// <item><description>The range covers <see cref="StartUnicodeValue"/> through <see cref="EndUnicodeValue"/>, inclusive.</description></item>
    /// <item><description><see cref="AdditionalCount"/> specifies the number of code points following the starting value, so the inclusive end is <c>StartUnicodeValue + AdditionalCount</c>.</description></item>
    /// <item><description>Declared as a <c>record struct</c> and packed to match the OpenType wire representation.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">format 14 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="DefaultUVSTable"/>
    /// <seealso cref="StartUnicodeValue"/>
    /// <seealso cref="AdditionalCount"/>
    /// <seealso cref="EndUnicodeValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct UnicodeRange : IEndianReversibleStruct<UnicodeRange>
    {
        /// <summary>Gets or sets the first Unicode code point in the range.</summary>
        /// <value>The 24-bit Unicode value at which the range begins.</value>
        /// <seealso cref="EndUnicodeValue"/>
        public UInt24 StartUnicodeValue;   // +0

        /// <summary>Gets or sets the number of additional code points covered after <see cref="StartUnicodeValue"/>.</summary>
        /// <value>A value from <c>0</c> through <c>255</c>.</value>
        /// <seealso cref="StartUnicodeValue"/>
        /// <seealso cref="EndUnicodeValue"/>
        public byte AdditionalCount;       // +3

        /// <summary>Gets the last Unicode code point in the range, inclusive.</summary>
        /// <value><see cref="StartUnicodeValue"/> plus <see cref="AdditionalCount"/>.</value>
        /// <seealso cref="StartUnicodeValue"/>
        /// <seealso cref="AdditionalCount"/>
        public readonly uint EndUnicodeValue => StartUnicodeValue.Value + AdditionalCount;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new range with the 24-bit Unicode value byte-reversed.</returns>
        /// <remarks>
        /// <see cref="AdditionalCount"/> is a single byte and therefore requires no byte-order transformation.
        /// </remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="UInt24.ReverseEndianness(UInt24)"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
        public static UnicodeRange ReverseEndianness(UnicodeRange v) => new()
        {
            StartUnicodeValue = UInt24.ReverseEndianness(v.StartUnicodeValue),
            AdditionalCount = v.AdditionalCount,
        };
    }

    /// <summary>
    /// An explicit Unicode-to-glyph mapping in a non-default UVS table.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The record occupies exactly 5 bytes on disk.</description></item>
    /// <item><description><see cref="UnicodeValue"/> is a 24-bit Unicode code point.</description></item>
    /// <item><description><see cref="GlyphId"/> is a 16-bit glyph identifier.</description></item>
    /// <item><description>Declared as a <c>record struct</c> and packed to match the OpenType wire representation.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">format 14 subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="NonDefaultUVSTable"/>
    /// <seealso cref="UnicodeValue"/>
    /// <seealso cref="GlyphId"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct UVSMapping : IEndianReversibleStruct<UVSMapping>
    {
        /// <summary>Gets or sets the Unicode code point being explicitly mapped.</summary>
        /// <value>The 24-bit base character code point.</value>
        /// <seealso cref="GlyphId"/>
        public UInt24 UnicodeValue;   // +0

        /// <summary>Gets or sets the glyph ID assigned to <see cref="UnicodeValue"/>.</summary>
        /// <value>The 16-bit glyph identifier.</value>
        /// <seealso cref="UnicodeValue"/>
        public ushort GlyphId;        // +3

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new mapping with each multi-byte field reversed.</returns>
        /// <remarks>
        /// The 24-bit <see cref="UnicodeValue"/> is reversed as a 3-byte value and the
        /// 16-bit <see cref="GlyphId"/> is reversed independently.
        /// </remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="UInt24.ReverseEndianness(UInt24)"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-14-unicode-variation-sequences">OpenType specification: format 14</seealso>
        public static UVSMapping ReverseEndianness(UVSMapping v) => new()
        {
            UnicodeValue = UInt24.ReverseEndianness(v.UnicodeValue),
            GlyphId = BinaryPrimitives.ReverseEndianness(v.GlyphId),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Unknown format
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
///
/// A placeholder for <c>cmap</c> subtable formats that the parser does not understand.
/// </summary>
/// <remarks>
///
/// <list type="bullet">
/// <item><description>The original format number is preserved through the inherited <see cref="CmapSubtable.Format"/> property.</description></item>
/// <item><description>No character-to-glyph mappings are interpreted by this implementation.</description></item>
/// <item><description><see cref="GetGlyphId(int)"/> therefore always returns <c>0</c>.</description></item>
/// <item><description>This type allows an unknown format to remain represented in the parsed model rather than causing the entire <c>cmap</c> table to become unrepresentable.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CmapSubtable"/>
public sealed record CmapFormatUnknown : CmapSubtable
{
    /// <inheritdoc/>
    /// <param name="codepoint">The Unicode code point to resolve.</param>
    /// <returns>Always <c>0</c>, because this implementation does not interpret the unknown format.</returns>
    public override int GetGlyphId(int codepoint) => 0;
}
