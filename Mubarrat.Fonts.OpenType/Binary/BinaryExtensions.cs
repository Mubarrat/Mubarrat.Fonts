using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.OpenType.Binary;

/// <summary>Extension methods that bridge headers, records, and sources. Covers three groups: header-to-record conversion, header-record array reads over <see cref="Source"/> and <see cref="Cursor"/>, and big-endian enum array reads.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Enums cannot implement <see cref="IBigEndianStruct{T}"/>, so they need their own read path — see <see cref="ReadEnumArrayAt{TEnum}(Source, long, Span{TEnum})"/> and its variants.</description></item>
/// <item><description>Every reader assumes the source bytes are big-endian on disk; on little-endian hosts the multi-byte fields are reversed before the value is returned.</description></item>
/// <item><description>The on-disk representation of each primitive is defined by the OpenType data types: <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification, Data Types</see>.</description></item>
/// </list>
/// </remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives">.NET API: <c>BinaryPrimitives</c></seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.memorymarshal">.NET API: <c>MemoryMarshal</c></seealso>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "ApiDesign",
    "RS0026:Do not add multiple public overloads with optional parameters",
    Scope = "type",
    Target = "~T:Mubarrat.Fonts.OpenType.Binary.BinaryExtensions",
    Justification = "Cursor overloads are disambiguated by the type of their first parameter (int count vs Span<T> destination); the optional 'context' parameter cannot introduce ambiguity between them.")]
public static class BinaryExtensions
{
    /// <summary>Builds a <typeparamref name="T"/> from this header.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IHeaderRecord{T, THeader}"/> for <typeparamref name="THeader"/>. Both plain and big-endian header records satisfy this constraint, because <see cref="IBigEndianHeaderRecord{T, THeader}"/> extends <see cref="IHeaderRecord{T, THeader}"/>.</typeparam>
    /// <typeparam name="THeader">The header type. Inferred from the receiver.</typeparam>
    /// <param name="header">The header to convert. Passed by value; small structs cost nothing and the JIT forwards it to the <c>in</c> parameter of <c>FromHeader</c> without a second copy.</param>
    /// <param name="context">External values the record needs, or <c>null</c> when the header is sufficient.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidDataException">The header's fields describe a structure the record cannot represent, or the context does not supply a value the record requires.</exception>
    /// <remarks>For the <c>in</c>-parameter optimization this method relies on, see <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/in-parameter-modifier"><c>in</c> parameter modifier</see>.</remarks>
    /// <seealso cref="IHeaderRecord{T, THeader}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/in-parameter-modifier">C# reference: <c>in</c> parameter modifier</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ToRecord<T, THeader>(this THeader header, object? context = null) where T : IHeaderRecord<T, THeader> where THeader : unmanaged => T.FromHeader(in header, context);

    /// <summary>Builds one <typeparamref name="T"/> per element of <paramref name="headers"/>.</summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <typeparam name="THeader">The header type. Inferred from the span.</typeparam>
    /// <param name="headers">The headers to convert.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the headers are sufficient. Every record receives the same context instance.</param>
    /// <returns>A new array of records, one per header, in order.</returns>
    /// <remarks>An empty input span returns an empty array — no allocation is performed.</remarks>
    /// <seealso cref="IHeaderRecord{T, THeader}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.readonlyspan-1">.NET API: <c>ReadOnlySpan&lt;T&gt;</c></seealso>
    public static T[] ToRecords<T, THeader>(this ReadOnlySpan<THeader> headers, object? context = null) where T : IHeaderRecord<T, THeader> where THeader : unmanaged
    {
        if (headers.IsEmpty) return [];

        var result = new T[headers.Length];
        for (int i = 0; i < headers.Length; i++)
            result[i] = T.FromHeader(in headers[i], context);
        return result;
    }

    /// <summary>Builds one <typeparamref name="T"/> per element of <paramref name="headers"/> into a caller-provided span.</summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <typeparam name="THeader">The header type. Inferred from the span.</typeparam>
    /// <param name="headers">The headers to convert.</param>
    /// <param name="destination">The span to fill. Its length must equal <paramref name="headers"/>'s length.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the headers are sufficient.</param>
    /// <exception cref="ArgumentException"><paramref name="destination"/> and <paramref name="headers"/> have different lengths.</exception>
    /// <remarks>The length check is performed before any element is written, so a length mismatch leaves <paramref name="destination"/> unmodified.</remarks>
    /// <seealso cref="IHeaderRecord{T, THeader}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.span-1">.NET API: <c>Span&lt;T&gt;</c></seealso>
    public static void ToRecords<T, THeader>(this ReadOnlySpan<THeader> headers, Span<T> destination, object? context = null) where T : IHeaderRecord<T, THeader> where THeader : unmanaged
    {
        if (destination.Length != headers.Length)
            throw new ArgumentException(
                $"{nameof(destination)} length {destination.Length} does not match " +
                $"{nameof(headers)} length {headers.Length}.",
                nameof(destination));

        for (int i = 0; i < headers.Length; i++)
            destination[i] = T.FromHeader(in headers[i], context);
    }

    /// <summary>Reads <paramref name="count"/> <typeparamref name="THeader"/> values starting at <paramref name="offset"/> with no endianness conversion, then converts each to a <typeparamref name="T"/> via <see cref="IHeaderRecord{T, THeader}.FromHeader"/>.</summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <typeparam name="THeader">The header type. Must be blittable and unpadded, and must not require big-endian reversal — otherwise use <see cref="ReadBigEndianHeaderRecordArrayAt{T, THeader}(Source, long, int, object?)"/>.</typeparam>
    /// <param name="source">The source to read from.</param>
    /// <param name="offset">Absolute offset of the first header.</param>
    /// <param name="count">The number of headers. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the headers are sufficient.</param>
    /// <returns>A new array of records, one per header, in source order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException"><c>count * sizeof(THeader)</c> bytes beginning at <paramref name="offset"/> exceed the source length.</exception>
    /// <remarks>The header layout must match the on-disk layout: blittable, sequentially laid out, unpadded, and already in native byte order. For big-endian headers, see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType data types</see>.</remarks>
    /// <seealso cref="IHeaderRecord{T, THeader}"/>
    /// <seealso cref="ReadBigEndianHeaderRecordArrayAt{T, THeader}(Source, long, int, object?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public static T[] ReadHeaderRecordArrayAt<T, THeader>(this Source source, long offset, int count, object? context = null) where T : IHeaderRecord<T, THeader> where THeader : unmanaged
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return [];

        THeader[] headers = source.ReadStructArrayAt<THeader>(offset, count);
        return headers.ToRecords<T, THeader>(context);
    }

    /// <summary>Reads <paramref name="count"/> big-endian <typeparamref name="THeader"/> values starting at <paramref name="offset"/>, reversing multi-byte fields on little-endian hosts, then converts each to a <typeparamref name="T"/> via <see cref="IHeaderRecord{T, THeader}.FromHeader"/>.</summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <typeparam name="THeader">The header type. Must implement <see cref="IBigEndianStruct{THeader}"/> so its multi-byte fields can be reversed.</typeparam>
    /// <param name="source">The source to read from.</param>
    /// <param name="offset">Absolute offset of the first header.</param>
    /// <param name="count">The number of headers. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the headers are sufficient.</param>
    /// <returns>A new array of records, one per header, in source order. Each record is built from a header whose multi-byte fields have already been reversed to native order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException"><c>count * sizeof(THeader)</c> bytes beginning at <paramref name="offset"/> exceed the source length.</exception>
    /// <remarks>This is the big-endian counterpart of <see cref="ReadHeaderRecordArrayAt{T, THeader}(Source, long, int, object?)"/> and should be used for every OpenType header, since the format is defined as big-endian on disk.</remarks>
    /// <seealso cref="IHeaderRecord{T, THeader}"/>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public static T[] ReadBigEndianHeaderRecordArrayAt<T, THeader>(this Source source, long offset, int count, object? context = null) where T : IHeaderRecord<T, THeader> where THeader : unmanaged, IBigEndianStruct<THeader>
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return [];

        return source.ReadBigEndianStructArrayAt<THeader>(offset, count)
            .ToRecords<T, THeader>(context);
    }

    /// <summary>Reads big-endian <typeparamref name="THeader"/> values starting at <paramref name="offset"/>, reversing multi-byte fields on little-endian hosts, and writes one <typeparamref name="T"/> per header into <paramref name="destination"/>.</summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <typeparam name="THeader">The header type. Must implement <see cref="IBigEndianStruct{THeader}"/> so its multi-byte fields can be reversed.</typeparam>
    /// <param name="source">The source to read from.</param>
    /// <param name="offset">Absolute offset of the first header.</param>
    /// <param name="destination">The span to fill. Its length determines the number of headers read.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the headers are sufficient.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
    /// <exception cref="EndOfStreamException"><c>destination.Length * sizeof(THeader)</c> bytes beginning at <paramref name="offset"/> exceed the source length.</exception>
    /// <remarks>The headers are materialized into a temporary array because the count is caller-controlled and can be arbitrarily large; a <c>stackalloc</c> path is not available for a generic <typeparamref name="THeader"/>.</remarks>
    /// <seealso cref="IHeaderRecord{T, THeader}"/>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public static void ReadBigEndianHeaderRecordArrayAt<T, THeader>(this Source source, long offset, Span<T> destination, object? context = null) where T : IHeaderRecord<T, THeader> where THeader : unmanaged, IBigEndianStruct<THeader>
    {
        ArgumentNullException.ThrowIfNull(source);
        if (destination.IsEmpty) return;

        // Read the headers as a temporary array, reverse in place, then convert.
        // A stackalloc path is not available because the count is caller-controlled
        // and can be large; the array is the correct trade-off.
        source.ReadBigEndianStructArrayAt<THeader>(offset, destination.Length).ToRecords(destination, context);
    }

    /// <summary>Reads <paramref name="count"/> <typeparamref name="THeader"/> values at the current position with no endianness conversion, converts each to a <typeparamref name="T"/> via <see cref="IHeaderRecord{T, THeader}.FromHeader"/>, and advances the cursor past them.</summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <typeparam name="THeader">The header type. Must be blittable and unpadded, and must not require big-endian reversal — otherwise use <see cref="ReadBigEndianHeaderRecordArray{T, THeader}(ref Cursor, int, object?)"/>.</typeparam>
    /// <param name="cursor">Cursor positioned at the first header.</param>
    /// <param name="count">The number of headers. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the headers are sufficient.</param>
    /// <returns>A new array of records, one per header, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException"><c>count * sizeof(THeader)</c> bytes beginning at <c>cursor.Position</c> exceed the source length.</exception>
    /// <remarks>On success the cursor advances by <c>count * sizeof(THeader)</c>. On failure the cursor is left unchanged, because the underlying read throws before mutating <see cref="Cursor.Position"/>.</remarks>
    /// <seealso cref="IHeaderRecord{T, THeader}"/>
    /// <seealso cref="ReadBigEndianHeaderRecordArray{T, THeader}(ref Cursor, int, object?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public static T[] ReadHeaderRecordArray<T, THeader>(this ref Cursor cursor, int count, object? context = null) where T : IHeaderRecord<T, THeader> where THeader : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return [];

        var result = new T[count];
        cursor.ReadHeaderRecordArray<T, THeader>(result, context);
        return result;
    }

    /// <summary>Reads <paramref name="destination"/>.Length <typeparamref name="THeader"/> values at the current position with no endianness conversion, converts each to a <typeparamref name="T"/>, and advances the cursor past them.</summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <typeparam name="THeader">The header type. Must be blittable and unpadded.</typeparam>
    /// <param name="cursor">Cursor positioned at the first header.</param>
    /// <param name="destination">The span to fill. Its length determines the number of headers read.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the headers are sufficient.</param>
    /// <exception cref="EndOfStreamException"><c>destination.Length * sizeof(THeader)</c> bytes beginning at <c>cursor.Position</c> exceed the source length.</exception>
    /// <remarks>The header layout must match the on-disk layout: blittable, sequentially laid out, unpadded, and already in native byte order.</remarks>
    /// <seealso cref="IHeaderRecord{T, THeader}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public static void ReadHeaderRecordArray<T, THeader>(this ref Cursor cursor, Span<T> destination, object? context = null) where T : IHeaderRecord<T, THeader> where THeader : unmanaged
    {
        if (destination.IsEmpty) return;

        // Read the headers as a temporary array, then convert. A stackalloc path is not
        // available because THeader is a caller-supplied generic and its count is
        // runtime-determined; the array is the correct trade-off.
        THeader[] headers = cursor.ReadStructArray<THeader>(destination.Length);
        headers.ToRecords(destination, context);
    }

    /// <summary>Reads <paramref name="count"/> big-endian <typeparamref name="THeader"/> values at the current position, reversing multi-byte fields on little-endian hosts, converts each to a <typeparamref name="T"/> via <see cref="IHeaderRecord{T, THeader}.FromHeader"/>, and advances the cursor past them.</summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <typeparam name="THeader">The header type. Must implement <see cref="IBigEndianStruct{THeader}"/> so its multi-byte fields can be reversed.</typeparam>
    /// <param name="cursor">Cursor positioned at the first header.</param>
    /// <param name="count">The number of headers. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the headers are sufficient.</param>
    /// <returns>A new array of records, one per header, in source order. Each record is built from a header whose multi-byte fields have already been reversed to native order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException"><c>count * sizeof(THeader)</c> bytes beginning at <c>cursor.Position</c> exceed the source length.</exception>
    /// <remarks>This is the big-endian counterpart of <see cref="ReadHeaderRecordArray{T, THeader}(ref Cursor, int, object?)"/> and should be used for every OpenType header, since the format is defined as big-endian on disk.</remarks>
    /// <seealso cref="IHeaderRecord{T, THeader}"/>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public static T[] ReadBigEndianHeaderRecordArray<T, THeader>(this ref Cursor cursor, int count, object? context = null) where T : IHeaderRecord<T, THeader> where THeader : unmanaged, IBigEndianStruct<THeader>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return [];

        var result = new T[count];
        cursor.ReadBigEndianHeaderRecordArray<T, THeader>(result, context);
        return result;
    }

    /// <summary>Reads <paramref name="destination"/>.Length big-endian <typeparamref name="THeader"/> values at the current position, reversing multi-byte fields on little-endian hosts, converts each to a <typeparamref name="T"/>, and advances the cursor past them.</summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <typeparam name="THeader">The header type. Must implement <see cref="IBigEndianStruct{THeader}"/> so its multi-byte fields can be reversed.</typeparam>
    /// <param name="cursor">Cursor positioned at the first header.</param>
    /// <param name="destination">The span to fill. Its length determines the number of headers read.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the headers are sufficient.</param>
    /// <exception cref="EndOfStreamException"><c>destination.Length * sizeof(THeader)</c> bytes beginning at <c>cursor.Position</c> exceed the source length.</exception>
    /// <remarks>The headers are materialized into a temporary array because the count is caller-controlled and can be arbitrarily large; a <c>stackalloc</c> path is not available for a generic <typeparamref name="THeader"/>.</remarks>
    /// <seealso cref="IHeaderRecord{T, THeader}"/>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public static void ReadBigEndianHeaderRecordArray<T, THeader>(this ref Cursor cursor, Span<T> destination, object? context = null) where T : IHeaderRecord<T, THeader> where THeader : unmanaged, IBigEndianStruct<THeader>
    {
        if (destination.IsEmpty) return;
        cursor.ReadBigEndianStructArray<THeader>(destination.Length).ToRecords(destination, context);
    }

    /// <summary>Reads <paramref name="destination"/>.Length values of an enum whose on-disk representation is big-endian, reversing bytes on little-endian hosts. The enum's underlying type determines the reversal width.</summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="source">The source to read from.</param>
    /// <param name="offset">Absolute offset of the first value.</param>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
    /// <exception cref="EndOfStreamException"><c>destination.Length * sizeof(TEnum)</c> bytes beginning at <paramref name="offset"/> exceed the source length.</exception>
    /// <exception cref="InvalidOperationException">The enum's underlying type is not 1, 2, 4, or 8 bytes wide.</exception>
    /// <remarks>Enums cannot implement <see cref="IBigEndianStruct{T}"/>, so this method dispatches on <c>Unsafe.SizeOf&lt;TEnum&gt;()</c> and reverses via <see cref="BinaryPrimitives.ReverseEndianness(ushort)"/> on the cast storage. Byte-sized enums are a no-op.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives.reverseendianness">.NET API: <c>BinaryPrimitives.ReverseEndianness</c></seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public static void ReadEnumArrayAt<TEnum>(this Source source, long offset, Span<TEnum> destination) where TEnum : unmanaged, Enum
    {
        if (destination.IsEmpty) return;

        // Read the raw bytes into the enum storage.
        source.ReadAt(offset, MemoryMarshal.AsBytes(destination));

        // Reverse in place, dispatching on the underlying size.
        if (!BitConverter.IsLittleEndian) return;

        switch (Unsafe.SizeOf<TEnum>())
        {
            case 1: break;   // byte/sbyte: identity
            case 2:
                BinaryPrimitives.ReverseEndianness(
                    MemoryMarshal.Cast<TEnum, ushort>(destination),
                    MemoryMarshal.Cast<TEnum, ushort>(destination));
                break;
            case 4:
                BinaryPrimitives.ReverseEndianness(
                    MemoryMarshal.Cast<TEnum, uint>(destination),
                    MemoryMarshal.Cast<TEnum, uint>(destination));
                break;
            case 8:
                BinaryPrimitives.ReverseEndianness(
                    MemoryMarshal.Cast<TEnum, ulong>(destination),
                    MemoryMarshal.Cast<TEnum, ulong>(destination));
                break;
            default:
                throw new InvalidOperationException($"Enum underlying size {Unsafe.SizeOf<TEnum>()} is not supported.");
        }
    }

    /// <summary>Reads <paramref name="count"/> values of an enum whose on-disk representation is big-endian, reversing bytes on little-endian hosts, and returns them in a new array. The enum's underlying type determines the reversal width.</summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="source">The source to read from.</param>
    /// <param name="offset">Absolute offset of the first value.</param>
    /// <param name="count">The number of values. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException"><c>count * sizeof(TEnum)</c> bytes beginning at <paramref name="offset"/> exceed the source length.</exception>
    /// <exception cref="InvalidOperationException">The enum's underlying type is not 1, 2, 4, or 8 bytes wide.</exception>
    /// <remarks>An empty request returns an empty array — no allocation is performed.</remarks>
    /// <seealso cref="ReadEnumArrayAt{TEnum}(Source, long, Span{TEnum})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives.reverseendianness">.NET API: <c>BinaryPrimitives.ReverseEndianness</c></seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public static TEnum[] ReadEnumArrayAt<TEnum>(this Source source, long offset, int count) where TEnum : unmanaged, Enum
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return [];
        var result = new TEnum[count];
        source.ReadEnumArrayAt(offset, result);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> values of an enum whose on-disk representation is big-endian at the cursor's current position, reversing bytes on little-endian hosts, advances the cursor past them, and returns the values in a new array. The enum's underlying type determines the reversal width.</summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="cursor">Cursor positioned at the first value.</param>
    /// <param name="count">The number of values. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException"><c>count * sizeof(TEnum)</c> bytes beginning at <c>cursor.Position</c> exceed the source length.</exception>
    /// <exception cref="InvalidOperationException">The enum's underlying type is not 1, 2, 4, or 8 bytes wide.</exception>
    /// <remarks>On success the cursor advances by <c>count * sizeof(TEnum)</c>.</remarks>
    /// <seealso cref="ReadEnumArray{TEnum}(ref Cursor, Span{TEnum})"/>
    /// <seealso cref="PeekEnumArray{TEnum}(ref Cursor, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives.reverseendianness">.NET API: <c>BinaryPrimitives.ReverseEndianness</c></seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public static TEnum[] ReadEnumArray<TEnum>(this ref Cursor cursor, int count) where TEnum : unmanaged, Enum
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new TEnum[count];
        cursor.ReadEnumArray(result);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> values of an enum whose on-disk representation is big-endian at the cursor's current position, reversing bytes on little-endian hosts, without advancing the cursor. The enum's underlying type determines the reversal width.</summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="cursor">Cursor positioned at the first value.</param>
    /// <param name="count">The number of values. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException"><c>count * sizeof(TEnum)</c> bytes beginning at <c>cursor.Position</c> exceed the source length.</exception>
    /// <exception cref="InvalidOperationException">The enum's underlying type is not 1, 2, 4, or 8 bytes wide.</exception>
    /// <remarks>The cursor's <see cref="Cursor.Position"/> is not modified; callers that need to consume the bytes should call <see cref="ReadEnumArray{TEnum}(ref Cursor, int)"/> instead.</remarks>
    /// <seealso cref="ReadEnumArray{TEnum}(ref Cursor, int)"/>
    /// <seealso cref="PeekEnumArray{TEnum}(ref Cursor, Span{TEnum})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives.reverseendianness">.NET API: <c>BinaryPrimitives.ReverseEndianness</c></seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public static TEnum[] PeekEnumArray<TEnum>(this ref Cursor cursor, int count) where TEnum : unmanaged, Enum
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new TEnum[count];
        cursor.PeekEnumArray(result);
        return result;
    }

    /// <summary>Reads <paramref name="destination"/>.Length values of an enum whose on-disk representation is big-endian at the cursor's current position, reversing bytes on little-endian hosts, and advances the cursor past them. The enum's underlying type determines the reversal width.</summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="cursor">Cursor positioned at the first value.</param>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException"><c>destination.Length * sizeof(TEnum)</c> bytes beginning at <c>cursor.Position</c> exceed the source length.</exception>
    /// <exception cref="InvalidOperationException">The enum's underlying type is not 1, 2, 4, or 8 bytes wide.</exception>
    /// <remarks>On success the cursor advances by <c>destination.Length * sizeof(TEnum)</c>.</remarks>
    /// <seealso cref="PeekEnumArray{TEnum}(ref Cursor, Span{TEnum})"/>
    /// <seealso cref="ReadEnumArray{TEnum}(ref Cursor, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives.reverseendianness">.NET API: <c>BinaryPrimitives.ReverseEndianness</c></seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public static void ReadEnumArray<TEnum>(this ref Cursor cursor, Span<TEnum> destination) where TEnum : unmanaged, Enum
    {
        cursor.PeekEnumArray(destination);
        cursor.Position = checked(cursor.Position + destination.Length * Unsafe.SizeOf<TEnum>());
    }

    /// <summary>Reads <paramref name="destination"/>.Length values of an enum whose on-disk representation is big-endian at the cursor's current position, reversing bytes on little-endian hosts, without advancing the cursor. The enum's underlying type determines the reversal width.</summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="cursor">Cursor positioned at the first value.</param>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException"><c>destination.Length * sizeof(TEnum)</c> bytes beginning at <c>cursor.Position</c> exceed the source length.</exception>
    /// <exception cref="InvalidOperationException">The enum's underlying type is not 1, 2, 4, or 8 bytes wide.</exception>
    /// <remarks>The cursor's <see cref="Cursor.Position"/> is not modified.</remarks>
    /// <seealso cref="ReadEnumArray{TEnum}(ref Cursor, Span{TEnum})"/>
    /// <seealso cref="PeekEnumArray{TEnum}(ref Cursor, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives.reverseendianness">.NET API: <c>BinaryPrimitives.ReverseEndianness</c></seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public static void PeekEnumArray<TEnum>(this ref Cursor cursor, Span<TEnum> destination) where TEnum : unmanaged, Enum => cursor.Source.ReadEnumArrayAt(cursor.Position, destination);
}
