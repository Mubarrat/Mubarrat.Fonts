using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════
// Header
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The 5-byte CFF2 table header. Blittable, no padding.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The header is followed immediately by the Top DICT, whose length is declared by <see cref="TopDictSize"/>.</description></item>
/// <item><description>Every field is a single byte or big-endian uint16, so the type implements <see cref="IEndianReversibleStruct{T}"/> for consistency with other headers.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">CFF2 table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Cff2Table"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">OpenType specification: CFF2 table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct Cff2Header : IEndianReversibleStruct<Cff2Header>
{
    /// <summary>Gets the major version. Must be 2.</summary>
    /// <value>The constant <c>2</c> for a conforming CFF2 header.</value>
    /// <seealso cref="MinorVersion"/>
    public byte MajorVersion;

    /// <summary>Gets the minor version. Must be 0.</summary>
    /// <value>The constant <c>0</c> for a conforming CFF2 header.</value>
    /// <seealso cref="MajorVersion"/>
    public byte MinorVersion;

    /// <summary>Gets the header size in bytes. Must be at least 5.</summary>
    /// <value>The on-disk size of the header; always 5 for the current version, but the field is present so future versions can extend the header.</value>
    /// <seealso cref="TopDictSize"/>
    public byte HeaderSize;

    /// <summary>Gets the size of the Top DICT that immediately follows the header.</summary>
    /// <value>The byte length of the Top DICT block; used by <see cref="Cff2Table.Parse"/> to locate the Global Subr INDEX that follows.</value>
    /// <seealso cref="HeaderSize"/>
    public ushort TopDictSize;

    /// <inheritdoc/>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new header with <see cref="TopDictSize"/> reversed and the three byte fields copied through.</returns>
    /// <remarks>The three version and size fields are single bytes and do not participate in the reversal.</remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    public static Cff2Header ReverseEndianness(Cff2Header v) => new()
    {
        MajorVersion = v.MajorVersion,
        MinorVersion = v.MinorVersion,
        HeaderSize = v.HeaderSize,
        TopDictSize = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(v.TopDictSize),
    };
}

// ═══════════════════════════════════════════════════════════════════════════
// Operators
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>CFF2 CharString operators. CFF2 keeps the path and hint operators from Type 2, removes the arithmetic, logic, stack, and transient-array operators, and adds <see cref="VsIndex"/> and <see cref="Blend"/> for variable-font support.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Single-byte operators occupy values 0–31; the four two-byte escape operators are encoded as <c>0x0Cxx</c> so they are distinguishable from single-byte values.</description></item>
/// <item><description>Operators removed from Type 2 include the arithmetic (add, sub, mul, div), logic (and, or, not), stack manipulation (dup, exch, index, roll), transient-array, and old hint operators.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2charstr">CFF2 CharString Format</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Cff2Interpreter"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2charstr">OpenType specification: CFF2 CharString Format</seealso>
public enum Cff2CharStringOperator : ushort
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
    /// <summary>14: end the character. No width, no <c>seac</c> in CFF2.</summary>
    EndChar = 14,
    /// <summary>15: select the ItemVariationData used by subsequent blend operators.</summary>
    VsIndex = 15,
    /// <summary>16: blend operand deltas against the active variation regions.</summary>
    Blend = 16,
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
    /// <summary>29: call a global subroutine.</summary>
    CallGSubr = 29,
    /// <summary>30: vertical-horizontal curveto.</summary>
    VHCurveTo = 30,
    /// <summary>31: horizontal-vertical curveto.</summary>
    HVCurveTo = 31,

    /// <summary>12 34: horizontal flex.</summary>
    HFlex = 0x0C22,
    /// <summary>12 35: flex.</summary>
    Flex = 0x0C23,
    /// <summary>12 36: horizontal flex with one argument.</summary>
    HFlex1 = 0x0C24,
    /// <summary>12 37: flex with one argument.</summary>
    Flex1 = 0x0C25,
}

// ═══════════════════════════════════════════════════════════════════════════
// INDEX
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A CFF2 INDEX: an array of variable-length data objects addressed by a parallel offset array. The format is identical to the CFF 1 INDEX except that the object count is a <c>uint32</c> rather than a <c>uint16</c>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The on-disk layout is: <c>uint32 count</c>, <c>byte offSize</c>, <c>offSize-byte offsets[count + 1]</c>, then the data block. Offsets are one-based so <c>Offsets[0]</c> is always <c>1</c>.</description></item>
/// <item><description>An INDEX with zero entries still occupies 4 bytes on disk (the count word) and no offset or data bytes; the parser represents this as the <see cref="Empty"/> singleton.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">CFF2 INDEX format</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Cff2Table"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">OpenType specification: CFF2 INDEX format</seealso>
public sealed record Cff2Index : IRecord<Cff2Index>
{
    /// <summary>An INDEX with zero entries.</summary>
    /// <remarks>The singleton uses a sentinel offset array of <c>[1]</c> and an empty data block, matching the on-disk representation of an empty INDEX.</remarks>
    public static readonly Cff2Index Empty = new() { Count = 0, Offsets = [1], Data = [], DataStart = 0 };

    /// <summary>Gets the number of objects in the index.</summary>
    /// <value>The <c>uint32</c> count field from the on-disk INDEX, narrowed to <see cref="int"/> during parse.</value>
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

    /// <summary>Gets the absolute byte offset of the data block within the CFF2 table.</summary>
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
    /// <seealso cref="Data"/>
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
    /// <exception cref="InvalidDataException">The count exceeds <see cref="int.MaxValue"/> - 1, the offset size is outside 1–4, the first offset is not 1, or the offsets are not monotonic.</exception>
    /// <exception cref="EndOfStreamException">The count, offset array, or data block extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The count is read first; a count of zero short-circuits and returns an INDEX with a sentinel offset array and empty data.</description></item>
    /// <item><description>The offset array is validated for the two invariants the specification requires: the first offset is exactly 1 and every subsequent offset is at least as large as the previous one.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">CFF2 INDEX format</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Offsets"/>
    /// <seealso cref="Data"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">OpenType specification: CFF2 INDEX format</seealso>
    public static Cff2Index Parse(ref Cursor cursor, object? context)
    {
        uint count = cursor.ReadUInt32();
        if (count == 0)
            return new() { Count = 0, Offsets = [1], Data = [], DataStart = cursor.Position };

        if (count > int.MaxValue - 1)
            throw new InvalidDataException($"CFF2 INDEX count {count} is too large.");

        int n = (int)count;
        byte offSize = cursor.ReadUInt8();
        if (offSize is < 1 or > 4)
            throw new InvalidDataException($"CFF2 INDEX offSize is {offSize}, expected 1–4.");

        int[] offsets = new int[n + 1];
        for (int i = 0; i <= n; i++)
            offsets[i] = ReadOffset(ref cursor, offSize);

        if (offsets[0] != 1)
            throw new InvalidDataException($"CFF2 INDEX first offset is {offsets[0]}, expected 1.");
        for (int i = 1; i <= n; i++)
            if (offsets[i] < offsets[i - 1])
                throw new InvalidDataException($"CFF2 INDEX offsets are not monotonic at index {i}.");

        long dataStart = cursor.Position;
        int dataLength = offsets[n] - 1;
        byte[] data = cursor.ReadBytes(dataLength);

        return new() { Count = n, Offsets = offsets, Data = data, DataStart = dataStart };
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

    /// <summary>Materializes every object in <paramref name="index"/> as a byte array.</summary>
    /// <param name="index">The INDEX to materialize.</param>
    /// <returns>An array with one byte array per object in the INDEX.</returns>
    /// <remarks>Each entry is an independent copy of the corresponding object's bytes.</remarks>
    /// <seealso cref="GetBytes(int)"/>
    public static byte[][] Materialize(Cff2Index index)
    {
        byte[][] result = new byte[index.Count][];
        for (int i = 0; i < index.Count; i++)
            result[i] = index.GetBytes(i);
        return result;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Top DICT
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A parsed CFF2 Top DICT. Only a small subset of the CFF 1 Top DICT operators is permitted in CFF2; glyph names, metrics, and character mapping come from other OpenType tables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Two operators are required: the CharStrings INDEX offset and the FontDICTINDEX offset. The parser rejects a Top DICT that omits either.</description></item>
/// <item><description>The remaining operators (FontMatrix, FontDICTSelect, ItemVariationStore) are optional; a missing FontDICTSelect defaults to font DICT 0 and a missing ItemVariationStore means the font has no variations.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">CFF2 Top DICT</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Cff2Table"/>
/// <seealso cref="CffDict"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">OpenType specification: CFF2 Top DICT</seealso>
public sealed record Cff2TopDict : IRecord<Cff2TopDict>
{
    /// <summary>CFF2 Top DICT operator codes.</summary>
    /// <remarks>Only the CFF2-permitted subset of the CFF 1 Top DICT operators is listed; other operators are undefined in CFF2.</remarks>
    public static class Ops
    {
        /// <summary>12 7: the FontMatrix operands.</summary>
        public const ushort FontMatrix = 0x0C07;
        /// <summary>17: the CharStrings INDEX offset. Required.</summary>
        public const ushort CharStrings = 17;
        /// <summary>12 36: the FontDICTINDEX offset. Required.</summary>
        public const ushort FDArray = 0x0C24;
        /// <summary>12 37: the FontDICTSelect offset, or 0 when absent.</summary>
        public const ushort FDSelect = 0x0C25;
        /// <summary>24: the ItemVariationStore offset, or 0 when absent.</summary>
        public const ushort VStore = 24;
    }

    /// <summary>Gets the raw DICT.</summary>
    /// <value>The <see cref="CffDict"/> that holds the parsed operator/operand entries; the named accessors delegate to it.</value>
    /// <seealso cref="CffDict"/>
    public CffDict Raw { get; init; } = null!;

    /// <summary>Gets the FontMatrix operands, or an empty array (default identity).</summary>
    /// <value>The six FontMatrix values when present; an empty array means the identity matrix applies.</value>
    /// <seealso cref="Ops.FontMatrix"/>
    public double[] FontMatrix => Raw.Get(Ops.FontMatrix) ?? [];

    /// <summary>Gets the CharString INDEX offset.</summary>
    /// <value>The byte offset of the CharStrings INDEX, measured from the CFF2 table start.</value>
    /// <seealso cref="Ops.CharStrings"/>
    public int CharStringsOffset => (int)Raw.GetNumber(Ops.CharStrings);

    /// <summary>Gets the FontDICTINDEX offset.</summary>
    /// <value>The byte offset of the FontDICTINDEX, measured from the CFF2 table start.</value>
    /// <seealso cref="Ops.FDArray"/>
    public int FDArrayOffset => (int)Raw.GetNumber(Ops.FDArray);

    /// <summary>Gets the FontDICTSelect offset, or 0 when absent.</summary>
    /// <value>The byte offset of the FontDICTSelect subtable, or <c>0</c> when the operator is not present.</value>
    /// <seealso cref="Ops.FDSelect"/>
    /// <seealso cref="HasFdSelect"/>
    public int FDSelectOffset => (int)Raw.GetNumber(Ops.FDSelect);

    /// <summary>Gets the ItemVariationStore offset, or 0 when absent.</summary>
    /// <value>The byte offset of the ItemVariationStore, or <c>0</c> when the operator is not present.</value>
    /// <seealso cref="Ops.VStore"/>
    /// <seealso cref="HasVariations"/>
    public int VStoreOffset => (int)Raw.GetNumber(Ops.VStore);

    /// <summary>True when the font carries variation data.</summary>
    /// <value><see langword="true"/> when the ItemVariationStore offset is greater than zero.</value>
    /// <seealso cref="VStoreOffset"/>
    public bool HasVariations => VStoreOffset > 0;

    /// <summary>True when a FontDICTSelect subtable is present.</summary>
    /// <value><see langword="true"/> when the FontDICTSelect offset is greater than zero.</value>
    /// <seealso cref="FDSelectOffset"/>
    public bool HasFdSelect => FDSelectOffset > 0;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the Top DICT.</param>
    /// <param name="context">A <see cref="CffDictContext"/> carrying the DICT's byte length.</param>
    /// <returns>The parsed Top DICT.</returns>
    /// <exception cref="InvalidDataException">The CharStrings offset or the FontDICTINDEX offset is missing or non-positive.</exception>
    /// <remarks>The DICT is parsed first, then the two required offsets are validated; the validation is what makes this type's parse stricter than the raw <see cref="CffDict"/> parse.</remarks>
    /// <seealso cref="CffDict.Parse"/>
    public static Cff2TopDict Parse(ref Cursor cursor, object? context)
    {
        CffDict raw = CffDict.Parse(ref cursor, context);
        Cff2TopDict topDict = new() { Raw = raw };

        if (topDict.CharStringsOffset <= 0)
            throw new InvalidDataException("CFF2 Top DICT is missing the required CharStrings offset.");
        if (topDict.FDArrayOffset <= 0)
            throw new InvalidDataException("CFF2 Top DICT is missing the required FDArray offset.");

        return topDict;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Private DICT
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A parsed CFF2 Private DICT: per-font hinting parameters, the Local Subr INDEX offset, and the variation data index used for blending.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>CFF2 removes the <c>defaultWidthX</c> and <c>nominalWidthX</c> keys — glyph advances come from <c>hmtx</c> — and adds <c>vsindex</c> for selecting the ItemVariationData subtable used by the CharString blend operator.</description></item>
/// <item><description>All fields are optional; a Private DICT can be an empty DICT with no operators at all.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">CFF2 Private DICT</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Cff2FontDict"/>
/// <seealso cref="CffDict"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">OpenType specification: CFF2 Private DICT</seealso>
public sealed record Cff2PrivateDict : IRecord<Cff2PrivateDict>
{
    /// <summary>CFF2 Private DICT operator codes.</summary>
    /// <remarks>The CFF2 Private DICT keeps most CFF 1 hinting keys but drops the width-related operators.</remarks>
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
        /// <summary>10: standard horizontal stem width.</summary>
        public const ushort StdHW = 10;
        /// <summary>11: standard vertical stem width.</summary>
        public const ushort StdVW = 11;
        /// <summary>12 9: blue scale.</summary>
        public const ushort BlueScale = 0x0C09;
        /// <summary>12 10: blue shift.</summary>
        public const ushort BlueShift = 0x0C0A;
        /// <summary>12 11: blue fuzz.</summary>
        public const ushort BlueFuzz = 0x0C0B;
        /// <summary>12 12: horizontal stem snap.</summary>
        public const ushort StemSnapH = 0x0C0C;
        /// <summary>12 13: vertical stem snap.</summary>
        public const ushort StemSnapV = 0x0C0D;
        /// <summary>12 17: language group.</summary>
        public const ushort LanguageGroup = 0x0C11;
        /// <summary>12 18: expansion factor.</summary>
        public const ushort ExpansionFactor = 0x0C12;
        /// <summary>19: offset to the local Subrs INDEX, relative to the Private DICT start.</summary>
        public const ushort Subrs = 19;
        /// <summary>22: the default variation data index used by blend.</summary>
        public const ushort VsIndex = 22;
    }

    /// <summary>Gets the raw DICT.</summary>
    /// <value>The <see cref="CffDict"/> that holds the parsed operator/operand entries; the named accessors delegate to it.</value>
    /// <seealso cref="CffDict"/>
    public CffDict Raw { get; init; } = null!;

    /// <summary>Gets the Local Subr INDEX offset relative to the Private DICT start, or 0.</summary>
    /// <value>The byte offset of the local Subrs INDEX from the start of the Private DICT, or <c>0</c> when the operator is not present.</value>
    /// <seealso cref="Ops.Subrs"/>
    public int SubrsOffset => (int)Raw.GetNumber(Ops.Subrs);

    /// <summary>Gets the variation data index used when blending. Defaults to 0.</summary>
    /// <value>The <c>vsindex</c> value declared in the Private DICT, or <c>0</c> when the operator is not present.</value>
    /// <seealso cref="Ops.VsIndex"/>
    /// <seealso cref="Cff2Interpreter.InitialVsIndex"/>
    public int VsIndex => (int)Raw.GetNumber(Ops.VsIndex);

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the Private DICT.</param>
    /// <param name="context">A <see cref="CffDictContext"/> carrying the DICT's byte length.</param>
    /// <returns>The parsed Private DICT.</returns>
    /// <exception cref="EndOfStreamException">The DICT extends past the end of the source.</exception>
    /// <remarks>All Private DICT operators are optional; an empty DICT parses successfully to an instance with default field values.</remarks>
    /// <seealso cref="CffDict.Parse"/>
    public static Cff2PrivateDict Parse(ref Cursor cursor, object? context)
    {
        CffDict raw = CffDict.Parse(ref cursor, context);
        return new Cff2PrivateDict { Raw = raw };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Font DICT
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A parsed CFF2 Font DICT. Every CFF2 table has at least one Font DICT; it provides the location of the Private DICT and an optional FontMatrix.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Font DICTs are collected in the FontDICTINDEX; each glyph is assigned to one Font DICT by the FontDICTSelect (or to Font DICT 0 when only one exists).</description></item>
/// <item><description>The Private DICT and its Local Subrs INDEX are resolved eagerly during parse; both are exposed as properties on this record.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">CFF2 Font DICT</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Cff2Table"/>
/// <seealso cref="Cff2PrivateDict"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">OpenType specification: CFF2 Font DICT</seealso>
public sealed record Cff2FontDict : IRecord<Cff2FontDict>
{
    /// <summary>Font DICT operator codes.</summary>
    /// <remarks>Font DICTs are much simpler than the Top DICT; they carry only the Private DICT location and an optional FontMatrix.</remarks>
    public static class Ops
    {
        /// <summary>18: the (size, offset) pair pointing at the Private DICT.</summary>
        public const ushort Private = 18;
        /// <summary>12 7: the FontMatrix operands.</summary>
        public const ushort FontMatrix = 0x0C07;
    }

    /// <summary>Gets the raw DICT.</summary>
    /// <value>The <see cref="CffDict"/> that holds the parsed operator/operand entries; the named accessors delegate to it.</value>
    /// <seealso cref="CffDict"/>
    public CffDict Raw { get; init; } = null!;

    /// <summary>Gets the Private DICT (size, offset) pair, or <c>null</c>.</summary>
    /// <value>A tuple of the Private DICT's byte length and its offset from the CFF2 table start, or <c>null</c> when the operator is missing or malformed.</value>
    /// <seealso cref="PrivateDict"/>
    /// <seealso cref="Ops.Private"/>
    public (int Size, int Offset)? Private
    {
        get
        {
            double[]? priv = Raw.Get(Ops.Private);
            return priv is { Length: >= 2 } ? ((int)priv[0], (int)priv[1]) : null;
        }
    }

    /// <summary>Gets the FontMatrix operands, or an empty array (default identity).</summary>
    /// <value>The six FontMatrix values when present on this Font DICT; an empty array means the Top DICT's FontMatrix or the identity matrix applies.</value>
    /// <seealso cref="Ops.FontMatrix"/>
    public double[] FontMatrix => Raw.Get(Ops.FontMatrix) ?? [];

    /// <summary>Gets the parsed Private DICT, or <c>null</c> when the Font DICT declares none.</summary>
    /// <value>The resolved <see cref="Cff2PrivateDict"/>, or <c>null</c> when the Private operator is absent or specifies a zero size or offset.</value>
    /// <seealso cref="Private"/>
    /// <seealso cref="Cff2PrivateDict"/>
    public Cff2PrivateDict? PrivateDict { get; init; }

    /// <summary>Gets the local subroutines for this Font DICT.</summary>
    /// <value>The materialized Local Subrs INDEX, or an empty list when the Private DICT declares no Subrs operator.</value>
    /// <remarks>Local subroutines are scoped to the Font DICT; a glyph assigned to a different Font DICT has a different local subroutine collection.</remarks>
    /// <seealso cref="Cff2Interpreter.LocalSubrs"/>
    public IReadOnlyList<byte[]> LocalSubrs { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the Font DICT.</param>
    /// <param name="context">A <see cref="Cff2FontDictContext"/> carrying the DICT's byte length and the CFF2 table source.</param>
    /// <returns>The parsed Font DICT with its Private DICT and Local Subrs INDEX resolved.</returns>
    /// <exception cref="EndOfStreamException">The DICT, its Private DICT, or its Local Subrs INDEX extends past the end of the CFF2 table source.</exception>
    /// <remarks>The Private DICT's Subrs offset is relative to the Private DICT start, not to the Font DICT or the CFF2 table; the parser adds the Private DICT's offset before resolving.</remarks>
    /// <seealso cref="Cff2FontDictContext"/>
    /// <seealso cref="Cff2PrivateDict"/>
    /// <seealso cref="Cff2Index"/>
    public static Cff2FontDict Parse(ref Cursor cursor, object? context)
    {
        Cff2FontDictContext ctx = (Cff2FontDictContext)context!;
        CffDict raw = CffDict.Parse(ref cursor, new CffDictContext(ctx.Length));

        Cff2PrivateDict? privateDict = null;
        IReadOnlyList<byte[]> localSubrs = [];

        double[]? priv = raw.Get(Ops.Private);
        if (priv is { Length: >= 2 })
        {
            int size = (int)priv[0];
            int offset = (int)priv[1];
            if (size > 0 && offset > 0)
            {
                privateDict = ctx.ParentSource.ParseRecordAt<Cff2PrivateDict>(
                    offset, new CffDictContext(size));

                if (privateDict.SubrsOffset > 0)
                {
                    Cff2Index subrsIndex = ctx.ParentSource.ParseRecordAt<Cff2Index>(
                        offset + privateDict.SubrsOffset);
                    localSubrs = Cff2Index.Materialize(subrsIndex);
                }
            }
        }

        return new Cff2FontDict
        {
            Raw = raw,
            PrivateDict = privateDict,
            LocalSubrs = localSubrs,
        };
    }
}

/// <summary>Context for parsing a CFF2 Font DICT. Carries the DICT's byte length and the CFF2 table source, needed to resolve the nested Private DICT and Local Subr INDEX.</summary>
/// <param name="Length">The number of bytes the Font DICT occupies.</param>
/// <param name="ParentSource">The CFF2 table source.</param>
/// <remarks>
/// <list type="bullet">
/// <item><description>The DICT's byte length is required because Font DICTs are stored inside an INDEX and have no self-describing terminator.</description></item>
/// <item><description>The source is passed through so the Font DICT can resolve offsets to the Private DICT and Local Subrs INDEX, which lie elsewhere in the CFF2 table.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Cff2FontDict.Parse"/>
public record Cff2FontDictContext(int Length, Source ParentSource);

// ═══════════════════════════════════════════════════════════════════════════
// FDSelect
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A CFF2 FontDICTSelect subtable: maps glyph IDs to Font DICT indices. Present only when the FontDICTINDEX contains more than one Font DICT.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>CFF2 adds format 4, which uses <c>uint32</c> range boundaries and a <c>uint32</c> sentinel, for fonts with more than 65 535 glyphs.</description></item>
/// <item><description>All three formats are expanded to a flat per-glyph index array during parse, so <see cref="GetFdIndex(int)"/> is a direct lookup regardless of the on-disk format.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2charstr">CFF2 CharString Format</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Cff2Table"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2charstr">OpenType specification: CFF2 CharString Format</seealso>
public sealed record Cff2FdSelect : IRecord<Cff2FdSelect>
{
    /// <summary>Gets the Font DICT indices, indexed by glyph ID.</summary>
    /// <value>The flat per-glyph Font DICT index array; length equals the font's glyph count.</value>
    /// <seealso cref="GetFdIndex(int)"/>
    /// <seealso cref="Count"/>
    public byte[] FdIndices { get; init; } = [];

    /// <summary>Gets the number of glyphs.</summary>
    /// <value>The size of the <see cref="FdIndices"/> array.</value>
    /// <seealso cref="FdIndices"/>
    public int Count => FdIndices.Length;

    /// <summary>Gets the Font DICT index for glyph <paramref name="glyphId"/>.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <returns>The Font DICT index assigned to the glyph.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is out of range.</exception>
    /// <remarks>The lookup is constant-time; the glyph ID directly indexes into <see cref="FdIndices"/>.</remarks>
    /// <seealso cref="FdIndices"/>
    public int GetFdIndex(int glyphId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, FdIndices.Length);
        return FdIndices[glyphId];
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the FDSelect subtable.</param>
    /// <param name="context">A <see cref="Cff2FdSelectContext"/> carrying the font's glyph count.</param>
    /// <returns>The parsed FDSelect subtable with a flat per-glyph index array.</returns>
    /// <exception cref="InvalidDataException">The format is not 0, 3, or 4.</exception>
    /// <exception cref="EndOfStreamException">The format-specific data extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Format 0 stores one byte per glyph; the parser reads them directly into the flat array.</description></item>
    /// <item><description>Formats 3 and 4 store ranges; the parser expands each range into per-glyph entries, capping the expansion at the font's glyph count so an out-of-range sentinel cannot overrun.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2charstr">CFF2 CharString Format</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Cff2FdSelectContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2charstr">OpenType specification: CFF2 CharString Format</seealso>
    public static Cff2FdSelect Parse(ref Cursor cursor, object? context)
    {
        Cff2FdSelectContext ctx = (Cff2FdSelectContext)context!;
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

            case 4:
                {
                    uint nRanges = cursor.ReadUInt32();
                    (uint First, byte Fd)[] ranges = new (uint, byte)[nRanges];
                    for (int i = 0; i < (int)nRanges; i++)
                    {
                        uint first = cursor.ReadUInt32();
                        byte fd = cursor.ReadUInt8();
                        ranges[i] = (first, fd);
                    }
                    uint sentinel = cursor.ReadUInt32();

                    for (int i = 0; i < (int)nRanges; i++)
                    {
                        long start = ranges[i].First;
                        long end = i + 1 < (int)nRanges ? ranges[i + 1].First : sentinel;
                        for (long gid = start; gid < end && gid < ctx.NumGlyphs; gid++)
                            indices[gid] = ranges[i].Fd;
                    }
                    break;
                }

            default:
                throw new InvalidDataException($"Invalid CFF2 FDSelect format {format}.");
        }

        return new Cff2FdSelect { FdIndices = indices };
    }
}

/// <summary>Context for parsing a CFF2 FontDICTSelect.</summary>
/// <param name="NumGlyphs">The number of glyphs, from the CharStrings INDEX.</param>
/// <remarks>
/// <list type="bullet">
/// <item><description>The glyph count determines the length of the flat index array the parser materializes from the format-specific on-disk representation.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Cff2FdSelect.Parse"/>
public record Cff2FdSelectContext(int NumGlyphs);

// ═══════════════════════════════════════════════════════════════════════════
// CharString interpreter
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Interprets a CFF2 CharString into a <see cref="CffGlyph"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The interpreter extends the Type 2 model with the CFF2 <c>vsindex</c> and <c>blend</c> operators, which evaluate variation deltas against the current normalized coordinates and the font's <see cref="ItemVariationStore"/>. Output is the same <see cref="CffGlyph"/> shape as Type 2, minus the width — CFF2 widths come from <c>hmtx</c>.</description></item>
/// <item><description>Configure with an object initializer; state is reset per <see cref="Run"/> call. The type is designed to be constructed once per glyph and discarded.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2charstr">CFF2 CharString Format</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Cff2Table"/>
/// <seealso cref="CffGlyph"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2charstr">OpenType specification: CFF2 CharString Format</seealso>
public sealed class Cff2Interpreter
{
    private const int StackLimit = 513;
    private const int SubrNestingLimit = 10;

    /// <summary>Gets the local subroutines.</summary>
    /// <value>The Font DICT-scoped local subroutine INDEX, or an empty list when the Font DICT declares no subroutines.</value>
    /// <seealso cref="GlobalSubrs"/>
    public IReadOnlyList<byte[]> LocalSubrs { get; init; } = [];

    /// <summary>Gets the global subroutines.</summary>
    /// <value>The CFF2 table-scoped global subroutine INDEX, shared across every Font DICT.</value>
    /// <seealso cref="LocalSubrs"/>
    public IReadOnlyList<byte[]> GlobalSubrs { get; init; } = [];

    /// <summary>Gets the ItemVariationStore used by blend, or <c>null</c> when the font has no variations.</summary>
    /// <value>The <see cref="ItemVariationStore"/> resolved from the Top DICT's VStore operator, or <c>null</c> when the font is not variable.</value>
    /// <seealso cref="NormalizedCoords"/>
    /// <seealso cref="ItemVariationStore"/>
    public ItemVariationStore? VariationStore { get; init; }

    /// <summary>Gets the normalized axis coordinates used when evaluating blend deltas.</summary>
    /// <value>One value per axis in the font's <c>fvar</c> table. An empty list means the default instance (all axes at zero).</value>
    /// <seealso cref="VariationStore"/>
    public IReadOnlyList<F2Dot14> NormalizedCoords { get; init; } = [];

    /// <summary>Gets the initial vsindex used when a CharString does not declare one.</summary>
    /// <value>The <c>vsindex</c> value from the Font DICT's Private DICT, or <c>0</c> when the operator is absent.</value>
    /// <seealso cref="Cff2PrivateDict.VsIndex"/>
    public int InitialVsIndex { get; init; }

    private readonly double[] _stack = new double[StackLimit];
    private int _stackDepth;
    private int _localBias;
    private int _globalBias;
    private int _vsIndex;
    private readonly List<CffContour> _contours = [];
    private List<CffSegment>? _segments;
    private CffPoint _current;
    private CffPoint _contourStart;
    private int _hintCount;
    private bool _ended;

    /// <summary>Runs <paramref name="program"/> and returns the resulting glyph outline.</summary>
    /// <param name="program">The CFF2 CharString byte code.</param>
    /// <returns>The interpreted <see cref="CffGlyph"/>. Its <see cref="CffGlyph.Width"/> is always zero and <see cref="CffGlyph.HasWidth"/> is always <see langword="false"/>; CFF2 widths live in <c>hmtx</c>.</returns>
    /// <exception cref="InvalidDataException">The program contains an undefined operator, references a subroutine or variation data index out of range, or overflows the operand stack.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The interpreter state is reset at the start of each call, so a single instance can be reused for successive glyphs.</description></item>
    /// <item><description>Subroutine bias is computed from the subroutine count using the Type 2 rules.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2charstr">CFF2 CharString Format</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CffGlyph"/>
    /// <seealso cref="Cff2Table.GetGlyphAt(int, IReadOnlyList{F2Dot14})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2charstr">OpenType specification: CFF2 CharString Format</seealso>
    public CffGlyph Run(byte[] program)
    {
        _stackDepth = 0;
        _contours.Clear();
        _segments = null;
        _current = default;
        _contourStart = default;
        _hintCount = 0;
        _vsIndex = InitialVsIndex;
        _ended = false;
        _localBias = ComputeBias(LocalSubrs.Count);
        _globalBias = ComputeBias(GlobalSubrs.Count);

        Execute(program, 0);
        CloseContour();

        return new CffGlyph
        {
            Contours = _contours,
            Width = 0,       // CFF2 widths live in hmtx
            HasWidth = false,
        };
    }

    private void Execute(byte[] program, int depth)
    {
        if (depth > SubrNestingLimit)
            throw new InvalidDataException($"CFF2 charstring subroutine nesting exceeded {SubrNestingLimit}.");

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
                    _hintCount += _stackDepth / 2;
                    _stackDepth = 0;
                    break;

                case 19:
                case 20:
                    _hintCount += _stackDepth / 2;
                    _stackDepth = 0;
                    i += (_hintCount + 7) >> 3;
                    break;

                case 21: // rmoveto
                    _current = new(_current.X + Pop2(out double dx21), _current.Y + dx21);
                    MoveTo();
                    break;

                case 22: // hmoveto
                    _current = new(_current.X + Pop(), _current.Y);
                    MoveTo();
                    break;

                case 4: // vmoveto
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

                case 15: // vsindex
                    _vsIndex = (int)Pop();
                    break;

                case 16: // blend
                    Blend();
                    break;

                case 10: // callsubr
                    {
                        int index = (int)Pop() + _localBias;
                        if ((uint)index >= (uint)LocalSubrs.Count)
                            throw new InvalidDataException($"CFF2 local subroutine index {index} is out of range.");
                        Execute(LocalSubrs[index], depth + 1);
                        break;
                    }

                case 29: // callgsubr
                    {
                        int index = (int)Pop() + _globalBias;
                        if ((uint)index >= (uint)GlobalSubrs.Count)
                            throw new InvalidDataException($"CFF2 global subroutine index {index} is out of range.");
                        Execute(GlobalSubrs[index], depth + 1);
                        break;
                    }

                case 11: return;
                case 14: _ended = true; break;

                case 12:
                    {
                        byte b1 = program[i++];
                        switch (b1)
                        {
                            case 35: Flex(); break;
                            case 34: HFlex(); break;
                            case 36: HFlex1(); break;
                            case 37: Flex1(); break;
                            default:
                                throw new InvalidDataException($"Undefined CFF2 escape operator 12 {b1}.");
                        }
                        break;
                    }

                default:
                    throw new InvalidDataException($"Undefined CFF2 charstring operator {b0}.");
            }
        }
    }

    /// <summary>Implements the CFF2 <c>blend</c> operator. Pops <c>n</c> default values, then <c>n * k</c> deltas where <c>k</c> is the region count of the ItemVariationData selected by the current <c>vsindex</c>, blends each default with its per-region deltas scaled by the region scalar, and pushes the resulting <c>n</c> values.</summary>
    /// <exception cref="InvalidDataException">The blend count is negative, no <see cref="VariationStore"/> is present, or the current <c>vsindex</c> is out of range.</exception>
    private void Blend()
    {
        int n = (int)Pop();
        if (n < 0)
            throw new InvalidDataException($"CFF2 blend count {n} is negative.");

        if (VariationStore is null || n == 0)
        {
            // No variation store: deltas contribute nothing. Read the deltas off the
            // stack and push the defaults unchanged.
            if (n > 0)
            {
                // Without a store we cannot know k, so fall through assuming the
                // charstring is malformed — this path is a defensive guard.
                throw new InvalidDataException(
                    "CFF2 blend requires an ItemVariationStore but none is present.");
            }
            return;
        }

        if ((uint)_vsIndex >= (uint)VariationStore.ItemVariationData.Count)
            throw new InvalidDataException(
                $"CFF2 vsindex {_vsIndex} is out of range (font has {VariationStore.ItemVariationData.Count} subtables).");

        ItemVariationData data = VariationStore.ItemVariationData[_vsIndex];
        int k = data.RegionIndexes.Count;

        double[] deltas = new double[n * k];
        for (int j = n * k - 1; j >= 0; j--)
            deltas[j] = Pop();

        double[] values = new double[n];
        for (int j = n - 1; j >= 0; j--)
            values[j] = Pop();

        // Compute per-region scalars from the normalized coordinates.
        double[] scalars = ComputeRegionScalars(VariationStore.VariationRegionList, data.RegionIndexes);

        for (int vi = 0; vi < n; vi++)
        {
            double blended = values[vi];
            int baseIdx = vi * k;
            for (int ri = 0; ri < k; ri++)
                blended += deltas[baseIdx + ri] * scalars[ri];
            Push(blended);
        }
    }

    /// <summary>Computes the scalar weight for each variation region referenced by <paramref name="regionIndexes"/>, given the interpreter's <see cref="NormalizedCoords"/>.</summary>
    /// <param name="regionList">The font's VariationRegionList.</param>
    /// <param name="regionIndexes">The region indices referenced by the active ItemVariationData.</param>
    /// <returns>An array of scalars, one per entry in <paramref name="regionIndexes"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Each region's scalar is the product of its per-axis factors; a factor of zero on any axis short-circuits the whole region to zero.</description></item>
    /// <item><description>Region indices beyond the region list's count are skipped, leaving their scalar at zero.</description></item>
    /// <item><description>Axis counts are capped at the smaller of the region's axis count and the interpreter's <see cref="NormalizedCoords"/> length, so a caller-supplied coordinate array shorter than the font's axis count does not overrun.</description></item>
    /// </list>
    /// </remarks>
    private double[] ComputeRegionScalars(
        VariationRegionList regionList,
        IReadOnlyList<ushort> regionIndexes)
    {
        double[] scalars = new double[regionIndexes.Count];

        for (int i = 0; i < regionIndexes.Count; i++)
        {
            int regionIdx = regionIndexes[i];
            if ((uint)regionIdx >= (uint)regionList.Regions.Count)
                continue;

            VariationRegion region = regionList.Regions[regionIdx];
            double scalar = 1.0;
            int axes = Math.Min(region.Axes.Count, NormalizedCoords.Count);

            for (int a = 0; a < axes; a++)
            {
                AxisRegionCoordinates coords = region.Axes[a];
                double start = coords.Start.Value;
                double peak = coords.Peak.Value;
                double end = coords.End.Value;
                double v = NormalizedCoords[a].Value;

                double factor;
                if (peak == 0)
                    factor = 1.0;
                else if (v < start || v > end)
                {
                    factor = 0.0;
                }
                else if (v == peak)
                {
                    factor = 1.0;
                }
                else if (v < peak)
                {
                    factor = peak == start ? 1.0 : (v - start) / (peak - start);
                }
                else
                {
                    factor = peak == end ? 1.0 : (end - v) / (end - peak);
                }

                scalar *= factor;
                if (scalar == 0.0) break;
            }

            scalars[i] = scalar;
        }

        return scalars;
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
            throw new InvalidDataException("CFF2 operand stack overflowed.");
        _stack[_stackDepth++] = v;
    }

    private double Pop()
    {
        if (_stackDepth == 0)
            throw new InvalidDataException("CFF2 operand stack underflowed.");
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
        throw new InvalidDataException($"Invalid CFF2 number byte 0x{b0:X2}.");
    }

    private static int ComputeBias(int subrCount) => subrCount switch
    {
        < 1240 => 107,
        < 33900 => 1131,
        _ => 32768,
    };
}

// ═══════════════════════════════════════════════════════════════════════════
// CFF2 table
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The <c>CFF2</c> table: Compact Font Format version 2 outline data. Used by OpenType fonts with PostScript outlines, including variable fonts.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>CFF2 is a refinement of <c>CFF </c> that removes data duplicated in other OpenType tables (glyph names, character mapping, metrics) and adds variation support through the <c>vsindex</c> and <c>blend</c> operators. It cannot be used as a stand-alone font.</description></item>
/// <item><description>Every glyph's outline is materialized during parsing by running each CharString through <see cref="Cff2Interpreter"/>. The result is a <see cref="CffGlyph"/> per glyph, exposed through <see cref="Glyphs"/>.</description></item>
/// <item><description>The parse-time interpretation uses an empty <see cref="Cff2Interpreter.NormalizedCoords"/> list, which evaluates every blend at the default instance. Use <see cref="GetGlyphAt(int, IReadOnlyList{F2Dot14})"/> to obtain a glyph at a specific instance.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">CFF2 table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Cff2Header"/>
/// <seealso cref="Cff2TopDict"/>
/// <seealso cref="Cff2FontDict"/>
/// <seealso cref="Cff2Interpreter"/>
/// <seealso cref="CffGlyph"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">OpenType specification: CFF2 table</seealso>
public sealed record Cff2Table : IFontTable<Cff2Table>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">OpenType specification: CFF2 table</seealso>
    public static Tag Tag => "CFF2";

    /// <summary>Gets the CFF2 header.</summary>
    /// <value>The raw <see cref="Cff2Header"/> struct read at the start of the table.</value>
    /// <seealso cref="Cff2Header"/>
    public Cff2Header Header { get; init; }

    /// <summary>Gets the parsed Top DICT.</summary>
    /// <value>The <see cref="Cff2TopDict"/> that provides the offsets to the CharStrings INDEX, FontDICTINDEX, optional FontDICTSelect, and optional ItemVariationStore.</value>
    /// <seealso cref="Cff2TopDict"/>
    public Cff2TopDict TopDict { get; init; } = null!;

    /// <summary>Gets the raw CharStrings, one per glyph.</summary>
    /// <value>The ordered list of <see cref="CffCharString"/> entries; each carries the raw program bytes and the Font DICT index used when interpreting it.</value>
    /// <seealso cref="Glyphs"/>
    /// <seealso cref="GetGlyphAt(int, IReadOnlyList{F2Dot14})"/>
    public IReadOnlyList<CffCharString> CharStrings { get; init; } = [];

    /// <summary>Gets the interpreted glyph outlines, one per glyph.</summary>
    /// <value>The ordered list of <see cref="CffGlyph"/> entries materialized at parse time using the default instance.</value>
    /// <seealso cref="CharStrings"/>
    /// <seealso cref="GetGlyphAt(int, IReadOnlyList{F2Dot14})"/>
    public IReadOnlyList<CffGlyph> Glyphs { get; init; } = [];

    /// <summary>Gets the global subroutines, shared across all CharStrings.</summary>
    /// <value>The materialized Global Subrs INDEX, or an empty list when the INDEX is empty.</value>
    /// <seealso cref="Cff2Interpreter.GlobalSubrs"/>
    public IReadOnlyList<byte[]> GlobalSubrs { get; init; } = [];

    /// <summary>Gets the parsed Font DICTs. Always at least one.</summary>
    /// <value>The ordered list of <see cref="Cff2FontDict"/> entries; index 0 is the default for fonts without a FontDICTSelect.</value>
    /// <seealso cref="GetFontDictIndex(int)"/>
    /// <seealso cref="Cff2FontDict"/>
    public IReadOnlyList<Cff2FontDict> FontDicts { get; init; } = [];

    /// <summary>Gets the FontDICTSelect, or <c>null</c> when the font has a single Font DICT.</summary>
    /// <value>The <see cref="Cff2FdSelect"/> that maps glyph IDs to Font DICT indices, or <c>null</c> when the font declares only one Font DICT.</value>
    /// <seealso cref="GetFontDictIndex(int)"/>
    /// <seealso cref="Cff2FdSelect"/>
    public Cff2FdSelect? FdSelect { get; init; }

    /// <summary>Gets the ItemVariationStore, or <c>null</c> when the font has no variation data.</summary>
    /// <value>The <see cref="ItemVariationStore"/> resolved from the Top DICT's VStore operator, or <c>null</c> for a non-variable font.</value>
    /// <seealso cref="HasVariations"/>
    /// <seealso cref="ItemVariationStore"/>
    public ItemVariationStore? VariationStore { get; init; }

    /// <summary>Gets the number of glyphs.</summary>
    /// <value>The size of the <see cref="CharStrings"/> list; also the size of <see cref="Glyphs"/>.</value>
    /// <seealso cref="CharStrings"/>
    /// <seealso cref="Glyphs"/>
    public int GlyphCount => CharStrings.Count;

    /// <summary>True when the font carries variation data.</summary>
    /// <value><see langword="true"/> when <see cref="VariationStore"/> is not <c>null</c>.</value>
    /// <seealso cref="VariationStore"/>
    public bool HasVariations => VariationStore is not null;

    /// <summary>Returns the Font DICT index assigned to glyph <paramref name="glyphId"/>.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <returns>The Font DICT index assigned to the glyph, or <c>0</c> when the font has a single Font DICT and no FontDICTSelect.</returns>
    /// <seealso cref="FdSelect"/>
    /// <seealso cref="FontDicts"/>
    public int GetFontDictIndex(int glyphId) => FdSelect?.GetFdIndex(glyphId) ?? 0;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the CFF2 table.</param>
    /// <param name="context">Unused. The table resolves its subtables through its own source.</param>
    /// <returns>The parsed CFF2 table with its glyphs materialized at the default instance.</returns>
    /// <exception cref="InvalidDataException">The version is not 2.0, the header size is less than 5, the TopDictSize is zero, the FontDICTINDEX is empty, or the font declares multiple Font DICTs but no FontDICTSelect.</exception>
    /// <exception cref="EndOfStreamException">The header, Top DICT, either INDEX, or any referenced subtable extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The Global Subrs INDEX is located immediately after the Top DICT, whose length comes from the header's <c>TopDictSize</c>.</description></item>
    /// <item><description>Each Font DICT resolves its own Private DICT and Local Subrs INDEX during parse, so the FontDICTs array is fully materialized.</description></item>
    /// <item><description>Every glyph is interpreted eagerly during parse; the interpretation uses an empty <see cref="Cff2Interpreter.NormalizedCoords"/> list, evaluating blend operators at the default instance.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">CFF2 table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Cff2Header"/>
    /// <seealso cref="Cff2TopDict"/>
    /// <seealso cref="Cff2FontDict"/>
    /// <seealso cref="Cff2Interpreter"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cff2">OpenType specification: CFF2 table</seealso>
    public static Cff2Table Parse(ref Cursor cursor, object? context)
    {
        Source source = cursor.Source;

        Cff2Header header = cursor.ReadBigEndianStruct<Cff2Header>();
        if (header.MajorVersion != 2)
            throw new InvalidDataException($"'CFF2'.majorVersion is {header.MajorVersion}, expected 2.");
        if (header.MinorVersion != 0)
            throw new InvalidDataException($"'CFF2'.minorVersion is {header.MinorVersion}, expected 0.");
        if (header.HeaderSize < 5)
            throw new InvalidDataException($"'CFF2'.headerSize is {header.HeaderSize}, expected at least 5.");
        if (header.TopDictSize == 0)
            throw new InvalidDataException("'CFF2'.topDictSize is 0.");

        // ── Top DICT ──
        Cff2TopDict topDict = source.ParseRecordAt<Cff2TopDict>(
            header.HeaderSize, new CffDictContext(header.TopDictSize));

        // ── Global Subr INDEX ──
        // Immediately follows the Top DICT.
        Cff2Index globalSubrIndex = source.ParseRecordAt<Cff2Index>(
            header.HeaderSize + header.TopDictSize);
        byte[][] globalSubrs = Cff2Index.Materialize(globalSubrIndex);

        // ── CharStrings INDEX ──
        Cff2Index charStringsIndex = source.ParseRecordAt<Cff2Index>(topDict.CharStringsOffset);
        int numGlyphs = charStringsIndex.Count;

        // ── FontDICTINDEX ──
        Cff2Index fontDictIndex = source.ParseRecordAt<Cff2Index>(topDict.FDArrayOffset);
        if (fontDictIndex.Count == 0)
            throw new InvalidDataException("CFF2 FontDICTINDEX is empty; at least one Font DICT is required.");

        Cff2FontDict[] fontDicts = new Cff2FontDict[fontDictIndex.Count];
        for (int i = 0; i < fontDictIndex.Count; i++)
        {
            (int rangeStart, int rangeLength) = fontDictIndex.GetRange(i);
            fontDicts[i] = source.ParseRecordAt<Cff2FontDict>(
                fontDictIndex.DataStart + rangeStart,
                new Cff2FontDictContext(rangeLength, source));
        }

        // ── FontDICTSelect ──
        Cff2FdSelect? fdSelect = null;
        if (topDict.FDSelectOffset > 0)
            fdSelect = source.ParseRecordAt<Cff2FdSelect>(
                topDict.FDSelectOffset, new Cff2FdSelectContext(numGlyphs));
        else if (fontDictIndex.Count > 1)
            throw new InvalidDataException("CFF2 has multiple Font DICTs but no FontDICTSelect.");

        // ── ItemVariationStore ──
        ItemVariationStore? variationStore = null;
        if (topDict.VStoreOffset != 0)
            variationStore = source.ParseRecordAt<ItemVariationStore>(topDict.VStoreOffset);

        // ── CharStrings + Interpretation ──
        CffCharString[] charStrings = new CffCharString[numGlyphs];
        CffGlyph[] glyphs = new CffGlyph[numGlyphs];

        for (int gid = 0; gid < numGlyphs; gid++)
        {
            int fdIndex = fdSelect?.GetFdIndex(gid) ?? 0;

            byte[] program = charStringsIndex.GetBytes(gid);
            charStrings[gid] = new() { Program = program, FdIndex = fdIndex };

            Cff2FontDict fontDict = fdIndex < fontDicts.Length ? fontDicts[fdIndex] : fontDicts[0];

            Cff2Interpreter interpreter = new()
            {
                LocalSubrs = fontDict.LocalSubrs,
                GlobalSubrs = globalSubrs,
                VariationStore = variationStore,
                NormalizedCoords = [],     // default instance; callers re-run with custom coords for variations
                InitialVsIndex = fontDict.PrivateDict?.VsIndex ?? 0,
            };
            glyphs[gid] = interpreter.Run(program);
        }

        return new()
        {
            Header = header,
            TopDict = topDict,
            CharStrings = charStrings,
            Glyphs = glyphs,
            GlobalSubrs = globalSubrs,
            FontDicts = fontDicts,
            FdSelect = fdSelect,
            VariationStore = variationStore,
        };
    }

    /// <summary>Interprets the glyph's CharString at the given normalized coordinates, applying variation deltas through the CFF2 <c>blend</c> operator.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <param name="normalizedCoords">The normalized axis coordinates.</param>
    /// <returns>The interpreted <see cref="CffGlyph"/> at the requested instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphId"/> is out of range.</exception>
    /// <exception cref="InvalidDataException">The CharString contains an undefined operator or overflows the operand stack when interpreted at the given coordinates.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Reinterprets the glyph's CharString from scratch, using the same Font DICT assignment as the parse-time interpretation.</description></item>
    /// <item><description>The <paramref name="normalizedCoords"/> list must align with the font's <c>fvar</c> axis order; the interpreter caps per-region axis counts at the smaller of the region's axis count and the supplied list's length.</description></item>
    /// <item><description>For non-variable fonts the method returns the same outline as <see cref="Glyphs"/>; the blend operator is not present in the CharStrings.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// F2Dot14[] coords = { new F2Dot14(0x2000) };  // weight axis at 0.5
    /// CffGlyph glyph = cff2.GetGlyphAt(glyphId, coords);
    /// </code>
    /// </example>
    /// <seealso cref="Glyphs"/>
    /// <seealso cref="CharStrings"/>
    /// <seealso cref="Cff2Interpreter"/>
    public CffGlyph GetGlyphAt(int glyphId, IReadOnlyList<F2Dot14> normalizedCoords)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(glyphId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(glyphId, GlyphCount);

        CffCharString charString = CharStrings[glyphId];
        int fdIndex = charString.FdIndex;
        Cff2FontDict fontDict = fdIndex < FontDicts.Count ? FontDicts[fdIndex] : FontDicts[0];

        Cff2Interpreter interpreter = new()
        {
            LocalSubrs = fontDict.LocalSubrs,
            GlobalSubrs = GlobalSubrs,
            VariationStore = VariationStore,
            NormalizedCoords = normalizedCoords,
            InitialVsIndex = fontDict.PrivateDict?.VsIndex ?? 0,
        };
        return interpreter.Run(charString.Program);
    }
}
