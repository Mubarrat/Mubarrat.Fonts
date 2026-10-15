using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>OS/2</c> table.</summary>
/// <remarks>
/// <para>
/// OS/2 covers five concerns: weight and width class, style bits in
/// <see cref="Os2Table.FsSelection"/>, vertical metrics for both typographic layout
/// (<c>sTypo*</c>) and Windows clipping (<c>usWin*</c>), Unicode and code page coverage,
/// and licensing in <see cref="Os2Table.FsType"/>. Each concern has its own rule group.
/// </para>
/// <para>
/// The version discriminant is enforced in <see cref="Os2Table.Parse"/>. Rules that
/// inspect version-gated fields first check the version and skip when the field is
/// absent — the trailing blocks are optional even when the version says they should be
/// present, because legacy generators truncate them.
/// </para>
/// <para>
/// Cross-table rules that compare OS/2 against <c>head</c>, <c>hhea</c>, <c>name</c>,
/// or <c>cmap</c> live in <c>CrossTableAnalyzer</c>.
/// </para>
/// </remarks>
public class Os2TableAnalyzer : IFontAnalyzer
{
    // ─────────────────────── FsSelection bit constants ───────────────────────

    private const ushort FsSelectionItalic = 0x0001;
    private const ushort FsSelectionUnderscore = 0x0002;
    private const ushort FsSelectionNegative = 0x0004;
    private const ushort FsSelectionOutlined = 0x0008;
    private const ushort FsSelectionStrikeout = 0x0010;
    private const ushort FsSelectionBold = 0x0020;
    private const ushort FsSelectionRegular = 0x0040;
    private const ushort FsSelectionUseTypoMetrics = 0x0080;
    private const ushort FsSelectionWws = 0x0100;
    private const ushort FsSelectionOblique = 0x0200;

    /// <summary>Reserved bits in fsSelection: bits 10–15.</summary>
    private const ushort FsSelectionReserved = 0xFC00;

    /// <summary>Bits that describe a specific style and are mutually constrained.</summary>
    private const ushort FsSelectionStyleBits = 0x0261;   // italic, bold, regular, oblique

    // ─────────────────────── FsType bit constants ───────────────────────

    /// <summary>Reserved bits in fsType: bits 5–15.</summary>
    private const ushort FsTypeReserved = 0xFFE0;

    // ─────────────────────── Descriptors ───────────────────────

    public static readonly DiagnosticDescriptor WeightClassOutOfRange = new(
        "OT.os2.weight-class", "Weight class out of range",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "usWeightClass is {0}, expected 1 through 1000.",
        "The weight class maps to the wght axis in variable fonts and to a specific style name in static fonts. Values outside the range are undefined.");

    public static readonly DiagnosticDescriptor WidthClassOutOfRange = new(
        "OT.os2.width-class", "Width class out of range",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "usWidthClass is {0}, expected 1 through 9.",
        "The width class maps to the wdth axis in variable fonts and to a specific condensed/expanded style. Values outside 1–9 are undefined.");

    public static readonly DiagnosticDescriptor CharRangeInverted = new(
        "OT.os2.char-range-inverted", "Character range is inverted",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "usFirstCharIndex is 0x{0:X4} but usLastCharIndex is 0x{1:X4}.",
        "The first character index must not exceed the last. An inverted range breaks consumers that use these values to bound cmap enumeration.");

    public static readonly DiagnosticDescriptor ItalicAndObliqueSet = new(
        "OT.os2.fs-selection-italic-oblique", "Both italic and oblique bits set",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "fsSelection has both ITALIC (bit 0) and OBLIQUE (bit 9) set.",
        "The two bits describe different styles and are mutually exclusive. Consumers that check either bit will classify the font inconsistently.");

    public static readonly DiagnosticDescriptor BoldAndRegularSet = new(
        "OT.os2.fs-selection-bold-regular", "Both bold and regular bits set",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "fsSelection has both BOLD (bit 5) and REGULAR (bit 6) set.",
        "BOLD and REGULAR are mutually exclusive. A font that declares both is ambiguous.");

    public static readonly DiagnosticDescriptor NoStyleBitsSet = new(
        "OT.os2.fs-selection-no-style", "No style bits set in fsSelection",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "fsSelection has none of ITALIC, BOLD, OBLIQUE, or REGULAR set.",
        "Every font must declare at least one style bit. A font with none is treated as regular by some consumers and unclassified by others.");

    public static readonly DiagnosticDescriptor FsSelectionReservedBits = new(
        "OT.os2.fs-selection-reserved", "Reserved fsSelection bits set",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "fsSelection has reserved bits 0x{0:X4} set (full value 0x{1:X4}).",
        "Bits 10–15 of fsSelection are reserved and must be zero.");

    public static readonly DiagnosticDescriptor FsTypeReservedBits = new(
        "OT.os2.fs-type-reserved", "Reserved fsType bits set",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "fsType has reserved bits 0x{0:X4} set (full value 0x{1:X4}).",
        "Bits 5–15 of fsType are reserved and must be zero.");

    public static readonly DiagnosticDescriptor FsTypeNoEmbeddingPermissions = new(
        "OT.os2.fs-type-permission-unset", "fsType declares no embedding permission",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "fsType has none of the RESTRICTED, PREVIEW_AND_PRINT, or EDITABLE bits set.",
        "Every font must declare at least one embedding permission. A value of zero means the font is unlicensed for any embedding, which is usually a generator bug.");

    public static readonly DiagnosticDescriptor AscenderBelowDescender = new(
        "OT.os2.typographic-ascender-below-descender", "Typographic ascender below descender",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "sTypoAscender is {0}, sTypoDescender is {1}; the ascender should be greater.",
        "By convention the ascender is positive and the descender negative. An ascender below the descender produces negative line heights when typographic metrics are in use.");

    public static readonly DiagnosticDescriptor WinMetricsZero = new(
        "OT.os2.win-metrics-zero", "Windows ascent and descent are both zero",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "usWinAscent and usWinDescent are both 0.",
        "The Windows metrics are unsigned clipping bounds. Both being zero causes the entire glyph to be clipped in some renderers.");

    public static readonly DiagnosticDescriptor StrikeoutNegativeSize = new(
        "OT.os2.strikeout-size-negative", "Negative strikeout size",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "yStrikeoutSize is {0}; strikeout stroke width should be non-negative.",
        "A negative strikeout size produces a stroke with no thickness or draws in the wrong direction.");

    public static readonly DiagnosticDescriptor StrikeoutZeroSizeWithPosition = new(
        "OT.os2.strikeout-zero-size", "Strikeout position set but size is zero",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "yStrikeoutPosition is {0} but yStrikeoutSize is 0.",
        "A non-zero strikeout position with a zero size means the field was partially populated. Either both should be set or both should be zero.");

    public static readonly DiagnosticDescriptor SubscriptSizeNegative = new(
        "OT.os2.subscript-size-negative", "Negative subscript size",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "ySubscript{0}Size is {1}; subscript sizes should be non-negative.",
        "Subscript glyph dimensions are magnitudes and must be positive.");

    public static readonly DiagnosticDescriptor SuperscriptSizeNegative = new(
        "OT.os2.superscript-size-negative", "Negative superscript size",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "ySuperscript{0}Size is {1}; superscript sizes should be non-negative.",
        "Superscript glyph dimensions are magnitudes and must be positive.");

    public static readonly DiagnosticDescriptor UnicodeRangeAllZero = new(
        "OT.os2.unicode-range-all-zero", "Unicode range bitfields are all zero",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "ulUnicodeRange1 through ulUnicodeRange4 are all 0.",
        "The Unicode range bitfields describe which script blocks the font covers. A font with any characters set should have at least one bit set.");

    public static readonly DiagnosticDescriptor VendorIdInvalidChars = new(
        "OT.os2.vendor-id-invalid-chars", "Vendor ID contains non-printable characters",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "achVendID contains a non-printable character (0x{0:X2}) at index {1}.",
        "The vendor ID is a four-character ASCII tag. Non-printable characters usually indicate a generator bug.");

    public static readonly DiagnosticDescriptor VendorIdUnknown = new(
        "OT.os2.vendor-id-unknown", "Vendor ID is all spaces or all zeros",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Consistency,
        "achVendID is '{0}'.",
        "The vendor ID is typically a registered four-character tag. A tag of all spaces or all zero bytes provides no information.");

    public static readonly DiagnosticDescriptor TypoMetricsNotPreferred = new(
        "OT.os2.use-typo-metrics-not-set", "Typographic metrics are not preferred",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "fsSelection does not have USE_TYPO_METRICS set; Windows will use usWinAscent/usWinDescent for line layout.",
        "Setting USE_TYPO_METRICS makes Windows use the sTypo* values for line spacing. Without it, line layout uses the larger usWin* values, which usually produces inconsistent spacing across platforms.");

    public static readonly DiagnosticDescriptor WinMetricsLargerThanTypo = new(
        "OT.os2.win-metrics-larger-than-typo", "Windows metrics exceed typographic metrics",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "usWinAscent ({0}) exceeds sTypoAscender ({1}) or usWinDescent ({2}) exceeds -sTypoDescender ({3}).",
        "Some clipping is expected — usWin* bounds the visible extent, sTypo* bounds the line box. A large gap usually indicates the font was designed for macOS line spacing and not adjusted for Windows.");

    public static readonly DiagnosticDescriptor XHeightZero = new(
        "OT.os2.x-height-zero", "x-height is zero",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Consistency,
        "sxHeight is 0.",
        "The x-height is the baseline-to-x-height distance and is used by some layout engines for optical size selection. Zero usually indicates an unpopulated field.");

    public static readonly DiagnosticDescriptor CapHeightZero = new(
        "OT.os2.cap-height-zero", "Cap height is zero",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Consistency,
        "sCapHeight is 0.",
        "The cap height is used by some layout engines and by CSS font-metrics overrides. Zero usually indicates an unpopulated field.");

    public static readonly DiagnosticDescriptor VersionTooOld = new(
        "OT.os2.version-outdated", "OS/2 version predates v4",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "version is {0}; version 4 is the current recommendation.",
        "Version 4 standardised USE_TYPO_METRICS and OBLIQUE. Fonts at version 0–3 may be handled inconsistently by modern layout engines.");

    // ─────────────────────── Singleton ───────────────────────

    private Os2TableAnalyzer() { }

    public static Os2TableAnalyzer Instance => field ??= new();

    // ─────────────────────── Analysis ───────────────────────

    /// <summary>Runs every OS/2 rule against the face's <c>OS/2</c> table.</summary>
    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var os2 = face.GetTable<Os2Table>();
        var tag = Os2Table.Tag;

        AnalyzeWeightAndWidth(os2, tag, bag);
        AnalyzeCharacterRange(os2, tag, bag);
        AnalyzeFsSelection(os2, tag, bag);
        AnalyzeFsType(os2, tag, bag);
        AnalyzeVerticalMetrics(os2, tag, bag);
        AnalyzeStrikeout(os2, tag, bag);
        AnalyzeSubSuperscript(os2, tag, bag);
        AnalyzeCoverage(os2, tag, bag);
        AnalyzeVendorId(os2, tag, bag);
        AnalyzeVersionGated(os2, tag, bag);
    }

    // ─────────────────────── Rule groups ───────────────────────

    private static void AnalyzeWeightAndWidth(Os2Table os2, Tag tag, DiagnosticBag bag)
    {
        if ((int)os2.UsWeightClass < 1 || (int)os2.UsWeightClass > 1000)
            bag.Add(WeightClassOutOfRange.Create(
                [os2.UsWeightClass],
                table: tag, field: nameof(Os2Table.UsWeightClass),
                span: new SourceSpan(4, 2)));

        if ((int)os2.UsWidthClass < 1 || (int)os2.UsWidthClass > 9)
            bag.Add(WidthClassOutOfRange.Create(
                [os2.UsWidthClass],
                table: tag, field: nameof(Os2Table.UsWidthClass),
                span: new SourceSpan(6, 2)));
    }

    private static void AnalyzeCharacterRange(Os2Table os2, Tag tag, DiagnosticBag bag)
    {
        // 0xFFFF in both fields is the "no characters" sentinel and is not an inverted
        // range.
        bool noChars = os2.UsFirstCharIndex == 0xFFFF && os2.UsLastCharIndex == 0xFFFF;
        if (!noChars && os2.UsFirstCharIndex > os2.UsLastCharIndex)
        {
            bag.Add(CharRangeInverted.Create(
                [os2.UsFirstCharIndex, os2.UsLastCharIndex],
                table: tag, field: "usFirstCharIndex/usLastCharIndex",
                span: new SourceSpan(64, 4)));
        }
    }

    private static void AnalyzeFsSelection(Os2Table os2, Tag tag, DiagnosticBag bag)
    {
        ushort raw = (ushort)os2.FsSelection;
        var span = new SourceSpan(62, 2);

        bool italic = (raw & FsSelectionItalic) != 0;
        bool oblique = (raw & FsSelectionOblique) != 0;
        bool bold = (raw & FsSelectionBold) != 0;
        bool regular = (raw & FsSelectionRegular) != 0;

        if (italic && oblique)
            bag.Add(ItalicAndObliqueSet.Create([], table: tag, field: nameof(Os2Table.FsSelection), span: span));

        if (bold && regular)
            bag.Add(BoldAndRegularSet.Create([], table: tag, field: nameof(Os2Table.FsSelection), span: span));

        if (!italic && !oblique && !bold && !regular)
            bag.Add(NoStyleBitsSet.Create([], table: tag, field: nameof(Os2Table.FsSelection), span: span));

        ushort reserved = (ushort)(raw & FsSelectionReserved);
        if (reserved != 0)
            bag.Add(FsSelectionReservedBits.Create(
                [reserved, raw],
                table: tag, field: nameof(Os2Table.FsSelection), span: span));

        if (!os2.UseTypoMetrics)
            bag.Add(TypoMetricsNotPreferred.Create([], table: tag, field: nameof(Os2Table.FsSelection), span: span));
    }

    private static void AnalyzeFsType(Os2Table os2, Tag tag, DiagnosticBag bag)
    {
        ushort raw = (ushort)os2.FsType;
        var span = new SourceSpan(8, 2);

        ushort reserved = (ushort)(raw & FsTypeReserved);
        if (reserved != 0)
            bag.Add(FsTypeReservedBits.Create(
                [reserved, raw],
                table: tag, field: nameof(Os2Table.FsType), span: span));

        // The three permission bits are bits 0, 1, 2. If all three are clear, the font
        // declares no embedding permission.
        if ((raw & 0x0007) == 0)
            bag.Add(FsTypeNoEmbeddingPermissions.Create(
                [], table: tag, field: nameof(Os2Table.FsType), span: span));
    }

    private static void AnalyzeVerticalMetrics(Os2Table os2, Tag tag, DiagnosticBag bag)
    {
        if (os2.STypoAscender < os2.STypoDescender)
        {
            bag.Add(AscenderBelowDescender.Create(
                [os2.STypoAscender, os2.STypoDescender],
                table: tag, field: "sTypoAscender/sTypoDescender",
                span: new SourceSpan(68, 4)));
        }

        if (os2.UsWinAscent == 0 && os2.UsWinDescent == 0)
        {
            bag.Add(WinMetricsZero.Create(
                [],
                table: tag, field: "usWinAscent/usWinDescent",
                span: new SourceSpan(74, 4)));
        }

        // Compare only when both metric groups are populated. Fires as information
        // because a large gap is a real design decision (macOS-oriented line spacing),
        // not a bug.
        int typoAscent = os2.STypoAscender;
        int typoDescent = -os2.STypoDescender;
        if (os2.UsWinAscent > typoAscent || os2.UsWinDescent > typoDescent)
        {
            bag.Add(WinMetricsLargerThanTypo.Create(
                [os2.UsWinAscent, typoAscent, os2.UsWinDescent, typoDescent],
                table: tag, field: "winMetrics",
                span: new SourceSpan(74, 4)));
        }
    }

    private static void AnalyzeStrikeout(Os2Table os2, Tag tag, DiagnosticBag bag)
    {
        if (os2.YStrikeoutSize < 0)
        {
            bag.Add(StrikeoutNegativeSize.Create(
                [os2.YStrikeoutSize],
                table: tag, field: nameof(Os2Table.YStrikeoutSize),
                span: new SourceSpan(26, 2)));
        }

        if (os2.YStrikeoutSize == 0 && os2.YStrikeoutPosition != 0)
        {
            bag.Add(StrikeoutZeroSizeWithPosition.Create(
                [os2.YStrikeoutPosition],
                table: tag, field: nameof(Os2Table.YStrikeoutSize),
                span: new SourceSpan(26, 2)));
        }
    }

    private static void AnalyzeSubSuperscript(Os2Table os2, Tag tag, DiagnosticBag bag)
    {
        if (os2.YSubscriptXSize < 0)
            bag.Add(SubscriptSizeNegative.Create(
                ["X", os2.YSubscriptXSize],
                table: tag, field: nameof(Os2Table.YSubscriptXSize),
                span: new SourceSpan(10, 2)));

        if (os2.YSubscriptYSize < 0)
            bag.Add(SubscriptSizeNegative.Create(
                ["Y", os2.YSubscriptYSize],
                table: tag, field: nameof(Os2Table.YSubscriptYSize),
                span: new SourceSpan(12, 2)));

        if (os2.YSuperscriptXSize < 0)
            bag.Add(SuperscriptSizeNegative.Create(
                ["X", os2.YSuperscriptXSize],
                table: tag, field: nameof(Os2Table.YSuperscriptXSize),
                span: new SourceSpan(18, 2)));

        if (os2.YSuperscriptYSize < 0)
            bag.Add(SuperscriptSizeNegative.Create(
                ["Y", os2.YSuperscriptYSize],
                table: tag, field: nameof(Os2Table.YSuperscriptYSize),
                span: new SourceSpan(20, 2)));
    }

    private static void AnalyzeCoverage(Os2Table os2, Tag tag, DiagnosticBag bag)
    {
        if (os2.UlUnicodeRange1 == 0 && os2.UlUnicodeRange2 == 0 &&
            os2.UlUnicodeRange3 == 0 && os2.UlUnicodeRange4 == 0)
        {
            bag.Add(UnicodeRangeAllZero.Create(
                [],
                table: tag, field: "ulUnicodeRange",
                span: new SourceSpan(42, 16)));
        }
    }

    private static void AnalyzeVendorId(Os2Table os2, Tag tag, DiagnosticBag bag)
    {
        uint raw = (uint)os2.AchVendId;

        // Reconstruct the four bytes; check each for printable ASCII.
        for (int i = 0; i < 4; i++)
        {
            byte b = (byte)(raw >> (24 - i * 8));
            if (b is < 0x20 or > 0x7E)
            {
                bag.Add(VendorIdInvalidChars.Create(
                    [b, i],
                    table: tag, field: nameof(Os2Table.AchVendId),
                    span: new SourceSpan(58, 4)));
                break;   // one diagnostic is enough
            }
        }

        // All spaces or all zeros: no information. Report as information.
        if (raw == 0x20202020u || raw == 0u)
        {
            bag.Add(VendorIdUnknown.Create(
                [os2.AchVendId.ToString()],
                table: tag, field: nameof(Os2Table.AchVendId),
                span: new SourceSpan(58, 4)));
        }
    }

    private static void AnalyzeVersionGated(Os2Table os2, Tag tag, DiagnosticBag bag)
    {
        if (os2.Version < 4)
            bag.Add(VersionTooOld.Create(
                [os2.Version],
                table: tag, field: nameof(Os2Table.Version),
                span: new SourceSpan(0, 2)));

        // Version 2+ fields. Guard against absence because legacy generators truncate.
        if (os2.SxHeight is 0)
            bag.Add(XHeightZero.Create(
                [],
                table: tag, field: nameof(Os2Table.SxHeight),
                span: new SourceSpan(86, 2)));

        if (os2.SCapHeight is 0)
            bag.Add(CapHeightZero.Create(
                [],
                table: tag, field: nameof(Os2Table.SCapHeight),
                span: new SourceSpan(88, 2)));
    }
}
