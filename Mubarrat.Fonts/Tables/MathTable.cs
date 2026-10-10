using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════
// MATH — Math table
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The <c>MATH</c> table: mathematical typography data. Supplies the constants, per-glyph corrections, kerning information, and size variants needed to lay out mathematical formulas.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Three subtables cover the three independent concerns: <see cref="Constants"/> for formula-wide metrics, <see cref="GlyphInfo"/> for per-glyph adjustments, and <see cref="Variants"/> for the size-variant and assembly mechanisms that grow glyphs to match context.</description></item>
/// <item><description>Only version 1.0 is currently defined; the major version must be 1.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math">MATH table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MathConstants"/>
/// <seealso cref="MathGlyphInfo"/>
/// <seealso cref="MathVariants"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math">OpenType specification: MATH table</seealso>
public sealed record MathTable : IFontTable<MathTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math">OpenType specification: MATH table</seealso>
    public static Tag Tag => "MATH";

    /// <summary>Gets the major version. Always 1.</summary>
    /// <value>The constant <c>1</c> for a conforming MATH table.</value>
    /// <seealso cref="MinorVersion"/>
    /// <seealso cref="Header.MajorVersion"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version. Always 0.</summary>
    /// <value>The constant <c>0</c> for a conforming MATH table.</value>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="Header.MinorVersion"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the formula-wide constants, or <c>null</c>.</summary>
    /// <value>The <see cref="MathConstants"/> table resolved from the header offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="MathConstants"/>
    public MathConstants? Constants { get; init; }

    /// <summary>Gets the per-glyph information, or <c>null</c>.</summary>
    /// <value>The <see cref="MathGlyphInfo"/> table resolved from the header offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="MathGlyphInfo"/>
    public MathGlyphInfo? GlyphInfo { get; init; }

    /// <summary>Gets the size-variant and assembly data, or <c>null</c>.</summary>
    /// <value>The <see cref="MathVariants"/> table resolved from the header offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="MathVariants"/>
    public MathVariants? Variants { get; init; }

    /// <summary>The 10-byte <c>MATH</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All three offsets are measured from the start of the MATH table; a zero offset means the corresponding subtable is absent.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math">MATH header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MathTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math">OpenType specification: MATH header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the major version. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming MATH table.</value>
        /// <seealso cref="MinorVersion"/>
        public ushort MajorVersion;   // +0

        /// <summary>Gets the minor version. Always 0.</summary>
        /// <value>The constant <c>0</c> for a conforming MATH table.</value>
        /// <seealso cref="MajorVersion"/>
        public ushort MinorVersion;   // +2

        /// <summary>Gets the offset to the constants subtable, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="MathConstants"/> from the MATH table start, or zero.</value>
        /// <seealso cref="GlyphInfoOffset"/>
        /// <seealso cref="VariantsOffset"/>
        public ushort ConstantsOffset;   // +4

        /// <summary>Gets the offset to the glyph-info subtable, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="MathGlyphInfo"/> from the MATH table start, or zero.</value>
        /// <seealso cref="ConstantsOffset"/>
        /// <seealso cref="VariantsOffset"/>
        public ushort GlyphInfoOffset;   // +6

        /// <summary>Gets the offset to the variants subtable, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="MathVariants"/> from the MATH table start, or zero.</value>
        /// <seealso cref="ConstantsOffset"/>
        /// <seealso cref="GlyphInfoOffset"/>
        public ushort VariantsOffset;   // +8

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All five fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            MajorVersion = BinaryPrimitives.ReverseEndianness(v.MajorVersion),
            MinorVersion = BinaryPrimitives.ReverseEndianness(v.MinorVersion),
            ConstantsOffset = BinaryPrimitives.ReverseEndianness(v.ConstantsOffset),
            GlyphInfoOffset = BinaryPrimitives.ReverseEndianness(v.GlyphInfoOffset),
            VariantsOffset = BinaryPrimitives.ReverseEndianness(v.VariantsOffset),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the MATH table.</param>
    /// <param name="context">Unused. The table resolves its subtables through its own source.</param>
    /// <returns>The parsed MATH table.</returns>
    /// <exception cref="InvalidDataException">The major version is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header or any referenced subtable extends past the end of the table-scoped source.</exception>
    /// <remarks>Every offset is checked for zero before parsing; a zero offset yields a <c>null</c> property rather than an exception.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="MathConstants"/>
    /// <seealso cref="MathGlyphInfo"/>
    /// <seealso cref="MathVariants"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math">OpenType specification: MATH table</seealso>
    public static MathTable Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.MajorVersion != 1)
            throw new InvalidDataException(
                $"'MATH'.majorVersion is {header.MajorVersion}, expected 1.");
        return new MathTable
        {
            MajorVersion = header.MajorVersion,
            MinorVersion = header.MinorVersion,
            Constants = header.ConstantsOffset != 0 ? cursor.Source.ParseRecordAt<MathConstants>(header.ConstantsOffset) : null,
            GlyphInfo = header.GlyphInfoOffset != 0 ? cursor.Source.ParseRecordAt<MathGlyphInfo>(header.GlyphInfoOffset) : null,
            Variants = header.VariantsOffset != 0 ? cursor.Source.ParseRecordAt<MathVariants>(header.VariantsOffset) : null,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// MathValueRecordHeader — the shared primitive
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A MathValueRecord with its Device table resolved.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A MathValueRecord pairs a design-unit value with an optional Device or VariationIndex table that adjusts the value per ppem or per variation instance.</description></item>
/// <item><description>The type is reused throughout the MATH table wherever a single adjusted metric is needed.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathvaluerecord">MathValueRecord</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Device"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathvaluerecord">OpenType specification: MathValueRecord</seealso>
public sealed record MathValueRecord : IEndianReversibleHeaderRecord<MathValueRecord, MathValueRecord.Header>
{
    /// <summary>Gets the design-unit value.</summary>
    /// <value>The base value in font design units, before device or variation adjustment.</value>
    /// <seealso cref="DeviceTable"/>
    public short Value { get; init; }

    /// <summary>Gets the Device or VariationIndex table, or <c>null</c>.</summary>
    /// <value>The <see cref="Device"/> table resolved from the record's device offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="Value"/>
    /// <seealso cref="Device"/>
    public Device? DeviceTable { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read header.</param>
    /// <param name="context">A <see cref="ParentContext"/> whose <see cref="IParentContext.ParentSource"/> is the parent subtable-scoped source.</param>
    /// <returns>A new record with its device table resolved.</returns>
    /// <remarks>The device offset is measured from the start of the parent subtable; the parent context carries the subtable-scoped source so the offset resolves correctly.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Device"/>
    static MathValueRecord IHeaderRecord<MathValueRecord, Header>.FromHeader(in Header header, object? context) => new()
    {
        Value = header.Value,
        DeviceTable = header.DeviceOffset != 0 ? ((ParentContext)context!).ParentSource.ParseRecordAt<Device>(header.DeviceOffset) : null,
    };

    /// <summary>On-disk form of a MathValueRecord: an FWORD value and an Offset16 to a Device table. Blittable, size 4. The DeviceOffset is relative to the beginning of the parent sub-table.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The record is read through <see cref="IEndianReversibleHeaderRecord{T, THeader}"/>, so the parent's <c>FromHeader</c> resolves the device offset against the parent source.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathvaluerecord">MathValueRecord</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MathValueRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathvaluerecord">OpenType specification: MathValueRecord</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The size of the header, in bytes.</summary>
        /// <value>The constant <c>4</c>.</value>
        /// <remarks>Used when the parent subtable computes offsets from a fixed record size.</remarks>
        public const int Size = 4;

        /// <summary>Gets the design-unit value.</summary>
        /// <value>The base value in font design units, before device or variation adjustment.</value>
        /// <seealso cref="DeviceOffset"/>
        public short Value { get; init; }   // +0

        /// <summary>Gets the offset to the Device or VariationIndex table, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="Device"/> table from the start of the parent subtable, or zero.</value>
        /// <seealso cref="Value"/>
        /// <seealso cref="Device"/>
        public ushort DeviceOffset;   // +2

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>The value uses <see cref="BinaryPrimitives.ReverseEndianness(short)"/>; the device offset uses <see cref="BinaryPrimitives.ReverseEndianness(ushort)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Value = BinaryPrimitives.ReverseEndianness(v.Value),
            DeviceOffset = BinaryPrimitives.ReverseEndianness(v.DeviceOffset),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// MathConstants
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The formula-wide metrics used during math layout: script-size reductions, minimum heights, and fifty-one named <see cref="MathValueRecord"/> metrics covering subscripts, superscripts, fractions, radicals, and bars.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record is fixed-layout: a small prefix, a 51-entry <see cref="Values"/> array, and a trailing one-word suffix.</description></item>
/// <item><description>The 51 metrics are exposed through named properties that index into <see cref="Values"/>; using the named properties is preferred over indexing directly.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathconstants-table">MathConstants table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MathValueRecord"/>
/// <seealso cref="MathTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathconstants-table">OpenType specification: MathConstants table</seealso>
public sealed record MathConstants : IRecord<MathConstants>
{
    /// <summary>The fixed number of <see cref="MathValueRecord"/> entries in the constants table.</summary>
    /// <value>The constant <c>51</c>.</value>
    /// <remarks>Used by <see cref="Parse"/> when reading the <see cref="Values"/> array.</remarks>
    /// <seealso cref="Values"/>
    public const int ValueCount = 51;

    /// <summary>Gets the script-size reduction percentage.</summary>
    /// <value>The percentage by which script-size text is reduced relative to normal-size text.</value>
    /// <seealso cref="ScriptScriptPercentScaleDown"/>
    public short ScriptPercentScaleDown { get; init; }

    /// <summary>Gets the script-script-size reduction percentage.</summary>
    /// <value>The percentage by which script-script-size text is reduced relative to normal-size text.</value>
    /// <seealso cref="ScriptPercentScaleDown"/>
    public short ScriptScriptPercentScaleDown { get; init; }

    /// <summary>Gets the minimum height for delimited sub-formulas, in design units.</summary>
    /// <value>The minimum height a delimited sub-formula must have, in font design units.</value>
    /// <seealso cref="DisplayOperatorMinHeight"/>
    public ushort DelimitedSubFormulaMinHeight { get; init; }

    /// <summary>Gets the minimum height for display-size operators, in design units.</summary>
    /// <value>The minimum height an operator glyph must have when rendered in display style, in font design units.</value>
    /// <seealso cref="DelimitedSubFormulaMinHeight"/>
    public ushort DisplayOperatorMinHeight { get; init; }

    /// <summary>Gets the fifty-one named math metrics, in specification order.</summary>
    /// <value>The ordered list of <see cref="MathValueRecord"/> entries indexed by their position in the specification's MathConstants table.</value>
    /// <remarks>The named accessors (e.g. <see cref="MathLeading"/>, <see cref="AxisHeight"/>) index into this array; the ordering is fixed by the specification.</remarks>
    /// <seealso cref="MathLeading"/>
    /// <seealso cref="AxisHeight"/>
    public IReadOnlyList<MathValueRecord> Values { get; init; } = [];

    /// <summary>Gets the percentage by which a radical's degree is raised relative to the bottom of the radical.</summary>
    /// <value>A percentage used for radical degree positioning.</value>
    /// <seealso cref="Values"/>
    public short RadicalDegreeBottomRaisePercent { get; init; }

    /// <summary>Gets the MathLeading metric.</summary>
    /// <value>The <c>MathLeading</c> value record, index 0 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord MathLeading => Values[0];

    /// <summary>Gets the AxisHeight metric.</summary>
    /// <value>The <c>AxisHeight</c> value record, index 1 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord AxisHeight => Values[1];

    /// <summary>Gets the AccentBaseHeight metric.</summary>
    /// <value>The <c>AccentBaseHeight</c> value record, index 2 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord AccentBaseHeight => Values[2];

    /// <summary>Gets the FlattenedAccentBaseHeight metric.</summary>
    /// <value>The <c>FlattenedAccentBaseHeight</c> value record, index 3 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord FlattenedAccentBaseHeight => Values[3];

    /// <summary>Gets the SubscriptShiftDown metric.</summary>
    /// <value>The <c>SubscriptShiftDown</c> value record, index 4 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord SubscriptShiftDown => Values[4];

    /// <summary>Gets the SubscriptTopMax metric.</summary>
    /// <value>The <c>SubscriptTopMax</c> value record, index 5 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord SubscriptTopMax => Values[5];

    /// <summary>Gets the SubscriptBaselineDropMin metric.</summary>
    /// <value>The <c>SubscriptBaselineDropMin</c> value record, index 6 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord SubscriptBaselineDropMin => Values[6];

    /// <summary>Gets the SuperscriptShiftUp metric.</summary>
    /// <value>The <c>SuperscriptShiftUp</c> value record, index 7 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord SuperscriptShiftUp => Values[7];

    /// <summary>Gets the SuperscriptShiftUpCramped metric.</summary>
    /// <value>The <c>SuperscriptShiftUpCramped</c> value record, index 8 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord SuperscriptShiftUpCramped => Values[8];

    /// <summary>Gets the SuperscriptBottomMin metric.</summary>
    /// <value>The <c>SuperscriptBottomMin</c> value record, index 9 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord SuperscriptBottomMin => Values[9];

    /// <summary>Gets the SuperscriptBaselineDropMax metric.</summary>
    /// <value>The <c>SuperscriptBaselineDropMax</c> value record, index 10 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord SuperscriptBaselineDropMax => Values[10];

    /// <summary>Gets the SubSuperscriptGapMin metric.</summary>
    /// <value>The <c>SubSuperscriptGapMin</c> value record, index 11 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord SubSuperscriptGapMin => Values[11];

    /// <summary>Gets the SuperscriptBottomMaxWithSubscript metric.</summary>
    /// <value>The <c>SuperscriptBottomMaxWithSubscript</c> value record, index 12 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord SuperscriptBottomMaxWithSubscript => Values[12];

    /// <summary>Gets the SpaceAfterScript metric.</summary>
    /// <value>The <c>SpaceAfterScript</c> value record, index 13 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord SpaceAfterScript => Values[13];

    /// <summary>Gets the UpperLimitGapMin metric.</summary>
    /// <value>The <c>UpperLimitGapMin</c> value record, index 14 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord UpperLimitGapMin => Values[14];

    /// <summary>Gets the UpperLimitBaselineRiseMin metric.</summary>
    /// <value>The <c>UpperLimitBaselineRiseMin</c> value record, index 15 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord UpperLimitBaselineRiseMin => Values[15];

    /// <summary>Gets the LowerLimitGapMin metric.</summary>
    /// <value>The <c>LowerLimitGapMin</c> value record, index 16 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord LowerLimitGapMin => Values[16];

    /// <summary>Gets the LowerLimitBaselineDropMin metric.</summary>
    /// <value>The <c>LowerLimitBaselineDropMin</c> value record, index 17 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord LowerLimitBaselineDropMin => Values[17];

    /// <summary>Gets the StackTopShiftUp metric.</summary>
    /// <value>The <c>StackTopShiftUp</c> value record, index 18 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord StackTopShiftUp => Values[18];

    /// <summary>Gets the StackTopDisplayStyleShiftUp metric.</summary>
    /// <value>The <c>StackTopDisplayStyleShiftUp</c> value record, index 19 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord StackTopDisplayStyleShiftUp => Values[19];

    /// <summary>Gets the StackBottomShiftDown metric.</summary>
    /// <value>The <c>StackBottomShiftDown</c> value record, index 20 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord StackBottomShiftDown => Values[20];

    /// <summary>Gets the StackBottomDisplayStyleShiftDown metric.</summary>
    /// <value>The <c>StackBottomDisplayStyleShiftDown</c> value record, index 21 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord StackBottomDisplayStyleShiftDown => Values[21];

    /// <summary>Gets the StackGapMin metric.</summary>
    /// <value>The <c>StackGapMin</c> value record, index 22 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord StackGapMin => Values[22];

    /// <summary>Gets the StackDisplayStyleGapMin metric.</summary>
    /// <value>The <c>StackDisplayStyleGapMin</c> value record, index 23 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord StackDisplayStyleGapMin => Values[23];

    /// <summary>Gets the StretchStackTopShiftUp metric.</summary>
    /// <value>The <c>StretchStackTopShiftUp</c> value record, index 24 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord StretchStackTopShiftUp => Values[24];

    /// <summary>Gets the StretchStackBottomShiftDown metric.</summary>
    /// <value>The <c>StretchStackBottomShiftDown</c> value record, index 25 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord StretchStackBottomShiftDown => Values[25];

    /// <summary>Gets the StretchStackGapAboveMin metric.</summary>
    /// <value>The <c>StretchStackGapAboveMin</c> value record, index 26 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord StretchStackGapAboveMin => Values[26];

    /// <summary>Gets the StretchStackGapBelowMin metric.</summary>
    /// <value>The <c>StretchStackGapBelowMin</c> value record, index 27 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord StretchStackGapBelowMin => Values[27];

    /// <summary>Gets the FractionNumeratorShiftUp metric.</summary>
    /// <value>The <c>FractionNumeratorShiftUp</c> value record, index 28 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord FractionNumeratorShiftUp => Values[28];

    /// <summary>Gets the FractionNumeratorDisplayStyleShiftUp metric.</summary>
    /// <value>The <c>FractionNumeratorDisplayStyleShiftUp</c> value record, index 29 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord FractionNumeratorDisplayStyleShiftUp => Values[29];

    /// <summary>Gets the FractionDenominatorShiftDown metric.</summary>
    /// <value>The <c>FractionDenominatorShiftDown</c> value record, index 30 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord FractionDenominatorShiftDown => Values[30];

    /// <summary>Gets the FractionDenominatorDisplayStyleShiftDown metric.</summary>
    /// <value>The <c>FractionDenominatorDisplayStyleShiftDown</c> value record, index 31 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord FractionDenominatorDisplayStyleShiftDown => Values[31];

    /// <summary>Gets the FractionNumeratorGapMin metric.</summary>
    /// <value>The <c>FractionNumeratorGapMin</c> value record, index 32 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord FractionNumeratorGapMin => Values[32];

    /// <summary>Gets the FractionNumDisplayStyleGapMin metric.</summary>
    /// <value>The <c>FractionNumDisplayStyleGapMin</c> value record, index 33 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord FractionNumDisplayStyleGapMin => Values[33];

    /// <summary>Gets the FractionRuleThickness metric.</summary>
    /// <value>The <c>FractionRuleThickness</c> value record, index 34 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord FractionRuleThickness => Values[34];

    /// <summary>Gets the FractionDenominatorGapMin metric.</summary>
    /// <value>The <c>FractionDenominatorGapMin</c> value record, index 35 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord FractionDenominatorGapMin => Values[35];

    /// <summary>Gets the FractionDenomDisplayStyleGapMin metric.</summary>
    /// <value>The <c>FractionDenomDisplayStyleGapMin</c> value record, index 36 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord FractionDenomDisplayStyleGapMin => Values[36];

    /// <summary>Gets the SkewedFractionHorizontalGap metric.</summary>
    /// <value>The <c>SkewedFractionHorizontalGap</c> value record, index 37 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord SkewedFractionHorizontalGap => Values[37];

    /// <summary>Gets the SkewedFractionVerticalGap metric.</summary>
    /// <value>The <c>SkewedFractionVerticalGap</c> value record, index 38 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord SkewedFractionVerticalGap => Values[38];

    /// <summary>Gets the OverbarVerticalGap metric.</summary>
    /// <value>The <c>OverbarVerticalGap</c> value record, index 39 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord OverbarVerticalGap => Values[39];

    /// <summary>Gets the OverbarRuleThickness metric.</summary>
    /// <value>The <c>OverbarRuleThickness</c> value record, index 40 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord OverbarRuleThickness => Values[40];

    /// <summary>Gets the OverbarExtraAscender metric.</summary>
    /// <value>The <c>OverbarExtraAscender</c> value record, index 41 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord OverbarExtraAscender => Values[41];

    /// <summary>Gets the UnderbarVerticalGap metric.</summary>
    /// <value>The <c>UnderbarVerticalGap</c> value record, index 42 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord UnderbarVerticalGap => Values[42];

    /// <summary>Gets the UnderbarRuleThickness metric.</summary>
    /// <value>The <c>UnderbarRuleThickness</c> value record, index 43 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord UnderbarRuleThickness => Values[43];

    /// <summary>Gets the UnderbarExtraDescender metric.</summary>
    /// <value>The <c>UnderbarExtraDescender</c> value record, index 44 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord UnderbarExtraDescender => Values[44];

    /// <summary>Gets the RadicalVerticalGap metric.</summary>
    /// <value>The <c>RadicalVerticalGap</c> value record, index 45 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord RadicalVerticalGap => Values[45];

    /// <summary>Gets the RadicalDisplayStyleVerticalGap metric.</summary>
    /// <value>The <c>RadicalDisplayStyleVerticalGap</c> value record, index 46 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord RadicalDisplayStyleVerticalGap => Values[46];

    /// <summary>Gets the RadicalRuleThickness metric.</summary>
    /// <value>The <c>RadicalRuleThickness</c> value record, index 47 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord RadicalRuleThickness => Values[47];

    /// <summary>Gets the RadicalExtraAscender metric.</summary>
    /// <value>The <c>RadicalExtraAscender</c> value record, index 48 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord RadicalExtraAscender => Values[48];

    /// <summary>Gets the RadicalKernBeforeDegree metric.</summary>
    /// <value>The <c>RadicalKernBeforeDegree</c> value record, index 49 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord RadicalKernBeforeDegree => Values[49];

    /// <summary>Gets the RadicalKernAfterDegree metric.</summary>
    /// <value>The <c>RadicalKernAfterDegree</c> value record, index 50 in <see cref="Values"/>.</value>
    /// <seealso cref="Values"/>
    public MathValueRecord RadicalKernAfterDegree => Values[50];

    /// <summary>The 8-byte MathConstants prefix. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Contains the four non-MathValueRecord fields that precede the fifty-one-entry <see cref="Values"/> array.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathconstants-table">MathConstants table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MathConstants"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathconstants-table">OpenType specification: MathConstants table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Prefix : IEndianReversibleStruct<Prefix>
    {
        /// <summary>Gets the script-size reduction percentage.</summary>
        /// <value>The percentage by which script-size text is reduced relative to normal-size text.</value>
        /// <seealso cref="ScriptScriptPercentScaleDown"/>
        public short ScriptPercentScaleDown { get; init; }   // +0

        /// <summary>Gets the script-script-size reduction percentage.</summary>
        /// <value>The percentage by which script-script-size text is reduced relative to normal-size text.</value>
        /// <seealso cref="ScriptPercentScaleDown"/>
        public short ScriptScriptPercentScaleDown { get; init; }   // +2

        /// <summary>Gets the minimum delimited-sub-formula height.</summary>
        /// <value>The minimum height a delimited sub-formula must have, in font design units.</value>
        /// <seealso cref="DisplayOperatorMinHeight"/>
        public ushort DelimitedSubFormulaMinHeight;   // +4

        /// <summary>Gets the minimum display-size operator height.</summary>
        /// <value>The minimum height an operator glyph must have when rendered in display style, in font design units.</value>
        /// <seealso cref="DelimitedSubFormulaMinHeight"/>
        public ushort DisplayOperatorMinHeight;   // +6

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new prefix with each multi-byte field reversed.</returns>
        /// <remarks>All four fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Prefix ReverseEndianness(Prefix v) => new()
        {
            ScriptPercentScaleDown = BinaryPrimitives.ReverseEndianness(v.ScriptPercentScaleDown),
            ScriptScriptPercentScaleDown = BinaryPrimitives.ReverseEndianness(v.ScriptScriptPercentScaleDown),
            DelimitedSubFormulaMinHeight = BinaryPrimitives.ReverseEndianness(v.DelimitedSubFormulaMinHeight),
            DisplayOperatorMinHeight = BinaryPrimitives.ReverseEndianness(v.DisplayOperatorMinHeight),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the MathConstants subtable.</param>
    /// <param name="context">Unused. The subtable is self-describing.</param>
    /// <returns>The parsed MathConstants subtable.</returns>
    /// <exception cref="EndOfStreamException">The prefix, values array, or suffix extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The fixed layout is: 8-byte prefix, 51 four-byte <see cref="MathValueRecord.Header"/> entries, 2-byte suffix.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathconstants-table">MathConstants table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Prefix"/>
    /// <seealso cref="Values"/>
    /// <seealso cref="MathValueRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathconstants-table">OpenType specification: MathConstants table</seealso>
    public static MathConstants Parse(ref Cursor cursor, object? context)
    {
        // Layout: Prefix (8) | 51 × MathValueRecordHeader (204) | Suffix (2) = 214 bytes.
        Prefix prefix = cursor.ReadBigEndianStruct<Prefix>();
        return new MathConstants
        {
            ScriptPercentScaleDown = prefix.ScriptPercentScaleDown,
            ScriptScriptPercentScaleDown = prefix.ScriptScriptPercentScaleDown,
            DelimitedSubFormulaMinHeight = prefix.DelimitedSubFormulaMinHeight,
            DisplayOperatorMinHeight = prefix.DisplayOperatorMinHeight,
            Values = cursor.ReadBigEndianHeaderRecordArray<MathValueRecord, MathValueRecord.Header>(ValueCount, new ParentContext(cursor.Source)),
            RadicalDegreeBottomRaisePercent = cursor.ReadInt16(),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// MathGlyphInfo
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The per-glyph information subtable of the MATH table: italics corrections, top-accent attachments, extended-shape coverage, and math kerning.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>All four subtables are optional; a font may declare any subset by zeroing the corresponding offset in the header.</description></item>
/// <item><description>The extended-shape coverage is a plain coverage table that identifies glyphs that stretch; the other three are subtable types defined in the MATH specification.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathglyphinfo-table">MathGlyphInfo table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MathTable"/>
/// <seealso cref="MathItalicsCorrectionInfo"/>
/// <seealso cref="MathTopAccentAttachment"/>
/// <seealso cref="MathKernInfo"/>
/// <seealso cref="Coverage"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathglyphinfo-table">OpenType specification: MathGlyphInfo table</seealso>
public sealed record MathGlyphInfo : IRecord<MathGlyphInfo>
{
    /// <summary>Gets the italics correction subtable, or <c>null</c>.</summary>
    /// <value>The <see cref="MathItalicsCorrectionInfo"/> resolved from the header offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="MathItalicsCorrectionInfo"/>
    public MathItalicsCorrectionInfo? ItalicsCorrection { get; init; }

    /// <summary>Gets the top-accent attachment subtable, or <c>null</c>.</summary>
    /// <value>The <see cref="MathTopAccentAttachment"/> resolved from the header offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="MathTopAccentAttachment"/>
    public MathTopAccentAttachment? TopAccentAttachment { get; init; }

    /// <summary>Gets the extended-shape coverage table, or <c>null</c>.</summary>
    /// <value>The <see cref="Coverage"/> that identifies glyphs eligible for stretching, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="Coverage"/>
    public Coverage? ExtendedShapeCoverage { get; init; }

    /// <summary>Gets the math kerning subtable, or <c>null</c>.</summary>
    /// <value>The <see cref="MathKernInfo"/> resolved from the header offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="MathKernInfo"/>
    public MathKernInfo? KernInfo { get; init; }

    /// <summary>The 8-byte MathGlyphInfo header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All four offsets are measured from the start of the MathGlyphInfo record; a zero offset means the corresponding subtable is absent.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathglyphinfo-table">MathGlyphInfo table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MathGlyphInfo"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathglyphinfo-table">OpenType specification: MathGlyphInfo table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the offset to the italics correction subtable, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="MathItalicsCorrectionInfo"/> from the MathGlyphInfo record start, or zero.</value>
        /// <seealso cref="TopAccentOffset"/>
        public ushort ItalicsCorrectionOffset;   // +0

        /// <summary>Gets the offset to the top-accent attachment subtable, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="MathTopAccentAttachment"/> from the MathGlyphInfo record start, or zero.</value>
        /// <seealso cref="ItalicsCorrectionOffset"/>
        public ushort TopAccentOffset;   // +2

        /// <summary>Gets the offset to the extended-shape coverage table, or zero when absent.</summary>
        /// <value>The byte offset of the extended-shape <see cref="Coverage"/> from the MathGlyphInfo record start, or zero.</value>
        /// <seealso cref="KernInfoOffset"/>
        public ushort ExtendedShapeOffset;   // +4

        /// <summary>Gets the offset to the math kerning subtable, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="MathKernInfo"/> from the MathGlyphInfo record start, or zero.</value>
        /// <seealso cref="ExtendedShapeOffset"/>
        public ushort KernInfoOffset;   // +6

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All four fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            ItalicsCorrectionOffset = BinaryPrimitives.ReverseEndianness(v.ItalicsCorrectionOffset),
            TopAccentOffset = BinaryPrimitives.ReverseEndianness(v.TopAccentOffset),
            ExtendedShapeOffset = BinaryPrimitives.ReverseEndianness(v.ExtendedShapeOffset),
            KernInfoOffset = BinaryPrimitives.ReverseEndianness(v.KernInfoOffset),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the MathGlyphInfo record.</param>
    /// <param name="context">Unused. The record resolves its subtables through its own source.</param>
    /// <returns>The parsed MathGlyphInfo record.</returns>
    /// <exception cref="EndOfStreamException">The header or any referenced subtable extends past the end of the source.</exception>
    /// <remarks>Every offset is checked for zero before parsing; a zero offset yields a <c>null</c> property rather than an exception.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathglyphinfo-table">OpenType specification: MathGlyphInfo table</seealso>
    public static MathGlyphInfo Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new MathGlyphInfo
        {
            ItalicsCorrection = header.ItalicsCorrectionOffset != 0 ? cursor.Source.ParseRecordAt<MathItalicsCorrectionInfo>(header.ItalicsCorrectionOffset) : null,
            TopAccentAttachment = header.TopAccentOffset != 0 ? cursor.Source.ParseRecordAt<MathTopAccentAttachment>(header.TopAccentOffset) : null,
            ExtendedShapeCoverage = header.ExtendedShapeOffset != 0 ? cursor.Source.ParseRecordAt<Coverage>(header.ExtendedShapeOffset) : null,
            KernInfo = header.KernInfoOffset != 0 ? cursor.Source.ParseRecordAt<MathKernInfo>(header.KernInfoOffset) : null,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// MathValuePerGlyphTable + wrappers
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Per-glyph italics corrections, indexed by Coverage.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each covered glyph has one <see cref="MathValueRecord"/> correction; the correction is typically nonzero only for glyphs that slant.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathitalicscorrectioninfo-table">MathItalicsCorrectionInfo table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MathGlyphInfo"/>
/// <seealso cref="MathValueRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathitalicscorrectioninfo-table">OpenType specification: MathItalicsCorrectionInfo table</seealso>
public sealed record MathItalicsCorrectionInfo : IRecord<MathItalicsCorrectionInfo>
{
    /// <summary>Gets the coverage table that assigns indices to covered glyphs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>Corrections[i]</c>.</value>
    /// <seealso cref="Corrections"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets the per-glyph italics corrections in Coverage Index order.</summary>
    /// <value>The ordered list of <see cref="MathValueRecord"/> entries; length equals <see cref="Coverage"/>'s glyph count.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="GetCorrection(int)"/>
    public IReadOnlyList<MathValueRecord> Corrections { get; init; } = [];

    /// <summary>Returns the italics correction for a glyph, or <c>null</c> when uncovered.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The <see cref="MathValueRecord"/> correction, or <c>null</c> when the glyph is not covered.</returns>
    /// <seealso cref="Corrections"/>
    public MathValueRecord? GetCorrection(int glyphId)
    {
        int i = Coverage.GetCoverageIndex(glyphId);
        return i >= 0 && i < Corrections.Count ? Corrections[i] : null;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable.</param>
    /// <param name="context">Unused. The subtable resolves its coverage and corrections through its own source.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, coverage, or correction array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathitalicscorrectioninfo-table">MathItalicsCorrectionInfo table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="MathValueRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathitalicscorrectioninfo-table">OpenType specification: MathItalicsCorrectionInfo table</seealso>
    public static MathItalicsCorrectionInfo Parse(ref Cursor cursor, object? context) => new()
    {
        Coverage = cursor.Source.ParseRecordAt<Coverage>(cursor.ReadUInt16()),
        Corrections = cursor.ReadBigEndianHeaderRecordArray<MathValueRecord, MathValueRecord.Header>(cursor.ReadUInt16(), new ParentContext(cursor.Source)),
    };
}

/// <summary>Per-glyph top-accent attachments, indexed by Coverage.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each covered glyph has one <see cref="MathValueRecord"/> that gives the x-coordinate where an accent should be attached above the glyph.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathtopaccentattachment-table">MathTopAccentAttachment table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MathGlyphInfo"/>
/// <seealso cref="MathValueRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathtopaccentattachment-table">OpenType specification: MathTopAccentAttachment table</seealso>
public sealed record MathTopAccentAttachment : IRecord<MathTopAccentAttachment>
{
    /// <summary>Gets the coverage table that assigns indices to covered glyphs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>Attachments[i]</c>.</value>
    /// <seealso cref="Attachments"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets the per-glyph top-accent attachments in Coverage Index order.</summary>
    /// <value>The ordered list of <see cref="MathValueRecord"/> entries; length equals <see cref="Coverage"/>'s glyph count.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="GetAttachment(int)"/>
    public IReadOnlyList<MathValueRecord> Attachments { get; init; } = [];

    /// <summary>Returns the top-accent attachment for a glyph, or <c>null</c> when uncovered.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The <see cref="MathValueRecord"/> attachment, or <c>null</c> when the glyph is not covered.</returns>
    /// <seealso cref="Attachments"/>
    public MathValueRecord? GetAttachment(int glyphId)
    {
        int i = Coverage.GetCoverageIndex(glyphId);
        return i >= 0 && i < Attachments.Count ? Attachments[i] : null;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable.</param>
    /// <param name="context">Unused. The subtable resolves its coverage and attachments through its own source.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, coverage, or attachment array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathtopaccentattachment-table">MathTopAccentAttachment table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="MathValueRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathtopaccentattachment-table">OpenType specification: MathTopAccentAttachment table</seealso>
    public static MathTopAccentAttachment Parse(ref Cursor cursor, object? context) => new()
    {
        Coverage = cursor.Source.ParseRecordAt<Coverage>(cursor.ReadUInt16()),
        Attachments = cursor.ReadBigEndianHeaderRecordArray<MathValueRecord, MathValueRecord.Header>(cursor.ReadUInt16(), new ParentContext(cursor.Source)),
    };
}

// ═══════════════════════════════════════════════════════════════════════════
// MathKernInfo, MathKernInfoRecord, MathKern
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Per-glyph math kerning information, indexed by Coverage.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each covered glyph has four optional kerning sub-tables, one per corner (top-right, top-left, bottom-right, bottom-left).</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathkerninfo-table">MathKernInfo table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MathGlyphInfo"/>
/// <seealso cref="MathKernInfoRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathkerninfo-table">OpenType specification: MathKernInfo table</seealso>
public sealed record MathKernInfo : IRecord<MathKernInfo>
{
    /// <summary>Gets the coverage table that assigns indices to covered glyphs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>Records[i]</c>.</value>
    /// <seealso cref="Records"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets the per-glyph kerning records in Coverage Index order.</summary>
    /// <value>The ordered list of <see cref="MathKernInfoRecord"/> entries; length equals <see cref="Coverage"/>'s glyph count.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="GetRecord(int)"/>
    public IReadOnlyList<MathKernInfoRecord> Records { get; init; } = [];

    /// <summary>Returns the kerning record for a glyph, or <c>null</c> when uncovered.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The <see cref="MathKernInfoRecord"/> for the glyph, or <c>null</c> when the glyph is not covered.</returns>
    /// <seealso cref="Records"/>
    public MathKernInfoRecord? GetRecord(int glyphId)
    {
        int i = Coverage.GetCoverageIndex(glyphId);
        return i >= 0 && i < Records.Count ? Records[i] : null;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable.</param>
    /// <param name="context">Unused. The subtable resolves its coverage and records through its own source.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, coverage, or record array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathkerninfo-table">MathKernInfo table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="MathKernInfoRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathkerninfo-table">OpenType specification: MathKernInfo table</seealso>
    public static MathKernInfo Parse(ref Cursor cursor, object? context) => new()
    {
        Coverage = cursor.Source.ParseRecordAt<Coverage>(cursor.ReadUInt16()),
        Records = cursor.ReadBigEndianHeaderRecordArray<MathKernInfoRecord, MathKernInfoRecord.Header>(cursor.ReadUInt16(), new ParentContext(cursor.Source)),
    };
}

/// <summary>Per-corner kerning tables for one glyph: four optional <see cref="MathKern"/> references, one per corner.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The four corners are independent; a glyph may declare kerning for any subset of them.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathkerninfo-table">MathKernInfo table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MathKernInfo"/>
/// <seealso cref="MathKern"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathkerninfo-table">OpenType specification: MathKernInfo table</seealso>
public sealed record MathKernInfoRecord : IEndianReversibleHeaderRecord<MathKernInfoRecord, MathKernInfoRecord.Header>
{
    /// <summary>Gets the top-right kerning table, or <c>null</c>.</summary>
    /// <value>The <see cref="MathKern"/> for the top-right corner, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="TopLeft"/>
    public MathKern? TopRight { get; init; }

    /// <summary>Gets the top-left kerning table, or <c>null</c>.</summary>
    /// <value>The <see cref="MathKern"/> for the top-left corner, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="TopRight"/>
    public MathKern? TopLeft { get; init; }

    /// <summary>Gets the bottom-right kerning table, or <c>null</c>.</summary>
    /// <value>The <see cref="MathKern"/> for the bottom-right corner, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="BottomLeft"/>
    public MathKern? BottomRight { get; init; }

    /// <summary>Gets the bottom-left kerning table, or <c>null</c>.</summary>
    /// <value>The <see cref="MathKern"/> for the bottom-left corner, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="BottomRight"/>
    public MathKern? BottomLeft { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read header.</param>
    /// <param name="context">A <see cref="ParentContext"/> whose <see cref="IParentContext.ParentSource"/> is the MathKernInfo-scoped source.</param>
    /// <returns>A new record with its four kerning tables resolved.</returns>
    /// <remarks>Each offset is measured from the start of the enclosing <see cref="MathKernInfo"/> table; the parent context carries the table-scoped source so the offsets resolve correctly.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="MathKern"/>
    static MathKernInfoRecord IHeaderRecord<MathKernInfoRecord, Header>.FromHeader(in Header header, object? context)
    {
        Source source = ((ParentContext)context!).ParentSource;
        return new()
        {
            TopRight = header.TopRight != 0 ? source.ParseRecordAt<MathKern>(header.TopRight) : null,
            TopLeft = header.TopLeft != 0 ? source.ParseRecordAt<MathKern>(header.TopLeft) : null,
            BottomRight = header.BottomRight != 0 ? source.ParseRecordAt<MathKern>(header.BottomRight) : null,
            BottomLeft = header.BottomLeft != 0 ? source.ParseRecordAt<MathKern>(header.BottomLeft) : null,
        };
    }

    /// <summary>Four Offset16 values, relative to the start of the MathKernInfo table.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All four offsets are measured from the start of the enclosing <see cref="MathKernInfo"/> table, not from the record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathkerninfo-table">MathKernInfo table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MathKernInfoRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathkerninfo-table">OpenType specification: MathKernInfo table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the offset to the top-right kerning table, or zero when absent.</summary>
        /// <value>The byte offset of the top-right <see cref="MathKern"/> from the MathKernInfo table start, or zero.</value>
        /// <seealso cref="TopLeft"/>
        public ushort TopRight;   // +0

        /// <summary>Gets the offset to the top-left kerning table, or zero when absent.</summary>
        /// <value>The byte offset of the top-left <see cref="MathKern"/> from the MathKernInfo table start, or zero.</value>
        /// <seealso cref="TopRight"/>
        public ushort TopLeft;   // +2

        /// <summary>Gets the offset to the bottom-right kerning table, or zero when absent.</summary>
        /// <value>The byte offset of the bottom-right <see cref="MathKern"/> from the MathKernInfo table start, or zero.</value>
        /// <seealso cref="BottomLeft"/>
        public ushort BottomRight;   // +4

        /// <summary>Gets the offset to the bottom-left kerning table, or zero when absent.</summary>
        /// <value>The byte offset of the bottom-left <see cref="MathKern"/> from the MathKernInfo table start, or zero.</value>
        /// <seealso cref="BottomRight"/>
        public ushort BottomLeft;   // +6

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All four fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            TopRight = BinaryPrimitives.ReverseEndianness(v.TopRight),
            TopLeft = BinaryPrimitives.ReverseEndianness(v.TopLeft),
            BottomRight = BinaryPrimitives.ReverseEndianness(v.BottomRight),
            BottomLeft = BinaryPrimitives.ReverseEndianness(v.BottomLeft),
        };
    }
}

/// <summary>A per-corner math kerning table: a stepwise function mapping glyph height to kerning correction.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table has N <see cref="CorrectionHeights"/> and N+1 <see cref="KernValues"/>. The extra kern value serves as the fallback for heights beyond the last correction height.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathkern-table">MathKern table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MathKernInfoRecord"/>
/// <seealso cref="MathValueRecord"/>
/// <seealso cref="GetKernValue(short)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathkern-table">OpenType specification: MathKern table</seealso>
public sealed record MathKern : IRecord<MathKern>
{
    /// <summary>Gets the correction heights in ascending order.</summary>
    /// <value>The ordered list of <see cref="MathValueRecord"/> heights that define the stepwise function's break points.</value>
    /// <seealso cref="KernValues"/>
    public IReadOnlyList<MathValueRecord> CorrectionHeights { get; init; } = [];

    /// <summary>Gets the kern values.</summary>
    /// <value>The ordered list of <see cref="MathValueRecord"/> kerning values; length is <c><see cref="CorrectionHeights"/>.Count + 1</c>, with the extra entry serving as the fallback beyond the last height.</value>
    /// <seealso cref="CorrectionHeights"/>
    public IReadOnlyList<MathValueRecord> KernValues { get; init; } = [];

    /// <summary>Returns the kerning value for a given glyph height.</summary>
    /// <param name="height">The glyph height to look up.</param>
    /// <returns>The kern value whose corresponding correction height is the first one greater than <paramref name="height"/>, or the final fallback value when no correction height is greater.</returns>
    /// <remarks>The lookup is linear over <see cref="CorrectionHeights"/>. The final entry of <see cref="KernValues"/> is returned when the height exceeds every correction height.</remarks>
    /// <seealso cref="CorrectionHeights"/>
    /// <seealso cref="KernValues"/>
    public short GetKernValue(short height)
    {
        for (int i = 0; i < CorrectionHeights.Count; i++)
            if (height < CorrectionHeights[i].Value)
                return KernValues[i].Value;
        return KernValues[^1].Value;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the MathKern table.</param>
    /// <param name="context">Unused. The table is self-describing.</param>
    /// <returns>The parsed MathKern table.</returns>
    /// <exception cref="EndOfStreamException">The count, offset arrays, or any referenced value record extends past the end of the source.</exception>
    /// <remarks>The parser reads N correction heights followed by N+1 kern values, where N is the value at the table start.</remarks>
    /// <seealso cref="CorrectionHeights"/>
    /// <seealso cref="KernValues"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathkern-table">OpenType specification: MathKern table</seealso>
    public static MathKern Parse(ref Cursor cursor, object? context)
    {
        int heightCount = cursor.ReadUInt16();
        ParentContext parent = new(cursor.Source);
        return new MathKern
        {
            CorrectionHeights = cursor.ReadOffset16ArrayPeekRecord<MathValueRecord>(heightCount, parent),
            KernValues = cursor.ReadOffset16ArrayPeekRecord<MathValueRecord>(heightCount + 1, parent)
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// MathVariants, MathGlyphConstruction, GlyphAssembly
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The size-variant subtable of the MATH table: gives each eligible glyph a set of pre-drawn variants and, optionally, a recipe for assembling larger versions from component parts.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Vertical and horizontal constructions are independent; a glyph may participate in one, both, or neither.</description></item>
/// <item><description><see cref="MinConnectorOverlap"/> gives the minimum overlap required between adjacent parts when assembling a stretchy glyph.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathvariants-table">MathVariants table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MathTable"/>
/// <seealso cref="MathGlyphConstruction"/>
/// <seealso cref="Coverage"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathvariants-table">OpenType specification: MathVariants table</seealso>
public sealed record MathVariants : IRecord<MathVariants>
{
    /// <summary>Gets the minimum connector overlap between adjacent glyph parts.</summary>
    /// <value>The minimum number of design units by which adjacent parts must overlap when assembling a stretchy glyph.</value>
    /// <seealso cref="VertGlyphConstructions"/>
    public ushort MinConnectorOverlap { get; init; }

    /// <summary>Gets the coverage table listing glyphs with vertical size variants.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>VertGlyphConstructions[i]</c>.</value>
    /// <seealso cref="VertGlyphConstructions"/>
    public Coverage VertGlyphCoverage { get; init; } = null!;

    /// <summary>Gets the coverage table listing glyphs with horizontal size variants.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>HorizGlyphConstructions[i]</c>.</value>
    /// <seealso cref="HorizGlyphConstructions"/>
    public Coverage HorizGlyphCoverage { get; init; } = null!;

    /// <summary>Gets the vertical glyph constructions in Vert Coverage Index order.</summary>
    /// <value>The ordered list of <see cref="MathGlyphConstruction"/> entries; length equals <see cref="VertGlyphCoverage"/>'s glyph count.</value>
    /// <seealso cref="GetVertConstruction(int)"/>
    /// <seealso cref="MathGlyphConstruction"/>
    public IReadOnlyList<MathGlyphConstruction> VertGlyphConstructions { get; init; } = [];

    /// <summary>Gets the horizontal glyph constructions in Horiz Coverage Index order.</summary>
    /// <value>The ordered list of <see cref="MathGlyphConstruction"/> entries; length equals <see cref="HorizGlyphCoverage"/>'s glyph count.</value>
    /// <seealso cref="GetHorizConstruction(int)"/>
    /// <seealso cref="MathGlyphConstruction"/>
    public IReadOnlyList<MathGlyphConstruction> HorizGlyphConstructions { get; init; } = [];

    /// <summary>Returns the vertical construction for a glyph, or <c>null</c> when uncovered.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The glyph's vertical <see cref="MathGlyphConstruction"/>, or <c>null</c> when the glyph is not covered.</returns>
    /// <seealso cref="VertGlyphConstructions"/>
    public MathGlyphConstruction? GetVertConstruction(int glyphId)
    {
        int i = VertGlyphCoverage.GetCoverageIndex(glyphId);
        return i >= 0 && i < VertGlyphConstructions.Count ? VertGlyphConstructions[i] : null;
    }

    /// <summary>Returns the horizontal construction for a glyph, or <c>null</c> when uncovered.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The glyph's horizontal <see cref="MathGlyphConstruction"/>, or <c>null</c> when the glyph is not covered.</returns>
    /// <seealso cref="HorizGlyphConstructions"/>
    public MathGlyphConstruction? GetHorizConstruction(int glyphId)
    {
        int i = HorizGlyphCoverage.GetCoverageIndex(glyphId);
        return i >= 0 && i < HorizGlyphConstructions.Count ? HorizGlyphConstructions[i] : null;
    }

    /// <summary>The 10-byte MathVariants header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both coverage offsets and both glyph-count fields are measured from the start of the MathVariants record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathvariants-table">MathVariants table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MathVariants"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathvariants-table">OpenType specification: MathVariants table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the minimum connector overlap between adjacent glyph parts.</summary>
        /// <value>The minimum number of design units by which adjacent parts must overlap.</value>
        /// <seealso cref="VertCoverageOffset"/>
        public ushort MinConnectorOverlap;   // +0

        /// <summary>Gets the offset to the vertical coverage table.</summary>
        /// <value>The byte offset of the vertical <see cref="Coverage"/> from the MathVariants record start.</value>
        /// <seealso cref="HorizCoverageOffset"/>
        public ushort VertCoverageOffset;   // +2

        /// <summary>Gets the offset to the horizontal coverage table.</summary>
        /// <value>The byte offset of the horizontal <see cref="Coverage"/> from the MathVariants record start.</value>
        /// <seealso cref="VertCoverageOffset"/>
        public ushort HorizCoverageOffset;   // +4

        /// <summary>Gets the number of vertical glyph constructions.</summary>
        /// <value>The count of Offset16 entries for vertical constructions; must equal the vertical coverage table's glyph count.</value>
        /// <seealso cref="HorizGlyphCount"/>
        public ushort VertGlyphCount;   // +6

        /// <summary>Gets the number of horizontal glyph constructions.</summary>
        /// <value>The count of Offset16 entries for horizontal constructions; must equal the horizontal coverage table's glyph count.</value>
        /// <seealso cref="VertGlyphCount"/>
        public ushort HorizGlyphCount;   // +8

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All five fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            MinConnectorOverlap = BinaryPrimitives.ReverseEndianness(v.MinConnectorOverlap),
            VertCoverageOffset = BinaryPrimitives.ReverseEndianness(v.VertCoverageOffset),
            HorizCoverageOffset = BinaryPrimitives.ReverseEndianness(v.HorizCoverageOffset),
            VertGlyphCount = BinaryPrimitives.ReverseEndianness(v.VertGlyphCount),
            HorizGlyphCount = BinaryPrimitives.ReverseEndianness(v.HorizGlyphCount),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the MathVariants subtable.</param>
    /// <param name="context">Unused. The subtable resolves its coverages and constructions through its own source.</param>
    /// <returns>The parsed MathVariants subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, coverages, or any construction array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathvariants-table">MathVariants table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="MathGlyphConstruction"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathvariants-table">OpenType specification: MathVariants table</seealso>
    public static MathVariants Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new MathVariants
        {
            MinConnectorOverlap = header.MinConnectorOverlap,
            VertGlyphCoverage = cursor.Source.ParseRecordAt<Coverage>(header.VertCoverageOffset),
            HorizGlyphCoverage = cursor.Source.ParseRecordAt<Coverage>(header.HorizCoverageOffset),
            VertGlyphConstructions = cursor.ReadOffset16ArrayPeekRecord<MathGlyphConstruction>(header.VertGlyphCount),
            HorizGlyphConstructions = cursor.ReadOffset16ArrayPeekRecord<MathGlyphConstruction>(header.HorizGlyphCount),
        };
    }
}

/// <summary>The size-variant and assembly data for one glyph: an optional assembly recipe plus a list of pre-drawn size variants.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>When a glyph must be sized to a target advance, a renderer first tries the pre-drawn <see cref="Variants"/> and, if none is large enough, uses the <see cref="Assembly"/> to build a larger version.</description></item>
/// <item><description>The variants array is ordered by increasing advance, so a renderer can pick the smallest variant that satisfies the target.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathglyphconstruction-table">MathGlyphConstruction table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MathVariants"/>
/// <seealso cref="GlyphAssembly"/>
/// <seealso cref="MathGlyphVariantRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathglyphconstruction-table">OpenType specification: MathGlyphConstruction table</seealso>
public sealed record MathGlyphConstruction : IRecord<MathGlyphConstruction>
{
    /// <summary>Gets the assembly recipe, or <c>null</c>.</summary>
    /// <value>The <see cref="GlyphAssembly"/> that describes how to build the glyph from component parts, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="Variants"/>
    /// <seealso cref="GlyphAssembly"/>
    public GlyphAssembly? Assembly { get; init; }

    /// <summary>Gets the pre-drawn size variants, ordered by increasing advance.</summary>
    /// <value>The ordered list of <see cref="MathGlyphVariantRecord"/> entries; sorted ascending by <see cref="MathGlyphVariantRecord.AdvanceMeasurement"/>.</value>
    /// <seealso cref="VariantCount"/>
    /// <seealso cref="MathGlyphVariantRecord"/>
    public IReadOnlyList<MathGlyphVariantRecord> Variants { get; init; } = [];

    /// <summary>Gets the number of pre-drawn variants.</summary>
    /// <value>The size of the <see cref="Variants"/> list.</value>
    /// <seealso cref="Variants"/>
    public int VariantCount => Variants.Count;

    /// <summary>The 4-byte MathGlyphConstruction header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The assembly offset is measured from the start of the MathGlyphConstruction record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathglyphconstruction-table">MathGlyphConstruction table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MathGlyphConstruction"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathglyphconstruction-table">OpenType specification: MathGlyphConstruction table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the offset to the assembly recipe, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="GlyphAssembly"/> from the MathGlyphConstruction record start, or zero.</value>
        /// <seealso cref="VariantCount"/>
        public ushort AssemblyOffset;   // +0

        /// <summary>Gets the number of pre-drawn variants.</summary>
        /// <value>The count of <see cref="MathGlyphVariantRecord"/> entries that follow the header.</value>
        /// <seealso cref="AssemblyOffset"/>
        public ushort VariantCount;   // +2

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            AssemblyOffset = BinaryPrimitives.ReverseEndianness(v.AssemblyOffset),
            VariantCount = BinaryPrimitives.ReverseEndianness(v.VariantCount),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the MathGlyphConstruction record.</param>
    /// <param name="context">Unused. The record resolves its subtables through its own source.</param>
    /// <returns>The parsed MathGlyphConstruction record.</returns>
    /// <exception cref="EndOfStreamException">The header, referenced assembly, or variant array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathglyphconstruction-table">MathGlyphConstruction table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="GlyphAssembly"/>
    /// <seealso cref="MathGlyphVariantRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathglyphconstruction-table">OpenType specification: MathGlyphConstruction table</seealso>
    public static MathGlyphConstruction Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new MathGlyphConstruction
        {
            Assembly = header.AssemblyOffset != 0 ? cursor.Source.ParseRecordAt<GlyphAssembly>(header.AssemblyOffset) : null,
            Variants = cursor.ReadBigEndianStructArray<MathGlyphVariantRecord>(header.VariantCount),
        };
    }
}

/// <summary>A size variant: a glyph and its advance along the construction axis. Blittable.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The variant glyph is typically a pre-drawn alternate of the base glyph, sized larger; the advance measurement tells a renderer how much space it occupies.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathglyphconstruction-table">MathGlyphConstruction table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MathGlyphConstruction"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#mathglyphconstruction-table">OpenType specification: MathGlyphConstruction table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct MathGlyphVariantRecord : IEndianReversibleStruct<MathGlyphVariantRecord>
{
    /// <summary>Gets the variant glyph ID.</summary>
    /// <value>The glyph ID of the pre-drawn size variant.</value>
    /// <seealso cref="AdvanceMeasurement"/>
    public ushort VariantGlyph;   // +0

    /// <summary>Gets the advance along the construction axis, in design units.</summary>
    /// <value>The advance measurement of the variant glyph along the axis the construction applies to (vertical or horizontal).</value>
    /// <seealso cref="VariantGlyph"/>
    public ushort AdvanceMeasurement;   // +2

    /// <inheritdoc/>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new record with both fields reversed.</returns>
    /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    public static MathGlyphVariantRecord ReverseEndianness(MathGlyphVariantRecord v) => new()
    {
        VariantGlyph = BinaryPrimitives.ReverseEndianness(v.VariantGlyph),
        AdvanceMeasurement = BinaryPrimitives.ReverseEndianness(v.AdvanceMeasurement),
    };
}

/// <summary>A recipe for assembling a large glyph from component parts: an italics correction plus an ordered list of <see cref="GlyphPartRecord"/> parts.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The parts are assembled in order along the construction axis; adjacent parts must overlap by at least <see cref="MathVariants.MinConnectorOverlap"/> design units.</description></item>
/// <item><description>Parts marked as extenders (see <see cref="GlyphPartRecord.IsExtender"/>) may be repeated any number of times to reach a target advance.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#glyphassembly-table">GlyphAssembly table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MathGlyphConstruction"/>
/// <seealso cref="GlyphPartRecord"/>
/// <seealso cref="MathValueRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#glyphassembly-table">OpenType specification: GlyphAssembly table</seealso>
public sealed record GlyphAssembly : IRecord<GlyphAssembly>
{
    /// <summary>Gets the italics correction applied to the assembled glyph.</summary>
    /// <value>The <see cref="MathValueRecord"/> that gives the assembled glyph's italics correction.</value>
    /// <seealso cref="Parts"/>
    public MathValueRecord ItalicsCorrection { get; init; } = null!;

    /// <summary>Gets the component parts in assembly order.</summary>
    /// <value>The ordered list of <see cref="GlyphPartRecord"/> entries; parts are drawn in this order along the construction axis.</value>
    /// <seealso cref="PartCount"/>
    /// <seealso cref="GlyphPartRecord"/>
    public IReadOnlyList<GlyphPartRecord> Parts { get; init; } = [];

    /// <summary>Gets the number of component parts.</summary>
    /// <value>The size of the <see cref="Parts"/> list.</value>
    /// <seealso cref="Parts"/>
    public int PartCount => Parts.Count;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the GlyphAssembly subtable.</param>
    /// <param name="context">Unused. The subtable is self-describing.</param>
    /// <returns>The parsed GlyphAssembly subtable.</returns>
    /// <exception cref="EndOfStreamException">The italics correction record, part count, or part array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#glyphassembly-table">GlyphAssembly table</see> in the OpenType specification.</remarks>
    /// <seealso cref="ItalicsCorrection"/>
    /// <seealso cref="Parts"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#glyphassembly-table">OpenType specification: GlyphAssembly table</seealso>
    public static GlyphAssembly Parse(ref Cursor cursor, object? context) => new()
    {
        ItalicsCorrection = cursor.ReadRecord<MathValueRecord>(new ParentContext(cursor.Source)),
        Parts = cursor.ReadBigEndianStructArray<GlyphPartRecord>(cursor.ReadUInt16())
    };
}

/// <summary>A single component glyph in a GlyphAssembly. Blittable, size 10.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Connector lengths describe how much of the glyph's start and end overlap with adjacent parts; the full advance is the glyph's total advance along the construction axis.</description></item>
/// <item><description>The <see cref="IsExtender"/> predicate wraps bit 0 of <see cref="PartFlags"/>; extender parts may be repeated during assembly.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#glyphpartrecord-table">GlyphPartRecord table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyphAssembly"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/math#glyphpartrecord-table">OpenType specification: GlyphPartRecord table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct GlyphPartRecord : IEndianReversibleStruct<GlyphPartRecord>
{
    /// <summary>Gets the component glyph ID.</summary>
    /// <value>The glyph ID of the part to draw.</value>
    /// <seealso cref="StartConnectorLength"/>
    public ushort GlyphId;   // +0

    /// <summary>Gets the start connector length, in design units.</summary>
    /// <value>The length of the glyph's start connector; adjacent parts must overlap by at least this much on the leading edge.</value>
    /// <seealso cref="EndConnectorLength"/>
    public ushort StartConnectorLength;   // +2

    /// <summary>Gets the end connector length, in design units.</summary>
    /// <value>The length of the glyph's end connector; adjacent parts must overlap by at least this much on the trailing edge.</value>
    /// <seealso cref="StartConnectorLength"/>
    public ushort EndConnectorLength;   // +4

    /// <summary>Gets the full advance, in design units.</summary>
    /// <value>The glyph's total advance along the construction axis.</value>
    /// <seealso cref="GlyphId"/>
    public ushort FullAdvance;   // +6

    /// <summary>Gets the raw part flags.</summary>
    /// <value>Bit 0 indicates an extender; all other bits are reserved and must be zero.</value>
    /// <remarks>Use <see cref="IsExtender"/> to test bit 0 rather than checking <see cref="PartFlags"/> directly.</remarks>
    /// <seealso cref="IsExtender"/>
    public ushort PartFlags;   // +8

    /// <summary>True when the part is an extender that may be repeated during assembly.</summary>
    /// <value><see langword="true"/> when bit 0 of <see cref="PartFlags"/> is set.</value>
    /// <seealso cref="PartFlags"/>
    public readonly bool IsExtender => (PartFlags & 0x0001) != 0;

    /// <inheritdoc/>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new record with each multi-byte field reversed.</returns>
    /// <remarks>All five fields are <c>uint16</c> and are reversed independently.</remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    public static GlyphPartRecord ReverseEndianness(GlyphPartRecord v) => new()
    {
        GlyphId = BinaryPrimitives.ReverseEndianness(v.GlyphId),
        StartConnectorLength = BinaryPrimitives.ReverseEndianness(v.StartConnectorLength),
        EndConnectorLength = BinaryPrimitives.ReverseEndianness(v.EndConnectorLength),
        FullAdvance = BinaryPrimitives.ReverseEndianness(v.FullAdvance),
        PartFlags = BinaryPrimitives.ReverseEndianness(v.PartFlags),
    };
}
