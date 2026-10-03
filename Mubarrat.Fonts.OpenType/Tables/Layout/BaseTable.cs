using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;
using Mubarrat.Fonts.OpenType.Tables.Variations;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.OpenType.Tables.Layout;

/// <summary>The <c>BASE</c> table: baseline data for horizontal and vertical text layout.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A font may declare baseline positions for one or both layout directions. Each direction is described by a <see cref="BaseAxis"/>.</description></item>
/// <item><description>Version 1.0 is the base layout; version 1.1 adds an <see cref="ItemVariationStore"/> so that baseline coordinates can vary with design space.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BaseAxis"/>
/// <seealso cref="BaseTagList"/>
/// <seealso cref="BaseScriptList"/>
/// <seealso cref="ItemVariationStore"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE table</seealso>
public sealed record BaseTable : IOpenTypeTable<BaseTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IOpenTypeTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE table</seealso>
    public static Tag Tag => "BASE";

    /// <summary>Gets the major version. Always 1.</summary>
    /// <value>The constant <c>1</c> for a conforming BASE table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>majorVersion</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MinorVersion"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>majorVersion</c></seealso>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version.</summary>
    /// <value>The constant <c>0</c> for a version 1.0 table, or <c>1</c> for a table that carries an item variation store.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>minorVersion</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="ItemVariationStore"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>minorVersion</c></seealso>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the horizontal axis, or <c>null</c>.</summary>
    /// <value>The <see cref="BaseAxis"/> for horizontal layout, or <c>null</c> when the table declares no horizontal axis (the axis offset was zero).</value>
    /// <seealso cref="VerticalAxis"/>
    /// <seealso cref="BaseAxis"/>
    public BaseAxis? HorizontalAxis { get; init; }

    /// <summary>Gets the vertical axis, or <c>null</c>.</summary>
    /// <value>The <see cref="BaseAxis"/> for vertical layout, or <c>null</c> when the table declares no vertical axis (the axis offset was zero).</value>
    /// <seealso cref="HorizontalAxis"/>
    /// <seealso cref="BaseAxis"/>
    public BaseAxis? VerticalAxis { get; init; }

    /// <summary>Gets the item variation store, or <c>null</c>. Present from version 1.1 onward when the font is variable.</summary>
    /// <value>The <see cref="ItemVariationStore"/> resolved from the version 1.1 trailing offset, or <c>null</c> when the table is version 1.0 or the offset was zero.</value>
    /// <seealso cref="MinorVersion"/>
    /// <seealso cref="ItemVariationStore"/>
    public ItemVariationStore? ItemVariationStore { get; init; }

    /// <summary>The 8-byte <c>BASE</c> version 1.0 header. Blittable, no padding. The version 1.1 ItemVariationStore offset is read separately because it is version-gated.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by axis data at the two offsets it declares; both offsets are measured from the BASE table start.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BaseTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the major version. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming BASE table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>majorVersion</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="MinorVersion"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>majorVersion</c></seealso>
        public ushort MajorVersion;        // +0

        /// <summary>Gets the minor version.</summary>
        /// <value>The constant <c>0</c> for a version 1.0 table, or <c>1</c> for a table with a trailing item variation store offset.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>minorVersion</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="MajorVersion"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>minorVersion</c></seealso>
        public ushort MinorVersion;        // +2

        /// <summary>Gets the offset to the horizontal axis, or zero when the axis is absent.</summary>
        /// <value>The byte offset of the horizontal <see cref="BaseAxis"/> from the BASE table start, or zero.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>horizAxisOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="VerticalAxisOffset"/>
        /// <seealso cref="BaseAxis"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>horizAxisOffset</c></seealso>
        public ushort HorizontalAxisOffset;// +4

        /// <summary>Gets the offset to the vertical axis, or zero when the axis is absent.</summary>
        /// <value>The byte offset of the vertical <see cref="BaseAxis"/> from the BASE table start, or zero.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>vertAxisOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="HorizontalAxisOffset"/>
        /// <seealso cref="BaseAxis"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>vertAxisOffset</c></seealso>
        public ushort VerticalAxisOffset;  // +6

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All four fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE header</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            MajorVersion = BinaryPrimitives.ReverseEndianness(v.MajorVersion),
            MinorVersion = BinaryPrimitives.ReverseEndianness(v.MinorVersion),
            HorizontalAxisOffset = BinaryPrimitives.ReverseEndianness(v.HorizontalAxisOffset),
            VerticalAxisOffset = BinaryPrimitives.ReverseEndianness(v.VerticalAxisOffset),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the BASE table.</param>
    /// <param name="context">Unused. BASE is self-contained except for the item variation store, which it resolves internally.</param>
    /// <returns>The parsed BASE table.</returns>
    /// <exception cref="InvalidDataException">The major version is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header, either axis, or any referenced subtable extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The version 1.1 item variation store offset is read only when <see cref="MinorVersion"/> is at least 1; a version 1.0 table does not advance the cursor past its declared extent.</description></item>
    /// <item><description>Every offset field is checked for zero before parsing; a zero offset yields a <c>null</c> property rather than an exception.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="BaseAxis"/>
    /// <seealso cref="ItemVariationStore"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE table</seealso>
    static BaseTable IRecord<BaseTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.MajorVersion != 1)
            throw new InvalidDataException($"'BASE'.majorVersion is {header.MajorVersion}, expected 1.");

        // Version 1.1 appends a 32-bit offset to the ItemVariationStore, relative to the
        // BASE table start. The cursor is at +8 after reading the version 1.0 header.
        uint itemVariationStoreOffset = header.MinorVersion >= 1 ? cursor.ReadUInt32() : 0u;

        return new BaseTable
        {
            MajorVersion = header.MajorVersion,
            MinorVersion = header.MinorVersion,
            HorizontalAxis = header.HorizontalAxisOffset != 0 ? cursor.Source.ParseRecordAt<BaseAxis>(header.HorizontalAxisOffset) : null,
            VerticalAxis = header.VerticalAxisOffset != 0 ? cursor.Source.ParseRecordAt<BaseAxis>(header.VerticalAxisOffset) : null,
            ItemVariationStore = itemVariationStoreOffset != 0 ? cursor.Source.ParseRecordAt<ItemVariationStore>(itemVariationStoreOffset) : null,
        };
    }
}

/// <summary>Baseline structures for one axis of a <see cref="BaseTable"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>An axis names a set of baseline tags and, for each script that participates in the axis, the baseline coordinates to use.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE axis</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BaseTable"/>
/// <seealso cref="BaseTagList"/>
/// <seealso cref="BaseScriptList"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE axis</seealso>
public sealed record BaseAxis : IRecord<BaseAxis>
{
    /// <summary>Gets the baseline tag list.</summary>
    /// <value>The <see cref="BaseTagList"/> that names every baseline this axis declares. Positions in the coordinate arrays of <see cref="BaseValues"/> and <see cref="MinMax"/> correspond one-to-one with the tags in this list.</value>
    /// <seealso cref="BaseTagList"/>
    /// <seealso cref="ScriptList"/>
    public BaseTagList TagList { get; init; } = null!;

    /// <summary>Gets the per-script baseline coordinate data.</summary>
    /// <value>The <see cref="BaseScriptList"/> that maps script tags to their baseline coordinate records.</value>
    /// <seealso cref="BaseScriptList"/>
    /// <seealso cref="TagList"/>
    public BaseScriptList ScriptList { get; init; } = null!;

    /// <summary>The 4-byte BaseAxis header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both offsets are measured from the start of the <see cref="BaseAxis"/> record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE axis</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BaseAxis"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE axis</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the offset to the baseline tag list.</summary>
        /// <value>The byte offset of the <see cref="BaseTagList"/> from the axis start.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>baseTagListOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ScriptListOffset"/>
        /// <seealso cref="BaseTagList"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>baseTagListOffset</c></seealso>
        public ushort TagListOffset;       // +0

        /// <summary>Gets the offset to the script list.</summary>
        /// <value>The byte offset of the <see cref="BaseScriptList"/> from the axis start.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>baseScriptListOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="TagListOffset"/>
        /// <seealso cref="BaseScriptList"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>baseScriptListOffset</c></seealso>
        public ushort ScriptListOffset;    // +2

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both offsets reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE axis</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            TagListOffset = BinaryPrimitives.ReverseEndianness(v.TagListOffset),
            ScriptListOffset = BinaryPrimitives.ReverseEndianness(v.ScriptListOffset),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the axis record.</param>
    /// <param name="context">Unused. The axis resolves its sub-lists through its own source.</param>
    /// <returns>The parsed axis with its tag list and script list resolved.</returns>
    /// <exception cref="EndOfStreamException">The header or either sub-list extends past the end of the axis-scoped source.</exception>
    /// <remarks>Both sub-list offsets are measured from the axis record start; the parser resolves both against <c>cursor.Source</c>, which is axis-scoped.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="BaseTagList"/>
    /// <seealso cref="BaseScriptList"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE axis</seealso>
    static BaseAxis IRecord<BaseAxis>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new BaseAxis
        {
            TagList = cursor.Source.ParseRecordAt<BaseTagList>(header.TagListOffset),
            ScriptList = cursor.Source.ParseRecordAt<BaseScriptList>(header.ScriptListOffset),
        };
    }
}

/// <summary>An array of baseline identification tags. Positions in the coordinate arrays of <see cref="BaseValues"/> and <see cref="MinMax"/> correspond one-to-one with this list.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The tag list is a flat <c>Tag[]</c>; there is no offset table and no header beyond the count.</description></item>
/// <item><description>Standard baseline tags include <c>"romn"</c>, <c>"ideo"</c>, <c>"hang"</c>, <c>"math"</c>, and <c>"icfb"</c>; see the specification for the current registry.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE tag list</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BaseAxis"/>
/// <seealso cref="BaseValues"/>
/// <seealso cref="MinMax"/>
/// <seealso cref="Tag"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE tag list</seealso>
public sealed record BaseTagList : IRecord<BaseTagList>
{
    /// <summary>Gets the baseline tags in declaration order.</summary>
    /// <value>The ordered list of <see cref="Tag"/> entries. A coordinate array's index <c>i</c> refers to the tag at position <c>i</c> in this list.</value>
    /// <seealso cref="Count"/>
    /// <seealso cref="BaseValues"/>
    /// <seealso cref="MinMax"/>
    public IReadOnlyList<Tag> Tags { get; init; } = [];

    /// <summary>Gets the number of baselines.</summary>
    /// <value>The size of the <see cref="Tags"/> list.</value>
    /// <seealso cref="Tags"/>
    public int Count => Tags.Count;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the tag list.</param>
    /// <param name="context">Unused. The tag list is self-describing.</param>
    /// <returns>The parsed tag list.</returns>
    /// <exception cref="EndOfStreamException">The tag array extends past the end of the axis-scoped source.</exception>
    /// <remarks>The count is read first; the parser then consumes exactly that many 4-byte <see cref="Tag"/> records.</remarks>
    /// <seealso cref="Tags"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE tag list</seealso>
    static BaseTagList IRecord<BaseTagList>.Parse(ref Cursor cursor, object? context) => new()
    {
        Tags = cursor.ReadBigEndianStructArray<Tag>(cursor.ReadUInt16()),
    };
}

/// <summary>Per-script baseline data for an axis.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The script list maps each script tag to a <see cref="BaseScript"/> that carries that script's baseline coordinates.</description></item>
/// <item><description>Scripts not present in the list fall back to the default script if one is declared, or to the font's built-in defaults.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE script list</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BaseAxis"/>
/// <seealso cref="BaseScript"/>
/// <seealso cref="BaseScriptRecord"/>
/// <seealso cref="TagOffsetRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE script list</seealso>
public sealed record BaseScriptList : IRecord<BaseScriptList>
{
    /// <summary>Gets the script records, sorted by tag.</summary>
    /// <value>The ordered list of <see cref="BaseScriptRecord"/> entries. Records are sorted ascending by <see cref="BaseScriptRecord.Tag"/>.</value>
    /// <seealso cref="Count"/>
    /// <seealso cref="Find(Tag)"/>
    /// <seealso cref="BaseScriptRecord"/>
    public IReadOnlyList<BaseScriptRecord> Scripts { get; init; } = [];

    /// <summary>Gets the number of scripts.</summary>
    /// <value>The size of the <see cref="Scripts"/> list.</value>
    /// <seealso cref="Scripts"/>
    public int Count => Scripts.Count;

    /// <summary>Returns the script for a tag, or <c>null</c>.</summary>
    /// <param name="tag">The script tag to look up.</param>
    /// <returns>The <see cref="BaseScript"/> associated with <paramref name="tag"/>, or <c>null</c> when no record matches.</returns>
    /// <remarks>The lookup is linear over <see cref="Scripts"/>. Because the list is sorted by tag, a binary search would also be valid.</remarks>
    /// <seealso cref="Scripts"/>
    /// <seealso cref="BaseScript"/>
    public BaseScript? Find(Tag tag)
    {
        foreach (var r in Scripts) if (r.Tag == tag) return r.Script;
        return null;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the script list.</param>
    /// <param name="context">Unused. The list resolves its records through its own source.</param>
    /// <returns>The parsed script list with its records resolved.</returns>
    /// <exception cref="EndOfStreamException">The count, record array, or any referenced script extends past the end of the axis-scoped source.</exception>
    /// <remarks>Each record's <c>offset</c> is relative to the start of the script list; the parser passes <c>cursor.Source</c> (list-scoped) as the parent context so the offset resolves correctly.</remarks>
    /// <seealso cref="BaseScriptRecord"/>
    /// <seealso cref="TagOffsetRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE script list</seealso>
    static BaseScriptList IRecord<BaseScriptList>.Parse(ref Cursor cursor, object? context) => new()
    {
        Scripts = cursor.ReadBigEndianHeaderRecordArray<BaseScriptRecord, TagOffsetRecord>(cursor.ReadUInt16(), new ParentContext(cursor.Source)),
    };
}

/// <summary>A script entry in a <see cref="BaseScriptList"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record pairs a script <see cref="Tag"/> with the <see cref="BaseScript"/> it points at.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE script list</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BaseScriptList"/>
/// <seealso cref="BaseScript"/>
/// <seealso cref="TagOffsetRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE script list</seealso>
public sealed record BaseScriptRecord : IBigEndianHeaderRecord<BaseScriptRecord, TagOffsetRecord>
{
    /// <summary>Gets the script tag.</summary>
    /// <value>The four-character script identifier, e.g. <c>"latn"</c>, <c>"arab"</c>.</value>
    /// <seealso cref="Script"/>
    public Tag Tag { get; init; }

    /// <summary>Gets the parsed script.</summary>
    /// <value>The <see cref="BaseScript"/> resolved from the record's offset.</value>
    /// <seealso cref="Tag"/>
    /// <seealso cref="BaseScript"/>
    public BaseScript Script { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="header">The already-read <see cref="TagOffsetRecord"/> header.</param>
    /// <param name="context">A <see cref="ParentContext"/> whose <see cref="IParentContext.ParentSource"/> is the list-scoped source.</param>
    /// <returns>A new record with its script resolved.</returns>
    /// <remarks>The <c>offset</c> is measured from the start of the enclosing <see cref="BaseScriptList"/>; the parent context carries the list-scoped source so the offset resolves correctly.</remarks>
    /// <seealso cref="TagOffsetRecord"/>
    /// <seealso cref="BaseScript"/>
    static BaseScriptRecord IHeaderRecord<BaseScriptRecord, TagOffsetRecord>.FromHeader(in TagOffsetRecord header, object? context) => new()
    {
        Tag = header.Tag,
        Script = ((ParentContext)context!).ParentSource.ParseRecordAt<BaseScript>(header.Offset),
    };
}

/// <summary>Baseline data for one script: base values plus optional default and language-specific MinMax tables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <see cref="BaseValues"/> give baseline coordinates for the script; the <see cref="DefaultMinMax"/> and per-language overrides give the coordinate extents relative to the baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE script</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BaseScriptList"/>
/// <seealso cref="BaseScriptRecord"/>
/// <seealso cref="BaseValues"/>
/// <seealso cref="MinMax"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE script</seealso>
public sealed record BaseScript : IRecord<BaseScript>
{
    /// <summary>Gets the base values, or <c>null</c>.</summary>
    /// <value>The <see cref="BaseValues"/> that give the script's baseline coordinates, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="DefaultMinMax"/>
    /// <seealso cref="BaseValues"/>
    public BaseValues? BaseValues { get; init; }

    /// <summary>Gets the script's default MinMax, or <c>null</c>.</summary>
    /// <value>The default <see cref="MinMax"/> for the script, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="BaseValues"/>
    /// <seealso cref="LangSysRecords"/>
    /// <seealso cref="MinMax"/>
    public MinMax? DefaultMinMax { get; init; }

    /// <summary>Gets the per-language MinMax records.</summary>
    /// <value>The ordered list of <see cref="BaseLangSysRecord"/> entries. Empty when the script declares no language-specific overrides.</value>
    /// <seealso cref="Find(Tag)"/>
    /// <seealso cref="BaseLangSysRecord"/>
    public IReadOnlyList<BaseLangSysRecord> LangSysRecords { get; init; } = [];

    /// <summary>Returns the MinMax for a language system tag, or <c>null</c>.</summary>
    /// <param name="tag">The language system tag to look up.</param>
    /// <returns>The <see cref="MinMax"/> associated with <paramref name="tag"/>, or <c>null</c> when no record matches.</returns>
    /// <remarks>The lookup is linear over <see cref="LangSysRecords"/>. Return <c>null</c> does not imply that <see cref="DefaultMinMax"/> applies — the caller must decide that separately.</remarks>
    /// <seealso cref="LangSysRecords"/>
    /// <seealso cref="DefaultMinMax"/>
    public MinMax? Find(Tag tag)
    {
        foreach (var r in LangSysRecords) if (r.Tag == tag) return r.MinMax;
        return null;
    }

    /// <summary>The 6-byte BaseScript header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both offsets are measured from the start of the <see cref="BaseScript"/> record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE script</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BaseScript"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE script</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the offset to the base values, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="BaseValues"/> from the script start, or zero.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>baseValuesOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="DefaultMinMaxOffset"/>
        /// <seealso cref="BaseValues"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>baseValuesOffset</c></seealso>
        public ushort BaseValuesOffset;        // +0

        /// <summary>Gets the offset to the default MinMax, or zero when absent.</summary>
        /// <value>The byte offset of the default <see cref="MinMax"/> from the script start, or zero.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>defaultMinMaxOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="BaseValuesOffset"/>
        /// <seealso cref="MinMax"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>defaultMinMaxOffset</c></seealso>
        public ushort DefaultMinMaxOffset;     // +2

        /// <summary>Gets the number of language-specific MinMax records.</summary>
        /// <value>The count of <see cref="BaseLangSysRecord"/> entries that follow the header.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>baseLangSysCount</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="DefaultMinMaxOffset"/>
        /// <seealso cref="BaseLangSysRecord"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>baseLangSysCount</c></seealso>
        public ushort LangSysCount;            // +4

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE script</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            BaseValuesOffset = BinaryPrimitives.ReverseEndianness(v.BaseValuesOffset),
            DefaultMinMaxOffset = BinaryPrimitives.ReverseEndianness(v.DefaultMinMaxOffset),
            LangSysCount = BinaryPrimitives.ReverseEndianness(v.LangSysCount),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the script record.</param>
    /// <param name="context">Unused. The script resolves its subtables through its own source.</param>
    /// <returns>The parsed script.</returns>
    /// <exception cref="EndOfStreamException">The header, either referenced subtable, or the language-system array extends past the end of the list-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both offsets are checked for zero before parsing; a zero offset yields a <c>null</c> property.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE script</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="BaseValues"/>
    /// <seealso cref="MinMax"/>
    /// <seealso cref="BaseLangSysRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE script</seealso>
    static BaseScript IRecord<BaseScript>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new BaseScript
        {
            BaseValues = header.BaseValuesOffset != 0 ? cursor.Source.ParseRecordAt<BaseValues>(header.BaseValuesOffset) : null,
            DefaultMinMax = header.DefaultMinMaxOffset != 0 ? cursor.Source.ParseRecordAt<MinMax>(header.DefaultMinMaxOffset) : null,
            LangSysRecords = cursor.ReadBigEndianHeaderRecordArray<BaseLangSysRecord, TagOffsetRecord>(header.LangSysCount, new ParentContext(cursor.Source)),
        };
    }
}

/// <summary>A language-specific MinMax entry in a <see cref="BaseScript"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record pairs a language system <see cref="Tag"/> with the <see cref="MinMax"/> it points at.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE script</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BaseScript"/>
/// <seealso cref="MinMax"/>
/// <seealso cref="TagOffsetRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE script</seealso>
public sealed record BaseLangSysRecord
    : IBigEndianHeaderRecord<BaseLangSysRecord, TagOffsetRecord>
{
    /// <summary>Gets the language system tag.</summary>
    /// <value>The four-character language system identifier, e.g. <c>"ENG "</c>, <c>"ARA "</c>.</value>
    /// <seealso cref="MinMax"/>
    public Tag Tag { get; init; }

    /// <summary>Gets the parsed MinMax.</summary>
    /// <value>The <see cref="MinMax"/> resolved from the record's offset.</value>
    /// <seealso cref="Tag"/>
    /// <seealso cref="MinMax"/>
    public MinMax MinMax { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="header">The already-read <see cref="TagOffsetRecord"/> header.</param>
    /// <param name="context">A <see cref="ParentContext"/> whose <see cref="IParentContext.ParentSource"/> is the script-scoped source.</param>
    /// <returns>A new record with its MinMax resolved.</returns>
    /// <remarks>The <c>offset</c> is measured from the start of the enclosing <see cref="BaseScript"/>; the parent context carries the script-scoped source so the offset resolves correctly.</remarks>
    /// <seealso cref="TagOffsetRecord"/>
    /// <seealso cref="MinMax"/>
    static BaseLangSysRecord IHeaderRecord<BaseLangSysRecord, TagOffsetRecord>.FromHeader(in TagOffsetRecord header, object? context) => new()
    {
        Tag = header.Tag,
        MinMax = ((ParentContext)context!).ParentSource.ParseRecordAt<MinMax>(header.Offset),
    };
}

/// <summary>Baseline coordinates for a script: one coordinate per tag in the enclosing <see cref="BaseTagList"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The coordinate array is index-aligned with the enclosing <see cref="BaseTagList"/>: entry <c>i</c> gives the coordinate for the baseline whose tag is at position <c>i</c>.</description></item>
/// <item><description>Entries may be <c>null</c> when the corresponding offset is zero; a <c>null</c> entry means the baseline is not defined for that script.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE base values</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BaseScript"/>
/// <seealso cref="BaseTagList"/>
/// <seealso cref="BaseCoord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE base values</seealso>
public sealed record BaseValues : IRecord<BaseValues>
{
    /// <summary>Gets the index of the default baseline within the tag list.</summary>
    /// <value>The zero-based index into <see cref="BaseTagList.Tags"/> that identifies the default baseline for the script.</value>
    /// <seealso cref="Coordinates"/>
    /// <seealso cref="BaseTagList"/>
    public ushort DefaultBaselineIndex { get; init; }

    /// <summary>Gets the coordinate for each baseline, in tag-list order. Entries may be <c>null</c> when the corresponding offset is NULL.</summary>
    /// <value>An array whose length matches the enclosing <see cref="BaseTagList.Count"/>. A <c>null</c> entry means the baseline is not defined for this script.</value>
    /// <seealso cref="DefaultBaselineIndex"/>
    /// <seealso cref="BaseCoord"/>
    /// <seealso cref="BaseTagList"/>
    public IReadOnlyList<BaseCoord?> Coordinates { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the base-values record.</param>
    /// <param name="context">Unused. The record resolves its coordinates through its own source.</param>
    /// <returns>The parsed base-values record.</returns>
    /// <exception cref="EndOfStreamException">The header, offset array, or any referenced coordinate extends past the end of the script-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The coordinate count is read from the cursor immediately after the default-baseline index; the parser then reads that many offset values.</description></item>
    /// <item><description>Each offset is measured from the start of the base-values record; a zero offset produces a <c>null</c> entry in <see cref="Coordinates"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE base values</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Coordinates"/>
    /// <seealso cref="BaseCoord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE base values</seealso>
    static BaseValues IRecord<BaseValues>.Parse(ref Cursor cursor, object? context)
    {
        Source source = cursor.Source;
        return new BaseValues
        {
            DefaultBaselineIndex = cursor.ReadUInt16(),
            Coordinates = cursor.ReadOffset16ArrayInterpret(cursor.ReadUInt16(), off => off != 0 ? source.ParseRecordAt<BaseCoord>(off) : null),
        };
    }
}

/// <summary>A baseline coordinate. Three formats exist, each referencing the coordinate's value differently.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Format 1 stores the coordinate value inline; format 2 adds a reference glyph and point; format 3 adds an optional <see cref="Device"/> or variation-index table.</description></item>
/// <item><description>Formats 1 and 2 use the header-derived parse path, whose header includes the format discriminant. Format 3 uses a custom parse because it resolves an offset to a <see cref="Device"/> subtable, which the header-derived form cannot do.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE base coordinates</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BaseCoordFormat1"/>
/// <seealso cref="BaseCoordFormat2"/>
/// <seealso cref="BaseCoordFormat3"/>
/// <seealso cref="BaseValues"/>
/// <seealso cref="MinMax"/>
/// <seealso cref="Device"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE base coordinates</seealso>
public abstract record BaseCoord : IRecord<BaseCoord>, IBaseRecord<BaseCoord>
{
    /// <summary>Gets the format number (1, 2, or 3).</summary>
    /// <value>The format discriminant from the first <c>uint16</c> of the coordinate record.</value>
    /// <seealso cref="Coordinate"/>
    public ushort Format { get; init; }

    /// <summary>Gets the coordinate value in font design units.</summary>
    /// <value>The signed coordinate, in font design units.</value>
    /// <seealso cref="Format"/>
    public abstract short Coordinate { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the coordinate record.</param>
    /// <param name="context">Unused. The coordinate is self-describing once the format is read.</param>
    /// <returns>The format-specific coordinate.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1, 2, or 3.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE base coordinates</see> in the OpenType specification.</remarks>
    /// <seealso cref="BaseCoordFormat1"/>
    /// <seealso cref="BaseCoordFormat2"/>
    /// <seealso cref="BaseCoordFormat3"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE base coordinates</seealso>
    static BaseCoord IRecord<BaseCoord>.Parse(ref Cursor cursor, object? context)
    {
        ushort format = cursor.ReadUInt16();
        return format switch
        {
            1 => IBaseRecord<BaseCoord>.Parse<BaseCoordFormat1>(ref cursor),
            2 => IBaseRecord<BaseCoord>.Parse<BaseCoordFormat2>(ref cursor),
            3 => IBaseRecord<BaseCoord>.Parse<BaseCoordFormat3>(ref cursor),
            _ => throw new InvalidDataException($"BaseCoord format {format} is not defined."),
        };
    }
}

/// <summary>Format 1: a single int16 coordinate.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Both the format byte (implicit in the header-derived parse path) and the coordinate value are stored inline. There are no subtable references.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE base coordinates</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BaseCoord"/>
/// <seealso cref="BaseCoordFormat2"/>
/// <seealso cref="BaseCoordFormat3"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE base coordinates</seealso>
public sealed record BaseCoordFormat1 : BaseCoord, IBigEndianHeaderDerivedRecord<BaseCoord, BaseCoordFormat1, BaseCoordFormat1.Header>
{
    /// <inheritdoc/>
    /// <value>The signed coordinate value, in font design units.</value>
    /// <seealso cref="Header.Coordinate"/>
    public override short Coordinate { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read header.</param>
    /// <param name="context">Unused.</param>
    /// <returns>A new instance populated from the header.</returns>
    static BaseCoordFormat1 IHeaderDerivedRecord<BaseCoord, BaseCoordFormat1, Header>.FromHeader(in Header header, object? context) => new()
    {
        Format = 1,
        Coordinate = header.Coordinate,
    };

    /// <summary>The 2-byte format 1 body. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>When this struct is read through the header-derived parse path, its first field is the format discriminant; the coordinate follows.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE base coordinates</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BaseCoordFormat1"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE base coordinates</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the coordinate value, in font design units.</summary>
        /// <value>The signed coordinate value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>coordinate</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="BaseCoordFormat1.Coordinate"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>coordinate</c></seealso>
        public short Coordinate;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte field is to be reversed.</param>
        /// <returns>A new header with the coordinate reversed.</returns>
        /// <remarks>The header contains a single multi-byte field.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE base coordinates</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Coordinate = BinaryPrimitives.ReverseEndianness(v.Coordinate),
        };
    }
}

/// <summary>Format 2: a coordinate plus a reference glyph and contour point.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The coordinate value is combined with a reference to a specific point in a specific glyph's outline; consumers that can resolve outlines can compute a physically meaningful baseline from this.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE base coordinates</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BaseCoord"/>
/// <seealso cref="BaseCoordFormat1"/>
/// <seealso cref="BaseCoordFormat3"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE base coordinates</seealso>
public sealed record BaseCoordFormat2
    : BaseCoord,
      IBigEndianHeaderDerivedRecord<BaseCoord, BaseCoordFormat2, BaseCoordFormat2.Header>
{
    /// <inheritdoc/>
    /// <value>The signed coordinate value, in font design units.</value>
    /// <seealso cref="ReferenceGlyph"/>
    /// <seealso cref="BaseCoordPoint"/>
    public override short Coordinate { get; init; }

    /// <summary>Gets the glyph ID whose contour point defines the baseline.</summary>
    /// <value>The glyph ID referenced by this coordinate.</value>
    /// <seealso cref="BaseCoordPoint"/>
    /// <seealso cref="Coordinate"/>
    public ushort ReferenceGlyph { get; init; }

    /// <summary>Gets the point index within the reference glyph's outline.</summary>
    /// <value>The zero-based index of the point within the glyph's outline that defines the coordinate.</value>
    /// <seealso cref="ReferenceGlyph"/>
    /// <seealso cref="Coordinate"/>
    public ushort BaseCoordPoint { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read header.</param>
    /// <param name="context">Unused.</param>
    /// <returns>A new instance populated from the header.</returns>
    static BaseCoordFormat2 IHeaderDerivedRecord<BaseCoord, BaseCoordFormat2, Header>.FromHeader(in Header header, object? context) => new()
    {
        Format = 2,
        Coordinate = header.Coordinate,
        ReferenceGlyph = header.ReferenceGlyph,
        BaseCoordPoint = header.BaseCoordPoint,
    };

    /// <summary>The 6-byte format 2 body. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>When this struct is read through the header-derived parse path, its first field is the format discriminant; the remaining fields follow.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE base coordinates</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BaseCoordFormat2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE base coordinates</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the coordinate value, in font design units.</summary>
        /// <value>The signed coordinate value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>coordinate</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="ReferenceGlyph"/>
        /// <seealso cref="BaseCoordPoint"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>coordinate</c></seealso>
        public short Coordinate;

        /// <summary>Gets the glyph ID whose contour point defines the baseline.</summary>
        /// <value>The glyph ID referenced by this coordinate.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>referenceGlyph</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Coordinate"/>
        /// <seealso cref="BaseCoordPoint"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>referenceGlyph</c></seealso>
        public ushort ReferenceGlyph;

        /// <summary>Gets the point index within the reference glyph's outline.</summary>
        /// <value>The zero-based index of the point within the glyph's outline that defines the coordinate.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>baseCoordPoint</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Coordinate"/>
        /// <seealso cref="ReferenceGlyph"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>baseCoordPoint</c></seealso>
        public ushort BaseCoordPoint;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE base coordinates</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Coordinate = BinaryPrimitives.ReverseEndianness(v.Coordinate),
            ReferenceGlyph = BinaryPrimitives.ReverseEndianness(v.ReferenceGlyph),
            BaseCoordPoint = BinaryPrimitives.ReverseEndianness(v.BaseCoordPoint),
        };
    }
}

/// <summary>Format 3: a coordinate plus an optional Device or VariationIndex table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The format is used when the coordinate needs per-size adjustment through a <see cref="Device"/> table, or per-instance variation through a variation index.</description></item>
/// <item><description>Uses <see cref="IDerivedRecord{TBase, TDerived}"/> rather than the header-derived form because it resolves an offset to a separate subtable.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE base coordinates</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BaseCoord"/>
/// <seealso cref="BaseCoordFormat1"/>
/// <seealso cref="BaseCoordFormat2"/>
/// <seealso cref="Device"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE base coordinates</seealso>
public sealed record BaseCoordFormat3 : BaseCoord, IDerivedRecord<BaseCoord, BaseCoordFormat3>
{
    /// <inheritdoc/>
    /// <value>The signed coordinate value, in font design units.</value>
    /// <seealso cref="DeviceTable"/>
    public override short Coordinate { get; init; }

    /// <summary>Gets the Device or VariationIndex table, or <c>null</c>.</summary>
    /// <value>The <see cref="Device"/> table resolved from the record's offset, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="Coordinate"/>
    /// <seealso cref="Device"/>
    public Device? DeviceTable { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-3 body.</param>
    /// <param name="context">Unused. The record resolves its device table through its own source.</param>
    /// <returns>The parsed format-3 coordinate.</returns>
    /// <exception cref="EndOfStreamException">The header or referenced device table extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE base coordinates</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Device"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE base coordinates</seealso>
    static BaseCoordFormat3 IDerivedRecord<BaseCoord, BaseCoordFormat3>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new BaseCoordFormat3
        {
            Format = 3,
            Coordinate = header.Coordinate,
            DeviceTable = header.DeviceOffset != 0 ? cursor.Source.ParseRecordAt<Device>(header.DeviceOffset) : null,
        };
    }

    /// <summary>The 4-byte format 3 body. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>When this struct is read through the format-3 parse path, its first field is the format discriminant; the coordinate and device offset follow.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE base coordinates</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BaseCoordFormat3"/>
    /// <seealso cref="Device"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE base coordinates</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the coordinate value, in font design units.</summary>
        /// <value>The signed coordinate value.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>coordinate</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="DeviceOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>coordinate</c></seealso>
        public short Coordinate;

        /// <summary>Gets the offset to the device or variation index table, or zero when absent.</summary>
        /// <value>The byte offset of the <see cref="Device"/> table from the coordinate record start, or zero.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>deviceOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Coordinate"/>
        /// <seealso cref="Device"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>deviceOffset</c></seealso>
        public ushort DeviceOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE base coordinates</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Coordinate = BinaryPrimitives.ReverseEndianness(v.Coordinate),
            DeviceOffset = BinaryPrimitives.ReverseEndianness(v.DeviceOffset),
        };
    }
}

/// <summary>Extent of a script or feature relative to the baseline: a minimum and maximum coordinate plus per-feature overrides.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The min and max coordinates describe the vertical extent of text rendered with the script or feature; a renderer uses them to size text boxes and allocate space correctly.</description></item>
/// <item><description>The per-feature overrides let individual features (e.g. ruby, or vertical punctuation) declare a different extent than the script default.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE MinMax</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BaseScript"/>
/// <seealso cref="FeatMinMaxRecord"/>
/// <seealso cref="BaseCoord"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE MinMax</seealso>
public sealed record MinMax : IRecord<MinMax>
{
    /// <summary>Gets the default minimum coordinate, or <c>null</c>.</summary>
    /// <value>The <see cref="BaseCoord"/> that gives the minimum extent for the script or feature, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="MaxCoord"/>
    /// <seealso cref="BaseCoord"/>
    public BaseCoord? MinCoord { get; init; }

    /// <summary>Gets the default maximum coordinate, or <c>null</c>.</summary>
    /// <value>The <see cref="BaseCoord"/> that gives the maximum extent for the script or feature, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="MinCoord"/>
    /// <seealso cref="BaseCoord"/>
    public BaseCoord? MaxCoord { get; init; }

    /// <summary>Gets the per-feature overrides.</summary>
    /// <value>The ordered list of <see cref="FeatMinMaxRecord"/> entries. Empty when the script declares no per-feature overrides.</value>
    /// <seealso cref="Find(Tag)"/>
    /// <seealso cref="FeatMinMaxRecord"/>
    public IReadOnlyList<FeatMinMaxRecord> FeatureOverrides { get; init; } = [];

    /// <summary>Returns the override for a feature tag, or <c>null</c>.</summary>
    /// <param name="tag">The feature tag to look up.</param>
    /// <returns>The <see cref="FeatMinMaxRecord"/> associated with <paramref name="tag"/>, or <c>null</c> when no record matches.</returns>
    /// <remarks>The lookup is linear over <see cref="FeatureOverrides"/>. Return <c>null</c> does not imply that <see cref="MinCoord"/> and <see cref="MaxCoord"/> apply — the caller must decide that separately.</remarks>
    /// <seealso cref="FeatureOverrides"/>
    /// <seealso cref="FeatMinMaxRecord"/>
    public FeatMinMaxRecord? Find(Tag tag)
    {
        foreach (var r in FeatureOverrides) if (r.Tag == tag) return r;
        return null;
    }

    /// <summary>The 6-byte MinMax header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both offsets are measured from the start of the <see cref="MinMax"/> record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE MinMax</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MinMax"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE MinMax</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the offset to the minimum coordinate, or zero when absent.</summary>
        /// <value>The byte offset of the minimum <see cref="BaseCoord"/> from the MinMax start, or zero.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>minCoordOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="MaxCoordOffset"/>
        /// <seealso cref="BaseCoord"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>minCoordOffset</c></seealso>
        public ushort MinCoordOffset;      // +0

        /// <summary>Gets the offset to the maximum coordinate, or zero when absent.</summary>
        /// <value>The byte offset of the maximum <see cref="BaseCoord"/> from the MinMax start, or zero.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>maxCoordOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="MinCoordOffset"/>
        /// <seealso cref="BaseCoord"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>maxCoordOffset</c></seealso>
        public ushort MaxCoordOffset;      // +2

        /// <summary>Gets the number of per-feature overrides.</summary>
        /// <value>The count of <see cref="FeatMinMaxRecord"/> entries that follow the header.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>featMinMaxCount</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="MinCoordOffset"/>
        /// <seealso cref="FeatMinMaxRecord"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>featMinMaxCount</c></seealso>
        public ushort FeatMinMaxCount;     // +4

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE MinMax</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            MinCoordOffset = BinaryPrimitives.ReverseEndianness(v.MinCoordOffset),
            MaxCoordOffset = BinaryPrimitives.ReverseEndianness(v.MaxCoordOffset),
            FeatMinMaxCount = BinaryPrimitives.ReverseEndianness(v.FeatMinMaxCount),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the MinMax record.</param>
    /// <param name="context">Unused. The record resolves its subtables through its own source.</param>
    /// <returns>The parsed MinMax record.</returns>
    /// <exception cref="EndOfStreamException">The header, either referenced coordinate, or the feature array extends past the end of the script-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both offsets are checked for zero before parsing; a zero offset yields a <c>null</c> property.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE MinMax</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="BaseCoord"/>
    /// <seealso cref="FeatMinMaxRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE MinMax</seealso>
    static MinMax IRecord<MinMax>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new MinMax
        {
            MinCoord = header.MinCoordOffset != 0 ? cursor.Source.ParseRecordAt<BaseCoord>(header.MinCoordOffset) : null,
            MaxCoord = header.MaxCoordOffset != 0 ? cursor.Source.ParseRecordAt<BaseCoord>(header.MaxCoordOffset) : null,
            FeatureOverrides = cursor.ReadBigEndianHeaderRecordArray<FeatMinMaxRecord, FeatMinMaxRecord.Header>(header.FeatMinMaxCount, new ParentContext(cursor.Source)),
        };
    }
}

/// <summary>A per-feature MinMax override.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Lets an individual feature override the script's default MinMax extent. The min and max offsets are each optional; when absent, the script-level value applies.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE feature MinMax</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MinMax"/>
/// <seealso cref="BaseCoord"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE feature MinMax</seealso>
public sealed record FeatMinMaxRecord
    : IBigEndianHeaderRecord<FeatMinMaxRecord, FeatMinMaxRecord.Header>
{
    /// <summary>Gets the feature tag.</summary>
    /// <value>The four-character feature identifier, e.g. <c>"kern"</c>, <c>"vert"</c>.</value>
    /// <seealso cref="MinCoord"/>
    /// <seealso cref="MaxCoord"/>
    public Tag Tag { get; init; }

    /// <summary>Gets the feature-specific minimum, or <c>null</c>.</summary>
    /// <value>The <see cref="BaseCoord"/> that gives the feature's minimum extent, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="Tag"/>
    /// <seealso cref="MaxCoord"/>
    /// <seealso cref="BaseCoord"/>
    public BaseCoord? MinCoord { get; init; }

    /// <summary>Gets the feature-specific maximum, or <c>null</c>.</summary>
    /// <value>The <see cref="BaseCoord"/> that gives the feature's maximum extent, or <c>null</c> when the offset was zero.</value>
    /// <seealso cref="Tag"/>
    /// <seealso cref="MinCoord"/>
    /// <seealso cref="BaseCoord"/>
    public BaseCoord? MaxCoord { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read header.</param>
    /// <param name="context">A <see cref="ParentContext"/> whose <see cref="IParentContext.ParentSource"/> is the MinMax-scoped source.</param>
    /// <returns>A new record with its min and max coordinates resolved.</returns>
    /// <remarks>Both offsets are measured from the start of the enclosing <see cref="MinMax"/>; the parent context carries the MinMax-scoped source so the offsets resolve correctly.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="BaseCoord"/>
    static FeatMinMaxRecord IHeaderRecord<FeatMinMaxRecord, Header>.FromHeader(in Header header, object? context)
    {
        Source source = ((ParentContext)context!).ParentSource;
        return new FeatMinMaxRecord
        {
            Tag = header.Tag,
            MinCoord = header.MinOffset != 0 ? source.ParseRecordAt<BaseCoord>(header.MinOffset) : null,
            MaxCoord = header.MaxOffset != 0 ? source.ParseRecordAt<BaseCoord>(header.MaxOffset) : null,
        };
    }

    /// <summary>The 8-byte FeatMinMax record. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both offsets are measured from the start of the enclosing <see cref="MinMax"/> record, not from the start of this header.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">BASE feature MinMax</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatMinMaxRecord"/>
    /// <seealso cref="MinMax"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE feature MinMax</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the feature tag.</summary>
        /// <value>The four-character feature identifier.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>featureTableTag</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="MinOffset"/>
        /// <seealso cref="MaxOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>featureTableTag</c></seealso>
        public Tag Tag;              // +0

        /// <summary>Gets the offset to the feature-specific minimum, or zero when absent.</summary>
        /// <value>The byte offset of the minimum <see cref="BaseCoord"/> from the enclosing MinMax start, or zero.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>minCoordOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Tag"/>
        /// <seealso cref="MaxOffset"/>
        /// <seealso cref="BaseCoord"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>minCoordOffset</c></seealso>
        public ushort MinOffset;     // +4

        /// <summary>Gets the offset to the feature-specific maximum, or zero when absent.</summary>
        /// <value>The byte offset of the maximum <see cref="BaseCoord"/> from the enclosing MinMax start, or zero.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/base"><c>maxCoordOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Tag"/>
        /// <seealso cref="MinOffset"/>
        /// <seealso cref="BaseCoord"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: <c>maxCoordOffset</c></seealso>
        public ushort MaxOffset;     // +6

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are multi-byte and are reversed independently; the tag is reversed through <see cref="Tag.ReverseEndianness(Tag)"/>.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/base">OpenType specification: BASE feature MinMax</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Tag = Tag.ReverseEndianness(v.Tag),
            MinOffset = BinaryPrimitives.ReverseEndianness(v.MinOffset),
            MaxOffset = BinaryPrimitives.ReverseEndianness(v.MaxOffset),
        };
    }
}
