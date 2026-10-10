using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>CBDT</c> table (version 3.0): Color Bitmap Data Table. Same layout as EBDT; adds PNG-compressed color formats 17, 18, and 19.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <c>CBDT</c> table stores the actual color bitmap data; the <c>CBLC</c> table stores the index structures that locate it. Parsing <c>CBDT</c> therefore requires <c>CBLC</c> to have been parsed first.</description></item>
/// <item><description>The header format is shared with <c>EBDT</c>; only the version (3.0) and the set of recognised image formats differ.</description></item>
/// <item><description>Image formats 17, 18, and 19 carry PNG-compressed BGRA bitmaps. Formats 1–9 from <c>EBDT</c> are technically available but rarely used in colour strikes.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">CBDT table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CblcTable"/>
/// <seealso cref="EblcTable"/>
/// <seealso cref="BitmapDataReader"/>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="BitmapStrike"/>
/// <seealso cref="FontFace"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: CBDT table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cblc">OpenType specification: CBLC table</seealso>
public sealed record CbdtTable : IFontTable<CbdtTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: CBDT table</seealso>
    public static Tag Tag => "CBDT";

    /// <summary>Gets the major version. Always 3.</summary>
    /// <value>The constant <c>3</c> for a conforming CBDT table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt"><c>majorVersion</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MinorVersion"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: <c>majorVersion</c></seealso>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version. Always 0.</summary>
    /// <value>The constant <c>0</c> for a conforming CBDT table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt"><c>minorVersion</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MajorVersion"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: <c>minorVersion</c></seealso>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the glyph bitmaps, keyed by (ppemX, ppemY, glyphId). Only glyphs that have bitmap data in some strike appear.</summary>
    /// <value>A dictionary whose keys carry the strike dimensions and glyph ID; the value is the parsed <see cref="GlyphBitmap"/>. Empty when no strikes have glyphs.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The dictionary is populated during parse by walking every strike in the corresponding <see cref="CblcTable"/> and materializing each glyph's bitmap eagerly.</description></item>
    /// <item><description>If two strikes share identical <c>(ppemX, ppemY)</c> dimensions, the later strike overwrites the earlier one's entries for glyphs present in both.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetGlyph(int, int, int)"/>
    /// <seealso cref="GlyphBitmap"/>
    /// <seealso cref="BitmapDataReader.ReadAll(Source, IReadOnlyList{BitmapStrike})"/>
    public IReadOnlyDictionary<(int PpemX, int PpemY, int GlyphId), GlyphBitmap> Glyphs { get; init; }
        = new Dictionary<(int, int, int), GlyphBitmap>();

    /// <summary>The 4-byte EBDT/CBDT header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is identical in shape to <c>EBDT</c>'s; the two tables differ only in their version constants and the set of image formats their data blocks may use.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">CBDT header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CbdtTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: CBDT header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the major version. Always 3.</summary>
        /// <value>The constant <c>3</c> for a conforming CBDT table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt"><c>majorVersion</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="MinorVersion"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: <c>majorVersion</c></seealso>
        public ushort MajorVersion;

        /// <summary>Gets the minor version. Always 0.</summary>
        /// <value>The constant <c>0</c> for a conforming CBDT table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt"><c>minorVersion</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="MajorVersion"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: <c>minorVersion</c></seealso>
        public ushort MinorVersion;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both version fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c>; there are no byte-only or nested structures to copy through.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: CBDT header</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            MajorVersion = BinaryPrimitives.ReverseEndianness(v.MajorVersion),
            MinorVersion = BinaryPrimitives.ReverseEndianness(v.MinorVersion),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the CBDT table.</param>
    /// <param name="context">A <see cref="FontFace"/> whose <see cref="FontFace.GetTable{T}"/> is used to resolve the matching <see cref="CblcTable"/>.</param>
    /// <returns>The parsed CBDT table with all glyph bitmaps materialized.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="FontFace"/>.</exception>
    /// <exception cref="InvalidDataException">The version is not 3.0, or a glyph bitmap's data is structurally invalid.</exception>
    /// <exception cref="EndOfStreamException">A glyph bitmap extends past the end of <c>cursor.Source</c>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The parse is eager: every strike in the corresponding <c>CBLC</c> table is walked, and each glyph's bitmap is materialized before this method returns.</description></item>
    /// <item><description>The <c>CBLC</c> table is resolved through <paramref name="context"/> rather than being passed in; this couples CBDT parsing to having already parsed CBLC for the same face.</description></item>
    /// <item><description>The bitmap reader receives <c>cursor.Source</c>, which is table-scoped to CBDT; the offsets stored in the CBLC index subtables are relative to the CBDT table start.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">CBDT table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CblcTable"/>
    /// <seealso cref="BitmapDataReader.ReadAll(Source, IReadOnlyList{BitmapStrike})"/>
    /// <seealso cref="FontFace.GetTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: CBDT table</seealso>
    static CbdtTable IRecord<CbdtTable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not FontFace face)
            throw new InvalidOperationException(
                $"{nameof(CbdtTable)}.Parse requires a {nameof(FontFace)} context.");

        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.MajorVersion != 3 || header.MinorVersion != 0)
            throw new InvalidDataException(
                $"'CBDT' version is {header.MajorVersion}.{header.MinorVersion}, expected 3.0.");

        var strikes = face.GetTable<CblcTable>().Strikes;
        var glyphs = BitmapDataReader.ReadAll(cursor.Source, strikes);

        return new CbdtTable
        {
            MajorVersion = header.MajorVersion,
            MinorVersion = header.MinorVersion,
            Glyphs = glyphs,
        };
    }

    /// <summary>Returns the bitmap for a glyph at a given strike, or <c>null</c>.</summary>
    /// <param name="ppemX">The horizontal pixels per em of the target strike.</param>
    /// <param name="ppemY">The vertical pixels per em of the target strike.</param>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The <see cref="GlyphBitmap"/> for the glyph at the given strike, or <c>null</c> when no entry exists for the composite key.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All three components must match exactly; the strike dimensions are not a range.</description></item>
    /// <item><description>Returns <c>null</c> both when the strike is absent and when the glyph is absent from an otherwise-present strike; the dictionary does not distinguish the two cases.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Glyphs"/>
    /// <seealso cref="GlyphBitmap"/>
    public GlyphBitmap? GetGlyph(int ppemX, int ppemY, int glyphId) =>
        Glyphs.TryGetValue((ppemX, ppemY, glyphId), out var bitmap) ? bitmap : null;
}
