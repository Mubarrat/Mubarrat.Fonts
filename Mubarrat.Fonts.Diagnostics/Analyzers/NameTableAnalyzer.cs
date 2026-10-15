using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>name</c> table.</summary>
/// <remarks>
/// <para>
/// <c>name</c> carries two categories of rule. The first is structural: record keys
/// must be sorted and unique, and the four spec-mandated name IDs (1 Family,
/// 2 Subfamily, 4 Full name, 6 PostScript name) must be present. The second is
/// content: the PostScript name is restricted to a subset of ASCII, required records
/// must not be empty, and platform coverage is checked because a font without a
/// Windows Unicode record behaves differently on Windows.
/// </para>
/// <para>
/// The version discriminant (0 or 1) is enforced in <see cref="NameTable.Parse"/>
/// and cannot be observed here. All rules below operate on the parsed record set.
/// </para>
/// </remarks>
public class NameTableAnalyzer : IFontAnalyzer
{
    // ─────────────────────── Name ID constants ───────────────────────

    private static readonly NameId IdCopyright = (NameId)0;
    private static readonly NameId IdFamily = (NameId)1;
    private static readonly NameId IdSubfamily = (NameId)2;
    private static readonly NameId IdFullName = (NameId)4;
    private static readonly NameId IdVersionString = (NameId)5;
    private static readonly NameId IdPostScriptName = (NameId)6;
    private static readonly NameId IdLicense = (NameId)13;
    private static readonly NameId IdTypographicFamily = (NameId)16;

    /// <summary>PostScript names are limited to 63 bytes by the CFF and Type 1 specifications.</summary>
    private const int MaxPostScriptNameLength = 63;

    /// <summary>Windows Unicode BMP encoding ID.</summary>
    private const ushort WindowsUnicodeBmp = 1;

    /// <summary>Windows Unicode full repertoire encoding ID.</summary>
    private const ushort WindowsUnicodeFull = 10;

    /// <summary>Macintosh Roman encoding ID.</summary>
    private const ushort MacRoman = 0;

    // ─────────────────────── Descriptors ───────────────────────

    public static readonly DiagnosticDescriptor RecordsUnsorted = new(
        "OT.name.records-unsorted", "Name records are not sorted",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Record at index {0} ({1}/{2}/{3}/{4}) precedes record {5} ({6}/{7}/{8}/{9}), but the spec requires sorting by platform, encoding, language, then name ID.",
        "Binary-search lookups in the name table depend on the sort order. Unsorted records break lookups in some consumers.");

    public static readonly DiagnosticDescriptor DuplicateKey = new(
        "OT.name.duplicate-key", "Duplicate name record key",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Two records share the key (platform {0}, encoding {1}, language {2}, name ID {3}).",
        "The spec requires each (platform, encoding, language, name ID) tuple to appear at most once.");

    public static readonly DiagnosticDescriptor MissingFamily = new(
        "OT.name.missing-family", "No family name (name ID 1)",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "No record with name ID 1 (Family) is present.",
        "The family name is required. Without it, the font cannot be identified or grouped correctly by any consumer.");

    public static readonly DiagnosticDescriptor MissingSubfamily = new(
        "OT.name.missing-subfamily", "No subfamily name (name ID 2)",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "No record with name ID 2 (Subfamily) is present.",
        "The subfamily name (Regular, Bold, Italic, …) is required. Without it, the font's style cannot be determined.");

    public static readonly DiagnosticDescriptor MissingFullName = new(
        "OT.name.missing-full-name", "No full name (name ID 4)",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "No record with name ID 4 (Full name) is present.",
        "The full name is required. Consumers that display or index a single human-readable name for the font rely on it.");

    public static readonly DiagnosticDescriptor MissingPostScript = new(
        "OT.name.missing-postscript", "No PostScript name (name ID 6)",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "No record with name ID 6 (PostScript name) is present.",
        "The PostScript name is required. It is used by print pipelines, PDF generators, and some font-caching systems as the canonical identifier.");

    public static readonly DiagnosticDescriptor PostScriptInvalidChars = new(
        "OT.name.postscript-invalid-chars", "PostScript name contains invalid characters",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "The name ID 6 record for platform {0}/encoding {1}/language {2} contains '{3}' at offset {4}.",
        "PostScript names are restricted to printable ASCII (33–126) excluding the ten characters [ ] ( ) { } < > / %.");

    public static readonly DiagnosticDescriptor PostScriptTooLong = new(
        "OT.name.postscript-too-long", "PostScript name exceeds 63 characters",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "The name ID 6 record for platform {0}/encoding {1}/language {2} is {3} characters; the maximum is {4}.",
        "The CFF and Type 1 specifications limit PostScript names to 63 bytes. Longer names are silently truncated by consumers.");

    public static readonly DiagnosticDescriptor EmptyRequiredString = new(
        "OT.name.empty-required-string", "Required name record has an empty string",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "The name ID {0} record for platform {1}/encoding {2}/language {3} has an empty string.",
        "A record for a required name ID exists but carries no content. Consumers that select this record over a populated record from another platform will display nothing.");

    public static readonly DiagnosticDescriptor NoWindowsUnicode = new(
        "OT.name.no-windows-unicode", "No Windows Unicode name records",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Compatibility,
        "No record uses Windows platform with Unicode encoding (1 or 10).",
        "Windows uses the Windows Unicode name records as the authoritative strings. Without them, the font falls back to Macintosh records, which may decode incorrectly.");

    public static readonly DiagnosticDescriptor MissingVersionString = new(
        "OT.name.missing-version-string", "No version string (name ID 5)",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Specification,
        "No record with name ID 5 (Version string) is present.",
        "The version string is not strictly required but is used by font managers, update checkers, and diagnostic tools to identify the font's revision.");

    public static readonly DiagnosticDescriptor NoMacRoman = new(
        "OT.name.no-mac-roman", "No Macintosh Roman name records",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "No record uses Macintosh platform with Roman encoding.",
        "Modern fonts frequently omit Macintosh records entirely. This is not a defect, but tools that target classic macOS may not identify the font.");

    public static readonly DiagnosticDescriptor MissingCopyright = new(
        "OT.name.missing-copyright", "No copyright notice (name ID 0)",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Specification,
        "No record with name ID 0 (Copyright) is present.",
        "The copyright notice is optional in the specification but expected in publicly distributed fonts.");

    public static readonly DiagnosticDescriptor MissingLicense = new(
        "OT.name.missing-license", "No license description (name ID 13)",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Specification,
        "No record with name ID 13 (License description) is present.",
        "The license description is required for fonts distributed under the SIL Open Font License and similar terms. It is optional for fonts distributed under other terms.");

    public static readonly DiagnosticDescriptor MissingTypographicFamily = new(
        "OT.name.missing-typographic-family", "No typographic family (name ID 16)",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "No record with name ID 16 (Typographic Family) is present.",
        "Fonts with more than four styles in a family (Regular, Bold, Italic, Bold Italic plus width or weight variants) should declare name ID 16 to distinguish the base family from the width-specific family.");

    // ─────────────────────── Singleton ───────────────────────

    private NameTableAnalyzer() { }

    public static NameTableAnalyzer Instance => field ??= new();

    // ─────────────────────── Analysis ───────────────────────

    /// <summary>Runs every name rule against the face's <c>name</c> table.</summary>
    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var name = face.GetTable<NameTable>();
        var tag = NameTable.Tag;

        AnalyzeOrdering(name, tag, bag);
        AnalyzeRequiredNames(name, tag, bag);
        AnalyzePostScriptName(name, tag, bag);
        AnalyzeEmptyStrings(name, tag, bag);
        AnalyzePlatformCoverage(name, tag, bag);
        AnalyzeRecommendedNames(name, tag, bag);
    }

    // ─────────────────────── Rule groups ───────────────────────

    private static void AnalyzeOrdering(NameTable name, Tag tag, DiagnosticBag bag)
    {
        for (int i = 1; i < name.Records.Count; i++)
        {
            NameRecord prev = name.Records[i - 1];
            NameRecord curr = name.Records[i];
            int cmp = CompareKey(prev, curr);

            // Each record header is 12 bytes starting at offset 6.
            var span = new SourceSpan(6 + i * 12L, 12);

            if (cmp > 0)
            {
                bag.Add(RecordsUnsorted.Create(
                    [i - 1, (ushort)prev.PlatformId, prev.EncodingId, prev.LanguageId, (ushort)prev.NameId,
                     i,     (ushort)curr.PlatformId, curr.EncodingId, curr.LanguageId, (ushort)curr.NameId],
                    table: tag, field: $"records[{i}]", span: span));
            }
            else if (cmp == 0)
            {
                bag.Add(DuplicateKey.Create(
                    [(ushort)curr.PlatformId, curr.EncodingId, curr.LanguageId, (ushort)curr.NameId],
                    table: tag, field: $"records[{i}]", span: span));
            }
        }
    }

    private static void AnalyzeRequiredNames(NameTable name, Tag tag, DiagnosticBag bag)
    {
        if (!HasNameId(name, IdFamily))
            bag.Add(MissingFamily.Create([], table: tag, field: "name ID 1"));

        if (!HasNameId(name, IdSubfamily))
            bag.Add(MissingSubfamily.Create([], table: tag, field: "name ID 2"));

        if (!HasNameId(name, IdFullName))
            bag.Add(MissingFullName.Create([], table: tag, field: "name ID 4"));

        if (!HasNameId(name, IdPostScriptName))
            bag.Add(MissingPostScript.Create([], table: tag, field: "name ID 6"));
    }

    private static void AnalyzePostScriptName(NameTable name, Tag tag, DiagnosticBag bag)
    {
        for (int i = 0; i < name.Records.Count; i++)
        {
            NameRecord r = name.Records[i];
            if (r.NameId != IdPostScriptName) continue;
            if (string.IsNullOrEmpty(r.String)) continue;   // empty handled elsewhere

            var span = new SourceSpan(6 + i * 12L, 12);

            // Length check first; a too-long name with an invalid char fires both,
            // which is correct because the two are independent requirements.
            if (r.String.Length > MaxPostScriptNameLength)
            {
                bag.Add(PostScriptTooLong.Create(
                    [(ushort)r.PlatformId, r.EncodingId, r.LanguageId,
                     r.String.Length, MaxPostScriptNameLength],
                    table: tag, field: $"records[{i}].String", span: span));
            }

            for (int c = 0; c < r.String.Length; c++)
            {
                char ch = r.String[c];
                if (IsValidPostScriptChar(ch)) continue;

                bag.Add(PostScriptInvalidChars.Create(
                    [(ushort)r.PlatformId, r.EncodingId, r.LanguageId,
                     FormatChar(ch), c],
                    table: tag, field: $"records[{i}].String", span: span));
                break;   // one diagnostic per record is enough
            }
        }
    }

    private static void AnalyzeEmptyStrings(NameTable name, Tag tag, DiagnosticBag bag)
    {
        for (int i = 0; i < name.Records.Count; i++)
        {
            NameRecord r = name.Records[i];
            if (!IsRequired(r.NameId)) continue;
            if (!string.IsNullOrEmpty(r.String)) continue;

            bag.Add(EmptyRequiredString.Create(
                [(ushort)r.NameId, (ushort)r.PlatformId, r.EncodingId, r.LanguageId],
                table: tag, field: $"records[{i}].String",
                span: new SourceSpan(6 + i * 12L, 12)));
        }
    }

    private static void AnalyzePlatformCoverage(NameTable name, Tag tag, DiagnosticBag bag)
    {
        bool hasWindowsUnicode = false;
        bool hasMacRoman = false;

        foreach (var r in name.Records)
        {
            if (r.PlatformId == NamePlatformId.Windows &&
                r.EncodingId is WindowsUnicodeBmp or WindowsUnicodeFull)
            {
                hasWindowsUnicode = true;
            }

            if (r.PlatformId == NamePlatformId.Macintosh && r.EncodingId == MacRoman)
                hasMacRoman = true;

            if (hasWindowsUnicode && hasMacRoman) break;
        }

        if (!hasWindowsUnicode)
            bag.Add(NoWindowsUnicode.Create([], table: tag));

        if (!hasMacRoman)
            bag.Add(NoMacRoman.Create([], table: tag));
    }

    private static void AnalyzeRecommendedNames(NameTable name, Tag tag, DiagnosticBag bag)
    {
        if (!HasNameId(name, IdVersionString))
            bag.Add(MissingVersionString.Create([], table: tag, field: "name ID 5"));

        if (!HasNameId(name, IdCopyright))
            bag.Add(MissingCopyright.Create([], table: tag, field: "name ID 0"));

        if (!HasNameId(name, IdLicense))
            bag.Add(MissingLicense.Create([], table: tag, field: "name ID 13"));

        if (!HasNameId(name, IdTypographicFamily))
            bag.Add(MissingTypographicFamily.Create([], table: tag, field: "name ID 16"));
    }

    // ─────────────────────── Helpers ───────────────────────

    private static bool HasNameId(NameTable name, NameId id)
    {
        foreach (var r in name.Records)
            if (r.NameId == id) return true;
        return false;
    }

    private static bool IsRequired(NameId id) =>
        id == IdFamily || id == IdSubfamily || id == IdFullName || id == IdPostScriptName;

    private static int CompareKey(in NameRecord a, in NameRecord b)
    {
        int c;
        if ((c = ((int)a.PlatformId).CompareTo((int)b.PlatformId)) != 0) return c;
        if ((c = a.EncodingId.CompareTo(b.EncodingId)) != 0) return c;
        if ((c = a.LanguageId.CompareTo(b.LanguageId)) != 0) return c;
        return ((int)a.NameId).CompareTo((int)b.NameId);
    }

    private static bool IsValidPostScriptChar(char c) =>
        c is >= (char)33 and <= (char)126 &&
        c is not ('[' or ']' or '(' or ')' or '{' or '}' or '<' or '>' or '/' or '%');

    private static string FormatChar(char c) =>
        c is >= ' ' and <= '~' ? c.ToString() : $"\\u{(int)c:X4}";
}
