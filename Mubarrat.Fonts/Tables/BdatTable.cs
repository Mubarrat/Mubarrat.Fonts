using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>bdat</c> table: the Apple Advanced Typography bitmap data table. Holds the actual bitmap glyph payloads, located by the paired <c>bloc</c> table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is a version header followed by the bitmap data. It carries no index of its own; the paired <c>bloc</c> table declares which glyphs have bitmaps at which sizes and gives the offset of each bitmap within this table.</description></item>
/// <item><description>Six glyph bitmap formats are defined: 1, 2, 4, 5, 6, and 7. Formats 1, 2, 5, 6, and 7 are byte-for-byte identical to the corresponding <c>EBDT</c> formats and are handled by the shared <see cref="GlyphBitmap"/> hierarchy. Format 4 is unique to <c>bdat</c> and carries a modified Huffman encoding.</description></item>
/// <item><description>Within a single font, different formats may be mixed. A font commonly uses one format for proportional glyphs and another for monospaced glyphs; the format for each glyph is declared by the <c>bloc</c> index subtable that covers it.</description></item>
/// <item><description>A <c>bdat</c> table and a <c>bloc</c> table are inextricably connected: if one is present, the other must be, and a change to one almost always requires a change to the other. The two are best read and written together.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">bitmap data table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BlocTable"/>
/// <seealso cref="BhedTable"/>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="GlyphBitmapFormat4"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">TrueType Reference Manual: The <c>bdat</c> table</seealso>
public sealed record BdatTable : IFontTable<BdatTable>, IRecord<BdatTable>
{
    /// <summary>The version value for the only defined bitmap data table format, 2.0.</summary>
    /// <value>The <see cref="Fixed"/> value whose raw bits are <c>0x00020000</c>.</value>
    /// <remarks>The specification documents <c>0x00020000</c> as the initial version of the table. The wire field is a <c>fixed32</c>.</remarks>
    public static readonly Fixed CurrentVersion = new(0x00020000);

    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">TrueType Reference Manual: The <c>bdat</c> table</seealso>
    public static Tag Tag => "bdat";

    /// <summary>Gets the table version.</summary>
    /// <value>The <see cref="CurrentVersion"/> value for a conforming table.</value>
    /// <remarks>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">bitmap data table</see> in the Apple TrueType Reference Manual.</remarks>
    /// <seealso cref="CurrentVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">TrueType Reference Manual: The <c>bdat</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the glyph bitmaps, keyed by (ppemX, ppemY, glyphId).</summary>
    /// <value>A dictionary whose keys carry the strike dimensions and glyph ID; the value is the parsed <see cref="GlyphBitmap"/>. Empty when the paired <c>bloc</c> table declares no strikes with bitmaps.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The dictionary is populated during parse by walking every strike in the paired <c>bloc</c> table and materializing each glyph's bitmap eagerly.</description></item>
    /// <item><description>If two strikes share identical <c>(ppemX, ppemY)</c> dimensions, the later strike overwrites the earlier one's entries for glyphs present in both.</description></item>
    /// <item><description>Format 4 records appear in the dictionary like any other format; the <see cref="GlyphBitmapFormat4"/> subclass exposes the raw Huffman trees and encoded glyph data for a downstream decoder.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GlyphBitmap"/>
    /// <seealso cref="BlocTable"/>
    /// <seealso cref="BitmapDataReader.ReadAll(Source, IReadOnlyList{BitmapStrike})"/>
    public IReadOnlyDictionary<(int PpemX, int PpemY, int GlyphId), GlyphBitmap> Glyphs { get; init; }
        = new Dictionary<(int, int, int), GlyphBitmap>();

    /// <summary>Gets the 8-byte <c>bdat</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The wire field is a <c>fixed32</c> version number. It is stored here as two <c>uint16</c> fields for consistency with the corresponding <c>EBDT</c> header, which is byte-identical on disk.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">bitmap data table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BdatTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">TrueType Reference Manual: The <c>bdat</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The major version of the table at byte offset 0. Always 2.</summary>
        /// <value>The constant <c>2</c> for a conforming <c>bdat</c> table.</value>
        /// <remarks>The wire field is a <c>fixed32</c>; the major version occupies the high <c>uint16</c> of that value.</remarks>
        public ushort MajorVersion;   // +0

        /// <summary>The minor version of the table at byte offset 2. Always 0.</summary>
        /// <value>The constant <c>0</c> for a conforming <c>bdat</c> table.</value>
        /// <remarks>The wire field is a <c>fixed32</c>; the minor version occupies the low <c>uint16</c> of that value.</remarks>
        public ushort MinorVersion;   // +2

        /// <summary>Reverses the byte order of both fields in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}"/>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">TrueType Reference Manual: The <c>bdat</c> table</seealso>
        public static Header ReverseEndianness(Header value) => new()
        {
            MajorVersion = BinaryPrimitives.ReverseEndianness(value.MajorVersion),
            MinorVersion = BinaryPrimitives.ReverseEndianness(value.MinorVersion),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the <c>bdat</c> table.</param>
    /// <param name="context">A <see cref="FontFace"/> whose <see cref="FontFace.GetTable{T}"/> is used to resolve the paired <see cref="BlocTable"/>.</param>
    /// <returns>The parsed <c>bdat</c> table with all glyph bitmaps materialized.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="FontFace"/>.</exception>
    /// <exception cref="InvalidDataException">The version is not 2.0, or a glyph bitmap's data is structurally invalid.</exception>
    /// <exception cref="EndOfStreamException">A glyph bitmap extends past the end of <c>cursor.Source</c>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The parse is eager: every strike in the paired <c>bloc</c> table is walked, and each glyph's bitmap is materialized before this method returns.</description></item>
    /// <item><description>The <c>bloc</c> table is resolved through <paramref name="context"/> rather than being passed in; this couples <c>bdat</c> parsing to having already parsed <c>bloc</c> for the same face.</description></item>
    /// <item><description>The bitmap reader receives <c>cursor.Source</c>, which is table-scoped to <c>bdat</c>; the offsets stored in the <c>bloc</c> index subtables are relative to the <c>bdat</c> table start.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">bitmap data table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BlocTable"/>
    /// <seealso cref="BitmapDataReader"/>
    /// <seealso cref="FontFace.GetTable{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">TrueType Reference Manual: The <c>bdat</c> table</seealso>
    static BdatTable IRecord<BdatTable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not FontFace face)
        {
            throw new InvalidOperationException(
                $"{nameof(BdatTable)}.Parse requires a {nameof(FontFace)} context.");
        }

        Header header = cursor.ReadEndianReversibleStruct<Header>();
        if (header.MajorVersion != 2 || header.MinorVersion != 0)
        {
            throw new InvalidDataException(
                $"'bdat' version is {header.MajorVersion}.{header.MinorVersion}, expected 2.0.");
        }

        var strikes = face.GetTable<BlocTable>().Strikes;
        var glyphs = BitmapDataReader.ReadAll(cursor.Source, strikes);

        return new BdatTable
        {
            Version = CurrentVersion,
            Glyphs = glyphs,
        };
    }
}
