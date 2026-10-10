using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>A (tag, offset) pair used by ScriptList, Script, and FeatureList.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Blittable, size 6, no padding. Fixed-layout arrays are read in one call via <see cref="Cursor.ReadBigEndianStructArray{T}(int)"/>.</description></item>
/// <item><description>The offset is measured from the start of the containing table, not from the record; the parent table owns the meaning of the base.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#common-table-formats">Common Table Formats</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ScriptList"/>
/// <seealso cref="Script"/>
/// <seealso cref="FeatureList"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#common-table-formats">OpenType specification: Common Table Formats</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct TagOffsetRecord : IEndianReversibleStruct<TagOffsetRecord>
{
    /// <summary>Gets the four-character record tag.</summary>
    /// <value>The identifier used to look up the record in the enclosing sorted list.</value>
    /// <seealso cref="Offset"/>
    public Tag Tag;         // +0

    /// <summary>Gets the offset from the start of the containing table to the target.</summary>
    /// <value>The byte offset of the referenced subtable, measured from the containing table's start.</value>
    /// <seealso cref="Tag"/>
    public ushort Offset;   // +4

    /// <inheritdoc/>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new record with the tag and offset reversed.</returns>
    /// <remarks>The tag uses <see cref="Tag.ReverseEndianness(Tag)"/>; the offset uses <see cref="BinaryPrimitives.ReverseEndianness(ushort)"/>.</remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    public static TagOffsetRecord ReverseEndianness(TagOffsetRecord v) => new()
    {
        Tag = Tag.ReverseEndianness(v.Tag),
        Offset = BinaryPrimitives.ReverseEndianness(v.Offset),
    };
}

/// <summary>Enumerates scripts supported by a GSUB or GPOS table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The list is a sorted array of <see cref="ScriptRecord"/> entries; records are sorted ascending by <see cref="ScriptRecord.Tag"/> so a binary search can locate a script.</description></item>
/// <item><description>Each record points at a <see cref="Script"/> that declares the default and language-specific language systems for that script.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#script-list-table">Script list table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ScriptRecord"/>
/// <seealso cref="Script"/>
/// <seealso cref="TagOffsetRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#script-list-table">OpenType specification: Script list table</seealso>
public sealed record ScriptList : IRecord<ScriptList>
{
    /// <summary>Gets the script records, sorted by tag.</summary>
    /// <value>The ordered list of <see cref="ScriptRecord"/> entries; sorted ascending by <see cref="ScriptRecord.Tag"/>.</value>
    /// <seealso cref="Find(Tag)"/>
    /// <seealso cref="ScriptRecord"/>
    public IReadOnlyList<ScriptRecord> Scripts { get; init; } = [];

    /// <summary>Returns the Script for a tag, or <c>null</c> when the script is absent.</summary>
    /// <param name="tag">The script tag to look up, e.g. <c>"latn"</c>, <c>"arab"</c>.</param>
    /// <returns>The <see cref="Script"/> associated with <paramref name="tag"/>, or <c>null</c> when no record matches.</returns>
    /// <remarks>The lookup is linear over <see cref="Scripts"/>. Because the list is sorted by tag, a binary search would also be valid.</remarks>
    /// <seealso cref="Scripts"/>
    /// <seealso cref="Script"/>
    public Script? Find(Tag tag)
    {
        foreach (var r in Scripts) if (r.Tag == tag) return r.Script;
        return null;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the script list.</param>
    /// <param name="context">Forwarded to the record parsers as a <see cref="ParentContext"/>.</param>
    /// <returns>The parsed script list with its records resolved.</returns>
    /// <exception cref="EndOfStreamException">The count, record array, or any referenced script extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#script-list-table">Script list table</see> in the OpenType specification.</remarks>
    /// <seealso cref="Scripts"/>
    /// <seealso cref="ScriptRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#script-list-table">OpenType specification: Script list table</seealso>
    public static ScriptList Parse(ref Cursor cursor, object? context) => new()
    {
        Scripts = cursor.ReadBigEndianHeaderRecordArray<ScriptRecord, TagOffsetRecord>(cursor.ReadUInt16(), new ParentContext(cursor.Source)),
    };
}

/// <summary>A script entry in a <see cref="ScriptList"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record pairs a script tag with the resolved <see cref="Script"/> that carries the script's language systems.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#script-list-table">Script list table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ScriptList"/>
/// <seealso cref="Script"/>
/// <seealso cref="TagOffsetRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#script-list-table">OpenType specification: Script list table</seealso>
public sealed record ScriptRecord : IEndianReversibleHeaderRecord<ScriptRecord, TagOffsetRecord>
{
    /// <summary>Gets the script tag.</summary>
    /// <value>The four-character script identifier, e.g. <c>"latn"</c>, <c>"arab"</c>.</value>
    /// <seealso cref="Script"/>
    public Tag Tag { get; init; }

    /// <summary>Gets the parsed Script.</summary>
    /// <value>The <see cref="Script"/> resolved from the record's offset.</value>
    /// <seealso cref="Tag"/>
    /// <seealso cref="Script"/>
    public Script Script { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="header">The already-read <see cref="TagOffsetRecord"/> header.</param>
    /// <param name="context">A <see cref="ParentContext"/> whose <see cref="IParentContext.ParentSource"/> is the list-scoped source.</param>
    /// <returns>A new record with its script resolved.</returns>
    /// <remarks>The <c>offset</c> is measured from the start of the enclosing <see cref="ScriptList"/>; the parent context carries the list-scoped source so the offset resolves correctly.</remarks>
    /// <seealso cref="TagOffsetRecord"/>
    /// <seealso cref="Script"/>
    static ScriptRecord IHeaderRecord<ScriptRecord, TagOffsetRecord>.FromHeader(in TagOffsetRecord header, object? context) => new()
    {
        Tag = header.Tag,
        Script = ((ParentContext)context!).ParentSource.ParseRecordAt<Script>(header.Offset),
    };
}

/// <summary>Script table: default and language-specific language systems.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The default language system applies to text whose language the font does not distinguish; language-specific records override it when their tag matches.</description></item>
/// <item><description>Both the default and the language-specific records are optional; a script may declare neither, in which case the script has no applicable layout.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#script-table-and-language-system-record">Script table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ScriptList"/>
/// <seealso cref="LangSys"/>
/// <seealso cref="LangSysRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#script-table-and-language-system-record">OpenType specification: Script table</seealso>
public sealed record Script : IRecord<Script>
{
    /// <summary>Gets the default language system, or <c>null</c>.</summary>
    /// <value>The <see cref="LangSys"/> used when no language-specific record matches, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="LangSysRecords"/>
    /// <seealso cref="Find(Tag)"/>
    public LangSys? DefaultLangSys { get; init; }

    /// <summary>Gets the language-specific records.</summary>
    /// <value>The ordered list of <see cref="LangSysRecord"/> entries, sorted ascending by <see cref="LangSysRecord.Tag"/>.</value>
    /// <seealso cref="Find(Tag)"/>
    /// <seealso cref="LangSysRecord"/>
    public IReadOnlyList<LangSysRecord> LangSysRecords { get; init; } = [];

    /// <summary>Returns the LangSys for a tag, or <c>null</c> when the language system is absent.</summary>
    /// <param name="tag">The language system tag to look up, e.g. <c>"ENG "</c>, <c>"ARA "</c>.</param>
    /// <returns>The <see cref="LangSys"/> associated with <paramref name="tag"/>, or <c>null</c> when no record matches.</returns>
    /// <remarks>Return <c>null</c> does not imply that <see cref="DefaultLangSys"/> applies — the caller must decide that separately.</remarks>
    /// <seealso cref="LangSysRecords"/>
    /// <seealso cref="DefaultLangSys"/>
    public LangSys? Find(Tag tag)
    {
        foreach (var r in LangSysRecords) if (r.Tag == tag) return r.LangSys;
        return null;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the script record.</param>
    /// <param name="context">Forwarded to the language-system parsers as a <see cref="ParentContext"/>.</param>
    /// <returns>The parsed script record.</returns>
    /// <exception cref="EndOfStreamException">The header, default language system, or language-system array extends past the end of the source.</exception>
    /// <remarks>The default language system offset is checked for zero before parsing; a zero offset yields a <c>null</c> property.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="LangSys"/>
    /// <seealso cref="LangSysRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#script-table-and-language-system-record">OpenType specification: Script table</seealso>
    public static Script Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new Script
        {
            DefaultLangSys = header.DefaultLangSysOffset != 0 ? cursor.Source.ParseRecordAt<LangSys>(header.DefaultLangSysOffset) : null,
            LangSysRecords = cursor.ReadBigEndianHeaderRecordArray<LangSysRecord, TagOffsetRecord>(header.LangSysCount, new ParentContext(cursor.Source)),
        };
    }

    /// <summary>The 4-byte Script header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed by <c>langSysCount</c> 6-byte <see cref="TagOffsetRecord"/> entries, each pointing at a language-specific <see cref="LangSys"/>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#script-table-and-language-system-record">Script table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Script"/>
    /// <seealso cref="TagOffsetRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#script-table-and-language-system-record">OpenType specification: Script table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the offset to the default language system, or zero when absent.</summary>
        /// <value>The byte offset of the default <see cref="LangSys"/> from the script record start, or zero.</value>
        /// <seealso cref="LangSysCount"/>
        /// <seealso cref="LangSys"/>
        public ushort DefaultLangSysOffset;   // +0

        /// <summary>Gets the number of language-specific records.</summary>
        /// <value>The count of <see cref="TagOffsetRecord"/> entries that follow the header.</value>
        /// <seealso cref="DefaultLangSysOffset"/>
        /// <seealso cref="TagOffsetRecord"/>
        public ushort LangSysCount;           // +2

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            DefaultLangSysOffset = BinaryPrimitives.ReverseEndianness(v.DefaultLangSysOffset),
            LangSysCount = BinaryPrimitives.ReverseEndianness(v.LangSysCount),
        };
    }
}

/// <summary>A language system entry in a <see cref="Script"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record pairs a language system tag with the resolved <see cref="LangSys"/> that carries the language's feature list.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#script-table-and-language-system-record">Script table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Script"/>
/// <seealso cref="LangSys"/>
/// <seealso cref="TagOffsetRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#script-table-and-language-system-record">OpenType specification: Script table</seealso>
public sealed record LangSysRecord : IEndianReversibleHeaderRecord<LangSysRecord, TagOffsetRecord>
{
    /// <summary>Gets the language system tag.</summary>
    /// <value>The four-character language system identifier, e.g. <c>"ENG "</c>, <c>"ARA "</c>.</value>
    /// <seealso cref="LangSys"/>
    public Tag Tag { get; init; }

    /// <summary>Gets the parsed LangSys.</summary>
    /// <value>The <see cref="LangSys"/> resolved from the record's offset.</value>
    /// <seealso cref="Tag"/>
    /// <seealso cref="LangSys"/>
    public LangSys LangSys { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="header">The already-read <see cref="TagOffsetRecord"/> header.</param>
    /// <param name="context">A <see cref="ParentContext"/> whose <see cref="IParentContext.ParentSource"/> is the script-scoped source.</param>
    /// <returns>A new record with its language system resolved.</returns>
    /// <remarks>The <c>offset</c> is measured from the start of the enclosing <see cref="Script"/>; the parent context carries the script-scoped source so the offset resolves correctly.</remarks>
    /// <seealso cref="TagOffsetRecord"/>
    /// <seealso cref="LangSys"/>
    static LangSysRecord IHeaderRecord<LangSysRecord, TagOffsetRecord>.FromHeader(in TagOffsetRecord header, object? context) => new()
    {
        Tag = header.Tag,
        LangSys = ((ParentContext)context!).ParentSource.ParseRecordAt<LangSys>(header.Offset),
    };
}

/// <summary>Language System table: the features used for a script and language.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table names the features that apply to a script/language combination. A shaper iterates the feature indices, resolves them through the enclosing <see cref="FeatureList"/>, and applies the resulting lookups.</description></item>
/// <item><description>The <see cref="RequiredFeatureIndex"/> is applied unconditionally when present; the <see cref="FeatureIndices"/> are applied only when the user or shaper enables the corresponding feature.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#language-system-table">Language System table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Script"/>
/// <seealso cref="LangSysRecord"/>
/// <seealso cref="FeatureList"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#language-system-table">OpenType specification: Language System table</seealso>
public sealed record LangSys : IRecord<LangSys>
{
    /// <summary>Gets the required feature index, or 0xFFFF when absent.</summary>
    /// <value>The zero-based feature index into the enclosing <see cref="FeatureList"/>, or <c>0xFFFF</c> when no required feature is declared.</value>
    /// <seealso cref="HasRequiredFeature"/>
    /// <seealso cref="FeatureIndices"/>
    public ushort RequiredFeatureIndex { get; init; }

    /// <summary>Gets the feature indices into the FeatureList.</summary>
    /// <value>The ordered list of zero-based indices into the enclosing <see cref="FeatureList"/> that are enabled for this script/language pair.</value>
    /// <seealso cref="RequiredFeatureIndex"/>
    /// <seealso cref="FeatureList"/>
    public IReadOnlyList<ushort> FeatureIndices { get; init; } = [];

    /// <summary>True when a required feature is declared.</summary>
    /// <value><see langword="true"/> when <see cref="RequiredFeatureIndex"/> is not the sentinel value <c>0xFFFF</c>.</value>
    /// <remarks>The specification uses <c>0xFFFF</c> rather than a null offset to indicate the absence of a required feature; this predicate wraps that test.</remarks>
    /// <seealso cref="RequiredFeatureIndex"/>
    public bool HasRequiredFeature => RequiredFeatureIndex != 0xFFFF;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the language system record.</param>
    /// <param name="context">Unused. The record is self-describing once the header is read.</param>
    /// <returns>The parsed language system.</returns>
    /// <exception cref="EndOfStreamException">The header or feature index array extends past the end of the source.</exception>
    /// <remarks>The <c>lookupOrderOffset</c> word is read as part of the header but not surfaced on the record, matching its reserved status in the specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="FeatureIndices"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#language-system-table">OpenType specification: Language System table</seealso>
    public static LangSys Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new LangSys
        {
            RequiredFeatureIndex = header.RequiredFeatureIndex,
            FeatureIndices = cursor.ReadUInt16Array(header.FeatureIndexCount),
        };
    }

    /// <summary>The 6-byte LangSys header. Blittable, no padding. The <c>lookupOrderOffset</c> field is reserved and must be zero; it is read as part of the header so the subsequent fields land at the correct offsets.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The reserved word is present only to preserve the historical layout; it is not surfaced on <see cref="LangSys"/> and is discarded after the header is read.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#language-system-table">Language System table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LangSys"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#language-system-table">OpenType specification: Language System table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the reserved <c>lookupOrderOffset</c> field.</summary>
        /// <value>Always zero in conforming fonts; discarded by <see cref="LangSys.Parse"/> after the header is read.</value>
        /// <seealso cref="RequiredFeatureIndex"/>
        public ushort LookupOrderOffset;      // +0  (reserved, must be 0)

        /// <summary>Gets the required feature index, or <c>0xFFFF</c> when absent.</summary>
        /// <value>The zero-based feature index of the required feature, or the sentinel <c>0xFFFF</c>.</value>
        /// <seealso cref="LookupOrderOffset"/>
        /// <seealso cref="FeatureIndexCount"/>
        public ushort RequiredFeatureIndex;   // +2

        /// <summary>Gets the number of feature indices.</summary>
        /// <value>The count of <c>uint16</c> indices that follow the header.</value>
        /// <seealso cref="RequiredFeatureIndex"/>
        public ushort FeatureIndexCount;      // +4

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            LookupOrderOffset = BinaryPrimitives.ReverseEndianness(v.LookupOrderOffset),
            RequiredFeatureIndex = BinaryPrimitives.ReverseEndianness(v.RequiredFeatureIndex),
            FeatureIndexCount = BinaryPrimitives.ReverseEndianness(v.FeatureIndexCount),
        };
    }
}
