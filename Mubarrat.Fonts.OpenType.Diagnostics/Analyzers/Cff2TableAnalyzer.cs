using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>CFF2</c> table.</summary>
/// <remarks>
/// <para>
/// CFF2 shares the CFF container model but differs in three ways the analyzer must account
/// for. First, it is always CID-keyed: there is no non-CID variant, so the FDArray is
/// always present and the Private DICT lives inside a Font DICT rather than at the top
/// level. Second, glyph names, character mappings, and metrics all come from other tables,
/// so there is no charset, encoding, or width to validate. Third, the <c>blend</c> operator
/// requires the ItemVariationStore, so the presence of variation data is a hard dependency
/// for any charstring that references it.
/// </para>
/// <para>
/// The parser already rejects a wrong header version, a missing CharStrings or FDArray
/// offset, an empty FontDICTINDEX, and a multi-Font-DICT font without a FontDICTSelect.
/// What remains is cross-table consistency — glyph count against <c>maxp</c>, the FontMatrix
/// against the CFF conventions — and structural integrity of the FDSelect and variation
/// references.
/// </para>
/// </remarks>
public class Cff2TableAnalyzer : IFontAnalyzer
{
    /// <summary>The number of operands the FontMatrix operator must have.</summary>
    private const int FontMatrixArityConst = 6;

    /// <summary>The number of FontMatrix entries that must be non-zero for the transform to be invertible.</summary>
    private const int InvertibleDeterminantThreshold = 0;

    // ─────────────────────── Descriptors ───────────────────────

    public static readonly DiagnosticDescriptor GlyphCountMismatch = new(
        "OT.cff2.glyph-count-vs-maxp", "CharStrings count disagrees with maxp",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "The CFF2 CharStrings INDEX has {0} entries but maxp.numGlyphs is {1}.",
        "Every glyph must have one charstring. A mismatch means the CFF2 glyph inventory and the font's declared glyph count disagree.");

    public static readonly DiagnosticDescriptor FdSelectIndexOutOfRange = new(
        "OT.cff2.fdselect-out-of-range", "FontDICTSelect references a non-existent Font DICT",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "FontDICTSelect assigns glyph {0} to Font DICT {1}, but the FDArray has only {2} entries.",
        "Every FontDICTSelect index must address a Font DICT in the FDArray.");

    public static readonly DiagnosticDescriptor MissingVariationStore = new(
        "OT.cff2.missing-variation-store", "Variable font has no ItemVariationStore",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "The font declares variations in fvar but the CFF2 Top DICT has no vstore offset.",
        "A CFF2 variable font must supply the ItemVariationStore that its blend operators reference.");

    public static readonly DiagnosticDescriptor UnexpectedVariationStore = new(
        "OT.cff2.unexpected-variation-store", "Static font has an ItemVariationStore",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "The CFF2 Top DICT declares a vstore but fvar has no axes.",
        "An ItemVariationStore without axes is inert. Fonts that declare one usually were meant to be variable.");

    public static readonly DiagnosticDescriptor FontMatrixArity = new(
        "OT.cff2.font-matrix-arity", "FontMatrix has the wrong number of operands",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "FontMatrix has {0} operands, expected {1}.",
        "The FontMatrix operator takes six operands forming a 2×3 affine transform.");

    public static readonly DiagnosticDescriptor FontMatrixNotInvertible = new(
        "OT.cff2.font-matrix-not-invertible", "FontMatrix is singular",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "FontMatrix determinant is {0}; the matrix is not invertible.",
        "The FontMatrix must be invertible. A zero determinant (ad - bc == 0) means the glyph outlines cannot be transformed back to design units.");

    public static readonly DiagnosticDescriptor NegativeFontScale = new(
        "OT.cff2.negative-font-scale", "FontMatrix has a negative scale",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "FontMatrix a ({0}) or d ({1}) is negative.",
        "A negative scale flips the glyph outlines on one or both axes. This is legal but unusual and often a sign that a transform was applied in the wrong order.");

    public static readonly DiagnosticDescriptor SubrCountHigh = new(
        "OT.cff2.subr-count-high", "Subroutine count crosses a bias threshold",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "The {0} subroutines have {1} entries, which crosses the CFF2 bias threshold at {2}.",
        "The CFF2 subroutine bias changes at 1240 and 33900 entries. Crossing a threshold shifts every subroutine index in every charstring.");

    public static readonly DiagnosticDescriptor AllGlyphsEmpty = new(
        "OT.cff2.all-glyphs-empty", "Every glyph has no contours",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Consistency,
        "All {0} glyphs have zero contours.",
        "A font where no glyph draws an outline produces no visible text. This is legal only for a font used as a metrics-only source.");

    public static readonly DiagnosticDescriptor SingleFontDictNoFdSelect = new(
        "OT.cff2.single-font-dict-no-fdselect", "Single-Font-DICT font omits FontDICTSelect",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Specification,
        "The font declares one Font DICT and no FontDICTSelect.",
        "A font with a single Font DICT may omit the FontDICTSelect entirely; every glyph is implicitly assigned to Font DICT 0.");

    public static readonly DiagnosticDescriptor CidFontNameConflict = new(
        "OT.cff2.cid-font-name", "CID font declares a FontName",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "The CID-keyed font declares a FontName SID in the Top DICT.",
        "CFF2 removed the FontName operator from the Top DICT. OpenType derives the PostScript name from the name table.");

    public static readonly DiagnosticDescriptor LocalSubrsWithoutPrivate = new(
        "OT.cff2.local-subrs-without-private", "Font DICT declares local subroutines but no Private DICT",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Structure,
        "Font DICT {0} has {1} local subroutines but no Private DICT.",
        "The Local Subr INDEX offset lives in the Private DICT. A Font DICT cannot carry subroutines without one.");

    public static readonly DiagnosticDescriptor VsIndexOutOfRange = new(
        "OT.cff2.vsindex-out-of-range", "Font DICT vsindex exceeds the variation data count",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "Font DICT {0} declares vsindex {1}, but the ItemVariationStore has {2} subtables.",
        "The vsindex selects an ItemVariationData subtable by position. It must be within the store's subtable range.");

    // ─────────────────────── Singleton ───────────────────────

    private Cff2TableAnalyzer() { }

    public static Cff2TableAnalyzer Instance => field ??= new();

    // ─────────────────────── Analysis ───────────────────────

    /// <summary>Runs every CFF2 rule against the face's <c>CFF2</c> table.</summary>
    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var cff = face.GetTable<Cff2Table>();
        var tag = Cff2Table.Tag;

        AnalyzeGlyphCount(face, cff, tag, bag);
        AnalyzeTopDict(cff, tag, bag);
        AnalyzeFontDicts(cff, tag, bag);
        AnalyzeFdSelect(cff, tag, bag);
        AnalyzeVariations(face, cff, tag, bag);
        AnalyzeSubrCounts(cff, tag, bag);
        AnalyzeGlyphOutlines(cff, tag, bag);
    }

    // ─────────────────────── Rule groups ───────────────────────

    private static void AnalyzeGlyphCount(FontFace face, Cff2Table cff, Tag tag, DiagnosticBag bag)
    {
        if (!face.Directory.ContainsKey(MaxpTable.Tag)) return;

        var maxp = face.GetTable<MaxpTable>();
        if (cff.GlyphCount != maxp.NumGlyphs)
        {
            bag.Add(GlyphCountMismatch.Create(
                [cff.GlyphCount, maxp.NumGlyphs],
                table: tag, field: nameof(Cff2Table.CharStrings)));
        }
    }

    private static void AnalyzeTopDict(Cff2Table cff, Tag tag, DiagnosticBag bag)
    {
        Cff2TopDict topDict = cff.TopDict;

        // ── FontMatrix ──
        double[] matrix = topDict.FontMatrix;
        if (matrix.Length > 0)
        {
            if (matrix.Length != FontMatrixArityConst)
            {
                bag.Add(FontMatrixArity.Create(
                    [matrix.Length, FontMatrixArityConst],
                    table: tag, field: "topDict.fontMatrix"));
            }
            else
            {
                double a = matrix[0], b = matrix[1];
                double c = matrix[2], d = matrix[3];
                // tx and ty (matrix[4], matrix[5]) do not affect invertibility.

                double determinant = a * d - b * c;
                if (determinant == InvertibleDeterminantThreshold)
                {
                    bag.Add(FontMatrixNotInvertible.Create(
                        [determinant],
                        table: tag, field: "topDict.fontMatrix"));
                }

                if (a < 0 || d < 0)
                {
                    bag.Add(NegativeFontScale.Create(
                        [a, d],
                        table: tag, field: "topDict.fontMatrix"));
                }
            }
        }

        // ── FontName in a CID-keyed font ──
        // CFF2 fonts are always CID-keyed. The Top DICT should not carry FontName.
        if (topDict.Raw.Contains(0x0C26))
        {
            bag.Add(CidFontNameConflict.Create(
                [], table: tag, field: "topDict.fontName"));
        }

        // ── Single Font DICT without FDSelect ──
        if (!topDict.HasFdSelect && cff.FontDicts.Count == 1)
        {
            bag.Add(SingleFontDictNoFdSelect.Create(
                [], table: tag, field: nameof(Cff2TopDict.FDSelectOffset)));
        }
    }

    private static void AnalyzeFontDicts(Cff2Table cff, Tag tag, DiagnosticBag bag)
    {
        for (int i = 0; i < cff.FontDicts.Count; i++)
        {
            Cff2FontDict fd = cff.FontDicts[i];

            // Local subroutines should not exist without a Private DICT to anchor them.
            if (fd.LocalSubrs.Count > 0 && fd.PrivateDict is null)
            {
                bag.Add(LocalSubrsWithoutPrivate.Create(
                    [i, fd.LocalSubrs.Count],
                    table: tag, field: $"fontDicts[{i}].localSubrs"));
            }

            // vsindex range is checked against the store below in AnalyzeVariations.
        }
    }

    private static void AnalyzeFdSelect(Cff2Table cff, Tag tag, DiagnosticBag bag)
    {
        if (cff.FdSelect is null) return;

        int fontDictCount = cff.FontDicts.Count;
        byte[] indices = cff.FdSelect.FdIndices;

        for (int gid = 0; gid < indices.Length; gid++)
        {
            if (indices[gid] >= fontDictCount)
            {
                bag.Add(FdSelectIndexOutOfRange.Create(
                    [gid, indices[gid], fontDictCount],
                    table: tag, field: $"fdSelect.fdIndices[{gid}]"));
                break;    // one diagnostic is enough; the whole table is unusable
            }
        }
    }

    private static void AnalyzeVariations(FontFace face, Cff2Table cff, Tag tag, DiagnosticBag bag)
    {
        bool fontIsVariable = false;
        if (face.Directory.ContainsKey(FvarTable.Tag))
        {
            var fvar = face.GetTable<FvarTable>();
            fontIsVariable = fvar.AxisCount > 0;
        }

        // ── Presence consistency ──
        if (fontIsVariable && cff.VariationStore is null)
        {
            bag.Add(MissingVariationStore.Create(
                [], table: tag, field: nameof(Cff2Table.VariationStore)));
        }
        else if (!fontIsVariable && cff.VariationStore is not null)
        {
            bag.Add(UnexpectedVariationStore.Create(
                [], table: tag, field: nameof(Cff2Table.VariationStore)));
        }

        // ── vsindex range per Font DICT ──
        if (cff.VariationStore is { } store)
        {
            int subtableCount = store.ItemVariationData.Count;
            for (int i = 0; i < cff.FontDicts.Count; i++)
            {
                int? vsIndex = cff.FontDicts[i].PrivateDict?.VsIndex;
                if (vsIndex is { } v && (uint)v >= (uint)subtableCount)
                {
                    bag.Add(VsIndexOutOfRange.Create(
                        [i, v, subtableCount],
                        table: tag, field: $"fontDicts[{i}].privateDict.vsIndex"));
                }
            }
        }
    }

    private static void AnalyzeSubrCounts(Cff2Table cff, Tag tag, DiagnosticBag bag)
    {
        ReportSubrBias(cff.GlobalSubrs.Count, "global", tag, bag);
        for (int i = 0; i < cff.FontDicts.Count; i++)
            ReportSubrBias(cff.FontDicts[i].LocalSubrs.Count, $"fontDicts[{i}].localSubrs", tag, bag);
    }

    private static void ReportSubrBias(int count, string which, Tag tag, DiagnosticBag bag)
    {
        int threshold = count switch
        {
            >= 33900 => 33900,
            >= 1240 => 1240,
            _ => 0,
        };
        if (threshold == 0) return;

        bag.Add(SubrCountHigh.Create(
            [which, count, threshold],
            table: tag, field: "subrs"));
    }

    private static void AnalyzeGlyphOutlines(Cff2Table cff, Tag tag, DiagnosticBag bag)
    {
        if (cff.Glyphs.Count == 0) return;

        bool anyContours = false;
        for (int i = 0; i < cff.Glyphs.Count; i++)
        {
            if (cff.Glyphs[i].ContourCount > 0)
            {
                anyContours = true;
                break;
            }
        }

        if (!anyContours)
        {
            bag.Add(AllGlyphsEmpty.Create(
                [cff.Glyphs.Count],
                table: tag, field: nameof(Cff2Table.Glyphs)));
        }
    }
}
