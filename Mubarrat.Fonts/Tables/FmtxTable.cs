using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// fmtx — Font Metrics Table (Apple Advanced Typography)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>fmtx</c> table: identifies one glyph whose points represent font-wide metrics — ascent, descent, caret angle, and caret offset — in both writing directions.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is an Apple Advanced Typography (AAT) table and is not part of OpenType. Apple documents it in the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual, <c>fmtx</c> table</see>, and notes there that the material applies only to TrueType fonts, because the metrics are points of a <c>glyf</c> outline.</description></item>
/// <item><description>When the table is present, its metric points override the corresponding values of <c>hhea</c> and <c>vhea</c>. Representing the metrics as points rather than values lets them move with the variation settings — a variable font can change line spacing or caret angle as it changes weight or optical point size — and lets them be instructed so that a font can tune line spacing at small sizes.</description></item>
/// <item><description>Line spacing is measured from the "before" point to the "after" point. "Before" is the distance from the baseline (or centerline for vertical text) to the previous line's "after", and corresponds to the font's ascent; "after" is the distance from the baseline to the next line's "before", and corresponds to the descent.</description></item>
/// <item><description>Caret angle is the angle between the "caret head" point and the "caret base" point. The caret head gives the angle relative to the caret base, and the caret base is where the caret should intersect the baseline (centerline). For horizontal text the y coordinate of the caret base must be 0, and for vertical text its x coordinate must be 0.</description></item>
/// <item><description>The manual requires all eight point numbers to be present even when the font is intended for a single writing direction: "If an <c>'fmtx'</c> table is present, it must specify point numbers for all eight metric points." A point number is an index into the outline of <see cref="GlyphIndex"/>; this parser cannot range-check it, because it does not read <c>glyf</c> or <c>loca</c>.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // The manual's italic example at 2048 units per em.
/// byte ascentPoint = fmtx.HorizontalBefore;      // (0, 1600)
/// byte descentPoint = fmtx.HorizontalAfter;      // (0, -448)
/// byte caretHead = fmtx.HorizontalCaretHead;     // (210, 1600)
/// byte caretBase = fmtx.HorizontalCaretBase;     // (-140, 0)
/// </code>
/// </example>
/// <seealso cref="Header"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6hhea.html">TrueType Reference Manual: The <c>hhea</c> table</seealso>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6vhea.html">TrueType Reference Manual: The <c>vhea</c> table</seealso>
public sealed record FmtxTable : IFontTable<FmtxTable>
{
    /// <summary>Gets the AAT table tag <c>fmtx</c>.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
    public static Tag Tag => "fmtx";

    /// <summary>Gets the table version.</summary>
    /// <remarks>The manual defines <c>0x00020000</c> (2.0) as the version of this table; the parser rejects any other value.</remarks>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="MinorVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the integer part of <see cref="Version"/>, which is 2 for a conforming table.</summary>
    /// <seealso cref="Version"/>
    /// <seealso cref="MinorVersion"/>
    public ushort MajorVersion => (ushort)Version.IntegerPart;

    /// <summary>Gets the fractional part of <see cref="Version"/>, which is 0 for a conforming table.</summary>
    /// <seealso cref="Version"/>
    /// <seealso cref="MajorVersion"/>
    public ushort MinorVersion => Version.FractionPart;

    /// <summary>Gets the glyph whose points represent the metrics.</summary>
    /// <remarks>Every point number in the table is an index into this glyph's outline. A glyph index that is not present in the font makes the table unusable even though its bytes are well formed, which this parser cannot detect on its own.</remarks>
    /// <seealso cref="HorizontalBefore"/>
    /// <seealso cref="VerticalBefore"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
    public uint GlyphIndex { get; init; }

    /// <summary>Gets the point number for the horizontal ascent.</summary>
    /// <remarks>Together with <see cref="HorizontalAfter"/> this defines the horizontal line spacing. "Before" is the distance from the baseline to the previous line's "after".</remarks>
    /// <seealso cref="HorizontalAfter"/>
    /// <seealso cref="HorizontalCaretHead"/>
    /// <seealso cref="HorizontalCaretBase"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
    public byte HorizontalBefore { get; init; }

    /// <summary>Gets the point number for the horizontal descent.</summary>
    /// <remarks>"After" is the distance from the baseline to the next line's "before".</remarks>
    /// <seealso cref="HorizontalBefore"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
    public byte HorizontalAfter { get; init; }

    /// <summary>Gets the point number for the horizontal caret head, which sets the caret's angle relative to the caret base.</summary>
    /// <seealso cref="HorizontalCaretBase"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
    public byte HorizontalCaretHead { get; init; }

    /// <summary>Gets the point number for the horizontal caret base, where the caret intersects the baseline.</summary>
    /// <remarks>For horizontal text the manual requires the y coordinate of this point to be 0; that is a constraint on the outline, not on the table bytes.</remarks>
    /// <seealso cref="HorizontalCaretHead"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
    public byte HorizontalCaretBase { get; init; }

    /// <summary>Gets the point number for the vertical ascent.</summary>
    /// <remarks>Together with <see cref="VerticalAfter"/> this defines the vertical line spacing, measured from the centerline.</remarks>
    /// <seealso cref="VerticalAfter"/>
    /// <seealso cref="VerticalCaretHead"/>
    /// <seealso cref="VerticalCaretBase"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
    public byte VerticalBefore { get; init; }

    /// <summary>Gets the point number for the vertical descent.</summary>
    /// <seealso cref="VerticalBefore"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
    public byte VerticalAfter { get; init; }

    /// <summary>Gets the point number for the vertical caret head, which sets the caret's angle relative to the caret base.</summary>
    /// <seealso cref="VerticalCaretBase"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
    public byte VerticalCaretHead { get; init; }

    /// <summary>Gets the point number for the vertical caret base, where the caret intersects the centerline.</summary>
    /// <remarks>For vertical text the manual requires the x coordinate of this point to be 0; that is a constraint on the outline, not on the table bytes.</remarks>
    /// <seealso cref="VerticalCaretHead"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
    public byte VerticalCaretBase { get; init; }

    /// <summary>The 16-byte fixed-layout <c>fmtx</c> table header, which is also the whole table.</summary>
    /// <remarks>Fields are stored in big-endian order at the offsets defined by the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html"><c>fmtx</c> table specification</see>. The table has no variable-length tail: the header is followed by nothing.</remarks>
    /// <seealso cref="FmtxTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0. Set to <c>0x00020000</c>.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
        public Fixed Version;              // +0

        /// <summary>The glyph whose points represent the metrics at byte offset 4.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
        public uint GlyphIndex;            // +4

        /// <summary>The point number for the horizontal ascent at byte offset 8.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
        public byte HorizontalBefore;      // +8

        /// <summary>The point number for the horizontal descent at byte offset 9.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
        public byte HorizontalAfter;       // +9

        /// <summary>The point number for the horizontal caret head at byte offset 10.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
        public byte HorizontalCaretHead;   // +10

        /// <summary>The point number for the horizontal caret base at byte offset 11.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
        public byte HorizontalCaretBase;   // +11

        /// <summary>The point number for the vertical ascent at byte offset 12.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
        public byte VerticalBefore;        // +12

        /// <summary>The point number for the vertical descent at byte offset 13.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
        public byte VerticalAfter;         // +13

        /// <summary>The point number for the vertical caret head at byte offset 14.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
        public byte VerticalCaretHead;     // +14

        /// <summary>The point number for the vertical caret base at byte offset 15.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
        public byte VerticalCaretBase;     // +15

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/> and copies the byte-width point numbers through unchanged.</summary>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with the version and glyph index reversed and the eight point numbers preserved.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>Only <see cref="Version"/> and <see cref="GlyphIndex"/> are multi-byte and are reversed independently.</description></item>
        /// <item><description>The eight point numbers are single bytes and are copied verbatim, because the object initializer of a struct begins from a zeroed value.</description></item>
        /// </list>
        /// </remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = Fixed.ReverseEndianness(v.Version),
            GlyphIndex = BinaryPrimitives.ReverseEndianness(v.GlyphIndex),
            HorizontalBefore = v.HorizontalBefore,
            HorizontalAfter = v.HorizontalAfter,
            HorizontalCaretHead = v.HorizontalCaretHead,
            HorizontalCaretBase = v.HorizontalCaretBase,
            VerticalBefore = v.VerticalBefore,
            VerticalAfter = v.VerticalAfter,
            VerticalCaretHead = v.VerticalCaretHead,
            VerticalCaretBase = v.VerticalCaretBase,
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the <c>fmtx</c> table.</param>
    /// <param name="context">Unused. The table is self-contained.</param>
    /// <returns>The parsed <c>fmtx</c> table.</returns>
    /// <exception cref="InvalidDataException">The version is not <c>0x00020000</c>.</exception>
    /// <exception cref="EndOfStreamException">The 16-byte header extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The point numbers cannot be validated here: doing so would require the point count of <see cref="GlyphIndex"/> from <c>glyf</c> and <c>loca</c>, which the table does not carry and the context does not supply.</description></item>
    /// <item><description>A font with no point outlines at all cannot honor the table, but that is a file-level inconsistency rather than a malformed <c>fmtx</c> table.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fmtx.html">TrueType Reference Manual: The <c>fmtx</c> table</seealso>
    static FmtxTable IRecord<FmtxTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version.Bits != 0x00020000)
            throw new InvalidDataException(
                $"'fmtx'.version is 0x{header.Version.Bits:X8}, expected 0x00020000.");

        return new FmtxTable
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
    }
}
