using Mubarrat.Fonts.OpenType.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.OpenType.Primitives;

/// <summary>A 24-bit signed integer in two's complement, stored in the host's native byte order — the same convention <see cref="short"/>, <see cref="int"/>, and <see cref="long"/> use. Blittable, size 3, no padding.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The value's in-memory layout depends on <see cref="BitConverter.IsLittleEndian"/>. On a little-endian host, byte 0 is the least significant byte; on a big-endian host, byte 0 is the most significant. The <see cref="Value"/> property and all arithmetic use <see cref="int"/> internally, so the numeric result is identical on every host.</description></item>
/// <item><description>OpenType font data is big-endian on disk. Use <see cref="ReadBigEndian(ReadOnlySpan{byte})"/> and <see cref="WriteBigEndian(Span{byte})"/> to convert between on-disk bytes and native values; they perform the byte swap only when <see cref="BitConverter.IsLittleEndian"/> is <c>true</c>.</description></item>
/// <item><description>The sign bit is bit 23. Conversion to <see cref="int"/> sign-extends; all arithmetic preserves the 24-bit range via truncation (unchecked) or overflow checking (checked).</description></item>
/// </list>
/// <para>For the on-disk <c>int24</c> data type that this struct models, see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification, Data Types</see>.</para>
/// </remarks>
/// <example>
/// <code>
/// var a = new Int24(0x7FFFFF);          // MaxValue
/// var b = Int24.FromBigEndianBytes(0x80, 0x00, 0x00);  // MinValue
/// int value = a;                        // implicit widening to int
/// </code>
/// </example>
/// <seealso cref="UInt24"/>
/// <seealso cref="Fixed"/>
/// <seealso cref="F2Dot14"/>
/// <seealso cref="Tag"/>
/// <seealso cref="IBigEndianStruct{T}"/>
/// <seealso cref="Source.ReadUInt24At(long)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types">C# reference: Integral numeric types</seealso>
[StructLayout(LayoutKind.Sequential, Size = 3, Pack = 1)]
public readonly struct Int24 :
    IComparable,
    IComparable<Int24>,
    IEquatable<Int24>,
    IFormattable,
    ISpanFormattable
{
    // ────────────────────────────── Constants ──────────────────────────────

    /// <summary>The number of bytes in an <see cref="Int24"/> value.</summary>
    /// <remarks>Always <c>3</c>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="MinValue"/>
    /// <seealso cref="MaxValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public const int SizeInBytes = 3;

    /// <summary>The minimum value: -8 388 608 (-0x800000).</summary>
    /// <value>The <see cref="Int24"/> whose raw 24-bit pattern is <c>0x800000</c>.</value>
    /// <seealso cref="MaxValue"/>
    /// <seealso cref="Checked(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public static readonly Int24 MinValue = new(-0x800000);

    /// <summary>The maximum value: 8 388 607 (0x7FFFFF).</summary>
    /// <value>The <see cref="Int24"/> whose raw 24-bit pattern is <c>0x7FFFFF</c>.</value>
    /// <seealso cref="MinValue"/>
    /// <seealso cref="Checked(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public static readonly Int24 MaxValue = new(0x7FFFFF);

    // ────────────────────────────── Storage ──────────────────────────────

    // On a little-endian host: _b0 = LSB, _b1 = middle, _b2 = MSB.
    // On a big-endian host:    _b0 = MSB, _b1 = middle, _b2 = LSB.
    private readonly byte _b0;
    private readonly byte _b1;
    private readonly byte _b2;

    // ────────────────────────────── Construction ──────────────────────────────

    /// <summary>Creates a value from three raw bytes in native byte order.</summary>
    /// <param name="b0">Byte at offset 0 in the native layout.</param>
    /// <param name="b1">Byte at offset 1 in the native layout.</param>
    /// <param name="b2">Byte at offset 2 in the native layout.</param>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The interpretation of the three arguments depends on the host's endianness; on little-endian, <paramref name="b0"/> is the least significant byte, and on big-endian it is the most significant.</description></item>
    /// <item><description>To construct from a fixed byte order regardless of host, use <see cref="FromBigEndianBytes(byte, byte, byte)"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Int24(int)"/>
    /// <seealso cref="FromBigEndianBytes(byte, byte, byte)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Int24(byte b0, byte b1, byte b2)
    {
        _b0 = b0;
        _b1 = b1;
        _b2 = b2;
    }

    /// <summary>Creates a value from an <see cref="int"/>, truncating to the low 24 bits.</summary>
    /// <param name="value">The value to narrow. Bits 24–31 are discarded.</param>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Truncation is by low-bit masking, not by clamping: <c>0x01000000</c> becomes <c>0</c> and <c>0xFFFFFFFF</c> becomes <c>-1</c>.</description></item>
    /// <item><description>To reject out-of-range values, use <see cref="Checked(int)"/> or <see cref="MaxValue"/>/<see cref="MinValue"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int24 data type</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Checked(int)"/>
    /// <seealso cref="Int24(byte, byte, byte)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Int24(int value)
    {
        if (BitConverter.IsLittleEndian)
        {
            _b0 = (byte)value;
            _b1 = (byte)(value >> 8);
            _b2 = (byte)(value >> 16);
        }
        else
        {
            _b0 = (byte)(value >> 16);
            _b1 = (byte)(value >> 8);
            _b2 = (byte)value;
        }
    }

    // ────────────────────────────── Access ──────────────────────────────

    /// <summary>Gets the raw byte at offset 0 in the native layout.</summary>
    /// <value>The byte at offset 0 of the in-memory representation. On little-endian hosts this is the least significant byte; on big-endian it is the most significant.</value>
    /// <seealso cref="Byte1"/>
    /// <seealso cref="Byte2"/>
    /// <seealso cref="Value"/>
    public byte Byte0 => _b0;

    /// <summary>Gets the raw byte at offset 1 in the native layout.</summary>
    /// <value>The byte at offset 1 of the in-memory representation.</value>
    /// <seealso cref="Byte0"/>
    /// <seealso cref="Byte2"/>
    /// <seealso cref="Value"/>
    public byte Byte1 => _b1;

    /// <summary>Gets the raw byte at offset 2 in the native layout.</summary>
    /// <value>The byte at offset 2 of the in-memory representation. On little-endian hosts this is the most significant byte; on big-endian it is the least significant.</value>
    /// <seealso cref="Byte0"/>
    /// <seealso cref="Byte1"/>
    /// <seealso cref="Value"/>
    public byte Byte2 => _b2;

    /// <summary>Gets the numeric value as an <see cref="int"/>. Bits 24–31 are filled with a copy of bit 23 (the sign bit).</summary>
    /// <value>The signed 24-bit value sign-extended to 32 bits. Range is [-8 388 608, 8 388 607].</value>
    /// <remarks>Sign extension is performed with an arithmetic left-then-right shift pair: <c>(v &lt;&lt; 8) &gt;&gt; 8</c>. The intermediate left shift is defined behavior in unchecked context and is not relied on for its overflow semantics.</remarks>
    /// <seealso cref="Byte0"/>
    /// <seealso cref="Byte1"/>
    /// <seealso cref="Byte2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public int Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            int v = BitConverter.IsLittleEndian
                ? _b0 | (_b1 << 8) | (_b2 << 16)
                : (_b0 << 16) | (_b1 << 8) | _b2;
            return (v << 8) >> 8;   // arithmetic shift: sign-extend bit 23 into bits 31..24
        }
    }

    // ────────────────────────────── Big-endian I/O ──────────────────────────────

    /// <summary>Reads a big-endian <see cref="Int24"/> from the start of <paramref name="source"/>.</summary>
    /// <param name="source">The source bytes. Must contain at least three bytes.</param>
    /// <returns>The value whose on-disk representation is the first three bytes of <paramref name="source"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is shorter than 3 bytes.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadBigEndian(ReadOnlySpan{byte}, int)"/>
    /// <seealso cref="TryReadBigEndian(ReadOnlySpan{byte}, out Int24)"/>
    /// <seealso cref="FromBigEndianBytes(byte, byte, byte)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int24 ReadBigEndian(ReadOnlySpan<byte> source)
    {
        if (source.Length < SizeInBytes)
            throw new ArgumentOutOfRangeException(nameof(source), source.Length,
                $"Source must be at least {SizeInBytes} bytes.");
        return FromBigEndianBytes(source[0], source[1], source[2]);
    }

    /// <summary>Reads a big-endian <see cref="Int24"/> at <paramref name="offset"/> in <paramref name="source"/>.</summary>
    /// <param name="source">The source bytes.</param>
    /// <param name="offset">The byte offset of the first of the three bytes.</param>
    /// <returns>The value whose on-disk representation is <c>source[offset..offset+3]</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The read extends past the end of <paramref name="source"/>.</exception>
    /// <remarks>The offset check is performed with unsigned arithmetic so negative offsets are rejected by the same comparison that rejects over-large ones.</remarks>
    /// <seealso cref="ReadBigEndian(ReadOnlySpan{byte})"/>
    /// <seealso cref="TryReadBigEndian(ReadOnlySpan{byte}, int, out Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int24 ReadBigEndian(ReadOnlySpan<byte> source, int offset)
    {
        if ((uint)offset > (uint)(source.Length - SizeInBytes))
            throw new ArgumentOutOfRangeException(nameof(offset), offset,
                $"Offset must allow reading {SizeInBytes} bytes from a {source.Length}-byte span.");
        return FromBigEndianBytes(source[offset], source[offset + 1], source[offset + 2]);
    }

    /// <summary>Attempts to read a big-endian <see cref="Int24"/> from the start of <paramref name="source"/>.</summary>
    /// <param name="source">The source bytes.</param>
    /// <param name="value">When this method returns, the value read, or <c>default</c> when the source is too short.</param>
    /// <returns><see langword="true"/> when <paramref name="source"/> contained at least three bytes; otherwise <see langword="false"/>.</returns>
    /// <remarks>Never throws. The <see langword="false"/> return corresponds to a source that is too short to contain the value.</remarks>
    /// <seealso cref="ReadBigEndian(ReadOnlySpan{byte})"/>
    /// <seealso cref="TryReadBigEndian(ReadOnlySpan{byte}, int, out Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out Int24 value)
    {
        if (source.Length < SizeInBytes) { value = default; return false; }
        value = FromBigEndianBytes(source[0], source[1], source[2]);
        return true;
    }

    /// <summary>Attempts to read a big-endian <see cref="Int24"/> at <paramref name="offset"/> in <paramref name="source"/>.</summary>
    /// <param name="source">The source bytes.</param>
    /// <param name="offset">The byte offset of the first of the three bytes.</param>
    /// <param name="value">When this method returns, the value read, or <c>default</c> when the range is out of bounds.</param>
    /// <returns><see langword="true"/> when the read succeeded; otherwise <see langword="false"/>.</returns>
    /// <remarks>Never throws. The offset and length are combined into a single unsigned comparison, so both negative offsets and over-runs are covered.</remarks>
    /// <seealso cref="ReadBigEndian(ReadOnlySpan{byte}, int)"/>
    /// <seealso cref="TryReadBigEndian(ReadOnlySpan{byte}, out Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, int offset, out Int24 value)
    {
        if ((uint)offset > (uint)(source.Length - SizeInBytes)) { value = default; return false; }
        value = FromBigEndianBytes(source[offset], source[offset + 1], source[offset + 2]);
        return true;
    }

    /// <summary>Writes this value as big-endian bytes at the start of <paramref name="destination"/>.</summary>
    /// <param name="destination">The destination span. Must be at least three bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is shorter than 3 bytes.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="WriteBigEndian(Span{byte}, int)"/>
    /// <seealso cref="ReadBigEndian(ReadOnlySpan{byte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBigEndian(Span<byte> destination)
    {
        if (destination.Length < SizeInBytes)
            throw new ArgumentOutOfRangeException(nameof(destination), destination.Length,
                $"Destination must be at least {SizeInBytes} bytes.");
        int v = Value;
        destination[0] = (byte)(v >> 16);
        destination[1] = (byte)(v >> 8);
        destination[2] = (byte)v;
    }

    /// <summary>Writes this value as big-endian bytes at <paramref name="offset"/> in <paramref name="destination"/>.</summary>
    /// <param name="destination">The destination span.</param>
    /// <param name="offset">The byte offset at which the first of the three bytes is written.</param>
    /// <exception cref="ArgumentOutOfRangeException">The write extends past the end of <paramref name="destination"/>.</exception>
    /// <remarks>The offset check uses unsigned arithmetic, so a negative <paramref name="offset"/> is rejected by the same comparison that catches an over-run.</remarks>
    /// <seealso cref="WriteBigEndian(Span{byte})"/>
    /// <seealso cref="ReadBigEndian(ReadOnlySpan{byte}, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBigEndian(Span<byte> destination, int offset)
    {
        if ((uint)offset > (uint)(destination.Length - SizeInBytes))
            throw new ArgumentOutOfRangeException(nameof(offset), offset,
                $"Offset must allow writing {SizeInBytes} bytes into a {destination.Length}-byte span.");
        int v = Value;
        destination[offset] = (byte)(v >> 16);
        destination[offset + 1] = (byte)(v >> 8);
        destination[offset + 2] = (byte)v;
    }

    /// <inheritdoc/>
    /// <param name="value">The value whose bytes are to be reversed.</param>
    /// <returns>An <see cref="Int24"/> whose three in-memory bytes are in reversed order.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The operation is unconditional. The host-endianness check is applied by the reader (<see cref="Source.ReadBigEndianStructAt{T}(long)"/>), not by this method.</description></item>
    /// <item><description>Symmetric: <c>ReverseEndianness(ReverseEndianness(x)) == x</c> for every <c>x</c>.</description></item>
    /// <item><description>Because this struct stores its bytes natively (unlike <see cref="Fixed"/>), a reverse is a pure byte swap; the numeric <see cref="Value"/> changes to the opposite 24-bit interpretation.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FromBigEndianBytes(byte, byte, byte)"/>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso cref="Source.ReadBigEndianStructAt{T}(long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int24 ReverseEndianness(Int24 value) => new(value._b2, value._b1, value._b0);

    /// <summary>Creates a value from three bytes in big-endian order. The result is identical on every host.</summary>
    /// <param name="msb">The most significant byte.</param>
    /// <param name="mid">The middle byte.</param>
    /// <param name="lsb">The least significant byte.</param>
    /// <returns>An <see cref="Int24"/> whose <see cref="Value"/> equals <c>(msb &lt;&lt; 16) | (mid &lt;&lt; 8) | lsb</c>, sign-extended.</returns>
    /// <remarks>The named parameters eliminate the endianness ambiguity of the three-argument constructor. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadBigEndian(ReadOnlySpan{byte})"/>
    /// <seealso cref="ReverseEndianness(Int24)"/>
    /// <seealso cref="Int24(byte, byte, byte)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int24 FromBigEndianBytes(byte msb, byte mid, byte lsb)
    {
        // Compose the numeric value explicitly big-endian, then construct natively.
        // Bits 0..23 hold the big-endian value; the constructor truncates to the low 24 bits.
        int value = (msb << 16) | (mid << 8) | lsb;
        return new Int24(value);
    }

    // ────────────────────────────── Conversion ──────────────────────────────
    //
    // Implicit when the conversion is lossless for every value of the source type.
    // Explicit when the conversion may truncate, reinterpret the sign, or round.
    //
    //   Narrower types (8-bit):  sbyte, byte
    //   Narrower types (16-bit): short, ushort, char
    //   Same level (24-bit):     Int24, UInt24
    //   Wider types (32-bit):    int, uint, float
    //   Wider types (64-bit):    long, ulong, double
    //   Wider types (128-bit):   decimal

    // ── Implicit: Int24 → wider signed type (lossless) ──

    /// <summary>Widens to <see cref="int"/>. Lossless.</summary>
    /// <param name="value">The value to widen.</param>
    /// <returns>The sign-extended 32-bit value.</returns>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="Value"/>
    /// <seealso cref="implicit operator int(Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static implicit operator int(Int24 value) => value.Value;

    /// <summary>Widens to <see cref="long"/>. Lossless.</summary>
    /// <param name="value">The value to widen.</param>
    /// <returns>The sign-extended 64-bit value.</returns>
    /// <remarks>The widening goes through <see cref="int"/> and then extends the sign again, which is a no-op because the intermediate is already sign-extended to 32 bits.</remarks>
    /// <seealso cref="implicit operator long(Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static implicit operator long(Int24 value) => value.Value;

    /// <summary>Widens to <see cref="float"/>. Every <see cref="Int24"/> is exactly representable.</summary>
    /// <param name="value">The value to widen.</param>
    /// <returns>The value as a 32-bit float.</returns>
    /// <remarks><see cref="float"/> has 24 bits of significand, which is exactly the range of an <see cref="Int24"/>, so the conversion is lossless for every value.</remarks>
    /// <seealso cref="implicit operator float(Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types">C# reference: Floating-point numeric types</seealso>
    public static implicit operator float(Int24 value) => value.Value;

    /// <summary>Widens to <see cref="double"/>. Lossless.</summary>
    /// <param name="value">The value to widen.</param>
    /// <returns>The value as a 64-bit float.</returns>
    /// <seealso cref="implicit operator double(Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types">C# reference: Floating-point numeric types</seealso>
    public static implicit operator double(Int24 value) => value.Value;

    /// <summary>Widens to <see cref="decimal"/>. Lossless.</summary>
    /// <param name="value">The value to widen.</param>
    /// <returns>The value as a 128-bit decimal.</returns>
    /// <seealso cref="implicit operator decimal(Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types">C# reference: Floating-point numeric types</seealso>
    public static implicit operator decimal(Int24 value) => value.Value;

    // ── Implicit: narrower type → Int24 (lossless) ──

    /// <summary>Widens from <see cref="sbyte"/>. Always fits.</summary>
    /// <param name="value">The value to widen.</param>
    /// <returns>The value as an <see cref="Int24"/>, sign-extended to 24 bits.</returns>
    /// <seealso cref="Int24(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static implicit operator Int24(sbyte value) => new(value);

    /// <summary>Widens from <see cref="byte"/>. Always fits.</summary>
    /// <param name="value">The value to widen.</param>
    /// <returns>The value as an <see cref="Int24"/>.</returns>
    /// <seealso cref="Int24(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static implicit operator Int24(byte value) => new(value);

    /// <summary>Widens from <see cref="short"/>. Always fits.</summary>
    /// <param name="value">The value to widen.</param>
    /// <returns>The value as an <see cref="Int24"/>.</returns>
    /// <seealso cref="Int24(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static implicit operator Int24(short value) => new(value);

    /// <summary>Widens from <see cref="ushort"/>. Always fits.</summary>
    /// <param name="value">The value to widen.</param>
    /// <returns>The value as an <see cref="Int24"/>.</returns>
    /// <seealso cref="Int24(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static implicit operator Int24(ushort value) => new(value);

    /// <summary>Widens from <see cref="char"/>. Always fits (0..65535).</summary>
    /// <param name="value">The value to widen.</param>
    /// <returns>The value as an <see cref="Int24"/>.</returns>
    /// <seealso cref="Int24(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static implicit operator Int24(char value) => new(value);

    // ── Explicit: wider type → Int24 (narrowing) ──

    /// <summary>Narrows from <see cref="int"/>, truncating to the low 24 bits.</summary>
    /// <param name="value">The value to narrow.</param>
    /// <returns>The low 24 bits of <paramref name="value"/>, interpreted as a signed <see cref="Int24"/>.</returns>
    /// <seealso cref="Int24(int)"/>
    /// <seealso cref="Checked(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator Int24(int value) => new(value);

    /// <summary>Reinterprets the low 24 bits of a <see cref="uint"/>.</summary>
    /// <param name="value">The value to reinterpret.</param>
    /// <returns>The low 24 bits of <paramref name="value"/>, interpreted as a signed <see cref="Int24"/>.</returns>
    /// <remarks>Values in the top 24 bits are discarded; values with bit 23 set become negative.</remarks>
    /// <seealso cref="explicit operator Int24(uint)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator Int24(uint value) => new((int)value);

    /// <summary>Narrows from <see cref="long"/>, truncating to the low 24 bits.</summary>
    /// <param name="value">The value to narrow.</param>
    /// <returns>The low 24 bits of <paramref name="value"/>, interpreted as a signed <see cref="Int24"/>.</returns>
    /// <seealso cref="Checked(long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator Int24(long value) => new((int)value);

    /// <summary>Reinterprets the low 24 bits of a <see cref="ulong"/>.</summary>
    /// <param name="value">The value to reinterpret.</param>
    /// <returns>The low 24 bits of <paramref name="value"/>, interpreted as a signed <see cref="Int24"/>.</returns>
    /// <seealso cref="explicit operator Int24(ulong)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator Int24(ulong value) => new((int)value);

    /// <summary>Truncates toward zero, then narrows to the low 24 bits.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The low 24 bits of the truncated integer value.</returns>
    /// <seealso cref="explicit operator Int24(float)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types">C# reference: Floating-point numeric types</seealso>
    public static explicit operator Int24(float value) => new((int)value);

    /// <summary>Truncates toward zero, then narrows to the low 24 bits.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The low 24 bits of the truncated integer value.</returns>
    /// <seealso cref="explicit operator Int24(float)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types">C# reference: Floating-point numeric types</seealso>
    public static explicit operator Int24(double value) => new((int)value);

    /// <summary>Truncates toward zero, then narrows to the low 24 bits.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The low 24 bits of the truncated integer value.</returns>
    /// <seealso cref="explicit operator Int24(double)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types">C# reference: Floating-point numeric types</seealso>
    public static explicit operator Int24(decimal value) => new((int)value);

    // ── Explicit: Int24 → narrower type (truncation) ──

    /// <summary>Narrows to <see cref="sbyte"/>, truncating to the low 8 bits.</summary>
    /// <param name="value">The value to narrow.</param>
    /// <returns>The low 8 bits of <paramref name="value"/>, interpreted as a signed byte.</returns>
    /// <seealso cref="Value"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator sbyte(Int24 value) => (sbyte)value.Value;

    /// <summary>Narrows to <see cref="byte"/>, truncating to the low 8 bits.</summary>
    /// <param name="value">The value to narrow.</param>
    /// <returns>The low 8 bits of <paramref name="value"/>.</returns>
    /// <seealso cref="Value"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator byte(Int24 value) => (byte)value.Value;

    /// <summary>Narrows to <see cref="short"/>, truncating to the low 16 bits.</summary>
    /// <param name="value">The value to narrow.</param>
    /// <returns>The low 16 bits of <paramref name="value"/>, interpreted as a signed short.</returns>
    /// <seealso cref="Value"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator short(Int24 value) => (short)value.Value;

    /// <summary>Narrows to <see cref="ushort"/>, truncating to the low 16 bits.</summary>
    /// <param name="value">The value to narrow.</param>
    /// <returns>The low 16 bits of <paramref name="value"/>.</returns>
    /// <seealso cref="Value"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator ushort(Int24 value) => (ushort)value.Value;

    /// <summary>Narrows to <see cref="char"/>, truncating to the low 16 bits.</summary>
    /// <param name="value">The value to narrow.</param>
    /// <returns>The low 16 bits of <paramref name="value"/>, reinterpreted as a UTF-16 code unit.</returns>
    /// <remarks>Negative <see cref="Int24"/> values produce code units with the high bit set; there is no clamping or rejection.</remarks>
    /// <seealso cref="Value"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator char(Int24 value) => (char)value.Value;

    // ── Explicit: Int24 → wider unsigned type (sign not preserved) ──

    /// <summary>Converts to <see cref="uint"/>. Negative values are reinterpreted as their 32-bit two's-complement form (e.g. <c>-1</c> → <c>0xFFFFFFFF</c>).</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The 32-bit two's-complement interpretation of <paramref name="value"/>.</returns>
    /// <remarks>Explicit because the sign is not preserved: a caller expecting a non-negative result must check first.</remarks>
    /// <seealso cref="Value"/>
    /// <seealso cref="explicit operator UInt24(Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator uint(Int24 value) => (uint)value.Value;

    /// <summary>Converts to <see cref="ulong"/>. Negative values are reinterpreted as their 64-bit two's-complement form.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The 64-bit two's-complement interpretation of <paramref name="value"/>.</returns>
    /// <remarks>Explicit because the sign is not preserved.</remarks>
    /// <seealso cref="Value"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator ulong(Int24 value) => unchecked((ulong)value.Value);

    // ── Explicit: cross-conversion with UInt24 (bit pattern preserved) ──

    /// <summary>Reinterprets the 24-bit pattern as unsigned. Values in <c>[0x800000, 0xFFFFFF]</c> become <c>[8388608, 16777215]</c>.</summary>
    /// <param name="value">The value whose bit pattern is reinterpreted.</param>
    /// <returns>The same 24 bits, interpreted as an unsigned value.</returns>
    /// <remarks>The three raw bytes are copied through unchanged; only the sign interpretation changes.</remarks>
    /// <seealso cref="UInt24"/>
    /// <seealso cref="explicit operator Int24(UInt24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public static explicit operator UInt24(Int24 value) => new(value.Byte0, value.Byte1, value.Byte2);

    /// <summary>Reinterprets the 24-bit pattern as signed. Values in <c>[8388608, 16777215]</c> become <c>[-8388608, -1]</c>.</summary>
    /// <param name="value">The value whose bit pattern is reinterpreted.</param>
    /// <returns>The same 24 bits, interpreted as a signed value.</returns>
    /// <remarks>The three raw bytes are copied through unchanged; only the sign interpretation changes.</remarks>
    /// <seealso cref="UInt24"/>
    /// <seealso cref="explicit operator UInt24(Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public static explicit operator Int24(UInt24 value) => new(value.Byte0, value.Byte1, value.Byte2);

    // ────────────────────────────── Arithmetic ──────────────────────────────

    /// <summary>Adds two values. Unchecked overflow wraps to the low 24 bits.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The sum, truncated to 24 bits.</returns>
    /// <remarks>To detect overflow, use <see cref="operator checked +(Int24, Int24)"/> or a <c>checked</c> context.</remarks>
    /// <seealso cref="operator -(Int24, Int24)"/>
    /// <seealso cref="operator checked +(Int24, Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public static Int24 operator +(Int24 a, Int24 b) => new(a.Value + b.Value);

    /// <summary>Subtracts two values. Unchecked overflow wraps to the low 24 bits.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The difference, truncated to 24 bits.</returns>
    /// <seealso cref="operator +(Int24, Int24)"/>
    /// <seealso cref="operator checked -(Int24, Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public static Int24 operator -(Int24 a, Int24 b) => new(a.Value - b.Value);

    /// <summary>Multiplies two values. Unchecked overflow wraps to the low 24 bits.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The product, truncated to 24 bits.</returns>
    /// <remarks>The multiplication is performed at full <see cref="int"/> width before truncation; the <see langword="checked"/> variant widens further to <see cref="long"/> to detect overflow.</remarks>
    /// <seealso cref="operator checked *(Int24, Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public static Int24 operator *(Int24 a, Int24 b) => new(a.Value * b.Value);

    /// <summary>Divides two values.</summary>
    /// <param name="a">The dividend.</param>
    /// <param name="b">The divisor.</param>
    /// <returns>The quotient, truncated toward zero.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="b"/> is zero.</exception>
    /// <remarks>There is no <c>checked</c> division operator: the quotient of two <see cref="Int24"/> values always fits except for the case <c>MinValue / -1</c>, which throws <see cref="OverflowException"/> as in the underlying <see cref="int"/> operation.</remarks>
    /// <seealso cref="operator %(Int24, Int24)"/>
    /// <seealso cref="operator *(Int24, Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public static Int24 operator /(Int24 a, Int24 b) => new(a.Value / b.Value);

    /// <summary>Computes the remainder of dividing two values.</summary>
    /// <param name="a">The dividend.</param>
    /// <param name="b">The divisor.</param>
    /// <returns>The remainder. The sign follows the dividend, as in C#.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="b"/> is zero.</exception>
    /// <seealso cref="operator /(Int24, Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public static Int24 operator %(Int24 a, Int24 b) => new(a.Value % b.Value);

    /// <summary>Returns the value unchanged.</summary>
    /// <param name="value">The value.</param>
    /// <returns><paramref name="value"/> unchanged.</returns>
    /// <remarks>Provided for symmetry with the unary negation operator.</remarks>
    /// <seealso cref="operator -(Int24)"/>
    public static Int24 operator +(Int24 value) => value;

    /// <summary>Negates the value. Unchecked overflow of <see cref="MinValue"/> wraps to itself.</summary>
    /// <param name="value">The value to negate.</param>
    /// <returns>The arithmetic negation, truncated to 24 bits.</returns>
    /// <remarks>Negating <see cref="MinValue"/> produces <see cref="MinValue"/> itself because <c>-(-8388608)</c> is not representable in 24-bit two's complement and wraps.</remarks>
    /// <seealso cref="operator checked -(Int24)"/>
    /// <seealso cref="MinValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public static Int24 operator -(Int24 value) => new(-value.Value);

    /// <summary>Increments by one. Unchecked overflow wraps within the 24-bit range.</summary>
    /// <param name="value">The value to increment.</param>
    /// <returns><paramref name="value"/> + 1, truncated to 24 bits.</returns>
    /// <seealso cref="operator --(Int24)"/>
    /// <seealso cref="operator checked ++(Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public static Int24 operator ++(Int24 value) => new(value.Value + 1);

    /// <summary>Decrements by one. Unchecked overflow wraps within the 24-bit range.</summary>
    /// <param name="value">The value to decrement.</param>
    /// <returns><paramref name="value"/> - 1, truncated to 24 bits.</returns>
    /// <seealso cref="operator ++(Int24)"/>
    /// <seealso cref="operator checked --(Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public static Int24 operator --(Int24 value) => new(value.Value - 1);

    /// <summary>Adds two values with overflow checking.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The sum, if it fits in the 24-bit range.</returns>
    /// <exception cref="OverflowException">The sum is outside <see cref="MinValue"/>..<see cref="MaxValue"/>.</exception>
    /// <remarks>Invoke explicitly as <c>checked(a + b)</c>, or inside a <c>checked</c> context.</remarks>
    /// <seealso cref="operator +(Int24, Int24)"/>
    /// <seealso cref="Checked(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/checked-and-unchecked">C# reference: <c>checked</c> and <c>unchecked</c></seealso>
    public static Int24 operator checked +(Int24 a, Int24 b) => Checked(a.Value + b.Value);

    /// <summary>Subtracts two values with overflow checking.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The difference, if it fits in the 24-bit range.</returns>
    /// <exception cref="OverflowException">The difference is outside <see cref="MinValue"/>..<see cref="MaxValue"/>.</exception>
    /// <seealso cref="operator -(Int24, Int24)"/>
    /// <seealso cref="Checked(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/checked-and-unchecked">C# reference: <c>checked</c> and <c>unchecked</c></seealso>
    public static Int24 operator checked -(Int24 a, Int24 b) => Checked(a.Value - b.Value);

    /// <summary>Multiplies two values with overflow checking.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The product, if it fits in the 24-bit range.</returns>
    /// <exception cref="OverflowException">The product is outside <see cref="MinValue"/>..<see cref="MaxValue"/>.</exception>
    /// <remarks>The multiply is performed in <see cref="long"/> so that no overflow occurs before the range check.</remarks>
    /// <seealso cref="operator *(Int24, Int24)"/>
    /// <seealso cref="Checked(long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/checked-and-unchecked">C# reference: <c>checked</c> and <c>unchecked</c></seealso>
    public static Int24 operator checked *(Int24 a, Int24 b) => Checked((long)a.Value * b.Value);

    /// <summary>Negates the value with overflow checking.</summary>
    /// <param name="value">The value to negate.</param>
    /// <returns>The arithmetic negation.</returns>
    /// <exception cref="OverflowException"><paramref name="value"/> is <see cref="MinValue"/>, whose negation is not representable.</exception>
    /// <seealso cref="operator -(Int24)"/>
    /// <seealso cref="MinValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/checked-and-unchecked">C# reference: <c>checked</c> and <c>unchecked</c></seealso>
    public static Int24 operator checked -(Int24 value) => Checked(-value.Value);

    /// <summary>Increments by one with overflow checking.</summary>
    /// <param name="value">The value to increment.</param>
    /// <returns><paramref name="value"/> + 1.</returns>
    /// <exception cref="OverflowException"><paramref name="value"/> is <see cref="MaxValue"/>.</exception>
    /// <seealso cref="operator ++(Int24)"/>
    /// <seealso cref="MaxValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/checked-and-unchecked">C# reference: <c>checked</c> and <c>unchecked</c></seealso>
    public static Int24 operator checked ++(Int24 value) => Checked(value.Value + 1);

    /// <summary>Decrements by one with overflow checking.</summary>
    /// <param name="value">The value to decrement.</param>
    /// <returns><paramref name="value"/> - 1.</returns>
    /// <exception cref="OverflowException"><paramref name="value"/> is <see cref="MinValue"/>.</exception>
    /// <seealso cref="operator --(Int24)"/>
    /// <seealso cref="MinValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/checked-and-unchecked">C# reference: <c>checked</c> and <c>unchecked</c></seealso>
    public static Int24 operator checked --(Int24 value) => Checked(value.Value - 1);

    // ────────────────────────────── Bitwise ──────────────────────────────

    /// <summary>Bitwise AND.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The bitwise AND of the 24-bit patterns of <paramref name="a"/> and <paramref name="b"/>.</returns>
    /// <remarks>The operation is performed at full <see cref="int"/> width; the upper 8 bits are discarded by the constructor. Because the source values are already sign-extended, the upper 8 bits are all-ones for negatives and all-zeros for non-negatives, so the AND result sign-extends correctly.</remarks>
    /// <seealso cref="operator |(Int24, Int24)"/>
    /// <seealso cref="operator ^(Int24, Int24)"/>
    public static Int24 operator &(Int24 a, Int24 b) => new(a.Value & b.Value);

    /// <summary>Bitwise OR.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The bitwise OR of the 24-bit patterns of <paramref name="a"/> and <paramref name="b"/>.</returns>
    /// <seealso cref="operator &amp;(Int24, Int24)"/>
    /// <seealso cref="operator ^(Int24, Int24)"/>
    public static Int24 operator |(Int24 a, Int24 b) => new(a.Value | b.Value);

    /// <summary>Bitwise XOR.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The bitwise XOR of the 24-bit patterns of <paramref name="a"/> and <paramref name="b"/>.</returns>
    /// <seealso cref="operator &amp;(Int24, Int24)"/>
    /// <seealso cref="operator |(Int24, Int24)"/>
    public static Int24 operator ^(Int24 a, Int24 b) => new(a.Value ^ b.Value);

    /// <summary>Bitwise complement. The upper 8 bits of the result are discarded; bit 23 is preserved as the sign bit.</summary>
    /// <param name="value">The value to complement.</param>
    /// <returns>The bitwise complement of the 24-bit pattern of <paramref name="value"/>.</returns>
    /// <remarks>Complementing the sign-extended <see cref="int"/> value flips bit 23 along with the low 24 bits; the upper 8 bits flip too but are discarded by the constructor, so the result remains correctly sign-extended.</remarks>
    /// <seealso cref="operator ~(Int24)"/>
    public static Int24 operator ~(Int24 value) => new(~value.Value);

    /// <summary>Shifts left. Unchecked overflow discards bits shifted past bit 23.</summary>
    /// <param name="value">The value to shift.</param>
    /// <param name="shift">The shift count. Uses the low 5 bits; the C# <c>&lt;&lt;</c> operator behaviour for <see cref="int"/> applies.</param>
    /// <returns>The shifted 24-bit pattern.</returns>
    /// <seealso cref="operator >>(Int24, int)"/>
    public static Int24 operator <<(Int24 value, int shift) => new(value.Value << shift);

    /// <summary>Shifts right, sign-extending (arithmetic shift).</summary>
    /// <param name="value">The value to shift.</param>
    /// <param name="shift">The shift count. Uses the low 5 bits; the C# <c>&gt;&gt;</c> operator behaviour for <see cref="int"/> applies.</param>
    /// <returns>The shifted 24-bit pattern.</returns>
    /// <remarks>The shift is arithmetic because the source value is sign-extended before the shift.</remarks>
    /// <seealso cref="operator &lt;&lt;(Int24, int)"/>
    public static Int24 operator >>(Int24 value, int shift) => new(value.Value >> shift);

    // ────────────────────────────── Comparison ──────────────────────────────

    /// <summary>Equality.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when both operands have identical <see cref="Value"/>.</returns>
    /// <seealso cref="Equals(Int24)"/>
    /// <seealso cref="operator !=(Int24, Int24)"/>
    public static bool operator ==(Int24 a, Int24 b) => a.Value == b.Value;

    /// <summary>Inequality.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when the operands have different <see cref="Value"/>.</returns>
    /// <seealso cref="Equals(Int24)"/>
    /// <seealso cref="operator ==(Int24, Int24)"/>
    public static bool operator !=(Int24 a, Int24 b) => a.Value != b.Value;

    /// <summary>Less-than.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="a"/> is numerically less than <paramref name="b"/>.</returns>
    /// <seealso cref="CompareTo(Int24)"/>
    /// <seealso cref="operator >(Int24, Int24)"/>
    public static bool operator <(Int24 a, Int24 b) => a.Value < b.Value;

    /// <summary>Greater-than.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="a"/> is numerically greater than <paramref name="b"/>.</returns>
    /// <seealso cref="CompareTo(Int24)"/>
    /// <seealso cref="operator &lt;(Int24, Int24)"/>
    public static bool operator >(Int24 a, Int24 b) => a.Value > b.Value;

    /// <summary>Less-than-or-equal.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="a"/> is numerically less than or equal to <paramref name="b"/>.</returns>
    /// <seealso cref="CompareTo(Int24)"/>
    /// <seealso cref="operator >=(Int24, Int24)"/>
    public static bool operator <=(Int24 a, Int24 b) => a.Value <= b.Value;

    /// <summary>Greater-than-or-equal.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="a"/> is numerically greater than or equal to <paramref name="b"/>.</returns>
    /// <seealso cref="CompareTo(Int24)"/>
    /// <seealso cref="operator &lt;=(Int24, Int24)"/>
    public static bool operator >=(Int24 a, Int24 b) => a.Value >= b.Value;

    /// <inheritdoc/>
    /// <param name="other">The value to compare with.</param>
    /// <returns><see langword="true"/> when both values have identical <see cref="Value"/>.</returns>
    /// <seealso cref="operator ==(Int24, Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1">.NET API: <c>IEquatable&lt;T&gt;</c></seealso>
    public bool Equals(Int24 other) => Value == other.Value;

    /// <inheritdoc/>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is an <see cref="Int24"/> with identical <see cref="Value"/>.</returns>
    /// <seealso cref="Equals(Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.object.equals">.NET API: <c>Object.Equals</c></seealso>
    public override bool Equals(object? obj) => obj is Int24 other && Equals(other);

    /// <inheritdoc/>
    /// <returns>A hash code derived from <see cref="Value"/>.</returns>
    /// <remarks>Two <see cref="Int24"/> values that compare equal produce the same hash because the hash is computed from <see cref="Value"/>, not from the raw bytes.</remarks>
    /// <seealso cref="Value"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.object.gethashcode">.NET API: <c>Object.GetHashCode</c></seealso>
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc/>
    /// <param name="other">The value to compare with.</param>
    /// <returns>A signed comparison of <see cref="Value"/>.</returns>
    /// <seealso cref="CompareTo(object?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.icomparable-1">.NET API: <c>IComparable&lt;T&gt;</c></seealso>
    public int CompareTo(Int24 other) => Value.CompareTo(other.Value);

    /// <inheritdoc/>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>A positive value when this instance is greater; zero when equal; negative when less.</returns>
    /// <exception cref="ArgumentException"><paramref name="obj"/> is neither <c>null</c> nor an <see cref="Int24"/>.</exception>
    /// <remarks>A <c>null</c> argument is treated as less than any non-null value, per the <see cref="IComparable"/> contract.</remarks>
    /// <seealso cref="CompareTo(Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.icomparable">.NET API: <c>IComparable</c></seealso>
    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        Int24 other => CompareTo(other),
        _ => throw new ArgumentException(
            $"Object must be of type {nameof(Int24)}.", nameof(obj)),
    };

    // ────────────────────────────── Formatting ──────────────────────────────

    /// <inheritdoc/>
    /// <returns>The <see cref="Value"/> formatted with the current culture and the default format.</returns>
    /// <seealso cref="ToString(string?)"/>
    /// <seealso cref="ToString(string?, IFormatProvider?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.iformattable">.NET API: <c>IFormattable</c></seealso>
    public override string ToString() => Value.ToString();

    /// <summary>Formats the value using the given format string.</summary>
    /// <param name="format">A standard or custom numeric format string, or <c>null</c> for the default.</param>
    /// <returns>The formatted <see cref="Value"/>.</returns>
    /// <seealso cref="ToString()"/>
    /// <seealso cref="ToString(IFormatProvider?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings">.NET guide: Standard numeric format strings</seealso>
    public string ToString(string? format) => Value.ToString(format);

    /// <summary>Formats the value using the given format provider.</summary>
    /// <param name="formatProvider">The provider to use for culture-specific formatting, or <c>null</c> for the current culture.</param>
    /// <returns>The formatted <see cref="Value"/>.</returns>
    /// <seealso cref="ToString()"/>
    /// <seealso cref="ToString(string?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.iformatprovider">.NET API: <c>IFormatProvider</c></seealso>
    public string ToString(IFormatProvider? formatProvider) => Value.ToString(formatProvider);

    /// <inheritdoc/>
    /// <param name="format">A standard or custom numeric format string, or <c>null</c> for the default.</param>
    /// <param name="formatProvider">The provider to use for culture-specific formatting, or <c>null</c> for the current culture.</param>
    /// <returns>The formatted <see cref="Value"/>.</returns>
    /// <seealso cref="ToString(string?)"/>
    /// <seealso cref="ToString(IFormatProvider?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.iformattable">.NET API: <c>IFormattable</c></seealso>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);

    /// <inheritdoc/>
    /// <param name="destination">The span to write the formatted characters into.</param>
    /// <param name="charsWritten">When this method returns, the number of characters written.</param>
    /// <param name="format">A standard or custom numeric format string, or an empty span for the default.</param>
    /// <param name="provider">The provider to use for culture-specific formatting, or <c>null</c> for the current culture.</param>
    /// <returns><see langword="true"/> when the value was formatted successfully; <see langword="false"/> when <paramref name="destination"/> was too small.</returns>
    /// <seealso cref="ToString(string?, IFormatProvider?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.ispanformattable">.NET API: <c>ISpanFormattable</c></seealso>
    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider) =>
        Value.TryFormat(destination, out charsWritten, format, provider);

    // ────────────────────────────── Parsing ──────────────────────────────

    /// <summary>Parses a decimal string.</summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException"><paramref name="s"/> is not a valid integer representation.</exception>
    /// <exception cref="OverflowException">The parsed value is outside the <see cref="Int24"/> range.</exception>
    /// <remarks>Uses <see cref="CultureInfo.InvariantCulture"/> and permits a leading sign.</remarks>
    /// <seealso cref="Parse(string, NumberStyles)"/>
    /// <seealso cref="TryParse(string?, out Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.int32.parse">.NET API: <c>Int32.Parse</c></seealso>
    public static Int24 Parse(string s) =>
        Checked(int.Parse(s, NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture));

    /// <summary>Parses a string using the given number style.</summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="style">A bitwise combination of the enumeration values that indicates the style elements permitted in <paramref name="s"/>.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException"><paramref name="s"/> is not a valid integer representation given <paramref name="style"/>.</exception>
    /// <exception cref="OverflowException">The parsed value is outside the <see cref="Int24"/> range.</exception>
    /// <seealso cref="Parse(string)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.globalization.numberstyles">.NET API: <c>NumberStyles</c></seealso>
    public static Int24 Parse(string s, NumberStyles style) =>
        Checked(int.Parse(s, style, CultureInfo.InvariantCulture));

    /// <summary>Parses a string using the given format provider.</summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">The provider to use for culture-specific parsing, or <c>null</c> for the current culture.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s"/> is <c>null</c>.</exception>
    /// <exception cref="FormatException"><paramref name="s"/> is not a valid integer representation.</exception>
    /// <exception cref="OverflowException">The parsed value is outside the <see cref="Int24"/> range.</exception>
    /// <seealso cref="Parse(string)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.iformatprovider">.NET API: <c>IFormatProvider</c></seealso>
    public static Int24 Parse(string s, IFormatProvider? provider) =>
        Checked(int.Parse(s, NumberStyles.Integer | NumberStyles.AllowLeadingSign, provider));

    /// <summary>Parses a character span.</summary>
    /// <param name="s">The character span to parse.</param>
    /// <param name="style">A bitwise combination of the enumeration values that indicates the style elements permitted in <paramref name="s"/>.</param>
    /// <param name="provider">The provider to use for culture-specific parsing, or <c>null</c> for the current culture.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="FormatException"><paramref name="s"/> is not a valid integer representation given <paramref name="style"/>.</exception>
    /// <exception cref="OverflowException">The parsed value is outside the <see cref="Int24"/> range.</exception>
    /// <seealso cref="TryParse(ReadOnlySpan{char}, out Int24)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.int32.parse">.NET API: <c>Int32.Parse</c></seealso>
    public static Int24 Parse(
        ReadOnlySpan<char> s,
        NumberStyles style = NumberStyles.Integer | NumberStyles.AllowLeadingSign,
        IFormatProvider? provider = null) =>
        Checked(int.Parse(s, style, provider));

    /// <summary>Attempts to parse a string.</summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">When this method returns, the parsed value, or <c>default</c> on failure.</param>
    /// <returns><see langword="true"/> when parsing succeeded; otherwise <see langword="false"/>.</returns>
    /// <remarks>Never throws. Returns <see langword="false"/> when the string is malformed or out of range.</remarks>
    /// <seealso cref="TryParse(string?, NumberStyles, IFormatProvider?, out Int24)"/>
    /// <seealso cref="Parse(string)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.int32.tryparse">.NET API: <c>Int32.TryParse</c></seealso>
    public static bool TryParse(string? s, out Int24 result) =>
        TryParse(s, NumberStyles.Integer | NumberStyles.AllowLeadingSign,
                 CultureInfo.InvariantCulture, out result);

    /// <summary>Attempts to parse a string using the given style and format provider.</summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="style">A bitwise combination of the enumeration values that indicates the style elements permitted in <paramref name="s"/>.</param>
    /// <param name="provider">The provider to use for culture-specific parsing, or <c>null</c> for the current culture.</param>
    /// <param name="result">When this method returns, the parsed value, or <c>default</c> on failure.</param>
    /// <returns><see langword="true"/> when parsing succeeded; otherwise <see langword="false"/>.</returns>
    /// <remarks>Rejects values outside <see cref="MinValue"/>..<see cref="MaxValue"/> even if they would parse as an <see cref="int"/>.</remarks>
    /// <seealso cref="TryParse(string?, out Int24)"/>
    /// <seealso cref="Parse(string, NumberStyles)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.int32.tryparse">.NET API: <c>Int32.TryParse</c></seealso>
    public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, out Int24 result)
    {
        if (int.TryParse(s, style, provider, out var value)
            && value >= -0x800000 && value <= 0x7FFFFF)
        {
            result = new(value);
            return true;
        }
        result = default;
        return false;
    }

    /// <summary>Attempts to parse a character span.</summary>
    /// <param name="s">The character span to parse.</param>
    /// <param name="result">When this method returns, the parsed value, or <c>default</c> on failure.</param>
    /// <returns><see langword="true"/> when parsing succeeded; otherwise <see langword="false"/>.</returns>
    /// <remarks>Never throws. Returns <see langword="false"/> when the span is malformed or out of range.</remarks>
    /// <seealso cref="TryParse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?, out Int24)"/>
    /// <seealso cref="Parse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.int32.tryparse">.NET API: <c>Int32.TryParse</c></seealso>
    public static bool TryParse(ReadOnlySpan<char> s, out Int24 result) =>
        TryParse(s, NumberStyles.Integer | NumberStyles.AllowLeadingSign,
                 CultureInfo.InvariantCulture, out result);

    /// <summary>Attempts to parse a character span using the given style and format provider.</summary>
    /// <param name="s">The character span to parse.</param>
    /// <param name="style">A bitwise combination of the enumeration values that indicates the style elements permitted in <paramref name="s"/>.</param>
    /// <param name="provider">The provider to use for culture-specific parsing, or <c>null</c> for the current culture.</param>
    /// <param name="result">When this method returns, the parsed value, or <c>default</c> on failure.</param>
    /// <returns><see langword="true"/> when parsing succeeded; otherwise <see langword="false"/>.</returns>
    /// <remarks>Rejects values outside <see cref="MinValue"/>..<see cref="MaxValue"/> even if they would parse as an <see cref="int"/>.</remarks>
    /// <seealso cref="TryParse(ReadOnlySpan{char}, out Int24)"/>
    /// <seealso cref="Parse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.int32.tryparse">.NET API: <c>Int32.TryParse</c></seealso>
    public static bool TryParse(
        ReadOnlySpan<char> s,
        NumberStyles style,
        IFormatProvider? provider,
        out Int24 result)
    {
        if (int.TryParse(s, style, provider, out var value)
            && value >= -0x800000 && value <= 0x7FFFFF)
        {
            result = new(value);
            return true;
        }
        result = default;
        return false;
    }

    // ────────────────────────────── Internal ──────────────────────────────

    /// <summary>Creates a value from an <see cref="int"/>, throwing if the value is outside the 24-bit range.</summary>
    /// <param name="value">The value to create the <see cref="Int24"/> from.</param>
    /// <returns>The value, if it fits in <see cref="MinValue"/>..<see cref="MaxValue"/>.</returns>
    /// <exception cref="OverflowException"><paramref name="value"/> is outside <see cref="MinValue"/>..<see cref="MaxValue"/>.</exception>
    /// <remarks>This is the checked constructor used by the <c>checked</c> operators and by the parsing methods. It is not the same as <see cref="Int24(int)"/>, which truncates silently.</remarks>
    /// <seealso cref="Int24(int)"/>
    /// <seealso cref="Checked(long)"/>
    /// <seealso cref="MaxValue"/>
    /// <seealso cref="MinValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.overflowexception">.NET API: <c>OverflowException</c></seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int24 Checked(int value) =>
        value >= -0x800000 && value <= 0x7FFFFF
            ? new Int24(value)
            : throw new OverflowException($"Value {value} does not fit in a {nameof(Int24)}.");

    /// <summary>Creates a value from a <see cref="long"/>, throwing if the value is outside the 24-bit range.</summary>
    /// <param name="value">The value to create the <see cref="Int24"/> from.</param>
    /// <returns>The value, if it fits in <see cref="MinValue"/>..<see cref="MaxValue"/>.</returns>
    /// <exception cref="OverflowException"><paramref name="value"/> is outside <see cref="MinValue"/>..<see cref="MaxValue"/>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>This overload exists for callers who compute the candidate value in 64-bit arithmetic — the <c>checked</c> multiplication operator, for example.</description></item>
    /// <item><description>Truncating the value to <see cref="int"/> first would lose bits above 31 that are relevant to the range check, so the check runs against the <see cref="long"/> value before narrowing.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Checked(int)"/>
    /// <seealso cref="MaxValue"/>
    /// <seealso cref="MinValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.overflowexception">.NET API: <c>OverflowException</c></seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int24 Checked(long value) =>
        value >= -0x800000 && value <= 0x7FFFFF
            ? new Int24((int)value)
            : throw new OverflowException($"Value {value} does not fit in a {nameof(Int24)}.");
}
