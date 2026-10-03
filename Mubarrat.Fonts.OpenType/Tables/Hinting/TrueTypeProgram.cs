using Mubarrat.Fonts.OpenType.Binary;

namespace Mubarrat.Fonts.OpenType.Tables.Hinting;

/// <summary>The TrueType instruction set. Every documented opcode from the OpenType specification appendix. Parameterized opcodes (PUSHB, PUSHW, MDRP, MIRP) are represented by a single enum value; the low bits of the raw opcode are stored in <see cref="TrueTypeInstruction.Variant"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The underlying type is <see cref="ushort"/> rather than <see cref="byte"/> so that <see cref="Unknown"/> can hold a sentinel value that never collides with a valid opcode.</description></item>
/// <item><description>Single-byte opcodes occupy 0x00–0x92, with four reserved gaps (0x28, 0x7B, 0x83–0x84, 0x8F–0x90) that the decoder maps to <see cref="Unknown"/>.</description></item>
/// <item><description>Parameterized ranges are 0xB0–0xB7 (PUSHB), 0xB8–0xBF (PUSHW), 0xC0–0xDF (MDRP), and 0xE0–0xFF (MIRP); the low bits carry the variant index.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/tt_instructions">TrueType instruction set</see> appendix in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="TrueTypeOpcodeTable"/>
/// <seealso cref="TrueTypeInstruction"/>
/// <seealso cref="TrueTypeProgram"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/tt_instructions">OpenType specification: TrueType instruction set</seealso>
public enum TrueTypeOpcode : ushort
{
    /// <summary>Sentinel for an opcode byte that does not map to any defined instruction.</summary>
    /// <value>The value <c>0xFFFF</c>, which cannot collide with any valid opcode byte.</value>
    /// <remarks>Returned by <see cref="TrueTypeOpcodeTable.Decode(byte)"/> for the four reserved gaps in the single-byte range.</remarks>
    /// <seealso cref="TrueTypeOpcodeTable.Decode(byte)"/>
    Unknown = 0xFFFF,

    // ── 0x00–0x2D: single-byte named instructions ──

    /// <summary>0x00: SVTCA[0], set both freedom and projection vectors to the Y axis.</summary>
    SvtcaY = 0x00,
    /// <summary>0x01: SVTCA[1], set both freedom and projection vectors to the X axis.</summary>
    SvtcaX = 0x01,
    /// <summary>0x02: SPVTCA[0], set the projection vector to the Y axis.</summary>
    SpvtcaY = 0x02,
    /// <summary>0x03: SPVTCA[1], set the projection vector to the X axis.</summary>
    SpvtcaX = 0x03,
    /// <summary>0x04: SFVTCA[0], set the freedom vector to the Y axis.</summary>
    SfvtcaY = 0x04,
    /// <summary>0x05: SFVTCA[1], set the freedom vector to the X axis.</summary>
    SfvtcaX = 0x05,
    /// <summary>0x06: SPVTL[0], set the projection vector parallel to a line.</summary>
    SpvtlParallel = 0x06,
    /// <summary>0x07: SPVTL[1], set the projection vector perpendicular to a line.</summary>
    SpvtlPerpendicular = 0x07,
    /// <summary>0x08: SFVTL[0], set the freedom vector parallel to a line.</summary>
    SfvtlParallel = 0x08,
    /// <summary>0x09: SFVTL[1], set the freedom vector perpendicular to a line.</summary>
    SfvtlPerpendicular = 0x09,
    /// <summary>0x0A: SPVFS, set the projection vector from the stack.</summary>
    Spvfs = 0x0A,
    /// <summary>0x0B: SFVFS, set the freedom vector from the stack.</summary>
    Sfvfs = 0x0B,
    /// <summary>0x0C: GPV, get the projection vector onto the stack.</summary>
    Gpv = 0x0C,
    /// <summary>0x0D: GFV, get the freedom vector onto the stack.</summary>
    Gfv = 0x0D,
    /// <summary>0x0E: SFVTPV, set the freedom vector to the projection vector.</summary>
    Sfvtpv = 0x0E,
    /// <summary>0x0F: ISECT, move a point to the intersection of two lines.</summary>
    Isect = 0x0F,
    /// <summary>0x10: SRP0, set reference point 0.</summary>
    Srp0 = 0x10,
    /// <summary>0x11: SRP1, set reference point 1.</summary>
    Srp1 = 0x11,
    /// <summary>0x12: SRP2, set reference point 2.</summary>
    Srp2 = 0x12,
    /// <summary>0x13: SZP0, set zone pointer 0.</summary>
    Szp0 = 0x13,
    /// <summary>0x14: SZP1, set zone pointer 1.</summary>
    Szp1 = 0x14,
    /// <summary>0x15: SZP2, set zone pointer 2.</summary>
    Szp2 = 0x15,
    /// <summary>0x16: SZPS, set all zone pointers.</summary>
    Szps = 0x16,
    /// <summary>0x17: SLOOP, set the loop variable.</summary>
    Sloop = 0x17,
    /// <summary>0x18: RTG, round to grid.</summary>
    Rtg = 0x18,
    /// <summary>0x19: RTHG, round to half grid.</summary>
    Rthg = 0x19,
    /// <summary>0x1A: SMD, set minimum distance.</summary>
    Smd = 0x1A,
    /// <summary>0x1B: ELSE, else clause of an IF.</summary>
    Else = 0x1B,
    /// <summary>0x1C: JMPR, jump relative.</summary>
    Jmpr = 0x1C,
    /// <summary>0x1D: SCVTCI, set CVT cut-in.</summary>
    Scvtci = 0x1D,
    /// <summary>0x1E: SSWCI, set single width cut-in.</summary>
    Sswci = 0x1E,
    /// <summary>0x1F: SSW, set single width.</summary>
    Ssw = 0x1F,
    /// <summary>0x20: DUP, duplicate the top stack element.</summary>
    Dup = 0x20,
    /// <summary>0x21: POP, pop the top stack element.</summary>
    Pop = 0x21,
    /// <summary>0x22: CLEAR, clear the stack.</summary>
    Clear = 0x22,
    /// <summary>0x23: SWAP, swap the top two stack elements.</summary>
    Swap = 0x23,
    /// <summary>0x24: DEPTH, return stack depth.</summary>
    Depth = 0x24,
    /// <summary>0x25: CINDEX, copy the indexed element to the top.</summary>
    Cindex = 0x25,
    /// <summary>0x26: MINDEX, move the indexed element to the top.</summary>
    Mindex = 0x26,
    /// <summary>0x27: ALIGNPTS, align two points.</summary>
    Alignpts = 0x27,
    /// <summary>0x29: UTP, untouch a point.</summary>
    Utp = 0x29,
    /// <summary>0x2A: LOOPCALL, call a function in a loop.</summary>
    Loopcall = 0x2A,
    /// <summary>0x2B: CALL, call a function.</summary>
    Call = 0x2B,
    /// <summary>0x2C: FDEF, define a function.</summary>
    Fdef = 0x2C,
    /// <summary>0x2D: ENDF, end a function definition.</summary>
    Endf = 0x2D,

    // ── 0x2E–0x3F: two-state instructions ──

    /// <summary>0x2E: MDAP[0], move direct absolute point without rounding.</summary>
    MdapNoRound = 0x2E,
    /// <summary>0x2F: MDAP[1], move direct absolute point with rounding.</summary>
    MdapRound = 0x2F,
    /// <summary>0x30: IUP[0], interpolate untouched points in Y.</summary>
    IupY = 0x30,
    /// <summary>0x31: IUP[1], interpolate untouched points in X.</summary>
    IupX = 0x31,
    /// <summary>0x32: SHP[0], shift point using rp2.</summary>
    ShpRp2 = 0x32,
    /// <summary>0x33: SHP[1], shift point using rp1.</summary>
    ShpRp1 = 0x33,
    /// <summary>0x34: SHC[0], shift contour using rp2.</summary>
    ShcRp2 = 0x34,
    /// <summary>0x35: SHC[1], shift contour using rp1.</summary>
    ShcRp1 = 0x35,
    /// <summary>0x36: SHZ[0], shift zone using rp2.</summary>
    ShzRp2 = 0x36,
    /// <summary>0x37: SHZ[1], shift zone using rp1.</summary>
    ShzRp1 = 0x37,
    /// <summary>0x38: SHPIX, shift a point by a pixel amount.</summary>
    Shpix = 0x38,
    /// <summary>0x39: IP, interpolate point.</summary>
    Ip = 0x39,
    /// <summary>0x3A: MSIRP[0], move stack indirect relative point without setting rp0.</summary>
    MsirpDoNotSetRp0 = 0x3A,
    /// <summary>0x3B: MSIRP[1], move stack indirect relative point and set rp0.</summary>
    MsirpSetRp0 = 0x3B,
    /// <summary>0x3C: ALIGNRP, align to reference point.</summary>
    Alignrp = 0x3C,
    /// <summary>0x3D: RTDG, round to double grid.</summary>
    Rtdg = 0x3D,
    /// <summary>0x3E: MIAP[0], move indirect absolute point without rounding.</summary>
    MiapNoRound = 0x3E,
    /// <summary>0x3F: MIAP[1], move indirect absolute point with rounding and CVT cut-in.</summary>
    MiapRound = 0x3F,

    // ── 0x40–0x75: general instructions ──

    /// <summary>0x40: NPUSHB, push N bytes onto the stack.</summary>
    NPushB = 0x40,
    /// <summary>0x41: NPUSHW, push N words onto the stack.</summary>
    NPushW = 0x41,
    /// <summary>0x42: WS, write to the storage area.</summary>
    Ws = 0x42,
    /// <summary>0x43: RS, read from the storage area.</summary>
    Rs = 0x43,
    /// <summary>0x44: WCVTP, write a CVT entry in pixel units.</summary>
    Wcvtp = 0x44,
    /// <summary>0x45: RCVT, read a CVT entry.</summary>
    Rcvt = 0x45,
    /// <summary>0x46: GC[0], get coordinate using the current position.</summary>
    GcCurrent = 0x46,
    /// <summary>0x47: GC[1], get coordinate using the original position.</summary>
    GcOriginal = 0x47,
    /// <summary>0x48: SCFS, set coordinate from the stack.</summary>
    Scfs = 0x48,
    /// <summary>0x49: MD[0], measure distance along the projection vector.</summary>
    MdProjection = 0x49,
    /// <summary>0x4A: MD[1], measure distance along the freedom vector.</summary>
    MdFreedom = 0x4A,
    /// <summary>0x4B: MPPEM, measure pixels per EM.</summary>
    Mppem = 0x4B,
    /// <summary>0x4C: MPS, measure point size.</summary>
    Mps = 0x4C,
    /// <summary>0x4D: FLIPON, turn auto-flip on.</summary>
    Flipon = 0x4D,
    /// <summary>0x4E: FLIPOFF, turn auto-flip off.</summary>
    Flipoff = 0x4E,
    /// <summary>0x4F: DEBUG, debug call.</summary>
    Debug = 0x4F,
    /// <summary>0x50: LT, less than.</summary>
    Lt = 0x50,
    /// <summary>0x51: LTEQ, less than or equal.</summary>
    Lteq = 0x51,
    /// <summary>0x52: GT, greater than.</summary>
    Gt = 0x52,
    /// <summary>0x53: GTEQ, greater than or equal.</summary>
    Gteq = 0x53,
    /// <summary>0x54: EQ, equal.</summary>
    Eq = 0x54,
    /// <summary>0x55: NEQ, not equal.</summary>
    Neq = 0x55,
    /// <summary>0x56: ODD, test for odd parity.</summary>
    Odd = 0x56,
    /// <summary>0x57: EVEN, test for even parity.</summary>
    Even = 0x57,
    /// <summary>0x58: IF, conditional test.</summary>
    If = 0x58,
    /// <summary>0x59: EIF, end IF.</summary>
    Eif = 0x59,
    /// <summary>0x5A: AND, logical AND.</summary>
    And = 0x5A,
    /// <summary>0x5B: OR, logical OR.</summary>
    Or = 0x5B,
    /// <summary>0x5C: NOT, logical NOT.</summary>
    Not = 0x5C,
    /// <summary>0x5D: DELTAP1, delta exception P1.</summary>
    Deltap1 = 0x5D,
    /// <summary>0x5E: DELTAP2, delta exception P2.</summary>
    Deltap2 = 0x5E,
    /// <summary>0x5F: DELTAP3, delta exception P3.</summary>
    Deltap3 = 0x5F,
    /// <summary>0x60: DELTAC1, delta exception C1.</summary>
    Deltac1 = 0x60,
    /// <summary>0x61: DELTAC2, delta exception C2.</summary>
    Deltac2 = 0x61,
    /// <summary>0x62: DELTAC3, delta exception C3.</summary>
    Deltac3 = 0x62,
    /// <summary>0x63: SDB, set delta base.</summary>
    Sdb = 0x63,
    /// <summary>0x64: SDS, set delta shift.</summary>
    Sds = 0x64,
    /// <summary>0x65: ADD, add.</summary>
    Add = 0x65,
    /// <summary>0x66: SUB, subtract.</summary>
    Sub = 0x66,
    /// <summary>0x67: DIV, divide.</summary>
    Div = 0x67,
    /// <summary>0x68: MUL, multiply.</summary>
    Mul = 0x68,
    /// <summary>0x69: ABS, absolute value.</summary>
    Abs = 0x69,
    /// <summary>0x6A: NEG, negate.</summary>
    Neg = 0x6A,
    /// <summary>0x6B: FLOOR, floor.</summary>
    Floor = 0x6B,
    /// <summary>0x6C: CEILING, ceiling.</summary>
    Ceiling = 0x6C,
    /// <summary>0x6D: ROUND[ab], round using both flags from the stack.</summary>
    RoundAb = 0x6D,
    /// <summary>0x6E: ROUND[01].</summary>
    Round01 = 0x6E,
    /// <summary>0x6F: ROUND[00].</summary>
    Round00 = 0x6F,
    /// <summary>0x70: ROUND[11].</summary>
    Round11 = 0x70,
    /// <summary>0x71: NROUND[ab], no-round using both flags from the stack.</summary>
    NroundAb = 0x71,
    /// <summary>0x72: NROUND[01].</summary>
    Nround01 = 0x72,
    /// <summary>0x73: NROUND[00].</summary>
    Nround00 = 0x73,
    /// <summary>0x74: NROUND[11].</summary>
    Nround11 = 0x74,
    /// <summary>0x75: WCVTF, write a CVT entry in FUnits.</summary>
    Wcvtf = 0x75,

    // ── 0x76–0x8E: extended instructions ──

    /// <summary>0x76: SROUND, super round.</summary>
    Sround = 0x76,
    /// <summary>0x77: S45ROUND, super round 45 degrees.</summary>
    S45round = 0x77,
    /// <summary>0x78: JROT, jump relative on true.</summary>
    Jrot = 0x78,
    /// <summary>0x79: JROF, jump relative on false.</summary>
    Jrof = 0x79,
    /// <summary>0x7A: ROFF, round off.</summary>
    Roff = 0x7A,
    /// <summary>0x7C: RUTG, round up to grid.</summary>
    Rutg = 0x7C,
    /// <summary>0x7D: RDTG, round down to grid.</summary>
    Rdtg = 0x7D,
    /// <summary>0x7E: SANGW, set angle weight.</summary>
    Sangw = 0x7E,
    /// <summary>0x7F: AA, adjust angle.</summary>
    Aa = 0x7F,
    /// <summary>0x80: FLIPPT, flip point.</summary>
    Flippt = 0x80,
    /// <summary>0x81: FLIPRGON, flip range on.</summary>
    Fliprgon = 0x81,
    /// <summary>0x82: FLIPRGOFF, flip range off.</summary>
    Fliprgoff = 0x82,
    /// <summary>0x85: SCANCTRL, scan control.</summary>
    Scanctrl = 0x85,
    /// <summary>0x86: SDPVTL[0], set dual projection vector to line (parallel).</summary>
    SdpvtlParallel = 0x86,
    /// <summary>0x87: SDPVTL[1], set dual projection vector to line (perpendicular).</summary>
    SdpvtlPerpendicular = 0x87,
    /// <summary>0x88: GETINFO, get information.</summary>
    Getinfo = 0x88,
    /// <summary>0x89: IDEF, define an instruction.</summary>
    Idef = 0x89,
    /// <summary>0x8A: ROLL, roll top 3 stack elements.</summary>
    Roll = 0x8A,
    /// <summary>0x8B: MAX, maximum.</summary>
    Max = 0x8B,
    /// <summary>0x8C: MIN, minimum.</summary>
    Min = 0x8C,
    /// <summary>0x8D: SCANTYPE, scan type.</summary>
    Scantype = 0x8D,
    /// <summary>0x8E: INSTCTRL, instruction control.</summary>
    Instctrl = 0x8E,

    // ── 0x8F–0x92: variable-font and data instructions ──

    /// <summary>0x91: GETVARIATION, get normalized variation (blend) coordinates.</summary>
    Getvariation = 0x91,
    /// <summary>0x92: GETDATA, get data.</summary>
    Getdata = 0x92,

    // ── 0xB0–0xBF: push with variant in the low bits ──

    /// <summary>0xB0–0xB7: PUSHB[0–7], push 1–8 bytes.</summary>
    PushB = 0xB0,
    /// <summary>0xB8–0xBF: PUSHW[0–7], push 1–8 words.</summary>
    PushW = 0xB8,

    // ── 0xC0–0xFF: MDRP and MIRP with flag bits in the low 5 bits ──

    /// <summary>0xC0–0xDF: MDRP[0–31], move direct relative point.</summary>
    Mdrp = 0xC0,
    /// <summary>0xE0–0xFF: MIRP[0–31], move indirect relative point.</summary>
    Mirp = 0xE0,
}

/// <summary>Decodes raw TrueType opcode bytes into base opcodes and variants.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The decoder is a pure function of the opcode byte: it does not consult the program or any external state.</description></item>
/// <item><description>Every byte in 0x00–0x92 that is not in one of four reserved gaps maps to a named enum member; the four reserved bytes and every byte in 0x93–0xAF map to <see cref="TrueTypeOpcode.Unknown"/> with variant zero.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/tt_instructions">TrueType instruction set</see> appendix in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="TrueTypeOpcode"/>
/// <seealso cref="TrueTypeInstruction"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/tt_instructions">OpenType specification: TrueType instruction set</seealso>
public static class TrueTypeOpcodeTable
{
    /// <summary>Decodes a raw opcode byte into its base mnemonic and variant index.</summary>
    /// <param name="raw">The raw opcode byte.</param>
    /// <returns>The resolved mnemonic and, for parameterized opcodes, the low-bit variant (0–7 for PUSHB and PUSHW, 0–31 for MDRP and MIRP, 0 otherwise).</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The variant ranges are checked first: 0xB0–0xB7 (PUSHB), 0xB8–0xBF (PUSHW), 0xC0–0xDF (MDRP), 0xE0–0xFF (MIRP).</description></item>
    /// <item><description>The remaining bytes in 0x00–0x92 map to named members, except for four reserved gaps (0x28, 0x7B, 0x83–0x84, 0x8F–0x90) which return <see cref="TrueTypeOpcode.Unknown"/>.</description></item>
    /// <item><description>The method never throws; unrecognised bytes are reported as <see cref="TrueTypeOpcode.Unknown"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var (opcode, variant) = TrueTypeOpcodeTable.Decode(0xB3);
    /// // opcode == TrueTypeOpcode.PushB, variant == 3, meaning four bytes follow.
    /// </code>
    /// </example>
    /// <seealso cref="TrueTypeOpcode"/>
    /// <seealso cref="TrueTypeInstruction.Variant"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/tt_instructions">OpenType specification: TrueType instruction set</seealso>
    public static (TrueTypeOpcode Opcode, int Variant) Decode(byte raw)
    {
        // Variant ranges first.
        if (raw >= 0xB0 && raw <= 0xB7) return (TrueTypeOpcode.PushB, raw - 0xB0);
        if (raw >= 0xB8 && raw <= 0xBF) return (TrueTypeOpcode.PushW, raw - 0xB8);
        if (raw >= 0xC0 && raw <= 0xDF) return (TrueTypeOpcode.Mdrp, raw - 0xC0);
        if (raw >= 0xE0) return (TrueTypeOpcode.Mirp, raw - 0xE0);

        // Every byte in 0x00–0x92 outside the four reserved gaps is a named member.
        if (raw is 0x28 or 0x7B or (>= 0x83 and <= 0x84) or (>= 0x8F and <= 0x90) or >= 0x93)
            return (TrueTypeOpcode.Unknown, 0);

        return ((TrueTypeOpcode)raw, 0);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Program
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A single decoded TrueType instruction. <see cref="RawOpcode"/> is the exact byte from the program; <see cref="Opcode"/> is the resolved mnemonic; <see cref="Variant"/> carries the low bits for parameterized opcodes.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The struct is a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>Inline push data is stored on the instruction itself, so iterating a <see cref="TrueTypeProgram"/> yields a flat sequence where push instructions carry their payload rather than the payload appearing as separate entries.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/tt_instructions">TrueType instruction set</see> appendix in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="TrueTypeOpcode"/>
/// <seealso cref="TrueTypeProgram"/>
/// <seealso cref="TrueTypeOpcodeTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/tt_instructions">OpenType specification: TrueType instruction set</seealso>
public record struct TrueTypeInstruction
{
    /// <summary>Gets the exact opcode byte.</summary>
    /// <value>The byte as it appeared in the program, before decoding.</value>
    /// <seealso cref="Opcode"/>
    /// <seealso cref="Variant"/>
    public byte RawOpcode;

    /// <summary>Gets the resolved mnemonic.</summary>
    /// <value>The <see cref="TrueTypeOpcode"/> value returned by <see cref="TrueTypeOpcodeTable.Decode(byte)"/> for <see cref="RawOpcode"/>.</value>
    /// <seealso cref="RawOpcode"/>
    /// <seealso cref="Variant"/>
    /// <seealso cref="TrueTypeOpcode"/>
    public TrueTypeOpcode Opcode;

    /// <summary>Gets the variant index for parameterized opcodes: 0–7 for <see cref="TrueTypeOpcode.PushB"/> and <see cref="TrueTypeOpcode.PushW"/>, 0–31 for <see cref="TrueTypeOpcode.Mdrp"/> and <see cref="TrueTypeOpcode.Mirp"/>, 0 otherwise.</summary>
    /// <value>The low bits of <see cref="RawOpcode"/> for parameterized opcodes; zero for all others.</value>
    /// <seealso cref="RawOpcode"/>
    /// <seealso cref="Opcode"/>
    public int Variant;

    /// <summary>Gets the inline push data. Empty for non-push instructions.</summary>
    /// <value>For a PUSHB or NPUSHB instruction, one entry per pushed byte, zero-extended to <see cref="int"/>. For a PUSHW or NPUSHW instruction, one entry per pushed word, sign-extended to <see cref="int"/>. Empty for all other opcodes.</value>
    /// <remarks>The array is shared with the enclosing <see cref="TrueTypeProgram"/>; callers must not mutate it.</remarks>
    /// <seealso cref="IsPush"/>
    /// <seealso cref="TrueTypeProgram"/>
    public int[] InlineData;

    /// <summary>True when this instruction carries inline push data.</summary>
    /// <value><see langword="true"/> when <see cref="InlineData"/> has at least one element.</value>
    /// <seealso cref="InlineData"/>
    public readonly bool IsPush => InlineData.Length > 0;

    /// <inheritdoc/>
    /// <returns>For a push instruction, the mnemonic followed by the comma-separated inline values in brackets; for any other instruction, the mnemonic alone.</returns>
    /// <remarks>The format is intended for diagnostic and debugging output; it is not a stable serialisation format.</remarks>
    /// <seealso cref="IsPush"/>
    /// <seealso cref="InlineData"/>
    public override string ToString() =>
        IsPush ? $"{Opcode} [{string.Join(", ", InlineData)}]" : Opcode.ToString();
}

/// <summary>A decoded TrueType bytecode program, as found in <c>fpgm</c>, <c>prep</c>, and the instruction section of a <c>glyf</c> glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The decoder performs a structural expansion only: push instructions are followed and their inline data extracted, and every other opcode is recorded with its resolved mnemonic and variant. No stack or control flow is simulated; semantic execution belongs to a consumer that understands the bytecode's runtime model.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/tt_instructions">TrueType instruction set</see> appendix in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="TrueTypeInstruction"/>
/// <seealso cref="TrueTypeOpcode"/>
/// <seealso cref="TrueTypeProgramContext"/>
/// <seealso cref="FpgmTable"/>
/// <seealso cref="PrepTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/tt_instructions">OpenType specification: TrueType instruction set</seealso>
public sealed record TrueTypeProgram : IRecord<TrueTypeProgram>
{
    /// <summary>Gets the decoded instruction sequence.</summary>
    /// <value>The flat list of <see cref="TrueTypeInstruction"/> entries. Push instructions carry their inline data on the instruction itself; the payload does not appear as separate entries.</value>
    /// <seealso cref="Count"/>
    /// <seealso cref="TrueTypeInstruction"/>
    public IReadOnlyList<TrueTypeInstruction> Instructions { get; init; } = [];

    /// <summary>Gets the number of instructions.</summary>
    /// <value>The size of the <see cref="Instructions"/> list.</value>
    /// <seealso cref="Instructions"/>
    public int Count => Instructions.Count;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the program.</param>
    /// <param name="context">A <see cref="TrueTypeProgramContext"/> carrying the program's byte length.</param>
    /// <returns>The decoded program.</returns>
    /// <exception cref="EndOfStreamException">The program extends past the end of the cursor's source.</exception>
    /// <exception cref="InvalidDataException">A push instruction's inline data is truncated.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The program bytes are buffered into a stack-allocated span for lengths up to 512 bytes, or a heap-allocated array above that threshold; the buffer is then handed to <see cref="Decode(ReadOnlySpan{byte})"/>.</description></item>
    /// <item><description>The length is supplied through <see cref="TrueTypeProgramContext.Length"/>; the source of that length depends on the caller — a size limit in <c>maxp</c>, an instruction-length prefix in a glyph, or the length of the enclosing table.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/tt_instructions">TrueType instruction set</see> appendix in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="TrueTypeProgramContext"/>
    /// <seealso cref="Decode(ReadOnlySpan{byte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/tt_instructions">OpenType specification: TrueType instruction set</seealso>
    static TrueTypeProgram IRecord<TrueTypeProgram>.Parse(ref Cursor cursor, object? context)
    {
        TrueTypeProgramContext ctx = (TrueTypeProgramContext)context!;
        Span<byte> buffer = ctx.Length <= 512
            ? stackalloc byte[ctx.Length]
            : new byte[ctx.Length];
        cursor.ReadBytes(buffer);
        return Decode(buffer);
    }

    /// <summary>Decodes a program from a byte sequence.</summary>
    /// <param name="bytes">The raw bytecode.</param>
    /// <returns>The decoded program.</returns>
    /// <exception cref="InvalidDataException">A push instruction's inline data extends past the end of the program.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Each opcode byte is decoded through <see cref="TrueTypeOpcodeTable.Decode(byte)"/>; push instructions consume their payload from the bytes that follow.</description></item>
    /// <item><description>PUSHB and NPUSHB payloads are zero-extended to <see cref="int"/>; PUSHW and NPUSHW payloads are sign-extended through <see cref="short"/>.</description></item>
    /// <item><description>The method never simulates stack or control flow; it is a pure structural decode.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/tt_instructions">TrueType instruction set</see> appendix in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// byte[] bytecode = /* raw program bytes */;
    /// TrueTypeProgram program = TrueTypeProgram.Decode(bytecode);
    /// foreach (var instr in program.Instructions)
    ///     Console.WriteLine(instr);
    /// </code>
    /// </example>
    /// <seealso cref="Instructions"/>
    /// <seealso cref="TrueTypeInstruction"/>
    /// <seealso cref="TrueTypeOpcodeTable.Decode(byte)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/tt_instructions">OpenType specification: TrueType instruction set</seealso>
    public static TrueTypeProgram Decode(ReadOnlySpan<byte> bytes)
    {
        List<TrueTypeInstruction> instructions = new(bytes.Length);
        int pos = 0;

        while (pos < bytes.Length)
        {
            byte opcode = bytes[pos++];
            (TrueTypeOpcode baseOpcode, int variant) = TrueTypeOpcodeTable.Decode(opcode);

            int[] data = [];
            switch (baseOpcode)
            {
                case TrueTypeOpcode.NPushB:
                    {
                        if (pos >= bytes.Length)
                            throw new InvalidDataException("Truncated NPUSHB instruction.");
                        int n = bytes[pos++];
                        if (pos + n > bytes.Length)
                            throw new InvalidDataException("Truncated NPUSHB payload.");
                        data = new int[n];
                        for (int i = 0; i < n; i++) data[i] = bytes[pos++];
                        break;
                    }
                case TrueTypeOpcode.NPushW:
                    {
                        if (pos >= bytes.Length)
                            throw new InvalidDataException("Truncated NPUSHW instruction.");
                        int n = bytes[pos++];
                        if (pos + n * 2 > bytes.Length)
                            throw new InvalidDataException("Truncated NPUSHW payload.");
                        data = new int[n];
                        for (int i = 0; i < n; i++)
                        {
                            data[i] = (short)((bytes[pos] << 8) | bytes[pos + 1]);
                            pos += 2;
                        }
                        break;
                    }
                case TrueTypeOpcode.PushB:
                    {
                        int n = variant + 1;
                        if (pos + n > bytes.Length)
                            throw new InvalidDataException("Truncated PUSHB payload.");
                        data = new int[n];
                        for (int i = 0; i < n; i++) data[i] = bytes[pos++];
                        break;
                    }
                case TrueTypeOpcode.PushW:
                    {
                        int n = variant + 1;
                        if (pos + n * 2 > bytes.Length)
                            throw new InvalidDataException("Truncated PUSHW payload.");
                        data = new int[n];
                        for (int i = 0; i < n; i++)
                        {
                            data[i] = (short)((bytes[pos] << 8) | bytes[pos + 1]);
                            pos += 2;
                        }
                        break;
                    }
            }

            instructions.Add(new TrueTypeInstruction
            {
                RawOpcode = opcode,
                Opcode = baseOpcode,
                Variant = variant,
                InlineData = data,
            });
        }

        return new() { Instructions = instructions };
    }
}

/// <summary>Context for parsing a TrueType program from a cursor. Carries the program's byte length, which comes from a separate field — a size limit in <c>maxp</c>, an instruction-length prefix in a glyph, or the length of the enclosing table.</summary>
/// <param name="Length">The number of bytes the program occupies.</param>
/// <remarks>
/// <list type="bullet">
/// <item><description>The context is a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>The length has no fixed source: <see cref="FpgmTable"/> and <see cref="PrepTable"/> pass the table's byte length, while a <c>glyf</c> glyph passes the value of the instruction-length prefix that precedes the program.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="TrueTypeProgram"/>
/// <seealso cref="IRecord{T}.Parse"/>
/// <seealso cref="FpgmTable"/>
/// <seealso cref="PrepTable"/>
public record TrueTypeProgramContext(int Length);
