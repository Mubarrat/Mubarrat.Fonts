using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// vmtx — Vertical Metrics Table
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>vmtx</c> table: vertical metrics for the glyphs in a vertical font.</summary>
/// <remarks>The table has no header. It contains <c>numberOfVMetrics</c> <c>longVerMetric</c> records followed by top side bearings for the remaining glyphs. Glyphs beyond the long metrics reuse the final advance height. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"><c>vmtx</c> specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
public sealed record VmtxTable : IFontTable<VmtxTable>
{
    /// <summary>Gets the OpenType table tag <c>vmtx</c>.</summary>
    public static Tag Tag => "vmtx";

    /// <summary>Gets the advance heights of the long vertical metric records.</summary>
    /// <remarks>The length is <see cref="NumberOfVMetrics"/>. Glyphs at or beyond that index reuse the final advance height.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"/>
    public IReadOnlyList<ushort> AdvanceHeights { get; init; } = [];

    /// <summary>Gets the top side bearing of every glyph.</summary>
    /// <remarks>The length is <see cref="NumGlyphs"/>. The first <see cref="NumberOfVMetrics"/> entries come from <c>longVerMetric</c> records; the remainder come from the trailing top-side-bearing array.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"/>
    public IReadOnlyList<short> TopSideBearings { get; init; } = [];

    /// <summary>Gets the number of <c>longVerMetric</c> records.</summary>
    /// <remarks>This value is taken from <c>vhea.numberOfVMetrics</c>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"/>
    public int NumberOfVMetrics { get; init; }

    /// <summary>Gets the total number of glyphs in the font.</summary>
    /// <remarks>This value is taken from <c>maxp.numGlyphs</c>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"/>
    public int NumGlyphs { get; init; }

    /// <summary>Gets the advance height of glyph <paramref name="glyphId"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is negative or greater than or equal to <see cref="NumGlyphs"/>.</exception>
    /// <remarks>Glyphs beyond <see cref="NumberOfVMetrics"/> use the advance height of the final long vertical metric.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"/>
    public ushort GetAdvanceHeight(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, NumGlyphs);

        int index = glyphId < NumberOfVMetrics ? glyphId : NumberOfVMetrics - 1;
        return AdvanceHeights[index];
    }

    /// <summary>Gets the top side bearing of glyph <paramref name="glyphId"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is negative or greater than or equal to <see cref="NumGlyphs"/>.</exception>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"/>
    public short GetTopSideBearing(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, NumGlyphs);
        return TopSideBearings[glyphId];
    }

    /// <summary>Gets the advance height and top side bearing of glyph <paramref name="glyphId"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is negative or greater than or equal to <see cref="NumGlyphs"/>.</exception>
    /// <remarks>The returned <c>AdvanceHeight</c> uses the final long vertical metric for glyphs beyond <see cref="NumberOfVMetrics"/>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"/>
    public (ushort AdvanceHeight, short TopSideBearing) GetMetric(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, NumGlyphs);

        int index = glyphId < NumberOfVMetrics ? glyphId : NumberOfVMetrics - 1;
        return (AdvanceHeights[index], TopSideBearings[glyphId]);
    }

    /// <summary>The 4-byte fixed-layout <c>longVerMetric</c> record containing an advance height and top side bearing.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct LongVerMetric : IEndianReversibleStruct<LongVerMetric>
    {
        /// <summary>The advance height at byte offset 0.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"/>
        public ushort AdvanceHeight;      // +0

        /// <summary>The top side bearing at byte offset 2.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"/>
        public short TopSideBearing;      // +2

        /// <summary>Reverses the byte order of both fields in a <see cref="LongVerMetric"/>.</summary>
        public static LongVerMetric ReverseEndianness(LongVerMetric v) => new()
        {
            AdvanceHeight = BinaryPrimitives.ReverseEndianness(v.AdvanceHeight),
            TopSideBearing = BinaryPrimitives.ReverseEndianness(v.TopSideBearing),
        };
    }

    /// <inheritdoc/>
    static VmtxTable IRecord<VmtxTable>.Parse(ref Cursor cursor, object? context)
    {
        var face = (FontFace)context!;
        VheaTable vhea = face.GetTable<VheaTable>();
        MaxpTable maxp = face.GetTable<MaxpTable>();

        int numberOfVMetrics = vhea.NumberOfVMetrics;
        int numGlyphs = maxp.NumGlyphs;

        if (numberOfVMetrics == 0)
            throw new InvalidDataException(
                "'vhea'.numOfLongVerMetrics is 0; 'vmtx' requires at least one long metric record.");

        if (numberOfVMetrics > numGlyphs)
            throw new InvalidDataException(
                $"'vhea'.numOfLongVerMetrics ({numberOfVMetrics}) exceeds 'maxp'.numGlyphs ({numGlyphs}).");

        // vmtx has no header. Read the long metrics directly from the cursor; it advances
        // by 4 bytes per record.
        var longs = cursor.ReadBigEndianStructArray<LongVerMetric>(numberOfVMetrics);

        var advanceHeights = new ushort[numberOfVMetrics];
        var topSideBearings = new short[numGlyphs];

        for (int i = 0; i < numberOfVMetrics; i++)
        {
            advanceHeights[i] = longs[i].AdvanceHeight;
            topSideBearings[i] = longs[i].TopSideBearing;
        }

        // Trailing glyphs carry only a top side bearing. Read the remaining entries in one
        // batched call; the cursor advances by (numGlyphs - numberOfVMetrics) * 2 bytes.
        if (numGlyphs > numberOfVMetrics)
        {
            var tail = cursor.ReadInt16Array(numGlyphs - numberOfVMetrics);
            tail.CopyTo(topSideBearings.AsSpan(numberOfVMetrics));
        }

        return new VmtxTable
        {
            AdvanceHeights = advanceHeights,
            TopSideBearings = topSideBearings,
            NumberOfVMetrics = numberOfVMetrics,
            NumGlyphs = numGlyphs,
        };
    }
}
