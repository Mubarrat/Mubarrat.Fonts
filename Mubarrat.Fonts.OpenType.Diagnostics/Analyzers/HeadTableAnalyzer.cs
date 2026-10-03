using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables;
using Mubarrat.Fonts.OpenType.Tables.Outlines;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>head</c> table.</summary>
/// <remarks>
/// <para>
/// Covers the field-range constraints from the OpenType specification plus a small
/// number of "well-formed but suspicious" rules that fire on real fonts and are worth
/// surfacing at information or warning severity.
/// </para>
/// <para>
/// Cross-table rules that involve <c>head</c> (for example: a variable font with
/// TrueType outlines must set <see cref="HeadFlags.LeftSidebearingAtX0"/>).
/// </para>
/// </remarks>
public class HeadTableAnalyzer : IFontAnalyzer
{
    // ─────────────────────── Descriptors ───────────────────────

    public static readonly DiagnosticDescriptor MagicMismatch = new(
        "OT.head.magic-number", "Invalid magic number",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "magicNumber is 0x{0:X8}, expected 0x{1:X8}.",
        "The head table must contain the magic number 0x5F0F3CF5. Fonts with a different value are malformed and should be rejected.");

    public static readonly DiagnosticDescriptor MajorVersionInvalid = new(
        "OT.head.major-version", "Unexpected majorVersion",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "majorVersion is {0}, expected 1.",
        "Some tools reject fonts with a major version other than 1. The specification reserves future versions but none are defined.");

    public static readonly DiagnosticDescriptor MinorVersionInvalid = new(
        "OT.head.minor-version", "Unexpected minorVersion",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Specification,
        "minorVersion is {0}, expected 0.",
        "Every published OpenType revision specifies minorVersion 0 for the head table.");

    public static readonly DiagnosticDescriptor UnitsPerEmOutOfRange = new(
        "OT.head.units-per-em", "Units per em out of range",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "unitsPerEm is {0}, expected a value in [{1}, {2}].",
        "The spec requires unitsPerEm between 16 and 16384. Values outside this range cause downstream scaling errors.");

    public static readonly DiagnosticDescriptor UnitsPerEmNotPowerOfTwo = new(
        "OT.head.units-per-em-not-power-of-two", "unitsPerEm is not a power of two",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "unitsPerEm is {0}; a power of two is recommended for TrueType outlines.",
        "The specification recommends a power of two for fonts with TrueType outlines, as it enables performance optimizations in some rasterizers. This is a recommendation, not a requirement.");

    public static readonly DiagnosticDescriptor BoundingBoxInverted = new(
        "OT.head.bbox-inverted", "Font bounding box inverted",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "bounding box is inverted: ({0}, {1}) – ({2}, {3}).",
        "Some rasterizers handle an inverted bounding box; others reject the font. The values are usually a bug in the font generator.");

    public static readonly DiagnosticDescriptor BoundingBoxZero = new(
        "OT.head.bbox-zero", "Font bounding box is all zeros",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "All four bounding box values are zero.",
        "A zero bounding box usually indicates an unpopulated or stub font. Glyphs with contours should produce a non-zero extent.");

    public static readonly DiagnosticDescriptor BoundingBoxEmpty = new(
        "OT.head.bbox-empty", "Font bounding box has zero extent",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Consistency,
        "bounding box has zero {0}: XMin == XMax is {1}, YMin == YMax is {2}.",
        "A zero-width or zero-height bounding box is unusual but permitted when no glyph has contours on the corresponding axis.");

    public static readonly DiagnosticDescriptor IndexToLocFormatInvalid = new(
        "OT.head.index-to-loc-format", "Invalid indexToLocFormat",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "indexToLocFormat is {0}, expected 0 (short) or 1 (long).",
        "loca cannot be parsed without a valid discriminant. Values outside 0 and 1 break every glyph lookup.");

    public static readonly DiagnosticDescriptor GlyphDataFormatInvalid = new(
        "OT.head.glyph-data-format", "Invalid glyphDataFormat",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "glyphDataFormat is {0}, expected 0.");

    public static readonly DiagnosticDescriptor ReservedFlagBitsSet = new(
        "OT.head.flags-reserved", "Reserved head flag bits set",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "flags has reserved bits 0x{0:X4} set (full value 0x{1:X4}).",
        "Bits 5–10 and 14–15 of the head flags field are reserved and must be zero.");

    public static readonly DiagnosticDescriptor ReservedMacStyleBitsSet = new(
        "OT.head.mac-style-reserved", "Reserved macStyle bits set",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "macStyle has reserved bits 0x{0:X4} set (full value 0x{1:X4}).",
        "Bits 7–15 of the macStyle field are reserved and must be zero.");

    public static readonly DiagnosticDescriptor FontDirectionHintOutOfRange = new(
        "OT.head.font-direction-hint", "fontDirectionHint out of range",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "fontDirectionHint is {0}, expected a value in [-2, 2].",
        "The font direction hint is deprecated. The specification defines values -2 through 2 and recommends 2 (LikeLeftToRightWithNeutrals).");

    public static readonly DiagnosticDescriptor LowestRecPPEMZero = new(
        "OT.head.lowest-rec-ppem-zero", "lowestRecPPEM is zero",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "lowestRecPPEM is 0.",
        "The smallest readable size cannot be zero. A value of zero usually indicates an unpopulated field.");

    public static readonly DiagnosticDescriptor FontRevisionZero = new(
        "OT.head.font-revision-zero", "fontRevision is zero",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Consistency,
        "fontRevision is 0.0.",
        "fontRevision is set by the font manufacturer. A value of zero provides no revision information and usually indicates an unpopulated field.");

    public static readonly DiagnosticDescriptor CreatedBeforeEpoch = new(
        "OT.head.created-before-epoch", "Creation date predates the Unix epoch",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "created is {0:u}, which is before 1970-01-01.",
        "The head table uses the 1904 epoch; values that convert to before 1970 are usually a bug in the font generator.");

    public static readonly DiagnosticDescriptor CreatedInFuture = new(
        "OT.head.created-in-future", "Creation date is in the future",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "created is {0:u}, which is after {1:u}.",
        "A creation date far in the future usually indicates an uninitialized or miscalculated value.");

    public static readonly DiagnosticDescriptor ModifiedBeforeCreated = new(
        "OT.head.modified-before-created", "Modification date precedes creation date",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "modified is {0:u}, which precedes created {1:u}.",
        "The modification timestamp should be greater than or equal to the creation timestamp.");

    // ─────────────────────── Constants ───────────────────────

    /// <summary>Reserved bits in <c>head.flags</c>: bits 5–10 (0x07E0) and 14–15 (0xC000).</summary>
    private const ushort ReservedFlagBits = 0xC7E0;

    /// <summary>Reserved bits in <c>head.macStyle</c>: bits 7–15 (0xFF80).</summary>
    private const ushort ReservedMacStyleBits = 0xFF80;

    /// <summary>Lower bound for a plausible creation or modification date.</summary>
    private static readonly DateTime MinReasonableDate = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Upper bound for a plausible creation or modification date.</summary>
    private static readonly DateTime MaxReasonableDate = new(2070, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ─────────────────────── Singleton ───────────────────────

    private HeadTableAnalyzer() { }

    public static HeadTableAnalyzer Instance => field ??= new();

    // ─────────────────────── Analysis ───────────────────────

    /// <summary>Runs every head rule against the face's <c>head</c> table.</summary>
    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var head = face.GetTable<HeadTable>();
        var tag = HeadTable.Tag;

        AnalyzeVersion(head, tag, bag);
        AnalyzeUnits(head, tag, bag);
        AnalyzeBoundingBox(head, tag, bag);
        AnalyzeFormatDiscriminants(head, tag, bag);
        AnalyzeFlags(head, tag, bag);
        AnalyzeMacStyle(head, tag, bag);
        AnalyzeFontDirectionHint(head, tag, bag);
        AnalyzeLowestRecPPEM(head, tag, bag);
        AnalyzeFontRevision(head, tag, bag);
        AnalyzeDates(head, tag, bag);
    }

    // ─────────────────────── Rule groups ───────────────────────

    private static void AnalyzeVersion(HeadTable head, Tag tag, DiagnosticBag bag)
    {
        if (head.MagicNumber != HeadTable.Magic)
            bag.Add(MagicMismatch.Create(
                [head.MagicNumber, HeadTable.Magic],
                table: tag, field: nameof(HeadTable.MagicNumber),
                span: new SourceSpan(12, 4)));

        if (head.MajorVersion != 1)
            bag.Add(MajorVersionInvalid.Create(
                [head.MajorVersion],
                table: tag, field: nameof(HeadTable.MajorVersion),
                span: new SourceSpan(0, 2)));

        if (head.MinorVersion != 0)
            bag.Add(MinorVersionInvalid.Create(
                [head.MinorVersion],
                table: tag, field: nameof(HeadTable.MinorVersion),
                span: new SourceSpan(2, 2)));
    }

    private static void AnalyzeUnits(HeadTable head, Tag tag, DiagnosticBag bag)
    {
        if (head.UnitsPerEm < HeadTable.MinUnitsPerEm || head.UnitsPerEm > HeadTable.MaxUnitsPerEm)
            bag.Add(UnitsPerEmOutOfRange.Create(
                [head.UnitsPerEm, HeadTable.MinUnitsPerEm, HeadTable.MaxUnitsPerEm],
                table: tag, field: nameof(HeadTable.UnitsPerEm),
                span: new SourceSpan(18, 2)));

        // Power-of-two check. Only fires when the range check passed, to avoid
        // duplicate diagnostics on a unitsPerEm of, say, 0.
        if (head.UnitsPerEm >= HeadTable.MinUnitsPerEm &&
            head.UnitsPerEm <= HeadTable.MaxUnitsPerEm &&
            (head.UnitsPerEm & (head.UnitsPerEm - 1)) != 0)
        {
            bag.Add(UnitsPerEmNotPowerOfTwo.Create(
                [head.UnitsPerEm],
                table: tag, field: nameof(HeadTable.UnitsPerEm),
                span: new SourceSpan(18, 2)));
        }
    }

    private static void AnalyzeBoundingBox(HeadTable head, Tag tag, DiagnosticBag bag)
    {
        var span = new SourceSpan(36, 8);

        if (head.XMin > head.XMax || head.YMin > head.YMax)
        {
            bag.Add(BoundingBoxInverted.Create(
                [head.XMin, head.YMin, head.XMax, head.YMax],
                table: tag, field: "boundingBox", span: span));
            return;   // further bbox rules would double-report
        }

        bool allZero = head.XMin == 0 && head.YMin == 0 && head.XMax == 0 && head.YMax == 0;
        if (allZero)
        {
            bag.Add(BoundingBoxZero.Create(
                [],
                table: tag, field: "boundingBox", span: span));
            return;
        }

        bool zeroWidth = head.XMin == head.XMax;
        bool zeroHeight = head.YMin == head.YMax;
        if (zeroWidth || zeroHeight)
        {
            string axis = zeroWidth && zeroHeight ? "width and height"
                        : zeroWidth ? "width"
                        : "height";
            bag.Add(BoundingBoxEmpty.Create(
                [axis, zeroWidth, zeroHeight],
                table: tag, field: "boundingBox", span: span));
        }
    }

    private static void AnalyzeFormatDiscriminants(HeadTable head, Tag tag, DiagnosticBag bag)
    {
        if (head.IndexToLocFormat is not (IndexToLocFormat.ShortOffsets or IndexToLocFormat.LongOffsets))
            bag.Add(IndexToLocFormatInvalid.Create(
                [(short)head.IndexToLocFormat],
                table: tag, field: nameof(HeadTable.IndexToLocFormat),
                span: new SourceSpan(50, 2)));

        if (head.GlyphDataFormat != GlyphDataFormat.Current)
            bag.Add(GlyphDataFormatInvalid.Create(
                [(short)head.GlyphDataFormat],
                table: tag, field: nameof(HeadTable.GlyphDataFormat),
                span: new SourceSpan(52, 2)));
    }

    private static void AnalyzeFlags(HeadTable head, Tag tag, DiagnosticBag bag)
    {
        ushort raw = (ushort)head.Flags;
        ushort reserved = (ushort)(raw & ReservedFlagBits);
        if (reserved != 0)
            bag.Add(ReservedFlagBitsSet.Create(
                [reserved, raw],
                table: tag, field: nameof(HeadTable.Flags),
                span: new SourceSpan(16, 2)));
    }

    private static void AnalyzeMacStyle(HeadTable head, Tag tag, DiagnosticBag bag)
    {
        ushort raw = (ushort)head.MacStyle;
        ushort reserved = (ushort)(raw & ReservedMacStyleBits);
        if (reserved != 0)
            bag.Add(ReservedMacStyleBitsSet.Create(
                [reserved, raw],
                table: tag, field: nameof(HeadTable.MacStyle),
                span: new SourceSpan(44, 2)));
    }

    private static void AnalyzeFontDirectionHint(HeadTable head, Tag tag, DiagnosticBag bag)
    {
        short value = (short)head.FontDirectionHint;
        if (value is < -2 or > 2)
            bag.Add(FontDirectionHintOutOfRange.Create(
                [value],
                table: tag, field: nameof(HeadTable.FontDirectionHint),
                span: new SourceSpan(48, 2)));
    }

    private static void AnalyzeLowestRecPPEM(HeadTable head, Tag tag, DiagnosticBag bag)
    {
        if (head.LowestRecPPEM == 0)
            bag.Add(LowestRecPPEMZero.Create(
                [],
                table: tag, field: nameof(HeadTable.LowestRecPPEM),
                span: new SourceSpan(46, 2)));
    }

    private static void AnalyzeFontRevision(HeadTable head, Tag tag, DiagnosticBag bag)
    {
        if (head.FontRevision == default)
            bag.Add(FontRevisionZero.Create(
                [],
                table: tag, field: nameof(HeadTable.FontRevision),
                span: new SourceSpan(4, 4)));
    }

    private static void AnalyzeDates(HeadTable head, Tag tag, DiagnosticBag bag)
    {
        if (head.Created < MinReasonableDate)
            bag.Add(CreatedBeforeEpoch.Create(
                [head.Created],
                table: tag, field: nameof(HeadTable.Created),
                span: new SourceSpan(20, 8)));

        if (head.Created > MaxReasonableDate)
            bag.Add(CreatedInFuture.Create(
                [head.Created, MaxReasonableDate],
                table: tag, field: nameof(HeadTable.Created),
                span: new SourceSpan(20, 8)));

        if (head.Modified < head.Created)
            bag.Add(ModifiedBeforeCreated.Create(
                [head.Modified, head.Created],
                table: tag, field: nameof(HeadTable.Modified),
                span: new SourceSpan(28, 8)));
    }
}
