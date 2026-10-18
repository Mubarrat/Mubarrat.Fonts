using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>bsln</c> table: the Apple Advanced Typography baseline table. Allows AAT fonts to align text runs to different baselines.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Every glyph in a font has a natural baseline defined by its position in the em square. The <c>bsln</c> table adds named baselines — Roman, ideographic, hanging, math — and, per glyph, identifies which of them is that glyph's default baseline.</description></item>
/// <item><description>Two families of baseline data are defined. Formats 0 and 1 store baseline positions as <c>int16</c> FUnit deltas from the font's natural baseline; formats 2 and 3 store them as control point indices within a designated standard glyph, which survive hinting better at small point sizes. A font declares exactly one family.</description></item>
/// <item><description>Formats 1 and 3 additionally carry a lookup table that maps individual glyphs onto their dominant baseline. Glyphs the lookup does not cover use the table-wide <see cref="DefaultBaseline"/>.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">baseline table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BslnFormat0"/>
/// <seealso cref="BslnFormat1"/>
/// <seealso cref="BslnFormat2"/>
/// <seealso cref="BslnFormat3"/>
/// <seealso cref="BaselineValue"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
public abstract record BslnTable : IRecord<BslnTable>, IBaseRecord<BslnTable>
{
    /// <summary>The number of baseline position entries every format carries.</summary>
    /// <value>The constant <c>32</c>.</value>
    /// <remarks>The baseline array in every format has exactly thirty-two entries, one per possible baseline value. Entries for baselines the font does not use are stored but ignored.</remarks>
    public const int BaselineCount = 32;

    /// <summary>The sentinel control point index that marks a baseline with no control point in formats 2 and 3.</summary>
    /// <value>The constant <c>0xFFFF</c>.</value>
    /// <seealso cref="BslnFormat2.BaselineControlPoints"/>
    /// <seealso cref="BslnFormat3.BaselineControlPoints"/>
    public const ushort NoControlPoint = 0xFFFF;

    /// <summary>The highest baseline value the specification defines.</summary>
    /// <value>The constant <c>31</c>.</value>
    /// <remarks>Values 0 through 4 are named baselines; 5 through 31 are reserved for future use. A value above 31 is not a valid baseline.</remarks>
    public const ushort MaxBaselineValue = 31;

    /// <summary>The version value for the only defined baseline table format, 1.0.</summary>
    /// <value>The <see cref="Fixed"/> value whose raw bits are <c>0x00010000</c>.</value>
    /// <seealso cref="Version"/>
    public static readonly Fixed CurrentVersion = new(0x00010000);

    /// <inheritdoc/>
    public static Tag Tag => "bsln";

    /// <summary>Gets the table version.</summary>
    /// <value>The <see cref="CurrentVersion"/> value for a conforming table.</value>
    /// <seealso cref="CurrentVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the table-wide default baseline value.</summary>
    /// <value>A <see cref="BaselineValue"/> in the range 0 through 31. Values 0 through 4 are the named baselines; 5 through 31 are reserved.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>For formats 0 and 2, the value applies to every glyph in the font.</description></item>
    /// <item><description>For formats 1 and 3, the value applies to every glyph not covered by the mapping lookup; glyphs the lookup covers use the mapped baseline.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BaselineValue"/>
    /// <seealso cref="GetBaseline(int)"/>
    public BaselineValue DefaultBaseline { get; init; }

    /// <summary>Gets the format number that produced this baseline table.</summary>
    /// <value>One of 0, 1, 2, or 3.</value>
    /// <seealso cref="BslnFormat0"/>
    /// <seealso cref="BslnFormat1"/>
    /// <seealso cref="BslnFormat2"/>
    /// <seealso cref="BslnFormat3"/>
    public abstract ushort Format { get; }

    /// <summary>Returns the baseline value for a glyph.</summary>
    /// <param name="glyphIndex">The glyph index to classify.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><description>For formats 0 and 2, <see cref="DefaultBaseline"/>.</description></item>
    /// <item><description>For formats 1 and 3, the mapped baseline when the lookup covers the glyph; otherwise <see cref="DefaultBaseline"/>.</description></item>
    /// </list>
    /// </returns>
    /// <seealso cref="DefaultBaseline"/>
    public abstract BaselineValue GetBaseline(int glyphIndex);

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the <c>bsln</c> table.</param>
    /// <param name="context">Unused. The table is self-contained.</param>
    /// <returns>The format-specific baseline table.</returns>
    /// <exception cref="InvalidDataException">The version is not 1.0, the format is not one of 0, 1, 2, or 3, or the default baseline value exceeds <see cref="MaxBaselineValue"/>.</exception>
    /// <exception cref="EndOfStreamException">The header or the format-specific data extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The dispatcher reads the whole eight-byte header and forwards the version and default baseline through a context, then routes on the format word to the matching derived parser.</description></item>
    /// <item><description>The derived parsers begin after the header, at the format-specific part.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="BslnFormatContext"/>
    static BslnTable IRecord<BslnTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
            throw new InvalidDataException($"'bsln'.version is 0x{header.Version.Bits:X8}, expected 0x{CurrentVersion.Bits:X8}.");

        if (header.DefaultBaseline > MaxBaselineValue)
            throw new InvalidDataException($"'bsln'.defaultBaseline is {header.DefaultBaseline}, expected 0 through {MaxBaselineValue}.");

        var formatContext = new BslnFormatContext(header.Version, (BaselineValue)header.DefaultBaseline);

        return header.Format switch
        {
            0 => IBaseRecord<BslnTable>.Parse<BslnFormat0>(ref cursor, formatContext),
            1 => IBaseRecord<BslnTable>.Parse<BslnFormat1>(ref cursor, formatContext),
            2 => IBaseRecord<BslnTable>.Parse<BslnFormat2>(ref cursor, formatContext),
            3 => IBaseRecord<BslnTable>.Parse<BslnFormat3>(ref cursor, formatContext),
            _ => throw new InvalidDataException(
                $"'bsln'.format is {header.Format}, expected 0, 1, 2, or 3."),
        };
    }

    /// <summary>The 8-byte <c>bsln</c> table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed by the format-specific part, whose layout is determined by <see cref="Format"/>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">baseline table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BslnTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0, as a <c>Fixed</c> (16.16) value.</summary>
        /// <value>The <see cref="CurrentVersion"/> value for a conforming table.</value>
        public Fixed Version;         // +0

        /// <summary>The format number at byte offset 4.</summary>
        /// <value>One of 0, 1, 2, or 3.</value>
        public ushort Format;         // +4

        /// <summary>The table-wide default baseline value at byte offset 6.</summary>
        /// <value>A value in the range 0 through 31.</value>
        public ushort DefaultBaseline; // +6

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = Fixed.ReverseEndianness(value.Version),
            Format = BinaryPrimitives.ReverseEndianness(value.Format),
            DefaultBaseline = BinaryPrimitives.ReverseEndianness(value.DefaultBaseline),
        };
    }
}

/// <summary>The predefined baseline values defined by the <c>bsln</c> table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Values 0 through 4 are the named baselines. Values 5 through 31 are reserved for future use; a font may store them but their meaning is undefined.</description></item>
/// <item><description>Every horizontal AAT font should define values 0 through 4.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">baseline table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BslnTable.DefaultBaseline"/>
/// <seealso cref="BslnTable.GetBaseline(int)"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
public enum BaselineValue : ushort
{
    /// <summary>The Roman baseline, value 0.</summary>
    /// <value>The constant <c>0</c>.</value>
    /// <remarks>Defines the alignment used by most Latin-script languages. The baseline appears near the bottom of the line, with descenders extending below it.</remarks>
    Roman = 0,

    /// <summary>The ideographic centered baseline, value 1.</summary>
    /// <value>The constant <c>1</c>.</value>
    /// <remarks>Defines the behavior used by Chinese, Japanese, and Korean ideographic scripts, which center themselves halfway through the line height.</remarks>
    IdeographicCentered = 1,

    /// <summary>The ideographic low baseline, value 2.</summary>
    /// <value>The constant <c>2</c>.</value>
    /// <remarks>Like <see cref="IdeographicCentered"/>, but with the glyphs lowered slightly so that ideographs adjacent to Latin characters descend below the Roman baseline.</remarks>
    IdeographicLow = 2,

    /// <summary>The hanging baseline, value 3.</summary>
    /// <value>The constant <c>3</c>.</value>
    /// <remarks>Defines the alignment used by Devanagari and derived scripts. Most of the glyph is below the baseline. Also used for drop capitals.</remarks>
    Hanging = 3,

    /// <summary>The math baseline, value 4.</summary>
    /// <value>The constant <c>4</c>.</value>
    /// <remarks>Defines the alignment used for setting mathematics, where operators like the minus sign need to be centered. Usually set at half the x-height.</remarks>
    Math = 4,
}

/// <summary>Context threaded from the <c>bsln</c> table dispatcher into each format parser, carrying the header fields the format does not re-read.</summary>
/// <param name="Version">The table version from the header.</param>
/// <param name="DefaultBaseline">The table-wide default baseline from the header.</param>
/// <remarks>
/// <list type="bullet">
/// <item><description>The dispatcher reads the whole eight-byte header before routing, so the derived parsers do not re-read <see cref="BslnTable.Version"/> or <see cref="BslnTable.DefaultBaseline"/>.</description></item>
/// <item><description>The format discriminant is not carried in the context because each derived type declares its own via <see cref="BslnTable.Format"/>.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BslnTable"/>
public record BslnFormatContext(Fixed Version, BaselineValue DefaultBaseline);

/// <summary>A format 0 <c>bsln</c> table: distance-based baseline positions with no per-glyph mapping.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The format stores thirty-two <c>int16</c> FUnit deltas from the font's natural baseline to each possible baseline position.</description></item>
/// <item><description>Every glyph in the font uses <see cref="BslnTable.DefaultBaseline"/>; there is no per-glyph variation.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">baseline table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BslnTable"/>
/// <seealso cref="BslnFormat1"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
public sealed record BslnFormat0 : BslnTable, IDerivedRecord<BslnTable, BslnFormat0>
{
    /// <summary>Gets the baseline position deltas in FUnits.</summary>
    /// <value>An array of <see cref="BslnTable.BaselineCount"/> <c>int16</c> deltas, one per possible baseline value. Index <c>i</c> is the delta for <c>(BaselineValue)i</c>.</value>
    /// <remarks>A delta is the signed distance from the font's natural baseline to the named baseline, measured in FUnits.</remarks>
    /// <seealso cref="BaselineValue"/>
    public required IReadOnlyList<short> BaselineDeltas { get; init; }

    /// <inheritdoc/>
    public override ushort Format => 0;

    /// <inheritdoc/>
    /// <param name="glyphIndex">The glyph index to classify. Ignored by this format.</param>
    /// <returns>Always <see cref="BslnTable.DefaultBaseline"/>, because the format carries no mapping.</returns>
    public override BaselineValue GetBaseline(int glyphIndex) => DefaultBaseline;

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the format-specific part, immediately after the eight-byte header.</param>
    /// <param name="context">A <see cref="BslnFormatContext"/> carrying the header fields.</param>
    /// <returns>The parsed format 0 baseline table.</returns>
    /// <exception cref="EndOfStreamException">The delta array extends past the end of the table-scoped source.</exception>
    static BslnFormat0 IDerivedRecord<BslnTable, BslnFormat0>.Parse(ref Cursor cursor, object? context)
    {
        var ctx = (BslnFormatContext)context!;
        return new()
        {
            Version = ctx.Version,
            DefaultBaseline = ctx.DefaultBaseline,
            BaselineDeltas = cursor.ReadInt16Array(BaselineCount),
        };
    }
}

/// <summary>A format 1 <c>bsln</c> table: distance-based baseline positions with a per-glyph mapping.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The format stores thirty-two <c>int16</c> FUnit deltas, one per possible baseline value, followed by a lookup table that maps glyphs onto their dominant baseline.</description></item>
/// <item><description>Glyphs the lookup does not cover use <see cref="BslnTable.DefaultBaseline"/>.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">baseline table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BslnTable"/>
/// <seealso cref="BslnFormat0"/>
/// <seealso cref="BslnFormat2"/>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
public sealed record BslnFormat1 : BslnTable, IDerivedRecord<BslnTable, BslnFormat1>
{
    /// <summary>Gets the baseline position deltas in FUnits.</summary>
    /// <value>An array of <see cref="BslnTable.BaselineCount"/> <c>int16</c> deltas, one per possible baseline value.</value>
    /// <seealso cref="BaselineValue"/>
    public required IReadOnlyList<short> BaselineDeltas { get; init; }

    /// <summary>Gets the lookup table that maps glyphs onto their dominant baseline.</summary>
    /// <value>The parsed Apple Advanced Typography lookup table. Lookup values are 16-bit <see cref="BaselineValue"/> values.</value>
    /// <remarks>Glyphs the lookup does not cover use <see cref="BslnTable.DefaultBaseline"/>.</remarks>
    /// <seealso cref="LookupTable"/>
    /// <seealso cref="BaselineValue"/>
    public required LookupTable Mapping { get; init; }

    /// <inheritdoc/>
    public override ushort Format => 1;

    /// <inheritdoc/>
    /// <param name="glyphIndex">The glyph index to classify.</param>
    /// <returns>The baseline value from the mapping when the lookup covers the glyph; otherwise <see cref="BslnTable.DefaultBaseline"/>.</returns>
    public override BaselineValue GetBaseline(int glyphIndex) => Mapping.TryGetValue(glyphIndex, out uint value) ? (BaselineValue)(ushort)value : DefaultBaseline;

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the format-specific part, immediately after the eight-byte header.</param>
    /// <param name="context">A <see cref="BslnFormatContext"/> carrying the header fields.</param>
    /// <returns>The parsed format 1 baseline table.</returns>
    /// <exception cref="EndOfStreamException">The delta array or the lookup table extends past the end of the table-scoped source.</exception>
    static BslnFormat1 IDerivedRecord<BslnTable, BslnFormat1>.Parse(ref Cursor cursor, object? context)
    {
        var ctx = (BslnFormatContext)context!;
        return new()
        {
            Version = ctx.Version,
            DefaultBaseline = ctx.DefaultBaseline,
            BaselineDeltas = cursor.ReadInt16Array(BaselineCount),
            Mapping = cursor.ReadRecord<LookupTable>(),
        };
    }
}

/// <summary>A format 2 <c>bsln</c> table: control-point-based baseline positions with no per-glyph mapping.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The format identifies a standard glyph by index and stores thirty-two control point indices within that glyph, one per possible baseline value. The control points are resolved after the standard glyph has been hinted, which preserves baseline information better at small point sizes.</description></item>
/// <item><description>A control point entry of <see cref="BslnTable.NoControlPoint"/> means the corresponding baseline has no control point in the standard glyph.</description></item>
/// <item><description>Every glyph in the font uses <see cref="BslnTable.DefaultBaseline"/>; there is no per-glyph variation.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">baseline table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BslnTable"/>
/// <seealso cref="BslnFormat3"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
public sealed record BslnFormat2 : BslnTable, IDerivedRecord<BslnTable, BslnFormat2>
{
    /// <summary>Gets the glyph index of the standard glyph that carries the baseline control points.</summary>
    /// <value>The index of the glyph in the <c>glyf</c> table whose control points define the baseline positions.</value>
    /// <seealso cref="BaselineControlPoints"/>
    public required ushort StandardGlyph { get; init; }

    /// <summary>Gets the control point indices within the standard glyph.</summary>
    /// <value>An array of <see cref="BslnTable.BaselineCount"/> <c>uint16</c> control point indices, one per possible baseline value. Index <c>i</c> is the control point for <c>(BaselineValue)i</c>, or <see cref="BslnTable.NoControlPoint"/> when the standard glyph has no control point for that baseline.</value>
    /// <seealso cref="BaselineValue"/>
    /// <seealso cref="StandardGlyph"/>
    public required IReadOnlyList<ushort> BaselineControlPoints { get; init; }

    /// <inheritdoc/>
    public override ushort Format => 2;

    /// <inheritdoc/>
    /// <param name="glyphIndex">The glyph index to classify. Ignored by this format.</param>
    /// <returns>Always <see cref="BslnTable.DefaultBaseline"/>, because the format carries no mapping.</returns>
    public override BaselineValue GetBaseline(int glyphIndex) => DefaultBaseline;

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the format-specific part, immediately after the eight-byte header.</param>
    /// <param name="context">A <see cref="BslnFormatContext"/> carrying the header fields.</param>
    /// <returns>The parsed format 2 baseline table.</returns>
    /// <exception cref="EndOfStreamException">The control point array extends past the end of the table-scoped source.</exception>
    static BslnFormat2 IDerivedRecord<BslnTable, BslnFormat2>.Parse(ref Cursor cursor, object? context)
    {
        var ctx = (BslnFormatContext)context!;
        return new()
        {
            Version = ctx.Version,
            DefaultBaseline = ctx.DefaultBaseline,
            StandardGlyph = cursor.ReadUInt16(),
            BaselineControlPoints = cursor.ReadUInt16Array(BaselineCount),
        };
    }
}

/// <summary>A format 3 <c>bsln</c> table: control-point-based baseline positions with a per-glyph mapping.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The format combines the standard glyph and control point array of <see cref="BslnFormat2"/> with the per-glyph mapping lookup of <see cref="BslnFormat1"/>.</description></item>
/// <item><description>Glyphs the lookup does not cover use <see cref="BslnTable.DefaultBaseline"/>.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">baseline table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BslnTable"/>
/// <seealso cref="BslnFormat1"/>
/// <seealso cref="BslnFormat2"/>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
public sealed record BslnFormat3 : BslnTable, IDerivedRecord<BslnTable, BslnFormat3>
{
    /// <summary>Gets the glyph index of the standard glyph that carries the baseline control points.</summary>
    /// <value>The index of the glyph in the <c>glyf</c> table whose control points define the baseline positions.</value>
    /// <seealso cref="BaselineControlPoints"/>
    public required ushort StandardGlyph { get; init; }

    /// <summary>Gets the control point indices within the standard glyph.</summary>
    /// <value>An array of <see cref="BslnTable.BaselineCount"/> <c>uint16</c> control point indices, one per possible baseline value. Index <c>i</c> is the control point for <c>(BaselineValue)i</c>, or <see cref="BslnTable.NoControlPoint"/> when the standard glyph has no control point for that baseline.</value>
    /// <seealso cref="BaselineValue"/>
    /// <seealso cref="StandardGlyph"/>
    public required IReadOnlyList<ushort> BaselineControlPoints { get; init; }

    /// <summary>Gets the lookup table that maps glyphs onto their dominant baseline.</summary>
    /// <value>The parsed Apple Advanced Typography lookup table. Lookup values are 16-bit <see cref="BaselineValue"/> values.</value>
    /// <remarks>Glyphs the lookup does not cover use <see cref="BslnTable.DefaultBaseline"/>.</remarks>
    /// <seealso cref="LookupTable"/>
    /// <seealso cref="BaselineValue"/>
    public required LookupTable Mapping { get; init; }

    /// <inheritdoc/>
    public override ushort Format => 3;

    /// <inheritdoc/>
    /// <param name="glyphIndex">The glyph index to classify.</param>
    /// <returns>The baseline value from the mapping when the lookup covers the glyph; otherwise <see cref="BslnTable.DefaultBaseline"/>.</returns>
    public override BaselineValue GetBaseline(int glyphIndex) => Mapping.TryGetValue(glyphIndex, out uint value) ? (BaselineValue)(ushort)value : DefaultBaseline;

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the format-specific part, immediately after the eight-byte header.</param>
    /// <param name="context">A <see cref="BslnFormatContext"/> carrying the header fields.</param>
    /// <returns>The parsed format 3 baseline table.</returns>
    /// <exception cref="EndOfStreamException">The control point array or the lookup table extends past the end of the table-scoped source.</exception>
    static BslnFormat3 IDerivedRecord<BslnTable, BslnFormat3>.Parse(ref Cursor cursor, object? context)
    {
        var ctx = (BslnFormatContext)context!;
        return new()
        {
            Version = ctx.Version,
            DefaultBaseline = ctx.DefaultBaseline,
            StandardGlyph = cursor.ReadUInt16(),
            BaselineControlPoints = cursor.ReadUInt16Array(BaselineCount),
            Mapping = cursor.ReadRecord<LookupTable>(),
        };
    }
}
