using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>hvpm</c> table: part remapping for Hierarchical Variation Fonts. Replaces parts of an <c>hvgl</c> table by other parts during rendering, using an ascending range map.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><b>Little-endian throughout.</b> Every multi-byte read passes <c>isLittleEndian: true</c>, matching <c>hvgl</c> rather than the big-endian sfnt default.</description></item>
/// <item><description>The table is a single range map from part ID to part ID. Any part ID that falls within a range is mapped by <c>id - Start + Destination</c>; any part ID outside every range is used unmodified.</description></item>
/// <item><description>Ranges must be in ascending order by start and must not overlap. The parser stores the ranges as they appear on disk; ordering and overlap are diagnostics concerns, not parse-time rejections.</description></item>
/// <item><description>Within a <c>.ttc</c> collection, several fonts may share an <c>hvgl</c> table while each carries its own <c>hvpm</c> table (or none), so the remapping is per-font.</description></item>
/// <item><description>For a substitute part to be valid for an original part, the two must share type, axis count, axis semantics, and — for composites — subpart count, recursively down the structure trees. The parser does not verify this; a diagnostics analyzer should.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6hvpm.html"><c>hvpm</c> table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="HvglTable"/>
/// <seealso cref="PartRange"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6hvpm.html">TrueType Reference Manual: The <c>hvpm</c> table</seealso>
public sealed record HvpmTable : IFontTable<HvpmTable>, IRecord<HvpmTable>
{
    /// <summary>The only version defined by the current specification.</summary>
    public const ushort CurrentVersion = 1;

    /// <inheritdoc/>
    public static Tag Tag => "hvpm";

    /// <summary>Gets the table version. Always <see cref="CurrentVersion"/>.</summary>
    /// <value>The value read from the header. The parser rejects any other value.</value>
    /// <seealso cref="CurrentVersion"/>
    public ushort Version { get; init; }

    /// <summary>Gets the raw flags value from the table header.</summary>
    /// <value>The flags word. The specification currently defines every bit as zero.</value>
    /// <remarks>The parser stores the value rather than rejecting non-zero flags; a diagnostics analyzer is the appropriate place to flag them, matching how <see cref="HvglTable.FormatFlags"/> is handled.</remarks>
    public ushort Flags { get; init; }

    /// <summary>Gets the remapping ranges, in the order they appear on disk.</summary>
    /// <value>One <see cref="PartRange"/> per contiguous span of part IDs. Valid tables have ranges in ascending order by start with no overlap.</value>
    /// <remarks>The array length is the mapping subtable's declared count; the parser reads exactly that many entries and does not pad or truncate.</remarks>
    /// <seealso cref="PartRange"/>
    public required PartRange[] Ranges { get; init; }

    /// <summary>Applies the remapping to <paramref name="partId"/>.</summary>
    /// <param name="partId">The part ID to remap.</param>
    /// <returns>The remapped part ID when <paramref name="partId"/> falls within a range, otherwise <paramref name="partId"/> unchanged.</returns>
    /// <remarks>Equivalent to <see cref="TryMapPart(uint, out uint)"/> followed by a fallback to the original ID.</remarks>
    /// <seealso cref="TryMapPart(uint, out uint)"/>
    public uint MapPart(uint partId) => TryMapPart(partId, out uint mapped) ? mapped : partId;

    /// <summary>Attempts to apply the remapping to <paramref name="partId"/>.</summary>
    /// <param name="partId">The part ID to remap.</param>
    /// <param name="mapped">When this method returns <see langword="true"/>, contains the remapped ID; otherwise, contains <paramref name="partId"/> unchanged.</param>
    /// <returns><see langword="true"/> when <paramref name="partId"/> falls within a range; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup is a binary search over <see cref="Ranges"/>, relying on the specification's requirement that ranges are in ascending order by start.</description></item>
    /// <item><description>A table whose ranges are unordered produces undefined results from this method. A diagnostics analyzer should flag that condition; the parser does not check it, per the parse/diagnose split.</description></item>
    /// <item><description>The remapping formula is <c>partId - Start + Destination</c>, applied only when <c>partId</c> lies in <c>[Start, End]</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MapPart(uint)"/>
    /// <seealso cref="PartRange"/>
    public bool TryMapPart(uint partId, out uint mapped)
    {
        int lo = 0;
        int hi = Ranges.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            PartRange range = Ranges[mid];
            if (partId < range.Start)
                hi = mid - 1;
            else if (partId > range.End)
                lo = mid + 1;
            else
            {
                mapped = partId - range.Start + range.Destination;
                return true;
            }
        }

        mapped = partId;
        return false;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidDataException">The version is not <see cref="CurrentVersion"/>, or the range count exceeds the representable maximum.</exception>
    /// <exception cref="EndOfStreamException">A read extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is read via <c>ReadEndianReversibleStruct</c> with <c>isLittleEndian: true</c>, which reverses multi-byte fields only when the host order differs from the wire order. The header is consumed locally and not retained on the record.</description></item>
    /// <item><description>The mapping subtable is reached through <c>MapSubtableOffset</c>, relative to the start of the table. Its <c>count</c> is read as a <c>UInt32</c>, and the ranges are read with a single bulk <c>ReadEndianReversibleStructArray</c> call sized by that count.</description></item>
    /// <item><description>The subtable's <c>count</c> is not stored separately, because <see cref="Ranges"/>'s length is that count by construction.</description></item>
    /// <item><description>The specification requires natural alignment for the <c>UInt32</c> fields; the reader's byte-wise access does not depend on alignment, so alignment is left to a diagnostics rule.</description></item>
    /// </list>
    /// </remarks>
    static HvpmTable IRecord<HvpmTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>(isLittleEndian: true);

        if (header.Version != CurrentVersion)
            throw new InvalidDataException(
                $"'hvpm'.version is {header.Version}, expected {CurrentVersion}.");

        cursor.Position = header.MapSubtableOffset;

        uint count = cursor.ReadUInt32(isLittleEndian: true);
        if (count > int.MaxValue)
            throw new InvalidDataException($"'hvpm'.map.count is {count}, which exceeds the supported maximum.");

        PartRange[] ranges = cursor.ReadEndianReversibleStructArray<PartRange>((int)count, isLittleEndian: true);

        return new()
        {
            Version = header.Version,
            Flags = header.Flags,
            Ranges = ranges,
        };
    }

    /// <summary>The 8-byte <c>hvpm</c> table header. Consumed at parse time and not retained on <see cref="HvpmTable"/>.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All fields are little-endian on disk. Declared as a mutable <c>record struct</c> with <see cref="StructLayoutAttribute"/> so the field layout matches the wire layout exactly.</description></item>
    /// <item><description>The header mixes <c>UInt16</c> and <c>UInt32</c> fields, so <see cref="ReverseEndianness(Header)"/> reverses field-by-field rather than as a uniform word span.</description></item>
    /// <item><description>The <c>&lt;Clone&gt;$()</c> method generated by the <c>record struct</c> declaration must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0. Always <see cref="CurrentVersion"/>.</summary>
        public ushort Version;           // +0

        /// <summary>The flags value at byte offset 2. Currently all zero.</summary>
        public ushort Flags;             // +2

        /// <summary>Byte offset of the mapping subtable from the start of the table, at byte offset 4.</summary>
        public uint MapSubtableOffset;   // +4

        /// <summary>Reverses the byte order of every multi-byte field.</summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(value.Version),
            Flags = BinaryPrimitives.ReverseEndianness(value.Flags),
            MapSubtableOffset = BinaryPrimitives.ReverseEndianness(value.MapSubtableOffset),
        };
    }

    /// <summary>A 12-byte part-mapping range: a contiguous span of part IDs and the first ID it maps to.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>A part ID <c>id</c> in <c>[<see cref="Start"/>, <see cref="End"/>]</c> maps to <c>id - <see cref="Start"/> + <see cref="Destination"/></c>. A part ID outside every range is used unmodified.</description></item>
    /// <item><description>Ranges must be in ascending order by <see cref="Start"/> and must not overlap. The parser does not verify either condition; a diagnostics analyzer should.</description></item>
    /// <item><description>All three fields are <c>UInt32</c>, so <see cref="ReverseEndianness(PartRange)"/> reverses the struct as three consecutive 32-bit words in a single pass.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="HvpmTable.Ranges"/>
    /// <seealso cref="HvpmTable.TryMapPart(uint, out uint)"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct PartRange : IEndianReversibleStruct<PartRange>
    {
        /// <summary>The first part ID of the range, at byte offset 0.</summary>
        public uint Start;        // +0

        /// <summary>The last part ID of the range, at byte offset 4. Must be greater than or equal to <see cref="Start"/>.</summary>
        public uint End;          // +4

        /// <summary>The first part ID that <see cref="Start"/> maps to, at byte offset 8.</summary>
        public uint Destination;  // +8

        /// <summary>Reverses the byte order of every field in one pass.</summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>A new range with all three fields reversed.</returns>
        /// <remarks>The range is entirely <c>UInt32</c> fields, so it is reinterpreted as a span of three 32-bit words and reversed with a single <see cref="BinaryPrimitives.ReverseEndianness(ReadOnlySpan{uint}, Span{uint})"/> call.</remarks>
        public static PartRange ReverseEndianness(PartRange value)
        {
            Span<uint> words = MemoryMarshal.CreateSpan(ref Unsafe.As<PartRange, uint>(ref value), 3);
            BinaryPrimitives.ReverseEndianness(words, words);
            return value;
        }
    }
}
