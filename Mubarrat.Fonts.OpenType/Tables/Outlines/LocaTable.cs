using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;
using System.Runtime.Intrinsics;

namespace Mubarrat.Fonts.OpenType.Tables.Outlines;

/// <summary>The <c>loca</c> table: index to location. Contains one offset per glyph (plus a final sentinel) pointing into the <c>glyf</c> table. Glyph <c>i</c> occupies bytes <c>[Offsets[i], Offsets[i+1])</c> of <c>glyf</c>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The on-disk format depends on <c>head.indexToLocFormat</c>: short offsets store a <c>uint16</c> value equal to the actual byte offset divided by 2, while long offsets store the byte offset directly as a <c>uint32</c>. Both are normalized to actual byte offsets during parsing, so consumers never see the division-by-2 convention.</description></item>
/// <item><description>The array has <c>maxp.numGlyphs + 1</c> entries. It is fully materialized as a <see cref="uint"/> array so no <see cref="Source"/> reference is retained.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/loca">loca table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyfTable"/>
/// <seealso cref="HeadTable"/>
/// <seealso cref="MaxpTable"/>
/// <seealso cref="IndexToLocFormat"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/loca">OpenType specification: loca table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/glyf">OpenType specification: glyf table</seealso>
public sealed record LocaTable : IOpenTypeTable<LocaTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IOpenTypeTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/loca">OpenType specification: loca table</seealso>
    public static Tag Tag => "loca";

    /// <summary>Gets the offset of each glyph in the <c>glyf</c> table. Length is <see cref="NumGlyphs"/> + 1; the final entry is the end offset of the last glyph.</summary>
    /// <value>An array of <c>numGlyphs + 1</c> byte offsets into the <c>glyf</c> table, in glyph ID order. The sentinel at the end gives the end offset of the last glyph.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The array is exposed directly for fast bulk iteration; returning a copy per access would defeat the purpose.</description></item>
    /// <item><description>Callers must not modify the array. The instance does not defensively copy, so mutation would silently corrupt subsequent glyph lookups.</description></item>
    /// <item><description>All offsets are actual byte offsets; the short-format division-by-2 convention has already been applied by the parser.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetOffset(int)"/>
    /// <seealso cref="GetGlyphLength(int)"/>
    /// <seealso cref="NumGlyphs"/>
    /// <seealso cref="Format"/>
    public required uint[] Offsets { get; init; }

    /// <summary>Gets the on-disk format as declared by <c>head.indexToLocFormat</c>.</summary>
    /// <value><see cref="IndexToLocFormat.ShortOffsets"/> or <see cref="IndexToLocFormat.LongOffsets"/>, captured from <c>head</c> at parse time.</value>
    /// <remarks>The value is informational; <see cref="Offsets"/> is already normalized to byte offsets regardless of the on-disk form.</remarks>
    /// <seealso cref="HeadTable.IndexToLocFormat"/>
    /// <seealso cref="IndexToLocFormat"/>
    public required IndexToLocFormat Format { get; init; }

    /// <summary>Gets the number of glyphs.</summary>
    /// <value>Equal to the length of <see cref="Offsets"/> minus one, matching <c>maxp.numGlyphs</c>.</value>
    /// <remarks>The minus-one accounts for the sentinel entry at the end of the offset array.</remarks>
    /// <seealso cref="Offsets"/>
    /// <seealso cref="GetOffset(int)"/>
    /// <seealso cref="GetGlyphLength(int)"/>
    public int NumGlyphs => Offsets.Length - 1;

    /// <summary>Gets the starting byte offset of glyph <paramref name="glyphId"/> within the <c>glyf</c> table.</summary>
    /// <param name="glyphId">The glyph ID; must be in <c>[0, <see cref="NumGlyphs"/>)</c>.</param>
    /// <returns>The byte offset of the glyph's first byte, relative to the start of the <c>glyf</c> table.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is out of range.</exception>
    /// <seealso cref="GetGlyphLength(int)"/>
    /// <seealso cref="GetGlyphRange(int)"/>
    /// <seealso cref="Offsets"/>
    public uint GetOffset(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, NumGlyphs);
        return Offsets[glyphId];
    }

    /// <summary>Gets the number of bytes occupied by glyph <paramref name="glyphId"/> in the <c>glyf</c> table. A length of 0 means the glyph has no outline data (e.g. a space character).</summary>
    /// <param name="glyphId">The glyph ID; must be in <c>[0, <see cref="NumGlyphs"/>)</c>.</param>
    /// <returns>The byte length of the glyph's on-disk record; <c>0</c> when the glyph has no outline data.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is out of range.</exception>
    /// <remarks>Computed as <c>Offsets[glyphId + 1] - Offsets[glyphId]</c>. The parser validates that no entry exceeds the <c>glyf</c> table's declared length.</remarks>
    /// <seealso cref="GetOffset(int)"/>
    /// <seealso cref="GetGlyphRange(int)"/>
    /// <seealso cref="GlyfTable"/>
    public uint GetGlyphLength(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, NumGlyphs);
        return Offsets[glyphId + 1] - Offsets[glyphId];
    }

    /// <summary>Gets both the offset and length of glyph <paramref name="glyphId"/> in a single call.</summary>
    /// <param name="glyphId">The glyph ID; must be in <c>[0, <see cref="NumGlyphs"/>)</c>.</param>
    /// <returns>A tuple of the glyph's byte offset and length within the <c>glyf</c> table.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is out of range.</exception>
    /// <remarks>Equivalent to calling <see cref="GetOffset(int)"/> and <see cref="GetGlyphLength(int)"/>, but performs the bounds check once and reads both array entries from a single cache line.</remarks>
    /// <seealso cref="GetOffset(int)"/>
    /// <seealso cref="GetGlyphLength(int)"/>
    public (uint Offset, uint Length) GetGlyphRange(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, NumGlyphs);
        uint start = Offsets[glyphId];
        uint end = Offsets[glyphId + 1];
        return (start, end - start);
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the loca table.</param>
    /// <param name="context">A <see cref="FontFace"/> whose <c>head</c> and <c>maxp</c> tables supply the format and glyph count.</param>
    /// <returns>The parsed loca table with its offsets normalized to byte offsets.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="FontFace"/>.</exception>
    /// <exception cref="InvalidDataException"><c>head.indexToLocFormat</c> is neither 0 nor 1.</exception>
    /// <exception cref="EndOfStreamException">The offset array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The array length is determined by <c>maxp.numGlyphs + 1</c> rather than by the table's own byte length.</description></item>
    /// <item><description>Short offsets are widened to <see cref="uint"/> and multiplied by two; the widening loop is vectorized with the widest available vector width (<see cref="Vector512{T}"/>, <see cref="Vector256{T}"/>, <see cref="Vector128{T}"/>, or <see cref="Vector64{T}"/>), with a scalar tail for the remainder.</description></item>
    /// <item><description>Long offsets are read as <see cref="uint"/> values in a single bulk read; no scaling is applied.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/loca">loca table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="HeadTable"/>
    /// <seealso cref="MaxpTable"/>
    /// <seealso cref="IndexToLocFormat"/>
    /// <seealso cref="FontFace.GetTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/loca">OpenType specification: loca table</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head">OpenType specification: head table</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp">OpenType specification: maxp table</seealso>
    public static LocaTable Parse(ref Cursor cursor, object? context)
    {
        // loca depends on head (for indexToLocFormat) and maxp (for numGlyphs).
        var face = (FontFace)context!;
        HeadTable head = face.GetTable<HeadTable>();
        MaxpTable maxp = face.GetTable<MaxpTable>();

        int count = checked(maxp.NumGlyphs + 1);
        var offsets = new uint[count];

        if (head.IndexToLocFormat == IndexToLocFormat.ShortOffsets)
        {
            // Short format: each entry is a uint16 storing offset/2. Widen to uint and multiply by 2,
            // vectorized where the platform supports it.
            ushort[] shorts = cursor.ReadUInt16Array(count);

            int i = 0;

            if (Vector512.IsHardwareAccelerated && count >= Vector512<ushort>.Count)
            {
                int width = Vector512<ushort>.Count;
                int last = count - width;
                for (; i <= last; i += width)
                {
                    var v = Vector512.LoadUnsafe(ref shorts[i]);
                    var (low, high) = Vector512.Widen(v);
                    low = Vector512.ShiftLeft(low, 1);
                    high = Vector512.ShiftLeft(high, 1);
                    low.StoreUnsafe(ref offsets[i]);
                    high.StoreUnsafe(ref offsets[i + Vector512<uint>.Count]);
                }
            }
            else if (Vector256.IsHardwareAccelerated && count >= Vector256<ushort>.Count)
            {
                int width = Vector256<ushort>.Count;
                int last = count - width;
                for (; i <= last; i += width)
                {
                    var v = Vector256.LoadUnsafe(ref shorts[i]);
                    var (low, high) = Vector256.Widen(v);
                    low = Vector256.ShiftLeft(low, 1);
                    high = Vector256.ShiftLeft(high, 1);
                    low.StoreUnsafe(ref offsets[i]);
                    high.StoreUnsafe(ref offsets[i + Vector256<uint>.Count]);
                }
            }
            else if (Vector128.IsHardwareAccelerated && count >= Vector128<ushort>.Count)
            {
                int width = Vector128<ushort>.Count;
                int last = count - width;
                for (; i <= last; i += width)
                {
                    var v = Vector128.LoadUnsafe(ref shorts[i]);
                    var (low, high) = Vector128.Widen(v);
                    low = Vector128.ShiftLeft(low, 1);
                    high = Vector128.ShiftLeft(high, 1);
                    low.StoreUnsafe(ref offsets[i]);
                    high.StoreUnsafe(ref offsets[i + Vector128<uint>.Count]);
                }
            }
            else if (Vector64.IsHardwareAccelerated && count >= Vector64<ushort>.Count)
            {
                int width = Vector64<ushort>.Count;
                int last = count - width;
                for (; i <= last; i += width)
                {
                    var v = Vector64.LoadUnsafe(ref shorts[i]);
                    var (low, high) = Vector64.Widen(v);
                    low = Vector64.ShiftLeft(low, 1);
                    high = Vector64.ShiftLeft(high, 1);
                    low.StoreUnsafe(ref offsets[i]);
                    high.StoreUnsafe(ref offsets[i + Vector64<uint>.Count]);
                }
            }

            // Scalar tail.
            for (; i < count; i++)
                offsets[i] = (uint)shorts[i] * 2;
        }
        else if (head.IndexToLocFormat == IndexToLocFormat.LongOffsets)
        {
            // Long format: each entry is a uint32 byte offset, read in one pass.
            cursor.ReadUInt32Array(offsets);
        }
        else
        {
            throw new InvalidDataException(
                $"'head'.indexToLocFormat is {(short)head.IndexToLocFormat}, expected 0 or 1.");
        }

        return new() { Offsets = offsets, Format = head.IndexToLocFormat };
    }
}

/// <summary>Format of the <c>loca</c> table, as specified by <c>head.indexToLocFormat</c>. Consumed by <c>loca</c>, <c>glyf</c>, and <c>gvar</c>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The two formats differ only in the width of the on-disk offset and whether the value is scaled. A parser that reads the table must consult <c>head</c> to know which form applies.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head"><c>indexToLocFormat</c> field</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LocaTable"/>
/// <seealso cref="HeadTable.IndexToLocFormat"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head">OpenType specification: head table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/loca">OpenType specification: loca table</seealso>
public enum IndexToLocFormat : short
{
    /// <summary>Short offsets: <c>loca</c> entries are 16-bit values, storing the actual byte offset divided by 2. Permitted only when the <c>glyf</c> table does not exceed 128 KB.</summary>
    /// <value>The constant <c>0</c>.</value>
    /// <remarks>Because each stored value is offset divided by 2, the maximum representable byte offset is 2 × <see cref="ushort.MaxValue"/> = 131 070 bytes, which bounds the whole <c>glyf</c> table.</remarks>
    /// <seealso cref="LongOffsets"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head">OpenType specification: head table</seealso>
    ShortOffsets = 0,

    /// <summary>Long offsets: <c>loca</c> entries are 32-bit byte offsets into the <c>glyf</c> table.</summary>
    /// <value>The constant <c>1</c>.</value>
    /// <remarks>Required for any font whose <c>glyf</c> table exceeds 128 KB, and permitted otherwise.</remarks>
    /// <seealso cref="ShortOffsets"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head">OpenType specification: head table</seealso>
    LongOffsets = 1,
}
