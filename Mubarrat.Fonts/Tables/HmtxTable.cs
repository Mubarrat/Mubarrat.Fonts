using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>
/// The <c>hmtx</c> table: horizontal metrics for glyphs.
/// </summary>
/// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx">OpenType hmtx — Horizontal Header</see> defines the horizontal metrics table. The table is fully materialized during parsing; no <see cref="Source"/> reference is retained. The first <see cref="NumberOfHMetrics"/> glyphs have an advance width and left side bearing, while remaining glyphs reuse the last advance width and store only their left side bearing.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx"/>
public sealed record HmtxTable : IFontTable<HmtxTable>
{
    /// <inheritdoc/>
    public static Tag Tag => "hmtx";

    /// <summary>Gets the distinct advance-width values, one for each long horizontal metric record.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics">Long metrics</see> stores one advance width for each of the first <see cref="NumberOfHMetrics"/> glyphs; subsequent glyphs reuse the final value.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics"/>
    public IReadOnlyList<ushort> AdvanceWidths { get; init; } = [];

    /// <summary>Gets the left side bearing of every glyph.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics">Long metrics</see> defines a left side bearing for every glyph, with trailing values stored separately when fewer long metrics exist than glyphs.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics"/>
    public IReadOnlyList<short> LeftSideBearings { get; init; } = [];

    /// <summary>Gets the number of long horizontal metric records.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea">hhea</see> supplies <c>numberOfHMetrics</c>, which determines how many advance-width and left-side-bearing pairs are stored in <c>hmtx</c>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea"/>
    public int NumberOfHMetrics { get; init; }

    /// <summary>Gets the total number of glyphs in the font.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp">maxp</see> supplies <c>numGlyphs</c>, which determines the number of left side bearings represented by this table.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp"/>
    public int NumGlyphs { get; init; }

    /// <summary>Gets the advance width of glyph <paramref name="glyphId"/> in font design units.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is negative or greater than or equal to <see cref="NumGlyphs"/>.</exception>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics">Long metrics</see> specifies that glyphs beyond the long-metric range use the advance width of the final long metric.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics"/>
    public ushort GetAdvanceWidth(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, NumGlyphs);

        int index = glyphId < NumberOfHMetrics ? glyphId : NumberOfHMetrics - 1;
        return AdvanceWidths[index];
    }

    /// <summary>Gets the left side bearing of glyph <paramref name="glyphId"/> in font design units.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is negative or greater than or equal to <see cref="NumGlyphs"/>.</exception>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics">Long metrics</see> defines the left side bearing for every glyph.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics"/>
    public short GetLeftSideBearing(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, NumGlyphs);
        return LeftSideBearings[glyphId];
    }

    /// <summary>Gets both the advance width and left side bearing of glyph <paramref name="glyphId"/>.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <returns>A tuple containing the advance width and left side bearing in font design units.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is negative or greater than or equal to <see cref="NumGlyphs"/>.</exception>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics">Long metrics</see> defines these two values as the horizontal metrics associated with a glyph.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics"/>
    public (ushort AdvanceWidth, short LeftSideBearing) GetMetric(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, NumGlyphs);

        int index = glyphId < NumberOfHMetrics ? glyphId : NumberOfHMetrics - 1;
        return (AdvanceWidths[index], LeftSideBearings[glyphId]);
    }

    /// <summary>Gets the right side bearing of glyph <paramref name="glyphId"/> from its horizontal metrics and bounding box.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <param name="xMin">The glyph's minimum x coordinate.</param>
    /// <param name="xMax">The glyph's maximum x coordinate.</param>
    /// <returns>The right side bearing, calculated as <c>advanceWidth - (leftSideBearing + xMax - xMin)</c>.</returns>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#additional-information">Additional information</see> defines the relationship between advance width, side bearings, and glyph extents.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#additional-information"/>
    public short GetRightSideBearing(int glyphId, short xMin, short xMax)
    {
        var (aw, lsb) = GetMetric(glyphId);
        return (short)(aw - (lsb + (xMax - xMin)));
    }

    /// <inheritdoc/>
    static HmtxTable IRecord<HmtxTable>.Parse(ref Cursor cursor, object? context)
    {
        // hmtx depends on hhea and maxp. The parent is the FontFace, which caches them.
        var face = (FontFace)context!;
        HheaTable hhea = face.GetTable<HheaTable>();
        MaxpTable maxp = face.GetTable<MaxpTable>();

        int numberOfHMetrics = hhea.NumberOfHMetrics;
        int numGlyphs = maxp.NumGlyphs;

        if (numberOfHMetrics == 0)
            throw new InvalidDataException(
                "'hhea'.numberOfHMetrics is 0; 'hmtx' requires at least one long metric record.");

        if (numberOfHMetrics > numGlyphs)
            throw new InvalidDataException(
                $"'hhea'.numberOfHMetrics ({numberOfHMetrics}) exceeds 'maxp'.numGlyphs ({numGlyphs}).");

        // Read the long metrics in one pass. Typical fonts have a few hundred; CJK fonts
        // with per-glyph widths may push into the thousands. The stack path covers the
        // common case.
        var longs = cursor.ReadBigEndianStructArray<LongHorMetric>(numberOfHMetrics);

        // Split into parallel arrays: advance widths indexed by the long-metric range, left
        // side bearings indexed by glyph.
        var advanceWidths = new ushort[numberOfHMetrics];
        var leftSideBearings = new short[numGlyphs];
        for (int i = 0; i < numberOfHMetrics; i++)
        {
            ref readonly LongHorMetric m = ref longs[i];
            advanceWidths[i] = m.AdvanceWidth;
            leftSideBearings[i] = m.LeftSideBearing;
        }

        // Trailing glyphs carry only a left side bearing. The slice at [numberOfHMetrics..]
        // receives them; the caller reuses the last long metric's advance width.
        if (numGlyphs > numberOfHMetrics)
            cursor.ReadInt16Array(leftSideBearings.AsSpan(numberOfHMetrics));

        return new()
        {
            AdvanceWidths = advanceWidths,
            LeftSideBearings = leftSideBearings,
            NumberOfHMetrics = numberOfHMetrics,
            NumGlyphs = numGlyphs,
        };
    }

    /// <summary>A four-byte long horizontal metric containing an advance width followed by a left side bearing.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics">Long metrics</see> defines each record as an unsigned 16-bit advance width followed by a signed 16-bit left side bearing; the packed representation is exactly 4 bytes.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct LongHorMetric : IEndianReversibleStruct<LongHorMetric>
    {
        /// <summary>Gets or sets the advance width.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics">Long metrics</see> stores <c>advanceWidth</c> as an unsigned 16-bit integer at offset 0.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics"/>
        public ushort AdvanceWidth;      // +0

        /// <summary>Gets or sets the left side bearing.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics">Long metrics</see> stores <c>lsb</c> as a signed 16-bit integer at offset 2.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hmtx#long-metrics"/>
        public short LeftSideBearing;    // +2

        /// <inheritdoc/>
        public static LongHorMetric ReverseEndianness(LongHorMetric v) => new()
        {
            AdvanceWidth = BinaryPrimitives.ReverseEndianness(v.AdvanceWidth),
            LeftSideBearing = BinaryPrimitives.ReverseEndianness(v.LeftSideBearing),
        };
    }
}
