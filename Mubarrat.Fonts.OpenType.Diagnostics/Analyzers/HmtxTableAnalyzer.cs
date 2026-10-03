using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>hmtx</c> table.</summary>
/// <remarks>
/// <para>
/// <c>hmtx</c> is different from the other metric tables in that the two hard spec
/// constraints — <c>numberOfHMetrics ≥ 1</c> and <c>numberOfHMetrics ≤ numGlyphs</c> —
/// are enforced by <see cref="HmtxTable.Parse"/> itself, because both values come from
/// other tables (<c>hhea</c> and <c>maxp</c>) that must be read before the first
/// <c>hmtx</c> byte can be interpreted. A violated constraint therefore throws before
/// an <see cref="HmtxTable"/> instance exists, and no analyzer can observe it.
/// </para>
/// <para>
/// What remains for analysis is the shape of the resulting metric arrays: whether the
/// font declares zero spacing, whether every metric is identical (monospace), whether
/// the LSBs are all zero (unpopulated), and so on. These are patterns, not
/// spec violations, and all carry information or warning severity.
/// </para>
/// <para>
/// Rules that compare <c>hmtx</c> against other tables — <c>hhea.advanceWidthMax</c> vs
/// the maximum advance, <c>hhea.minLeftSideBearing</c> vs the minimum LSB, and so on —
/// live in <c>CrossTableAnalyzer</c>.
/// </para>
/// </remarks>
public class HmtxTableAnalyzer : IFontAnalyzer
{
    // ─────────────────────── Descriptors ───────────────────────

    public static readonly DiagnosticDescriptor AllAdvanceWidthsZero = new(
        "OT.hmtx.all-advance-widths-zero", "Every glyph has zero advance width",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "All {0} advance widths are 0.",
        "A font where no glyph advances produces no visible text when laid out. This is legal only for a font used exclusively as a glyph source rather than for rendering.");

    public static readonly DiagnosticDescriptor AllLeftSideBearingsZero = new(
        "OT.hmtx.all-lsb-zero", "Every left side bearing is zero",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "All {0} left side bearings are 0 while at least one advance width is non-zero.",
        "A font with non-zero advances and uniformly zero LSBs usually indicates that the metrics were generated without consulting the glyph outlines.");

    public static readonly DiagnosticDescriptor UniformAdvanceWidths = new(
        "OT.hmtx.uniform-advances", "All glyphs share one advance width",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "All {0} glyphs share the advance width {1}.",
        "A font where every glyph has the same advance is monospaced. This is legal and intentional for monospace designs, but unexpected in a proportional font.");

    public static readonly DiagnosticDescriptor SingleLongMetric = new(
        "OT.hmtx.single-long-metric", "Only one long metric record",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "numberOfHMetrics is 1; all {0} glyphs reuse the first advance width.",
        "The compressed form of hmtx. Correct and efficient for monospace fonts; unusual in a proportional font where the advance varies across glyphs.");

    public static readonly DiagnosticDescriptor NotdefZeroAdvance = new(
        "OT.hmtx.notdef-zero-advance", ".notdef has zero advance width",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "Glyph 0 (.notdef) has advance width 0.",
        "Most fonts give .notdef a non-zero advance so a missing glyph occupies space and is visible. A zero advance can cause a missing character to collapse to nothing in some renderers.");

    public static readonly DiagnosticDescriptor FullyExpandedMetrics = new(
        "OT.hmtx.fully-expanded", "Every glyph has an explicit metric record",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "numberOfHMetrics equals numGlyphs ({0}); no glyph reuses a trailing advance width.",
        "The uncompressed form of hmtx. Correct but larger than necessary when many trailing glyphs share the same advance. Some font generators always emit this form.");

    public static readonly DiagnosticDescriptor CountMismatch = new(
    "OT.hmtx.count-vs-maxp", "hmtx record count disagrees with maxp.numGlyphs",
    DiagnosticSeverity.Error,
    DiagnosticCategory.Consistency,
    "hmtx covers {0} glyphs but maxp.numGlyphs is {1}.",
    "Every glyph must have an entry in hmtx. A shorter table leaves trailing glyphs without metrics; a longer table has unused records.");

    // ─────────────────────── Singleton ───────────────────────

    private HmtxTableAnalyzer() { }

    public static HmtxTableAnalyzer Instance => field ??= new();

    // ─────────────────────── Analysis ───────────────────────

    /// <summary>Runs every hmtx rule against the face's <c>hmtx</c> table.</summary>
    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var hmtx = face.GetTable<HmtxTable>();
        var tag = HmtxTable.Tag;

        AnalyzeAdvanceWidths(hmtx, tag, bag);
        AnalyzeSideBearings(hmtx, tag, bag);
        AnalyzeMetricCount(hmtx, tag, bag);
        AnalyzeCrossTable(face, hmtx, tag, bag);
    }

    // ─────────────────────── Rule groups ───────────────────────

    private static void AnalyzeAdvanceWidths(HmtxTable hmtx, Tag tag, DiagnosticBag bag)
    {
        var advances = hmtx.AdvanceWidths;
        if (advances.Count == 0) return;

        // All zero — no glyph advances.
        bool allZero = true;
        ushort firstNonZero = 0;
        bool allEqual = true;
        ushort firstValue = advances[0];

        for (int i = 0; i < advances.Count; i++)
        {
            ushort v = advances[i];
            if (v != 0)
            {
                allZero = false;
                if (firstNonZero == 0) firstNonZero = v;
            }
            if (v != firstValue) allEqual = false;
        }

        if (allZero)
        {
            bag.Add(AllAdvanceWidthsZero.Create(
                [advances.Count],
                table: tag, field: nameof(HmtxTable.AdvanceWidths),
                span: new SourceSpan(0, advances.Count * 4)));
            return;
        }

        if (allEqual)
        {
            bag.Add(UniformAdvanceWidths.Create(
                [hmtx.NumGlyphs, firstValue],
                table: tag, field: nameof(HmtxTable.AdvanceWidths),
                span: new SourceSpan(0, advances.Count * 4)));
        }

        // .notdef is glyph 0 and its metric is always the first long metric.
        if (hmtx.NumGlyphs > 0 && advances[0] == 0)
        {
            bag.Add(NotdefZeroAdvance.Create(
                [],
                table: tag, field: "advanceWidths[0]",
                span: new SourceSpan(0, 2)));
        }
    }

    private static void AnalyzeSideBearings(HmtxTable hmtx, Tag tag, DiagnosticBag bag)
    {
        var lsbs = hmtx.LeftSideBearings;
        if (lsbs.Count == 0) return;

        // All zero — likely unpopulated. Skip when all advances are also zero; the
        // all-advance-widths rule already covers that case and the two together would
        // produce two diagnostics for one broken font.
        bool allZero = true;
        for (int i = 0; i < lsbs.Count; i++)
        {
            if (lsbs[i] != 0) { allZero = false; break; }
        }

        if (!allZero) return;
        if (hmtx.AdvanceWidths.Count > 0 && AllAdvancesZero(hmtx.AdvanceWidths)) return;

        bag.Add(AllLeftSideBearingsZero.Create(
            [lsbs.Count],
            table: tag, field: nameof(HmtxTable.LeftSideBearings),
            span: new SourceSpan(2, 2)));
    }

    private static void AnalyzeMetricCount(HmtxTable hmtx, Tag tag, DiagnosticBag bag)
    {
        if (hmtx.NumberOfHMetrics == 1 && hmtx.NumGlyphs > 1)
        {
            bag.Add(SingleLongMetric.Create(
                [hmtx.NumGlyphs],
                table: tag, field: nameof(HmtxTable.NumberOfHMetrics)));
            return;
        }

        if (hmtx.NumberOfHMetrics == hmtx.NumGlyphs && hmtx.NumGlyphs > 1)
        {
            bag.Add(FullyExpandedMetrics.Create(
                [hmtx.NumGlyphs],
                table: tag, field: nameof(HmtxTable.NumberOfHMetrics)));
        }
    }

    private static void AnalyzeCrossTable(FontFace face, HmtxTable hmtx, Tag tag, DiagnosticBag bag)
    {
        if (!face.Directory.Contains(MaxpTable.Tag)) return;

        var maxp = face.GetTable<MaxpTable>();
        if (maxp.NumGlyphs != hmtx.NumGlyphs)
        {
            bag.Add(CountMismatch.Create(
                [hmtx.NumGlyphs, maxp.NumGlyphs],
                table: tag, field: nameof(HmtxTable.NumGlyphs)));
        }
    }

    private static bool AllAdvancesZero(IReadOnlyList<ushort> advances)
    {
        for (int i = 0; i < advances.Count; i++)
            if (advances[i] != 0) return false;
        return true;
    }
}
