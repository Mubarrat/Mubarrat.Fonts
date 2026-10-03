using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables.Hinting;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.OpenType.Tables.Outlines;

// ═══════════════════════════════════════════════════════════════════════════
// Header
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The 10-byte glyph header common to simple and composite glyphs. Blittable, no padding.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Every glyph in the <c>glyf</c> table begins with this header. The sign of <see cref="NumberOfContours"/> selects between <see cref="SimpleGlyph"/> and <see cref="CompositeGlyph"/>.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">glyf table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Glyph"/>
/// <seealso cref="SimpleGlyph"/>
/// <seealso cref="CompositeGlyph"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">OpenType specification: glyf table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct GlyphHeader : IBigEndianStruct<GlyphHeader>
{
    /// <summary>Number of contours. Non-negative for simple glyphs; negative for composites.</summary>
    /// <value>A count for <see cref="SimpleGlyph"/>, or <c>-1</c> for <see cref="CompositeGlyph"/>.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf"><c>numberOfContours</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="XMin"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">OpenType specification: <c>numberOfContours</c></seealso>
    public short NumberOfContours;

    /// <summary>Minimum x of the glyph's bounding box, in font design units.</summary>
    /// <value>The left edge of the glyph's bounding box, in font design units.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf"><c>xMin</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="YMin"/>
    /// <seealso cref="XMax"/>
    /// <seealso cref="YMax"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">OpenType specification: <c>xMin</c></seealso>
    public short XMin;

    /// <summary>Minimum y of the glyph's bounding box, in font design units.</summary>
    /// <value>The bottom edge of the glyph's bounding box, in font design units.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf"><c>yMin</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="XMin"/>
    /// <seealso cref="XMax"/>
    /// <seealso cref="YMax"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">OpenType specification: <c>yMin</c></seealso>
    public short YMin;

    /// <summary>Maximum x of the glyph's bounding box, in font design units.</summary>
    /// <value>The right edge of the glyph's bounding box, in font design units.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf"><c>xMax</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="XMin"/>
    /// <seealso cref="YMin"/>
    /// <seealso cref="YMax"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">OpenType specification: <c>xMax</c></seealso>
    public short XMax;

    /// <summary>Maximum y of the glyph's bounding box, in font design units.</summary>
    /// <value>The top edge of the glyph's bounding box, in font design units.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf"><c>yMax</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="XMin"/>
    /// <seealso cref="YMin"/>
    /// <seealso cref="XMax"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">OpenType specification: <c>yMax</c></seealso>
    public short YMax;

    /// <inheritdoc/>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new header with each multi-byte field reversed.</returns>
    /// <remarks>All five fields are <c>int16</c> and are reversed independently.</remarks>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">OpenType specification: glyf header</seealso>
    public static GlyphHeader ReverseEndianness(GlyphHeader v) => new()
    {
        NumberOfContours = BinaryPrimitives.ReverseEndianness(v.NumberOfContours),
        XMin = BinaryPrimitives.ReverseEndianness(v.XMin),
        YMin = BinaryPrimitives.ReverseEndianness(v.YMin),
        XMax = BinaryPrimitives.ReverseEndianness(v.XMax),
        YMax = BinaryPrimitives.ReverseEndianness(v.YMax),
    };
}

/// <summary>Context for parsing a glyph. Carries the glyph's declared byte length, from the difference between consecutive <c>loca</c> offsets.</summary>
/// <param name="Length">The number of bytes the glyph occupies.</param>
/// <remarks>
/// <list type="bullet">
/// <item><description>The length is derived from the <c>loca</c> table rather than stored in the glyph itself; the <c>glyf</c> format has no per-glyph length field.</description></item>
/// <item><description>The dispatcher uses the length to bound-check that the glyph header fits before reading it; the derived parsers use it to verify they did not overrun.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Glyph.Parse(ref Cursor, object?)"/>
/// <seealso cref="GlyphBodyContext"/>
/// <seealso cref="LocaTable"/>
public record GlyphContext(long Length);

// ═══════════════════════════════════════════════════════════════════════════
// Component reference
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A single component reference in a composite glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The placement arguments are either an xy offset (when <see cref="ArgsAreXYValues"/> is set) or a pair of point numbers to align.</description></item>
/// <item><description>The affine transform defaults to identity; the three transform flag forms constrain it to a uniform scale, independent x/y scales, or a general 2×2 matrix respectively.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">composite glyph description</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CompositeGlyph"/>
/// <seealso cref="CompositeGlyphFlags"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: composite glyph description</seealso>
public record struct Component
{
    /// <summary>The raw component flags.</summary>
    /// <value>The flag word from the on-disk component record, exposed as a typed bit set.</value>
    /// <seealso cref="CompositeGlyphFlags"/>
    /// <seealso cref="ArgsAreXYValues"/>
    /// <seealso cref="UseMyMetrics"/>
    public CompositeGlyphFlags Flags;

    /// <summary>The glyph ID of the component.</summary>
    /// <value>The ID of the glyph whose outline is incorporated at this component's position.</value>
    /// <seealso cref="Flags"/>
    /// <seealso cref="Argument1"/>
    /// <seealso cref="Argument2"/>
    public ushort GlyphIndex;

    /// <summary>The first argument: an x offset when <see cref="ArgsAreXYValues"/> is set, otherwise a parent point number.</summary>
    /// <value>A signed pixel offset, or an unsigned point index depending on <see cref="ArgsAreXYValues"/>.</value>
    /// <seealso cref="Argument2"/>
    /// <seealso cref="ArgsAreXYValues"/>
    public int Argument1;

    /// <summary>The second argument: a y offset when <see cref="ArgsAreXYValues"/> is set, otherwise a child point number.</summary>
    /// <value>A signed pixel offset, or an unsigned point index depending on <see cref="ArgsAreXYValues"/>.</value>
    /// <seealso cref="Argument1"/>
    /// <seealso cref="ArgsAreXYValues"/>
    public int Argument2;

    /// <summary>The x-scale matrix element (<c>xscale</c>). Identity is 1.</summary>
    /// <value>The value of <c>xscale</c> in the 2×2 transform matrix. <c>1.0</c> when the component is untransformed.</value>
    /// <seealso cref="Scale10"/>
    /// <seealso cref="Scale01"/>
    /// <seealso cref="YScale"/>
    /// <seealso cref="HasTransform"/>
    public double XScale;

    /// <summary>The matrix element <c>scale10</c> (contribution of y to x'). Identity is 0.</summary>
    /// <value>The value of <c>scale10</c> in the 2×2 transform matrix. <c>0.0</c> when the component is untransformed.</value>
    /// <seealso cref="XScale"/>
    /// <seealso cref="Scale01"/>
    /// <seealso cref="YScale"/>
    /// <seealso cref="HasTransform"/>
    public double Scale10;

    /// <summary>The matrix element <c>scale01</c> (contribution of x to y'). Identity is 0.</summary>
    /// <value>The value of <c>scale01</c> in the 2×2 transform matrix. <c>0.0</c> when the component is untransformed.</value>
    /// <seealso cref="XScale"/>
    /// <seealso cref="Scale10"/>
    /// <seealso cref="YScale"/>
    /// <seealso cref="HasTransform"/>
    public double Scale01;

    /// <summary>The y-scale matrix element (<c>yscale</c>). Identity is 1.</summary>
    /// <value>The value of <c>yscale</c> in the 2×2 transform matrix. <c>1.0</c> when the component is untransformed.</value>
    /// <seealso cref="XScale"/>
    /// <seealso cref="Scale10"/>
    /// <seealso cref="Scale01"/>
    /// <seealso cref="HasTransform"/>
    public double YScale;

    /// <summary>True when the arguments are signed xy offsets rather than point numbers.</summary>
    /// <value><see langword="true"/> when <see cref="CompositeGlyphFlags.ArgsAreXYValues"/> is set.</value>
    /// <remarks>Selected by flag bit 1 of <see cref="Flags"/>. When set, <see cref="Argument1"/> and <see cref="Argument2"/> are offsets in font design units; when clear, they are point numbers to align.</remarks>
    /// <seealso cref="Argument1"/>
    /// <seealso cref="Argument2"/>
    /// <seealso cref="CompositeGlyphFlags.ArgsAreXYValues"/>
    public readonly bool ArgsAreXYValues => (Flags & CompositeGlyphFlags.ArgsAreXYValues) != 0;

    /// <summary>True when this component supplies the composite's advance width and side bearings.</summary>
    /// <value><see langword="true"/> when <see cref="CompositeGlyphFlags.UseMyMetrics"/> is set.</value>
    /// <remarks>At most one component in a composite should carry this flag; it designates the component whose metrics are attributed to the whole composite.</remarks>
    /// <seealso cref="CompositeGlyphFlags.UseMyMetrics"/>
    public readonly bool UseMyMetrics => (Flags & CompositeGlyphFlags.UseMyMetrics) != 0;

    /// <summary>True when the offset vector is in the component's coordinate system and is scaled.</summary>
    /// <value><see langword="true"/> when <see cref="CompositeGlyphFlags.ScaledComponentOffset"/> is set.</value>
    /// <remarks>Added in OpenType 1.8.3 as a refinement of the offset interpretation; a renderer that does not know about it treats the flag as absent.</remarks>
    /// <seealso cref="UnscaledComponentOffset"/>
    /// <seealso cref="CompositeGlyphFlags.ScaledComponentOffset"/>
    public readonly bool ScaledComponentOffset => (Flags & CompositeGlyphFlags.ScaledComponentOffset) != 0;

    /// <summary>True when the offset vector is in the parent's coordinate system and is not scaled.</summary>
    /// <value><see langword="true"/> when <see cref="CompositeGlyphFlags.UnscaledComponentOffset"/> is set.</value>
    /// <remarks>Added in OpenType 1.8.3 alongside <see cref="ScaledComponentOffset"/>; the two are mutually exclusive when both are recognised.</remarks>
    /// <seealso cref="ScaledComponentOffset"/>
    /// <seealso cref="CompositeGlyphFlags.UnscaledComponentOffset"/>
    public readonly bool UnscaledComponentOffset => (Flags & CompositeGlyphFlags.UnscaledComponentOffset) != 0;

    /// <summary>True when the component carries a non-identity transform.</summary>
    /// <value><see langword="true"/> when any of the four matrix elements differs from its identity value.</value>
    /// <remarks>Equivalent to checking <see cref="XScale"/> != 1, <see cref="YScale"/> != 1, <see cref="Scale01"/> != 0, or <see cref="Scale10"/> != 0.</remarks>
    /// <seealso cref="XScale"/>
    /// <seealso cref="YScale"/>
    /// <seealso cref="Scale01"/>
    /// <seealso cref="Scale10"/>
    public readonly bool HasTransform =>
        XScale != 1.0 || YScale != 1.0 || Scale01 != 0.0 || Scale10 != 0.0;
}

/// <summary>Flag bits in a composite glyph component record.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The three transform forms — <see cref="WeHaveAScale"/>, <see cref="WeHaveAnXAndYScale"/>, and <see cref="WeHaveATwoByTwo"/> — are mutually exclusive.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">composite glyph description</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Component"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: composite glyph description</seealso>
[Flags]
public enum CompositeGlyphFlags : ushort
{
    /// <summary>Bit 0: arguments are 16-bit; otherwise they are 8-bit.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description"><c>ARG_1_AND_2_ARE_WORDS</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="ArgsAreXYValues"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: <c>ARG_1_AND_2_ARE_WORDS</c></seealso>
    Arg1And2AreWords = 0x0001,

    /// <summary>Bit 1: arguments are signed xy offsets; otherwise they are point numbers.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description"><c>ARGS_ARE_XY_VALUES</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="Arg1And2AreWords"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: <c>ARGS_ARE_XY_VALUES</c></seealso>
    ArgsAreXYValues = 0x0002,

    /// <summary>Bit 2: when <see cref="ArgsAreXYValues"/> is set, round the offset to the grid.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description"><c>ROUND_XY_TO_GRID</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="ArgsAreXYValues"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: <c>ROUND_XY_TO_GRID</c></seealso>
    RoundXYToGrid = 0x0004,

    /// <summary>Bit 3: a single F2DOT14 scale follows.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description"><c>WE_HAVE_A_SCALE</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="WeHaveAnXAndYScale"/>
    /// <seealso cref="WeHaveATwoByTwo"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: <c>WE_HAVE_A_SCALE</c></seealso>
    WeHaveAScale = 0x0008,

    /// <summary>Bit 5: at least one more component follows.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description"><c>MORE_COMPONENTS</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="WeHaveInstructions"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: <c>MORE_COMPONENTS</c></seealso>
    MoreComponents = 0x0020,

    /// <summary>Bit 6: two F2DOT14 values (x and y scales) follow.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description"><c>WE_HAVE_AN_X_AND_Y_SCALE</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="WeHaveAScale"/>
    /// <seealso cref="WeHaveATwoByTwo"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: <c>WE_HAVE_AN_X_AND_Y_SCALE</c></seealso>
    WeHaveAnXAndYScale = 0x0040,

    /// <summary>Bit 7: four F2DOT14 values (a 2×2 matrix) follow.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description"><c>WE_HAVE_A_TWO_BY_TWO</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="WeHaveAScale"/>
    /// <seealso cref="WeHaveAnXAndYScale"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: <c>WE_HAVE_A_TWO_BY_TWO</c></seealso>
    WeHaveATwoByTwo = 0x0080,

    /// <summary>Bit 8: instructions for the composite follow the last component.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description"><c>WE_HAVE_INSTRUCTIONS</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="MoreComponents"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: <c>WE_HAVE_INSTRUCTIONS</c></seealso>
    WeHaveInstructions = 0x0100,

    /// <summary>Bit 9: use the component's advance width and side bearings for the composite.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description"><c>USE_MY_METRICS</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="Component.UseMyMetrics"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: <c>USE_MY_METRICS</c></seealso>
    UseMyMetrics = 0x0200,

    /// <summary>Bit 10: components overlap. When set, it must be set on the first component.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description"><c>OVERLAP_COMPOUND</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="CompositeGlyph.OverlapCompound"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: <c>OVERLAP_COMPOUND</c></seealso>
    OverlapCompound = 0x0400,

    /// <summary>Bit 11: the offset vector is in the component's coordinate system and is scaled.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description"><c>SCALED_COMPONENT_OFFSET</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="UnscaledComponentOffset"/>
    /// <seealso cref="Component.ScaledComponentOffset"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: <c>SCALED_COMPONENT_OFFSET</c></seealso>
    ScaledComponentOffset = 0x0800,

    /// <summary>Bit 12: the offset vector is in the parent's coordinate system and is not scaled.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description"><c>UNSCALED_COMPONENT_OFFSET</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="ScaledComponentOffset"/>
    /// <seealso cref="Component.UnscaledComponentOffset"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: <c>UNSCALED_COMPONENT_OFFSET</c></seealso>
    UnscaledComponentOffset = 0x1000,
}

// ═══════════════════════════════════════════════════════════════════════════
// Contour and point
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A closed contour: a contiguous range of points in the parent <see cref="SimpleGlyph"/>'s point array.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Contours partition the parent's point array into consecutive ranges; contour <c>i</c> starts immediately after contour <c>i-1</c> ends.</description></item>
/// <item><description>The last point of each contour is implicitly connected back to the first to close the contour.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SimpleGlyph"/>
/// <seealso cref="GlyphPoint"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">OpenType specification: simple glyph description</seealso>
public record struct Contour
{
    /// <summary>The index of the first point in the glyph's point array.</summary>
    /// <value>The inclusive starting index of this contour's range within <see cref="SimpleGlyph.Points"/>.</value>
    /// <seealso cref="PointCount"/>
    /// <seealso cref="EndPoint"/>
    public int StartPoint;

    /// <summary>The number of points in the contour.</summary>
    /// <value>The count of consecutive points belonging to this contour.</value>
    /// <seealso cref="StartPoint"/>
    /// <seealso cref="EndPoint"/>
    public int PointCount;

    /// <summary>The inclusive index of the last point in the glyph's point array.</summary>
    /// <value>Equal to <c><see cref="StartPoint"/> + <see cref="PointCount"/> - 1</c>.</value>
    /// <seealso cref="StartPoint"/>
    /// <seealso cref="PointCount"/>
    public readonly int EndPoint => StartPoint + PointCount - 1;
}

/// <summary>A single outline control point, in font design units. The on-disk delta encoding is resolved during parsing, so <see cref="X"/> and <see cref="Y"/> are absolute.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Points are stored in contour order in the parent <see cref="SimpleGlyph"/>; use <see cref="Contour.StartPoint"/> and <see cref="Contour.EndPoint"/> to iterate a single contour.</description></item>
/// <item><description>On-curve points lie on the outline; off-curve points are quadratic Bézier control points. Two consecutive off-curve points imply an on-curve point at their midpoint.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SimpleGlyph"/>
/// <seealso cref="Contour"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">OpenType specification: simple glyph description</seealso>
public record struct GlyphPoint
{
    /// <summary>The x coordinate, in font design units.</summary>
    /// <value>The absolute horizontal coordinate, resolved from the on-disk delta encoding during parse.</value>
    /// <seealso cref="Y"/>
    /// <seealso cref="OnCurve"/>
    public int X;

    /// <summary>The y coordinate, in font design units.</summary>
    /// <value>The absolute vertical coordinate, resolved from the on-disk delta encoding during parse.</value>
    /// <seealso cref="X"/>
    /// <seealso cref="OnCurve"/>
    public int Y;

    /// <summary>True when the point lies on the curve; otherwise it is an off-curve control point.</summary>
    /// <value><see langword="true"/> when <see cref="SimpleGlyphFlags.OnCurvePoint"/> was set for this point in the flags array.</value>
    /// <seealso cref="X"/>
    /// <seealso cref="Y"/>
    /// <seealso cref="SimpleGlyphFlags.OnCurvePoint"/>
    public bool OnCurve;

    /// <inheritdoc/>
    /// <returns>The point formatted as <c>(x, y)</c>, with a trailing <c> off</c> when the point is off-curve.</returns>
    /// <remarks>Useful for debugging and trace output; the format is not part of any interoperability contract.</remarks>
    /// <seealso cref="X"/>
    /// <seealso cref="Y"/>
    /// <seealso cref="OnCurve"/>
    public override readonly string ToString() => $"({X}, {Y}){(OnCurve ? "" : " off")}";
}

// ═══════════════════════════════════════════════════════════════════════════
// Glyph base and derived
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A single glyph description. Either a <see cref="SimpleGlyph"/> (a direct outline) or a <see cref="CompositeGlyph"/> (a reference to other glyphs).</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The dispatcher reads the common <see cref="GlyphHeader"/>, then routes to the concrete type based on the sign of <see cref="GlyphHeader.NumberOfContours"/>. Non-negative values select <see cref="SimpleGlyph"/>; negative values select <see cref="CompositeGlyph"/>.</description></item>
/// <item><description>A glyph with a zero-length <c>loca</c> range is not parsed at all; the loader substitutes <see cref="EmptyGlyph.Instance"/>.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">glyf table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SimpleGlyph"/>
/// <seealso cref="CompositeGlyph"/>
/// <seealso cref="EmptyGlyph"/>
/// <seealso cref="GlyphHeader"/>
/// <seealso cref="GlyphBodyContext"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">OpenType specification: glyf table</seealso>
public abstract record Glyph : IRecord<Glyph>, IBaseRecord<Glyph>
{
    /// <summary>The size of the glyph header in bytes.</summary>
    /// <remarks>Always <c>10</c>. The dispatcher uses this to bound-check that the glyph's declared length can hold a header before reading one.</remarks>
    /// <seealso cref="GlyphHeader"/>
    public const int HeaderSize = 10;

    /// <summary>Gets the minimum x of the glyph's bounding box, in font design units.</summary>
    /// <value>The left edge of the glyph's bounding box, copied from <see cref="GlyphHeader.XMin"/>.</value>
    /// <seealso cref="YMin"/>
    /// <seealso cref="XMax"/>
    /// <seealso cref="YMax"/>
    public short XMin { get; init; }

    /// <summary>Gets the minimum y of the glyph's bounding box, in font design units.</summary>
    /// <value>The bottom edge of the glyph's bounding box, copied from <see cref="GlyphHeader.YMin"/>.</value>
    /// <seealso cref="XMin"/>
    /// <seealso cref="XMax"/>
    /// <seealso cref="YMax"/>
    public short YMin { get; init; }

    /// <summary>Gets the maximum x of the glyph's bounding box, in font design units.</summary>
    /// <value>The right edge of the glyph's bounding box, copied from <see cref="GlyphHeader.XMax"/>.</value>
    /// <seealso cref="XMin"/>
    /// <seealso cref="YMin"/>
    /// <seealso cref="YMax"/>
    public short XMax { get; init; }

    /// <summary>Gets the maximum y of the glyph's bounding box, in font design units.</summary>
    /// <value>The top edge of the glyph's bounding box, copied from <see cref="GlyphHeader.YMax"/>.</value>
    /// <seealso cref="XMin"/>
    /// <seealso cref="YMin"/>
    /// <seealso cref="XMax"/>
    public short YMax { get; init; }

    /// <summary>Gets the decoded hinting program for this glyph. The program is empty when the glyph carries no instructions.</summary>
    /// <value>A <see cref="TrueTypeProgram"/> with zero instructions when the glyph was not hinted; otherwise the decoded program.</value>
    /// <seealso cref="HasInstructions"/>
    /// <seealso cref="TrueTypeProgram"/>
    public abstract TrueTypeProgram Program { get; init; }

    /// <summary>True when the glyph carries hinting instructions.</summary>
    /// <value><see langword="true"/> when <see cref="Program"/> has one or more instructions.</value>
    /// <seealso cref="Program"/>
    public bool HasInstructions => Program.Count > 0;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the glyph header.</param>
    /// <param name="context">A <see cref="GlyphContext"/> carrying the glyph's declared byte length.</param>
    /// <returns>A <see cref="SimpleGlyph"/> or <see cref="CompositeGlyph"/> depending on the header's contour count.</returns>
    /// <exception cref="InvalidDataException">The glyph's declared length cannot hold a header.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>This is a <c>public static</c> method serving as the implementation of <see cref="IRecord{T}.Parse"/>, not an explicit interface implementation — it is callable directly on <see cref="Glyph"/>.</description></item>
    /// <item><description>The dispatcher reads the header, then delegates to the derived parser via <see cref="IBaseRecord{TBase}.Parse{TDerived}(ref Cursor, object?)"/>.</description></item>
    /// <item><description>A glyph with zero bytes in its <c>loca</c> range is not passed through this method at all; the loader substitutes <see cref="EmptyGlyph.Instance"/> directly.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GlyphContext"/>
    /// <seealso cref="GlyphBodyContext"/>
    /// <seealso cref="SimpleGlyph"/>
    /// <seealso cref="CompositeGlyph"/>
    public static Glyph Parse(ref Cursor cursor, object? context)
    {
        GlyphContext ctx = (GlyphContext)context!;

        if (ctx.Length - cursor.Position < HeaderSize)
            throw new InvalidDataException(
                $"Glyph data block of {ctx.Length - cursor.Position} bytes is too short for a header.");

        GlyphHeader header = cursor.ReadBigEndianStruct<GlyphHeader>();
        GlyphBodyContext bodyCtx = new(header, ctx.Length);

        return header.NumberOfContours >= 0
            ? IBaseRecord<Glyph>.Parse<SimpleGlyph>(ref cursor, bodyCtx)
            : IBaseRecord<Glyph>.Parse<CompositeGlyph>(ref cursor, bodyCtx);
    }
}

/// <summary>Context threaded from the dispatcher to each glyph format after the shared <see cref="GlyphHeader"/> has been consumed. Carries the header plus the glyph's declared byte length.</summary>
/// <param name="Header">The common glyph header.</param>
/// <param name="Length">The number of bytes the glyph occupies.</param>
/// <remarks>
/// <list type="bullet">
/// <item><description><see cref="Length"/> is the total glyph extent including the header, not the body length.</description></item>
/// <item><description>The derived parsers use <see cref="Length"/> to verify they did not overrun the glyph's declared range; a mismatch throws <see cref="InvalidDataException"/>.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Glyph.Parse(ref Cursor, object?)"/>
/// <seealso cref="GlyphContext"/>
/// <seealso cref="GlyphHeader"/>
public record GlyphBodyContext(GlyphHeader Header, long Length);

// ═══════════════════════════════════════════════════════════════════════════
// Empty glyph
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A glyph with no outline data at all. Produced when the <c>loca</c> table gives a zero-length range for the glyph — most commonly a space character or another non-marking glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><see cref="Instance"/> is the canonical singleton; the loader substitutes it for every zero-length <c>loca</c> range rather than allocating.</description></item>
/// <item><description><see cref="Program"/> is <c>init</c>-only and public, so <c>Instance with { Program = ... }</c> compiles and returns a fresh instance. Callers that need reference identity should compare against <see cref="Instance"/> directly rather than relying on it surviving a <c>with</c> expression.</description></item>
/// <item><description>The private constructor prevents external instantiation; use <see cref="Instance"/>.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Glyph"/>
/// <seealso cref="GlyfTable"/>
/// <seealso cref="LocaTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">OpenType specification: glyf table</seealso>
public sealed record EmptyGlyph : Glyph
{
    /// <summary>The single shared instance.</summary>
    /// <value>The canonical <see cref="EmptyGlyph"/> substituted by the loader for every zero-length <c>loca</c> range.</value>
    /// <remarks>Because <see cref="Program"/> is <c>init</c>-only, <c>Instance with { ... }</c> returns a new instance and does not mutate the singleton.</remarks>
    /// <seealso cref="Glyph.Program"/>
    /// <seealso cref="GlyfTable.Parse(ref Cursor, object?)"/>
    public static readonly EmptyGlyph Instance = new();

    private EmptyGlyph() { }

    /// <inheritdoc/>
    /// <value>An empty <see cref="TrueTypeProgram"/>. <see cref="EmptyGlyph"/> never carries instructions.</value>
    /// <seealso cref="TrueTypeProgram"/>
    public override TrueTypeProgram Program { get; init; } = new();
}

// ═══════════════════════════════════════════════════════════════════════════
// Simple glyph
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A simple glyph: an outline stored directly as a set of contours and control points.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>On disk, points are stored as deltas with three alternative encodings per axis, selected per point by the flag byte. All three are resolved during parsing, so <see cref="Points"/> always holds absolute coordinates.</description></item>
/// <item><description>A zero-contour simple glyph is legal; it may still carry instructions that operate on phantom points.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">simple glyph description</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Glyph"/>
/// <seealso cref="CompositeGlyph"/>
/// <seealso cref="GlyphPoint"/>
/// <seealso cref="Contour"/>
/// <seealso cref="SimpleGlyphFlags"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">OpenType specification: simple glyph description</seealso>
public sealed record SimpleGlyph : Glyph, IDerivedRecord<Glyph, SimpleGlyph>
{
    /// <summary>Gets the glyph's control points in contour order.</summary>
    /// <value>The full point array, with on-curve and off-curve flags resolved. Contours partition this array into consecutive ranges.</value>
    /// <seealso cref="Contours"/>
    /// <seealso cref="GlyphPoint"/>
    public required GlyphPoint[] Points { get; init; }

    /// <summary>Gets the contours, each a range of indices into <see cref="Points"/>.</summary>
    /// <value>An array of <see cref="Contour"/> entries; contour <c>i</c> starts immediately after contour <c>i-1</c> ends.</value>
    /// <seealso cref="Points"/>
    /// <seealso cref="Contour"/>
    public required Contour[] Contours { get; init; }

    /// <inheritdoc/>
    /// <value>The decoded hinting program, or an empty program when the glyph carries no instructions.</value>
    /// <seealso cref="Glyph.Program"/>
    /// <seealso cref="Glyph.HasInstructions"/>
    public override required TrueTypeProgram Program { get; init; }

    /// <summary>True when the glyph declares that its contours may overlap.</summary>
    /// <value><see langword="true"/> when <see cref="SimpleGlyphFlags.OverlapSimple"/> is set on the first flag byte.</value>
    /// <remarks>The flag may be set on any of the glyph's points, but the specification requires it to be set on the first point when present on any.</remarks>
    /// <seealso cref="SimpleGlyphFlags.OverlapSimple"/>
    public bool OverlapSimple { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the shared <see cref="GlyphHeader"/>.</param>
    /// <param name="context">A <see cref="GlyphBodyContext"/> carrying the header and the glyph's declared length.</param>
    /// <returns>The parsed simple glyph.</returns>
    /// <exception cref="EndOfStreamException">The contour ends, instructions, flags, or deltas extend past the end of the glyph's declared range.</exception>
    /// <exception cref="InvalidDataException">The glyph overran its declared length.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The parser expands the flags array in place, honouring the repeat-flag encoding, before decoding the per-point x and y deltas.</description></item>
    /// <item><description>A glyph with zero contours but a non-empty body still reads an instruction length and program; a glyph with a body of exactly 10 bytes (header only) has neither.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">simple glyph description</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GlyphBodyContext"/>
    /// <seealso cref="SimpleGlyphFlags"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">OpenType specification: simple glyph description</seealso>
    static SimpleGlyph IDerivedRecord<Glyph, SimpleGlyph>.Parse(ref Cursor cursor, object? context)
    {
        GlyphBodyContext ctx = (GlyphBodyContext)context!;
        int numContours = ctx.Header.NumberOfContours;
        long end = ctx.Length;

        // endPtsOfContours: one uint16 per contour. Absent when there are zero contours.
        ushort[] endPts = numContours > 0
            ? cursor.ReadUInt16Array(numContours)
            : [];

        // A zero-contour glyph may still carry instructions (they operate on phantom points).
        // A 10-byte block (header only) has none.
        TrueTypeProgram instructions = new();
        if (numContours > 0 || cursor.Position < end)
        {
            ushort instructionLength = cursor.ReadUInt16();
            if (instructionLength > 0)
                instructions = cursor.ReadRecord<TrueTypeProgram>(new TrueTypeProgramContext(instructionLength));
        }

        int numPoints = numContours == 0 ? 0 : endPts[numContours - 1] + 1;

        bool overlap = false;
        int[] xs = new int[numPoints];
        int[] ys = new int[numPoints];
        GlyphPoint[] points = new GlyphPoint[numPoints];

        if (numPoints > 0)
        {
            // Expand the flags array, honouring REPEAT_FLAG. The stored array is shorter than
            // the logical one whenever consecutive points share the same flag byte.
            Span<byte> flags = numPoints <= 256
                ? stackalloc byte[numPoints]
                : new byte[numPoints];

            int i = 0;
            while (i < numPoints)
            {
                byte flag = cursor.ReadUInt8();
                flags[i++] = flag;
                if ((flag & (byte)SimpleGlyphFlags.RepeatFlag) != 0)
                {
                    byte repeat = cursor.ReadUInt8();
                    for (int r = 0; r < repeat && i < numPoints; r++)
                        flags[i++] = flag;
                }
            }

            overlap = (flags[0] & (byte)SimpleGlyphFlags.OverlapSimple) != 0;

            // x deltas: 1 byte (+/-), 0 bytes (same as previous), or 2-byte signed.
            int x = 0;
            for (int p = 0; p < numPoints; p++)
            {
                byte f = flags[p];
                if ((f & (byte)SimpleGlyphFlags.XShortVector) != 0)
                {
                    int b = cursor.ReadUInt8();
                    x += (f & (byte)SimpleGlyphFlags.XIsSameOrPositiveXShortVector) != 0 ? b : -b;
                }
                else if ((f & (byte)SimpleGlyphFlags.XIsSameOrPositiveXShortVector) == 0)
                {
                    x += cursor.ReadInt16();
                }
                xs[p] = x;
            }

            // y deltas: same three encodings, driven by the y-axis flag bits.
            int y = 0;
            for (int p = 0; p < numPoints; p++)
            {
                byte f = flags[p];
                if ((f & (byte)SimpleGlyphFlags.YShortVector) != 0)
                {
                    int b = cursor.ReadUInt8();
                    y += (f & (byte)SimpleGlyphFlags.YIsSameOrPositiveYShortVector) != 0 ? b : -b;
                }
                else if ((f & (byte)SimpleGlyphFlags.YIsSameOrPositiveYShortVector) == 0)
                {
                    y += cursor.ReadInt16();
                }
                ys[p] = y;
            }

            for (int p = 0; p < numPoints; p++)
            {
                points[p] = new()
                {
                    X = xs[p],
                    Y = ys[p],
                    OnCurve = (flags[p] & (byte)SimpleGlyphFlags.OnCurvePoint) != 0,
                };
            }
        }

        // Build contours from the inclusive end-point indices.
        Contour[] contours = new Contour[numContours];
        int start = 0;
        for (int c = 0; c < numContours; c++)
        {
            int last = endPts[c];
            contours[c] = new() { StartPoint = start, PointCount = last - start + 1 };
            start = last + 1;
        }

        if (cursor.Position > end)
            throw new InvalidDataException(
                $"Simple glyph overran its declared length: parsed to {cursor.Position}, end is {end}.");

        return new()
        {
            XMin = ctx.Header.XMin,
            YMin = ctx.Header.YMin,
            XMax = ctx.Header.XMax,
            YMax = ctx.Header.YMax,
            Points = points,
            Contours = contours,
            Program = instructions,
            OverlapSimple = overlap,
        };
    }
}

/// <summary>Flag bits in the per-point <c>flags</c> array of a simple glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The x-delta and y-delta bits are independent; the encoding for each axis is selected per point.</description></item>
/// <item><description><see cref="RepeatFlag"/> applies to the whole flag byte: the following byte gives the number of additional points that reuse the same flags, so a run of identical flags is stored once.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">simple glyph description</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SimpleGlyph"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">OpenType specification: simple glyph description</seealso>
[Flags]
public enum SimpleGlyphFlags : byte
{
    /// <summary>Bit 0: the point is on the curve; otherwise it is an off-curve control point.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description"><c>ON_CURVE_POINT</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphPoint.OnCurve"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">OpenType specification: <c>ON_CURVE_POINT</c></seealso>
    OnCurvePoint = 0x01,

    /// <summary>Bit 1: the x-coordinate delta is one unsigned byte (sign from bit 4); otherwise its size depends on bit 4.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description"><c>X_SHORT_VECTOR</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="XIsSameOrPositiveXShortVector"/>
    /// <seealso cref="YShortVector"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">OpenType specification: <c>X_SHORT_VECTOR</c></seealso>
    XShortVector = 0x02,

    /// <summary>Bit 2: the y-coordinate delta is one unsigned byte (sign from bit 5); otherwise its size depends on bit 5.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description"><c>Y_SHORT_VECTOR</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="YIsSameOrPositiveYShortVector"/>
    /// <seealso cref="XShortVector"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">OpenType specification: <c>Y_SHORT_VECTOR</c></seealso>
    YShortVector = 0x04,

    /// <summary>Bit 3: the next byte gives the number of additional times this flag byte repeats.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description"><c>REPEAT_FLAG</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="SimpleGlyph"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">OpenType specification: <c>REPEAT_FLAG</c></seealso>
    RepeatFlag = 0x08,

    /// <summary>Bit 4. When <see cref="XShortVector"/> is set, this is the sign of the one-byte delta (set = positive). When it is clear, a set bit means the x delta is zero and no x element is present; a clear bit means the delta is a signed 16-bit value.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description"><c>X_IS_SAME_OR_POSITIVE_X_SHORT_VECTOR</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="XShortVector"/>
    /// <seealso cref="YIsSameOrPositiveYShortVector"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">OpenType specification: <c>X_IS_SAME_OR_POSITIVE_X_SHORT_VECTOR</c></seealso>
    XIsSameOrPositiveXShortVector = 0x10,

    /// <summary>Bit 5. The y-axis counterpart of <see cref="XIsSameOrPositiveXShortVector"/>.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description"><c>Y_IS_SAME_OR_POSITIVE_Y_SHORT_VECTOR</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="YShortVector"/>
    /// <seealso cref="XIsSameOrPositiveXShortVector"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">OpenType specification: <c>Y_IS_SAME_OR_POSITIVE_Y_SHORT_VECTOR</c></seealso>
    YIsSameOrPositiveYShortVector = 0x20,

    /// <summary>Bit 6: contours may overlap. When set, it must be set on the first flag byte.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description"><c>OVERLAP_SIMPLE</c> flag</see> in the OpenType specification.</remarks>
    /// <seealso cref="SimpleGlyph.OverlapSimple"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">OpenType specification: <c>OVERLAP_SIMPLE</c></seealso>
    OverlapSimple = 0x40,

    /// <summary>Bit 7: reserved; must be zero.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">simple glyph description</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description">OpenType specification: simple glyph description</seealso>
    Reserved = 0x80,
}

// ═══════════════════════════════════════════════════════════════════════════
// Composite glyph
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A composite glyph: an outline assembled from one or more other glyphs, each optionally offset and transformed.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Components are incorporated in array order, so a later component is drawn over an earlier one.</description></item>
/// <item><description>Nested composites are permitted by the format, but the specification requires that the component graph be acyclic.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">composite glyph description</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Glyph"/>
/// <seealso cref="SimpleGlyph"/>
/// <seealso cref="Component"/>
/// <seealso cref="CompositeGlyphFlags"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: composite glyph description</seealso>
public sealed record CompositeGlyph : Glyph, IDerivedRecord<Glyph, CompositeGlyph>
{
    /// <summary>Gets the component references, in incorporation order.</summary>
    /// <value>The array of <see cref="Component"/> entries; each names a glyph ID, a placement, and an optional transform.</value>
    /// <seealso cref="Component"/>
    /// <seealso cref="OverlapCompound"/>
    public required Component[] Components { get; init; }

    /// <inheritdoc/>
    /// <value>The decoded hinting program, or an empty program when the composite carries no instructions.</value>
    /// <seealso cref="Glyph.Program"/>
    /// <seealso cref="Glyph.HasInstructions"/>
    public override required TrueTypeProgram Program { get; init; }

    /// <summary>True when the glyph declares that its components may overlap.</summary>
    /// <value><see langword="true"/> when <see cref="CompositeGlyphFlags.OverlapCompound"/> is set on any component.</value>
    /// <remarks>The specification requires the flag to be set on the first component when present on any; the parser ORs the flag across all components for robustness.</remarks>
    /// <seealso cref="CompositeGlyphFlags.OverlapCompound"/>
    public bool OverlapCompound { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the shared <see cref="GlyphHeader"/>.</param>
    /// <param name="context">A <see cref="GlyphBodyContext"/> carrying the header and the glyph's declared length.</param>
    /// <returns>The parsed composite glyph.</returns>
    /// <exception cref="EndOfStreamException">The component array or instructions extend past the end of the glyph's declared range.</exception>
    /// <exception cref="InvalidDataException">The glyph overran its declared length.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The component loop terminates when a component is read without the <see cref="CompositeGlyphFlags.MoreComponents"/> flag.</description></item>
    /// <item><description>Component arguments are interpreted as signed or unsigned according to the <see cref="CompositeGlyphFlags.ArgsAreXYValues"/> flag, not according to their width.</description></item>
    /// <item><description>The <see cref="CompositeGlyphFlags.WeHaveInstructions"/> flag may be set on any component; the parser ORs the flags across all components to decide whether to read the composite's hinting program.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">composite glyph description</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GlyphBodyContext"/>
    /// <seealso cref="Component"/>
    /// <seealso cref="CompositeGlyphFlags"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description">OpenType specification: composite glyph description</seealso>
    static CompositeGlyph IDerivedRecord<Glyph, CompositeGlyph>.Parse(ref Cursor cursor, object? context)
    {
        GlyphBodyContext ctx = (GlyphBodyContext)context!;
        long end = ctx.Length;

        List<Component> components = new(4);
        CompositeGlyphFlags allFlags = 0;

        while (true)
        {
            CompositeGlyphFlags flags = (CompositeGlyphFlags)cursor.ReadUInt16();
            ushort glyphIndex = cursor.ReadUInt16();

            // Arguments are xy offsets or point numbers, and 8 or 16 bits wide. The signedness
            // follows the interpretation, not the width.
            int arg1, arg2;
            if ((flags & CompositeGlyphFlags.Arg1And2AreWords) != 0)
            {
                arg1 = (flags & CompositeGlyphFlags.ArgsAreXYValues) != 0
                    ? cursor.ReadInt16() : cursor.ReadUInt16();
                arg2 = (flags & CompositeGlyphFlags.ArgsAreXYValues) != 0
                    ? cursor.ReadInt16() : cursor.ReadUInt16();
            }
            else
            {
                arg1 = (flags & CompositeGlyphFlags.ArgsAreXYValues) != 0
                    ? cursor.ReadInt8() : cursor.ReadUInt8();
                arg2 = (flags & CompositeGlyphFlags.ArgsAreXYValues) != 0
                    ? cursor.ReadInt8() : cursor.ReadUInt8();
            }

            // At most one of the three transform forms is set.
            double xScale = 1.0, scale10 = 0.0, scale01 = 0.0, yScale = 1.0;
            if ((flags & CompositeGlyphFlags.WeHaveAScale) != 0)
            {
                xScale = yScale = cursor.ReadF2Dot14();
            }
            else if ((flags & CompositeGlyphFlags.WeHaveAnXAndYScale) != 0)
            {
                xScale = cursor.ReadF2Dot14();
                yScale = cursor.ReadF2Dot14();
            }
            else if ((flags & CompositeGlyphFlags.WeHaveATwoByTwo) != 0)
            {
                xScale = cursor.ReadF2Dot14();
                scale01 = cursor.ReadF2Dot14();
                scale10 = cursor.ReadF2Dot14();
                yScale = cursor.ReadF2Dot14();
            }

            components.Add(new Component
            {
                Flags = flags,
                GlyphIndex = glyphIndex,
                Argument1 = arg1,
                Argument2 = arg2,
                XScale = xScale,
                Scale01 = scale01,
                Scale10 = scale10,
                YScale = yScale,
            });

            allFlags |= flags;
            if ((flags & CompositeGlyphFlags.MoreComponents) == 0) break;
        }

        // The composite's own instructions follow the last component. The flag may be set on
        // any component; OR-ing across all of them covers every spelling.
        TrueTypeProgram instructions = new();
        if ((allFlags & CompositeGlyphFlags.WeHaveInstructions) != 0 && cursor.Position < end)
        {
            ushort instructionLength = cursor.ReadUInt16();
            if (instructionLength > 0)
                instructions = cursor.ReadRecord<TrueTypeProgram>(new TrueTypeProgramContext(instructionLength));
        }

        if (cursor.Position > end)
            throw new InvalidDataException(
                $"Composite glyph overran its declared length: parsed to {cursor.Position}, end is {end}.");

        return new()
        {
            XMin = ctx.Header.XMin,
            YMin = ctx.Header.YMin,
            XMax = ctx.Header.XMax,
            YMax = ctx.Header.YMax,
            Components = [.. components],
            Program = instructions,
            OverlapCompound = (allFlags & CompositeGlyphFlags.OverlapCompound) != 0,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// glyf table
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The <c>glyf</c> table: TrueType glyph outline data. Holds one <see cref="Glyph"/> per glyph, indexed by glyph ID.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table has no internal header or index of its own; glyph data blocks are located through the <c>loca</c> table and their lengths inferred from consecutive offsets. The parser walks <c>loca</c> in glyph ID order, which is the order the blocks are stored in.</description></item>
/// <item><description>Every glyph is materialized during parse. This means the full outline data — points, contours, components, and instructions — is decoded up front and no <see cref="Source"/> reference is retained. For a large font this is the dominant memory cost of the loaded table; it is bounded by the size of the <c>glyf</c> table itself, scaled by the ratio of decoded to packed representation.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">glyf table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LocaTable"/>
/// <seealso cref="Glyph"/>
/// <seealso cref="SimpleGlyph"/>
/// <seealso cref="CompositeGlyph"/>
/// <seealso cref="EmptyGlyph"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">OpenType specification: glyf table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/loca">OpenType specification: loca table</seealso>
public sealed record GlyfTable : IOpenTypeTable<GlyfTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IOpenTypeTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">OpenType specification: glyf table</seealso>
    public static Tag Tag => "glyf";

    /// <summary>Gets the glyphs, indexed by glyph ID. Length equals <c>maxp.numGlyphs</c>. Glyphs with no outline data are <see cref="EmptyGlyph.Instance"/>.</summary>
    /// <value>An array of <see cref="Glyph"/> entries in glyph ID order. The length matches the glyph count declared by <c>maxp</c> and the <c>loca</c> table.</value>
    /// <remarks>Zero-length <c>loca</c> ranges produce <see cref="EmptyGlyph.Instance"/>; no per-glyph allocation occurs for those.</remarks>
    /// <seealso cref="this[int]"/>
    /// <seealso cref="Count"/>
    /// <seealso cref="EmptyGlyph"/>
    /// <seealso cref="LocaTable"/>
    public required Glyph[] Glyphs { get; init; }

    /// <summary>Gets the number of glyphs.</summary>
    /// <value>The length of the <see cref="Glyphs"/> array.</value>
    /// <seealso cref="Glyphs"/>
    /// <seealso cref="this[int]"/>
    public int Count => Glyphs.Length;

    /// <summary>Gets the glyph at <paramref name="glyphId"/>.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <returns>The <see cref="Glyph"/> at the given index.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is out of range.</exception>
    /// <seealso cref="Glyphs"/>
    /// <seealso cref="Count"/>
    public Glyph this[int glyphId]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, Glyphs.Length);
            return Glyphs[glyphId];
        }
    }

    /// <summary>Returns the depth of the composite glyph graph rooted at <paramref name="glyphId"/>. A simple glyph has depth 0; a composite has depth 1 plus the deepest depth among its components.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <returns>The depth of the deepest composite nesting under <paramref name="glyphId"/>, or <c>0</c> for a simple or empty glyph.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is out of range.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The traversal is a plain recursion with no memoization; a shared component subtree is re-walked once per reference to it.</description></item>
    /// <item><description>The depth is bounded internally by the glyph count. A malformed font with a cyclic component graph is not rejected — the recursion bails out and returns the current depth rather than throwing.</description></item>
    /// <item><description>This method is intended for diagnostics and dependency analysis, not for hot rendering paths.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CompositeGlyph"/>
    /// <seealso cref="Component"/>
    public int GetComponentDepth(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, Glyphs.Length);
        return ComponentDepth(glyphId, 0);
    }

    private int ComponentDepth(int glyphId, int depth)
    {
        // A composite's depth is 1 plus the deepest depth among its components. The bound on
        // recursion comes from the acyclicity requirement; a depth exceeding the glyph count
        // can only arise from a cycle.
        if (depth > Glyphs.Length) return depth;
        if (Glyphs[glyphId] is not CompositeGlyph composite) return depth;

        int best = depth;
        foreach (Component component in composite.Components)
            best = Math.Max(best, ComponentDepth(component.GlyphIndex, depth + 1));
        return best;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the glyf table.</param>
    /// <param name="context">A <see cref="FontFace"/> whose <c>loca</c> table and table-directory length are used to bound the glyph reads.</param>
    /// <returns>The parsed glyf table with every glyph materialized.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="FontFace"/>.</exception>
    /// <exception cref="InvalidDataException">A glyph's <c>loca</c> range has <c>end &lt; start</c>, or extends past the <c>glyf</c> table length.</exception>
    /// <exception cref="EndOfStreamException">A glyph body extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The parse is eager: every glyph in the <c>loca</c> range is walked and its body decoded before this method returns.</description></item>
    /// <item><description>Zero-length <c>loca</c> ranges produce <see cref="EmptyGlyph.Instance"/>; the source is not consulted for those entries.</description></item>
    /// <item><description>Both the <c>loca</c> table and the <c>glyf</c> table length are resolved through <paramref name="context"/>; the glyph offsets are validated against the latter before any read is attempted.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">glyf table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LocaTable"/>
    /// <seealso cref="Glyph.Parse(ref Cursor, object?)"/>
    /// <seealso cref="FontFace.GetTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">OpenType specification: glyf table</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/loca">OpenType specification: loca table</seealso>
    public static GlyfTable Parse(ref Cursor cursor, object? context)
    {
        if (context is not FontFace face)
            throw new InvalidOperationException(
                $"{nameof(GlyfTable)}.Parse requires a {nameof(FontFace)} context.");

        int glyfLength = face.GetTableLength(Tag);
        LocaTable loca = face.GetTable<LocaTable>();
        Source source = cursor.Source;

        int numGlyphs = loca.NumGlyphs;
        Glyph[] glyphs = new Glyph[numGlyphs];

        for (int gid = 0; gid < numGlyphs; gid++)
        {
            uint start = loca.Offsets[gid];
            uint end = loca.Offsets[gid + 1];

            if (end < start)
                throw new InvalidDataException(
                    $"'glyf' glyph {gid} has loca range [{start}, {end}) with end < start.");
            if (end > glyfLength)
                throw new InvalidDataException(
                    $"'glyf' glyph {gid} extends to offset {end}, beyond table length {glyfLength}.");

            if (start == end)
            {
                glyphs[gid] = EmptyGlyph.Instance;
                continue;
            }

            glyphs[gid] = source.ParseRecordAt<Glyph>(start, new GlyphContext(end - start));
        }

        return new() { Glyphs = glyphs };
    }
}

/// <summary>Format of the <c>glyf</c> table, as specified by <c>head.glyphDataFormat</c>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Only one format has ever been defined; the field exists in <c>head</c> for forward compatibility.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head"><c>glyphDataFormat</c> field</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyfTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head">OpenType specification: head table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">OpenType specification: glyf table</seealso>
public enum GlyphDataFormat : short
{
    /// <summary>The only format defined; current format.</summary>
    /// <remarks>Value <c>0</c>. Any other value would indicate an unrecognised format.</remarks>
    /// <seealso cref="GlyfTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head">OpenType specification: head table</seealso>
    Current = 0,
}
