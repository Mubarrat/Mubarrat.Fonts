using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>STAT</c> table.</summary>
public class StatTableAnalyzer : IFontAnalyzer
{
    private const int AxisRecordSize = 8;
    private const int MinNameId = 256;
    private const int MaxNameId = 32768;

    public static readonly DiagnosticDescriptor MajorVersionInvalid = new(
        "OT.stat.major-version", "Unexpected STAT major version",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "majorVersion is {0}, expected 1.");

    public static readonly DiagnosticDescriptor MinorVersionInvalid = new(
        "OT.stat.minor-version", "Unexpected STAT minor version",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "minorVersion is {0}, expected 0, 1, or 2.");

    public static readonly DiagnosticDescriptor AxisSizeTooSmall = new(
        "OT.stat.design-axis-size", "STAT designAxisSize is too small",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "designAxisSize is {0}, expected at least {1}.");

    public static readonly DiagnosticDescriptor ElidedFallbackNameIdMissing = new(
        "OT.stat.missing-elided-fallback", "STAT version 1.1+ requires elidedFallbackNameID",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "minorVersion is {0} but elidedFallbackNameID is absent.");

    public static readonly DiagnosticDescriptor ElidedFallbackNameIdOutOfRange = new(
        "OT.stat.elided-fallback-name-id", "STAT elidedFallbackNameID out of range",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "elidedFallbackNameID is {0}, expected a value in [{1}, {2}).");

    public static readonly DiagnosticDescriptor DuplicateAxisTag = new(
        "OT.stat.axis-tag-duplicate", "Two design axes share a tag",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "Design axes at index {0} and {1} both use tag '{2}'.");

    public static readonly DiagnosticDescriptor AxisOrderingNotSorted = new(
        "OT.stat.axis-ordering", "Design axes are not sorted by axisOrdering",
        DiagnosticSeverity.Warning, DiagnosticCategory.Specification,
        "Axis '{0}' at index {1} has ordering {2}, less than the previous axis ordering {3}.");

    public static readonly DiagnosticDescriptor AxisNameIdOutOfRange = new(
        "OT.stat.axis-name-id", "Design axis name ID out of range",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "Axis '{0}' has axisNameID {1}, expected a value in [{2}, {3}).");

    public static readonly DiagnosticDescriptor DesignAxesMismatch = new(
        "OT.stat.design-axes-vs-fvar", "STAT and fvar disagree on axis count",
        DiagnosticSeverity.Error, DiagnosticCategory.Consistency,
        "STAT has {0} design axes but fvar has {1} axes.");

    public static readonly DiagnosticDescriptor MissingFvarAxis = new(
        "OT.stat.missing-fvar-axis", "STAT does not describe an fvar axis",
        DiagnosticSeverity.Error, DiagnosticCategory.Consistency,
        "fvar axis '{0}' has no matching STAT design axis.");

    public static readonly DiagnosticDescriptor ExtraStatAxis = new(
        "OT.stat.extra-design-axis", "STAT declares a design axis not present in fvar",
        DiagnosticSeverity.Warning, DiagnosticCategory.Consistency,
        "STAT design axis '{0}' has no matching fvar axis.");

    public static readonly DiagnosticDescriptor AxisIndexOutOfRange = new(
        "OT.stat.axis-index-out-of-range", "Axis value references an out-of-range axis",
        DiagnosticSeverity.Error, DiagnosticCategory.Consistency,
        "Axis value at index {0} references axis {1}, but only {2} design axes exist.");

    public static readonly DiagnosticDescriptor RangeInverted = new(
        "OT.stat.range-inverted", "STAT axis value range is inverted",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "Axis value at index {0} has rangeMin {1} greater than rangeMax {2}.");

    public static readonly DiagnosticDescriptor NominalOutsideRange = new(
        "OT.stat.nominal-outside-range", "STAT nominal value lies outside its range",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "Axis value at index {0} has nominal {1} outside [{2}, {3}].");

    public static readonly DiagnosticDescriptor ValueNameIdOutOfRange = new(
        "OT.stat.value-name-id", "STAT value name ID out of range",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "Axis value at index {0} has valueNameID {1}, expected a value in [{2}, {3}).");

    public static readonly DiagnosticDescriptor LinkedValueNameIdOutOfRange = new(
        "OT.stat.linked-value-name-id", "STAT linked value name ID out of range",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "Axis value at index {0} has linkedValueNameID {1}, expected a value in [{2}, {3}).");

    public static readonly DiagnosticDescriptor DuplicateAxisIndexInFormat4 = new(
        "OT.stat.format-4-duplicate-axis", "Format 4 axis value repeats an axis",
        DiagnosticSeverity.Error, DiagnosticCategory.Specification,
        "Axis value at index {0} lists axis {1} more than once.");

    private StatTableAnalyzer() { }

    public static StatTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var stat = face.GetTable<StatTable>();
        var tag = StatTable.Tag;

        AnalyzeHeader(stat, tag, bag);
        AnalyzeDesignAxes(stat, tag, bag);
        AnalyzeAxisValues(stat, tag, bag);

        if (face.Directory.ContainsKey(FvarTable.Tag))
            AnalyzeAgainstFvar(face.GetTable<FvarTable>(), stat, tag, bag);
    }

    private static void AnalyzeHeader(StatTable stat, Tag tag, DiagnosticBag bag)
    {
        if (stat.MajorVersion != 1)
            bag.Add(MajorVersionInvalid.Create(
                [stat.MajorVersion], table: tag, field: nameof(StatTable.MajorVersion)));

        if (stat.MinorVersion > 2)
            bag.Add(MinorVersionInvalid.Create(
                [stat.MinorVersion], table: tag, field: nameof(StatTable.MinorVersion)));

        if (stat.MinorVersion >= 1)
        {
            if (stat.ElidedFallbackNameID is not { } id)
            {
                bag.Add(ElidedFallbackNameIdMissing.Create(
                    [stat.MinorVersion], table: tag, field: nameof(StatTable.ElidedFallbackNameID)));
            }
            else if (id < MinNameId || id >= MaxNameId)
            {
                bag.Add(ElidedFallbackNameIdOutOfRange.Create(
                    [id, MinNameId, MaxNameId],
                    table: tag, field: nameof(StatTable.ElidedFallbackNameID)));
            }
        }
    }

    private static void AnalyzeDesignAxes(StatTable stat, Tag tag, DiagnosticBag bag)
    {
        var seen = new Dictionary<Tag, int>();

        for (int i = 0; i < stat.DesignAxes.Count; i++)
        {
            var axis = stat.DesignAxes[i];

            if (seen.TryGetValue(axis.AxisTag, out int first))
                bag.Add(DuplicateAxisTag.Create(
                    [first, i, axis.AxisTag.ToString()],
                    table: tag, field: $"designAxes[{i}]"));
            else
                seen[axis.AxisTag] = i;

            if (i > 0 && stat.DesignAxes[i - 1].AxisOrdering > axis.AxisOrdering)
                bag.Add(AxisOrderingNotSorted.Create(
                    [axis.AxisTag.ToString(), i, axis.AxisOrdering,
                     stat.DesignAxes[i - 1].AxisOrdering],
                    table: tag, field: $"designAxes[{i}]"));

            if (axis.AxisNameID < MinNameId || axis.AxisNameID >= MaxNameId)
                bag.Add(AxisNameIdOutOfRange.Create(
                    [axis.AxisTag.ToString(), axis.AxisNameID, MinNameId, MaxNameId],
                    table: tag, field: $"designAxes[{i}].axisNameID"));
        }
    }

    private static void AnalyzeAxisValues(StatTable stat, Tag tag, DiagnosticBag bag)
    {
        int axisCount = stat.DesignAxes.Count;

        for (int i = 0; i < stat.AxisValues.Count; i++)
        {
            var value = stat.AxisValues[i];

            if (value.ValueNameID < MinNameId || value.ValueNameID >= MaxNameId)
                bag.Add(ValueNameIdOutOfRange.Create(
                    [i, value.ValueNameID, MinNameId, MaxNameId],
                    table: tag, field: $"axisValues[{i}].valueNameID"));

            switch (value)
            {
                case StatAxisValueFormat1 f1:
                    CheckAxisIndex(f1.AxisIndex, i, axisCount, tag, bag);
                    break;

                case StatAxisValueFormat2 f2:
                    CheckAxisIndex(f2.AxisIndex, i, axisCount, tag, bag);
                    if (f2.RangeMinValue.Value > f2.RangeMaxValue.Value)
                        bag.Add(RangeInverted.Create(
                            [i, f2.RangeMinValue.Value, f2.RangeMaxValue.Value],
                            table: tag, field: $"axisValues[{i}]"));
                    if (f2.NominalValue.Value < f2.RangeMinValue.Value ||
                        f2.NominalValue.Value > f2.RangeMaxValue.Value)
                    {
                        bag.Add(NominalOutsideRange.Create(
                            [i, f2.NominalValue.Value, f2.RangeMinValue.Value, f2.RangeMaxValue.Value],
                            table: tag, field: $"axisValues[{i}]"));
                    }
                    break;

                case StatAxisValueFormat3 f3:
                    CheckAxisIndex(f3.AxisIndex, i, axisCount, tag, bag);
                    if (f3.LinkedValueNameID < MinNameId || f3.LinkedValueNameID >= MaxNameId)
                        bag.Add(LinkedValueNameIdOutOfRange.Create(
                            [i, f3.LinkedValueNameID, MinNameId, MaxNameId],
                            table: tag, field: $"axisValues[{i}].linkedValueNameID"));
                    break;

                case StatAxisValueFormat4 f4:
                    {
                        var seen = new HashSet<int>();
                        for (int r = 0; r < f4.AxisValues.Count; r++)
                        {
                            int axisIndex = f4.AxisValues[r].AxisIndex;
                            CheckAxisIndex(axisIndex, i, axisCount, tag, bag);
                            if (!seen.Add(axisIndex))
                                bag.Add(DuplicateAxisIndexInFormat4.Create(
                                    [i, axisIndex],
                                    table: tag, field: $"axisValues[{i}].axisValues[{r}]"));
                        }
                    }
                    break;
            }
        }
    }

    private static void CheckAxisIndex(int axisIndex, int valueIndex, int axisCount, Tag tag, DiagnosticBag bag)
    {
        if (axisIndex >= axisCount)
            bag.Add(AxisIndexOutOfRange.Create(
                [valueIndex, axisIndex, axisCount],
                table: tag, field: $"axisValues[{valueIndex}]"));
    }

    private static void AnalyzeAgainstFvar(FvarTable fvar, StatTable stat, Tag tag, DiagnosticBag bag)
    {
        if (stat.DesignAxisCount != fvar.AxisCount)
            bag.Add(DesignAxesMismatch.Create(
                [stat.DesignAxisCount, fvar.AxisCount],
                table: tag, field: nameof(StatTable.DesignAxes)));

        foreach (var fvarAxis in fvar.Axes)
        {
            if (stat.GetAxis(fvarAxis.Tag) is null)
                bag.Add(MissingFvarAxis.Create(
                    [fvarAxis.Tag.ToString()],
                    table: tag, field: nameof(StatTable.DesignAxes)));
        }

        // The reverse direction: a STAT axis not in fvar is a spec violation in a variable
        // font but a legitimate static-font pattern. Report as warning.
        foreach (var statAxis in stat.DesignAxes)
        {
            if (fvar.GetAxis(statAxis.AxisTag) is null)
                bag.Add(ExtraStatAxis.Create(
                    [statAxis.AxisTag.ToString()],
                    table: tag, field: nameof(StatTable.DesignAxes)));
        }
    }
}
