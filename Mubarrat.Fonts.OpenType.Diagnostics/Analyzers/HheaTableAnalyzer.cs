using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>hhea</c> table.</summary>
/// <remarks>
/// <para>
/// <c>hhea</c> declares the horizontal line-layout metrics and, via
/// <see cref="HheaTable.NumberOfHMetrics"/>, the number of long records in <c>hmtx</c>.
/// Cross-table rules that compare the metric count against <c>maxp.numGlyphs</c> live
/// in <c>CrossTableAnalyzer</c>.
/// </para>
/// <para>
/// The ascent, descent, and line gap values are Apple-specific. Windows layout uses the
/// <c>OS/2</c> table instead. Rules here check internal consistency, not the
/// relationship to <c>OS/2</c>; those live in <c>CrossTableAnalyzer</c>.
/// </para>
/// </remarks>
public class HheaTableAnalyzer : IFontAnalyzer
{
    // ─────────────────────── Descriptors ───────────────────────

    public static readonly DiagnosticDescriptor VersionInvalid = new(
        "OT.hhea.version", "Invalid hhea version",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "version is 0x{0:X8}, expected 0x00010000.",
        "Every published OpenType revision specifies hhea.version 1.0. Other values indicate a malformed or unsupported table.");

    public static readonly DiagnosticDescriptor NumberOfHMetricsZero = new(
        "OT.hhea.number-of-h-metrics-zero", "hhea declares zero h-metrics",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "numberOfHMetrics is 0.",
        "Every font must declare at least one horizontal metric record. Without one, no glyph can be laid out.");

    public static readonly DiagnosticDescriptor MetricDataFormatInvalid = new(
        "OT.hhea.metric-data-format", "Invalid metricDataFormat",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "metricDataFormat is {0}, expected 0.",
        "The specification defines only format 0. Other values indicate a malformed table.");

    public static readonly DiagnosticDescriptor CaretSlopeBothZero = new(
        "OT.hhea.caret-slope-both-zero", "Caret slope undefined",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "caretSlopeRise and caretSlopeRun are both 0; the caret angle is undefined.",
        "The slope is rise/run. Both zero produces a 0/0 division. Either value must be non-zero.");

    public static readonly DiagnosticDescriptor CaretSlopeRiseZero = new(
        "OT.hhea.caret-slope-rise-zero", "Caret slope is infinite",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "caretSlopeRise is 0 but caretSlopeRun is {0}; the caret slope is infinite.",
        "A horizontal rise with non-zero run produces a vertical caret in every direction, which is not a valid caret angle.");

    public static readonly DiagnosticDescriptor ReservedFieldsNonZero = new(
        "OT.hhea.reserved-fields", "Reserved hhea fields are non-zero",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Reserved field at offset +{0} is 0x{1:X4}, expected 0.",
        "The four reserved shorts at offsets +24 through +31 must be zero. Non-zero values usually indicate a generator bug or a misread layout.");

    public static readonly DiagnosticDescriptor AscenderBelowDescender = new(
        "OT.hhea.ascender-below-descender", "Ascender below descender",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "ascender is {0}, descender is {1}; the ascender should be greater than the descender.",
        "By convention the ascender is positive and the descender negative. An ascender below the descender inverts the vertical extent and produces negative line heights.");

    public static readonly DiagnosticDescriptor AdvanceWidthMaxZero = new(
        "OT.hhea.advance-width-max-zero", "advanceWidthMax is zero",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "advanceWidthMax is 0.",
        "Every glyph in the font has zero advance width. This is legal only for a font with no spacing glyphs, which is unusual.");

    public static readonly DiagnosticDescriptor LineGapNegative = new(
        "OT.hhea.line-gap-negative", "Negative line gap",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "lineGap is {0}; Windows treats negative values as 0.",
        "Negative line gaps are permitted but not portable. Windows 3.1, System 6, and System 7 clamp negative values to zero, so the effective line gap differs by platform.");

    public static readonly DiagnosticDescriptor MetricsUnpopulated = new(
        "OT.hhea.metrics-unpopulated", "Horizontal extent metrics are all zero",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Consistency,
        "minLeftSideBearing, minRightSideBearing, and xMaxExtent are all 0 while advanceWidthMax is {0}.",
        "All three extent fields being zero while a non-zero advance exists suggests the fields were not computed. Real glyphs almost always produce a non-zero extent on at least one side.");

    public static readonly DiagnosticDescriptor ItalicCaretDeclared = new(
        "OT.hhea.italic-caret", "Font declares a slanted caret",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "caretSlopeRise is {0} and caretSlopeRun is {1}; the font is italic.",
        "A non-zero caret slope run declares an italic font. Purely informational; the field is correct for a slanted design.");

    // ─────────────────────── Singleton ───────────────────────

    private HheaTableAnalyzer() { }

    public static HheaTableAnalyzer Instance => field ??= new();

    // ─────────────────────── Analysis ───────────────────────

    /// <summary>Runs every hhea rule against the face's <c>hhea</c> table.</summary>
    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var hhea = face.GetTable<HheaTable>();
        var tag = HheaTable.Tag;

        AnalyzeVersionAndFormat(hhea, tag, bag);
        AnalyzeMetricCount(hhea, tag, bag);
        AnalyzeCaret(hhea, tag, bag);
        AnalyzeVerticalMetrics(hhea, tag, bag);
        AnalyzeExtents(hhea, tag, bag);
    }

    // ─────────────────────── Rule groups ───────────────────────

    private static void AnalyzeVersionAndFormat(HheaTable hhea, Tag tag, DiagnosticBag bag)
    {
        if (hhea.Version != 0x00010000)
            bag.Add(VersionInvalid.Create(
                [hhea.Version],
                table: tag, field: nameof(HheaTable.Version),
                span: new SourceSpan(0, 4)));

        if (hhea.MetricDataFormat != 0)
            bag.Add(MetricDataFormatInvalid.Create(
                [hhea.MetricDataFormat],
                table: tag, field: nameof(HheaTable.MetricDataFormat),
                span: new SourceSpan(32, 2)));
    }

    private static void AnalyzeMetricCount(HheaTable hhea, Tag tag, DiagnosticBag bag)
    {
        if (hhea.NumberOfHMetrics == 0)
            bag.Add(NumberOfHMetricsZero.Create(
                [],
                table: tag, field: nameof(HheaTable.NumberOfHMetrics),
                span: new SourceSpan(34, 2)));
    }

    private static void AnalyzeCaret(HheaTable hhea, Tag tag, DiagnosticBag bag)
    {
        short rise = hhea.CaretSlopeRise;
        short run = hhea.CaretSlopeRun;

        if (rise == 0 && run == 0)
        {
            bag.Add(CaretSlopeBothZero.Create(
                [],
                table: tag, field: "caretSlope",
                span: new SourceSpan(18, 4)));
        }
        else if (rise == 0)
        {
            bag.Add(CaretSlopeRiseZero.Create(
                [run],
                table: tag, field: nameof(HheaTable.CaretSlopeRise),
                span: new SourceSpan(18, 2)));
        }
        else if (run != 0)
        {
            bag.Add(ItalicCaretDeclared.Create(
                [rise, run],
                table: tag, field: "caretSlope",
                span: new SourceSpan(18, 4)));
        }
    }

    private static void AnalyzeVerticalMetrics(HheaTable hhea, Tag tag, DiagnosticBag bag)
    {
        if (hhea.Ascender < hhea.Descender)
            bag.Add(AscenderBelowDescender.Create(
                [hhea.Ascender, hhea.Descender],
                table: tag, field: "ascender/descender",
                span: new SourceSpan(4, 4)));

        if (hhea.LineGap < 0)
            bag.Add(LineGapNegative.Create(
                [hhea.LineGap],
                table: tag, field: nameof(HheaTable.LineGap),
                span: new SourceSpan(8, 2)));
    }

    private static void AnalyzeExtents(HheaTable hhea, Tag tag, DiagnosticBag bag)
    {
        if (hhea.AdvanceWidthMax == 0)
        {
            bag.Add(AdvanceWidthMaxZero.Create(
                [],
                table: tag, field: nameof(HheaTable.AdvanceWidthMax),
                span: new SourceSpan(10, 2)));
        }
        else if (hhea.MinLeftSideBearing == 0 &&
                 hhea.MinRightSideBearing == 0 &&
                 hhea.XMaxExtent == 0)
        {
            bag.Add(MetricsUnpopulated.Create(
                [hhea.AdvanceWidthMax],
                table: tag, field: "extentMetrics",
                span: new SourceSpan(12, 6)));
        }
    }
}
