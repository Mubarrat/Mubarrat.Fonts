using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>vmtx</c> table.</summary>
/// <remarks>
/// <para>
/// <c>vmtx</c> mirrors <c>hmtx</c> for vertical writing. The two structural constraints
/// — <c>numberOfVMetrics ≥ 1</c> and <c>numberOfVMetrics ≤ numGlyphs</c> — are enforced
/// in <see cref="VmtxTable.Parse"/> because both values come from other tables
/// (<c>vhea</c> and <c>maxp</c>) that must be read before the first <c>vmtx</c> byte can
/// be interpreted. The analyzer therefore cannot observe those violations and only checks
/// the shape of the resulting metric arrays plus their agreement with <c>vhea</c>.
/// </para>
/// <para>
/// VMTX is a dependent of VHEA for the maxima/minima agreement rules. Cross-table
/// comparisons against <c>glyf</c> (to verify <c>minBottomSideBearing</c>) are not
/// checked here because that value requires per-glyph outline extents, which the
/// vertical analyzer doesn't reach for.
/// </para>
/// </remarks>
public class VmtxTableAnalyzer : IFontAnalyzer
{
    // ─────────────────────── Descriptors ───────────────────────

    public static readonly DiagnosticDescriptor AllAdvanceHeightsZero = new(
        "OT.vmtx.all-advance-heights-zero", "Every glyph has zero advance height",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "All {0} advance heights are 0.",
        "A vertical font where no glyph advances vertically produces no visible text in vertical layout.");

    public static readonly DiagnosticDescriptor AllTopSideBearingsZero = new(
        "OT.vmtx.all-tsb-zero", "Every top side bearing is zero",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "All {0} top side bearings are 0 while at least one advance height is non-zero.",
        "A font with non-zero advances and uniformly zero TSBs usually indicates that the metrics were generated without consulting the glyph outlines.");

    public static readonly DiagnosticDescriptor UniformAdvanceHeights = new(
        "OT.vmtx.uniform-advances", "All glyphs share one advance height",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "All {0} glyphs share the advance height {1}.",
        "A font where every glyph has the same vertical advance is expected for certain CJK designs and unusual otherwise.");

    public static readonly DiagnosticDescriptor SingleLongMetric = new(
        "OT.vmtx.single-long-metric", "Only one long vertical metric record",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "numberOfVMetrics is 1; all {0} glyphs reuse the first advance height.",
        "The compressed form of vmtx. Correct and efficient when all glyphs share one advance height.");

    public static readonly DiagnosticDescriptor FullyExpandedMetrics = new(
        "OT.vmtx.fully-expanded", "Every glyph has an explicit metric record",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "numberOfVMetrics equals numGlyphs ({0}); no glyph reuses a trailing advance height.",
        "The uncompressed form of vmtx. Correct but larger than necessary when many trailing glyphs share the same advance height.");

    public static readonly DiagnosticDescriptor AdvanceHeightMaxMismatch = new(
        "OT.vmtx.advance-height-max-vs-vhea", "vhea.advanceHeightMax disagrees with vmtx",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "vhea.advanceHeightMax is {0} but the largest advance height in vmtx is {1}.",
        "The spec requires vhea.advanceHeightMax to equal the maximum advance height across all glyphs. A mismatch means one of the two tables is stale.");

    public static readonly DiagnosticDescriptor MinTopSideBearingMismatch = new(
        "OT.vmtx.min-tsb-vs-vhea", "vhea.minTopSideBearing disagrees with vmtx",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "vhea.minTopSideBearing is {0} but the smallest top side bearing in vmtx is {1}.",
        "The spec requires vhea.minTopSideBearing to equal the minimum top side bearing across all glyphs. A mismatch means one of the two tables is stale.");

    // ─────────────────────── Singleton ───────────────────────

    private VmtxTableAnalyzer() { }

    public static VmtxTableAnalyzer Instance => field ??= new();

    // ─────────────────────── Analysis ───────────────────────

    /// <summary>Runs every vmtx rule against the face's <c>vmtx</c> table.</summary>
    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var vmtx = face.GetTable<VmtxTable>();
        var tag = VmtxTable.Tag;

        AnalyzeAdvanceHeights(vmtx, tag, bag);
        AnalyzeTopSideBearings(vmtx, tag, bag);
        AnalyzeMetricCount(vmtx, tag, bag);

        // Cross-table against vhea. VMTX is the dependent; VHEA holds the source of truth.
        if (face.Directory.ContainsKey(VheaTable.Tag))
            AnalyzeAgainstVhea(face.GetTable<VheaTable>(), vmtx, tag, bag);
    }

    // ─────────────────────── Rule groups ───────────────────────

    private static void AnalyzeAdvanceHeights(VmtxTable vmtx, Tag tag, DiagnosticBag bag)
    {
        var heights = vmtx.AdvanceHeights;
        if (heights.Count == 0) return;

        bool allZero = true;
        bool allEqual = true;
        ushort firstValue = heights[0];

        for (int i = 0; i < heights.Count; i++)
        {
            ushort v = heights[i];
            if (v != 0) allZero = false;
            if (v != firstValue) allEqual = false;
        }

        if (allZero)
        {
            bag.Add(AllAdvanceHeightsZero.Create(
                [heights.Count],
                table: tag, field: nameof(VmtxTable.AdvanceHeights),
                span: new SourceSpan(0, heights.Count * 4)));
            return;   // uniform-advances would also fire on all-zero; skip.
        }

        if (allEqual)
            bag.Add(UniformAdvanceHeights.Create(
                [vmtx.NumGlyphs, firstValue],
                table: tag, field: nameof(VmtxTable.AdvanceHeights),
                span: new SourceSpan(0, heights.Count * 4)));
    }

    private static void AnalyzeTopSideBearings(VmtxTable vmtx, Tag tag, DiagnosticBag bag)
    {
        var tsbs = vmtx.TopSideBearings;
        if (tsbs.Count == 0) return;

        bool allZero = true;
        for (int i = 0; i < tsbs.Count; i++)
        {
            if (tsbs[i] != 0) { allZero = false; break; }
        }

        if (!allZero) return;

        // Skip when all advances are also zero — the all-advance-heights rule already
        // covers that case and both diagnostics on one font would be noise.
        if (AreAllAdvanceHeightsZero(vmtx.AdvanceHeights)) return;

        bag.Add(AllTopSideBearingsZero.Create(
            [tsbs.Count],
            table: tag, field: nameof(VmtxTable.TopSideBearings),
            span: new SourceSpan(vmtx.NumberOfVMetrics * 4L, 2)));
    }

    private static void AnalyzeMetricCount(VmtxTable vmtx, Tag tag, DiagnosticBag bag)
    {
        if (vmtx.NumberOfVMetrics == 1 && vmtx.NumGlyphs > 1)
        {
            bag.Add(SingleLongMetric.Create(
                [vmtx.NumGlyphs],
                table: tag, field: nameof(VmtxTable.NumberOfVMetrics)));
            return;
        }

        if (vmtx.NumberOfVMetrics == vmtx.NumGlyphs && vmtx.NumGlyphs > 1)
        {
            bag.Add(FullyExpandedMetrics.Create(
                [vmtx.NumGlyphs],
                table: tag, field: nameof(VmtxTable.NumberOfVMetrics)));
        }
    }

    private static void AnalyzeAgainstVhea(
        VheaTable vhea, VmtxTable vmtx, Tag tag, DiagnosticBag bag)
    {
        // The two maxima/minima comparisons require the full per-glyph metric arrays.
        // vmtx.AdvanceHeights only covers the first NumberOfVMetrics glyphs; trailing
        // glyphs reuse the last advance height, so the effective maximum is over the
        // array plus that reused value.
        ushort actualMaxAdvance = MaxEffectiveAdvanceHeight(vmtx);
        if (vhea.AdvanceHeightMax != actualMaxAdvance)
        {
            bag.Add(AdvanceHeightMaxMismatch.Create(
                [vhea.AdvanceHeightMax, actualMaxAdvance],
                table: tag, field: nameof(VmtxTable.AdvanceHeights)));
        }

        // Top side bearings are a full array, one per glyph. Min is over the whole array.
        short actualMinTsb = MinTopSideBearing(vmtx.TopSideBearings);
        if (vhea.MinTopSideBearing != actualMinTsb)
        {
            bag.Add(MinTopSideBearingMismatch.Create(
                [vhea.MinTopSideBearing, actualMinTsb],
                table: tag, field: nameof(VmtxTable.TopSideBearings)));
        }
    }

    // ─────────────────────── Helpers ───────────────────────

    private static bool AreAllAdvanceHeightsZero(IReadOnlyList<ushort> heights)
    {
        for (int i = 0; i < heights.Count; i++)
            if (heights[i] != 0) return false;
        return true;
    }

    private static ushort MaxEffectiveAdvanceHeight(VmtxTable vmtx)
    {
        var heights = vmtx.AdvanceHeights;
        if (heights.Count == 0) return 0;

        ushort max = 0;
        for (int i = 0; i < heights.Count; i++)
            if (heights[i] > max) max = heights[i];

        // Glyphs past NumberOfVMetrics reuse the last advance height. If that last
        // value is larger than anything in the array, it's the effective maximum.
        if (vmtx.NumGlyphs > vmtx.NumberOfVMetrics)
        {
            ushort reused = heights[heights.Count - 1];
            if (reused > max) max = reused;
        }

        return max;
    }

    private static short MinTopSideBearing(IReadOnlyList<short> tsbs)
    {
        if (tsbs.Count == 0) return 0;
        short min = tsbs[0];
        for (int i = 1; i < tsbs.Count; i++)
            if (tsbs[i] < min) min = tsbs[i];
        return min;
    }
}
