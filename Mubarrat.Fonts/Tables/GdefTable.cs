using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>GDEF</c> table: glyph definitions used by OpenType Layout processing. Supplies glyph classification, attachment point lists, ligature caret positions, mark attachment classes, mark glyph sets, and the item variation store that GPOS and JSTF reference for variation data.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>All six subtables are optional. A consumer that does not find <see cref="GlyphClassDef"/> must classify glyphs itself when applying <c>GSUB</c> and <c>GPOS</c> lookups.</description></item>
/// <item><description>Version 1.0 defines the base header and first four subtables; version 1.2 adds <see cref="MarkGlyphSets"/>; version 1.3 adds the <see cref="ItemVariationStore"/>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef">GDEF table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyphClass"/>
/// <seealso cref="AttachList"/>
/// <seealso cref="LigCaretList"/>
/// <seealso cref="CaretValue"/>
/// <seealso cref="MarkGlyphSets"/>
/// <seealso cref="ItemVariationStore"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef">OpenType specification: GDEF table</seealso>
public sealed record GdefTable : IFontTable<GdefTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef">OpenType specification: GDEF table</seealso>
    public static Tag Tag => "GDEF";

    /// <summary>Gets the major version. Always 1.</summary>
    /// <value>The constant <c>1</c> for a conforming GDEF table.</value>
    /// <seealso cref="MinorVersion"/>
    /// <seealso cref="HeaderV0.Major"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version: 0, 2, or 3.</summary>
    /// <value><c>0</c> for the base version, <c>2</c> when <see cref="MarkGlyphSets"/> is present, <c>3</c> when <see cref="ItemVariationStore"/> is also present.</value>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="MarkGlyphSets"/>
    /// <seealso cref="ItemVariationStore"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the glyph class definition, or <c>null</c>.</summary>
    /// <value>The <see cref="ClassDef"/> that assigns each glyph a <see cref="GlyphClass"/> value, or <c>null</c> when the table declares no classification.</value>
    /// <seealso cref="GetGlyphClass(int)"/>
    /// <seealso cref="GlyphClass"/>
    public ClassDef? GlyphClassDef { get; init; }

    /// <summary>Gets the attachment point list, or <c>null</c>.</summary>
    /// <value>The <see cref="AttachList"/> that gives the contour point indices a GPOS attachment can target, or <c>null</c> when absent.</value>
    /// <seealso cref="GetAttachPoints(int)"/>
    /// <seealso cref="AttachList"/>
    public AttachList? AttachList { get; init; }

    /// <summary>Gets the ligature caret list, or <c>null</c>.</summary>
    /// <value>The <see cref="LigCaretList"/> that gives the caret positions for ligature glyphs, or <c>null</c> when absent.</value>
    /// <seealso cref="GetLigatureCarets(int)"/>
    /// <seealso cref="LigCaretList"/>
    public LigCaretList? LigCaretList { get; init; }

    /// <summary>Gets the mark attachment class definition, or <c>null</c>.</summary>
    /// <value>The <see cref="ClassDef"/> that assigns each mark glyph a class used by mark-attachment lookups, or <c>null</c> when absent.</value>
    /// <seealso cref="GetMarkAttachClass(int)"/>
    /// <seealso cref="ClassDef"/>
    public ClassDef? MarkAttachClassDef { get; init; }

    /// <summary>Gets the mark glyph sets, or <c>null</c>. Version 1.2 and later.</summary>
    /// <value>The <see cref="MarkGlyphSets"/> table, or <c>null</c> when the version is below 1.2 or the offset was zero.</value>
    /// <seealso cref="GetMarkGlyphSet(int)"/>
    /// <seealso cref="MarkGlyphSets"/>
    public MarkGlyphSets? MarkGlyphSets { get; init; }

    /// <summary>Gets the item variation store, or <c>null</c>. Version 1.3 and later. Contains the variation data used to adjust caret positions in GDEF, anchor and other values in GPOS, and justification parameters in JSTF.</summary>
    /// <value>The <see cref="ItemVariationStore"/> resolved from the version 1.3 trailing offset, or <c>null</c> when the version is below 1.3 or the offset was zero.</value>
    /// <seealso cref="MinorVersion"/>
    /// <seealso cref="ItemVariationStore"/>
    public ItemVariationStore? ItemVariationStore { get; init; }

    /// <summary>Returns the class of <paramref name="glyphId"/>, or <see cref="GlyphClass.Unclassified"/> when the font does not classify it.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The glyph's <see cref="GlyphClass"/> from <see cref="GlyphClassDef"/>, or <see cref="GlyphClass.Unclassified"/> when the font declares no class definition or the glyph is not classified.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><see cref="GlyphClass.Unclassified"/> is equivalent to class 0 in the on-disk ClassDef, so no special path is needed.</description></item>
    /// <item><description>The lookup is delegated to <see cref="ClassDef.GetClass(int)"/>; performance depends on the class-definition format in use.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GlyphClassDef"/>
    /// <seealso cref="GlyphClass"/>
    /// <seealso cref="ClassDef.GetClass(int)"/>
    public GlyphClass GetGlyphClass(int glyphId) =>
        GlyphClassDef is { } cd ? (GlyphClass)cd.GetClass(glyphId) : GlyphClass.Unclassified;

    /// <summary>Returns the attachment point indices for <paramref name="glyphId"/>, or an empty list when the glyph has no entry.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The contour point indices declared for the glyph, or an empty list when the table is absent or the glyph is not covered.</returns>
    /// <seealso cref="AttachList"/>
    /// <seealso cref="AttachList.GetPoints(int)"/>
    public IReadOnlyList<ushort> GetAttachPoints(int glyphId) =>
        AttachList is { } al ? al.GetPoints(glyphId) : [];

    /// <summary>Returns the caret values for <paramref name="glyphId"/>, or an empty list when the glyph is not a ligature with caret data.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The <see cref="CaretValue"/> entries for the glyph, or an empty list when the table is absent or the glyph is not covered.</returns>
    /// <seealso cref="LigCaretList"/>
    /// <seealso cref="LigCaretList.GetCarets(int)"/>
    /// <seealso cref="CaretValue"/>
    public IReadOnlyList<CaretValue> GetLigatureCarets(int glyphId) =>
        LigCaretList is { } lcl ? lcl.GetCarets(glyphId) : [];

    /// <summary>Returns the mark attachment class of <paramref name="glyphId"/>, or 0 when the font does not assign it to a mark class.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The mark attachment class number, or <c>0</c> when the table is absent or the glyph is not classified as a mark.</returns>
    /// <remarks>Class 0 is the conventional "no mark attachment class" sentinel; the specification does not define a symbolic name for it.</remarks>
    /// <seealso cref="MarkAttachClassDef"/>
    /// <seealso cref="ClassDef.GetClass(int)"/>
    public int GetMarkAttachClass(int glyphId) =>
        MarkAttachClassDef is { } mad ? mad.GetClass(glyphId) : 0;

    /// <summary>Returns the mark glyph set at <paramref name="index"/>, or <c>null</c> when the index is out of range.</summary>
    /// <param name="index">The zero-based index into <see cref="MarkGlyphSets.Sets"/>.</param>
    /// <returns>The <see cref="Coverage"/> for the mark glyph set at <paramref name="index"/>, or <c>null</c> when the table is absent or the index is out of range.</returns>
    /// <seealso cref="MarkGlyphSets"/>
    /// <seealso cref="Coverage"/>
    public Coverage? GetMarkGlyphSet(int index) =>
        MarkGlyphSets is { } mgs && (uint)index < (uint)mgs.Sets.Count ? mgs.Sets[index] : null;

    /// <summary>The 12-byte version 1.0 <c>GDEF</c> header. Blittable, no padding. Versions 1.2 and 1.3 append version-gated tails that are read separately so a v1.0 table does not read past its own extent.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Every offset is measured from the start of the GDEF table; a zero offset means the corresponding subtable is absent.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef">GDEF header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GdefTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef">OpenType specification: GDEF header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct HeaderV0 : IEndianReversibleStruct<HeaderV0>
    {
        /// <summary>Gets the major version. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming GDEF table.</value>
        /// <seealso cref="Minor"/>
        public ushort Major;

        /// <summary>Gets the minor version: 0, 2, or 3.</summary>
        /// <value><c>0</c> for the base version, <c>2</c> when a mark-glyph-sets offset follows, <c>3</c> when an item-variation-store offset also follows.</value>
        /// <seealso cref="Major"/>
        public ushort Minor;

        /// <summary>Gets the offset to the glyph class definition, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="ClassDef"/> from the GDEF table start, or zero.</value>
        /// <seealso cref="GdefTable.GlyphClassDef"/>
        public ushort GlyphClassDefOffset;

        /// <summary>Gets the offset to the attachment point list, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="AttachList"/> from the GDEF table start, or zero.</value>
        /// <seealso cref="GdefTable.AttachList"/>
        public ushort AttachListOffset;

        /// <summary>Gets the offset to the ligature caret list, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="LigCaretList"/> from the GDEF table start, or zero.</value>
        /// <seealso cref="GdefTable.LigCaretList"/>
        public ushort LigCaretListOffset;

        /// <summary>Gets the offset to the mark attachment class definition, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="ClassDef"/> from the GDEF table start, or zero.</value>
        /// <seealso cref="GdefTable.MarkAttachClassDef"/>
        public ushort MarkAttachClassDefOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All six fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static HeaderV0 ReverseEndianness(HeaderV0 v) => new()
        {
            Major = BinaryPrimitives.ReverseEndianness(v.Major),
            Minor = BinaryPrimitives.ReverseEndianness(v.Minor),
            GlyphClassDefOffset = BinaryPrimitives.ReverseEndianness(v.GlyphClassDefOffset),
            AttachListOffset = BinaryPrimitives.ReverseEndianness(v.AttachListOffset),
            LigCaretListOffset = BinaryPrimitives.ReverseEndianness(v.LigCaretListOffset),
            MarkAttachClassDefOffset = BinaryPrimitives.ReverseEndianness(v.MarkAttachClassDefOffset),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the GDEF table.</param>
    /// <param name="context">Unused. The table resolves its subtables through its own source.</param>
    /// <returns>The parsed GDEF table.</returns>
    /// <exception cref="InvalidDataException">The major version is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header or any referenced subtable extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The version 1.2 mark-glyph-sets offset and the version 1.3 item-variation-store offset are read only when the minor version declares them; a v1.0 table does not advance the cursor past its declared extent.</description></item>
    /// <item><description>Every offset is checked for zero before parsing; a zero offset yields a <c>null</c> property rather than an exception.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef">GDEF table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="HeaderV0"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef">OpenType specification: GDEF table</seealso>
    static GdefTable IRecord<GdefTable>.Parse(ref Cursor cursor, object? context)
    {
        HeaderV0 header = cursor.ReadBigEndianStruct<HeaderV0>();
        if (header.Major != 1)
            throw new InvalidDataException($"'GDEF'.majorVersion is {header.Major}, expected 1.");

        // Version-gated tails. Each is read only when the version declares it, so a
        // v1.0 table does not read past its own 12-byte extent.
        ushort markGlyphSetsOffset = header.Minor >= 2 ? cursor.ReadUInt16() : (ushort)0;
        uint itemVarStoreOffset = header.Minor >= 3 ? cursor.ReadUInt32() : 0u;

        Source source = cursor.Source;

        return new GdefTable
        {
            MajorVersion = header.Major,
            MinorVersion = header.Minor,
            GlyphClassDef = header.GlyphClassDefOffset != 0 ? source.ParseRecordAt<ClassDef>(header.GlyphClassDefOffset) : null,
            AttachList = header.AttachListOffset != 0 ? source.ParseRecordAt<AttachList>(header.AttachListOffset) : null,
            LigCaretList = header.LigCaretListOffset != 0 ? source.ParseRecordAt<LigCaretList>(header.LigCaretListOffset) : null,
            MarkAttachClassDef = header.MarkAttachClassDefOffset != 0 ? source.ParseRecordAt<ClassDef>(header.MarkAttachClassDefOffset) : null,
            MarkGlyphSets = markGlyphSetsOffset != 0 ? source.ParseRecordAt<MarkGlyphSets>(markGlyphSetsOffset) : null,
            ItemVariationStore = itemVarStoreOffset != 0 ? source.ParseRecordAt<ItemVariationStore>(itemVarStoreOffset) : null,
        };
    }
}

/// <summary>Glyph class values used by <see cref="GdefTable.GlyphClassDef"/>. Class 0 is assigned by default to any glyph the font does not explicitly classify.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The class values are also used by the GDEF-adjacent GSUB and GPOS lookups to distinguish base, ligature, mark, and component glyphs.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#glyph-class-definition-table">Glyph Class Definition table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GdefTable.GlyphClassDef"/>
/// <seealso cref="GdefTable.GetGlyphClass(int)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#glyph-class-definition-table">OpenType specification: Glyph Class Definition table</seealso>
public enum GlyphClass : ushort
{
    /// <summary>Glyph not assigned to any class.</summary>
    /// <remarks>Class 0 in the on-disk ClassDef; the default for any glyph the font does not classify.</remarks>
    Unclassified = 0,

    /// <summary>Base glyph: a single character, spacing glyph.</summary>
    /// <remarks>Class 1 in the on-disk ClassDef. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#glyph-class-definition-table">Glyph Class Definition table</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#glyph-class-definition-table">OpenType specification: <c>Base glyph</c></seealso>
    Base = 1,

    /// <summary>Ligature glyph: a multiple character, spacing glyph.</summary>
    /// <remarks>Class 2 in the on-disk ClassDef. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#glyph-class-definition-table">Glyph Class Definition table</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#glyph-class-definition-table">OpenType specification: <c>Ligature glyph</c></seealso>
    Ligature = 2,

    /// <summary>Mark glyph: a non-spacing combining glyph.</summary>
    /// <remarks>Class 3 in the on-disk ClassDef. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#glyph-class-definition-table">Glyph Class Definition table</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#glyph-class-definition-table">OpenType specification: <c>Mark glyph</c></seealso>
    Mark = 3,

    /// <summary>Component glyph: part of a single character, spacing glyph.</summary>
    /// <remarks>Class 4 in the on-disk ClassDef. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#glyph-class-definition-table">Glyph Class Definition table</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#glyph-class-definition-table">OpenType specification: <c>Component glyph</c></seealso>
    Component = 4,
}

// ═══════════════════════════════════════════════════════════════════════════
// AttachList
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The Attachment Point List: per-glyph attachment point indices, indexed by Coverage.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each covered glyph gets one <see cref="AttachPoint"/> entry, indexed by the glyph's Coverage Index.</description></item>
/// <item><description>The point indices refer to points in the glyph's outline and are used by GPOS mark-to-base and mark-to-ligature lookups.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#attachment-point-list-table">Attachment Point List table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GdefTable"/>
/// <seealso cref="AttachPoint"/>
/// <seealso cref="Coverage"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#attachment-point-list-table">OpenType specification: Attachment Point List table</seealso>
public sealed record AttachList : IRecord<AttachList>
{
    /// <summary>Gets the coverage table that assigns indices to covered glyphs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>Points[i]</c>.</value>
    /// <seealso cref="Points"/>
    /// <seealso cref="GetPoints(int)"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets one AttachPoint per covered glyph, in Coverage Index order.</summary>
    /// <value>The ordered list of <see cref="AttachPoint"/> entries. Its length equals <see cref="Coverage"/>'s glyph count.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="AttachPoint"/>
    public IReadOnlyList<AttachPoint> Points { get; init; } = [];

    /// <summary>Returns the point list for a glyph, or an empty list when the glyph is uncovered.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The contour point indices for the glyph, or an empty list when the glyph is not covered.</returns>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="Points"/>
    /// <seealso cref="AttachPoint.PointIndices"/>
    public IReadOnlyList<ushort> GetPoints(int glyphId)
    {
        int i = Coverage.GetCoverageIndex(glyphId);
        return i >= 0 && i < Points.Count ? Points[i].PointIndices : [];
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the attach list.</param>
    /// <param name="context">Forwarded to the coverage and attach-point parsers.</param>
    /// <returns>The parsed attach list.</returns>
    /// <exception cref="EndOfStreamException">The header, coverage, or point array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#attachment-point-list-table">Attachment Point List table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="AttachPoint"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#attachment-point-list-table">OpenType specification: Attachment Point List table</seealso>
    static AttachList IRecord<AttachList>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new AttachList
        {
            Coverage = cursor.Source.ParseRecordAt<Coverage>(header.CoverageOffset),
            Points = cursor.ReadOffset16ArrayPeekRecord<AttachPoint>(header.GlyphCount),
        };
    }

    /// <summary>The 4-byte AttachList header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The coverage offset is measured from the start of the AttachList record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#attachment-point-list-table">Attachment Point List table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AttachList"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#attachment-point-list-table">OpenType specification: Attachment Point List table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the offset to the coverage table.</summary>
        /// <value>The byte offset of the <see cref="Coverage"/> from the AttachList record start.</value>
        /// <seealso cref="GlyphCount"/>
        /// <seealso cref="Coverage"/>
        public ushort CoverageOffset;

        /// <summary>Gets the number of covered glyphs.</summary>
        /// <value>The count of <see cref="AttachPoint"/> entries that follow the header.</value>
        /// <seealso cref="CoverageOffset"/>
        /// <seealso cref="AttachPoint"/>
        public ushort GlyphCount;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            CoverageOffset = BinaryPrimitives.ReverseEndianness(v.CoverageOffset),
            GlyphCount = BinaryPrimitives.ReverseEndianness(v.GlyphCount),
        };
    }
}

/// <summary>The attachment points of a single glyph: contour point indices in increasing numerical order.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Point indices refer to points in the glyph's outline and must be in strictly increasing order.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#attachment-point-list-table">Attachment Point List table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AttachList"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#attachment-point-list-table">OpenType specification: Attachment Point List table</seealso>
public sealed record AttachPoint : IRecord<AttachPoint>
{
    /// <summary>Gets the contour point indices.</summary>
    /// <value>The increasing sequence of point indices that a GPOS attachment can target.</value>
    /// <seealso cref="Count"/>
    public IReadOnlyList<ushort> PointIndices { get; init; } = [];

    /// <summary>Gets the number of attachment points.</summary>
    /// <value>The size of the <see cref="PointIndices"/> list.</value>
    /// <seealso cref="PointIndices"/>
    public int Count => PointIndices.Count;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the attach-point record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed attach-point record.</returns>
    /// <exception cref="EndOfStreamException">The count or point array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#attachment-point-list-table">Attachment Point List table</see> in the OpenType specification.</remarks>
    /// <seealso cref="PointIndices"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#attachment-point-list-table">OpenType specification: Attachment Point List table</seealso>
    static AttachPoint IRecord<AttachPoint>.Parse(ref Cursor cursor, object? context) => new()
    {
        PointIndices = cursor.ReadUInt16Array(cursor.ReadUInt16()),
    };
}

// ═══════════════════════════════════════════════════════════════════════════
// LigCaretList
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The Ligature Caret List: per-ligature caret coordinates, indexed by Coverage.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each covered ligature glyph gets one <see cref="LigGlyph"/> entry, indexed by the glyph's Coverage Index.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#ligature-caret-list-table">Ligature Caret List table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GdefTable"/>
/// <seealso cref="LigGlyph"/>
/// <seealso cref="CaretValue"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#ligature-caret-list-table">OpenType specification: Ligature Caret List table</seealso>
public sealed record LigCaretList : IRecord<LigCaretList>
{
    /// <summary>Gets the coverage table that assigns indices to covered ligature glyphs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>LigGlyphs[i]</c>.</value>
    /// <seealso cref="LigGlyphs"/>
    /// <seealso cref="GetCarets(int)"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets one LigGlyph per covered glyph, in Coverage Index order.</summary>
    /// <value>The ordered list of <see cref="LigGlyph"/> entries. Its length equals <see cref="Coverage"/>'s glyph count.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="LigGlyph"/>
    public IReadOnlyList<LigGlyph> LigGlyphs { get; init; } = [];

    /// <summary>Returns the caret values for a glyph, or an empty list when the glyph is uncovered.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The <see cref="CaretValue"/> entries for the glyph, or an empty list when the glyph is not covered.</returns>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="LigGlyphs"/>
    /// <seealso cref="CaretValue"/>
    public IReadOnlyList<CaretValue> GetCarets(int glyphId)
    {
        int i = Coverage.GetCoverageIndex(glyphId);
        return i >= 0 && i < LigGlyphs.Count ? LigGlyphs[i].CaretValues : [];
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the ligature caret list.</param>
    /// <param name="context">Forwarded to the coverage and lig-glyph parsers.</param>
    /// <returns>The parsed ligature caret list.</returns>
    /// <exception cref="EndOfStreamException">The header, coverage, or lig-glyph array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#ligature-caret-list-table">Ligature Caret List table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="LigGlyph"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#ligature-caret-list-table">OpenType specification: Ligature Caret List table</seealso>
    static LigCaretList IRecord<LigCaretList>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new LigCaretList
        {
            Coverage = cursor.Source.ParseRecordAt<Coverage>(header.CoverageOffset),
            LigGlyphs = cursor.ReadOffset16ArrayPeekRecord<LigGlyph>(header.LigGlyphCount),
        };
    }

    /// <summary>The 4-byte LigCaretList header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The coverage offset is measured from the start of the LigCaretList record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#ligature-caret-list-table">Ligature Caret List table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LigCaretList"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#ligature-caret-list-table">OpenType specification: Ligature Caret List table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the offset to the coverage table.</summary>
        /// <value>The byte offset of the <see cref="Coverage"/> from the LigCaretList record start.</value>
        /// <seealso cref="LigGlyphCount"/>
        /// <seealso cref="Coverage"/>
        public ushort CoverageOffset;

        /// <summary>Gets the number of covered ligature glyphs.</summary>
        /// <value>The count of <see cref="LigGlyph"/> entries that follow the header.</value>
        /// <seealso cref="CoverageOffset"/>
        /// <seealso cref="LigGlyph"/>
        public ushort LigGlyphCount;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            CoverageOffset = BinaryPrimitives.ReverseEndianness(v.CoverageOffset),
            LigGlyphCount = BinaryPrimitives.ReverseEndianness(v.LigGlyphCount),
        };
    }
}

/// <summary>The caret coordinates for one ligature glyph. The number of caret values equals the number of components in the ligature minus one.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each entry is a <see cref="CaretValue"/> whose format determines whether the coordinate is a design unit value, a contour point index, or a design unit value with a device adjustment.</description></item>
/// <item><description>The specification requires caret values to be ordered by increasing coordinate position; a renderer cycles through them as the user presses the caret-navigation key.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#ligature-caret-list-table">Ligature Caret List table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LigCaretList"/>
/// <seealso cref="CaretValue"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#ligature-caret-list-table">OpenType specification: Ligature Caret List table</seealso>
public sealed record LigGlyph : IRecord<LigGlyph>
{
    /// <summary>Gets the caret values in increasing coordinate order.</summary>
    /// <value>The ordered list of <see cref="CaretValue"/> entries for the ligature.</value>
    /// <seealso cref="CaretCount"/>
    /// <seealso cref="CaretValue"/>
    public IReadOnlyList<CaretValue> CaretValues { get; init; } = [];

    /// <summary>Gets the number of caret values.</summary>
    /// <value>The size of the <see cref="CaretValues"/> list. Equals the ligature's component count minus one.</value>
    /// <seealso cref="CaretValues"/>
    public int CaretCount => CaretValues.Count;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the lig-glyph record.</param>
    /// <param name="context">Forwarded to the caret-value parsers.</param>
    /// <returns>The parsed lig-glyph record.</returns>
    /// <exception cref="EndOfStreamException">The count, offset array, or any referenced caret value extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#ligature-caret-list-table">Ligature Caret List table</see> in the OpenType specification.</remarks>
    /// <seealso cref="CaretValues"/>
    /// <seealso cref="CaretValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#ligature-caret-list-table">OpenType specification: Ligature Caret List table</seealso>
    static LigGlyph IRecord<LigGlyph>.Parse(ref Cursor cursor, object? context) => new()
    {
        CaretValues = cursor.ReadOffset16ArrayPeekRecord<CaretValue>(cursor.ReadUInt16()),
    };
}

// ═══════════════════════════════════════════════════════════════════════════
// CaretValue
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Base type for a ligature caret value. Three formats exist and are distinguished by the concrete subclass.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Format 1 stores a design-unit coordinate directly.</description></item>
/// <item><description>Format 2 references a contour point; the renderer resolves the position from the hinted outline at the target ppem.</description></item>
/// <item><description>Format 3 combines a design-unit coordinate with a Device or VariationIndex table for adjustment.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">Caret Value tables</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CaretValueFormat1"/>
/// <seealso cref="CaretValueFormat2"/>
/// <seealso cref="CaretValueFormat3"/>
/// <seealso cref="LigGlyph"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">OpenType specification: Caret Value tables</seealso>
public abstract record CaretValue : IRecord<CaretValue>, IBaseRecord<CaretValue>
{
    /// <summary>Gets the format number: 1, 2, or 3.</summary>
    /// <value>The format discriminant from the first <c>uint16</c> of the caret-value record.</value>
    /// <seealso cref="CaretValueFormat1"/>
    /// <seealso cref="CaretValueFormat2"/>
    /// <seealso cref="CaretValueFormat3"/>
    public ushort Format { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the caret-value record.</param>
    /// <param name="context">Forwarded to the format-specific parser.</param>
    /// <returns>The format-specific caret value.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1, 2, or 3.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">Caret Value tables</see> in the OpenType specification.</remarks>
    /// <seealso cref="IBaseRecord{TBase}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">OpenType specification: Caret Value tables</seealso>
    static CaretValue IRecord<CaretValue>.Parse(ref Cursor cursor, object? context)
    {
        ushort format = cursor.ReadUInt16();
        return format switch
        {
            1 => IBaseRecord<CaretValue>.Parse<CaretValueFormat1>(ref cursor, context),
            2 => IBaseRecord<CaretValue>.Parse<CaretValueFormat2>(ref cursor, context),
            3 => IBaseRecord<CaretValue>.Parse<CaretValueFormat3>(ref cursor, context),
            _ => throw new InvalidDataException($"CaretValue format {format} is not defined."),
        };
    }
}

/// <summary>Caret value format 1: a design-unit coordinate.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The coordinate is in font design units and is interpreted along the inline axis of the text run.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">Caret Value tables</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CaretValue"/>
/// <seealso cref="CaretValueFormat2"/>
/// <seealso cref="CaretValueFormat3"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">OpenType specification: Caret Value tables</seealso>
public sealed record CaretValueFormat1 : CaretValue, IEndianReversibleHeaderDerivedRecord<CaretValue, CaretValueFormat1, CaretValueFormat1.Header>
{
    /// <summary>Gets the X or Y coordinate in design units.</summary>
    /// <value>The caret position as a signed design-unit value.</value>
    /// <seealso cref="Header.Coordinate"/>
    public short Coordinate { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read header.</param>
    /// <param name="context">Unused.</param>
    /// <returns>A new instance populated from the header.</returns>
    static CaretValueFormat1 IHeaderDerivedRecord<CaretValue, CaretValueFormat1, Header>.FromHeader(in Header header, object? context) => new()
    {
        Format = 1,
        Coordinate = header.Coordinate,
    };

    /// <summary>The 2-byte format 1 body. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>When this struct is read through the header-derived parse path, its first field is the coordinate; the format discriminant was consumed by <see cref="IRecord{T}.Parse"/>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">Caret Value tables</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CaretValueFormat1"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">OpenType specification: Caret Value tables</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the coordinate in design units.</summary>
        /// <value>The signed caret position.</value>
        /// <seealso cref="CaretValueFormat1.Coordinate"/>
        public short Coordinate;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte field is to be reversed.</param>
        /// <returns>A new header with the coordinate reversed.</returns>
        /// <remarks>The header contains a single multi-byte field.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Coordinate = BinaryPrimitives.ReverseEndianness(v.Coordinate),
        };
    }
}

/// <summary>Caret value format 2: a contour point index on the ligature glyph. The resolved position comes from the hinted outline at the target ppem.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The caret position is the position of a specific point in the glyph's outline, resolved by the rasterizer after hinting.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">Caret Value tables</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CaretValue"/>
/// <seealso cref="CaretValueFormat1"/>
/// <seealso cref="CaretValueFormat3"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">OpenType specification: Caret Value tables</seealso>
public sealed record CaretValueFormat2 : CaretValue, IEndianReversibleHeaderDerivedRecord<CaretValue, CaretValueFormat2, CaretValueFormat2.Header>
{
    /// <summary>Gets the contour point index on the glyph.</summary>
    /// <value>The zero-based index of the point within the glyph's outline whose position defines the caret.</value>
    /// <seealso cref="Header.CaretValuePointIndex"/>
    public ushort CaretValuePointIndex { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read header.</param>
    /// <param name="context">Unused.</param>
    /// <returns>A new instance populated from the header.</returns>
    static CaretValueFormat2 IHeaderDerivedRecord<CaretValue, CaretValueFormat2, Header>.FromHeader(in Header header, object? context) => new()
    {
        Format = 2,
        CaretValuePointIndex = header.CaretValuePointIndex,
    };

    /// <summary>The 2-byte format 2 body. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>When this struct is read through the header-derived parse path, its first field is the point index; the format discriminant was consumed by <see cref="IRecord{T}.Parse"/>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">Caret Value tables</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CaretValueFormat2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">OpenType specification: Caret Value tables</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the contour point index on the glyph.</summary>
        /// <value>The zero-based index of the point within the glyph's outline.</value>
        /// <seealso cref="CaretValueFormat2.CaretValuePointIndex"/>
        public ushort CaretValuePointIndex;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte field is to be reversed.</param>
        /// <returns>A new header with the point index reversed.</returns>
        /// <remarks>The header contains a single multi-byte field.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            CaretValuePointIndex = BinaryPrimitives.ReverseEndianness(v.CaretValuePointIndex),
        };
    }
}

/// <summary>Caret value format 3: a design-unit coordinate plus a Device table (non-variable) or VariationIndex table (variable) for adjustment.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The device or variation index table provides ppem- or instance-specific adjustment to the base coordinate.</description></item>
/// <item><description>Uses <see cref="IDerivedRecord{TBase, TDerived}"/> rather than the header-derived form because it resolves an offset to a separate <see cref="Device"/> subtable.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">Caret Value tables</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CaretValue"/>
/// <seealso cref="CaretValueFormat1"/>
/// <seealso cref="CaretValueFormat2"/>
/// <seealso cref="Device"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">OpenType specification: Caret Value tables</seealso>
public sealed record CaretValueFormat3 : CaretValue, IDerivedRecord<CaretValue, CaretValueFormat3>
{
    /// <summary>Gets the X or Y coordinate in design units.</summary>
    /// <value>The base caret position as a signed design-unit value, before device adjustment.</value>
    /// <seealso cref="DeviceTable"/>
    public short Coordinate { get; init; }

    /// <summary>Gets the Device or VariationIndex table, or <c>null</c>.</summary>
    /// <value>The <see cref="Device"/> table resolved from the record's offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="Coordinate"/>
    /// <seealso cref="Device"/>
    public Device? DeviceTable { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-3 body (after the format discriminant).</param>
    /// <param name="context">Unused. The record resolves its device table through its own source.</param>
    /// <returns>The parsed format-3 caret value.</returns>
    /// <exception cref="EndOfStreamException">The header or referenced device table extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">Caret Value tables</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Device"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">OpenType specification: Caret Value tables</seealso>
    static CaretValueFormat3 IDerivedRecord<CaretValue, CaretValueFormat3>.Parse(ref Cursor cursor, object? context)
    {
        // The format word was consumed by CaretValue.Parse.
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new CaretValueFormat3
        {
            Format = 3,
            Coordinate = header.Coordinate,
            DeviceTable = header.DeviceOffset != 0 ? cursor.Source.ParseRecordAt<Device>(header.DeviceOffset) : null,
        };
    }

    /// <summary>The 4-byte format 3 body. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>When this struct is read through the format-3 parse path, its first field is the coordinate; the format discriminant was consumed by <see cref="IRecord{T}.Parse"/>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">Caret Value tables</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CaretValueFormat3"/>
    /// <seealso cref="Device"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#caret-value-tables">OpenType specification: Caret Value tables</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the coordinate in design units.</summary>
        /// <value>The base caret position as a signed design-unit value.</value>
        /// <seealso cref="DeviceOffset"/>
        public short Coordinate;

        /// <summary>Gets the offset to the Device or VariationIndex table, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="Device"/> table from the caret-value record start, or zero.</value>
        /// <seealso cref="Coordinate"/>
        /// <seealso cref="Device"/>
        public ushort DeviceOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Coordinate = BinaryPrimitives.ReverseEndianness(v.Coordinate),
            DeviceOffset = BinaryPrimitives.ReverseEndianness(v.DeviceOffset),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// MarkGlyphSets
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The Mark Glyph Sets table: an enumeration of glyph sets that GSUB and GPOS lookups can reference to filter which marks are considered or ignored. Sets may intersect.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The format field is always 1. Each set is a Coverage table; the offset array uses Offset32, not Offset16, because fonts can have many mark sets.</description></item>
/// <item><description>Sets are referenced from GSUB/GPOS lookups by index, allowing a single lookup to reference a named subset of marks without duplicating coverage data.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#mark-glyph-sets-table">Mark Glyph Sets table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GdefTable"/>
/// <seealso cref="Coverage"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#mark-glyph-sets-table">OpenType specification: Mark Glyph Sets table</seealso>
public sealed record MarkGlyphSets : IRecord<MarkGlyphSets>
{
    /// <summary>Gets the format. Always 1.</summary>
    /// <value>The constant <c>1</c> for a conforming MarkGlyphSets table.</value>
    /// <seealso cref="Sets"/>
    /// <seealso cref="Header.Format"/>
    public ushort Format { get; init; }

    /// <summary>Gets the coverage tables, one per mark glyph set.</summary>
    /// <value>The ordered list of <see cref="Coverage"/> entries; set index <c>i</c> corresponds to <c>Sets[i]</c>.</value>
    /// <seealso cref="Count"/>
    /// <seealso cref="Coverage"/>
    public IReadOnlyList<Coverage> Sets { get; init; } = [];

    /// <summary>Gets the number of mark glyph sets.</summary>
    /// <value>The size of the <see cref="Sets"/> list.</value>
    /// <seealso cref="Sets"/>
    public int Count => Sets.Count;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the mark glyph sets table.</param>
    /// <param name="context">Forwarded to the coverage parsers.</param>
    /// <returns>The parsed mark glyph sets table.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header, offset array, or any referenced coverage extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#mark-glyph-sets-table">Mark Glyph Sets table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Coverage"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#mark-glyph-sets-table">OpenType specification: Mark Glyph Sets table</seealso>
    static MarkGlyphSets IRecord<MarkGlyphSets>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.Format != 1)
            throw new InvalidDataException($"'GDEF'.MarkGlyphSets.format is {header.Format}, expected 1.");

        return new MarkGlyphSets
        {
            Format = header.Format,
            Sets = cursor.ReadOffset32ArrayPeekRecord<Coverage>(header.MarkGlyphSetCount),
        };
    }

    /// <summary>The 4-byte MarkGlyphSets header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed by <c>markGlyphSetCount</c> Offset32 values, each measuring a <see cref="Coverage"/> from the table start.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#mark-glyph-sets-table">Mark Glyph Sets table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MarkGlyphSets"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gdef#mark-glyph-sets-table">OpenType specification: Mark Glyph Sets table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the format. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming MarkGlyphSets table.</value>
        /// <seealso cref="MarkGlyphSetCount"/>
        public ushort Format;

        /// <summary>Gets the number of mark glyph sets.</summary>
        /// <value>The count of Offset32 entries that follow the header.</value>
        /// <seealso cref="Format"/>
        public ushort MarkGlyphSetCount;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            MarkGlyphSetCount = BinaryPrimitives.ReverseEndianness(v.MarkGlyphSetCount),
        };
    }
}
