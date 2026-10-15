using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>cvar</c> table.</summary>
public class CvarTableAnalyzer : IFontAnalyzer
{
    public static readonly DiagnosticDescriptor MajorVersionInvalid = new(
        "OT.cvar.major-version", "Unexpected cvar major version",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "majorVersion is {0}, expected 1.");

    public static readonly DiagnosticDescriptor MinorVersionNonZero = new(
        "OT.cvar.minor-version", "Unexpected cvar minor version",
        DiagnosticSeverity.Information, DiagnosticCategory.Specification,
        "minorVersion is {0}, expected 0.");

    public static readonly DiagnosticDescriptor EmptyStore = new(
        "OT.cvar.empty-store", "cvar contains no variation tuples",
        DiagnosticSeverity.Information, DiagnosticCategory.Performance,
        "The tuple variation store declares {0} tuples.",
        "A cvar table with no tuples has no effect and could be omitted.");

    public static readonly DiagnosticDescriptor CvtTableMissing = new(
        "OT.cvar.cvt-missing", "cvar present without a cvt table",
        DiagnosticSeverity.Warning, DiagnosticCategory.Consistency,
        "The font has a cvar table but no cvt table.",
        "cvar deltas modify control value table entries. Without a cvt table there are no entries to modify.");

    public static readonly DiagnosticDescriptor ReservedBitsSet = new(
        "OT.cvar.reserved-flags", "Reserved tuple variation store bits set",
        DiagnosticSeverity.Warning, DiagnosticCategory.Specification,
        "The tuple variation store count field has reserved bits 0x{0:X4} set (raw value 0x{1:X4}).",
        "Bits 12–14 of the tupleVariationCount field are reserved and must be zero.");

    private CvarTableAnalyzer() { }

    public static CvarTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var cvar = face.GetTable<CvarTable>();
        var tag = CvarTable.Tag;

        if (cvar.MajorVersion != 1)
            bag.Add(MajorVersionInvalid.Create(
                [cvar.MajorVersion], table: tag, field: nameof(CvarTable.MajorVersion),
                span: new SourceSpan(0, 2)));

        if (cvar.MinorVersion != 0)
            bag.Add(MinorVersionNonZero.Create(
                [cvar.MinorVersion], table: tag, field: nameof(CvarTable.MinorVersion),
                span: new SourceSpan(2, 2)));

        if (cvar.Store.TupleVariationCount == 0)
            bag.Add(EmptyStore.Create(
                [cvar.Store.TupleVariationCount],
                table: tag, field: nameof(CvarTable.Store)));

        ushort reserved = (ushort)(cvar.Store.RawTupleVariationCount & (ushort)TupleVariationStoreFlags.Reserved);
        if (reserved != 0)
            bag.Add(ReservedBitsSet.Create(
                [reserved, cvar.Store.RawTupleVariationCount],
                table: tag, field: nameof(CvarTable.Store)));

        if (!face.Directory.ContainsKey("cvt "))
            bag.Add(CvtTableMissing.Create([], table: tag));
    }
}
