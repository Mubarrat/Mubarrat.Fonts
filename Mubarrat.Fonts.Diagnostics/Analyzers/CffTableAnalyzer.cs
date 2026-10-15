using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>CFF </c> table.</summary>
/// <remarks>
/// <para>
/// CFF is a self-contained container: the charset, encoding, Top DICT, Private DICT,
/// subroutines, and charstrings all live inside the single table. The parser rejects the
/// most fundamental structural errors at read time — wrong header version, wrong
/// charstring type, non-monotonic INDEX offsets, over/underflow in the interpreter. What
/// remains for analysis is cross-references between the CFF structures themselves and
/// between CFF and the rest of the font.
/// </para>
/// <para>
/// The PostScript name (CFF Name INDEX) must match the name table's PostScript name
/// (name ID 6). The glyph count must match <c>maxp</c>. Charset SIDs must resolve through
/// the standard strings or the String INDEX. CID-keyed fonts must supply the FDArray and
/// FDSelect structures, and every FDSelect index must address a valid Font DICT.
/// </para>
/// </remarks>
public class CffTableAnalyzer : IFontAnalyzer
{
    /// <summary>The number of standard strings defined by the CFF specification.</summary>
    private const int StandardStringCount = CffStandardStrings.Count;

    /// <summary>The four-operand arity expected for the FontBBox operator.</summary>
    private const int FontBBoxArityConst = 4;

    // ─────────────────────── Descriptors ───────────────────────

    public static readonly DiagnosticDescriptor GlyphCountMismatch = new(
        "OT.cff.glyph-count-vs-maxp", "CharStrings count disagrees with maxp",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "The CFF CharStrings INDEX has {0} entries but maxp.numGlyphs is {1}.",
        "Every glyph must have one charstring. A mismatch means the CFF glyph inventory and the font's declared glyph count disagree.");

    public static readonly DiagnosticDescriptor MissingPrivateDict = new(
        "OT.cff.missing-private-dict", "Non-CID font has no Private DICT",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "The font is not CID-keyed but declares no Private DICT.",
        "A non-CID CFF font must have a Private DICT; the CharstringType 2 interpreter reads DefaultWidthX and NominalWidthX from it, and hinting parameters live there.");

    public static readonly DiagnosticDescriptor MissingFdArray = new(
        "OT.cff.missing-fdarray", "CID font has no FDArray",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "The font is CID-keyed but declares no FDArray.",
        "A CID-keyed CFF font must declare an FDArray INDEX containing at least one Font DICT.");

    public static readonly DiagnosticDescriptor MissingFdSelect = new(
        "OT.cff.missing-fdselect", "CID font with multiple Font DICTs has no FDSelect",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "The font is CID-keyed and declares {0} Font DICTs but no FDSelect.",
        "When a CID-keyed font declares more than one Font DICT, every glyph must be assigned to one through the FDSelect table.");

    public static readonly DiagnosticDescriptor FdSelectIndexOutOfRange = new(
        "OT.cff.fdselect-out-of-range", "FDSelect references a non-existent Font DICT",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "FDSelect assigns glyph {0} to Font DICT {1}, but the FDArray has only {2} entries.",
        "Every FDSelect index must address a Font DICT in the FDArray.");

    public static readonly DiagnosticDescriptor CharsetSidOutOfRange = new(
        "OT.cff.charset-sid-out-of-range", "Charset references an undefined SID",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "Glyph {0} has charset SID {1}, but only {2} strings are defined.",
        "A SID must resolve either to a standard string (0–390) or to an entry in the String INDEX (391 and up). SIDs beyond that range have no name.");

    public static readonly DiagnosticDescriptor FontBBoxArity = new(
        "OT.cff.bbox-arity", "FontBBox has the wrong number of operands",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "FontBBox has {0} operands, expected {1}.",
        "The FontBBox operator takes four operands: xMin, yMin, xMax, yMax.");

    public static readonly DiagnosticDescriptor FontBBoxInverted = new(
        "OT.cff.bbox-inverted", "FontBBox is inverted",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "FontBBox is inverted: ({0}, {1}) – ({2}, {3}).",
        "A bounding box where xMin > xMax or yMin > yMax is a common symptom of a font that was edited without recomputing metrics.");

    public static readonly DiagnosticDescriptor CharsetNotSpecified = new(
        "OT.cff.charset-missing", "Top DICT does not specify a charset",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "The Top DICT has no charset operator.",
        "The CFF specification requires the charset operator. An absent charset leaves the glyph-to-SID mapping undefined.");

    public static readonly DiagnosticDescriptor EncodingPresent = new(
        "OT.cff.encoding-present", "Top DICT declares an encoding",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "The Top DICT declares a CFF encoding (offset {0}).",
        "OpenType CFF fonts resolve characters through the cmap table, not through the CFF encoding. A declared encoding is legal but unused by conforming consumers.");

    public static readonly DiagnosticDescriptor CidKeyedFont = new(
        "OT.cff.cid-keyed", "Font is CID-keyed",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Specification,
        "The font is CID-keyed with {0} Font DICT(s).",
        "CID-keyed fonts organize glyphs by character identifier rather than by name. Both forms are legal in OpenType.");

    public static readonly DiagnosticDescriptor AllGlyphsEmpty = new(
        "OT.cff.all-glyphs-empty", "Every glyph has no contours",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Consistency,
        "All {0} glyphs have zero contours.",
        "A font where no glyph draws an outline produces no visible text. This is legal only for a font used as a metrics-only source.");

    public static readonly DiagnosticDescriptor PostScriptNameMismatch = new(
        "OT.cff.postscript-name-vs-name", "CFF name disagrees with the name table",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "The CFF Name INDEX is '{0}' but the name table's PostScript name (ID 6) is '{1}'.",
        "The two names should agree. Some print pipelines and font caches use the CFF name as the canonical identifier and the name table for display, so a mismatch causes them to disagree about which font they are operating on.");

    public static readonly DiagnosticDescriptor SubrCountHigh = new(
        "OT.cff.subr-count-high", "Subroutine count crosses a bias threshold",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "The {0} subroutines have {1} entries, which crosses the CFF bias threshold at {2}.",
        "The CFF bias changes at 1240 and 33900 entries. Crossing a threshold shifts every subroutine index in every charstring.");

    // ─────────────────────── Singleton ───────────────────────

    private CffTableAnalyzer() { }

    public static CffTableAnalyzer Instance => field ??= new();

    // ─────────────────────── Analysis ───────────────────────

    /// <summary>Runs every CFF rule against the face's <c>CFF </c> table.</summary>
    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var cff = face.GetTable<CffTable>();
        var tag = CffTable.Tag;

        AnalyzeGlyphCount(face, cff, tag, bag);
        AnalyzePrivateDict(cff, tag, bag);
        AnalyzeCidStructures(cff, tag, bag);
        AnalyzeCharset(cff, tag, bag);
        AnalyzeFontBBox(cff, tag, bag);
        AnalyzeEncoding(cff, tag, bag);
        AnalyzeName(face, cff, tag, bag);
        AnalyzeSubrCounts(cff, tag, bag);
        AnalyzeGlyphOutlines(cff, tag, bag);
    }

    // ─────────────────────── Rule groups ───────────────────────

    private static void AnalyzeGlyphCount(FontFace face, CffTable cff, Tag tag, DiagnosticBag bag)
    {
        if (!face.Directory.ContainsKey(MaxpTable.Tag)) return;

        var maxp = face.GetTable<MaxpTable>();
        if (cff.GlyphCount != maxp.NumGlyphs)
        {
            bag.Add(GlyphCountMismatch.Create(
                [cff.GlyphCount, maxp.NumGlyphs],
                table: tag, field: nameof(CffTable.CharStrings)));
        }
    }

    private static void AnalyzePrivateDict(CffTable cff, Tag tag, DiagnosticBag bag)
    {
        if (!cff.IsCidFont && cff.PrivateDict is null)
        {
            bag.Add(MissingPrivateDict.Create(
                [], table: tag, field: nameof(CffTable.PrivateDict)));
        }
    }

    private static void AnalyzeCidStructures(CffTable cff, Tag tag, DiagnosticBag bag)
    {
        if (!cff.IsCidFont) return;

        if (cff.FontDicts is null || cff.FontDicts.Count == 0)
        {
            bag.Add(MissingFdArray.Create([], table: tag, field: nameof(CffTable.FontDicts)));
            return;
        }

        if (cff.FontDicts.Count > 1 && cff.FdSelect is null)
        {
            bag.Add(MissingFdSelect.Create(
                [cff.FontDicts.Count], table: tag, field: nameof(CffTable.FdSelect)));
            return;
        }

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

        bag.Add(CidKeyedFont.Create(
            [fontDictCount], table: tag));
    }

    private static void AnalyzeCharset(CffTable cff, Tag tag, DiagnosticBag bag)
    {
        int maxSid = StandardStringCount + cff.Strings.Count;
        ushort[] sids = cff.Charset.Sids;

        for (int gid = 1; gid < sids.Length; gid++)
        {
            if (sids[gid] >= maxSid)
            {
                bag.Add(CharsetSidOutOfRange.Create(
                    [gid, sids[gid], maxSid],
                    table: tag, field: $"charset.sids[{gid}]"));
                break;    // one diagnostic is enough; the charset is malformed
            }
        }

        // The charset operator is required by the spec but the parser defaults to
        // ISOAdobe when absent. Distinguish "0 in the DICT" from "no operator at all".
        if (!cff.TopDict.Raw.Contains(CffTopDict.Ops.Charset))
        {
            bag.Add(CharsetNotSpecified.Create(
                [], table: tag, field: nameof(CffTable.Charset)));
        }
    }

    private static void AnalyzeFontBBox(CffTable cff, Tag tag, DiagnosticBag bag)
    {
        double[] bbox = cff.TopDict.FontBBox;
        if (bbox.Length == 0) return;    // operator absent; the default is not required

        if (bbox.Length != FontBBoxArityConst)
        {
            bag.Add(FontBBoxArity.Create(
                [bbox.Length, FontBBoxArityConst],
                table: tag, field: "topDict.fontBBox"));
            return;
        }

        double xMin = bbox[0], yMin = bbox[1], xMax = bbox[2], yMax = bbox[3];
        if (xMin > xMax || yMin > yMax)
        {
            bag.Add(FontBBoxInverted.Create(
                [xMin, yMin, xMax, yMax],
                table: tag, field: "topDict.fontBBox"));
        }
    }

    private static void AnalyzeEncoding(CffTable cff, Tag tag, DiagnosticBag bag)
    {
        if (cff.Encoding is null) return;
        bag.Add(EncodingPresent.Create(
            [cff.TopDict.EncodingOffset],
            table: tag, field: nameof(CffTable.Encoding)));
    }

    private static void AnalyzeName(FontFace face, CffTable cff, Tag tag, DiagnosticBag bag)
    {
        if (string.IsNullOrEmpty(cff.Name)) return;
        if (!face.Directory.ContainsKey(NameTable.Tag)) return;

        var name = face.GetTable<NameTable>();
        string? psName = name.GetString(NameId.PostScriptName);
        if (psName is null) return;

        if (!string.Equals(cff.Name, psName, StringComparison.Ordinal))
        {
            bag.Add(PostScriptNameMismatch.Create(
                [cff.Name, psName],
                table: tag, field: nameof(CffTable.Name)));
        }
    }

    private static void AnalyzeSubrCounts(CffTable cff, Tag tag, DiagnosticBag bag)
    {
        ReportSubrBias(cff.GlobalSubrs.Count, "global", tag, bag);
        for (int i = 0; i < cff.LocalSubrs.Count; i++)
            ReportSubrBias(cff.LocalSubrs[i].Count, $"local[{i}]", tag, bag);
    }

    private static void ReportSubrBias(int count, string which, Tag tag, DiagnosticBag bag)
    {
        // The bias changes at 1240 and 33900. Crossing either threshold shifts every
        // subroutine index in every charstring that calls into that set.
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

    private static void AnalyzeGlyphOutlines(CffTable cff, Tag tag, DiagnosticBag bag)
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
                table: tag, field: nameof(CffTable.Glyphs)));
        }
    }
}
