using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// xref — Cross-reference Table (Apple Advanced Typography)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>xref</c> table: the symbolic names Apple's <c>ftxdumperfuser</c> and <c>ftxenhancer</c> tools associate with the classes, states, entries, and actions of the <c>just</c>, <c>kerx</c>, and <c>morx</c> tables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is an Apple Advanced Typography (AAT) table and is not part of OpenType. Apple documents it in the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html">TrueType Reference Manual, <c>xref</c> table</see>.</description></item>
/// <item><description><c>ftxenhancer</c> lets a designer generate the <c>just</c>, <c>kerx</c>, and <c>morx</c> tables using symbolic names. Those tables cannot store the names themselves, so the names are written to <c>xref</c> instead and lost when <c>ftxenhancer</c> is run without it.</description></item>
/// <item><description>Each <see cref="XrefEntry"/> identifies one named object by four indices — a chain, a subtable, an object type, and an object index — and carries the name as UTF-8 string data addressed by offset from the end of the <see cref="Header"/>.</description></item>
/// <item><description>Any of the four indices may be <see cref="WildcardIndex"/> (<c>-1</c>), which matches every value at that position. <see cref="GetEntry(Tag, int, int, int, int)"/> honors that, preferring the most specific match.</description></item>
/// <item><description>Apple warns that the table is useful for production purposes only and should be removed from shipping fonts.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Header"/>
/// <seealso cref="Entry"/>
/// <seealso cref="XrefEntry"/>
/// <seealso cref="XrefObjectType"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html">TrueType Reference Manual: The <c>xref</c> table</seealso>
public sealed record XrefTable : IFontTable<XrefTable>
{
    /// <summary>Gets the AAT table tag <c>xref</c>.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html">TrueType Reference Manual: The <c>xref</c> table</seealso>
    public static Tag Tag => "xref";

    /// <summary>The current version of the table format, <c>1</c>.</summary>
    /// <remarks>The specification defines version 1 only, so parsing rejects any other version.</remarks>
    /// <seealso cref="Version"/>
    public const uint CurrentVersion = 1;

    /// <summary>The index value that stands for "any value" in an <see cref="Entry"/>'s chain, subtable, type, and index fields, <c>-1</c>.</summary>
    /// <remarks>The specification notes that a wildcard is rarely appropriate, but that all four index fields can use it.</remarks>
    /// <seealso cref="XrefEntry.ChainIndex"/>
    /// <seealso cref="GetEntry(Tag, int, int, int, int)"/>
    public const short WildcardIndex = -1;

    /// <summary>The size, in bytes, of the fixed-layout <see cref="Header"/>.</summary>
    /// <seealso cref="Header"/>
    public const int HeaderSize = 16;

    /// <summary>The size, in bytes, of one fixed-layout <see cref="Entry"/> record.</summary>
    /// <seealso cref="Entry"/>
    public const int EntrySize = 16;

    /// <summary>Gets the version of the table format.</summary>
    /// <remarks>The current version is <see cref="CurrentVersion"/> (1). Parsing rejects any other value because no other version's layout is defined.</remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html">TrueType Reference Manual: The <c>xref</c> table</seealso>
    public uint Version { get; init; }

    /// <summary>Gets the table's flags.</summary>
    /// <remarks>The specification declares the field unused and requires it to be set to 0. It is preserved rather than discarded so that a validator can inspect it and so that the table round-trips.</remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html">TrueType Reference Manual: The <c>xref</c> table</seealso>
    public uint Flags { get; init; }

    /// <summary>Gets the offset from the beginning of the table to the string data, as declared by the header.</summary>
    /// <remarks>Each <see cref="Entry"/>'s <c>stringOffset</c> is relative to this position rather than to the beginning of the table. The names in <see cref="Entries"/> are already resolved, so the property is preserved for round-tripping and diagnostics.</remarks>
    /// <seealso cref="Entry.StringOffset"/>
    public uint StringOffset { get; init; }

    /// <summary>Gets the entries of the table, in table order, each with its name resolved from the string data.</summary>
    /// <remarks>The array is a flat list of fixed-size records; the relationship between an entry and the object it names is expressed entirely by the entry's four indices.</remarks>
    /// <seealso cref="Count"/>
    /// <seealso cref="XrefEntry"/>
    /// <seealso cref="GetEntry(Tag, int, int, int, int)"/>
    public IReadOnlyList<XrefEntry> Entries { get; init; } = [];

    /// <summary>Gets the number of entries in the table.</summary>
    /// <remarks>The size of <see cref="Entries"/>, taken from the header's <c>numEntries</c> field.</remarks>
    /// <seealso cref="Entries"/>
    public int Count => Entries.Count;

    /// <summary>Gets the entries that name objects in one table.</summary>
    /// <param name="tableTag">The tag of the table to select, normally <c>just</c>, <c>kerx</c>, or <c>morx</c>.</param>
    /// <returns>The matching entries, in table order; empty when the table names nothing.</returns>
    /// <seealso cref="Entries"/>
    /// <seealso cref="GetEntry(Tag, int, int, int, int)"/>
    public IReadOnlyList<XrefEntry> GetEntries(Tag tableTag)
    {
        var matches = new List<XrefEntry>();

        foreach (var entry in Entries)
            if (entry.TableTag == tableTag)
                matches.Add(entry);

        return matches;
    }

    /// <summary>Finds the entry that names one object.</summary>
    /// <param name="tableTag">The tag of the table the object belongs to.</param>
    /// <param name="chainIndex">The chain index to match, or <see cref="WildcardIndex"/> to accept any chain.</param>
    /// <param name="subtableIndex">The subtable index to match, or <see cref="WildcardIndex"/> to accept any subtable.</param>
    /// <param name="tableType">The object type to match, normally a value of <see cref="XrefObjectType"/>, or <see cref="WildcardIndex"/> to accept any type.</param>
    /// <param name="tableIndex">The object index to match, or <see cref="WildcardIndex"/> to accept any index.</param>
    /// <returns>The most specific matching entry, or <see langword="null"/> when no entry matches.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>An entry field matches when it equals the requested value or holds <see cref="WildcardIndex"/>.</description></item>
    /// <item><description>When several entries match, the one with the most non-wildcard fields wins, so an entry naming an object exactly takes precedence over one that names its whole chain.</description></item>
    /// <item><description>Indices are zero-based, and <paramref name="tableType"/> counts the implicit classes and states of the owning table, exactly as the specification's example does.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// XrefEntry? entry = xref.GetEntry("morx", 0, 3, (int)XrefObjectType.ClassName, 4);
    /// string? name = entry?.Name; // "myClass"
    /// </code>
    /// </example>
    /// <seealso cref="Entries"/>
    /// <seealso cref="GetName(Tag, int, int, int, int)"/>
    /// <seealso cref="WildcardIndex"/>
    public XrefEntry? GetEntry(Tag tableTag, int chainIndex, int subtableIndex, int tableType, int tableIndex)
    {
        XrefEntry? best = null;
        int bestSpecificity = -1;

        foreach (var entry in Entries)
        {
            if (entry.TableTag != tableTag) continue;
            if (!Matches(entry.ChainIndex, chainIndex)) continue;
            if (!Matches(entry.SubtableIndex, subtableIndex)) continue;
            if (!Matches(entry.TableType, tableType)) continue;
            if (!Matches(entry.TableIndex, tableIndex)) continue;

            int specificity = Specificity(entry);
            if (specificity <= bestSpecificity) continue;

            best = entry;
            bestSpecificity = specificity;
        }

        return best;
    }

    /// <summary>Finds the name of one object.</summary>
    /// <param name="tableTag">The tag of the table the object belongs to.</param>
    /// <param name="chainIndex">The chain index to match, or <see cref="WildcardIndex"/> to accept any chain.</param>
    /// <param name="subtableIndex">The subtable index to match, or <see cref="WildcardIndex"/> to accept any subtable.</param>
    /// <param name="tableType">The object type to match, or <see cref="WildcardIndex"/> to accept any type.</param>
    /// <param name="tableIndex">The object index to match, or <see cref="WildcardIndex"/> to accept any index.</param>
    /// <returns>The name of the most specific matching entry, or <see langword="null"/> when no entry matches.</returns>
    /// <seealso cref="GetEntry(Tag, int, int, int, int)"/>
    public string? GetName(Tag tableTag, int chainIndex, int subtableIndex, int tableType, int tableIndex) =>
        GetEntry(tableTag, chainIndex, subtableIndex, tableType, tableIndex)?.Name;

    /// <summary>Tests one index field of an entry against a requested value.</summary>
    /// <param name="field">The entry's stored index.</param>
    /// <param name="requested">The requested index.</param>
    /// <returns><see langword="true"/> when the field is a wildcard or equals the request.</returns>
    /// <seealso cref="WildcardIndex"/>
    private static bool Matches(short field, int requested) => field == WildcardIndex || field == requested;

    /// <summary>Counts the non-wildcard index fields of an entry, which is how specific a match it is.</summary>
    /// <param name="entry">The entry to score.</param>
    /// <returns>The number of index fields that are not <see cref="WildcardIndex"/>, from 0 through 4.</returns>
    /// <seealso cref="GetEntry(Tag, int, int, int, int)"/>
    private static int Specificity(XrefEntry entry) =>
        (entry.ChainIndex != WildcardIndex ? 1 : 0) +
        (entry.SubtableIndex != WildcardIndex ? 1 : 0) +
        (entry.TableType != WildcardIndex ? 1 : 0) +
        (entry.TableIndex != WildcardIndex ? 1 : 0);

    /// <summary>The 16-byte fixed-layout <c>xref</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Fields are stored in big-endian order at the offsets the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html"><c>xref</c> table specification</see> defines. The header is followed by <see cref="NumEntries"/> 16-byte <see cref="Entry"/> records, and the string data begins at <see cref="StringOffset"/>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="XrefTable"/>
    /// <seealso cref="Entry"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html">TrueType Reference Manual: The <c>xref</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The version of the table format at byte offset 0.</summary>
        /// <remarks>Currently 1.</remarks>
        /// <seealso cref="XrefTable.CurrentVersion"/>
        public uint Version;      // +0

        /// <summary>The flags of the table at byte offset 4.</summary>
        /// <remarks>Currently unused and set to 0.</remarks>
        public uint Flags;        // +4

        /// <summary>The number of entries in the table at byte offset 8.</summary>
        /// <seealso cref="Entry"/>
        public uint NumEntries;   // +8

        /// <summary>The offset from the beginning of the table to the string data at byte offset 12.</summary>
        /// <remarks>An entry's <c>stringOffset</c> is relative to this position.</remarks>
        /// <seealso cref="Entry.StringOffset"/>
        public uint StringOffset; // +12

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>All four fields are <c>uint32</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
            NumEntries = BinaryPrimitives.ReverseEndianness(v.NumEntries),
            StringOffset = BinaryPrimitives.ReverseEndianness(v.StringOffset),
        };
    }

    /// <summary>The 16-byte fixed-layout <c>xref</c> table entry that names one object of a named table.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Fields are stored in big-endian order at the offsets the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html"><c>xref</c> table specification</see> defines.</description></item>
    /// <item><description>The four index fields may each hold <see cref="WildcardIndex"/> (<c>-1</c>) to match any value. Parsing preserves them as stored, and <see cref="XrefTable.GetEntry(Tag, int, int, int, int)"/> interprets them.</description></item>
    /// <item><description><see cref="StringOffset"/> is relative to the header's <c>stringOffset</c>, not to the beginning of the table, and the string at that position is <see cref="StringLength"/> bytes of UTF-8.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="XrefTable"/>
    /// <seealso cref="XrefEntry"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html">TrueType Reference Manual: The <c>xref</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Entry : IEndianReversibleStruct<Entry>
    {
        /// <summary>The tag of the table that uses this entry at byte offset 0.</summary>
        /// <remarks>The specification lists <c>just</c>, <c>kerx</c>, and <c>morx</c>.</remarks>
        /// <seealso cref="XrefEntry.TableTag"/>
        public Tag TableTag;        // +0

        /// <summary>The chain index of the named object at byte offset 4.</summary>
        /// <remarks>Or <see cref="XrefTable.WildcardIndex"/> to match any chain.</remarks>
        public short ChainIndex;    // +4

        /// <summary>The subtable index of the named object at byte offset 6.</summary>
        /// <remarks>Or <see cref="XrefTable.WildcardIndex"/> to match any subtable.</remarks>
        public short SubtableIndex; // +6

        /// <summary>The type of the named object at byte offset 8.</summary>
        /// <remarks>One of the <see cref="XrefObjectType"/> values, or <see cref="XrefTable.WildcardIndex"/> to match any type.</remarks>
        /// <seealso cref="XrefObjectType"/>
        public short TableType;     // +8

        /// <summary>The index of the named object at byte offset 10.</summary>
        /// <remarks>Or <see cref="XrefTable.WildcardIndex"/> to match any index. The index counts the owning table's implicit classes and states.</remarks>
        public short TableIndex;    // +10

        /// <summary>The offset of the name from the beginning of the string data at byte offset 12.</summary>
        /// <seealso cref="StringLength"/>
        /// <seealso cref="Header.StringOffset"/>
        public ushort StringOffset; // +12

        /// <summary>The length of the name in bytes at byte offset 14.</summary>
        /// <remarks>The bytes are UTF-8.</remarks>
        /// <seealso cref="StringOffset"/>
        public ushort StringLength; // +14

        /// <summary>Reverses the byte order of every field in an <see cref="Entry"/>.</summary>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new entry with each field reversed.</returns>
        /// <remarks>The tag counts as a multi-byte field and is reversed as a whole, the same way <see cref="Cursor.ReadTag"/> stores it.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Entry ReverseEndianness(Entry v) => new()
        {
            TableTag = Tag.ReverseEndianness(v.TableTag),
            ChainIndex = BinaryPrimitives.ReverseEndianness(v.ChainIndex),
            SubtableIndex = BinaryPrimitives.ReverseEndianness(v.SubtableIndex),
            TableType = BinaryPrimitives.ReverseEndianness(v.TableType),
            TableIndex = BinaryPrimitives.ReverseEndianness(v.TableIndex),
            StringOffset = BinaryPrimitives.ReverseEndianness(v.StringOffset),
            StringLength = BinaryPrimitives.ReverseEndianness(v.StringLength),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the xref table.</param>
    /// <param name="context">Unused. The xref table is self-contained and declares its own entry count and string offset.</param>
    /// <returns>The parsed xref table, with every entry's name decoded from the string data.</returns>
    /// <exception cref="InvalidDataException">The version is not 1, the string offset lies past the end of the table, the entry count does not fit in the remaining bytes, or an entry's name lies past the end of the table.</exception>
    /// <exception cref="EndOfStreamException">The header or the entry array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both counts are ranges rather than lengths the reader can trust: the entry count is checked against the bytes actually left in the table before the array is read, so an implausible count is reported as the offending field rather than as an end-of-stream failure.</description></item>
    /// <item><description>Names are decoded eagerly, because the string data is addressed by offset and the caller would otherwise have to repeat the header's <c>stringOffset</c> arithmetic.</description></item>
    /// <item><description>The specification requires a name's bytes to be UTF-8 and gives their length explicitly, so no terminator is assumed and none is looked for.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Entry"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html">TrueType Reference Manual: The <c>xref</c> table</seealso>
    static XrefTable IRecord<XrefTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version != CurrentVersion)
            throw new InvalidDataException($"'xref'.version is {header.Version}, expected {CurrentVersion}.");

        if (header.StringOffset > (ulong)cursor.Length)
        {
            throw new InvalidDataException(
                $"'xref'.stringOffset is {header.StringOffset}, which lies past the end of the " +
                $"{cursor.Length}-byte table.");
        }

        if (header.NumEntries > int.MaxValue)
        {
            throw new InvalidDataException(
                $"'xref'.numEntries is {header.NumEntries}, which is more entries than the table can address.");
        }

        int count = (int)header.NumEntries;
        long fitting = cursor.Remaining / EntrySize;
        if (count > fitting)
        {
            throw new InvalidDataException(
                $"'xref'.numEntries is {count}, but only {fitting} entries fit in the remaining " +
                $"{cursor.Remaining} bytes of the table.");
        }

        Entry[] records = cursor.ReadBigEndianStructArray<Entry>(count);
        var entries = new XrefEntry[records.Length];

        for (int i = 0; i < records.Length; i++)
        {
            Entry record = records[i];
            long start = (long)header.StringOffset + record.StringOffset;

            if (start + record.StringLength > cursor.Length)
            {
                throw new InvalidDataException(
                    $"'xref' entry {i}.stringOffset is {record.StringOffset} with length {record.StringLength}, " +
                    $"which lies past the end of the {cursor.Length}-byte table.");
            }

            entries[i] = new XrefEntry
            {
                TableTag = record.TableTag,
                ChainIndex = record.ChainIndex,
                SubtableIndex = record.SubtableIndex,
                TableType = record.TableType,
                TableIndex = record.TableIndex,
                Name = Encoding.UTF8.GetString(cursor.Source.ReadBytesAt(start, record.StringLength)),
            };
        }

        return new XrefTable
        {
            Version = header.Version,
            Flags = header.Flags,
            StringOffset = header.StringOffset,
            Entries = entries,
        };
    }
}

/// <summary>One named object of an <see cref="XrefTable"/>, with the UTF-8 name the table stores for it resolved to a string.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The four indices identify the object within the table the entry names. Each may be <see cref="XrefTable.WildcardIndex"/>, which matches any value.</description></item>
/// <item><description>Indices are zero-based. The index counts the implicit classes and states of the owning table, so the standard classes occupy the first indices even though they have standard names.</description></item>
/// <item><description>Declared as a <c>record</c>, so value equality and <c>ToString</c> are supplied automatically. The compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="XrefTable"/>
/// <seealso cref="XrefTable.Entries"/>
/// <seealso cref="XrefObjectType"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html">TrueType Reference Manual: The <c>xref</c> table</seealso>
public sealed record XrefEntry
{
    /// <summary>Gets the tag of the table that uses this entry.</summary>
    /// <remarks>The specification lists <c>just</c>, <c>kerx</c>, and <c>morx</c>.</remarks>
    /// <seealso cref="XrefTable.GetEntries(Tag)"/>
    public Tag TableTag { get; init; }

    /// <summary>Gets the chain index of the named object, or <see cref="XrefTable.WildcardIndex"/> for any chain.</summary>
    /// <remarks>Meaningful for <c>morx</c>, which is organized into chains; the specification notes that a wildcard is rarely appropriate.</remarks>
    /// <seealso cref="SubtableIndex"/>
    public short ChainIndex { get; init; }

    /// <summary>Gets the subtable index of the named object, or <see cref="XrefTable.WildcardIndex"/> for any subtable.</summary>
    /// <seealso cref="ChainIndex"/>
    public short SubtableIndex { get; init; }

    /// <summary>Gets the raw object type of the named object, or <see cref="XrefTable.WildcardIndex"/> for any type.</summary>
    /// <remarks>See <see cref="ObjectType"/> for the same value decoded as an <see cref="XrefObjectType"/>.</remarks>
    /// <seealso cref="ObjectType"/>
    public short TableType { get; init; }

    /// <summary>Gets the index of the named object within its table, or <see cref="XrefTable.WildcardIndex"/> for any index.</summary>
    /// <seealso cref="TableType"/>
    public short TableIndex { get; init; }

    /// <summary>Gets the name of the object.</summary>
    /// <remarks>The name is the UTF-8 string the entry's offset and length address, decoded during parsing. It is empty when the entry declares a length of zero.</remarks>
    /// <seealso cref="XrefTable.GetName(Tag, int, int, int, int)"/>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the object type decoded as an <see cref="XrefObjectType"/>.</summary>
    /// <remarks>A cast of <see cref="TableType"/>; the value is not one of the defined members when the field holds <see cref="XrefTable.WildcardIndex"/> or a type the specification does not define.</remarks>
    /// <seealso cref="TableType"/>
    /// <seealso cref="XrefObjectType"/>
    public XrefObjectType ObjectType => (XrefObjectType)TableType;
}

/// <summary>The kinds of object an <see cref="XrefEntry"/> can name, as the <c>tableType</c> field of an <c>xref</c> entry encodes them.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The four kinds a state table defines — class, state, entry/transition, and action — are joined by the two kinds a kerning or justification subtable's class table defines: column classes and row classes.</description></item>
/// <item><description>The field is <c>int16</c> on disk and may also hold <see cref="XrefTable.WildcardIndex"/> (<c>-1</c>), which is deliberately not a member of this enumeration.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html"><c>xref</c> table</see> for the field's definition.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="XrefEntry.ObjectType"/>
/// <seealso cref="XrefEntry.TableType"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6xref.html">TrueType Reference Manual: The <c>xref</c> table</seealso>
public enum XrefObjectType : short
{
    /// <summary>A class name, type 0.</summary>
    /// <remarks>Names one class of a state table or class subtable.</remarks>
    /// <seealso cref="StateName"/>
    ClassName = 0,

    /// <summary>A state name, type 1.</summary>
    /// <remarks>Names one state of a state table. The two implicit states, <c>StartOfText</c> and <c>StartOfLine</c>, occupy the first two indices.</remarks>
    /// <seealso cref="ClassName"/>
    StateName = 1,

    /// <summary>An entry or transition name, type 2.</summary>
    /// <remarks>Names one entry of a state table's entry table.</remarks>
    /// <seealso cref="ActionName"/>
    EntryName = 2,

    /// <summary>An action name, type 3.</summary>
    /// <remarks>Names one action a state table's entry can perform.</remarks>
    /// <seealso cref="EntryName"/>
    ActionName = 3,

    /// <summary>A column class name, type 4.</summary>
    /// <remarks>Names one column class of a class table.</remarks>
    /// <seealso cref="RowClassName"/>
    ColumnClassName = 4,

    /// <summary>A row class name, type 5.</summary>
    /// <remarks>Names one row class of a class table.</remarks>
    /// <seealso cref="ColumnClassName"/>
    RowClassName = 5,
}
