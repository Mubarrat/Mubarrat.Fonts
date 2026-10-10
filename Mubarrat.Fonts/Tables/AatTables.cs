using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// AAT shared components — binary search headers, lookup tables, state table headers
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>BinSrchHeader</c> structure: the redundant binary-search accelerator that introduces many Apple Advanced Typography lookup and array structures.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The structure exists so that a late-1980s lookup could take a small number of steps and avoid a divide. On a modern processor the three derived fields are pure overhead, and the manual says so outright: they "are no longer used". They are parsed and preserved rather than discarded because a validator may want to check that they agree with <see cref="UnitSize"/> and <see cref="UnitCount"/>, and because round-tripping a table requires them.</description></item>
/// <item><description>To guarantee termination, a searched array must end with one or more units whose first word is <see cref="EndOfSearch"/> (<c>0xFFFF</c>). The number of such units is table-specific.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Table Components</see> for the definition of this structure.</description></item>
/// </list>
/// </remarks>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct BinSrchHeader : IEndianReversibleStruct<BinSrchHeader>
{
    /// <summary>The end-of-search sentinel, <c>0xFFFF</c>, that terminates a searched array.</summary>
    /// <remarks>Append one or more units beginning with this value to the array being searched so that the search is guaranteed to terminate.</remarks>
    public const ushort EndOfSearch = 0xFFFF;

    /// <summary>The size of one lookup unit, in bytes, at byte offset 0.</summary>
    /// <remarks>The manual requires at least 6 for segment-based arrays and at least 4 for single-entry arrays.</remarks>
    public ushort UnitSize;      // +0

    /// <summary>The number of units to be searched at byte offset 2.</summary>
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
    public static BinSrchHeader ReverseEndianness(BinSrchHeader v) => new()
    {
        UnitSize = BinaryPrimitives.ReverseEndianness(v.UnitSize),
        UnitCount = BinaryPrimitives.ReverseEndianness(v.UnitCount),
        SearchRange = BinaryPrimitives.ReverseEndianness(v.SearchRange),
        EntrySelector = BinaryPrimitives.ReverseEndianness(v.EntrySelector),
        RangeShift = BinaryPrimitives.ReverseEndianness(v.RangeShift),
    };
}

/// <summary>One <c>LookupSegment</c> of a format 2 lookup table: a contiguous glyph range that shares a single two-byte lookup value.</summary>
/// <remarks>Segments must be sorted by <see cref="LastGlyph"/> so that a binary search over the last glyph of each segment finds the containing segment.</remarks>
/// <seealso cref="LookupTable2"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
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
    public static LookupSegment ReverseEndianness(LookupSegment v) => new()
    {
        LastGlyph = BinaryPrimitives.ReverseEndianness(v.LastGlyph),
        FirstGlyph = BinaryPrimitives.ReverseEndianness(v.FirstGlyph),
        Value = BinaryPrimitives.ReverseEndianness(v.Value),
    };
}

/// <summary>One <c>LookupSegment</c> of a format 4 lookup table: a contiguous glyph range whose per-glyph values live at an offset from the start of the lookup table.</summary>
/// <remarks>Segments must be sorted by <see cref="LastGlyph"/>. The value array addressed by <see cref="Offset"/> holds one two-byte value per glyph in the segment, indexed by <c>glyphIndex - firstGlyph</c>.</remarks>
/// <seealso cref="LookupTable4"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct LookupSegment4 : IEndianReversibleStruct<LookupSegment4>
{
    /// <summary>The last glyph index in the segment at byte offset 0.</summary>
    public ushort LastGlyph;  // +0

    /// <summary>The first glyph index in the segment at byte offset 2.</summary>
    public ushort FirstGlyph; // +2

    /// <summary>The offset from the start of the lookup table to the segment's value array at byte offset 4.</summary>
    public ushort Offset;     // +4

    /// <summary>Reverses the byte order of every field in a <see cref="LookupSegment4"/>.</summary>
    public static LookupSegment4 ReverseEndianness(LookupSegment4 v) => new()
    {
        LastGlyph = BinaryPrimitives.ReverseEndianness(v.LastGlyph),
        FirstGlyph = BinaryPrimitives.ReverseEndianness(v.FirstGlyph),
        Offset = BinaryPrimitives.ReverseEndianness(v.Offset),
    };
}

/// <summary>One <c>LookupSingle</c> entry of a format 6 lookup table: a glyph index and its two-byte lookup value.</summary>
/// <remarks>Entries must be sorted by <see cref="Glyph"/>.</remarks>
/// <seealso cref="LookupTable6"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct LookupEntry : IEndianReversibleStruct<LookupEntry>
{
    /// <summary>The glyph index at byte offset 0.</summary>
    public ushort Glyph; // +0

    /// <summary>The lookup value at byte offset 2.</summary>
    public ushort Value; // +2

    /// <summary>Reverses the byte order of every field in a <see cref="LookupEntry"/>.</summary>
    public static LookupEntry ReverseEndianness(LookupEntry v) => new()
    {
        Glyph = BinaryPrimitives.ReverseEndianness(v.Glyph),
        Value = BinaryPrimitives.ReverseEndianness(v.Value),
    };
}

/// <summary>A glyph-to-value lookup table, one of the five formats Apple Advanced Typography tables use to map glyph indices onto per-glyph information.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table begins with a <c>uint16</c> format and a format-specific header followed by the lookup data. Format 0 is a simple array indexed by glyph index; format 2 maps contiguous glyph ranges onto a shared value; format 4 maps contiguous ranges onto per-glyph values; format 6 is a sorted list of glyph/value pairs; format 8 is a trimmed array of two-byte values; format 10 is a trimmed array whose value unit size is declared.</description></item>
/// <item><description>What a lookup value means depends on the table that owns the lookup. This type therefore only locates values; interpreting them is the owning table's job. For example, in the <c>prop</c> table the value is an index into a properties array, while in <c>morx</c> it is usually a state-table index.</description></item>
/// <item><description>Formats 2 and 4 embed the redundant <see cref="BinSrchHeader"/>, which the manual documents as no longer used. The segments are parsed in full and searched directly instead.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Table Components</see> for the lookup table formats.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LookupTable0"/>
/// <seealso cref="LookupTable2"/>
/// <seealso cref="LookupTable4"/>
/// <seealso cref="LookupTable6"/>
/// <seealso cref="LookupTable8"/>
/// <seealso cref="LookupTable10"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
public abstract record LookupTable
{
    /// <summary>The format 0 lookup table format number.</summary>
    public const ushort FormatNumber0 = 0;

    /// <summary>The format 2 lookup table format number.</summary>
    public const ushort FormatNumber2 = 2;

    /// <summary>The format 4 lookup table format number.</summary>
    public const ushort FormatNumber4 = 4;

    /// <summary>The format 6 lookup table format number.</summary>
    public const ushort FormatNumber6 = 6;

    /// <summary>The format 8 lookup table format number.</summary>
    public const ushort FormatNumber8 = 8;

    /// <summary>The format 10 lookup table format number.</summary>
    public const ushort FormatNumber10 = 10;

    /// <summary>Gets the lookup table format number.</summary>
    /// <remarks>One of the <c>FormatNumber</c> constants on <see cref="LookupTable"/>.</remarks>
    public abstract ushort Format { get; }

    /// <summary>Gets the size, in bytes, of one lookup value in this table.</summary>
    /// <remarks>Format 0 and format 8 values are two bytes; format 2 and format 4 values are two bytes; format 6 values are two bytes; format 10 declares its unit size, which the specification restricts to 1, 2, 4, or 8.</remarks>
    public abstract int ValueSize { get; }

    /// <summary>Gets the number of glyphs covered by the lookup.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Formats 0, 8, and 10 cover a known, dense glyph range, so the count is exact.</description></item>
    /// <item><description>Formats 2, 4, and 6 cover a sparse set of glyphs; the count is the number of segments or entries, not the number of glyphs the table can answer for.</description></item>
    /// </list>
    /// </remarks>
    public abstract int Count { get; }

    /// <summary>Tries to find the lookup value for a glyph index.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <param name="value">When this method returns <see langword="true"/>, contains the value; otherwise, zero.</param>
    /// <returns><see langword="true"/> when the table covers <paramref name="glyphIndex"/>; otherwise, <see langword="false"/>.</returns>
    /// <remarks>The value is returned as a 32-bit unsigned integer regardless of the table's value width, so that formats 8 and 10 need no separate accessor. Callers that know their table's value width cast the result.</remarks>
    public abstract bool TryGetValue(int glyphIndex, out uint value);

    /// <summary>Finds the lookup value for a glyph index, or returns zero when the table does not cover it.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <returns>The lookup value, or <c>0</c> when the table does not cover <paramref name="glyphIndex"/>.</returns>
    /// <remarks>Zero is a valid lookup value in several tables, so callers that must distinguish "absent" from "zero" should use <see cref="TryGetValue(int, out uint)"/>.</remarks>
    /// <seealso cref="TryGetValue(int, out uint)"/>
    public uint GetValue(int glyphIndex) => TryGetValue(glyphIndex, out uint value) ? value : 0u;

    /// <summary>Reads a lookup table of whichever format the leading <c>uint16</c> declares.</summary>
    /// <param name="cursor">The cursor positioned at the lookup table's format word.</param>
    /// <param name="context">External values the lookup needs, or <see langword="null"/>. Lookup tables are self-contained and ignore it.</param>
    /// <returns>The parsed lookup table, typed as the format-specific subclass for the declared format.</returns>
    /// <exception cref="InvalidDataException">The format is not one of 0, 2, 4, 6, 8, or 10, or a format 10 unit size is not 1, 2, 4, or 8.</exception>
    /// <exception cref="EndOfStreamException">The lookup extends past the end of the table-scoped source.</exception>
    /// <remarks>The cursor is left at the first byte after the lookup table's data, so a caller can read a sequence of lookups back to back.</remarks>
    /// <seealso cref="LookupTable.Parse(ref Cursor, object?)"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
    public static LookupTable Parse(ref Cursor cursor, object? context = null)
    {
        ushort format = cursor.PeekUInt16();

        return format switch
        {
            FormatNumber0 => LookupTable0.Parse(ref cursor, context),
            FormatNumber2 => LookupTable2.Parse(ref cursor, context),
            FormatNumber4 => LookupTable4.Parse(ref cursor, context),
            FormatNumber6 => LookupTable6.Parse(ref cursor, context),
            FormatNumber8 => LookupTable8.Parse(ref cursor, context),
            FormatNumber10 => LookupTable10.Parse(ref cursor, context),
            _ => throw new InvalidDataException(
                $"AAT lookup table format is {format}, expected 0, 2, 4, 6, 8, or 10."),
        };
    }
}

/// <summary>An AAT format 0 lookup table: a simple array of two-byte lookup values indexed directly by glyph index.</summary>
/// <remarks>The array is not trimmed and has no declared length, so the lookup covers glyph indices <c>0</c> through <c>count - 1</c> and the array must be sized to the font's glyph count.</remarks>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
public sealed record LookupTable0 : LookupTable, IRecord<LookupTable0>
{
    /// <summary>Gets the lookup values, indexed by glyph index.</summary>
    public IReadOnlyList<ushort> Values { get; init; } = [];

    /// <inheritdoc/>
    public override ushort Format => FormatNumber0;

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

    /// <summary>Reads a format 0 lookup table from <paramref name="cursor"/>.</summary>
    /// <param name="cursor">The cursor positioned at the format word.</param>
    /// <param name="context">A <see cref="int"/> giving the number of glyphs to read, or <see langword="null"/> to read the array to the end of the source.</param>
    /// <returns>The parsed lookup table.</returns>
    /// <exception cref="InvalidDataException">The declared format is not 0.</exception>
    /// <exception cref="EndOfStreamException">The array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// A format 0 array carries no length of its own. The number of entries is therefore taken from <paramref name="context"/> when a glyph count is supplied — which is what the owning <c>morx</c>, <c>kerx</c>, and <c>prop</c> tables do, since each knows <c>maxp.numGlyphs</c> — and otherwise from the remainder of the source.
    /// </remarks>
    public static new LookupTable0 Parse(ref Cursor cursor, object? context = null)
    {
        ushort format = cursor.ReadUInt16();
        if (format != FormatNumber0)
            throw new InvalidDataException($"AAT lookup table format is {format}, expected 0.");

        int count = context is int glyphCount
            ? glyphCount
            : checked((int)(cursor.Remaining / 2));

        ArgumentOutOfRangeException.ThrowIfNegative(count);

        return new LookupTable0 { Values = cursor.ReadUInt16Array(count) };
    }
}

/// <summary>An AAT format 2 lookup table: contiguous glyph ranges, each sharing one two-byte lookup value.</summary>
/// <remarks>The segment array is introduced by a redundant <see cref="BinSrchHeader"/> whose units are at least six bytes long. Segments are sorted by <see cref="LookupSegment.LastGlyph"/>.</remarks>
/// <seealso cref="LookupTable"/>
/// <seealso cref="LookupSegment"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
public sealed record LookupTable2 : LookupTable, IRecord<LookupTable2>
{
    /// <summary>Gets the binary search header that introduces the segment array.</summary>
    /// <remarks>Preserved for round-tripping; the segments are searched directly.</remarks>
    public BinSrchHeader BinSrch { get; init; }

    /// <summary>Gets the segments, sorted by <see cref="LookupSegment.LastGlyph"/>.</summary>
    public IReadOnlyList<LookupSegment> Segments { get; init; } = [];

    /// <inheritdoc/>
    public override ushort Format => FormatNumber2;

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
    /// The search is a binary search on <see cref="LookupSegment.LastGlyph"/>. A segment also declares <see cref="LookupSegment.FirstGlyph"/>, which is checked after the search narrows the candidate to one, because the segments need not be adjacent.
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
                return Validate(candidate, glyphIndex, out segment);

            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }

        // The binary search lands on the first segment whose last glyph is at least glyphIndex.
        if (lo < Segments.Count)
            return Validate(Segments[lo], glyphIndex, out segment);

        segment = default;
        return false;
    }

    private static bool Validate(LookupSegment candidate, int glyphIndex, out LookupSegment segment)
    {
        if (candidate.FirstGlyph <= glyphIndex)
        {
            segment = candidate;
            return true;
        }

        segment = default;
        return false;
    }

    /// <summary>Reads a format 2 lookup table from <paramref name="cursor"/>.</summary>
    /// <param name="cursor">The cursor positioned at the format word.</param>
    /// <param name="context">Ignored; the lookup declares how many segments it has.</param>
    /// <returns>The parsed lookup table.</returns>
    /// <exception cref="InvalidDataException">The declared format is not 2.</exception>
    /// <exception cref="EndOfStreamException">The segment array extends past the end of the table-scoped source.</exception>
    public static new LookupTable2 Parse(ref Cursor cursor, object? context = null)
    {
        ushort format = cursor.ReadUInt16();
        if (format != FormatNumber2)
            throw new InvalidDataException($"AAT lookup table format is {format}, expected 2.");

        BinSrchHeader binSrch = cursor.ReadBigEndianStruct<BinSrchHeader>();
        var segments = cursor.ReadBigEndianStructArray<LookupSegment>(binSrch.UnitCount);

        return new LookupTable2 { BinSrch = binSrch, Segments = segments };
    }
}

/// <summary>An AAT format 4 lookup table: contiguous glyph ranges whose per-glyph values live in separate arrays addressed by offset.</summary>
/// <remarks>The segment array is introduced by a redundant <see cref="BinSrchHeader"/>. Segments are sorted by <see cref="LookupSegment4.LastGlyph"/>.</remarks>
/// <seealso cref="LookupTable"/>
/// <seealso cref="LookupSegment4"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
public sealed record LookupTable4 : LookupTable, IRecord<LookupTable4>
{
    /// <summary>Gets the binary search header that introduces the segment array.</summary>
    /// <remarks>Preserved for round-tripping; the segments are searched directly.</remarks>
    public BinSrchHeader BinSrch { get; init; }

    /// <summary>Gets the segments, sorted by <see cref="LookupSegment4.LastGlyph"/>.</summary>
    public IReadOnlyList<LookupSegment4> Segments { get; init; } = [];

    /// <summary>Gets the per-segment value arrays keyed by the segment's offset from the start of the lookup table.</summary>
    /// <remarks>Each array holds one two-byte value per glyph in its segment; index <c>i</c> of the array is the value for <c>firstGlyph + i</c>.</remarks>
    public IReadOnlyDictionary<ushort, IReadOnlyList<ushort>> ValueArrays { get; init; } =
        new Dictionary<ushort, IReadOnlyList<ushort>>();

    /// <inheritdoc/>
    public override ushort Format => FormatNumber4;

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
    /// <seealso cref="LookupTable2.TryFindSegment(int, out LookupSegment)"/>
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

                break;
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

    /// <summary>Reads a format 4 lookup table from <paramref name="cursor"/>.</summary>
    /// <param name="cursor">The cursor positioned at the format word.</param>
    /// <param name="context">Ignored; the lookup declares how many segments it has.</param>
    /// <returns>The parsed lookup table.</returns>
    /// <exception cref="InvalidDataException">The declared format is not 4.</exception>
    /// <exception cref="EndOfStreamException">The segment array or a value array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// The offsets in <see cref="LookupSegment4.Offset"/> are relative to the start of the lookup table, not to the cursor's position, so the value arrays can lie either before or after the segment array. The relative arithmetic is what keeps this correct when the lookup is itself stored at a non-zero offset inside a parent table.
    /// </remarks>
    public static new LookupTable4 Parse(ref Cursor cursor, object? context = null)
    {
        long lookupStart = cursor.Position;

        ushort format = cursor.ReadUInt16();
        if (format != FormatNumber4)
            throw new InvalidDataException($"AAT lookup table format is {format}, expected 4.");

        BinSrchHeader binSrch = cursor.ReadBigEndianStruct<BinSrchHeader>();
        var segments = cursor.ReadBigEndianStructArray<LookupSegment4>(binSrch.UnitCount);

        var valueArrays = new Dictionary<ushort, IReadOnlyList<ushort>>();
        foreach (var segment in segments)
        {
            if (valueArrays.ContainsKey(segment.Offset)) continue;

            int glyphCount = segment.LastGlyph - segment.FirstGlyph + 1;
            if (glyphCount <= 0)
            {
                valueArrays[segment.Offset] = [];
                continue;
            }

            valueArrays[segment.Offset] =
                cursor.Source.ReadUInt16ArrayAt(lookupStart + segment.Offset, glyphCount);
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
/// <remarks>The entry array is introduced by a redundant <see cref="BinSrchHeader"/> whose units are at least four bytes long.</remarks>
/// <seealso cref="LookupTable"/>
/// <seealso cref="LookupEntry"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
public sealed record LookupTable6 : LookupTable, IRecord<LookupTable6>
{
    /// <summary>Gets the binary search header that introduces the entry array.</summary>
    /// <remarks>Preserved for round-tripping; the entries are searched directly.</remarks>
    public BinSrchHeader BinSrch { get; init; }

    /// <summary>Gets the entries, sorted by <see cref="LookupEntry.Glyph"/>.</summary>
    public IReadOnlyList<LookupEntry> Entries { get; init; } = [];

    /// <inheritdoc/>
    public override ushort Format => FormatNumber6;

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

    /// <summary>Reads a format 6 lookup table from <paramref name="cursor"/>.</summary>
    /// <param name="cursor">The cursor positioned at the format word.</param>
    /// <param name="context">Ignored; the lookup declares how many entries it has.</param>
    /// <returns>The parsed lookup table.</returns>
    /// <exception cref="InvalidDataException">The declared format is not 6.</exception>
    /// <exception cref="EndOfStreamException">The entry array extends past the end of the table-scoped source.</exception>
    public static new LookupTable6 Parse(ref Cursor cursor, object? context = null)
    {
        ushort format = cursor.ReadUInt16();
        if (format != FormatNumber6)
            throw new InvalidDataException($"AAT lookup table format is {format}, expected 6.");

        BinSrchHeader binSrch = cursor.ReadBigEndianStruct<BinSrchHeader>();
        var entries = cursor.ReadBigEndianStructArray<LookupEntry>(binSrch.UnitCount);

        return new LookupTable6 { BinSrch = binSrch, Entries = entries };
    }
}

/// <summary>An AAT format 8 lookup table: a trimmed array of two-byte values covering a contiguous glyph range.</summary>
/// <remarks>A trimmed array begins at <see cref="FirstGlyph"/> rather than at glyph zero, so no space is spent on the glyphs before it.</remarks>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
public sealed record LookupTable8 : LookupTable, IRecord<LookupTable8>
{
    /// <summary>Gets the first glyph index covered by the trimmed array.</summary>
    public ushort FirstGlyph { get; init; }

    /// <summary>Gets the lookup values; index <c>i</c> is the value for glyph <c>firstGlyph + i</c>.</summary>
    public IReadOnlyList<ushort> Values { get; init; } = [];

    /// <inheritdoc/>
    public override ushort Format => FormatNumber8;

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

    /// <summary>Reads a format 8 lookup table from <paramref name="cursor"/>.</summary>
    /// <param name="cursor">The cursor positioned at the format word.</param>
    /// <param name="context">Ignored; the lookup declares its first glyph and glyph count.</param>
    /// <returns>The parsed lookup table.</returns>
    /// <exception cref="InvalidDataException">The declared format is not 8.</exception>
    /// <exception cref="EndOfStreamException">The value array extends past the end of the table-scoped source.</exception>
    public static new LookupTable8 Parse(ref Cursor cursor, object? context = null)
    {
        ushort format = cursor.ReadUInt16();
        if (format != FormatNumber8)
            throw new InvalidDataException($"AAT lookup table format is {format}, expected 8.");

        ushort firstGlyph = cursor.ReadUInt16();
        ushort glyphCount = cursor.ReadUInt16();

        return new LookupTable8
        {
            FirstGlyph = firstGlyph,
            Values = cursor.ReadUInt16Array(glyphCount),
        };
    }
}

/// <summary>An AAT format 10 lookup table: a trimmed array of values whose unit size the table declares.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The specification restricts the unit size to 1, 2, 4, or 8 bytes. Values wider than four bytes are read as a <c>ulong</c> and narrowed to 32 bits by <see cref="TryGetValue(int, out uint)"/>; a caller that needs the full 64-bit value should read the array itself.</description></item>
/// <item><description>The table is otherwise identical to format 8, with the first glyph and glyph count declared rather than implied by the array length.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
public sealed record LookupTable10 : LookupTable, IRecord<LookupTable10>
{
    /// <summary>Gets the declared size of one lookup value, in bytes. One of 1, 2, 4, or 8.</summary>
    public ushort UnitSize { get; init; }

    /// <summary>Gets the first glyph index covered by the trimmed array.</summary>
    public ushort FirstGlyph { get; init; }

    /// <summary>Gets the raw lookup values as 64-bit unsigned integers, one per glyph.</summary>
    /// <remarks>Index <c>i</c> is the value for glyph <c>firstGlyph + i</c>. Each element holds the value as it was decoded; when <see cref="UnitSize"/> is smaller than eight, the high bytes are zero.</remarks>
    public IReadOnlyList<ulong> Values { get; init; } = [];

    /// <inheritdoc/>
    public override ushort Format => FormatNumber10;

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

    /// <summary>Reads a format 10 lookup table from <paramref name="cursor"/>.</summary>
    /// <param name="cursor">The cursor positioned at the format word.</param>
    /// <param name="context">Ignored; the lookup declares its unit size, first glyph, and glyph count.</param>
    /// <returns>The parsed lookup table.</returns>
    /// <exception cref="InvalidDataException">The declared format is not 10, or the unit size is not 1, 2, 4, or 8.</exception>
    /// <exception cref="EndOfStreamException">The value array extends past the end of the table-scoped source.</exception>
    public static new LookupTable10 Parse(ref Cursor cursor, object? context = null)
    {
        ushort format = cursor.ReadUInt16();
        if (format != FormatNumber10)
            throw new InvalidDataException($"AAT lookup table format is {format}, expected 10.");

        ushort unitSize = cursor.ReadUInt16();
        if (unitSize is not (1 or 2 or 4 or 8))
        {
            throw new InvalidDataException(
                $"AAT format 10 lookup table unitSize is {unitSize}, expected 1, 2, 4, or 8.");
        }

        ushort firstGlyph = cursor.ReadUInt16();
        ushort glyphCount = cursor.ReadUInt16();

        var values = new ulong[glyphCount];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = unitSize switch
            {
                1 => cursor.ReadUInt8(),
                2 => cursor.ReadUInt16(),
                4 => cursor.ReadUInt32(),
                _ => cursor.ReadUInt64(),
            };
        }

        return new LookupTable10
        {
            UnitSize = unitSize,
            FirstGlyph = firstGlyph,
            Values = values,
        };
    }
}

/// <summary>The <c>STHeader</c> structure that introduces an Apple Advanced Typography state table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A state table is a finite state machine that maps glyph classes onto actions. It is used by the <c>just</c>, <c>kerx</c>, and <c>morx</c> tables, whose pages document the corresponding class and action tables.</description></item>
/// <item><description><see cref="StateArrayOffset"/> is a 32-bit quantity only in the extended form used by newer tables; this structure is the common short form. Tables that store a 32-bit state array offset declare their own header.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, State Tables</see> for the finite state machine model and the predefined classes and states.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="StateHeader"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: State Tables</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct StateHeader : IEndianReversibleStruct<StateHeader>
{
    /// <summary>The state table version at byte offset 0. Always 0 or 1.</summary>
    public ushort Version;           // +0

    /// <summary>The number of classes into which the class table groups glyphs at byte offset 2.</summary>
    /// <remarks>Every state table reserves classes <see cref="AatClass.EndOfText"/>, <see cref="AatClass.OutOfBounds"/>, and <see cref="AatClass.DeletedGlyph"/>, so a table with <c>n</c> classes of its own declares <c>n + 3</c>.</remarks>
    public ushort GlyphClassCount;   // +2

    /// <summary>The offset of the class table from the start of the state table at byte offset 4.</summary>
    public ushort ClassTableOffset;  // +4

    /// <summary>The offset of the state array from the start of the state table at byte offset 6.</summary>
    public ushort StateArrayOffset;  // +6

    /// <summary>The offset of the entry table from the start of the state table at byte offset 8.</summary>
    public ushort EntryTableOffset;  // +8

    /// <summary>Reverses the byte order of every field in a <see cref="StateHeader"/>.</summary>
    public static StateHeader ReverseEndianness(StateHeader v) => new()
    {
        Version = BinaryPrimitives.ReverseEndianness(v.Version),
        GlyphClassCount = BinaryPrimitives.ReverseEndianness(v.GlyphClassCount),
        ClassTableOffset = BinaryPrimitives.ReverseEndianness(v.ClassTableOffset),
        StateArrayOffset = BinaryPrimitives.ReverseEndianness(v.StateArrayOffset),
        EntryTableOffset = BinaryPrimitives.ReverseEndianness(v.EntryTableOffset),
    };
}

/// <summary>The predefined glyph classes and states that Apple Advanced Typography state tables reserve.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Every state table reserves at least three classes: <see cref="EndOfText"/>, <see cref="OutOfBounds"/>, and <see cref="DeletedGlyph"/>. A table may define further classes starting at index 3.</description></item>
/// <item><description><see cref="DeletedGlyph"/> is the class of a glyph that an earlier action removed from the glyph array. Actions that mark glyphs as deleted must route them through this class so that later passes skip them.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Class Subtable</see> for the predefined class values.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="StateHeader"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
public static class AatClass
{
    /// <summary>The end-of-text class, class 0.</summary>
    /// <remarks>The state machine enters this class when the glyph array is exhausted.</remarks>
    public const ushort EndOfText = 0;

    /// <summary>The out-of-bounds class, class 1.</summary>
    /// <remarks>The state machine enters this class for a glyph index that the class table does not cover.</remarks>
    public const ushort OutOfBounds = 1;

    /// <summary>The deleted-glyph class, class 2.</summary>
    /// <remarks>The state machine enters this class for a glyph that an earlier action marked as deleted.</remarks>
    public const ushort DeletedGlyph = 2;

    /// <summary>The first class index available to a table for its own use, class 3.</summary>
    public const ushort FirstFree = 3;

    /// <summary>The state index at which a state machine starts when processing a text run, <c>0</c>.</summary>
    /// <remarks>The initial state depends on the table's direction: state 0 is <c>startOfText</c>, used for left-to-right processing.</remarks>
    public const ushort StateStartOfText = 0;

    /// <summary>The state index at which a state machine starts for right-to-left processing, <c>1</c>.</summary>
    /// <remarks>The manual names this state <c>startOfLine</c>. It exists so that contextual rules can be applied in visual order for a bidirectional script.</remarks>
    public const ushort StateStartOfLine = 1;
}
