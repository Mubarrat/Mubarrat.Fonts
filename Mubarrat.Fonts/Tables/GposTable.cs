using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using static Mubarrat.Fonts.Tables.Anchor;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════
// Value format
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Bit flags declaring which optional fields are present in a GPOS ValueRecord.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A ValueRecord stores only the fields whose corresponding flag bit is set. The fields are read in declaration order: the four adjustment words (if present), then the four device offsets (if present).</description></item>
/// <item><description>The record's byte size is <c>2 * popcount(format)</c>; see <see cref="GposValueFormatExtensions.Size(GposValueFormat)"/>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#value-record">ValueRecord</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GposValueRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#value-record">OpenType specification: ValueRecord</seealso>
[Flags]
public enum GposValueFormat : ushort
{
    /// <summary>No fields present. The record occupies zero bytes.</summary>
    None = 0,

    /// <summary>Includes <c>XPlacement</c>, a signed horizontal placement adjustment.</summary>
    /// <seealso cref="GposValueRecord.XPlacement"/>
    XPlacement = 0x0001,

    /// <summary>Includes <c>YPlacement</c>, a signed vertical placement adjustment.</summary>
    /// <seealso cref="GposValueRecord.YPlacement"/>
    YPlacement = 0x0002,

    /// <summary>Includes <c>XAdvance</c>, a signed horizontal advance adjustment.</summary>
    /// <seealso cref="GposValueRecord.XAdvance"/>
    XAdvance = 0x0004,

    /// <summary>Includes <c>YAdvance</c>, a signed vertical advance adjustment.</summary>
    /// <seealso cref="GposValueRecord.YAdvance"/>
    YAdvance = 0x0008,

    /// <summary>Includes an Offset16 to a Device or VariationIndex table for <c>XPlacement</c>.</summary>
    /// <seealso cref="GposValueRecord.XPlacementDevice"/>
    XPlaDevice = 0x0010,

    /// <summary>Includes an Offset16 to a Device or VariationIndex table for <c>YPlacement</c>.</summary>
    /// <seealso cref="GposValueRecord.YPlacementDevice"/>
    YPlaDevice = 0x0020,

    /// <summary>Includes an Offset16 to a Device or VariationIndex table for <c>XAdvance</c>.</summary>
    /// <seealso cref="GposValueRecord.XAdvanceDevice"/>
    XAdvDevice = 0x0040,

    /// <summary>Includes an Offset16 to a Device or VariationIndex table for <c>YAdvance</c>.</summary>
    /// <seealso cref="GposValueRecord.YAdvanceDevice"/>
    YAdvDevice = 0x0080,

    /// <summary>Reserved bits 8–15. Set to zero by conforming writers.</summary>
    Reserved = 0xF000,
}

/// <summary>Helpers for <see cref="GposValueFormat"/>.</summary>
file static class GposValueFormatExtensions
{
    /// <summary>Gets the number of bytes a ValueRecord with this format occupies in the source. Each set bit contributes one 2-byte field (either an FWORD adjustment or an Offset16 device reference).</summary>
    /// <param name="f">The value format flag set.</param>
    /// <returns>The record size in bytes, equal to <c>2 × popcount(f)</c>.</returns>
    /// <remarks>The size is used to skip ValueRecords in tables that carry them inline without an explicit size field.</remarks>
    /// <seealso cref="GposValueFormat"/>
    public static int Size(this GposValueFormat f) => BitOperations.PopCount((ushort)f) * 2;
}

// ═══════════════════════════════════════════════════════════════════════════
// ValueRecord
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A GPOS ValueRecord: optional adjustments to a glyph's placement and advance.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A ValueRecord is a variable-layout structure whose fields are determined by the <see cref="GposValueFormat"/> declared on its parent subtable. Each optional field is exposed as a nullable property; a <c>null</c> means the format did not include that field, not that the value is zero.</description></item>
/// <item><description>The four adjustment properties (<see cref="XPlacement"/> through <see cref="YAdvance"/>) carry design-unit deltas. The four device properties carry resolved <see cref="Device"/> tables that provide ppem-specific adjustments.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#value-record">ValueRecord</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GposValueFormat"/>
/// <seealso cref="Context"/>
/// <seealso cref="Device"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#value-record">OpenType specification: ValueRecord</seealso>
public sealed record GposValueRecord : IRecord<GposValueRecord>
{
    /// <summary>A ValueRecord with no fields present, equivalent to a format of <see cref="GposValueFormat.None"/>.</summary>
    /// <remarks>Returned directly by <see cref="Parse"/> when the context's value format is zero; avoids allocating a fresh empty record per call.</remarks>
    /// <seealso cref="IsEmpty"/>
    public static readonly GposValueRecord Empty = new();

    /// <summary>Gets the signed horizontal placement adjustment in design units, or <c>null</c> when absent from the format.</summary>
    /// <value>A signed FWORD, or <c>null</c> when <see cref="GposValueFormat.XPlacement"/> is not set.</value>
    /// <seealso cref="YPlacement"/>
    /// <seealso cref="XPlacementDevice"/>
    public short? XPlacement { get; init; }

    /// <summary>Gets the signed vertical placement adjustment in design units, or <c>null</c> when absent from the format.</summary>
    /// <value>A signed FWORD, or <c>null</c> when <see cref="GposValueFormat.YPlacement"/> is not set.</value>
    /// <seealso cref="XPlacement"/>
    /// <seealso cref="YPlacementDevice"/>
    public short? YPlacement { get; init; }

    /// <summary>Gets the signed horizontal advance adjustment in design units, or <c>null</c> when absent from the format.</summary>
    /// <value>A signed FWORD, or <c>null</c> when <see cref="GposValueFormat.XAdvance"/> is not set.</value>
    /// <seealso cref="YAdvance"/>
    /// <seealso cref="XAdvanceDevice"/>
    public short? XAdvance { get; init; }

    /// <summary>Gets the signed vertical advance adjustment in design units, or <c>null</c> when absent from the format.</summary>
    /// <value>A signed FWORD, or <c>null</c> when <see cref="GposValueFormat.YAdvance"/> is not set.</value>
    /// <seealso cref="XAdvance"/>
    /// <seealso cref="YAdvanceDevice"/>
    public short? YAdvance { get; init; }

    /// <summary>Gets the Device or VariationIndex table for <see cref="XPlacement"/>, or <c>null</c>.</summary>
    /// <value>The <see cref="Device"/> resolved from the record's X-placement device offset, or <c>null</c> when the offset was zero or the format bit was not set.</value>
    /// <seealso cref="XPlacement"/>
    /// <seealso cref="Device"/>
    public Device? XPlacementDevice { get; init; }

    /// <summary>Gets the Device or VariationIndex table for <see cref="YPlacement"/>, or <c>null</c>.</summary>
    /// <value>The <see cref="Device"/> resolved from the record's Y-placement device offset, or <c>null</c> when the offset was zero or the format bit was not set.</value>
    /// <seealso cref="YPlacement"/>
    /// <seealso cref="Device"/>
    public Device? YPlacementDevice { get; init; }

    /// <summary>Gets the Device or VariationIndex table for <see cref="XAdvance"/>, or <c>null</c>.</summary>
    /// <value>The <see cref="Device"/> resolved from the record's X-advance device offset, or <c>null</c> when the offset was zero or the format bit was not set.</value>
    /// <seealso cref="XAdvance"/>
    /// <seealso cref="Device"/>
    public Device? XAdvanceDevice { get; init; }

    /// <summary>Gets the Device or VariationIndex table for <see cref="YAdvance"/>, or <c>null</c>.</summary>
    /// <value>The <see cref="Device"/> resolved from the record's Y-advance device offset, or <c>null</c> when the offset was zero or the format bit was not set.</value>
    /// <seealso cref="YAdvance"/>
    /// <seealso cref="Device"/>
    public Device? YAdvanceDevice { get; init; }

    /// <summary>True when every field is absent — the record carries no adjustment information.</summary>
    /// <value><see langword="true"/> when every one of the eight optional properties is <c>null</c>.</value>
    /// <seealso cref="Empty"/>
    public bool IsEmpty =>
        XPlacement is null && YPlacement is null && XAdvance is null && YAdvance is null &&
        XPlacementDevice is null && YPlacementDevice is null &&
        XAdvanceDevice is null && YAdvanceDevice is null;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the ValueRecord.</param>
    /// <param name="baseContext">A <see cref="Context"/> carrying the parent source and the value format flags.</param>
    /// <returns>The parsed ValueRecord, or <see cref="Empty"/> when the format is <see cref="GposValueFormat.None"/>.</returns>
    /// <exception cref="EndOfStreamException">The record or any referenced device table extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Fields are read in the order the specification defines: the four adjustment words first, then the four device offsets, each gated by its format bit.</description></item>
    /// <item><description>Device offsets are resolved against the parent source, which must be the table where the ValueRecord's offsets are measured from.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#value-record">ValueRecord</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Context"/>
    /// <seealso cref="GposValueFormat"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#value-record">OpenType specification: ValueRecord</seealso>
    static GposValueRecord IRecord<GposValueRecord>.Parse(ref Cursor cursor, object? baseContext)
    {
        Context context = (Context)baseContext!;
        if (context.ValueFormat == GposValueFormat.None) return Empty;

        short? xp = null, yp = null, xa = null, ya = null;
        ushort xpdOff = 0, ypdOff = 0, xadOff = 0, yadOff = 0;

        if ((context.ValueFormat & GposValueFormat.XPlacement) != 0) xp = cursor.ReadInt16();
        if ((context.ValueFormat & GposValueFormat.YPlacement) != 0) yp = cursor.ReadInt16();
        if ((context.ValueFormat & GposValueFormat.XAdvance) != 0) xa = cursor.ReadInt16();
        if ((context.ValueFormat & GposValueFormat.YAdvance) != 0) ya = cursor.ReadInt16();
        if ((context.ValueFormat & GposValueFormat.XPlaDevice) != 0) xpdOff = cursor.ReadOffset16();
        if ((context.ValueFormat & GposValueFormat.YPlaDevice) != 0) ypdOff = cursor.ReadOffset16();
        if ((context.ValueFormat & GposValueFormat.XAdvDevice) != 0) xadOff = cursor.ReadOffset16();
        if ((context.ValueFormat & GposValueFormat.YAdvDevice) != 0) yadOff = cursor.ReadOffset16();

        return new GposValueRecord
        {
            XPlacement = xp,
            YPlacement = yp,
            XAdvance = xa,
            YAdvance = ya,
            XPlacementDevice = xpdOff != 0 ? context.ParentSource.ParseRecordAt<Device>(xpdOff) : null,
            YPlacementDevice = ypdOff != 0 ? context.ParentSource.ParseRecordAt<Device>(ypdOff) : null,
            XAdvanceDevice = xadOff != 0 ? context.ParentSource.ParseRecordAt<Device>(xadOff) : null,
            YAdvanceDevice = yadOff != 0 ? context.ParentSource.ParseRecordAt<Device>(yadOff) : null,
        };
    }

    /// <summary>Context for parsing a ValueRecord: the parent source for device-offset resolution plus the value format flags.</summary>
    /// <param name="ParentSource">The source used to resolve device-table offsets.</param>
    /// <param name="ValueFormat">The value format flags that select which fields are present.</param>
    /// <remarks>Implements <see cref="IParentContext"/> so it can be passed to <see cref="Source.ParseRecordAt{T}(long, object?)"/> directly.</remarks>
    /// <seealso cref="GposValueFormat"/>
    /// <seealso cref="IParentContext"/>
    public record Context(Source ParentSource, GposValueFormat ValueFormat) : IParentContext;
}

// ═══════════════════════════════════════════════════════════════════════════
// Anchor
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>An Anchor table: a glyph attachment point used by cursive, mark-to-base, mark-to-ligature, and mark-to-mark positioning.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Three formats are defined. Format 1 is a coordinate pair. Format 2 adds a contour point index, allowing the anchor to follow the hinted position of a specific point on the glyph's outline. Format 3 adds optional Device or VariationIndex tables that adjust the coordinate per ppem or per variation instance.</description></item>
/// <item><description>Coordinates are relative to the glyph origin, in font design units.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-tables">Anchor tables</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AnchorFormat1"/>
/// <seealso cref="AnchorFormat2"/>
/// <seealso cref="AnchorFormat3"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-tables">OpenType specification: Anchor tables</seealso>
public abstract record Anchor : IRecord<Anchor>, IBaseRecord<Anchor>
{
    /// <summary>Gets the format number: 1, 2, or 3.</summary>
    /// <value>The format discriminant from the first <c>uint16</c> of the anchor record.</value>
    /// <seealso cref="AnchorFormat1"/>
    /// <seealso cref="AnchorFormat2"/>
    /// <seealso cref="AnchorFormat3"/>
    public ushort Format { get; init; }

    /// <summary>Gets the horizontal design-unit coordinate, relative to the glyph origin.</summary>
    /// <value>A signed horizontal coordinate in font design units.</value>
    /// <seealso cref="YCoordinate"/>
    public short XCoordinate { get; init; }

    /// <summary>Gets the vertical design-unit coordinate, relative to the glyph origin.</summary>
    /// <value>A signed vertical coordinate in font design units.</value>
    /// <seealso cref="XCoordinate"/>
    public short YCoordinate { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the anchor record.</param>
    /// <param name="context">Forwarded to the format-specific parser.</param>
    /// <returns>The format-specific anchor.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1, 2, or 3.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-tables">Anchor tables</see> in the OpenType specification.</remarks>
    /// <seealso cref="IBaseRecord{TBase}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-tables">OpenType specification: Anchor tables</seealso>
    public static Anchor Parse(ref Cursor cursor, object? context)
    {
        ushort format = cursor.ReadUInt16();
        return format switch
        {
            1 => IBaseRecord<Anchor>.Parse<AnchorFormat1>(ref cursor, context),
            2 => IBaseRecord<Anchor>.Parse<AnchorFormat2>(ref cursor, context),
            3 => IBaseRecord<Anchor>.Parse<AnchorFormat3>(ref cursor, context),
            _ => throw new InvalidDataException($"Anchor format {format} is not defined."),
        };
    }

    /// <summary>The shared X/Y coordinate pair used by every anchor format. Blittable, size 4, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Reused as an embedded field in <see cref="AnchorFormat2.Header"/> and <see cref="AnchorFormat3.Header"/>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Anchor"/>
    /// <seealso cref="AnchorFormat2.Header"/>
    /// <seealso cref="AnchorFormat3.Header"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct BaseHeader : IEndianReversibleStruct<BaseHeader>
    {
        /// <summary>Gets the X coordinate in design units.</summary>
        /// <value>A signed horizontal coordinate relative to the glyph origin.</value>
        /// <seealso cref="YCoordinate"/>
        public short XCoordinate;

        /// <summary>Gets the Y coordinate in design units.</summary>
        /// <value>A signed vertical coordinate relative to the glyph origin.</value>
        /// <seealso cref="XCoordinate"/>
        public short YCoordinate;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new base header with both coordinates reversed.</returns>
        /// <remarks>Both fields are <c>int16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static BaseHeader ReverseEndianness(BaseHeader v) => new()
        {
            XCoordinate = BinaryPrimitives.ReverseEndianness(v.XCoordinate),
            YCoordinate = BinaryPrimitives.ReverseEndianness(v.YCoordinate),
        };
    }
}

/// <summary>Anchor format 1: a coordinate pair with no further information.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The simplest anchor format, using the base header layout directly with no additional fields.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-format-1">Anchor Format 1</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Anchor"/>
/// <seealso cref="Anchor.BaseHeader"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-format-1">OpenType specification: Anchor Format 1</seealso>
public sealed record AnchorFormat1
    : Anchor,
      IEndianReversibleHeaderDerivedRecord<Anchor, AnchorFormat1, Anchor.BaseHeader>
{
    /// <inheritdoc/>
    /// <param name="header">The already-read base header.</param>
    /// <param name="context">Unused.</param>
    /// <returns>A new instance populated from the header.</returns>
    static AnchorFormat1 IHeaderDerivedRecord<Anchor, AnchorFormat1, BaseHeader>.FromHeader(in BaseHeader header, object? context) => new()
    {
        Format = 1,
        XCoordinate = header.XCoordinate,
        YCoordinate = header.YCoordinate,
    };
}

/// <summary>Anchor format 2: a coordinate pair plus the index of a contour point on the glyph. When hinting is available, the anchor follows the hinted position of that point.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>If hinting is available at the target ppem, the renderer may substitute the hinted position of <see cref="AnchorPoint"/> for the stored coordinates.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-format-2">Anchor Format 2</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Anchor"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-format-2">OpenType specification: Anchor Format 2</seealso>
public sealed record AnchorFormat2
    : Anchor,
      IEndianReversibleHeaderDerivedRecord<Anchor, AnchorFormat2, AnchorFormat2.Header>
{
    /// <summary>Gets the index of the contour point on the glyph.</summary>
    /// <value>The zero-based index of the point within the glyph's outline whose hinted position may replace the stored coordinates.</value>
    /// <seealso cref="Anchor.XCoordinate"/>
    /// <seealso cref="Anchor.YCoordinate"/>
    public ushort AnchorPoint { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read header.</param>
    /// <param name="context">Unused.</param>
    /// <returns>A new instance populated from the header.</returns>
    static AnchorFormat2 IHeaderDerivedRecord<Anchor, AnchorFormat2, Header>.FromHeader(in Header header, object? context) => new()
    {
        Format = 2,
        XCoordinate = header.Base.XCoordinate,
        YCoordinate = header.Base.YCoordinate,
        AnchorPoint = header.AnchorPoint,
    };

    /// <summary>The 6-byte format 2 body. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Composes the shared <see cref="Anchor.BaseHeader"/> with a single <c>uint16</c> contour point index.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-format-2">Anchor Format 2</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AnchorFormat2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-format-2">OpenType specification: Anchor Format 2</seealso>
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the shared X/Y coordinate pair.</summary>
        /// <value>The X/Y coordinate pair from the base anchor header.</value>
        /// <seealso cref="AnchorPoint"/>
        public BaseHeader Base;

        /// <summary>Gets the contour point index on the glyph.</summary>
        /// <value>The zero-based index of the point within the glyph's outline.</value>
        /// <seealso cref="Base"/>
        public ushort AnchorPoint;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>The embedded base header uses <see cref="BaseHeader.ReverseEndianness(BaseHeader)"/>; the point index uses <see cref="BinaryPrimitives.ReverseEndianness(ushort)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Base = BaseHeader.ReverseEndianness(v.Base),
            AnchorPoint = BinaryPrimitives.ReverseEndianness(v.AnchorPoint),
        };
    }
}

/// <summary>Anchor format 3: a coordinate pair plus optional Device tables that adjust the coordinates per ppem or per variation instance.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each device table adjusts one of the two coordinates; both offsets are optional and independently zero or non-zero.</description></item>
/// <item><description>The device table is a VariationIndex table in variable fonts, providing per-instance deltas.</description></item>
/// <item><description>Uses <see cref="IDerivedRecord{TBase, TDerived}"/> rather than the header-derived form because it resolves offsets to separate <see cref="Device"/> subtables.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-format-3">Anchor Format 3</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Anchor"/>
/// <seealso cref="Device"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-format-3">OpenType specification: Anchor Format 3</seealso>
public sealed record AnchorFormat3 : Anchor, IDerivedRecord<Anchor, AnchorFormat3>
{
    /// <summary>Gets the Device or VariationIndex table that adjusts <see cref="Anchor.XCoordinate"/>, or <c>null</c>.</summary>
    /// <value>The <see cref="Device"/> resolved from the record's X-device offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="YDeviceTable"/>
    /// <seealso cref="Device"/>
    public Device? XDeviceTable { get; init; }

    /// <summary>Gets the Device or VariationIndex table that adjusts <see cref="Anchor.YCoordinate"/>, or <c>null</c>.</summary>
    /// <value>The <see cref="Device"/> resolved from the record's Y-device offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="XDeviceTable"/>
    /// <seealso cref="Device"/>
    public Device? YDeviceTable { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-3 body (after the format discriminant).</param>
    /// <param name="context">Forwarded to the device parsers.</param>
    /// <returns>The parsed format-3 anchor.</returns>
    /// <exception cref="EndOfStreamException">The header or any referenced device table extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The cursor is at byte 2 of the Anchor table (the format word was consumed by the base dispatcher). <c>cursor.Source</c> is still based at byte 0 of the Anchor table, so device offsets — which the specification defines as relative to the Anchor table start — resolve correctly against <c>cursor.Source</c> with no adjustment.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-format-3">Anchor Format 3</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Device"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-format-3">OpenType specification: Anchor Format 3</seealso>
    static AnchorFormat3 IDerivedRecord<Anchor, AnchorFormat3>.Parse(ref Cursor cursor, object? context)
    {
        // The cursor is at byte 2 of the Anchor table (the format word was consumed by
        // the base dispatcher). cursor.Source is still based at byte 0 of the Anchor
        // table, so device offsets — which the spec defines as relative to the Anchor
        // table start — resolve correctly against cursor.Source with no adjustment.
        var header = cursor.ReadBigEndianStruct<Header>();
        return new AnchorFormat3
        {
            Format = 3,
            XCoordinate = header.Base.XCoordinate,
            YCoordinate = header.Base.YCoordinate,
            XDeviceTable = header.XDeviceOffset != 0 ? cursor.Source.ParseRecordAt<Device>(header.XDeviceOffset, context) : null,
            YDeviceTable = header.YDeviceOffset != 0 ? cursor.Source.ParseRecordAt<Device>(header.YDeviceOffset, context) : null,
        };
    }

    /// <summary>The 8-byte format 3 body. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Composes the shared <see cref="Anchor.BaseHeader"/> with two Offset16 device references.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-format-3">Anchor Format 3</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AnchorFormat3"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#anchor-format-3">OpenType specification: Anchor Format 3</seealso>
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the shared X/Y coordinate pair.</summary>
        /// <value>The X/Y coordinate pair from the base anchor header.</value>
        /// <seealso cref="XDeviceOffset"/>
        /// <seealso cref="YDeviceOffset"/>
        public BaseHeader Base;

        /// <summary>Gets the offset to the X-coordinate device table, or zero when absent.</summary>
        /// <value>The byte offset of the X-device <see cref="Device"/> from the Anchor table start, or zero.</value>
        /// <seealso cref="YDeviceOffset"/>
        /// <seealso cref="Device"/>
        public ushort XDeviceOffset;

        /// <summary>Gets the offset to the Y-coordinate device table, or zero when absent.</summary>
        /// <value>The byte offset of the Y-device <see cref="Device"/> from the Anchor table start, or zero.</value>
        /// <seealso cref="XDeviceOffset"/>
        /// <seealso cref="Device"/>
        public ushort YDeviceOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>The embedded base header and both offsets are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Base = BaseHeader.ReverseEndianness(v.Base),
            XDeviceOffset = BinaryPrimitives.ReverseEndianness(v.XDeviceOffset),
            YDeviceOffset = BinaryPrimitives.ReverseEndianness(v.YDeviceOffset),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// GPOS table
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The <c>GPOS</c> table: glyph positioning. Supplies pair adjustments, cursive attachment, mark attachment, and contextual positioning, organized by script and language system.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table's top-level structure is three parallel lists — ScriptList, FeatureList, LookupList — that together describe which lookups apply to which script/feature combinations.</description></item>
/// <item><description>Version 1.0 defines the base header; version 1.1 adds a <see cref="FeatureVariations"/> table for conditional feature substitution in variable fonts.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos">GPOS table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ScriptList"/>
/// <seealso cref="FeatureList"/>
/// <seealso cref="LookupList{T}"/>
/// <seealso cref="FeatureVariations"/>
/// <seealso cref="GposSubtable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos">OpenType specification: GPOS table</seealso>
public sealed record GposTable : IFontTable<GposTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos">OpenType specification: GPOS table</seealso>
    public static Tag Tag => "GPOS";

    /// <summary>Gets the major version. Always 1.</summary>
    /// <value>The constant <c>1</c> for a conforming GPOS table.</value>
    /// <seealso cref="MinorVersion"/>
    /// <seealso cref="Header.Major"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version: 0 or 1. Version 1.1 adds <see cref="FeatureVariations"/>.</summary>
    /// <value><c>0</c> for the base version, or <c>1</c> when the table declares a feature-variations offset.</value>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="FeatureVariations"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the script list: the scripts and language systems the font declares.</summary>
    /// <value>The <see cref="ScriptList"/> resolved from the table's script-list offset.</value>
    /// <seealso cref="FeatureList"/>
    /// <seealso cref="LookupList"/>
    public ScriptList ScriptList { get; init; } = null!;

    /// <summary>Gets the feature list: the positioning features the font declares.</summary>
    /// <value>The <see cref="FeatureList"/> resolved from the table's feature-list offset.</value>
    /// <seealso cref="ScriptList"/>
    /// <seealso cref="LookupList"/>
    public FeatureList FeatureList { get; init; } = null!;

    /// <summary>Gets the lookups in processing order, indexed by lookup index from feature tables.</summary>
    /// <value>The <see cref="LookupList{T}"/> resolved from the table's lookup-list offset; its indices are referenced by <see cref="Feature.LookupIndices"/>.</value>
    /// <seealso cref="FeatureList"/>
    /// <seealso cref="GposSubtable"/>
    public LookupList<GposSubtable> LookupList { get; init; } = null!;

    /// <summary>Gets the feature variations, present only in version 1.1. Provides alternate feature tables for different variation-space regions.</summary>
    /// <value>The <see cref="FeatureVariations"/> resolved from the version 1.1 trailing offset, or <c>null</c> when the table is version 1.0 or the offset was zero.</value>
    /// <seealso cref="MinorVersion"/>
    /// <seealso cref="FeatureVariations"/>
    public FeatureVariations? FeatureVariations { get; init; }

    /// <summary>The 10-byte version 1.0 <c>GPOS</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The three offsets are measured from the start of the GPOS table.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos">GPOS header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GposTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos">OpenType specification: GPOS header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the major version. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming GPOS table.</value>
        /// <seealso cref="Minor"/>
        public ushort Major;

        /// <summary>Gets the minor version: 0 or 1.</summary>
        /// <value><c>0</c> for the base version, or <c>1</c> when a feature-variations offset follows.</value>
        /// <seealso cref="Major"/>
        public ushort Minor;

        /// <summary>Gets the offset to the script list.</summary>
        /// <value>The byte offset of the <see cref="ScriptList"/> from the GPOS table start.</value>
        /// <seealso cref="FeatureListOffset"/>
        /// <seealso cref="LookupListOffset"/>
        public ushort ScriptListOffset;

        /// <summary>Gets the offset to the feature list.</summary>
        /// <value>The byte offset of the <see cref="FeatureList"/> from the GPOS table start.</value>
        /// <seealso cref="ScriptListOffset"/>
        /// <seealso cref="LookupListOffset"/>
        public ushort FeatureListOffset;

        /// <summary>Gets the offset to the lookup list.</summary>
        /// <value>The byte offset of the <see cref="LookupList{T}"/> from the GPOS table start.</value>
        /// <seealso cref="ScriptListOffset"/>
        /// <seealso cref="FeatureListOffset"/>
        public ushort LookupListOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All five fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Major = BinaryPrimitives.ReverseEndianness(v.Major),
            Minor = BinaryPrimitives.ReverseEndianness(v.Minor),
            ScriptListOffset = BinaryPrimitives.ReverseEndianness(v.ScriptListOffset),
            FeatureListOffset = BinaryPrimitives.ReverseEndianness(v.FeatureListOffset),
            LookupListOffset = BinaryPrimitives.ReverseEndianness(v.LookupListOffset),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the GPOS table.</param>
    /// <param name="context">Forwarded to the subtable parsers.</param>
    /// <returns>The parsed GPOS table.</returns>
    /// <exception cref="InvalidDataException">The major version is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header or any referenced subtable extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The version 1.1 feature-variations offset is read only when <see cref="MinorVersion"/> is at least 1; a version 1.0 table does not advance the cursor past its declared extent.</description></item>
    /// <item><description>Every offset is resolved against the table-scoped source, so the referenced subtables' internal offsets resolve correctly.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos">GPOS table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="ScriptList"/>
    /// <seealso cref="FeatureList"/>
    /// <seealso cref="LookupList{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos">OpenType specification: GPOS table</seealso>
    public static GposTable Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Major != 1)
            throw new InvalidDataException($"'GPOS'.majorVersion is {header.Major}, expected 1.");

        uint featureVariationsOffset = header.Minor >= 1 ? cursor.ReadUInt32() : 0u;

        return new GposTable
        {
            MajorVersion = header.Major,
            MinorVersion = header.Minor,
            ScriptList = cursor.Source.ParseRecordAt<ScriptList>(header.ScriptListOffset, context),
            FeatureList = cursor.Source.ParseRecordAt<FeatureList>(header.FeatureListOffset, context),
            LookupList = cursor.Source.ParseRecordAt<LookupList<GposSubtable>>(header.LookupListOffset, context),
            FeatureVariations = featureVariationsOffset != 0 ? cursor.Source.ParseRecordAt<FeatureVariations>(featureVariationsOffset, context) : null,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// GposSubtable — dispatcher
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Base class for all GPOS lookup subtables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The dispatcher reads the lookup type from <c>context</c> and routes to the matching concrete subtable family. The context value passed by <c>Lookup&lt;GposSubtable&gt;</c> must be the enclosing lookup's <c>LookupType</c>.</description></item>
/// <item><description>Lookup type 9 (extension positioning) is followed transparently: the dispatcher reads the extension record and re-dispatches to the wrapped subtable.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos">GPOS table</see> chapter in the OpenType specification for the full list of lookup types.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SinglePos"/>
/// <seealso cref="PairPos"/>
/// <seealso cref="CursivePos"/>
/// <seealso cref="MarkBasePos"/>
/// <seealso cref="MarkLigPos"/>
/// <seealso cref="MarkMarkPos"/>
/// <seealso cref="ContextualPos"/>
/// <seealso cref="ChainContextPos"/>
/// <seealso cref="ExtensionPos"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos">OpenType specification: GPOS table</seealso>
public abstract record GposSubtable : IRecord<GposSubtable>, IBaseRecord<GposSubtable>
{
    /// <summary>Gets the subtable format number. Its meaning depends on the lookup type.</summary>
    /// <value>The format discriminant, interpreted in the context of the enclosing lookup's type.</value>
    public ushort Format { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable.</param>
    /// <param name="context">A <see cref="SubtableContext"/> carrying the enclosing lookup's <see cref="SubtableContext.LookupType"/>.</param>
    /// <returns>The concrete subtable for the given lookup type.</returns>
    /// <exception cref="InvalidDataException">The lookup type is not in 1–9.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup-type switch is exhaustive over the eight concrete families plus extension; type 9 is dispatched through <see cref="ExtensionPos.ParseAndUnwrap(ref Cursor)"/> which returns the wrapped subtable directly.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos">GPOS table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SubtableContext"/>
    /// <seealso cref="ExtensionPos"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos">OpenType specification: GPOS table</seealso>
    public static GposSubtable Parse(ref Cursor cursor, object? context)
    {
        ushort lookupType = ((SubtableContext)context!).LookupType;
        return lookupType switch
        {
            1 => IBaseRecord<GposSubtable>.Parse<SinglePos>(ref cursor),
            2 => IBaseRecord<GposSubtable>.Parse<PairPos>(ref cursor),
            3 => IBaseRecord<GposSubtable>.Parse<CursivePos>(ref cursor),
            4 => IBaseRecord<GposSubtable>.Parse<MarkBasePos>(ref cursor),
            5 => IBaseRecord<GposSubtable>.Parse<MarkLigPos>(ref cursor),
            6 => IBaseRecord<GposSubtable>.Parse<MarkMarkPos>(ref cursor),
            7 => IBaseRecord<GposSubtable>.Parse<ContextualPos>(ref cursor),
            8 => IBaseRecord<GposSubtable>.Parse<ChainContextPos>(ref cursor),
            9 => ExtensionPos.ParseAndUnwrap(ref cursor),
            _ => throw new InvalidDataException($"GPOS lookup type {lookupType} is not defined."),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Lookup 1: SinglePos
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Lookup type 1: single adjustment positioning. Adjusts individual glyphs without regard to their neighbors.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Both formats share the same header fields (coverage, value format) and differ in whether they carry one shared value or a per-glyph array.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-1-single-adjustment-positioning-subtable">Lookup Type 1</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SinglePosFormat1"/>
/// <seealso cref="SinglePosFormat2"/>
/// <seealso cref="GposValueRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-1-single-adjustment-positioning-subtable">OpenType specification: Lookup Type 1</seealso>
public abstract record SinglePos : GposSubtable, IBaseRecord<SinglePos>, IDerivedRecord<GposSubtable, SinglePos>
{
    /// <summary>Gets the coverage table listing the adjusted glyphs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects a value in the format-2 array, or applies the shared format-1 value.</value>
    /// <seealso cref="ValueFormat"/>
    /// <seealso cref="Coverage"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets the format of the adjustment records. Determines which fields each record carries.</summary>
    /// <value>The <see cref="GposValueFormat"/> flags that apply to every value record in the subtable.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="GposValueFormat"/>
    public GposValueFormat ValueFormat { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable body (after the lookup-type dispatch).</param>
    /// <param name="context">Forwarded to the format-specific parser.</param>
    /// <returns>The format-specific SinglePos subtable.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1 or 2.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-1-single-adjustment-positioning-subtable">Lookup Type 1</see> in the OpenType specification.</remarks>
    /// <seealso cref="SinglePosFormat1"/>
    /// <seealso cref="SinglePosFormat2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-1-single-adjustment-positioning-subtable">OpenType specification: Lookup Type 1</seealso>
    static SinglePos IDerivedRecord<GposSubtable, SinglePos>.Parse(ref Cursor cursor, object? context)
    {
        ushort format = cursor.ReadUInt16();
        return format switch
        {
            1 => IBaseRecord<SinglePos>.Parse<SinglePosFormat1>(ref cursor, context),
            2 => IBaseRecord<SinglePos>.Parse<SinglePosFormat2>(ref cursor, context),
            _ => throw new InvalidDataException($"SinglePos format {format} is not defined."),
        };
    }
}

/// <summary>SinglePos format 1: one shared adjustment applied to every covered glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The format is compact when many glyphs share the same adjustment, since the value record is stored once regardless of the coverage table's size.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#singlepos-format-1">SinglePos Format 1</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SinglePos"/>
/// <seealso cref="SinglePosFormat2"/>
/// <seealso cref="GposValueRecord"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#singlepos-format-1">OpenType specification: SinglePos Format 1</seealso>
public sealed record SinglePosFormat1 : SinglePos, IDerivedRecord<SinglePos, SinglePosFormat1>
{
    /// <summary>Gets the shared adjustment record applied to all covered glyphs.</summary>
    /// <value>The single <see cref="GposValueRecord"/> whose fields are determined by <see cref="SinglePos.ValueFormat"/>.</value>
    /// <seealso cref="SinglePos.Coverage"/>
    /// <seealso cref="GposValueRecord"/>
    public GposValueRecord Value { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-1 body (after the format discriminant).</param>
    /// <param name="context">Forwarded to the coverage parser.</param>
    /// <returns>The parsed format-1 subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, coverage, or value record extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#singlepos-format-1">SinglePos Format 1</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="GposValueRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#singlepos-format-1">OpenType specification: SinglePos Format 1</seealso>
    static SinglePosFormat1 IDerivedRecord<SinglePos, SinglePosFormat1>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        var valueFormat = (GposValueFormat)header.ValueFormat;

        return new SinglePosFormat1
        {
            Format = 1,
            Coverage = cursor.Source.ParseRecordAt<Coverage>(header.CoverageOffset, context),
            ValueFormat = valueFormat,
            Value = cursor.ReadRecord<GposValueRecord>(new GposValueRecord.Context(cursor.Source, valueFormat)),
        };
    }

    /// <summary>The 6-byte SinglePos format 1 header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The coverage offset is measured from the start of the SinglePos subtable.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#singlepos-format-1">SinglePos Format 1</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SinglePosFormat1"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#singlepos-format-1">OpenType specification: SinglePos Format 1</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the offset to the coverage table.</summary>
        /// <value>The byte offset of the <see cref="Coverage"/> from the subtable start.</value>
        /// <seealso cref="ValueFormat"/>
        public ushort CoverageOffset;

        /// <summary>Gets the value format flags for the shared value record.</summary>
        /// <value>The <see cref="GposValueFormat"/> flags that select which fields the value record carries.</value>
        /// <seealso cref="CoverageOffset"/>
        /// <seealso cref="GposValueFormat"/>
        public ushort ValueFormat;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            CoverageOffset = BinaryPrimitives.ReverseEndianness(v.CoverageOffset),
            ValueFormat = BinaryPrimitives.ReverseEndianness(v.ValueFormat),
        };
    }
}

/// <summary>SinglePos format 2: one adjustment per covered glyph, in Coverage Index order.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The value array's length must equal the coverage table's glyph count; entry <c>i</c> applies to the glyph at coverage index <c>i</c>.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#singlepos-format-2">SinglePos Format 2</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SinglePos"/>
/// <seealso cref="SinglePosFormat1"/>
/// <seealso cref="GposValueRecord"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#singlepos-format-2">OpenType specification: SinglePos Format 2</seealso>
public sealed record SinglePosFormat2 : SinglePos, IDerivedRecord<SinglePos, SinglePosFormat2>
{
    /// <summary>Gets the per-glyph adjustments in Coverage Index order.</summary>
    /// <value>The ordered list of <see cref="GposValueRecord"/> entries; length equals <see cref="SinglePos.Coverage"/>'s glyph count.</value>
    /// <seealso cref="SinglePos.Coverage"/>
    /// <seealso cref="GposValueRecord"/>
    public IReadOnlyList<GposValueRecord> Values { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-2 body (after the format discriminant).</param>
    /// <param name="context">Forwarded to the coverage parser.</param>
    /// <returns>The parsed format-2 subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, coverage, or value array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#singlepos-format-2">SinglePos Format 2</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="GposValueRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#singlepos-format-2">OpenType specification: SinglePos Format 2</seealso>
    static SinglePosFormat2 IDerivedRecord<SinglePos, SinglePosFormat2>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        var valueFormat = (GposValueFormat)header.ValueFormat;

        return new SinglePosFormat2
        {
            Format = 2,
            Coverage = cursor.Source.ParseRecordAt<Coverage>(header.CoverageOffset, context),
            ValueFormat = valueFormat,
            Values = cursor.ReadRecordArray<GposValueRecord>(header.ValueCount, new GposValueRecord.Context(cursor.Source, valueFormat)),
        };
    }

    /// <summary>The 8-byte SinglePos format 2 header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The coverage offset is measured from the start of the SinglePos subtable.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#singlepos-format-2">SinglePos Format 2</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SinglePosFormat2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#singlepos-format-2">OpenType specification: SinglePos Format 2</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the offset to the coverage table.</summary>
        /// <value>The byte offset of the <see cref="Coverage"/> from the subtable start.</value>
        /// <seealso cref="ValueFormat"/>
        /// <seealso cref="ValueCount"/>
        public ushort CoverageOffset;

        /// <summary>Gets the value format flags for the value array.</summary>
        /// <value>The <see cref="GposValueFormat"/> flags that apply to every value record in the array.</value>
        /// <seealso cref="CoverageOffset"/>
        /// <seealso cref="ValueCount"/>
        public ushort ValueFormat;

        /// <summary>Gets the number of value records.</summary>
        /// <value>The count of <see cref="GposValueRecord"/> entries; must equal the coverage table's glyph count.</value>
        /// <seealso cref="ValueFormat"/>
        /// <seealso cref="GposValueRecord"/>
        public ushort ValueCount;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            CoverageOffset = BinaryPrimitives.ReverseEndianness(v.CoverageOffset),
            ValueFormat = BinaryPrimitives.ReverseEndianness(v.ValueFormat),
            ValueCount = BinaryPrimitives.ReverseEndianness(v.ValueCount),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Lookup 2: PairPos
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Lookup type 2: pair adjustment positioning. Adjusts a glyph's position based on the glyph that follows it.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each subtable declares two value formats: one for the adjustment applied to the first glyph of a matched pair, one for the second.</description></item>
/// <item><description>Format 1 lists explicit pairs per covered first glyph; format 2 uses class definitions to compress many pairs into a matrix.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-2-pair-adjustment-positioning-subtable">Lookup Type 2</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PairPosFormat1"/>
/// <seealso cref="PairPosFormat2"/>
/// <seealso cref="GposValueRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-2-pair-adjustment-positioning-subtable">OpenType specification: Lookup Type 2</seealso>
public abstract record PairPos : GposSubtable, IBaseRecord<PairPos>, IDerivedRecord<GposSubtable, PairPos>
{
    /// <summary>Gets the coverage table listing first glyphs of adjusted pairs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects the pair set (format 1) or the first-glyph class (format 2).</value>
    /// <seealso cref="ValueFormat1"/>
    /// <seealso cref="ValueFormat2"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets the format of the first-glyph adjustment records.</summary>
    /// <value>The <see cref="GposValueFormat"/> flags that apply to the first-glyph value record of each pair.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="ValueFormat2"/>
    public GposValueFormat ValueFormat1 { get; init; }

    /// <summary>Gets the format of the second-glyph adjustment records.</summary>
    /// <value>The <see cref="GposValueFormat"/> flags that apply to the second-glyph value record of each pair.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="ValueFormat1"/>
    public GposValueFormat ValueFormat2 { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable body (after the lookup-type dispatch).</param>
    /// <param name="context">Forwarded to the format-specific parser.</param>
    /// <returns>The format-specific PairPos subtable.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1 or 2.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-2-pair-adjustment-positioning-subtable">Lookup Type 2</see> in the OpenType specification.</remarks>
    /// <seealso cref="PairPosFormat1"/>
    /// <seealso cref="PairPosFormat2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-2-pair-adjustment-positioning-subtable">OpenType specification: Lookup Type 2</seealso>
    static PairPos IDerivedRecord<GposSubtable, PairPos>.Parse(ref Cursor cursor, object? context)
    {
        ushort format = cursor.ReadUInt16();
        return format switch
        {
            1 => IBaseRecord<PairPos>.Parse<PairPosFormat1>(ref cursor, context),
            2 => IBaseRecord<PairPos>.Parse<PairPosFormat2>(ref cursor, context),
            _ => throw new InvalidDataException($"PairPos format {format} is not defined."),
        };
    }
}

/// <summary>PairPos format 1: explicit per-first-glyph pair sets.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each covered first glyph has its own <see cref="PairSet"/> listing every second glyph it pairs with. The pair sets are independent, so a font can ship only the pairs that actually kern.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-1">PairPos Format 1</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PairPos"/>
/// <seealso cref="PairPosFormat2"/>
/// <seealso cref="PairSet"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-1">OpenType specification: PairPos Format 1</seealso>
public sealed record PairPosFormat1 : PairPos, IDerivedRecord<PairPos, PairPosFormat1>
{
    /// <summary>Gets the pair sets in Coverage Index order. A <c>null</c> entry means the first glyph at that coverage index has no pair records.</summary>
    /// <value>The ordered list of <see cref="PairSet"/> entries; its length equals <see cref="PairPos.Coverage"/>'s glyph count.</value>
    /// <seealso cref="PairPos.Coverage"/>
    /// <seealso cref="PairSet"/>
    public IReadOnlyList<PairSet?> PairSets { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-1 body (after the format discriminant).</param>
    /// <param name="context">Forwarded to the coverage parser.</param>
    /// <returns>The parsed format-1 subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, coverage, or any pair set extends past the end of the source.</exception>
    /// <remarks>The pair-set offsets are checked for zero; a zero offset yields a <c>null</c> entry rather than a throw, matching the specification's allowance for absent pair sets.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="PairSet"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-1">OpenType specification: PairPos Format 1</seealso>
    static PairPosFormat1 IDerivedRecord<PairPos, PairPosFormat1>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        Source source = cursor.Source;
        var pairSetContext = new PairSet.Context((GposValueFormat)header.ValueFormat1, (GposValueFormat)header.ValueFormat2);

        return new PairPosFormat1
        {
            Format = 1,
            Coverage = source.ParseRecordAt<Coverage>(header.CoverageOffset, context),
            ValueFormat1 = (GposValueFormat)header.ValueFormat1,
            ValueFormat2 = (GposValueFormat)header.ValueFormat2,
            PairSets = cursor.ReadOffset16ArrayInterpret(header.PairSetCount,
                off => off != 0 ? source.ParseRecordAt<PairSet>(off, pairSetContext) : null),
        };
    }

    /// <summary>The 8-byte PairPos format 1 header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The coverage offset is measured from the start of the PairPos subtable.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-1">PairPos Format 1</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PairPosFormat1"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-1">OpenType specification: PairPos Format 1</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the offset to the coverage table.</summary>
        /// <value>The byte offset of the <see cref="Coverage"/> from the subtable start.</value>
        /// <seealso cref="PairSetCount"/>
        public ushort CoverageOffset;

        /// <summary>Gets the value format flags for the first-glyph value record.</summary>
        /// <value>The <see cref="GposValueFormat"/> flags that apply to the first-glyph value of each pair.</value>
        /// <seealso cref="ValueFormat2"/>
        public ushort ValueFormat1;

        /// <summary>Gets the value format flags for the second-glyph value record.</summary>
        /// <value>The <see cref="GposValueFormat"/> flags that apply to the second-glyph value of each pair.</value>
        /// <seealso cref="ValueFormat1"/>
        public ushort ValueFormat2;

        /// <summary>Gets the number of pair sets.</summary>
        /// <value>The count of Offset16 entries that follow the header; must equal the coverage table's glyph count.</value>
        /// <seealso cref="CoverageOffset"/>
        public ushort PairSetCount;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All four fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            CoverageOffset = BinaryPrimitives.ReverseEndianness(v.CoverageOffset),
            ValueFormat1 = BinaryPrimitives.ReverseEndianness(v.ValueFormat1),
            ValueFormat2 = BinaryPrimitives.ReverseEndianness(v.ValueFormat2),
            PairSetCount = BinaryPrimitives.ReverseEndianness(v.PairSetCount),
        };
    }
}

/// <summary>A PairSet: the second-glyph records for one covered first glyph. Records are sorted by <see cref="PairValueRecord.SecondGlyph"/> so the shaper can binary-search.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The specification requires records to be sorted ascending by second glyph ID, enabling a binary search at shaping time.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-1">PairPos Format 1</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PairPosFormat1"/>
/// <seealso cref="PairValueRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-1">OpenType specification: PairPos Format 1</seealso>
public sealed record PairSet : IRecord<PairSet>
{
    /// <summary>Gets the pair records, sorted by second glyph ID.</summary>
    /// <value>The ordered list of <see cref="PairValueRecord"/> entries; sorted ascending by <see cref="PairValueRecord.SecondGlyph"/>.</value>
    /// <seealso cref="PairValueRecord"/>
    public IReadOnlyList<PairValueRecord> Records { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the pair set.</param>
    /// <param name="context">A <see cref="Context"/> carrying the two value formats.</param>
    /// <returns>The parsed pair set.</returns>
    /// <exception cref="EndOfStreamException">The count or value array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-1">PairPos Format 1</see> in the OpenType specification.</remarks>
    /// <seealso cref="Context"/>
    /// <seealso cref="PairValueRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-1">OpenType specification: PairPos Format 1</seealso>
    static PairSet IRecord<PairSet>.Parse(ref Cursor cursor, object? context)
    {
        Context context1 = (Context)context!;
        return new() { Records = cursor.ReadRecordArray<PairValueRecord>(cursor.ReadUInt16(), new PairValueRecord.Context(cursor.Source, context1.ValueFormat1, context1.ValueFormat2)) };
    }

    /// <summary>Context for parsing a <see cref="PairSet"/>: the two value formats that apply to each pair record.</summary>
    /// <param name="ValueFormat1">The value format for the first-glyph adjustment.</param>
    /// <param name="ValueFormat2">The value format for the second-glyph adjustment.</param>
    /// <seealso cref="PairSet"/>
    /// <seealso cref="PairValueRecord.Context"/>
    public record Context(GposValueFormat ValueFormat1, GposValueFormat ValueFormat2);
}

/// <summary>One pair of glyphs and their adjustments.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The first glyph is implicit through the pair set's coverage index; only the second glyph is stored in the record.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-1">PairPos Format 1</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PairSet"/>
/// <seealso cref="GposValueRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-1">OpenType specification: PairPos Format 1</seealso>
public sealed record PairValueRecord : IRecord<PairValueRecord>
{
    /// <summary>Gets the second glyph ID of the pair.</summary>
    /// <value>The glyph ID that must follow the coverage-index-selected first glyph for this record to apply.</value>
    /// <seealso cref="Value1"/>
    /// <seealso cref="Value2"/>
    public ushort SecondGlyph { get; init; }

    /// <summary>Gets the adjustment applied to the first glyph of the pair.</summary>
    /// <value>The <see cref="GposValueRecord"/> whose fields are selected by the subtable's first value format.</value>
    /// <seealso cref="SecondGlyph"/>
    /// <seealso cref="Value2"/>
    public GposValueRecord Value1 { get; init; } = null!;

    /// <summary>Gets the adjustment applied to the second glyph of the pair.</summary>
    /// <value>The <see cref="GposValueRecord"/> whose fields are selected by the subtable's second value format.</value>
    /// <seealso cref="SecondGlyph"/>
    /// <seealso cref="Value1"/>
    public GposValueRecord Value2 { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the pair value record.</param>
    /// <param name="baseContext">A <see cref="Context"/> carrying the parent source and the two value formats.</param>
    /// <returns>The parsed pair value record.</returns>
    /// <exception cref="EndOfStreamException">The record or any referenced device table extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-1">PairPos Format 1</see> in the OpenType specification.</remarks>
    /// <seealso cref="Context"/>
    /// <seealso cref="GposValueRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-1">OpenType specification: PairPos Format 1</seealso>
    static PairValueRecord IRecord<PairValueRecord>.Parse(ref Cursor cursor, object? baseContext)
    {
        Context context = (Context)baseContext!;
        return new()
        {
            SecondGlyph = cursor.ReadUInt16(),
            Value1 = cursor.ReadRecord<GposValueRecord>(new GposValueRecord.Context(context.ParentSource, context.ValueFormat1)),
            Value2 = cursor.ReadRecord<GposValueRecord>(new GposValueRecord.Context(context.ParentSource, context.ValueFormat2)),
        };
    }

    /// <summary>Context for parsing a <see cref="PairValueRecord"/>: the parent source and both value formats.</summary>
    /// <param name="ParentSource">The source used to resolve device-table offsets.</param>
    /// <param name="ValueFormat1">The value format for the first-glyph adjustment.</param>
    /// <param name="ValueFormat2">The value format for the second-glyph adjustment.</param>
    /// <remarks>Implements <see cref="IParentContext"/> so it can be passed through to nested parsers that need the parent source.</remarks>
    /// <seealso cref="IParentContext"/>
    /// <seealso cref="GposValueFormat"/>
    public record Context(Source ParentSource, GposValueFormat ValueFormat1, GposValueFormat ValueFormat2) : IParentContext;
}

/// <summary>PairPos format 2: class-based pair adjustments. First and second glyphs are mapped to class indices via class definition tables; the adjustment is looked up from a two-dimensional array indexed by class pair.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The format is compact when many glyph pairs share the same adjustment, since each distinct pair of classes stores only one value pair.</description></item>
/// <item><description>Class 0 is reserved for glyphs that appear in the coverage table but are not assigned to any other class; class 0 also serves as a "wildcard" or default in the matrix.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-2-class-pair-adjustment">PairPos Format 2</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PairPos"/>
/// <seealso cref="PairPosFormat1"/>
/// <seealso cref="ClassDef"/>
/// <seealso cref="ClassPairValue"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-2-class-pair-adjustment">OpenType specification: PairPos Format 2</seealso>
public sealed record PairPosFormat2 : PairPos, IDerivedRecord<PairPos, PairPosFormat2>
{
    /// <summary>Gets the class definition that maps first glyphs to class indices.</summary>
    /// <value>The <see cref="ClassDef"/> whose class value indexes the first dimension of <see cref="ClassRecords"/>.</value>
    /// <seealso cref="ClassDef2"/>
    /// <seealso cref="ClassRecords"/>
    public ClassDef ClassDef1 { get; init; } = null!;

    /// <summary>Gets the class definition that maps second glyphs to class indices.</summary>
    /// <value>The <see cref="ClassDef"/> whose class value indexes the second dimension of <see cref="ClassRecords"/>.</value>
    /// <seealso cref="ClassDef1"/>
    /// <seealso cref="ClassRecords"/>
    public ClassDef ClassDef2 { get; init; } = null!;

    /// <summary>Gets the number of first-glyph classes, including class 0.</summary>
    /// <value>The length of the first dimension of <see cref="ClassRecords"/>; equals the header's class1Count.</value>
    /// <seealso cref="Class2Count"/>
    /// <seealso cref="ClassRecords"/>
    public int Class1Count => ClassRecords.GetLength(0);

    /// <summary>Gets the number of second-glyph classes, including class 0.</summary>
    /// <value>The length of the second dimension of <see cref="ClassRecords"/>; equals the header's class2Count.</value>
    /// <seealso cref="Class1Count"/>
    /// <seealso cref="ClassRecords"/>
    public int Class2Count => ClassRecords.GetLength(1);

    /// <summary>Gets the class-pair value matrix.</summary>
    /// <value>A rectangular array indexed by <c>[firstClass, secondClass]</c>; every entry provides the two value records for that class pair.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The array is stored in row-major order matching the on-disk layout.</description></item>
    /// <item><description>Because <see cref="ClassPairValue"/> is a reference type, the matrix is a reference array; <c>null</c> entries indicate positions that were not materialized during parse.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ClassPairValue"/>
    /// <seealso cref="ClassDef1"/>
    /// <seealso cref="ClassDef2"/>
    public ClassPairValue[,] ClassRecords { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-2 body (after the format discriminant).</param>
    /// <param name="context">Forwarded to the coverage and class-definition parsers.</param>
    /// <returns>The parsed format-2 subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, coverage, class definitions, or value matrix extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-2-class-pair-adjustment">PairPos Format 2</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="ClassPairValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-2-class-pair-adjustment">OpenType specification: PairPos Format 2</seealso>
    static PairPosFormat2 IDerivedRecord<PairPos, PairPosFormat2>.Parse(ref Cursor cursor, object? context)
    {
        // The format word was consumed by the PairPos dispatcher.
        Header header = cursor.ReadBigEndianStruct<Header>();
        var classRecords = new ClassPairValue[header.Class1Count, header.Class2Count];
        if (classRecords.Length > 0)
            cursor.ReadRecordArray(MemoryMarshal.CreateSpan(ref classRecords[0, 0], classRecords.Length), new ClassPairValue.Context(cursor.Source, (GposValueFormat)header.ValueFormat1, (GposValueFormat)header.ValueFormat2));
        return new PairPosFormat2
        {
            Format = 2,
            Coverage = cursor.Source.ParseRecordAt<Coverage>(header.CoverageOffset, context),
            ValueFormat1 = (GposValueFormat)header.ValueFormat1,
            ValueFormat2 = (GposValueFormat)header.ValueFormat2,
            ClassDef1 = cursor.Source.ParseRecordAt<ClassDef>(header.ClassDef1Offset, context),
            ClassDef2 = cursor.Source.ParseRecordAt<ClassDef>(header.ClassDef2Offset, context),
            ClassRecords = classRecords,
        };
    }

    /// <summary>The 14-byte PairPos format 2 header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The coverage and class-definition offsets are measured from the start of the PairPos subtable.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-2-class-pair-adjustment">PairPos Format 2</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PairPosFormat2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-2-class-pair-adjustment">OpenType specification: PairPos Format 2</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the offset to the coverage table.</summary>
        /// <value>The byte offset of the <see cref="Coverage"/> from the subtable start.</value>
        /// <seealso cref="ClassDef1Offset"/>
        /// <seealso cref="ClassDef2Offset"/>
        public ushort CoverageOffset;

        /// <summary>Gets the value format flags for the first-glyph value record.</summary>
        /// <value>The <see cref="GposValueFormat"/> flags that apply to the first-glyph value of each class pair.</value>
        /// <seealso cref="ValueFormat2"/>
        public ushort ValueFormat1;

        /// <summary>Gets the value format flags for the second-glyph value record.</summary>
        /// <value>The <see cref="GposValueFormat"/> flags that apply to the second-glyph value of each class pair.</value>
        /// <seealso cref="ValueFormat1"/>
        public ushort ValueFormat2;

        /// <summary>Gets the offset to the first-glyph class definition.</summary>
        /// <value>The byte offset of <see cref="ClassDef1"/> from the subtable start.</value>
        /// <seealso cref="ClassDef2Offset"/>
        public ushort ClassDef1Offset;

        /// <summary>Gets the offset to the second-glyph class definition.</summary>
        /// <value>The byte offset of <see cref="ClassDef2"/> from the subtable start.</value>
        /// <seealso cref="ClassDef1Offset"/>
        public ushort ClassDef2Offset;

        /// <summary>Gets the number of first-glyph classes, including class 0.</summary>
        /// <value>The first dimension of the class-pair value matrix.</value>
        /// <seealso cref="Class2Count"/>
        public ushort Class1Count;

        /// <summary>Gets the number of second-glyph classes, including class 0.</summary>
        /// <value>The second dimension of the class-pair value matrix.</value>
        /// <seealso cref="Class1Count"/>
        public ushort Class2Count;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All seven fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            CoverageOffset = BinaryPrimitives.ReverseEndianness(v.CoverageOffset),
            ValueFormat1 = BinaryPrimitives.ReverseEndianness(v.ValueFormat1),
            ValueFormat2 = BinaryPrimitives.ReverseEndianness(v.ValueFormat2),
            ClassDef1Offset = BinaryPrimitives.ReverseEndianness(v.ClassDef1Offset),
            ClassDef2Offset = BinaryPrimitives.ReverseEndianness(v.ClassDef2Offset),
            Class1Count = BinaryPrimitives.ReverseEndianness(v.Class1Count),
            Class2Count = BinaryPrimitives.ReverseEndianness(v.Class2Count),
        };
    }
}

/// <summary>The pair of adjustments applied to one class pair.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each class-pair value carries two <see cref="GposValueRecord"/> instances; the fields each carries is determined by the enclosing subtable's two value formats.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-2-class-pair-adjustment">PairPos Format 2</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PairPosFormat2"/>
/// <seealso cref="GposValueRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-2-class-pair-adjustment">OpenType specification: PairPos Format 2</seealso>
public sealed record ClassPairValue : IRecord<ClassPairValue>
{
    /// <summary>Gets the adjustment applied to the first glyph of the pair.</summary>
    /// <value>The <see cref="GposValueRecord"/> whose fields are selected by the subtable's first value format.</value>
    /// <seealso cref="Value2"/>
    public GposValueRecord Value1 { get; init; } = null!;

    /// <summary>Gets the adjustment applied to the second glyph of the pair.</summary>
    /// <value>The <see cref="GposValueRecord"/> whose fields are selected by the subtable's second value format.</value>
    /// <seealso cref="Value1"/>
    public GposValueRecord Value2 { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the class-pair value record.</param>
    /// <param name="baseContext">A <see cref="Context"/> carrying the parent source and the two value formats.</param>
    /// <returns>The parsed class-pair value.</returns>
    /// <exception cref="EndOfStreamException">The record or any referenced device table extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-2-class-pair-adjustment">PairPos Format 2</see> in the OpenType specification.</remarks>
    /// <seealso cref="Context"/>
    /// <seealso cref="GposValueRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#pairpos-format-2-class-pair-adjustment">OpenType specification: PairPos Format 2</seealso>
    static ClassPairValue IRecord<ClassPairValue>.Parse(ref Cursor cursor, object? baseContext)
    {
        Context context = (Context)baseContext!;
        return new()
        {
            Value1 = cursor.ReadRecord<GposValueRecord>(new GposValueRecord.Context(context.ParentSource, context.ValueFormat1)),
            Value2 = cursor.ReadRecord<GposValueRecord>(new GposValueRecord.Context(context.ParentSource, context.ValueFormat2)),
        };
    }

    /// <summary>Context for parsing a <see cref="ClassPairValue"/>: the parent source and both value formats.</summary>
    /// <param name="ParentSource">The source used to resolve device-table offsets.</param>
    /// <param name="ValueFormat1">The value format for the first-glyph adjustment.</param>
    /// <param name="ValueFormat2">The value format for the second-glyph adjustment.</param>
    /// <remarks>Implements <see cref="IParentContext"/> so it can be passed through to nested parsers that need the parent source.</remarks>
    /// <seealso cref="IParentContext"/>
    /// <seealso cref="GposValueFormat"/>
    public record Context(Source ParentSource, GposValueFormat ValueFormat1, GposValueFormat ValueFormat2) : IParentContext;
}

// ═══════════════════════════════════════════════════════════════════════════
// Lookup 3: CursivePos
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Lookup type 3: cursive attachment positioning. Attaches the exit anchor of one glyph to the entry anchor of the next, producing cursive joins for scripts such as Arabic and Nastaliq.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Both an entry and an exit anchor may be present or absent per glyph; a glyph with only an exit anchor terminates a cursive run, and a glyph with only an entry anchor begins one.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-3-cursive-attachment-positioning-subtable">Lookup Type 3</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="EntryExitRecord"/>
/// <seealso cref="Anchor"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-3-cursive-attachment-positioning-subtable">OpenType specification: Lookup Type 3</seealso>
public sealed record CursivePos : GposSubtable, IDerivedRecord<GposSubtable, CursivePos>
{
    /// <summary>Gets the coverage table listing glyphs that carry cursive anchors.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>EntryExitRecords[i]</c>.</value>
    /// <seealso cref="EntryExitRecords"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets the entry and exit records in Coverage Index order.</summary>
    /// <value>The ordered list of <see cref="EntryExitRecord"/> entries; its length equals <see cref="Coverage"/>'s glyph count.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="EntryExitRecord"/>
    public IReadOnlyList<EntryExitRecord> EntryExitRecords { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable body (after the lookup-type dispatch).</param>
    /// <param name="context">Forwarded to the coverage parser.</param>
    /// <returns>The parsed CursivePos subtable.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header, coverage, or entry/exit array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-3-cursive-attachment-positioning-subtable">Lookup Type 3</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="EntryExitRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-3-cursive-attachment-positioning-subtable">OpenType specification: Lookup Type 3</seealso>
    static CursivePos IDerivedRecord<GposSubtable, CursivePos>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.Format != 1)
            throw new InvalidDataException($"CursivePos format {header.Format} is not defined.");
        return new CursivePos
        {
            Format = 1,
            Coverage = cursor.Source.ParseRecordAt<Coverage>(header.CoverageOffset, context),
            EntryExitRecords = cursor.ReadRecordArray<EntryExitRecord>(header.EntryExitCount, new ParentContext(cursor.Source)),
        };
    }

    /// <summary>The 6-byte CursivePos header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The coverage offset is measured from the start of the CursivePos subtable.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-3-cursive-attachment-positioning-subtable">Lookup Type 3</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CursivePos"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-3-cursive-attachment-positioning-subtable">OpenType specification: Lookup Type 3</seealso>
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the format. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming CursivePos subtable.</value>
        /// <seealso cref="CoverageOffset"/>
        /// <seealso cref="EntryExitCount"/>
        public ushort Format;

        /// <summary>Gets the offset to the coverage table.</summary>
        /// <value>The byte offset of the <see cref="Coverage"/> from the subtable start.</value>
        /// <seealso cref="Format"/>
        /// <seealso cref="EntryExitCount"/>
        public ushort CoverageOffset;

        /// <summary>Gets the number of entry/exit records.</summary>
        /// <value>The count of <see cref="EntryExitRecord"/> entries; must equal the coverage table's glyph count.</value>
        /// <seealso cref="CoverageOffset"/>
        /// <seealso cref="EntryExitRecord"/>
        public ushort EntryExitCount;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            Format = BinaryPrimitives.ReverseEndianness(value.Format),
            CoverageOffset = BinaryPrimitives.ReverseEndianness(value.CoverageOffset),
            EntryExitCount = BinaryPrimitives.ReverseEndianness(value.EntryExitCount),
        };
    }
}

/// <summary>The entry and exit anchors for one glyph in a cursive attachment sequence.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Both anchors are optional; the specification permits a glyph to participate only as a joiner-in, only as a joiner-out, or both.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-3-cursive-attachment-positioning-subtable">Lookup Type 3</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CursivePos"/>
/// <seealso cref="Anchor"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-3-cursive-attachment-positioning-subtable">OpenType specification: Lookup Type 3</seealso>
public sealed record EntryExitRecord : IRecord<EntryExitRecord>
{
    /// <summary>Gets the anchor at which the previous glyph attaches, or <c>null</c>.</summary>
    /// <value>The entry <see cref="Anchor"/>, or <c>null</c> when the offset was zero (the glyph cannot be entered from the previous glyph).</value>
    /// <seealso cref="ExitAnchor"/>
    /// <seealso cref="Anchor"/>
    public Anchor? EntryAnchor { get; init; }

    /// <summary>Gets the anchor at which the next glyph attaches, or <c>null</c>.</summary>
    /// <value>The exit <see cref="Anchor"/>, or <c>null</c> when the offset was zero (the glyph cannot be exited toward the next glyph).</value>
    /// <seealso cref="EntryAnchor"/>
    /// <seealso cref="Anchor"/>
    public Anchor? ExitAnchor { get; init; }

    /// <summary>Reads the two anchor offsets and resolves them against the parent source.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the entry/exit record.</param>
    /// <param name="context">A <see cref="ParentContext"/> carrying the parent source.</param>
    /// <returns>The parsed entry/exit record.</returns>
    /// <exception cref="EndOfStreamException">The record or either anchor extends past the end of the parent source.</exception>
    /// <remarks>Both offsets are checked for zero before parsing; a zero offset yields a <c>null</c> anchor rather than an exception.</remarks>
    /// <seealso cref="ReadAnchor(ref Cursor, ParentContext)"/>
    /// <seealso cref="Anchor"/>
    public static EntryExitRecord Parse(ref Cursor cursor, ParentContext context) => new()
    {
        EntryAnchor = ReadAnchor(ref cursor, context),
        ExitAnchor = ReadAnchor(ref cursor, context),
    };

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the entry/exit record.</param>
    /// <param name="context">A <see cref="ParentContext"/> carrying the parent source.</param>
    /// <returns>The parsed entry/exit record.</returns>
    /// <exception cref="EndOfStreamException">The record or either anchor extends past the end of the parent source.</exception>
    static EntryExitRecord IRecord<EntryExitRecord>.Parse(ref Cursor cursor, object? context) =>
        Parse(ref cursor, (ParentContext)context!);

    /// <summary>Reads a single Offset16 anchor reference and resolves it against the parent source.</summary>
    /// <param name="cursor">Cursor positioned at the anchor's Offset16 field.</param>
    /// <param name="context">A <see cref="ParentContext"/> carrying the parent source.</param>
    /// <returns>The resolved <see cref="Anchor"/>, or <c>null</c> when the offset was zero.</returns>
    /// <exception cref="EndOfStreamException">The offset or the referenced anchor extends past the end of the parent source.</exception>
    /// <remarks>Used by <see cref="Parse(ref Cursor, ParentContext)"/> for both the entry and exit anchors.</remarks>
    /// <seealso cref="EntryExitRecord"/>
    /// <seealso cref="Anchor"/>
    public static Anchor? ReadAnchor(ref Cursor cursor, ParentContext context)
    {
        ushort offset = cursor.ReadOffset16();
        return offset != 0 ? context.ParentSource.ParseRecordAt<Anchor>(offset, context) : null;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// MarkArray
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A MarkArray: one class-and-anchor record per mark glyph. Shared by mark-to-base, mark-to-ligature, and mark-to-mark attachment.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The MarkArray's records are indexed by the Mark Coverage Index of the mark glyph being attached.</description></item>
/// <item><description>The array is reused across the three mark-attachment lookup types; the meaning of the mark class depends on the enclosing subtable's class count.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark-array-table">Mark Array table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MarkRecord"/>
/// <seealso cref="MarkBasePos"/>
/// <seealso cref="MarkLigPos"/>
/// <seealso cref="MarkMarkPos"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark-array-table">OpenType specification: Mark Array table</seealso>
public sealed record MarkArray : IRecord<MarkArray>
{
    /// <summary>Gets the mark records in Mark Coverage Index order.</summary>
    /// <value>The ordered list of <see cref="MarkRecord"/> entries; its length equals the enclosing subtable's mark coverage glyph count.</value>
    /// <seealso cref="Count"/>
    /// <seealso cref="MarkRecord"/>
    public IReadOnlyList<MarkRecord> Marks { get; init; } = [];

    /// <summary>Gets the number of mark records.</summary>
    /// <value>The size of the <see cref="Marks"/> list.</value>
    /// <seealso cref="Marks"/>
    public int Count => Marks.Count;

    /// <summary>Reads the mark count and the corresponding mark records from the cursor.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the mark array.</param>
    /// <param name="context">Forwarded to the mark-record parsers.</param>
    /// <returns>The parsed mark array.</returns>
    /// <exception cref="EndOfStreamException">The count or mark array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark-array-table">Mark Array table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Marks"/>
    /// <seealso cref="MarkRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark-array-table">OpenType specification: Mark Array table</seealso>
    public static MarkArray Parse(ref Cursor cursor, object? context) => new()
    {
        Marks = cursor.ReadRecordArray<MarkRecord>(cursor.ReadUInt16(), new ParentContext(cursor.Source)),
    };
}

/// <summary>One mark glyph's class and anchor.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The mark class selects the corresponding anchor in the enclosing base/ligature/mark2 record; a mark with class <c>m</c> attaches only to anchors at index <c>m</c>.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark-array-table">Mark Array table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MarkArray"/>
/// <seealso cref="Anchor"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark-array-table">OpenType specification: Mark Array table</seealso>
public sealed record MarkRecord : IRecord<MarkRecord>
{
    /// <summary>Gets the mark class index. Classes are zero-based and must be less than the enclosing subtable's <c>MarkClassCount</c>.</summary>
    /// <value>The zero-based class index that selects an anchor position in the enclosing base record.</value>
    /// <seealso cref="Anchor"/>
    public ushort MarkClass { get; init; }

    /// <summary>Gets the anchor for this mark, or <c>null</c> when no anchor is present.</summary>
    /// <value>The <see cref="Anchor"/> resolved from the record's anchor offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="MarkClass"/>
    /// <seealso cref="Anchor"/>
    public Anchor? Anchor { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the mark record.</param>
    /// <param name="baseContext">A <see cref="ParentContext"/> carrying the parent source.</param>
    /// <returns>The parsed mark record.</returns>
    /// <exception cref="EndOfStreamException">The record or referenced anchor extends past the end of the parent source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark-array-table">Mark Array table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Anchor"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark-array-table">OpenType specification: Mark Array table</seealso>
    static MarkRecord IRecord<MarkRecord>.Parse(ref Cursor cursor, object? baseContext)
    {
        ParentContext context = (ParentContext)baseContext!;
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new MarkRecord
        {
            MarkClass = header.MarkClass,
            Anchor = header.MarkAnchorOffset != 0 ? context.ParentSource.ParseRecordAt<Anchor>(header.MarkAnchorOffset, context) : null,
        };
    }

    /// <summary>The 4-byte MarkRecord on-disk form. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The anchor offset is measured from the start of the enclosing MarkArray.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark-array-table">Mark Array table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MarkRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark-array-table">OpenType specification: Mark Array table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the mark class index.</summary>
        /// <value>The zero-based class index that selects an anchor position in the enclosing base record.</value>
        /// <seealso cref="MarkAnchorOffset"/>
        public ushort MarkClass;

        /// <summary>Gets the offset to the mark anchor, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="Anchor"/> from the MarkArray start, or zero.</value>
        /// <seealso cref="MarkClass"/>
        /// <seealso cref="Anchor"/>
        public ushort MarkAnchorOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            MarkClass = BinaryPrimitives.ReverseEndianness(v.MarkClass),
            MarkAnchorOffset = BinaryPrimitives.ReverseEndianness(v.MarkAnchorOffset),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Lookup 4: MarkBasePos
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Lookup type 4: mark-to-base attachment positioning. Attaches marks to base glyphs.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each base glyph provides one anchor per mark class, and each mark glyph declares the class it belongs to. The shaper attaches a mark to a base by looking up the base's anchor at the mark's class.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-4-mark-to-base-attachment-positioning-subtable">Lookup Type 4</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MarkArray"/>
/// <seealso cref="BaseArray"/>
/// <seealso cref="Anchor"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-4-mark-to-base-attachment-positioning-subtable">OpenType specification: Lookup Type 4</seealso>
public sealed record MarkBasePos : GposSubtable, IDerivedRecord<GposSubtable, MarkBasePos>
{
    /// <summary>Gets the coverage table listing mark glyphs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>MarkArray.Marks[i]</c>.</value>
    /// <seealso cref="MarkArray"/>
    /// <seealso cref="BaseCoverage"/>
    public Coverage MarkCoverage { get; init; } = null!;

    /// <summary>Gets the coverage table listing base glyphs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>BaseArray.BaseRecords[i]</c>.</value>
    /// <seealso cref="BaseArray"/>
    /// <seealso cref="MarkCoverage"/>
    public Coverage BaseCoverage { get; init; } = null!;

    /// <summary>Gets the number of mark classes. Bounds every <see cref="MarkRecord.MarkClass"/>.</summary>
    /// <value>The count of anchor positions in each base record; every mark's class must be less than this value.</value>
    /// <seealso cref="MarkArray"/>
    /// <seealso cref="BaseArray"/>
    public int MarkClassCount { get; init; }

    /// <summary>Gets the mark array: one record per mark in Mark Coverage Index order.</summary>
    /// <value>The <see cref="MarkArray"/> that declares each mark's class and anchor.</value>
    /// <seealso cref="MarkCoverage"/>
    /// <seealso cref="BaseArray"/>
    public MarkArray MarkArray { get; init; } = null!;

    /// <summary>Gets the base array: one record per base in Base Coverage Index order.</summary>
    /// <value>The <see cref="BaseArray"/> that declares each base's per-class anchors.</value>
    /// <seealso cref="BaseCoverage"/>
    /// <seealso cref="MarkArray"/>
    public BaseArray BaseArray { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable body (after the lookup-type dispatch).</param>
    /// <param name="context">Forwarded to the coverage and array parsers.</param>
    /// <returns>The parsed MarkBasePos subtable.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header or any referenced subtable extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-4-mark-to-base-attachment-positioning-subtable">Lookup Type 4</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="MarkArray"/>
    /// <seealso cref="BaseArray"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-4-mark-to-base-attachment-positioning-subtable">OpenType specification: Lookup Type 4</seealso>
    static MarkBasePos IDerivedRecord<GposSubtable, MarkBasePos>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.Format != 1)
            throw new InvalidDataException($"MarkBasePos format {header.Format} is not defined.");

        return new MarkBasePos
        {
            Format = 1,
            MarkCoverage = cursor.Source.ParseRecordAt<Coverage>(header.MarkCoverageOffset, context),
            BaseCoverage = cursor.Source.ParseRecordAt<Coverage>(header.BaseCoverageOffset, context),
            MarkClassCount = header.MarkClassCount,
            MarkArray = cursor.Source.ParseRecordAt<MarkArray>(header.MarkArrayOffset, context),
            BaseArray = cursor.Source.ParseRecordAt<BaseArray>(header.BaseArrayOffset, new BaseArray.Context(header.MarkClassCount)),
        };
    }

    /// <summary>The 12-byte MarkBasePos header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All offsets are measured from the start of the MarkBasePos subtable.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-4-mark-to-base-attachment-positioning-subtable">Lookup Type 4</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MarkBasePos"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-4-mark-to-base-attachment-positioning-subtable">OpenType specification: Lookup Type 4</seealso>
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the format. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming MarkBasePos subtable.</value>
        /// <seealso cref="MarkCoverageOffset"/>
        public ushort Format;

        /// <summary>Gets the offset to the mark coverage table.</summary>
        /// <value>The byte offset of <see cref="MarkCoverage"/> from the subtable start.</value>
        /// <seealso cref="BaseCoverageOffset"/>
        public ushort MarkCoverageOffset;

        /// <summary>Gets the offset to the base coverage table.</summary>
        /// <value>The byte offset of <see cref="BaseCoverage"/> from the subtable start.</value>
        /// <seealso cref="MarkCoverageOffset"/>
        public ushort BaseCoverageOffset;

        /// <summary>Gets the number of mark classes.</summary>
        /// <value>The count of anchor positions per base record; bounds every mark's class index.</value>
        /// <seealso cref="MarkArrayOffset"/>
        public ushort MarkClassCount;

        /// <summary>Gets the offset to the mark array.</summary>
        /// <value>The byte offset of the <see cref="MarkArray"/> from the subtable start.</value>
        /// <seealso cref="BaseArrayOffset"/>
        public ushort MarkArrayOffset;

        /// <summary>Gets the offset to the base array.</summary>
        /// <value>The byte offset of the <see cref="BaseArray"/> from the subtable start.</value>
        /// <seealso cref="MarkArrayOffset"/>
        public ushort BaseArrayOffset;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All six fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            Format = BinaryPrimitives.ReverseEndianness(value.Format),
            MarkCoverageOffset = BinaryPrimitives.ReverseEndianness(value.MarkCoverageOffset),
            BaseCoverageOffset = BinaryPrimitives.ReverseEndianness(value.BaseCoverageOffset),
            MarkClassCount = BinaryPrimitives.ReverseEndianness(value.MarkClassCount),
            MarkArrayOffset = BinaryPrimitives.ReverseEndianness(value.MarkArrayOffset),
            BaseArrayOffset = BinaryPrimitives.ReverseEndianness(value.BaseArrayOffset),
        };
    }
}

/// <summary>A BaseArray: the per-base-glyph anchor records for a MarkBasePos subtable. Offsets inside each record are relative to the BaseArray's start.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each base record holds one anchor per mark class; a <c>null</c> anchor means no mark of that class can attach to this base.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#base-array-table">Base Array table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MarkBasePos"/>
/// <seealso cref="BaseAnchorRecord"/>
/// <seealso cref="Context"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#base-array-table">OpenType specification: Base Array table</seealso>
public sealed record BaseArray : IRecord<BaseArray>
{
    /// <summary>Gets the base records in Base Coverage Index order.</summary>
    /// <value>The ordered list of <see cref="BaseAnchorRecord"/> entries; its length equals the enclosing subtable's base coverage glyph count.</value>
    /// <seealso cref="BaseAnchorRecord"/>
    public IReadOnlyList<BaseAnchorRecord> BaseRecords { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the base array.</param>
    /// <param name="context">A <see cref="Context"/> carrying the number of mark classes.</param>
    /// <returns>The parsed base array.</returns>
    /// <exception cref="EndOfStreamException">The count or base-record array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#base-array-table">Base Array table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Context"/>
    /// <seealso cref="BaseAnchorRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#base-array-table">OpenType specification: Base Array table</seealso>
    static BaseArray IRecord<BaseArray>.Parse(ref Cursor cursor, object? context) => new()
    {
        BaseRecords = cursor.ReadRecordArray<BaseAnchorRecord>(cursor.ReadUInt16(), new BaseAnchorRecord.Context(cursor.Source, ((Context)context!).MarkClassCount)),
    };

    /// <summary>Context carrying the number of mark classes.</summary>
    /// <param name="MarkClassCount">The count of anchor positions in each base record.</param>
    /// <remarks>The count is forwarded from <see cref="MarkBasePos.MarkClassCount"/> so each base record can size its anchor array.</remarks>
    /// <seealso cref="BaseArray"/>
    /// <seealso cref="BaseAnchorRecord.Context"/>
    public record Context(int MarkClassCount);
}

/// <summary>The set of anchors for one base glyph — one anchor per mark class.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Anchor array length equals the enclosing subtable's mark class count.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#base-array-table">Base Array table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BaseArray"/>
/// <seealso cref="Anchor"/>
/// <seealso cref="Context"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#base-array-table">OpenType specification: Base Array table</seealso>
public sealed record BaseAnchorRecord : IRecord<BaseAnchorRecord>
{
    /// <summary>Gets the anchors indexed by mark class. Entry <c>m</c> is the anchor at which a mark of class <c>m</c> attaches, or <c>null</c> when no anchor is provided.</summary>
    /// <value>An array of length <see cref="Context.MarkClassCount"/>; a <c>null</c> entry means no mark of that class attaches to this base.</value>
    /// <seealso cref="Context.MarkClassCount"/>
    /// <seealso cref="Anchor"/>
    public IReadOnlyList<Anchor?> BaseAnchors { get; init; } = [];

    /// <summary>Reads the anchor offset array and resolves each against the parent source.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the base anchor record.</param>
    /// <param name="context">A <see cref="Context"/> carrying the parent source and the number of mark classes.</param>
    /// <returns>The parsed base anchor record.</returns>
    /// <exception cref="EndOfStreamException">The offset array or any referenced anchor extends past the end of the parent source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#base-array-table">Base Array table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Context"/>
    /// <seealso cref="Anchor"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#base-array-table">OpenType specification: Base Array table</seealso>
    public static BaseAnchorRecord Parse(ref Cursor cursor, Context context) => new()
    {
        BaseAnchors = cursor.ReadOffset16ArrayInterpret(context.MarkClassCount,
            off => off != 0 ? context.ParentSource.ParseRecordAt<Anchor>(off, context) : null),
    };

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the base anchor record.</param>
    /// <param name="context">A <see cref="Context"/> carrying the parent source and the number of mark classes.</param>
    /// <returns>The parsed base anchor record.</returns>
    /// <exception cref="EndOfStreamException">The offset array or any referenced anchor extends past the end of the parent source.</exception>
    static BaseAnchorRecord IRecord<BaseAnchorRecord>.Parse(ref Cursor cursor, object? context) =>
        Parse(ref cursor, (Context)context!);

    /// <summary>Context carrying the parent source and the number of mark classes.</summary>
    /// <param name="ParentSource">The source used to resolve anchor offsets.</param>
    /// <param name="MarkClassCount">The count of anchor positions in the record.</param>
    /// <remarks>Implements <see cref="IParentContext"/> so it can be passed through to nested parsers that need the parent source.</remarks>
    /// <seealso cref="IParentContext"/>
    /// <seealso cref="BaseAnchorRecord"/>
    public record Context(Source ParentSource, int MarkClassCount) : IParentContext;
}

// ═══════════════════════════════════════════════════════════════════════════
// Lookup 5: MarkLigPos
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Lookup type 5: mark-to-ligature attachment positioning. Attaches marks to specific components of a ligature glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Because a ligature is composed of multiple input glyphs, each ligature record provides a separate anchor set for every component, and the shaper selects the set that corresponds to the component the mark follows.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-5-mark-to-ligature-attachment-positioning-subtable">Lookup Type 5</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MarkArray"/>
/// <seealso cref="LigatureArray"/>
/// <seealso cref="Anchor"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-5-mark-to-ligature-attachment-positioning-subtable">OpenType specification: Lookup Type 5</seealso>
public sealed record MarkLigPos : GposSubtable, IDerivedRecord<GposSubtable, MarkLigPos>
{
    /// <summary>Gets the coverage table listing mark glyphs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>MarkArray.Marks[i]</c>.</value>
    /// <seealso cref="MarkArray"/>
    public Coverage MarkCoverage { get; init; } = null!;

    /// <summary>Gets the coverage table listing ligature glyphs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>LigatureArray.LigatureAttachments[i]</c>.</value>
    /// <seealso cref="LigatureArray"/>
    public Coverage LigatureCoverage { get; init; } = null!;

    /// <summary>Gets the number of mark classes. Bounds every <see cref="MarkRecord.MarkClass"/>.</summary>
    /// <value>The count of anchor positions in each component anchor record.</value>
    /// <seealso cref="MarkArray"/>
    /// <seealso cref="LigatureArray"/>
    public int MarkClassCount { get; init; }

    /// <summary>Gets the mark array: one record per mark in Mark Coverage Index order.</summary>
    /// <value>The <see cref="MarkArray"/> that declares each mark's class and anchor.</value>
    /// <seealso cref="MarkCoverage"/>
    /// <seealso cref="LigatureArray"/>
    public MarkArray MarkArray { get; init; } = null!;

    /// <summary>Gets the ligature array: one record per ligature in Ligature Coverage Index order.</summary>
    /// <value>The <see cref="LigatureArray"/> whose entries name the anchor sets for the ligature components.</value>
    /// <seealso cref="LigatureCoverage"/>
    /// <seealso cref="MarkArray"/>
    public LigatureArray LigatureArray { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable body (after the lookup-type dispatch).</param>
    /// <param name="context">Forwarded to the coverage and array parsers.</param>
    /// <returns>The parsed MarkLigPos subtable.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header or any referenced subtable extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-5-mark-to-ligature-attachment-positioning-subtable">Lookup Type 5</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="MarkArray"/>
    /// <seealso cref="LigatureArray"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-5-mark-to-ligature-attachment-positioning-subtable">OpenType specification: Lookup Type 5</seealso>
    static MarkLigPos IDerivedRecord<GposSubtable, MarkLigPos>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.Format != 1)
            throw new InvalidDataException($"MarkLigPos format {header.Format} is not defined.");

        return new MarkLigPos
        {
            Format = 1,
            MarkCoverage = cursor.Source.ParseRecordAt<Coverage>(header.MarkCoverageOffset, context),
            LigatureCoverage = cursor.Source.ParseRecordAt<Coverage>(header.LigatureCoverageOffset, context),
            MarkClassCount = header.MarkClassCount,
            MarkArray = cursor.Source.ParseRecordAt<MarkArray>(header.MarkArrayOffset, context),
            LigatureArray = cursor.Source.ParseRecordAt<LigatureArray>(header.LigatureArrayOffset, new LigatureArray.Context(header.MarkClassCount)),
        };
    }

    /// <summary>The 12-byte MarkLigPos header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All offsets are measured from the start of the MarkLigPos subtable.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-5-mark-to-ligature-attachment-positioning-subtable">Lookup Type 5</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MarkLigPos"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-5-mark-to-ligature-attachment-positioning-subtable">OpenType specification: Lookup Type 5</seealso>
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the format. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming MarkLigPos subtable.</value>
        /// <seealso cref="MarkCoverageOffset"/>
        public ushort Format;

        /// <summary>Gets the offset to the mark coverage table.</summary>
        /// <value>The byte offset of <see cref="MarkCoverage"/> from the subtable start.</value>
        /// <seealso cref="LigatureCoverageOffset"/>
        public ushort MarkCoverageOffset;

        /// <summary>Gets the offset to the ligature coverage table.</summary>
        /// <value>The byte offset of <see cref="LigatureCoverage"/> from the subtable start.</value>
        /// <seealso cref="MarkCoverageOffset"/>
        public ushort LigatureCoverageOffset;

        /// <summary>Gets the number of mark classes.</summary>
        /// <value>The count of anchor positions per component anchor record.</value>
        /// <seealso cref="MarkArrayOffset"/>
        public ushort MarkClassCount;

        /// <summary>Gets the offset to the mark array.</summary>
        /// <value>The byte offset of the <see cref="MarkArray"/> from the subtable start.</value>
        /// <seealso cref="LigatureArrayOffset"/>
        public ushort MarkArrayOffset;

        /// <summary>Gets the offset to the ligature array.</summary>
        /// <value>The byte offset of the <see cref="LigatureArray"/> from the subtable start.</value>
        /// <seealso cref="MarkArrayOffset"/>
        public ushort LigatureArrayOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All six fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            MarkCoverageOffset = BinaryPrimitives.ReverseEndianness(v.MarkCoverageOffset),
            LigatureCoverageOffset = BinaryPrimitives.ReverseEndianness(v.LigatureCoverageOffset),
            MarkClassCount = BinaryPrimitives.ReverseEndianness(v.MarkClassCount),
            MarkArrayOffset = BinaryPrimitives.ReverseEndianness(v.MarkArrayOffset),
            LigatureArrayOffset = BinaryPrimitives.ReverseEndianness(v.LigatureArrayOffset),
        };
    }
}

/// <summary>A LigatureArray: offsets to the LigatureAttach table for each ligature in a MarkLigPos subtable. Offsets are relative to the LigatureArray's start.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The array's length equals the enclosing subtable's ligature coverage glyph count.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#ligature-array-table">Ligature Array table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MarkLigPos"/>
/// <seealso cref="LigatureAttach"/>
/// <seealso cref="Context"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#ligature-array-table">OpenType specification: Ligature Array table</seealso>
public sealed record LigatureArray : IRecord<LigatureArray>
{
    /// <summary>Gets the ligature attach records in Ligature Coverage Index order. A <c>null</c> entry means the ligature at that coverage index has no anchors.</summary>
    /// <value>An array whose length equals the enclosing subtable's ligature coverage glyph count; a <c>null</c> entry indicates an absent LigatureAttach table.</value>
    /// <seealso cref="LigatureAttach"/>
    public IReadOnlyList<LigatureAttach?> LigatureAttachments { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the ligature array.</param>
    /// <param name="context">A <see cref="Context"/> carrying the number of mark classes.</param>
    /// <returns>The parsed ligature array.</returns>
    /// <exception cref="EndOfStreamException">The count, offset array, or any referenced LigatureAttach extends past the end of the source.</exception>
    /// <remarks>Each offset is checked for zero; a zero offset yields a <c>null</c> entry rather than a throw.</remarks>
    /// <seealso cref="Context"/>
    /// <seealso cref="LigatureAttach"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#ligature-array-table">OpenType specification: Ligature Array table</seealso>
    static LigatureArray IRecord<LigatureArray>.Parse(ref Cursor cursor, object? context)
    {
        Source arraySource = cursor.Source;
        return new LigatureArray
        {
            LigatureAttachments = cursor.ReadOffset16ArrayInterpret(cursor.ReadUInt16(), off => off != 0 ? arraySource.ParseRecordAt<LigatureAttach>(off, new LigatureAttach.Context(arraySource, ((Context)context!).MarkClassCount)) : null),
        };
    }

    /// <summary>Context carrying the number of mark classes.</summary>
    /// <param name="MarkClassCount">The count of anchor positions per component anchor record.</param>
    /// <remarks>Forwarded from <see cref="MarkLigPos.MarkClassCount"/> so each LigatureAttach can size its component anchor arrays.</remarks>
    /// <seealso cref="LigatureArray"/>
    /// <seealso cref="LigatureAttach.Context"/>
    public record Context(int MarkClassCount);
}

/// <summary>The anchor data for one ligature glyph: one <see cref="ComponentAnchorRecord"/> per component in the ligature.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The number of components must equal the number of input glyphs the ligature replaces.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#ligature-array-table">Ligature Array table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LigatureArray"/>
/// <seealso cref="ComponentAnchorRecord"/>
/// <seealso cref="Context"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#ligature-array-table">OpenType specification: Ligature Array table</seealso>
public sealed record LigatureAttach : IRecord<LigatureAttach>
{
    /// <summary>Gets the component records in ligature component order (first to last).</summary>
    /// <value>The ordered list of <see cref="ComponentAnchorRecord"/> entries; index <c>i</c> corresponds to the ligature's <c>i</c>-th input component.</value>
    /// <seealso cref="ComponentAnchorRecord"/>
    public IReadOnlyList<ComponentAnchorRecord> Components { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the LigatureAttach record.</param>
    /// <param name="baseContext">A <see cref="Context"/> carrying the parent source and the number of mark classes.</param>
    /// <returns>The parsed LigatureAttach record.</returns>
    /// <exception cref="EndOfStreamException">The count or component array extends past the end of the parent source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#ligature-array-table">Ligature Array table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Context"/>
    /// <seealso cref="ComponentAnchorRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#ligature-array-table">OpenType specification: Ligature Array table</seealso>
    static LigatureAttach IRecord<LigatureAttach>.Parse(ref Cursor cursor, object? baseContext)
    {
        Context context = (Context)baseContext!;
        int componentCount = cursor.ReadUInt16();
        return new LigatureAttach
        {
            Components = cursor.ReadRecordArray<ComponentAnchorRecord>(
                componentCount, new ComponentAnchorRecord.Context(context.ParentSource, context.MarkClassCount)),
        };
    }

    /// <summary>Context carrying the parent source and the number of mark classes.</summary>
    /// <param name="ParentSource">The source used to resolve anchor offsets.</param>
    /// <param name="MarkClassCount">The count of anchor positions per component record.</param>
    /// <remarks>Implements <see cref="IParentContext"/> so it can be passed through to nested parsers that need the parent source.</remarks>
    /// <seealso cref="IParentContext"/>
    /// <seealso cref="LigatureAttach"/>
    public record Context(Source ParentSource, int MarkClassCount) : IParentContext;
}

/// <summary>The anchors for one ligature component — one per mark class.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Anchor array length equals the enclosing subtable's mark class count.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#ligature-array-table">Ligature Array table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LigatureAttach"/>
/// <seealso cref="Anchor"/>
/// <seealso cref="Context"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#ligature-array-table">OpenType specification: Ligature Array table</seealso>
public sealed record ComponentAnchorRecord : IRecord<ComponentAnchorRecord>
{
    /// <summary>Gets the anchors indexed by mark class. Entry <c>m</c> is the anchor at which a mark of class <c>m</c> attaches to this component, or <c>null</c> when absent.</summary>
    /// <value>An array of length <see cref="Context.MarkClassCount"/>; a <c>null</c> entry means no mark of that class attaches to this component.</value>
    /// <seealso cref="Context.MarkClassCount"/>
    /// <seealso cref="Anchor"/>
    public IReadOnlyList<Anchor?> LigatureAnchors { get; init; } = [];

    /// <summary>Reads the anchor offset array and resolves each against the parent source.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the component anchor record.</param>
    /// <param name="context">A <see cref="Context"/> carrying the parent source and the number of mark classes.</param>
    /// <returns>The parsed component anchor record.</returns>
    /// <exception cref="EndOfStreamException">The offset array or any referenced anchor extends past the end of the parent source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#ligature-array-table">Ligature Array table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Context"/>
    /// <seealso cref="Anchor"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#ligature-array-table">OpenType specification: Ligature Array table</seealso>
    public static ComponentAnchorRecord Parse(ref Cursor cursor, Context context) => new()
    {
        LigatureAnchors = cursor.ReadOffset16ArrayInterpret(context.MarkClassCount,
            off => off != 0 ? context.ParentSource.ParseRecordAt<Anchor>(off, context) : null),
    };

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the component anchor record.</param>
    /// <param name="context">A <see cref="Context"/> carrying the parent source and the number of mark classes.</param>
    /// <returns>The parsed component anchor record.</returns>
    /// <exception cref="EndOfStreamException">The offset array or any referenced anchor extends past the end of the parent source.</exception>
    static ComponentAnchorRecord IRecord<ComponentAnchorRecord>.Parse(ref Cursor cursor, object? context) =>
        Parse(ref cursor, (Context)context!);

    /// <summary>Context carrying the parent source and the number of mark classes.</summary>
    /// <param name="ParentSource">The source used to resolve anchor offsets.</param>
    /// <param name="MarkClassCount">The count of anchor positions in the record.</param>
    /// <remarks>Implements <see cref="IParentContext"/> so it can be passed through to nested parsers that need the parent source.</remarks>
    /// <seealso cref="IParentContext"/>
    /// <seealso cref="ComponentAnchorRecord"/>
    public record Context(Source ParentSource, int MarkClassCount) : IParentContext;
}

// ═══════════════════════════════════════════════════════════════════════════
// Lookup 6: MarkMarkPos
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Lookup type 6: mark-to-mark attachment positioning. Attaches a mark glyph to another mark glyph, as required for stacked diacritics.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The structure mirrors MarkBasePos, but both the attaching and the base glyphs are drawn from mark coverage tables.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-6-mark-to-mark-attachment-positioning-subtable">Lookup Type 6</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MarkArray"/>
/// <seealso cref="Mark2Array"/>
/// <seealso cref="Anchor"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-6-mark-to-mark-attachment-positioning-subtable">OpenType specification: Lookup Type 6</seealso>
public sealed record MarkMarkPos : GposSubtable, IDerivedRecord<GposSubtable, MarkMarkPos>
{
    /// <summary>Gets the coverage table listing the attaching (upper) mark glyphs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>Mark1Array.Marks[i]</c>.</value>
    /// <seealso cref="Mark1Array"/>
    /// <seealso cref="Mark2Coverage"/>
    public Coverage Mark1Coverage { get; init; } = null!;

    /// <summary>Gets the coverage table listing the base (lower) mark glyphs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>Mark2Array.Mark2Records[i]</c>.</value>
    /// <seealso cref="Mark2Array"/>
    /// <seealso cref="Mark1Coverage"/>
    public Coverage Mark2Coverage { get; init; } = null!;

    /// <summary>Gets the number of mark classes. Bounds every <see cref="MarkRecord.MarkClass"/>.</summary>
    /// <value>The count of anchor positions in each Mark2 record.</value>
    /// <seealso cref="Mark1Array"/>
    /// <seealso cref="Mark2Array"/>
    public int MarkClassCount { get; init; }

    /// <summary>Gets the array of attaching marks.</summary>
    /// <value>The <see cref="MarkArray"/> that declares each upper mark's class and anchor.</value>
    /// <seealso cref="Mark1Coverage"/>
    /// <seealso cref="Mark2Array"/>
    public MarkArray Mark1Array { get; init; } = null!;

    /// <summary>Gets the array of base marks.</summary>
    /// <value>The <see cref="Mark2Array"/> that declares each lower mark's per-class anchors.</value>
    /// <seealso cref="Mark2Coverage"/>
    /// <seealso cref="Mark1Array"/>
    public Mark2Array Mark2Array { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable body (after the lookup-type dispatch).</param>
    /// <param name="context">Forwarded to the coverage and array parsers.</param>
    /// <returns>The parsed MarkMarkPos subtable.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header or any referenced subtable extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-6-mark-to-mark-attachment-positioning-subtable">Lookup Type 6</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="MarkArray"/>
    /// <seealso cref="Mark2Array"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-6-mark-to-mark-attachment-positioning-subtable">OpenType specification: Lookup Type 6</seealso>
    static MarkMarkPos IDerivedRecord<GposSubtable, MarkMarkPos>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.Format != 1)
            throw new InvalidDataException($"MarkMarkPos format {header.Format} is not defined.");
        return new MarkMarkPos
        {
            Format = 1,
            Mark1Coverage = cursor.Source.ParseRecordAt<Coverage>(header.Mark1CoverageOffset, context),
            Mark2Coverage = cursor.Source.ParseRecordAt<Coverage>(header.Mark2CoverageOffset, context),
            MarkClassCount = header.MarkClassCount,
            Mark1Array = cursor.Source.ParseRecordAt<MarkArray>(header.Mark1ArrayOffset, context),
            Mark2Array = cursor.Source.ParseRecordAt<Mark2Array>(header.Mark2ArrayOffset, new Mark2Array.Context(header.MarkClassCount)),
        };
    }

    /// <summary>The 12-byte MarkMarkPos header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All offsets are measured from the start of the MarkMarkPos subtable.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-6-mark-to-mark-attachment-positioning-subtable">Lookup Type 6</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MarkMarkPos"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-6-mark-to-mark-attachment-positioning-subtable">OpenType specification: Lookup Type 6</seealso>
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the format. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming MarkMarkPos subtable.</value>
        /// <seealso cref="Mark1CoverageOffset"/>
        public ushort Format;

        /// <summary>Gets the offset to the attaching-mark coverage table.</summary>
        /// <value>The byte offset of <see cref="Mark1Coverage"/> from the subtable start.</value>
        /// <seealso cref="Mark2CoverageOffset"/>
        public ushort Mark1CoverageOffset;

        /// <summary>Gets the offset to the base-mark coverage table.</summary>
        /// <value>The byte offset of <see cref="Mark2Coverage"/> from the subtable start.</value>
        /// <seealso cref="Mark1CoverageOffset"/>
        public ushort Mark2CoverageOffset;

        /// <summary>Gets the number of mark classes.</summary>
        /// <value>The count of anchor positions per Mark2 record.</value>
        /// <seealso cref="Mark1ArrayOffset"/>
        public ushort MarkClassCount;

        /// <summary>Gets the offset to the attaching-mark array.</summary>
        /// <value>The byte offset of <see cref="Mark1Array"/> from the subtable start.</value>
        /// <seealso cref="Mark2ArrayOffset"/>
        public ushort Mark1ArrayOffset;

        /// <summary>Gets the offset to the base-mark array.</summary>
        /// <value>The byte offset of <see cref="Mark2Array"/> from the subtable start.</value>
        /// <seealso cref="Mark1ArrayOffset"/>
        public ushort Mark2ArrayOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All six fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            Mark1CoverageOffset = BinaryPrimitives.ReverseEndianness(v.Mark1CoverageOffset),
            Mark2CoverageOffset = BinaryPrimitives.ReverseEndianness(v.Mark2CoverageOffset),
            MarkClassCount = BinaryPrimitives.ReverseEndianness(v.MarkClassCount),
            Mark1ArrayOffset = BinaryPrimitives.ReverseEndianness(v.Mark1ArrayOffset),
            Mark2ArrayOffset = BinaryPrimitives.ReverseEndianness(v.Mark2ArrayOffset),
        };
    }
}

/// <summary>A Mark2Array: the per-base-mark anchor records for a MarkMarkPos subtable. Offsets inside each record are relative to the Mark2Array's start.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Structurally identical to <see cref="BaseArray"/>; the two exist as distinct types to keep the semantic difference clear (base glyph vs. mark glyph as the attachment target).</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark2array-table">Mark2Array table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MarkMarkPos"/>
/// <seealso cref="Mark2AnchorRecord"/>
/// <seealso cref="Context"/>
/// <seealso cref="BaseArray"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark2array-table">OpenType specification: Mark2Array table</seealso>
public sealed record Mark2Array : IRecord<Mark2Array>
{
    /// <summary>Gets the base-mark records in Mark2 Coverage Index order.</summary>
    /// <value>The ordered list of <see cref="Mark2AnchorRecord"/> entries; its length equals the enclosing subtable's Mark2 coverage glyph count.</value>
    /// <seealso cref="Mark2AnchorRecord"/>
    public IReadOnlyList<Mark2AnchorRecord> Mark2Records { get; init; } = [];

    /// <summary>Reads the Mark2 record count and the corresponding anchor records from the cursor.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the Mark2Array.</param>
    /// <param name="context">A <see cref="Context"/> carrying the number of mark classes.</param>
    /// <returns>The parsed Mark2Array.</returns>
    /// <exception cref="EndOfStreamException">The count or record array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark2array-table">Mark2Array table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Context"/>
    /// <seealso cref="Mark2AnchorRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark2array-table">OpenType specification: Mark2Array table</seealso>
    public static Mark2Array Parse(ref Cursor cursor, Context context) => new()
    {
        Mark2Records = cursor.ReadRecordArray<Mark2AnchorRecord>(cursor.ReadUInt16(), new Mark2AnchorRecord.Context(cursor.Source, context.MarkClassCount)),
    };

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the Mark2Array.</param>
    /// <param name="context">A <see cref="Context"/> carrying the number of mark classes.</param>
    /// <returns>The parsed Mark2Array.</returns>
    /// <exception cref="EndOfStreamException">The count or record array extends past the end of the source.</exception>
    static Mark2Array IRecord<Mark2Array>.Parse(ref Cursor cursor, object? context) => new()
    {
        Mark2Records = cursor.ReadRecordArray<Mark2AnchorRecord>(cursor.ReadUInt16(), new Mark2AnchorRecord.Context(cursor.Source, ((Context)context!).MarkClassCount)),
    };

    /// <summary>Context carrying the number of mark classes.</summary>
    /// <param name="MarkClassCount">The count of anchor positions per Mark2 record.</param>
    /// <remarks>Forwarded from <see cref="MarkMarkPos.MarkClassCount"/> so each record can size its anchor array.</remarks>
    /// <seealso cref="Mark2Array"/>
    /// <seealso cref="Mark2AnchorRecord.Context"/>
    public record Context(int MarkClassCount);
}

/// <summary>The anchors for one base mark — one per mark class.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Anchor array length equals the enclosing subtable's mark class count.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark2array-table">Mark2Array table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Mark2Array"/>
/// <seealso cref="Anchor"/>
/// <seealso cref="Context"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark2array-table">OpenType specification: Mark2Array table</seealso>
public sealed record Mark2AnchorRecord : IRecord<Mark2AnchorRecord>
{
    /// <summary>Gets the anchors indexed by mark class. Entry <c>m</c> is the anchor at which an attaching mark of class <c>m</c> attaches to this base mark, or <c>null</c>.</summary>
    /// <value>An array of length <see cref="Context.MarkClassCount"/>; a <c>null</c> entry means no mark of that class attaches to this base.</value>
    /// <seealso cref="Context.MarkClassCount"/>
    /// <seealso cref="Anchor"/>
    public IReadOnlyList<Anchor?> Mark2Anchors { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the Mark2 anchor record.</param>
    /// <param name="baseContext">A <see cref="Context"/> carrying the parent source and the number of mark classes.</param>
    /// <returns>The parsed Mark2 anchor record.</returns>
    /// <exception cref="EndOfStreamException">The offset array or any referenced anchor extends past the end of the parent source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark2array-table">Mark2Array table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Context"/>
    /// <seealso cref="Anchor"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark2array-table">OpenType specification: Mark2Array table</seealso>
    static Mark2AnchorRecord IRecord<Mark2AnchorRecord>.Parse(ref Cursor cursor, object? baseContext)
    {
        Context context = (Context)baseContext!;
        return new()
        {
            Mark2Anchors = cursor.ReadOffset16ArrayInterpret(context.MarkClassCount,
            off => off != 0 ? context.ParentSource.ParseRecordAt<Anchor>(off, context) : null),
        };
    }

    /// <summary>Context carrying the parent source and the number of mark classes.</summary>
    /// <param name="ParentSource">The source used to resolve anchor offsets.</param>
    /// <param name="MarkClassCount">The count of anchor positions in the record.</param>
    /// <remarks>Implements <see cref="IParentContext"/> so it can be passed through to nested parsers that need the parent source.</remarks>
    /// <seealso cref="IParentContext"/>
    /// <seealso cref="Mark2AnchorRecord"/>
    public record Context(Source ParentSource, int MarkClassCount) : IParentContext;
}

// ═══════════════════════════════════════════════════════════════════════════
// Lookup 7 & 8: Contextual positioning
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Lookup type 7: contextual positioning. Applies positioning rules based on the surrounding glyph sequence using the shared <see cref="SequenceContext"/> formats.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The contextual formats are shared with GSUB; only the nested lookup references differ, pointing at GPOS lookups instead of GSUB lookups.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-7-contextual-positioning-subtables">Lookup Type 7</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SequenceContext"/>
/// <seealso cref="ChainContextPos"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-7-contextual-positioning-subtables">OpenType specification: Lookup Type 7</seealso>
public sealed record ContextualPos : GposSubtable, IDerivedRecord<GposSubtable, ContextualPos>
{
    /// <summary>Gets the contextual rule, in format 1, 2, or 3.</summary>
    /// <value>The <see cref="SequenceContext"/> that describes the matched input sequence and the nested lookups to apply on a match.</value>
    /// <seealso cref="SequenceContext"/>
    public SequenceContext Context { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable body (after the lookup-type dispatch).</param>
    /// <param name="context">Forwarded to the contextual parser.</param>
    /// <returns>The parsed ContextualPos subtable.</returns>
    /// <exception cref="EndOfStreamException">The contextual record or any referenced subtable extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The <see cref="GposSubtable.Format"/> property is set to <c>1</c> as a placeholder; the real format is inside the <see cref="SequenceContext"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-7-contextual-positioning-subtables">Lookup Type 7</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Context"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-7-contextual-positioning-subtables">OpenType specification: Lookup Type 7</seealso>
    static ContextualPos IDerivedRecord<GposSubtable, ContextualPos>.Parse(ref Cursor cursor, object? context) => new()
    {
        Format = 1,
        Context = cursor.ReadRecord<SequenceContext>(),
    };
}

/// <summary>Lookup type 8: chained contextual positioning. Applies positioning rules based on both backtrack and lookahead glyph sequences using the shared <see cref="ChainedSequenceContext"/> formats.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The chained contextual formats are shared with GSUB; only the nested lookup references differ, pointing at GPOS lookups instead of GSUB lookups.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-8-chained-contextual-positioning-subtables">Lookup Type 8</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ChainedSequenceContext"/>
/// <seealso cref="ContextualPos"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-8-chained-contextual-positioning-subtables">OpenType specification: Lookup Type 8</seealso>
public sealed record ChainContextPos : GposSubtable, IDerivedRecord<GposSubtable, ChainContextPos>
{
    /// <summary>Gets the chained contextual rule, in format 1, 2, or 3.</summary>
    /// <value>The <see cref="ChainedSequenceContext"/> that describes the matched backtrack/input/lookahead sequences and the nested lookups to apply on a match.</value>
    /// <seealso cref="ChainedSequenceContext"/>
    public ChainedSequenceContext Context { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable body (after the lookup-type dispatch).</param>
    /// <param name="context">Forwarded to the chained contextual parser.</param>
    /// <returns>The parsed ChainContextPos subtable.</returns>
    /// <exception cref="EndOfStreamException">The chained contextual record or any referenced subtable extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The <see cref="GposSubtable.Format"/> property is set to <c>1</c> as a placeholder; the real format is inside the <see cref="ChainedSequenceContext"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-8-chained-contextual-positioning-subtables">Lookup Type 8</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Context"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-8-chained-contextual-positioning-subtables">OpenType specification: Lookup Type 8</seealso>
    static ChainContextPos IDerivedRecord<GposSubtable, ChainContextPos>.Parse(ref Cursor cursor, object? context) => new()
    {
        Format = 1,
        Context = cursor.ReadRecord<ChainedSequenceContext>(),
    };
}

// ═══════════════════════════════════════════════════════════════════════════
// Lookup 9: ExtensionPos
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Lookup type 9: extension positioning. A single-record wrapper that holds a 32-bit offset to another positioning subtable, allowing the enclosing lookup to reach targets beyond the 16-bit offset range. Followed transparently during parsing.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>ExtensionPos is a static helper rather than a derived record: its parse returns the <em>wrapped</em> subtable, not an ExtensionPos object. The wrapper is transparent — consumers never observe type 9 in the returned object graph.</description></item>
/// <item><description>The extension indirection exists solely to widen the reachable offset space; it carries no semantics of its own.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-9-extension-positioning">Lookup Type 9</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GposSubtable"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-9-extension-positioning">OpenType specification: Lookup Type 9</seealso>
public static class ExtensionPos
{
    /// <summary>Reads an extension record, follows the 32-bit offset, and re-dispatches to the wrapped subtable.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the extension record.</param>
    /// <returns>The subtable identified by the extension's lookup type and offset.</returns>
    /// <exception cref="InvalidDataException">The format is not 1, or the extension's lookup type is itself 9 (recursive extension).</exception>
    /// <exception cref="EndOfStreamException">The extension header or the wrapped subtable extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The extension's offset is relative to the start of the extension subtable, which is <c>cursor.Source</c>'s base. The parser re-bases the source at that offset before dispatching so the wrapped subtable's internal offsets resolve against its own start rather than the extension's.</description></item>
    /// <item><description>Recursive extension indirection is rejected: an extension whose lookup type is 9 would loop indefinitely.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-9-extension-positioning">Lookup Type 9</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="SubtableContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-9-extension-positioning">OpenType specification: Lookup Type 9</seealso>
    public static GposSubtable ParseAndUnwrap(ref Cursor cursor)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.Format != 1)
            throw new InvalidDataException($"ExtensionPos format {header.Format} is not defined.");

        if (header.ExtensionLookupType == 9)
            throw new InvalidDataException("ExtensionPos cannot reference another type 9 lookup.");

        // The extension's offset is relative to the start of the extension subtable,
        // which is cursor.Source's base. Re-base the source at that offset before
        // dispatching so the wrapped subtable's internal offsets resolve against its
        // own start rather than the extension's.
        return cursor.Source.ParseRecordAt<GposSubtable>(header.ExtensionOffset, new SubtableContext(header.ExtensionLookupType));
    }

    /// <summary>The 8-byte ExtensionPos header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The extension offset is a 32-bit value measured from the start of this record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-9-extension-positioning">Lookup Type 9</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ExtensionPos"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-9-extension-positioning">OpenType specification: Lookup Type 9</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the format. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming extension record.</value>
        /// <seealso cref="ExtensionLookupType"/>
        public ushort Format;

        /// <summary>Gets the lookup type of the wrapped subtable.</summary>
        /// <value>The lookup type from 1 to 8 that the extension's offset points at; must not be <c>9</c>.</value>
        /// <seealso cref="Format"/>
        /// <seealso cref="ExtensionOffset"/>
        public ushort ExtensionLookupType;

        /// <summary>Gets the offset from the start of this extension record to the wrapped subtable.</summary>
        /// <value>A 32-bit byte offset measured from the extension record start.</value>
        /// <seealso cref="ExtensionLookupType"/>
        public uint ExtensionOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            ExtensionLookupType = BinaryPrimitives.ReverseEndianness(v.ExtensionLookupType),
            ExtensionOffset = BinaryPrimitives.ReverseEndianness(v.ExtensionOffset),
        };
    }
}
