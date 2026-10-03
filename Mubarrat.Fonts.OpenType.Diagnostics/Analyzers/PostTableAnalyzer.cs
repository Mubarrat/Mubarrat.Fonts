using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables;

namespace Mubarrat.Fonts.OpenType.Diagnostics.Analyzers;

/// <summary>Rules for the <c>post</c> table.</summary>
/// <remarks>
/// <para>
/// The <c>post</c> table carries the italic angle, underline metrics, a fixed-pitch hint,
/// memory-usage hints for Type 42/Type 1 downloads, and (version 2.0 only) PostScript glyph
/// names. The unknown-version case is enforced in <see cref="PostTable.Parse"/> and cannot be
/// observed here.
/// </para>
/// <para>
/// Cross-table rules treat <c>post</c> as a dependent: <c>head.macStyle</c> and
/// <c>OS/2.fsSelection</c> are the authoritative style indicators, <c>hmtx</c> is the
/// authoritative spacing, and <c>maxp.numGlyphs</c> is the authoritative glyph count.
/// When any of those disagree with <c>post</c>, the rule fires here.
/// </para>
/// </remarks>
public class PostTableAnalyzer : IFontAnalyzer
{
    // ─────────────────────── Constants ───────────────────────

    private const int StandardMacGlyphCount = 258;

    // ─────────────────────── Descriptors ───────────────────────

    public static readonly DiagnosticDescriptor MinMemGreaterThanMax = new(
        "OT.post.min-mem-greater-than-max", "Minimum memory exceeds maximum",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Specification,
        "minMem{0} is {1} but maxMem{0} is {2}.",
        "The MinMem fields declare the minimum memory usage when a font is downloaded; the MaxMem fields declare the maximum. A minimum greater than the maximum is inverted.");

    public static readonly DiagnosticDescriptor NumGlyphsMismatch = new(
        "OT.post.num-glyphs-vs-maxp", "post glyph count disagrees with maxp",
        DiagnosticSeverity.Error,
        DiagnosticCategory.Consistency,
        "post declares {0} glyphs but maxp.numGlyphs is {1}.",
        "Every per-glyph table must agree with maxp.numGlyphs. A mismatch means the post table was written against a different glyph set than the font contains.");

    public static readonly DiagnosticDescriptor UnderlineThicknessZero = new(
        "OT.post.underline-thickness-zero", "Underline thickness is zero",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "underlineThickness is 0.",
        "A zero underline thickness produces no visible underline in renderers that honor the post table's metrics.");

    public static readonly DiagnosticDescriptor UnderlineThicknessNegative = new(
        "OT.post.underline-thickness-negative", "Underline thickness is negative",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "underlineThickness is {0}; thickness is a magnitude.",
        "The spec describes underlineThickness as the suggested thickness of the underline stroke. A negative value draws a stroke with no meaningful geometry.");

    public static readonly DiagnosticDescriptor GlyphNameDuplicate = new(
        "OT.post.glyph-name-duplicate", "Two glyphs share a PostScript name",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "Glyphs {0} and {1} are both named '{2}'.",
        "PostScript glyph names must be unique within a font. Duplicate names confuse tools that look up glyphs by name rather than by ID.");

    public static readonly DiagnosticDescriptor GlyphNameInvalidChars = new(
        "OT.post.glyph-name-invalid-chars", "PostScript name contains invalid characters",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Specification,
        "Glyph {0} is named '{1}', which contains character U+{2:X4} at index {3}.",
        "PostScript glyph names are restricted to printable ASCII (33–126) excluding [ ] ( ) { } < > / %.");

    public static readonly DiagnosticDescriptor FixedPitchMismatch = new(
        "OT.post.fixed-pitch-vs-hmtx", "isFixedPitch disagrees with hmtx advances",
        DiagnosticSeverity.Warning,
        DiagnosticCategory.Consistency,
        "post.isFixedPitch is {0} but the glyph advance widths are {1}.",
        "A non-zero isFixedPitch declares a monospaced font. Renderers and layout engines consult the hint when classifying the font, so a mismatch affects downstream behavior.");

    public static readonly DiagnosticDescriptor Version25Deprecated = new(
        "OT.post.version-2-5-deprecated", "post version 2.5 is deprecated",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "version is 0x00025000.",
        "Version 2.5 was deprecated before OpenType 1.0. It is supported here for completeness but is not expected in modern fonts.");

    public static readonly DiagnosticDescriptor NoGlyphNames = new(
        "OT.post.no-glyph-names", "Font does not carry PostScript glyph names",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "version 0x{0:X8} does not include a glyph name block.",
        "Version 1.0 and 3.0 carry no per-glyph names. Tools that rely on PostScript names — some PDF pipelines and PostScript printers — will derive them from the character map or leave them empty.");

    public static readonly DiagnosticDescriptor FixedPitchSet = new(
        "OT.post.is-fixed-pitch-set", "Font declares itself monospaced",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "isFixedPitch is {0} (non-zero).",
        "The font declares itself monospaced. Layout engines use the hint to choose monospace rendering paths even when all advances happen to agree.");

    public static readonly DiagnosticDescriptor ItalicAngleNonZero = new(
        "OT.post.italic-angle-nonzero", "Font declares a non-zero italic angle",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "italicAngle is {0:F4} degrees.",
        "A non-zero italic angle means the glyphs lean. Purely informational; the value is correct for an oblique or italic design.");

    public static readonly DiagnosticDescriptor ItalicAngleMismatch = new(
        "OT.post.italic-angle-vs-style", "Italic angle disagrees with style bits",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Consistency,
        "italicAngle is {0:F4} degrees but the font's style bits say it is {1}.",
        "head.macStyle.italic and OS/2.fsSelection italic/oblique are the authoritative style indicators. A disagreement is usually a leftover from the design phase.");

    public static readonly DiagnosticDescriptor MemoryFieldsPopulated = new(
        "OT.post.memory-fields-populated", "Memory-usage fields are non-zero",
        DiagnosticSeverity.Information,
        DiagnosticCategory.Compatibility,
        "minMemType42 is {0}, maxMemType42 is {1}, minMemType1 is {2}, maxMemType1 is {3}.",
        "The memory-usage fields are optional hints from the pre-OpenType era. Fonts with these fields set are usually older or use a generator that still populates them.");

    // ─────────────────────── Singleton ───────────────────────

    private PostTableAnalyzer() { }

    public static PostTableAnalyzer Instance => field ??= new();

    // ─────────────────────── Analysis ───────────────────────

    /// <summary>Runs every post rule against the face's <c>post</c> table.</summary>
    public void Analyze(FontFace face, DiagnosticBag bag)
    {
        var post = face.GetTable<PostTable>();
        var tag = PostTable.Tag;

        // Single-table rules.
        AnalyzeMemoryFields(post, tag, bag);
        AnalyzeUnderline(post, tag, bag);
        AnalyzeVersionShape(post, tag, bag);
        AnalyzeGlyphNames(post, tag, bag);
        AnalyzeStyleAgreement(face, post, tag, bag);   // needs head or OS/2
        AnalyzeFixedPitch(face, post, tag, bag);       // needs hmtx

        // Cross-table rules. post is the dependent; maxp is the source of truth.
        if (face.Directory.Contains(MaxpTable.Tag))
        {
            var maxp = face.GetTable<MaxpTable>();
            if (post.Version is PostVersion.Version20 or PostVersion.Version25 &&
                post.NumGlyphs != maxp.NumGlyphs)
            {
                bag.Add(NumGlyphsMismatch.Create(
                    [post.NumGlyphs, maxp.NumGlyphs],
                    table: tag, field: nameof(PostTable.NumGlyphs)));
            }
        }
    }

    // ─────────────────────── Rule groups ───────────────────────

    private static void AnalyzeMemoryFields(PostTable post, Tag tag, DiagnosticBag bag)
    {
        if (post.MinMemType42 > post.MaxMemType42)
        {
            bag.Add(MinMemGreaterThanMax.Create(
                ["Type42", post.MinMemType42, post.MaxMemType42],
                table: tag, field: "minMemType42/maxMemType42",
                span: new SourceSpan(16, 8)));
        }

        if (post.MinMemType1 > post.MaxMemType1)
        {
            bag.Add(MinMemGreaterThanMax.Create(
                ["Type1", post.MinMemType1, post.MaxMemType1],
                table: tag, field: "minMemType1/maxMemType1",
                span: new SourceSpan(24, 8)));
        }

        bool anyPopulated =
            post.MinMemType42 != 0 || post.MaxMemType42 != 0 ||
            post.MinMemType1 != 0 || post.MaxMemType1 != 0;

        if (anyPopulated)
        {
            bag.Add(MemoryFieldsPopulated.Create(
                [post.MinMemType42, post.MaxMemType42,
                 post.MinMemType1,  post.MaxMemType1],
                table: tag, field: "memoryFields",
                span: new SourceSpan(16, 16)));
        }
    }

    private static void AnalyzeUnderline(PostTable post, Tag tag, DiagnosticBag bag)
    {
        if (post.UnderlineThickness < 0)
        {
            bag.Add(UnderlineThicknessNegative.Create(
                [post.UnderlineThickness],
                table: tag, field: nameof(PostTable.UnderlineThickness),
                span: new SourceSpan(10, 2)));
        }
        else if (post.UnderlineThickness == 0)
        {
            bag.Add(UnderlineThicknessZero.Create(
                [],
                table: tag, field: nameof(PostTable.UnderlineThickness),
                span: new SourceSpan(10, 2)));
        }
    }

    private static void AnalyzeVersionShape(PostTable post, Tag tag, DiagnosticBag bag)
    {
        if (post.Version == PostVersion.Version25)
        {
            bag.Add(Version25Deprecated.Create(
                [],
                table: tag, field: nameof(PostTable.Version),
                span: new SourceSpan(0, 4)));
        }

        if (post.Version is PostVersion.Version10 or PostVersion.Version30)
        {
            bag.Add(NoGlyphNames.Create(
                [(uint)post.Version],
                table: tag, field: nameof(PostTable.Version),
                span: new SourceSpan(0, 4)));
        }
    }

    private static void AnalyzeGlyphNames(PostTable post, Tag tag, DiagnosticBag bag)
    {
        if (post.GlyphNames is not { } names || names.Count == 0) return;

        // Duplicate detection. Skip .notdef (index 0) — a second glyph named
        // .notdef is genuinely a bug, but it's easier to allow the conventional
        // one and report only the second occurrence.
        var seen = new Dictionary<string, int>(names.Count, StringComparer.Ordinal);
        for (int i = 0; i < names.Count; i++)
        {
            string name = names[i];
            if (string.IsNullOrEmpty(name)) continue;

            if (seen.TryGetValue(name, out int first))
            {
                bag.Add(GlyphNameDuplicate.Create(
                    [first, i, name],
                    table: tag, field: $"glyphNames[{i}]"));
                continue;   // don't add to seen; keep the first occurrence as canonical
            }

            seen[name] = i;
        }

        // Character validation. One diagnostic per glyph is enough.
        for (int i = 0; i < names.Count; i++)
        {
            string name = names[i];
            if (string.IsNullOrEmpty(name)) continue;

            for (int c = 0; c < name.Length; c++)
            {
                if (IsValidPostScriptNameChar(name[c])) continue;

                bag.Add(GlyphNameInvalidChars.Create(
                    [i, name, (int)name[c], c],
                    table: tag, field: $"glyphNames[{i}]"));
                break;
            }
        }
    }

    private static void AnalyzeStyleAgreement(FontFace face, PostTable post, Tag tag, DiagnosticBag bag)
    {
        // The head table's macStyle is the authoritative italic indicator.
        // If it's absent, fall back to OS/2.
        bool? declaresItalic = null;

        if (face.Directory.Contains(HeadTable.Tag))
        {
            var head = face.GetTable<HeadTable>();
            declaresItalic = (head.MacStyle & MacStyle.Italic) != 0;
        }
        else if (face.Directory.Contains(Os2Table.Tag))
        {
            var os2 = face.GetTable<Os2Table>();
            declaresItalic = (os2.FsSelection & FsSelection.Italic) != 0
                          || (os2.FsSelection & FsSelection.Oblique) != 0;
        }

        double angle = post.ItalicAngle.Value;   // degrees, from the Fixed 16.16

        if (angle != 0.0)
        {
            bag.Add(ItalicAngleNonZero.Create(
                [angle],
                table: tag, field: nameof(PostTable.ItalicAngle),
                span: new SourceSpan(4, 4)));
        }

        if (declaresItalic is { } italic)
        {
            bool angleNonZero = angle != 0.0;
            if (italic && !angleNonZero)
            {
                bag.Add(ItalicAngleMismatch.Create(
                    [angle, "italic"],
                    table: tag, field: nameof(PostTable.ItalicAngle),
                    span: new SourceSpan(4, 4)));
            }
            else if (!italic && angleNonZero)
            {
                bag.Add(ItalicAngleMismatch.Create(
                    [angle, "upright"],
                    table: tag, field: nameof(PostTable.ItalicAngle),
                    span: new SourceSpan(4, 4)));
            }
        }
    }

    private static void AnalyzeFixedPitch(FontFace face, PostTable post, Tag tag, DiagnosticBag bag)
    {
        bool declaredFixed = post.IsFixedPitch != 0;

        if (declaredFixed)
        {
            bag.Add(FixedPitchSet.Create(
                [post.IsFixedPitch],
                table: tag, field: nameof(PostTable.IsFixedPitch),
                span: new SourceSpan(12, 4)));
        }

        if (!face.Directory.Contains(HmtxTable.Tag)) return;

        var hmtx = face.GetTable<HmtxTable>();
        bool actuallyFixed = AreAllAdvancesEqual(hmtx.AdvanceWidths);

        if (declaredFixed && !actuallyFixed)
        {
            bag.Add(FixedPitchMismatch.Create(
                [post.IsFixedPitch, "not all equal"],
                table: tag, field: nameof(PostTable.IsFixedPitch),
                span: new SourceSpan(12, 4)));
        }
        else if (!declaredFixed && actuallyFixed && hmtx.AdvanceWidths.Count > 1)
        {
            bag.Add(FixedPitchMismatch.Create(
                [post.IsFixedPitch, "all equal"],
                table: tag, field: nameof(PostTable.IsFixedPitch),
                span: new SourceSpan(12, 4)));
        }
    }

    // ─────────────────────── Cross-table: maxp ───────────────────────

    /// <summary>
    /// Compares the post table's declared glyph count against <c>maxp.numGlyphs</c>.
    /// Called by the framework when both tables are present. The rule fires on
    /// <c>post</c> because <c>maxp.numGlyphs</c> is the authoritative count.
    /// </summary>
    public static void AnalyzeAgainstMaxp(PostTable post, MaxpTable maxp, DiagnosticBag bag)
    {
        // Only versions 2.0 and 2.5 carry a glyph count.
        if (post.Version is not (PostVersion.Version20 or PostVersion.Version25)) return;

        if (post.NumGlyphs != maxp.NumGlyphs)
        {
            bag.Add(NumGlyphsMismatch.Create(
                [post.NumGlyphs, maxp.NumGlyphs],
                table: PostTable.Tag, field: nameof(PostTable.NumGlyphs)));
        }
    }

    // ─────────────────────── Helpers ───────────────────────

    private static bool AreAllAdvancesEqual(IReadOnlyList<ushort> advances)
    {
        if (advances.Count <= 1) return true;
        ushort first = advances[0];
        for (int i = 1; i < advances.Count; i++)
            if (advances[i] != first) return false;
        return true;
    }

    private static bool IsValidPostScriptNameChar(char c) =>
        c is >= (char)33 and <= (char)126 &&
        c is not ('[' or ']' or '(' or ')' or '{' or '}' or '<' or '>' or '/' or '%');
}
