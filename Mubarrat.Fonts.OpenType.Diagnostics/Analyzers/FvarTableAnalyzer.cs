using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables.Variations;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>fvar</c> table.</summary>
public class FvarTableAnalyzer : IFontAnalyzer
{
    private const int AxisRecordSize = 20;
    private const int MinNameId = 256;
    private const int MaxNameId = 32768;

    public static readonly DiagnosticDescriptor MajorVersionInvalid = new(
        "OT.fvar.major-version", "Unexpected fvar major version",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "majorVersion is {0}, expected 1.");

    public static readonly DiagnosticDescriptor MinorVersionNonZero = new(
        "OT.fvar.minor-version", "Unexpected fvar minor version",
        DiagnosticSeverity.Information, DiagnosticCategory.Specification,
        "minorVersion is {0}, expected 0.");

    public static readonly DiagnosticDescriptor AxisCountZero = new(
        "OT.fvar.axis-count-zero", "fvar declares zero axes",
        DiagnosticSeverity.Information, DiagnosticCategory.Specification,
        "axisCount is 0.",
        "The spec permits this for conditionally-variable fonts. The font is treated as non-variable.");

    public static readonly DiagnosticDescriptor AxisSizeTooSmall = new(
        "OT.fvar.axis-size", "fvar axisSize is too small",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "axisSize is {0}, expected at least {1}.");

    public static readonly DiagnosticDescriptor InstanceSizeInvalid = new(
        "OT.fvar.instance-size", "fvar instanceSize is invalid",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "instanceSize is {0}, expected {1} (no postScriptNameID) or {2} (with postScriptNameID).");

    public static readonly DiagnosticDescriptor DuplicateAxisTag = new(
        "OT.fvar.axis-tag-duplicate", "Two axes share a tag",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "Axes at index {0} and {1} both use tag '{2}'.");

    public static readonly DiagnosticDescriptor AxisRangeInverted = new(
        "OT.fvar.axis-range", "Axis min/default/max are out of order",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "Axis '{0}' has min {1}, default {2}, max {3}; expected min ≤ default ≤ max.");

    public static readonly DiagnosticDescriptor AxisRangeEqual = new(
        "OT.fvar.axis-range-equal", "Axis declares min == max",
        DiagnosticSeverity.Warning, DiagnosticCategory.Consistency,
        "Axis '{0}' has min = max = {1}; the axis has no variation range.");

    public static readonly DiagnosticDescriptor AxisNameIdOutOfRange = new(
        "OT.fvar.axis-name-id", "Axis name ID out of range",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "Axis '{0}' has axisNameID {1}, expected a value in [{2}, {3}).");

    public static readonly DiagnosticDescriptor InstanceSubfamilyNameIdOutOfRange = new(
        "OT.fvar.instance-subfamily-name-id", "Instance subfamily name ID out of range",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "Instance {0} has subfamilyNameID {1}, expected 2, 17, or a value in [{2}, {3}).");

    public static readonly DiagnosticDescriptor InstancePostScriptNameIdOutOfRange = new(
        "OT.fvar.instance-postscript-name-id", "Instance postScriptNameID out of range",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "Instance {0} has postScriptNameID {1}, expected 0xFFFF or a value in [{2}, {3}).");

    public static readonly DiagnosticDescriptor InstanceCoordinateOutOfRange = new(
        "OT.fvar.instance-coordinate", "Named instance coordinate out of axis range",
        DiagnosticSeverity.Error, DiagnosticCategory.Consistency,
        "Instance {0} coordinate for axis '{1}' is {2}, outside [{3}, {4}].");

    public static readonly DiagnosticDescriptor InstanceCountZero = new(
        "OT.fvar.instance-count-zero", "fvar declares axes but no named instances",
        DiagnosticSeverity.Information, DiagnosticCategory.Compatibility,
        "axisCount is {0} but instanceCount is 0.",
        "Named instances are optional. Some authoring tools rely on them to present preset configurations.");

    public static readonly DiagnosticDescriptor HiddenAxis = new(
        "OT.fvar.hidden-axis", "Axis is marked hidden",
        DiagnosticSeverity.Information, DiagnosticCategory.Compatibility,
        "Axis '{0}' has the HIDDEN_AXIS flag set.",
        "The axis is not exposed directly in user interfaces. Named instances are unaffected.");

    private FvarTableAnalyzer() { }

    public static FvarTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var fvar = face.GetTable<FvarTable>();
        var tag = FvarTable.Tag;

        AnalyzeHeader(fvar, tag, bag);
        AnalyzeAxes(fvar, tag, bag);
        AnalyzeInstances(fvar, tag, bag);
    }

    private static void AnalyzeHeader(FvarTable fvar, Tag tag, DiagnosticBag bag)
    {
        if (fvar.MajorVersion != 1)
            bag.Add(MajorVersionInvalid.Create(
                [fvar.MajorVersion], table: tag, field: nameof(FvarTable.MajorVersion),
                span: new SourceSpan(0, 2)));

        if (fvar.MinorVersion != 0)
            bag.Add(MinorVersionNonZero.Create(
                [fvar.MinorVersion], table: tag, field: nameof(FvarTable.MinorVersion),
                span: new SourceSpan(2, 2)));

        if (fvar.AxisCount == 0)
            bag.Add(AxisCountZero.Create(
                [], table: tag, field: nameof(FvarTable.AxisCount),
                span: new SourceSpan(8, 2)));

        if (fvar.DeclaredAxisSize < AxisRecordSize)
            bag.Add(AxisSizeTooSmall.Create(
                [fvar.DeclaredAxisSize, AxisRecordSize],
                table: tag, field: nameof(FvarTable.DeclaredAxisSize),
                span: new SourceSpan(10, 2)));

        if (fvar.AxisCount > 0 && fvar.InstanceCount == 0)
            bag.Add(InstanceCountZero.Create(
                [fvar.AxisCount], table: tag, field: nameof(FvarTable.InstanceCount),
                span: new SourceSpan(12, 2)));
    }

    private static void AnalyzeAxes(FvarTable fvar, Tag tag, DiagnosticBag bag)
    {
        var seen = new Dictionary<Tag, int>();

        for (int i = 0; i < fvar.Axes.Count; i++)
        {
            var axis = fvar.Axes[i];

            if (seen.TryGetValue(axis.Tag, out int first))
                bag.Add(DuplicateAxisTag.Create(
                    [first, i, axis.Tag.ToString()],
                    table: tag, field: $"axes[{i}]"));
            else
                seen[axis.Tag] = i;

            double min = axis.MinValue.Value;
            double def = axis.DefaultValue.Value;
            double max = axis.MaxValue.Value;

            if (min > def || def > max)
                bag.Add(AxisRangeInverted.Create(
                    [axis.Tag.ToString(), min, def, max],
                    table: tag, field: $"axes[{i}]"));
            else if (min == max)
                bag.Add(AxisRangeEqual.Create(
                    [axis.Tag.ToString(), min],
                    table: tag, field: $"axes[{i}]"));

            if (axis.AxisNameID < MinNameId || axis.AxisNameID >= MaxNameId)
                bag.Add(AxisNameIdOutOfRange.Create(
                    [axis.Tag.ToString(), axis.AxisNameID, MinNameId, MaxNameId],
                    table: tag, field: $"axes[{i}].axisNameID"));

            if (axis.IsHidden)
                bag.Add(HiddenAxis.Create(
                    [axis.Tag.ToString()],
                    table: tag, field: $"axes[{i}].flags"));
        }
    }

    private static void AnalyzeInstances(FvarTable fvar, Tag tag, DiagnosticBag bag)
    {
        for (int i = 0; i < fvar.Instances.Count; i++)
        {
            var inst = fvar.Instances[i];

            // SubfamilyNameID valid set: 2, 17 (default-instance cases), or [256, 32768).
            if (inst.SubfamilyNameID != 2 && inst.SubfamilyNameID != 17 &&
                (inst.SubfamilyNameID < MinNameId || inst.SubfamilyNameID >= MaxNameId))
            {
                bag.Add(InstanceSubfamilyNameIdOutOfRange.Create(
                    [i, inst.SubfamilyNameID, MinNameId, MaxNameId],
                    table: tag, field: $"instances[{i}].subfamilyNameID"));
            }

            if (inst.PostScriptNameID is { } psId && psId != 0xFFFF
                && (psId < MinNameId || psId >= MaxNameId))
            {
                bag.Add(InstancePostScriptNameIdOutOfRange.Create(
                    [i, psId, MinNameId, MaxNameId],
                    table: tag, field: $"instances[{i}].postScriptNameID"));
            }

            int axes = Math.Min(fvar.Axes.Count, inst.Coordinates.Count);
            for (int a = 0; a < axes; a++)
            {
                double value = inst.Coordinates[a];
                double min = fvar.Axes[a].MinValue.Value;
                double max = fvar.Axes[a].MaxValue.Value;

                if (value < min || value > max)
                    bag.Add(InstanceCoordinateOutOfRange.Create(
                        [i, fvar.Axes[a].Tag.ToString(), value, min, max],
                        table: tag, field: $"instances[{i}]"));
            }
        }
    }
}
