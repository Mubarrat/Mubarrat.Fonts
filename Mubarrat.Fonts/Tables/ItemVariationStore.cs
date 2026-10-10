using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.Tables;

/// <summary>The item variation store format used by variation tables to organize deltas by outer and inner item indices.</summary>
/// <remarks>The store consists of a <see cref="VariationRegionList"/> and one or more <see cref="ItemVariationData"/> subtables. An outer index selects an <see cref="ItemVariationData"/> subtable and an inner index selects a delta-set row within that subtable. The format is shared by <c>MVAR</c>, <c>HVAR</c>, <c>VVAR</c>, <c>BASE</c>, <c>GDEF</c>, <c>COLR</c>, and <c>CFF2</c> variation data. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-store">Item Variation Store specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-store"/>
public sealed record ItemVariationStore : IRecord<ItemVariationStore>
{
    /// <summary>Gets the store format.</summary>
    /// <remarks>Must be 1.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-store"/>
    public required ushort Format { get; init; }

    /// <summary>Gets the variation region list referenced by the delta-set data.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#variation-region-list"/>
    public required VariationRegionList VariationRegionList { get; init; }

    /// <summary>Gets the <see cref="ItemVariationData"/> subtables indexed by outer delta-set index.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-data-subtable"/>
    public required IReadOnlyList<ItemVariationData> ItemVariationData { get; init; }

    /// <summary>Gets the number of <see cref="ItemVariationData"/> subtables.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-store"/>
    public int SubtableCount => ItemVariationData.Count;

    /// <summary>Gets the delta-set row identified by <paramref name="outerIndex"/> and <paramref name="innerIndex"/>, or <c>null</c> when either index is outside its valid range.</summary>
    /// <param name="outerIndex">The zero-based index of the <see cref="ItemVariationData"/> subtable.</param>
    /// <param name="innerIndex">The zero-based index of the delta-set row within the selected subtable.</param>
    /// <returns>The raw delta-set row, or <c>null</c> when the indices are out of range.</returns>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-store"/>
    public IReadOnlyList<int>? GetDeltaSet(int outerIndex, int innerIndex)
    {
        if ((uint)outerIndex >= (uint)ItemVariationData.Count) return null;
        var subtable = ItemVariationData[outerIndex];
        if ((uint)innerIndex >= (uint)subtable.ItemCount) return null;
        return subtable.DeltaSets[innerIndex];
    }

    /// <summary>Reads an <see cref="ItemVariationStore"/> from <paramref name="cursor"/>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-store"/>
    public static ItemVariationStore Parse(ref Cursor cursor, object? context)
    {
        ushort format = cursor.ReadUInt16();
        if (format != 1)
            throw new InvalidDataException(
                $"ItemVariationStore format is {format}, expected 1.");

        uint regionListOffset = cursor.ReadOffset32();
        var regionList = cursor.Source.ParseRecordAt<VariationRegionList>(regionListOffset);

        int subtableCount = cursor.ReadUInt16();
        ItemVariationData[] subtables = cursor.ReadOffset32ArrayPeekRecord<ItemVariationData>(subtableCount);
        return new() { Format = format, VariationRegionList = regionList, ItemVariationData = subtables };
    }
}

/// <summary>An <c>ItemVariationData</c> subtable containing delta-set rows that share a common set of variation-region columns.</summary>
/// <remarks>Each row contains one delta for every region in <see cref="RegionIndexes"/>. The first <see cref="WordDeltaCount"/> deltas use either 16-bit or, when <see cref="LongWords"/> is set, 32-bit values; the remaining deltas use 8-bit values. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-data-subtable">Item Variation Data specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-data-subtable"/>
public sealed record ItemVariationData : IRecord<ItemVariationData>
{
    /// <summary>Flag in <c>wordDeltaCount</c> indicating that all deltas use 32-bit values.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-data-subtable"/>
    public const ushort LongWordsFlag = 0x8000;

    /// <summary>Mask for the low 15 bits of <c>wordDeltaCount</c>, which specify the number of long-format deltas.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-data-subtable"/>
    public const ushort WordDeltaCountMask = 0x7FFF;

    /// <summary>Gets the number of delta-set rows.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-data-subtable"/>
    public required int ItemCount { get; init; }

    /// <summary>Gets the number of leading deltas in each row that use the long representation.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-data-subtable"/>
    public required int WordDeltaCount { get; init; }

    /// <summary>Gets a value indicating whether every delta in each row uses the 32-bit representation.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-data-subtable"/>
    public required bool LongWords { get; init; }

    /// <summary>Gets the variation-region indices corresponding to the columns of each delta-set row.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-data-subtable"/>
    public required IReadOnlyList<ushort> RegionIndexes { get; init; }

    /// <summary>Gets the raw delta-set rows, one row for each target item.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-data-subtable"/>
    public required IReadOnlyList<IReadOnlyList<int>> DeltaSets { get; init; }

    /// <summary>Gets the number of variation-region columns in each delta-set row.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-data-subtable"/>
    public int RegionIndexCount => RegionIndexes.Count;

    /// <summary>Reads an <see cref="ItemVariationData"/> subtable from <paramref name="cursor"/>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#item-variation-data-subtable"/>
    public static ItemVariationData Parse(ref Cursor cursor, object? context)
    {
        int itemCount = cursor.ReadUInt16();
        ushort packedWordCount = cursor.ReadUInt16();
        int regionIndexCount = cursor.ReadUInt16();

        bool longWords = (packedWordCount & LongWordsFlag) != 0;
        int wordDeltaCount = packedWordCount & WordDeltaCountMask;

        if (wordDeltaCount > regionIndexCount)
            throw new InvalidDataException(
                $"ItemVariationData wordDeltaCount {wordDeltaCount} exceeds regionIndexCount {regionIndexCount}.");

        var regionIndexes = cursor.ReadUInt16Array(regionIndexCount);

        var deltaSets = new int[itemCount][];
        for (int item = 0; item < itemCount; item++)
        {
            var row = new int[regionIndexCount];
            for (int col = 0; col < regionIndexCount; col++)
            {
                if (longWords)
                    row[col] = cursor.ReadInt32();
                else if (col < wordDeltaCount)
                    row[col] = cursor.ReadInt16();
                else
                    row[col] = cursor.ReadInt8();
            }
            deltaSets[item] = row;
        }

        return new()
        {
            ItemCount = itemCount,
            WordDeltaCount = wordDeltaCount,
            LongWords = longWords,
            RegionIndexes = regionIndexes,
            DeltaSets = deltaSets
        };
    }
}

/// <summary>A variation region list containing the regions referenced by item variation data.</summary>
/// <remarks>Each region defines a start, peak, and end coordinate for every variation axis. The axis order corresponds to the axes defined by <c>fvar</c>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#variation-region-list">Variation Region List specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#variation-region-list"/>
public sealed record VariationRegionList : IRecord<VariationRegionList>
{
    /// <summary>Gets the number of variation axes represented by each region.</summary>
    /// <remarks>This value corresponds to the <c>fvar</c> axis count.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#variation-region-list"/>
    public required int AxisCount { get; init; }

    /// <summary>Gets the variation regions in table order.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#variation-region-list"/>
    public required IReadOnlyList<VariationRegion> Regions { get; init; }

    /// <summary>Gets the number of variation regions.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#variation-region-list"/>
    public int RegionCount => Regions.Count;

    /// <summary>Reads a <see cref="VariationRegionList"/> from <paramref name="cursor"/>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#variation-region-list"/>
    public static VariationRegionList Parse(ref Cursor cursor, object? context)
    {
        int axisCount = cursor.ReadUInt16();
        int regionCount = cursor.ReadUInt16();

        if (axisCount is < 1 or > 512)
            throw new InvalidDataException(
                $"VariationRegionList axisCount is {axisCount}, expected 1–512.");
        if (regionCount > 65535)
            throw new InvalidDataException(
                $"VariationRegionList regionCount is {regionCount}, which exceeds the safety limit.");

        // All regions' coordinates are contiguous: regionCount * axisCount records.
        var flat = cursor.ReadBigEndianStructArray<AxisRegionCoordinates>(checked(regionCount * axisCount));

        var regions = new VariationRegion[regionCount];
        for (int r = 0; r < regionCount; r++)
            regions[r] = new() { Axes = new ArraySegment<AxisRegionCoordinates>(flat, r * axisCount, axisCount) };

        return new() { AxisCount = axisCount, Regions = regions };
    }
}

/// <summary>A fixed-layout set of start, peak, and end coordinates for one variation axis within a variation region.</summary>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#axisregion"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AxisRegionCoordinates : IEndianReversibleStruct<AxisRegionCoordinates>
{
    /// <summary>Gets the start normalized coordinate of the region on this axis.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#axisregion"/>
    public F2Dot14 Start;

    /// <summary>Gets the peak normalized coordinate of the region on this axis.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#axisregion"/>
    public F2Dot14 Peak;

    /// <summary>Gets the end normalized coordinate of the region on this axis.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#axisregion"/>
    public F2Dot14 End;

    /// <summary>Reverses the byte order of every field in an <see cref="AxisRegionCoordinates"/> value.</summary>
    public static AxisRegionCoordinates ReverseEndianness(AxisRegionCoordinates value) => new()
    {
        Start = F2Dot14.ReverseEndianness(value.Start),
        Peak = F2Dot14.ReverseEndianness(value.Peak),
        End = F2Dot14.ReverseEndianness(value.End)
    };
}

/// <summary>A variation region represented by one <see cref="AxisRegionCoordinates"/> value for each variation axis.</summary>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#variation-region-list"/>
public record struct VariationRegion
{
    /// <summary>Gets or sets the per-axis region coordinates in variation-axis order.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#axisregion"/>
    public IReadOnlyList<AxisRegionCoordinates> Axes;
}

/// <summary>A variation axis record defining an axis tag, user-space coordinate range, qualifiers, and display name.</summary>
/// <remarks>The coordinate values are 16.16 fixed-point values in user space. They are normalized to the [-1, 1] range before being used by other variation tables. The current layout is 20 bytes. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord">Variation Axis Record specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VariationAxisRecord : IEndianReversibleStruct<VariationAxisRecord>
{
    /// <summary>Gets the four-byte tag identifying the variation axis.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
    public Tag Tag;

    /// <summary>Gets the minimum user-space coordinate supported by the axis.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
    public Fixed MinValue;

    /// <summary>Gets the default user-space coordinate of the axis.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
    public Fixed DefaultValue;

    /// <summary>Gets the maximum user-space coordinate supported by the axis.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
    public Fixed MaxValue;

    /// <summary>Gets the axis flags.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
    public AxisFlags Flags;

    /// <summary>Gets the <c>name</c> table ID for the axis display name.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/name"/>
    public ushort AxisNameID;

    /// <summary>Gets a value indicating whether the axis is marked as hidden.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
    public readonly bool IsHidden => (Flags & AxisFlags.HiddenAxis) != 0;

    /// <summary>Normalizes a user-space coordinate to the [-1, 1] range used by variation tables.</summary>
    /// <param name="value">The user-space coordinate to normalize.</param>
    /// <returns>The normalized coordinate, with the default position represented by 0.</returns>
    /// <remarks>The input is clamped to <see cref="MinValue"/> and <see cref="MaxValue"/> before piecewise-linear normalization around <see cref="DefaultValue"/>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar#normalized-axis-coordinates">normalized axis coordinate specification</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar#normalized-axis-coordinates"/>
    public readonly double Normalize(double value)
    {
        double min = MinValue.Value;
        double def = DefaultValue.Value;
        double max = MaxValue.Value;

        if (value < min) value = min;
        else if (value > max) value = max;

        if (value == def) return 0.0;
        if (value < def)
            return def == min ? 0.0 : -(def - value) / (def - min);
        return def == max ? 0.0 : (value - def) / (max - def);
    }

    /// <summary>Reverses the byte order of every field in a <see cref="VariationAxisRecord"/> value.</summary>
    public static VariationAxisRecord ReverseEndianness(VariationAxisRecord value) => new()
    {
        Tag = Tag.ReverseEndianness(value.Tag),
        MinValue = Fixed.ReverseEndianness(value.MinValue),
        DefaultValue = Fixed.ReverseEndianness(value.DefaultValue),
        MaxValue = Fixed.ReverseEndianness(value.MaxValue),
        Flags = (AxisFlags)BinaryPrimitives.ReverseEndianness((ushort)value.Flags),
        AxisNameID = BinaryPrimitives.ReverseEndianness(value.AxisNameID),
    };
}

/// <summary>Defines flag bits in the <c>flags</c> field of a <see cref="VariationAxisRecord"/>.</summary>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
[Flags]
public enum AxisFlags : ushort
{
    /// <summary>No axis flags are set.</summary>
    None = 0,

    /// <summary>Bit 0: the axis should normally be hidden from direct user-interface controls except in specialized font-inspection scenarios; named instances are unaffected.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar#variationaxisrecord"/>
    HiddenAxis = 0x0001
}
