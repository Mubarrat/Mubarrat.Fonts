using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════
// Header
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The 4-byte CFF table header. Blittable, no padding.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Every field is a single byte, so no endianness reversal is required and the type deliberately does not implement <see cref="IEndianReversibleStruct{T}"/>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct CffHeader
{
    /// <summary>Gets the major version. 1 for CFF, 2 for CFF2.</summary>
    /// <value>The constant <c>1</c> for a conforming CFF 1.x table.</value>
    /// <seealso cref="Minor"/>
    public byte Major;

    /// <summary>Gets the minor version. Always 0.</summary>
    /// <value>The constant <c>0</c> for a conforming CFF 1.x table.</value>
    /// <seealso cref="Major"/>
    public byte Minor;

    /// <summary>Gets the header size in bytes. At least 4.</summary>
    /// <value>The on-disk size of the header; the parser seeks to this offset to reach the Name INDEX when the header is extended.</value>
    /// <seealso cref="OffSize"/>
    public byte HdrSize;

    /// <summary>Gets the absolute offset size used by INDEX structures. 1–4 bytes.</summary>
    /// <value>The number of bytes each offset occupies in the INDEX structures of this CFF table; the CFF 1 specification fixes this at 4.</value>
    /// <seealso cref="HdrSize"/>
    public byte OffSize;
}

// ═══════════════════════════════════════════════════════════════════════════
// Outline output
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A single Type 2 charstring: the byte code program that draws one glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The program is stored raw. Consumers that need outlines either run it through <see cref="CffInterpreter"/> (the path <see cref="CffTable.Parse"/> takes) or interpret it themselves.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF CharStrings INDEX</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffInterpreter"/>
/// <seealso cref="CffTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF CharStrings INDEX</seealso>
public sealed record CffCharString
{
    /// <summary>Gets the raw charstring byte code.</summary>
    /// <value>The charstring as stored on disk; not decoded or interpreted.</value>
    /// <seealso cref="FdIndex"/>
    public byte[] Program { get; init; } = [];

    /// <summary>Gets the Font DICT index this charstring belongs to. Always 0 for non-CID fonts.</summary>
    /// <value>The Font DICT index used when selecting local subroutines and default width parameters.</value>
    /// <seealso cref="Program"/>
    /// <seealso cref="CffFdSelect"/>
    public int FdIndex { get; init; }

    /// <inheritdoc/>
    /// <returns>A diagnostic string of the form <c>CffCharString(N bytes, fd I)</c>.</returns>
    /// <remarks>The format is intended for diagnostic output; it is not a stable serialization format.</remarks>
    /// <seealso cref="Program"/>
    /// <seealso cref="FdIndex"/>
    public override string ToString() => $"CffCharString({Program.Length} bytes, fd {FdIndex})";
}

/// <summary>A point on a CFF outline, in font design units.</summary>
/// <param name="X">The x coordinate.</param>
/// <param name="Y">The y coordinate.</param>
/// <remarks>Declared as a <c>readonly record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</remarks>
/// <seealso cref="CffSegment"/>
public readonly record struct CffPoint(double X, double Y);

/// <summary>A single segment of a contour.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>When <see cref="IsCubic"/> is <see langword="false"/> the segment is a straight line from the previous point to <see cref="End"/>, and the control points are ignored.</description></item>
/// <item><description>Declared as a <c>readonly record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffContour"/>
public readonly record struct CffSegment
{
    /// <summary>Gets the endpoint of the segment.</summary>
    /// <value>The point the pen moves to when the segment is drawn.</value>
    /// <seealso cref="Control1"/>
    /// <seealso cref="Control2"/>
    public CffPoint End { get; init; }

    /// <summary>Gets the first cubic Bézier control point. Ignored when <see cref="IsCubic"/> is <c>false</c>.</summary>
    /// <value>The first of two control points for a cubic segment; unspecified for line segments.</value>
    /// <seealso cref="Control2"/>
    /// <seealso cref="IsCubic"/>
    public CffPoint Control1 { get; init; }

    /// <summary>Gets the second cubic Bézier control point. Ignored when <see cref="IsCubic"/> is <c>false</c>.</summary>
    /// <value>The second of two control points for a cubic segment; unspecified for line segments.</value>
    /// <seealso cref="Control1"/>
    /// <seealso cref="IsCubic"/>
    public CffPoint Control2 { get; init; }

    /// <summary>True when the segment is a cubic Bézier; otherwise a straight line.</summary>
    /// <value><see langword="true"/> for a cubic segment, <see langword="false"/> for a line.</value>
    /// <seealso cref="Control1"/>
    /// <seealso cref="Control2"/>
    public bool IsCubic { get; init; }
}

/// <summary>A closed contour: a starting point plus the segments that follow it.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Contours are implicitly closed: the pen returns from the final segment's endpoint to <see cref="Start"/> when the shape is filled.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffGlyph"/>
/// <seealso cref="CffSegment"/>
public sealed record CffContour
{
    /// <summary>Gets the starting point of the contour.</summary>
    /// <value>The point set by the most recent moveto operator before the contour's segments were drawn.</value>
    /// <seealso cref="Segments"/>
    public CffPoint Start { get; init; }

    /// <summary>Gets the segments in drawing order.</summary>
    /// <value>The ordered list of segments; each begins at the previous segment's endpoint.</value>
    /// <seealso cref="Start"/>
    /// <seealso cref="CffSegment"/>
    public IReadOnlyList<CffSegment> Segments { get; init; } = [];
}

/// <summary>The interpreted outline of a single CFF glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The width is expressed in font design units, following the Type 2 charstring width rule: the charstring may declare a delta that is added to the font's <c>NominalWidthX</c>; when no delta is declared the <c>DefaultWidthX</c> applies.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffInterpreter"/>
/// <seealso cref="CffContour"/>
public sealed record CffGlyph
{
    /// <summary>Gets the contours in drawing order.</summary>
    /// <value>The ordered list of <see cref="CffContour"/> entries. Empty when the glyph has no outline (e.g. a space).</value>
    /// <seealso cref="ContourCount"/>
    public IReadOnlyList<CffContour> Contours { get; init; } = [];

    /// <summary>Gets the glyph's advance width in font design units. Defaults to the font's <c>DefaultWidthX</c> when the charstring does not declare a delta.</summary>
    /// <value>The resolved advance width; equals <c>NominalWidthX + delta</c> when <see cref="HasWidth"/> is <see langword="true"/>, otherwise <c>DefaultWidthX</c>.</value>
    /// <seealso cref="HasWidth"/>
    public double Width { get; init; }

    /// <summary>True when the charstring declared an explicit width delta.</summary>
    /// <value><see langword="true"/> when the charstring's first stack-clearing operator observed an odd operand count.</value>
    /// <seealso cref="Width"/>
    public bool HasWidth { get; init; }

    /// <summary>Gets the number of contours.</summary>
    /// <value>The size of the <see cref="Contours"/> list.</value>
    /// <seealso cref="Contours"/>
    public int ContourCount => Contours.Count;
}

// ═══════════════════════════════════════════════════════════════════════════
// INDEX
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A CFF INDEX: an array of variable-length data objects addressed by a parallel offset array.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The on-disk format is <c>uint16 count</c>, <c>uint8 offSize</c>, then <c>count + 1</c> offsets each <c>offSize</c> bytes wide, then the concatenated object data.</description></item>
/// <item><description>Offsets are 1-based and relative to the byte immediately preceding the data section.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF INDEX format</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF INDEX format</seealso>
public sealed record CffIndex : IRecord<CffIndex>
{
    /// <summary>An INDEX with zero entries, used where the spec permits an empty one.</summary>
    /// <remarks>The singleton uses a sentinel offset array of <c>[1]</c> and an empty data block, matching the on-disk representation of an empty INDEX.</remarks>
    public static readonly CffIndex Empty = new() { Count = 0, Offsets = [1], Data = [], DataStart = 0 };

    /// <summary>Gets the number of objects in the index.</summary>
    /// <value>The <c>uint16</c> count field from the on-disk INDEX.</value>
    /// <seealso cref="Offsets"/>
    /// <seealso cref="Data"/>
    public int Count { get; init; }

    /// <summary>Gets the byte offset array. Length is <see cref="Count"/> + 1.</summary>
    /// <value>An array of one-based offsets; <c>Offsets[i] - 1</c> is the byte index within <see cref="Data"/> where object <c>i</c> begins.</value>
    /// <seealso cref="GetRange(int)"/>
    /// <seealso cref="GetBytes(int)"/>
    public int[] Offsets { get; init; } = [];

    /// <summary>Gets the raw object data block.</summary>
    /// <value>The concatenated object bytes; length is <c>Offsets[Count] - 1</c>.</value>
    /// <seealso cref="GetBytes(int)"/>
    public byte[] Data { get; init; } = [];

    /// <summary>Gets the byte offset of the data block within the CFF table.</summary>
    /// <value>The file offset of the first byte of <see cref="Data"/>. Used by callers that need to convert the index's local offsets to absolute positions.</value>
    /// <seealso cref="Data"/>
    public long DataStart { get; init; }

    /// <summary>Gets the byte range of object <paramref name="index"/> within <see cref="Data"/>.</summary>
    /// <param name="index">The zero-based index of the object.</param>
    /// <returns>A <c>(Start, Length)</c> tuple giving the object's position and size within <see cref="Data"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    /// <remarks>Offsets are stored one-based on disk, so the method subtracts one from each before computing the range.</remarks>
    /// <seealso cref="Data"/>
    /// <seealso cref="Offsets"/>
    public (int Start, int Length) GetRange(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
        int start = Offsets[index] - 1;
        int end = Offsets[index + 1] - 1;
        return (start, end - start);
    }

    /// <summary>Gets a copy of object <paramref name="index"/> as a byte array.</summary>
    /// <param name="index">The zero-based index of the object.</param>
    /// <returns>A fresh array containing the object's bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    /// <remarks>The returned array is a copy; mutating it does not affect <see cref="Data"/>.</remarks>
    /// <seealso cref="GetRange(int)"/>
    public byte[] GetBytes(int index)
    {
        (int start, int length) = GetRange(index);
        byte[] result = new byte[length];
        Array.Copy(Data, start, result, 0, length);
        return result;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the INDEX.</param>
    /// <param name="context">Unused. The INDEX is self-describing.</param>
    /// <returns>The parsed INDEX.</returns>
    /// <exception cref="InvalidDataException">The offset size is outside 1–4, the first offset is not 1, or the offsets are not monotonic.</exception>
    /// <exception cref="EndOfStreamException">The count, offset array, or data block extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The count is read first; a count of zero short-circuits and returns an INDEX with a sentinel offset array and empty data.</description></item>
    /// <item><description>The offset array is validated for the two invariants the specification requires: the first offset is exactly 1 and every subsequent offset is at least as large as the previous one.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF INDEX format</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Offsets"/>
    /// <seealso cref="Data"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF INDEX format</seealso>
    public static CffIndex Parse(ref Cursor cursor, object? context = null)
    {
        ushort count = cursor.ReadUInt16();
        if (count == 0)
            return new() { Count = 0, Offsets = [1], Data = [], DataStart = cursor.Position };

        byte offSize = cursor.ReadUInt8();
        if (offSize is < 1 or > 4)
            throw new InvalidDataException($"CFF INDEX offSize is {offSize}, expected 1–4.");

        int[] offsets = new int[count + 1];
        for (int i = 0; i <= count; i++)
            offsets[i] = ReadOffset(ref cursor, offSize);

        if (offsets[0] != 1)
            throw new InvalidDataException($"CFF INDEX first offset is {offsets[0]}, expected 1.");
        for (int i = 1; i <= count; i++)
            if (offsets[i] < offsets[i - 1])
                throw new InvalidDataException($"CFF INDEX offsets are not monotonic at index {i}.");

        long dataStart = cursor.Position;
        int dataLength = offsets[count] - 1;
        byte[] data = cursor.ReadBytes(dataLength);

        return new CffIndex { Count = count, Offsets = offsets, Data = data, DataStart = dataStart };
    }

    /// <summary>Reads a big-endian offset of <paramref name="offSize"/> bytes.</summary>
    /// <param name="cursor">The cursor to read from.</param>
    /// <param name="offSize">The number of bytes in the offset. Must be 1–4.</param>
    /// <returns>The offset value, assembled from <paramref name="offSize"/> big-endian bytes.</returns>
    /// <remarks>The method does not validate <paramref name="offSize"/>; callers are expected to have validated it against 1–4 before reading offsets.</remarks>
    /// <seealso cref="Parse"/>
    public static int ReadOffset(ref Cursor cursor, int offSize)
    {
        int value = 0;
        for (int i = 0; i < offSize; i++)
            value = (value << 8) | cursor.ReadUInt8();
        return value;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// DICT
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A parsed CFF DICT: a set of (operator, operands) entries.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>On disk a DICT is a flat stream of number encodings and operators. Numbers are pushed onto a stack; an operator pops its operands and records an entry.</description></item>
/// <item><description>Operators are one byte (0–21) or two bytes (12 followed by a second byte).</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF DICT Data</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffTopDict"/>
/// <seealso cref="CffPrivateDict"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF DICT Data</seealso>
public sealed record CffDict : IRecord<CffDict>
{
    /// <summary>Gets the operator-to-operands map.</summary>
    /// <value>A dictionary keyed by operator code; the value is the array of operands that preceded the operator in the DICT stream.</value>
    /// <seealso cref="Get(ushort)"/>
    /// <seealso cref="Contains(ushort)"/>
    public Dictionary<ushort, double[]> Entries { get; init; } = [];

    /// <summary>Gets the number of entries.</summary>
    /// <value>The size of the <see cref="Entries"/> dictionary.</value>
    /// <seealso cref="Entries"/>
    public int Count => Entries.Count;

    /// <summary>Gets the operand array for <paramref name="op"/>, or <c>null</c> when absent.</summary>
    /// <param name="op">The DICT operator code.</param>
    /// <returns>The operand array for the operator, or <c>null</c> when the operator is not present.</returns>
    /// <seealso cref="Entries"/>
    /// <seealso cref="Contains(ushort)"/>
    public double[]? Get(ushort op) => Entries.TryGetValue(op, out double[]? v) ? v : null;

    /// <summary>Gets the first operand for <paramref name="op"/>, or <paramref name="fallback"/> when absent.</summary>
    /// <param name="op">The DICT operator code.</param>
    /// <param name="fallback">The value to return when the operator is absent or has no operands.</param>
    /// <returns>The first operand of the operator, or <paramref name="fallback"/> when absent.</returns>
    /// <seealso cref="Get(ushort)"/>
    /// <seealso cref="GetOptionalNumber(ushort)"/>
    public double GetNumber(ushort op, double fallback = 0) =>
        Entries.TryGetValue(op, out double[]? v) && v.Length > 0 ? v[0] : fallback;

    /// <summary>Gets the first operand for <paramref name="op"/>, or <c>null</c> when absent.</summary>
    /// <param name="op">The DICT operator code.</param>
    /// <returns>The first operand of the operator, or <c>null</c> when the operator is absent or has no operands.</returns>
    /// <seealso cref="GetNumber(ushort, double)"/>
    public double? GetOptionalNumber(ushort op) =>
        Entries.TryGetValue(op, out double[]? v) && v.Length > 0 ? v[0] : null;

    /// <summary>True when the DICT contains <paramref name="op"/>.</summary>
    /// <param name="op">The DICT operator code.</param>
    /// <returns><see langword="true"/> when the operator is present in the DICT.</returns>
    /// <seealso cref="Entries"/>
    public bool Contains(ushort op) => Entries.ContainsKey(op);

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the DICT.</param>
    /// <param name="context">A <see cref="CffDictContext"/> carrying the DICT's byte length.</param>
    /// <returns>The parsed DICT.</returns>
    /// <exception cref="InvalidDataException">An unrecognised DICT byte is encountered, or a real number uses the reserved nibble.</exception>
    /// <exception cref="EndOfStreamException">The DICT extends past the declared length.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The loop runs until the cursor reaches the position declared by the context's <see cref="CffDictContext.Length"/>; DICT streams have no self-describing terminator.</description></item>
    /// <item><description>Each operator consumes the operand stack and records it under the operator's code; the stack is cleared for the next operator.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF DICT Data</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CffDictContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF DICT Data</seealso>
    public static CffDict Parse(ref Cursor cursor, object? context)
    {
        CffDictContext ctx = (CffDictContext)context!;
        long end = cursor.Position + ctx.Length;

        CffDict dict = new();
        List<double> stack = new(8);

        while (cursor.Position < end)
        {
            byte b0 = cursor.ReadUInt8();
            if (b0 <= 21)
            {
                ushort op = b0 == 12 ? (ushort)(0x0C00 | cursor.ReadUInt8()) : b0;
                dict.Entries[op] = [.. stack];
                stack.Clear();
            }
            else if (b0 == 28)
                stack.Add(cursor.ReadInt16());
            else if (b0 == 29)
                stack.Add(cursor.ReadInt32());
            else if (b0 == 30)
                stack.Add(ReadReal(ref cursor));
            else if (b0 is >= 32 and <= 246)
                stack.Add(b0 - 139);
            else if (b0 is >= 247 and <= 250)
                stack.Add((b0 - 247) * 256 + cursor.ReadUInt8() + 108);
            else if (b0 is >= 251 and <= 254)
                stack.Add(-(b0 - 251) * 256 - cursor.ReadUInt8() - 108);
            else
                throw new InvalidDataException($"Invalid CFF DICT byte 0x{b0:X2}.");
        }

        return dict;
    }

    /// <summary>Reads a CFF real number encoded as nibbles terminated by <c>0xF</c>.</summary>
    /// <param name="cursor">The cursor to read from.</param>
    /// <returns>The decoded real number.</returns>
    /// <exception cref="InvalidDataException">The number contains the reserved nibble <c>0xD</c>, exceeds 64 characters, or contains an undefined nibble.</exception>
    /// <remarks>Nibbles are mapped to characters as: <c>0–9</c> digits, <c>A</c> = decimal point, <c>B</c>/<c>C</c> = exponent, <c>E</c> = minus sign, <c>F</c> = terminator.</remarks>
    public static double ReadReal(ref Cursor cursor)
    {
        Span<char> buffer = stackalloc char[64];
        int n = 0;

    done: while (true)
        {
            byte b = cursor.ReadUInt8();
            for (int half = 0; half < 2; half++)
            {
                int nibble = half == 0 ? (b >> 4) & 0xF : b & 0xF;
                if (nibble == 0xF) break done;
                if (nibble == 0xD)
                    throw new InvalidDataException("CFF real number contains the reserved nibble 0xD.");
                if (n >= buffer.Length)
                    throw new InvalidDataException("CFF real number exceeds 64 characters.");
                buffer[n++] = nibble switch
                {
                    <= 9 => (char)('0' + nibble),
                    0xA => '.',
                    0xB => 'E',
                    0xC => 'E',
                    0xE => '-',
                    _ => throw new InvalidDataException($"CFF real number contains invalid nibble 0x{nibble:X}."),
                };
            }
        }

        return double.Parse(buffer[..n], CultureInfo.InvariantCulture);
    }
}

/// <summary>Context for parsing a DICT over a declared byte length.</summary>
/// <param name="Length">The number of bytes the DICT occupies.</param>
/// <remarks>
/// <list type="bullet">
/// <item><description>DICT streams have no self-describing terminator, so the parser needs the length to know where to stop.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffDict.Parse"/>
public record CffDictContext(int Length);

// ═══════════════════════════════════════════════════════════════════════════
// Top DICT
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A parsed CFF Top DICT: font-level metadata and pointers to the other CFF structures.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>All operands are exposed as computed properties over <see cref="Raw"/>; the raw DICT is preserved so callers can read operators not surfaced here.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF Top DICT</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffTable"/>
/// <seealso cref="CffDict"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF Top DICT</seealso>
public sealed record CffTopDict : IRecord<CffTopDict>
{
    /// <summary>Top DICT operator codes.</summary>
    /// <remarks>Only the operators that OpenType CFF fonts can legally use are listed; others are undefined by the specification's embedding rules.</remarks>
    public static class Ops
    {
        /// <summary>0: the Version SID.</summary>
        public const ushort Version = 0;
        /// <summary>1: the Notice SID.</summary>
        public const ushort Notice = 1;
        /// <summary>2: the FullName SID.</summary>
        public const ushort FullName = 2;
        /// <summary>3: the FamilyName SID.</summary>
        public const ushort FamilyName = 3;
        /// <summary>4: the Weight SID.</summary>
        public const ushort Weight = 4;
        /// <summary>5: the FontBBox operands.</summary>
        public const ushort FontBBox = 5;
        /// <summary>15: the charset offset.</summary>
        public const ushort Charset = 15;
        /// <summary>16: the encoding offset.</summary>
        public const ushort Encoding = 16;
        /// <summary>17: the CharStrings INDEX offset.</summary>
        public const ushort CharStrings = 17;
        /// <summary>18: the Private DICT (size, offset) pair.</summary>
        public const ushort Private = 18;
        /// <summary>12 30: the ROS operands for a CID-keyed font.</summary>
        public const ushort ROS = 0x0C1E;
        /// <summary>12 31: the CIDFontVersion.</summary>
        public const ushort CIDFontVersion = 0x0C1F;
        /// <summary>12 36: the FDArray INDEX offset.</summary>
        public const ushort FDArray = 0x0C24;
        /// <summary>12 37: the FDSelect offset.</summary>
        public const ushort FDSelect = 0x0C25;
        /// <summary>12 38: the FontName SID.</summary>
        public const ushort FontName = 0x0C26;
        /// <summary>12 6: the CharstringType. Must be 2.</summary>
        public const ushort CharstringType = 0x0C06;
    }

    /// <summary>Gets the raw DICT.</summary>
    /// <value>The underlying <see cref="CffDict"/>; all named accessors delegate to it.</value>
    /// <seealso cref="CffDict"/>
    public CffDict Raw { get; init; } = null!;

    /// <summary>Gets the Version SID, or 0.</summary>
    /// <value>The SID for the font's Version string, or <c>0</c> when the operator is absent.</value>
    /// <seealso cref="Ops.Version"/>
    public ushort VersionSid => (ushort)Raw.GetNumber(Ops.Version);

    /// <summary>Gets the Notice SID, or 0.</summary>
    /// <value>The SID for the font's Notice string, or <c>0</c> when the operator is absent.</value>
    /// <seealso cref="Ops.Notice"/>
    public ushort NoticeSid => (ushort)Raw.GetNumber(Ops.Notice);

    /// <summary>Gets the FullName SID, or 0.</summary>
    /// <value>The SID for the font's FullName string, or <c>0</c> when the operator is absent.</value>
    /// <seealso cref="Ops.FullName"/>
    public ushort FullNameSid => (ushort)Raw.GetNumber(Ops.FullName);

    /// <summary>Gets the FamilyName SID, or 0.</summary>
    /// <value>The SID for the font's FamilyName string, or <c>0</c> when the operator is absent.</value>
    /// <seealso cref="Ops.FamilyName"/>
    public ushort FamilyNameSid => (ushort)Raw.GetNumber(Ops.FamilyName);

    /// <summary>Gets the Weight SID, or 0.</summary>
    /// <value>The SID for the font's Weight string, or <c>0</c> when the operator is absent.</value>
    /// <seealso cref="Ops.Weight"/>
    public ushort WeightSid => (ushort)Raw.GetNumber(Ops.Weight);

    /// <summary>Gets the FontName SID, or 0.</summary>
    /// <value>The SID for the PostScript font name (CID-keyed fonts only), or <c>0</c> when absent.</value>
    /// <seealso cref="Ops.FontName"/>
    public ushort FontNameSid => (ushort)Raw.GetNumber(Ops.FontName);

    /// <summary>Gets the FontBBox operands, or an empty array.</summary>
    /// <value>The four bounding-box values <c>[xMin, yMin, xMax, yMax]</c>, or an empty array when the operator is absent.</value>
    /// <seealso cref="Ops.FontBBox"/>
    public double[] FontBBox => Raw.Get(Ops.FontBBox) ?? [];

    /// <summary>Gets the charset offset. 0 selects ISOAdobe, 1 Expert, 2 ExpertSubset; other values are byte offsets.</summary>
    /// <value>The raw charset offset from the Top DICT; interpretation is performed by <see cref="CffCharset.FromOffset"/>.</value>
    /// <seealso cref="Ops.Charset"/>
    public int CharsetOffset => (int)Raw.GetNumber(Ops.Charset);

    /// <summary>Gets the encoding offset. 0 selects Standard, 1 Expert; other values are byte offsets.</summary>
    /// <value>The raw encoding offset from the Top DICT; interpretation is performed by <see cref="CffEncoding.FromOffset"/>.</value>
    /// <seealso cref="Ops.Encoding"/>
    public int EncodingOffset => (int)Raw.GetNumber(Ops.Encoding);

    /// <summary>Gets the CharStrings INDEX offset.</summary>
    /// <value>The byte offset of the CharStrings INDEX, measured from the CFF table start.</value>
    /// <seealso cref="Ops.CharStrings"/>
    public int CharStringsOffset => (int)Raw.GetNumber(Ops.CharStrings);

    /// <summary>Gets the CharstringType. Must be 2 for OpenType CFF.</summary>
    /// <value>The charstring interpreter format; <c>2</c> is the only value allowed in an OpenType CFF table.</value>
    /// <seealso cref="Ops.CharstringType"/>
    public int CharstringType => (int)Raw.GetNumber(Ops.CharstringType, 2);

    /// <summary>Gets the (size, offset) pair from the Private operator, or <c>null</c>.</summary>
    /// <value>A tuple of the Private DICT's byte length and its offset from the CFF table start, or <c>null</c> when the operator is missing or malformed.</value>
    /// <seealso cref="Ops.Private"/>
    public (int Size, int Offset)? Private
    {
        get
        {
            double[]? priv = Raw.Get(Ops.Private);
            return priv is { Length: >= 2 } ? ((int)priv[0], (int)priv[1]) : null;
        }
    }

    /// <summary>Gets the ROS operands for a CID-keyed font, or <c>null</c>.</summary>
    /// <value>The three ROS operands <c>[registry, ordering, supplement]</c> as SIDs, or <c>null</c> when the font is not CID-keyed.</value>
    /// <seealso cref="IsCIDFont"/>
    /// <seealso cref="Ops.ROS"/>
    public double[]? ROS => Raw.Get(Ops.ROS);

    /// <summary>Gets the FDArray INDEX offset for a CID-keyed font, or 0.</summary>
    /// <value>The byte offset of the FDArray INDEX from the CFF table start, or <c>0</c> when absent.</value>
    /// <seealso cref="Ops.FDArray"/>
    public int FDArrayOffset => (int)Raw.GetNumber(Ops.FDArray);

    /// <summary>Gets the FDSelect offset for a CID-keyed font, or 0.</summary>
    /// <value>The byte offset of the FDSelect subtable from the CFF table start, or <c>0</c> when absent.</value>
    /// <seealso cref="Ops.FDSelect"/>
    public int FDSelectOffset => (int)Raw.GetNumber(Ops.FDSelect);

    /// <summary>True when this is a CID-keyed font.</summary>
    /// <value><see langword="true"/> when the ROS operator is present.</value>
    /// <seealso cref="ROS"/>
    public bool IsCIDFont => ROS is not null;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the Top DICT.</param>
    /// <param name="context">A <see cref="CffDictContext"/> carrying the DICT's byte length.</param>
    /// <returns>The parsed Top DICT.</returns>
    /// <exception cref="EndOfStreamException">The DICT extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF Top DICT</see> in the OpenType specification.</remarks>
    /// <seealso cref="CffDict.Parse"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF Top DICT</seealso>
    public static CffTopDict Parse(ref Cursor cursor, object? context)
    {
        CffDict raw = CffDict.Parse(ref cursor, context);
        return new CffTopDict { Raw = raw };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Private DICT
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A parsed CFF Private DICT. Holds hinting parameters, the local Subrs offset, and the default and nominal glyph widths.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The Private DICT carries the <c>DefaultWidthX</c> and <c>NominalWidthX</c> values that the Type 2 interpreter uses to resolve glyph advances when a charstring omits or declares a width delta.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF Private DICT</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffTopDict"/>
/// <seealso cref="CffFontDict"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF Private DICT</seealso>
public sealed record CffPrivateDict : IRecord<CffPrivateDict>
{
    /// <summary>Private DICT operator codes.</summary>
    /// <remarks>The Private DICT carries hinting and width parameters; all operators are optional.</remarks>
    public static class Ops
    {
        /// <summary>6: blue zone values.</summary>
        public const ushort BlueValues = 6;
        /// <summary>7: other blue zone values.</summary>
        public const ushort OtherBlues = 7;
        /// <summary>8: family blue zone values.</summary>
        public const ushort FamilyBlues = 8;
        /// <summary>9: other family blue zone values.</summary>
        public const ushort FamilyOtherBlues = 9;
        /// <summary>12 9: blue scale.</summary>
        public const ushort BlueScale = 0x0C09;
        /// <summary>12 10: blue shift.</summary>
        public const ushort BlueShift = 0x0C0A;
        /// <summary>12 11: blue fuzz.</summary>
        public const ushort BlueFuzz = 0x0C0B;
        /// <summary>10: standard horizontal stem width.</summary>
        public const ushort StdHW = 10;
        /// <summary>11: standard vertical stem width.</summary>
        public const ushort StdVW = 11;
        /// <summary>12 12: horizontal stem snap.</summary>
        public const ushort StemSnapH = 0x0C0C;
        /// <summary>12 13: vertical stem snap.</summary>
        public const ushort StemSnapV = 0x0C0D;
        /// <summary>12 14: force bold flag.</summary>
        public const ushort ForceBold = 0x0C0E;
        /// <summary>12 17: language group.</summary>
        public const ushort LanguageGroup = 0x0C11;
        /// <summary>12 18: expansion factor.</summary>
        public const ushort ExpansionFactor = 0x0C12;
        /// <summary>12 19: initial random seed.</summary>
        public const ushort InitialRandomSeed = 0x0C13;
        /// <summary>19: offset to the local Subrs INDEX, relative to the Private DICT start.</summary>
        public const ushort Subrs = 19;
        /// <summary>20: default glyph width.</summary>
        public const ushort DefaultWidthX = 20;
        /// <summary>21: nominal glyph width.</summary>
        public const ushort NominalWidthX = 21;
    }

    /// <summary>Gets the raw DICT.</summary>
    /// <value>The underlying <see cref="CffDict"/>; all named accessors delegate to it.</value>
    /// <seealso cref="CffDict"/>
    public CffDict Raw { get; init; } = null!;

    /// <summary>Gets the byte offset of the local Subrs INDEX relative to the Private DICT start, or 0.</summary>
    /// <value>The offset of the local subroutine INDEX from the Private DICT start, or <c>0</c> when absent.</value>
    /// <seealso cref="Ops.Subrs"/>
    /// <seealso cref="CffTable.ResolveLocalSubrs"/>
    public int SubrsOffset => (int)Raw.GetNumber(Ops.Subrs);

    /// <summary>Gets the default glyph width.</summary>
    /// <value>The default advance width used when a charstring declares no width delta; <c>0</c> when the operator is absent.</value>
    /// <seealso cref="NominalWidthX"/>
    /// <seealso cref="Ops.DefaultWidthX"/>
    public double DefaultWidthX => Raw.GetNumber(Ops.DefaultWidthX);

    /// <summary>Gets the nominal glyph width.</summary>
    /// <value>The nominal width added to a charstring's width delta to compute the glyph's advance; <c>0</c> when the operator is absent.</value>
    /// <seealso cref="DefaultWidthX"/>
    /// <seealso cref="Ops.NominalWidthX"/>
    public double NominalWidthX => Raw.GetNumber(Ops.NominalWidthX);

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the Private DICT.</param>
    /// <param name="context">A <see cref="CffDictContext"/> carrying the DICT's byte length.</param>
    /// <returns>The parsed Private DICT.</returns>
    /// <exception cref="EndOfStreamException">The DICT extends past the end of the source.</exception>
    /// <remarks>All Private DICT operators are optional; an empty DICT parses successfully to an instance with default field values.</remarks>
    /// <seealso cref="CffDict.Parse"/>
    public static CffPrivateDict Parse(ref Cursor cursor, object? context)
    {
        CffDict raw = CffDict.Parse(ref cursor, context);
        return new CffPrivateDict { Raw = raw };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Font DICT (CID)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A parsed Font DICT from a CID-keyed font's FDArray.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The FD DICT uses the Top DICT operator set. Its <c>Private</c> operator points to the nested Private DICT that carries <c>Subrs</c> and the default widths.</description></item>
/// <item><description>That nested DICT is resolved during parse and exposed as <see cref="PrivateDict"/>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF Font DICT</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffTable"/>
/// <seealso cref="CffPrivateDict"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF Font DICT</seealso>
public sealed record CffFontDict : IRecord<CffFontDict>
{
    /// <summary>Gets the raw FD DICT.</summary>
    /// <value>The underlying <see cref="CffDict"/>; the Private accessor delegates to it.</value>
    /// <seealso cref="CffDict"/>
    public CffDict Raw { get; init; } = null!;

    /// <summary>Gets the (size, offset) pair from the Private operator, or <c>null</c>.</summary>
    /// <value>A tuple of the Private DICT's byte length and its offset from the CFF table start, or <c>null</c> when the operator is missing or malformed.</value>
    /// <seealso cref="PrivateDict"/>
    public (int Size, int Offset)? Private
    {
        get
        {
            double[]? priv = Raw.Get(CffTopDict.Ops.Private);
            return priv is { Length: >= 2 } ? ((int)priv[0], (int)priv[1]) : null;
        }
    }

    /// <summary>Gets the parsed Private DICT, or <c>null</c> when the FD DICT declares none.</summary>
    /// <value>The resolved <see cref="CffPrivateDict"/>, or <c>null</c> when the Private operator is absent or specifies a zero size or offset.</value>
    /// <seealso cref="Private"/>
    /// <seealso cref="CffPrivateDict"/>
    public CffPrivateDict? PrivateDict { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the FD DICT.</param>
    /// <param name="context">A <see cref="CffFontDictContext"/> carrying the DICT's byte length and the CFF table source.</param>
    /// <returns>The parsed Font DICT with its Private DICT resolved.</returns>
    /// <exception cref="EndOfStreamException">The DICT or its Private DICT extends past the end of the CFF table source.</exception>
    /// <remarks>The FD DICT's Private offset is absolute within the CFF table, not relative to the FD DICT; the parser uses the parent source directly.</remarks>
    /// <seealso cref="CffFontDictContext"/>
    /// <seealso cref="CffPrivateDict"/>
    public static CffFontDict Parse(ref Cursor cursor, object? context)
    {
        CffFontDictContext ctx = (CffFontDictContext)context!;
        CffDict raw = CffDict.Parse(ref cursor, new CffDictContext(ctx.Length));

        CffPrivateDict? privateDict = null;
        double[]? priv = raw.Get(CffTopDict.Ops.Private);
        if (priv is { Length: >= 2 })
        {
            int size = (int)priv[0];
            int offset = (int)priv[1];
            if (size > 0 && offset > 0)
            {
                privateDict = ctx.ParentSource.ParseRecordAt<CffPrivateDict>(
                    offset, new CffDictContext(size));
            }
        }

        return new CffFontDict { Raw = raw, PrivateDict = privateDict };
    }
}

/// <summary>Context for parsing a Font DICT. Carries the DICT's byte length and the CFF table source, needed to resolve the FD DICT's absolute Private offset.</summary>
/// <param name="Length">The number of bytes the FD DICT occupies.</param>
/// <param name="ParentSource">The CFF table source, used to resolve the nested Private DICT.</param>
/// <remarks>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</remarks>
/// <seealso cref="CffFontDict.Parse"/>
public record CffFontDictContext(int Length, Source ParentSource);

// ═══════════════════════════════════════════════════════════════════════════
// Charset
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A CFF charset: maps glyph IDs to string identifiers (SIDs). Glyph 0 is always <c>.notdef</c> and is not stored on disk.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Three charset formats exist on disk (0, 1, 2) that differ in how ranges of consecutive SIDs are encoded. All three expand to the same flat per-glyph SID array in this type.</description></item>
/// <item><description>Three predefined charsets (ISOAdobe, Expert, ExpertSubset) are selected by special offsets 0, 1, and 2 in the Top DICT; they have no on-disk representation.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF Charset</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF Charset</seealso>
public sealed record CffCharset : IRecord<CffCharset>
{
    /// <summary>Gets the SIDs, indexed by glyph ID. Entry 0 is always 0 (<c>.notdef</c>).</summary>
    /// <value>The per-glyph SID array; length equals the font's glyph count.</value>
    /// <seealso cref="GetSid(int)"/>
    public ushort[] Sids { get; init; } = [];

    /// <summary>Gets the number of glyphs.</summary>
    /// <value>The size of the <see cref="Sids"/> array.</value>
    /// <seealso cref="Sids"/>
    public int Count => Sids.Length;

    /// <summary>Gets the SID for glyph <paramref name="glyphId"/>.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <returns>The SID assigned to the glyph.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is out of range.</exception>
    /// <remarks>The lookup is constant-time; the glyph ID directly indexes into <see cref="Sids"/>.</remarks>
    /// <seealso cref="Sids"/>
    public ushort GetSid(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, Sids.Length);
        return Sids[glyphId];
    }

    /// <summary>Resolves the charset selected by <paramref name="offset"/> in the Top DICT.</summary>
    /// <param name="source">The CFF table source.</param>
    /// <param name="offset">0 for ISOAdobe, 1 for Expert, 2 for ExpertSubset; other values parse a custom charset.</param>
    /// <param name="numGlyphs">The number of glyphs, from the CharStrings INDEX.</param>
    /// <returns>The resolved <see cref="CffCharset"/>.</returns>
    /// <remarks>The three predefined offsets are handled by dedicated builder methods; any other offset is treated as a byte offset into the CFF table and parsed as a custom charset.</remarks>
    /// <example>
    /// <code>
    /// CffCharset charset = CffCharset.FromOffset(source, topDict.CharsetOffset, numGlyphs);
    /// </code>
    /// </example>
    /// <seealso cref="IsoAdobe(int)"/>
    /// <seealso cref="Expert(int)"/>
    /// <seealso cref="ExpertSubset(int)"/>
    public static CffCharset FromOffset(Source source, int offset, int numGlyphs) => offset switch
    {
        0 => IsoAdobe(numGlyphs),
        1 => Expert(numGlyphs),
        2 => ExpertSubset(numGlyphs),
        _ => source.ParseRecordAt<CffCharset>(offset, new CffCharsetContext(numGlyphs)),
    };

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the charset record.</param>
    /// <param name="context">A <see cref="CffCharsetContext"/> carrying the font's glyph count.</param>
    /// <returns>The parsed charset with its flat per-glyph SID array.</returns>
    /// <exception cref="InvalidDataException">The format is not 0, 1, or 2.</exception>
    /// <exception cref="EndOfStreamException">The format-specific data extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Format 0 stores one uint16 per glyph (starting at glyph 1).</description></item>
    /// <item><description>Formats 1 and 2 store ranges; the parser expands each range into per-glyph entries, capping the expansion at the font's glyph count.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF Charset</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CffCharsetContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF Charset</seealso>
    public static CffCharset Parse(ref Cursor cursor, object? context)
    {
        CffCharsetContext ctx = (CffCharsetContext)context!;
        byte format = cursor.ReadUInt8();

        ushort[] sids = new ushort[ctx.NumGlyphs];
        // Glyph 0 is .notdef and is not stored.
        switch (format)
        {
            case 0:
                for (int gid = 1; gid < ctx.NumGlyphs; gid++)
                    sids[gid] = cursor.ReadUInt16();
                break;

            case 1:
                {
                    int gid = 1;
                    while (gid < ctx.NumGlyphs)
                    {
                        ushort first = cursor.ReadUInt16();
                        byte nLeft = cursor.ReadUInt8();
                        for (int i = 0; i <= nLeft && gid < ctx.NumGlyphs; i++)
                            sids[gid++] = (ushort)(first + i);
                    }
                    break;
                }

            case 2:
                {
                    int gid = 1;
                    while (gid < ctx.NumGlyphs)
                    {
                        ushort first = cursor.ReadUInt16();
                        ushort nLeft = cursor.ReadUInt16();
                        for (int i = 0; i <= nLeft && gid < ctx.NumGlyphs; i++)
                            sids[gid++] = (ushort)(first + i);
                    }
                    break;
                }

            default:
                throw new InvalidDataException($"Invalid CFF charset format {format}.");
        }

        return new CffCharset { Sids = sids };
    }

    /// <summary>Builds the ISOAdobe predefined charset.</summary>
    /// <param name="numGlyphs">The number of glyphs.</param>
    /// <returns>The ISOAdobe charset, with SIDs 1–228 assigned to glyphs 1–228 in order.</returns>
    /// <remarks>The ISOAdobe charset is a direct identity mapping for the first 229 SIDs; glyphs beyond 228 are assigned SID 0.</remarks>
    public static CffCharset IsoAdobe(int numGlyphs)
    {
        ushort[] sids = new ushort[numGlyphs];
        int max = Math.Min(numGlyphs - 1, 228);
        for (int gid = 1; gid <= max; gid++)
            sids[gid] = (ushort)gid;
        return new CffCharset { Sids = sids };
    }

    /// <summary>Builds the Expert predefined charset.</summary>
    /// <param name="numGlyphs">The number of glyphs.</param>
    /// <returns>The Expert charset, with SIDs taken from the specification's predefined list.</returns>
    public static CffCharset Expert(int numGlyphs)
    {
        ushort[] sids = new ushort[numGlyphs];
        int max = Math.Min(numGlyphs - 1, ExpertSids.Length);
        for (int gid = 1; gid <= max; gid++)
            sids[gid] = ExpertSids[gid - 1];
        return new CffCharset { Sids = sids };
    }

    /// <summary>Builds the ExpertSubset predefined charset.</summary>
    /// <param name="numGlyphs">The number of glyphs.</param>
    /// <returns>The ExpertSubset charset, with SIDs taken from the specification's predefined list.</returns>
    public static CffCharset ExpertSubset(int numGlyphs)
    {
        ushort[] sids = new ushort[numGlyphs];
        int max = Math.Min(numGlyphs - 1, ExpertSubsetSids.Length);
        for (int gid = 1; gid <= max; gid++)
            sids[gid] = ExpertSubsetSids[gid - 1];
        return new CffCharset { Sids = sids };
    }

    private static readonly ushort[] ExpertSids =
    [
        1, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 13, 14, 15, 99, 239,
        240, 241, 242, 243, 244, 245, 246, 247, 248, 27, 28, 249, 250, 251, 252,
        253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 109,
        110, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280,
        281, 282, 283, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 295,
        296, 297, 298, 299, 300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 310,
        311, 312, 313, 314, 315, 316, 317, 318, 158, 155, 163, 319, 320, 321, 322,
        323, 324, 325, 326, 150, 164, 169, 327, 328, 329, 330, 331, 332, 333, 334,
        335, 336, 337, 338, 339, 340, 341, 342, 343, 344, 345, 346, 347, 348, 349,
        350, 351, 352, 353, 354, 355, 356, 357, 358, 359, 360, 361, 362, 363, 364,
        365, 366, 367, 368, 369, 370, 371, 372, 373, 374, 375, 376, 377, 378,
    ];

    private static readonly ushort[] ExpertSubsetSids =
    [
        1, 231, 232, 235, 236, 237, 238, 13, 14, 15, 99, 239, 240, 241, 242, 243,
        244, 245, 246, 247, 248, 27, 28, 249, 250, 251, 253, 254, 255, 256, 257,
        258, 259, 260, 261, 262, 263, 264, 265, 266, 109, 110, 267, 268, 269, 270,
        272, 300, 301, 302, 305, 314, 315, 158, 155, 163, 320, 321, 322, 323, 324,
        325, 326, 150, 164, 169, 327, 328, 329, 330, 331, 332, 333, 334, 335, 336,
        337, 338, 339, 340, 341, 342, 343, 344, 345, 346,
    ];
}

/// <summary>Context for parsing a custom charset.</summary>
/// <param name="NumGlyphs">The number of glyphs, from the CharStrings INDEX.</param>
/// <remarks>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</remarks>
/// <seealso cref="CffCharset.Parse"/>
public record CffCharsetContext(int NumGlyphs);

// ═══════════════════════════════════════════════════════════════════════════
// Encoding
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A CFF encoding: maps single-byte codes to glyph IDs. Rarely used in OpenType fonts, which rely on <c>cmap</c> for character mapping.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The encoding is largely vestigial in OpenType CFF fonts; the spec still requires the field to be present, but character mapping comes from <c>cmap</c>.</description></item>
/// <item><description>Two predefined encodings (Standard, Expert) are selected by offsets 0 and 1 in the Top DICT; other offsets parse a custom encoding.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF Encoding</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF Encoding</seealso>
public sealed record CffEncoding : IRecord<CffEncoding>
{
    /// <summary>Gets the code-to-glyph mapping. 256 entries; 0 means unmapped.</summary>
    /// <value>A 256-entry array indexed by character code; the value is the glyph ID or <c>0</c> when unmapped.</value>
    /// <seealso cref="GetGlyphId(byte)"/>
    public byte[] CodeToGid { get; init; } = new byte[256];

    /// <summary>Gets the supplement entries (code, SID) that override the primary mapping.</summary>
    /// <value>The ordered list of supplement mappings declared by the encoding.</value>
    /// <seealso cref="CodeToGid"/>
    public IReadOnlyList<(byte Code, ushort Sid)> Supplement { get; init; } = [];

    /// <summary>Gets the glyph ID for a single-byte code, or 0 if unmapped.</summary>
    /// <param name="code">The character code.</param>
    /// <returns>The glyph ID assigned to the code, or <c>0</c> when unmapped.</returns>
    /// <seealso cref="CodeToGid"/>
    public int GetGlyphId(byte code) => CodeToGid[code];

    /// <summary>Resolves the encoding selected by <paramref name="offset"/> in the Top DICT.</summary>
    /// <param name="source">The CFF table source.</param>
    /// <param name="offset">0 for Standard, 1 for Expert; other values parse a custom encoding.</param>
    /// <returns>The resolved <see cref="CffEncoding"/>.</returns>
    public static CffEncoding FromOffset(Source source, int offset) => offset switch
    {
        0 => Standard(),
        1 => Expert(),
        _ => source.ParseRecordAt<CffEncoding>(offset),
    };

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the encoding record.</param>
    /// <param name="context">Unused. The encoding is self-describing.</param>
    /// <returns>The parsed encoding.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 0 or 1.</exception>
    /// <exception cref="EndOfStreamException">The format-specific data extends past the end of the source.</exception>
    /// <remarks>Format 0 stores a list of codes to be assigned consecutively; format 1 stores ranges of codes. Both map codes to glyph IDs starting at 1.</remarks>
    /// <seealso cref="CffEncoding"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF Encoding</seealso>
    public static CffEncoding Parse(ref Cursor cursor, object? context)
    {
        byte formatAndSupplement = cursor.ReadUInt8();
        bool hasSupplement = (formatAndSupplement & 0x80) != 0;
        byte format = (byte)(formatAndSupplement & 0x7F);

        byte[] codeToGid = new byte[256];
        switch (format)
        {
            case 0:
                {
                    byte nCodes = cursor.ReadUInt8();
                    for (int i = 0; i < nCodes; i++)
                        codeToGid[cursor.ReadUInt8()] = (byte)(i + 1);
                    break;
                }

            case 1:
                {
                    byte nRanges = cursor.ReadUInt8();
                    int gid = 1;
                    for (int i = 0; i < nRanges; i++)
                    {
                        byte first = cursor.ReadUInt8();
                        byte nLeft = cursor.ReadUInt8();
                        for (int c = 0; c <= nLeft && first + c < 256; c++)
                            codeToGid[first + c] = (byte)gid;
                        gid += nLeft + 1;
                    }
                    break;
                }

            default:
                throw new InvalidDataException($"Invalid CFF encoding format {format}.");
        }

        List<(byte, ushort)> supplement = [];
        if (hasSupplement)
        {
            byte nSups = cursor.ReadUInt8();
            for (int i = 0; i < nSups; i++)
            {
                byte code = cursor.ReadUInt8();
                ushort sid = cursor.ReadUInt16();
                supplement.Add((code, sid));
            }
        }

        return new CffEncoding { CodeToGid = codeToGid, Supplement = supplement };
    }

    /// <summary>Builds the Standard predefined encoding.</summary>
    /// <returns>The Standard encoding, whose code-to-glyph mapping follows the Adobe StandardEncoding convention.</returns>
    public static CffEncoding Standard()
    {
        byte[] mapping = new byte[256];
        for (int i = 0; i < StandardCodeToSid.Length; i++)
        {
            byte code = StandardCodeToSid[i];
            if (code != 0) mapping[code] = (byte)i;
        }
        return new CffEncoding { CodeToGid = mapping };
    }

    /// <summary>Builds the Expert predefined encoding.</summary>
    /// <returns>The Expert encoding, whose code-to-glyph mapping follows the Adobe ExpertEncoding convention.</returns>
    public static CffEncoding Expert()
    {
        byte[] mapping = new byte[256];
        for (int i = 0; i < ExpertCodeToSid.Length; i++)
        {
            byte code = ExpertCodeToSid[i];
            if (code != 0) mapping[code] = (byte)i;
        }
        return new CffEncoding { CodeToGid = mapping };
    }

    private static readonly byte[] StandardCodeToSid =
    [
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32,
        33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
        49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64,
        65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80,
        81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110,
        0, 111, 112, 113, 114, 0, 115, 116, 117, 118, 119, 120, 121, 122, 0, 123,
        0, 124, 125, 126, 127, 128, 129, 130, 131, 0, 132, 133, 0, 134, 135, 136,
        137, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 138, 0, 139, 0, 0, 0, 0, 140, 141, 142, 143, 0, 0, 0, 0,
        0, 144, 0, 0, 0, 145, 0, 0, 146, 147, 148, 149, 0, 0, 0, 0,
    ];

    private static readonly byte[] ExpertCodeToSid =
    [
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32,
        33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
        49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 0, 0, 0, 0, 0, 0,
        0, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73,
        74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104,
        105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120,
        121, 122, 0, 123, 0, 124, 125, 126, 127, 128, 129, 130, 131, 132, 0, 133,
        0, 134, 135, 136, 137, 138, 0, 139, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 140, 141, 142, 143, 144, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        145, 0, 0, 146, 147, 148, 149, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    ];
}

// ═══════════════════════════════════════════════════════════════════════════
// FDSelect
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A CFF FDSelect table: maps glyph IDs to Font DICT indices in a CID-keyed font.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Two formats exist on disk (0 and 3); both expand to a flat per-glyph index array in this type.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF FDSelect</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF FDSelect</seealso>
public sealed record CffFdSelect : IRecord<CffFdSelect>
{
    /// <summary>Gets the FD indices, indexed by glyph ID.</summary>
    /// <value>The flat per-glyph Font DICT index array; length equals the font's glyph count.</value>
    /// <seealso cref="GetFdIndex(int)"/>
    public byte[] FdIndices { get; init; } = [];

    /// <summary>Gets the number of glyphs.</summary>
    /// <value>The size of the <see cref="FdIndices"/> array.</value>
    /// <seealso cref="FdIndices"/>
    public int Count => FdIndices.Length;

    /// <summary>Gets the Font DICT index for glyph <paramref name="glyphId"/>.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <returns>The Font DICT index assigned to the glyph.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is out of range.</exception>
    /// <seealso cref="FdIndices"/>
    public int GetFdIndex(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, FdIndices.Length);
        return FdIndices[glyphId];
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the FDSelect subtable.</param>
    /// <param name="context">A <see cref="CffFdSelectContext"/> carrying the font's glyph count.</param>
    /// <returns>The parsed FDSelect subtable with a flat per-glyph index array.</returns>
    /// <exception cref="InvalidDataException">The format is not 0 or 3.</exception>
    /// <exception cref="EndOfStreamException">The format-specific data extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Format 0 stores one byte per glyph; the parser reads them directly into the flat array.</description></item>
    /// <item><description>Format 3 stores ranges; the parser expands each range into per-glyph entries, capping the expansion at the font's glyph count.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF FDSelect</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CffFdSelectContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF FDSelect</seealso>
    public static CffFdSelect Parse(ref Cursor cursor, object? context)
    {
        CffFdSelectContext ctx = (CffFdSelectContext)context!;
        byte format = cursor.ReadUInt8();

        byte[] indices = new byte[ctx.NumGlyphs];

        switch (format)
        {
            case 0:
                for (int gid = 0; gid < ctx.NumGlyphs; gid++)
                    indices[gid] = cursor.ReadUInt8();
                break;

            case 3:
                {
                    ushort nRanges = cursor.ReadUInt16();
                    (ushort First, byte Fd)[] ranges = new (ushort, byte)[nRanges];
                    for (int i = 0; i < nRanges; i++)
                    {
                        byte first = cursor.ReadUInt8();
                        byte fd = cursor.ReadUInt8();
                        ranges[i] = (first, fd);
                    }
                    ushort sentinel = cursor.ReadUInt16();

                    for (int i = 0; i < nRanges; i++)
                    {
                        int start = ranges[i].First;
                        int end = i + 1 < nRanges ? ranges[i + 1].First : sentinel;
                        for (int gid = start; gid < end && gid < ctx.NumGlyphs; gid++)
                            indices[gid] = ranges[i].Fd;
                    }
                    break;
                }

            default:
                throw new InvalidDataException($"Invalid CFF FDSelect format {format}.");
        }

        return new CffFdSelect { FdIndices = indices };
    }
}

/// <summary>Context for parsing an FDSelect.</summary>
/// <param name="NumGlyphs">The number of glyphs, from the CharStrings INDEX.</param>
/// <remarks>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</remarks>
/// <seealso cref="CffFdSelect.Parse"/>
public record CffFdSelectContext(int NumGlyphs);

// ═══════════════════════════════════════════════════════════════════════════
// Type 2 interpreter
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Interprets a Type 2 charstring into a <see cref="CffGlyph"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The interpreter executes the operand stack machine, follows local and global subroutine calls, and materializes every moveto/lineto/curveto into a <see cref="CffContour"/>.</description></item>
/// <item><description>Hint operators are tracked so <c>hintmask</c> consumes the correct number of mask bytes, but are not otherwise modeled.</description></item>
/// <item><description>The type is designed to be instantiated once and reused across glyphs via <see cref="Run"/>; all runtime state is reset per call. Configure it with an object initializer rather than a constructor.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF CharString Format</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffTable"/>
/// <seealso cref="CffGlyph"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF CharString Format</seealso>
public sealed class CffInterpreter
{
    private const int StackLimit = 48;
    private const int SubrNestingLimit = 10;

    /// <summary>Gets the local subroutines, indexed by subroutine number plus bias.</summary>
    /// <value>The Private DICT-scoped local subroutine INDEX, or an empty list when the Private DICT declares no subroutines.</value>
    /// <seealso cref="GlobalSubrs"/>
    public IReadOnlyList<byte[]> LocalSubrs { get; init; } = [];

    /// <summary>Gets the global subroutines, indexed by subroutine number plus bias.</summary>
    /// <value>The CFF table-scoped global subroutine INDEX, shared across all fonts in the FontSet.</value>
    /// <seealso cref="LocalSubrs"/>
    public IReadOnlyList<byte[]> GlobalSubrs { get; init; } = [];

    /// <summary>Gets the default glyph width applied when a charstring omits an explicit width.</summary>
    /// <value>The <c>DefaultWidthX</c> from the Private DICT; the interpreter returns this as the glyph's width when no delta is declared.</value>
    /// <seealso cref="NominalWidthX"/>
    public double DefaultWidthX { get; init; }

    /// <summary>Gets the nominal glyph width added to a charstring's width delta.</summary>
    /// <value>The <c>NominalWidthX</c> from the Private DICT; the interpreter returns <c>NominalWidthX + delta</c> when a delta is declared.</value>
    /// <seealso cref="DefaultWidthX"/>
    public double NominalWidthX { get; init; }

    // ─── Runtime state, reset per Run ───

    private readonly double[] _stack = new double[StackLimit];
    private readonly double[] _transient = new double[32];
    private readonly List<CffContour> _contours = [];
    private int _stackDepth;
    private int _localBias;
    private int _globalBias;
    private List<CffSegment>? _segments;
    private CffPoint _current;
    private CffPoint _contourStart;
    private int _hintCount;
    private double _width;
    private bool _hasWidth;
    private bool _ended;

    /// <summary>Runs <paramref name="program"/> and returns the resulting glyph outline.</summary>
    /// <param name="program">The charstring byte code.</param>
    /// <returns>The interpreted glyph.</returns>
    /// <exception cref="InvalidDataException">The program contains an undefined operator, references a subroutine out of range, overflows or underflows the operand stack, or nests subroutines too deeply.</exception>
    /// <remarks>The interpreter state is reset at the start of each call, so a single instance can be reused for successive glyphs.</remarks>
    /// <seealso cref="CffGlyph"/>
    public CffGlyph Run(byte[] program)
    {
        _stackDepth = 0;
        _contours.Clear();
        _segments = null;
        _current = default;
        _contourStart = default;
        _hintCount = 0;
        _width = DefaultWidthX;
        _hasWidth = false;
        _ended = false;
        _localBias = ComputeBias(LocalSubrs.Count);
        _globalBias = ComputeBias(GlobalSubrs.Count);

        Execute(program, 0);
        CloseContour();

        return new CffGlyph
        {
            Contours = _contours,
            Width = _width,
            HasWidth = _hasWidth,
        };
    }

    private void Execute(byte[] program, int depth)
    {
        if (depth > SubrNestingLimit)
            throw new InvalidDataException($"CFF charstring subroutine nesting exceeded {SubrNestingLimit}.");

        int i = 0;
        while (i < program.Length && !_ended)
        {
            byte b0 = program[i++];

            if (b0 >= 32 || b0 == 28 || b0 == 255)
            {
                Push(ReadNumber(program, ref i, b0));
                continue;
            }

            switch (b0)
            {
                case 1:
                case 3:
                case 18:
                case 23:
                    CheckWidth();
                    _hintCount += _stackDepth / 2;
                    _stackDepth = 0;
                    break;

                case 19:
                case 20:
                    CheckWidth();
                    _hintCount += _stackDepth / 2;
                    _stackDepth = 0;
                    i += (_hintCount + 7) >> 3;
                    break;

                case 21: // rmoveto
                    CheckWidth();
                    _current = new(_current.X + Pop2(out double dx21), _current.Y + dx21);
                    MoveTo();
                    break;

                case 22: // hmoveto
                    CheckWidth();
                    _current = new(_current.X + Pop(), _current.Y);
                    MoveTo();
                    break;

                case 4: // vmoveto
                    CheckWidth();
                    _current = new(_current.X, _current.Y + Pop());
                    MoveTo();
                    break;

                case 5: // rlineto
                    while (_stackDepth >= 2)
                        LineTo(new(_current.X + Pop(), _current.Y + Pop()));
                    break;

                case 6:
                case 7: // hlineto, vlineto
                    {
                        bool horizontal = b0 == 6;
                        while (_stackDepth >= 1)
                        {
                            double v = Pop();
                            CffPoint next = horizontal
                                ? new(_current.X + v, _current.Y)
                                : new(_current.X, _current.Y + v);
                            LineTo(next);
                            horizontal = !horizontal;
                        }
                        break;
                    }

                case 8: // rrcurveto
                    while (_stackDepth >= 6) CurveRelative();
                    break;

                case 24: // rcurveline
                    while (_stackDepth >= 8) CurveRelative();
                    if (_stackDepth >= 2)
                        LineTo(new(_current.X + Pop(), _current.Y + Pop()));
                    break;

                case 25: // rlinecurve
                    while (_stackDepth >= 8)
                        LineTo(new(_current.X + Pop(), _current.Y + Pop()));
                    if (_stackDepth >= 6) CurveRelative();
                    break;

                case 26: VvCurveTo(); break;
                case 27: HhCurveTo(); break;
                case 30: case 31: AlternatingCurveTo(horizontalFirst: b0 == 31); break;

                case 10: // callsubr
                    {
                        int index = (int)Pop() + _localBias;
                        if ((uint)index >= (uint)LocalSubrs.Count)
                            throw new InvalidDataException($"CFF local subroutine index {index} is out of range.");
                        Execute(LocalSubrs[index], depth + 1);
                        break;
                    }

                case 29: // callgsubr
                    {
                        int index = (int)Pop() + _globalBias;
                        if ((uint)index >= (uint)GlobalSubrs.Count)
                            throw new InvalidDataException($"CFF global subroutine index {index} is out of range.");
                        Execute(GlobalSubrs[index], depth + 1);
                        break;
                    }

                case 11: return;
                case 14: CheckWidth(); _ended = true; break;

                case 12:
                    {
                        byte b1 = program[i++];
                        switch (b1)
                        {
                            case 35: Flex(); break;
                            case 34: HFlex(); break;
                            case 36: HFlex1(); break;
                            case 37: Flex1(); break;
                            default: ExecuteArithmetic(b1); break;
                        }
                        break;
                    }

                default:
                    throw new InvalidDataException($"Undefined Type 2 charstring operator {b0}.");
            }
        }
    }

    private void CurveRelative()
    {
        double dx1 = Pop(), dy1 = Pop();
        double dx2 = Pop(), dy2 = Pop();
        double dx3 = Pop(), dy3 = Pop();
        CffPoint c1 = new(_current.X + dx1, _current.Y + dy1);
        CffPoint c2 = new(c1.X + dx2, c1.Y + dy2);
        CffPoint end = new(c2.X + dx3, c2.Y + dy3);
        AddSegment(new CffSegment { Control1 = c1, Control2 = c2, End = end, IsCubic = true });
        _current = end;
    }

    private void VvCurveTo()
    {
        double dx1 = _stackDepth % 4 == 0 ? Pop() : 0;
        while (_stackDepth >= 4)
        {
            double c1y = Pop(), c2x = Pop(), c2y = Pop(), endy = Pop();
            CffPoint c1 = new(_current.X + dx1, _current.Y + c1y);
            CffPoint c2 = new(c1.X + c2x, c1.Y + c2y);
            CffPoint end = new(c2.X, c2.Y + endy);
            AddSegment(new CffSegment { Control1 = c1, Control2 = c2, End = end, IsCubic = true });
            _current = end;
            dx1 = 0;
        }
    }

    private void HhCurveTo()
    {
        double dy1 = _stackDepth % 4 == 0 ? Pop() : 0;
        while (_stackDepth >= 4)
        {
            double c1x = Pop(), c2x = Pop(), c2y = Pop(), endx = Pop();
            CffPoint c1 = new(_current.X + c1x, _current.Y + dy1);
            CffPoint c2 = new(c1.X + c2x, c1.Y + c2y);
            CffPoint end = new(c2.X + endx, c2.Y);
            AddSegment(new CffSegment { Control1 = c1, Control2 = c2, End = end, IsCubic = true });
            _current = end;
            dy1 = 0;
        }
    }

    private void AlternatingCurveTo(bool horizontalFirst)
    {
        bool horizontal = horizontalFirst;
        while (_stackDepth >= 4)
        {
            CffPoint c1, c2, end;

            if (horizontal)
            {
                double dx1 = Pop(), dx2 = Pop(), dy2 = Pop(), dy3 = Pop();
                double dx3 = _stackDepth == 1 ? Pop() : 0;
                c1 = new(_current.X + dx1, _current.Y);
                c2 = new(c1.X + dx2, c1.Y + dy2);
                end = new(c2.X + dx3, c2.Y + dy3);
            }
            else
            {
                double dy1 = Pop(), dx2 = Pop(), dy2 = Pop(), dx3 = Pop();
                double dy3 = _stackDepth == 1 ? Pop() : 0;
                c1 = new(_current.X, _current.Y + dy1);
                c2 = new(c1.X + dx2, c1.Y + dy2);
                end = new(c2.X + dx3, c2.Y + dy3);
            }

            AddSegment(new CffSegment { Control1 = c1, Control2 = c2, End = end, IsCubic = true });
            _current = end;
            horizontal = !horizontal;
        }
    }

    private void Flex()
    {
        double dx1 = Pop(), dy1 = Pop(), dx2 = Pop(), dy2 = Pop();
        double dx3 = Pop(), dy3 = Pop(), dx4 = Pop(), dy4 = Pop();
        double dx5 = Pop(), dy5 = Pop(), dx6 = Pop(), dy6 = Pop();
        _ = Pop();

        CffPoint c1 = new(_current.X + dx1, _current.Y + dy1);
        CffPoint c2 = new(c1.X + dx2, c1.Y + dy2);
        CffPoint end1 = new(c2.X + dx3, c2.Y + dy3);
        AddSegment(new CffSegment { Control1 = c1, Control2 = c2, End = end1, IsCubic = true });

        CffPoint c3 = new(end1.X + dx4, end1.Y + dy4);
        CffPoint c4 = new(c3.X + dx5, c3.Y + dy5);
        CffPoint end2 = new(c4.X + dx6, c4.Y + dy6);
        AddSegment(new CffSegment { Control1 = c3, Control2 = c4, End = end2, IsCubic = true });
        _current = end2;
    }

    private void HFlex()
    {
        double dx1 = Pop(), dx2 = Pop(), dy2 = Pop();
        double dx3 = Pop(), dx4 = Pop(), dx5 = Pop(), dx6 = Pop();

        CffPoint c1 = new(_current.X + dx1, _current.Y);
        CffPoint c2 = new(c1.X + dx2, c1.Y + dy2);
        CffPoint end1 = new(c2.X + dx3, c1.Y);
        AddSegment(new CffSegment { Control1 = c1, Control2 = c2, End = end1, IsCubic = true });

        CffPoint c3 = new(end1.X + dx4, end1.Y);
        CffPoint c4 = new(c3.X + dx5, end1.Y);
        CffPoint end2 = new(c4.X + dx6, end1.Y);
        AddSegment(new CffSegment { Control1 = c3, Control2 = c4, End = end2, IsCubic = true });
        _current = end2;
    }

    private void HFlex1()
    {
        double dx1 = Pop(), dy1 = Pop(), dx2 = Pop(), dy2 = Pop();
        double dx3 = Pop(), dx4 = Pop(), dx5 = Pop(), dy5 = Pop(), dx6 = Pop();

        CffPoint c1 = new(_current.X + dx1, _current.Y + dy1);
        CffPoint c2 = new(c1.X + dx2, c1.Y + dy2);
        CffPoint end1 = new(c2.X + dx3, c2.Y);
        AddSegment(new CffSegment { Control1 = c1, Control2 = c2, End = end1, IsCubic = true });

        CffPoint c3 = new(end1.X + dx4, end1.Y);
        CffPoint c4 = new(c3.X + dx5, c3.Y + dy5);
        CffPoint end2 = new(c4.X + dx6, c4.Y);
        AddSegment(new CffSegment { Control1 = c3, Control2 = c4, End = end2, IsCubic = true });
        _current = end2;
    }

    private void Flex1()
    {
        double dx1 = Pop(), dy1 = Pop(), dx2 = Pop(), dy2 = Pop();
        double dx3 = Pop(), dy3 = Pop(), dx4 = Pop(), dy4 = Pop();
        double dx5 = Pop(), dy5 = Pop();
        double d6 = Pop();

        double sumDx = dx1 + dx2 + dx3 + dx4 + dx5;
        double sumDy = dy1 + dy2 + dy3 + dy4 + dy5;
        double dx6 = Math.Abs(sumDx) > Math.Abs(sumDy) ? d6 : 0;
        double dy6 = Math.Abs(sumDx) > Math.Abs(sumDy) ? 0 : d6;

        CffPoint c1 = new(_current.X + dx1, _current.Y + dy1);
        CffPoint c2 = new(c1.X + dx2, c1.Y + dy2);
        CffPoint end1 = new(c2.X + dx3, c2.Y + dy3);
        AddSegment(new CffSegment { Control1 = c1, Control2 = c2, End = end1, IsCubic = true });

        CffPoint c3 = new(end1.X + dx4, end1.Y + dy4);
        CffPoint c4 = new(c3.X + dx5, c3.Y + dy5);
        CffPoint end2 = new(c4.X + dx6, c4.Y + dy6);
        AddSegment(new CffSegment { Control1 = c3, Control2 = c4, End = end2, IsCubic = true });
        _current = end2;
    }

    private void ExecuteArithmetic(byte op)
    {
        switch (op)
        {
            case 3: Push(Pop() != 0 && Pop() != 0 ? 1 : 0); break;
            case 4: Push(Pop() != 0 || Pop() != 0 ? 1 : 0); break;
            case 5: Push(Pop() == 0 ? 1 : 0); break;
            case 9: Push(Math.Abs(Pop())); break;
            case 10: { double b = Pop(), a = Pop(); Push(a + b); break; }
            case 11: { double b = Pop(), a = Pop(); Push(a - b); break; }
            case 12: { double b = Pop(), a = Pop(); Push(a / b); break; }
            case 14: Push(-Pop()); break;
            case 15: { double b = Pop(), a = Pop(); Push(a == b ? 1 : 0); break; }
            case 18: Pop(); break;
            case 20: { int j = (int)Pop(); double v = Pop(); Put(j, v); break; }
            case 21: { int j = (int)Pop(); Push(Get(j)); break; }
            case 22: { double v2 = Pop(), v1 = Pop(), s2 = Pop(), s1 = Pop(); Push(v1 <= v2 ? s1 : s2); break; }
            case 23: Push(0.5); break;
            case 24: { double b = Pop(), a = Pop(); Push(a * b); break; }
            case 26: Push(Math.Sqrt(Pop())); break;
            case 27: { double a = Pop(); Push(a); Push(a); break; }
            case 28: { double b = Pop(), a = Pop(); Push(b); Push(a); break; }
            case 29: { int n = (int)Pop(); Push(Get(_stackDepth - n)); break; }
            case 30: Roll(); break;
            default: throw new InvalidDataException($"Undefined Type 2 escape operator 12 {op}.");
        }
    }

    private void Put(int j, double v)
    {
        if ((uint)j >= (uint)_transient.Length) return;
        _transient[j] = v;
    }

    private double Get(int j) => (uint)j >= (uint)_transient.Length ? 0 : _transient[j];

    private void Roll()
    {
        int n = (int)Pop();
        int j = (int)Pop();
        if (n <= 0 || n > _stackDepth) return;
        j %= n;
        if (j < 0) j += n;
        if (j == 0) return;

        int start = _stackDepth - n;
        double[] tmp = new double[n];
        for (int k = 0; k < n; k++) tmp[k] = _stack[start + k];
        for (int k = 0; k < n; k++) _stack[start + k] = tmp[(k + j) % n];
    }

    private void MoveTo()
    {
        CloseContour();
        _contourStart = _current;
        _segments = [];
    }

    private void LineTo(CffPoint end)
    {
        EnsureContour();
        _segments!.Add(new CffSegment { End = end, IsCubic = false });
        _current = end;
    }

    private void AddSegment(CffSegment segment)
    {
        EnsureContour();
        _segments!.Add(segment);
    }

    private void EnsureContour()
    {
        if (_segments is null)
        {
            _contourStart = _current;
            _segments = [];
        }
    }

    private void CloseContour()
    {
        if (_segments is null) return;
        if (_segments.Count > 0)
            _contours.Add(new CffContour { Start = _contourStart, Segments = _segments });
        _segments = null;
    }

    private void Push(double v)
    {
        if (_stackDepth >= StackLimit)
            throw new InvalidDataException("CFF operand stack overflowed.");
        _stack[_stackDepth++] = v;
    }

    private double Pop()
    {
        if (_stackDepth == 0)
            throw new InvalidDataException("CFF operand stack underflowed.");
        return _stack[--_stackDepth];
    }

    private double Pop2(out double b)
    {
        b = Pop();
        return Pop();
    }

    private static double ReadNumber(byte[] program, ref int i, byte b0)
    {
        if (b0 == 28)
        {
            short v = (short)((program[i] << 8) | program[i + 1]);
            i += 2;
            return v;
        }
        if (b0 == 255)
        {
            int raw = (program[i] << 24) | (program[i + 1] << 16)
                    | (program[i + 2] << 8) | program[i + 3];
            i += 4;
            return raw / 65536.0;
        }
        if (b0 is >= 32 and <= 246) return b0 - 139;
        if (b0 is >= 247 and <= 250)
        {
            double v = (b0 - 247) * 256 + program[i] + 108;
            i += 1;
            return v;
        }
        if (b0 is >= 251 and <= 254)
        {
            double v = -(b0 - 251) * 256 - program[i] - 108;
            i += 1;
            return v;
        }
        throw new InvalidDataException($"Invalid Type 2 number byte 0x{b0:X2}.");
    }

    private void CheckWidth()
    {
        if (_hasWidth || _stackDepth == 0) return;

        if ((_stackDepth & 1) == 1)
        {
            double w = _stack[0];
            Array.Copy(_stack, 1, _stack, 0, _stackDepth - 1);
            _stackDepth--;
            _width = NominalWidthX + w;
            _hasWidth = true;
        }
    }

    private static int ComputeBias(int subrCount) => subrCount switch
    {
        < 1240 => 107,
        < 33900 => 1131,
        _ => 32768,
    };
}

// ═══════════════════════════════════════════════════════════════════════════
// CFF table
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The <c>CFF </c> table: Compact Font Format outline data for PostScript-flavored OpenType fonts. A self-contained container holding its own string, dictionary, charset, encoding, subroutine, and charstring structures.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The CFF table is present when the font's sfnt version is <c>'OTTO'</c>. The font's <c>maxp.numGlyphs</c> must equal the number of entries in the CharStrings INDEX.</description></item>
/// <item><description>Every glyph's outline is materialized during parsing by running each charstring through the Type 2 interpreter. The result is a <see cref="CffGlyph"/> per glyph, exposed through <see cref="Glyphs"/>. The raw programs are also retained in <see cref="CharStrings"/>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffHeader"/>
/// <seealso cref="CffTopDict"/>
/// <seealso cref="CffPrivateDict"/>
/// <seealso cref="CffFontDict"/>
/// <seealso cref="CffGlyph"/>
/// <seealso cref="CffInterpreter"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF table</seealso>
public sealed record CffTable : IFontTable<CffTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF table</seealso>
    public static Tag Tag => "CFF ";

    /// <summary>Gets the CFF header.</summary>
    /// <value>The raw <see cref="CffHeader"/> struct read at the start of the table.</value>
    /// <seealso cref="CffHeader"/>
    public CffHeader Header { get; init; }

    /// <summary>Gets the font name from the Name INDEX.</summary>
    /// <value>The single entry from the Name INDEX, decoded as ASCII; the spec requires exactly one name in an OpenType-embedded CFF.</value>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the parsed Top DICT.</summary>
    /// <value>The <see cref="CffTopDict"/> that provides the offsets to the CharStrings INDEX, Charset, Encoding, Private DICT, and (for CID-keyed fonts) FDArray and FDSelect.</value>
    /// <seealso cref="CffTopDict"/>
    public CffTopDict TopDict { get; init; } = null!;

    /// <summary>Gets the custom strings from the String INDEX.</summary>
    /// <value>The ordered list of custom strings, indexed relative to <see cref="CffStandardStrings.Count"/> for SID resolution.</value>
    /// <seealso cref="GetGlyphName(int)"/>
    public IReadOnlyList<string> Strings { get; init; } = [];

    /// <summary>Gets the charset mapping glyph IDs to SIDs.</summary>
    /// <value>The resolved <see cref="CffCharset"/> for the font.</value>
    /// <seealso cref="GetGlyphName(int)"/>
    public CffCharset Charset { get; init; } = null!;

    /// <summary>Gets the encoding, or <c>null</c> when absent.</summary>
    /// <value>The resolved <see cref="CffEncoding"/>, or <c>null</c> when the Top DICT declares a negative encoding offset.</value>
    public CffEncoding? Encoding { get; init; }

    /// <summary>Gets the raw charstrings, one per glyph.</summary>
    /// <value>The ordered list of <see cref="CffCharString"/> entries; each carries the raw program bytes and the Font DICT index used when interpreting it.</value>
    /// <seealso cref="Glyphs"/>
    public IReadOnlyList<CffCharString> CharStrings { get; init; } = [];

    /// <summary>Gets the interpreted glyph outlines, one per glyph.</summary>
    /// <value>The ordered list of <see cref="CffGlyph"/> entries materialized at parse time.</value>
    /// <seealso cref="CharStrings"/>
    public IReadOnlyList<CffGlyph> Glyphs { get; init; } = [];

    /// <summary>Gets the parsed Private DICT for a non-CID font, or <c>null</c>.</summary>
    /// <value>The <see cref="CffPrivateDict"/> for a non-CID-keyed font, or <c>null</c> for a CID-keyed font (which uses per-Font-DICT Private DICTs instead).</value>
    /// <seealso cref="FontDicts"/>
    public CffPrivateDict? PrivateDict { get; init; }

    /// <summary>Gets the local subroutines, indexed by Font DICT index for CID fonts.</summary>
    /// <value>For a non-CID font, a single-element list; for a CID-keyed font, one entry per Font DICT.</value>
    /// <seealso cref="GlobalSubrs"/>
    public IReadOnlyList<IReadOnlyList<byte[]>> LocalSubrs { get; init; } = [];

    /// <summary>Gets the global subroutines, shared across all fonts in the FontSet.</summary>
    /// <value>The materialized Global Subrs INDEX, or an empty list when the INDEX is empty.</value>
    public IReadOnlyList<byte[]> GlobalSubrs { get; init; } = [];

    /// <summary>Gets the Font DICTs for a CID-keyed font, or <c>null</c>.</summary>
    /// <value>The ordered list of <see cref="CffFontDict"/> entries, or <c>null</c> for a non-CID-keyed font.</value>
    /// <seealso cref="IsCidFont"/>
    public IReadOnlyList<CffFontDict>? FontDicts { get; init; }

    /// <summary>Gets the FDSelect for a CID-keyed font, or <c>null</c>.</summary>
    /// <value>The <see cref="CffFdSelect"/> that maps glyph IDs to Font DICT indices, or <c>null</c> for a non-CID-keyed font.</value>
    public CffFdSelect? FdSelect { get; init; }

    /// <summary>Gets the number of glyphs.</summary>
    /// <value>The size of the <see cref="CharStrings"/> list; also the size of <see cref="Glyphs"/>.</value>
    /// <seealso cref="CharStrings"/>
    public int GlyphCount => CharStrings.Count;

    /// <summary>True when this is a CID-keyed font.</summary>
    /// <value><see langword="true"/> when the Top DICT declares the ROS operator.</value>
    /// <seealso cref="FontDicts"/>
    public bool IsCidFont => TopDict.IsCIDFont;

    /// <summary>Returns the PostScript name of glyph <paramref name="glyphId"/>, resolving the SID through the standard strings and the String INDEX.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <returns>The glyph's name, or an empty string when the SID cannot be resolved.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is out of range.</exception>
    /// <remarks>Standard SIDs below <see cref="CffStandardStrings.Count"/> resolve through <see cref="CffStandardStrings.Get"/>; larger SIDs index into <see cref="Strings"/> after subtracting the standard count.</remarks>
    /// <example>
    /// <code>
    /// string name = cff.GetGlyphName(glyphId);
    /// </code>
    /// </example>
    /// <seealso cref="Charset"/>
    /// <seealso cref="Strings"/>
    public string GetGlyphName(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, GlyphCount);

        ushort sid = Charset.GetSid(glyphId);
        if (sid < CffStandardStrings.Count)
            return CffStandardStrings.Get(sid);
        int index = sid - CffStandardStrings.Count;
        return index < Strings.Count ? Strings[index] : string.Empty;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the CFF table.</param>
    /// <param name="context">Unused. The table resolves its subtables through its own source.</param>
    /// <returns>The parsed CFF table with its glyphs materialized.</returns>
    /// <exception cref="InvalidDataException">The major version is not 1, the header size is less than 4, the Name or Top DICT INDEX does not contain exactly one entry, or the CharstringType is not 2.</exception>
    /// <exception cref="EndOfStreamException">The header or any referenced structure extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The Name INDEX, Top DICT INDEX, String INDEX, and Global Subrs INDEX are read in the order the specification defines.</description></item>
    /// <item><description>For a CID-keyed font, each Font DICT's Private DICT and Local Subrs INDEX are resolved eagerly, so the FontDicts and LocalSubrs arrays are fully materialized.</description></item>
    /// <item><description>Every glyph is interpreted eagerly during parse; the raw programs remain accessible through <see cref="CharStrings"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CffHeader"/>
    /// <seealso cref="CffTopDict"/>
    /// <seealso cref="CffInterpreter"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF table</seealso>
    public static CffTable Parse(ref Cursor cursor, object? context)
    {
        Source source = cursor.Source;

        CffHeader header = cursor.ReadStruct<CffHeader>();
        if (header.Major != 1)
            throw new InvalidDataException(
                $"'CFF '.major is {header.Major}, expected 1 (CFF2 is not supported).");
        if (header.HdrSize < 4)
            throw new InvalidDataException($"'CFF '.hdrSize is {header.HdrSize}, expected at least 4.");

        if (header.HdrSize > 4)
            cursor.Position = header.HdrSize;

        CffIndex nameIndex = CffIndex.Parse(ref cursor);
        if (nameIndex.Count != 1)
            throw new InvalidDataException(
                $"'CFF ' Name INDEX has {nameIndex.Count} entries, expected 1 in an OpenType font.");
        string name = System.Text.Encoding.ASCII.GetString(nameIndex.Data);

        CffIndex topDictIndex = CffIndex.Parse(ref cursor);
        if (topDictIndex.Count != 1)
            throw new InvalidDataException(
                $"'CFF ' Top DICT INDEX has {topDictIndex.Count} entries, expected 1.");
        CffTopDict topDict = source.ParseRecordAt<CffTopDict>(
            topDictIndex.DataStart,
            new CffDictContext(topDictIndex.Data.Length));

        if (topDict.CharstringType != 2)
            throw new InvalidDataException(
                $"'CFF ' CharstringType is {topDict.CharstringType}, expected 2.");

        CffIndex stringIndex = CffIndex.Parse(ref cursor);
        string[] strings = new string[stringIndex.Count];
        for (int i = 0; i < stringIndex.Count; i++)
        {
            (int start, int length) = stringIndex.GetRange(i);
            strings[i] = System.Text.Encoding.ASCII.GetString(stringIndex.Data, start, length);
        }

        CffIndex globalSubrIndex = CffIndex.Parse(ref cursor);
        byte[][] globalSubrs = Materialize(globalSubrIndex);

        CffIndex charStringsIndex = source.ParseRecordAt<CffIndex>(topDict.CharStringsOffset);
        int numGlyphs = charStringsIndex.Count;

        CffCharset charset = CffCharset.FromOffset(source, topDict.CharsetOffset, numGlyphs);

        CffEncoding? encoding = topDict.EncodingOffset >= 0
            ? CffEncoding.FromOffset(source, topDict.EncodingOffset)
            : null;

        CffPrivateDict? privateDict = null;
        List<IReadOnlyList<byte[]>> localSubrs = [];
        List<CffFontDict>? fontDicts = null;
        CffFdSelect? fdSelect = null;

        if (topDict.IsCIDFont)
        {
            CffIndex fdArrayIndex = source.ParseRecordAt<CffIndex>(topDict.FDArrayOffset);
            fontDicts = new List<CffFontDict>(fdArrayIndex.Count);
            for (int i = 0; i < fdArrayIndex.Count; i++)
            {
                (int rangeStart, int rangeLength) = fdArrayIndex.GetRange(i);
                CffFontDict fdDict = source.ParseRecordAt<CffFontDict>(
                    fdArrayIndex.DataStart + rangeStart,
                    new CffFontDictContext(rangeLength, source));
                fontDicts.Add(fdDict);

                IReadOnlyList<byte[]> subrs = fdDict.PrivateDict is { } pd
                    ? ResolveLocalSubrs(source, fdDict.Private?.Offset ?? 0, pd)
                    : [];
                localSubrs.Add(subrs);
            }

            fdSelect = topDict.FDSelectOffset > 0
                ? source.ParseRecordAt<CffFdSelect>(
                    topDict.FDSelectOffset, new CffFdSelectContext(numGlyphs))
                : null;
        }
        else if (topDict.Private is { } priv)
        {
            privateDict = source.ParseRecordAt<CffPrivateDict>(
                priv.Offset, new CffDictContext(priv.Size));
            localSubrs.Add(ResolveLocalSubrs(source, priv.Offset, privateDict));
        }

        CffCharString[] charStrings = new CffCharString[numGlyphs];
        CffGlyph[] glyphs = new CffGlyph[numGlyphs];

        for (int gid = 0; gid < numGlyphs; gid++)
        {
            int fdIndex = fdSelect?.GetFdIndex(gid) ?? 0;

            byte[] program = charStringsIndex.GetBytes(gid);
            charStrings[gid] = new() { Program = program, FdIndex = fdIndex };

            IReadOnlyList<byte[]> glyphLocalSubrs = fdIndex < localSubrs.Count
                ? localSubrs[fdIndex]
                : [];

            CffPrivateDict? glyphPrivate = topDict.IsCIDFont
                ? (fontDicts is not null && fdIndex < fontDicts.Count ? fontDicts[fdIndex].PrivateDict : null)
                : privateDict;

            CffInterpreter interpreter = new()
            {
                LocalSubrs = glyphLocalSubrs,
                GlobalSubrs = globalSubrs,
                DefaultWidthX = glyphPrivate?.DefaultWidthX ?? 0,
                NominalWidthX = glyphPrivate?.NominalWidthX ?? 0,
            };
            glyphs[gid] = interpreter.Run(program);
        }

        return new()
        {
            Header = header,
            Name = name,
            TopDict = topDict,
            Strings = strings,
            Charset = charset,
            Encoding = encoding,
            CharStrings = charStrings,
            Glyphs = glyphs,
            PrivateDict = privateDict,
            LocalSubrs = localSubrs,
            GlobalSubrs = globalSubrs,
            FontDicts = fontDicts,
            FdSelect = fdSelect,
        };
    }

    /// <summary>Resolves the local Subrs INDEX for a Private DICT.</summary>
    /// <param name="source">The CFF table source.</param>
    /// <param name="privateOffset">The absolute offset of the Private DICT within the CFF table.</param>
    /// <param name="privateDict">The parsed Private DICT whose Subrs offset is used.</param>
    /// <returns>The materialized subroutine byte arrays, or an empty list when none are declared.</returns>
    /// <remarks>The Subrs offset is relative to the Private DICT start, so the method adds <paramref name="privateOffset"/> before resolving.</remarks>
    /// <seealso cref="CffPrivateDict.SubrsOffset"/>
    public static IReadOnlyList<byte[]> ResolveLocalSubrs(
        Source source, int privateOffset, CffPrivateDict privateDict)
    {
        int subrsRelative = privateDict.SubrsOffset;
        if (subrsRelative <= 0) return [];
        CffIndex index = source.ParseRecordAt<CffIndex>(privateOffset + subrsRelative);
        return Materialize(index);
    }

    /// <summary>Materializes every object in <paramref name="index"/> as a byte array.</summary>
    /// <param name="index">The INDEX to materialize.</param>
    /// <returns>An array with one byte array per object in the INDEX.</returns>
    /// <remarks>Each entry is an independent copy of the corresponding object's bytes.</remarks>
    /// <seealso cref="CffIndex.GetBytes(int)"/>
    public static byte[][] Materialize(CffIndex index)
    {
        byte[][] result = new byte[index.Count][];
        for (int i = 0; i < index.Count; i++)
            result[i] = index.GetBytes(i);
        return result;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Standard strings
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The 391 standard strings defined by the CFF specification (Adobe TN #5176, Appendix A).</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>SIDs below <see cref="Count"/> are resolved through this table; SIDs at or above it index into the CFF table's own String INDEX.</description></item>
/// <item><description>The strings are the exact PostScript glyph names and other identifiers the specification reserves.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffTable.GetGlyphName(int)"/>
public static class CffStandardStrings
{
    /// <summary>The number of standard strings.</summary>
    /// <value>The constant <c>391</c>; SIDs 0–390 are standard, and 391 and above index into the String INDEX.</value>
    /// <seealso cref="Strings"/>
    public const int Count = 391;

    /// <summary>Resolves a standard SID to its string.</summary>
    /// <param name="sid">The string identifier. Must be in <c>[0, Count)</c>.</param>
    /// <returns>The string assigned to the SID.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sid"/> is outside the standard range.</exception>
    /// <seealso cref="Strings"/>
    public static string Get(int sid) => sid >= 0 && sid < Count
        ? Strings[sid]
        : throw new ArgumentOutOfRangeException(nameof(sid), sid, $"SID {sid} is not a standard string.");

    /// <summary>The standard strings, indexed by SID.</summary>
    /// <value>The 391 strings; entries 0–390 are reserved by the specification for standard PostScript names.</value>
    /// <seealso cref="Get(int)"/>
    public static readonly string[] Strings =
    [
        ".notdef", "space", "exclam", "quotedbl", "numbersign", "dollar", "percent",
        "ampersand", "quoteright", "parenleft", "parenright", "asterisk", "plus",
        "comma", "hyphen", "period", "slash", "zero", "one", "two", "three",
        "four", "five", "six", "seven", "eight", "nine", "colon", "semicolon",
        "less", "equal", "greater",
        "question", "at", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K",
        "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y",
        "Z", "bracketleft", "backslash", "bracketright", "asciicircum", "underscore",
        "quoteleft", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l",
        "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
        "braceleft", "bar", "braceright", "asciitilde",
        "exclamdown", "cent", "sterling", "fraction", "yen", "florin", "section",
        "currency", "quotesingle", "quotedblleft", "guillemotleft", "guilsinglleft",
        "guilsinglright", "fi", "fl", "endash", "dagger", "daggerdbl",
        "periodcentered", "paragraph", "bullet", "quotesinglbase", "quotedblbase",
        "quotedblright", "guillemotright", "ellipsis", "perthousand",
        "questiondown", "grave", "acute", "circumflex", "tilde",
        "macron", "breve", "dotaccent", "dieresis", "ring", "cedilla",
        "hungarumlaut", "ogonek", "caron", "emdash", "AE", "ordfeminine",
        "Lslash", "Oslash", "OE", "ordmasculine", "ae", "dotlessi", "lslash",
        "oslash", "oe", "germandbls", "onesuperior", "logicalnot", "mu",
        "trademark", "Eth", "onehalf", "plusminus", "Thorn", "onequarter",
        "divide",
        "brokenbar", "degree", "thorn", "threequarters", "twosuperior",
        "registered", "minus", "eth", "multiply", "threesuperior", "copyright",
        "Aacute", "Acircumflex", "Adieresis", "Agrave", "Aring", "Atilde",
        "Ccedilla", "Eacute", "Ecircumflex", "Edieresis", "Egrave", "Iacute",
        "Icircumflex", "Idieresis", "Igrave", "Ntilde", "Oacute", "Ocircumflex",
        "Odieresis", "Ograve", "Otilde",
        "Scaron", "Uacute", "Ucircumflex", "Udieresis", "Ugrave", "Yacute",
        "Ydieresis", "Zcaron", "aacute", "acircumflex", "adieresis", "agrave",
        "aring", "atilde", "ccedilla", "eacute", "ecircumflex", "edieresis",
        "egrave", "iacute", "icircumflex", "idieresis", "igrave", "ntilde",
        "oacute", "ocircumflex", "odieresis", "ograve", "otilde", "scaron",
        "uacute", "ucircumflex", "udieresis", "ugrave", "yacute", "ydieresis",
        "zcaron",
        "exclamsmall", "Hungarumlautsmall", "dollaroldstyle", "dollarsuperior",
        "ampersandsmall", "Acutesmall", "parenleftsuperior", "parenrightsuperior",
        "twodotenleader", "onedotenleader", "zerooldstyle", "oneoldstyle",
        "twooldstyle", "threeoldstyle", "fouroldstyle", "fiveoldstyle",
        "sixoldstyle", "sevenoldstyle", "eightoldstyle", "nineoldstyle",
        "commasuperior", "threequartersemdash", "periodsuperior",
        "questionsmall", "asuperior", "bsuperior", "centsuperior", "dsuperior",
        "esuperior", "isuperior", "lsuperior", "msuperior", "nsuperior",
        "osuperior", "rsuperior", "ssuperior", "tsuperior", "ff", "ffi", "ffl",
        "parenleftinferior", "parenrightinferior", "Circumflexsmall",
        "hyphensuperior", "Gravesmall", "Asmall", "Bsmall", "Csmall", "Dsmall",
        "Esmall", "Fsmall", "Gsmall", "Hsmall", "Ismall", "Jsmall", "Ksmall",
        "Lsmall", "Msmall", "Nsmall", "Osmall", "Psmall", "Qsmall", "Rsmall",
        "Ssmall", "Tsmall", "Usmall", "Vsmall", "Wsmall", "Xsmall", "Ysmall",
        "Zsmall", "colonmonetary", "onefitted", "rupiah", "Tildesmall",
        "exclamdownsmall", "centoldstyle", "Lslashsmall", "Scaronsmall",
        "Zcaronsmall", "Dieresissmall", "Brevesmall", "Caronsmall",
        "Dotaccentsmall", "Macronsmall", "figuredash", "hypheninferior",
        "Ogoneksmall", "Ringsmall", "Cedillasmall", "questiondownsmall",
        "oneeighth", "threeeighths", "fiveeighths", "seveneighths", "onethird",
        "twothirds", "zerosuperior", "foursuperior", "fivesuperior",
        "sixsuperior", "sevensuperior", "eightsuperior", "ninesuperior",
        "zeroinferior", "oneinferior", "twoinferior", "threeinferior",
        "fourinferior", "fiveinferior", "sixinferior", "seveninferior",
        "eightinferior", "nineinferior", "centinferior", "dollarinferior",
        "periodinferior", "commainferior", "Agravesmall", "Aacutesmall",
        "Acircumflexsmall", "Atildesmall", "Adieresissmall", "Aringsmall",
        "AEsmall", "Ccedillasmall", "Egravesmall", "Eacutesmall",
        "Ecircumflexsmall", "Edieresissmall", "Igravesmall", "Iacutesmall",
        "Icircumflexsmall", "Idieresissmall", "Ethsmall", "Ntildesmall",
        "Ogravesmall", "Oacutesmall", "Ocircumflexsmall", "Otildesmall",
        "Odieresissmall", "OEsmall", "Oslashsmall", "Ugravesmall",
        "Uacutesmall", "Ucircumflexsmall", "Udieresissmall", "Yacutesmall",
        "Thornsmall", "Ydieresissmall", "001.000", "001.001", "001.002",
        "001.003", "Black", "Bold", "Book", "Light", "Medium", "Regular",
        "Roman", "Semibold",
    ];
}

// ═══════════════════════════════════════════════════════════════════════════
// Type 2 operator enum
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Type 2 charstring operators as defined by Adobe TN #5177. The numeric value is the operator's byte code; two-byte operators use a value in the 0x0C00 range with the low byte being the second byte after the 12 escape.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Single-byte operators occupy values 1–31; the four two-byte escape operators are encoded as <c>0x0Cxx</c> so they are distinguishable from single-byte values.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">CFF CharString Format</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CffInterpreter"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff">OpenType specification: CFF CharString Format</seealso>
public enum Type2CharStringOperator : ushort
{
    /// <summary>1: horizontal stem hint.</summary>
    HStem = 1,
    /// <summary>3: vertical stem hint.</summary>
    VStem = 3,
    /// <summary>4: vertical moveto.</summary>
    VMoveTo = 4,
    /// <summary>5: relative lineto.</summary>
    RLineTo = 5,
    /// <summary>6: horizontal lineto.</summary>
    HLineTo = 6,
    /// <summary>7: vertical lineto.</summary>
    VLineTo = 7,
    /// <summary>8: relative cubic Bézier curveto.</summary>
    RRCurveTo = 8,
    /// <summary>10: call a local subroutine.</summary>
    CallSubr = 10,
    /// <summary>11: return from a subroutine.</summary>
    Return = 11,
    /// <summary>12: escape prefix for two-byte operators.</summary>
    Escape = 12,
    /// <summary>14: end the character.</summary>
    EndChar = 14,
    /// <summary>18: horizontal stem hint with implicit vstem.</summary>
    HStemHM = 18,
    /// <summary>19: hint mask.</summary>
    HintMask = 19,
    /// <summary>20: counter mask.</summary>
    CntrMask = 20,
    /// <summary>21: relative moveto.</summary>
    RMoveTo = 21,
    /// <summary>22: horizontal moveto.</summary>
    HMoveTo = 22,
    /// <summary>23: vertical stem hint with implicit hstem.</summary>
    VStemHM = 23,
    /// <summary>24: relative curveto followed by lineto.</summary>
    RCurveLine = 24,
    /// <summary>25: relative lineto followed by curveto.</summary>
    RLineCurve = 25,
    /// <summary>26: vertical-vertical curveto.</summary>
    VVCurveTo = 26,
    /// <summary>27: horizontal-horizontal curveto.</summary>
    HHCurveTo = 27,
    /// <summary>28: short integer encoding.</summary>
    ShortInt = 28,
    /// <summary>29: call a global subroutine.</summary>
    CallGSubr = 29,
    /// <summary>30: vertical-horizontal curveto.</summary>
    VHCurveTo = 30,
    /// <summary>31: horizontal-vertical curveto.</summary>
    HVCurveTo = 31,

    /// <summary>12 3: logical AND.</summary>
    And = 0x0C03,
    /// <summary>12 4: logical OR.</summary>
    Or = 0x0C04,
    /// <summary>12 5: logical NOT.</summary>
    Not = 0x0C05,
    /// <summary>12 9: absolute value.</summary>
    Abs = 0x0C09,
    /// <summary>12 10: addition.</summary>
    Add = 0x0C0A,
    /// <summary>12 11: subtraction.</summary>
    Sub = 0x0C0B,
    /// <summary>12 12: division.</summary>
    Div = 0x0C0C,
    /// <summary>12 14: negation.</summary>
    Neg = 0x0C0E,
    /// <summary>12 15: equality.</summary>
    Eq = 0x0C0F,
    /// <summary>12 18: drop the top stack element.</summary>
    Drop = 0x0C12,
    /// <summary>12 20: put a value into the transient array.</summary>
    Put = 0x0C14,
    /// <summary>12 21: get a value from the transient array.</summary>
    Get = 0x0C15,
    /// <summary>12 22: if-else.</summary>
    IfElse = 0x0C16,
    /// <summary>12 23: random number.</summary>
    Random = 0x0C17,
    /// <summary>12 24: multiplication.</summary>
    Mul = 0x0C18,
    /// <summary>12 26: square root.</summary>
    Sqrt = 0x0C1A,
    /// <summary>12 27: duplicate the top stack element.</summary>
    Dup = 0x0C1B,
    /// <summary>12 28: exchange the top two stack elements.</summary>
    Exch = 0x0C1C,
    /// <summary>12 29: copy the n-th stack element to the top.</summary>
    Index = 0x0C1D,
    /// <summary>12 30: roll the top n stack elements.</summary>
    Roll = 0x0C1E,
    /// <summary>12 34: horizontal flex.</summary>
    HFlex = 0x0C22,
    /// <summary>12 35: flex.</summary>
    Flex = 0x0C23,
    /// <summary>12 36: horizontal flex with one argument.</summary>
    HFlex1 = 0x0C24,
    /// <summary>12 37: flex with one argument.</summary>
    Flex1 = 0x0C25,
}
