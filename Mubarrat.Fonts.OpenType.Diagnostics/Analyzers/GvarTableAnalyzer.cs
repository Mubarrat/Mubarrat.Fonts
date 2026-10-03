using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables;
using Mubarrat.Fonts.OpenType.Tables.Variations;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>gvar</c> table.</summary>
public class GvarTableAnalyzer : IFontAnalyzer
{
    public static readonly DiagnosticDescriptor MajorVersionInvalid = new(
        "OT.gvar.major-version", "Unexpected gvar major version",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "majorVersion is {0}, expected 1.");

    public static readonly DiagnosticDescriptor MinorVersionNonZero = new(
        "OT.gvar.minor-version", "Unexpected gvar minor version",
        DiagnosticSeverity.Information, DiagnosticCategory.Specification,
        "minorVersion is {0}, expected 0.");

    public static readonly DiagnosticDescriptor AxisCountMismatch = new(
        "OT.gvar.axis-count-vs-fvar", "gvar axisCount disagrees with fvar",
        DiagnosticSeverity.Error, DiagnosticCategory.Consistency,
        "gvar.axisCount is {0} but fvar.axisCount is {1}.");

    public static readonly DiagnosticDescriptor GlyphCountMismatch = new(
        "OT.gvar.glyph-count-vs-maxp", "gvar glyphCount disagrees with maxp",
        DiagnosticSeverity.Error, DiagnosticCategory.Consistency,
        "gvar.glyphCount is {0} but maxp.numGlyphs is {1}.");

    public static readonly DiagnosticDescriptor NoVariations = new(
        "OT.gvar.no-variations", "No glyph has variation data",
        DiagnosticSeverity.Information, DiagnosticCategory.Performance,
        "Every glyph variation data block is empty.",
        "The gvar table has no effect. This is legal when variations flow only through HVAR, MVAR, and CFF2.");

    public static readonly DiagnosticDescriptor UnreferencedSharedTuple = new(
        "OT.gvar.unreferenced-shared-tuple", "Shared tuple is never referenced",
        DiagnosticSeverity.Information, DiagnosticCategory.Performance,
        "{0} of {1} shared tuples are not referenced by any glyph's tuple headers.",
        "Unreferenced shared tuples add to the table size without providing data.");

    private GvarTableAnalyzer() { }

    public static GvarTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var gvar = face.GetTable<GvarTable>();
        var tag = GvarTable.Tag;

        if (gvar.MajorVersion != 1)
            bag.Add(MajorVersionInvalid.Create(
                [gvar.MajorVersion], table: tag, field: nameof(GvarTable.MajorVersion),
                span: new SourceSpan(0, 2)));

        if (gvar.MinorVersion != 0)
            bag.Add(MinorVersionNonZero.Create(
                [gvar.MinorVersion], table: tag, field: nameof(GvarTable.MinorVersion),
                span: new SourceSpan(2, 2)));

        if (face.Directory.Contains(FvarTable.Tag))
        {
            var fvar = face.GetTable<FvarTable>();
            if (gvar.AxisCount != fvar.AxisCount)
                bag.Add(AxisCountMismatch.Create(
                    [gvar.AxisCount, fvar.AxisCount],
                    table: tag, field: nameof(GvarTable.AxisCount),
                    span: new SourceSpan(4, 2)));
        }

        if (face.Directory.Contains(MaxpTable.Tag))
        {
            var maxp = face.GetTable<MaxpTable>();
            if (gvar.GlyphCount != maxp.NumGlyphs)
                bag.Add(GlyphCountMismatch.Create(
                    [gvar.GlyphCount, maxp.NumGlyphs],
                    table: tag, field: nameof(GvarTable.GlyphCount)));
        }

        if (gvar.GlyphVariationData.Length == 0)
        {
            bag.Add(NoVariations.Create([], table: tag));
            return;
        }

        bool anyVariations = false;
        for (int i = 0; i < gvar.GlyphVariationData.Length; i++)
        {
            if (gvar.GlyphVariationData[i] is { Length: > 0 })
            {
                anyVariations = true;
                break;
            }
        }
        if (!anyVariations)
            bag.Add(NoVariations.Create([], table: tag));
    }
}
