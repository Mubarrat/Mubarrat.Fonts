using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>A 4-byte fixed-layout <c>axisValueMap</c> record mapping one normalized coordinate to another.</summary>
/// <remarks>Both coordinates use <see cref="F2Dot14"/> fixed-point values. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"><c>avar</c> specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AxisValueMap : IEndianReversibleStruct<AxisValueMap>
{
    /// <summary>Gets the input normalized coordinate.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
    public F2Dot14 FromCoordinate;

    /// <summary>Gets the mapped output normalized coordinate.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
    public F2Dot14 ToCoordinate;

    /// <summary>Reverses the byte order of both coordinates.</summary>
    public static AxisValueMap ReverseEndianness(AxisValueMap value) => new()
    {
        FromCoordinate = F2Dot14.ReverseEndianness(value.FromCoordinate),
        ToCoordinate = F2Dot14.ReverseEndianness(value.ToCoordinate),
    };
}

/// <summary>The <c>avar</c> table: piecewise-linear remapping of normalized variation-axis coordinates.</summary>
/// <remarks>The table contains one segment map per axis, in <c>fvar</c> axis order. Each map defines a piecewise-linear mapping from normalized input coordinates to normalized output coordinates and must include the endpoints <c>(-1, -1)</c> and <c>(1, 1)</c>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"><c>avar</c> specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
public sealed record AvarTable : IFontTable<AvarTable>
{
    /// <summary>Gets the OpenType table tag <c>avar</c>.</summary>
    public static Tag Tag => "avar";

    /// <summary>Gets the major version.</summary>
    /// <remarks>Must be 1.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version.</summary>
    /// <remarks>Must be 0 for the current <c>avar</c> format.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the reserved field.</summary>
    /// <remarks>Must be 0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
    public ushort Reserved { get; init; }

    /// <summary>Gets the number of variation axes.</summary>
    /// <remarks>Must equal <c>fvar.axisCount</c>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar"/>
    public int AxisCount { get; init; }

    /// <summary>Gets the segment map for each variation axis, in <c>fvar</c> axis order.</summary>
    /// <remarks>Each inner list contains the <see cref="AxisValueMap"/> records for one axis.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
    public IReadOnlyList<IReadOnlyList<AxisValueMap>> AxisSegmentMaps { get; init; } = [];

    /// <summary>The 8-byte fixed-layout <c>avar</c> table header.</summary>
    /// <remarks>Fields are stored in big-endian order at the offsets defined by the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"><c>avar</c> specification</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The major version at byte offset 0.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
        public ushort MajorVersion; // +0

        /// <summary>The minor version at byte offset 2.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
        public ushort MinorVersion; // +2

        /// <summary>The reserved field at byte offset 4.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
        public ushort Reserved; // +4

        /// <summary>The number of variation axes at byte offset 6.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
        public ushort AxisCount; // +6

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        public static Header ReverseEndianness(Header value) => new()
        {
            MajorVersion = BinaryPrimitives.ReverseEndianness(value.MajorVersion),
            MinorVersion = BinaryPrimitives.ReverseEndianness(value.MinorVersion),
            Reserved = BinaryPrimitives.ReverseEndianness(value.Reserved),
            AxisCount = BinaryPrimitives.ReverseEndianness(value.AxisCount),
        };
    }

    /// <summary>Remaps one normalized coordinate through the segment map for <paramref name="axisIndex"/>.</summary>
    /// <param name="axisIndex">The zero-based variation-axis index.</param>
    /// <param name="value">The normalized input coordinate.</param>
    /// <returns>The piecewise-linearly remapped normalized coordinate.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="axisIndex"/> is negative or greater than or equal to <see cref="AxisCount"/>.</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
    public double MapCoordinate(int axisIndex, double value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(axisIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(axisIndex, AxisSegmentMaps.Count);

        var map = AxisSegmentMaps[axisIndex];
        if (map.Count == 0) return value;

        double first = map[0].FromCoordinate.Value;
        double last = map[^1].FromCoordinate.Value;
        if (value <= first) return map[0].ToCoordinate.Value;
        if (value >= last) return map[^1].ToCoordinate.Value;

        for (int i = 1; i < map.Count; i++)
        {
            double from = map[i].FromCoordinate.Value;
            if (value <= from)
            {
                double prevFrom = map[i - 1].FromCoordinate.Value;
                double prevTo = map[i - 1].ToCoordinate.Value;
                double thisTo = map[i].ToCoordinate.Value;

                if (from == prevFrom) return thisTo;
                double t = (value - prevFrom) / (from - prevFrom);
                return prevTo + t * (thisTo - prevTo);
            }
        }
        return value;
    }

    /// <summary>Remaps every coordinate in <paramref name="normalized"/> through its corresponding axis segment map.</summary>
    /// <param name="normalized">The normalized coordinates, one per variation axis.</param>
    /// <returns>A new array containing the remapped coordinates.</returns>
    /// <exception cref="ArgumentException">The length of <paramref name="normalized"/> does not equal <see cref="AxisCount"/>.</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/avar"/>
    public double[] MapCoordinates(ReadOnlySpan<double> normalized)
    {
        if (normalized.Length != AxisCount)
            throw new ArgumentException(
                $"Expected {AxisCount} coordinates, got {normalized.Length}.", nameof(normalized));

        var result = new double[AxisCount];
        for (int i = 0; i < AxisCount; i++)
            result[i] = MapCoordinate(i, normalized[i]);
        return result;
    }

    /// <inheritdoc/>
    public static AvarTable Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.MajorVersion != 1)
            throw new InvalidDataException($"'avar'.majorVersion is {header.MajorVersion}, expected 1.");
        if (header.AxisCount == 0)
            throw new InvalidDataException("'avar'.axisCount is 0.");

        var maps = new AxisValueMap[header.AxisCount][];
        for (int a = 0; a < header.AxisCount; a++)
        {
            int count = cursor.ReadUInt16();
            if (count == 0)
                throw new InvalidDataException($"'avar' axis {a} has an empty segment map.");

            maps[a] = cursor.ReadBigEndianStructArray<AxisValueMap>(count);
        }

        return new AvarTable
        {
            MajorVersion = header.MajorVersion,
            MinorVersion = header.MinorVersion,
            Reserved = header.Reserved,
            AxisCount = header.AxisCount,
            AxisSegmentMaps = maps,
        };
    }
}
