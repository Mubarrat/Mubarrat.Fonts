using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>EBDT</c> table (version 2.0): Embedded Bitmap Data Table. Holds monochrome or grayscale bitmap glyph payloads, addressed by the paired EBLC table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <c>EBDT</c> table stores the actual bitmap data; the <c>EBLC</c> table stores the index structures that locate it. Parsing <c>EBDT</c> therefore requires <c>EBLC</c> to have been parsed first.</description></item>
/// <item><description>The header format is shared with <c>CBDT</c>; only the version (2.0) and the set of recognised image formats differ. <c>CBDT</c> adds PNG-compressed formats 17–19 for colour.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">EBDT table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="EblcTable"/>
/// <seealso cref="CbdtTable"/>
/// <seealso cref="BitmapDataReader"/>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="BitmapStrike"/>
/// <seealso cref="EbdtComponent"/>
/// <seealso cref="FontFace"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">OpenType specification: EBDT table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">OpenType specification: EBLC table</seealso>
public sealed record EbdtTable : IFontTable<EbdtTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">OpenType specification: EBDT table</seealso>
    public static Tag Tag => "EBDT";

    /// <summary>Gets the major version. Always 2.</summary>
    /// <value>The constant <c>2</c> for a conforming EBDT table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt"><c>majorVersion</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MinorVersion"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">OpenType specification: <c>majorVersion</c></seealso>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version. Always 0.</summary>
    /// <value>The constant <c>0</c> for a conforming EBDT table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt"><c>minorVersion</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MajorVersion"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">OpenType specification: <c>minorVersion</c></seealso>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the glyph bitmaps, keyed by (ppemX, ppemY, glyphId). Only glyphs that have bitmap data in some strike appear.</summary>
    /// <value>A dictionary whose keys carry the strike dimensions and glyph ID; the value is the parsed <see cref="GlyphBitmap"/>. Empty when no strikes have glyphs.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The dictionary is populated during parse by walking every strike in the corresponding <see cref="EblcTable"/> and materializing each glyph's bitmap eagerly.</description></item>
    /// <item><description>If two strikes share identical <c>(ppemX, ppemY)</c> dimensions, the later strike overwrites the earlier one's entries for glyphs present in both.</description></item>
    /// <item><description>Composite glyphs are stored as a single entry under their own glyph ID; the component bitmaps referenced by <see cref="EbdtComponent"/> are not expanded or merged into the parent entry.</description></item>
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
    /// <item><description>The header is identical in shape to <c>CBDT</c>'s; the two tables differ only in their version constants and the set of image formats their data blocks may use.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">EBDT header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="EbdtTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">OpenType specification: EBDT header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the major version. Always 2.</summary>
        /// <value>The constant <c>2</c> for a conforming EBDT table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt"><c>majorVersion</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="MinorVersion"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">OpenType specification: <c>majorVersion</c></seealso>
        public ushort MajorVersion;

        /// <summary>Gets the minor version. Always 0.</summary>
        /// <value>The constant <c>0</c> for a conforming EBDT table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt"><c>minorVersion</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="MajorVersion"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">OpenType specification: <c>minorVersion</c></seealso>
        public ushort MinorVersion;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both version fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c>; there are no byte-only or nested structures to copy through.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">OpenType specification: EBDT header</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            MajorVersion = BinaryPrimitives.ReverseEndianness(v.MajorVersion),
            MinorVersion = BinaryPrimitives.ReverseEndianness(v.MinorVersion),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the EBDT table.</param>
    /// <param name="context">A <see cref="FontFace"/> whose <see cref="FontFace.GetTable{T}"/> is used to resolve the matching <see cref="EblcTable"/>.</param>
    /// <returns>The parsed EBDT table with all glyph bitmaps materialized.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="FontFace"/>.</exception>
    /// <exception cref="InvalidDataException">The version is not 2.0, or a glyph bitmap's data is structurally invalid.</exception>
    /// <exception cref="EndOfStreamException">A glyph bitmap extends past the end of <c>cursor.Source</c>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The parse is eager: every strike in the corresponding <c>EBLC</c> table is walked, and each glyph's bitmap is materialized before this method returns.</description></item>
    /// <item><description>The <c>EBLC</c> table is resolved through <paramref name="context"/> rather than being passed in; this couples EBDT parsing to having already parsed EBLC for the same face.</description></item>
    /// <item><description>The bitmap reader receives <c>cursor.Source</c>, which is table-scoped to EBDT; the offsets stored in the EBLC index subtables are relative to the EBDT table start.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">EBDT table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="EblcTable"/>
    /// <seealso cref="BitmapDataReader.ReadAll(Source, IReadOnlyList{BitmapStrike})"/>
    /// <seealso cref="FontFace.GetTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">OpenType specification: EBDT table</seealso>
    static EbdtTable IRecord<EbdtTable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not FontFace face)
            throw new InvalidOperationException(
                $"{nameof(EbdtTable)}.Parse requires a {nameof(FontFace)} context.");

        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.MajorVersion != 2 || header.MinorVersion != 0)
            throw new InvalidDataException(
                $"'EBDT' version is {header.MajorVersion}.{header.MinorVersion}, expected 2.0.");

        var strikes = face.GetTable<EblcTable>().Strikes;
        var glyphs = BitmapDataReader.ReadAll(cursor.Source, strikes);

        return new EbdtTable
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
    /// <item><description>Composite glyphs are returned as-is; component references are not resolved by this method.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Glyphs"/>
    /// <seealso cref="GlyphBitmap"/>
    /// <seealso cref="EbdtComponent"/>
    public GlyphBitmap? GetGlyph(int ppemX, int ppemY, int glyphId) =>
        Glyphs.TryGetValue((ppemX, ppemY, glyphId), out var bitmap) ? bitmap : null;
}

/// <summary>A component reference inside a composite bitmap glyph. Blittable, size 4. Nested composites are allowed; the depth is limited by the implementation's stack space.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Used by the composite bitmap image formats: <c>EBDT</c> formats 8 and 9, and <c>CBDT</c> formats 18 and 19. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</description></item>
/// <item><description>Blittable, size 4, no padding. The struct can be read directly via <see cref="Cursor.ReadBigEndianStruct{T}"/> or <see cref="Source.ReadEndianReversibleStructAt{T}(long)"/>.</description></item>
/// <item><description>Each reference identifies a component glyph plus a signed placement offset. Components are drawn in array order, so later components overlap earlier ones.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // A composite glyph is an array of component references:
/// EbdtComponent[] components = cursor.ReadBigEndianStructArray&lt;EbdtComponent&gt;(componentCount);
/// foreach (var c in components)
///     Draw(c.GlyphId, x + c.XOffset, y + c.YOffset);
/// </code>
/// </example>
/// <seealso cref="EbdtTable"/>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="IEndianReversibleStruct{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct EbdtComponent : IEndianReversibleStruct<EbdtComponent>
{
    /// <summary>Component glyph ID.</summary>
    /// <value>The glyph ID of the referenced component; the referenced glyph's own bitmap is looked up in the same strike.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats"><c>glyphID</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="XOffset"/>
    /// <seealso cref="YOffset"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: <c>glyphID</c></seealso>
    public ushort GlyphId;

    /// <summary>X offset of the component's top-left corner within the composite.</summary>
    /// <value>A signed pixel offset. Positive values move the component right of the composite origin.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats"><c>xOffset</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphId"/>
    /// <seealso cref="YOffset"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: <c>xOffset</c></seealso>
    public sbyte XOffset;

    /// <summary>Y offset of the component's top-left corner within the composite.</summary>
    /// <value>A signed pixel offset. Positive values move the component down from the composite origin, matching the bitmap coordinate convention.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats"><c>yOffset</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphId"/>
    /// <seealso cref="XOffset"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: <c>yOffset</c></seealso>
    public sbyte YOffset;

    /// <inheritdoc/>
    /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new reference with <see cref="GlyphId"/> reversed and the two byte-width offsets copied through.</returns>
    /// <remarks>The two offset fields are <c>int8</c> and do not participate in the reversal; only <see cref="GlyphId"/> is <c>uint16</c>.</remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    /// <seealso cref="Source.ReadEndianReversibleStructAt{T}(long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
    public static EbdtComponent ReverseEndianness(EbdtComponent value) => new()
    {
        GlyphId = BinaryPrimitives.ReverseEndianness(value.GlyphId),
        XOffset = value.XOffset,
        YOffset = value.YOffset
    };
}
