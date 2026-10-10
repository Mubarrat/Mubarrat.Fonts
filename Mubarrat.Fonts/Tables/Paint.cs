using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>Base class for all COLR version 1 paint nodes. The dispatcher reads the format byte and routes to the matching concrete subclass.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>All offset fields within a paint record are relative to that record's own start, not to the COLR table or to the enclosing paint.</description></item>
/// <item><description>Paints form a directed acyclic graph; a paint's children are reached by offsets that resolve to other paints in the same graph.</description></item>
/// <item><description>Formats 1 through 32 are defined. The odd-numbered pairs (2/3, 4/5, 6/7, …, 30/31) are the non-variable and variable variants of the same visual operation.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 paint tables</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PaintColrLayers"/>
/// <seealso cref="PaintSolid"/>
/// <seealso cref="PaintLinearGradient"/>
/// <seealso cref="PaintGlyph"/>
/// <seealso cref="PaintComposite"/>
/// <seealso cref="ColorLine"/>
/// <seealso cref="VarColorLine"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 paint tables</seealso>
public abstract record Paint : IRecord<Paint>, IBaseRecord<Paint>
{
    /// <summary>Gets the paint format number (1–32).</summary>
    /// <value>The format discriminant from the first byte of the paint record.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 paint tables</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 paint tables</seealso>
    public byte Format { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the format byte of the paint.</param>
    /// <param name="context">Unused. Paint records are self-describing once the format byte is read.</param>
    /// <returns>The format-specific paint record.</returns>
    /// <exception cref="InvalidDataException">The format byte is not in the range 1–32.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 paint tables</see> in the OpenType specification.</remarks>
    /// <seealso cref="IBaseRecord{TBase}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 paint tables</seealso>
    static Paint IRecord<Paint>.Parse(ref Cursor cursor, object? context)
    {
        byte format = cursor.ReadUInt8();
        return format switch
        {
            1 => IBaseRecord<Paint>.Parse<PaintColrLayers>(ref cursor),
            2 => IBaseRecord<Paint>.Parse<PaintSolid>(ref cursor),
            3 => IBaseRecord<Paint>.Parse<PaintVarSolid>(ref cursor),
            4 => IBaseRecord<Paint>.Parse<PaintLinearGradient>(ref cursor),
            5 => IBaseRecord<Paint>.Parse<PaintVarLinearGradient>(ref cursor),
            6 => IBaseRecord<Paint>.Parse<PaintRadialGradient>(ref cursor),
            7 => IBaseRecord<Paint>.Parse<PaintVarRadialGradient>(ref cursor),
            8 => IBaseRecord<Paint>.Parse<PaintSweepGradient>(ref cursor),
            9 => IBaseRecord<Paint>.Parse<PaintVarSweepGradient>(ref cursor),
            10 => IBaseRecord<Paint>.Parse<PaintGlyph>(ref cursor),
            11 => IBaseRecord<Paint>.Parse<PaintColrGlyph>(ref cursor),
            12 => IBaseRecord<Paint>.Parse<PaintTransform>(ref cursor),
            13 => IBaseRecord<Paint>.Parse<PaintVarTransform>(ref cursor),
            14 => IBaseRecord<Paint>.Parse<PaintTranslate>(ref cursor),
            15 => IBaseRecord<Paint>.Parse<PaintVarTranslate>(ref cursor),
            16 => IBaseRecord<Paint>.Parse<PaintScale>(ref cursor),
            17 => IBaseRecord<Paint>.Parse<PaintVarScale>(ref cursor),
            18 => IBaseRecord<Paint>.Parse<PaintScaleAroundCenter>(ref cursor),
            19 => IBaseRecord<Paint>.Parse<PaintVarScaleAroundCenter>(ref cursor),
            20 => IBaseRecord<Paint>.Parse<PaintScaleUniform>(ref cursor),
            21 => IBaseRecord<Paint>.Parse<PaintVarScaleUniform>(ref cursor),
            22 => IBaseRecord<Paint>.Parse<PaintScaleUniformAroundCenter>(ref cursor),
            23 => IBaseRecord<Paint>.Parse<PaintVarScaleUniformAroundCenter>(ref cursor),
            24 => IBaseRecord<Paint>.Parse<PaintRotate>(ref cursor),
            25 => IBaseRecord<Paint>.Parse<PaintVarRotate>(ref cursor),
            26 => IBaseRecord<Paint>.Parse<PaintRotateAroundCenter>(ref cursor),
            27 => IBaseRecord<Paint>.Parse<PaintVarRotateAroundCenter>(ref cursor),
            28 => IBaseRecord<Paint>.Parse<PaintSkew>(ref cursor),
            29 => IBaseRecord<Paint>.Parse<PaintVarSkew>(ref cursor),
            30 => IBaseRecord<Paint>.Parse<PaintSkewAroundCenter>(ref cursor),
            31 => IBaseRecord<Paint>.Parse<PaintVarSkewAroundCenter>(ref cursor),
            32 => IBaseRecord<Paint>.Parse<PaintComposite>(ref cursor),
            _ => throw new InvalidDataException($"COLR paint format {format} is not defined."),
        };
    }
}

/// <summary>Wraps a value type with a variation index base, marking it as variable. Blittable, size <c>sizeof(T) + 4</c>.</summary>
/// <typeparam name="T">The wrapped value type. Must itself be a big-endian struct so the wrapper can reverse it.</typeparam>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <c>varIndexBase</c> field is the starting index into the item variation store for the value's variation deltas.</description></item>
/// <item><description>Used by the variable variants of the COLR v1 paint records, and by COLR clip boxes of format 2.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 paint tables</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Affine2x3"/>
/// <seealso cref="ColorStop"/>
/// <seealso cref="IEndianReversibleStruct{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: COLR v1 paint tables</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct Var<T> : IEndianReversibleStruct<Var<T>> where T : unmanaged, IEndianReversibleStruct<T>
{
    /// <summary>The wrapped value.</summary>
    /// <value>The base value before variation deltas are applied.</value>
    /// <seealso cref="VarIndexBase"/>
    public T Value;

    /// <summary>The variation index base.</summary>
    /// <value>The starting index into the item variation store for this value's deltas.</value>
    /// <seealso cref="Value"/>
    public uint VarIndexBase;

    /// <inheritdoc/>
    /// <param name="v">The value whose fields are to be reversed.</param>
    /// <returns>A new wrapper with the wrapped value and the variation index base reversed.</returns>
    /// <remarks>The wrapped value is reversed through its own <see cref="IEndianReversibleStruct{T}.ReverseEndianness(T)"/> implementation.</remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    public static Var<T> ReverseEndianness(Var<T> v) => new()
    {
        Value = T.ReverseEndianness(v.Value),
        VarIndexBase = BinaryPrimitives.ReverseEndianness(v.VarIndexBase),
    };
}

/// <summary>An affine 2×3 transformation matrix. Blittable, size 24.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The matrix is stored as six <see cref="Fixed"/> values: four for the 2×2 linear part and two for the translation.</description></item>
/// <item><description>Applied to a point <c>(x, y)</c> as <c>(x', y') = (Xx·x + Yx·y + Dx, Xy·x + Yy·y + Dy)</c>, matching the column-major convention used by the COLR specification.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>Affine2x3</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Var{T}"/>
/// <seealso cref="PaintTransform"/>
/// <seealso cref="PaintVarTransform"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>Affine2x3</c></seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct Affine2x3 : IEndianReversibleStruct<Affine2x3>
{
    /// <summary>Matrix element xx.</summary>
    /// <value>The 2×2 matrix element in row 1, column 1.</value>
    /// <seealso cref="Yx"/>
    /// <seealso cref="Xy"/>
    /// <seealso cref="Yy"/>
    public Fixed Xx;

    /// <summary>Matrix element yx.</summary>
    /// <value>The 2×2 matrix element in row 1, column 2.</value>
    /// <seealso cref="Xx"/>
    /// <seealso cref="Xy"/>
    /// <seealso cref="Yy"/>
    public Fixed Yx;

    /// <summary>Matrix element xy.</summary>
    /// <value>The 2×2 matrix element in row 2, column 1.</value>
    /// <seealso cref="Xx"/>
    /// <seealso cref="Yx"/>
    /// <seealso cref="Yy"/>
    public Fixed Xy;

    /// <summary>Matrix element yy.</summary>
    /// <value>The 2×2 matrix element in row 2, column 2.</value>
    /// <seealso cref="Xx"/>
    /// <seealso cref="Yx"/>
    /// <seealso cref="Xy"/>
    public Fixed Yy;

    /// <summary>Translation dx.</summary>
    /// <value>The x component of the translation vector.</value>
    /// <seealso cref="Dy"/>
    public Fixed Dx;

    /// <summary>Translation dy.</summary>
    /// <value>The y component of the translation vector.</value>
    /// <seealso cref="Dx"/>
    public Fixed Dy;

    /// <inheritdoc/>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new matrix with each <see cref="Fixed"/> element reversed.</returns>
    /// <remarks>All six fields are <see cref="Fixed"/> values and are reversed independently.</remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    public static Affine2x3 ReverseEndianness(Affine2x3 v) => new()
    {
        Xx = Fixed.ReverseEndianness(v.Xx),
        Yx = Fixed.ReverseEndianness(v.Yx),
        Xy = Fixed.ReverseEndianness(v.Xy),
        Yy = Fixed.ReverseEndianness(v.Yy),
        Dx = Fixed.ReverseEndianness(v.Dx),
        Dy = Fixed.ReverseEndianness(v.Dy),
    };
}

/// <summary>A color stop in a gradient. Blittable, size 6.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each stop pairs a position along the gradient axis with a CPAL palette entry and an alpha multiplier.</description></item>
/// <item><description>Stops within a <see cref="ColorLine"/> must be ordered by ascending <see cref="StopOffset"/>.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>ColorStop</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Var{T}"/>
/// <seealso cref="ColorLine"/>
/// <seealso cref="VarColorLine"/>
/// <seealso cref="CpalTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>ColorStop</c></seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct ColorStop : IEndianReversibleStruct<ColorStop>
{
    /// <summary>Position along the gradient.</summary>
    /// <value>The stop's location on the gradient axis as an F2DOT14 value.</value>
    /// <seealso cref="PaletteIndex"/>
    /// <seealso cref="Alpha"/>
    public F2Dot14 StopOffset;

    /// <summary>CPAL palette entry index.</summary>
    /// <value>The index into the CPAL palette that supplies the stop's RGB colour.</value>
    /// <seealso cref="StopOffset"/>
    /// <seealso cref="Alpha"/>
    /// <seealso cref="CpalTable"/>
    public ushort PaletteIndex;

    /// <summary>Alpha multiplier.</summary>
    /// <value>The stop's alpha as an F2DOT14 value; the CPAL entry's own alpha is multiplied by this.</value>
    /// <seealso cref="StopOffset"/>
    /// <seealso cref="PaletteIndex"/>
    public F2Dot14 Alpha;

    /// <inheritdoc/>
    /// <param name="v">The value whose fields are to be reversed.</param>
    /// <returns>A new stop with each multi-byte field reversed.</returns>
    /// <remarks>Both <see cref="F2Dot14"/> fields and the <c>uint16</c> palette index are reversed independently.</remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    public static ColorStop ReverseEndianness(ColorStop v) => new()
    {
        StopOffset = F2Dot14.ReverseEndianness(v.StopOffset),
        PaletteIndex = BinaryPrimitives.ReverseEndianness(v.PaletteIndex),
        Alpha = F2Dot14.ReverseEndianness(v.Alpha),
    };
}

/// <summary>Extend mode for <see cref="ColorLine"/> and <see cref="VarColorLine"/>. Blittable, size 1.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Determines how the gradient is continued outside the <c>[0, 1]</c> stop-offset range.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>Extend</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ColorLine.Extend"/>
/// <seealso cref="VarColorLine.Extend"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>Extend</c></seealso>
public enum Extend : byte
{
    /// <summary>Pad: use the colour of the nearest endpoint.</summary>
    Pad = 0,

    /// <summary>Repeat: restart the gradient from the other end.</summary>
    Repeat = 1,

    /// <summary>Reflect: mirror the gradient back and forth.</summary>
    Reflect = 2,
}

/// <summary>Composite mode for <see cref="PaintComposite"/>. Blittable, size 1.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The values 0–12 are Porter-Duff operators; values 13–27 are blend modes drawn from the W3C compositing specification.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>CompositeMode</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PaintComposite.CompositeMode"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>CompositeMode</c></seealso>
public enum CompositeMode : byte
{
    /// <summary>Clear: both source and destination are cleared.</summary>
    Clear = 0,

    /// <summary>Source: draw the source over a transparent backdrop.</summary>
    Src = 1,

    /// <summary>Destination: keep the destination unchanged.</summary>
    Dest = 2,

    /// <summary>Source-over: the canonical top-layer composite.</summary>
    SrcOver = 3,

    /// <summary>Destination-over: the source is drawn behind the destination.</summary>
    DestOver = 4,

    /// <summary>Source-in: keep the source where the destination is opaque.</summary>
    SrcIn = 5,

    /// <summary>Destination-in: keep the destination where the source is opaque.</summary>
    DestIn = 6,

    /// <summary>Source-out: keep the source where the destination is transparent.</summary>
    SrcOut = 7,

    /// <summary>Destination-out: keep the destination where the source is transparent.</summary>
    DestOut = 8,

    /// <summary>Source-atop: the source is drawn only where the destination is opaque.</summary>
    SrcAtop = 9,

    /// <summary>Destination-atop: the destination is kept only where the source is opaque.</summary>
    DestAtop = 10,

    /// <summary>XOR: keep where exactly one of source and destination is opaque.</summary>
    Xor = 11,

    /// <summary>Plus: additive compositing.</summary>
    Plus = 12,

    /// <summary>Screen blend.</summary>
    Screen = 13,

    /// <summary>Overlay blend.</summary>
    Overlay = 14,

    /// <summary>Darken blend.</summary>
    Darken = 15,

    /// <summary>Lighten blend.</summary>
    Lighten = 16,

    /// <summary>Color-dodge blend.</summary>
    ColorDodge = 17,

    /// <summary>Color-burn blend.</summary>
    ColorBurn = 18,

    /// <summary>Hard-light blend.</summary>
    HardLight = 19,

    /// <summary>Soft-light blend.</summary>
    SoftLight = 20,

    /// <summary>Difference blend.</summary>
    Difference = 21,

    /// <summary>Exclusion blend.</summary>
    Exclusion = 22,

    /// <summary>Multiply blend.</summary>
    Multiply = 23,

    /// <summary>HSL hue blend.</summary>
    HslHue = 24,

    /// <summary>HSL saturation blend.</summary>
    HslSaturation = 25,

    /// <summary>HSL colour blend.</summary>
    HslColor = 26,

    /// <summary>HSL luminosity blend.</summary>
    HslLuminosity = 27,
}

/// <summary>A gradient color line. Blittable stops in ascending offset order with an extend mode.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A color line is a sequence of <see cref="ColorStop"/> entries plus an <see cref="Extend"/> mode that defines how the gradient is continued outside the stop range.</description></item>
/// <item><description>Used by the non-variable gradient paints (formats 4, 6, 8). The variable gradient paints use <see cref="VarColorLine"/> instead.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>ColorLine</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="VarColorLine"/>
/// <seealso cref="ColorStop"/>
/// <seealso cref="Extend"/>
/// <seealso cref="PaintLinearGradient"/>
/// <seealso cref="PaintRadialGradient"/>
/// <seealso cref="PaintSweepGradient"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>ColorLine</c></seealso>
public sealed record ColorLine : IRecord<ColorLine>
{
    /// <summary>Gets the extend mode applied outside the stop-offset range.</summary>
    /// <value>The <see cref="Extend"/> mode that determines how the gradient is continued beyond the first and last stops.</value>
    /// <seealso cref="Extend"/>
    /// <seealso cref="Stops"/>
    public Extend Extend { get; init; }

    /// <summary>Gets the non-variable stops.</summary>
    /// <value>The ordered sequence of <see cref="ColorStop"/> entries; offsets are non-decreasing.</value>
    /// <seealso cref="ColorStop"/>
    /// <seealso cref="Extend"/>
    public IReadOnlyList<ColorStop> Stops { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the color-line header.</param>
    /// <param name="context">Unused. The color line is self-describing.</param>
    /// <returns>The parsed color line.</returns>
    /// <exception cref="EndOfStreamException">The header or stop array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>ColorLine</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="ColorStop"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>ColorLine</c></seealso>
    static ColorLine IRecord<ColorLine>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new ColorLine
        {
            Extend = header.Extend,
            Stops = cursor.ReadBigEndianStructArray<ColorStop>(header.NumStops),
        };
    }

    /// <summary>The 3-byte color-line header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by <c>numStops</c> 6-byte <see cref="ColorStop"/> records.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>ColorLine</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ColorLine"/>
    /// <seealso cref="ColorStop"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>ColorLine</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The extend mode.</summary>
        /// <value>The <see cref="Tables.Extend"/> mode applied outside the stop-offset range.</value>
        /// <seealso cref="Extend"/>
        /// <seealso cref="NumStops"/>
        public Extend Extend;

        /// <summary>The number of color stops that follow.</summary>
        /// <value>The count of <see cref="ColorStop"/> records in the array.</value>
        /// <seealso cref="ColorStop"/>
        /// <seealso cref="Extend"/>
        public ushort NumStops;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with <c>NumStops</c> reversed and the byte-width extend copied through.</returns>
        /// <remarks>The <c>Extend</c> field is a single byte and does not participate in the reversal.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            Extend = value.Extend,
            NumStops = BinaryPrimitives.ReverseEndianness(value.NumStops),
        };
    }
}

/// <summary>A variable gradient color line. Each stop carries a variation index base.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Shares its header layout with <see cref="ColorLine"/>; only the stop record type differs.</description></item>
/// <item><description>Used by the variable gradient paints (formats 5, 7, 9).</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>VarColorLine</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ColorLine"/>
/// <seealso cref="Var{T}"/>
/// <seealso cref="ColorStop"/>
/// <seealso cref="Extend"/>
/// <seealso cref="PaintVarLinearGradient"/>
/// <seealso cref="PaintVarRadialGradient"/>
/// <seealso cref="PaintVarSweepGradient"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>VarColorLine</c></seealso>
public sealed record VarColorLine : IRecord<VarColorLine>
{
    /// <summary>Gets the extend mode applied outside the stop-offset range.</summary>
    /// <value>The <see cref="Extend"/> mode that determines how the gradient is continued beyond the first and last stops.</value>
    /// <seealso cref="Extend"/>
    /// <seealso cref="VarStops"/>
    public Extend Extend { get; init; }

    /// <summary>Gets the variable stops.</summary>
    /// <value>The ordered sequence of <see cref="Var{T}"/>-wrapped <see cref="ColorStop"/> entries; each carries its own variation index base.</value>
    /// <seealso cref="Var{T}"/>
    /// <seealso cref="ColorStop"/>
    /// <seealso cref="Extend"/>
    public IReadOnlyList<Var<ColorStop>> VarStops { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the color-line header.</param>
    /// <param name="context">Unused. The color line is self-describing.</param>
    /// <returns>The parsed variable color line.</returns>
    /// <exception cref="EndOfStreamException">The header or stop array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>VarColorLine</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="ColorLine.Header"/>
    /// <seealso cref="Var{T}"/>
    /// <seealso cref="ColorStop"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>VarColorLine</c></seealso>
    static VarColorLine IRecord<VarColorLine>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<ColorLine.Header>();
        return new VarColorLine
        {
            Extend = header.Extend,
            VarStops = cursor.ReadBigEndianStructArray<Var<ColorStop>>(header.NumStops),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 1 — layer stack
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 1: layer stack referencing the LayerList.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Draws a contiguous range of the enclosing COLR v1 layer list, in order. Each referenced layer's root paint is drawn over the previous one.</description></item>
/// <item><description>This is the bridge between the v0 layered model and the v1 paint graph; a v1-only font may still use it to compose glyphs from reused sub-paints.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintColrLayers</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="LayerList"/>
/// <seealso cref="PaintSolid"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintColrLayers</c></seealso>
public sealed record PaintColrLayers : Paint, IEndianReversibleHeaderDerivedRecord<Paint, PaintColrLayers, PaintColrLayers.Header>
{
    /// <summary>Gets the number of layers.</summary>
    /// <value>The count of consecutive layer-list entries drawn by this paint.</value>
    /// <seealso cref="FirstLayerIndex"/>
    /// <seealso cref="LayerList"/>
    public byte NumLayers { get; init; }

    /// <summary>Gets the index into the LayerList of the first layer.</summary>
    /// <value>The zero-based index into the enclosing <see cref="LayerList"/> at which the drawn range begins.</value>
    /// <seealso cref="NumLayers"/>
    /// <seealso cref="LayerList"/>
    public uint FirstLayerIndex { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read header.</param>
    /// <param name="context">Unused.</param>
    /// <returns>A new instance populated from the header.</returns>
    /// <remarks>The paint carries no sub-graph references; the referenced layers are drawn in the order they appear in the enclosing <see cref="LayerList"/>.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="LayerList"/>
    static PaintColrLayers IHeaderDerivedRecord<Paint, PaintColrLayers, Header>.FromHeader(in Header header, object? context) => new()
    {
        Format = 1,
        NumLayers = header.NumLayers,
        FirstLayerIndex = header.FirstLayerIndex,
    };

    /// <summary>The 5-byte <c>PaintColrLayers</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Followed immediately by no additional data; the layer references are implicit in <see cref="NumLayers"/> and <see cref="FirstLayerIndex"/>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintColrLayers</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintColrLayers"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintColrLayers</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Number of layers.</summary>
        /// <value>The count of consecutive layer-list entries drawn by this paint.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>numLayers</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="FirstLayerIndex"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>numLayers</c></seealso>
        public byte NumLayers;

        /// <summary>Index into the LayerList of the first layer.</summary>
        /// <value>The zero-based index into the enclosing <see cref="LayerList"/> at which the drawn range begins.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>firstLayerIndex</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="NumLayers"/>
        /// <seealso cref="LayerList"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>firstLayerIndex</c></seealso>
        public uint FirstLayerIndex;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with <c>FirstLayerIndex</c> reversed and the byte-width layer count copied through.</returns>
        /// <remarks>The <c>NumLayers</c> field is a single byte and does not participate in the reversal.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            NumLayers = value.NumLayers,
            FirstLayerIndex = BinaryPrimitives.ReverseEndianness(value.FirstLayerIndex),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 2 — solid fill
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 2: solid fill.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Fills the current clip region with a single colour from the CPAL palette.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintSolid</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintVarSolid"/>
/// <seealso cref="CpalTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintSolid</c></seealso>
public sealed record PaintSolid : Paint, IEndianReversibleHeaderDerivedRecord<Paint, PaintSolid, PaintSolid.Header>
{
    /// <summary>Gets the CPAL palette index, or <c>0xFFFF</c> for the text foreground colour.</summary>
    /// <value>The zero-based palette entry index, or <c>0xFFFF</c> to use the text foreground colour.</value>
    /// <seealso cref="Alpha"/>
    /// <seealso cref="CpalTable"/>
    public ushort PaletteIndex { get; init; }

    /// <summary>Gets the alpha multiplier.</summary>
    /// <value>The alpha to apply, as an F2DOT14 value; multiplied with the palette entry's own alpha.</value>
    /// <seealso cref="PaletteIndex"/>
    public F2Dot14 Alpha { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read header.</param>
    /// <param name="context">Unused.</param>
    /// <returns>A new instance populated from the header.</returns>
    /// <seealso cref="Header"/>
    static PaintSolid IHeaderDerivedRecord<Paint, PaintSolid, Header>.FromHeader(in Header header, object? context) => new()
    {
        Format = 2,
        PaletteIndex = header.PaletteIndex,
        Alpha = header.Alpha,
    };

    /// <summary>The 4-byte <c>PaintSolid</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintSolid</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintSolid"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintSolid</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>CPAL palette index. 0xFFFF means foreground color.</summary>
        /// <value>The zero-based palette entry index, or <c>0xFFFF</c> to use the text foreground colour.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>paletteIndex</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Alpha"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>paletteIndex</c></seealso>
        public ushort PaletteIndex;

        /// <summary>Alpha multiplier.</summary>
        /// <value>The alpha to apply, as an F2DOT14 value; multiplied with the palette entry's own alpha.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>alpha</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="PaletteIndex"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>alpha</c></seealso>
        public F2Dot14 Alpha;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>Both fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            PaletteIndex = BinaryPrimitives.ReverseEndianness(value.PaletteIndex),
            Alpha = F2Dot14.ReverseEndianness(value.Alpha),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 3 — variable solid fill
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 3: variable solid fill.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Same visual operation as <see cref="PaintSolid"/>, with a variation index base added for use with the item variation store.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarSolid</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintSolid"/>
/// <seealso cref="Var{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarSolid</c></seealso>
public sealed record PaintVarSolid : Paint, IEndianReversibleHeaderDerivedRecord<Paint, PaintVarSolid, Var<PaintSolid.Header>>
{
    /// <summary>Gets the CPAL palette index, or <c>0xFFFF</c> for the text foreground colour.</summary>
    /// <value>The zero-based palette entry index, or <c>0xFFFF</c> to use the text foreground colour.</value>
    /// <seealso cref="Alpha"/>
    /// <seealso cref="VarIndexBase"/>
    /// <seealso cref="CpalTable"/>
    public ushort PaletteIndex { get; init; }

    /// <summary>Gets the alpha multiplier.</summary>
    /// <value>The base alpha before variation deltas are applied.</value>
    /// <seealso cref="PaletteIndex"/>
    /// <seealso cref="VarIndexBase"/>
    public F2Dot14 Alpha { get; init; }

    /// <summary>Gets the variation index base.</summary>
    /// <value>The starting index into the item variation store for the alpha's deltas.</value>
    /// <seealso cref="Alpha"/>
    public uint VarIndexBase { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read <see cref="Var{T}"/>-wrapped header.</param>
    /// <param name="context">Unused.</param>
    /// <returns>A new instance populated from the header.</returns>
    /// <seealso cref="Var{T}"/>
    /// <seealso cref="PaintSolid.Header"/>
    static PaintVarSolid IHeaderDerivedRecord<Paint, PaintVarSolid, Var<PaintSolid.Header>>.FromHeader(in Var<PaintSolid.Header> header, object? context) => new()
    {
        Format = 3,
        PaletteIndex = header.Value.PaletteIndex,
        Alpha = header.Value.Alpha,
        VarIndexBase = header.VarIndexBase,
    };
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 4 — linear gradient
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 4: linear gradient.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Fills the current clip region with a linear gradient defined by three points; the third point rotates the gradient axis and is often equal to the second for simple axis-aligned gradients.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintLinearGradient</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintVarLinearGradient"/>
/// <seealso cref="ColorLine"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintLinearGradient</c></seealso>
public sealed record PaintLinearGradient : Paint, IDerivedRecord<Paint, PaintLinearGradient>
{
    /// <summary>Gets the color line.</summary>
    /// <value>The gradient's colour stops and extend mode.</value>
    /// <seealso cref="ColorLine"/>
    /// <seealso cref="ColorStop"/>
    public ColorLine ColorLine { get; init; } = null!;

    /// <summary>Gets the gradient start x coordinate.</summary>
    /// <value>The x coordinate of the first reference point.</value>
    /// <seealso cref="Y0"/>
    /// <seealso cref="X1"/>
    public short X0 { get; init; }

    /// <summary>Gets the gradient start y coordinate.</summary>
    /// <value>The y coordinate of the first reference point.</value>
    /// <seealso cref="X0"/>
    /// <seealso cref="Y1"/>
    public short Y0 { get; init; }

    /// <summary>Gets the gradient end x coordinate.</summary>
    /// <value>The x coordinate of the second reference point; stop offset 1 maps here.</value>
    /// <seealso cref="X0"/>
    /// <seealso cref="Y1"/>
    public short X1 { get; init; }

    /// <summary>Gets the gradient end y coordinate.</summary>
    /// <value>The y coordinate of the second reference point; stop offset 1 maps here.</value>
    /// <seealso cref="Y0"/>
    /// <seealso cref="X1"/>
    public short Y1 { get; init; }

    /// <summary>Gets the rotation point x coordinate.</summary>
    /// <value>The x coordinate of the third reference point; defines the gradient's normal direction.</value>
    /// <seealso cref="X0"/>
    /// <seealso cref="X1"/>
    /// <seealso cref="Y2"/>
    public short X2 { get; init; }

    /// <summary>Gets the rotation point y coordinate.</summary>
    /// <value>The y coordinate of the third reference point; defines the gradient's normal direction.</value>
    /// <seealso cref="Y0"/>
    /// <seealso cref="Y1"/>
    /// <seealso cref="X2"/>
    public short Y2 { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or color line extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintLinearGradient</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="ColorLine"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintLinearGradient</c></seealso>
    static PaintLinearGradient IDerivedRecord<Paint, PaintLinearGradient>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new PaintLinearGradient
        {
            Format = 4,
            ColorLine = cursor.Source.ParseRecordAt<ColorLine>(header.ColorLineOffset),
            X0 = header.X0,
            Y0 = header.Y0,
            X1 = header.X1,
            Y1 = header.Y1,
            X2 = header.X2,
            Y2 = header.Y2,
        };
    }

    /// <summary>The <c>PaintLinearGradient</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>ColorLineOffset</c> is a <see cref="UInt24"/> measured from the start of this paint record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintLinearGradient</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintLinearGradient"/>
    /// <seealso cref="ColorLine"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintLinearGradient</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Offset from the start of this paint record to the color line.</summary>
        /// <value>The byte offset of the <see cref="ColorLine"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>colorLineOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ColorLine"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>colorLineOffset</c></seealso>
        public UInt24 ColorLineOffset;

        /// <summary>Gradient start x.</summary>
        /// <value>The x coordinate of the first reference point.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>x0</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Y0"/>
        /// <seealso cref="X1"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>x0</c></seealso>
        public short X0;

        /// <summary>Gradient start y.</summary>
        /// <value>The y coordinate of the first reference point.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>y0</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="X0"/>
        /// <seealso cref="Y1"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>y0</c></seealso>
        public short Y0;

        /// <summary>Gradient end x.</summary>
        /// <value>The x coordinate of the second reference point; stop offset 1 maps here.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>x1</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="X0"/>
        /// <seealso cref="Y1"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>x1</c></seealso>
        public short X1;

        /// <summary>Gradient end y.</summary>
        /// <value>The y coordinate of the second reference point; stop offset 1 maps here.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>y1</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Y0"/>
        /// <seealso cref="X1"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>y1</c></seealso>
        public short Y1;

        /// <summary>Rotation point x.</summary>
        /// <value>The x coordinate of the third reference point; defines the gradient's normal direction.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>x2</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Y2"/>
        /// <seealso cref="X0"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>x2</c></seealso>
        public short X2;

        /// <summary>Rotation point y.</summary>
        /// <value>The y coordinate of the third reference point; defines the gradient's normal direction.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>y2</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="X2"/>
        /// <seealso cref="Y0"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>y2</c></seealso>
        public short Y2;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>The <c>ColorLineOffset</c> field uses <see cref="UInt24.ReverseEndianness(UInt24)"/>; the coordinate fields are reversed through <see cref="BinaryPrimitives.ReverseEndianness(short)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            ColorLineOffset = UInt24.ReverseEndianness(value.ColorLineOffset),
            X0 = BinaryPrimitives.ReverseEndianness(value.X0),
            Y0 = BinaryPrimitives.ReverseEndianness(value.Y0),
            X1 = BinaryPrimitives.ReverseEndianness(value.X1),
            Y1 = BinaryPrimitives.ReverseEndianness(value.Y1),
            X2 = BinaryPrimitives.ReverseEndianness(value.X2),
            Y2 = BinaryPrimitives.ReverseEndianness(value.Y2),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 5 — variable linear gradient
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 5: variable linear gradient.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Same as <see cref="PaintLinearGradient"/> with a variable color line and a variation index base for the coordinates.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarLinearGradient</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintLinearGradient"/>
/// <seealso cref="VarColorLine"/>
/// <seealso cref="Var{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarLinearGradient</c></seealso>
public sealed record PaintVarLinearGradient : Paint, IDerivedRecord<Paint, PaintVarLinearGradient>
{
    /// <summary>Gets the variable color line.</summary>
    /// <value>The gradient's variable colour stops and extend mode.</value>
    /// <seealso cref="VarColorLine"/>
    /// <seealso cref="Var{T}"/>
    public VarColorLine ColorLine { get; init; } = null!;

    /// <summary>Gets the gradient start x coordinate.</summary>
    /// <value>The base x coordinate of the first reference point, before variation deltas.</value>
    /// <seealso cref="Y0"/>
    /// <seealso cref="VarIndexBase"/>
    public short X0 { get; init; }

    /// <summary>Gets the gradient start y coordinate.</summary>
    /// <value>The base y coordinate of the first reference point, before variation deltas.</value>
    /// <seealso cref="X0"/>
    /// <seealso cref="VarIndexBase"/>
    public short Y0 { get; init; }

    /// <summary>Gets the gradient end x coordinate.</summary>
    /// <value>The base x coordinate of the second reference point, before variation deltas.</value>
    /// <seealso cref="X0"/>
    /// <seealso cref="VarIndexBase"/>
    public short X1 { get; init; }

    /// <summary>Gets the gradient end y coordinate.</summary>
    /// <value>The base y coordinate of the second reference point, before variation deltas.</value>
    /// <seealso cref="Y0"/>
    /// <seealso cref="VarIndexBase"/>
    public short Y1 { get; init; }

    /// <summary>Gets the rotation point x coordinate.</summary>
    /// <value>The base x coordinate of the third reference point, before variation deltas.</value>
    /// <seealso cref="X0"/>
    /// <seealso cref="Y2"/>
    /// <seealso cref="VarIndexBase"/>
    public short X2 { get; init; }

    /// <summary>Gets the rotation point y coordinate.</summary>
    /// <value>The base y coordinate of the third reference point, before variation deltas.</value>
    /// <seealso cref="Y0"/>
    /// <seealso cref="X2"/>
    /// <seealso cref="VarIndexBase"/>
    public short Y2 { get; init; }

    /// <summary>Gets the variation index base.</summary>
    /// <value>The starting index into the item variation store for the coordinate deltas.</value>
    /// <seealso cref="X0"/>
    /// <seealso cref="Y0"/>
    public uint VarIndexBase { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or color line extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarLinearGradient</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Var{T}"/>
    /// <seealso cref="VarColorLine"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarLinearGradient</c></seealso>
    static PaintVarLinearGradient IDerivedRecord<Paint, PaintVarLinearGradient>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Var<PaintLinearGradient.Header>>();
        return new PaintVarLinearGradient
        {
            Format = 5,
            ColorLine = cursor.Source.ParseRecordAt<VarColorLine>(header.Value.ColorLineOffset),
            X0 = header.Value.X0,
            Y0 = header.Value.Y0,
            X1 = header.Value.X1,
            Y1 = header.Value.Y1,
            X2 = header.Value.X2,
            Y2 = header.Value.Y2,
            VarIndexBase = header.VarIndexBase,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 6 — radial gradient
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 6: radial gradient.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Fills the current clip region with a radial gradient defined by two circles; the interpolation is defined by the OpenType gradient algorithm.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintRadialGradient</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintVarRadialGradient"/>
/// <seealso cref="ColorLine"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintRadialGradient</c></seealso>
public sealed record PaintRadialGradient : Paint, IDerivedRecord<Paint, PaintRadialGradient>
{
    /// <summary>Gets the color line.</summary>
    /// <value>The gradient's colour stops and extend mode.</value>
    /// <seealso cref="ColorLine"/>
    /// <seealso cref="ColorStop"/>
    public ColorLine ColorLine { get; init; } = null!;

    /// <summary>Gets the first circle center x coordinate.</summary>
    /// <value>The x coordinate of the first circle's center.</value>
    /// <seealso cref="Y0"/>
    /// <seealso cref="X1"/>
    public short X0 { get; init; }

    /// <summary>Gets the first circle center y coordinate.</summary>
    /// <value>The y coordinate of the first circle's center.</value>
    /// <seealso cref="X0"/>
    /// <seealso cref="Y1"/>
    public short Y0 { get; init; }

    /// <summary>Gets the first circle radius.</summary>
    /// <value>The radius of the first circle.</value>
    /// <seealso cref="Radius1"/>
    public ushort Radius0 { get; init; }

    /// <summary>Gets the second circle center x coordinate.</summary>
    /// <value>The x coordinate of the second circle's center.</value>
    /// <seealso cref="X0"/>
    /// <seealso cref="Y1"/>
    public short X1 { get; init; }

    /// <summary>Gets the second circle center y coordinate.</summary>
    /// <value>The y coordinate of the second circle's center.</value>
    /// <seealso cref="Y0"/>
    /// <seealso cref="X1"/>
    public short Y1 { get; init; }

    /// <summary>Gets the second circle radius.</summary>
    /// <value>The radius of the second circle.</value>
    /// <seealso cref="Radius0"/>
    public ushort Radius1 { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or color line extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintRadialGradient</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="ColorLine"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintRadialGradient</c></seealso>
    static PaintRadialGradient IDerivedRecord<Paint, PaintRadialGradient>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new PaintRadialGradient
        {
            Format = 6,
            ColorLine = cursor.Source.ParseRecordAt<ColorLine>(header.ColorLineOffset),
            X0 = header.X0,
            Y0 = header.Y0,
            Radius0 = header.Radius0,
            X1 = header.X1,
            Y1 = header.Y1,
            Radius1 = header.Radius1,
        };
    }

    /// <summary>The <c>PaintRadialGradient</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>ColorLineOffset</c> is a <see cref="UInt24"/> measured from the start of this paint record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintRadialGradient</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintRadialGradient"/>
    /// <seealso cref="ColorLine"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintRadialGradient</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Offset from the start of this paint record to the color line.</summary>
        /// <value>The byte offset of the <see cref="ColorLine"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>colorLineOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ColorLine"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>colorLineOffset</c></seealso>
        public UInt24 ColorLineOffset;

        /// <summary>First circle center x.</summary>
        /// <value>The x coordinate of the first circle's center.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>x0</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Y0"/>
        /// <seealso cref="Radius0"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>x0</c></seealso>
        public short X0;

        /// <summary>First circle center y.</summary>
        /// <value>The y coordinate of the first circle's center.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>y0</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="X0"/>
        /// <seealso cref="Radius0"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>y0</c></seealso>
        public short Y0;

        /// <summary>First circle radius.</summary>
        /// <value>The radius of the first circle.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>r0</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="X0"/>
        /// <seealso cref="Y0"/>
        /// <seealso cref="Radius1"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>r0</c></seealso>
        public ushort Radius0;

        /// <summary>Second circle center x.</summary>
        /// <value>The x coordinate of the second circle's center.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>x1</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Y1"/>
        /// <seealso cref="Radius1"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>x1</c></seealso>
        public short X1;

        /// <summary>Second circle center y.</summary>
        /// <value>The y coordinate of the second circle's center.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>y1</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="X1"/>
        /// <seealso cref="Radius1"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>y1</c></seealso>
        public short Y1;

        /// <summary>Second circle radius.</summary>
        /// <value>The radius of the second circle.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>r1</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="X1"/>
        /// <seealso cref="Y1"/>
        /// <seealso cref="Radius0"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>r1</c></seealso>
        public ushort Radius1;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>The <c>ColorLineOffset</c> field uses <see cref="UInt24.ReverseEndianness(UInt24)"/>; the coordinate and radius fields are reversed through the corresponding <see cref="BinaryPrimitives"/> overloads.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            ColorLineOffset = UInt24.ReverseEndianness(value.ColorLineOffset),
            X0 = BinaryPrimitives.ReverseEndianness(value.X0),
            Y0 = BinaryPrimitives.ReverseEndianness(value.Y0),
            Radius0 = BinaryPrimitives.ReverseEndianness(value.Radius0),
            X1 = BinaryPrimitives.ReverseEndianness(value.X1),
            Y1 = BinaryPrimitives.ReverseEndianness(value.Y1),
            Radius1 = BinaryPrimitives.ReverseEndianness(value.Radius1),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 7 — variable radial gradient
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 7: variable radial gradient.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Same as <see cref="PaintRadialGradient"/> with a variable color line and a variation index base for the geometry.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarRadialGradient</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintRadialGradient"/>
/// <seealso cref="VarColorLine"/>
/// <seealso cref="Var{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarRadialGradient</c></seealso>
public sealed record PaintVarRadialGradient : Paint, IDerivedRecord<Paint, PaintVarRadialGradient>
{
    /// <summary>Gets the variable color line.</summary>
    /// <value>The gradient's variable colour stops and extend mode.</value>
    /// <seealso cref="VarColorLine"/>
    /// <seealso cref="Var{T}"/>
    public VarColorLine ColorLine { get; init; } = null!;

    /// <summary>Gets the first circle center x coordinate.</summary>
    /// <value>The base x coordinate of the first circle's center, before variation deltas.</value>
    /// <seealso cref="Y0"/>
    /// <seealso cref="VarIndexBase"/>
    public short X0 { get; init; }

    /// <summary>Gets the first circle center y coordinate.</summary>
    /// <value>The base y coordinate of the first circle's center, before variation deltas.</value>
    /// <seealso cref="X0"/>
    /// <seealso cref="VarIndexBase"/>
    public short Y0 { get; init; }

    /// <summary>Gets the first circle radius.</summary>
    /// <value>The base radius of the first circle, before variation deltas.</value>
    /// <seealso cref="Radius1"/>
    /// <seealso cref="VarIndexBase"/>
    public ushort Radius0 { get; init; }

    /// <summary>Gets the second circle center x coordinate.</summary>
    /// <value>The base x coordinate of the second circle's center, before variation deltas.</value>
    /// <seealso cref="X0"/>
    /// <seealso cref="VarIndexBase"/>
    public short X1 { get; init; }

    /// <summary>Gets the second circle center y coordinate.</summary>
    /// <value>The base y coordinate of the second circle's center, before variation deltas.</value>
    /// <seealso cref="Y0"/>
    /// <seealso cref="VarIndexBase"/>
    public short Y1 { get; init; }

    /// <summary>Gets the second circle radius.</summary>
    /// <value>The base radius of the second circle, before variation deltas.</value>
    /// <seealso cref="Radius0"/>
    /// <seealso cref="VarIndexBase"/>
    public ushort Radius1 { get; init; }

    /// <summary>Gets the variation index base.</summary>
    /// <value>The starting index into the item variation store for the geometry deltas.</value>
    /// <seealso cref="X0"/>
    /// <seealso cref="Radius0"/>
    public uint VarIndexBase { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or color line extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarRadialGradient</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Var{T}"/>
    /// <seealso cref="VarColorLine"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarRadialGradient</c></seealso>
    static PaintVarRadialGradient IDerivedRecord<Paint, PaintVarRadialGradient>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Var<PaintRadialGradient.Header>>();
        return new PaintVarRadialGradient
        {
            Format = 7,
            ColorLine = cursor.Source.ParseRecordAt<VarColorLine>(header.Value.ColorLineOffset),
            X0 = header.Value.X0,
            Y0 = header.Value.Y0,
            Radius0 = header.Value.Radius0,
            X1 = header.Value.X1,
            Y1 = header.Value.Y1,
            Radius1 = header.Value.Radius1,
            VarIndexBase = header.VarIndexBase,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 8 — sweep gradient
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 8: sweep gradient.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Fills the current clip region with a sweep gradient: the colour varies with the angle around a center point.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintSweepGradient</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintVarSweepGradient"/>
/// <seealso cref="ColorLine"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintSweepGradient</c></seealso>
public sealed record PaintSweepGradient : Paint, IDerivedRecord<Paint, PaintSweepGradient>
{
    /// <summary>Gets the color line.</summary>
    /// <value>The gradient's colour stops and extend mode.</value>
    /// <seealso cref="ColorLine"/>
    /// <seealso cref="ColorStop"/>
    public ColorLine ColorLine { get; init; } = null!;

    /// <summary>Gets the center x coordinate.</summary>
    /// <value>The x coordinate of the sweep center.</value>
    /// <seealso cref="CenterY"/>
    public short CenterX { get; init; }

    /// <summary>Gets the center y coordinate.</summary>
    /// <value>The y coordinate of the sweep center.</value>
    /// <seealso cref="CenterX"/>
    public short CenterY { get; init; }

    /// <summary>Gets the start angle.</summary>
    /// <value>The angle at which the sweep begins, as an F2DOT14 value in counter-clockwise degrees.</value>
    /// <seealso cref="EndAngle"/>
    public F2Dot14 StartAngle { get; init; }

    /// <summary>Gets the end angle.</summary>
    /// <value>The angle at which the sweep ends, as an F2DOT14 value in counter-clockwise degrees.</value>
    /// <seealso cref="StartAngle"/>
    public F2Dot14 EndAngle { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or color line extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintSweepGradient</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="ColorLine"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintSweepGradient</c></seealso>
    static PaintSweepGradient IDerivedRecord<Paint, PaintSweepGradient>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new PaintSweepGradient
        {
            Format = 8,
            ColorLine = cursor.Source.ParseRecordAt<ColorLine>(header.ColorLineOffset),
            CenterX = header.CenterX,
            CenterY = header.CenterY,
            StartAngle = header.StartAngle,
            EndAngle = header.EndAngle,
        };
    }

    /// <summary>The <c>PaintSweepGradient</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>ColorLineOffset</c> is a <see cref="UInt24"/> measured from the start of this paint record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintSweepGradient</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintSweepGradient"/>
    /// <seealso cref="ColorLine"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintSweepGradient</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Offset from the start of this paint record to the color line.</summary>
        /// <value>The byte offset of the <see cref="ColorLine"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>colorLineOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ColorLine"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>colorLineOffset</c></seealso>
        public UInt24 ColorLineOffset;

        /// <summary>Center x.</summary>
        /// <value>The x coordinate of the sweep center.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>centerX</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CenterY"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>centerX</c></seealso>
        public short CenterX;

        /// <summary>Center y.</summary>
        /// <value>The y coordinate of the sweep center.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>centerY</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CenterX"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>centerY</c></seealso>
        public short CenterY;

        /// <summary>Start angle.</summary>
        /// <value>The angle at which the sweep begins, as an F2DOT14 value in counter-clockwise degrees.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>startAngle</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="EndAngle"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>startAngle</c></seealso>
        public F2Dot14 StartAngle;

        /// <summary>End angle.</summary>
        /// <value>The angle at which the sweep ends, as an F2DOT14 value in counter-clockwise degrees.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>endAngle</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="StartAngle"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>endAngle</c></seealso>
        public F2Dot14 EndAngle;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>The <c>ColorLineOffset</c> field uses <see cref="UInt24.ReverseEndianness(UInt24)"/>; the angle fields are reversed through <see cref="F2Dot14.ReverseEndianness(F2Dot14)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            ColorLineOffset = UInt24.ReverseEndianness(value.ColorLineOffset),
            CenterX = BinaryPrimitives.ReverseEndianness(value.CenterX),
            CenterY = BinaryPrimitives.ReverseEndianness(value.CenterY),
            StartAngle = F2Dot14.ReverseEndianness(value.StartAngle),
            EndAngle = F2Dot14.ReverseEndianness(value.EndAngle),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 9 — variable sweep gradient
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 9: variable sweep gradient.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Same as <see cref="PaintSweepGradient"/> with a variable color line and a variation index base.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarSweepGradient</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintSweepGradient"/>
/// <seealso cref="VarColorLine"/>
/// <seealso cref="Var{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarSweepGradient</c></seealso>
public sealed record PaintVarSweepGradient : Paint, IDerivedRecord<Paint, PaintVarSweepGradient>
{
    /// <summary>Gets the variable color line.</summary>
    /// <value>The gradient's variable colour stops and extend mode.</value>
    /// <seealso cref="VarColorLine"/>
    /// <seealso cref="Var{T}"/>
    public VarColorLine ColorLine { get; init; } = null!;

    /// <summary>Gets the center x coordinate.</summary>
    /// <value>The base x coordinate of the sweep center, before variation deltas.</value>
    /// <seealso cref="CenterY"/>
    /// <seealso cref="VarIndexBase"/>
    public short CenterX { get; init; }

    /// <summary>Gets the center y coordinate.</summary>
    /// <value>The base y coordinate of the sweep center, before variation deltas.</value>
    /// <seealso cref="CenterX"/>
    /// <seealso cref="VarIndexBase"/>
    public short CenterY { get; init; }

    /// <summary>Gets the start angle.</summary>
    /// <value>The base angle at which the sweep begins, before variation deltas.</value>
    /// <seealso cref="EndAngle"/>
    /// <seealso cref="VarIndexBase"/>
    public F2Dot14 StartAngle { get; init; }

    /// <summary>Gets the end angle.</summary>
    /// <value>The base angle at which the sweep ends, before variation deltas.</value>
    /// <seealso cref="StartAngle"/>
    /// <seealso cref="VarIndexBase"/>
    public F2Dot14 EndAngle { get; init; }

    /// <summary>Gets the variation index base.</summary>
    /// <value>The starting index into the item variation store for the geometry deltas.</value>
    /// <seealso cref="CenterX"/>
    /// <seealso cref="StartAngle"/>
    public uint VarIndexBase { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or color line extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarSweepGradient</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Var{T}"/>
    /// <seealso cref="VarColorLine"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarSweepGradient</c></seealso>
    static PaintVarSweepGradient IDerivedRecord<Paint, PaintVarSweepGradient>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Var<PaintSweepGradient.Header>>();
        return new PaintVarSweepGradient
        {
            Format = 9,
            ColorLine = cursor.Source.ParseRecordAt<VarColorLine>(header.Value.ColorLineOffset),
            CenterX = header.Value.CenterX,
            CenterY = header.Value.CenterY,
            StartAngle = header.Value.StartAngle,
            EndAngle = header.Value.EndAngle,
            VarIndexBase = header.VarIndexBase,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 10 — glyph shape
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 10: a glyph outline shape.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Establishes the outline of <see cref="GlyphId"/> as the clip shape for the sub-graph reached through <see cref="Paint"/>.</description></item>
/// <item><description>This is the primary way a colour glyph composes an actual shape; the sub-graph is normally a gradient or solid fill.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintGlyph</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintColrGlyph"/>
/// <seealso cref="PaintSolid"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintGlyph</c></seealso>
public sealed record PaintGlyph : Paint, IDerivedRecord<Paint, PaintGlyph>
{
    /// <summary>Gets the sub-graph that fills the outline.</summary>
    /// <value>The paint applied within the glyph's outline.</value>
    /// <seealso cref="GlyphId"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the glyph ID providing the outline.</summary>
    /// <value>The glyph whose outline is used as the clip shape.</value>
    /// <seealso cref="Paint"/>
    public ushort GlyphId { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintGlyph</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Paint"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintGlyph</c></seealso>
    static PaintGlyph IDerivedRecord<Paint, PaintGlyph>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new PaintGlyph
        {
            Format = 10,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.PaintOffset),
            GlyphId = header.GlyphId,
        };
    }

    /// <summary>The <c>PaintGlyph</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>PaintOffset</c> is a <see cref="UInt24"/> measured from the start of this paint record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintGlyph</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintGlyph"/>
    /// <seealso cref="Paint"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintGlyph</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Offset from the start of this paint record to the sub-graph.</summary>
        /// <value>The byte offset of the <see cref="Paint"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>paintOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="GlyphId"/>
        /// <seealso cref="Paint"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>paintOffset</c></seealso>
        public UInt24 PaintOffset;

        /// <summary>Glyph ID providing the outline.</summary>
        /// <value>The glyph whose outline is used as the clip shape.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>glyphID</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="PaintOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>glyphID</c></seealso>
        public ushort GlyphId;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>The <c>PaintOffset</c> field uses <see cref="UInt24.ReverseEndianness(UInt24)"/>; the glyph ID is reversed through <see cref="BinaryPrimitives.ReverseEndianness(ushort)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            PaintOffset = UInt24.ReverseEndianness(value.PaintOffset),
            GlyphId = BinaryPrimitives.ReverseEndianness(value.GlyphId),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 11 — glyph reference
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 11: a reference to another color glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Reuses the whole paint graph of another base glyph within the current graph. This is how glyphs share colour definitions.</description></item>
/// <item><description>The reference is resolved through <see cref="ColrTable.GetRootPaint(int)"/> on the enclosing table, not by an offset stored in this record.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintColrGlyph</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintGlyph"/>
/// <seealso cref="ColrTable.GetRootPaint(int)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintColrGlyph</c></seealso>
public sealed record PaintColrGlyph : Paint, IEndianReversibleHeaderDerivedRecord<Paint, PaintColrGlyph, PaintColrGlyph.Header>
{
    /// <summary>Gets the base glyph ID whose color glyph is reused.</summary>
    /// <value>The glyph whose root paint is drawn in place of this node.</value>
    /// <seealso cref="ColrTable.GetRootPaint(int)"/>
    public ushort GlyphId { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read header.</param>
    /// <param name="context">Unused.</param>
    /// <returns>A new instance populated from the header.</returns>
    /// <remarks>Only the glyph ID is stored; the referenced paint graph is resolved lazily by the caller through <see cref="ColrTable.GetRootPaint(int)"/>.</remarks>
    /// <seealso cref="Header"/>
    static PaintColrGlyph IHeaderDerivedRecord<Paint, PaintColrGlyph, Header>.FromHeader(in Header header, object? context) => new()
    {
        Format = 11,
        GlyphId = header.GlyphId,
    };

    /// <summary>The 2-byte <c>PaintColrGlyph</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintColrGlyph</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintColrGlyph"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintColrGlyph</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Base glyph ID whose color glyph is reused.</summary>
        /// <value>The glyph whose root paint is drawn in place of this node.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>glyphID</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ColrTable.GetRootPaint(int)"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>glyphID</c></seealso>
        public ushort GlyphId;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte field is to be reversed.</param>
        /// <returns>A new header with the glyph ID reversed.</returns>
        /// <remarks>The header contains a single multi-byte field.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            GlyphId = BinaryPrimitives.ReverseEndianness(value.GlyphId),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 12 — affine transform
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 12: affine transform.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Applies an arbitrary <see cref="Affine2x3"/> matrix to the coordinate system of the sub-graph reached through <see cref="Paint"/>.</description></item>
/// <item><description>Equivalent in effect to a translate/scale/rotate/skew combination, but expressed directly as a matrix.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintTransform</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintVarTransform"/>
/// <seealso cref="Affine2x3"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintTransform</c></seealso>
public sealed record PaintTransform : Paint, IDerivedRecord<Paint, PaintTransform>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system the transform applies to.</value>
    /// <seealso cref="Transform"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the transform matrix.</summary>
    /// <value>The <see cref="Affine2x3"/> applied to the sub-graph.</value>
    /// <seealso cref="Paint"/>
    /// <seealso cref="Affine2x3"/>
    public Affine2x3 Transform { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header, the referenced paint, or the referenced matrix extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintTransform</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Affine2x3"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintTransform</c></seealso>
    static PaintTransform IDerivedRecord<Paint, PaintTransform>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new PaintTransform
        {
            Format = 12,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.PaintOffset),
            Transform = cursor.Source.ReadEndianReversibleStructAt<Affine2x3>(header.TransformOffset),
        };
    }

    /// <summary>The <c>PaintTransform</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both offsets are <see cref="UInt24"/> values measured from the start of this paint record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintTransform</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintTransform"/>
    /// <seealso cref="Affine2x3"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintTransform</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Offset from the start of this paint record to the sub-graph.</summary>
        /// <value>The byte offset of the <see cref="Paint"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>paintOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="TransformOffset"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>paintOffset</c></seealso>
        public UInt24 PaintOffset;

        /// <summary>Offset from the start of this paint record to the transform matrix.</summary>
        /// <value>The byte offset of the <see cref="Affine2x3"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>transformOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="PaintOffset"/>
        /// <seealso cref="Affine2x3"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>transformOffset</c></seealso>
        public UInt24 TransformOffset;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both offsets reversed.</returns>
        /// <remarks>Both fields are <see cref="UInt24"/> values and use <see cref="UInt24.ReverseEndianness(UInt24)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            PaintOffset = UInt24.ReverseEndianness(value.PaintOffset),
            TransformOffset = UInt24.ReverseEndianness(value.TransformOffset),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 13 — variable affine transform
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 13: variable affine transform.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Same as <see cref="PaintTransform"/>, but the referenced transform is a <see cref="Var{T}"/> of <see cref="Affine2x3"/>, so it carries a <see cref="Var{T}.VarIndexBase"/> for variation.</description></item>
/// <item><description>The transform's variation index base is exposed through <see cref="Transform"/>'s own <c>VarIndexBase</c> field, not as a separate property.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarTransform</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintTransform"/>
/// <seealso cref="Affine2x3"/>
/// <seealso cref="Var{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarTransform</c></seealso>
public sealed record PaintVarTransform : Paint, IDerivedRecord<Paint, PaintVarTransform>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system the transform applies to.</value>
    /// <seealso cref="Transform"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the variable transform matrix.</summary>
    /// <value>The <see cref="Var{T}"/>-wrapped <see cref="Affine2x3"/>; its <see cref="Var{T}.VarIndexBase"/> carries the variation index for the matrix.</value>
    /// <seealso cref="Paint"/>
    /// <seealso cref="Affine2x3"/>
    /// <seealso cref="Var{T}"/>
    public Var<Affine2x3> Transform { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header, the referenced paint, or the referenced matrix extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header layout is the same as <see cref="PaintTransform.Header"/>; the record shape differs only in what the transform offset points at.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarTransform</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintTransform.Header"/>
    /// <seealso cref="Var{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarTransform</c></seealso>
    static PaintVarTransform IDerivedRecord<Paint, PaintVarTransform>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<PaintTransform.Header>();
        return new PaintVarTransform
        {
            Format = 13,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.PaintOffset),
            Transform = cursor.Source.ReadEndianReversibleStructAt<Var<Affine2x3>>(header.TransformOffset),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 14 — translate
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 14: translate.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Shifts the coordinate system of the sub-graph reached through <see cref="Paint"/> by a fixed <c>(Dx, Dy)</c> offset.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintTranslate</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintVarTranslate"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintTranslate</c></seealso>
public sealed record PaintTranslate : Paint, IDerivedRecord<Paint, PaintTranslate>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is shifted.</value>
    /// <seealso cref="Dx"/>
    /// <seealso cref="Dy"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the translation x component.</summary>
    /// <value>The horizontal shift, in font design units.</value>
    /// <seealso cref="Paint"/>
    /// <seealso cref="Dy"/>
    public short Dx { get; init; }

    /// <summary>Gets the translation y component.</summary>
    /// <value>The vertical shift, in font design units.</value>
    /// <seealso cref="Paint"/>
    /// <seealso cref="Dx"/>
    public short Dy { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintTranslate</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintTranslate</c></seealso>
    static PaintTranslate IDerivedRecord<Paint, PaintTranslate>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new PaintTranslate
        {
            Format = 14,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.PaintOffset),
            Dx = header.Dx,
            Dy = header.Dy,
        };
    }

    /// <summary>The <c>PaintTranslate</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>PaintOffset</c> is a <see cref="UInt24"/> measured from the start of this paint record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintTranslate</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintTranslate"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintTranslate</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Offset from the start of this paint record to the sub-graph.</summary>
        /// <value>The byte offset of the <see cref="Paint"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>paintOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Dx"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>paintOffset</c></seealso>
        public UInt24 PaintOffset;

        /// <summary>Translation x.</summary>
        /// <value>The horizontal shift, in font design units.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>dx</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Dy"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>dx</c></seealso>
        public short Dx;

        /// <summary>Translation y.</summary>
        /// <value>The vertical shift, in font design units.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>dy</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Dx"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>dy</c></seealso>
        public short Dy;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>The <c>PaintOffset</c> field uses <see cref="UInt24.ReverseEndianness(UInt24)"/>; the coordinate fields are reversed through <see cref="BinaryPrimitives.ReverseEndianness(short)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            PaintOffset = UInt24.ReverseEndianness(value.PaintOffset),
            Dx = BinaryPrimitives.ReverseEndianness(value.Dx),
            Dy = BinaryPrimitives.ReverseEndianness(value.Dy),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 15 — variable translate
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 15: variable translate.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Same as <see cref="PaintTranslate"/>, with a variation index base for the translation components.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarTranslate</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintTranslate"/>
/// <seealso cref="Var{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarTranslate</c></seealso>
public sealed record PaintVarTranslate : Paint, IDerivedRecord<Paint, PaintVarTranslate>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is shifted.</value>
    /// <seealso cref="Dx"/>
    /// <seealso cref="Dy"/>
    /// <seealso cref="VarIndexBase"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the translation x component.</summary>
    /// <value>The base horizontal shift, before variation deltas.</value>
    /// <seealso cref="Paint"/>
    /// <seealso cref="Dy"/>
    /// <seealso cref="VarIndexBase"/>
    public short Dx { get; init; }

    /// <summary>Gets the translation y component.</summary>
    /// <value>The base vertical shift, before variation deltas.</value>
    /// <seealso cref="Paint"/>
    /// <seealso cref="Dx"/>
    /// <seealso cref="VarIndexBase"/>
    public short Dy { get; init; }

    /// <summary>Gets the variation index base.</summary>
    /// <value>The starting index into the item variation store for the translation deltas.</value>
    /// <seealso cref="Dx"/>
    /// <seealso cref="Dy"/>
    public uint VarIndexBase { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarTranslate</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Var{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarTranslate</c></seealso>
    static PaintVarTranslate IDerivedRecord<Paint, PaintVarTranslate>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Var<PaintTranslate.Header>>();
        return new PaintVarTranslate
        {
            Format = 15,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.Value.PaintOffset),
            Dx = header.Value.Dx,
            Dy = header.Value.Dy,
            VarIndexBase = header.VarIndexBase,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 16 — non-uniform scale
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 16: non-uniform scale.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Scales the coordinate system of the sub-graph reached through <see cref="Paint"/> independently along the x and y axes.</description></item>
/// <item><description>The scale is around the origin; use <see cref="PaintScaleAroundCenter"/> to scale around an arbitrary point.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintScale</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintVarScale"/>
/// <seealso cref="PaintScaleUniform"/>
/// <seealso cref="PaintScaleAroundCenter"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintScale</c></seealso>
public sealed record PaintScale : Paint, IDerivedRecord<Paint, PaintScale>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is scaled.</value>
    /// <seealso cref="ScaleX"/>
    /// <seealso cref="ScaleY"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the x scale factor.</summary>
    /// <value>The scale applied along the x axis, as an F2DOT14 value.</value>
    /// <seealso cref="Paint"/>
    /// <seealso cref="ScaleY"/>
    public F2Dot14 ScaleX { get; init; }

    /// <summary>Gets the y scale factor.</summary>
    /// <value>The scale applied along the y axis, as an F2DOT14 value.</value>
    /// <seealso cref="Paint"/>
    /// <seealso cref="ScaleX"/>
    public F2Dot14 ScaleY { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintScale</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintScale</c></seealso>
    static PaintScale IDerivedRecord<Paint, PaintScale>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new PaintScale
        {
            Format = 16,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.PaintOffset),
            ScaleX = header.ScaleX,
            ScaleY = header.ScaleY,
        };
    }

    /// <summary>The <c>PaintScale</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>PaintOffset</c> is a <see cref="UInt24"/> measured from the start of this paint record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintScale</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintScale"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintScale</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Offset from the start of this paint record to the sub-graph.</summary>
        /// <value>The byte offset of the <see cref="Paint"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>paintOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ScaleX"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>paintOffset</c></seealso>
        public UInt24 PaintOffset;

        /// <summary>X scale factor.</summary>
        /// <value>The scale applied along the x axis, as an F2DOT14 value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>scaleX</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ScaleY"/>
        /// <seealso cref="F2Dot14"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>scaleX</c></seealso>
        public F2Dot14 ScaleX;

        /// <summary>Y scale factor.</summary>
        /// <value>The scale applied along the y axis, as an F2DOT14 value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>scaleY</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ScaleX"/>
        /// <seealso cref="F2Dot14"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>scaleY</c></seealso>
        public F2Dot14 ScaleY;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>The <c>PaintOffset</c> field uses <see cref="UInt24.ReverseEndianness(UInt24)"/>; the scale fields use <see cref="F2Dot14.ReverseEndianness(F2Dot14)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            PaintOffset = UInt24.ReverseEndianness(value.PaintOffset),
            ScaleX = F2Dot14.ReverseEndianness(value.ScaleX),
            ScaleY = F2Dot14.ReverseEndianness(value.ScaleY),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 17 — variable non-uniform scale
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 17: variable non-uniform scale.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Same as <see cref="PaintScale"/>, with a variation index base for the scale factors.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarScale</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintScale"/>
/// <seealso cref="Var{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarScale</c></seealso>
public sealed record PaintVarScale : Paint, IDerivedRecord<Paint, PaintVarScale>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is scaled.</value>
    /// <seealso cref="ScaleX"/>
    /// <seealso cref="ScaleY"/>
    /// <seealso cref="VarIndexBase"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the x scale factor.</summary>
    /// <value>The base scale along the x axis, before variation deltas.</value>
    /// <seealso cref="Paint"/>
    /// <seealso cref="ScaleY"/>
    /// <seealso cref="VarIndexBase"/>
    public F2Dot14 ScaleX { get; init; }

    /// <summary>Gets the y scale factor.</summary>
    /// <value>The base scale along the y axis, before variation deltas.</value>
    /// <seealso cref="Paint"/>
    /// <seealso cref="ScaleX"/>
    /// <seealso cref="VarIndexBase"/>
    public F2Dot14 ScaleY { get; init; }

    /// <summary>Gets the variation index base.</summary>
    /// <value>The starting index into the item variation store for the scale deltas.</value>
    /// <seealso cref="ScaleX"/>
    /// <seealso cref="ScaleY"/>
    public uint VarIndexBase { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarScale</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Var{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarScale</c></seealso>
    static PaintVarScale IDerivedRecord<Paint, PaintVarScale>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Var<PaintScale.Header>>();
        return new PaintVarScale
        {
            Format = 17,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.Value.PaintOffset),
            ScaleX = header.Value.ScaleX,
            ScaleY = header.Value.ScaleY,
            VarIndexBase = header.VarIndexBase,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 18 — non-uniform scale around a center
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 18: non-uniform scale around a center.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Scales the sub-graph reached through <see cref="Paint"/> around an explicit <c>(CenterX, CenterY)</c> point rather than the origin.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintScaleAroundCenter</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintScale"/>
/// <seealso cref="PaintVarScaleAroundCenter"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintScaleAroundCenter</c></seealso>
public sealed record PaintScaleAroundCenter : Paint, IDerivedRecord<Paint, PaintScaleAroundCenter>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is scaled.</value>
    /// <seealso cref="ScaleX"/>
    /// <seealso cref="CenterX"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the x scale factor.</summary>
    /// <value>The scale applied along the x axis, as an F2DOT14 value.</value>
    /// <seealso cref="ScaleY"/>
    /// <seealso cref="CenterX"/>
    public F2Dot14 ScaleX { get; init; }

    /// <summary>Gets the y scale factor.</summary>
    /// <value>The scale applied along the y axis, as an F2DOT14 value.</value>
    /// <seealso cref="ScaleX"/>
    /// <seealso cref="CenterY"/>
    public F2Dot14 ScaleY { get; init; }

    /// <summary>Gets the center x coordinate.</summary>
    /// <value>The x coordinate of the point about which the scale is applied.</value>
    /// <seealso cref="ScaleX"/>
    /// <seealso cref="CenterY"/>
    public short CenterX { get; init; }

    /// <summary>Gets the center y coordinate.</summary>
    /// <value>The y coordinate of the point about which the scale is applied.</value>
    /// <seealso cref="ScaleY"/>
    /// <seealso cref="CenterX"/>
    public short CenterY { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintScaleAroundCenter</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintScaleAroundCenter</c></seealso>
    static PaintScaleAroundCenter IDerivedRecord<Paint, PaintScaleAroundCenter>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new PaintScaleAroundCenter
        {
            Format = 18,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.PaintOffset),
            ScaleX = header.ScaleX,
            ScaleY = header.ScaleY,
            CenterX = header.CenterX,
            CenterY = header.CenterY,
        };
    }

    /// <summary>The <c>PaintScaleAroundCenter</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>PaintOffset</c> is a <see cref="UInt24"/> measured from the start of this paint record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintScaleAroundCenter</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintScaleAroundCenter"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintScaleAroundCenter</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Offset from the start of this paint record to the sub-graph.</summary>
        /// <value>The byte offset of the <see cref="Paint"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>paintOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ScaleX"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>paintOffset</c></seealso>
        public UInt24 PaintOffset;

        /// <summary>X scale factor.</summary>
        /// <value>The scale applied along the x axis, as an F2DOT14 value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>scaleX</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ScaleY"/>
        /// <seealso cref="F2Dot14"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>scaleX</c></seealso>
        public F2Dot14 ScaleX;

        /// <summary>Y scale factor.</summary>
        /// <value>The scale applied along the y axis, as an F2DOT14 value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>scaleY</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ScaleX"/>
        /// <seealso cref="F2Dot14"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>scaleY</c></seealso>
        public F2Dot14 ScaleY;

        /// <summary>Center x.</summary>
        /// <value>The x coordinate of the point about which the scale is applied.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>centerX</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CenterY"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>centerX</c></seealso>
        public short CenterX;

        /// <summary>Center y.</summary>
        /// <value>The y coordinate of the point about which the scale is applied.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>centerY</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CenterX"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>centerY</c></seealso>
        public short CenterY;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>Uses <see cref="UInt24.ReverseEndianness(UInt24)"/>, <see cref="F2Dot14.ReverseEndianness(F2Dot14)"/>, and <see cref="BinaryPrimitives.ReverseEndianness(short)"/> for the three field types.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            PaintOffset = UInt24.ReverseEndianness(value.PaintOffset),
            ScaleX = F2Dot14.ReverseEndianness(value.ScaleX),
            ScaleY = F2Dot14.ReverseEndianness(value.ScaleY),
            CenterX = BinaryPrimitives.ReverseEndianness(value.CenterX),
            CenterY = BinaryPrimitives.ReverseEndianness(value.CenterY),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 19 — variable non-uniform scale around a center
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 19: variable non-uniform scale around a center.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Same as <see cref="PaintScaleAroundCenter"/>, with a variation index base for the scale factors and center.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarScaleAroundCenter</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintScaleAroundCenter"/>
/// <seealso cref="Var{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarScaleAroundCenter</c></seealso>
public sealed record PaintVarScaleAroundCenter : Paint, IDerivedRecord<Paint, PaintVarScaleAroundCenter>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is scaled.</value>
    /// <seealso cref="ScaleX"/>
    /// <seealso cref="CenterX"/>
    /// <seealso cref="VarIndexBase"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the x scale factor.</summary>
    /// <value>The base scale along the x axis, before variation deltas.</value>
    /// <seealso cref="ScaleY"/>
    /// <seealso cref="VarIndexBase"/>
    public F2Dot14 ScaleX { get; init; }

    /// <summary>Gets the y scale factor.</summary>
    /// <value>The base scale along the y axis, before variation deltas.</value>
    /// <seealso cref="ScaleX"/>
    /// <seealso cref="VarIndexBase"/>
    public F2Dot14 ScaleY { get; init; }

    /// <summary>Gets the center x coordinate.</summary>
    /// <value>The base x coordinate of the point about which the scale is applied, before variation deltas.</value>
    /// <seealso cref="CenterY"/>
    /// <seealso cref="VarIndexBase"/>
    public short CenterX { get; init; }

    /// <summary>Gets the center y coordinate.</summary>
    /// <value>The base y coordinate of the point about which the scale is applied, before variation deltas.</value>
    /// <seealso cref="CenterX"/>
    /// <seealso cref="VarIndexBase"/>
    public short CenterY { get; init; }

    /// <summary>Gets the variation index base.</summary>
    /// <value>The starting index into the item variation store for the scale and center deltas.</value>
    /// <seealso cref="ScaleX"/>
    /// <seealso cref="CenterX"/>
    public uint VarIndexBase { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarScaleAroundCenter</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Var{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarScaleAroundCenter</c></seealso>
    static PaintVarScaleAroundCenter IDerivedRecord<Paint, PaintVarScaleAroundCenter>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Var<PaintScaleAroundCenter.Header>>();
        return new PaintVarScaleAroundCenter
        {
            Format = 19,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.Value.PaintOffset),
            ScaleX = header.Value.ScaleX,
            ScaleY = header.Value.ScaleY,
            CenterX = header.Value.CenterX,
            CenterY = header.Value.CenterY,
            VarIndexBase = header.VarIndexBase,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 20 — uniform scale
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 20: uniform scale.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Scales the sub-graph reached through <see cref="Paint"/> by the same factor along both axes.</description></item>
/// <item><description>The scale is around the origin; use <see cref="PaintScaleUniformAroundCenter"/> to scale around an arbitrary point.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintScaleUniform</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintScale"/>
/// <seealso cref="PaintVarScaleUniform"/>
/// <seealso cref="PaintScaleUniformAroundCenter"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintScaleUniform</c></seealso>
public sealed record PaintScaleUniform : Paint, IDerivedRecord<Paint, PaintScaleUniform>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is scaled.</value>
    /// <seealso cref="Scale"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the uniform scale factor.</summary>
    /// <value>The scale applied along both axes, as an F2DOT14 value.</value>
    /// <seealso cref="Paint"/>
    public F2Dot14 Scale { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintScaleUniform</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintScaleUniform</c></seealso>
    static PaintScaleUniform IDerivedRecord<Paint, PaintScaleUniform>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new PaintScaleUniform
        {
            Format = 20,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.PaintOffset),
            Scale = header.Scale,
        };
    }

    /// <summary>The <c>PaintScaleUniform</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>PaintOffset</c> is a <see cref="UInt24"/> measured from the start of this paint record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintScaleUniform</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintScaleUniform"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintScaleUniform</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Offset from the start of this paint record to the sub-graph.</summary>
        /// <value>The byte offset of the <see cref="Paint"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>paintOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Scale"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>paintOffset</c></seealso>
        public UInt24 PaintOffset;

        /// <summary>Uniform scale factor.</summary>
        /// <value>The scale applied along both axes, as an F2DOT14 value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>scale</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="PaintOffset"/>
        /// <seealso cref="F2Dot14"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>scale</c></seealso>
        public F2Dot14 Scale;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Uses <see cref="UInt24.ReverseEndianness(UInt24)"/> and <see cref="F2Dot14.ReverseEndianness(F2Dot14)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            PaintOffset = UInt24.ReverseEndianness(value.PaintOffset),
            Scale = F2Dot14.ReverseEndianness(value.Scale),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 21 — variable uniform scale
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 21: variable uniform scale.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Same as <see cref="PaintScaleUniform"/>, with a variation index base for the scale factor.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarScaleUniform</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintScaleUniform"/>
/// <seealso cref="Var{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarScaleUniform</c></seealso>
public sealed record PaintVarScaleUniform : Paint, IDerivedRecord<Paint, PaintVarScaleUniform>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is scaled.</value>
    /// <seealso cref="Scale"/>
    /// <seealso cref="VarIndexBase"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the uniform scale factor.</summary>
    /// <value>The base scale along both axes, before variation deltas.</value>
    /// <seealso cref="Paint"/>
    /// <seealso cref="VarIndexBase"/>
    public F2Dot14 Scale { get; init; }

    /// <summary>Gets the variation index base.</summary>
    /// <value>The starting index into the item variation store for the scale deltas.</value>
    /// <seealso cref="Scale"/>
    public uint VarIndexBase { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarScaleUniform</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Var{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarScaleUniform</c></seealso>
    static PaintVarScaleUniform IDerivedRecord<Paint, PaintVarScaleUniform>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Var<PaintScaleUniform.Header>>();
        return new PaintVarScaleUniform
        {
            Format = 21,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.Value.PaintOffset),
            Scale = header.Value.Scale,
            VarIndexBase = header.VarIndexBase,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 22 — uniform scale around a center
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 22: uniform scale around a center.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Scales the sub-graph reached through <see cref="Paint"/> around an explicit <c>(CenterX, CenterY)</c> point rather than the origin.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintScaleUniformAroundCenter</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintScaleUniform"/>
/// <seealso cref="PaintVarScaleUniformAroundCenter"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintScaleUniformAroundCenter</c></seealso>
public sealed record PaintScaleUniformAroundCenter : Paint, IDerivedRecord<Paint, PaintScaleUniformAroundCenter>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is scaled.</value>
    /// <seealso cref="Scale"/>
    /// <seealso cref="CenterX"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the uniform scale factor.</summary>
    /// <value>The scale applied along both axes, as an F2DOT14 value.</value>
    /// <seealso cref="CenterX"/>
    /// <seealso cref="CenterY"/>
    public F2Dot14 Scale { get; init; }

    /// <summary>Gets the center x coordinate.</summary>
    /// <value>The x coordinate of the point about which the scale is applied.</value>
    /// <seealso cref="Scale"/>
    /// <seealso cref="CenterY"/>
    public short CenterX { get; init; }

    /// <summary>Gets the center y coordinate.</summary>
    /// <value>The y coordinate of the point about which the scale is applied.</value>
    /// <seealso cref="Scale"/>
    /// <seealso cref="CenterX"/>
    public short CenterY { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintScaleUniformAroundCenter</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintScaleUniformAroundCenter</c></seealso>
    static PaintScaleUniformAroundCenter IDerivedRecord<Paint, PaintScaleUniformAroundCenter>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new PaintScaleUniformAroundCenter
        {
            Format = 22,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.PaintOffset),
            Scale = header.Scale,
            CenterX = header.CenterX,
            CenterY = header.CenterY,
        };
    }

    /// <summary>The <c>PaintScaleUniformAroundCenter</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>PaintOffset</c> is a <see cref="UInt24"/> measured from the start of this paint record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintScaleUniformAroundCenter</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintScaleUniformAroundCenter"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintScaleUniformAroundCenter</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Offset from the start of this paint record to the sub-graph.</summary>
        /// <value>The byte offset of the <see cref="Paint"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>paintOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Scale"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>paintOffset</c></seealso>
        public UInt24 PaintOffset;

        /// <summary>Uniform scale factor.</summary>
        /// <value>The scale applied along both axes, as an F2DOT14 value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>scale</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CenterX"/>
        /// <seealso cref="F2Dot14"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>scale</c></seealso>
        public F2Dot14 Scale;

        /// <summary>Center x.</summary>
        /// <value>The x coordinate of the point about which the scale is applied.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>centerX</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CenterY"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>centerX</c></seealso>
        public short CenterX;

        /// <summary>Center y.</summary>
        /// <value>The y coordinate of the point about which the scale is applied.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>centerY</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CenterX"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>centerY</c></seealso>
        public short CenterY;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>Uses <see cref="UInt24.ReverseEndianness(UInt24)"/>, <see cref="F2Dot14.ReverseEndianness(F2Dot14)"/>, and <see cref="BinaryPrimitives.ReverseEndianness(short)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            PaintOffset = UInt24.ReverseEndianness(value.PaintOffset),
            Scale = F2Dot14.ReverseEndianness(value.Scale),
            CenterX = BinaryPrimitives.ReverseEndianness(value.CenterX),
            CenterY = BinaryPrimitives.ReverseEndianness(value.CenterY),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 23 — variable uniform scale around a center
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 23: variable uniform scale around a center.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Same as <see cref="PaintScaleUniformAroundCenter"/>, with a variation index base for the scale and center.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarScaleUniformAroundCenter</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintScaleUniformAroundCenter"/>
/// <seealso cref="Var{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarScaleUniformAroundCenter</c></seealso>
public sealed record PaintVarScaleUniformAroundCenter : Paint, IDerivedRecord<Paint, PaintVarScaleUniformAroundCenter>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is scaled.</value>
    /// <seealso cref="Scale"/>
    /// <seealso cref="CenterX"/>
    /// <seealso cref="VarIndexBase"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the uniform scale factor.</summary>
    /// <value>The base scale along both axes, before variation deltas.</value>
    /// <seealso cref="CenterX"/>
    /// <seealso cref="VarIndexBase"/>
    public F2Dot14 Scale { get; init; }

    /// <summary>Gets the center x coordinate.</summary>
    /// <value>The base x coordinate of the point about which the scale is applied, before variation deltas.</value>
    /// <seealso cref="CenterY"/>
    /// <seealso cref="VarIndexBase"/>
    public short CenterX { get; init; }

    /// <summary>Gets the center y coordinate.</summary>
    /// <value>The base y coordinate of the point about which the scale is applied, before variation deltas.</value>
    /// <seealso cref="CenterX"/>
    /// <seealso cref="VarIndexBase"/>
    public short CenterY { get; init; }

    /// <summary>Gets the variation index base.</summary>
    /// <value>The starting index into the item variation store for the scale and center deltas.</value>
    /// <seealso cref="Scale"/>
    /// <seealso cref="CenterX"/>
    public uint VarIndexBase { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarScaleUniformAroundCenter</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Var{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarScaleUniformAroundCenter</c></seealso>
    static PaintVarScaleUniformAroundCenter IDerivedRecord<Paint, PaintVarScaleUniformAroundCenter>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Var<PaintScaleUniformAroundCenter.Header>>();
        return new PaintVarScaleUniformAroundCenter
        {
            Format = 23,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.Value.PaintOffset),
            Scale = header.Value.Scale,
            CenterX = header.Value.CenterX,
            CenterY = header.Value.CenterY,
            VarIndexBase = header.VarIndexBase,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 24 — rotate
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 24: rotate.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Rotates the coordinate system of the sub-graph reached through <see cref="Paint"/> by <see cref="Angle"/> about the origin.</description></item>
/// <item><description>Use <see cref="PaintRotateAroundCenter"/> to rotate around an arbitrary point.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintRotate</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintVarRotate"/>
/// <seealso cref="PaintRotateAroundCenter"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintRotate</c></seealso>
public sealed record PaintRotate : Paint, IDerivedRecord<Paint, PaintRotate>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is rotated.</value>
    /// <seealso cref="Angle"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the rotation angle.</summary>
    /// <value>The counter-clockwise rotation, in degrees, as an F2DOT14 value.</value>
    /// <seealso cref="Paint"/>
    public F2Dot14 Angle { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintRotate</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintRotate</c></seealso>
    static PaintRotate IDerivedRecord<Paint, PaintRotate>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new PaintRotate
        {
            Format = 24,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.PaintOffset),
            Angle = header.Angle,
        };
    }

    /// <summary>The <c>PaintRotate</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>PaintOffset</c> is a <see cref="UInt24"/> measured from the start of this paint record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintRotate</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintRotate"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintRotate</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Offset from the start of this paint record to the sub-graph.</summary>
        /// <value>The byte offset of the <see cref="Paint"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>paintOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Angle"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>paintOffset</c></seealso>
        public UInt24 PaintOffset;

        /// <summary>Rotation angle.</summary>
        /// <value>The counter-clockwise rotation, in degrees, as an F2DOT14 value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>angle</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="PaintOffset"/>
        /// <seealso cref="F2Dot14"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>angle</c></seealso>
        public F2Dot14 Angle;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Uses <see cref="UInt24.ReverseEndianness(UInt24)"/> and <see cref="F2Dot14.ReverseEndianness(F2Dot14)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            PaintOffset = UInt24.ReverseEndianness(value.PaintOffset),
            Angle = F2Dot14.ReverseEndianness(value.Angle),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 25 — variable rotate
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 25: variable rotate.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Same as <see cref="PaintRotate"/>, with a variation index base for the rotation angle.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarRotate</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintRotate"/>
/// <seealso cref="Var{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarRotate</c></seealso>
public sealed record PaintVarRotate : Paint, IDerivedRecord<Paint, PaintVarRotate>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is rotated.</value>
    /// <seealso cref="Angle"/>
    /// <seealso cref="VarIndexBase"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the rotation angle.</summary>
    /// <value>The base counter-clockwise rotation, before variation deltas.</value>
    /// <seealso cref="Paint"/>
    /// <seealso cref="VarIndexBase"/>
    public F2Dot14 Angle { get; init; }

    /// <summary>Gets the variation index base.</summary>
    /// <value>The starting index into the item variation store for the rotation deltas.</value>
    /// <seealso cref="Angle"/>
    public uint VarIndexBase { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarRotate</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Var{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarRotate</c></seealso>
    static PaintVarRotate IDerivedRecord<Paint, PaintVarRotate>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Var<PaintRotate.Header>>();
        return new PaintVarRotate
        {
            Format = 25,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.Value.PaintOffset),
            Angle = header.Value.Angle,
            VarIndexBase = header.VarIndexBase,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 26 — rotate around a center
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 26: rotate around a center.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Rotates the sub-graph reached through <see cref="Paint"/> around an explicit <c>(CenterX, CenterY)</c> point rather than the origin.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintRotateAroundCenter</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintRotate"/>
/// <seealso cref="PaintVarRotateAroundCenter"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintRotateAroundCenter</c></seealso>
public sealed record PaintRotateAroundCenter : Paint, IDerivedRecord<Paint, PaintRotateAroundCenter>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is rotated.</value>
    /// <seealso cref="Angle"/>
    /// <seealso cref="CenterX"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the rotation angle.</summary>
    /// <value>The counter-clockwise rotation, in degrees, as an F2DOT14 value.</value>
    /// <seealso cref="Paint"/>
    /// <seealso cref="CenterX"/>
    public F2Dot14 Angle { get; init; }

    /// <summary>Gets the center x coordinate.</summary>
    /// <value>The x coordinate of the point about which the rotation is applied.</value>
    /// <seealso cref="Angle"/>
    /// <seealso cref="CenterY"/>
    public short CenterX { get; init; }

    /// <summary>Gets the center y coordinate.</summary>
    /// <value>The y coordinate of the point about which the rotation is applied.</value>
    /// <seealso cref="Angle"/>
    /// <seealso cref="CenterX"/>
    public short CenterY { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintRotateAroundCenter</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintRotateAroundCenter</c></seealso>
    static PaintRotateAroundCenter IDerivedRecord<Paint, PaintRotateAroundCenter>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new PaintRotateAroundCenter
        {
            Format = 26,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.PaintOffset),
            Angle = header.Angle,
            CenterX = header.CenterX,
            CenterY = header.CenterY,
        };
    }

    /// <summary>The <c>PaintRotateAroundCenter</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>PaintOffset</c> is a <see cref="UInt24"/> measured from the start of this paint record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintRotateAroundCenter</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintRotateAroundCenter"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintRotateAroundCenter</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Offset from the start of this paint record to the sub-graph.</summary>
        /// <value>The byte offset of the <see cref="Paint"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>paintOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Angle"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>paintOffset</c></seealso>
        public UInt24 PaintOffset;

        /// <summary>Rotation angle.</summary>
        /// <value>The counter-clockwise rotation, in degrees, as an F2DOT14 value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>angle</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CenterX"/>
        /// <seealso cref="F2Dot14"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>angle</c></seealso>
        public F2Dot14 Angle;

        /// <summary>Center x.</summary>
        /// <value>The x coordinate of the point about which the rotation is applied.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>centerX</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CenterY"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>centerX</c></seealso>
        public short CenterX;

        /// <summary>Center y.</summary>
        /// <value>The y coordinate of the point about which the rotation is applied.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>centerY</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CenterX"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>centerY</c></seealso>
        public short CenterY;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>Uses <see cref="UInt24.ReverseEndianness(UInt24)"/>, <see cref="F2Dot14.ReverseEndianness(F2Dot14)"/>, and <see cref="BinaryPrimitives.ReverseEndianness(short)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            PaintOffset = UInt24.ReverseEndianness(value.PaintOffset),
            Angle = F2Dot14.ReverseEndianness(value.Angle),
            CenterX = BinaryPrimitives.ReverseEndianness(value.CenterX),
            CenterY = BinaryPrimitives.ReverseEndianness(value.CenterY),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 27 — variable rotate around a center
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 27: variable rotate around a center.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Same as <see cref="PaintRotateAroundCenter"/>, with a variation index base for the angle and center.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarRotateAroundCenter</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintRotateAroundCenter"/>
/// <seealso cref="Var{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarRotateAroundCenter</c></seealso>
public sealed record PaintVarRotateAroundCenter : Paint, IDerivedRecord<Paint, PaintVarRotateAroundCenter>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is rotated.</value>
    /// <seealso cref="Angle"/>
    /// <seealso cref="VarIndexBase"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the rotation angle.</summary>
    /// <value>The base counter-clockwise rotation, before variation deltas.</value>
    /// <seealso cref="CenterX"/>
    /// <seealso cref="VarIndexBase"/>
    public F2Dot14 Angle { get; init; }

    /// <summary>Gets the center x coordinate.</summary>
    /// <value>The base x coordinate of the point about which the rotation is applied, before variation deltas.</value>
    /// <seealso cref="CenterY"/>
    /// <seealso cref="VarIndexBase"/>
    public short CenterX { get; init; }

    /// <summary>Gets the center y coordinate.</summary>
    /// <value>The base y coordinate of the point about which the rotation is applied, before variation deltas.</value>
    /// <seealso cref="CenterX"/>
    /// <seealso cref="VarIndexBase"/>
    public short CenterY { get; init; }

    /// <summary>Gets the variation index base.</summary>
    /// <value>The starting index into the item variation store for the angle and center deltas.</value>
    /// <seealso cref="Angle"/>
    /// <seealso cref="CenterX"/>
    public uint VarIndexBase { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarRotateAroundCenter</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Var{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarRotateAroundCenter</c></seealso>
    static PaintVarRotateAroundCenter IDerivedRecord<Paint, PaintVarRotateAroundCenter>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Var<PaintRotateAroundCenter.Header>>();
        return new PaintVarRotateAroundCenter
        {
            Format = 27,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.Value.PaintOffset),
            Angle = header.Value.Angle,
            CenterX = header.Value.CenterX,
            CenterY = header.Value.CenterY,
            VarIndexBase = header.VarIndexBase,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 28 — skew
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 28: skew.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Applies an independent x and y skew to the coordinate system of the sub-graph reached through <see cref="Paint"/>, about the origin.</description></item>
/// <item><description>Use <see cref="PaintSkewAroundCenter"/> to skew around an arbitrary point.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintSkew</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintVarSkew"/>
/// <seealso cref="PaintSkewAroundCenter"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintSkew</c></seealso>
public sealed record PaintSkew : Paint, IDerivedRecord<Paint, PaintSkew>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is skewed.</value>
    /// <seealso cref="XSkewAngle"/>
    /// <seealso cref="YSkewAngle"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the x skew angle.</summary>
    /// <value>The skew along the x axis, in degrees, as an F2DOT14 value.</value>
    /// <seealso cref="Paint"/>
    /// <seealso cref="YSkewAngle"/>
    public F2Dot14 XSkewAngle { get; init; }

    /// <summary>Gets the y skew angle.</summary>
    /// <value>The skew along the y axis, in degrees, as an F2DOT14 value.</value>
    /// <seealso cref="Paint"/>
    /// <seealso cref="XSkewAngle"/>
    public F2Dot14 YSkewAngle { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintSkew</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintSkew</c></seealso>
    static PaintSkew IDerivedRecord<Paint, PaintSkew>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new PaintSkew
        {
            Format = 28,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.PaintOffset),
            XSkewAngle = header.XSkewAngle,
            YSkewAngle = header.YSkewAngle,
        };
    }

    /// <summary>The <c>PaintSkew</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>PaintOffset</c> is a <see cref="UInt24"/> measured from the start of this paint record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintSkew</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintSkew"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintSkew</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Offset from the start of this paint record to the sub-graph.</summary>
        /// <value>The byte offset of the <see cref="Paint"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>paintOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="XSkewAngle"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>paintOffset</c></seealso>
        public UInt24 PaintOffset;

        /// <summary>X skew angle.</summary>
        /// <value>The skew along the x axis, in degrees, as an F2DOT14 value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>xSkewAngle</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="YSkewAngle"/>
        /// <seealso cref="F2Dot14"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>xSkewAngle</c></seealso>
        public F2Dot14 XSkewAngle;

        /// <summary>Y skew angle.</summary>
        /// <value>The skew along the y axis, in degrees, as an F2DOT14 value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>ySkewAngle</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="XSkewAngle"/>
        /// <seealso cref="F2Dot14"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>ySkewAngle</c></seealso>
        public F2Dot14 YSkewAngle;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>Uses <see cref="UInt24.ReverseEndianness(UInt24)"/> and <see cref="F2Dot14.ReverseEndianness(F2Dot14)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            PaintOffset = UInt24.ReverseEndianness(value.PaintOffset),
            XSkewAngle = F2Dot14.ReverseEndianness(value.XSkewAngle),
            YSkewAngle = F2Dot14.ReverseEndianness(value.YSkewAngle),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 29 — variable skew
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 29: variable skew.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Same as <see cref="PaintSkew"/>, with a variation index base for the skew angles.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarSkew</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintSkew"/>
/// <seealso cref="Var{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarSkew</c></seealso>
public sealed record PaintVarSkew : Paint, IDerivedRecord<Paint, PaintVarSkew>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is skewed.</value>
    /// <seealso cref="XSkewAngle"/>
    /// <seealso cref="VarIndexBase"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the x skew angle.</summary>
    /// <value>The base skew along the x axis, before variation deltas.</value>
    /// <seealso cref="YSkewAngle"/>
    /// <seealso cref="VarIndexBase"/>
    public F2Dot14 XSkewAngle { get; init; }

    /// <summary>Gets the y skew angle.</summary>
    /// <value>The base skew along the y axis, before variation deltas.</value>
    /// <seealso cref="XSkewAngle"/>
    /// <seealso cref="VarIndexBase"/>
    public F2Dot14 YSkewAngle { get; init; }

    /// <summary>Gets the variation index base.</summary>
    /// <value>The starting index into the item variation store for the skew deltas.</value>
    /// <seealso cref="XSkewAngle"/>
    /// <seealso cref="YSkewAngle"/>
    public uint VarIndexBase { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarSkew</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Var{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarSkew</c></seealso>
    static PaintVarSkew IDerivedRecord<Paint, PaintVarSkew>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Var<PaintSkew.Header>>();
        return new PaintVarSkew
        {
            Format = 29,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.Value.PaintOffset),
            XSkewAngle = header.Value.XSkewAngle,
            YSkewAngle = header.Value.YSkewAngle,
            VarIndexBase = header.VarIndexBase,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 30 — skew around a center
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 30: skew around a center.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Applies an independent x and y skew to the sub-graph reached through <see cref="Paint"/>, around an explicit <c>(CenterX, CenterY)</c> point.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintSkewAroundCenter</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintSkew"/>
/// <seealso cref="PaintVarSkewAroundCenter"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintSkewAroundCenter</c></seealso>
public sealed record PaintSkewAroundCenter : Paint, IDerivedRecord<Paint, PaintSkewAroundCenter>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is skewed.</value>
    /// <seealso cref="XSkewAngle"/>
    /// <seealso cref="CenterX"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the x skew angle.</summary>
    /// <value>The skew along the x axis, in degrees, as an F2DOT14 value.</value>
    /// <seealso cref="YSkewAngle"/>
    /// <seealso cref="CenterX"/>
    public F2Dot14 XSkewAngle { get; init; }

    /// <summary>Gets the y skew angle.</summary>
    /// <value>The skew along the y axis, in degrees, as an F2DOT14 value.</value>
    /// <seealso cref="XSkewAngle"/>
    /// <seealso cref="CenterY"/>
    public F2Dot14 YSkewAngle { get; init; }

    /// <summary>Gets the center x coordinate.</summary>
    /// <value>The x coordinate of the point about which the skew is applied.</value>
    /// <seealso cref="YSkewAngle"/>
    /// <seealso cref="CenterY"/>
    public short CenterX { get; init; }

    /// <summary>Gets the center y coordinate.</summary>
    /// <value>The y coordinate of the point about which the skew is applied.</value>
    /// <seealso cref="XSkewAngle"/>
    /// <seealso cref="CenterX"/>
    public short CenterY { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintSkewAroundCenter</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintSkewAroundCenter</c></seealso>
    static PaintSkewAroundCenter IDerivedRecord<Paint, PaintSkewAroundCenter>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new PaintSkewAroundCenter
        {
            Format = 30,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.PaintOffset),
            XSkewAngle = header.XSkewAngle,
            YSkewAngle = header.YSkewAngle,
            CenterX = header.CenterX,
            CenterY = header.CenterY,
        };
    }

    /// <summary>The <c>PaintSkewAroundCenter</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>PaintOffset</c> is a <see cref="UInt24"/> measured from the start of this paint record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintSkewAroundCenter</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintSkewAroundCenter"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintSkewAroundCenter</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Offset from the start of this paint record to the sub-graph.</summary>
        /// <value>The byte offset of the <see cref="Paint"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>paintOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="XSkewAngle"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>paintOffset</c></seealso>
        public UInt24 PaintOffset;

        /// <summary>X skew angle.</summary>
        /// <value>The skew along the x axis, in degrees, as an F2DOT14 value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>xSkewAngle</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="YSkewAngle"/>
        /// <seealso cref="F2Dot14"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>xSkewAngle</c></seealso>
        public F2Dot14 XSkewAngle;

        /// <summary>Y skew angle.</summary>
        /// <value>The skew along the y axis, in degrees, as an F2DOT14 value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>ySkewAngle</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="XSkewAngle"/>
        /// <seealso cref="F2Dot14"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>ySkewAngle</c></seealso>
        public F2Dot14 YSkewAngle;

        /// <summary>Center x.</summary>
        /// <value>The x coordinate of the point about which the skew is applied.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>centerX</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CenterY"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>centerX</c></seealso>
        public short CenterX;

        /// <summary>Center y.</summary>
        /// <value>The y coordinate of the point about which the skew is applied.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>centerY</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CenterX"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>centerY</c></seealso>
        public short CenterY;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>Uses <see cref="UInt24.ReverseEndianness(UInt24)"/>, <see cref="F2Dot14.ReverseEndianness(F2Dot14)"/>, and <see cref="BinaryPrimitives.ReverseEndianness(short)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            PaintOffset = UInt24.ReverseEndianness(value.PaintOffset),
            XSkewAngle = F2Dot14.ReverseEndianness(value.XSkewAngle),
            YSkewAngle = F2Dot14.ReverseEndianness(value.YSkewAngle),
            CenterX = BinaryPrimitives.ReverseEndianness(value.CenterX),
            CenterY = BinaryPrimitives.ReverseEndianness(value.CenterY),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 31 — variable skew around a center
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 31: variable skew around a center.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Same as <see cref="PaintSkewAroundCenter"/>, with a variation index base for the skew angles and center.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarSkewAroundCenter</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="PaintSkewAroundCenter"/>
/// <seealso cref="Var{T}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarSkewAroundCenter</c></seealso>
public sealed record PaintVarSkewAroundCenter : Paint, IDerivedRecord<Paint, PaintVarSkewAroundCenter>
{
    /// <summary>Gets the sub-graph.</summary>
    /// <value>The paint whose coordinate system is skewed.</value>
    /// <seealso cref="XSkewAngle"/>
    /// <seealso cref="VarIndexBase"/>
    public Paint Paint { get; init; } = null!;

    /// <summary>Gets the x skew angle.</summary>
    /// <value>The base skew along the x axis, before variation deltas.</value>
    /// <seealso cref="YSkewAngle"/>
    /// <seealso cref="VarIndexBase"/>
    public F2Dot14 XSkewAngle { get; init; }

    /// <summary>Gets the y skew angle.</summary>
    /// <value>The base skew along the y axis, before variation deltas.</value>
    /// <seealso cref="XSkewAngle"/>
    /// <seealso cref="VarIndexBase"/>
    public F2Dot14 YSkewAngle { get; init; }

    /// <summary>Gets the center x coordinate.</summary>
    /// <value>The base x coordinate of the point about which the skew is applied, before variation deltas.</value>
    /// <seealso cref="CenterY"/>
    /// <seealso cref="VarIndexBase"/>
    public short CenterX { get; init; }

    /// <summary>Gets the center y coordinate.</summary>
    /// <value>The base y coordinate of the point about which the skew is applied, before variation deltas.</value>
    /// <seealso cref="CenterX"/>
    /// <seealso cref="VarIndexBase"/>
    public short CenterY { get; init; }

    /// <summary>Gets the variation index base.</summary>
    /// <value>The starting index into the item variation store for the skew and center deltas.</value>
    /// <seealso cref="XSkewAngle"/>
    /// <seealso cref="CenterX"/>
    public uint VarIndexBase { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or the referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintVarSkewAroundCenter</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Var{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintVarSkewAroundCenter</c></seealso>
    static PaintVarSkewAroundCenter IDerivedRecord<Paint, PaintVarSkewAroundCenter>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Var<PaintSkewAroundCenter.Header>>();
        return new PaintVarSkewAroundCenter
        {
            Format = 31,
            Paint = cursor.Source.ParseRecordAt<Paint>(header.Value.PaintOffset),
            XSkewAngle = header.Value.XSkewAngle,
            YSkewAngle = header.Value.YSkewAngle,
            CenterX = header.Value.CenterX,
            CenterY = header.Value.CenterY,
            VarIndexBase = header.VarIndexBase,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 32 — composite
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Paint format 32: composite of two sub-graphs with a blending mode.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Composites a source sub-graph with a backdrop sub-graph using the Porter-Duff or blend mode selected by <see cref="CompositeMode"/>.</description></item>
/// <item><description>The source is composited onto the backdrop, matching the standard "source over destination" convention; the specific operation is determined by the mode.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintComposite</c></see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Paint"/>
/// <seealso cref="CompositeMode"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintComposite</c></seealso>
public sealed record PaintComposite : Paint, IDerivedRecord<Paint, PaintComposite>
{
    /// <summary>Gets the source sub-graph.</summary>
    /// <value>The paint whose result is composited onto the backdrop.</value>
    /// <seealso cref="CompositeMode"/>
    /// <seealso cref="BackdropPaint"/>
    public Paint SourcePaint { get; init; } = null!;

    /// <summary>Gets the composite mode (Porter-Duff or blend).</summary>
    /// <value>The <see cref="Tables.CompositeMode"/> that defines how source and backdrop combine.</value>
    /// <seealso cref="SourcePaint"/>
    /// <seealso cref="BackdropPaint"/>
    /// <seealso cref="CompositeMode"/>
    public CompositeMode CompositeMode { get; init; }

    /// <summary>Gets the backdrop sub-graph.</summary>
    /// <value>The paint that provides the backdrop for the composite operation.</value>
    /// <seealso cref="SourcePaint"/>
    /// <seealso cref="CompositeMode"/>
    public Paint BackdropPaint { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the paint record.</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed paint.</returns>
    /// <exception cref="EndOfStreamException">The header or either referenced paint extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintComposite</c></see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="CompositeMode"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintComposite</c></seealso>
    static PaintComposite IDerivedRecord<Paint, PaintComposite>.Parse(ref Cursor cursor, object? context)
    {
        var header = cursor.ReadBigEndianStruct<Header>();
        return new PaintComposite
        {
            Format = 32,
            SourcePaint = cursor.Source.ParseRecordAt<Paint>(header.SourceOffset),
            CompositeMode = header.CompositeMode,
            BackdropPaint = cursor.Source.ParseRecordAt<Paint>(header.BackdropOffset),
        };
    }

    /// <summary>The <c>PaintComposite</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both offsets are <see cref="UInt24"/> values measured from the start of this paint record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">COLR v1 <c>PaintComposite</c></see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PaintComposite"/>
    /// <seealso cref="CompositeMode"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>PaintComposite</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Offset from the start of this paint record to the source sub-graph.</summary>
        /// <value>The byte offset of the source <see cref="Paint"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>sourcePaintOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="CompositeMode"/>
        /// <seealso cref="BackdropOffset"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>sourcePaintOffset</c></seealso>
        public UInt24 SourceOffset;

        /// <summary>Composite mode (Porter-Duff or blend).</summary>
        /// <value>The <see cref="Tables.CompositeMode"/> that defines how source and backdrop combine.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>compositeMode</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="SourceOffset"/>
        /// <seealso cref="BackdropOffset"/>
        /// <seealso cref="CompositeMode"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>compositeMode</c></seealso>
        public CompositeMode CompositeMode;

        /// <summary>Offset from the start of this paint record to the backdrop sub-graph.</summary>
        /// <value>The byte offset of the backdrop <see cref="Paint"/>, encoded as a 24-bit unsigned value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"><c>backdropPaintOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="SourceOffset"/>
        /// <seealso cref="CompositeMode"/>
        /// <seealso cref="UInt24"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr">OpenType specification: <c>backdropPaintOffset</c></seealso>
        public UInt24 BackdropOffset;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with the two offsets reversed and the byte-width composite mode copied through.</returns>
        /// <remarks>The <c>CompositeMode</c> field is a single byte and does not participate in the reversal.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            SourceOffset = UInt24.ReverseEndianness(value.SourceOffset),
            CompositeMode = value.CompositeMode,
            BackdropOffset = UInt24.ReverseEndianness(value.BackdropOffset),
        };
    }
}
