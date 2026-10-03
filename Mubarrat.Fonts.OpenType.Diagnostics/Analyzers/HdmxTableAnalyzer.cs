using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables;
using Mubarrat.Fonts.OpenType.Tables.Metadata;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>hdmx</c> table.</summary>
/// <remarks>
/// <para>
/// The parser rejects the wrong version, an undersized <c>sizeDeviceRecord</c>, and any
/// read past the table's declared length. What remains for analysis is the shape of the
/// device record list: sortedness by pixel size, per-record width consistency, and the
/// <c>head.flags</c> cross-check the spec requires.
/// </para>
/// <para>
/// hdmx is only meaningful when <c>head.flags</c> bit 4 is set. The spec says the table
/// "should not be present unless the bit is set" — because it exists to cache the
/// nonlinear advance widths that hinting produces.
/// </para>
/// </remarks>
public class HdmxTableAnalyzer : IFontAnalyzer
{
    public static readonly DiagnosticDescriptor RecordsNotSorted = new(
        "OT.hdmx.records-unsorted", "Device records are not sorted by pixel size",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Record at index {0} has pixelSize {1}, less than the previous record's pixelSize {2}.",
        "The spec requires device records sorted by increasing pixel size. Unsorted records break binary search in consumers.");

    public static readonly DiagnosticDescriptor DuplicatePixelSize = new(
        "OT.hdmx.duplicate-pixel-size", "Two device records share a pixel size",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Records at index {0} and {1} both have pixelSize {2}.",
        "Each pixel size must appear at most once. Duplicates make lookups ambiguous.");

    public static readonly DiagnosticDescriptor MaxWidthInconsistent = new(
        "OT.hdmx.max-width-inconsistent", "MaxWidth is less than a per-glyph width",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "Record for pixelSize {0} declares maxWidth {1} but glyph {2} has width {3}.",
        "MaxWidth is the maximum of the per-glyph widths in the record. A smaller value means the record was written inconsistently.");

    public static readonly DiagnosticDescriptor SizeDeviceRecordNotAligned = new(
        "OT.hdmx.size-not-aligned", "sizeDeviceRecord is not 4-byte aligned",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "sizeDeviceRecord is {0}; the spec requires 32-bit alignment.",
        "Each device record must be padded to a 32-bit boundary so consumers can stride through the array by index.");

    public static readonly DiagnosticDescriptor SizeDeviceRecordHasPadding = new(
        "OT.hdmx.size-has-padding", "sizeDeviceRecord exceeds the minimum record size",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "sizeDeviceRecord is {0} but only {1} bytes are needed for {2} glyphs.",
        "Extra padding is legal but wastes space. Some fonts use it to align records for faster indexing.");

    public static readonly DiagnosticDescriptor TablePresentWithoutNonlinearFlag = new(
        "OT.hdmx.missing-nonlinear-flag", "hdmx is present but head.flags does not indicate nonlinear scaling",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "head.flags bit 4 is clear, but the font includes an hdmx table.",
        "The spec says hdmx should not be present unless head.flags bit 4 is set. Consumers that ignore the flag may not consult hdmx.");

    public static readonly DiagnosticDescriptor NoRecords = new(
        "OT.hdmx.no-records", "hdmx declares zero device records",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "The table has no device records.",
        "An hdmx table with no records provides no cached metrics and could be omitted.");

    public static readonly DiagnosticDescriptor AllWidthsZero = new(
        "OT.hdmx.all-widths-zero", "Device record has all-zero widths",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "Record for pixelSize {0} has every glyph width set to 0.",
        "A record where every glyph has zero advance produces no visible text at that size. Usually a generation error.");

    private HdmxTableAnalyzer() { }

    public static HdmxTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var hdmx = face.GetTable<HdmxTable>();
        var tag = HdmxTable.Tag;

        AnalyzeHeader(face, hdmx, tag, bag);
        AnalyzeRecords(hdmx, tag, bag);
    }

    private static void AnalyzeHeader(FontFace face, HdmxTable hdmx, Tag tag, DiagnosticBag bag)
    {
        int minimumSize = 2 + hdmx.NumGlyphs;
        if (hdmx.SizeDeviceRecord < (uint)minimumSize)
        {
            // Parser already rejects this; rule kept for lenient parse modes.
        }
        else if (hdmx.SizeDeviceRecord > (uint)minimumSize)
        {
            // Any extra bytes beyond 2 + numGlyphs are padding. Accept but note it.
            // Only report informational when the extra exceeds 3 bytes (the alignment
            // padding a conforming writer would use).
            uint padding = hdmx.SizeDeviceRecord - (uint)minimumSize;
            if (padding >= 4)
            {
                bag.Add(SizeDeviceRecordHasPadding.Create(
                    [hdmx.SizeDeviceRecord, minimumSize, hdmx.NumGlyphs],
                    table: tag, field: nameof(HdmxTable.SizeDeviceRecord)));
            }
        }

        if ((hdmx.SizeDeviceRecord & 3) != 0)
        {
            bag.Add(SizeDeviceRecordNotAligned.Create(
                [hdmx.SizeDeviceRecord],
                table: tag, field: nameof(HdmxTable.SizeDeviceRecord)));
        }

        if (hdmx.RecordCount == 0)
        {
            bag.Add(NoRecords.Create(
                [], table: tag, field: nameof(HdmxTable.Records)));
        }

        // Cross-check: head.flags bit 4 must be set for hdmx to be meaningful.
        if (face.Directory.Contains(HeadTable.Tag))
        {
            var head = face.GetTable<HeadTable>();
            bool nonlinear = (head.Flags & HeadFlags.InstructionsAlterAdvanceWidth) != 0;
            if (!nonlinear)
            {
                bag.Add(TablePresentWithoutNonlinearFlag.Create(
                    [], table: tag));
            }
        }
    }

    private static void AnalyzeRecords(HdmxTable hdmx, Tag tag, DiagnosticBag bag)
    {
        var records = hdmx.Records;
        if (records.Count == 0) return;

        // Sortedness and duplicate detection in one pass.
        for (int i = 1; i < records.Count; i++)
        {
            byte prev = records[i - 1].PixelSize;
            byte curr = records[i].PixelSize;

            if (curr < prev)
            {
                bag.Add(RecordsNotSorted.Create(
                    [i, curr, prev],
                    table: tag, field: $"records[{i}].PixelSize"));
            }
            else if (curr == prev)
            {
                bag.Add(DuplicatePixelSize.Create(
                    [i - 1, i, curr],
                    table: tag, field: $"records[{i}].PixelSize"));
            }
        }

        // Per-record max-width and all-zero checks.
        for (int i = 0; i < records.Count; i++)
        {
            HdmxDeviceRecord rec = records[i];
            byte declared = rec.MaxWidth;
            byte actualMax = 0;
            bool allZero = true;

            for (int g = 0; g < rec.Widths.Count; g++)
            {
                byte w = rec.Widths[g];
                if (w > actualMax) actualMax = w;
                if (w != 0) allZero = false;
                if (w > declared && declared != 0)
                {
                    bag.Add(MaxWidthInconsistent.Create(
                        [rec.PixelSize, declared, g, w],
                        table: tag, field: $"records[{i}].MaxWidth"));
                    declared = w;   // report once per record
                }
            }

            if (allZero)
            {
                bag.Add(AllWidthsZero.Create(
                    [rec.PixelSize],
                    table: tag, field: $"records[{i}].Widths"));
            }
        }
    }
}
