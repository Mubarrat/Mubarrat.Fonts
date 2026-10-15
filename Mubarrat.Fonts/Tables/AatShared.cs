using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// AAT table components — binary search header, lookup tables, state tables
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>BinSrchHeader</c> structure: the redundant binary-search accelerator that introduces many Apple Advanced Typography lookup and array structures.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The structure exists so that a late-1980s lookup could take a small number of steps and avoid a divide. The manual states plainly that the three derived fields are no longer used; they are parsed and preserved so that a validator can check them against <see cref="UnitSize"/> and <see cref="UnitCount"/>, and so that the table can be round-tripped.</description></item>
/// <item><description>To guarantee termination, a searched array must end with one or more units whose first word is <see cref="EndOfSearch"/> (<c>0xFFFF</c>). The number of such units is table-specific.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Binary Searching Tables</see> for the definition of this structure.</description></item>
/// </list>
/// </remarks>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Binary Searching Tables</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct BinSrchHeader : IEndianReversibleStruct<BinSrchHeader>
{
    /// <summary>The end-of-search sentinel, <c>0xFFFF</c>, that terminates a searched array.</summary>
    /// <remarks>Append one or more units beginning with this value to the array being searched so that the search is guaranteed to terminate.</remarks>
    public const ushort EndOfSearch = 0xFFFF;

    /// <summary>The size of one lookup unit, in bytes, at byte offset 0.</summary>
    /// <remarks>The manual requires at least 6 for segment-based arrays and at least 4 for single-entry arrays.</remarks>
    /// <seealso cref="UnitCount"/>
    public ushort UnitSize;      // +0

    /// <summary>The number of units to be searched at byte offset 2.</summary>
    /// <seealso cref="UnitSize"/>
    public ushort UnitCount;     // +2

    /// <summary>The value of <see cref="UnitSize"/> times the largest power of two less than or equal to <see cref="UnitCount"/>, at byte offset 4.</summary>
    /// <remarks>Derived, and unused by modern consumers.</remarks>
    /// <seealso cref="UnitSize"/>
    /// <seealso cref="UnitCount"/>
    public ushort SearchRange;   // +4

    /// <summary>The base-two logarithm of the largest power of two less than or equal to <see cref="UnitCount"/>, at byte offset 6.</summary>
    /// <remarks>Derived, and unused by modern consumers.</remarks>
    /// <seealso cref="UnitCount"/>
    public ushort EntrySelector; // +6

    /// <summary>The value of <see cref="UnitSize"/> times the difference of <see cref="UnitCount"/> minus the largest power of two less than or equal to it, at byte offset 8.</summary>
    /// <remarks>Derived, and unused by modern consumers.</remarks>
    /// <seealso cref="UnitSize"/>
    /// <seealso cref="UnitCount"/>
    public ushort RangeShift;    // +8

    /// <summary>Reverses the byte order of every field in a <see cref="BinSrchHeader"/>.</summary>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new header with each field reversed.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Binary Searching Tables</seealso>
    public static BinSrchHeader ReverseEndianness(BinSrchHeader v) => new()
    {
        UnitSize = BinaryPrimitives.ReverseEndianness(v.UnitSize),
        UnitCount = BinaryPrimitives.ReverseEndianness(v.UnitCount),
        SearchRange = BinaryPrimitives.ReverseEndianness(v.SearchRange),
        EntrySelector = BinaryPrimitives.ReverseEndianness(v.EntrySelector),
        RangeShift = BinaryPrimitives.ReverseEndianness(v.RangeShift),
    };
}

/// <summary>One <c>LookupSegment</c> of a format 2 lookup table: a contiguous glyph range that shares one two-byte lookup value.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Segments must be sorted by <see cref="LastGlyph"/> so that a binary search over the last glyph of each segment finds the containing segment.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Segment Single Format 2 Lookup Table</see> for the definition of this structure.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LookupTable2"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Segment Single Format 2 Lookup Table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct LookupSegment : IEndianReversibleStruct<LookupSegment>
{
    /// <summary>The last glyph index in the segment at byte offset 0.</summary>
    public ushort LastGlyph;  // +0

    /// <summary>The first glyph index in the segment at byte offset 2.</summary>
    public ushort FirstGlyph; // +2

    /// <summary>The lookup value shared by every glyph in the segment at byte offset 4.</summary>
    public ushort Value;      // +4

    /// <summary>Reverses the byte order of every field in a <see cref="LookupSegment"/>.</summary>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new segment with each field reversed.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Segment Single Format 2 Lookup Table</seealso>
    public static LookupSegment ReverseEndianness(LookupSegment v) => new()
    {
        LastGlyph = BinaryPrimitives.ReverseEndianness(v.LastGlyph),
        FirstGlyph = BinaryPrimitives.ReverseEndianness(v.FirstGlyph),
        Value = BinaryPrimitives.ReverseEndianness(v.Value),
    };
}

/// <summary>One <c>LookupSegment</c> of a format 4 lookup table: a contiguous glyph range whose per-glyph values live at an offset from the start of the lookup table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Segments must be sorted by <see cref="LastGlyph"/>. The value array addressed by <see cref="Offset"/> holds one two-byte value per glyph in the segment, indexed by <c>glyphIndex - FirstGlyph</c>.</description></item>
/// <item><description>The manual calls the third field <c>value</c> and describes it as "a 16-bit offset from the start of the table to the data"; the name <see cref="Offset"/> is used here because the field is not a value but a pointer to a value array.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Segment Array Format 4 Lookup Table</see> for the definition of this structure.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LookupTable4"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Segment Array Format 4 Lookup Table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct LookupSegment4 : IEndianReversibleStruct<LookupSegment4>
{
    /// <summary>The last glyph index in the segment at byte offset 0.</summary>
    public ushort LastGlyph;  // +0

    /// <summary>The first glyph index in the segment at byte offset 2.</summary>
    public ushort FirstGlyph; // +2

    /// <summary>The 16-bit offset from the start of the lookup table to the segment's value array at byte offset 4.</summary>
    public ushort Offset;     // +4

    /// <summary>Reverses the byte order of every field in a <see cref="LookupSegment4"/>.</summary>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new segment with each field reversed.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Segment Array Format 4 Lookup Table</seealso>
    public static LookupSegment4 ReverseEndianness(LookupSegment4 v) => new()
    {
        LastGlyph = BinaryPrimitives.ReverseEndianness(v.LastGlyph),
        FirstGlyph = BinaryPrimitives.ReverseEndianness(v.FirstGlyph),
        Offset = BinaryPrimitives.ReverseEndianness(v.Offset),
    };
}

/// <summary>One <c>LookupSingle</c> entry of a format 6 lookup table: a glyph index and its two-byte lookup value.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Entries must be sorted by <see cref="Glyph"/>.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Single Table Format 6 Lookup Table</see> for the definition of this structure.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LookupTable6"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Single Table Format 6 Lookup Table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct LookupSingle : IEndianReversibleStruct<LookupSingle>
{
    /// <summary>The glyph index at byte offset 0.</summary>
    public ushort Glyph; // +0

    /// <summary>The lookup value at byte offset 2.</summary>
    public ushort Value; // +2

    /// <summary>Reverses the byte order of every field in a <see cref="LookupSingle"/>.</summary>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new entry with each field reversed.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Single Table Format 6 Lookup Table</seealso>
    public static LookupSingle ReverseEndianness(LookupSingle v) => new()
    {
        Glyph = BinaryPrimitives.ReverseEndianness(v.Glyph),
        Value = BinaryPrimitives.ReverseEndianness(v.Value),
    };
}

/// <summary>A glyph-to-value lookup table, one of the six formats Apple Advanced Typography tables use to map glyph indices onto per-glyph information.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table begins with a <c>uint16</c> format and a format-specific header followed by the lookup data. Format 0 is a simple array indexed by glyph index; format 2 maps contiguous glyph ranges onto a shared value; format 4 maps contiguous ranges onto per-glyph values addressed by offset; format 6 is a sorted list of glyph/value pairs; format 8 is a trimmed array of two-byte values; format 10 is a trimmed array whose value unit size is declared.</description></item>
/// <item><description>What a lookup value means depends on the table that owns the lookup. This type therefore only locates values; interpreting them is the owning table's job. For example, in the <c>prop</c> table the value is an index into a properties array, while in <c>morx</c> it is usually a state-table index.</description></item>
/// <item><description>Formats 2, 4, and 6 embed the redundant <see cref="BinSrchHeader"/>, which the manual documents as no longer used. The segments and entries are parsed in full and searched directly instead.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Lookup Tables</see> for the lookup table formats.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LookupTable0"/>
/// <seealso cref="LookupTable2"/>
/// <seealso cref="LookupTable4"/>
/// <seealso cref="LookupTable6"/>
/// <seealso cref="LookupTable8"/>
/// <seealso cref="LookupTable10"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Lookup Tables</seealso>
public abstract record LookupTable : IRecord<LookupTable>, IBaseRecord<LookupTable>
{
    /// <summary>Gets the lookup table format number.</summary>
    /// <value>One of the <c>FormatNumber</c> constants on <see cref="LookupTable"/>.</value>
    public abstract ushort Format { get; }

    /// <summary>Gets the size, in bytes, of one lookup value in this table.</summary>
    /// <value>Two for formats 0, 2, 4, 6, and 8; the declared unit size for format 10, which the specification restricts to 1, 2, 4, or 8.</value>
    public abstract int ValueSize { get; }

    /// <summary>Gets the number of entries or segments in the table.</summary>
    /// <value>
    /// <list type="bullet">
    /// <item><description>For formats 0, 8, and 10 the count is the number of glyphs in the dense range the table covers.</description></item>
    /// <item><description>For formats 2, 4, and 6 the count is the number of segments or entries; it is not the number of glyphs the table can answer for, because those formats cover sparse sets.</description></item>
    /// </list>
    /// </value>
    public abstract int Count { get; }

    /// <summary>Tries to find the lookup value for a glyph index.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <param name="value">When this method returns <see langword="true"/>, contains the value; otherwise, zero.</param>
    /// <returns><see langword="true"/> when the table covers <paramref name="glyphIndex"/>; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The value is returned as a 32-bit unsigned integer regardless of the table's declared value size. A format 10 table whose unit size exceeds four bytes cannot represent its full value through this method; callers that need the wide value should read <see cref="LookupTable10.Values"/> directly.</description></item>
    /// <item><description>Zero is a valid lookup value in several tables, so callers that must distinguish "absent" from "zero" should use this method rather than <see cref="GetValue(int)"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetValue(int)"/>
    public abstract bool TryGetValue(int glyphIndex, out uint value);

    /// <summary>Finds the lookup value for a glyph index, or returns zero when the table does not cover it.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <returns>The lookup value, or <c>0</c> when the table does not cover <paramref name="glyphIndex"/>.</returns>
    /// <remarks>Zero is a valid lookup value in several tables, so callers that must distinguish "absent" from "zero" should use <see cref="TryGetValue(int, out uint)"/>.</remarks>
    /// <seealso cref="TryGetValue(int, out uint)"/>
    public uint GetValue(int glyphIndex) => TryGetValue(glyphIndex, out uint value) ? value : 0u;

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the lookup table's format word.</param>
    /// <param name="context">An optional <see cref="Context"/> carrying the glyph count, or <see langword="null"/>. Only format 0 consumes it; the other five formats read their own extents.</param>
    /// <returns>The parsed lookup table, typed as the format-specific subclass for the declared format.</returns>
    /// <exception cref="InvalidDataException">The format is not one of 0, 2, 4, 6, 8, or 10, or a format 10 unit size is not 1, 2, 4, or 8.</exception>
    /// <exception cref="EndOfStreamException">The lookup extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The dispatcher reads the leading <c>uint16</c> format word and forwards the cursor, the context, and control to the matching derived parser.</description></item>
    /// <item><description>The cursor is left at the first byte after the lookup table's data, so a caller can read a sequence of lookups back to back.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LookupTable0"/>
    /// <seealso cref="LookupTable2"/>
    /// <seealso cref="LookupTable4"/>
    /// <seealso cref="LookupTable6"/>
    /// <seealso cref="LookupTable8"/>
    /// <seealso cref="LookupTable10"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Lookup Tables</seealso>
    static LookupTable IRecord<LookupTable>.Parse(ref Cursor cursor, object? context)
    {
        ushort format = cursor.ReadUInt16();
        return format switch
        {
            0 => IBaseRecord<LookupTable>.Parse<LookupTable0>(ref cursor, context),
            2 => IBaseRecord<LookupTable>.Parse<LookupTable2>(ref cursor, context),
            4 => IBaseRecord<LookupTable>.Parse<LookupTable4>(ref cursor, context),
            6 => IBaseRecord<LookupTable>.Parse<LookupTable6>(ref cursor, context),
            8 => IBaseRecord<LookupTable>.Parse<LookupTable8>(ref cursor, context),
            10 => IBaseRecord<LookupTable>.Parse<LookupTable10>(ref cursor, context),
            _ => throw new InvalidDataException(
                $"AAT lookup table format is {format}, expected 0, 2, 4, 6, 8, or 10."),
        };
    }

    /// <summary>Context for a lookup table parse. Currently carries the glyph count that format 0 needs to size its dense array.</summary>
    /// <param name="GlyphCount">The number of glyphs in the font, from <c>maxp.numGlyphs</c>.</param>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Only format 0 consumes the glyph count; the other five formats carry their own extents and ignore the context.</description></item>
    /// <item><description>A caller may pass <see langword="null"/> as the context; format 0 then falls back to reading its array up to the end of the table-scoped source.</description></item>
    /// <item><description>The context is a record type so that the wrapped glyph count cannot be confused with a bare integer of a different width at the call site.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LookupTable0"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Simple Array Format 0 Lookup Table</seealso>
    public record Context(int GlyphCount);
}

/// <summary>An AAT format 0 lookup table: a simple array of two-byte lookup values indexed directly by glyph index.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The array is not trimmed and has no declared length of its own, so the lookup covers glyph indices <c>0</c> through <c>count - 1</c> and the array must be sized to the font's glyph count.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Simple Array Format 0 Lookup Table</see> for the definition of this format.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Simple Array Format 0 Lookup Table</seealso>
public sealed record LookupTable0 : LookupTable, IDerivedRecord<LookupTable, LookupTable0>
{
    /// <summary>Gets the lookup values, indexed by glyph index.</summary>
    /// <value>An array of two-byte values; index <c>i</c> is the lookup value for glyph <c>i</c>.</value>
    public IReadOnlyList<ushort> Values { get; init; } = [];

    /// <inheritdoc/>
    public override ushort Format => 0;

    /// <inheritdoc/>
    public override int ValueSize => 2;

    /// <inheritdoc/>
    public override int Count => Values.Count;

    /// <inheritdoc/>
    public override bool TryGetValue(int glyphIndex, out uint value)
    {
        if ((uint)glyphIndex < (uint)Values.Count)
        {
            value = Values[glyphIndex];
            return true;
        }

        value = 0;
        return false;
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the format word.</param>
    /// <param name="context">A glyph count as an <see cref="int"/>, or <see langword="null"/> to read the array to the end of the source.</param>
    /// <returns>The parsed lookup table.</returns>
    /// <exception cref="InvalidDataException">The declared format is not 0.</exception>
    /// <exception cref="EndOfStreamException">The array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// A format 0 array carries no length of its own. The number of entries is therefore taken from <paramref name="context"/> when a glyph count is supplied — which is what the owning <c>morx</c>, <c>kerx</c>, and <c>prop</c> tables do, since each knows <c>maxp.numGlyphs</c> — and otherwise from the remainder of the source.
    /// </remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Simple Array Format 0 Lookup Table</seealso>
    static LookupTable0 IDerivedRecord<LookupTable, LookupTable0>.Parse(ref Cursor cursor, object? context)
    {
        int count = context is Context ctx ? ctx.GlyphCount : checked((int)(cursor.Remaining / 2));
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return new LookupTable0 { Values = cursor.ReadUInt16Array(count) };
    }
}

/// <summary>An AAT format 2 lookup table: contiguous glyph ranges, each sharing one two-byte lookup value.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The segment array is introduced by a redundant <see cref="BinSrchHeader"/> whose units are of type <see cref="LookupSegment"/> and are at least six bytes long. Segments are sorted by <see cref="LookupSegment.LastGlyph"/>.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Segment Single Format 2 Lookup Table</see> for the definition of this format.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LookupTable"/>
/// <seealso cref="LookupSegment"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Segment Single Format 2 Lookup Table</seealso>
public sealed record LookupTable2 : LookupTable, IDerivedRecord<LookupTable, LookupTable2>
{
    /// <summary>Gets the binary search header that introduces the segment array.</summary>
    /// <value>The on-disk <see cref="BinSrchHeader"/>, preserved for round-tripping and validation. The segments are searched directly.</value>
    public BinSrchHeader BinSrch { get; init; }

    /// <summary>Gets the segments, sorted by <see cref="LookupSegment.LastGlyph"/>.</summary>
    /// <value>The segment array as stored, in table order.</value>
    public IReadOnlyList<LookupSegment> Segments { get; init; } = [];

    /// <inheritdoc/>
    public override ushort Format => 2;

    /// <inheritdoc/>
    public override int ValueSize => 2;

    /// <inheritdoc/>
    public override int Count => Segments.Count;

    /// <inheritdoc/>
    public override bool TryGetValue(int glyphIndex, out uint value)
    {
        if (TryFindSegment(glyphIndex, out var segment))
        {
            value = segment.Value;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>Finds the segment that contains <paramref name="glyphIndex"/>.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <param name="segment">When this method returns <see langword="true"/>, contains the containing segment.</param>
    /// <returns><see langword="true"/> when a segment contains the glyph index; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The search is a binary search on <see cref="LookupSegment.LastGlyph"/>, which the format requires to be sorted.</description></item>
    /// <item><description>Because segments need not be adjacent, the search narrows to a single candidate and then verifies <see cref="LookupSegment.FirstGlyph"/>.</description></item>
    /// </list>
    /// </remarks>
    public bool TryFindSegment(int glyphIndex, out LookupSegment segment)
    {
        int lo = 0;
        int hi = Segments.Count - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            var candidate = Segments[mid];
            int cmp = candidate.LastGlyph - glyphIndex;

            if (cmp == 0)
            {
                if (candidate.FirstGlyph <= glyphIndex)
                {
                    segment = candidate;
                    return true;
                }

                segment = default;
                return false;
            }

            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        if (lo < Segments.Count && Segments[lo].FirstGlyph <= glyphIndex)
        {
            segment = Segments[lo];
            return true;
        }

        segment = default;
        return false;
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the format word.</param>
    /// <param name="context">Ignored; the lookup declares how many segments it has.</param>
    /// <returns>The parsed lookup table.</returns>
    /// <exception cref="InvalidDataException">The declared format is not 2.</exception>
    /// <exception cref="EndOfStreamException">The segment array extends past the end of the table-scoped source.</exception>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Segment Single Format 2 Lookup Table</seealso>
    static LookupTable2 IDerivedRecord<LookupTable, LookupTable2>.Parse(ref Cursor cursor, object? context)
    {
        BinSrchHeader binSrch = cursor.ReadEndianReversibleStruct<BinSrchHeader>();
        return new() { BinSrch = binSrch, Segments = cursor.ReadEndianReversibleStructArray<LookupSegment>(binSrch.UnitCount) };
    }
}

/// <summary>An AAT format 4 lookup table: contiguous glyph ranges whose per-glyph values live in separate arrays addressed by offset.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The segment array is introduced by a redundant <see cref="BinSrchHeader"/> whose units are of type <see cref="LookupSegment4"/> and are at least six bytes long. Segments are sorted by <see cref="LookupSegment4.LastGlyph"/>.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Segment Array Format 4 Lookup Table</see> for the definition of this format.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LookupTable"/>
/// <seealso cref="LookupSegment4"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Segment Array Format 4 Lookup Table</seealso>
public sealed record LookupTable4 : LookupTable, IDerivedRecord<LookupTable, LookupTable4>
{
    /// <summary>Gets the binary search header that introduces the segment array.</summary>
    /// <value>The on-disk <see cref="BinSrchHeader"/>, preserved for round-tripping and validation. The segments are searched directly.</value>
    public BinSrchHeader BinSrch { get; init; }

    /// <summary>Gets the segments, sorted by <see cref="LookupSegment4.LastGlyph"/>.</summary>
    /// <value>The segment array as stored, in table order.</value>
    public IReadOnlyList<LookupSegment4> Segments { get; init; } = [];

    /// <summary>Gets the per-segment value arrays keyed by the segment's offset from the start of the lookup table.</summary>
    /// <value>A dictionary mapping each distinct segment offset to the array of two-byte values at that offset.</value>
    /// <remarks>Each array holds one two-byte value per glyph in its segment; index <c>i</c> of the array is the value for <c>firstGlyph + i</c>. Multiple segments may share an offset and thus share the same array.</remarks>
    public IReadOnlyDictionary<ushort, IReadOnlyList<ushort>> ValueArrays { get; init; } =
        new Dictionary<ushort, IReadOnlyList<ushort>>();

    /// <inheritdoc/>
    public override ushort Format => 4;

    /// <inheritdoc/>
    public override int ValueSize => 2;

    /// <inheritdoc/>
    public override int Count => Segments.Count;

    /// <inheritdoc/>
    public override bool TryGetValue(int glyphIndex, out uint value)
    {
        if (!TryFindSegment(glyphIndex, out var segment) ||
            !ValueArrays.TryGetValue(segment.Offset, out var values))
        {
            value = 0;
            return false;
        }

        int index = glyphIndex - segment.FirstGlyph;
        if ((uint)index < (uint)values.Count)
        {
            value = values[index];
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>Finds the segment that contains <paramref name="glyphIndex"/>.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <param name="segment">When this method returns <see langword="true"/>, contains the containing segment.</param>
    /// <returns><see langword="true"/> when a segment contains the glyph index; otherwise, <see langword="false"/>.</returns>
    /// <remarks>The search is a binary search on <see cref="LookupSegment4.LastGlyph"/>, followed by a check of <see cref="LookupSegment4.FirstGlyph"/> because segments need not be adjacent.</remarks>
    public bool TryFindSegment(int glyphIndex, out LookupSegment4 segment)
    {
        int lo = 0;
        int hi = Segments.Count - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            var candidate = Segments[mid];
            int cmp = candidate.LastGlyph - glyphIndex;

            if (cmp == 0)
            {
                if (candidate.FirstGlyph <= glyphIndex)
                {
                    segment = candidate;
                    return true;
                }

                segment = default;
                return false;
            }

            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        if (lo < Segments.Count && Segments[lo].FirstGlyph <= glyphIndex)
        {
            segment = Segments[lo];
            return true;
        }

        segment = default;
        return false;
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the format word.</param>
    /// <param name="context">Ignored; the lookup declares how many segments it has.</param>
    /// <returns>The parsed lookup table with its value arrays resolved.</returns>
    /// <exception cref="InvalidDataException">The declared format is not 4.</exception>
    /// <exception cref="EndOfStreamException">The segment array or a value array extends past the end of the table-scoped source.</exception>
    /// <remarks>The offsets in <see cref="LookupSegment4.Offset"/> are relative to the start of the lookup table, not to the cursor's position, so the value arrays may lie either before or after the segment array. The relative arithmetic keeps this correct when the lookup is itself stored at a non-zero offset inside a parent table.</remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Segment Array Format 4 Lookup Table</seealso>
    static LookupTable4 IDerivedRecord<LookupTable, LookupTable4>.Parse(ref Cursor cursor, object? context)
    {
        BinSrchHeader binSrch = cursor.ReadEndianReversibleStruct<BinSrchHeader>();
        var segments = cursor.ReadEndianReversibleStructArray<LookupSegment4>(binSrch.UnitCount);

        var valueArrays = new Dictionary<ushort, IReadOnlyList<ushort>>();
        foreach (var segment in segments)
        {
            if (valueArrays.ContainsKey(segment.Offset)) continue;

            int glyphCount = segment.LastGlyph - segment.FirstGlyph + 1;
            valueArrays[segment.Offset] = glyphCount <= 0 ? [] : cursor.Source.ReadUInt16ArrayAt(segment.Offset, glyphCount);
        }

        return new LookupTable4
        {
            BinSrch = binSrch,
            Segments = segments,
            ValueArrays = valueArrays,
        };
    }
}

/// <summary>An AAT format 6 lookup table: a sorted list of glyph index and lookup value pairs.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The entry array is introduced by a redundant <see cref="BinSrchHeader"/> whose units are of type <see cref="LookupSingle"/> and are at least four bytes long.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Single Table Format 6 Lookup Table</see> for the definition of this format.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LookupTable"/>
/// <seealso cref="LookupSingle"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Single Table Format 6 Lookup Table</seealso>
public sealed record LookupTable6 : LookupTable, IDerivedRecord<LookupTable, LookupTable6>
{
    /// <summary>Gets the binary search header that introduces the entry array.</summary>
    /// <value>The on-disk <see cref="BinSrchHeader"/>, preserved for round-tripping and validation. The entries are searched directly.</value>
    public BinSrchHeader BinSrch { get; init; }

    /// <summary>Gets the entries, sorted by <see cref="LookupSingle.Glyph"/>.</summary>
    /// <value>The entry array as stored, in table order.</value>
    public IReadOnlyList<LookupSingle> Entries { get; init; } = [];

    /// <inheritdoc/>
    public override ushort Format => 6;

    /// <inheritdoc/>
    public override int ValueSize => 2;

    /// <inheritdoc/>
    public override int Count => Entries.Count;

    /// <inheritdoc/>
    public override bool TryGetValue(int glyphIndex, out uint value)
    {
        int lo = 0;
        int hi = Entries.Count - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            int cmp = Entries[mid].Glyph - glyphIndex;

            if (cmp == 0)
            {
                value = Entries[mid].Value;
                return true;
            }

            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        value = 0;
        return false;
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the format word.</param>
    /// <param name="context">Ignored; the lookup declares how many entries it has.</param>
    /// <returns>The parsed lookup table.</returns>
    /// <exception cref="InvalidDataException">The declared format is not 6.</exception>
    /// <exception cref="EndOfStreamException">The entry array extends past the end of the table-scoped source.</exception>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Single Table Format 6 Lookup Table</seealso>
    static LookupTable6 IDerivedRecord<LookupTable, LookupTable6>.Parse(ref Cursor cursor, object? context)
    {
        BinSrchHeader binSrch = cursor.ReadEndianReversibleStruct<BinSrchHeader>();
        return new() { BinSrch = binSrch, Entries = cursor.ReadEndianReversibleStructArray<LookupSingle>(binSrch.UnitCount) };
    }
}

/// <summary>An AAT format 8 lookup table: a trimmed array of two-byte values covering a contiguous glyph range.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A trimmed array begins at <see cref="FirstGlyph"/> rather than at glyph zero, so no space is spent on the glyphs before it.</description></item>
/// <item><description>Format 8 differs from format 10 in that its lookup values are fixed at two bytes; the extended trimmed array format allows other unit sizes.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Trimmed Array Format 8 Lookup Table</see> for the definition of this format.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LookupTable"/>
/// <seealso cref="LookupTable10"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Trimmed Array Format 8 Lookup Table</seealso>
public sealed record LookupTable8 : LookupTable, IDerivedRecord<LookupTable, LookupTable8>
{
    /// <summary>Gets the first glyph index covered by the trimmed array.</summary>
    /// <value>The lowest glyph index the array answers for; glyph indices below it are not covered.</value>
    public ushort FirstGlyph { get; init; }

    /// <summary>Gets the number of glyphs covered by the trimmed array.</summary>
    /// <value>The count of consecutive glyph indices starting at <see cref="FirstGlyph"/>; equal to <c>lastGlyph - firstGlyph + 1</c>.</value>
    public ushort GlyphCount { get; init; }

    /// <summary>Gets the lookup values; index <c>i</c> is the value for glyph <c>firstGlyph + i</c>.</summary>
    /// <value>An array of two-byte values, one per covered glyph.</value>
    public IReadOnlyList<ushort> Values { get; init; } = [];

    /// <inheritdoc/>
    public override ushort Format => 8;

    /// <inheritdoc/>
    public override int ValueSize => 2;

    /// <inheritdoc/>
    public override int Count => Values.Count;

    /// <inheritdoc/>
    public override bool TryGetValue(int glyphIndex, out uint value)
    {
        int index = glyphIndex - FirstGlyph;

        if ((uint)index < (uint)Values.Count)
        {
            value = Values[index];
            return true;
        }

        value = 0;
        return false;
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned immediately after the format word.</param>
    /// <param name="context">Ignored; the lookup declares its first glyph and glyph count.</param>
    /// <returns>The parsed lookup table.</returns>
    /// <exception cref="EndOfStreamException">The value array extends past the end of the table-scoped source.</exception>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Trimmed Array Format 8 Lookup Table</seealso>
    static LookupTable8 IDerivedRecord<LookupTable, LookupTable8>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();
        return new LookupTable8
        {
            FirstGlyph = header.FirstGlyph,
            GlyphCount = header.GlyphCount,
            Values = cursor.ReadUInt16Array(header.GlyphCount),
        };
    }

    /// <summary>The format 8 header: the first glyph and glyph count that bound the trimmed array. Blittable, size 4.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Trimmed Array Format 8 Lookup Table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The first glyph index covered by the trimmed array at byte offset 0.</summary>
        public ushort FirstGlyph;  // +0

        /// <summary>The number of glyphs covered by the trimmed array at byte offset 2.</summary>
        public ushort GlyphCount;  // +2

        /// <summary>Reverses the byte order of both fields in a <see cref="Header"/>.</summary>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        public static Header ReverseEndianness(Header v) => new()
        {
            FirstGlyph = BinaryPrimitives.ReverseEndianness(v.FirstGlyph),
            GlyphCount = BinaryPrimitives.ReverseEndianness(v.GlyphCount),
        };
    }
}

/// <summary>An AAT format 10 lookup table: an extended trimmed array whose value unit size the table declares.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The specification restricts the unit size to 1, 2, 4, or 8 bytes. Values wider than four bytes are read as a <c>ulong</c> and narrowed to 32 bits by <see cref="TryGetValue(int, out uint)"/>; a caller that needs the full wide value should read <see cref="Values"/> directly.</description></item>
/// <item><description>The table is otherwise identical to format 8, with the unit size declared and the glyph count carried explicitly.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Extended Trimmed Array Format 10 Lookup Table</see> for the definition of this format.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LookupTable"/>
/// <seealso cref="LookupTable8"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Extended Trimmed Array Format 10 Lookup Table</seealso>
public sealed record LookupTable10 : LookupTable, IDerivedRecord<LookupTable, LookupTable10>
{
    /// <summary>Gets the declared size of one lookup value, in bytes.</summary>
    /// <value>One of 1, 2, 4, or 8.</value>
    public ushort UnitSize { get; init; }

    /// <summary>Gets the first glyph index covered by the trimmed array.</summary>
    /// <value>The lowest glyph index the array answers for; glyph indices below it are not covered.</value>
    public ushort FirstGlyph { get; init; }

    /// <summary>Gets the number of glyphs covered by the trimmed array.</summary>
    /// <value>The count of consecutive glyph indices starting at <see cref="FirstGlyph"/>.</value>
    public ushort GlyphCount { get; init; }

    /// <summary>Gets the raw lookup values as 64-bit unsigned integers, one per glyph.</summary>
    /// <value>An array of values; index <c>i</c> is the value for glyph <c>firstGlyph + i</c>.</value>
    /// <remarks>Each element holds the value as it was decoded. When <see cref="UnitSize"/> is smaller than eight, the high bytes are zero.</remarks>
    public IReadOnlyList<ulong> Values { get; init; } = [];

    /// <inheritdoc/>
    public override ushort Format => 10;

    /// <inheritdoc/>
    public override int ValueSize => UnitSize;

    /// <inheritdoc/>
    public override int Count => Values.Count;

    /// <inheritdoc/>
    public override bool TryGetValue(int glyphIndex, out uint value)
    {
        int index = glyphIndex - FirstGlyph;

        if ((uint)index < (uint)Values.Count)
        {
            value = unchecked((uint)Values[index]);
            return true;
        }

        value = 0;
        return false;
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the format word.</param>
    /// <param name="context">Ignored; the lookup declares its unit size, first glyph, and glyph count.</param>
    /// <returns>The parsed lookup table.</returns>
    /// <exception cref="InvalidDataException">The declared format is not 10, or the unit size is not 1, 2, 4, or 8.</exception>
    /// <exception cref="EndOfStreamException">The value array extends past the end of the table-scoped source.</exception>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Extended Trimmed Array Format 10 Lookup Table</seealso>
    static LookupTable10 IDerivedRecord<LookupTable, LookupTable10>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();
        if (header.UnitSize is not (1 or 2 or 4 or 8))
            throw new InvalidDataException($"AAT format 10 lookup table unitSize is {header.UnitSize}, expected 1, 2, 4, or 8.");
        return new LookupTable10
        {
            UnitSize = header.UnitSize,
            FirstGlyph = header.FirstGlyph,
            GlyphCount = header.GlyphCount,
            Values = header.UnitSize switch
            {
                1 => Array.ConvertAll(cursor.ReadUInt8Array(header.GlyphCount), b => (ulong)b),
                2 => Array.ConvertAll(cursor.ReadUInt16Array(header.GlyphCount), s => (ulong)s),
                4 => Array.ConvertAll(cursor.ReadUInt32Array(header.GlyphCount), i => (ulong)i),
                _ => cursor.ReadUInt64Array(header.GlyphCount),
            },
        };
    }

    /// <summary>The format 10 header: the unit size, first glyph, and glyph count. Blittable, size 6.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Extended Trimmed Array Format 10 Lookup Table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The declared size of one lookup value, in bytes, at byte offset 0. One of 1, 2, 4, or 8.</summary>
        public ushort UnitSize;    // +0

        /// <summary>The first glyph index covered by the trimmed array at byte offset 2.</summary>
        public ushort FirstGlyph;  // +2

        /// <summary>The number of glyphs covered by the trimmed array at byte offset 4.</summary>
        public ushort GlyphCount;  // +4

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        public static Header ReverseEndianness(Header v) => new()
        {
            UnitSize = BinaryPrimitives.ReverseEndianness(v.UnitSize),
            FirstGlyph = BinaryPrimitives.ReverseEndianness(v.FirstGlyph),
            GlyphCount = BinaryPrimitives.ReverseEndianness(v.GlyphCount),
        };
    }
}

/// <summary>The <c>StateHeader</c> structure that introduces a classic Apple Advanced Typography state table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A state table is a finite state machine that maps glyph classes onto actions. It is used by the <c>just</c>, <c>kerx</c>, and <c>morx</c> tables, whose pages document the corresponding class and entry tables.</description></item>
/// <item><description>The three offsets are measured from the start of this header. Because they are <c>uint16</c>, a classic state table is limited to 64 KB.</description></item>
/// <item><description>The class subtable, state array, and entry table may appear in any order after the embedding table's own header. The class and entry tables must be word-aligned.</description></item>
/// <item><description>The extended state table format, introduced later, replaced this header with <see cref="STXHeader"/> in most AAT tables.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, State Tables</see> for the finite state machine model.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="STXHeader"/>
/// <seealso cref="ClassTableHeader"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: State Tables</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct StateHeader : IEndianReversibleStruct<StateHeader>
{
    /// <summary>The size of one state row, in bytes, at byte offset 0.</summary>
    /// <remarks>The value is limited to 8 bits even though the field is 16 bits wide for alignment. One state row holds <see cref="StateSize"/> class codes, because each entry in the state array is a single byte.</remarks>
    public ushort StateSize;   // +0

    /// <summary>The byte offset from the start of the state table to the class subtable at byte offset 2.</summary>
    /// <seealso cref="ClassTableHeader"/>
    public ushort ClassTable;  // +2

    /// <summary>The byte offset from the start of the state table to the state array at byte offset 4.</summary>
    public ushort StateArray;  // +4

    /// <summary>The byte offset from the start of the state table to the entry subtable at byte offset 6.</summary>
    public ushort EntryTable;  // +6

    /// <summary>Reverses the byte order of every field in a <see cref="StateHeader"/>.</summary>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new header with each field reversed.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: State Tables</seealso>
    public static StateHeader ReverseEndianness(StateHeader v) => new()
    {
        StateSize = BinaryPrimitives.ReverseEndianness(v.StateSize),
        ClassTable = BinaryPrimitives.ReverseEndianness(v.ClassTable),
        StateArray = BinaryPrimitives.ReverseEndianness(v.StateArray),
        EntryTable = BinaryPrimitives.ReverseEndianness(v.EntryTable),
    };
}

/// <summary>The <c>STXHeader</c> structure that introduces an extended Apple Advanced Typography state table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The extended form replaced the classic form as fonts grew past the 64 KB addressable limit of the classic header's 16-bit offsets, and as wide glyph-index ranges made the classic class array wasteful.</description></item>
/// <item><description>The class table in an extended state table is no longer a dense byte array; it is an Apple Advanced Typography <see cref="LookupTable"/> whose looked-up value is a 16-bit class code. A format 8 trimmed array produces the same result as the classic class array for fonts whose glyph indices are dense.</description></item>
/// <item><description>The entries in the state array are 16-bit zero-based indices into the entry table, replacing the classic form's byte offsets and enormously increasing the range of the machine.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Extended State Tables</see> for the extended format description.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="StateHeader"/>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Extended State Tables</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct STXHeader : IEndianReversibleStruct<STXHeader>
{
    /// <summary>The number of classes, which is the number of 16-bit entry indices in a single row of the state array, at byte offset 0.</summary>
    /// <remarks>Unlike <see cref="StateHeader.StateSize"/>, this is a full 32-bit count and is not limited to eight bits.</remarks>
    public uint ClassCount;         // +0

    /// <summary>The byte offset from the start of this header to the class table at byte offset 4.</summary>
    /// <remarks>The class table is a <see cref="LookupTable"/> whose values are 16-bit class codes.</remarks>
    public uint ClassTableOffset;   // +4

    /// <summary>The byte offset from the start of this header to the state array at byte offset 8.</summary>
    public uint StateArrayOffset;   // +8

    /// <summary>The byte offset from the start of this header to the entry table at byte offset 12.</summary>
    public uint EntryTableOffset;   // +12

    /// <summary>Reverses the byte order of every field in an <see cref="STXHeader"/>.</summary>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new header with each field reversed.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Extended State Tables</seealso>
    public static STXHeader ReverseEndianness(STXHeader v) => new()
    {
        ClassCount = BinaryPrimitives.ReverseEndianness(v.ClassCount),
        ClassTableOffset = BinaryPrimitives.ReverseEndianness(v.ClassTableOffset),
        StateArrayOffset = BinaryPrimitives.ReverseEndianness(v.StateArrayOffset),
        EntryTableOffset = BinaryPrimitives.ReverseEndianness(v.EntryTableOffset),
    };
}

/// <summary>The header of a classic Apple Advanced Typography class subtable: the first glyph and glyph count that bound the class code array.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The header is followed by <see cref="GlyphCount"/> one-byte class codes. The array is indexed by <c>glyphIndex - FirstGlyph</c>.</description></item>
/// <item><description>Glyph indices outside the covered range are assigned class <see cref="AatClass.OutOfBounds"/> automatically.</description></item>
/// <item><description>The classic form is superseded by the extended state table, which uses a <see cref="LookupTable"/> for the same purpose and can therefore cover sparse glyph-index sets without wasting space.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Class Subtable</see> for the classic class table description.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="StateHeader"/>
/// <seealso cref="AatClass"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Class Subtable</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct ClassTableHeader : IEndianReversibleStruct<ClassTableHeader>
{
    /// <summary>The glyph index of the first glyph covered by the class code array at byte offset 0.</summary>
    public ushort FirstGlyph;  // +0

    /// <summary>The number of glyphs covered by the class code array at byte offset 2.</summary>
    public ushort GlyphCount;  // +2

    /// <summary>Reverses the byte order of both fields in a <see cref="ClassTableHeader"/>.</summary>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new header with each field reversed.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Class Subtable</seealso>
    public static ClassTableHeader ReverseEndianness(ClassTableHeader v) => new()
    {
        FirstGlyph = BinaryPrimitives.ReverseEndianness(v.FirstGlyph),
        GlyphCount = BinaryPrimitives.ReverseEndianness(v.GlyphCount),
    };
}

/// <summary>The predefined glyph classes and states that Apple Advanced Typography state tables reserve.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Classes 0 through 3 are predefined by the format. Classes 4 and above are available to the font developer, up to the state table's declared class count.</description></item>
/// <item><description>Classes 0, 2, and 3 never appear in a class array or lookup; the state machine enters them as a matter of course when the glyph stream is exhausted, when a glyph has been deleted by an earlier action, or when a line boundary is reached.</description></item>
/// <item><description>Class 1 (out of bounds) may appear in a class array or lookup, but is also assigned automatically to any glyph index the class table does not cover.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Class Subtable</see> for the predefined class codes.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="StateHeader"/>
/// <seealso cref="STXHeader"/>
/// <seealso cref="ClassTableHeader"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Class Subtable</seealso>
public static class AatClass
{
    /// <summary>The end-of-text class, class 0.</summary>
    /// <value>The constant <c>0</c>.</value>
    /// <remarks>The state machine enters this class when the glyph array is exhausted. The class should not appear in a class array or lookup.</remarks>
    public const ushort EndOfText = 0;

    /// <summary>The out-of-bounds class, class 1.</summary>
    /// <value>The constant <c>1</c>.</value>
    /// <remarks>The state machine enters this class for a glyph index that the class table does not cover. The class may also appear explicitly in a class array or lookup.</remarks>
    public const ushort OutOfBounds = 1;

    /// <summary>The deleted-glyph class, class 2.</summary>
    /// <value>The constant <c>2</c>.</value>
    /// <remarks>The state machine enters this class for a glyph that an earlier action marked as deleted by setting its glyph index to <c>0xFFFF</c>. The class should not appear in a class array or lookup.</remarks>
    public const ushort DeletedGlyph = 2;

    /// <summary>The end-of-line class, class 3.</summary>
    /// <value>The constant <c>3</c>.</value>
    /// <remarks>The state machine enters this class at a line boundary. The class should not appear in a class array or lookup.</remarks>
    public const ushort EndOfLine = 3;

    /// <summary>The first class index available to a font for its own use, class 4.</summary>
    /// <value>The constant <c>4</c>.</value>
    /// <remarks>Classes 0 through 3 are predefined; a font may define its own classes starting at this index.</remarks>
    public const ushort FirstFree = 4;

    /// <summary>The state index at which a state machine starts when processing the beginning of the text stream, <c>0</c>.</summary>
    /// <value>The constant <c>0</c>.</value>
    /// <remarks>The manual names this state <c>start of text</c>. Which of the two initial states is used depends on context.</remarks>
    /// <seealso cref="StateStartOfLine"/>
    public const ushort StateStartOfText = 0;

    /// <summary>The state index at which a state machine starts for line-initial processing, <c>1</c>.</summary>
    /// <value>The constant <c>1</c>.</value>
    /// <remarks>The manual names this state <c>start of line</c>. It exists so that contextual rules can be applied differently at the start of a line than mid-line.</remarks>
    /// <seealso cref="StateStartOfText"/>
    public const ushort StateStartOfLine = 1;
}
