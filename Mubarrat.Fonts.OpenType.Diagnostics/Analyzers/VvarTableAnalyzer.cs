using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables.Vertical;
using Mubarrat.Fonts.OpenType.Tables.Variations;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>VVAR</c> table.</summary>
public class VvarTableAnalyzer : IFontAnalyzer
{
    public static readonly DiagnosticDescriptor MajorVersionInvalid = new(
        "OT.vvar.major-version", "Unexpected VVAR major version",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "majorVersion is {0}, expected 1.");

    public static readonly DiagnosticDescriptor MinorVersionNonZero = new(
        "OT.vvar.minor-version", "Unexpected VVAR minor version",
        DiagnosticSeverity.Information, DiagnosticCategory.Specification,
        "minorVersion is {0}, expected 0.");

    public static readonly DiagnosticDescriptor MissingVhea = new(
        "OT.vvar.missing-vhea", "VVAR present without a vhea table",
        DiagnosticSeverity.Warning, DiagnosticCategory.Consistency,
        "VVAR deltas modify vertical metrics, but the font has no vhea table.");

    public static readonly DiagnosticDescriptor MissingVmtx = new(
        "OT.vvar.missing-vmtx", "VVAR present without a vmtx table",
        DiagnosticSeverity.Warning, DiagnosticCategory.Consistency,
        "VVAR deltas modify vertical metrics, but the font has no vmtx table.");

    public static readonly DiagnosticDescriptor NoMaps = new(
        "OT.vvar.no-index-maps", "VVAR has no index maps",
        DiagnosticSeverity.Information, DiagnosticCategory.Specification,
        "None of advanceHeightMap, tsbMap, bsbMap, or vorgMap is present.",
        "Implicit indexing by glyph ID applies when no index map is provided.");

    public static readonly DiagnosticDescriptor StoreEmpty = new(
        "OT.vvar.empty-store", "VVAR item variation store is empty",
        DiagnosticSeverity.Warning, DiagnosticCategory.Consistency,
        "The item variation store has no variation data subtables.");

    private VvarTableAnalyzer() { }

    public static VvarTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var vvar = face.GetTable<VvarTable>();
        var tag = VvarTable.Tag;

        if (vvar.MajorVersion != 1)
            bag.Add(MajorVersionInvalid.Create(
                [vvar.MajorVersion], table: tag, field: nameof(VvarTable.MajorVersion),
                span: new SourceSpan(0, 2)));

        if (vvar.MinorVersion != 0)
            bag.Add(MinorVersionNonZero.Create(
                [vvar.MinorVersion], table: tag, field: nameof(VvarTable.MinorVersion),
                span: new SourceSpan(2, 2)));

        if (!face.Directory.Contains(VheaTable.Tag))
            bag.Add(MissingVhea.Create([], table: tag));
        if (!face.Directory.Contains(VmtxTable.Tag))
            bag.Add(MissingVmtx.Create([], table: tag));

        if (vvar.AdvanceHeightMap is null
            && vvar.TopSideBearingMap is null
            && vvar.BottomSideBearingMap is null
            && vvar.VerticalOriginMap is null)
        {
            bag.Add(NoMaps.Create([], table: tag));
        }

        if (vvar.Store.SubtableCount == 0)
            bag.Add(StoreEmpty.Create([], table: tag, field: nameof(VvarTable.Store)));
    }
}
