using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>maxp</c> table.</summary>
/// <remarks>
/// <para>
/// <c>maxp</c> is version-gated: version 0.5 carries only <see cref="MaxpTable.NumGlyphs"/>,
/// version 1.0 carries thirteen additional ushorts describing TrueType outline and
/// hinting limits. Every TrueType-only rule guards on <see cref="MaxpTable.HasTrueTypeFields"/>
/// so a CFF font produces no spurious diagnostics.
/// </para>
/// <para>
/// Rules that compare <c>maxp</c> against other tables (loca count, hmtx count, presence
/// of a <c>glyf</c> table, hinting program maxima) live in <c>CrossTableAnalyzer</c>.
/// </para>
/// </remarks>
public class MaxpTableAnalyzer : IFontAnalyzer
{
    // ─────────────────────── Descriptors ───────────────────────

    public static readonly DiagnosticDescriptor NumGlyphsZero = new(
        "OT.maxp.num-glyphs-zero", "maxp declares zero glyphs",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "numGlyphs is 0.",
        "A font must contain at least .notdef, so numGlyphs must be at least 1. Every table indexed by glyph ID (loca, hmtx, cmap) depends on this count.");

    public static readonly DiagnosticDescriptor NumGlyphsOne = new(
        "OT.maxp.num-glyphs-one", "maxp declares only .notdef",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "numGlyphs is 1; the font contains only .notdef.",
        "A font with a single glyph cannot render any character. This usually indicates a stub or a build error.");

    public static readonly DiagnosticDescriptor MaxZonesInvalid = new(
        "OT.maxp.max-zones-invalid", "Invalid maxZones",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "maxZones is {0}, expected 1 or 2.",
        "maxZones declares whether the twilight zone (Z0) is used. Values outside 1 and 2 are undefined and cause hinting interpreters to fail.");

    public static readonly DiagnosticDescriptor TwilightPointsWithoutZone = new(
        "OT.maxp.twilight-points-without-zone", "Twilight points declared without twilight zone",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "maxTwilightPoints is {0} but maxZones is 1.",
        "When maxZones is 1 only the glyph zone exists. Declaring twilight points requires maxZones to be 2.");

    public static readonly DiagnosticDescriptor ComponentDepthZero = new(
        "OT.maxp.component-depth-zero", "Invalid maxComponentDepth",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "maxComponentDepth is 0; must be at least 1.",
        "The minimum value is 1, which describes a font whose composite glyphs reference only simple glyphs.");

    public static readonly DiagnosticDescriptor NoOutlineMaxima = new(
        "OT.maxp.no-outline-maxima", "No outline maxima declared",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "maxPoints, maxContours, maxCompositePoints, and maxCompositeContours are all zero.",
        "A version 1.0 maxp table declares the largest outline it contains. All four values being zero means every glyph in the font is empty, which is valid only for a stub.");

    public static readonly DiagnosticDescriptor MaxZonesOne = new(
        "OT.maxp.max-zones-one", "No twilight zone declared",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "maxZones is 1; the font does not use the twilight zone.",
        "The specification recommends maxZones = 2 to make the font work with all TrueType rasterizers. A value of 1 is permitted but restricts the font to glyph-zone-only hinting.");

    public static readonly DiagnosticDescriptor NoHinting = new(
        "OT.maxp.no-hinting", "Font declares no hinting",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "All hinting-related maxima are zero (twilight points, storage, function defs, instruction defs, stack, instructions).",
        "The font contains no TrueType instructions. This is legal and increasingly common, but font renderers that rely on hinting will grid-fit without guidance.");

    // ─────────────────────── Singleton ───────────────────────

    private MaxpTableAnalyzer() { }

    public static MaxpTableAnalyzer Instance => field ??= new();

    // ─────────────────────── Analysis ───────────────────────

    /// <summary>Runs every maxp rule against the face's <c>maxp</c> table.</summary>
    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var maxp = face.GetTable<MaxpTable>();
        var tag = MaxpTable.Tag;

        AnalyzeGlyphCount(maxp, tag, bag);

        // TrueType-only rules. A version 0.5 (CFF) table carries none of these fields.
        if (!maxp.HasTrueTypeFields) return;

        AnalyzeZones(maxp, tag, bag);
        AnalyzeComponentDepth(maxp, tag, bag);
        AnalyzeOutlineMaxima(maxp, tag, bag);
        AnalyzeHinting(maxp, tag, bag);
    }

    // ─────────────────────── Rule groups ───────────────────────

    private static void AnalyzeGlyphCount(MaxpTable maxp, Tag tag, DiagnosticBag bag)
    {
        // Offset 4: numGlyphs follows the 4-byte version in both versions.
        var span = new SourceSpan(4, 2);

        if (maxp.NumGlyphs == 0)
        {
            bag.Add(NumGlyphsZero.Create(
                [],
                table: tag, field: nameof(MaxpTable.NumGlyphs), span: span));
            return;
        }

        if (maxp.NumGlyphs == 1)
            bag.Add(NumGlyphsOne.Create(
                [],
                table: tag, field: nameof(MaxpTable.NumGlyphs), span: span));
    }

    private static void AnalyzeZones(MaxpTable maxp, Tag tag, DiagnosticBag bag)
    {
        ushort zones = maxp.MaxZones ?? 0;

        if (zones is not (1 or 2))
        {
            bag.Add(MaxZonesInvalid.Create(
                [zones],
                table: tag, field: nameof(MaxpTable.MaxZones),
                span: new SourceSpan(14, 2)));
        }

        // Twilight points only exist when the twilight zone exists. Report as an error
        // rather than a warning: an interpreter that sees this pair will either fail or
        // use uninitialized zone memory.
        if (zones == 1 && (maxp.MaxTwilightPoints ?? 0) > 0)
        {
            bag.Add(TwilightPointsWithoutZone.Create(
                [maxp.MaxTwilightPoints ?? 0],
                table: tag, field: nameof(MaxpTable.MaxTwilightPoints),
                span: new SourceSpan(16, 2)));
        }

        if (zones == 1)
        {
            bag.Add(MaxZonesOne.Create(
                [],
                table: tag, field: nameof(MaxpTable.MaxZones),
                span: new SourceSpan(14, 2)));
        }
    }

    private static void AnalyzeComponentDepth(MaxpTable maxp, Tag tag, DiagnosticBag bag)
    {
        if (maxp.MaxComponentDepth == 0)
        {
            bag.Add(ComponentDepthZero.Create(
                [],
                table: tag, field: nameof(MaxpTable.MaxComponentDepth),
                span: new SourceSpan(30, 2)));
        }
    }

    private static void AnalyzeOutlineMaxima(MaxpTable maxp, Tag tag, DiagnosticBag bag)
    {
        ushort maxPoints = maxp.MaxPoints ?? 0;
        ushort maxContours = maxp.MaxContours ?? 0;
        ushort maxCompositePoints = maxp.MaxCompositePoints ?? 0;
        ushort maxCompositeContours = maxp.MaxCompositeContours ?? 0;

        bool allZero = maxPoints == 0
                    && maxContours == 0
                    && maxCompositePoints == 0
                    && maxCompositeContours == 0;

        if (allZero)
        {
            bag.Add(NoOutlineMaxima.Create(
                [],
                table: tag, field: "outlineMaxima",
                span: new SourceSpan(6, 8)));
        }
    }

    private static void AnalyzeHinting(MaxpTable maxp, Tag tag, DiagnosticBag bag)
    {
        // Every hinting-related maximum is zero: no twilight, no storage, no FDEFs,
        // no IDEFs, no stack, no instruction bytes. The font has no instructions.
        bool noHinting =
            (maxp.MaxTwilightPoints ?? 0) == 0 &&
            (maxp.MaxStorage ?? 0) == 0 &&
            (maxp.MaxFunctionDefs ?? 0) == 0 &&
            (maxp.MaxInstructionDefs ?? 0) == 0 &&
            (maxp.MaxStackElements ?? 0) == 0 &&
            (maxp.MaxSizeOfInstructions ?? 0) == 0;

        if (noHinting)
        {
            bag.Add(NoHinting.Create(
                [],
                table: tag, field: "hintingMaxima",
                span: new SourceSpan(16, 8)));
        }
    }
}
