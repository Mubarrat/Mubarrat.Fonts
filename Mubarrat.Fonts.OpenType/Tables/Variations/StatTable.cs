using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Tables.Variations;

/// <summary>The <c>STAT</c> table: style attributes describing variation axes, axis ordering, and named style values.</summary>
/// <remarks><c>STAT</c> is required in variable fonts and optional in static fonts. It provides applications with axis and style information without requiring knowledge of the semantics of individual axes. Versions 1.0, 1.1, and 1.2 add progressively newer fields and axis-value formats. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat">STAT specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat"/>
public sealed record StatTable : IOpenTypeTable<StatTable>
{
    /// <summary>Gets the OpenType table tag <c>STAT</c>.</summary>
    public static Tag Tag => "STAT";

    /// <summary>Gets the major version.</summary>
    /// <remarks>Must be 1.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version.</summary>
    /// <remarks>Supported values are 0, 1, and 2.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the design-axis records in <c>STAT</c> axis order.</summary>
    /// <remarks>Each record corresponds to a variation axis defined by <c>fvar</c>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-record"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
    public IReadOnlyList<StatAxisRecord> DesignAxes { get; init; } = [];

    /// <summary>Gets the axis value tables describing named or otherwise significant style values.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table"/>
    public IReadOnlyList<StatAxisValue> AxisValues { get; init; } = [];

    /// <summary>Gets the name ID used as the fallback name for an elided style attribute, or <c>null</c> for version 1.0.</summary>
    /// <remarks>This field is present only in STAT version 1.1 and later.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat"/>
    public ushort? ElidedFallbackNameID { get; init; }

    /// <summary>Gets the number of design-axis records.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-record"/>
    public int DesignAxisCount => DesignAxes.Count;

    /// <summary>Gets the number of axis value tables.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table"/>
    public int AxisValueCount => AxisValues.Count;

    /// <summary>Gets the design-axis record with the specified <paramref name="tag"/>, or <c>null</c> when no matching axis exists.</summary>
    /// <param name="tag">The four-byte variation-axis tag to find.</param>
    /// <returns>The matching axis record, or <c>null</c>.</returns>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-record"/>
    public StatAxisRecord? GetAxis(Tag tag)
    {
        for (int i = 0; i < DesignAxes.Count; i++)
            if (DesignAxes[i].AxisTag == tag) return DesignAxes[i];
        return null;
    }

    /// <summary>The 18-byte fixed-layout header of a <c>STAT</c> table.</summary>
    /// <remarks>The version 1.1 and later <c>elidedFallbackNameID</c> field follows this fixed header.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct HeaderV0 : IBigEndianStruct<HeaderV0>
    {
        /// <summary>The major version at byte offset 0.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat"/>
        public ushort Major;

        /// <summary>The minor version at byte offset 2.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat"/>
        public ushort Minor;

        /// <summary>The size of each design-axis record at byte offset 4.</summary>
        /// <remarks>The standard record size is 8 bytes; larger values permit forward-compatible extensions.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-record"/>
        public ushort DesignAxisSize;

        /// <summary>The number of design-axis records at byte offset 6.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-record"/>
        public ushort DesignAxisCount;

        /// <summary>The offset to the design-axis array at byte offset 8.</summary>
        /// <remarks>The offset is measured from the beginning of the <c>STAT</c> table.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-record"/>
        public uint DesignAxesOffset;

        /// <summary>The number of axis value tables at byte offset 12.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table"/>
        public ushort AxisValueCount;

        /// <summary>The offset to the array of offsets to axis value tables at byte offset 14.</summary>
        /// <remarks>The offset is measured from the beginning of the <c>STAT</c> table.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table"/>
        public uint OffsetToAxisValueOffsets;

        /// <summary>Reverses the byte order of every field in a <see cref="HeaderV0"/> value.</summary>
        public static HeaderV0 ReverseEndianness(HeaderV0 value) => new()
        {
            Major = BinaryPrimitives.ReverseEndianness(value.Major),
            Minor = BinaryPrimitives.ReverseEndianness(value.Minor),
            DesignAxisSize = BinaryPrimitives.ReverseEndianness(value.DesignAxisSize),
            DesignAxisCount = BinaryPrimitives.ReverseEndianness(value.DesignAxisCount),
            DesignAxesOffset = BinaryPrimitives.ReverseEndianness(value.DesignAxesOffset),
            AxisValueCount = BinaryPrimitives.ReverseEndianness(value.AxisValueCount),
            OffsetToAxisValueOffsets = BinaryPrimitives.ReverseEndianness(value.OffsetToAxisValueOffsets),
        };
    }

    /// <summary>Reads a <see cref="StatTable"/> from <paramref name="cursor"/>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat"/>
    public static StatTable Parse(ref Cursor cursor, object? context)
    {
        HeaderV0 h = cursor.ReadBigEndianStruct<HeaderV0>();

        if (h.Major != 1)
            throw new InvalidDataException($"'STAT'.majorVersion is {h.Major}, expected 1.");
        if (h.Minor > 2)
            throw new InvalidDataException($"'STAT'.minorVersion is {h.Minor}, expected 0, 1, or 2.");
        if (h.DesignAxisSize < 8)
            throw new InvalidDataException(
                $"'STAT'.designAxisSize is {h.DesignAxisSize}, expected at least 8.");

        ushort? elidedFallbackNameID = h.Minor >= 1 ? cursor.ReadUInt16() : null;

        var axes = ParseDesignAxes(cursor.Source.CreateOffsetCursor(h.DesignAxesOffset), h.DesignAxisCount, h.DesignAxisSize);
        var values = cursor.Source.CreateOffsetCursor(h.OffsetToAxisValueOffsets).ReadOffset16ArrayPeekRecord<StatAxisValue>(h.AxisValueCount);

        return new StatTable
        {
            MajorVersion = h.Major,
            MinorVersion = h.Minor,
            DesignAxes = axes,
            AxisValues = values,
            ElidedFallbackNameID = elidedFallbackNameID,
        };
    }

    /// <summary>Reads design-axis records using the declared record stride.</summary>
    /// <param name="cursor">The cursor positioned at the first design-axis record.</param>
    /// <param name="count">The number of design-axis records to read.</param>
    /// <param name="declaredSize">The declared size of each design-axis record in bytes.</param>
    /// <returns>The parsed design-axis records.</returns>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-record"/>
    public static StatAxisRecord[] ParseDesignAxes(Cursor cursor, int count, ushort declaredSize)
    {
        if (count == 0) return [];

        if (declaredSize == StatAxisRecordSize)
        {
            // Fast path: declared size matches the struct, one batch read.
            return cursor.ReadBigEndianStructArray<StatAxisRecord>(count);
        }

        // Forward-compatible path: records are larger than the fields we understand.
        var axes = new StatAxisRecord[count];
        for (int i = 0; i < count; i++)
        {
            axes[i] = cursor.PeekBigEndianStruct<StatAxisRecord>();
            cursor.Position += declaredSize;
        }
        return axes;
    }

    /// <summary>Gets the standard on-disk size of a <see cref="StatAxisRecord"/> in bytes.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-record"/>
    public const int StatAxisRecordSize = 8;
}

/// <summary>Base type for the four defined <c>STAT</c> axis value table formats.</summary>
/// <remarks>The format field is consumed by the dispatcher and supplied to the selected derived record. The concrete formats are 1, 2, 3, and 4. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table">axis value table specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table"/>
public abstract record StatAxisValue : IRecord<StatAxisValue>, IBaseRecord<StatAxisValue>
{
    /// <summary>Gets the axis value table format number.</summary>
    /// <remarks>Valid formats are 1 through 4.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table"/>
    public ushort Format { get; init; }

    /// <summary>Gets the axis value flags.</summary>
    /// <remarks>Bit 0 is <c>OLDER_SIBLING_FONT_ATTRIBUTE</c>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table"/>
    public ushort Flags { get; init; }

    /// <summary>Gets the <c>name</c> table ID for the axis value's display name.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name"/>
    public ushort ValueNameID { get; init; }

    /// <summary>Gets a value indicating whether the older-sibling font attribute flag is set.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table"/>
    public bool IsOlderSiblingFontAttribute => (Flags & 0x0001) != 0;

    /// <summary>Reads the format field and dispatches to the corresponding concrete axis value format.</summary>
    /// <param name="cursor">The cursor positioned at the start of the axis value table.</param>
    /// <param name="context">External parsing context, or <c>null</c>.</param>
    /// <returns>The parsed axis value table.</returns>
    /// <exception cref="InvalidDataException">The format is not 1, 2, 3, or 4.</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table"/>
    public static StatAxisValue Parse(ref Cursor cursor, object? context)
    {
        ushort format = cursor.ReadUInt16();
        return format switch
        {
            1 => IBaseRecord<StatAxisValue>.Parse<StatAxisValueFormat1>(ref cursor, context),
            2 => IBaseRecord<StatAxisValue>.Parse<StatAxisValueFormat2>(ref cursor, context),
            3 => IBaseRecord<StatAxisValue>.Parse<StatAxisValueFormat3>(ref cursor, context),
            4 => IBaseRecord<StatAxisValue>.Parse<StatAxisValueFormat4>(ref cursor, context),
            _ => throw new InvalidDataException($"'STAT' axis value format {format} is not defined."),
        };
    }
}

/// <summary>Represents <c>STAT</c> axis value table format 1: a single value on one axis.</summary>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-1"/>
public sealed record StatAxisValueFormat1
    : StatAxisValue,
      IBigEndianHeaderDerivedRecord<StatAxisValue, StatAxisValueFormat1, StatAxisValueFormat1.Header>
{
    /// <summary>Gets the index of the axis to which the value belongs.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-1"/>
    public ushort AxisIndex { get; init; }

    /// <summary>Gets the coordinate value on the axis.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-1"/>
    public Fixed Value { get; init; }

    static StatAxisValueFormat1 IHeaderDerivedRecord<StatAxisValue, StatAxisValueFormat1, Header>.FromHeader(in Header header, object? context) => new()
    {
        Format = 1,
        AxisIndex = header.AxisIndex,
        Flags = header.Flags,
        ValueNameID = header.ValueNameID,
        Value = header.Value,
    };

    /// <summary>The 12-byte fixed-layout format 1 body containing the axis index, flags, value name ID, and coordinate value.</summary>
    /// <remarks>The two-byte format field is consumed by <see cref="StatAxisValue.Parse"/> before this header is read.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-1"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>The axis index at byte offset 0.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-1"/>
        public ushort AxisIndex;

        /// <summary>The flags at byte offset 2.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-1"/>
        public ushort Flags;

        /// <summary>The value name ID at byte offset 4.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-1"/>
        public ushort ValueNameID;

        /// <summary>The axis coordinate value at byte offset 6.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-1"/>
        public Fixed Value;

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/> value.</summary>
        public static Header ReverseEndianness(Header v) => new()
        {
            AxisIndex = BinaryPrimitives.ReverseEndianness(v.AxisIndex),
            Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
            ValueNameID = BinaryPrimitives.ReverseEndianness(v.ValueNameID),
            Value = Fixed.ReverseEndianness(v.Value),
        };
    }
}

/// <summary>Represents <c>STAT</c> axis value table format 2: a nominal value and inclusive range on one axis.</summary>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-2"/>
public sealed record StatAxisValueFormat2
    : StatAxisValue,
      IBigEndianHeaderDerivedRecord<StatAxisValue, StatAxisValueFormat2, StatAxisValueFormat2.Header>
{
    /// <summary>Gets the index of the axis to which the range belongs.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-2"/>
    public ushort AxisIndex { get; init; }

    /// <summary>Gets the nominal value used for the axis when this range is selected.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-2"/>
    public Fixed NominalValue { get; init; }

    /// <summary>Gets the inclusive minimum value of the range.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-2"/>
    public Fixed RangeMinValue { get; init; }

    /// <summary>Gets the inclusive maximum value of the range.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-2"/>
    public Fixed RangeMaxValue { get; init; }

    static StatAxisValueFormat2 IHeaderDerivedRecord<StatAxisValue, StatAxisValueFormat2, Header>.FromHeader(in Header header, object? context) => new()
    {
        Format = 2,
        AxisIndex = header.AxisIndex,
        Flags = header.Flags,
        ValueNameID = header.ValueNameID,
        NominalValue = header.NominalValue,
        RangeMinValue = header.RangeMinValue,
        RangeMaxValue = header.RangeMaxValue,
    };

    /// <summary>The 20-byte fixed-layout format 2 body containing the axis index, flags, value name ID, nominal value, and range bounds.</summary>
    /// <remarks>The two-byte format field is consumed by <see cref="StatAxisValue.Parse"/> before this header is read.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-2"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>The axis index at byte offset 0.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-2"/>
        public ushort AxisIndex;

        /// <summary>The flags at byte offset 2.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-2"/>
        public ushort Flags;

        /// <summary>The value name ID at byte offset 4.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-2"/>
        public ushort ValueNameID;

        /// <summary>The nominal axis value at byte offset 6.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-2"/>
        public Fixed NominalValue;

        /// <summary>The minimum axis value at byte offset 10.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-2"/>
        public Fixed RangeMinValue;

        /// <summary>The maximum axis value at byte offset 14.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-2"/>
        public Fixed RangeMaxValue;

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/> value.</summary>
        public static Header ReverseEndianness(Header v) => new()
        {
            AxisIndex = BinaryPrimitives.ReverseEndianness(v.AxisIndex),
            Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
            ValueNameID = BinaryPrimitives.ReverseEndianness(v.ValueNameID),
            NominalValue = Fixed.ReverseEndianness(v.NominalValue),
            RangeMinValue = Fixed.ReverseEndianness(v.RangeMinValue),
            RangeMaxValue = Fixed.ReverseEndianness(v.RangeMaxValue),
        };
    }
}

/// <summary>Represents <c>STAT</c> axis value table format 3: a single value linked to another value for elision.</summary>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-3"/>
public sealed record StatAxisValueFormat3
    : StatAxisValue,
      IBigEndianHeaderDerivedRecord<StatAxisValue, StatAxisValueFormat3, StatAxisValueFormat3.Header>
{
    /// <summary>Gets the index of the axis to which the value belongs.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-3"/>
    public ushort AxisIndex { get; init; }

    /// <summary>Gets the coordinate value on the axis.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-3"/>
    public Fixed Value { get; init; }

    /// <summary>Gets the name ID of the linked axis value used for elision.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-3"/>
    public ushort LinkedValueNameID { get; init; }

    static StatAxisValueFormat3 IHeaderDerivedRecord<StatAxisValue, StatAxisValueFormat3, Header>.FromHeader(in Header header, object? context) => new()
    {
        Format = 3,
        AxisIndex = header.AxisIndex,
        Flags = header.Flags,
        ValueNameID = header.ValueNameID,
        Value = header.Value,
        LinkedValueNameID = header.LinkedValueNameID,
    };

    /// <summary>The 14-byte fixed-layout format 3 body containing the axis index, flags, value name ID, value, and linked value name ID.</summary>
    /// <remarks>The two-byte format field is consumed by <see cref="StatAxisValue.Parse"/> before this header is read.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-3"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>The axis index at byte offset 0.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-3"/>
        public ushort AxisIndex;

        /// <summary>The flags at byte offset 2.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-3"/>
        public ushort Flags;

        /// <summary>The value name ID at byte offset 4.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-3"/>
        public ushort ValueNameID;

        /// <summary>The axis coordinate value at byte offset 6.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-3"/>
        public Fixed Value;

        /// <summary>The linked value name ID at byte offset 10.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-3"/>
        public ushort LinkedValueNameID;

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/> value.</summary>
        public static Header ReverseEndianness(Header v) => new()
        {
            AxisIndex = BinaryPrimitives.ReverseEndianness(v.AxisIndex),
            Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
            ValueNameID = BinaryPrimitives.ReverseEndianness(v.ValueNameID),
            Value = Fixed.ReverseEndianness(v.Value),
            LinkedValueNameID = BinaryPrimitives.ReverseEndianness(v.LinkedValueNameID),
        };
    }
}

/// <summary>Represents <c>STAT</c> axis value table format 4: a combination of values across multiple axes.</summary>
/// <remarks>The fixed prefix is followed by one <see cref="StatAxisValueRecord"/> for each axis value. Because the record has a variable-length tail, it is parsed directly rather than through <see cref="IBigEndianHeaderDerivedRecord{TBase, TDerived, THeader}"/>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-4"/>
public sealed record StatAxisValueFormat4
    : StatAxisValue,
      IDerivedRecord<StatAxisValue, StatAxisValueFormat4>
{
    /// <summary>Gets the per-axis values defining this multi-axis style value.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-4"/>
    public IReadOnlyList<StatAxisValueRecord> AxisValues { get; init; } = [];

    static StatAxisValueFormat4 IDerivedRecord<StatAxisValue, StatAxisValueFormat4>.Parse(
        ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new StatAxisValueFormat4
        {
            Format = 4,
            Flags = header.Flags,
            ValueNameID = header.ValueNameID,
            AxisValues = cursor.ReadBigEndianStructArray<StatAxisValueRecord>(header.AxisCount),
        };
    }

    /// <summary>The 6-byte fixed-layout format 4 prefix containing the axis count, flags, and value name ID.</summary>
    /// <remarks>The prefix is followed by an array of <see cref="StatAxisValueRecord"/> values.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-4"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>The number of axis value records that follow the prefix at byte offset 0.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-4"/>
        public ushort AxisCount;

        /// <summary>The flags at byte offset 2.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-4"/>
        public ushort Flags;

        /// <summary>The value name ID at byte offset 4.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-4"/>
        public ushort ValueNameID;

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/> value.</summary>
        public static Header ReverseEndianness(Header v) => new()
        {
            AxisCount = BinaryPrimitives.ReverseEndianness(v.AxisCount),
            Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
            ValueNameID = BinaryPrimitives.ReverseEndianness(v.ValueNameID),
        };
    }
}

/// <summary>A fixed-layout axis index and coordinate value pair used by <c>STAT</c> axis value format 4.</summary>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-4"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct StatAxisValueRecord : IBigEndianStruct<StatAxisValueRecord>
{
    /// <summary>The axis index at byte offset 0.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-4"/>
    public ushort AxisIndex;

    /// <summary>The coordinate value at byte offset 2.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-value-table-format-4"/>
    public Fixed Value;

    /// <summary>Reverses the byte order of every field in a <see cref="StatAxisValueRecord"/> value.</summary>
    public static StatAxisValueRecord ReverseEndianness(StatAxisValueRecord v) => new()
    {
        AxisIndex = BinaryPrimitives.ReverseEndianness(v.AxisIndex),
        Value = Fixed.ReverseEndianness(v.Value),
    };
}

/// <summary>A fixed-layout <c>STAT</c> design-axis record corresponding to a variation axis.</summary>
/// <remarks>The record is 8 bytes: a four-byte axis tag followed by a name ID and an ordering value. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-record">STAT axis record specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-record"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct StatAxisRecord : IBigEndianStruct<StatAxisRecord>
{
    /// <summary>Gets the four-byte axis tag, corresponding to an <c>fvar</c> axis tag.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-record"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
    public Tag AxisTag;

    /// <summary>Gets the <c>name</c> table ID for the axis display name.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-record"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name"/>
    public ushort AxisNameID;

    /// <summary>Gets the axis ordering value used to determine presentation order.</summary>
    /// <remarks>Lower ordering values are presented before higher ordering values.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/stat#axis-record"/>
    public ushort AxisOrdering;

    /// <summary>Reverses the byte order of every field in a <see cref="StatAxisRecord"/> value.</summary>
    public static StatAxisRecord ReverseEndianness(StatAxisRecord value) => new()
    {
        AxisTag = Tag.ReverseEndianness(value.AxisTag),
        AxisNameID = BinaryPrimitives.ReverseEndianness(value.AxisNameID),
        AxisOrdering = BinaryPrimitives.ReverseEndianness(value.AxisOrdering),
    };
}
