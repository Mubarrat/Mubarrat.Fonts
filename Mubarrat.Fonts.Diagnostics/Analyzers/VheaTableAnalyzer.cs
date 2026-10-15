using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>vhea</c> table.</summary>
/// <remarks>
/// <para>
/// <c>vhea</c> mirrors <c>hhea</c> with two differences. First, the "no slant" caret
/// convention is inverted: a vertical font uses a horizontal caret, so
/// <c>caretSlopeRise = 0</c> and <c>caretSlopeRun = 1</c> is the natural state, and a
/// non-zero rise indicates the table was copied from a horizontal font. Second, version
/// 1.1 renamed the ascender/descender fields without changing their layout, and version
/// 1.0 reserved the line gap.
/// </para>
/// <para>
/// Cross-table rules against <c>vmtx</c> fire on <c>vmtx</c>, not here: <c>vhea</c> holds
/// the summary values and <c>vmtx</c> is what those values summarize. The
/// <c>numberOfVMetrics</c> vs <c>maxp.numGlyphs</c> rule is the exception — it fires
/// here, because <c>vhea</c> is the dependent table for that comparison.
/// </para>
/// </remarks>
public class VheaTableAnalyzer : IFontAnalyzer
{
    // ─────────────────────── Constants ───────────────────────

    private const uint Version10 = 0x00010000u;
    private const uint Version11 = 0x00011000u;

    // ─────────────────────── Descriptors ───────────────────────

    public static readonly DiagnosticDescriptor VersionInvalid = new(
        "OT.vhea.version", "Invalid vhea version",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "version is 0x{0:X8}, expected 0x00010000 (v1.0) or 0x00011000 (v1.1).",
        "The two published versions differ only in the names of the ascender and descender fields. Other values are undefined.");

    public static readonly DiagnosticDescriptor NumberOfVMetricsZero = new(
        "OT.vhea.number-of-v-metrics-zero", "vhea declares zero v-metrics",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "numberOfVMetrics is 0.",
        "Every vertical font must declare at least one vertical metric record. Without one, no glyph can be laid out vertically.");

    public static readonly DiagnosticDescriptor MetricDataFormatInvalid = new(
        "OT.vhea.metric-data-format", "Invalid metricDataFormat",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "metricDataFormat is {0}, expected 0.",
        "The specification defines only format 0. Other values indicate a malformed table.");

    public static readonly DiagnosticDescriptor CaretSlopeBothZero = new(
        "OT.vhea.caret-slope-both-zero", "Caret slope undefined",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "caretSlopeRise and caretSlopeRun are both 0; the caret angle is undefined.",
        "The slope is rise/run. Both zero produces a 0/0 division. Either value must be non-zero.");

    public static readonly DiagnosticDescriptor NumberOfVMetricsExceedsGlyphs = new(
        "OT.vhea.num-v-metrics-vs-maxp", "numberOfVMetrics exceeds numGlyphs",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "numberOfVMetrics is {0} but maxp.numGlyphs is {1}.",
        "There can be no more long metric records than there are glyphs.");

    public static readonly DiagnosticDescriptor LineGapNonZeroInV10 = new(
        "OT.vhea.line-gap-v1-0", "lineGap is non-zero in version 1.0",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "version is 1.0 but lineGap is {0}.",
        "Version 1.0 reserved the line gap field and required it to be 0. Version 1.1 gave it meaning.");

    public static readonly DiagnosticDescriptor ReservedFieldsNonZero = new(
        "OT.vhea.reserved-fields", "Reserved vhea fields are non-zero",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Reserved field at offset +{0} is 0x{1:X4}, expected 0.",
        "The four reserved shorts at offsets +24 through +31 must be zero.");

    public static readonly DiagnosticDescriptor AscenderBelowDescender = new(
        "OT.vhea.ascender-below-descender", "Vertical ascender below descender",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "ascender is {0}, descender is {1}; the ascender should be greater.",
        "By convention the ascender is positive and the descender negative. An ascender below the descender inverts the vertical extent.");

    public static readonly DiagnosticDescriptor CaretSlopeSuggestsHorizontal = new(
        "OT.vhea.caret-slope-suggests-horizontal", "Caret slope looks like a horizontal font",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "caretSlopeRise is {0} and caretSlopeRun is {1}; a vertical font normally uses rise=0, run=1.",
        "For a vertical font a horizontal caret (rise=0, run=1) is the natural state. A non-zero rise suggests the vhea table was copied from the hhea table of a horizontal font.");

    public static readonly DiagnosticDescriptor HorizontalCaret = new(
        "OT.vhea.horizontal-caret", "Vertical font declares a horizontal caret",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "caretSlopeRise is 0 and caretSlopeRun is 1; the font uses a horizontal caret.",
        "Purely informational. The horizontal caret is the correct choice for a vertical font.");

    public static readonly DiagnosticDescriptor Version10Deprecated = new(
        "OT.vhea.version-1-0-deprecated", "vhea version 1.0 is superseded",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "version is 1.0; version 1.1 renamed the ascender and descender fields.",
        "Version 1.1 is the current revision. Version 1.0 tables are still valid but do not match the modern field names.");

    public static readonly DiagnosticDescriptor AdvanceHeightMaxNegative = new(
        "OT.vhea.advance-height-max-negative", "advanceHeightMax is negative",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "advanceHeightMax is {0}.",
        "The maximum advance height is a magnitude and cannot be negative.");

    // ─────────────────────── Singleton ───────────────────────

    private VheaTableAnalyzer() { }

    public static VheaTableAnalyzer Instance => field ??= new();

    // ─────────────────────── Analysis ───────────────────────

    /// <summary>Runs every vhea rule against the face's <c>vhea</c> table.</summary>
    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var vhea = face.GetTable<VheaTable>();
        var tag = VheaTable.Tag;

        AnalyzeVersion(vhea, tag, bag);
        AnalyzeMetricCount(vhea, tag, bag);
        AnalyzeCaret(vhea, tag, bag);
        AnalyzeVerticalMetrics(vhea, tag, bag);
        AnalyzeReservedFields(vhea, tag, bag);

        // Cross-table against maxp. VHEA is the dependent.
        if (face.Directory.ContainsKey(MaxpTable.Tag))
        {
            var maxp = face.GetTable<MaxpTable>();
            if (vhea.NumberOfVMetrics > maxp.NumGlyphs)
            {
                bag.Add(NumberOfVMetricsExceedsGlyphs.Create(
                    [vhea.NumberOfVMetrics, maxp.NumGlyphs],
                    table: tag, field: nameof(VheaTable.NumberOfVMetrics),
                    span: new SourceSpan(34, 2)));
            }
        }
    }

    // ─────────────────────── Rule groups ───────────────────────

    private static void AnalyzeVersion(VheaTable vhea, Tag tag, DiagnosticBag bag)
    {
        if (vhea.Version is not (Version10 or Version11))
        {
            bag.Add(VersionInvalid.Create(
                [vhea.Version],
                table: tag, field: nameof(VheaTable.Version),
                span: new SourceSpan(0, 4)));
            return;
        }

        if (vhea.Version == Version10)
        {
            bag.Add(Version10Deprecated.Create(
                [],
                table: tag, field: nameof(VheaTable.Version),
                span: new SourceSpan(0, 4)));

            // Version 1.0 reserved the line gap. A non-zero value means the font
            // was written for 1.1 but the version field wasn't updated.
            if (vhea.LineGap != 0)
            {
                bag.Add(LineGapNonZeroInV10.Create(
                    [vhea.LineGap],
                    table: tag, field: nameof(VheaTable.LineGap),
                    span: new SourceSpan(8, 2)));
            }
        }
    }

    private static void AnalyzeMetricCount(VheaTable vhea, Tag tag, DiagnosticBag bag)
    {
        if (vhea.NumberOfVMetrics == 0)
            bag.Add(NumberOfVMetricsZero.Create(
                [],
                table: tag, field: nameof(VheaTable.NumberOfVMetrics),
                span: new SourceSpan(34, 2)));

        if (vhea.MetricDataFormat != 0)
            bag.Add(MetricDataFormatInvalid.Create(
                [vhea.MetricDataFormat],
                table: tag, field: nameof(VheaTable.MetricDataFormat),
                span: new SourceSpan(32, 2)));
    }

    private static void AnalyzeCaret(VheaTable vhea, Tag tag, DiagnosticBag bag)
    {
        short rise = vhea.CaretSlopeRise;
        short run = vhea.CaretSlopeRun;

        if (rise == 0 && run == 0)
        {
            bag.Add(CaretSlopeBothZero.Create(
                [],
                table: tag, field: "caretSlope",
                span: new SourceSpan(18, 4)));
            return;
        }

        // For a vertical font the natural state is a horizontal caret: rise=0, run=1.
        if (rise == 0 && run == 1)
        {
            bag.Add(HorizontalCaret.Create(
                [],
                table: tag, field: "caretSlope",
                span: new SourceSpan(18, 4)));
            return;
        }

        // Anything else is unusual for a vertical font. A non-zero rise in particular
        // is the signature of a copy-paste from hhea.
        bag.Add(CaretSlopeSuggestsHorizontal.Create(
            [rise, run],
            table: tag, field: "caretSlope",
            span: new SourceSpan(18, 4)));
    }

    private static void AnalyzeVerticalMetrics(VheaTable vhea, Tag tag, DiagnosticBag bag)
    {
        if (vhea.Ascender < vhea.Descender)
        {
            bag.Add(AscenderBelowDescender.Create(
                [vhea.Ascender, vhea.Descender],
                table: tag, field: "ascender/descender",
                span: new SourceSpan(4, 4)));
        }

        if (vhea.AdvanceHeightMax < 0)
            bag.Add(AdvanceHeightMaxNegative.Create(
                [vhea.AdvanceHeightMax],
                table: tag, field: nameof(VheaTable.AdvanceHeightMax),
                span: new SourceSpan(10, 2)));
    }

    private static void AnalyzeReservedFields(VheaTable vhea, Tag tag, DiagnosticBag bag)
    {
        ReportReserved(bag, tag, 24, vhea.Reserved1);
        ReportReserved(bag, tag, 26, vhea.Reserved2);
        ReportReserved(bag, tag, 28, vhea.Reserved3);
        ReportReserved(bag, tag, 30, vhea.Reserved4);
    }

    private static void ReportReserved(DiagnosticBag bag, Tag tag, int offset, short value)
    {
        if (value != 0)
            bag.Add(ReservedFieldsNonZero.Create(
                [offset, (ushort)value],
                table: tag, field: $"reserved@{offset}",
                span: new SourceSpan(offset, 2)));
    }
}
