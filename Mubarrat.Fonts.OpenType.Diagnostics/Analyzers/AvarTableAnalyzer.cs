using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables.Variations;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>avar</c> table.</summary>
/// <remarks>
/// <para>
/// <c>avar</c> is a dependent of <c>fvar</c>: the axis count must match, and the segment
/// maps are ordered by <c>fvar</c> axis order. The mismatch rule fires here because
/// <c>avar</c> is the table that must agree with <c>fvar</c>.
/// </para>
/// <para>
/// Three structural violations are already enforced in <see cref="AvarTable.Parse"/> and
/// cannot be observed by the analyzer: <c>majorVersion != 1</c>, <c>axisCount == 0</c>, and
/// an empty segment map (<c>count == 0</c>). What remains — versions above the min, the
/// reserved field, the axis-count cross-check, and the segment-map shape — is checked here.
/// </para>
/// </remarks>
public class AvarTableAnalyzer : IFontAnalyzer
{
    // ─────────────────────── Descriptors ───────────────────────

    public static readonly DiagnosticDescriptor MajorVersionInvalid = new(
        "OT.avar.major-version", "Unexpected avar major version",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "majorVersion is {0}, expected 1.",
        "Every published revision of the avar table specifies major version 1.");

    public static readonly DiagnosticDescriptor MinorVersionNonZero = new(
        "OT.avar.minor-version", "Unexpected avar minor version",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Specification,
        "minorVersion is {0}, expected 0.",
        "Every published revision of the avar table specifies minor version 0.");

    public static readonly DiagnosticDescriptor ReservedNonZero = new(
        "OT.avar.reserved", "Reserved avar field is non-zero",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "reserved is 0x{0:X4}, expected 0.",
        "The reserved field must be zero. A non-zero value usually indicates a malformed table or a layout misread.");

    public static readonly DiagnosticDescriptor AxisCountMismatch = new(
        "OT.avar.axis-count-vs-fvar", "avar axisCount disagrees with fvar",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "avar.axisCount is {0} but fvar.axisCount is {1}.",
        "Every axis declared in fvar must have a corresponding segment map in avar, in the same order.");

    public static readonly DiagnosticDescriptor SegmentMapTooShort = new(
        "OT.avar.segment-map-too-short", "Segment map has fewer than 2 entries",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Axis {0} segment map has {1} entries; the minimum is 2.",
        "A segment map must contain at least a start point and an end point. A single entry cannot satisfy both the (-1, -1) and (1, 1) requirements.");

    public static readonly DiagnosticDescriptor SegmentMapStartInvalid = new(
        "OT.avar.segment-map-start", "Segment map does not begin with (-1, -1)",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Axis {0} segment map begins with ({1}, {2}), expected (-1, -1).",
        "The spec requires every segment map to anchor its start point at the minimum normalized coordinate.");

    public static readonly DiagnosticDescriptor SegmentMapEndInvalid = new(
        "OT.avar.segment-map-end", "Segment map does not end with (1, 1)",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Axis {0} segment map ends with ({1}, {2}), expected (1, 1).",
        "The spec requires every segment map to anchor its end point at the maximum normalized coordinate.");

    public static readonly DiagnosticDescriptor FromCoordinatesNotIncreasing = new(
        "OT.avar.from-not-increasing", "From coordinates are not strictly increasing",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Axis {0} has fromCoordinate {1} at index {2}, not greater than the previous {3}.",
        "The fromCoordinate values must be strictly increasing. Duplicate or descending input coordinates make the piecewise-linear map undefined.");

    public static readonly DiagnosticDescriptor ToCoordinatesNotMonotonic = new(
        "OT.avar.to-not-monotonic", "To coordinates are not monotonic",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Axis {0} has toCoordinate {1} at index {2}, less than the previous {3}.",
        "The spec requires the toCoordinate values to be in ascending order. A non-monotonic output map reverses the user's axis direction in part of the range.");

    public static readonly DiagnosticDescriptor IdentityMap = new(
        "OT.avar.identity-map", "Segment map is the identity",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "Axis {0} segment map maps every input to itself.",
        "An identity segment map has no effect on the axis. The axis could be omitted from avar, or the whole avar table could be removed.");

    // ─────────────────────── Singleton ───────────────────────

    private AvarTableAnalyzer() { }

    public static AvarTableAnalyzer Instance => field ??= new();

    // ─────────────────────── Analysis ───────────────────────

    /// <summary>Runs every avar rule against the face's <c>avar</c> table.</summary>
    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var avar = face.GetTable<AvarTable>();
        var tag = AvarTable.Tag;

        AnalyzeHeader(avar, tag, bag);
        AnalyzeAxisCount(face, avar, tag, bag);
        AnalyzeSegmentMaps(avar, tag, bag);
    }

    // ─────────────────────── Rule groups ───────────────────────

    private static void AnalyzeHeader(AvarTable avar, Tag tag, DiagnosticBag bag)
    {
        if (avar.MajorVersion != 1)
            bag.Add(MajorVersionInvalid.Create(
                [avar.MajorVersion],
                table: tag, field: nameof(AvarTable.MajorVersion),
                span: new SourceSpan(0, 2)));

        if (avar.MinorVersion != 0)
            bag.Add(MinorVersionNonZero.Create(
                [avar.MinorVersion],
                table: tag, field: nameof(AvarTable.MinorVersion),
                span: new SourceSpan(2, 2)));

        if (avar.Reserved != 0)
            bag.Add(ReservedNonZero.Create(
                [avar.Reserved],
                table: tag, field: nameof(AvarTable.Reserved),
                span: new SourceSpan(4, 2)));
    }

    private static void AnalyzeAxisCount(FontFace face, AvarTable avar, Tag tag, DiagnosticBag bag)
    {
        if (!face.Directory.Contains(FvarTable.Tag)) return;

        var fvar = face.GetTable<FvarTable>();
        if (avar.AxisCount != fvar.AxisCount)
        {
            bag.Add(AxisCountMismatch.Create(
                [avar.AxisCount, fvar.AxisCount],
                table: tag, field: nameof(AvarTable.AxisCount),
                span: new SourceSpan(6, 2)));
        }
    }

    private static void AnalyzeSegmentMaps(AvarTable avar, Tag tag, DiagnosticBag bag)
    {
        for (int a = 0; a < avar.AxisSegmentMaps.Count; a++)
            AnalyzeSegmentMap(avar.AxisSegmentMaps[a], a, tag, bag);
    }

    private static void AnalyzeSegmentMap(
        IReadOnlyList<AxisValueMap> map, int axisIndex, Tag tag, DiagnosticBag bag)
    {
        // Parse throws on count == 0, so a map reaching the analyzer always has at least
        // one entry. A single entry cannot satisfy the (-1, -1) and (1, 1) anchors.
        if (map.Count < 2)
        {
            bag.Add(SegmentMapTooShort.Create(
                [axisIndex, map.Count],
                table: tag, field: $"axisSegmentMaps[{axisIndex}]"));
            return;
        }

        // First entry must be (-1, -1).
        if (map[0].FromCoordinate.Value != -1.0 || map[0].ToCoordinate.Value != -1.0)
        {
            bag.Add(SegmentMapStartInvalid.Create(
                [axisIndex, map[0].FromCoordinate.Value, map[0].ToCoordinate.Value],
                table: tag, field: $"axisSegmentMaps[{axisIndex}][0]"));
        }

        // Last entry must be (1, 1).
        int last = map.Count - 1;
        if (map[last].FromCoordinate.Value != 1.0 || map[last].ToCoordinate.Value != 1.0)
        {
            bag.Add(SegmentMapEndInvalid.Create(
                [axisIndex, map[last].FromCoordinate.Value, map[last].ToCoordinate.Value],
                table: tag, field: $"axisSegmentMaps[{axisIndex}][{last}]"));
        }

        // Monotonicity and identity in one pass. Report each monotonicity violation at
        // most once per map to avoid flooding the bag on a badly damaged font.
        bool identity = map[0].FromCoordinate.Value == map[0].ToCoordinate.Value;
        bool fromReported = false;
        bool toReported = false;

        for (int i = 1; i < map.Count; i++)
        {
            double prevFrom = map[i - 1].FromCoordinate.Value;
            double currFrom = map[i].FromCoordinate.Value;
            double prevTo = map[i - 1].ToCoordinate.Value;
            double currTo = map[i].ToCoordinate.Value;

            if (!fromReported && currFrom <= prevFrom)
            {
                bag.Add(FromCoordinatesNotIncreasing.Create(
                    [axisIndex, currFrom, i, prevFrom],
                    table: tag, field: $"axisSegmentMaps[{axisIndex}][{i}]"));
                fromReported = true;
            }

            if (!toReported && currTo < prevTo)
            {
                bag.Add(ToCoordinatesNotMonotonic.Create(
                    [axisIndex, currTo, i, prevTo],
                    table: tag, field: $"axisSegmentMaps[{axisIndex}][{i}]"));
                toReported = true;
            }

            if (identity && currFrom != currTo)
                identity = false;
        }

        if (identity)
        {
            bag.Add(IdentityMap.Create(
                [axisIndex],
                table: tag, field: $"axisSegmentMaps[{axisIndex}]"));
        }
    }
}
