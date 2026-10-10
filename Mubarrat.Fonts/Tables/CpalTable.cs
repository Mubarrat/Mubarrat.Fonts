using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>CPAL</c> table: Color Palette Table. A set of one or more palettes, each containing a predefined number of color records in sRGB BGRA format.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>All palettes have the same number of entries. Color records are stored in a single array, and each palette's records are a contiguous sequence within it.</description></item>
/// <item><description>Palettes may overlap (share color records), so the number of distinct palettes may be fewer than <see cref="NumPalettes"/>. The <see cref="ColorRecords"/> array size is therefore <see cref="NumColorRecords"/>, not <c>NumPalettes * NumPaletteEntries</c>.</description></item>
/// <item><description>Version 1 adds palette type flags, palette labels, and palette entry labels. Labels are name table IDs; <c>0xFFFF</c> means "no string provided".</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">CPAL table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ColrTable"/>
/// <seealso cref="ColorRecord"/>
/// <seealso cref="PaletteTypeFlags"/>
/// <seealso cref="HeaderV0"/>
/// <seealso cref="HeaderV1"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: CPAL table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR table</seealso>
public sealed record CpalTable : IFontTable<CpalTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: CPAL table</seealso>
    public static Tag Tag => "CPAL";

    /// <summary>Gets the table version: 0 or 1.</summary>
    /// <value>The constant <c>0</c> for a v0 table, or <c>1</c> for a table that carries palette types and labels.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>version</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="PaletteTypes"/>
    /// <seealso cref="PaletteLabels"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>version</c></seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the number of color records in each palette.</summary>
    /// <value>The count of palette entries per palette. All palettes share this count.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>numPaletteEntries</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="NumPalettes"/>
    /// <seealso cref="GetColor(int, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>numPaletteEntries</c></seealso>
    public ushort NumPaletteEntries { get; init; }

    /// <summary>Gets the number of palettes. At least 1.</summary>
    /// <value>The count of palette definitions in the table. Conforming tables declare one or more.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>numPalettes</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="NumPaletteEntries"/>
    /// <seealso cref="ColorRecordIndices"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>numPalettes</c></seealso>
    public ushort NumPalettes { get; init; }

    /// <summary>Gets the total number of color records across all palettes.</summary>
    /// <value>The size of the <see cref="ColorRecords"/> array. Because palettes may share records, this can be less than <c><see cref="NumPalettes"/> * <see cref="NumPaletteEntries"/></c>.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>numColorRecords</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="ColorRecords"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>numColorRecords</c></seealso>
    public ushort NumColorRecords { get; init; }

    /// <summary>Gets all color records, indexed by <c>ColorRecordIndices[palette] + entry</c>.</summary>
    /// <value>The flat array of <see cref="ColorRecord"/> entries. Individual palettes are windows into this array, so the same record may be shared between palettes.</value>
    /// <seealso cref="GetColor(int, int)"/>
    /// <seealso cref="ColorRecordIndices"/>
    /// <seealso cref="ColorRecord"/>
    public IReadOnlyList<ColorRecord> ColorRecords { get; init; } = [];

    /// <summary>Gets the index of each palette's first color record.</summary>
    /// <value>An array of <see cref="NumPalettes"/> entries; entry <c>i</c> is the index into <see cref="ColorRecords"/> where palette <c>i</c>'s records begin.</value>
    /// <remarks>The array is read from the table immediately after the v0 header, before the v1 extension. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>colorRecordIndices</c> array</see> in the OpenType specification.</remarks>
    /// <seealso cref="GetColor(int, int)"/>
    /// <seealso cref="ColorRecords"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>colorRecordIndices</c></seealso>
    public IReadOnlyList<ushort> ColorRecordIndices { get; init; } = [];

    /// <summary>Gets the palette type flags, or an empty list for version 0.</summary>
    /// <value>An array of <see cref="NumPalettes"/> <see cref="PaletteTypeFlags"/> values, or an empty list when the v1 extension is absent.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>paletteTypes</c> array</see> in the OpenType specification.</remarks>
    /// <seealso cref="PaletteTypeFlags"/>
    /// <seealso cref="Version"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>paletteTypes</c></seealso>
    public IReadOnlyList<PaletteTypeFlags> PaletteTypes { get; init; } = [];

    /// <summary>Gets the name IDs for palette labels, or an empty list. 0xFFFF means none.</summary>
    /// <value>An array of <see cref="NumPalettes"/> name table IDs, or an empty list when the v1 extension is absent. The sentinel value <c>0xFFFF</c> marks a palette with no label.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>paletteLabels</c> array</see> in the OpenType specification.</remarks>
    /// <seealso cref="PaletteEntryLabels"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>paletteLabels</c></seealso>
    public IReadOnlyList<ushort> PaletteLabels { get; init; } = [];

    /// <summary>Gets the name IDs for palette entry labels, or an empty list.</summary>
    /// <value>An array of <see cref="NumPaletteEntries"/> name table IDs, or an empty list when the v1 extension is absent. The sentinel value <c>0xFFFF</c> marks an entry with no label.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>paletteEntryLabels</c> array</see> in the OpenType specification.</remarks>
    /// <seealso cref="PaletteLabels"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>paletteEntryLabels</c></seealso>
    public IReadOnlyList<ushort> PaletteEntryLabels { get; init; } = [];

    /// <summary>Returns the color record at (paletteIndex, entryIndex).</summary>
    /// <param name="paletteIndex">The zero-based palette index; must be in <c>[0, <see cref="NumPalettes"/>)</c>.</param>
    /// <param name="entryIndex">The zero-based entry index within the palette; must be in <c>[0, <see cref="NumPaletteEntries"/>)</c>.</param>
    /// <returns>The <see cref="ColorRecord"/> at the requested palette position.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="paletteIndex"/> is negative or at least <see cref="NumPalettes"/>, or <paramref name="entryIndex"/> is negative or at least <see cref="NumPaletteEntries"/>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup is constant-time: <c>ColorRecordIndices[paletteIndex] + entryIndex</c> indexes directly into <see cref="ColorRecords"/>.</description></item>
    /// <item><description>Both index bounds are validated before the array access; a caller iterating a full palette should still cache the two limits.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// for (int p = 0; p &lt; cpal.NumPalettes; p++)
    ///     for (int e = 0; e &lt; cpal.NumPaletteEntries; e++)
    ///         DrawSwatch(cpal.GetColor(p, e));
    /// </code>
    /// </example>
    /// <seealso cref="ColorRecords"/>
    /// <seealso cref="ColorRecordIndices"/>
    /// <seealso cref="ColorRecord"/>
    public ColorRecord GetColor(int paletteIndex, int entryIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(paletteIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(paletteIndex, NumPalettes);
        ArgumentOutOfRangeException.ThrowIfNegative(entryIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(entryIndex, NumPaletteEntries);

        int index = ColorRecordIndices[paletteIndex] + entryIndex;
        return ColorRecords[index];
    }

    /// <summary>The 12-byte CPAL v0 header. Blittable, no padding. The version 1 extension (three Offset32 fields) is read separately, after the variable-length colorRecordIndices array, so a v0 table does not read past its own extent.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by a <c>colorRecordIndices</c> array of <see cref="NumPalettes"/> <c>uint16</c> values, which sits between the v0 header and the v1 tail.</description></item>
    /// <item><description>The variable-length index array is why the v1 extension cannot be folded into a single struct with this header; the parser reads it as a separate step.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">CPAL header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="HeaderV1"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: CPAL header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct HeaderV0 : IEndianReversibleStruct<HeaderV0>
    {
        /// <summary>Gets the table version: 0 or 1.</summary>
        /// <value>The constant <c>0</c> for a v0 table, or <c>1</c> for a table with the extension.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>version</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="HeaderV1"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>version</c></seealso>
        public ushort Version;               // +0

        /// <summary>Gets the number of color records in each palette.</summary>
        /// <value>The count of palette entries per palette. All palettes share this count.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>numPaletteEntries</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="NumPalettes"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>numPaletteEntries</c></seealso>
        public ushort NumPaletteEntries;     // +2

        /// <summary>Gets the number of palettes.</summary>
        /// <value>The count of entries in the <c>colorRecordIndices</c> array that follows this header.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>numPalettes</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="NumPaletteEntries"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>numPalettes</c></seealso>
        public ushort NumPalettes;           // +4

        /// <summary>Gets the number of color records in the <c>colorRecords</c> array.</summary>
        /// <value>The size of the array at <see cref="ColorRecordsArrayOffset"/>. Palettes may share records, so this can be less than <c>NumPalettes * NumPaletteEntries</c>.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>numColorRecords</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ColorRecordsArrayOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>numColorRecords</c></seealso>
        public ushort NumColorRecords;       // +6

        /// <summary>Gets the offset from the start of the CPAL table to the colorRecords array.</summary>
        /// <value>The byte offset of the first <see cref="ColorRecord"/>, measured from the table start.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>colorRecordsArrayOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="NumColorRecords"/>
        /// <seealso cref="ColorRecord"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>colorRecordsArrayOffset</c></seealso>
        public uint ColorRecordsArrayOffset; // +8

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All five fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: CPAL header</seealso>
        public static HeaderV0 ReverseEndianness(HeaderV0 v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            NumPaletteEntries = BinaryPrimitives.ReverseEndianness(v.NumPaletteEntries),
            NumPalettes = BinaryPrimitives.ReverseEndianness(v.NumPalettes),
            NumColorRecords = BinaryPrimitives.ReverseEndianness(v.NumColorRecords),
            ColorRecordsArrayOffset = BinaryPrimitives.ReverseEndianness(v.ColorRecordsArrayOffset),
        };
    }

    /// <summary>The 12-byte CPAL v1 header extension. Blittable, no padding. Read immediately after the colorRecordIndices array when the version is 1. Every field is an Offset32 relative to the start of the CPAL table; zero means the corresponding array is absent.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Every offset is measured from the start of the CPAL table, not from the start of this header.</description></item>
    /// <item><description>A zero offset is the canonical way to signal an absent array; the parser leaves the corresponding <see cref="CpalTable"/> property as an empty list.</description></item>
    /// <item><description>The extension is read after the <c>colorRecordIndices</c> array, not immediately after the v0 header, because that array is variable-length.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">CPAL v1 extension</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="HeaderV0"/>
    /// <seealso cref="CpalTable.PaletteTypes"/>
    /// <seealso cref="CpalTable.PaletteLabels"/>
    /// <seealso cref="CpalTable.PaletteEntryLabels"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: CPAL v1 extension</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct HeaderV1 : IEndianReversibleStruct<HeaderV1>
    {
        /// <summary>Gets the offset to the paletteTypes array, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="PaletteTypeFlags"/> array from the CPAL table start, or zero when the array is not present.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>paletteTypesArrayOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CpalTable.PaletteTypes"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>paletteTypesArrayOffset</c></seealso>
        public uint PaletteTypesArrayOffset;        // +0

        /// <summary>Gets the offset to the paletteLabels array, or zero when absent.</summary>
        /// <value>The byte offset of the <c>paletteLabels</c> name-ID array from the CPAL table start, or zero when the array is not present.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>paletteLabelsArrayOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CpalTable.PaletteLabels"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>paletteLabelsArrayOffset</c></seealso>
        public uint PaletteLabelsArrayOffset;       // +4

        /// <summary>Gets the offset to the paletteEntryLabels array, or zero when absent.</summary>
        /// <value>The byte offset of the <c>paletteEntryLabels</c> name-ID array from the CPAL table start, or zero when the array is not present.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>paletteEntryLabelsArrayOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CpalTable.PaletteEntryLabels"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>paletteEntryLabelsArrayOffset</c></seealso>
        public uint PaletteEntryLabelsArrayOffset;  // +8

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new extension with each offset field reversed.</returns>
        /// <remarks>All three fields are <c>uint32</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: CPAL v1 extension</seealso>
        public static HeaderV1 ReverseEndianness(HeaderV1 v) => new()
        {
            PaletteTypesArrayOffset = BinaryPrimitives.ReverseEndianness(v.PaletteTypesArrayOffset),
            PaletteLabelsArrayOffset = BinaryPrimitives.ReverseEndianness(v.PaletteLabelsArrayOffset),
            PaletteEntryLabelsArrayOffset = BinaryPrimitives.ReverseEndianness(v.PaletteEntryLabelsArrayOffset),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the CPAL table.</param>
    /// <param name="context">Unused. CPAL is a leaf table with no external dependencies.</param>
    /// <returns>The parsed CPAL table.</returns>
    /// <exception cref="InvalidDataException">The version is not 0 or 1, or the table declares zero palettes or zero palette entries.</exception>
    /// <exception cref="EndOfStreamException">The header, index array, or any referenced array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The v0 header and the variable-length <c>colorRecordIndices</c> array are always read; the v1 extension is read only when the version is 1, so a v0 table does not advance the cursor past its declared extent.</description></item>
    /// <item><description>Every v1 array offset is checked for zero before parsing; a zero offset yields an empty list rather than an exception.</description></item>
    /// <item><description>The <c>colorRecords</c> array is read as <see cref="ColorRecord"/> values (byte-only, no reversal needed); the v1 arrays are read as <c>uint16</c> or <see cref="PaletteTypeFlags"/> values as appropriate.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">CPAL table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="HeaderV0"/>
    /// <seealso cref="HeaderV1"/>
    /// <seealso cref="ColorRecord"/>
    /// <seealso cref="PaletteTypeFlags"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: CPAL table</seealso>
    static CpalTable IRecord<CpalTable>.Parse(ref Cursor cursor, object? context)
    {
        HeaderV0 h0 = cursor.ReadBigEndianStruct<HeaderV0>();

        if (h0.Version is not (0 or 1))
            throw new InvalidDataException($"'CPAL'.version is {h0.Version}, expected 0 or 1.");
        if (h0.NumPalettes == 0)
            throw new InvalidDataException("'CPAL' must declare at least one palette.");
        if (h0.NumPaletteEntries == 0)
            throw new InvalidDataException("'CPAL' must have at least one palette entry.");

        // colorRecordIndices: numPalettes uint16 values immediately after the header.
        // This variable-length array sits between the v0 header and the v1 tail, so the
        // v1 fields cannot be folded into a single struct.
        ushort[] indices = cursor.ReadUInt16Array(h0.NumPalettes);

        // Version 1 header extension: three Offset32 values after the index array.
        HeaderV1 h1 = default;
        if (h0.Version >= 1)
            h1 = cursor.ReadBigEndianStruct<HeaderV1>();

        return new CpalTable
        {
            Version = h0.Version,
            NumPaletteEntries = h0.NumPaletteEntries,
            NumPalettes = h0.NumPalettes,
            NumColorRecords = h0.NumColorRecords,
            ColorRecords = cursor.Source.ReadStructArrayAt<ColorRecord>(h0.ColorRecordsArrayOffset, h0.NumColorRecords),
            ColorRecordIndices = indices,
            PaletteTypes = h1.PaletteTypesArrayOffset != 0 ? cursor.Source.ReadEnumArrayAt<PaletteTypeFlags>(h1.PaletteTypesArrayOffset, h0.NumPalettes) : [],
            PaletteLabels = h1.PaletteLabelsArrayOffset != 0 ? cursor.Source.ReadUInt16ArrayAt(h1.PaletteLabelsArrayOffset, h0.NumPalettes) : [],
            PaletteEntryLabels = h1.PaletteEntryLabelsArrayOffset != 0 ? cursor.Source.ReadUInt16ArrayAt(h1.PaletteEntryLabelsArrayOffset, h0.NumPaletteEntries) : [],
        };
    }
}

/// <summary>A single CPAL color record. Blittable, size 4. The on-disk order is BGRA.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Colors are stored in sRGB and are <b>not pre-multiplied</b>. Alpha 0 is fully transparent; alpha 255 is opaque.</description></item>
/// <item><description>Every field is a single byte, so no endianness reversal is needed and the type deliberately does not implement <see cref="IEndianReversibleStruct{T}"/>.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">CPAL color record</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CpalTable"/>
/// <seealso cref="CpalTable.GetColor(int, int)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: CPAL color record</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct ColorRecord
{
    /// <summary>Blue channel.</summary>
    /// <value>The blue component, in the range <c>[0, 255]</c>.</value>
    /// <remarks>Stored first on disk; the CPAL record layout is BGRA. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>blue</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Green"/>
    /// <seealso cref="Red"/>
    /// <seealso cref="Alpha"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>blue</c></seealso>
    public byte Blue;    // +0

    /// <summary>Green channel.</summary>
    /// <value>The green component, in the range <c>[0, 255]</c>.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>green</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Blue"/>
    /// <seealso cref="Red"/>
    /// <seealso cref="Alpha"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>green</c></seealso>
    public byte Green;   // +1

    /// <summary>Red channel.</summary>
    /// <value>The red component, in the range <c>[0, 255]</c>.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>red</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Blue"/>
    /// <seealso cref="Green"/>
    /// <seealso cref="Alpha"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>red</c></seealso>
    public byte Red;     // +2

    /// <summary>Alpha channel.</summary>
    /// <value>The alpha component, in the range <c>[0, 255]</c>. <c>0</c> is fully transparent; <c>255</c> is opaque.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>alpha</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Blue"/>
    /// <seealso cref="Green"/>
    /// <seealso cref="Red"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>alpha</c></seealso>
    public byte Alpha;   // +3

    /// <summary>Creates a color record from its RGBA components.</summary>
    /// <param name="red">The red component.</param>
    /// <param name="green">The green component.</param>
    /// <param name="blue">The blue component.</param>
    /// <param name="alpha">The alpha component. Defaults to <c>255</c> (opaque).</param>
    /// <remarks>The parameter order is RGBA for call-site readability; the fields are stored in the on-disk BGRA order regardless.</remarks>
    /// <example>
    /// <code>
    /// var magenta = new ColorRecord(255, 0, 255);          // opaque magenta
    /// var halfRed = new ColorRecord(255, 0, 0, 128);       // translucent red
    /// </code>
    /// </example>
    /// <seealso cref="Bgra"/>
    /// <seealso cref="Rgba"/>
    public ColorRecord(byte red, byte green, byte blue, byte alpha = 255)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }

    /// <summary>Gets the 32-bit BGRA value.</summary>
    /// <value>The four channels packed with <see cref="Blue"/> in the least significant byte and <see cref="Alpha"/> in the most significant byte.</value>
    /// <remarks>Matches the on-disk order of a CPAL record treated as a single <see cref="uint"/> on a little-endian host.</remarks>
    /// <seealso cref="Rgba"/>
    public readonly uint Bgra =>
        ((uint)Alpha << 24) | ((uint)Red << 16) | ((uint)Green << 8) | Blue;

    /// <summary>Gets the 32-bit RGBA value.</summary>
    /// <value>The four channels packed with <see cref="Alpha"/> in the least significant byte and <see cref="Red"/> in the most significant byte.</value>
    /// <remarks>Matches the channel order used by many graphics APIs and by CSS 8-digit hex colour notation.</remarks>
    /// <seealso cref="Bgra"/>
    public readonly uint Rgba =>
        ((uint)Red << 24) | ((uint)Green << 16) | ((uint)Blue << 8) | Alpha;

    /// <inheritdoc/>
    /// <returns>The colour formatted as <c>#RRGGBBAA</c>, using uppercase hex digits.</returns>
    /// <remarks>The format matches CSS 8-digit hex notation; the alpha channel is always included, even when it is <c>FF</c>.</remarks>
    /// <seealso cref="Rgba"/>
    public override readonly string ToString() => $"#{Red:X2}{Green:X2}{Blue:X2}{Alpha:X2}";
}

/// <summary>Palette type flags for a CPAL version 1 palette. The on-disk representation is a 32-bit word; this enum mirrors it. Bits 2–31 are reserved and must be zero.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The two defined flags are not mutually exclusive; both may be set on the same palette.</description></item>
/// <item><description>A palette with neither flag set is usable with any background, though renderers are encouraged to fall back on the default palette (index 0) in that case.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>paletteTypes</c> array</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CpalTable.PaletteTypes"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>paletteTypes</c></seealso>
[Flags]
public enum PaletteTypeFlags : uint
{
    /// <summary>No flags set.</summary>
    /// <remarks>Indicates the palette is usable regardless of background, and is a valid combination with no special meaning beyond that.</remarks>
    None = 0,

    /// <summary>Bit 0: palette is appropriate for use with a light background.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>PALETTE_TYPE_USABLE_WITH_LIGHT_BACKGROUND</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="UsableWithDarkBackground"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>PALETTE_TYPE_USABLE_WITH_LIGHT_BACKGROUND</c></seealso>
    UsableWithLightBackground = 0x0001,

    /// <summary>Bit 1: palette is appropriate for use with a dark background.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"><c>PALETTE_TYPE_USABLE_WITH_DARK_BACKGROUND</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="UsableWithLightBackground"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal">OpenType specification: <c>PALETTE_TYPE_USABLE_WITH_DARK_BACKGROUND</c></seealso>
    UsableWithDarkBackground = 0x0002,
}
