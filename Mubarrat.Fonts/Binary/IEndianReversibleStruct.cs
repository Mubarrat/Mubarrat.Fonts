using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Binary;

/// <summary>
/// Defines a value type whose binary representation can be transformed by
/// reversing the byte order of its serialized representation.
/// </summary>
/// <remarks>
/// <para>
/// This interface is used by the binary layer for fixed-layout values whose
/// serialized representation has a defined byte order. The implementation
/// provides the unconditional byte-reversal operation; the reader determines
/// whether that operation is required for the current host and serialized
/// format.
/// </para>
/// <para>
/// The interface itself does not identify a particular serialized byte order.
/// In particular, it does not mean that the implementing type is inherently
/// big-endian. It defines the reversible transformation between byte orders.
/// Format-specific readers determine which byte order is used by the format.
/// </para>
/// <para>
/// SFNT-based font formats use big-endian byte order for their multi-byte
/// numeric data. This interface is therefore used by the SFNT/OpenType binary
/// readers when converting fixed-layout values between their serialized
/// representation and the native representation used by the host.
/// </para>
/// <para>
/// Implementations may represent individual binary primitives or complete
/// fixed-layout record structs read in bulk. Examples include
/// <see cref="Tag"/>, <see cref="Fixed"/>, <see cref="F2Dot14"/>,
/// <see cref="UInt24"/>, <see cref="Int24"/>, and fixed-layout font records.
/// </para>
/// <para>
/// The operation must be an involution: applying it twice must restore the
/// original value.
/// </para>
/// <code>
/// ReverseEndianness(ReverseEndianness(value)) == value
/// </code>
/// <para>
/// Every multi-byte component participating in the serialized representation
/// must be reversed exactly once. Single-byte components have no byte order
/// and therefore remain unchanged.
/// </para>
/// <para>
/// Implementations must not partially reverse a composite value. If a value
/// contains nested fixed-layout values whose serialized representations have
/// multiple-byte components, those components must be reversed consistently
/// with the enclosing representation.
/// </para>
/// <para>
/// The operation is unconditional and does not inspect
/// <see cref="System.BitConverter.IsLittleEndian"/>. Host-aware behavior
/// belongs to the caller. The readers in <see cref="Source"/> and
/// <see cref="Cursor"/> determine whether reversal is required before invoking
/// this operation.
/// </para>
/// <para>
/// Implementations should be pure: they must not mutate the input, perform
/// observable side effects, or require heap allocation as part of the
/// reversal operation.
/// </para>
/// <para>
/// The SFNT byte-order and binary data-type conventions are described by both
/// the OpenType specification and Apple's TrueType Reference Manual. The
/// OpenType specification describes the OpenType Font File and its data types,
/// while Apple's documentation describes the corresponding sfnt data types
/// and font-table organization.
/// </para>
/// </remarks>
/// <typeparam name="T">
/// The unmanaged value type whose byte-order representation can be reversed.
/// </typeparam>
/// <seealso cref="Source.ReadEndianReversibleStructAt{T}(long)"/>
/// <seealso cref="Source.ReadEndianReversibleStructArrayAt{T}(long, Span{T})"/>
/// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
/// <seealso cref="Cursor.PeekBigEndianStruct{T}"/>
/// <seealso cref="Tag"/>
/// <seealso cref="Fixed"/>
/// <seealso cref="F2Dot14"/>
/// <seealso cref="UInt24"/>
/// <seealso cref="Int24"/>
/// <seealso cref="System.BitConverter.IsLittleEndian"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff">
/// Microsoft OpenType specification: OpenType Font File
/// </seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">
/// Microsoft OpenType specification: Data Types
/// </seealso>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6.html">
/// Apple TrueType Reference Manual: Font Tables and sfnt Data Types
/// </seealso>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/">
/// Apple TrueType Reference Manual
/// </seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives.reverseendianness">
/// .NET API: BinaryPrimitives.ReverseEndianness
/// </seealso>
public interface IEndianReversibleStruct<T>
    where T : unmanaged
{
    /// <summary>
    /// Returns <paramref name="value"/> with the byte order of its serialized
    /// representation reversed.
    /// </summary>
    /// <param name="value">
    /// The value whose byte-order representation is to be reversed.
    /// </param>
    /// <returns>
    /// A value whose multi-byte components have their byte order reversed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The operation is unconditional. It does not inspect
    /// <see cref="System.BitConverter.IsLittleEndian"/> and does not determine
    /// whether conversion is necessary. A caller requiring host-aware behavior
    /// must determine whether reversal is required before invoking this method.
    /// </para>
    /// <para>
    /// For a single-byte component, reversal has no effect. For a composite
    /// value, every multi-byte component belonging to its serialized
    /// representation must be reversed.
    /// </para>
    /// <para>
    /// The operation must be pure and must satisfy the symmetry property:
    /// </para>
    /// <code>
    /// ReverseEndianness(ReverseEndianness(value)) == value
    /// </code>
    /// <para>
    /// This property is required because the binary readers may use the
    /// operation to convert between serialized and native representations.
    /// Applying the operation twice must therefore be a no-op.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// A fixed-layout record typically reverses each multi-byte field using
    /// the appropriate primitive reversal operation:
    /// </para>
    /// <code>
    /// public static Header ReverseEndianness(Header value) => new()
    /// {
    ///     Version = BinaryPrimitives.ReverseEndianness(value.Version),
    ///     ItalicAngle = Fixed.ReverseEndianness(value.ItalicAngle),
    ///     UnderlinePosition =
    ///         BinaryPrimitives.ReverseEndianness(value.UnderlinePosition)
    /// };
    /// </code>
    /// </example>
    /// <seealso cref="Source.ReadEndianReversibleStructAt{T}(long)"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    /// <seealso cref="System.BitConverter.IsLittleEndian"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">
    /// Microsoft OpenType specification: Data Types
    /// </seealso>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6.html">
    /// Apple TrueType Reference Manual: sfnt Data Types
    /// </seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives.reverseendianness">
    /// .NET API: BinaryPrimitives.ReverseEndianness
    /// </seealso>
    static abstract T ReverseEndianness(T value);
}
