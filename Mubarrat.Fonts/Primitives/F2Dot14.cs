using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;

namespace Mubarrat.Fonts.Primitives;

/// <summary>A 2.14 signed fixed-point value, stored as a big-endian <see cref="short"/> on disk. Range is [-2.0, 1.99993896484375]; used for normalized variation coordinates in <c>fvar</c>-dependent tables, tuple records, and CFF2 blend operands.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The wire format is a signed 16-bit big-endian integer whose value is <c>raw / 16384.0</c>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">F2DOT14 data type</see> in the OpenType specification.</description></item>
/// <item><description>The struct is <c>Pack = 1</c> and blittable; its in-memory layout matches the on-disk layout on both little- and big-endian hosts, so it can be read directly via <see cref="Source.ReadEndianReversibleStructAt{T}(long)"/>.</description></item>
/// <item><description>Endianness conversion is provided by <see cref="ReverseEndianness(F2Dot14)"/> as required by <see cref="IEndianReversibleStruct{T}"/>.</description></item>
/// <item><description>Conversions to <see cref="double"/> and <see cref="float"/> are implicit; conversions from floating-point types are explicit because they clamp rather than wrap.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Fixed"/>
/// <seealso cref="UInt24"/>
/// <seealso cref="Int24"/>
/// <seealso cref="Tag"/>
/// <seealso cref="IEndianReversibleStruct{T}"/>
/// <seealso cref="Source.ReadEndianReversibleStructAt{T}(long)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.numerics">.NET API: <c>System.Numerics</c></seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct F2Dot14 :
    IComparable,
    IComparable<F2Dot14>,
    IEquatable<F2Dot14>,
    IFormattable,
    IEndianReversibleStruct<F2Dot14>
{
    /// <summary>The number of fractional bits.</summary>
    /// <remarks>Always <c>14</c>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">F2DOT14 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="One"/>
    /// <seealso cref="Epsilon"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    public const int FractionBits = 14;

    /// <summary>The scale factor: 1 integer unit equals 16384 raw bits.</summary>
    /// <remarks>Equal to <c>1 &lt;&lt; <see cref="FractionBits"/></c>. The raw value of <c>1.0</c> in this representation.</remarks>
    /// <seealso cref="FractionBits"/>
    /// <seealso cref="OneValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    public const int One = 1 << FractionBits;

    /// <summary>The smallest representable increment: 1/16384.</summary>
    /// <remarks>Equal to <c>1.0 / <see cref="One"/></c>. The difference between adjacent representable values.</remarks>
    /// <seealso cref="One"/>
    /// <seealso cref="FractionBits"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    public const double Epsilon = 1.0 / One;

    /// <summary>The zero value.</summary>
    /// <value>The default <see cref="F2Dot14"/>, with raw bits <c>0</c>.</value>
    /// <seealso cref="IsZero"/>
    /// <seealso cref="OneValue"/>
    /// <seealso cref="NegativeOneValue"/>
    public static readonly F2Dot14 Zero = default;

    /// <summary>The value 1.0.</summary>
    /// <value>The <see cref="F2Dot14"/> whose raw bits equal <see cref="One"/>.</value>
    /// <seealso cref="One"/>
    /// <seealso cref="Zero"/>
    /// <seealso cref="NegativeOneValue"/>
    public static readonly F2Dot14 OneValue = new(One);

    /// <summary>The value -1.0.</summary>
    /// <value>The <see cref="F2Dot14"/> whose raw bits equal <c>-<see cref="One"/></c>.</value>
    /// <seealso cref="One"/>
    /// <seealso cref="Zero"/>
    /// <seealso cref="OneValue"/>
    public static readonly F2Dot14 NegativeOneValue = new(-One);

    /// <summary>The minimum value: -2.0.</summary>
    /// <value>The <see cref="F2Dot14"/> whose raw bits equal <see cref="short.MinValue"/>.</value>
    /// <seealso cref="MaxValue"/>
    /// <seealso cref="FromDouble(double)"/>
    public static readonly F2Dot14 MinValue = new(short.MinValue);

    /// <summary>The maximum value: <c>short.MaxValue / 16384.0</c>.</summary>
    /// <value>The <see cref="F2Dot14"/> whose raw bits equal <see cref="short.MaxValue"/>.</value>
    /// <seealso cref="MinValue"/>
    /// <seealso cref="FromDouble(double)"/>
    public static readonly F2Dot14 MaxValue = new(short.MaxValue);

    private readonly short _bits;

    /// <summary>Creates a value from its raw 2.14 bit representation.</summary>
    /// <param name="bits">The signed 16-bit raw value; the caller supplies it as-is without scaling.</param>
    /// <remarks>This is the unchecked constructor. To clamp a floating-point value into range, use <see cref="FromDouble(double)"/> or the explicit cast.</remarks>
    /// <seealso cref="FromBits(short)"/>
    /// <seealso cref="FromDouble(double)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public F2Dot14(short bits) => _bits = bits;

    /// <summary>Creates a value from a double, clamping to the representable range.</summary>
    /// <param name="value">The value to convert. Values at or above <c>2.0</c> are clamped to <see cref="MaxValue"/>; values at or below <c>-2.0</c> are clamped to <see cref="MinValue"/>.</param>
    /// <returns>The nearest representable <see cref="F2Dot14"/>, truncated toward zero within the representable range.</returns>
    /// <remarks>The conversion multiplies by <see cref="One"/> and casts to <see cref="short"/>; the clamping guards run first so no wrap can occur.</remarks>
    /// <example>
    /// <code>
    /// var a = F2Dot14.FromDouble(0.5);   // raw 8192
    /// var b = F2Dot14.FromDouble(3.0);   // clamped to MaxValue
    /// var c = F2Dot14.FromDouble(-3.0);  // clamped to MinValue
    /// </code>
    /// </example>
    /// <seealso cref="MaxValue"/>
    /// <seealso cref="MinValue"/>
    /// <seealso cref="FromBits(short)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    public static F2Dot14 FromDouble(double value)
    {
        if (value >= 2.0) return MaxValue;
        if (value <= -2.0) return MinValue;
        return new(unchecked((short)(value * One)));
    }

    /// <summary>Creates a value from its raw bit representation.</summary>
    /// <param name="bits">The signed 16-bit raw value.</param>
    /// <returns>A <see cref="F2Dot14"/> wrapping <paramref name="bits"/> unchanged.</returns>
    /// <remarks>Equivalent to calling the constructor directly; provided as a named factory for call-site readability.</remarks>
    /// <seealso cref="F2Dot14(short)"/>
    /// <seealso cref="Bits"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static F2Dot14 FromBits(short bits) => new(bits);

    /// <summary>Gets the raw 2.14 bit representation.</summary>
    /// <value>The signed 16-bit value whose numeric interpretation is <c>raw / 16384.0</c>.</value>
    /// <seealso cref="Value"/>
    /// <seealso cref="IntegerPart"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    public readonly short Bits => _bits;

    /// <summary>Gets the numeric value as a <see cref="double"/>.</summary>
    /// <value>The raw bits divided by <see cref="One"/>. Range is [-2.0, <c>short.MaxValue / 16384.0</c>].</value>
    /// <seealso cref="Bits"/>
    /// <seealso cref="IntegerPart"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    public readonly double Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _bits / 16384.0;
    }

    /// <summary>Gets the integer part (signed).</summary>
    /// <value>The raw bits shifted right by <see cref="FractionBits"/>, discarding the fractional part. Range is [-2, 1].</value>
    /// <seealso cref="Value"/>
    /// <seealso cref="Bits"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    public readonly int IntegerPart => _bits >> FractionBits;

    /// <summary>True when this is exactly zero.</summary>
    /// <value><see langword="true"/> when <see cref="Bits"/> equals <c>0</c>.</value>
    /// <seealso cref="Zero"/>
    /// <seealso cref="Bits"/>
    public readonly bool IsZero => _bits == 0;

    /// <inheritdoc/>
    /// <param name="value">The value whose bytes are to be reversed.</param>
    /// <returns>A <see cref="F2Dot14"/> whose bytes are the reverse of <paramref name="value"/>'s.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The operation is unconditional. The host-endianness check is applied by the reader (<see cref="Source.ReadEndianReversibleStructAt{T}(long)"/>, <see cref="Cursor.ReadBigEndianStruct{T}"/>), not by this method.</description></item>
    /// <item><description>Symmetric: <c>ReverseEndianness(ReverseEndianness(x)) == x</c> for every <c>x</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">F2DOT14 data type</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Source.ReadEndianReversibleStructAt{T}(long)"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static F2Dot14 ReverseEndianness(F2Dot14 value) =>
        new(BinaryPrimitives.ReverseEndianness(value._bits));

    /// <summary>Converts to <see cref="double"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The numeric value as a <see cref="double"/>.</returns>
    /// <remarks>The conversion is lossless: every <see cref="F2Dot14"/> maps to a distinct <see cref="double"/>.</remarks>
    /// <seealso cref="Value"/>
    /// <seealso cref="FromDouble(double)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static implicit operator double(F2Dot14 value) => value.Value;

    /// <summary>Converts to <see cref="float"/>.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The numeric value as a <see cref="float"/>.</returns>
    /// <remarks>The conversion goes through <see cref="double"/> and then narrows to <see cref="float"/>; <see cref="float"/> has enough precision to represent every <see cref="F2Dot14"/> exactly.</remarks>
    /// <seealso cref="Value"/>
    /// <seealso cref="FromDouble(double)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static implicit operator float(F2Dot14 value) => (float)value.Value;

    /// <summary>Converts from <see cref="double"/>, clamping.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The nearest representable <see cref="F2Dot14"/>.</returns>
    /// <remarks>Explicit because the conversion clamps out-of-range values silently, which should not happen implicitly at every assignment site.</remarks>
    /// <seealso cref="FromDouble(double)"/>
    /// <seealso cref="MaxValue"/>
    /// <seealso cref="MinValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator F2Dot14(double value) => FromDouble(value);

    /// <summary>Converts from <see cref="float"/>, clamping.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The nearest representable <see cref="F2Dot14"/>.</returns>
    /// <remarks>Explicit because the conversion clamps out-of-range values silently.</remarks>
    /// <seealso cref="FromDouble(double)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static explicit operator F2Dot14(float value) => FromDouble(value);

    // Arithmetic mirrors Fixed but at the smaller scale.

    /// <summary>Adds two values. Unchecked overflow wraps.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The sum, computed on the raw 2.14 representation.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The addition is performed on the raw <see cref="short"/> bits; the scale factor cancels.</description></item>
    /// <item><description>Overflow is unchecked, so the result wraps mod 2<sup>16</sup>. Callers needing saturation should check the operands against <see cref="MaxValue"/> and <see cref="MinValue"/> first.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">F2DOT14 data type</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="operator -(F2Dot14, F2Dot14)"/>
    /// <seealso cref="MaxValue"/>
    /// <seealso cref="MinValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    public static F2Dot14 operator +(F2Dot14 a, F2Dot14 b) =>
        new(unchecked((short)(a._bits + b._bits)));

    /// <summary>Subtracts two values. Unchecked overflow wraps.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The difference, computed on the raw 2.14 representation.</returns>
    /// <remarks>Overflow is unchecked, so the result wraps mod 2<sup>16</sup>.</remarks>
    /// <seealso cref="operator +(F2Dot14, F2Dot14)"/>
    /// <seealso cref="MaxValue"/>
    /// <seealso cref="MinValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    public static F2Dot14 operator -(F2Dot14 a, F2Dot14 b) =>
        new(unchecked((short)(a._bits - b._bits)));

    /// <summary>Multiplies two values, rounding to the nearest fractional unit.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns>The product, rounded to the nearest representable <see cref="F2Dot14"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The product of two 2.14 values has 28 fractional bits; the implementation adds <c>1 &lt;&lt; 13</c> before shifting right by 14 to round half-away-from-zero on positive products.</description></item>
    /// <item><description>The raw multiply and the addition of the rounding constant use <see cref="int"/> arithmetic, avoiding intermediate overflow.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">F2DOT14 data type</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FractionBits"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    public static F2Dot14 operator *(F2Dot14 a, F2Dot14 b)
    {
        int product = a._bits * b._bits;
        int rounding = 1 << (FractionBits - 1);
        return new(unchecked((short)((product + rounding) >> FractionBits)));
    }

    /// <summary>Negates the value. Unchecked overflow of <see cref="MinValue"/> wraps.</summary>
    /// <param name="value">The value to negate.</param>
    /// <returns>The arithmetic negation, computed on the raw 2.14 representation.</returns>
    /// <remarks>Negating <see cref="MinValue"/> produces <see cref="MinValue"/> itself because <c>-(-32768)</c> wraps to <c>-32768</c> in 16-bit two's complement.</remarks>
    /// <seealso cref="operator +(F2Dot14)"/>
    /// <seealso cref="MinValue"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    public static F2Dot14 operator -(F2Dot14 value) => new(unchecked((short)-value._bits));

    /// <summary>Returns the value unchanged.</summary>
    /// <param name="value">The value.</param>
    /// <returns><paramref name="value"/> unchanged.</returns>
    /// <remarks>Provided for symmetry with the unary negation operator.</remarks>
    /// <seealso cref="operator -(F2Dot14)"/>
    public static F2Dot14 operator +(F2Dot14 value) => value;

    /// <summary>Equality on the raw bit representation.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when both operands have identical raw bits.</returns>
    /// <remarks>Because every bit pattern is a valid <see cref="F2Dot14"/>, bit equality is value equality; there is no <c>NaN</c> equivalent.</remarks>
    /// <seealso cref="Equals(F2Dot14)"/>
    /// <seealso cref="operator !=(F2Dot14, F2Dot14)"/>
    public static bool operator ==(F2Dot14 a, F2Dot14 b) => a._bits == b._bits;

    /// <summary>Inequality on the raw bit representation.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when the operands have different raw bits.</returns>
    /// <seealso cref="Equals(F2Dot14)"/>
    /// <seealso cref="operator ==(F2Dot14, F2Dot14)"/>
    public static bool operator !=(F2Dot14 a, F2Dot14 b) => a._bits != b._bits;

    /// <summary>Less-than.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="a"/>'s raw bits are less than <paramref name="b"/>'s.</returns>
    /// <remarks>Raw-bit comparison matches numeric comparison because the representation is two's complement over a fixed scale.</remarks>
    /// <seealso cref="CompareTo(F2Dot14)"/>
    /// <seealso cref="operator >(F2Dot14, F2Dot14)"/>
    public static bool operator <(F2Dot14 a, F2Dot14 b) => a._bits < b._bits;

    /// <summary>Greater-than.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="a"/>'s raw bits are greater than <paramref name="b"/>'s.</returns>
    /// <seealso cref="CompareTo(F2Dot14)"/>
    /// <seealso cref="operator &lt;(F2Dot14, F2Dot14)"/>
    public static bool operator >(F2Dot14 a, F2Dot14 b) => a._bits > b._bits;

    /// <summary>Less-than-or-equal.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="a"/>'s raw bits are less than or equal to <paramref name="b"/>'s.</returns>
    /// <seealso cref="CompareTo(F2Dot14)"/>
    /// <seealso cref="operator >=(F2Dot14, F2Dot14)"/>
    public static bool operator <=(F2Dot14 a, F2Dot14 b) => a._bits <= b._bits;

    /// <summary>Greater-than-or-equal.</summary>
    /// <param name="a">The left operand.</param>
    /// <param name="b">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="a"/>'s raw bits are greater than or equal to <paramref name="b"/>'s.</returns>
    /// <seealso cref="CompareTo(F2Dot14)"/>
    /// <seealso cref="operator &lt;=(F2Dot14, F2Dot14)"/>
    public static bool operator >=(F2Dot14 a, F2Dot14 b) => a._bits >= b._bits;

    /// <inheritdoc/>
    /// <param name="other">The value to compare with.</param>
    /// <returns><see langword="true"/> when both values have identical raw bits.</returns>
    /// <seealso cref="operator ==(F2Dot14, F2Dot14)"/>
    /// <seealso cref="IEquatable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1">.NET API: <c>IEquatable&lt;T&gt;</c></seealso>
    public readonly bool Equals(F2Dot14 other) => _bits == other._bits;

    /// <inheritdoc/>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is a <see cref="F2Dot14"/> with identical raw bits.</returns>
    /// <seealso cref="Equals(F2Dot14)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.object.equals">.NET API: <c>Object.Equals</c></seealso>
    public readonly override bool Equals(object? obj) => obj is F2Dot14 other && Equals(other);

    /// <inheritdoc/>
    /// <returns>A hash code derived from the raw 16-bit value.</returns>
    /// <seealso cref="Bits"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.object.gethashcode">.NET API: <c>Object.GetHashCode</c></seealso>
    public readonly override int GetHashCode() => _bits.GetHashCode();

    /// <inheritdoc/>
    /// <param name="other">The value to compare with.</param>
    /// <returns>A signed comparison of the raw bit representations.</returns>
    /// <seealso cref="CompareTo(object?)"/>
    /// <seealso cref="IComparable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.icomparable-1">.NET API: <c>IComparable&lt;T&gt;</c></seealso>
    public readonly int CompareTo(F2Dot14 other) => _bits.CompareTo(other._bits);

    /// <inheritdoc/>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>A positive value when this instance is greater; zero when equal; negative when less.</returns>
    /// <exception cref="ArgumentException"><paramref name="obj"/> is neither <c>null</c> nor a <see cref="F2Dot14"/>.</exception>
    /// <remarks>A <c>null</c> argument is treated as less than any non-null value, per the <see cref="IComparable"/> contract.</remarks>
    /// <seealso cref="CompareTo(F2Dot14)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.icomparable">.NET API: <c>IComparable</c></seealso>
    public readonly int CompareTo(object? obj) => obj switch
    {
        null => 1,
        F2Dot14 other => CompareTo(other),
        _ => throw new ArgumentException(
            $"Object must be of type {nameof(F2Dot14)}.", nameof(obj)),
    };

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
