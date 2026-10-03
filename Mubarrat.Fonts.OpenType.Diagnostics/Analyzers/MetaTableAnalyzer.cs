using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables.Metadata;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>meta</c> table.</summary>
/// <remarks>
/// <para>
/// The parser rejects the wrong version and bounds every data block against the table's
/// extent. What remains for analysis is the header consistency (<c>reserved</c> must be
/// zero), tag validity per the specification's character rules, and content sanity for
/// the two registered tags whose grammar is defined — <c>dlng</c> and <c>slng</c>.
/// </para>
/// <para>
/// Private tags begin with an uppercase letter; registered tags are lowercase and often
/// shorter than four characters, padded with spaces.
/// </para>
/// </remarks>
public class MetaTableAnalyzer : IFontAnalyzer
{
    public static readonly DiagnosticDescriptor ReservedNonZero = new(
        "OT.meta.reserved", "Reserved meta field is non-zero",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "The header's reserved field is 0x{0:X8}, expected 0.",
        "The specification requires the field at header offset +8 to be zero. A non-zero value usually indicates a writer that reused the slot for something else.");

    public static readonly DiagnosticDescriptor FlagsNonZero = new(
        "OT.meta.flags", "meta flags field is non-zero",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Specification,
        "flags is 0x{0:X8}, expected 0.",
        "No bits in the flags field are currently defined. Non-zero values reserve the field for future use.");

    public static readonly DiagnosticDescriptor DuplicateTag = new(
        "OT.meta.duplicate-tag", "Two data maps share a tag",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "Data maps at index {0} and {1} both use tag '{2}'.",
        "Each tag must appear at most once. Duplicates make the lookup ambiguous.");

    public static readonly DiagnosticDescriptor InvalidTagCharacters = new(
        "OT.meta.invalid-tag", "Tag contains invalid characters",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Data map at index {0} uses tag '{1}', which does not follow the specification's tag grammar.",
        "Tags must begin with a letter and contain only letters, digits, or trailing spaces.");

    public static readonly DiagnosticDescriptor DataLengthMismatch = new(
        "OT.meta.length-mismatch", "Data map length disagrees with the payload size",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "Data map '{0}' declares length {1} but its payload is {2} bytes.",
        "The declared length and the actual byte count must agree.");

    public static readonly DiagnosticDescriptor EmptyDlng = new(
        "OT.meta.empty-dlng", "Design languages list is empty",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "The 'dlng' tag is present but its value is empty.",
        "An empty design-languages list provides no information.");

    public static readonly DiagnosticDescriptor EmptySlng = new(
        "OT.meta.empty-slng", "Supported languages list is empty",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "The 'slng' tag is present but its value is empty.",
        "An empty supported-languages list provides no information.");

    public static readonly DiagnosticDescriptor LanguageTagWithWhitespace = new(
        "OT.meta.language-tag-whitespace", "Language tag list contains empty entries",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "The '{0}' value contains consecutive commas or leading/trailing whitespace.",
        "Language tags are comma-separated. Empty entries between commas are usually a formatting error.");

    public static readonly DiagnosticDescriptor UnregisteredTagPresent = new(
        "OT.meta.unregistered-tag", "Unregistered tag present",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "Data map '{0}' uses a tag that is neither 'dlng' nor 'slng'.",
        "Tags other than 'dlng' and 'slng' are either private (begining with an uppercase letter) or vendor-defined.");

    public static readonly DiagnosticDescriptor NoDataMaps = new(
        "OT.meta.no-data-maps", "meta has no data maps",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Performance,
        "The table declares zero data maps.",
        "A meta table with no data maps provides no information and could be omitted.");

    private MetaTableAnalyzer() { }

    public static MetaTableAnalyzer Instance => field ??= new();

    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var meta = face.GetTable<MetaTable>();
        var tag = MetaTable.Tag;

        AnalyzeHeader(meta, tag, bag);
        AnalyzeDataMaps(meta, tag, bag);
        AnalyzeLanguageLists(meta, tag, bag);
    }

    private static void AnalyzeHeader(MetaTable meta, Tag tag, DiagnosticBag bag)
    {
        if (meta.Reserved != 0)
        {
            bag.Add(ReservedNonZero.Create(
                [meta.Reserved],
                table: tag, field: nameof(MetaTable.Reserved)));
        }

        if (meta.Flags != 0)
        {
            bag.Add(FlagsNonZero.Create(
                [meta.Flags],
                table: tag, field: nameof(MetaTable.Flags)));
        }

        if (meta.Count == 0)
        {
            bag.Add(NoDataMaps.Create(
                [], table: tag, field: nameof(MetaTable.DataMaps)));
        }
    }

    private static void AnalyzeDataMaps(MetaTable meta, Tag tag, DiagnosticBag bag)
    {
        var seen = new Dictionary<Tag, int>();

        for (int i = 0; i < meta.DataMaps.Count; i++)
        {
            MetaDataMap map = meta.DataMaps[i];

            if (seen.TryGetValue(map.Tag, out int first))
            {
                bag.Add(DuplicateTag.Create(
                    [first, i, map.Tag.ToString()],
                    table: tag, field: $"dataMaps[{i}].Tag"));
            }
            else
            {
                seen[map.Tag] = i;
            }

            if (!MetaTable.IsValidTag(map.Tag))
            {
                bag.Add(InvalidTagCharacters.Create(
                    [i, map.Tag.ToString()],
                    table: tag, field: $"dataMaps[{i}].Tag"));
            }

            string tagName = map.Tag.ToString();
            if (tagName != "dlng" && tagName != "slng")
            {
                bag.Add(UnregisteredTagPresent.Create(
                    [tagName],
                    table: tag, field: $"dataMaps[{i}].Tag"));
            }
        }
    }

    private static void AnalyzeLanguageLists(MetaTable meta, Tag tag, DiagnosticBag bag)
    {
        MetaDataMap? dlng = meta.Find("dlng");
        if (dlng is not null)
        {
            string value = dlng.AsString();
            if (string.IsNullOrEmpty(value))
            {
                bag.Add(EmptyDlng.Create([], table: tag, field: "dlng"));
            }
            else if (HasEmptyEntries(value))
            {
                bag.Add(LanguageTagWithWhitespace.Create(
                    ["dlng"],
                    table: tag, field: "dlng"));
            }
        }

        MetaDataMap? slng = meta.Find("slng");
        if (slng is not null)
        {
            string value = slng.AsString();
            if (string.IsNullOrEmpty(value))
            {
                bag.Add(EmptySlng.Create([], table: tag, field: "slng"));
            }
            else if (HasEmptyEntries(value))
            {
                bag.Add(LanguageTagWithWhitespace.Create(
                    ["slng"],
                    table: tag, field: "slng"));
            }
        }
    }

    private static bool HasEmptyEntries(string value)
    {
        if (value.StartsWith(',') || value.EndsWith(',')) return true;
        return value.Contains(",,", StringComparison.Ordinal);
    }
}
