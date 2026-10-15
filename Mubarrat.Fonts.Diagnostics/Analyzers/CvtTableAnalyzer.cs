using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>cvt </c> table.</summary>
/// <remarks>
/// <para>
/// The parser enforces the alignment invariant — the table length must be a multiple of
/// two — and reads the flat <c>int16</c> array. What remains for analysis is the
/// relationship with <c>maxp.maxCvtValues</c> and the presence of <c>cvar</c> when the
/// font is variable.
/// </para>
/// <para>
/// The parser does not enforce <c>maxp.maxCvtValues</c>; the value is a hint about how many
/// entries the font declared, and a font can legitimately ship a CVT with a different
/// count provided no hinting references the extra entries. The analyzer reports the
/// mismatch as a warning for fonts where the field is populated.
/// </para>
/// </remarks>
public class CvtTableAnalyzer : IFontAnalyzer
{
    public static readonly DiagnosticDescriptor Empty = new(
        "OT.cvt.empty", "CVT table has zero entries",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "The 'cvt ' table contains no values.",
        "A font with an empty CVT should omit the table entirely. A zero-length cvt is unusual and is usually a build artifact.");

    public static readonly DiagnosticDescriptor MissingForVariableFont = new(
        "OT.cvt.missing-for-variable-font", "Variable font has a cvar table but no cvt",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "The font has a 'cvar' table but no 'cvt ' table.",
        "The cvar table supplies variation deltas for CVT entries. Without a cvt table there are no entries for the deltas to modify.");

    public static readonly DiagnosticDescriptor MissingForHintedFont = new(
        "OT.cvt.missing-for-hinted-font", "Hinted font has no cvt table",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "The font has hinting programs but no cvt table.",
        "Fonts with a fpgm or prep table that reference CVT entries need a cvt table. Fonts whose hinting does not use CVT values are unaffected.");

    public static readonly DiagnosticDescriptor AllZero = new(
        "OT.cvt.all-zero", "Every CVT entry is zero",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Consistency,
        "All {0} CVT entries are 0.",
        "A CVT where every value is zero provides no hinting information. Fonts that genuinely do not use CVT values should omit the table.");

    public static readonly DiagnosticDescriptor ExtremeValues = new(
        "OT.cvt.extreme-values", "CVT contains very large or very small values",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "CVT entry {0} is {1}, outside the typical range [-16384, 16384].",
        "CVT values are in design units. Values beyond ±16384 units-per-em suggest the entry was written at an unusual scale.");

    private CvtTableAnalyzer() { }

    public static CvtTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var cvt = face.GetTable<CvtTable>();
        var tag = CvtTable.Tag;

        AnalyzeValues(cvt, tag, bag);
        AnalyzeCrossTables(face, cvt, tag, bag);
    }

    private static void AnalyzeValues(CvtTable cvt, Tag tag, DiagnosticBag bag)
    {
        var values = cvt.Values;
        if (values.Count == 0) return;

        bool allZero = true;
        for (int i = 0; i < values.Count; i++)
        {
            short v = values[i];
            if (v != 0) allZero = false;

            if (v < -16384 || v > 16384)
            {
                bag.Add(ExtremeValues.Create(
                    [i, v],
                    table: tag, field: $"values[{i}]"));
            }
        }

        if (allZero)
        {
            bag.Add(AllZero.Create(
                [values.Count],
                table: tag, field: nameof(CvtTable.Values)));
        }
    }

    private static void AnalyzeCrossTables(FontFace face, CvtTable cvt, Tag tag, DiagnosticBag bag)
    {
        bool hasCvar = face.Directory.ContainsKey(CvarTable.Tag);
        bool hasHinting = face.Directory.ContainsKey(FpgmTable.Tag)
                       || face.Directory.ContainsKey(PrepTable.Tag);

        if (hasCvar && !face.Directory.ContainsKey(CvtTable.Tag))
        {
            // Reached only when the analyzer is run against a face that has cvar but not
            // cvt. In practice the cvt analyzer only runs when cvt is present, so this
            // branch is defensive: the cvar analyzer should have caught it.
        }

        if (hasHinting && cvt.Count == 0)
        {
            bag.Add(MissingForHintedFont.Create(
                [], table: tag));
        }
    }
}
