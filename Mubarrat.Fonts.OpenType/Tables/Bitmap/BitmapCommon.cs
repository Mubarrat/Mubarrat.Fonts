using Mubarrat.Fonts.OpenType.Binary;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.OpenType.Tables.Bitmap;

/// <summary>Big glyph metrics: bounding box and side bearings for both horizontal and vertical layouts. Blittable, size 8. Used when both layout directions may apply to the same strike (e.g. Kanji fonts).</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Used by the bitmap size tables (<c>EBDT</c>/<c>CBDT</c>) when the strike declares metrics that describe both layout directions. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics">EBLC, <c>bigGlyphMetrics</c></see> section of the specification.</description></item>
/// <item><description>Blittable, size 8, no padding. The struct can be read directly via <see cref="Source.ReadBigEndianStructAt{T}(long)"/>; every field is one byte, so no endianness conversion is needed.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SmallGlyphMetrics"/>
/// <seealso cref="BitmapStrike"/>
/// <seealso cref="IBigEndianStruct{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics">OpenType specification: <c>bigGlyphMetrics</c></seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct BigGlyphMetrics
{
    /// <summary>Bitmap height in pixels.</summary>
    /// <value>The vertical extent of the glyph bitmap, measured in pixels.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics"><c>height</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Width"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics">OpenType specification: <c>height</c></seealso>
    public byte Height;

    /// <summary>Bitmap width in pixels.</summary>
    /// <value>The horizontal extent of the glyph bitmap, measured in pixels.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics"><c>width</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Height"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics">OpenType specification: <c>width</c></seealso>
    public byte Width;

    /// <summary>Horizontal bearing X: distance from the horizontal origin to the left edge.</summary>
    /// <value>A signed pixel offset. Negative values place the left edge to the left of the origin.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics"><c>horiBearingX</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="HoriBearingY"/>
    /// <seealso cref="HoriAdvance"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics">OpenType specification: <c>horiBearingX</c></seealso>
    public sbyte HoriBearingX;

    /// <summary>Horizontal bearing Y: distance from the horizontal origin to the top edge.</summary>
    /// <value>A signed pixel offset, measured upward from the baseline.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics"><c>horiBearingY</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="HoriBearingX"/>
    /// <seealso cref="HoriAdvance"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics">OpenType specification: <c>horiBearingY</c></seealso>
    public sbyte HoriBearingY;

    /// <summary>Horizontal advance width in pixels.</summary>
    /// <value>The distance the pen moves forward after drawing the glyph in horizontal layout.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics"><c>horiAdvance</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="HoriBearingX"/>
    /// <seealso cref="HoriBearingY"/>
    /// <seealso cref="VertAdvance"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics">OpenType specification: <c>horiAdvance</c></seealso>
    public byte HoriAdvance;

    /// <summary>Vertical bearing X: distance from the vertical origin to the left edge.</summary>
    /// <value>A signed pixel offset, measured rightward from the vertical origin.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics"><c>vertBearingX</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="VertBearingY"/>
    /// <seealso cref="VertAdvance"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics">OpenType specification: <c>vertBearingX</c></seealso>
    public sbyte VertBearingX;

    /// <summary>Vertical bearing Y: distance from the vertical origin to the top edge.</summary>
    /// <value>A signed pixel offset, measured upward from the vertical origin.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics"><c>vertBearingY</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="VertBearingX"/>
    /// <seealso cref="VertAdvance"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics">OpenType specification: <c>vertBearingY</c></seealso>
    public sbyte VertBearingY;

    /// <summary>Vertical advance width in pixels.</summary>
    /// <value>The distance the pen moves downward after drawing the glyph in vertical layout.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics"><c>vertAdvance</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="VertBearingX"/>
    /// <seealso cref="VertBearingY"/>
    /// <seealso cref="HoriAdvance"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bigglyphmetrics">OpenType specification: <c>vertAdvance</c></seealso>
    public byte VertAdvance;
}

/// <summary>Small glyph metrics: bounding box and side bearings for one layout direction only. Blittable, size 5. The applicable direction (horizontal or vertical) is determined by the flags field of the containing <see cref="BitmapStrike"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Used when the strike declares metrics for only one layout direction; the direction is selected by the strike's flags. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#smallglyphmetrics">EBLC, <c>smallGlyphMetrics</c></see> section of the specification.</description></item>
/// <item><description>Blittable, size 5, no padding. The struct can be read directly via <see cref="Source.ReadBigEndianStructAt{T}(long)"/>; every field is one byte, so no endianness conversion is needed.</description></item>
/// <item><description>The same struct is used for both horizontal and vertical strikes; the meaning of <see cref="BearingX"/> and <see cref="BearingY"/> flips depending on the layout direction, as described in their individual docs.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BigGlyphMetrics"/>
/// <seealso cref="BitmapStrike"/>
/// <seealso cref="IBigEndianStruct{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#smallglyphmetrics">OpenType specification: <c>smallGlyphMetrics</c></seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct SmallGlyphMetrics
{
    /// <summary>Bitmap height in pixels.</summary>
    /// <value>The vertical extent of the glyph bitmap, measured in pixels.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#smallglyphmetrics"><c>height</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Width"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#smallglyphmetrics">OpenType specification: <c>height</c></seealso>
    public byte Height;

    /// <summary>Bitmap width in pixels.</summary>
    /// <value>The horizontal extent of the glyph bitmap, measured in pixels.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#smallglyphmetrics"><c>width</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Height"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#smallglyphmetrics">OpenType specification: <c>width</c></seealso>
    public byte Width;

    /// <summary>For horizontal text: distance from the horizontal origin to the left edge of the bitmap. For vertical text: distance from the vertical origin to the top edge.</summary>
    /// <value>A signed pixel offset whose meaning depends on the layout direction selected by the containing strike's flags.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#smallglyphmetrics"><c>bearingX</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="BearingY"/>
    /// <seealso cref="Advance"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#smallglyphmetrics">OpenType specification: <c>bearingX</c></seealso>
    public sbyte BearingX;

    /// <summary>For horizontal text: distance from the horizontal origin to the top edge of the bitmap. For vertical text: distance from the vertical origin to the left edge.</summary>
    /// <value>A signed pixel offset whose meaning depends on the layout direction selected by the containing strike's flags.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#smallglyphmetrics"><c>bearingY</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="BearingX"/>
    /// <seealso cref="Advance"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#smallglyphmetrics">OpenType specification: <c>bearingY</c></seealso>
    public sbyte BearingY;

    /// <summary>Advance width in pixels for the applicable layout direction.</summary>
    /// <value>The distance the pen moves after drawing the glyph, measured in pixels along the layout axis.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#smallglyphmetrics"><c>advance</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="BearingX"/>
    /// <seealso cref="BearingY"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#smallglyphmetrics">OpenType specification: <c>advance</c></seealso>
    public byte Advance;
}
