using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>JSTF</c> table: justification data. Specifies the substitutions and positioning operations used to justify text, per script and language system.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A JSTF table is only needed when a font's default glyph set cannot be justified by the standard client-side algorithms. The table declares a priority-ordered list of GSUB and GPOS lookup combinations to enable or disable at each justification stage.</description></item>
/// <item><description>Justification is split into two directions — shrinking and extending — and each priority carries independent lookup lists for both.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="JstfScript"/>
/// <seealso cref="JstfLangSys"/>
/// <seealso cref="JstfPriority"/>
/// <seealso cref="ExtenderGlyph"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF table</seealso>
public sealed record JstfTable : IFontTable<JstfTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF table</seealso>
    public static Tag Tag => "JSTF";

    /// <summary>Gets the major version. Always 1.</summary>
    /// <value>The constant <c>1</c> for a conforming JSTF table.</value>
    /// <seealso cref="MinorVersion"/>
    /// <seealso cref="Header.Major"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version. Always 0.</summary>
    /// <value>The constant <c>0</c> for a conforming JSTF table.</value>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="Header.Minor"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the script records, sorted by tag.</summary>
    /// <value>The ordered list of <see cref="JstfScriptRecord"/> entries, sorted ascending by <see cref="JstfScriptRecord.Tag"/>.</value>
    /// <seealso cref="Find(Tag)"/>
    /// <seealso cref="JstfScriptRecord"/>
    public IReadOnlyList<JstfScriptRecord> Scripts { get; init; } = [];

    /// <summary>Gets the number of scripts.</summary>
    /// <value>The size of the <see cref="Scripts"/> list.</value>
    /// <seealso cref="Scripts"/>
    public int ScriptCount => Scripts.Count;

    /// <summary>Returns the script for a tag, or <c>null</c>.</summary>
    /// <param name="tag">The script tag to look up.</param>
    /// <returns>The <see cref="JstfScript"/> associated with <paramref name="tag"/>, or <c>null</c> when no record matches.</returns>
    /// <remarks>The lookup is linear over <see cref="Scripts"/>. Because the list is sorted by tag, a binary search would also be valid.</remarks>
    /// <seealso cref="Scripts"/>
    /// <seealso cref="JstfScript"/>
    public JstfScript? Find(Tag tag)
    {
        foreach (var r in Scripts) if (r.Tag == tag) return r.Script;
        return null;
    }

    /// <summary>The 4-byte <c>JSTF</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed by a <c>uint16</c> script count and that many 6-byte <see cref="ScriptHeader"/> entries.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="JstfTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the major version. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming JSTF table.</value>
        /// <seealso cref="Minor"/>
        public ushort Major;

        /// <summary>Gets the minor version. Always 0.</summary>
        /// <value>The constant <c>0</c> for a conforming JSTF table.</value>
        /// <seealso cref="Major"/>
        public ushort Minor;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            Major = BinaryPrimitives.ReverseEndianness(value.Major),
            Minor = BinaryPrimitives.ReverseEndianness(value.Minor),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the JSTF table.</param>
    /// <param name="context">Unused. The table resolves its scripts through its own source.</param>
    /// <returns>The parsed JSTF table.</returns>
    /// <exception cref="InvalidDataException">The major version is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header, script array, or any referenced script extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The script headers are materialized as a struct array and then converted to <see cref="JstfScriptRecord"/> entries in one pass; each entry's offset is then resolved against the table-scoped source.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="ScriptHeader"/>
    /// <seealso cref="JstfScriptRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF table</seealso>
    public static JstfTable Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Major != 1)
            throw new InvalidDataException($"'JSTF'.majorVersion is {header.Major}, expected 1.");

        var records = cursor.ReadBigEndianStructArray<ScriptHeader>(cursor.ReadUInt16());

        var scripts = new JstfScriptRecord[records.Length];
        for (int i = 0; i < records.Length; i++)
            scripts[i] = new JstfScriptRecord
            {
                Tag = records[i].Tag,
                Script = cursor.Source.ParseRecordAt<JstfScript>(records[i].Offset),
            };

        return new JstfTable
        {
            MajorVersion = header.Major,
            MinorVersion = header.Minor,
            Scripts = scripts,
        };
    }

    /// <summary>The 6-byte JSTF script header: a script tag plus an Offset16 to the script. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The offset is measured from the start of the JSTF table, not from the script record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF script records</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="JstfTable"/>
    /// <seealso cref="JstfScriptRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF script records</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct ScriptHeader : IEndianReversibleStruct<ScriptHeader>
    {
        /// <summary>Gets the script tag.</summary>
        /// <value>The four-character script identifier, e.g. <c>"latn"</c>, <c>"arab"</c>.</value>
        /// <seealso cref="Offset"/>
        public Tag Tag;

        /// <summary>Gets the offset to the script.</summary>
        /// <value>The byte offset of the <see cref="JstfScript"/> from the JSTF table start.</value>
        /// <seealso cref="Tag"/>
        /// <seealso cref="JstfScript"/>
        public ushort Offset;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with the tag and offset reversed.</returns>
        /// <remarks>The tag uses <see cref="Tag.ReverseEndianness(Tag)"/>; the offset uses <see cref="BinaryPrimitives.ReverseEndianness(ushort)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static ScriptHeader ReverseEndianness(ScriptHeader value) => new()
        {
            Tag = Tag.ReverseEndianness(value.Tag),
            Offset = BinaryPrimitives.ReverseEndianness(value.Offset),
        };
    }
}

/// <summary>A script entry in a <see cref="JstfTable"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Pairs a script tag with the resolved <see cref="JstfScript"/> that carries the script's justification data.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF script records</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="JstfTable"/>
/// <seealso cref="JstfScript"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF script records</seealso>
public sealed record JstfScriptRecord
{
    /// <summary>Gets the script tag.</summary>
    /// <value>The four-character script identifier, e.g. <c>"latn"</c>, <c>"arab"</c>.</value>
    /// <seealso cref="Script"/>
    public Tag Tag { get; init; }

    /// <summary>Gets the parsed script.</summary>
    /// <value>The <see cref="JstfScript"/> resolved from the record's offset.</value>
    /// <seealso cref="Tag"/>
    /// <seealso cref="JstfScript"/>
    public JstfScript Script { get; init; } = null!;
}

/// <summary>Justification data for a single script: extender glyphs, a default language system, and zero or more language-specific language systems.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The extender glyph list is optional and applies to the script as a whole, not to a specific language system.</description></item>
/// <item><description>Language-specific records override the default language system when their tag matches the text's language.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF script table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="JstfTable"/>
/// <seealso cref="JstfLangSys"/>
/// <seealso cref="ExtenderGlyph"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF script table</seealso>
public sealed record JstfScript : IRecord<JstfScript>
{
    /// <summary>Gets the extender glyphs, or <c>null</c>.</summary>
    /// <value>The <see cref="ExtenderGlyph"/> table resolved from the script's extender offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="DefaultLangSys"/>
    /// <seealso cref="ExtenderGlyph"/>
    public ExtenderGlyph? ExtenderGlyphs { get; init; }

    /// <summary>Gets the default language system, or <c>null</c>.</summary>
    /// <value>The <see cref="JstfLangSys"/> to use when no language-specific record matches the text's language, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="LangSysRecords"/>
    /// <seealso cref="Find(Tag)"/>
    public JstfLangSys? DefaultLangSys { get; init; }

    /// <summary>Gets the language-specific records, sorted by tag.</summary>
    /// <value>The ordered list of <see cref="JstfLangSysRecord"/> entries, sorted ascending by <see cref="JstfLangSysRecord.Tag"/>.</value>
    /// <seealso cref="Find(Tag)"/>
    /// <seealso cref="JstfLangSysRecord"/>
    public IReadOnlyList<JstfLangSysRecord> LangSysRecords { get; init; } = [];

    /// <summary>Returns the language system for a tag, or <c>null</c>.</summary>
    /// <param name="tag">The language system tag to look up.</param>
    /// <returns>The <see cref="JstfLangSys"/> associated with <paramref name="tag"/>, or <c>null</c> when no record matches.</returns>
    /// <remarks>Return <c>null</c> does not imply that <see cref="DefaultLangSys"/> applies — the caller must decide that separately.</remarks>
    /// <seealso cref="LangSysRecords"/>
    /// <seealso cref="DefaultLangSys"/>
    public JstfLangSys? Find(Tag tag)
    {
        foreach (var r in LangSysRecords) if (r.Tag == tag) return r.LangSys;
        return null;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the JstfScript record.</param>
    /// <param name="context">Unused. The script resolves its subtables through its own source.</param>
    /// <returns>The parsed JstfScript record.</returns>
    /// <exception cref="EndOfStreamException">The header, either referenced subtable, or the language-system array extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both the extender and default-lang-sys offsets are checked for zero before parsing; a zero offset yields a <c>null</c> property.</description></item>
    /// <item><description>The language-system headers are materialized as a struct array and then converted to <see cref="JstfLangSysRecord"/> entries in one pass.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF script table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LangSysHeader"/>
    /// <seealso cref="JstfLangSysRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF script table</seealso>
    public static JstfScript Parse(ref Cursor cursor, object? context)
    {
        ushort extenderOffset = cursor.ReadOffset16();
        ushort defaultLangSysOffset = cursor.ReadOffset16();
        var records = cursor.ReadBigEndianStructArray<LangSysHeader>(cursor.ReadUInt16());

        ExtenderGlyph? extender = extenderOffset != 0
            ? cursor.Source.ParseRecordAt<ExtenderGlyph>(extenderOffset)
            : null;

        JstfLangSys? defaultLangSys = defaultLangSysOffset != 0
            ? cursor.Source.ParseRecordAt<JstfLangSys>(defaultLangSysOffset)
            : null;

        var langSysRecords = new JstfLangSysRecord[records.Length];
        for (int i = 0; i < records.Length; i++)
            langSysRecords[i] = new JstfLangSysRecord
            {
                Tag = records[i].Tag,
                LangSys = cursor.Source.ParseRecordAt<JstfLangSys>(records[i].Offset),
            };

        return new JstfScript
        {
            ExtenderGlyphs = extender,
            DefaultLangSys = defaultLangSys,
            LangSysRecords = langSysRecords,
        };
    }

    /// <summary>The 6-byte language-system header: a tag plus an Offset16 to the language system. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The offset is measured from the start of the enclosing <see cref="JstfScript"/> record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF script table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="JstfScript"/>
    /// <seealso cref="JstfLangSysRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF script table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct LangSysHeader : IEndianReversibleStruct<LangSysHeader>
    {
        /// <summary>Gets the language system tag.</summary>
        /// <value>The four-character language system identifier, e.g. <c>"ENG "</c>, <c>"ARA "</c>.</value>
        /// <seealso cref="Offset"/>
        public Tag Tag;

        /// <summary>Gets the offset to the language system.</summary>
        /// <value>The byte offset of the <see cref="JstfLangSys"/> from the enclosing <see cref="JstfScript"/> record start.</value>
        /// <seealso cref="Tag"/>
        /// <seealso cref="JstfLangSys"/>
        public ushort Offset;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with the tag and offset reversed.</returns>
        /// <remarks>The tag uses <see cref="Tag.ReverseEndianness(Tag)"/>; the offset uses <see cref="BinaryPrimitives.ReverseEndianness(ushort)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static LangSysHeader ReverseEndianness(LangSysHeader value) => new()
        {
            Tag = Tag.ReverseEndianness(value.Tag),
            Offset = BinaryPrimitives.ReverseEndianness(value.Offset)
        };
    }
}

/// <summary>A language system entry in a <see cref="JstfScript"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Pairs a language system tag with the resolved <see cref="JstfLangSys"/> that carries the language's justification priorities.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF script table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="JstfScript"/>
/// <seealso cref="JstfLangSys"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF script table</seealso>
public sealed record JstfLangSysRecord
{
    /// <summary>Gets the language system tag.</summary>
    /// <value>The four-character language system identifier, e.g. <c>"ENG "</c>, <c>"ARA "</c>.</value>
    /// <seealso cref="LangSys"/>
    public Tag Tag { get; init; }

    /// <summary>Gets the parsed language system.</summary>
    /// <value>The <see cref="JstfLangSys"/> resolved from the record's offset.</value>
    /// <seealso cref="Tag"/>
    /// <seealso cref="JstfLangSys"/>
    public JstfLangSys LangSys { get; init; } = null!;
}

/// <summary>A JSTF Language System table: a priority-ordered list of justification priorities. The first priority that can be applied to a line is used.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Priorities are evaluated in order; a client that cannot apply a given priority to a line moves on to the next one.</description></item>
/// <item><description>An empty priorities list means the language system provides no justification data.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF language system table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="JstfScript"/>
/// <seealso cref="JstfPriority"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF language system table</seealso>
public sealed record JstfLangSys : IRecord<JstfLangSys>
{
    /// <summary>Gets the priorities in descending order of preference.</summary>
    /// <value>The ordered list of <see cref="JstfPriority"/> entries; the first entry that a client can apply is used.</value>
    /// <seealso cref="JstfPriority"/>
    public IReadOnlyList<JstfPriority> Priorities { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the language system.</param>
    /// <param name="context">Forwarded to the priority parsers.</param>
    /// <returns>The parsed language system.</returns>
    /// <exception cref="EndOfStreamException">The count, offset array, or any referenced priority extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF language system table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Priorities"/>
    /// <seealso cref="JstfPriority"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF language system table</seealso>
    public static JstfLangSys Parse(ref Cursor cursor, object? context) => new() { Priorities = cursor.ReadOffset16ArrayPeekRecord<JstfPriority>(cursor.ReadUInt16(), context) };
}

/// <summary>A JstfPriority table: pairs of GSUB and GPOS lookup lists used at one stage of justification, for both shrinking and extending text.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The ten offset fields are fixed in order. The five shrinkage fields come first, then the five extension fields. Each of the ten may be NULL.</description></item>
/// <item><description>Each direction has independent enable and disable lists for GSUB and GPOS, plus a reserved <see cref="JstfMax"/> table.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF priority table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="JstfLangSys"/>
/// <seealso cref="JstfModList"/>
/// <seealso cref="JstfMax"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF priority table</seealso>
public sealed record JstfPriority : IRecord<JstfPriority>
{
    /// <summary>Gets the GSUB lookups to enable when shrinking text.</summary>
    /// <value>The <see cref="JstfModList"/> resolved from the first priority offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="ShrinkageDisableGSUB"/>
    /// <seealso cref="JstfModList"/>
    public JstfModList? ShrinkageEnableGSUB { get; init; }

    /// <summary>Gets the GSUB lookups to disable when shrinking text.</summary>
    /// <value>The <see cref="JstfModList"/> resolved from the second priority offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="ShrinkageEnableGSUB"/>
    /// <seealso cref="JstfModList"/>
    public JstfModList? ShrinkageDisableGSUB { get; init; }

    /// <summary>Gets the GPOS lookups to enable when shrinking text.</summary>
    /// <value>The <see cref="JstfModList"/> resolved from the third priority offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="ShrinkageDisableGPOS"/>
    /// <seealso cref="JstfModList"/>
    public JstfModList? ShrinkageEnableGPOS { get; init; }

    /// <summary>Gets the GPOS lookups to disable when shrinking text.</summary>
    /// <value>The <see cref="JstfModList"/> resolved from the fourth priority offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="ShrinkageEnableGPOS"/>
    /// <seealso cref="JstfModList"/>
    public JstfModList? ShrinkageDisableGPOS { get; init; }

    /// <summary>Gets the priority's shrinkage JstfMax table, or <c>null</c>.</summary>
    /// <value>The <see cref="JstfMax"/> resolved from the fifth priority offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="ExtensionJstfMax"/>
    /// <seealso cref="JstfMax"/>
    public JstfMax? ShrinkageJstfMax { get; init; }

    /// <summary>Gets the GSUB lookups to enable when extending text.</summary>
    /// <value>The <see cref="JstfModList"/> resolved from the sixth priority offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="ExtensionDisableGSUB"/>
    /// <seealso cref="JstfModList"/>
    public JstfModList? ExtensionEnableGSUB { get; init; }

    /// <summary>Gets the GSUB lookups to disable when extending text.</summary>
    /// <value>The <see cref="JstfModList"/> resolved from the seventh priority offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="ExtensionEnableGSUB"/>
    /// <seealso cref="JstfModList"/>
    public JstfModList? ExtensionDisableGSUB { get; init; }

    /// <summary>Gets the GPOS lookups to enable when extending text.</summary>
    /// <value>The <see cref="JstfModList"/> resolved from the eighth priority offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="ExtensionDisableGPOS"/>
    /// <seealso cref="JstfModList"/>
    public JstfModList? ExtensionEnableGPOS { get; init; }

    /// <summary>Gets the GPOS lookups to disable when extending text.</summary>
    /// <value>The <see cref="JstfModList"/> resolved from the ninth priority offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="ExtensionEnableGPOS"/>
    /// <seealso cref="JstfModList"/>
    public JstfModList? ExtensionDisableGPOS { get; init; }

    /// <summary>Gets the priority's extension JstfMax table, or <c>null</c>.</summary>
    /// <value>The <see cref="JstfMax"/> resolved from the tenth priority offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="ShrinkageJstfMax"/>
    /// <seealso cref="JstfMax"/>
    public JstfMax? ExtensionJstfMax { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the priority record.</param>
    /// <param name="context">Unused. The priority resolves its subtables through its own source.</param>
    /// <returns>The parsed priority record.</returns>
    /// <exception cref="EndOfStreamException">The offset array or any referenced subtable extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The ten offsets are read in one call, then each is resolved in order against the source. Every offset is checked for zero before parsing; a zero offset yields a <c>null</c> property.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF priority table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ParseModListAt(ref Cursor, ushort)"/>
    /// <seealso cref="ParseMaxAt(ref Cursor, ushort)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF priority table</seealso>
    public static JstfPriority Parse(ref Cursor cursor, object? context)
    {
        // The ten offsets are contiguous uint16. Read them in one call.
        ushort[] o = cursor.ReadUInt16Array(10);
        return new JstfPriority
        {
            ShrinkageEnableGSUB = ParseModListAt(ref cursor, o[0]),
            ShrinkageDisableGSUB = ParseModListAt(ref cursor, o[1]),
            ShrinkageEnableGPOS = ParseModListAt(ref cursor, o[2]),
            ShrinkageDisableGPOS = ParseModListAt(ref cursor, o[3]),
            ShrinkageJstfMax = ParseMaxAt(ref cursor, o[4]),
            ExtensionEnableGSUB = ParseModListAt(ref cursor, o[5]),
            ExtensionDisableGSUB = ParseModListAt(ref cursor, o[6]),
            ExtensionEnableGPOS = ParseModListAt(ref cursor, o[7]),
            ExtensionDisableGPOS = ParseModListAt(ref cursor, o[8]),
            ExtensionJstfMax = ParseMaxAt(ref cursor, o[9]),
        };
    }

    /// <summary>Resolves a JstfModList at the given offset against the priority's source, or returns <c>null</c> when the offset is zero.</summary>
    /// <param name="cursor">Cursor whose <see cref="Cursor.Source"/> provides the JstfPriority-scoped source.</param>
    /// <param name="offset">The byte offset from the priority record start, or <c>0</c> when the mod list is absent.</param>
    /// <returns>The resolved <see cref="JstfModList"/>, or <c>null</c> when <paramref name="offset"/> is zero.</returns>
    /// <exception cref="EndOfStreamException">The offset or the referenced mod list extends past the end of the source.</exception>
    /// <remarks>Used by <see cref="Parse"/> for the eight mod-list offsets in the priority record.</remarks>
    /// <seealso cref="JstfModList"/>
    /// <seealso cref="ParseMaxAt(ref Cursor, ushort)"/>
    public static JstfModList? ParseModListAt(ref Cursor cursor, ushort offset) =>
        offset != 0 ? cursor.Source.ParseRecordAt<JstfModList>(offset) : null;

    /// <summary>Resolves a JstfMax at the given offset against the priority's source, or returns <c>null</c> when the offset is zero.</summary>
    /// <param name="cursor">Cursor whose <see cref="Cursor.Source"/> provides the JstfPriority-scoped source.</param>
    /// <param name="offset">The byte offset from the priority record start, or <c>0</c> when the JstfMax is absent.</param>
    /// <returns>The resolved <see cref="JstfMax"/>, or <c>null</c> when <paramref name="offset"/> is zero.</returns>
    /// <exception cref="EndOfStreamException">The offset or the referenced JstfMax extends past the end of the source.</exception>
    /// <remarks>Used by <see cref="Parse"/> for the two JstfMax offsets in the priority record.</remarks>
    /// <seealso cref="JstfMax"/>
    /// <seealso cref="ParseModListAt(ref Cursor, ushort)"/>
    public static JstfMax? ParseMaxAt(ref Cursor cursor, ushort offset) =>
        offset != 0 ? cursor.Source.ParseRecordAt<JstfMax>(offset) : null;
}

/// <summary>A JstfModList table: lookup indices to enable or disable during one stage of justification.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The indices refer to the enclosing font's GSUB or GPOS <c>LookupList</c>; which list the indices apply to is determined by the field of <see cref="JstfPriority"/> that carries the mod list.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF modification list table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="JstfPriority"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF modification list table</seealso>
public sealed record JstfModList : IRecord<JstfModList>
{
    /// <summary>Gets the lookup indices into the corresponding GSUB or GPOS LookupList.</summary>
    /// <value>The ordered list of lookup indices to enable or disable, depending on which <see cref="JstfPriority"/> field references this mod list.</value>
    /// <seealso cref="JstfPriority"/>
    public IReadOnlyList<ushort> LookupIndices { get; init; } = [];

    /// <summary>Reads the lookup index count and the corresponding index array from the cursor.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the mod list.</param>
    /// <param name="context">Unused. The mod list is self-describing.</param>
    /// <returns>The parsed mod list.</returns>
    /// <exception cref="EndOfStreamException">The count or index array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF modification list table</see> in the OpenType specification.</remarks>
    /// <seealso cref="LookupIndices"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF modification list table</seealso>
    public static JstfModList Parse(ref Cursor cursor, object? context) => new()
    {
        LookupIndices = cursor.ReadUInt16Array(cursor.ReadUInt16()),
    };
}

/// <summary>A JstfMax table: the maximum number of lookup indices permitted for the corresponding priority level. Reserved for future use; the specification requires the table to be present but does not yet define its semantics.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Structurally identical to <see cref="JstfModList"/>; the two exist as distinct types because their intended semantics differ in the specification.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF maximum lookup table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="JstfPriority"/>
/// <seealso cref="JstfModList"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF maximum lookup table</seealso>
public sealed record JstfMax : IRecord<JstfMax>
{
    /// <summary>Gets the lookup indices.</summary>
    /// <value>The ordered list of lookup indices that the priority level permits; the specification reserves the semantics for future use.</value>
    /// <seealso cref="JstfPriority"/>
    public IReadOnlyList<ushort> LookupIndices { get; init; } = [];

    /// <summary>Reads the lookup index count and the corresponding index array from the cursor.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the JstfMax table.</param>
    /// <param name="context">Unused. The table is self-describing.</param>
    /// <returns>The parsed JstfMax table.</returns>
    /// <exception cref="EndOfStreamException">The count or index array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF maximum lookup table</see> in the OpenType specification.</remarks>
    /// <seealso cref="LookupIndices"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF maximum lookup table</seealso>
    public static JstfMax Parse(ref Cursor cursor, object? context) => new()
    {
        LookupIndices = cursor.ReadUInt16Array(cursor.ReadUInt16()),
    };
}

/// <summary>An ExtenderGlyph table: glyphs that can be inserted to extend a line during justification.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Extender glyphs are typically kashida-like strokes or similar connector shapes in cursive scripts.</description></item>
/// <item><description>The glyph list is a flat array; there is no header beyond the count.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF extender glyph table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="JstfScript"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF extender glyph table</seealso>
public sealed record ExtenderGlyph : IRecord<ExtenderGlyph>
{
    /// <summary>Gets the extender glyph IDs, sorted by glyph ID.</summary>
    /// <value>The ordered list of glyph IDs available as extenders for the script.</value>
    /// <seealso cref="Count"/>
    public IReadOnlyList<ushort> Glyphs { get; init; } = [];

    /// <summary>Gets the number of extender glyphs.</summary>
    /// <value>The size of the <see cref="Glyphs"/> list.</value>
    /// <seealso cref="Glyphs"/>
    public int Count => Glyphs.Count;

    /// <summary>Reads the extender count and the corresponding glyph ID array from the cursor.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the extender glyph table.</param>
    /// <param name="context">Unused. The table is self-describing.</param>
    /// <returns>The parsed extender glyph table.</returns>
    /// <exception cref="EndOfStreamException">The count or glyph ID array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">JSTF extender glyph table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Glyphs"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/jstf">OpenType specification: JSTF extender glyph table</seealso>
    public static ExtenderGlyph Parse(ref Cursor cursor, object? context) => new()
    {
        Glyphs = cursor.ReadUInt16Array(cursor.ReadUInt16()),
    };
}
