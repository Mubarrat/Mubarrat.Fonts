using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.OpenType.Binary;

namespace Mubarrat.Fonts.OpenType.Primitives;

/// <summary>A 16.16 signed fixed-point value, stored as a big-endian <see cref="int"/> on disk. Used by <c>head.fontRevision</c>, <c>post.italicAngle</c>, <c>fvar</c> axis and instance coordinates, and any other OpenType field declared as <c>Fixed</c>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The high 16 bits hold the signed integer part; the low 16 bits hold the fraction in units of 1/65536. The numeric value is <c>bits / 65536.0</c>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Fixed data type</see> in the OpenType specification.</description></item>
/// <item><description>Stored as a single <see cref="int"/>. Blittable, size 4, no padding. On a little-endian host the backing field is little-endian; <see cref="ReverseEndianness(Fixed)"/> reverses it to the big-endian on-disk layout.</description></item>
/// <item><description>Conversions to <see cref="double"/> and <see cref="float"/> are implicit; conversions from floating-point types are explicit because they truncate.</description></item>
/// <item><description>The unchecked arithmetic operators (<c>+</c>, <c>-</c>, unary <c>-</c>, <c>++</c>, <c>--</c>) wrap on overflow; the <c>checked</c> variants throw <see cref="OverflowException"/>.</description></item>
/// </list>
/// <para>For the tables that carry <c>Fixed</c> fields, see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification, Organization of an OpenType Font</see>.</para>
/// </remarks>
/// <seealso cref="F2Dot14"/>
/// <seealso cref="UInt24"/>
/// <seealso cref="Int24"/>
/// <seealso cref="Tag"/>
/// <seealso cref="IBigEndianStruct{T}"/>
/// <seealso cref="Source.ReadBigEndianStructAt{T}(long)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType Font</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Fixed :
    IComparable,
    IComparable<Fixed>,
    IEquatable<Fixed>,
    IFormattable,
    IBigEndianStruct<Fixed>
{
    // ────────────────────────────── Constants ──────────────────────────────

    /// <summary>The number of fractional bits.</summary>
    /// <remarks>Always <c>16</c>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Fixed data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="One"/>
    /// <seealso cref="Epsilon"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public const int FractionBits = 16;

    /// <summary>The scale factor: 1 integer unit equals 65536 raw bits.</summary>
    /// <remarks>Equal to <c>1 &lt;&lt; <see cref="FractionBits"/></c>. The raw value of <c>1.0</c> in this representation.</remarks>
    /// <seealso cref="FractionBits"/>
    /// <seealso cref="OneValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public const int One = 1 << FractionBits;

    /// <summary>The smallest representable increment: 1/65536.</summary>
    /// <remarks>Equal to <c>1.0 / <see cref="One"/></c>. The difference between adjacent representable values.</remarks>
    /// <seealso cref="One"/>
    /// <seealso cref="FractionBits"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public const double Epsilon = 1.0 / One;

    /// <summary>The zero value.</summary>
    /// <value>The default <see cref="Fixed"/>, with raw bits <c>0</c>.</value>
    /// <seealso cref="OneValue"/>
    /// <seealso cref="MinValue"/>
    /// <seealso cref="MaxValue"/>
    public static readonly Fixed Zero = default;

    /// <summary>The value 1.0.</summary>
    /// <value>The <see cref="Fixed"/> whose raw bits equal <see cref="One"/>.</value>
    /// <seealso cref="One"/>
    /// <seealso cref="Zero"/>
    public static readonly Fixed OneValue = new(One);

    /// <summary>The minimum value: <c>int.MinValue / 65536.0</c>.</summary>
    /// <value>The <see cref="Fixed"/> whose raw bits equal <see cref="int.MinValue"/>. Approximately <c>-32768.0</c>.</value>
    /// <seealso cref="MaxValue"/>
    /// <seealso cref="FromDouble(double)"/>
    public static readonly Fixed MinValue = new(int.MinValue);

    /// <summary>The maximum value: <c>int.MaxValue / 65536.0</c>.</summary>
    /// <value>The <see cref="Fixed"/> whose raw bits equal <see cref="int.MaxValue"/>. Approximately <c>32767.99998</c>.</value>
    /// <seealso cref="MinValue"/>
    /// <seealso cref="FromDouble(double)"/>
    public static readonly Fixed MaxValue = new(int.MaxValue);

    // ────────────────────────────── Storage ──────────────────────────────

    private readonly int _bits;

    // ────────────────────────────── Construction ──────────────────────────────

    /// <summary>Creates a value from its raw 16.16 bit representation.</summary>
    /// <param name="bits">The signed 32-bit raw value; the caller supplies it as-is without scaling.</param>
    /// <remarks>This is the unchecked constructor. To convert a floating-point value, use <see cref="FromDouble(double)"/> or the explicit cast.</remarks>
    /// <seealso cref="FromBits(int)"/>
    /// <seealso cref="FromInt32(int)"/>
    /// <seealso cref="FromDouble(double)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Fixed(int bits) => _bits = bits;

    /// <summary>Creates a value from a double, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The nearest representable <see cref="Fixed"/> whose magnitude does not exceed <paramref name="value"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The conversion multiplies by <see cref="One"/> and casts to <see cref="int"/>; the cast is unchecked, so out-of-range values wrap rather than clamp or throw.</description></item>
    /// <item><description>Truncation toward zero is the C# <c>double</c>-to-<c>int</c> default; <c>0.99999</c> becomes <c>0</c>, and <c>-0.99999</c> becomes <c>0</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Fixed data type</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var a = Fixed.FromDouble(1.5);      // raw 98304
    /// var b = Fixed.FromDouble(0.99999);  // truncates to 0
    /// </code>
    /// </example>
    /// <seealso cref="FromInt32(int)"/>
    /// <seealso cref="MaxValue"/>
    /// <seealso cref="MinValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fixed FromDouble(double value) =>
        new(unchecked((int)(value * One)));

    /// <summary>Creates a value from an integer.</summary>
    /// <param name="value">The integer value to convert.</param>
    /// <returns>A <see cref="Fixed"/> whose numeric value equals <paramref name="value"/>.</returns>
    /// <remarks>The conversion multiplies by <see cref="One"/> and casts to <see cref="int"/>; the cast is unchecked, so values outside the representable integer range wrap.</remarks>
    /// <seealso cref="FromDouble(double)"/>
    /// <seealso cref="FromBits(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fixed FromInt32(int value) =>
        new(unchecked(value * One));

    /// <summary>Creates a value from its raw bit representation.</summary>
    /// <param name="bits">The signed 32-bit raw value.</param>
    /// <returns>A <see cref="Fixed"/> wrapping <paramref name="bits"/> unchanged.</returns>
    /// <remarks>Equivalent to calling the constructor directly; provided as a named factory for call-site readability.</remarks>
    /// <seealso cref="Fixed(int)"/>
    /// <seealso cref="Bits"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fixed FromBits(int bits) => new(bits);

    // ────────────────────────────── Access ──────────────────────────────

    /// <summary>Gets the raw 16.16 bit representation.</summary>
    /// <value>The signed 32-bit value whose numeric interpretation is <c>raw / 65536.0</c>.</value>
    /// <seealso cref="Value"/>
    /// <seealso cref="IntegerPart"/>
    /// <seealso cref="FractionPart"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public readonly int Bits => _bits;

    /// <summary>Gets the numeric value as a <see cref="double"/>.</summary>
    /// <value>The raw bits divided by <see cref="One"/>.</value>
    /// <seealso cref="Bits"/>
    /// <seealso cref="IntegerPart"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public readonly double Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _bits / 65536.0;
    }

    /// <summary>Gets the integer part (truncated toward zero).</summary>
    /// <value>The raw bits shifted right by <see cref="FractionBits"/> with arithmetic shift, preserving the sign.</value>
    /// <seealso cref="Value"/>
    /// <seealso cref="FractionPart"/>
    /// <seealso cref="IntegerPart"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public readonly int IntegerPart => _bits >> FractionBits;

    /// <summary>Gets the fractional part as a raw 16-bit value.</summary>
    /// <value>The low 16 bits of <see cref="Bits"/>, interpreted as an unsigned quantity. For negative values this is the magnitude of the fractional part as a two's-complement bit pattern, not a signed value.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>For positive values, <c>FractionPart / 65536.0</c> equals the fractional part of <see cref="Value"/>.</description></item>
    /// <item><description>For negative values, the sign lives in <see cref="IntegerPart"/>, so <c>Value = IntegerPart + FractionPart / 65536.0</c> does not hold. The correct relation is <c>Value = Bits / 65536.0</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Bits"/>
    /// <seealso cref="IntegerPart"/>
    /// <seealso cref="Value"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public readonly ushort FractionPart => (ushort)(_bits & 0xFFFF);

    // ────────────────────────────── Endianness ──────────────────────────────

    /// <inheritdoc/>
    /// <param name="value">The value whose bytes are to be reversed.</param>
    /// <returns>A <see cref="Fixed"/> whose bytes are the reverse of <paramref name="value"/>'s.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The operation is unconditional. The host-endianness check is applied by the reader (<see cref="Source.ReadBigEndianStructAt{T}(long)"/>, <see cref="Cursor.ReadBigEndianStruct{T}"/>), not by this method.</description></item>
    /// <item><description>Symmetric: <c>ReverseEndianness(ReverseEndianness(x)) == x</c> for every <c>x</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Fixed data type</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso cref="Source.ReadBigEndianStructAt{T}(long)"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fixed ReverseEndianness(Fixed value) =>
        new(BinaryPrimitives.ReverseEndianness(value._bits));

    // ────────────────────────────── Conversion ──────────────────────────────

    /// <summary>Converts to <see cref="double"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The numeric value as a <see cref="double"/>.</returns>
    /// <remarks>The conversion is lossless: every <see cref="Fixed"/> maps to a distinct <see cref="double"/>.</remarks>
    /// <seealso cref="Value"/>
    /// <seealso cref="FromDouble(double)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static implicit operator double(Fixed value) => value.Value;

    /// <summary>Converts to <see cref="float"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The numeric value as a <see cref="float"/>.</returns>
    /// <remarks>The conversion goes through <see cref="double"/> and then narrows to <see cref="float"/>. <see cref="float"/> has 24 bits of mantissa; the 16-bit fraction fits, but the integer part may lose precision for very large values.</remarks>
    /// <seealso cref="Value"/>
    /// <seealso cref="FromDouble(double)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static implicit operator float(Fixed value) => (float)value.Value;

    /// <summary>Converts from <see cref="double"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The nearest representable <see cref="Fixed"/> whose magnitude does not exceed <paramref name="value"/>.</returns>
    /// <remarks>Explicit because the conversion truncates and wraps out-of-range values silently.</remarks>
    /// <seealso cref="FromDouble(double)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator Fixed(double value) => FromDouble(value);

    /// <summary>Converts from <see cref="float"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The nearest representable <see cref="Fixed"/> whose magnitude does not exceed <paramref name="value"/>.</returns>
    /// <remarks>Explicit because the conversion truncates and wraps out-of-range values silently.</remarks>
    /// <seealso cref="FromDouble(double)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator Fixed(float value) => FromDouble(value);

    /// <summary>Converts from <see cref="int"/>.</summary>
    /// <param name="value">The integer value to convert.</param>
    /// <returns>A <see cref="Fixed"/> whose numeric value equals <paramref name="value"/>.</returns>
    /// <remarks>Explicit because values with magnitude above roughly 32767 wrap during the multiply by <see cref="One"/>.</remarks>
    /// <seealso cref="FromInt32(int)"/>
    /// <seealso cref="explicit operator int(Fixed)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator Fixed(int value) => FromInt32(value);

    /// <summary>Converts to <see cref="int"/>, truncating toward zero.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The integer part of <paramref name="value"/>.</returns>
    /// <remarks>Discards the fractional part; <c>1.9</c> converts to <c>1</c> and <c>-1.9</c> converts to <c>-1</c>.</remarks>
    /// <seealso cref="IntegerPart"/>
    /// <seealso cref="FromInt32(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator int(Fixed value) => value.IntegerPart;

    // ────────────────────────────── Arithmetic ──────────────────────────────

    /// <summary>Adds two values. Unchecked overflow wraps.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The sum, computed on the raw 16.16 representation.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The addition is performed on the raw <see cref="int"/> bits; the scale factor cancels.</description></item>
    /// <item><description>Overflow is unchecked, so the result wraps mod 2<sup>32</sup>. Callers needing saturation should use <see cref="operator checked +(Fixed, Fixed)"/> or check the operands against <see cref="MaxValue"/> and <see cref="MinValue"/> first.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Fixed data type</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="operator -(Fixed, Fixed)"/>
    /// <seealso cref="operator checked +(Fixed, Fixed)"/>
    /// <seealso cref="MaxValue"/>
    /// <seealso cref="MinValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public static Fixed operator +(Fixed a, Fixed b) => new(unchecked(a._bits + b._bits));

    /// <summary>Subtracts two values. Unchecked overflow wraps.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The difference, computed on the raw 16.16 representation.</returns>
    /// <remarks>Overflow is unchecked, so the result wraps mod 2<sup>32</sup>.</remarks>
    /// <seealso cref="operator +(Fixed, Fixed)"/>
    /// <seealso cref="operator checked -(Fixed, Fixed)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public static Fixed operator -(Fixed a, Fixed b) => new(unchecked(a._bits - b._bits));

    /// <summary>Multiplies two values, rounding to the nearest fractional unit.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The product, rounded to the nearest representable <see cref="Fixed"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The product of two 16.16 values has 32 fractional bits; the implementation adds <c>1 &lt;&lt; 15</c> before shifting right by 16.</description></item>
    /// <item><description>The intermediate multiply is widened to <see cref="long"/>, so no overflow occurs for any pair of <see cref="int"/>-range operands; the final cast to <see cref="int"/> is unchecked and wraps on overflow.</description></item>
    /// <item><description>The <c>+ rounding</c> step implements round-half-up toward positive infinity. For negative products this rounds toward zero rather than away from zero.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Fixed data type</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="operator /(Fixed, Fixed)"/>
    /// <seealso cref="FractionBits"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public static Fixed operator *(Fixed a, Fixed b)
    {
        long product = (long)a._bits * b._bits;
        // Round-half-to-even, matching (int)(x + 0.5) for positives and its symmetric
        // counterpart for negatives.
        long rounding = 1L << (FractionBits - 1);
        return new((int)((product + rounding) >> FractionBits));
    }

    /// <summary>Divides two values, rounding to the nearest fractional unit.</summary>
    /// <param name="a">The dividend.</param>
    /// <param name="b">The divisor.</param>
    /// <returns>The quotient, rounded to the nearest representable <see cref="Fixed"/>.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="b"/> is <see cref="Zero"/>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The numerator is shifted left by <see cref="FractionBits"/> and widened to <see cref="long"/>, keeping the result at the 16.16 scale.</description></item>
    /// <item><description>A sign-aware rounding constant (<c>b / 2</c>) is added to the numerator before the divide, so positive quotients round half-up and negative quotients round half-away-from-zero.</description></item>
    /// <item><description>The final cast to <see cref="int"/> is unchecked; a quotient outside the representable range wraps rather than throwing.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Fixed data type</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="operator *(Fixed, Fixed)"/>
    /// <seealso cref="Zero"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public static Fixed operator /(Fixed a, Fixed b)
    {
        long numerator = (long)a._bits << FractionBits;
        long rounding = b._bits >= 0 ? b._bits / 2 : -(b._bits / 2);
        return new((int)((numerator + rounding) / b._bits));
    }

    /// <summary>Negates the value. Unchecked overflow of <see cref="MinValue"/> wraps.</summary>
    /// <param name="value">The value to negate.</param>
    /// <returns>The arithmetic negation, computed on the raw 16.16 representation.</returns>
    /// <remarks>Negating <see cref="MinValue"/> produces <see cref="MinValue"/> itself because <c>-(-2147483648)</c> wraps to <c>-2147483648</c> in 32-bit two's complement.</remarks>
    /// <seealso cref="operator +(Fixed)"/>
    /// <seealso cref="operator checked -(Fixed)"/>
    /// <seealso cref="MinValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public static Fixed operator -(Fixed value) => new(unchecked(-value._bits));

    /// <summary>Returns the value unchanged.</summary>
    /// <param name="value">The value.</param>
    /// <returns><paramref name="value"/> unchanged.</returns>
    /// <remarks>Provided for symmetry with the unary negation operator.</remarks>
    /// <seealso cref="operator -(Fixed)"/>
    public static Fixed operator +(Fixed value) => value;

    /// <summary>Increments by the smallest representable unit.</summary>
    /// <param name="value">The value to increment.</param>
    /// <returns><paramref name="value"/> plus <see cref="Epsilon"/>.</returns>
    /// <remarks>Increments the raw bits, so the step is exactly <see cref="Epsilon"/> regardless of magnitude.</remarks>
    /// <seealso cref="operator --(Fixed)"/>
    /// <seealso cref="operator checked ++(Fixed)"/>
    /// <seealso cref="Epsilon"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public static Fixed operator ++(Fixed value) => new(unchecked(value._bits + 1));

    /// <summary>Decrements by the smallest representable unit.</summary>
    /// <param name="value">The value to decrement.</param>
    /// <returns><paramref name="value"/> minus <see cref="Epsilon"/>.</returns>
    /// <remarks>Decrements the raw bits, so the step is exactly <see cref="Epsilon"/> regardless of magnitude.</remarks>
    /// <seealso cref="operator ++(Fixed)"/>
    /// <seealso cref="operator checked --(Fixed)"/>
    /// <seealso cref="Epsilon"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public static Fixed operator --(Fixed value) => new(unchecked(value._bits - 1));

    /// <summary>Adds two values with overflow checking.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The sum, computed on the raw 16.16 representation.</returns>
    /// <exception cref="OverflowException">The sum exceeds the range of <see cref="int"/>.</exception>
    /// <remarks>Call the <c>checked</c> operator explicitly, or use a <c>checked</c> context, to opt into the throw-on-overflow behavior.</remarks>
    /// <seealso cref="operator +(Fixed, Fixed)"/>
    /// <seealso cref="operator checked -(Fixed, Fixed)"/>
    /// <seealso cref="MaxValue"/>
    /// <seealso cref="MinValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/checked-and-unchecked">C# reference: <c>checked</c> and <c>unchecked</c></seealso>
    public static Fixed operator checked +(Fixed a, Fixed b) => new(checked(a._bits + b._bits));

    /// <summary>Subtracts two values with overflow checking.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The difference, computed on the raw 16.16 representation.</returns>
    /// <exception cref="OverflowException">The difference exceeds the range of <see cref="int"/>.</exception>
    /// <seealso cref="operator -(Fixed, Fixed)"/>
    /// <seealso cref="operator checked +(Fixed, Fixed)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/checked-and-unchecked">C# reference: <c>checked</c> and <c>unchecked</c></seealso>
    public static Fixed operator checked -(Fixed a, Fixed b) => new(checked(a._bits - b._bits));

    /// <summary>Negates the value with overflow checking.</summary>
    /// <param name="value">The value to negate.</param>
    /// <returns>The arithmetic negation.</returns>
    /// <exception cref="OverflowException"><paramref name="value"/> is <see cref="MinValue"/>, whose negation is not representable.</exception>
    /// <seealso cref="operator -(Fixed)"/>
    /// <seealso cref="MinValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/checked-and-unchecked">C# reference: <c>checked</c> and <c>unchecked</c></seealso>
    public static Fixed operator checked -(Fixed value) => new(checked(-value._bits));

    /// <summary>Increments by the smallest unit with overflow checking.</summary>
    /// <param name="value">The value to increment.</param>
    /// <returns><paramref name="value"/> plus <see cref="Epsilon"/>.</returns>
    /// <exception cref="OverflowException"><paramref name="value"/> is <see cref="MaxValue"/>.</exception>
    /// <seealso cref="operator ++(Fixed)"/>
    /// <seealso cref="MaxValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/checked-and-unchecked">C# reference: <c>checked</c> and <c>unchecked</c></seealso>
    public static Fixed operator checked ++(Fixed value) => new(checked(value._bits + 1));

    /// <summary>Decrements by the smallest unit with overflow checking.</summary>
    /// <param name="value">The value to decrement.</param>
    /// <returns><paramref name="value"/> minus <see cref="Epsilon"/>.</returns>
    /// <exception cref="OverflowException"><paramref name="value"/> is <see cref="MinValue"/>.</exception>
    /// <seealso cref="operator --(Fixed)"/>
    /// <seealso cref="MinValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/checked-and-unchecked">C# reference: <c>checked</c> and <c>unchecked</c></seealso>
    public static Fixed operator checked --(Fixed value) => new(checked(value._bits - 1));

    // ────────────────────────────── Comparison ──────────────────────────────

    /// <summary>Equality.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when both operands have identical raw bits.</returns>
    /// <remarks>Bit equality is value equality: every bit pattern of the underlying <see cref="int"/> is a valid <see cref="Fixed"/>.</remarks>
    /// <seealso cref="Equals(Fixed)"/>
    /// <seealso cref="operator !=(Fixed, Fixed)"/>
    public static bool operator ==(Fixed a, Fixed b) => a._bits == b._bits;

    /// <summary>Inequality.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when the operands have different raw bits.</returns>
    /// <seealso cref="Equals(Fixed)"/>
    /// <seealso cref="operator ==(Fixed, Fixed)"/>
    public static bool operator !=(Fixed a, Fixed b) => a._bits != b._bits;

    /// <summary>Less-than.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="a"/>'s raw bits are less than <paramref name="b"/>'s.</returns>
    /// <remarks>Raw-bit comparison matches numeric comparison because the representation is two's complement over a fixed scale.</remarks>
    /// <seealso cref="CompareTo(Fixed)"/>
    /// <seealso cref="operator >(Fixed, Fixed)"/>
    public static bool operator <(Fixed a, Fixed b) => a._bits < b._bits;

    /// <summary>Greater-than.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="a"/>'s raw bits are greater than <paramref name="b"/>'s.</returns>
    /// <seealso cref="CompareTo(Fixed)"/>
    /// <seealso cref="operator &lt;(Fixed, Fixed)"/>
    public static bool operator >(Fixed a, Fixed b) => a._bits > b._bits;

    /// <summary>Less-than-or-equal.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="a"/>'s raw bits are less than or equal to <paramref name="b"/>'s.</returns>
    /// <seealso cref="CompareTo(Fixed)"/>
    /// <seealso cref="operator >=(Fixed, Fixed)"/>
    public static bool operator <=(Fixed a, Fixed b) => a._bits <= b._bits;

    /// <summary>Greater-than-or-equal.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="a"/>'s raw bits are greater than or equal to <paramref name="b"/>'s.</returns>
    /// <seealso cref="CompareTo(Fixed)"/>
    /// <seealso cref="operator &lt;=(Fixed, Fixed)"/>
    public static bool operator >=(Fixed a, Fixed b) => a._bits >= b._bits;

    /// <inheritdoc/>
    /// <param name="other">The value to compare with.</param>
    /// <returns><see langword="true"/> when both values have identical raw bits.</returns>
    /// <seealso cref="operator ==(Fixed, Fixed)"/>
    /// <seealso cref="IEquatable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1">.NET API: <c>IEquatable&lt;T&gt;</c></seealso>
    public readonly bool Equals(Fixed other) => _bits == other._bits;

    /// <inheritdoc/>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is a <see cref="Fixed"/> with identical raw bits.</returns>
    /// <seealso cref="Equals(Fixed)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.object.equals">.NET API: <c>Object.Equals</c></seealso>
    public readonly override bool Equals(object? obj) => obj is Fixed other && Equals(other);

    /// <inheritdoc/>
    /// <returns>A hash code derived from the raw 32-bit value.</returns>
    /// <seealso cref="Bits"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.object.gethashcode">.NET API: <c>Object.GetHashCode</c></seealso>
    public readonly override int GetHashCode() => _bits.GetHashCode();

    /// <inheritdoc/>
    /// <param name="other">The value to compare with.</param>
    /// <returns>A signed comparison of the raw bit representations.</returns>
    /// <seealso cref="CompareTo(object?)"/>
    /// <seealso cref="IComparable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.icomparable-1">.NET API: <c>IComparable&lt;T&gt;</c></seealso>
    public readonly int CompareTo(Fixed other) => _bits.CompareTo(other._bits);

    /// <inheritdoc/>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>A positive value when this instance is greater; zero when equal; negative when less.</returns>
    /// <exception cref="ArgumentException"><paramref name="obj"/> is neither <c>null</c> nor a <see cref="Fixed"/>.</exception>
    /// <remarks>A <c>null</c> argument is treated as less than any non-null value, per the <see cref="IComparable"/> contract.</remarks>
    /// <seealso cref="CompareTo(Fixed)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.icomparable">.NET API: <c>IComparable</c></seealso>
    public readonly int CompareTo(object? obj) => obj switch
    {
        null => 1,
        Fixed other => CompareTo(other),
        _ => throw new ArgumentException(
            $"Object must be of type {nameof(Fixed)}.", nameof(obj)),
    };

    // ────────────────────────────── Formatting ──────────────────────────────

    /// <inheritdoc/>
    /// <returns>The numeric value formatted with the invariant culture and the default format.</returns>
    /// <seealso cref="ToString(string?)"/>
    /// <seealso cref="ToString(string?, IFormatProvider?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.iformattable">.NET API: <c>IFormattable</c></seealso>
    public readonly override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Formats the value using the given format string.</summary>
    /// <param name="format">A standard or custom numeric format string, or <c>null</c> for the default.</param>
    /// <returns>The formatted value, using the invariant culture.</returns>
    /// <seealso cref="ToString()"/>
    /// <seealso cref="ToString(IFormatProvider?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings">.NET guide: Standard numeric format strings</seealso>
    public readonly string ToString(string? format) =>
        Value.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Formats the value using the given format provider.</summary>
    /// <param name="formatProvider">The provider to use for culture-specific formatting, or <c>null</c> for the current culture.</param>
    /// <returns>The formatted value.</returns>
    /// <seealso cref="ToString()"/>
    /// <seealso cref="ToString(string?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.iformatprovider">.NET API: <c>IFormatProvider</c></seealso>
    public readonly string ToString(IFormatProvider? formatProvider) =>
        Value.ToString(formatProvider);

    /// <inheritdoc/>
    /// <param name="format">A standard or custom numeric format string, or <c>null</c> for the default.</param>
    /// <param name="formatProvider">The provider to use for culture-specific formatting, or <c>null</c> for the current culture.</param>
    /// <returns>The formatted value.</returns>
    /// <seealso cref="ToString(string?)"/>
    /// <seealso cref="ToString(IFormatProvider?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.iformattable">.NET API: <c>IFormattable</c></seealso>
    public readonly string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);

    /// <inheritdoc/>
    /// <param name="destination">The span to write the formatted characters into.</param>
    /// <param name="charsWritten">When this method returns, the number of characters written.</param>
    /// <param name="format">A standard or custom numeric format string, or an empty span for the default.</param>
    /// <param name="provider">The provider to use for culture-specific formatting, or <c>null</c> for the current culture.</param>
    /// <returns><see langword="true"/> when the value was formatted successfully; <see langword="false"/> when <paramref name="destination"/> was too small.</returns>
    /// <seealso cref="ToString(string?, IFormatProvider?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.ispanformattable">.NET API: <c>ISpanFormattable</c></seealso>
    public readonly bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider) =>
        Value.TryFormat(destination, out charsWritten, format, provider);
}
