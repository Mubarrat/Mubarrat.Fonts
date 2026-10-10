using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>sbix</c> table: Standard Bitmap Graphics Table. Provides access to bitmap data in standard graphics formats such as PNG, JPEG, or TIFF.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Unlike <c>EBDT</c>, <c>sbix</c> stores data in standard graphics formats and includes the complete data required for processing bitmaps.</description></item>
/// <item><description>A font may include outline glyph data alongside <c>sbix</c>; the outlines are typically the fallback for renderers that do not support bitmap strikes.</description></item>
/// <item><description>Strikes are keyed by a <c>(ppem, ppi)</c> pair, allowing the same font to provide bitmaps at different pixel densities (e.g. 72 ppi and 144 ppi at the same ppem).</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">sbix table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SbixStrike"/>
/// <seealso cref="SbixGlyphData"/>
/// <seealso cref="FontFace"/>
/// <seealso cref="MaxpTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: sbix table</seealso>
public sealed record SbixTable : IFontTable<SbixTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: sbix table</seealso>
    public static Tag Tag => "sbix";

    /// <summary>Gets the table version. Always 1.</summary>
    /// <value>The constant <c>1</c> for a conforming sbix table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix"><c>version</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Flags"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: <c>version</c></seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the header flags. Bit 0 is always set to 1 per the specification; bit 1, when set, instructs the application to draw the outline over the bitmap.</summary>
    /// <value>A 16-bit flag word. Bit 0 must be <c>1</c>; bit 1 controls outline overlay; bits 2–15 are reserved.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix"><c>flags</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="DrawOutlines"/>
    /// <seealso cref="Version"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: <c>flags</c></seealso>
    public ushort Flags { get; init; }

    /// <summary>Gets the strikes, in table order.</summary>
    /// <value>The ordered list of <see cref="SbixStrike"/> entries declared by the table. Empty when the table declares no strikes.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The list order follows the on-disk order; it is not required to be sorted by <c>(ppem, ppi)</c>.</description></item>
    /// <item><description>Each strike is fully materialized during parse, including its per-glyph data.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetStrike(int, int)"/>
    /// <seealso cref="StrikeCount"/>
    /// <seealso cref="SbixStrike"/>
    public IReadOnlyList<SbixStrike> Strikes { get; init; } = [];

    /// <summary>Gets the number of strikes.</summary>
    /// <value>The size of the <see cref="Strikes"/> list.</value>
    /// <seealso cref="Strikes"/>
    public int StrikeCount => Strikes.Count;

    /// <summary>True when bit 1 of <see cref="Flags"/> is set (outline drawn over bitmap).</summary>
    /// <value><see langword="true"/> when <see cref="Flags"/> has bit 1 set.</value>
    /// <remarks>When this is set, a renderer that supports both outlines and sbix should draw the outline on top of the bitmap.</remarks>
    /// <seealso cref="Flags"/>
    public bool DrawOutlines => (Flags & 0x0002) != 0;

    /// <summary>Returns the strike with the given (ppem, ppi), or <c>null</c> when absent.</summary>
    /// <param name="ppem">The horizontal pixels-per-em to search for.</param>
    /// <param name="ppi">The target device pixel density to search for.</param>
    /// <returns>The first <see cref="SbixStrike"/> whose <see cref="SbixStrike.Ppem"/> and <see cref="SbixStrike.Ppi"/> match, or <c>null</c> when no strike matches.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup is linear over <see cref="Strikes"/>; the table is typically small.</description></item>
    /// <item><description>Both components of the key must match exactly; the ppem is not a range.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Strikes"/>
    /// <seealso cref="SbixStrike.Ppem"/>
    /// <seealso cref="SbixStrike.Ppi"/>
    public SbixStrike? GetStrike(int ppem, int ppi)
    {
        foreach (var s in Strikes)
            if (s.Ppem == ppem && s.Ppi == ppi) return s;
        return null;
    }

    /// <summary>The 8-byte <c>sbix</c> table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by a <c>strikeOffsets[numStrikes]</c> array of Offset32 values, each relative to the table start.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">sbix header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SbixTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: sbix header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the table version. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming sbix table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix"><c>version</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Flags"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: <c>version</c></seealso>
        public ushort Version;     // +0

        /// <summary>Gets the header flags. Bit 0 is always set to 1 per the specification; bit 1, when set, instructs the application to draw the outline over the bitmap.</summary>
        /// <value>A 16-bit flag word. Bit 0 must be <c>1</c>; bit 1 controls outline overlay; bits 2–15 are reserved.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix"><c>flags</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Version"/>
        /// <seealso cref="NumStrikes"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: <c>flags</c></seealso>
        public ushort Flags;       // +2

        /// <summary>Gets the number of strikes. Must not exceed 65 535.</summary>
        /// <value>The count of <c>strikeOffsets</c> entries that follow the header.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix"><c>numStrikes</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Flags"/>
        /// <seealso cref="SbixStrike"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: <c>numStrikes</c></seealso>
        public uint NumStrikes;    // +4

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: sbix header</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
            NumStrikes = BinaryPrimitives.ReverseEndianness(v.NumStrikes),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the sbix table.</param>
    /// <param name="context">A <see cref="FontFace"/> whose <c>maxp</c> table provides the glyph count and whose table directory provides the declared table length.</param>
    /// <returns>The parsed sbix table with all strikes and glyph data materialized.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="FontFace"/>.</exception>
    /// <exception cref="InvalidDataException">The version is not 1, bit 0 of the flags is not set, the strike count exceeds the safety limit, or the strike-offset array exceeds the declared table length.</exception>
    /// <exception cref="EndOfStreamException">The header, strike array, or any referenced strike extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The glyph count for the strike's per-glyph offset array comes from <c>maxp.numGlyphs</c>, resolved through <paramref name="context"/>.</description></item>
    /// <item><description>The declared table length from the sfnt directory bounds the strike-offset array before allocation; <c>cursor.Source</c> is table-scoped so <c>cursor.Position</c> measures from the table start.</description></item>
    /// <item><description>Every strike and its glyph data are materialized eagerly during parse.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">sbix table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="SbixStrike"/>
    /// <seealso cref="FontFace.GetTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: sbix table</seealso>
    static SbixTable IRecord<SbixTable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not FontFace face)
            throw new InvalidOperationException(
                $"{nameof(SbixTable)}.Parse requires a {nameof(FontFace)} context.");

        int numGlyphs = face.GetTable<MaxpTable>().NumGlyphs;

        long tableLength = cursor.Source.Length;

        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version != 1)
            throw new InvalidDataException($"'sbix'.version is {header.Version}, expected 1.");
        if ((header.Flags & 0x0001) == 0)
            throw new InvalidDataException(
                $"'sbix'.flags is 0x{header.Flags:X4}; bit 0 must be set to 1 per the specification.");

        if (header.NumStrikes > 65535)
            throw new InvalidDataException(
                $"'sbix'.numStrikes is {header.NumStrikes}, exceeding the safety limit.");

        Source source = cursor.Source;
        int numStrikes = (int)header.NumStrikes;

        // Bound the strike-offset array against the table's declared length before
        // allocating. cursor.Position is now 8 (past the header); the remaining table
        // bytes are tableLength - 8.
        long neededBytes = (long)numStrikes * 4;
        long availableBytes = tableLength - cursor.Position;
        if (neededBytes > availableBytes)
            throw new InvalidDataException(
                $"'sbix' needs {neededBytes} bytes for {numStrikes} strike offsets " +
                $"but only {availableBytes} remain in the declared table length {tableLength}.");

        var strikes = cursor.ReadOffset32ArrayPeekRecord<SbixStrike>(
            numStrikes, new SbixStrike.Context(numGlyphs));

        return new SbixTable
        {
            Version = header.Version,
            Flags = header.Flags,
            Strikes = strikes,
        };
    }
}

/// <summary>A single <c>sbix</c> strike: a PPEM/PPI pair plus per-glyph bitmap data.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The strike's glyph-data offsets are relative to the start of the strike data header, which is the cursor's source base when this record is parsed.</description></item>
/// <item><description>Each glyph's data block is resolved lazily through <see cref="IRecord{T}.Parse"/> against the same strike-scoped source.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">sbix strike</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SbixTable"/>
/// <seealso cref="SbixGlyphData"/>
/// <seealso cref="Context"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: sbix strike</seealso>
public sealed record SbixStrike : IRecord<SbixStrike>
{
    /// <summary>Gets the PPEM this strike was designed for.</summary>
    /// <value>The horizontal pixels-per-em at which the bitmaps were designed.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix"><c>ppem</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Ppi"/>
    /// <seealso cref="SbixTable.GetStrike(int, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: <c>ppem</c></seealso>
    public ushort Ppem { get; init; }

    /// <summary>Gets the device pixel density (PPI) this strike targets.</summary>
    /// <value>The pixel density in pixels per inch, typically <c>72</c> for unscaled or <c>144</c> for Retina-style displays.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix"><c>ppi</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Ppem"/>
    /// <seealso cref="SbixTable.GetStrike(int, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: <c>ppi</c></seealso>
    public ushort Ppi { get; init; }

    /// <summary>Gets the per-glyph data. Length is <c>maxp.numGlyphs</c>; entries may be <c>null</c> when the strike has no bitmap for that glyph.</summary>
    /// <value>The array of <see cref="SbixGlyphData"/> entries indexed by glyph ID. A <c>null</c> entry means the strike provides no bitmap for that glyph; the renderer should fall back to outlines or another strike.</value>
    /// <seealso cref="GetGlyph(int)"/>
    /// <seealso cref="SbixGlyphData"/>
    public IReadOnlyList<SbixGlyphData?> Glyphs { get; init; } = [];

    /// <summary>Returns the bitmap for a glyph, or <c>null</c> when this strike has no data for it.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The glyph's bitmap data, or <c>null</c> when the glyph is out of range or the strike has no bitmap for it.</returns>
    /// <remarks>The lookup is constant-time; the array is indexed directly by glyph ID.</remarks>
    /// <seealso cref="Glyphs"/>
    /// <seealso cref="SbixGlyphData"/>
    public SbixGlyphData? GetGlyph(int glyphId) =>
        (uint)glyphId < (uint)Glyphs.Count ? Glyphs[glyphId] : null;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the strike data header.</param>
    /// <param name="context">A <see cref="Context"/> carrying the glyph count from <c>maxp</c>.</param>
    /// <returns>The parsed strike with its per-glyph data resolved.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="Context"/>.</exception>
    /// <exception cref="EndOfStreamException">The strike header, offset array, or any referenced glyph data extends past the end of the strike-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The per-glyph offset array has <c>numGlyphs + 1</c> entries; each entry bounds the start of the next, so a glyph's data length is the difference between consecutive offsets.</description></item>
    /// <item><description>Consecutive offsets that are equal (or decreasing) mark an absent glyph; the corresponding entry is left <c>null</c> rather than throwing.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">sbix strike</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Context"/>
    /// <seealso cref="SbixGlyphData"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: sbix strike</seealso>
    static SbixStrike IRecord<SbixStrike>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
            throw new InvalidOperationException(
                $"{nameof(SbixStrike)}.Parse requires a {nameof(Context)} " +
                "carrying the glyph count from 'maxp'.");

        ushort ppem = cursor.ReadUInt16();
        ushort ppi = cursor.ReadUInt16();

        // glyphDataOffsets: Offset32[numGlyphs+1] immediately after the 4-byte header.
        // Each offset is relative to the strike start, which is cursor.Source's base.
        int count = ctx.NumGlyphs + 1;
        uint[] offsets = cursor.ReadUInt32Array(count);

        var glyphs = new SbixGlyphData?[ctx.NumGlyphs];
        for (int gid = 0; gid < ctx.NumGlyphs; gid++)
        {
            uint start = offsets[gid];
            uint end = offsets[gid + 1];
            if (end <= start) continue;   // no bitmap for this glyph in this strike

            glyphs[gid] = cursor.Source.ParseRecordAt<SbixGlyphData>(start, new SbixGlyphData.Context((int)(end - start)));
        }

        return new SbixStrike { Ppem = ppem, Ppi = ppi, Glyphs = glyphs };
    }

    /// <summary>Context carrying the number of glyphs from <c>maxp</c>, which determines the length of the strike's glyph-data offset array.</summary>
    /// <param name="NumGlyphs">The value of <c>maxp.numGlyphs</c> for the font.</param>
    /// <remarks>The offset array has <c>NumGlyphs + 1</c> entries so that each glyph's data extent can be derived from consecutive offsets.</remarks>
    /// <seealso cref="MaxpTable.NumGlyphs"/>
    /// <seealso cref="SbixStrike"/>
    public record Context(int NumGlyphs);
}

/// <summary>A single glyph's bitmap data within a strike.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The glyph data block has no internal length field; the enclosing strike determines its extent from consecutive glyph-data offsets.</description></item>
/// <item><description>That length is supplied through <see cref="Context"/> so the parse can find the payload boundary.</description></item>
/// <item><description>The payload format is determined by <see cref="GraphicType"/>: <c>"png "</c>, <c>"jpg "</c>, and <c>"tiff"</c> carry a standard image file; <c>"dupe"</c> carries a 2-byte glyph ID that the current glyph's bitmap duplicates.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">sbix glyph data</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SbixStrike"/>
/// <seealso cref="Context"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: sbix glyph data</seealso>
public sealed record SbixGlyphData : IRecord<SbixGlyphData>
{
    /// <summary>Gets the x position of the bitmap's left edge relative to the glyph origin.</summary>
    /// <value>A signed horizontal offset, in font design units, from the glyph origin to the left edge of the bitmap.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix"><c>originOffsetX</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="OriginOffsetY"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: <c>originOffsetX</c></seealso>
    public short OriginOffsetX { get; init; }

    /// <summary>Gets the y position of the bitmap's bottom edge relative to the glyph origin.</summary>
    /// <value>A signed vertical offset, in font design units, from the glyph origin to the bottom edge of the bitmap.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix"><c>originOffsetY</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="OriginOffsetX"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: <c>originOffsetY</c></seealso>
    public short OriginOffsetY { get; init; }

    /// <summary>Gets the graphic type tag: <c>"png "</c>, <c>"jpg "</c>, <c>"tiff"</c>, or <c>"dupe"</c>.</summary>
    /// <value>The four-character tag identifying the format of <see cref="Data"/>.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix"><c>graphicType</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Data"/>
    /// <seealso cref="IsDupe"/>
    /// <seealso cref="GetDupeGlyphId"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: <c>graphicType</c></seealso>
    public Tag GraphicType { get; init; }

    /// <summary>Gets the raw payload. For <c>"dupe"</c>, a 2-byte glyph ID.</summary>
    /// <value>The complete image file bytes for image formats, or a 2-byte big-endian glyph ID for <c>"dupe"</c>.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The payload is returned verbatim; the library does not decode PNG, JPEG, or TIFF data.</description></item>
    /// <item><description>For a <c>"dupe"</c> record, prefer <see cref="GetDupeGlyphId"/> over reading <see cref="Data"/> directly.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GraphicType"/>
    /// <seealso cref="IsDupe"/>
    /// <seealso cref="GetDupeGlyphId"/>
    public ReadOnlyMemory<byte> Data { get; init; }

    /// <summary>True when this is a <c>"dupe"</c> reference to another glyph.</summary>
    /// <value><see langword="true"/> when <see cref="GraphicType"/> equals the tag <c>"dupe"</c>.</value>
    /// <seealso cref="GraphicType"/>
    /// <seealso cref="GetDupeGlyphId"/>
    public bool IsDupe => GraphicType == "dupe";

    /// <summary>Returns the dupe glyph ID, or <c>-1</c> when not a dupe or the payload is short.</summary>
    /// <returns>The glyph ID referenced by a <c>"dupe"</c> record, or <c>-1</c> when <see cref="IsDupe"/> is <see langword="false"/> or <see cref="Data"/> has fewer than two bytes.</returns>
    /// <remarks>The glyph ID is read as a big-endian <c>uint16</c> from the first two bytes of <see cref="Data"/>.</remarks>
    /// <seealso cref="IsDupe"/>
    /// <seealso cref="Data"/>
    /// <seealso cref="GraphicType"/>
    public int GetDupeGlyphId() =>
        IsDupe && Data.Length >= 2
            ? BinaryPrimitives.ReadUInt16BigEndian(Data.Span)
            : -1;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the glyph data block.</param>
    /// <param name="context">A <see cref="Context"/> carrying the block's byte length, derived from consecutive strike offsets.</param>
    /// <returns>The parsed glyph data.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="Context"/>.</exception>
    /// <exception cref="InvalidDataException">The declared length is smaller than the glyph data header.</exception>
    /// <exception cref="EndOfStreamException">The header or payload extends past the end of the strike-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">sbix glyph data</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Context"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: sbix glyph data</seealso>
    static SbixGlyphData IRecord<SbixGlyphData>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
            throw new InvalidOperationException(
                $"{nameof(SbixGlyphData)}.Parse requires a {nameof(Context)} " +
                "carrying the glyph data block's byte length.");

        if (ctx.Length < Header.Size)
            throw new InvalidDataException(
                $"'sbix' glyph data length is {ctx.Length}, expected at least {Header.Size}.");

        Header header = cursor.ReadBigEndianStruct<Header>();

        int payloadLength = ctx.Length - Header.Size;
        byte[] payload = payloadLength > 0 ? cursor.ReadBytes(payloadLength) : [];

        return new SbixGlyphData
        {
            OriginOffsetX = header.OriginOffsetX,
            OriginOffsetY = header.OriginOffsetY,
            GraphicType = header.GraphicType,
            Data = payload,
        };
    }

    /// <summary>The 8-byte glyph data header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed by a variable-length payload whose length is determined by the enclosing strike, not by a field in this header.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">sbix glyph data</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SbixGlyphData"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: sbix glyph data</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The size of the header, in bytes.</summary>
        /// <value>The constant <c>8</c>.</value>
        /// <remarks>Used by <see cref="IRecord{T}.Parse"/> to derive the payload offset from the block length.</remarks>
        /// <seealso cref="IRecord{T}.Parse"/>
        public const int Size = 8;

        /// <summary>Gets the x position of the bitmap's left edge relative to the glyph origin.</summary>
        /// <value>A signed horizontal offset, in font design units.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix"><c>originOffsetX</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="OriginOffsetY"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: <c>originOffsetX</c></seealso>
        public short OriginOffsetX;   // +0

        /// <summary>Gets the y position of the bitmap's bottom edge relative to the glyph origin.</summary>
        /// <value>A signed vertical offset, in font design units.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix"><c>originOffsetY</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="OriginOffsetX"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: <c>originOffsetY</c></seealso>
        public short OriginOffsetY;   // +2

        /// <summary>Gets the graphic type tag: <c>"png "</c>, <c>"jpg "</c>, <c>"tiff"</c>, or <c>"dupe"</c>.</summary>
        /// <value>The four-character format identifier for the payload.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix"><c>graphicType</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="OriginOffsetX"/>
        /// <seealso cref="Tag"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: <c>graphicType</c></seealso>
        public Tag GraphicType;       // +4

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>The two coordinate fields use <see cref="BinaryPrimitives.ReverseEndianness(short)"/>; the graphic type uses <see cref="Tag.ReverseEndianness(Tag)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/sbix">OpenType specification: sbix glyph data</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            OriginOffsetX = BinaryPrimitives.ReverseEndianness(v.OriginOffsetX),
            OriginOffsetY = BinaryPrimitives.ReverseEndianness(v.OriginOffsetY),
            GraphicType = Tag.ReverseEndianness(v.GraphicType),
        };
    }

    /// <summary>Context carrying the byte length of the glyph data block, which is derived from the difference between consecutive glyph-data offsets in the enclosing strike.</summary>
    /// <param name="Length">The total byte length of the glyph data block, including the 8-byte header.</param>
    /// <remarks>The length is derived by the enclosing <see cref="SbixStrike"/> from <c>offsets[gid + 1] - offsets[gid]</c>.</remarks>
    /// <seealso cref="SbixStrike"/>
    /// <seealso cref="Header.Size"/>
    public record Context(int Length);
}
