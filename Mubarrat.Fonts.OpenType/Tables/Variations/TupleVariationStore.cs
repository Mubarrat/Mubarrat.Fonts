using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Tables.Variations;

/// <summary>A tuple variation store used by <c>gvar</c> and <c>cvar</c> to encode deltas grouped by variation region.</summary>
/// <remarks>Contains tuple variation headers followed by serialized point-number and delta data. <c>gvar</c> stores X and Y delta sequences; <c>cvar</c> stores one delta sequence.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon">OpenType Variation Common Formats</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar">OpenType <c>gvar</c> Table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvar">OpenType <c>cvar</c> Table</seealso>
public sealed record TupleVariationStore
{
    /// <summary>Gets the number of tuple variation tables in the store.</summary>
    public int TupleVariationCount { get; }

    /// <summary>Gets the raw packed <c>tupleVariationCount</c> field.</summary>
    public ushort RawTupleVariationCount { get; }

    /// <summary>Gets the shared point numbers, or <c>null</c> when the shared-point flag is clear.</summary>
    public required PackedPointNumbers? SharedPointNumbers { get; init; }

    /// <summary>Gets the tuple variation headers.</summary>
    public required IReadOnlyList<TupleVariationHeader> Headers { get; init; }

    /// <summary>Gets the per-tuple serialized data, parallel to <see cref="Headers"/>.</summary>
    public required IReadOnlyList<TupleVariationTableData> Data { get; init; }

    /// <summary>Gets the shared tuple records, or an empty list when the store has none.</summary>
    public required IReadOnlyList<TupleRecord> SharedTuples { get; init; }

    /// <summary>Gets a value indicating whether the store provides shared point numbers.</summary>
    public bool HasSharedPointNumbers =>
        (RawTupleVariationCount & (ushort)TupleVariationStoreFlags.SharedPointNumbers) != 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleVariationStore"/> class with the specified raw packed <c>tupleVariationCount</c> field.
    /// </summary>
    /// <param name="rawCount">The raw packed <c>tupleVariationCount</c> field.</param>
    public TupleVariationStore(ushort rawCount)
    {
        RawTupleVariationCount = rawCount;
        TupleVariationCount = rawCount & (ushort)TupleVariationStoreFlags.CountMask;
    }

    /// <summary>Resolves the peak tuple for tuple variation table <paramref name="index"/>, using the shared tuple array when the header does not embed the peak.</summary>
    /// <param name="index">Zero-based index of the tuple variation table.</param>
    /// <returns>The resolved peak tuple.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside <see cref="Headers"/>.</exception>
    /// <exception cref="InvalidDataException">The header references a shared tuple that is not present.</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#tuple-variation-store-header">OpenType Tuple Variation Store Header</seealso>
    public TupleRecord GetPeakTuple(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Headers.Count);
        var header = Headers[index];
        if (header.PeakTuple is { } embedded) return embedded;
        if (header.SharedTupleIndex >= (uint)SharedTuples.Count)
            throw new InvalidDataException(
                $"Tuple {index} references shared tuple {header.SharedTupleIndex}, " +
                $"but only {SharedTuples.Count} are available.");
        return SharedTuples[header.SharedTupleIndex];
    }

    /// <summary>Reads a tuple variation store body from <paramref name="cursor"/>.</summary>
    /// <param name="cursor">Cursor positioned at the <c>tupleVariationCount</c> field.</param>
    /// <param name="axisCount">Number of variation axes from <c>fvar</c>.</param>
    /// <param name="isCvar"><c>true</c> when parsing <c>cvar</c>; its tuple headers require embedded peak tuples and its data contains one delta sequence.</param>
    /// <param name="sharedTuples">Shared tuple records available to the store.</param>
    /// <returns>The parsed tuple variation store.</returns>
    /// <exception cref="InvalidDataException">The serialized store contains an invalid tuple reference or an invalid <c>cvar</c> tuple header.</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#tuple-variation-store-header">OpenType Tuple Variation Store Header</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar">OpenType <c>gvar</c> Table</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvar">OpenType <c>cvar</c> Table</seealso>
    public static TupleVariationStore Parse(ref Cursor cursor, int axisCount, bool isCvar, IReadOnlyList<TupleRecord>? sharedTuples = null)
    {
        long storeStart = cursor.Position;
        ushort rawCount = cursor.ReadUInt16();
        ushort dataOffset = cursor.ReadUInt16();

        int tupleCount = rawCount & (ushort)TupleVariationStoreFlags.CountMask;
        if (tupleCount == 0 && (rawCount & (ushort)TupleVariationStoreFlags.SharedPointNumbers) == 0)
            throw new InvalidDataException("Tuple variation store has zero tuple variation tables and no shared points.");

        var headers = new TupleVariationHeader[tupleCount];
        for (int i = 0; i < tupleCount; i++)
            headers[i] = TupleVariationHeader.Parse(ref cursor, axisCount, isCvar);

        // After the header loop, resolve each header's peak from the shared tuple array when needed.
        for (int i = 0; i < headers.Length; i++)
        {
            if (!headers[i].HasEmbeddedPeakTuple)
            {
                ushort idx = headers[i].SharedTupleIndex;
                if (sharedTuples is null || idx >= sharedTuples.Count)
                    throw new InvalidDataException(
                        $"Tuple {i} references shared tuple {idx}, but only " +
                        $"{(sharedTuples?.Count ?? 0)} are available.");
                headers[i] = headers[i] with { PeakTuple = sharedTuples[idx] };
            }
        }

        cursor.Position = storeStart + dataOffset;

        PackedPointNumbers? sharedPoints = null;
        if ((rawCount & (ushort)TupleVariationStoreFlags.SharedPointNumbers) != 0)
            sharedPoints = cursor.ReadRecord<PackedPointNumbers>();

        int deltaAxes = isCvar ? 1 : 2;
        var data = new TupleVariationTableData[tupleCount];

        for (int i = 0; i < tupleCount; i++)
        {
            long tableStart = cursor.Position;

            PackedPointNumbers? privatePoints = null;
            if (headers[i].HasPrivatePointNumbers)
                privatePoints = cursor.ReadRecord<PackedPointNumbers>();

            int pointCount = privatePoints is { IsAllPoints: false } pp
                ? pp.Points.Count
                : sharedPoints is { IsAllPoints: false } sp
                    ? sp.Points.Count
                    : 0;

            var deltas = new int[deltaAxes][];
            for (int axis = 0; axis < deltaAxes; axis++)
                deltas[axis] = PackedDeltas.Parse(ref cursor, pointCount);

            data[i] = new() { PrivatePointNumbers = privatePoints, Deltas = deltas };
            cursor.Position = tableStart + headers[i].VariationDataSize;
        }

        return new(rawCount)
        {
            SharedPointNumbers = sharedPoints,
            Headers = headers,
            Data = data,
            SharedTuples = sharedTuples ?? []
        };
    }

    /// <summary>Computes the interpolation scalar for a tuple at the specified normalized coordinates.</summary>
    /// <param name="peak">The tuple's peak coordinates.</param>
    /// <param name="intermediateStart">The intermediate region start coordinates, or <c>null</c>.</param>
    /// <param name="intermediateEnd">The intermediate region end coordinates, or <c>null</c>.</param>
    /// <param name="normalizedCoords">The normalized font coordinates in variation-axis order.</param>
    /// <returns>The interpolation scalar in the range from 0 to 1.</returns>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#interpolation">OpenType Variation Interpolation</seealso>
    public static double ComputeScalar(
        TupleRecord peak,
        TupleRecord? intermediateStart,
        TupleRecord? intermediateEnd,
        ReadOnlySpan<F2Dot14> normalizedCoords)
    {
        var peakCoords = peak.Coordinates;
        int axisCount = Math.Min(peakCoords.Count, normalizedCoords.Length);

        double scalar = 1.0;
        for (int a = 0; a < axisCount; a++)
        {
            double p = peakCoords[a].Value;
            if (p == 0.0) continue;

            double c = normalizedCoords[a].Value;
            double axisFactor;

            if (intermediateStart is not null && intermediateEnd is not null)
            {
                double start = intermediateStart!.Value.Coordinates[a].Value;
                double end = intermediateEnd!.Value.Coordinates[a].Value;

                if (c < start || c > end) return 0.0;

                if (c < p)
                    axisFactor = p == start ? 1.0 : (c - start) / (p - start);
                else if (c > p)
                    axisFactor = p == end ? 1.0 : (end - c) / (end - p);
                else
                    axisFactor = 1.0;
            }
            else
            {
                if (c == 0.0) return 0.0;
                if ((p > 0.0) != (c > 0.0)) return 0.0;
                axisFactor = Math.Min(1.0, Math.Abs(c) / Math.Abs(p));
            }

            scalar *= axisFactor;
            if (scalar == 0.0) return 0.0;
        }
        return scalar;
    }

    /// <summary>Computes the interpolation scalar for tuple <paramref name="tupleIndex"/> at the specified normalized coordinates.</summary>
    /// <param name="tupleIndex">Zero-based index of the tuple variation table.</param>
    /// <param name="normalizedCoords">The normalized font coordinates in variation-axis order.</param>
    /// <returns>The interpolation scalar in the range from 0 to 1.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tupleIndex"/> is outside <see cref="Headers"/>.</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#interpolation">OpenType Variation Interpolation</seealso>
    public double GetScalar(int tupleIndex, ReadOnlySpan<F2Dot14> normalizedCoords)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tupleIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(tupleIndex, Headers.Count);

        var header = Headers[tupleIndex];
        var peak = GetPeakTuple(tupleIndex);
        return ComputeScalar(peak, header.IntermediateStartTuple, header.IntermediateEndTuple, normalizedCoords);
    }
}

/// <summary>A tuple variation header describing one tuple's variation region and serialized data.</summary>
/// <remarks>The peak tuple is resolved during parsing, whether it was embedded in the header or referenced through the store's shared tuple array.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#tuple-variation-header">OpenType Tuple Variation Header</seealso>
public sealed record TupleVariationHeader
{
    /// <summary>Gets the size in bytes of this tuple's serialized variation data, excluding its header.</summary>
    public int VariationDataSize { get; init; }

    /// <summary>Gets the decoded flag bits from the raw <c>tupleIndex</c> field.</summary>
    public TupleVariationIndexFlags Flags { get; init; }

    /// <summary>Gets the resolved peak tuple. This value is never <c>null</c>.</summary>
    public TupleRecord PeakTuple { get; init; }

    /// <summary>Gets the intermediate region start tuple, or <c>null</c> when no intermediate region is present.</summary>
    public TupleRecord? IntermediateStartTuple { get; init; }

    /// <summary>Gets the intermediate region end tuple, or <c>null</c> when no intermediate region is present.</summary>
    public TupleRecord? IntermediateEndTuple { get; init; }

    /// <summary>Gets a value indicating whether the tuple uses an intermediate region.</summary>
    public bool HasIntermediateRegion => (Flags & TupleVariationIndexFlags.IntermediateRegion) != 0;

    /// <summary>Gets a value indicating whether the tuple's serialized data carries private point numbers.</summary>
    public bool HasPrivatePointNumbers => (Flags & TupleVariationIndexFlags.PrivatePointNumbers) != 0;

    /// <summary>Gets a value indicating whether the peak tuple is embedded in the header.</summary>
    public bool HasEmbeddedPeakTuple => (Flags & TupleVariationIndexFlags.EmbeddedPeakTuple) != 0;

    /// <summary>Gets the index into the store's shared tuple array when the peak tuple is not embedded.</summary>
    public ushort SharedTupleIndex { get; init; }

    /// <summary>Parses a tuple variation header from <paramref name="cursor"/>.</summary>
    /// <param name="cursor">Cursor positioned at the <c>variationDataSize</c> field.</param>
    /// <param name="axisCount">Number of variation axes from <c>fvar</c>.</param>
    /// <param name="isCvar"><c>true</c> when parsing a <c>cvar</c> tuple header.</param>
    /// <returns>The parsed tuple variation header.</returns>
    /// <exception cref="InvalidDataException"><c>cvar</c> does not contain an embedded peak tuple.</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#tuple-variation-header">OpenType Tuple Variation Header</seealso>
    public static TupleVariationHeader Parse(ref Cursor cursor, int axisCount, bool isCvar)
    {
        int size = cursor.ReadUInt16();
        ushort rawTupleIndex = cursor.ReadUInt16();
        var flags = (TupleVariationIndexFlags)rawTupleIndex;

        bool embedded = (flags & TupleVariationIndexFlags.EmbeddedPeakTuple) != 0;
        if (isCvar && !embedded)
            throw new InvalidDataException("'cvar' tuple header requires an embedded peak tuple.");

        TupleRecord peak = default!;
        if (embedded)
            peak = TupleRecord.Parse(ref cursor, axisCount);

        TupleRecord? start = null, end = null;
        if ((flags & TupleVariationIndexFlags.IntermediateRegion) != 0)
        {
            start = TupleRecord.Parse(ref cursor, axisCount);
            end = TupleRecord.Parse(ref cursor, axisCount);
        }

        return new TupleVariationHeader
        {
            VariationDataSize = size,
            Flags = flags,
            PeakTuple = peak!,
            IntermediateStartTuple = start,
            IntermediateEndTuple = end,
            SharedTupleIndex = embedded ? (ushort)0 : (ushort)(rawTupleIndex & 0x0FFF),
        };
    }
}

/// <summary>Defines the flag bits in the <c>tupleIndex</c> field of a <see cref="TupleVariationHeader"/>.</summary>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#tuple-variation-header">OpenType Tuple Variation Header</seealso>
[Flags]
public enum TupleVariationIndexFlags : ushort
{
    /// <summary>Bit 15: the header includes an embedded peak tuple record; required by <c>cvar</c>.</summary>
    EmbeddedPeakTuple = 0x8000,

    /// <summary>Bit 14: the header includes intermediate start and end tuple records.</summary>
    IntermediateRegion = 0x4000,

    /// <summary>Bit 13: the serialized variation data begins with private point numbers.</summary>
    PrivatePointNumbers = 0x2000,

    /// <summary>Bit 12: reserved; must be zero.</summary>
    Reserved = 0x1000,

    /// <summary>Mask for the low 12 bits containing the shared tuple index.</summary>
    TupleIndexMask = 0x0FFF,
}

/// <summary>A tuple record containing a position in normalized variation space as F2DOT14 coordinates.</summary>
/// <remarks>The number of coordinates corresponds to the variation-axis count from <c>fvar</c>, and coordinates are stored in variation-axis order.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#tuple-record">OpenType Tuple Record</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/fvar">OpenType <c>fvar</c> Table</seealso>
public readonly struct TupleRecord
{
    /// <summary>Gets the normalized F2DOT14 coordinate on each variation axis.</summary>
    public IReadOnlyList<F2Dot14> Coordinates { get; }

    /// <summary>Creates a tuple record from pre-read coordinates.</summary>
    /// <param name="coordinates">The normalized coordinates in variation-axis order.</param>
    public TupleRecord(IReadOnlyList<F2Dot14> coordinates) => Coordinates = coordinates;

    /// <summary>Reads a tuple record containing <paramref name="axisCount"/> F2DOT14 coordinates.</summary>
    /// <param name="cursor">Cursor positioned at the first coordinate.</param>
    /// <param name="axisCount">Number of variation axes.</param>
    /// <returns>The parsed tuple record.</returns>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#tuple-record">OpenType Tuple Record</seealso>
    public static TupleRecord Parse(ref Cursor cursor, int axisCount) =>
        new(cursor.ReadBigEndianStructArray<F2Dot14>(axisCount));

    /// <inheritdoc/>
    public override string ToString() => $"({string.Join(", ", Coordinates)})";
}

/// <summary>Defines the flag bits in the packed <c>tupleVariationCount</c> field of a <see cref="TupleVariationStore"/> header.</summary>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#tuple-variation-store-header">OpenType Tuple Variation Store Header</seealso>
[Flags]
public enum TupleVariationStoreFlags : ushort
{
    /// <summary>Bit 15: shared point numbers precede the per-tuple variation data.</summary>
    SharedPointNumbers = 0x8000,

    /// <summary>Bits 12–14: reserved; must be zero.</summary>
    Reserved = 0x7000,

    /// <summary>Mask for the low 12 bits containing the number of tuple variation tables.</summary>
    CountMask = 0x0FFF,
}

/// <summary>Contains the serialized variation data for one tuple variation table.</summary>
/// <remarks>In <c>gvar</c> there are separate X and Y delta sequences; in <c>cvar</c> there is one delta sequence.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon">OpenType Variation Common Formats</seealso>
public sealed record TupleVariationTableData
{
    /// <summary>Gets the private point numbers, or <c>null</c> when the table uses the shared point list.</summary>
    public PackedPointNumbers? PrivatePointNumbers { get; init; }

    /// <summary>Gets the delta sequences: two sequences for <c>gvar</c> and one sequence for <c>cvar</c>.</summary>
    public IReadOnlyList<int[]> Deltas { get; init; } = [];
}

/// <summary>Provides parsing for packed variation delta sequences.</summary>
/// <remarks>Deltas are run-length encoded by control bytes; the logical delta count is supplied by the caller.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#packed-delta-data">OpenType Packed Delta Data</seealso>
public static class PackedDeltas
{
    /// <summary>Reads <paramref name="count"/> packed deltas from <paramref name="cursor"/>.</summary>
    /// <param name="cursor">Cursor positioned at the first control byte.</param>
    /// <param name="count">Number of logical delta values to decode.</param>
    /// <returns>The decoded signed delta values.</returns>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#packed-delta-data">OpenType Packed Delta Data</seealso>
    public static int[] Parse(ref Cursor cursor, int count)
    {
        if (count == 0) return [];

        var deltas = new int[count];
        int written = 0;

        while (written < count)
        {
            byte control = cursor.ReadUInt8();
            bool areZero = (control & 0x80) != 0;
            bool areWords = (control & 0x40) != 0;
            int runCount = (control & 0x3F) + 1;

            for (int i = 0; i < runCount && written < count; i++)
            {
                if (areZero)
                {
                    deltas[written++] = 0;
                }
                else if (areWords)
                {
                    deltas[written++] = cursor.ReadInt16();
                }
                else
                {
                    deltas[written++] = cursor.ReadInt8();
                }
            }
        }

        return deltas;
    }
}

/// <summary>Contains packed point-number data identifying the points affected by a tuple variation table.</summary>
/// <remarks>Point numbers are delta-encoded. A count of zero means that the deltas apply to all points and no explicit point list follows.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#packed-point-numbers">OpenType Packed Point Numbers</seealso>
public sealed record PackedPointNumbers : IRecord<PackedPointNumbers>
{
    /// <summary>Gets the decoded point numbers.</summary>
    public required IReadOnlyList<int> Points { get; init; }

    /// <summary>Gets a value indicating whether the encoded count was zero and the deltas apply to all points.</summary>
    public required bool IsAllPoints { get; init; }

    /// <summary>Reads packed point-number data from <paramref name="cursor"/>.</summary>
    /// <param name="cursor">Cursor positioned at the encoded point count.</param>
    /// <param name="context">Unused; may be <c>null</c>.</param>
    /// <returns>The parsed packed point-number data.</returns>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#packed-point-numbers">OpenType Packed Point Numbers</seealso>
    static PackedPointNumbers IRecord<PackedPointNumbers>.Parse(ref Cursor cursor, object? context)
    {
        int count = ReadCount(ref cursor);
        if (count == 0)
            return new() { Points = [], IsAllPoints = true };

        var points = new List<int>(count);
        int last = 0;

        while (points.Count < count)
        {
            byte control = cursor.ReadUInt8();
            bool areWords = (control & 0x80) != 0;
            int runCount = (control & 0x7F) + 1;

            for (int i = 0; i < runCount && points.Count < count; i++)
            {
                int delta = areWords ? cursor.ReadUInt16() : cursor.ReadUInt8();
                last += delta;
                points.Add(last);
            }
        }

        return new() { Points = points, IsAllPoints = false };
    }

    /// <summary>Reads the encoded point-number count from <paramref name="cursor"/>.</summary>
    /// <param name="cursor">Cursor positioned at the count field.</param>
    /// <returns>The decoded point count; zero indicates that all points are affected.</returns>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#packed-point-numbers">OpenType Packed Point Numbers</seealso>
    public static int ReadCount(ref Cursor cursor)
    {
        byte first = cursor.ReadUInt8();
        if (first == 0) return 0;
        if ((first & 0x80) == 0) return first;
        byte second = cursor.ReadUInt8();
        return ((first & 0x7F) << 8) | second;
    }
}
