using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>Conditional replacement of feature lookups in a variable font.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A FeatureVariations table lets a variable font substitute an entirely different <see cref="Feature"/> for a given index, based on a condition set evaluated against the current normalized variation coordinates.</description></item>
/// <item><description>Each <see cref="FeatureVariationRecord"/> pairs a condition set with a set of feature-index substitutions. The first record whose conditions are satisfied wins.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-variations">Feature Variations</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatureVariationRecord"/>
/// <seealso cref="ConditionSet"/>
/// <seealso cref="FeatureTableSubstitution"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-variations">OpenType specification: Feature Variations</seealso>
public sealed record FeatureVariations : IRecord<FeatureVariations>
{
    /// <summary>Gets the major version. Always 1.</summary>
    /// <value>The constant <c>1</c> for a conforming FeatureVariations table.</value>
    /// <seealso cref="MinorVersion"/>
    /// <seealso cref="Header.MajorVersion"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version. Always 0.</summary>
    /// <value>The constant <c>0</c> for a conforming FeatureVariations table.</value>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="Header.MinorVersion"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the feature variation records.</summary>
    /// <value>The ordered list of <see cref="FeatureVariationRecord"/> entries. Evaluation is first-match-wins over this list.</value>
    /// <seealso cref="FeatureVariationRecord"/>
    /// <seealso cref="ConditionSet"/>
    public IReadOnlyList<FeatureVariationRecord> Records { get; init; } = [];

    /// <summary>The 8-byte FeatureVariations header: majorVersion, minorVersion, featureVariationRecordCount.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by <c>featureVariationRecordCount</c> 8-byte <see cref="RecordHeader"/> entries.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-variations">Feature Variations</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatureVariations"/>
    /// <seealso cref="RecordHeader"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-variations">OpenType specification: Feature Variations</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the major version. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming FeatureVariations table.</value>
        /// <seealso cref="MinorVersion"/>
        /// <seealso cref="FeatureVariationRecordCount"/>
        public ushort MajorVersion;

        /// <summary>Gets the minor version. Always 0.</summary>
        /// <value>The constant <c>0</c> for a conforming FeatureVariations table.</value>
        /// <seealso cref="MajorVersion"/>
        /// <seealso cref="FeatureVariationRecordCount"/>
        public ushort MinorVersion;

        /// <summary>Gets the number of feature variation records that follow.</summary>
        /// <value>The count of <see cref="RecordHeader"/> entries; the parser rejects values above 65 535 as a safety limit.</value>
        /// <seealso cref="MajorVersion"/>
        /// <seealso cref="RecordHeader"/>
        public uint FeatureVariationRecordCount;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            MajorVersion = BinaryPrimitives.ReverseEndianness(v.MajorVersion),
            MinorVersion = BinaryPrimitives.ReverseEndianness(v.MinorVersion),
            FeatureVariationRecordCount = BinaryPrimitives.ReverseEndianness(v.FeatureVariationRecordCount),
        };
    }

    /// <summary>The 8-byte FeatureVariationRecord on-disk form: two Offset32 values that are relative to the start of the FeatureVariations table.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both offsets are checked for zero before parsing; a zero offset yields a <c>null</c> property on the parsed record rather than an exception.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-variations">Feature Variations</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatureVariations"/>
    /// <seealso cref="FeatureVariationRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-variations">OpenType specification: Feature Variations</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct RecordHeader : IEndianReversibleStruct<RecordHeader>
    {
        /// <summary>Gets the offset to the condition set, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="ConditionSet"/> from the FeatureVariations table start, or zero for a universal match (the conditions are trivially satisfied).</value>
        /// <seealso cref="SubstitutionOffset"/>
        /// <seealso cref="ConditionSet"/>
        public uint ConditionSetOffset;

        /// <summary>Gets the offset to the feature substitution, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="FeatureTableSubstitution"/> from the FeatureVariations table start, or zero when no substitutions apply.</value>
        /// <seealso cref="ConditionSetOffset"/>
        /// <seealso cref="FeatureTableSubstitution"/>
        public uint SubstitutionOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new record header with both offsets reversed.</returns>
        /// <remarks>Both fields are <c>uint32</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static RecordHeader ReverseEndianness(RecordHeader v) => new()
        {
            ConditionSetOffset = BinaryPrimitives.ReverseEndianness(v.ConditionSetOffset),
            SubstitutionOffset = BinaryPrimitives.ReverseEndianness(v.SubstitutionOffset),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the FeatureVariations table.</param>
    /// <param name="context">Unused. The table resolves its records through its own source.</param>
    /// <returns>The parsed FeatureVariations table with its records resolved.</returns>
    /// <exception cref="InvalidDataException">The major version is not 1, or the record count exceeds 65 535.</exception>
    /// <exception cref="EndOfStreamException">The header, record array, or any referenced subtable extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <description>The record headers are materialized as a struct array and then converted to <see cref="FeatureVariationRecord"/> entries in one pass; each entry's offsets are then resolved against the table-scoped source.</description>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-variations">Feature Variations</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="FeatureVariationRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-variations">OpenType specification: Feature Variations</seealso>
    static FeatureVariations IRecord<FeatureVariations>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.MajorVersion != 1)
            throw new InvalidDataException(
                $"FeatureVariations majorVersion is {header.MajorVersion}, expected 1.");

        int count = (int)header.FeatureVariationRecordCount;

        if (count > 65535)
            throw new InvalidDataException(
                $"FeatureVariations recordCount {count} exceeds the safety limit.");

        return new FeatureVariations
        {
            MajorVersion = header.MajorVersion,
            MinorVersion = header.MinorVersion,
            Records = count == 0 ? [] : cursor.ReadBigEndianStructArray<RecordHeader>(count).ToRecords<FeatureVariationRecord, RecordHeader>(new ParentContext(cursor.Source)),
        };
    }
}

/// <summary>A feature variation record: a condition set plus the substitutions to apply when the condition set matches.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <see cref="ConditionSet"/> identifies a region of variation space; when the current normalized coordinates fall inside it, the substitutions take effect.</description></item>
/// <item><description>A <c>null</c> condition set represents a universal match; a <c>null</c> substitution means the record applies no changes.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-variations">Feature Variations</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatureVariations"/>
/// <seealso cref="ConditionSet"/>
/// <seealso cref="FeatureTableSubstitution"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-variations">OpenType specification: Feature Variations</seealso>
public sealed record FeatureVariationRecord : IEndianReversibleHeaderRecord<FeatureVariationRecord, FeatureVariations.RecordHeader>
{
    /// <summary>Gets the condition set, or <c>null</c> for a universal match.</summary>
    /// <value>The <see cref="ConditionSet"/> resolved from the record's condition-set offset, or <c>null</c> when the offset was zero.</value>
    /// <remarks>A <c>null</c> condition set means the record applies unconditionally; it should be the last record in the list.</remarks>
    /// <seealso cref="FeatureTableSubstitution"/>
    /// <seealso cref="ConditionSet"/>
    public ConditionSet? ConditionSet { get; init; }

    /// <summary>Gets the substitution, or <c>null</c>.</summary>
    /// <value>The <see cref="FeatureTableSubstitution"/> resolved from the record's substitution offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="ConditionSet"/>
    /// <seealso cref="FeatureTableSubstitution"/>
    public FeatureTableSubstitution? FeatureTableSubstitution { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read <see cref="FeatureVariations.RecordHeader"/> header.</param>
    /// <param name="context">A <see cref="ParentContext"/> whose <see cref="IParentContext.ParentSource"/> is the table-scoped source.</param>
    /// <returns>A new record with its condition set and substitution resolved.</returns>
    /// <remarks>Both offsets are measured from the start of the enclosing <see cref="FeatureVariations"/> table; the parent context carries the table-scoped source so the offsets resolve correctly.</remarks>
    /// <seealso cref="FeatureVariations.RecordHeader"/>
    /// <seealso cref="ConditionSet"/>
    /// <seealso cref="FeatureTableSubstitution"/>
    static FeatureVariationRecord IHeaderRecord<FeatureVariationRecord, FeatureVariations.RecordHeader>.FromHeader(in FeatureVariations.RecordHeader header, object? context)
    {
        Source source = ((ParentContext)context!).ParentSource;
        return new FeatureVariationRecord
        {
            ConditionSet = header.ConditionSetOffset != 0 ? source.ParseRecordAt<ConditionSet>(header.ConditionSetOffset) : null,
            FeatureTableSubstitution = header.SubstitutionOffset != 0 ? source.ParseRecordAt<FeatureTableSubstitution>(header.SubstitutionOffset) : null,
        };
    }
}

/// <summary>A set of conditions that must all be satisfied.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>All conditions in the set are logically ANDed: the set matches only when every individual condition is satisfied at the current normalized coordinates.</description></item>
/// <item><description>An empty condition set matches unconditionally.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#condition-set-table">ConditionSet table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Condition"/>
/// <seealso cref="ConditionFormat1"/>
/// <seealso cref="FeatureVariationRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#condition-set-table">OpenType specification: ConditionSet table</seealso>
public sealed record ConditionSet : IRecord<ConditionSet>
{
    /// <summary>Gets the conditions.</summary>
    /// <value>The ordered list of <see cref="Condition"/> entries, all of which must be satisfied for the set to match.</value>
    /// <seealso cref="Condition"/>
    public IReadOnlyList<Condition> Conditions { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the condition set.</param>
    /// <param name="context">Forwarded to the condition parsers.</param>
    /// <returns>The parsed condition set.</returns>
    /// <exception cref="EndOfStreamException">The count, condition array, or any referenced condition extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#condition-set-table">ConditionSet table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Conditions"/>
    /// <seealso cref="Condition"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#condition-set-table">OpenType specification: ConditionSet table</seealso>
    static ConditionSet IRecord<ConditionSet>.Parse(ref Cursor cursor, object? context) => new()
    {
        Conditions = cursor.ReadOffset32ArrayPeekRecord<Condition>(cursor.ReadUInt16()),
    };
}

/// <summary>Base type for condition tables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each condition tests a specific property of the current variation instance — format 1 tests whether an axis coordinate falls inside a range.</description></item>
/// <item><description>Conditions are stateless and evaluated against the normalized coordinates supplied by the caller; the condition itself has no dependency on the font face.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#condition-table">Condition table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ConditionFormat1"/>
/// <seealso cref="ConditionSet"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#condition-table">OpenType specification: Condition table</seealso>
public abstract record Condition : IRecord<Condition>, IBaseRecord<Condition>
{
    /// <summary>Gets the format number. Only format 1 is defined.</summary>
    /// <value>The format discriminant from the first <c>uint16</c> of the condition record.</value>
    /// <seealso cref="ConditionFormat1"/>
    public ushort Format { get; init; }

    /// <summary>Returns true when the condition is satisfied at the given normalized variation-space coordinates.</summary>
    /// <param name="normalizedCoordinates">The current normalized axis coordinates, one per axis in the font's <c>fvar</c> table.</param>
    /// <returns><see langword="true"/> when the condition holds at <paramref name="normalizedCoordinates"/>; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The span's length must equal the number of axes in the font's <c>fvar</c> table; conditions that reference an axis index beyond the span's length are treated as unsatisfied.</description></item>
    /// <item><description>The check is a pure function of the condition and the coordinates; no font-face state is read.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ConditionFormat1.IsSatisfied(ReadOnlySpan{F2Dot14})"/>
    /// <seealso cref="ConditionSet"/>
    /// <seealso cref="F2Dot14"/>
    public abstract bool IsSatisfied(ReadOnlySpan<F2Dot14> normalizedCoordinates);

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the condition record.</param>
    /// <param name="context">Forwarded to the format-specific parser.</param>
    /// <returns>The format-specific condition.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#condition-table">Condition table</see> in the OpenType specification.</remarks>
    /// <seealso cref="IBaseRecord{TBase}"/>
    /// <seealso cref="ConditionFormat1"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#condition-table">OpenType specification: Condition table</seealso>
    static Condition IRecord<Condition>.Parse(ref Cursor cursor, object? context)
    {
        ushort format = cursor.ReadUInt16();
        return format switch
        {
            1 => IBaseRecord<Condition>.Parse<ConditionFormat1>(ref cursor, context),
            _ => throw new InvalidDataException($"Condition format {format} is not defined."),
        };
    }
}

/// <summary>Condition format 1: a range on a single variation axis.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The condition is satisfied when the normalized coordinate at <see cref="AxisIndex"/> lies in the inclusive interval <c>[<see cref="MinValue"/>, <see cref="MaxValue"/>]</c>.</description></item>
/// <item><description>Coordinates and range bounds are all in the normalized variation space, typically <c>[-1, 1]</c> for continuous axes.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#condition-format-1">Condition Format 1</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Condition"/>
/// <seealso cref="ConditionSet"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#condition-format-1">OpenType specification: Condition Format 1</seealso>
public sealed record ConditionFormat1 : Condition, IEndianReversibleHeaderDerivedRecord<Condition, ConditionFormat1, ConditionFormat1.Header>
{
    /// <summary>Gets the axis index into the fvar axis array.</summary>
    /// <value>The zero-based index of the axis whose normalized coordinate is tested.</value>
    /// <seealso cref="MinValue"/>
    /// <seealso cref="MaxValue"/>
    public ushort AxisIndex { get; init; }

    /// <summary>Gets the minimum value of the range (inclusive).</summary>
    /// <value>The lower bound of the satisfied interval in normalized variation space.</value>
    /// <seealso cref="AxisIndex"/>
    /// <seealso cref="MaxValue"/>
    public F2Dot14 MinValue { get; init; }

    /// <summary>Gets the maximum value of the range (inclusive).</summary>
    /// <value>The upper bound of the satisfied interval in normalized variation space.</value>
    /// <seealso cref="AxisIndex"/>
    /// <seealso cref="MinValue"/>
    public F2Dot14 MaxValue { get; init; }

    /// <inheritdoc/>
    /// <param name="coords">The current normalized axis coordinates.</param>
    /// <returns><see langword="true"/> when <see cref="AxisIndex"/> is in range and <c>coords[AxisIndex]</c> lies in <c>[<see cref="MinValue"/>, <see cref="MaxValue"/>]</c>.</returns>
    /// <remarks>An out-of-range <see cref="AxisIndex"/> (greater than or equal to the span's length) is treated as unsatisfied rather than throwing.</remarks>
    /// <seealso cref="AxisIndex"/>
    /// <seealso cref="MinValue"/>
    /// <seealso cref="MaxValue"/>
    public override bool IsSatisfied(ReadOnlySpan<F2Dot14> coords) =>
        AxisIndex < coords.Length && coords[AxisIndex] >= MinValue && coords[AxisIndex] <= MaxValue;

    /// <inheritdoc/>
    /// <param name="header">The already-read header.</param>
    /// <param name="context">Unused.</param>
    /// <returns>A new instance populated from the header.</returns>
    static ConditionFormat1 IHeaderDerivedRecord<Condition, ConditionFormat1, Header>.FromHeader(in Header header, object? context) => new()
    {
        Format = 1,
        AxisIndex = header.AxisIndex,
        MinValue = header.MinValue,
        MaxValue = header.MaxValue,
    };

    /// <summary>The 6-byte format 1 body: axisIndex, minValue, maxValue. Blittable, no padding. The format word was consumed by <see cref="IRecord{T}.Parse"/>.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>When this struct is read through the header-derived parse path, its first field is the axis index rather than the format discriminant — the discriminant was consumed by the base parser.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#condition-format-1">Condition Format 1</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ConditionFormat1"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#condition-format-1">OpenType specification: Condition Format 1</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the axis index into the fvar axis array.</summary>
        /// <value>The zero-based index of the axis whose normalized coordinate is tested.</value>
        /// <seealso cref="MinValue"/>
        /// <seealso cref="MaxValue"/>
        public ushort AxisIndex;

        /// <summary>Gets the minimum value of the range (inclusive).</summary>
        /// <value>The lower bound of the satisfied interval in normalized variation space.</value>
        /// <seealso cref="AxisIndex"/>
        /// <seealso cref="MaxValue"/>
        public F2Dot14 MinValue;

        /// <summary>Gets the maximum value of the range (inclusive).</summary>
        /// <value>The upper bound of the satisfied interval in normalized variation space.</value>
        /// <seealso cref="AxisIndex"/>
        /// <seealso cref="MinValue"/>
        public F2Dot14 MaxValue;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>The axis index uses <see cref="BinaryPrimitives.ReverseEndianness(ushort)"/>; the two <see cref="F2Dot14"/> bounds use <see cref="F2Dot14.ReverseEndianness(F2Dot14)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            AxisIndex = BinaryPrimitives.ReverseEndianness(v.AxisIndex),
            MinValue = F2Dot14.ReverseEndianness(v.MinValue),
            MaxValue = F2Dot14.ReverseEndianness(v.MaxValue),
        };
    }
}

/// <summary>Replacement Feature tables for a subset of feature indices.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each entry names an index into the parent FeatureList and provides an alternate <see cref="Feature"/> to use in place of the original when this substitution is active.</description></item>
/// <item><description>Feature indices not mentioned in the substitution list retain their original definitions.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-table-substitution">Feature Table Substitution</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatureTableSubstitutionEntry"/>
/// <seealso cref="Feature"/>
/// <seealso cref="FeatureVariationRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-table-substitution">OpenType specification: Feature Table Substitution</seealso>
public sealed record FeatureTableSubstitution : IRecord<FeatureTableSubstitution>
{
    /// <summary>Gets the major version. Always 1.</summary>
    /// <value>The constant <c>1</c> for a conforming FeatureTableSubstitution table.</value>
    /// <seealso cref="MinorVersion"/>
    /// <seealso cref="Header.MajorVersion"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version. Always 0.</summary>
    /// <value>The constant <c>0</c> for a conforming FeatureTableSubstitution table.</value>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="Header.MinorVersion"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the substitution records.</summary>
    /// <value>The ordered list of <see cref="FeatureTableSubstitutionEntry"/> entries.</value>
    /// <seealso cref="FeatureTableSubstitutionEntry"/>
    public IReadOnlyList<FeatureTableSubstitutionEntry> Records { get; init; } = [];

    /// <summary>The 6-byte FeatureTableSubstitution header: majorVersion, minorVersion, substitutionCount.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by <c>substitutionCount</c> 6-byte <see cref="RecordHeader"/> entries.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-table-substitution">Feature Table Substitution</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatureTableSubstitution"/>
    /// <seealso cref="RecordHeader"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-table-substitution">OpenType specification: Feature Table Substitution</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the major version. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming FeatureTableSubstitution table.</value>
        /// <seealso cref="MinorVersion"/>
        /// <seealso cref="SubstitutionCount"/>
        public ushort MajorVersion;

        /// <summary>Gets the minor version. Always 0.</summary>
        /// <value>The constant <c>0</c> for a conforming FeatureTableSubstitution table.</value>
        /// <seealso cref="MajorVersion"/>
        /// <seealso cref="SubstitutionCount"/>
        public ushort MinorVersion;

        /// <summary>Gets the number of substitution records that follow.</summary>
        /// <value>The count of <see cref="RecordHeader"/> entries.</value>
        /// <seealso cref="MajorVersion"/>
        /// <seealso cref="RecordHeader"/>
        public ushort SubstitutionCount;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            MajorVersion = BinaryPrimitives.ReverseEndianness(v.MajorVersion),
            MinorVersion = BinaryPrimitives.ReverseEndianness(v.MinorVersion),
            SubstitutionCount = BinaryPrimitives.ReverseEndianness(v.SubstitutionCount),
        };
    }

    /// <summary>The 6-byte substitution record: featureIndex (uint16) and alternateFeatureOffset (Offset32, relative to the FeatureTableSubstitution start).</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The alternate-feature offset is measured from the start of the FeatureTableSubstitution table, not from the enclosing FeatureVariations table.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-table-substitution">Feature Table Substitution</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatureTableSubstitution"/>
    /// <seealso cref="FeatureTableSubstitutionEntry"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-table-substitution">OpenType specification: Feature Table Substitution</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct RecordHeader : IEndianReversibleStruct<RecordHeader>
    {
        /// <summary>Gets the feature index into the parent FeatureList.</summary>
        /// <value>The zero-based index of the <see cref="Feature"/> to replace.</value>
        /// <seealso cref="AlternateFeatureOffset"/>
        public ushort FeatureIndex;

        /// <summary>Gets the offset to the alternate Feature, relative to the FeatureTableSubstitution start.</summary>
        /// <value>The byte offset of the alternate <see cref="Feature"/> from the FeatureTableSubstitution table start.</value>
        /// <seealso cref="FeatureIndex"/>
        /// <seealso cref="Feature"/>
        public uint AlternateFeatureOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new record header with both fields reversed.</returns>
        /// <remarks>Both fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static RecordHeader ReverseEndianness(RecordHeader v) => new()
        {
            FeatureIndex = BinaryPrimitives.ReverseEndianness(v.FeatureIndex),
            AlternateFeatureOffset = BinaryPrimitives.ReverseEndianness(v.AlternateFeatureOffset),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the FeatureTableSubstitution table.</param>
    /// <param name="context">Unused. The table resolves its records through its own source.</param>
    /// <returns>The parsed FeatureTableSubstitution table with its records resolved.</returns>
    /// <exception cref="InvalidDataException">The major version is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header, record array, or any referenced feature extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-table-substitution">Feature Table Substitution</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="FeatureTableSubstitutionEntry"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-table-substitution">OpenType specification: Feature Table Substitution</seealso>
    static FeatureTableSubstitution IRecord<FeatureTableSubstitution>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.MajorVersion != 1)
            throw new InvalidDataException(
                $"FeatureTableSubstitution majorVersion is {header.MajorVersion}, expected 1.");
        return new FeatureTableSubstitution
        {
            MajorVersion = header.MajorVersion,
            MinorVersion = header.MinorVersion,
            Records = header.SubstitutionCount == 0 ? [] : cursor.ReadBigEndianStructArray<RecordHeader>(header.SubstitutionCount).ToRecords<FeatureTableSubstitutionEntry, RecordHeader>(new ParentContext(cursor.Source)),
        };
    }
}

/// <summary>An entry in a <see cref="FeatureTableSubstitution"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The entry pairs a feature index with an alternate <see cref="Feature"/> that replaces the original when this substitution is active.</description></item>
/// <item><description>The alternate feature is a full <see cref="Feature"/>; it carries its own lookup indices and optional params block.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-table-substitution">Feature Table Substitution</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatureTableSubstitution"/>
/// <seealso cref="Feature"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#feature-table-substitution">OpenType specification: Feature Table Substitution</seealso>
public sealed record FeatureTableSubstitutionEntry : IEndianReversibleHeaderRecord<FeatureTableSubstitutionEntry, FeatureTableSubstitution.RecordHeader>
{
    /// <summary>Gets the feature index into the parent FeatureList.</summary>
    /// <value>The zero-based index of the <see cref="Feature"/> to replace.</value>
    /// <seealso cref="AlternateFeature"/>
    public ushort FeatureIndex { get; init; }

    /// <summary>Gets the alternate Feature.</summary>
    /// <value>The <see cref="Feature"/> resolved from the record's alternate-feature offset.</value>
    /// <seealso cref="FeatureIndex"/>
    /// <seealso cref="Feature"/>
    public Feature AlternateFeature { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="header">The already-read <see cref="FeatureTableSubstitution.RecordHeader"/> header.</param>
    /// <param name="context">A <see cref="ParentContext"/> whose <see cref="IParentContext.ParentSource"/> is the substitution-table-scoped source.</param>
    /// <returns>A new entry with its alternate feature resolved.</returns>
    /// <remarks>The alternate-feature offset is measured from the start of the enclosing <see cref="FeatureTableSubstitution"/>; the parent context carries the substitution-table-scoped source so the offset resolves correctly.</remarks>
    /// <seealso cref="FeatureTableSubstitution.RecordHeader"/>
    /// <seealso cref="Feature"/>
    static FeatureTableSubstitutionEntry IHeaderRecord<FeatureTableSubstitutionEntry, FeatureTableSubstitution.RecordHeader>.FromHeader(in FeatureTableSubstitution.RecordHeader header, object? context) => new()
    {
        FeatureIndex = header.FeatureIndex,
        AlternateFeature = ((ParentContext)context!).ParentSource.ParseRecordAt<Feature>(header.AlternateFeatureOffset)
    };
}
