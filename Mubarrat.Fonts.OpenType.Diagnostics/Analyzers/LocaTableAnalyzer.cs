using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>loca</c> table.</summary>
/// <remarks>
/// <para>
/// <c>loca</c> is a dependent of <c>maxp</c> (for the glyph count), <c>head</c> (for the
/// offset format), and <c>glyf</c> (for the extent the last offset must match). The
/// count and format constraints are enforced in <see cref="LocaTable.Parse"/> and cannot
/// be observed here. What remains is the shape of the offset array itself: strictly
/// non-decreasing, anchored at zero, ending at the <c>glyf</c> length, and short enough
/// for the offset format in use.
/// </para>
/// <para>
/// <c>loca</c> is not optional when the font has TrueType outlines, so the presence
/// checks live in <c>CrossTableAnalyzer</c>, not here.
/// </para>
/// </remarks>
public class LocaTableAnalyzer : IFontAnalyzer
{
    /// <summary>
    /// The largest <c>glyf</c> size the short offset format can address. Offsets are
    /// stored as <c>uint16</c> values representing <c>actualOffset / 2</c>, so the largest
    /// representable offset is <c>0xFFFF * 2 = 131070</c> bytes.
    /// </summary>
    private const uint ShortFormatMaxOffset = 0xFFFF * 2;

    // ─────────────────────── Descriptors ───────────────────────

    public static readonly DiagnosticDescriptor FirstOffsetNonZero = new(
        "OT.loca.first-offset-nonzero", "First glyph does not start at offset 0",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "The first offset is {0}, expected 0.",
        "The first entry in loca is the offset of glyph 0 within glyf. Since glyph 0 is the first glyph stored, its offset must be 0.");

    public static readonly DiagnosticDescriptor OffsetsNotMonotonic = new(
        "OT.loca.offsets-not-monotonic", "Offset array is not non-decreasing",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Offset at index {0} is {1}, less than the previous offset {2}.",
        "Each glyph's offset must be greater than or equal to the previous glyph's, since glyphs are stored sequentially.");

    public static readonly DiagnosticDescriptor LastOffsetMismatch = new(
        "OT.loca.last-offset-vs-glyf", "Last offset disagrees with glyf table length",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "The final offset is {0} but the glyf table length is {1}.",
        "The last entry is the end of the final glyph, which must be the total length of the glyf table.");

    public static readonly DiagnosticDescriptor ShortFormatTooLarge = new(
        "OT.loca.short-format-too-large", "Short offsets used for an oversized glyf table",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "The last offset is {0}, which exceeds the short format's maximum of {1}.",
        "The short offset format stores offsets divided by 2 in a uint16, capping the addressable glyf size at 131070 bytes. A larger glyf requires the long format.");

    public static readonly DiagnosticDescriptor EmptyGlyphsPresent = new(
        "OT.loca.empty-glyphs", "Some glyphs have no outline data",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "{0} of {1} glyphs have zero length.",
        "Zero-length glyphs are legal (space, combining marks). A font where most glyphs are empty is usually a stub or a partially-generated font.");

    public static readonly DiagnosticDescriptor OffsetArrayLongerThanGlyphs = new(
        "OT.loca.offset-count-vs-maxp", "Offset count disagrees with maxp.numGlyphs",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "loca has {0} offsets but maxp.numGlyphs + 1 is {1}.",
        "The offset array must contain exactly one entry per glyph plus a sentinel.");

    // ─────────────────────── Singleton ───────────────────────

    private LocaTableAnalyzer() { }

    public static LocaTableAnalyzer Instance => field ??= new();

    // ─────────────────────── Analysis ───────────────────────

    /// <summary>Runs every loca rule against the face's <c>loca</c> table.</summary>
    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var loca = face.GetTable<LocaTable>();
        var tag = LocaTable.Tag;

        AnalyzeCount(face, loca, tag, bag);
        AnalyzeOffsetArray(loca, tag, bag);
        AnalyzeAgainstGlyf(face, loca, tag, bag);
    }

    // ─────────────────────── Rule groups ───────────────────────

    private static void AnalyzeCount(FontFace face, LocaTable loca, Tag tag, DiagnosticBag bag)
    {
        if (!face.Directory.ContainsKey(MaxpTable.Tag)) return;

        var maxp = face.GetTable<MaxpTable>();
        int expected = maxp.NumGlyphs + 1;
        if (loca.Offsets.Length != expected)
        {
            bag.Add(OffsetArrayLongerThanGlyphs.Create(
                [loca.Offsets.Length, expected],
                table: tag, field: nameof(LocaTable.Offsets)));
        }
    }

    private static void AnalyzeOffsetArray(LocaTable loca, Tag tag, DiagnosticBag bag)
    {
        uint[] offsets = loca.Offsets;
        if (offsets.Length == 0) return;

        if (offsets[0] != 0)
        {
            bag.Add(FirstOffsetNonZero.Create(
                [offsets[0]],
                table: tag, field: "offsets[0]"));
        }

        // Monotonic check. Report the first violation only — a badly damaged font
        // would otherwise produce one diagnostic per offset.
        for (int i = 1; i < offsets.Length; i++)
        {
            if (offsets[i] < offsets[i - 1])
            {
                bag.Add(OffsetsNotMonotonic.Create(
                    [i, offsets[i], offsets[i - 1]],
                    table: tag, field: $"offsets[{i}]"));
                break;
            }
        }

        // Short-format size bound. Checked against the largest offset, which is the
        // last entry (the array is non-decreasing when this passes).
        if (loca.Format == IndexToLocFormat.ShortOffsets)
        {
            uint maxOffset = offsets[^1];
            if (maxOffset > ShortFormatMaxOffset)
            {
                bag.Add(ShortFormatTooLarge.Create(
                    [maxOffset, ShortFormatMaxOffset],
                    table: tag, field: "offsets[^1]"));
            }
        }

        // Empty glyph count. This is informational — many fonts have spaces and
        // combining marks with no outline. The rule fires only when the count is
        // useful context, which is when the font declares glyphs.
        int empty = 0;
        for (int i = 0; i < offsets.Length - 1; i++)
        {
            if (offsets[i + 1] == offsets[i]) empty++;
        }

        if (empty > 0)
        {
            bag.Add(EmptyGlyphsPresent.Create(
                [empty, offsets.Length - 1],
                table: tag, field: nameof(LocaTable.Offsets)));
        }
    }

    private static void AnalyzeAgainstGlyf(FontFace face, LocaTable loca, Tag tag, DiagnosticBag bag)
    {
        if (!face.Directory.ContainsKey(GlyfTable.Tag)) return;

        long glyfLength = face.Directory[GlyfTable.Tag] is Sfnt.SfntTableRecord entry ? entry.Length : 0;
        uint lastOffset = loca.Offsets.Length > 0 ? loca.Offsets[^1] : 0;

        if (lastOffset != (uint)glyfLength)
        {
            bag.Add(LastOffsetMismatch.Create(
                [lastOffset, glyfLength],
                table: tag, field: "offsets[^1]"));
        }
    }
}
