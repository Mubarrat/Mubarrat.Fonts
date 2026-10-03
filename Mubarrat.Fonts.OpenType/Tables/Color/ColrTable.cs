using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables.Variations;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.OpenType.Tables.Color;

/// <summary>The <c>COLR</c> table: Color Table. Defines color presentations for glyphs using layered arrangements of glyphs, each filled with a color from the CPAL palette.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Two versions are defined. Version 0 is a simple layered composition: a base glyph record references a contiguous range of layer records, each a glyph ID plus a CPAL palette entry index. Version 1 adds a paint graph — a directed acyclic graph of <see cref="Paint"/> nodes supporting gradients, transforms, and compositing.</description></item>
/// <item><description>All version 1 paint offsets are relative to the start of the record that contains them, not to the COLR table.</description></item>
/// <item><description>All version 1 header offsets are relative to the start of the COLR table. A zero offset means the corresponding subtable is absent.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CpalTable"/>
/// <seealso cref="BaseGlyphRecord"/>
/// <seealso cref="LayerRecord"/>
/// <seealso cref="BaseGlyphList"/>
/// <seealso cref="LayerList"/>
/// <seealso cref="ClipList"/>
/// <seealso cref="Paint"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: CPAL table</seealso>
public sealed record ColrTable : IOpenTypeTable<ColrTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IOpenTypeTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR table</seealso>
    public static Tag Tag => "COLR";

    /// <summary>Gets the major version: 0 or 1.</summary>
    /// <value>The constant <c>0</c> for a layered-only table, or <c>1</c> for a table that carries a paint graph.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>version</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="HasV1"/>
    /// <seealso cref="BaseGlyphPaintRecords"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>version</c></seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the version 0 base glyph records, sorted by glyph ID.</summary>
    /// <value>The array of <see cref="BaseGlyphRecord"/> entries, sorted ascending by <see cref="BaseGlyphRecord.GlyphId"/> so a binary search can locate a glyph's layers.</value>
    /// <remarks>Present for both version 0 and version 1 tables; version 1 retains the v0 array for backward-compatible renderers.</remarks>
    /// <seealso cref="GetLayersV0(int)"/>
    /// <seealso cref="LayerRecords"/>
    /// <seealso cref="BaseGlyphRecord"/>
    public IReadOnlyList<BaseGlyphRecord> BaseGlyphRecords { get; init; } = [];

    /// <summary>Gets the version 0 layer records.</summary>
    /// <value>The flat array of <see cref="LayerRecord"/> entries, indexed by <see cref="BaseGlyphRecord.FirstLayerIndex"/>.</value>
    /// <remarks>The array is not sorted; it is indexed through the base glyph records.</remarks>
    /// <seealso cref="GetLayersV0(int)"/>
    /// <seealso cref="BaseGlyphRecords"/>
    /// <seealso cref="LayerRecord"/>
    public IReadOnlyList<LayerRecord> LayerRecords { get; init; } = [];

    /// <summary>Gets the version 1 base glyph paint records, or <c>null</c> for version 0.</summary>
    /// <value>The <see cref="BaseGlyphList"/> resolved from the v1 header extension, or <c>null</c> when the version is 0 or the offset was zero.</value>
    /// <seealso cref="GetRootPaint(int)"/>
    /// <seealso cref="HasV1"/>
    /// <seealso cref="BaseGlyphList"/>
    public BaseGlyphList? BaseGlyphPaintRecords { get; init; }

    /// <summary>Gets the version 1 layer list, or <c>null</c>.</summary>
    /// <value>The <see cref="LayerList"/> resolved from the v1 header extension, or <c>null</c> when the version is 0 or the offset was zero.</value>
    /// <remarks>The layer list holds reusable paint nodes that may be referenced by multiple glyphs' paint graphs.</remarks>
    /// <seealso cref="LayerList"/>
    public LayerList? LayerList { get; init; }

    /// <summary>Gets the version 1 clip list, or <c>null</c>.</summary>
    /// <value>The <see cref="ClipList"/> resolved from the v1 header extension, or <c>null</c> when the version is 0 or the offset was zero.</value>
    /// <seealso cref="ClipList"/>
    public ClipList? ClipList { get; init; }

    /// <summary>Gets the version 1 var index map, or <c>null</c>.</summary>
    /// <value>The <see cref="DeltaSetIndexMap"/> resolved from the v1 header extension, or <c>null</c> when the version is 0 or the offset was zero.</value>
    /// <remarks>The map indexes into <see cref="ItemVariationStore"/> to resolve variation deltas for paints that carry a variation index.</remarks>
    /// <seealso cref="ItemVariationStore"/>
    /// <seealso cref="DeltaSetIndexMap"/>
    public DeltaSetIndexMap? VarIndexMap { get; init; }

    /// <summary>Gets the version 1 item variation store, or <c>null</c>.</summary>
    /// <value>The <see cref="ItemVariationStore"/> resolved from the v1 header extension, or <c>null</c> when the version is 0 or the offset was zero.</value>
    /// <seealso cref="VarIndexMap"/>
    /// <seealso cref="ItemVariationStore"/>
    public ItemVariationStore? ItemVariationStore { get; init; }

    /// <summary>True when the table carries version 1 paint graph data.</summary>
    /// <value><see langword="true"/> when <see cref="Version"/> is at least 1 and <see cref="BaseGlyphPaintRecords"/> is non-<c>null</c>.</value>
    /// <remarks>Check this before calling <see cref="GetRootPaint(int)"/>; a v0-only table returns <c>null</c> from that method regardless.</remarks>
    /// <seealso cref="Version"/>
    /// <seealso cref="BaseGlyphPaintRecords"/>
    /// <seealso cref="GetRootPaint(int)"/>
    public bool HasV1 => Version >= 1 && BaseGlyphPaintRecords is not null;

    /// <summary>Returns the version 0 layers for a glyph, or <c>null</c>.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>An array of <see cref="LayerRecord"/> entries for the glyph, in layer order (bottom to top), or <c>null</c> when the glyph has no v0 definition.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup uses binary search over <see cref="BaseGlyphRecords"/>, which the specification requires to be sorted by glyph ID.</description></item>
    /// <item><description>The returned array is a fresh slice; modifying it does not affect <see cref="LayerRecords"/>.</description></item>
    /// <item><description>The layers are drawn in array order, with the first layer at the bottom of the stack.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var layers = colr.GetLayersV0(glyphId);
    /// if (layers is not null)
    ///     foreach (var layer in layers)
    ///         DrawLayer(layer.GlyphId, palette[layer.PaletteIndex]);
    /// </code>
    /// </example>
    /// <seealso cref="BaseGlyphRecords"/>
    /// <seealso cref="LayerRecords"/>
    /// <seealso cref="LayerRecord"/>
    public IReadOnlyList<LayerRecord>? GetLayersV0(int glyphId)
    {
        int lo = 0, hi = BaseGlyphRecords.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            int cmp = BaseGlyphRecords[mid].GlyphId - glyphId;
            if (cmp == 0)
            {
                BaseGlyphRecord bg = BaseGlyphRecords[mid];
                var slice = new LayerRecord[bg.NumLayers];
                for (int i = 0; i < bg.NumLayers; i++)
                    slice[i] = LayerRecords[bg.FirstLayerIndex + i];
                return slice;
            }
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }
        return null;
    }

    /// <summary>Returns the version 1 root paint for a glyph, or <c>null</c>.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The root <see cref="Paint"/> of the glyph's colour graph, or <c>null</c> when the glyph has no v1 definition.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Returns <c>null</c> immediately when <see cref="BaseGlyphPaintRecords"/> is <c>null</c>, so calling this on a v0-only table is safe.</description></item>
    /// <item><description>The lookup uses binary search over <see cref="BaseGlyphList.Records"/>, which the specification requires to be sorted by glyph ID.</description></item>
    /// <item><description>The returned paint is the entry node of a DAG; traversing it requires following child references through <see cref="Paint"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BaseGlyphPaintRecords"/>
    /// <seealso cref="Paint"/>
    /// <seealso cref="HasV1"/>
    public Paint? GetRootPaint(int glyphId)
    {
        if (BaseGlyphPaintRecords is null) return null;
        int lo = 0, hi = BaseGlyphPaintRecords.Records.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            int cmp = BaseGlyphPaintRecords.Records[mid].GlyphId - glyphId;
            if (cmp == 0) return BaseGlyphPaintRecords.Records[mid].Paint;
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }
        return null;
    }

    /// <summary>The 14-byte COLR v0 header. Blittable, no padding. The version 1 extension (five Offset32 fields) is read separately so a v0 table does not read past its own extent.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is common to both versions; version 1 appends a 20-byte extension described by <see cref="HeaderV1"/>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="HeaderV1"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct HeaderV0 : IBigEndianStruct<HeaderV0>
    {
        /// <summary>Gets the version number: 0 or 1. Version 1 tables have a 20-byte extension immediately after this header.</summary>
        /// <value>The constant <c>0</c> or <c>1</c> for a conforming COLR table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>version</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="HeaderV1"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>version</c></seealso>
        public ushort Version;               // +0

        /// <summary>Gets the number of base glyph records. The array is sorted by glyph ID.</summary>
        /// <value>The count of <see cref="BaseGlyphRecord"/> entries in the array at <see cref="BaseGlyphRecordsOffset"/>.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>numBaseGlyphRecords</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="BaseGlyphRecordsOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>numBaseGlyphRecords</c></seealso>
        public ushort NumBaseGlyphRecords;   // +2

        /// <summary>Gets the offset from the start of the COLR table to the base glyph records array.</summary>
        /// <value>The byte offset of the first <see cref="BaseGlyphRecord"/>, measured from the table start.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>baseGlyphRecordsOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="NumBaseGlyphRecords"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>baseGlyphRecordsOffset</c></seealso>
        public uint BaseGlyphRecordsOffset;  // +4

        /// <summary>Gets the offset from the start of the COLR table to the layer records array.</summary>
        /// <value>The byte offset of the first <see cref="LayerRecord"/>, measured from the table start.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>layerRecordsOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="NumLayerRecords"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>layerRecordsOffset</c></seealso>
        public uint LayerRecordsOffset;      // +8

        /// <summary>Gets the number of layer records. The array is not sorted.</summary>
        /// <value>The count of <see cref="LayerRecord"/> entries in the array at <see cref="LayerRecordsOffset"/>.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>numLayerRecords</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="LayerRecordsOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>numLayerRecords</c></seealso>
        public ushort NumLayerRecords;       // +12

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All five fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR header</seealso>
        public static HeaderV0 ReverseEndianness(HeaderV0 v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            NumBaseGlyphRecords = BinaryPrimitives.ReverseEndianness(v.NumBaseGlyphRecords),
            BaseGlyphRecordsOffset = BinaryPrimitives.ReverseEndianness(v.BaseGlyphRecordsOffset),
            LayerRecordsOffset = BinaryPrimitives.ReverseEndianness(v.LayerRecordsOffset),
            NumLayerRecords = BinaryPrimitives.ReverseEndianness(v.NumLayerRecords),
        };
    }

    /// <summary>The 20-byte COLR v1 header extension. Blittable, no padding. Read immediately after <see cref="HeaderV0"/> when the version is 1. Every field is an Offset32 relative to the start of the COLR table; zero means the corresponding subtable is absent.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Every offset is measured from the start of the COLR table, not from the start of this header.</description></item>
    /// <item><description>A zero offset is the canonical way to signal an absent subtable; the parser leaves the corresponding <see cref="ColrTable"/> property <c>null</c>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 header extension</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="HeaderV0"/>
    /// <seealso cref="ColrTable.BaseGlyphPaintRecords"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 header extension</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct HeaderV1 : IBigEndianStruct<HeaderV1>
    {
        /// <summary>Gets the offset to the version 1 base glyph paint list, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="BaseGlyphList"/> from the COLR table start.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>baseGlyphListOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ColrTable.BaseGlyphPaintRecords"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>baseGlyphListOffset</c></seealso>
        public uint BaseGlyphListOffset;       // +0

        /// <summary>Gets the offset to the version 1 layer list, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="LayerList"/> from the COLR table start.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>layerListOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ColrTable.LayerList"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>layerListOffset</c></seealso>
        public uint LayerListOffset;           // +4

        /// <summary>Gets the offset to the version 1 clip list, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="ClipList"/> from the COLR table start.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>clipListOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ColrTable.ClipList"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>clipListOffset</c></seealso>
        public uint ClipListOffset;            // +8

        /// <summary>Gets the offset to the version 1 variation index map, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="DeltaSetIndexMap"/> from the COLR table start.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>varIndexMapOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ColrTable.VarIndexMap"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>varIndexMapOffset</c></seealso>
        public uint VarIndexMapOffset;         // +12

        /// <summary>Gets the offset to the version 1 item variation store, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="ItemVariationStore"/> from the COLR table start.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>itemVariationStoreOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ColrTable.ItemVariationStore"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>itemVariationStoreOffset</c></seealso>
        public uint ItemVariationStoreOffset;  // +16

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new extension with each offset field reversed.</returns>
        /// <remarks>All five fields are <c>uint32</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 header extension</seealso>
        public static HeaderV1 ReverseEndianness(HeaderV1 v) => new()
        {
            BaseGlyphListOffset = BinaryPrimitives.ReverseEndianness(v.BaseGlyphListOffset),
            LayerListOffset = BinaryPrimitives.ReverseEndianness(v.LayerListOffset),
            ClipListOffset = BinaryPrimitives.ReverseEndianness(v.ClipListOffset),
            VarIndexMapOffset = BinaryPrimitives.ReverseEndianness(v.VarIndexMapOffset),
            ItemVariationStoreOffset = BinaryPrimitives.ReverseEndianness(v.ItemVariationStoreOffset),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the COLR table.</param>
    /// <param name="context">A <see cref="FontFace"/> or other context passed through to the v1 variation subtable parsers.</param>
    /// <returns>The parsed COLR table.</returns>
    /// <exception cref="InvalidDataException">The version is not 0 or 1.</exception>
    /// <exception cref="EndOfStreamException">The header or any referenced subtable extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The v0 header is read first for both versions; the v1 header extension is read only when the version is 1, so a v0 table does not advance the cursor past its declared extent.</description></item>
    /// <item><description>The base glyph records and layer records arrays are shared between the two versions; a v1 table retains the v0 arrays for backward compatibility.</description></item>
    /// <item><description>Every v1 subtable offset is checked for zero before parsing; a zero offset yields a <c>null</c> property rather than an exception.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="HeaderV0"/>
    /// <seealso cref="HeaderV1"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR table</seealso>
    static ColrTable IRecord<ColrTable>.Parse(ref Cursor cursor, object? context)
    {
        HeaderV0 h0 = cursor.ReadBigEndianStruct<HeaderV0>();

        if (h0.Version is not (0 or 1))
            throw new InvalidDataException($"'COLR'.version is {h0.Version}, expected 0 or 1.");

        Source source = cursor.Source;

        BaseGlyphRecord[] baseGlyphs = h0.NumBaseGlyphRecords > 0
            ? source.ReadBigEndianStructArrayAt<BaseGlyphRecord>(h0.BaseGlyphRecordsOffset, h0.NumBaseGlyphRecords)
            : [];
        LayerRecord[] layers = h0.NumLayerRecords > 0
            ? source.ReadBigEndianStructArrayAt<LayerRecord>(h0.LayerRecordsOffset, h0.NumLayerRecords)
            : [];

        if (h0.Version == 0)
            return new ColrTable { Version = 0, BaseGlyphRecords = baseGlyphs, LayerRecords = layers };

        // Version 1 header extension: five Offset32 fields immediately after the
        // 14-byte v0 header. The cursor is at +14 after the struct read above.
        HeaderV1 h1 = cursor.ReadBigEndianStruct<HeaderV1>();

        return new ColrTable
        {
            Version = 1,
            BaseGlyphRecords = baseGlyphs,
            LayerRecords = layers,
            BaseGlyphPaintRecords = h1.BaseGlyphListOffset != 0 ? source.ParseRecordAt<BaseGlyphList>(h1.BaseGlyphListOffset) : null,
            LayerList = h1.LayerListOffset != 0 ? source.ParseRecordAt<LayerList>(h1.LayerListOffset) : null,
            ClipList = h1.ClipListOffset != 0 ? source.ParseRecordAt<ClipList>(h1.ClipListOffset) : null,
            VarIndexMap = h1.VarIndexMapOffset != 0 ? source.ParseRecordAt<DeltaSetIndexMap>(h1.VarIndexMapOffset, context) : null,
            ItemVariationStore = h1.ItemVariationStoreOffset != 0 ? source.ParseRecordAt<ItemVariationStore>(h1.ItemVariationStoreOffset, context) : null,
        };
    }
}

/// <summary>A version 0 COLR base glyph record. Blittable, size 6.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each record declares a contiguous range of layer records: <see cref="NumLayers"/> consecutive entries starting at <see cref="FirstLayerIndex"/> in the table's layer array.</description></item>
/// <item><description>Records are sorted ascending by <see cref="GlyphId"/>; the parser's binary search relies on this.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v0 base glyph records</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ColrTable"/>
/// <seealso cref="LayerRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v0 base glyph records</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct BaseGlyphRecord : IBigEndianStruct<BaseGlyphRecord>
{
    /// <summary>Base glyph ID.</summary>
    /// <value>The glyph ID whose colour presentation this record defines.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>glyphID</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="FirstLayerIndex"/>
    /// <seealso cref="NumLayers"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>glyphID</c></seealso>
    public ushort GlyphId;          // +0

    /// <summary>Index into the layer records of this glyph's first layer.</summary>
    /// <value>The array index of the first <see cref="LayerRecord"/> belonging to this glyph.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>firstLayerIndex</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphId"/>
    /// <seealso cref="NumLayers"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>firstLayerIndex</c></seealso>
    public ushort FirstLayerIndex;  // +2

    /// <summary>Number of layers in this glyph's definition.</summary>
    /// <value>The count of consecutive <see cref="LayerRecord"/> entries starting at <see cref="FirstLayerIndex"/>.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>numLayers</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphId"/>
    /// <seealso cref="FirstLayerIndex"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>numLayers</c></seealso>
    public ushort NumLayers;        // +4

    /// <inheritdoc/>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new record with each multi-byte field reversed.</returns>
    /// <remarks>All three fields are <c>uint16</c> and are reversed independently.</remarks>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v0 base glyph records</seealso>
    public static BaseGlyphRecord ReverseEndianness(BaseGlyphRecord v) => new()
    {
        GlyphId = BinaryPrimitives.ReverseEndianness(v.GlyphId),
        FirstLayerIndex = BinaryPrimitives.ReverseEndianness(v.FirstLayerIndex),
        NumLayers = BinaryPrimitives.ReverseEndianness(v.NumLayers),
    };
}

/// <summary>A version 0 COLR layer record. Blittable, size 4.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each record pairs a glyph whose outline forms the layer with a CPAL palette entry index for the fill colour.</description></item>
/// <item><description>Layers are drawn in the order they appear in the array, with the first layer of a glyph drawn first (at the bottom of the stack).</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v0 layer records</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ColrTable"/>
/// <seealso cref="BaseGlyphRecord"/>
/// <seealso cref="CpalTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v0 layer records</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct LayerRecord : IBigEndianStruct<LayerRecord>
{
    /// <summary>Glyph ID whose outline is the layer's shape.</summary>
    /// <value>The glyph whose outline is filled with <see cref="PaletteIndex"/> for this layer.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>glyphID</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="PaletteIndex"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>glyphID</c></seealso>
    public ushort GlyphId;       // +0

    /// <summary>CPAL palette entry index for the layer's fill.</summary>
    /// <value>The zero-based index into the CPAL table's palette entries for the current palette.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>paletteIndex</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphId"/>
    /// <seealso cref="CpalTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>paletteIndex</c></seealso>
    public ushort PaletteIndex;  // +2

    /// <inheritdoc/>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new record with each multi-byte field reversed.</returns>
    /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v0 layer records</seealso>
    public static LayerRecord ReverseEndianness(LayerRecord v) => new()
    {
        GlyphId = BinaryPrimitives.ReverseEndianness(v.GlyphId),
        PaletteIndex = BinaryPrimitives.ReverseEndianness(v.PaletteIndex),
    };
}

/// <summary>The COLR version 1 base glyph list: an array of <see cref="BaseGlyphPaintRecord"/> entries, one per base glyph that has a paint graph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Entries are sorted ascending by <see cref="BaseGlyphPaintRecord.GlyphId"/>; the parser's binary search relies on this.</description></item>
/// <item><description>The list is addressed from the COLR v1 header extension's <c>baseGlyphListOffset</c>, and each record's paint offset is relative to the start of this list.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 base glyph list</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ColrTable"/>
/// <seealso cref="BaseGlyphPaintRecord"/>
/// <seealso cref="Paint"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 base glyph list</seealso>
public sealed record BaseGlyphList : IRecord<BaseGlyphList>
{
    /// <summary>Gets the base glyph paint records.</summary>
    /// <value>The array of <see cref="BaseGlyphPaintRecord"/> entries, sorted ascending by <see cref="BaseGlyphPaintRecord.GlyphId"/>.</value>
    /// <seealso cref="BaseGlyphPaintRecord"/>
    public IReadOnlyList<BaseGlyphPaintRecord> Records { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte after the record count.</param>
    /// <param name="context">A <see cref="ParentContext"/> carrying the list-scoped source.</param>
    /// <returns>The parsed list with its paint records resolved.</returns>
    /// <exception cref="EndOfStreamException">The record array or any referenced paint extends past the end of the list-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 base glyph list</see> in the OpenType specification.</remarks>
    /// <seealso cref="BaseGlyphPaintRecord"/>
    /// <seealso cref="ParentContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 base glyph list</seealso>
    static BaseGlyphList IRecord<BaseGlyphList>.Parse(ref Cursor cursor, object? context) => new()
    {
        Records = cursor.ReadBigEndianHeaderRecordArray<BaseGlyphPaintRecord, BaseGlyphPaintRecord.Header>(checked((int)cursor.ReadUInt32()), new ParentContext(cursor.Source)),
    };
}

/// <summary>A version 1 base glyph paint record.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each record pairs a base glyph ID with an offset to the root <see cref="Paint"/> of that glyph's colour graph.</description></item>
/// <item><description>The paint offset is relative to the start of the enclosing <see cref="BaseGlyphList"/>, not to the COLR table.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 base glyph list</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BaseGlyphList"/>
/// <seealso cref="Paint"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 base glyph list</seealso>
public sealed record BaseGlyphPaintRecord : IBigEndianHeaderRecord<BaseGlyphPaintRecord, BaseGlyphPaintRecord.Header>
{
    /// <summary>Gets the base glyph ID.</summary>
    /// <value>The glyph whose colour presentation this record defines.</value>
    /// <seealso cref="Paint"/>
    public ushort GlyphId { get; init; }

    /// <summary>Gets the root paint of the color glyph graph.</summary>
    /// <value>The entry node of the glyph's colour graph, resolved from <see cref="Header.PaintOffset"/>.</value>
    /// <seealso cref="GlyphId"/>
    /// <seealso cref="Paint"/>
    public Paint Paint { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="header">The already-read record header.</param>
    /// <param name="context">A <see cref="ParentContext"/> carrying the list-scoped source.</param>
    /// <returns>A new record with its root paint resolved.</returns>
    /// <remarks>The <c>paintOffset</c> is measured from the start of the enclosing <see cref="BaseGlyphList"/>; the parent context carries the list-scoped source so the offset resolves correctly.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="BaseGlyphList"/>
    static BaseGlyphPaintRecord IHeaderRecord<BaseGlyphPaintRecord, Header>.FromHeader(in Header header, object? context) => new()
    {
        GlyphId = header.GlyphId,
        Paint = ((ParentContext)context!).ParentSource.ParseRecordAt<Paint>(header.PaintOffset),
    };

    /// <summary>The 6-byte base glyph paint record header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>PaintOffset</c> is measured from the start of the enclosing <see cref="BaseGlyphList"/>, not from the COLR table.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// </list>
    /// <para>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 base glyph list</see> in the OpenType specification.</para>
    /// </remarks>
    /// <seealso cref="BaseGlyphPaintRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 base glyph list</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the base glyph ID.</summary>
        /// <value>The glyph whose colour presentation this record defines.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>glyphID</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="PaintOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>glyphID</c></seealso>
        public ushort GlyphId;

        /// <summary>Gets the offset from the start of the enclosing base glyph list to the root paint.</summary>
        /// <value>The byte offset of the <see cref="Paint"/> from the base glyph list start.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>paintOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="GlyphId"/>
        /// <seealso cref="Paint"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>paintOffset</c></seealso>
        public uint PaintOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>Both fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 base glyph list</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            GlyphId = BinaryPrimitives.ReverseEndianness(v.GlyphId),
            PaintOffset = BinaryPrimitives.ReverseEndianness(v.PaintOffset),
        };
    }
}

/// <summary>The COLR version 1 layer list: a flat array of paints that may be shared across glyphs' paint graphs.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The list stores reusable paint nodes; a paint graph references a member through its index rather than duplicating the node.</description></item>
/// <item><description>Paint offsets within the list are relative to the start of the list itself.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 layer list</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ColrTable"/>
/// <seealso cref="Paint"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 layer list</seealso>
public sealed record LayerList : IRecord<LayerList>
{
    /// <summary>Gets the paints.</summary>
    /// <value>The array of <see cref="Paint"/> nodes indexed by the layer list index that other paints reference.</value>
    /// <seealso cref="Paint"/>
    public IReadOnlyList<Paint> Paints { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte after the paint count.</param>
    /// <param name="context">Unused. The layer list's paints carry their own offsets.</param>
    /// <returns>The parsed layer list.</returns>
    /// <exception cref="EndOfStreamException">The paint array or any referenced paint extends past the end of the list-scoped source.</exception>
    /// <remarks>Each paint offset is measured from the start of the layer list itself, which is why the parser is invoked on the list-scoped source.</remarks>
    /// <seealso cref="Paint"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 layer list</seealso>
    static LayerList IRecord<LayerList>.Parse(ref Cursor cursor, object? context) => new() { Paints = cursor.ReadOffset32ArrayPeekRecord<Paint>(checked((int)cursor.ReadUInt32())) };
}

/// <summary>The COLR version 1 ClipList: per-glyph-range clip boxes.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each clip record names a contiguous glyph ID range and a clip box; the clip box bounds the area in which the glyph's colour paint may draw.</description></item>
/// <item><description>Only one clip format is currently defined (format 1).</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 clip list</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ColrTable"/>
/// <seealso cref="ClipRecord"/>
/// <seealso cref="ClipBox"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 clip list</seealso>
public sealed record ClipList : IRecord<ClipList>
{
    /// <summary>Gets the format. Always 1.</summary>
    /// <value>The constant <c>1</c> for a conforming clip list.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>format</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Records"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>format</c></seealso>
    public byte Format { get; init; }

    /// <summary>Gets the clip records.</summary>
    /// <value>The array of <see cref="ClipRecord"/> entries, sorted ascending by <see cref="ClipRecord.StartGlyphId"/>; ranges must not overlap.</value>
    /// <seealso cref="ClipRecord"/>
    public IReadOnlyList<ClipRecord> Records { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the clip list.</param>
    /// <param name="context">A <see cref="ParentContext"/> carrying the list-scoped source.</param>
    /// <returns>The parsed clip list.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1.</exception>
    /// <exception cref="EndOfStreamException">The record array or any referenced clip box extends past the end of the list-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 clip list</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="ClipRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 clip list</seealso>
    static ClipList IRecord<ClipList>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        if (header.Format != 1)
            throw new InvalidDataException($"COLR ClipList format {header.Format} is not defined.");
        return new()
        {
            Format = 1,
            Records = cursor.ReadBigEndianHeaderRecordArray<ClipRecord, ClipRecord.Header>(checked((int)header.NumClips), new ParentContext(cursor.Source)),
        };
    }

    /// <summary>The 5-byte clip list header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by <c>numClips</c> 7-byte <see cref="ClipRecord.Header"/> records.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ClipList"/>
    /// <seealso cref="ClipRecord.Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 clip list</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the format. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming clip list.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>format</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="NumClips"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>format</c></seealso>
        public byte Format;

        /// <summary>Gets the number of clip records.</summary>
        /// <value>The count of <see cref="ClipRecord"/> entries that follow the header.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>numClips</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Format"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>numClips</c></seealso>
        public uint NumClips;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with <c>NumClips</c> reversed and the byte-width format copied through.</returns>
        /// <remarks>The <c>Format</c> field is a single byte and does not participate in the reversal.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 clip list</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            NumClips = BinaryPrimitives.ReverseEndianness(v.NumClips),
        };
    }
}

/// <summary>A single clip record.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each record names a contiguous glyph ID range and carries an offset to a <see cref="ClipBox"/>.</description></item>
/// <item><description>The clip box offset is relative to the start of the enclosing <see cref="ClipList"/>.</description></item>
/// <item><description>Records are sorted ascending by <see cref="StartGlyphId"/>; ranges must not overlap.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 clip list</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ClipList"/>
/// <seealso cref="ClipBox"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 clip list</seealso>
public sealed record ClipRecord : IBigEndianHeaderRecord<ClipRecord, ClipRecord.Header>
{
    /// <summary>First glyph ID covered.</summary>
    /// <value>The inclusive lower bound of the glyph ID range that shares <see cref="ClipBox"/>.</value>
    /// <seealso cref="EndGlyphId"/>
    /// <seealso cref="ClipBox"/>
    public ushort StartGlyphId { get; init; }

    /// <summary>Last glyph ID covered (inclusive).</summary>
    /// <value>The inclusive upper bound of the glyph ID range that shares <see cref="ClipBox"/>.</value>
    /// <seealso cref="StartGlyphId"/>
    /// <seealso cref="ClipBox"/>
    public ushort EndGlyphId { get; init; }

    /// <summary>The clip box.</summary>
    /// <value>The <see cref="ClipBox"/> that bounds the drawing area for glyphs in <see cref="StartGlyphId"/>..<see cref="EndGlyphId"/>.</value>
    /// <seealso cref="StartGlyphId"/>
    /// <seealso cref="EndGlyphId"/>
    /// <seealso cref="ClipBox"/>
    public ClipBox ClipBox { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="header">The already-read record header.</param>
    /// <param name="context">A <see cref="ParentContext"/> carrying the list-scoped source.</param>
    /// <returns>A new record with its clip box resolved.</returns>
    /// <remarks>The <c>clipBoxOffset</c> is a 24-bit offset measured from the start of the enclosing <see cref="ClipList"/>; the parent context carries the list-scoped source so the offset resolves correctly.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="ClipBox"/>
    /// <seealso cref="ClipList"/>
    static ClipRecord IHeaderRecord<ClipRecord, Header>.FromHeader(in Header header, object? context) => new()
    {
        StartGlyphId = header.StartGlyphId,
        EndGlyphId = header.EndGlyphId,
        ClipBox = ((ParentContext)context!).ParentSource.ParseRecordAt<ClipBox>(header.ClipBoxOffset),
    };

    /// <summary>The 7-byte clip record header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>ClipBoxOffset</c> is a <see cref="UInt24"/> measured from the start of the enclosing <see cref="ClipList"/>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// </list>
    /// <para>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 clip list</see> in the OpenType specification.</para>
    /// </remarks>
    /// <seealso cref="ClipRecord"/>
    /// <seealso cref="ClipList"/>
    /// <seealso cref="UInt24"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 clip list</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>First glyph ID covered.</summary>
        /// <value>The inclusive lower bound of the glyph ID range that shares the clip box.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>startGlyphID</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="EndGlyphId"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>startGlyphID</c></seealso>
        public ushort StartGlyphId;

        /// <summary>Last glyph ID covered (inclusive).</summary>
        /// <value>The inclusive upper bound of the glyph ID range that shares the clip box.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>endGlyphID</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="StartGlyphId"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>endGlyphID</c></seealso>
        public ushort EndGlyphId;

        /// <summary>Offset from the start of the enclosing clip list to the clip box.</summary>
        /// <value>The byte offset of the <see cref="ClipBox"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>clipBoxOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ClipBox"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>clipBoxOffset</c></seealso>
        public UInt24 ClipBoxOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>The three fields are reversed independently; <see cref="UInt24.ReverseEndianness(UInt24)"/> handles the 24-bit offset.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 clip list</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            StartGlyphId = BinaryPrimitives.ReverseEndianness(v.StartGlyphId),
            EndGlyphId = BinaryPrimitives.ReverseEndianness(v.EndGlyphId),
            ClipBoxOffset = UInt24.ReverseEndianness(v.ClipBoxOffset),
        };
    }
}

/// <summary>A COLR clip box.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Two formats are defined. Format 1 is a plain rectangle; format 2 adds a variation index base for use with the item variation store.</description></item>
/// <item><description>Coordinates are in font design units, using the same coordinate system as the glyph outlines.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 clip list</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ClipRecord"/>
/// <seealso cref="ClipList"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 clip list</seealso>
public sealed record ClipBox : IRecord<ClipBox>
{
    /// <summary>Format number (1 or 2).</summary>
    /// <value>The constant <c>1</c> or <c>2</c> for a conforming clip box.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>format</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="VarIndexBase"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>format</c></seealso>
    public byte Format { get; init; }

    /// <summary>Left edge.</summary>
    /// <value>The leftmost X coordinate of the clip box, in font design units.</value>
    /// <seealso cref="YMin"/>
    /// <seealso cref="XMax"/>
    /// <seealso cref="YMax"/>
    public short XMin { get; init; }

    /// <summary>Bottom edge.</summary>
    /// <value>The bottommost Y coordinate of the clip box, in font design units.</value>
    /// <seealso cref="XMin"/>
    /// <seealso cref="XMax"/>
    /// <seealso cref="YMax"/>
    public short YMin { get; init; }

    /// <summary>Right edge.</summary>
    /// <value>The rightmost X coordinate of the clip box, in font design units.</value>
    /// <seealso cref="XMin"/>
    /// <seealso cref="YMin"/>
    /// <seealso cref="YMax"/>
    public short XMax { get; init; }

    /// <summary>Top edge.</summary>
    /// <value>The topmost Y coordinate of the clip box, in font design units.</value>
    /// <seealso cref="XMin"/>
    /// <seealso cref="YMin"/>
    /// <seealso cref="XMax"/>
    public short YMax { get; init; }

    /// <summary>Variation index base, or 0 for format 1.</summary>
    /// <value>The base variation index into the item variation store for format 2, or <c>0</c> for format 1 which has no variation support.</value>
    /// <seealso cref="Format"/>
    /// <seealso cref="ColrTable.ItemVariationStore"/>
    /// <seealso cref="ColrTable.VarIndexMap"/>
    public uint VarIndexBase { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the clip box.</param>
    /// <param name="context">Unused. The clip box is self-describing.</param>
    /// <returns>The parsed clip box.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1 or 2.</exception>
    /// <exception cref="EndOfStreamException">The clip box extends past the end of the list-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Format 1 is a plain 9-byte record; format 2 is a 9-byte record followed by a <c>uint32</c> variation index base.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 clip list</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 clip list</seealso>
    static ClipBox IRecord<ClipBox>.Parse(ref Cursor cursor, object? context)
    {
        byte format = cursor.ReadUInt8();
        switch (format)
        {
            case 1:
                {
                    var header = cursor.ReadBigEndianStruct<Header>();
                    return new ClipBox
                    {
                        Format = 1,
                        XMin = header.XMin,
                        YMin = header.YMin,
                        XMax = header.XMax,
                        YMax = header.YMax,
                    };
                }
            case 2:
                {
                    var header = cursor.ReadBigEndianStruct<Var<Header>>();
                    return new ClipBox
                    {
                        Format = 2,
                        XMin = header.Value.XMin,
                        YMin = header.Value.YMin,
                        XMax = header.Value.XMax,
                        YMax = header.Value.YMax,
                        VarIndexBase = header.VarIndexBase,
                    };
                }
            default:
                throw new InvalidDataException($"COLR ClipBox format {format} is not defined.");
        }
    }

    /// <summary>The 9-byte COLR clip box header. Blittable, no padding. Format 1 only; format 2 is a <see cref="Var{T}"/> of this header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header includes the format byte itself, so the total is 1 byte for the format plus four <c>int16</c> coordinate fields.</description></item>
    /// <item><description>For format 2, the same layout is embedded inside a <see cref="Var{T}"/> whose trailing <c>uint32</c> carries the variation index base.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 clip list</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ClipBox"/>
    /// <seealso cref="Var{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 clip list</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Format number (1 for a plain header).</summary>
        /// <value>The constant <c>1</c> for a header read directly, or <c>2</c> when embedded inside a <see cref="Var{T}"/>.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>format</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="XMin"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>format</c></seealso>
        public byte Format;

        /// <summary>Left edge.</summary>
        /// <value>The leftmost X coordinate of the clip box, in font design units.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>xMin</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="YMin"/>
        /// <seealso cref="XMax"/>
        /// <seealso cref="YMax"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>xMin</c></seealso>
        public short XMin;

        /// <summary>Bottom edge.</summary>
        /// <value>The bottommost Y coordinate of the clip box, in font design units.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>yMin</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="XMin"/>
        /// <seealso cref="XMax"/>
        /// <seealso cref="YMax"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>yMin</c></seealso>
        public short YMin;

        /// <summary>Right edge.</summary>
        /// <value>The rightmost X coordinate of the clip box, in font design units.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>xMax</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="XMin"/>
        /// <seealso cref="YMin"/>
        /// <seealso cref="YMax"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>xMax</c></seealso>
        public short XMax;

        /// <summary>Top edge.</summary>
        /// <value>The topmost Y coordinate of the clip box, in font design units.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>yMax</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="XMin"/>
        /// <seealso cref="YMin"/>
        /// <seealso cref="XMax"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>yMax</c></seealso>
        public short YMax;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte coordinate field reversed and the byte-width format copied through.</returns>
        /// <remarks>The <c>Format</c> field is a single byte and does not participate in the reversal.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 clip list</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Format = v.Format,
            XMin = BinaryPrimitives.ReverseEndianness(v.XMin),
            YMin = BinaryPrimitives.ReverseEndianness(v.YMin),
            XMax = BinaryPrimitives.ReverseEndianness(v.XMax),
            YMax = BinaryPrimitives.ReverseEndianness(v.YMax)
        };
    }
}
