using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>fmtx</c> table: the Apple Advanced Typography font metrics table. Identifies a glyph whose points represent font-wide metrics — ascent, descent, caret angle, caret offset — and overrides the corresponding values in the <c>hhea</c> and <c>vhea</c> tables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is used only by ATSUI, the Apple Type Services for Unicode Imaging layout engine. It exists so that a font can declare "real" ascent and descent values for modern layout without changing its behavior under Quickdraw Text, which constrains glyphs to not exceed the values in the <c>hhea</c> table.</description></item>
/// <item><description>Because the metrics are represented as points rather than fixed values, they can be moved by variation axes (so line spacing and caret angle can change with weight or optical size) and they can be hinted (so line spacing can be tuned at small sizes).</description></item>
/// <item><description>The table applies only to TrueType fonts. It is not defined for CFF-outline fonts.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">font metrics table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="HheaTable"/>
/// <seealso cref="VheaTable"/>
/// <seealso cref="GlyfTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
public sealed record FmtxTable : IFontTable<FmtxTable>, IEndianReversibleHeaderRecord<FmtxTable, FmtxTable.Header>
{
    /// <summary>The version value for the only defined font metrics table format, 2.0.</summary>
    /// <value>The <see cref="Fixed"/> value whose raw bits are <c>0x00020000</c>.</value>
    /// <seealso cref="Version"/>
    public static readonly Fixed CurrentVersion = new(0x00020000);

    /// <inheritdoc/>
    public static Tag Tag => "fmtx";

    /// <summary>Gets the table version.</summary>
    /// <value>The <see cref="CurrentVersion"/> value for a conforming table.</value>
    /// <remarks>The specification documents the version as <c>0x00020000</c> and provides no older version to fall back to.</remarks>
    /// <seealso cref="CurrentVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the glyph whose points carry the font metrics.</summary>
    /// <value>A glyph index in the <c>glyf</c> table. The eight metric points are indices into this glyph's point array.</value>
    /// <remarks>The specification does not restrict the glyph's shape or its classification; any glyph whose outline contains the eight required points is valid.</remarks>
    /// <seealso cref="GlyfTable"/>
    /// <seealso cref="HorizontalBefore"/>
    /// <seealso cref="HorizontalAfter"/>
    public uint GlyphIndex { get; init; }

    /// <summary>Gets the point index for the horizontal ascent metric within the metric glyph.</summary>
    /// <value>The index of the point that specifies the distance from the horizontal baseline to the previous line's <see cref="HorizontalAfter"/> point. In horizontal text this corresponds to the font's ascent.</value>
    /// <seealso cref="HorizontalAfter"/>
    /// <seealso cref="GlyphIndex"/>
    public byte HorizontalBefore { get; init; }

    /// <summary>Gets the point index for the horizontal descent metric within the metric glyph.</summary>
    /// <value>The index of the point that specifies the distance from the horizontal baseline to the next line's <see cref="HorizontalBefore"/> point. In horizontal text this corresponds to the font's descent.</value>
    /// <seealso cref="HorizontalBefore"/>
    /// <seealso cref="GlyphIndex"/>
    public byte HorizontalAfter { get; init; }

    /// <summary>Gets the point index for the horizontal caret head metric within the metric glyph.</summary>
    /// <value>The index of the point that, together with <see cref="HorizontalCaretBase"/>, defines the caret angle for horizontal text.</value>
    /// <remarks>The caret angle is computed from the vector between the caret base point and the caret head point.</remarks>
    /// <seealso cref="HorizontalCaretBase"/>
    /// <seealso cref="GlyphIndex"/>
    public byte HorizontalCaretHead { get; init; }

    /// <summary>Gets the point index for the horizontal caret base metric within the metric glyph.</summary>
    /// <value>The index of the point where the caret intersects the horizontal baseline.</value>
    /// <remarks>The specification requires the y-coordinate of this point to be zero for horizontal text.</remarks>
    /// <seealso cref="HorizontalCaretHead"/>
    /// <seealso cref="GlyphIndex"/>
    public byte HorizontalCaretBase { get; init; }

    /// <summary>Gets the point index for the vertical ascent metric within the metric glyph.</summary>
    /// <value>The index of the point that specifies the distance from the vertical centerline to the previous line's <see cref="VerticalAfter"/> point. In vertical text this corresponds to the font's vertical ascent.</value>
    /// <seealso cref="VerticalAfter"/>
    /// <seealso cref="GlyphIndex"/>
    public byte VerticalBefore { get; init; }

    /// <summary>Gets the point index for the vertical descent metric within the metric glyph.</summary>
    /// <value>The index of the point that specifies the distance from the vertical centerline to the next line's <see cref="VerticalBefore"/> point. In vertical text this corresponds to the font's vertical descent.</value>
    /// <seealso cref="VerticalBefore"/>
    /// <seealso cref="GlyphIndex"/>
    public byte VerticalAfter { get; init; }

    /// <summary>Gets the point index for the vertical caret head metric within the metric glyph.</summary>
    /// <value>The index of the point that, together with <see cref="VerticalCaretBase"/>, defines the caret angle for vertical text.</value>
    /// <seealso cref="VerticalCaretBase"/>
    /// <seealso cref="GlyphIndex"/>
    public byte VerticalCaretHead { get; init; }

    /// <summary>Gets the point index for the vertical caret base metric within the metric glyph.</summary>
    /// <value>The index of the point where the caret intersects the vertical centerline.</value>
    /// <remarks>The specification requires the x-coordinate of this point to be zero for vertical text.</remarks>
    /// <seealso cref="VerticalCaretHead"/>
    /// <seealso cref="GlyphIndex"/>
    public byte VerticalCaretBase { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read <c>fmtx</c> header.</param>
    /// <param name="context">Ignored; the header is self-contained.</param>
    /// <returns>A new <see cref="FmtxTable"/> populated from the header fields.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The eight point indices are copied through unchanged. They are indices into the point array of the glyph identified by <see cref="GlyphIndex"/>, not coordinates themselves.</description></item>
    /// <item><description>Resolving the indices into actual coordinates requires parsing the metric glyph through the <c>glyf</c> table; the table itself carries no coordinates.</description></item>
    /// </list>
    /// </remarks>
    static FmtxTable IHeaderRecord<FmtxTable, Header>.FromHeader(in Header header, object? context) => new()
    {
        Version = header.Version,
        GlyphIndex = header.GlyphIndex,
        HorizontalBefore = header.HorizontalBefore,
        HorizontalAfter = header.HorizontalAfter,
        HorizontalCaretHead = header.HorizontalCaretHead,
        HorizontalCaretBase = header.HorizontalCaretBase,
        VerticalBefore = header.VerticalBefore,
        VerticalAfter = header.VerticalAfter,
        VerticalCaretHead = header.VerticalCaretHead,
        VerticalCaretBase = header.VerticalCaretBase,
    };

    /// <summary>The 16-byte <c>fmtx</c> table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is the whole table; there is no trailing data.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">font metrics table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FmtxTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0, as a <c>Fixed</c> (16.16) value.</summary>
        /// <value>The <see cref="CurrentVersion"/> value for a conforming table.</value>
        public Fixed Version;             // +0

        /// <summary>The glyph whose points carry the metrics at byte offset 4.</summary>
        /// <value>A glyph index in the <c>glyf</c> table.</value>
        public uint GlyphIndex;           // +4

        /// <summary>The horizontal ascent point index at byte offset 8.</summary>
        public byte HorizontalBefore;     // +8

        /// <summary>The horizontal descent point index at byte offset 9.</summary>
        public byte HorizontalAfter;      // +9

        /// <summary>The horizontal caret head point index at byte offset 10.</summary>
        public byte HorizontalCaretHead;  // +10

        /// <summary>The horizontal caret base point index at byte offset 11.</summary>
        public byte HorizontalCaretBase;  // +11

        /// <summary>The vertical ascent point index at byte offset 12.</summary>
        public byte VerticalBefore;       // +12

        /// <summary>The vertical descent point index at byte offset 13.</summary>
        public byte VerticalAfter;        // +13

        /// <summary>The vertical caret head point index at byte offset 14.</summary>
        public byte VerticalCaretHead;    // +14

        /// <summary>The vertical caret base point index at byte offset 15.</summary>
        public byte VerticalCaretBase;    // +15

        /// <summary>Reverses the byte order of the multi-byte fields in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with the multi-byte fields reversed and the byte-only fields copied through.</returns>
        /// <remarks>The eight point indices are single bytes and do not participate in the reversal.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}"/>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = Fixed.ReverseEndianness(value.Version),
            GlyphIndex = BinaryPrimitives.ReverseEndianness(value.GlyphIndex),
            HorizontalBefore = value.HorizontalBefore,
            HorizontalAfter = value.HorizontalAfter,
            HorizontalCaretHead = value.HorizontalCaretHead,
            HorizontalCaretBase = value.HorizontalCaretBase,
            VerticalBefore = value.VerticalBefore,
            VerticalAfter = value.VerticalAfter,
            VerticalCaretHead = value.VerticalCaretHead,
            VerticalCaretBase = value.VerticalCaretBase,
        };
    }
}
