using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Binary;

/// <summary>A value type that can convert itself between native byte order and big-endian byte order. Implemented by both primitives (<see cref="Tag"/>, <see cref="Fixed"/>, <see cref="F2Dot14"/>, <see cref="UInt24"/>, <see cref="Int24"/>) and by fixed-layout record structs read in bulk.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>OpenType files store every multi-byte value in big-endian order; see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification, Data Types</see>.</description></item>
/// <item><description>The reversal is applied once at read time — see <see cref="Source.ReadBigEndianStructAt{T}(long)"/> and <see cref="Cursor.ReadBigEndianStruct{T}"/> — so callers never need to reverse a field twice.</description></item>
/// <item><description>Implementations must satisfy the symmetry property <c>ReverseEndianness(ReverseEndianness(x)) == x</c> for every value <c>x</c>. This is what makes a double reversal a no-op on the read path.</description></item>
/// <item><description>Do not implement this on a struct whose fields are not all multi-byte-aligned primitives. Partial reversal produces a value that is neither native nor big-endian.</description></item>
/// </list>
/// <para>For the <c>BinaryPrimitives.ReverseEndianness</c> methods used to implement this interface on the built-in numeric types, see <see href="https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives.reverseendianness">.NET API, <c>BinaryPrimitives.ReverseEndianness</c></see>.</para>
/// </remarks>
/// <typeparam name="T">The implementing type. Must be an unmanaged value type.</typeparam>
/// <seealso cref="Source.ReadBigEndianStructAt{T}(long)"/>
/// <seealso cref="Source.ReadBigEndianStructArrayAt{T}(long, Span{T})"/>
/// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
/// <seealso cref="Cursor.PeekBigEndianStruct{T}"/>
/// <seealso cref="Tag"/>
/// <seealso cref="Fixed"/>
/// <seealso cref="F2Dot14"/>
/// <seealso cref="UInt24"/>
/// <seealso cref="Int24"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives.reverseendianness">.NET API: <c>BinaryPrimitives.ReverseEndianness</c></seealso>
public interface IBigEndianStruct<T> where T : unmanaged
{
    /// <summary>Returns <paramref name="value"/> with its bytes reversed. On a little-endian host this converts between native and big-endian storage; on a big-endian host the result differs from the input (the operation is unconditional).</summary>
    /// <param name="value">The value whose bytes are to be reversed.</param>
    /// <returns>A new value with the same bytes as <paramref name="value"/>, in reversed order.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The operation is unconditional: it does not consult <see cref="System.BitConverter.IsLittleEndian"/>. Callers that need host-aware behavior wrap the call in a host-endianness check; the readers in <see cref="Source"/> and <see cref="Cursor"/> do exactly that.</description></item>
    /// <item><description>For byte-width fields (8-bit), the implementation is expected to return the input unchanged, since a single byte has no byte order.</description></item>
    /// <item><description>Implementations must be pure: no mutation of <paramref name="value"/>, no side effects, no allocation.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Typical implementation on a fixed-layout struct:
    /// public static Header ReverseEndianness(Header v) => new()
    /// {
    ///     Version = BinaryPrimitives.ReverseEndianness(v.Version),
    ///     ItalicAngle = Fixed.ReverseEndianness(v.ItalicAngle),
    ///     UnderlinePosition = BinaryPrimitives.ReverseEndianness(v.UnderlinePosition),
    ///     // ...remaining fields
    /// };
    /// </code>
    /// </example>
    /// <seealso cref="Source.ReadBigEndianStructAt{T}(long)"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    /// <seealso cref="System.BitConverter.IsLittleEndian"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives.reverseendianness">.NET API: <c>BinaryPrimitives.ReverseEndianness</c></seealso>
    static abstract T ReverseEndianness(T value);
}
