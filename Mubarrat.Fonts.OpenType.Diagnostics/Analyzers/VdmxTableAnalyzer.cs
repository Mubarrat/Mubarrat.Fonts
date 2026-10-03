using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables.Metadata;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>VDMX</c> table.</summary>
/// <remarks>
/// <para>
/// The parser rejects the wrong version and the empty-ratio-range case. What remains for
/// analysis is the internal consistency of the grouping hierarchy: sortedness of ratio
/// ranges and group records, coverage of every record by its group's start/end size
/// bounds, and the top-level <c>numRecs</c> count.
/// </para>
/// <para>
/// The VDMX table is only meaningful for hinted TrueType fonts. Fonts that ship it with
/// no hinting (or with the head.flags bit cleared) provide data consumers ignore.
/// </para>
/// </remarks>
public class VdmxTableAnalyzer : IFontAnalyzer
{
    public static readonly DiagnosticDescriptor NumRecsMismatch = new(
        "OT.vdmx.num-recs", "numRecs does not match the sum of group record counts",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "numRecs is {0}, but the groups declare {1} records in total.",
        "The header's numRecs field must equal the total number of VDMX records across all groups.");

    public static readonly DiagnosticDescriptor RatioRangesUnsorted = new(
        "OT.vdmx.ratios-unsorted", "Ratio ranges are not sorted",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Ratio range at index {0} ({1},{2})–({3},{4}) precedes the previous range ({5},{6})–({7},{8}).",
        "Ratio ranges are declared in order. A caller that walks the list in order relies on the ordering.");

    public static readonly DiagnosticDescriptor RatioYRangeInverted = new(
        "OT.vdmx.ratio-inverted", "Ratio range has YStartRatio greater than YEndRatio",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Ratio range at index {0} has YStartRatio {1} and YEndRatio {2}.",
        "The y-ratio bounds must be non-decreasing.");

    public static readonly DiagnosticDescriptor GroupStartGreaterThanEnd = new(
        "OT.vdmx.group-size-inverted", "Group StartSize exceeds EndSize",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Group {0} has StartSize {1} and EndSize {2}.",
        "The pixel-height bounds must be non-decreasing.");

    public static readonly DiagnosticDescriptor RecordsUnsorted = new(
        "OT.vdmx.records-unsorted", "Group records are not sorted by pixel height",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Group {0} record at index {1} has yPelHeight {2}, less than the previous {3}.",
        "Records within a group must be sorted by yPelHeight so consumers can binary-search.");

    public static readonly DiagnosticDescriptor RecordOutOfGroupBounds = new(
        "OT.vdmx.record-out-of-bounds", "Record yPelHeight lies outside its group's bounds",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "Group {0} declares bounds [{1}, {2}] but record {3} has yPelHeight {4}.",
        "Every record's pixel height must fall within the group's declared range.");

    public static readonly DiagnosticDescriptor YMaxLessThanYMin = new(
        "OT.vdmx.ymax-below-ymin", "Record YMax is below YMin",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "Group {0} record for yPelHeight {1} has YMax {2} and YMin {3}.",
        "The upper and lower bounds are inverted. This is almost always a byte order or a sign error.");

    public static readonly DiagnosticDescriptor DuplicateYPelHeight = new(
        "OT.vdmx.duplicate-height", "Group has two records for the same pixel height",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "Group {0} records at index {1} and {2} both declare yPelHeight {3}.",
        "Each pixel height must appear at most once per group.");

    public static readonly DiagnosticDescriptor DefaultRatioRangeMissing = new(
        "OT.vdmx.missing-default-ratio", "No default ratio range present",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "None of the {0} ratio ranges declares the (0, 0, 0) default.",
        "A default ratio range applies to every aspect ratio not explicitly covered. Fonts with only specific ratios may fail to produce metrics for unusual displays.");

    public static readonly DiagnosticDescriptor NoRatioRanges = new(
        "OT.vdmx.no-ratios", "VDMX has no ratio ranges",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "The table declares no ratio ranges.",
        "A VDMX table with no ratio ranges provides no metrics.");

    private VdmxTableAnalyzer() { }

    public static VdmxTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var vdmx = face.GetTable<VdmxTable>();
        var tag = VdmxTable.Tag;

        AnalyzeHeader(vdmx, tag, bag);
        AnalyzeRatioRanges(vdmx, tag, bag);
        AnalyzeGroups(vdmx, tag, bag);
    }

    private static void AnalyzeHeader(VdmxTable vdmx, Tag tag, DiagnosticBag bag)
    {
        if (vdmx.RatioCount == 0)
        {
            bag.Add(NoRatioRanges.Create(
                [], table: tag, field: nameof(VdmxTable.RatioRanges)));
            return;
        }

        int totalRecords = 0;
        for (int i = 0; i < vdmx.Groups.Count; i++)
            totalRecords += vdmx.Groups[i].Records.Count;

        if (vdmx.NumRecs != totalRecords)
        {
            bag.Add(NumRecsMismatch.Create(
                [vdmx.NumRecs, totalRecords],
                table: tag, field: nameof(VdmxTable.NumRecs)));
        }
    }

    private static void AnalyzeRatioRanges(VdmxTable vdmx, Tag tag, DiagnosticBag bag)
    {
        bool hasDefault = false;

        for (int i = 0; i < vdmx.RatioRanges.Count; i++)
        {
            VdmxRatioRange r = vdmx.RatioRanges[i];

            if (r.XRatio == 0 && r.YStartRatio == 0 && r.YEndRatio == 0)
                hasDefault = true;

            if (r.YStartRatio > r.YEndRatio)
            {
                bag.Add(RatioYRangeInverted.Create(
                    [i, r.YStartRatio, r.YEndRatio],
                    table: tag, field: $"ratioRanges[{i}]"));
            }

            if (i > 0)
            {
                VdmxRatioRange prev = vdmx.RatioRanges[i - 1];
                bool outOfOrder =
                    r.XRatio < prev.XRatio ||
                    (r.XRatio == prev.XRatio && r.YStartRatio < prev.YStartRatio);

                if (outOfOrder)
                {
                    bag.Add(RatioRangesUnsorted.Create(
                        [i, r.CharSet, r.XRatio, r.YStartRatio, r.YEndRatio,
                         prev.CharSet, prev.XRatio, prev.YStartRatio, prev.YEndRatio],
                        table: tag, field: $"ratioRanges[{i}]"));
                }
            }
        }

        if (!hasDefault)
        {
            bag.Add(DefaultRatioRangeMissing.Create(
                [vdmx.RatioRanges.Count],
                table: tag, field: nameof(VdmxTable.RatioRanges)));
        }
    }

    private static void AnalyzeGroups(VdmxTable vdmx, Tag tag, DiagnosticBag bag)
    {
        for (int g = 0; g < vdmx.Groups.Count; g++)
        {
            VdmxGroup group = vdmx.Groups[g];

            if (group.StartSize > group.EndSize)
            {
                bag.Add(GroupStartGreaterThanEnd.Create(
                    [g, group.StartSize, group.EndSize],
                    table: tag, field: $"groups[{g}]"));
            }

            var records = group.Records;
            for (int i = 0; i < records.Count; i++)
            {
                VdmxRecord rec = records[i];

                if (rec.YPelHeight < group.StartSize || rec.YPelHeight > group.EndSize)
                {
                    bag.Add(RecordOutOfGroupBounds.Create(
                        [g, group.StartSize, group.EndSize, i, rec.YPelHeight],
                        table: tag, field: $"groups[{g}].records[{i}]"));
                }

                if (rec.YMax < rec.YMin)
                {
                    bag.Add(YMaxLessThanYMin.Create(
                        [g, rec.YPelHeight, rec.YMax, rec.YMin],
                        table: tag, field: $"groups[{g}].records[{i}]"));
                }

                if (i > 0)
                {
                    VdmxRecord prev = records[i - 1];
                    if (rec.YPelHeight < prev.YPelHeight)
                    {
                        bag.Add(RecordsUnsorted.Create(
                            [g, i, rec.YPelHeight, prev.YPelHeight],
                            table: tag, field: $"groups[{g}].records[{i}]"));
                    }
                    else if (rec.YPelHeight == prev.YPelHeight)
                    {
                        bag.Add(DuplicateYPelHeight.Create(
                            [g, i - 1, i, rec.YPelHeight],
                            table: tag, field: $"groups[{g}].records[{i}]"));
                    }
                }
            }
        }
    }
}
