using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;
using System.Buffers.Binary;

namespace Mubarrat.Fonts.OpenType.Tables.Metadata;

/// <summary>The <c>hdmx</c> table: horizontal device metrics. Stores integer advance widths scaled to specific pixel sizes for TrueType fonts, letting layout engines skip a call to the scaler.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Only meaningful when bit 4 of <c>head.flags</c> is set — the flag that indicates some glyphs scale nonlinearly. The table should not be present otherwise.</description></item>
/// <item><description>Each device record stores one byte per glyph, so the table's usefulness is limited to fonts with fewer than 65 536 glyphs and widths under 256 pixels.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">hdmx table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="HdmxDeviceRecord"/>
/// <seealso cref="MaxpTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">OpenType specification: hdmx table</seealso>
public sealed record HdmxTable : IOpenTypeTable<HdmxTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IOpenTypeTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">OpenType specification: hdmx table</seealso>
    public static Tag Tag => "hdmx";

    /// <summary>Gets the table version. Always 0.</summary>
    /// <value>The constant <c>0</c> for a conforming hdmx table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx"><c>version</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="SizeDeviceRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">OpenType specification: <c>version</c></seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the declared size of each device record, in bytes, 32-bit aligned.</summary>
    /// <value>The on-disk size of a single <see cref="HdmxDeviceRecord"/>, including trailing padding to a 4-byte boundary.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx"><c>sizeDeviceRecord</c> field</see> in the OpenType specification.</description></item>
    /// <item><description>Must be at least <c>2 + numGlyphs</c>; the parser rejects smaller values.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Records"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">OpenType specification: <c>sizeDeviceRecord</c></seealso>
    public uint SizeDeviceRecord { get; init; }

    /// <summary>Gets the number of glyphs, from <c>maxp</c>.</summary>
    /// <value>The value of <see cref="MaxpTable.NumGlyphs"/>, used as the length of each device record's width array.</value>
    /// <seealso cref="MaxpTable"/>
    /// <seealso cref="HdmxDeviceRecord.Widths"/>
    public int NumGlyphs { get; init; }

    /// <summary>Gets the device records, sorted by increasing <see cref="HdmxDeviceRecord.PixelSize"/>.</summary>
    /// <value>The ordered list of <see cref="HdmxDeviceRecord"/> entries. The specification requires increasing pixel-size order, enabling binary search in <see cref="GetRecord(int)"/>.</value>
    /// <seealso cref="GetRecord(int)"/>
    /// <seealso cref="RecordCount"/>
    /// <seealso cref="HdmxDeviceRecord"/>
    public IReadOnlyList<HdmxDeviceRecord> Records { get; init; } = [];

    /// <summary>Gets the number of device records.</summary>
    /// <value>The size of the <see cref="Records"/> list.</value>
    /// <seealso cref="Records"/>
    public int RecordCount => Records.Count;

    /// <summary>Returns the record for <paramref name="pixelSize"/>, or <c>null</c> when the table has no data for that ppem.</summary>
    /// <param name="pixelSize">The pixel size (ppem) to look up.</param>
    /// <returns>The <see cref="HdmxDeviceRecord"/> whose <see cref="HdmxDeviceRecord.PixelSize"/> matches, or <c>null</c> when absent.</returns>
    /// <remarks>The lookup uses binary search over <see cref="Records"/>, which the specification requires to be sorted by ascending pixel size.</remarks>
    /// <seealso cref="Records"/>
    /// <seealso cref="HdmxDeviceRecord.PixelSize"/>
    public HdmxDeviceRecord? GetRecord(int pixelSize)
    {
        int lo = 0, hi = Records.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            var r = Records[mid];
            if (r.PixelSize == pixelSize) return r;
            if (r.PixelSize < pixelSize) lo = mid + 1;
            else hi = mid - 1;
        }
        return null;
    }

    /// <summary>Returns the advance width of a glyph at <paramref name="pixelSize"/>, or 0 when the size is absent.</summary>
    /// <param name="pixelSize">The pixel size (ppem) to look up.</param>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The glyph's advance width in pixels at the given size, or <c>0</c> when the size is not present or the glyph ID is out of range.</returns>
    /// <remarks>Returns <c>0</c> for both "size absent" and "glyph out of range"; the two cases are not distinguished.</remarks>
    /// <example>
    /// <code>
    /// byte width = hdmx.GetWidth(12, glyphId);
    /// </code>
    /// </example>
    /// <seealso cref="GetRecord(int)"/>
    /// <seealso cref="HdmxDeviceRecord.GetWidth(int)"/>
    public byte GetWidth(int pixelSize, int glyphId) =>
        GetRecord(pixelSize)?.GetWidth(glyphId) ?? 0;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the hdmx table.</param>
    /// <param name="context">A <see cref="FontFace"/> whose <c>maxp</c> table provides the glyph count used to size each record's width array.</param>
    /// <returns>The parsed hdmx table.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="FontFace"/>.</exception>
    /// <exception cref="InvalidDataException">The version is not 0, or <see cref="SizeDeviceRecord"/> is smaller than <c>2 + numGlyphs</c>.</exception>
    /// <exception cref="EndOfStreamException">The header or any device record extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The glyph count is read from <c>maxp</c> through <paramref name="context"/>, not stored in the hdmx table itself.</description></item>
    /// <item><description>Each device record is passed the <see cref="MaxpTable"/> as its parse context so the width array can be sized correctly.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">hdmx table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="HdmxDeviceRecord"/>
    /// <seealso cref="FontFace.GetTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">OpenType specification: hdmx table</seealso>
    public static HdmxTable Parse(ref Cursor cursor, object? context)
    {
        var face = (FontFace)context!;
        MaxpTable maxpTable = face.GetTable<MaxpTable>();
        int numGlyphs = maxpTable.NumGlyphs;

        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version != 0)
            throw new InvalidDataException($"'hdmx'.version is {header.Version}, expected 0.");

        if (header.SizeDeviceRecord < (uint)(2 + numGlyphs))
            throw new InvalidDataException(
                $"'hdmx'.sizeDeviceRecord is {header.SizeDeviceRecord}, " +
                $"too small for {numGlyphs} glyphs.");

        var records = cursor.ReadRecordArray<HdmxDeviceRecord>(header.NumRecords, maxpTable);
        return new()
        {
            Version = header.Version,
            SizeDeviceRecord = header.SizeDeviceRecord,
            NumGlyphs = numGlyphs,
            Records = records,
        };
    }

    /// <summary>The 8-byte <c>hdmx</c> header as defined by the specification: version, numRecords, sizeDeviceRecord.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">hdmx header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="HdmxTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">OpenType specification: hdmx header</seealso>
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the table version. Always 0.</summary>
        /// <value>The constant <c>0</c> for a conforming hdmx table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx"><c>version</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="NumRecords"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">OpenType specification: <c>version</c></seealso>
        public ushort Version;

        /// <summary>Gets the number of device records that follow the header.</summary>
        /// <value>The count of <see cref="HdmxDeviceRecord"/> entries.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx"><c>numRecords</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Version"/>
        /// <seealso cref="SizeDeviceRecord"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">OpenType specification: <c>numRecords</c></seealso>
        public ushort NumRecords;

        /// <summary>Gets the size of each device record, in bytes, 32-bit aligned.</summary>
        /// <value>The on-disk size of one <see cref="HdmxDeviceRecord"/>, including trailing padding.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx"><c>sizeDeviceRecord</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="NumRecords"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">OpenType specification: <c>sizeDeviceRecord</c></seealso>
        public uint SizeDeviceRecord;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(value.Version),
            NumRecords = BinaryPrimitives.ReverseEndianness(value.NumRecords),
            SizeDeviceRecord = BinaryPrimitives.ReverseEndianness(value.SizeDeviceRecord),
        };
    }
}

/// <summary>A single <c>hdmx</c> device record: a pixel size, the maximum width, and a width per glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record is declared to occupy <c>2 + numGlyphs</c> bytes plus trailing padding to satisfy the table's 4-byte alignment rule.</description></item>
/// <item><description>Records in the enclosing table are sorted by ascending <see cref="PixelSize"/>; the parser relies on that ordering for the binary search in <see cref="HdmxTable.GetRecord(int)"/>.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">hdmx device record</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="HdmxTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">OpenType specification: hdmx device record</seealso>
public sealed record HdmxDeviceRecord : IRecord<HdmxDeviceRecord>
{
    /// <summary>Gets the ppem this record applies to.</summary>
    /// <value>The pixel size (pixels per em) at which the widths in <see cref="Widths"/> apply.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx"><c>pixelSize</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MaxWidth"/>
    /// <seealso cref="Widths"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">OpenType specification: <c>pixelSize</c></seealso>
    public byte PixelSize { get; init; }

    /// <summary>Gets the maximum advance width across all glyphs at this ppem.</summary>
    /// <value>The largest value in <see cref="Widths"/>, in pixels.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx"><c>maxWidth</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="PixelSize"/>
    /// <seealso cref="Widths"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">OpenType specification: <c>maxWidth</c></seealso>
    public byte MaxWidth { get; init; }

    /// <summary>Gets the advance width of each glyph at this ppem, in pixels.</summary>
    /// <value>The per-glyph advance widths; length equals the font's <c>maxp.numGlyphs</c>. Index <c>i</c> is glyph ID <c>i</c>.</value>
    /// <seealso cref="GetWidth(int)"/>
    /// <seealso cref="MaxWidth"/>
    public IReadOnlyList<byte> Widths { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the device record.</param>
    /// <param name="context">A <see cref="MaxpTable"/> whose <see cref="MaxpTable.NumGlyphs"/> determines the width array's length.</param>
    /// <returns>The parsed device record.</returns>
    /// <exception cref="EndOfStreamException">The pixel size, max width, or width array extends past the end of the source.</exception>
    /// <remarks>The record's trailing padding, if any, is not consumed; each record is sized to the enclosing table's <see cref="HdmxTable.SizeDeviceRecord"/> when the parser advances between records.</remarks>
    /// <seealso cref="MaxpTable"/>
    /// <seealso cref="Widths"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hdmx">OpenType specification: hdmx device record</seealso>
    public static HdmxDeviceRecord Parse(ref Cursor cursor, object? context)
    {
        MaxpTable table = (MaxpTable)context!;
        return new HdmxDeviceRecord
        {
            PixelSize = cursor.ReadUInt8(),
            MaxWidth = cursor.ReadUInt8(),
            Widths = cursor.ReadUInt8Array(table.NumGlyphs),
        };
    }

    /// <summary>Returns the advance width of a glyph, or 0 when out of range.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The glyph's advance width in pixels, or <c>0</c> when <paramref name="glyphId"/> is outside <see cref="Widths"/>.</returns>
    /// <remarks>The lookup is constant-time; the glyph ID directly indexes into <see cref="Widths"/>.</remarks>
    /// <seealso cref="Widths"/>
    /// <seealso cref="HdmxTable.GetWidth(int, int)"/>
    public byte GetWidth(int glyphId) =>
        (uint)glyphId < (uint)Widths.Count ? Widths[glyphId] : (byte)0;
}
