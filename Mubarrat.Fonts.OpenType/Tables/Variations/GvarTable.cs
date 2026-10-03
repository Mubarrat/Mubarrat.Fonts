using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Tables.Variations;

/// <summary>The <c>gvar</c> table: per-glyph variation data for variable-font outlines.</summary>
/// <remarks>The table contains shared tuple records and one variable-length glyph variation data block for each glyph. Each glyph block is a tuple variation store whose tuple scalars are evaluated against normalized variation coordinates. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"><c>gvar</c> specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon"/>
public sealed record GvarTable : IOpenTypeTable<GvarTable>
{
    /// <summary>Gets the OpenType table tag <c>gvar</c>.</summary>
    public static Tag Tag => "gvar";

    /// <summary>Gets the major version.</summary>
    /// <remarks>Must be 1.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version.</summary>
    /// <remarks>Must be 0 for the current <c>gvar</c> format.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the number of variation axes.</summary>
    /// <remarks>Must equal <c>fvar.axisCount</c>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
    public int AxisCount { get; init; }

    /// <summary>Gets the shared tuple records referenced by tuple variation headers.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar#tuple-record"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#tuple-record"/>
    public IReadOnlyList<TupleRecord> SharedTuples { get; init; } = [];

    /// <summary>Gets the number of glyphs covered by the table.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
    public int GlyphCount { get; init; }

    /// <summary>Gets a value indicating whether glyph variation data offsets use the 32-bit format.</summary>
    /// <remarks>When <c>false</c>, offsets are stored as 16-bit values multiplied by 2.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
    public bool UsesLongOffsets { get; init; }

    /// <summary>Gets the raw glyph variation data blocks indexed by glyph ID.</summary>
    /// <remarks>An entry is <c>null</c> when the corresponding glyph has no variation data.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
    public byte[]?[] GlyphVariationData { get; init; } = [];

    /// <summary>The 20-byte fixed-layout <c>gvar</c> table header.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>The major version at byte offset 0.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
        public ushort MajorVersion;

        /// <summary>The minor version at byte offset 2.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
        public ushort MinorVersion;

        /// <summary>The number of variation axes at byte offset 4.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
        public ushort AxisCount;

        /// <summary>The number of shared tuple records at byte offset 6.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
        public ushort SharedTupleCount;

        /// <summary>The offset to the shared tuple array at byte offset 8.</summary>
        /// <remarks>The offset is measured from the beginning of the <c>gvar</c> table.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
        public uint SharedTuplesOffset;

        /// <summary>The number of glyphs at byte offset 12.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
        public ushort GlyphCount;

        /// <summary>The flags at byte offset 14.</summary>
        /// <remarks>Bit 0 selects long 32-bit glyph variation data offsets; all other bits are reserved.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
        public ushort Flags;

        /// <summary>The offset to the glyph variation data offset array at byte offset 16.</summary>
        /// <remarks>The offset is measured from the beginning of the <c>gvar</c> table.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
        public uint GlyphVariationDataArrayOffset;

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        public static Header ReverseEndianness(Header value) => new()
        {
            MajorVersion = BinaryPrimitives.ReverseEndianness(value.MajorVersion),
            MinorVersion = BinaryPrimitives.ReverseEndianness(value.MinorVersion),
            AxisCount = BinaryPrimitives.ReverseEndianness(value.AxisCount),
            SharedTupleCount = BinaryPrimitives.ReverseEndianness(value.SharedTupleCount),
            SharedTuplesOffset = BinaryPrimitives.ReverseEndianness(value.SharedTuplesOffset),
            GlyphCount = BinaryPrimitives.ReverseEndianness(value.GlyphCount),
            Flags = BinaryPrimitives.ReverseEndianness(value.Flags),
            GlyphVariationDataArrayOffset = BinaryPrimitives.ReverseEndianness(value.GlyphVariationDataArrayOffset),
        };
    }

    /// <summary>Gets a value indicating whether <paramref name="glyphId"/> has variation data.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <returns><c>true</c> when the glyph has a non-empty variation data block; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is negative or is greater than or equal to <see cref="GlyphCount"/>.</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar"/>
    public bool HasVariations(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, GlyphCount);
        return GlyphVariationData[glyphId] is { Length: > 0 };
    }

    /// <summary>Decodes the tuple variation store for <paramref name="glyphId"/>.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <returns>The decoded tuple variation store, or <c>null</c> when the glyph has no variation data.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is negative or is greater than or equal to <see cref="GlyphCount"/>.</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar#glyphvariationdata"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommon#tuple-variation-store"/>
    public TupleVariationStore? DecodeGlyphVariations(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, GlyphCount);

        var bytes = GlyphVariationData[glyphId];
        if (bytes is null || bytes.Length == 0) return null;

        using var source = new MemorySource(bytes);
        var cursor = new Cursor(source);
        return TupleVariationStore.Parse(ref cursor, AxisCount, isCvar: false, SharedTuples);
    }

    /// <summary>Gets the fully applied X and Y variation deltas for <paramref name="glyphId"/> at the specified normalized coordinates.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <param name="normalizedCoords">The normalized variation coordinates, with one value for each axis.</param>
    /// <param name="outlinePointCount">The number of contour points in the glyph outline, excluding the four phantom points.</param>
    /// <returns>The applied glyph variation, including the four phantom-point deltas, or <c>null</c> when the glyph has no variation data.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is outside the range of <see cref="GlyphCount"/>, or <paramref name="outlinePointCount"/> is negative.</exception>
    /// <remarks>The returned arrays contain the outline points followed by the four phantom points. The phantom points represent horizontal advance, vertical advance, horizontal origin, and vertical origin. Variation deltas are applied in glyph design-unit space.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar#glyphvariationdata"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ttglyf#phantom-points"/>
    public GlyphVariation? GetGlyphVariation(int glyphId, ReadOnlySpan<F2Dot14> normalizedCoords, int outlinePointCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, GlyphCount);
        ArgumentOutOfRangeException.ThrowIfNegative(outlinePointCount);

        var store = DecodeGlyphVariations(glyphId);
        if (store is null) return null;

        int totalPoints = checked(outlinePointCount + 4);
        var xDeltas = new int[totalPoints];
        var yDeltas = new int[totalPoints];

        for (int t = 0; t < store.TupleVariationCount; t++)
        {
            double scalar = store.GetScalar(t, normalizedCoords);
            if (scalar == 0.0) continue;

            var data = store.Data[t];
            var pointNumbers = data.PrivatePointNumbers ?? store.SharedPointNumbers;
            var deltasX = data.Deltas[0];
            var deltasY = data.Deltas[1];

            if (pointNumbers is { IsAllPoints: false } pts)
            {
                int count = Math.Min(pts.Points.Count, Math.Min(deltasX.Length, deltasY.Length));
                for (int i = 0; i < count; i++)
                {
                    int pointIndex = pts.Points[i];
                    if ((uint)pointIndex >= (uint)totalPoints) continue;
                    xDeltas[pointIndex] += ApplyDelta(deltasX[i], scalar);
                    yDeltas[pointIndex] += ApplyDelta(deltasY[i], scalar);
                }
            }
            else
            {
                int count = Math.Min(Math.Min(deltasX.Length, deltasY.Length), totalPoints);
                for (int i = 0; i < count; i++)
                {
                    xDeltas[i] += ApplyDelta(deltasX[i], scalar);
                    yDeltas[i] += ApplyDelta(deltasY[i], scalar);
                }
            }
        }

        return new GlyphVariation
        {
            OutlinePointCount = outlinePointCount,
            PointCount = totalPoints,
            XDeltas = xDeltas,
            YDeltas = yDeltas,
        };
    }

    /// <summary>Applies a variation scalar to a delta and rounds the result to the nearest integer.</summary>
    /// <param name="delta">The unscaled design-unit delta.</param>
    /// <param name="scalar">The tuple variation scalar.</param>
    /// <returns>The scaled and rounded delta.</returns>
    public static int ApplyDelta(int delta, double scalar)
    {
        if (delta == 0 || scalar == 0.0) return 0;
        double product = delta * scalar;
        return (int)Math.Round(product, MidpointRounding.AwayFromZero);
    }

    /// <inheritdoc/>
    public static GvarTable Parse(ref Cursor cursor, object? context)
    {
        var face = (FontFace)context!;
        int fvarAxisCount = face.GetTable<FvarTable>().AxisCount;

        Header h = cursor.ReadBigEndianStruct<Header>();

        if (h.MajorVersion != 1)
            throw new InvalidDataException($"'gvar'.majorVersion is {h.MajorVersion}, expected 1.");
        if (h.AxisCount != fvarAxisCount)
            throw new InvalidDataException(
                $"'gvar'.axisCount is {h.AxisCount}, but 'fvar'.axisCount is {fvarAxisCount}.");

        bool longOffsets = (h.Flags & 0x0001) != 0;

        // glyphVariationDataOffsets: glyphCount + 1 entries immediately after the 20-byte
        // header. Each is either uint16 (stored as offset/2) or uint32 (absolute from gvar
        // table start).
        long[] offsets = new long[h.GlyphCount + 1];
        if (longOffsets)
        {
            for (int i = 0; i <= h.GlyphCount; i++)
                offsets[i] = cursor.ReadUInt32();
        }
        else
        {
            for (int i = 0; i <= h.GlyphCount; i++)
                offsets[i] = (long)cursor.ReadUInt16() * 2;
        }

        // Shared tuples.
        var sharedTuples = new TupleRecord[h.SharedTupleCount];
        if (h.SharedTupleCount > 0)
        {
            var sharedCursor = cursor.At(h.SharedTuplesOffset);
            for (int i = 0; i < h.SharedTupleCount; i++)
                sharedTuples[i] = TupleRecord.Parse(ref sharedCursor, h.AxisCount);
        }

        // Per-glyph variation data. Offsets are relative to gvar table start.
        var glyphData = new byte[]?[h.GlyphCount];
        for (int gid = 0; gid < h.GlyphCount; gid++)
        {
            long start = offsets[gid];
            long end = offsets[gid + 1];
            if (end <= start) continue;

            int length = checked((int)(end - start));
            glyphData[gid] = cursor.Source.ReadBytesAt(start, length);
        }

        return new GvarTable
        {
            MajorVersion = h.MajorVersion,
            MinorVersion = h.MinorVersion,
            AxisCount = h.AxisCount,
            SharedTuples = sharedTuples,
            GlyphCount = h.GlyphCount,
            UsesLongOffsets = longOffsets,
            GlyphVariationData = glyphData,
        };
    }
}

/// <summary>Contains the applied variation deltas for one glyph at one variation instance.</summary>
/// <remarks>The delta arrays contain the glyph's outline points followed by four phantom points. The phantom points represent horizontal advance, vertical advance, horizontal origin, and vertical origin.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar#glyphvariationdata"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ttglyf#phantom-points"/>
public sealed record GlyphVariation
{
    /// <summary>Gets the number of contour points in the glyph outline.</summary>
    public int OutlinePointCount { get; init; }

    /// <summary>Gets the total number of points covered by the delta arrays, equal to <see cref="OutlinePointCount"/> plus four phantom points.</summary>
    public int PointCount { get; init; }

    /// <summary>Gets the X-coordinate deltas for all outline and phantom points.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar#glyphvariationdata"/>
    public IReadOnlyList<int> XDeltas { get; init; } = [];

    /// <summary>Gets the Y-coordinate deltas for all outline and phantom points.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gvar#glyphvariationdata"/>
    public IReadOnlyList<int> YDeltas { get; init; } = [];

    /// <summary>Gets the horizontal-advance delta from the first phantom point.</summary>
    public int HorizontalAdvanceDelta => PointCount >= 4 ? XDeltas[PointCount - 4] : 0;

    /// <summary>Gets the vertical-advance delta from the second phantom point.</summary>
    public int VerticalAdvanceDelta => PointCount >= 3 ? YDeltas[PointCount - 3] : 0;
}
