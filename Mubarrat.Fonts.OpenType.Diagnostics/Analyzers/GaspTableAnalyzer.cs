using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables.Hinting;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>gasp</c> table.</summary>
/// <remarks>
/// <para>
/// The parser accepts version 0 and 1 and reads the range array. What remains is version
/// consistency, the sort order of the ranges, the terminal range invariant, and the
/// version-gated flag bits.
/// </para>
/// <para>
/// Ranges are declared with <c>MaxPPEM</c> values that partition the ppem space. The
/// final range's <c>MaxPPEM</c> must be 0xFFFF so every size is covered.
/// </para>
/// </remarks>
public class GaspTableAnalyzer : IFontAnalyzer
{
    /// <summary>The terminal range's MaxPPEM value. Anything less leaves the largest sizes uncovered.</summary>
    private const ushort TerminalMaxPpem = 0xFFFF;

    /// <summary>Bits 4–15 of the behavior field are reserved.</summary>
    private const ushort ReservedBehaviorBitsConst = 0xFFF0;

    /// <summary>Bits 2 and 3 are version 1 additions.</summary>
    private const ushort Version1OnlyBits = 0x000C;

    public static readonly DiagnosticDescriptor VersionInvalid = new(
        "OT.gasp.version", "Invalid gasp version",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "version is {0}, expected 0 or 1.",
        "The specification defines versions 0 and 1 only.");

    public static readonly DiagnosticDescriptor NoRanges = new(
        "OT.gasp.no-ranges", "gasp has no ranges",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "The table declares zero ranges.",
        "A gasp table with no ranges provides no behavior for any size and could be omitted.");

    public static readonly DiagnosticDescriptor RangesUnsorted = new(
        "OT.gasp.ranges-unsorted", "Ranges are not sorted by MaxPPEM",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Range at index {0} has MaxPPEM {1}, less than the previous range's {2}.",
        "The specification requires ranges sorted by increasing MaxPPEM. Unsorted ranges break the linear walk that finds the applicable behavior.");

    public static readonly DiagnosticDescriptor TerminalRangeMissing = new(
        "OT.gasp.missing-terminal-range", "Last range does not cover all larger sizes",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "The last range has MaxPPEM {0}, expected 0xFFFF.",
        "The final range's MaxPPEM must be 0xFFFF so that every ppem above the previous range's upper bound is covered.");

    public static readonly DiagnosticDescriptor Version0OnlyBitsInVersion0 = new(
        "OT.gasp.version-1-bits-in-version-0", "Version 1 behavior bits set in a version 0 table",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Range {0} sets bits 0x{1:X4}; SymmetricGridFit and SymmetricSmoothing are defined only in version 1.",
        "Version 0 only defines GridFit and DoGray. The two ClearType-related bits were added in version 1.");

    public static readonly DiagnosticDescriptor ReservedBehaviorBits = new(
        "OT.gasp.reserved-behavior-bits", "Reserved behavior bits set",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Range {0} has reserved bits 0x{1:X4} set (full value 0x{2:X4}).",
        "Bits 4–15 of the range's behavior field are unused and must be zero.");

    public static readonly DiagnosticDescriptor BehaviorMissingGridFit = new(
        "OT.gasp.no-grid-fit", "Range does not enable grid fitting",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "Range {0} (MaxPPEM {1}) does not set GridFit or SymmetricGridFit.",
        "Without a grid-fit flag the rasterizer will not apply hinting at this ppem range, even if the font carries hinting programs.");

    public static readonly DiagnosticDescriptor BehaviorMissingSmoothing = new(
        "OT.gasp.no-smoothing", "Range does not enable any smoothing",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "Range {0} (MaxPPEM {1}) does not set DoGray or SymmetricSmoothing.",
        "Without a smoothing flag the rasterizer will produce bilevel output at this ppem range.");

    public static readonly DiagnosticDescriptor DuplicateMaxPpem = new(
        "OT.gasp.duplicate-max-ppem", "Two ranges share a MaxPPEM",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "Ranges at index {0} and {1} both have MaxPPEM {2}.",
        "Each MaxPPEM is the upper bound of exactly one range. Duplicates make the applicable range ambiguous.");

    public static readonly DiagnosticDescriptor VeryLargeNumberOfRanges = new(
        "OT.gasp.many-ranges", "gasp declares an unusually large number of ranges",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "The table declares {0} ranges.",
        "Typical fonts have fewer than six ranges. Many small ranges increase the linear search cost when the rasterizer resolves behavior at a ppem.");

    private GaspTableAnalyzer() { }

    public static GaspTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var gasp = face.GetTable<GaspTable>();
        var tag = GaspTable.Tag;

        AnalyzeVersion(gasp, tag, bag);
        AnalyzeRanges(gasp, tag, bag);
    }

    private static void AnalyzeVersion(GaspTable gasp, Tag tag, DiagnosticBag bag)
    {
        if (gasp.Version is not (0 or 1))
        {
            bag.Add(VersionInvalid.Create(
                [gasp.Version],
                table: tag, field: nameof(GaspTable.Version)));
        }

        if (gasp.RangeCount == 0)
        {
            bag.Add(NoRanges.Create(
                [], table: tag, field: nameof(GaspTable.Ranges)));
        }
        else if (gasp.RangeCount > 8)
        {
            bag.Add(VeryLargeNumberOfRanges.Create(
                [gasp.RangeCount], table: tag, field: nameof(GaspTable.Ranges)));
        }
    }

    private static void AnalyzeRanges(GaspTable gasp, Tag tag, DiagnosticBag bag)
    {
        var ranges = gasp.Ranges;
        if (ranges.Count == 0) return;

        for (int i = 0; i < ranges.Count; i++)
        {
            GaspRange range = ranges[i];
            ushort behavior = range.RangeGaspBehavior;

            ushort reserved = (ushort)(behavior & ReservedBehaviorBitsConst);
            if (reserved != 0)
            {
                bag.Add(ReservedBehaviorBits.Create(
                    [i, reserved, behavior],
                    table: tag, field: $"ranges[{i}].RangeGaspBehavior"));
            }

            if (gasp.Version == 0 && (behavior & Version1OnlyBits) != 0)
            {
                bag.Add(Version0OnlyBitsInVersion0.Create(
                    [i, (ushort)(behavior & Version1OnlyBits)],
                    table: tag, field: $"ranges[{i}].RangeGaspBehavior"));
            }

            bool hasGridFit = (behavior & 0x0001) != 0 || (behavior & 0x0004) != 0;
            bool hasSmoothing = (behavior & 0x0002) != 0 || (behavior & 0x0008) != 0;

            if (!hasGridFit && behavior != 0)
            {
                bag.Add(BehaviorMissingGridFit.Create(
                    [i, range.MaxPPEM],
                    table: tag, field: $"ranges[{i}].RangeGaspBehavior"));
            }

            if (!hasSmoothing && behavior != 0)
            {
                bag.Add(BehaviorMissingSmoothing.Create(
                    [i, range.MaxPPEM],
                    table: tag, field: $"ranges[{i}].RangeGaspBehavior"));
            }

            if (i > 0)
            {
                GaspRange prev = ranges[i - 1];
                if (range.MaxPPEM < prev.MaxPPEM)
                {
                    bag.Add(RangesUnsorted.Create(
                        [i, range.MaxPPEM, prev.MaxPPEM],
                        table: tag, field: $"ranges[{i}].MaxPPEM"));
                }
                else if (range.MaxPPEM == prev.MaxPPEM)
                {
                    bag.Add(DuplicateMaxPpem.Create(
                        [i - 1, i, range.MaxPPEM],
                        table: tag, field: $"ranges[{i}].MaxPPEM"));
                }
            }
        }

        ushort lastMax = ranges[^1].MaxPPEM;
        if (lastMax != TerminalMaxPpem)
        {
            bag.Add(TerminalRangeMissing.Create(
                [lastMax],
                table: tag, field: $"ranges[{ranges.Count - 1}].MaxPPEM"));
        }
    }
}
