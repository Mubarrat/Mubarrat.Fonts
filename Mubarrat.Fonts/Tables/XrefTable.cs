using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>xref</c> table: symbolic names for AAT class, state, entry, action, and row/column objects. Produced by Apple's font tools for authoring and diagnostics; not consumed by layout engines.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><b>Authoring artifact.</b> The <c>xref</c> table carries the human-readable names that <c>ftxenhancer</c> lets font authors attach to states and classes in the <c>just</c>, <c>kerx</c>, and <c>morx</c> tables. Layout engines ignore it; its sole purpose is round-tripping those names through the toolchain. Apple warns that shipping fonts should not include it.</description></item>
/// <item><description><b>Five-field key.</b> Each entry names one object identified by a table tag, a chain index, a subtable index, an object kind, and an object index. Any of the four index fields can be <c>-1</c> as a wildcard matching any query value for that field.</description></item>
/// <item><description><b>Zero-based indices that count the implicit entries.</b> A state index, for example, counts <c>StartOfText</c> and <c>StartOfLine</c> as slots 0 and 1.</description></item>
/// <item><description><b>Strings are UTF-8.</b> Names live in a shared string region; each entry carries a 16-bit offset into that region and a 16-bit length.</description></item>
/// <item><description>All multi-byte reads use the sfnt default of big-endian.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html"><c>xref</c> table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="XrefEntry"/>
/// <seealso cref="XrefTableType"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html">TrueType Reference Manual: The <c>xref</c> table</seealso>
public sealed record XrefTable : IFontTable<XrefTable>, IRecord<XrefTable>
{
    /// <summary>The only version defined by the current specification.</summary>
    public const uint CurrentVersion = 1;

    /// <inheritdoc/>
    public static Tag Tag => "xref";

    /// <summary>Gets the table version. Always <see cref="CurrentVersion"/>.</summary>
    /// <value>The raw <c>UInt32</c> version read from the header. Unlike most AAT table versions, this is not a <see cref="Fixed"/> 16.16 value.</value>
    /// <seealso cref="CurrentVersion"/>
    public uint Version { get; init; }

    /// <summary>Gets the raw flags word. Currently unused.</summary>
    /// <value>The <c>flags</c> field from the header. The specification currently defines every bit as zero; the parser preserves the value for diagnostics.</value>
    public uint Flags { get; init; }

    /// <summary>Gets the number of entries, as declared in the header.</summary>
    /// <value>The raw <c>numEntries</c> field. Equal to <see cref="Entries"/>.Count for a successfully parsed table; exposed separately so a diagnostics analyzer can compare the declared count against the materialized list.</value>
    /// <seealso cref="Entries"/>
    public uint NumberOfEntries { get; init; }

    /// <summary>Gets the symbolic-name entries, in the order they appear on disk.</summary>
    /// <value>A read-only view over the <see cref="XrefEntry"/> records. Callers that want a specific name should use <see cref="TryGetName"/> or <see cref="FindEntries"/> rather than searching the list manually.</value>
    /// <seealso cref="TryGetName"/>
    /// <seealso cref="FindEntries"/>
    public required IReadOnlyList<XrefEntry> Entries { get; init; }

    /// <summary>Looks up the first entry that matches a query and returns its name.</summary>
    /// <param name="tableTag">The table tag. Must match the entry's tag exactly; <c>xref</c> defines no wildcard for the tag.</param>
    /// <param name="chainIndex">The chain index, or <c>-1</c> to match any chain.</param>
    /// <param name="subtableIndex">The subtable index, or <c>-1</c> to match any subtable.</param>
    /// <param name="tableType">The object kind, or <see cref="XrefTableType.Wildcard"/> to match any kind.</param>
    /// <param name="tableIndex">The object index, or <c>-1</c> to match any object.</param>
    /// <param name="name">When this method returns <see langword="true"/>, contains the matching entry's name; otherwise, <c>null</c>.</param>
    /// <returns><see langword="true"/> when at least one entry matches; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Wildcards are symmetric: an entry field of <c>-1</c> matches any query value for that field, and a query field of <c>-1</c> matches any entry value for that field.</description></item>
    /// <item><description>When multiple entries match, the first one in on-disk order wins. Fonts that mix specific and wildcard entries for the same object should order them specific-first.</description></item>
    /// <item><description>Passing <c>-1</c> for every index field of a query returns the name of the first entry with a matching table tag, regardless of chain/subtable/kind/index. Use <see cref="FindEntries"/> to enumerate all matches.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FindEntries"/>
    public bool TryGetName(Tag tableTag, int chainIndex, int subtableIndex, XrefTableType tableType, int tableIndex, [NotNullWhen(true)] out string? name)
    {
        foreach (XrefEntry entry in Entries)
        {
            if (entry.Matches(tableTag, chainIndex, subtableIndex, tableType, tableIndex))
            {
                name = entry.Name;
                return true;
            }
        }

        name = null;
        return false;
    }

    /// <summary>Enumerates every entry that matches a query.</summary>
    /// <param name="tableTag">The table tag.</param>
    /// <param name="chainIndex">The chain index, or <c>-1</c> to match any chain.</param>
    /// <param name="subtableIndex">The subtable index, or <c>-1</c> to match any subtable.</param>
    /// <param name="tableType">The object kind, or <see cref="XrefTableType.Wildcard"/> to match any kind.</param>
    /// <param name="tableIndex">The object index, or <c>-1</c> to match any object.</param>
    /// <returns>An enumerable yielding each matching entry in on-disk order.</returns>
    /// <remarks>Wildcards are symmetric, as in <see cref="TryGetName"/>.</remarks>
    /// <seealso cref="TryGetName"/>
    public IEnumerable<XrefEntry> FindEntries(Tag tableTag, int chainIndex, int subtableIndex, XrefTableType tableType, int tableIndex)
    {
        foreach (XrefEntry entry in Entries)
            if (entry.Matches(tableTag, chainIndex, subtableIndex, tableType, tableIndex))
                yield return entry;
    }

    static XrefTable IRecord<XrefTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
            throw new InvalidDataException($"'xref'.version is {header.Version}, expected {CurrentVersion}.");

        if (header.NumEntries > int.MaxValue)
            throw new InvalidDataException($"'xref'.numEntries is {header.NumEntries}, which exceeds the supported maximum.");

        return new()
        {
            Version = header.Version,
            Flags = header.Flags,
            NumberOfEntries = header.NumEntries,

            // Every string offset is measured from the string data region's start, not
            // from the table start. Pass both the table-scoped source and the region's
            // start so FromHeader can resolve each entry without slice compensation.
            Entries = Array.AsReadOnly(cursor.ReadEndianReversibleHeaderRecordArray<XrefEntry, XrefEntry.Header>((int)header.NumEntries, new XrefEntry.Context(cursor.Source, header.StringOffset))),
        };
    }

    /// <summary>The 16-byte <c>xref</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All four fields are <c>UInt32</c>, so the reversal is a single word-span pass over four 32-bit words.</description></item>
    /// <item><description>Immediately after this header, <c>numEntries</c> 16-byte <see cref="XrefEntry.Header"/> records follow. The string data region is reached by <see cref="StringOffset"/> from the start of the table.</description></item>
    /// <item><description>The <c>&lt;Clone&gt;$()</c> method generated by the <c>record struct</c> declaration must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0. Currently <c>1</c>.</summary>
        public uint Version;       // +0

        /// <summary>The flags word at byte offset 4. Currently <c>0</c>.</summary>
        public uint Flags;         // +4

        /// <summary>The number of entries at byte offset 8.</summary>
        public uint NumEntries;    // +8

        /// <summary>Byte offset from the table start to the string data, at byte offset 12.</summary>
        public uint StringOffset;  // +12

        /// <summary>Reverses the byte order of all four 32-bit fields in one pass.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with all four fields reversed.</returns>
        /// <remarks>The header is entirely <c>UInt32</c> fields, so it is reinterpreted as a span of four 32-bit words and reversed with a single <see cref="BinaryPrimitives.ReverseEndianness(ReadOnlySpan{uint}, Span{uint})"/> call.</remarks>
        public static Header ReverseEndianness(Header value)
        {
            Span<uint> words = MemoryMarshal.CreateSpan(ref value.Version, 4);
            BinaryPrimitives.ReverseEndianness(words, words);
            return value;
        }
    }
}

/// <summary>A single <c>xref</c> entry: a five-field key naming one object, plus the UTF-8 string the object is called.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The five-field key identifies the object: <see cref="TableTag"/> names the containing table (<c>just</c>, <c>kerx</c>, or <c>morx</c>), and <see cref="ChainIndex"/>, <see cref="SubtableIndex"/>, <see cref="TableType"/>, and <see cref="TableIndex"/> narrow it to one state, class, entry, or action.</description></item>
/// <item><description>Any of the four index fields can be <c>-1</c> as a wildcard, matching every query value for that field. <see cref="TableTag"/> has no wildcard form.</description></item>
/// <item><description>Indices are zero-based, and they count the implicit entries. A state index, for example, counts <c>StartOfText</c> and <c>StartOfLine</c> as slots 0 and 1.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="XrefTable"/>
/// <seealso cref="XrefTableType"/>
public sealed record XrefEntry : IEndianReversibleHeaderRecord<XrefEntry, XrefEntry.Header>
{
    /// <summary>Gets the tag of the table this name applies to.</summary>
    /// <value>One of <c>just</c>, <c>kerx</c>, or <c>morx</c>.</value>
    public required Tag TableTag { get; init; }

    /// <summary>Gets the <c>morx</c> chain index, or <c>-1</c> for a wildcard.</summary>
    public short ChainIndex { get; init; }

    /// <summary>Gets the subtable index within the chain, or <c>-1</c> for a wildcard.</summary>
    public short SubtableIndex { get; init; }

    /// <summary>Gets the object kind, or <see cref="XrefTableType.Wildcard"/> for a wildcard.</summary>
    public XrefTableType TableType { get; init; }

    /// <summary>Gets the object index within its table/chain/subtable, or <c>-1</c> for a wildcard.</summary>
    public short TableIndex { get; init; }

    /// <summary>Gets the object's symbolic name.</summary>
    /// <value>A UTF-8 string of the entry's declared length.</value>
    public required string Name { get; init; }

    /// <summary>Gets a value indicating whether <see cref="ChainIndex"/> is a wildcard.</summary>
    public bool IsChainWildcard => ChainIndex == -1;

    /// <summary>Gets a value indicating whether <see cref="SubtableIndex"/> is a wildcard.</summary>
    public bool IsSubtableWildcard => SubtableIndex == -1;

    /// <summary>Gets a value indicating whether <see cref="TableType"/> is a wildcard.</summary>
    public bool IsTableTypeWildcard => TableType == XrefTableType.Wildcard;

    /// <summary>Gets a value indicating whether <see cref="TableIndex"/> is a wildcard.</summary>
    public bool IsTableIndexWildcard => TableIndex == -1;

    /// <summary>Determines whether this entry matches a query.</summary>
    /// <param name="tableTag">The query's table tag.</param>
    /// <param name="chainIndex">The query's chain index, or <c>-1</c> to match any chain.</param>
    /// <param name="subtableIndex">The query's subtable index, or <c>-1</c> to match any subtable.</param>
    /// <param name="tableType">The query's object kind, or <see cref="XrefTableType.Wildcard"/> to match any kind.</param>
    /// <param name="tableIndex">The query's object index, or <c>-1</c> to match any object.</param>
    /// <returns><see langword="true"/> when the entry's tag matches and every wildcard-free field on both sides agrees; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Wildcards are symmetric: an entry field of <c>-1</c> matches any query value, and a query field of <c>-1</c> matches any entry value.</description></item>
    /// <item><description>The <see cref="TableTag"/> field has no wildcard form on either side; a query must pass a concrete tag.</description></item>
    /// <item><description>The <see cref="TableType"/> comparison operates on the enum value, so <see cref="XrefTableType.Wildcard"/> is a legitimate value in either an entry or a query.</description></item>
    /// </list>
    /// </remarks>
    public bool Matches(Tag tableTag, int chainIndex, int subtableIndex, XrefTableType tableType, int tableIndex)
    {
        if (TableTag != tableTag) return false;
        if (chainIndex != -1 && ChainIndex != -1 && ChainIndex != chainIndex) return false;
        if (subtableIndex != -1 && SubtableIndex != -1 && SubtableIndex != subtableIndex) return false;
        if (tableType != XrefTableType.Wildcard && TableType != XrefTableType.Wildcard && TableType != tableType) return false;
        if (tableIndex != -1 && TableIndex != -1 && TableIndex != tableIndex) return false;
        return true;
    }

    static XrefEntry IHeaderRecord<XrefEntry, Header>.FromHeader(in Header header, object? context)
    {
        if (context is not Context ctx)
            throw new InvalidDataException("'xref' entry requires a Context carrying the table-scoped source and the string data offset.");
        return new()
        {
            TableTag = header.TableTag,
            ChainIndex = header.ChainIndex,
            SubtableIndex = header.SubtableIndex,
            TableType = (XrefTableType)header.TableType,
            TableIndex = header.TableIndex,

            // StringOffset is relative to the string data region, not to the table start.
            // StringDataStart carries the region's table-relative start, so the sum is
            // the table-relative position of the string bytes themselves.
            Name = Encoding.UTF8.GetString(ctx.ParentSource.ReadBytesAt(ctx.StringDataStart + header.StringOffset, header.StringLength)),
        };
    }

    /// <summary>Context for parsing an <see cref="XrefEntry"/>: the table-scoped source and the table-relative string data offset.</summary>
    /// <param name="ParentSource">The table-scoped source; the entry's string offset and length are resolved against it.</param>
    /// <param name="StringDataStart">The table-relative byte offset of the string data region, from the enclosing header's <c>stringOffset</c>.</param>
    /// <remarks>Implements <see cref="IParentContext"/> because it carries the parent source. A <see cref="ParentContext"/> alone is insufficient: the entry's <c>stringOffset</c> is relative to the string data region, not to the table start.</remarks>
    public record Context(Source ParentSource, long StringDataStart) : IParentContext;

    /// <summary>The 16-byte <c>xref</c> entry on disk: a five-field key plus the string offset and length.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The tag is a <see cref="Tag"/> 32-bit value; the four indices are <c>Int16</c> because <c>-1</c> is a wildcard; the string offset and length are <c>UInt16</c>. The struct is mixed-width and is reversed field-by-field.</description></item>
    /// <item><description><see cref="StringOffset"/> is relative to the start of the string data region, not to the start of the table or to this entry.</description></item>
    /// <item><description>The <c>&lt;Clone&gt;$()</c> method generated by the <c>record struct</c> declaration must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The tag of the containing table at byte offset 0.</summary>
        public Tag TableTag;           // +0

        /// <summary>The chain index at byte offset 4, or <c>-1</c> for a wildcard.</summary>
        public short ChainIndex;       // +4

        /// <summary>The subtable index at byte offset 6, or <c>-1</c> for a wildcard.</summary>
        public short SubtableIndex;    // +6

        /// <summary>The object kind at byte offset 8. Values 0–5 are defined; <c>-1</c> is a wildcard.</summary>
        public short TableType;        // +8

        /// <summary>The object index at byte offset 10, or <c>-1</c> for a wildcard.</summary>
        public short TableIndex;       // +10

        /// <summary>The offset from the string data start to this entry's name at byte offset 12.</summary>
        public ushort StringOffset;    // +12

        /// <summary>The length of this entry's name at byte offset 14.</summary>
        public ushort StringLength;    // +14

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>The tag is reversed through <see cref="Tag.ReverseEndianness(Tag)"/>; the indices and length fields use <see cref="BinaryPrimitives.ReverseEndianness(short)"/> and <see cref="BinaryPrimitives.ReverseEndianness(ushort)"/>.</remarks>
        public static Header ReverseEndianness(Header value)
        {
            value.TableTag = Tag.ReverseEndianness(value.TableTag);
            var span = MemoryMarshal.CreateSpan(ref value.ChainIndex, 6);
            BinaryPrimitives.ReverseEndianness(span, span);
            return value;
        }
    }
}

/// <summary>The kind of object a <see cref="XrefEntry"/> names.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The values are drawn from the specification's <c>tableType</c> field. Value <c>-1</c> is not an object kind; it is the wildcard used to match any kind.</description></item>
/// <item><description>The <c>column class name</c> and <c>row class name</c> values apply only to the state-table kinds; other tables use the first four.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="XrefEntry.TableType"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html">TrueType Reference Manual: The <c>xref</c> table</seealso>
public enum XrefTableType : short
{
    /// <summary>The wildcard that matches any object kind.</summary>
    Wildcard = -1,

    /// <summary>A class name.</summary>
    ClassName = 0,

    /// <summary>A state name.</summary>
    StateName = 1,

    /// <summary>An entry or transition name.</summary>
    EntryName = 2,

    /// <summary>An action name.</summary>
    ActionName = 3,

    /// <summary>A column class name.</summary>
    ColumnClassName = 4,

    /// <summary>A row class name.</summary>
    RowClassName = 5,
}
