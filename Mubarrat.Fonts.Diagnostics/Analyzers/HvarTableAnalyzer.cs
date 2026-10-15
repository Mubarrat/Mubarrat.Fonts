using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>HVAR</c> table.</summary>
public class HvarTableAnalyzer : IFontAnalyzer
{
    public static readonly DiagnosticDescriptor MajorVersionInvalid = new(
        "OT.hvar.major-version", "Unexpected HVAR major version",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "majorVersion is {0}, expected 1.");

    public static readonly DiagnosticDescriptor MinorVersionNonZero = new(
        "OT.hvar.minor-version", "Unexpected HVAR minor version",
        DiagnosticSeverity.Information, DiagnosticCategory.Specification,
        "minorVersion is {0}, expected 0.");

    public static readonly DiagnosticDescriptor NoMaps = new(
        "OT.hvar.no-index-maps", "HVAR has no index maps",
        DiagnosticSeverity.Information, DiagnosticCategory.Specification,
        "None of advanceWidthMap, lsbMap, or rsbMap is present.",
        "Implicit indexing by glyph ID applies when no index map is provided. This is the normal configuration.");

    public static readonly DiagnosticDescriptor StoreEmpty = new(
        "OT.hvar.empty-store", "HVAR item variation store is empty",
        DiagnosticSeverity.Warning, DiagnosticCategory.Consistency,
        "The item variation store has no variation data subtables.",
        "Without at least one subtable no deltas can be resolved. The table has no effect.");

    public static readonly DiagnosticDescriptor RegionListEmpty = new(
        "OT.hvar.empty-region-list", "HVAR variation region list is empty",
        DiagnosticSeverity.Warning, DiagnosticCategory.Consistency,
        "The variation region list declares zero regions.");

    public static readonly DiagnosticDescriptor MapTooShort = new(
        "OT.hvar.index-map-too-short", "HVAR index map covers fewer items than needed",
        DiagnosticSeverity.Information, DiagnosticCategory.Consistency,
        "The {0} index map has {1} entries; items past the end map to (0, 0).",
        "Implicit zero mapping applies past the end of a DeltaSetIndexMap.");

    private HvarTableAnalyzer() { }

    public static HvarTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var hvar = face.GetTable<HvarTable>();
        var tag = HvarTable.Tag;

        if (hvar.MajorVersion != 1)
            bag.Add(MajorVersionInvalid.Create(
                [hvar.MajorVersion], table: tag, field: nameof(HvarTable.MajorVersion),
                span: new SourceSpan(0, 2)));

        if (hvar.MinorVersion != 0)
            bag.Add(MinorVersionNonZero.Create(
                [hvar.MinorVersion], table: tag, field: nameof(HvarTable.MinorVersion),
                span: new SourceSpan(2, 2)));

        if (hvar.AdvanceWidthMap is null
            && hvar.LeftSideBearingMap is null
            && hvar.RightSideBearingMap is null)
        {
            bag.Add(NoMaps.Create([], table: tag));
        }

        if (hvar.Store.SubtableCount == 0)
            bag.Add(StoreEmpty.Create([], table: tag, field: nameof(HvarTable.Store)));

        if (hvar.Store.VariationRegionList.RegionCount == 0)
            bag.Add(RegionListEmpty.Create([], table: tag, field: nameof(HvarTable.Store)));
    }
}
