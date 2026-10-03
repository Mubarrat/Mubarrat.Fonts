using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables.Metadata;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>kern</c> table.</summary>
/// <remarks>
/// <para>
/// The parser dispatches on the subtable format and preserves unknown formats verbatim.
/// What remains for analysis is the coherence of the subtable list: version, coverage
/// bit consistency, sortedness of format-0 pairs, class-table dimensions in format 2,
/// and the flag combination rules the spec defines.
/// </para>
/// <para>
/// CFF-flavored OpenType fonts must not include a <c>kern</c> table at all — GPOS is the
/// only supported mechanism. This analyzer cross-checks against the sfnt version when
/// available.
/// </para>
/// </remarks>
public class KernTableAnalyzer : IFontAnalyzer
{
    /// <summary>Bits 4–7 of the coverage field are reserved.</summary>
    private const ushort ReservedCoverageBits = 0x00F0;

    public static readonly DiagnosticDescriptor SubtableVersionNotZero = new(
        "OT.kern.subtable-version", "Subtable version is not 0",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Subtable {0} has version {1}; OpenType specifies 0.",
        "The Microsoft version of the kern table sets the subtable version to 0. Non-zero values indicate either a pre-OpenType Apple table or a generator error.");

    public static readonly DiagnosticDescriptor SubtableReservedBits = new(
        "OT.kern.subtable-reserved", "Subtable coverage has reserved bits set",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Subtable {0} coverage is 0x{1:X4}; bits 4–7 are reserved.",
        "Bits 4–7 of the coverage field are unused and must be zero.");

    public static readonly DiagnosticDescriptor UnknownSubtableFormat = new(
        "OT.kern.subtable-format", "Unrecognized subtable format",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Compatibility,
        "Subtable {0} uses format {1}, which is not defined.",
        "Only formats 0 and 2 are defined. Unknown formats are preserved for round-trip but resolve no kerning.");

    public static readonly DiagnosticDescriptor MinimumWithoutHorizontal = new(
        "OT.kern.minimum-without-horizontal", "Minimum flag set without Horizontal flag",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Subtable {0} has the Minimum bit set but not the Horizontal bit.",
        "The Minimum flag only affects horizontal kerning accumulation. With Horizontal clear, the flag has no effect.");

    public static readonly DiagnosticDescriptor OverrideAndMinimumConflict = new(
        "OT.kern.override-and-minimum", "Subtable sets both Override and Minimum",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "Subtable {0} sets both Override and Minimum.",
        "Override replaces the accumulated value; Minimum caps it. Setting both makes the subtable's contribution ambiguous.");

    public static readonly DiagnosticDescriptor PairsNotSorted = new(
        "OT.kern.pairs-unsorted", "Format 0 pairs are not sorted",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Pair at index {0} has key 0x{1:X8}, less than the previous key 0x{2:X8}.",
        "Format 0 pairs must be sorted by the combined (left << 16 | right) key so consumers can binary-search.");

    public static readonly DiagnosticDescriptor DuplicatePair = new(
        "OT.kern.duplicate-pair", "Two pairs share a key",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "Pairs at index {0} and {1} both have key 0x{2:X8} ({3}, {4}).",
        "Duplicate pairs make the kerning value ambiguous.");

    public static readonly DiagnosticDescriptor ClassTableUnsorted = new(
        "OT.kern.class-table-unsorted", "Class table covers glyphs out of order",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "Subtable {0}, {1} class table starts at glyph {2}, but the other class table starts at glyph {3}.",
        "While the spec does not require a particular order, the two class tables typically start at glyph 0. A non-zero start is unusual and worth flagging.");

    public static readonly DiagnosticDescriptor EmptyKerningArray = new(
        "OT.kern.empty-kerning-array", "Format 2 subtable has no kerning entries",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "Subtable {0} has a zero-length kerning array.",
        "A format 2 subtable with no kerning entries contributes nothing. The subtable should be omitted.");

    public static readonly DiagnosticDescriptor RowWidthTooSmall = new(
        "OT.kern.row-width", "Format 2 rowWidth is zero",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Subtable {0} declares rowWidth 0.",
        "The row width is the number of bytes per class row in the kerning array. Zero means the array cannot be indexed.");

    public static readonly DiagnosticDescriptor KernInCffFont = new(
        "OT.kern.in-cff-font", "kern present in a CFF-flavored font",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "The font uses CFF outlines but includes a kern table.",
        "CFF-flavored OpenType fonts must use GPOS for kerning. The kern table is only permitted in TrueType-flavored fonts.");

    public static readonly DiagnosticDescriptor AllSubtablesNonHorizontal = new(
        "OT.kern.no-horizontal", "No horizontal subtable",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "None of the {0} subtables declares horizontal kerning.",
        "Every defined subtable in this font is cross-stream. Consumers that only look for horizontal kerning will see no effect.");

    private KernTableAnalyzer() { }

    public static KernTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var kern = face.GetTable<KernTable>();
        var tag = KernTable.Tag;

        AnalyzeFlavor(face, kern, tag, bag);
        AnalyzeSubtables(kern, tag, bag);
    }

    private static void AnalyzeFlavor(FontFace face, KernTable kern, Tag tag, DiagnosticBag bag)
    {
        // sfnt version 0x4F54544F ("OTTO") indicates CFF outlines.
        if (face.SfntVersion == TableDirectory.SfntVersionCff)
        {
            bag.Add(KernInCffFont.Create(
                [], table: tag));
        }
    }

    private static void AnalyzeSubtables(KernTable kern, Tag tag, DiagnosticBag bag)
    {
        bool anyHorizontal = false;

        for (int i = 0; i < kern.Subtables.Count; i++)
        {
            KernSubtable sub = kern.Subtables[i];
            var subTag = tag;

            if (sub.Version != 0)
            {
                bag.Add(SubtableVersionNotZero.Create(
                    [i, sub.Version],
                    table: subTag, field: $"subtables[{i}].Version"));
            }

            ushort reserved = (ushort)(sub.Coverage & 0x00F0);
            if (reserved != 0)
            {
                bag.Add(SubtableReservedBits.Create(
                    [i, sub.Coverage],
                    table: subTag, field: $"subtables[{i}].Coverage"));
            }

            if (sub.Horizontal) anyHorizontal = true;

            if (sub.Minimum && !sub.Horizontal)
            {
                bag.Add(MinimumWithoutHorizontal.Create(
                    [i],
                    table: subTag, field: $"subtables[{i}].Coverage"));
            }

            if (sub.Minimum && sub.Override)
            {
                bag.Add(OverrideAndMinimumConflict.Create(
                    [i],
                    table: subTag, field: $"subtables[{i}].Coverage"));
            }

            switch (sub)
            {
                case KernUnknown unknown:
                    bag.Add(UnknownSubtableFormat.Create(
                        [i, (int)unknown.Format],
                        table: subTag, field: $"subtables[{i}].Coverage"));
                    break;

                case KernFormat0 f0:
                    AnalyzeFormat0(f0, i, subTag, bag);
                    break;

                case KernFormat2 f2:
                    AnalyzeFormat2(f2, i, subTag, bag);
                    break;
            }
        }

        if (kern.Subtables.Count > 0 && !anyHorizontal)
        {
            bag.Add(AllSubtablesNonHorizontal.Create(
                [kern.Subtables.Count],
                table: tag, field: nameof(KernTable.Subtables)));
        }
    }

    private static void AnalyzeFormat0(KernFormat0 sub, int index, Tag tag, DiagnosticBag bag)
    {
        var pairs = sub.Pairs;
        if (pairs.Count == 0) return;

        for (int j = 1; j < pairs.Count; j++)
        {
            uint prevKey = pairs[j - 1].Key;
            uint currKey = pairs[j].Key;

            if (currKey < prevKey)
            {
                bag.Add(PairsNotSorted.Create(
                    [j, currKey, prevKey],
                    table: tag, field: $"subtables[{index}].pairs[{j}]"));
            }
            else if (currKey == prevKey)
            {
                var a = pairs[j - 1];
                var b = pairs[j];
                bag.Add(DuplicatePair.Create(
                    [j - 1, j, currKey, b.Left, b.Right],
                    table: tag, field: $"subtables[{index}].pairs[{j}]"));
            }
        }
    }

    private static void AnalyzeFormat2(KernFormat2 sub, int index, Tag tag, DiagnosticBag bag)
    {
        if (sub.RowWidth == 0)
        {
            bag.Add(RowWidthTooSmall.Create(
                [index],
                table: tag, field: $"subtables[{index}].RowWidth"));
        }

        if (sub.KerningArray.Count == 0)
        {
            bag.Add(EmptyKerningArray.Create(
                [index],
                table: tag, field: $"subtables[{index}].KerningArray"));
        }

        if (sub.LeftClassTable.FirstGlyph != 0 || sub.RightClassTable.FirstGlyph != 0)
        {
            bag.Add(ClassTableUnsorted.Create(
                [index, "left", sub.LeftClassTable.FirstGlyph, sub.RightClassTable.FirstGlyph],
                table: tag, field: $"subtables[{index}]"));
        }
    }
}
