using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>PCLT</c> table.</summary>
/// <remarks>
/// <para>
/// PCLT is metadata for HP PCL 5 printers. It carries no information a shaper or
/// rasterizer uses. The rules here check the internal consistency the specification
/// requires: version, reserved byte, and the ranges of the two signed style fields.
/// </para>
/// <para>
/// The table is deprecated. Almost no consumer reads it, and a font without one is
/// perfectly valid.
/// </para>
/// </remarks>
public class PcltTableAnalyzer : IFontAnalyzer
{
    public static readonly DiagnosticDescriptor MajorVersionInvalid = new(
        "OT.pclt.major-version", "Unexpected PCLT major version",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "majorVersion is {0}, expected 1.",
        "Every published revision of the PCLT table specifies major version 1.");

    public static readonly DiagnosticDescriptor MinorVersionNonZero = new(
        "OT.pclt.minor-version", "Unexpected PCLT minor version",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Specification,
        "minorVersion is {0}, expected 0.",
        "Every published revision of the PCLT table specifies minor version 0.");

    public static readonly DiagnosticDescriptor ReservedNonZero = new(
        "OT.pclt.reserved", "Reserved PCLT byte is non-zero",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "The reserved byte is 0x{0:X2}, expected 0.",
        "The final byte of the PCLT header is reserved and must be zero.");

    public static readonly DiagnosticDescriptor StrokeWeightOutOfRange = new(
        "OT.pclt.stroke-weight", "strokeWeight is outside the PCL range",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "strokeWeight is {0}, expected a value in [−7, 7].",
        "The PCL stroke weight scale runs from −7 to 7. Values outside that range are undefined.");

    public static readonly DiagnosticDescriptor WidthTypeOutOfRange = new(
        "OT.pclt.width-type", "widthType is outside the PCL range",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "widthType is {0}, expected a value in [−5, 5].",
        "The PCL appearance-width scale runs from −5 to 5. Values outside that range are undefined.");

    public static readonly DiagnosticDescriptor EmptyTypeface = new(
        "OT.pclt.empty-typeface", "Typeface string is empty",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Consistency,
        "The 16-byte typeface field is entirely NUL bytes.",
        "PCL font listings use the typeface string to identify the font. An empty field leaves the entry unidentified.");

    public static readonly DiagnosticDescriptor EmptyFileName = new(
        "OT.pclt.empty-file-name", "File name field is empty",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Consistency,
        "The 6-byte file name field is entirely NUL bytes.",
        "PCL font listings use the file name field to identify the font. An empty field leaves the entry unidentified.");

    public static readonly DiagnosticDescriptor FileNameWrongLength = new(
        "OT.pclt.file-name-length", "File name is not the expected length",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Consistency,
        "The file name '{0}' is {1} characters; the PCL field expects 5 characters plus a NUL.",
        "The file name field is a fixed 6-byte slot in which bytes 0–4 carry the name and byte 5 is a NUL terminator.");

    public static readonly DiagnosticDescriptor VendorCodeZero = new(
        "OT.pclt.vendor-code", "Vendor code is zero",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "typeFamily vendor code is 0.",
        "Vendor code 0 is the PCL default and means no specific vendor. Registered fonts typically carry a non-zero code.");

    public static readonly DiagnosticDescriptor NonUnicodeIndexingDeclared = new(
        "OT.pclt.non-unicode-indexing", "Font declares non-Unicode character ordering",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "characterComplement bit 0 is set.",
        "Bit 0 is cleared when glyph elements are provided in Unicode order. Set means the font uses a legacy ordering for PCL.");

    private PcltTableAnalyzer() { }

    public static PcltTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var pclt = face.GetTable<PcltTable>();
        var tag = PcltTable.Tag;

        AnalyzeVersions(pclt, tag, bag);
        AnalyzeStyleFields(pclt, tag, bag);
        AnalyzeStrings(pclt, tag, bag);
        AnalyzeComplement(pclt, tag, bag);
    }

    private static void AnalyzeVersions(PcltTable pclt, Tag tag, DiagnosticBag bag)
    {
        if (pclt.MajorVersion != 1)
        {
            bag.Add(MajorVersionInvalid.Create(
                [pclt.MajorVersion],
                table: tag, field: nameof(PcltTable.MajorVersion)));
        }

        if (pclt.MinorVersion != 0)
        {
            bag.Add(MinorVersionNonZero.Create(
                [pclt.MinorVersion],
                table: tag, field: nameof(PcltTable.MinorVersion)));
        }
    }

    private static void AnalyzeStyleFields(PcltTable pclt, Tag tag, DiagnosticBag bag)
    {
        if (pclt.StrokeWeight < -7 || pclt.StrokeWeight > 7)
        {
            bag.Add(StrokeWeightOutOfRange.Create(
                [pclt.StrokeWeight],
                table: tag, field: nameof(PcltTable.StrokeWeight)));
        }

        if (pclt.WidthType < -5 || pclt.WidthType > 5)
        {
            bag.Add(WidthTypeOutOfRange.Create(
                [pclt.WidthType],
                table: tag, field: nameof(PcltTable.WidthType)));
        }

        if (pclt.VendorCode == 0)
        {
            bag.Add(VendorCodeZero.Create(
                [], table: tag, field: nameof(PcltTable.TypeFamily)));
        }
    }

    private static void AnalyzeStrings(PcltTable pclt, Tag tag, DiagnosticBag bag)
    {
        if (string.IsNullOrEmpty(pclt.Typeface))
        {
            bag.Add(EmptyTypeface.Create(
                [], table: tag, field: nameof(PcltTable.Typeface)));
        }

        if (string.IsNullOrEmpty(pclt.FileName))
        {
            bag.Add(EmptyFileName.Create(
                [], table: tag, field: nameof(PcltTable.FileName)));
        }
        else if (pclt.FileName.Length != 5)
        {
            bag.Add(FileNameWrongLength.Create(
                [pclt.FileName, pclt.FileName.Length],
                table: tag, field: nameof(PcltTable.FileName)));
        }
    }

    private static void AnalyzeComplement(PcltTable pclt, Tag tag, DiagnosticBag bag)
    {
        if ((pclt.CharacterComplement & CharacterComplement.NonUnicodeIndexing) != 0)
        {
            bag.Add(NonUnicodeIndexingDeclared.Create(
                [], table: tag, field: nameof(PcltTable.CharacterComplement)));
        }
    }
}
