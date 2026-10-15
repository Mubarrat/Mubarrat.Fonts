using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>VORG</c> table.</summary>
/// <remarks>
/// <para>
/// VORG is optional and CFF-only. It supplies vertical-origin y coordinates for glyphs
/// whose value differs from <see cref="VorgTable.DefaultVertOriginY"/>. The per-glyph
/// array must be strictly increasing by glyph ID, and every glyph index must be within
/// <c>maxp.numGlyphs</c>.
/// </para>
/// <para>
/// VORG is a dependent of <c>maxp</c> for its glyph-range check and of the outline
/// tables for its validity check: if the font has <c>glyf</c>, the spec says the
/// VORG table must be ignored, because TrueType outlines carry the bounding box
/// needed to compute vertical origin directly.
/// </para>
/// </remarks>
public class VorgTableAnalyzer : IFontAnalyzer
{
    // ─────────────────────── Descriptors ───────────────────────

    public static readonly DiagnosticDescriptor MajorVersionInvalid = new(
        "OT.vorg.major-version", "Unexpected VORG major version",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "majorVersion is {0}, expected 1.",
        "Every published revision of the VORG table specifies major version 1. Other values are undefined.");

    public static readonly DiagnosticDescriptor RecordsNotIncreasing = new(
        "OT.vorg.records-not-increasing", "VORG records are not strictly increasing",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Record at index {0} has glyphIndex {1}, which is not greater than the previous record's glyphIndex {2}.",
        "The spec requires vertOriginYMetrics sorted by strictly increasing glyph ID. Binary search in consumers depends on it.");

    public static readonly DiagnosticDescriptor GlyphIndexOutOfRange = new(
        "OT.vorg.glyph-index-vs-maxp", "VORG glyph index exceeds numGlyphs",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "Record at index {0} has glyphIndex {1} but maxp.numGlyphs is {2}.",
        "Every glyph ID referenced by VORG must correspond to a real glyph. An out-of-range index points past the glyph set.");

    public static readonly DiagnosticDescriptor PresentInTrueTypeFont = new(
        "OT.vorg.present-in-truetype", "VORG present in a TrueType font",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Compatibility,
        "The font contains a glyf table, so VORG is not used. The spec says a VORG table in a TrueType-flavored font must be ignored.",
        "TrueType outlines carry bounding boxes directly, so vertical origins are computed from glyf. The VORG table adds no information and may confuse tools that don't ignore it.");

    public static readonly DiagnosticDescriptor VertOriginYZero = new(
        "OT.vorg.default-vert-origin-y-zero", "Default vertical origin is zero",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "defaultVertOriginY is 0.",
        "The vertical origin y coordinate is typically the ascender of the font. Zero is unusual and usually indicates an unpopulated field.");

    public static readonly DiagnosticDescriptor MinorVersionNonZero = new(
        "OT.vorg.minor-version", "Unexpected VORG minor version",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Specification,
        "minorVersion is {0}, expected 0.",
        "Every published revision of the VORG table specifies minor version 0.");

    public static readonly DiagnosticDescriptor RedundantEntries = new(
        "OT.vorg.redundant-entries", "VORG entries duplicate the default origin",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "{0} of {1} vertOriginYMetrics entries equal the default vertical origin ({2}).",
        "The spec says a glyph whose vertical origin equals the default is typically omitted. Redundant entries are harmless but increase the table size and slow the binary search.");

    public static readonly DiagnosticDescriptor AllGlyphsListed = new(
        "OT.vorg.all-glyphs-listed", "VORG lists every glyph explicitly",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "vertOriginYMetrics has {0} entries, covering every glyph. No glyph uses the default.",
        "If every glyph has a distinct vertical origin, the default field provides no value. This is legal but indicates the table was generated without checking for glyphs that share the default.");

    public static readonly DiagnosticDescriptor ExcessiveEntries = new(
        "OT.vorg.excessive-entries", "VORG lists more than half of all glyphs",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "vertOriginYMetrics has {0} entries for {1} glyphs ({2:P0}).",
        "A large override list suggests that most glyphs have a vertical origin different from the default. If that is intentional, the default field may not be representative of the font.");

    // ─────────────────────── Singleton ───────────────────────

    private VorgTableAnalyzer() { }

    public static VorgTableAnalyzer Instance => field ??= new();

    // ─────────────────────── Analysis ───────────────────────

    /// <summary>Runs every VORG rule against the face's <c>VORG</c> table.</summary>
    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var vorg = face.GetTable<VorgTable>();
        var tag = VorgTable.Tag;

        // Single-table rules.
        AnalyzeVersion(vorg, tag, bag);
        AnalyzeRecords(vorg, tag, bag);
        AnalyzeDefault(vorg, tag, bag);
        AnalyzeOutlineFlavor(face, tag, bag);   // needs glyf presence

        // Cross-table rules against maxp. VORG is the dependent.
        if (face.Directory.ContainsKey(MaxpTable.Tag))
        {
            var maxp = face.GetTable<MaxpTable>();
            int numGlyphs = maxp.NumGlyphs;

            // Glyph indices must be in range.
            for (int i = 0; i < vorg.VertOriginYMetrics.Count; i++)
            {
                int glyphId = vorg.VertOriginYMetrics[i].GlyphIndex;
                if (glyphId >= numGlyphs)
                {
                    bag.Add(GlyphIndexOutOfRange.Create(
                        [i, glyphId, numGlyphs],
                        table: tag, field: $"vertOriginYMetrics[{i}]",
                        span: new SourceSpan(8 + i * 4L, 2)));
                }
            }

            // Density rules that need the glyph count.
            int entries = vorg.VertOriginYMetrics.Count;
            if (entries > 0 && numGlyphs > 0)
            {
                if (entries == numGlyphs)
                    bag.Add(AllGlyphsListed.Create(
                        [entries], table: tag, field: nameof(VorgTable.VertOriginYMetrics)));
                else if (entries > numGlyphs / 2)
                    bag.Add(ExcessiveEntries.Create(
                        [entries, numGlyphs, (double)entries / numGlyphs],
                        table: tag, field: nameof(VorgTable.VertOriginYMetrics)));
            }
        }
    }

    // ─────────────────────── Rule groups ───────────────────────

    private static void AnalyzeVersion(VorgTable vorg, Tag tag, DiagnosticBag bag)
    {
        if (vorg.MajorVersion != 1)
            bag.Add(MajorVersionInvalid.Create(
                [vorg.MajorVersion],
                table: tag, field: nameof(VorgTable.MajorVersion),
                span: new SourceSpan(0, 2)));

        if (vorg.MinorVersion != 0)
            bag.Add(MinorVersionNonZero.Create(
                [vorg.MinorVersion],
                table: tag, field: nameof(VorgTable.MinorVersion),
                span: new SourceSpan(2, 2)));
    }

    private static void AnalyzeRecords(VorgTable vorg, Tag tag, DiagnosticBag bag)
    {
        var records = vorg.VertOriginYMetrics;
        for (int i = 1; i < records.Count; i++)
        {
            int prev = records[i - 1].GlyphIndex;
            int curr = records[i].GlyphIndex;
            if (curr <= prev)
            {
                bag.Add(RecordsNotIncreasing.Create(
                    [i, curr, prev],
                    table: tag, field: $"vertOriginYMetrics[{i}]",
                    span: new SourceSpan(8 + i * 4L, 4)));
            }
        }
    }

    private static void AnalyzeDefault(VorgTable vorg, Tag tag, DiagnosticBag bag)
    {
        if (vorg.DefaultVertOriginY == 0)
            bag.Add(VertOriginYZero.Create(
                [],
                table: tag, field: nameof(VorgTable.DefaultVertOriginY),
                span: new SourceSpan(4, 2)));

        // Redundant entries: glyphs whose explicit value equals the default.
        int redundant = 0;
        for (int i = 0; i < vorg.VertOriginYMetrics.Count; i++)
            if (vorg.VertOriginYMetrics[i].VertOriginY == vorg.DefaultVertOriginY)
                redundant++;

        if (redundant > 0)
        {
            bag.Add(RedundantEntries.Create(
                [redundant, vorg.VertOriginYMetrics.Count, vorg.DefaultVertOriginY],
                table: tag, field: nameof(VorgTable.VertOriginYMetrics)));
        }
    }

    private static void AnalyzeOutlineFlavor(FontFace face, Tag tag, DiagnosticBag bag)
    {
        // VORG is CFF-only. Its presence alongside glyf means it should be ignored.
        if (!face.Directory.ContainsKey(GlyfTable.Tag)) return;

        bag.Add(PresentInTrueTypeFont.Create(
            [],
            table: tag, field: "outlineFlavor"));
    }

    // ─────────────────────── Cross-table: maxp ───────────────────────

    /// <summary>
    /// Checks every VORG glyph index against <c>maxp.numGlyphs</c>. The rule fires on
    /// <c>VORG</c> because <c>maxp.numGlyphs</c> is the authoritative glyph count.
    /// </summary>
    public static void AnalyzeAgainstMaxp(VorgTable vorg, MaxpTable maxp, DiagnosticBag bag)
    {
        int numGlyphs = maxp.NumGlyphs;

        for (int i = 0; i < vorg.VertOriginYMetrics.Count; i++)
        {
            int glyphId = vorg.VertOriginYMetrics[i].GlyphIndex;
            if (glyphId >= numGlyphs)
            {
                bag.Add(GlyphIndexOutOfRange.Create(
                    [i, glyphId, numGlyphs],
                    table: VorgTable.Tag, field: $"vertOriginYMetrics[{i}]",
                    span: new SourceSpan(8 + i * 4L, 2)));
            }
        }

        // Density rules that need the glyph count.
        int entries = vorg.VertOriginYMetrics.Count;
        if (entries == 0 || numGlyphs == 0) return;

        if (entries == numGlyphs)
        {
            bag.Add(AllGlyphsListed.Create(
                [entries],
                table: VorgTable.Tag, field: nameof(VorgTable.VertOriginYMetrics)));
        }
        else if (entries > numGlyphs / 2)
        {
            bag.Add(ExcessiveEntries.Create(
                [entries, numGlyphs, (double)entries / numGlyphs],
                table: VorgTable.Tag, field: nameof(VorgTable.VertOriginYMetrics)));
        }
    }

    // ─────────────────────── Helpers ───────────────────────

    private static bool HasCount(VorgTable vorg, out int numGlyphs)
    {
        // VORG has no glyph count of its own; the density rules need maxp, but the
        // analyzer only has the VorgTable here. The density checks therefore run in
        // AnalyzeAgainstMaxp via the count comparison. This helper is a placeholder
        // that returns false when no count is available.
        numGlyphs = 0;
        return false;
    }
}
