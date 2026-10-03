using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables;
using Mubarrat.Fonts.OpenType.Tables.Metadata;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>LTSH</c> table.</summary>
/// <remarks>
/// The parser rejects the wrong version and enforces the <c>numGlyphs</c> cross-check
/// against <c>maxp</c>. What remains for analysis is the shape of the per-glyph yPixels
/// array and the <c>head.flags</c> requirement that the table only appear when bit 4 is
/// set.
/// </remarks>
public class LtshTableAnalyzer : IFontAnalyzer
{
    public static readonly DiagnosticDescriptor TablePresentWithoutNonlinearFlag = new(
        "OT.ltsh.missing-nonlinear-flag", "LTSH present but head.flags does not indicate nonlinear scaling",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "head.flags bit 4 is clear, but the font includes an LTSH table.",
        "The spec says LTSH should not be present unless head.flags bit 4 is set. Consumers that ignore the flag may not consult LTSH.");

    public static readonly DiagnosticDescriptor ThresholdZero = new(
        "OT.ltsh.threshold-zero", "Glyph has a linear threshold of 0",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Glyph {0} has yPixels 0.",
        "A threshold of 0 has no meaning; 1 is the minimum value and indicates that the glyph scales linearly at every size.");

    public static readonly DiagnosticDescriptor ThresholdBelowSmallestRecPpem = new(
        "OT.ltsh.threshold-below-lowest-rec-ppem", "Linear threshold is below the font's lowestRecPPEM",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "Glyph {0} declares yPixels {1}, below head.lowestRecPPEM {2}.",
        "A threshold below the smallest supported render size means the glyph is treated as linear at every size the font claims to support.");

    public static readonly DiagnosticDescriptor AllGylphsLinear = new(
        "OT.ltsh.all-linear", "Every glyph declares a linear threshold of 1",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "All {0} glyphs declare yPixels 1.",
        "An LTSH table where every glyph scales linearly at all sizes provides no information. Consumers that ignore LTSH behave identically.");

    public static readonly DiagnosticDescriptor SomeGylphsNonLinear = new(
        "OT.ltsh.some-nonlinear", "Some glyphs declare a non-linear scaling threshold",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "{0} of {1} glyphs declare a yPixels greater than 1.",
        "The table carries meaningful caching information for hinting-aware consumers. Fonts with hinting that adjusts advance widths use non-1 thresholds.");

    private LtshTableAnalyzer() { }

    public static LtshTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var ltsh = face.GetTable<LtshTable>();
        var tag = LtshTable.Tag;

        AnalyzeHeader(face, ltsh, tag, bag);
        AnalyzeThresholds(face, ltsh, tag, bag);
    }

    private static void AnalyzeHeader(FontFace face, LtshTable ltsh, Tag tag, DiagnosticBag bag)
    {
        if (!face.Directory.Contains(HeadTable.Tag)) return;

        var head = face.GetTable<HeadTable>();
        bool nonlinear = (head.Flags & HeadFlags.InstructionsAlterAdvanceWidth) != 0;
        if (!nonlinear)
        {
            bag.Add(TablePresentWithoutNonlinearFlag.Create(
                [], table: tag));
        }
    }

    private static void AnalyzeThresholds(FontFace face, LtshTable ltsh, Tag tag, DiagnosticBag bag)
    {
        var yPixels = ltsh.YPixels;
        if (yPixels.Count == 0) return;

        int lowestRecPPEM = 0;
        if (face.Directory.Contains(HeadTable.Tag))
        {
            lowestRecPPEM = face.GetTable<HeadTable>().LowestRecPPEM;
        }

        int nonLinear = 0;
        for (int gid = 0; gid < yPixels.Count; gid++)
        {
            byte v = yPixels[gid];

            if (v == 0)
            {
                bag.Add(ThresholdZero.Create(
                    [gid],
                    table: tag, field: $"yPixels[{gid}]"));
                continue;
            }

            if (v > 1)
            {
                nonLinear++;

                if (lowestRecPPEM > 0 && v < lowestRecPPEM)
                {
                    bag.Add(ThresholdBelowSmallestRecPpem.Create(
                        [gid, v, lowestRecPPEM],
                        table: tag, field: $"yPixels[{gid}]"));
                }
            }
        }

        if (nonLinear == 0)
        {
            bag.Add(AllGylphsLinear.Create(
                [yPixels.Count],
                table: tag, field: nameof(LtshTable.YPixels)));
        }
        else
        {
            bag.Add(SomeGylphsNonLinear.Create(
                [nonLinear, yPixels.Count],
                table: tag, field: nameof(LtshTable.YPixels)));
        }
    }
}
