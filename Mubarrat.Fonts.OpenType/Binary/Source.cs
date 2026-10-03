using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Binary;

/// <summary>A random-access byte source for font data. Implementations may be backed by memory, a file, or any other random-access backing. Reads are addressed by absolute offset; the source owns no cursor.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><see cref="Source"/> is the query-time representation of a table's backing bytes: table objects that expose accessors (e.g. <c>Loca</c>, <c>Coverage</c>, <c>CmapFormat12</c>) store a <see cref="Source"/> plus a base offset and read fields on demand.</description></item>
/// <item><description>All multi-byte values are big-endian on disk. The scalar readers compose them explicitly and the array readers swap in place when <see cref="BitConverter.IsLittleEndian"/> is <c>true</c>; there is no host-endian assumption anywhere.</description></item>
/// <item><description>Implementations must be safe for concurrent reads at different offsets. The font stream (or buffer) must remain open for the lifetime of any table obtained from it.</description></item>
/// </list>
/// <para>For the on-disk layout of every primitive read by this type, see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification, Data Types</see>.</para>
/// </remarks>
/// <seealso cref="MemorySource"/>
/// <seealso cref="FileSource"/>
/// <seealso cref="SliceSource"/>
/// <seealso cref="Cursor"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType Font</seealso>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "ApiDesign",
    "RS0026:Do not add multiple public overloads with optional parameters",
    Scope = "type",
    Target = "~T:Mubarrat.Fonts.OpenType.Binary.Source",
    Justification = "Overloads are disambiguated by the type of their first parameter (int count vs Span<T> destination); the trailing optional 'context' cannot introduce ambiguity between them.")]
public abstract class Source : IDisposable
{
    // ────────────────────────────── Constants ──────────────────────────────

    /// <summary>Seconds between 1904-01-01 (OpenType LONGDATETIME epoch) and 1970-01-01 (Unix epoch).</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">LONGDATETIME data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadLongDateTimeAt(long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: LONGDATETIME</seealso>
    public const long OpenTypeEpochOffset = 2082844800L;

    // ────────────────────────────── Core ──────────────────────────────

    /// <summary>Gets the total length of the source, in bytes.</summary>
    /// <value>A non-negative byte count. Implementations report the length of the underlying backing.</value>
    /// <seealso cref="ReadAt(long, Span{byte})"/>
    /// <seealso cref="TryGetSpanAt(long, int, out ReadOnlySpan{byte})"/>
    public abstract long Length { get; }

    /// <summary>Reads bytes at <paramref name="offset"/> into <paramref name="destination"/>.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="destination">Buffer to fill.</param>
    /// <remarks>Implementations receive an already-validated <paramref name="offset"/> and <paramref name="destination"/>. The base <see cref="ReadAt(long, Span{byte})"/> checks bounds against <see cref="Length"/> before delegating here.</remarks>
    /// <seealso cref="ReadAt(long, Span{byte})"/>
    protected abstract void ReadAtCore(long offset, Span<byte> destination);

    /// <summary>Reads bytes at an absolute offset, bounds-checked against <see cref="Length"/>.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="destination">Buffer to fill.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>An empty <paramref name="destination"/> returns immediately without touching the source.</description></item>
    /// <item><description>For implementations that support zero-copy views — see <see cref="TryGetSpanAt(long, int, out ReadOnlySpan{byte})"/> — the scalar readers bypass this method entirely.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ReadAtCore(long, Span{byte})"/>
    /// <seealso cref="TryGetSpanAt(long, int, out ReadOnlySpan{byte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public virtual void ReadAt(long offset, scoped Span<byte> destination)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        if (destination.IsEmpty) return;
        if (offset + destination.Length > Length)
            throw new EndOfStreamException(
                $"Read of {destination.Length} bytes at offset {offset} exceeds length {Length}.");
        ReadAtCore(offset, destination);
    }

    /// <summary>Attempts to get a <see cref="ReadOnlySpan{T}"/> of the source at an absolute offset.</summary>
    /// <param name="offset">The absolute offset within the source.</param>
    /// <param name="length">The number of bytes to include in the span.</param>
    /// <param name="span">When this method returns, contains the resulting span, if successful; otherwise, an empty span.</param>
    /// <returns><see langword="true"/> if the span was successfully retrieved; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The requested span extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Bounds are checked against <see cref="Length"/> before <see cref="TryGetSpanAtCore(long, int, out ReadOnlySpan{byte})"/> is called, so an out-of-range request throws rather than returning <see langword="false"/>.</description></item>
    /// <item><description><see langword="false"/> is returned only when the implementation declines the request — typically because the backing is not a contiguous block of memory.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType data types</see> page for the widths and byte orders these spans are read with.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="TryGetSpanAtCore(long, int, out ReadOnlySpan{byte})"/>
    /// <seealso cref="ReadAt(long, Span{byte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public bool TryGetSpanAt(long offset, int length, out ReadOnlySpan<byte> span)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        if (offset + length > Length)
            throw new EndOfStreamException(
                $"Span of {length} bytes at offset {offset} exceeds length {Length}.");
        return TryGetSpanAtCore(offset, length, out span);
    }

    /// <summary>Attempts to get a <see cref="ReadOnlySpan{T}"/> of the source at an absolute offset.</summary>
    /// <param name="offset">The absolute offset within the source.</param>
    /// <param name="length">The number of bytes to include in the span.</param>
    /// <param name="span">When this method returns, contains the resulting span, if successful; otherwise, an empty span.</param>
    /// <returns><see langword="true"/> if the span was successfully retrieved; otherwise, <see langword="false"/>.</returns>
    /// <remarks>The default implementation always returns <see langword="false"/> with an empty span, so callers fall back to <see cref="ReadAtCore(long, Span{byte})"/>. Override to provide a zero-copy path; see <see cref="MemorySource"/> for the canonical example.</remarks>
    /// <seealso cref="TryGetSpanAt(long, int, out ReadOnlySpan{byte})"/>
    /// <seealso cref="MemorySource"/>
    protected virtual bool TryGetSpanAtCore(long offset, int length, out ReadOnlySpan<byte> span)
    {
        span = default;
        return false;
    }

    // ────────────────────────── Scalar reads (8) ──────────────────────────

    /// <summary>Reads an unsigned 8-bit integer at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The byte at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt8At(long)"/>
    /// <seealso cref="ReadUInt8ArrayAt(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint8</seealso>
    public byte ReadUInt8At(long offset)
    {
        Span<byte> buffer = stackalloc byte[1];
        ReadAt(offset, buffer);
        return buffer[0];
    }

    /// <summary>Reads a signed 8-bit integer at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The signed byte at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt8At(long)"/>
    /// <seealso cref="ReadInt8ArrayAt(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int8</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadInt8At(long offset) => unchecked((sbyte)ReadUInt8At(offset));

    // ───────────────────────── Scalar reads (16) ─────────────────────────

    /// <summary>Reads a big-endian unsigned 16-bit integer at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt16At(long)"/>
    /// <seealso cref="ReadUInt16ArrayAt(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint16</seealso>
    public ushort ReadUInt16At(long offset)
    {
        if (TryGetSpanAt(offset, 2, out var span))
            return BinaryPrimitives.ReadUInt16BigEndian(span);
        Span<byte> buffer = stackalloc byte[2];
        ReadAt(offset, buffer);
        return BinaryPrimitives.ReadUInt16BigEndian(buffer);
    }

    /// <summary>Reads a big-endian signed 16-bit integer at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt16At(long)"/>
    /// <seealso cref="ReadInt16ArrayAt(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int16</seealso>
    public short ReadInt16At(long offset)
    {
        if (TryGetSpanAt(offset, 2, out var span))
            return BinaryPrimitives.ReadInt16BigEndian(span);
        Span<byte> buffer = stackalloc byte[2];
        ReadAt(offset, buffer);
        return BinaryPrimitives.ReadInt16BigEndian(buffer);
    }

    // ───────────────────────── Scalar reads (24) ─────────────────────────

    /// <summary>Reads a big-endian unsigned 24-bit integer at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt24At(long)"/>
    /// <seealso cref="UInt24"/>
    /// <seealso cref="ReadUInt24ArrayAt(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint24</seealso>
    public UInt24 ReadUInt24At(long offset)
    {
        if (TryGetSpanAt(offset, 3, out var span))
            return UInt24.ReadBigEndian(span);
        Span<byte> buffer = stackalloc byte[3];
        ReadAt(offset, buffer);
        return UInt24.ReadBigEndian(buffer);
    }

    /// <summary>Reads a big-endian signed 24-bit integer at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt24At(long)"/>
    /// <seealso cref="Int24"/>
    /// <seealso cref="ReadInt24ArrayAt(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public Int24 ReadInt24At(long offset)
    {
        if (TryGetSpanAt(offset, 3, out var span))
            return Int24.ReadBigEndian(span);
        Span<byte> buffer = stackalloc byte[3];
        ReadAt(offset, buffer);
        return Int24.ReadBigEndian(buffer);
    }

    // ───────────────────────── Scalar reads (32) ─────────────────────────

    /// <summary>Reads a big-endian unsigned 32-bit integer at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt32At(long)"/>
    /// <seealso cref="ReadUInt32ArrayAt(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint32</seealso>
    public uint ReadUInt32At(long offset)
    {
        if (TryGetSpanAt(offset, 4, out var span))
            return BinaryPrimitives.ReadUInt32BigEndian(span);
        Span<byte> buffer = stackalloc byte[4];
        ReadAt(offset, buffer);
        return BinaryPrimitives.ReadUInt32BigEndian(buffer);
    }

    /// <summary>Reads a big-endian signed 32-bit integer at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt32At(long)"/>
    /// <seealso cref="ReadInt32ArrayAt(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int32</seealso>
    public int ReadInt32At(long offset)
    {
        if (TryGetSpanAt(offset, 4, out var span))
            return BinaryPrimitives.ReadInt32BigEndian(span);
        Span<byte> buffer = stackalloc byte[4];
        ReadAt(offset, buffer);
        return BinaryPrimitives.ReadInt32BigEndian(buffer);
    }

    // ───────────────────────── Scalar reads (64) ─────────────────────────

    /// <summary>Reads a big-endian unsigned 64-bit integer at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint64 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt64At(long)"/>
    /// <seealso cref="ReadUInt64ArrayAt(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint64</seealso>
    public ulong ReadUInt64At(long offset)
    {
        if (TryGetSpanAt(offset, 8, out var span))
            return BinaryPrimitives.ReadUInt64BigEndian(span);
        Span<byte> buffer = stackalloc byte[8];
        ReadAt(offset, buffer);
        return BinaryPrimitives.ReadUInt64BigEndian(buffer);
    }

    /// <summary>Reads a big-endian signed 64-bit integer at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int64 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt64At(long)"/>
    /// <seealso cref="ReadInt64ArrayAt(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int64</seealso>
    public long ReadInt64At(long offset)
    {
        if (TryGetSpanAt(offset, 8, out var span))
            return BinaryPrimitives.ReadInt64BigEndian(span);
        Span<byte> buffer = stackalloc byte[8];
        ReadAt(offset, buffer);
        return BinaryPrimitives.ReadInt64BigEndian(buffer);
    }

    // ───────────────────────── Semantic scalars ─────────────────────────

    /// <summary>Reads a 16.16 fixed-point value at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The <see cref="Fixed"/> value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Fixed data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="Fixed"/>
    /// <seealso cref="ReadF2Dot14At(long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Fixed ReadFixedAt(long offset) => (Fixed)ReadInt32At(offset);

    /// <summary>Reads an F2DOT14 value at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The <see cref="F2Dot14"/> value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">F2DOT14 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="F2Dot14"/>
    /// <seealso cref="ReadFixedAt(long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public F2Dot14 ReadF2Dot14At(long offset) => (F2Dot14)ReadInt16At(offset);

    /// <summary>Reads a Version16Dot16 value at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The value at <paramref name="offset"/>, decoded as a <see cref="double"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Version16Dot16 data type</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Version16Dot16</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadVersion16Dot16At(long offset) => ReadUInt32At(offset) / 65536.0;

    /// <summary>Reads a 4-byte tag at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The <see cref="Tag"/> value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Tag data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="Tag"/>
    /// <seealso cref="ReadOffset32At(long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Tag</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Tag ReadTagAt(long offset) => new(ReadUInt32At(offset));

    /// <summary>Reads a LONGDATETIME at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>A <see cref="DateTime"/> in UTC, converted from the 1904 epoch to the Unix epoch.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">LONGDATETIME data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="OpenTypeEpochOffset"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: LONGDATETIME</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime ReadLongDateTimeAt(long offset) =>
        DateTime.UnixEpoch.AddSeconds(ReadInt64At(offset) - OpenTypeEpochOffset);

    /// <summary>Reads an FWORD (signed 16-bit) at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">FWORD data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUFWordAt(long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: FWORD</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadFWordAt(long offset) => ReadInt16At(offset);

    /// <summary>Reads a UFWORD (unsigned 16-bit) at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">UFWORD data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadFWordAt(long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: UFWORD</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUFWordAt(long offset) => ReadUInt16At(offset);

    // ───────────────────────── Offset reads ─────────────────────────

    /// <summary>Reads an 8-bit offset at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The offset value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset16At(long)"/>
    /// <seealso cref="ReadOffset24At(long)"/>
    /// <seealso cref="ReadOffset32At(long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadOffset8At(long offset) => ReadUInt8At(offset);

    /// <summary>Reads a 16-bit offset at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The offset value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8At(long)"/>
    /// <seealso cref="ReadOffset24At(long)"/>
    /// <seealso cref="ReadOffset32At(long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadOffset16At(long offset) => ReadUInt16At(offset);

    /// <summary>Reads a 24-bit offset at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The offset value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8At(long)"/>
    /// <seealso cref="ReadOffset16At(long)"/>
    /// <seealso cref="ReadOffset32At(long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UInt24 ReadOffset24At(long offset) => ReadUInt24At(offset);

    /// <summary>Reads a 32-bit offset at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The offset value at <paramref name="offset"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8At(long)"/>
    /// <seealso cref="ReadOffset16At(long)"/>
    /// <seealso cref="ReadOffset24At(long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadOffset32At(long offset) => ReadUInt32At(offset);

    // ───────────────────────── Byte arrays ─────────────────────────

    /// <summary>Reads <paramref name="destination"/>.Length bytes at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="destination">The buffer to fill.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="ReadBytesAt(long, int)"/>
    /// <seealso cref="ReadAt(long, Span{byte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadBytesAt(long offset, scoped Span<byte> destination) =>
        ReadUInt8ArrayAt(offset, destination);

    /// <summary>Reads <paramref name="count"/> bytes at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="count">The number of bytes to read. Must be non-negative.</param>
    /// <returns>A new array containing the bytes read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="ReadBytesAt(long, Span{byte})"/>
    /// <seealso cref="ReadUInt8ArrayAt(long, int)"/>
    public byte[] ReadBytesAt(long offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new byte[count];
        ReadUInt8ArrayAt(offset, result);
        return result;
    }

    // ───────────────────────── Typed arrays (8-bit) ─────────────────────────

    /// <summary>Reads an array of unsigned 8-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt8ArrayAt(long, int)"/>
    /// <seealso cref="ReadInt8ArrayAt(long, Span{sbyte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint8</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadUInt8ArrayAt(long offset, scoped Span<byte> destination) =>
        ReadAt(offset, destination);

    /// <summary>Reads an array of signed 8-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt8ArrayAt(long, int)"/>
    /// <seealso cref="ReadUInt8ArrayAt(long, Span{byte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int8</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadInt8ArrayAt(long offset, scoped Span<sbyte> destination) =>
        ReadAt(offset, MemoryMarshal.AsBytes(destination));

    /// <summary>Reads <paramref name="count"/> unsigned 8-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt8ArrayAt(long, Span{byte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint8</seealso>
    public byte[] ReadUInt8ArrayAt(long offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new byte[count];
        ReadUInt8ArrayAt(offset, result);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> signed 8-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt8ArrayAt(long, Span{sbyte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int8</seealso>
    public sbyte[] ReadInt8ArrayAt(long offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new sbyte[count];
        ReadInt8ArrayAt(offset, result);
        return result;
    }

    // ───────────────────────── Typed arrays (16-bit) ─────────────────────────

    /// <summary>Reads an array of big-endian unsigned 16-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The bytes are read as-is and then reversed in place on little-endian hosts. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt16ArrayAt(long, int)"/>
    /// <seealso cref="ReadInt16ArrayAt(long, Span{short})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint16</seealso>
    public void ReadUInt16ArrayAt(long offset, scoped Span<ushort> destination)
    {
        ReadAt(offset, MemoryMarshal.AsBytes(destination));
        if (BitConverter.IsLittleEndian)
            BinaryPrimitives.ReverseEndianness(destination, destination);
    }

    /// <summary>Reads an array of big-endian signed 16-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The bytes are read as-is and then reversed in place on little-endian hosts. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt16ArrayAt(long, int)"/>
    /// <seealso cref="ReadUInt16ArrayAt(long, Span{ushort})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int16</seealso>
    public void ReadInt16ArrayAt(long offset, scoped Span<short> destination)
    {
        ReadAt(offset, MemoryMarshal.AsBytes(destination));
        if (BitConverter.IsLittleEndian)
            BinaryPrimitives.ReverseEndianness(destination, destination);
    }

    /// <summary>Reads <paramref name="count"/> big-endian unsigned 16-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read, in native byte order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt16ArrayAt(long, Span{ushort})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint16</seealso>
    public ushort[] ReadUInt16ArrayAt(long offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new ushort[count];
        ReadUInt16ArrayAt(offset, result);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> big-endian signed 16-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read, in native byte order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt16ArrayAt(long, Span{short})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int16</seealso>
    public short[] ReadInt16ArrayAt(long offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new short[count];
        ReadInt16ArrayAt(offset, result);
        return result;
    }

    // ───────────────────────── Typed arrays (24-bit) ─────────────────────────

    /// <summary>Reads an array of big-endian unsigned 24-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>Reversal is performed per element because <see cref="BinaryPrimitives.ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})"/> has no 24-bit overload. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="UInt24"/>
    /// <seealso cref="ReadUInt24ArrayAt(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint24</seealso>
    public void ReadUInt24ArrayAt(long offset, scoped Span<UInt24> destination)
    {
        ReadAt(offset, MemoryMarshal.AsBytes(destination));
        if (BitConverter.IsLittleEndian)
            for (int i = 0; i < destination.Length; i++)
                destination[i] = UInt24.ReverseEndianness(destination[i]);
    }

    /// <summary>Reads an array of big-endian signed 24-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>Reversal is performed per element. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="Int24"/>
    /// <seealso cref="ReadInt24ArrayAt(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public void ReadInt24ArrayAt(long offset, scoped Span<Int24> destination)
    {
        ReadAt(offset, MemoryMarshal.AsBytes(destination));
        if (BitConverter.IsLittleEndian)
            for (int i = 0; i < destination.Length; i++)
                destination[i] = Int24.ReverseEndianness(destination[i]);
    }

    /// <summary>Reads <paramref name="count"/> big-endian unsigned 24-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read, in native byte order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="UInt24"/>
    /// <seealso cref="ReadUInt24ArrayAt(long, Span{UInt24})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint24</seealso>
    public UInt24[] ReadUInt24ArrayAt(long offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new UInt24[count];
        ReadUInt24ArrayAt(offset, result);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> big-endian signed 24-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read, in native byte order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="Int24"/>
    /// <seealso cref="ReadInt24ArrayAt(long, Span{Int24})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public Int24[] ReadInt24ArrayAt(long offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new Int24[count];
        ReadInt24ArrayAt(offset, result);
        return result;
    }

    // ───────────────────────── Typed arrays (32-bit) ─────────────────────────

    /// <summary>Reads an array of big-endian unsigned 32-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The bytes are read as-is and then reversed in place on little-endian hosts. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt32ArrayAt(long, int)"/>
    /// <seealso cref="ReadInt32ArrayAt(long, Span{int})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint32</seealso>
    public void ReadUInt32ArrayAt(long offset, scoped Span<uint> destination)
    {
        ReadAt(offset, MemoryMarshal.AsBytes(destination));
        if (BitConverter.IsLittleEndian)
            BinaryPrimitives.ReverseEndianness(destination, destination);
    }

    /// <summary>Reads an array of big-endian signed 32-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The bytes are read as-is and then reversed in place on little-endian hosts. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt32ArrayAt(long, int)"/>
    /// <seealso cref="ReadUInt32ArrayAt(long, Span{uint})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int32</seealso>
    public void ReadInt32ArrayAt(long offset, scoped Span<int> destination)
    {
        ReadAt(offset, MemoryMarshal.AsBytes(destination));
        if (BitConverter.IsLittleEndian)
            BinaryPrimitives.ReverseEndianness(destination, destination);
    }

    /// <summary>Reads <paramref name="count"/> big-endian unsigned 32-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read, in native byte order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt32ArrayAt(long, Span{uint})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint32</seealso>
    public uint[] ReadUInt32ArrayAt(long offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new uint[count];
        ReadUInt32ArrayAt(offset, result);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> big-endian signed 32-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read, in native byte order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt32ArrayAt(long, Span{int})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int32</seealso>
    public int[] ReadInt32ArrayAt(long offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new int[count];
        ReadInt32ArrayAt(offset, result);
        return result;
    }

    // ───────────────────────── Typed arrays (64-bit) ─────────────────────────

    /// <summary>Reads an array of big-endian unsigned 64-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The bytes are read as-is and then reversed in place on little-endian hosts. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint64 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt64ArrayAt(long, int)"/>
    /// <seealso cref="ReadInt64ArrayAt(long, Span{long})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint64</seealso>
    public void ReadUInt64ArrayAt(long offset, scoped Span<ulong> destination)
    {
        ReadAt(offset, MemoryMarshal.AsBytes(destination));
        if (BitConverter.IsLittleEndian)
            BinaryPrimitives.ReverseEndianness(destination, destination);
    }

    /// <summary>Reads an array of big-endian signed 64-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The bytes are read as-is and then reversed in place on little-endian hosts. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int64 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt64ArrayAt(long, int)"/>
    /// <seealso cref="ReadUInt64ArrayAt(long, Span{ulong})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int64</seealso>
    public void ReadInt64ArrayAt(long offset, scoped Span<long> destination)
    {
        ReadAt(offset, MemoryMarshal.AsBytes(destination));
        if (BitConverter.IsLittleEndian)
            BinaryPrimitives.ReverseEndianness(destination, destination);
    }

    /// <summary>Reads <paramref name="count"/> big-endian unsigned 64-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read, in native byte order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint64 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt64ArrayAt(long, Span{ulong})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint64</seealso>
    public ulong[] ReadUInt64ArrayAt(long offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new ulong[count];
        ReadUInt64ArrayAt(offset, result);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> big-endian signed 64-bit integers at an absolute offset.</summary>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read, in native byte order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int64 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt64ArrayAt(long, Span{long})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int64</seealso>
    public long[] ReadInt64ArrayAt(long offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new long[count];
        ReadInt64ArrayAt(offset, result);
        return result;
    }

    // ─────────────────── Offset array interpretation ───────────────────

    /// <summary>Reads <paramref name="count"/> 8-bit offsets at <paramref name="offset"/> and interprets each.</summary>
    /// <typeparam name="T">The interpreter's return type.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="interpreter">The function applied to each offset value.</param>
    /// <returns>A new array of interpreted values, one per offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="interpreter"/> is <c>null</c>.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset16ArrayInterpretAt{T}(long, int, Func{ushort, T})"/>
    /// <seealso cref="ReadOffset24ArrayInterpretAt{T}(long, int, Func{UInt24, T})"/>
    /// <seealso cref="ReadOffset32ArrayInterpretAt{T}(long, int, Func{uint, T})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public T[] ReadOffset8ArrayInterpretAt<T>(long offset, int count, Func<byte, T> interpreter)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentNullException.ThrowIfNull(interpreter);
        if (count == 0) return [];

        var offsets = ReadUInt8ArrayAt(offset, count);
        var result = new T[count];
        for (int i = 0; i < count; i++)
            result[i] = interpreter(offsets[i]);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 16-bit offsets at <paramref name="offset"/> and interprets each.</summary>
    /// <typeparam name="T">The interpreter's return type.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="interpreter">The function applied to each offset value.</param>
    /// <returns>A new array of interpreted values, one per offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="interpreter"/> is <c>null</c>.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8ArrayInterpretAt{T}(long, int, Func{byte, T})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public T[] ReadOffset16ArrayInterpretAt<T>(long offset, int count, Func<ushort, T> interpreter)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentNullException.ThrowIfNull(interpreter);
        if (count == 0) return [];

        var offsets = ReadUInt16ArrayAt(offset, count);
        var result = new T[count];
        for (int i = 0; i < count; i++)
            result[i] = interpreter(offsets[i]);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 24-bit offsets at <paramref name="offset"/> and interprets each.</summary>
    /// <typeparam name="T">The interpreter's return type.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="interpreter">The function applied to each offset value.</param>
    /// <returns>A new array of interpreted values, one per offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="interpreter"/> is <c>null</c>.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8ArrayInterpretAt{T}(long, int, Func{byte, T})"/>
    /// <seealso cref="UInt24"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public T[] ReadOffset24ArrayInterpretAt<T>(long offset, int count, Func<UInt24, T> interpreter)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentNullException.ThrowIfNull(interpreter);
        if (count == 0) return [];

        var offsets = ReadUInt24ArrayAt(offset, count);
        var result = new T[count];
        for (int i = 0; i < count; i++)
            result[i] = interpreter(offsets[i]);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 32-bit offsets at <paramref name="offset"/> and interprets each.</summary>
    /// <typeparam name="T">The interpreter's return type.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="interpreter">The function applied to each offset value.</param>
    /// <returns>A new array of interpreted values, one per offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="interpreter"/> is <c>null</c>.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8ArrayInterpretAt{T}(long, int, Func{byte, T})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public T[] ReadOffset32ArrayInterpretAt<T>(long offset, int count, Func<uint, T> interpreter)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentNullException.ThrowIfNull(interpreter);
        if (count == 0) return [];

        var offsets = ReadUInt32ArrayAt(offset, count);
        var result = new T[count];
        for (int i = 0; i < count; i++)
            result[i] = interpreter(offsets[i]);
        return result;
    }

    // ───────────────────── Record parsing ─────────────────────

    /// <summary>Parses a record of type <typeparamref name="T"/> at an absolute offset.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset of the record.</param>
    /// <param name="context">External values the record needs, or <c>null</c> when the record's own bytes are sufficient.</param>
    /// <returns>The parsed record.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The cursor passed to the record is created with <see cref="CreateOffsetCursor(long, long)"/>, so the record's internal offsets resolve relative to <paramref name="offset"/>.</description></item>
    /// <item><description>This is a convenience wrapper over <c>T.Parse(ref cursor, context)</c>; use <see cref="CreateCursor(long)"/> directly when you need to inspect the cursor afterwards.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ParseRecordArrayAt{T}(long, int, object?)"/>
    /// <seealso cref="ReadOffset8ArrayParseRecordAt{T}(long, int, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ParseRecordAt<T>(long offset, object? context = null) where T : IRecord<T>
    {
        var cursor = CreateOffsetCursor(offset);
        return T.Parse(ref cursor, context);
    }

    /// <summary>Parses <paramref name="count"/> consecutive records starting at an absolute offset.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset of the first record.</param>
    /// <param name="count">The number of records. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <returns>A new array of parsed records, one per record, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>Records are parsed sequentially; the offset is advanced by the bytes each record consumed.</remarks>
    /// <seealso cref="ParseRecordAt{T}(long, object?)"/>
    /// <seealso cref="ParseRecordArrayAt{T}(long, Span{T}, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    public T[] ParseRecordArrayAt<T>(long offset, int count, object? context = null) where T : IRecord<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return [];

        var result = new T[count];
        ParseRecordArrayAt(offset, result, context);
        return result;
    }

    // ─────────────────── Offset array + record parsing ───────────────────

    /// <summary>Reads <paramref name="count"/> 8-bit offsets at <paramref name="offset"/> and parses a record at each offset.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <returns>A new array of parsed records, one per offset, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The offsets are relative to the source origin. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset16ArrayParseRecordAt{T}(long, int, object?)"/>
    /// <seealso cref="ReadOffset24ArrayParseRecordAt{T}(long, int, object?)"/>
    /// <seealso cref="ReadOffset32ArrayParseRecordAt{T}(long, int, object?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public T[] ReadOffset8ArrayParseRecordAt<T>(long offset, int count, object? context = null)
        where T : IRecord<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return [];
        var result = new T[count];
        ReadOffset8ArrayParseRecordAt(offset, result, context);
        return result;
    }

    /// <summary>16-bit variant of <see cref="ReadOffset8ArrayParseRecordAt{T}(long, int, object?)"/>.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <returns>A new array of parsed records, one per offset, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8ArrayParseRecordAt{T}(long, int, object?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public T[] ReadOffset16ArrayParseRecordAt<T>(long offset, int count, object? context = null)
        where T : IRecord<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return [];
        var result = new T[count];
        ReadOffset16ArrayParseRecordAt(offset, result, context);
        return result;
    }

    /// <summary>24-bit variant of <see cref="ReadOffset8ArrayParseRecordAt{T}(long, int, object?)"/>.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <returns>A new array of parsed records, one per offset, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8ArrayParseRecordAt{T}(long, int, object?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public T[] ReadOffset24ArrayParseRecordAt<T>(long offset, int count, object? context = null)
        where T : IRecord<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return [];
        var result = new T[count];
        ReadOffset24ArrayParseRecordAt(offset, result, context);
        return result;
    }

    /// <summary>32-bit variant of <see cref="ReadOffset8ArrayParseRecordAt{T}(long, int, object?)"/>.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <returns>A new array of parsed records, one per offset, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8ArrayParseRecordAt{T}(long, int, object?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public T[] ReadOffset32ArrayParseRecordAt<T>(long offset, int count, object? context = null)
        where T : IRecord<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return [];
        var result = new T[count];
        ReadOffset32ArrayParseRecordAt(offset, result, context);
        return result;
    }

    // ───────────────────── Record parsing (span variants) ─────────────────────

    /// <summary>Parses consecutive records of type <typeparamref name="T"/> starting at an absolute offset, writing them into <paramref name="destination"/>.</summary>
    /// <typeparam name="T">The record type. Must be a reference type; the destination span holds references.</typeparam>
    /// <param name="offset">Absolute offset of the first record.</param>
    /// <param name="destination">The span to fill. Its length determines how many records are parsed. An empty span is a no-op.</param>
    /// <param name="context">External values passed to each parse, or <c>null</c>.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>Records are parsed sequentially. After each record, the cursor position is added to the offset so the next record begins immediately after the bytes the previous one consumed.</remarks>
    /// <seealso cref="ParseRecordArrayAt{T}(long, int, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    public void ParseRecordArrayAt<T>(long offset, scoped Span<T> destination, object? context = null)
        where T : IRecord<T>
    {
        if (destination.IsEmpty) return;
        for (int i = 0; i < destination.Length; i++)
        {
            var cursor = CreateOffsetCursor(offset);
            destination[i] = T.Parse(ref cursor, context);
            offset += cursor.Position;
        }
    }

    // ─────────────────── Offset array + record parsing (span variants) ───────────────────

    /// <summary>Reads <paramref name="destination"/>.Length 8-bit offsets at <paramref name="offset"/> and parses a record at each, writing into <paramref name="destination"/>.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="destination">The span to fill. Its length determines the number of offsets.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8ArrayParseRecordAt{T}(long, int, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public void ReadOffset8ArrayParseRecordAt<T>(long offset, scoped Span<T> destination, object? context = null)
        where T : IRecord<T>
    {
        if (destination.IsEmpty) return;
        var offsets = ReadUInt8ArrayAt(offset, destination.Length);
        for (int i = 0; i < destination.Length; i++)
            destination[i] = ParseRecordAt<T>(offsets[i], context);
    }

    /// <summary>16-bit variant of <see cref="ReadOffset8ArrayParseRecordAt{T}(long, Span{T}, object?)"/>.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="destination">The span to fill. Its length determines the number of offsets.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset16ArrayParseRecordAt{T}(long, int, object?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public void ReadOffset16ArrayParseRecordAt<T>(long offset, scoped Span<T> destination, object? context = null)
        where T : IRecord<T>
    {
        if (destination.IsEmpty) return;
        var offsets = ReadUInt16ArrayAt(offset, destination.Length);
        for (int i = 0; i < destination.Length; i++)
            destination[i] = ParseRecordAt<T>(offsets[i], context);
    }

    /// <summary>24-bit variant of <see cref="ReadOffset8ArrayParseRecordAt{T}(long, Span{T}, object?)"/>.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="destination">The span to fill. Its length determines the number of offsets.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset24ArrayParseRecordAt{T}(long, int, object?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public void ReadOffset24ArrayParseRecordAt<T>(long offset, scoped Span<T> destination, object? context = null)
        where T : IRecord<T>
    {
        if (destination.IsEmpty) return;
        var offsets = ReadUInt24ArrayAt(offset, destination.Length);
        for (int i = 0; i < destination.Length; i++)
            destination[i] = ParseRecordAt<T>(offsets[i], context);
    }

    /// <summary>32-bit variant of <see cref="ReadOffset8ArrayParseRecordAt{T}(long, Span{T}, object?)"/>.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="destination">The span to fill. Its length determines the number of offsets.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset32ArrayParseRecordAt{T}(long, int, object?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public void ReadOffset32ArrayParseRecordAt<T>(long offset, scoped Span<T> destination, object? context = null)
        where T : IRecord<T>
    {
        if (destination.IsEmpty) return;
        var offsets = ReadUInt32ArrayAt(offset, destination.Length);
        for (int i = 0; i < destination.Length; i++)
            destination[i] = ParseRecordAt<T>(offsets[i], context);
    }

    // ───────────────────────── Struct reads ─────────────────────────
    //
    // ReadStructAt<T> reinterprets raw bytes as T with no endianness conversion. The caller
    // is responsible for swapping multi-byte fields when the on-disk byte order does not
    // match the host.
    //
    // ReadBigEndianStructAt<T> reads bytes that are known to be big-endian on disk and
    // calls T.SwapEndianness() on little-endian hosts. T must implement IBigEndianStruct.
    //
    // Both require T to be unmanaged, sequentially laid out, and padded to the same size as
    // its fields. The struct layout is the caller's responsibility.

    /// <summary>Reads an unmanaged struct of type <typeparamref name="T"/> at an absolute offset with no endianness conversion.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The value read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The caller is responsible for swapping multi-byte fields when the on-disk byte order does not match the host.</description></item>
    /// <item><description>For big-endian structs, use <see cref="ReadBigEndianStructAt{T}(long)"/> instead.</description></item>
    /// <item><description>See <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType Data Types</see> for the on-disk format of primitive fields.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ReadBigEndianStructAt{T}(long)"/>
    /// <seealso cref="ReadStructArrayAt{T}(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadStructAt<T>(long offset) where T : unmanaged
    {
        T value = default;
        ReadAt(offset, MemoryMarshal.AsBytes(new Span<T>(ref value)));
        return value;
    }

    /// <summary>Reads <paramref name="destination"/>.Length unmanaged structs of type <typeparamref name="T"/> at an absolute offset with no endianness conversion.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="destination">Span to fill.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="ReadBigEndianStructArrayAt{T}(long, Span{T})"/>
    /// <seealso cref="ReadStructArrayAt{T}(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public void ReadStructArrayAt<T>(long offset, scoped Span<T> destination) where T : unmanaged
    {
        if (destination.IsEmpty) return;
        ReadAt(offset, MemoryMarshal.AsBytes(destination));
    }

    /// <summary>Reads <paramref name="count"/> unmanaged structs of type <typeparamref name="T"/> at an absolute offset with no endianness conversion.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="count">The number of elements to read. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="ReadStructArrayAt{T}(long, Span{T})"/>
    /// <seealso cref="ReadStructAt{T}(long)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public T[] ReadStructArrayAt<T>(long offset, int count) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new T[count];
        ReadStructArrayAt(offset, result);
        return result;
    }

    /// <summary>Reads a big-endian unmanaged struct of type <typeparamref name="T"/> at an absolute offset, swapping multi-byte fields to native order on little-endian hosts.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IBigEndianStruct{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <returns>The value read, with multi-byte fields in native byte order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>This is the reader to use for every OpenType header or record struct, since the format is defined as big-endian on disk. See <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType Data Types</see>.</remarks>
    /// <seealso cref="ReadStructAt{T}(long)"/>
    /// <seealso cref="ReadBigEndianStructArrayAt{T}(long, int)"/>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadBigEndianStructAt<T>(long offset) where T : unmanaged, IBigEndianStruct<T>
    {
        T value = default;
        ReadAt(offset, MemoryMarshal.AsBytes(new Span<T>(ref value)));
        return BitConverter.IsLittleEndian ? T.ReverseEndianness(value) : value;
    }

    /// <summary>Reads <paramref name="destination"/>.Length big-endian unmanaged structs of type <typeparamref name="T"/> at an absolute offset, swapping multi-byte fields to native order on little-endian hosts.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IBigEndianStruct{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="destination">Span to fill.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>Reversal is performed per element on little-endian hosts.</remarks>
    /// <seealso cref="ReadStructArrayAt{T}(long, Span{T})"/>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public void ReadBigEndianStructArrayAt<T>(long offset, scoped Span<T> destination)
        where T : unmanaged, IBigEndianStruct<T>
    {
        if (destination.IsEmpty) return;
        ReadAt(offset, MemoryMarshal.AsBytes(destination));
        if (BitConverter.IsLittleEndian)
            for (int i = 0; i < destination.Length; i++)
                destination[i] = T.ReverseEndianness(destination[i]);
    }

    /// <summary>Reads <paramref name="count"/> big-endian unmanaged structs of type <typeparamref name="T"/> at an absolute offset, swapping multi-byte fields to native order on little-endian hosts.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IBigEndianStruct{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset within the source.</param>
    /// <param name="count">The number of elements to read. Must be non-negative.</param>
    /// <returns>A new array containing the values read, with multi-byte fields in native byte order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="ReadBigEndianStructArrayAt{T}(long, Span{T})"/>
    /// <seealso cref="ReadBigEndianStructAt{T}(long)"/>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public T[] ReadBigEndianStructArrayAt<T>(long offset, int count)
        where T : unmanaged, IBigEndianStruct<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new T[count];
        ReadBigEndianStructArrayAt(offset, result);
        return result;
    }

    // ─────────────── Offset array + struct reads ───────────────

    /// <summary>Reads <paramref name="count"/> 8-bit offsets at <paramref name="offset"/> and reads an unmanaged struct of type <typeparamref name="T"/> at each offset. No endianness conversion is performed.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8ArrayPeekBigEndianStructAt{T}(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public T[] ReadOffset8ArrayPeekStructAt<T>(long offset, int count) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var offsets = ReadUInt8ArrayAt(offset, count);
        var result = new T[count];
        for (int i = 0; i < count; i++)
            result[i] = ReadStructAt<T>(offsets[i]);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 16-bit offsets at <paramref name="offset"/> and reads an unmanaged struct of type <typeparamref name="T"/> at each offset. No endianness conversion is performed.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset16ArrayPeekBigEndianStructAt{T}(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public T[] ReadOffset16ArrayPeekStructAt<T>(long offset, int count) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var offsets = ReadUInt16ArrayAt(offset, count);
        var result = new T[count];
        for (int i = 0; i < count; i++)
            result[i] = ReadStructAt<T>(offsets[i]);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 24-bit offsets at <paramref name="offset"/> and reads an unmanaged struct of type <typeparamref name="T"/> at each offset. No endianness conversion is performed.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset24ArrayPeekBigEndianStructAt{T}(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public T[] ReadOffset24ArrayPeekStructAt<T>(long offset, int count) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var offsets = ReadUInt24ArrayAt(offset, count);
        var result = new T[count];
        for (int i = 0; i < count; i++)
            result[i] = ReadStructAt<T>(offsets[i]);   // UInt24 → uint → long
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 32-bit offsets at <paramref name="offset"/> and reads an unmanaged struct of type <typeparamref name="T"/> at each offset. No endianness conversion is performed.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset32ArrayPeekBigEndianStructAt{T}(long, int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public T[] ReadOffset32ArrayPeekStructAt<T>(long offset, int count) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var offsets = ReadUInt32ArrayAt(offset, count);
        var result = new T[count];
        for (int i = 0; i < count; i++)
            result[i] = ReadStructAt<T>(offsets[i]);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 8-bit offsets at <paramref name="offset"/> and reads a big-endian struct of type <typeparamref name="T"/> at each offset, swapping multi-byte fields on little-endian hosts.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IBigEndianStruct{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read, with multi-byte fields in native byte order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8ArrayPeekStructAt{T}(long, int)"/>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public T[] ReadOffset8ArrayPeekBigEndianStructAt<T>(long offset, int count)
        where T : unmanaged, IBigEndianStruct<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var offsets = ReadUInt8ArrayAt(offset, count);
        var result = new T[count];
        for (int i = 0; i < count; i++)
            result[i] = ReadBigEndianStructAt<T>(offsets[i]);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 16-bit offsets at <paramref name="offset"/> and reads a big-endian struct of type <typeparamref name="T"/> at each offset, swapping multi-byte fields on little-endian hosts.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IBigEndianStruct{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read, with multi-byte fields in native byte order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset16ArrayPeekStructAt{T}(long, int)"/>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public T[] ReadOffset16ArrayPeekBigEndianStructAt<T>(long offset, int count)
        where T : unmanaged, IBigEndianStruct<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var offsets = ReadUInt16ArrayAt(offset, count);
        var result = new T[count];
        for (int i = 0; i < count; i++)
            result[i] = ReadBigEndianStructAt<T>(offsets[i]);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 24-bit offsets at <paramref name="offset"/> and reads a big-endian struct of type <typeparamref name="T"/> at each offset, swapping multi-byte fields on little-endian hosts.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IBigEndianStruct{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read, with multi-byte fields in native byte order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset24ArrayPeekStructAt{T}(long, int)"/>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public T[] ReadOffset24ArrayPeekBigEndianStructAt<T>(long offset, int count)
        where T : unmanaged, IBigEndianStruct<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var offsets = ReadUInt24ArrayAt(offset, count);
        var result = new T[count];
        for (int i = 0; i < count; i++)
            result[i] = ReadBigEndianStructAt<T>(offsets[i]);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 32-bit offsets at <paramref name="offset"/> and reads a big-endian struct of type <typeparamref name="T"/> at each offset, swapping multi-byte fields on little-endian hosts.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IBigEndianStruct{T}"/>.</typeparam>
    /// <param name="offset">Absolute offset of the offset array.</param>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read, with multi-byte fields in native byte order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative, or <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset32ArrayPeekStructAt{T}(long, int)"/>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public T[] ReadOffset32ArrayPeekBigEndianStructAt<T>(long offset, int count)
        where T : unmanaged, IBigEndianStruct<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var offsets = ReadUInt32ArrayAt(offset, count);
        var result = new T[count];
        for (int i = 0; i < count; i++)
            result[i] = ReadBigEndianStructAt<T>(offsets[i]);
        return result;
    }

    // ───────────────────────── Source factories ─────────────────────────

    /// <summary>Returns a slice of this source starting at <paramref name="shift"/>. Reading offset <c>O</c> on the returned source reads offset <c>O + shift</c> here, bounded to the bytes from <paramref name="shift"/> to the end of this source.</summary>
    /// <param name="shift">The signed offset added to every read offset.</param>
    /// <returns>A <see cref="SliceSource"/> view of this source.</returns>
    /// <remarks>When this source is itself a <see cref="SliceSource"/> and <paramref name="shift"/> is non-negative, the returned slice collapses with this one into a single <see cref="SliceSource"/>. The read path is therefore always one slice hop deep, regardless of how many times <see cref="WithOffset"/> is chained.</remarks>
    /// <seealso cref="WithOffsetAndLength(long, long, bool)"/>
    /// <seealso cref="SliceSource"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType Font</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Source WithOffset(long shift) => new SliceSource(this, shift);

    /// <summary>Returns a slice of this source covering <paramref name="length"/> bytes starting at <paramref name="offset"/>. Reading offset <c>O</c> on the returned source reads offset <c>O + offset</c> here, and reads beyond <paramref name="length"/> throw <see cref="EndOfStreamException"/>.</summary>
    /// <param name="offset">The signed offset added to every read offset.</param>
    /// <param name="length">The number of bytes reachable from offset 0.</param>
    /// <param name="isolated">Indicates whether the slice should be isolated from the parent source.</param>
    /// <returns>A <see cref="SliceSource"/> view of this source with the given region and isolation.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
    /// <remarks>When this source is itself a <see cref="SliceSource"/> and <paramref name="offset"/> is non-negative, the returned slice collapses with this one into a single <see cref="SliceSource"/>. The effective length is capped at whatever the enclosing slice has left from <paramref name="offset"/>.</remarks>
    /// <seealso cref="WithOffset(long)"/>
    /// <seealso cref="SliceSource"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType Font</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Source WithOffsetAndLength(long offset, long length, bool isolated = false) =>
        new SliceSource(this, offset, length, isolated);

    /// <summary>Creates a <see cref="Cursor"/> for reading from this source.</summary>
    /// <param name="position">Initial position, in bytes from the start of the source.</param>
    /// <returns>A new cursor over this source.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is negative.</exception>
    /// <seealso cref="CreateOffsetCursor(long, long)"/>
    /// <seealso cref="CreateSliceCursor(long, long, bool, long)"/>
    /// <seealso cref="Cursor"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Cursor CreateCursor(long position = 0) => new(this, position);

    /// <summary>Creates a <see cref="Cursor"/> over an offset-shifted view of this source.</summary>
    /// <param name="shift">The offset applied to the source.</param>
    /// <param name="position">Initial position within the shifted view.</param>
    /// <returns>A new cursor over the shifted view.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is negative.</exception>
    /// <remarks>The cursor's source is the result of <see cref="WithOffset(long)"/>; internal offsets declared by records read through it resolve relative to <paramref name="shift"/>.</remarks>
    /// <seealso cref="CreateCursor(long)"/>
    /// <seealso cref="CreateSliceCursor(long, long, bool, long)"/>
    /// <seealso cref="WithOffset(long)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Cursor CreateOffsetCursor(long shift, long position = 0) =>
        WithOffset(shift).CreateCursor(position);

    /// <summary>Creates a <see cref="Cursor"/> over a slice of this source.</summary>
    /// <param name="offset">The offset of the slice.</param>
    /// <param name="length">The length of the slice.</param>
    /// <param name="isolated">Indicates whether the slice should be isolated from the parent source.</param>
    /// <param name="position">Initial position within the slice.</param>
    /// <returns>A new cursor over the slice.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative, or <paramref name="position"/> is negative.</exception>
    /// <remarks>The cursor's source is the result of <see cref="WithOffsetAndLength(long, long, bool)"/>; internal offsets declared by records read through it resolve relative to <paramref name="offset"/>.</remarks>
    /// <seealso cref="CreateCursor(long)"/>
    /// <seealso cref="CreateOffsetCursor(long, long)"/>
    /// <seealso cref="WithOffsetAndLength(long, long, bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Cursor CreateSliceCursor(long offset, long length, bool isolated = false, long position = 0) =>
        WithOffsetAndLength(offset, length, isolated).CreateCursor(position);

    // ───────────────────────── Lifetime ─────────────────────────

    /// <summary>Releases the underlying backing.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Implementations release whatever unmanaged resource they own: <see cref="FileSource"/> closes its file handle; <see cref="MemorySource"/> is a no-op; <see cref="SliceSource"/> does not dispose its <see cref="SliceSource.Outer"/>.</description></item>
    /// <item><description>After this call returns, any further read throws <see cref="ObjectDisposedException"/> or produces undefined results depending on the backing.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IDisposable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.idisposable">.NET API: <c>IDisposable</c></seealso>
    public abstract void Dispose();
}
