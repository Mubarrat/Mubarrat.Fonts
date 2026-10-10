using Mubarrat.Fonts.Primitives;
using System.Runtime.CompilerServices;

namespace Mubarrat.Fonts.Binary;

/// <summary>A positional reader over a <see cref="Source"/>, used during the parse phase of a record. A <c>ref struct</c> that cannot be stored, boxed, or captured; a cursor never escapes the <c>Parse</c> method that created it.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The cursor owns its own <see cref="Position"/>. Creating a nested cursor via <see cref="At"/> or <see cref="Here"/> produces an independent cursor and does not affect this one.</description></item>
/// <item><description>Reads are bounds-checked by <see cref="Source.ReadAt"/> against <see cref="Source.Length"/>.</description></item>
/// <item><description>Offsets passed to record helpers are relative to the source origin, not to <see cref="Position"/>. When the source is a table-scoped <see cref="SliceSource"/>, this matches the OpenType convention of table-internal offsets being relative to the table's start.</description></item>
/// </list>
/// <para>For the on-disk layout of every primitive read by this type, see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification, Data Types</see>.</para>
/// </remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/ref-struct">C# reference: <c>ref struct</c></seealso>
/// <seealso cref="Source"/>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "ApiDesign",
    "RS0026:Do not add multiple public overloads with optional parameters",
    Scope = "type",
    Target = "~T:Mubarrat.Fonts.OpenType.Binary.Cursor",
    Justification = "Cursor overloads are disambiguated by the type of their first parameter (int count vs Span<T> destination); the optional 'context' parameter cannot introduce ambiguity between them.")]
public ref struct Cursor
{
    private readonly Source _source;
    private long _position;

    /// <summary>Creates a cursor over <paramref name="source"/> at <paramref name="position"/>.</summary>
    /// <param name="source">The byte source.</param>
    /// <param name="position">Initial position, in bytes from the start of the source.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is negative.</exception>
    /// <seealso cref="Source.CreateCursor(long)"/>
    /// <seealso cref="Source.CreateOffsetCursor(long, long)"/>
    /// <seealso cref="Source.CreateSliceCursor(long, long, bool, long)"/>
    public Cursor(Source source, long position = 0)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        _source = source;
        _position = position;
    }

    /// <summary>Gets the underlying byte source.</summary>
    /// <value>The <see cref="Source"/> passed to the constructor.</value>
    /// <seealso cref="Source"/>
    public readonly Source Source => _source;

    /// <summary>Gets or sets the current position, in bytes from the start of the source.</summary>
    /// <value>The byte offset of the next read, relative to the source origin.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    /// <seealso cref="Length"/>
    /// <seealso cref="Remaining"/>
    public long Position
    {
        readonly get => _position;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _position = value;
        }
    }

    /// <summary>Gets the total length of the underlying source, in bytes.</summary>
    /// <value>Equivalent to <c>Source.Length</c>.</value>
    /// <seealso cref="Position"/>
    /// <seealso cref="Remaining"/>
    public readonly long Length => _source.Length;

    /// <summary>Gets the number of bytes from <see cref="Position"/> to the end of the source.</summary>
    /// <value><c>Length - Position</c>; never negative for a valid cursor.</value>
    /// <seealso cref="Length"/>
    /// <seealso cref="Position"/>
    public readonly long Remaining => _source.Length - _position;

    /// <summary>Reads an unsigned 8-bit integer and advances the position by 1 byte.</summary>
    /// <returns>The byte at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekUInt8"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint8</seealso>
    public byte ReadUInt8()
    {
        byte value = _source.ReadUInt8At(_position);
        _position = checked(_position + 1);
        return value;
    }

    /// <summary>Reads a signed 8-bit integer and advances the position by 1 byte.</summary>
    /// <returns>The signed byte at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekInt8"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int8</seealso>
    public sbyte ReadInt8()
    {
        sbyte value = _source.ReadInt8At(_position);
        _position = checked(_position + 1);
        return value;
    }

    /// <summary>Reads a big-endian unsigned 16-bit integer and advances the position by 2 bytes.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekUInt16"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint16</seealso>
    public ushort ReadUInt16()
    {
        ushort value = _source.ReadUInt16At(_position);
        _position = checked(_position + 2);
        return value;
    }

    /// <summary>Reads a big-endian signed 16-bit integer and advances the position by 2 bytes.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekInt16"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int16</seealso>
    public short ReadInt16()
    {
        short value = _source.ReadInt16At(_position);
        _position = checked(_position + 2);
        return value;
    }

    /// <summary>Reads a big-endian unsigned 24-bit integer and advances the position by 3 bytes.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekUInt24"/>
    /// <seealso cref="UInt24"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint24</seealso>
    public UInt24 ReadUInt24()
    {
        UInt24 value = _source.ReadUInt24At(_position);
        _position = checked(_position + 3);
        return value;
    }

    /// <summary>Reads a big-endian signed 24-bit integer and advances the position by 3 bytes.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekInt24"/>
    /// <seealso cref="Int24"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public Int24 ReadInt24()
    {
        Int24 value = _source.ReadInt24At(_position);
        _position = checked(_position + 3);
        return value;
    }

    /// <summary>Reads a big-endian unsigned 32-bit integer and advances the position by 4 bytes.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekUInt32"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint32</seealso>
    public uint ReadUInt32()
    {
        uint value = _source.ReadUInt32At(_position);
        _position = checked(_position + 4);
        return value;
    }

    /// <summary>Reads a big-endian signed 32-bit integer and advances the position by 4 bytes.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekInt32"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int32</seealso>
    public int ReadInt32()
    {
        int value = _source.ReadInt32At(_position);
        _position = checked(_position + 4);
        return value;
    }

    /// <summary>Reads a big-endian unsigned 64-bit integer and advances the position by 8 bytes.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint64 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekUInt64"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint64</seealso>
    public ulong ReadUInt64()
    {
        ulong value = _source.ReadUInt64At(_position);
        _position = checked(_position + 8);
        return value;
    }

    /// <summary>Reads a big-endian signed 64-bit integer and advances the position by 8 bytes.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int64 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekInt64"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int64</seealso>
    public long ReadInt64()
    {
        long value = _source.ReadInt64At(_position);
        _position = checked(_position + 8);
        return value;
    }

    /// <summary>Reads an unsigned 8-bit integer without advancing the position.</summary>
    /// <returns>The byte at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt8"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint8</seealso>
    public readonly byte PeekUInt8() => _source.ReadUInt8At(_position);

    /// <summary>Reads a signed 8-bit integer without advancing the position.</summary>
    /// <returns>The signed byte at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt8"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int8</seealso>
    public readonly sbyte PeekInt8() => _source.ReadInt8At(_position);

    /// <summary>Reads a big-endian unsigned 16-bit integer without advancing the position.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt16"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint16</seealso>
    public readonly ushort PeekUInt16() => _source.ReadUInt16At(_position);

    /// <summary>Reads a big-endian signed 16-bit integer without advancing the position.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt16"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int16</seealso>
    public readonly short PeekInt16() => _source.ReadInt16At(_position);

    /// <summary>Reads a big-endian unsigned 24-bit integer without advancing the position.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt24"/>
    /// <seealso cref="UInt24"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint24</seealso>
    public readonly UInt24 PeekUInt24() => _source.ReadUInt24At(_position);

    /// <summary>Reads a big-endian signed 24-bit integer without advancing the position.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt24"/>
    /// <seealso cref="Int24"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public readonly Int24 PeekInt24() => _source.ReadInt24At(_position);

    /// <summary>Reads a big-endian unsigned 32-bit integer without advancing the position.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt32"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint32</seealso>
    public readonly uint PeekUInt32() => _source.ReadUInt32At(_position);

    /// <summary>Reads a big-endian signed 32-bit integer without advancing the position.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt32"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int32</seealso>
    public readonly int PeekInt32() => _source.ReadInt32At(_position);

    /// <summary>Reads a big-endian unsigned 64-bit integer without advancing the position.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint64 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt64"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint64</seealso>
    public readonly ulong PeekUInt64() => _source.ReadUInt64At(_position);

    /// <summary>Reads a big-endian signed 64-bit integer without advancing the position.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int64 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt64"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int64</seealso>
    public readonly long PeekInt64() => _source.ReadInt64At(_position);

    /// <summary>Reads <paramref name="count"/> bytes into a new array and advances the position.</summary>
    /// <param name="count">The number of bytes to read. Must be non-negative.</param>
    /// <returns>A new array containing the bytes read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="PeekBytes(int)"/>
    /// <seealso cref="ReadBytes(Span{byte})"/>
    public byte[] ReadBytes(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new byte[count];
        ReadBytes(result);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> bytes into a new array without advancing the position.</summary>
    /// <param name="count">The number of bytes to read. Must be non-negative.</param>
    /// <returns>A new array containing the bytes read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="ReadBytes(int)"/>
    /// <seealso cref="PeekBytes(Span{byte})"/>
    public readonly byte[] PeekBytes(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new byte[count];
        PeekBytes(result);
        return result;
    }

    /// <summary>Reads <paramref name="destination"/>.Length bytes and advances the position.</summary>
    /// <param name="destination">The buffer to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The position is advanced by exactly <c>destination.Length</c> on success. On failure the position is unchanged.</remarks>
    /// <seealso cref="PeekBytes(Span{byte})"/>
    /// <seealso cref="ReadBytes(int)"/>
    public void ReadBytes(scoped Span<byte> destination)
    {
        PeekBytes(destination);
        _position = checked(_position + destination.Length);
    }

    /// <summary>Reads <paramref name="destination"/>.Length bytes without advancing the position.</summary>
    /// <param name="destination">The buffer to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="ReadBytes(Span{byte})"/>
    /// <seealso cref="PeekBytes(int)"/>
    public readonly void PeekBytes(scoped Span<byte> destination) => _source.ReadAt(_position, destination);

    /// <summary>Reads a 16.16 fixed-point value and advances the position by 4 bytes.</summary>
    /// <returns>The <see cref="Fixed"/> value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Fixed data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekFixed"/>
    /// <seealso cref="Fixed"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public Fixed ReadFixed() => (Fixed)ReadInt32();

    /// <summary>Reads a 16.16 fixed-point value without advancing the position.</summary>
    /// <returns>The <see cref="Fixed"/> value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Fixed data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadFixed"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Fixed</seealso>
    public readonly Fixed PeekFixed() => (Fixed)PeekInt32();

    /// <summary>Reads an FWORD (signed 16-bit) and advances the position by 2 bytes.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">FWORD data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekFWord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: FWORD</seealso>
    public short ReadFWord() => ReadInt16();

    /// <summary>Reads an FWORD (signed 16-bit) without advancing the position.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">FWORD data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadFWord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: FWORD</seealso>
    public readonly short PeekFWord() => PeekInt16();

    /// <summary>Reads a UFWORD (unsigned 16-bit) and advances the position by 2 bytes.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">UFWORD data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekUFWord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: UFWORD</seealso>
    public ushort ReadUFWord() => ReadUInt16();

    /// <summary>Reads a UFWORD (unsigned 16-bit) without advancing the position.</summary>
    /// <returns>The value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">UFWORD data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUFWord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: UFWORD</seealso>
    public readonly ushort PeekUFWord() => PeekUInt16();

    /// <summary>Reads an F2DOT14 value and advances the position by 2 bytes.</summary>
    /// <returns>The <see cref="F2Dot14"/> value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">F2DOT14 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekF2Dot14"/>
    /// <seealso cref="F2Dot14"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    public F2Dot14 ReadF2Dot14() => (F2Dot14)ReadInt16();

    /// <summary>Reads an F2DOT14 value without advancing the position.</summary>
    /// <returns>The <see cref="F2Dot14"/> value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">F2DOT14 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadF2Dot14"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: F2DOT14</seealso>
    public readonly F2Dot14 PeekF2Dot14() => (F2Dot14)PeekInt16();

    /// <summary>Seconds between 1904-01-01 (OpenType LONGDATETIME epoch) and 1970-01-01 (Unix epoch).</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">LONGDATETIME data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadLongDateTime"/>
    /// <seealso cref="PeekLongDateTime"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: LONGDATETIME</seealso>
    public const long OpenTypeEpochOffset = 2082844800L;

    /// <summary>Reads a LONGDATETIME and advances the position by 8 bytes.</summary>
    /// <returns>A <see cref="DateTime"/> in UTC, converted from the 1904 epoch to the Unix epoch.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">LONGDATETIME data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekLongDateTime"/>
    /// <seealso cref="OpenTypeEpochOffset"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: LONGDATETIME</seealso>
    public DateTime ReadLongDateTime() =>
        DateTime.UnixEpoch.AddSeconds(ReadInt64() - OpenTypeEpochOffset);

    /// <summary>Reads a LONGDATETIME without advancing the position.</summary>
    /// <returns>A <see cref="DateTime"/> in UTC, converted from the 1904 epoch to the Unix epoch.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">LONGDATETIME data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadLongDateTime"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: LONGDATETIME</seealso>
    public readonly DateTime PeekLongDateTime() =>
        DateTime.UnixEpoch.AddSeconds(PeekInt64() - OpenTypeEpochOffset);

    /// <summary>Reads a 4-byte tag and advances the position by 4 bytes.</summary>
    /// <returns>The <see cref="Tag"/> value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Tag data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekTag"/>
    /// <seealso cref="Tag"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Tag</seealso>
    public Tag ReadTag() => new(ReadUInt32());

    /// <summary>Reads a 4-byte tag without advancing the position.</summary>
    /// <returns>The <see cref="Tag"/> value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Tag data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadTag"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Tag</seealso>
    public readonly Tag PeekTag() => new(PeekUInt32());

    /// <summary>Reads an 8-bit offset and advances the position by 1 byte.</summary>
    /// <returns>The offset value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset8"/>
    /// <seealso cref="ReadOffset16"/>
    /// <seealso cref="ReadOffset24"/>
    /// <seealso cref="ReadOffset32"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public byte ReadOffset8() => ReadUInt8();

    /// <summary>Reads an 8-bit offset without advancing the position.</summary>
    /// <returns>The offset value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public readonly byte PeekOffset8() => PeekUInt8();

    /// <summary>Reads a 16-bit offset and advances the position by 2 bytes.</summary>
    /// <returns>The offset value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset16"/>
    /// <seealso cref="ReadOffset8"/>
    /// <seealso cref="ReadOffset24"/>
    /// <seealso cref="ReadOffset32"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public ushort ReadOffset16() => ReadUInt16();

    /// <summary>Reads a 16-bit offset without advancing the position.</summary>
    /// <returns>The offset value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset16"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public readonly ushort PeekOffset16() => PeekUInt16();

    /// <summary>Reads a 24-bit offset and advances the position by 3 bytes.</summary>
    /// <returns>The offset value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset24"/>
    /// <seealso cref="ReadOffset8"/>
    /// <seealso cref="ReadOffset16"/>
    /// <seealso cref="ReadOffset32"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public UInt24 ReadOffset24() => ReadUInt24();

    /// <summary>Reads a 24-bit offset without advancing the position.</summary>
    /// <returns>The offset value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset24"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public readonly UInt24 PeekOffset24() => PeekUInt24();

    /// <summary>Reads a 32-bit offset and advances the position by 4 bytes.</summary>
    /// <returns>The offset value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset32"/>
    /// <seealso cref="ReadOffset8"/>
    /// <seealso cref="ReadOffset16"/>
    /// <seealso cref="ReadOffset24"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public uint ReadOffset32() => ReadUInt32();

    /// <summary>Reads a 32-bit offset without advancing the position.</summary>
    /// <returns>The offset value at <see cref="Position"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset32"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public readonly uint PeekOffset32() => PeekUInt32();

    /// <summary>Reads a Version16Dot16 value and advances the position by 4 bytes.</summary>
    /// <returns>The value at <see cref="Position"/>, decoded as a <see cref="double"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Version16Dot16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekVersion16Dot16"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Version16Dot16</seealso>
    public double ReadVersion16Dot16() => ReadUInt32() / 65536.0;

    /// <summary>Reads a Version16Dot16 value without advancing the position.</summary>
    /// <returns>The value at <see cref="Position"/>, decoded as a <see cref="double"/>.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Version16Dot16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadVersion16Dot16"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Version16Dot16</seealso>
    public readonly double PeekVersion16Dot16() => PeekUInt32() / 65536.0;

    /// <summary>Reads an array of unsigned 8-bit integers and advances the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekUInt8Array(int)"/>
    /// <seealso cref="ReadUInt8Array(Span{byte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint8</seealso>
    public byte[] ReadUInt8Array(int count)
    {
        var result = PeekUInt8Array(count);
        _position = checked(_position + result.Length);
        return result;
    }

    /// <summary>Reads an array of unsigned 8-bit integers without advancing the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt8Array(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint8</seealso>
    public readonly byte[] PeekUInt8Array(int count) =>
        _source.ReadUInt8ArrayAt(_position, count);

    /// <summary>Fills <paramref name="destination"/> with unsigned 8-bit integers and advances the position.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekUInt8Array(Span{byte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint8</seealso>
    public void ReadUInt8Array(Span<byte> destination)
    {
        PeekUInt8Array(destination);
        _position = checked(_position + destination.Length);
    }

    /// <summary>Fills <paramref name="destination"/> with unsigned 8-bit integers without advancing.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt8Array(Span{byte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint8</seealso>
    public readonly void PeekUInt8Array(Span<byte> destination) =>
        _source.ReadUInt8ArrayAt(_position, destination);

    /// <summary>Reads an array of signed 8-bit integers and advances the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekInt8Array(int)"/>
    /// <seealso cref="ReadInt8Array(Span{sbyte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int8</seealso>
    public sbyte[] ReadInt8Array(int count)
    {
        var result = PeekInt8Array(count);
        _position = checked(_position + result.Length);
        return result;
    }

    /// <summary>Reads an array of signed 8-bit integers without advancing the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt8Array(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int8</seealso>
    public readonly sbyte[] PeekInt8Array(int count) =>
        _source.ReadInt8ArrayAt(_position, count);

    /// <summary>Fills <paramref name="destination"/> with signed 8-bit integers and advances the position.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekInt8Array(Span{sbyte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int8</seealso>
    public void ReadInt8Array(Span<sbyte> destination)
    {
        PeekInt8Array(destination);
        _position = checked(_position + destination.Length);
    }

    /// <summary>Fills <paramref name="destination"/> with signed 8-bit integers without advancing.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt8Array(Span{sbyte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int8</seealso>
    public readonly void PeekInt8Array(Span<sbyte> destination) =>
        _source.ReadInt8ArrayAt(_position, destination);

    /// <summary>Reads an array of unsigned 16-bit integers and advances the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekUInt16Array(int)"/>
    /// <seealso cref="ReadUInt16Array(Span{ushort})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint16</seealso>
    public ushort[] ReadUInt16Array(int count)
    {
        var result = PeekUInt16Array(count);
        _position = checked(_position + result.Length * 2);
        return result;
    }

    /// <summary>Reads an array of unsigned 16-bit integers without advancing the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt16Array(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint16</seealso>
    public readonly ushort[] PeekUInt16Array(int count) =>
        _source.ReadUInt16ArrayAt(_position, count);

    /// <summary>Fills <paramref name="destination"/> with unsigned 16-bit integers and advances the position.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekUInt16Array(Span{ushort})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint16</seealso>
    public void ReadUInt16Array(Span<ushort> destination)
    {
        PeekUInt16Array(destination);
        _position = checked(_position + destination.Length * 2);
    }

    /// <summary>Fills <paramref name="destination"/> with unsigned 16-bit integers without advancing.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt16Array(Span{ushort})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint16</seealso>
    public readonly void PeekUInt16Array(Span<ushort> destination) =>
        _source.ReadUInt16ArrayAt(_position, destination);

    /// <summary>Reads an array of signed 16-bit integers and advances the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekInt16Array(int)"/>
    /// <seealso cref="ReadInt16Array(Span{short})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int16</seealso>
    public short[] ReadInt16Array(int count)
    {
        var result = PeekInt16Array(count);
        _position = checked(_position + result.Length * 2);
        return result;
    }

    /// <summary>Reads an array of signed 16-bit integers without advancing the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt16Array(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int16</seealso>
    public readonly short[] PeekInt16Array(int count) =>
        _source.ReadInt16ArrayAt(_position, count);

    /// <summary>Fills <paramref name="destination"/> with signed 16-bit integers and advances the position.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekInt16Array(Span{short})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int16</seealso>
    public void ReadInt16Array(Span<short> destination)
    {
        PeekInt16Array(destination);
        _position = checked(_position + destination.Length * 2);
    }

    /// <summary>Fills <paramref name="destination"/> with signed 16-bit integers without advancing.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt16Array(Span{short})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int16</seealso>
    public readonly void PeekInt16Array(Span<short> destination) =>
        _source.ReadInt16ArrayAt(_position, destination);

    /// <summary>Reads an array of unsigned 24-bit integers and advances the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekUInt24Array(int)"/>
    /// <seealso cref="ReadUInt24Array(Span{UInt24})"/>
    /// <seealso cref="UInt24"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint24</seealso>
    public UInt24[] ReadUInt24Array(int count)
    {
        var result = PeekUInt24Array(count);
        _position = checked(_position + result.Length * 3);
        return result;
    }

    /// <summary>Reads an array of unsigned 24-bit integers without advancing the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt24Array(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint24</seealso>
    public readonly UInt24[] PeekUInt24Array(int count) =>
        _source.ReadUInt24ArrayAt(_position, count);

    /// <summary>Fills <paramref name="destination"/> with unsigned 24-bit integers and advances the position.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekUInt24Array(Span{UInt24})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint24</seealso>
    public void ReadUInt24Array(Span<UInt24> destination)
    {
        PeekUInt24Array(destination);
        _position = checked(_position + destination.Length * 3);
    }

    /// <summary>Fills <paramref name="destination"/> with unsigned 24-bit integers without advancing.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt24Array(Span{UInt24})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint24</seealso>
    public readonly void PeekUInt24Array(Span<UInt24> destination) =>
        _source.ReadUInt24ArrayAt(_position, destination);

    /// <summary>Reads an array of signed 24-bit integers and advances the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekInt24Array(int)"/>
    /// <seealso cref="ReadInt24Array(Span{Int24})"/>
    /// <seealso cref="Int24"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public Int24[] ReadInt24Array(int count)
    {
        var result = PeekInt24Array(count);
        _position = checked(_position + result.Length * 3);
        return result;
    }

    /// <summary>Reads an array of signed 24-bit integers without advancing the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt24Array(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public readonly Int24[] PeekInt24Array(int count) =>
        _source.ReadInt24ArrayAt(_position, count);

    /// <summary>Fills <paramref name="destination"/> with signed 24-bit integers and advances the position.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekInt24Array(Span{Int24})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public void ReadInt24Array(Span<Int24> destination)
    {
        PeekInt24Array(destination);
        _position = checked(_position + destination.Length * 3);
    }

    /// <summary>Fills <paramref name="destination"/> with signed 24-bit integers without advancing.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt24Array(Span{Int24})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int24</seealso>
    public readonly void PeekInt24Array(Span<Int24> destination) =>
        _source.ReadInt24ArrayAt(_position, destination);

    /// <summary>Reads an array of unsigned 32-bit integers and advances the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekUInt32Array(int)"/>
    /// <seealso cref="ReadUInt32Array(Span{uint})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint32</seealso>
    public uint[] ReadUInt32Array(int count)
    {
        var result = PeekUInt32Array(count);
        _position = checked(_position + result.Length * 4);
        return result;
    }

    /// <summary>Reads an array of unsigned 32-bit integers without advancing the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt32Array(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint32</seealso>
    public readonly uint[] PeekUInt32Array(int count) =>
        _source.ReadUInt32ArrayAt(_position, count);

    /// <summary>Fills <paramref name="destination"/> with unsigned 32-bit integers and advances the position.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekUInt32Array(Span{uint})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint32</seealso>
    public void ReadUInt32Array(Span<uint> destination)
    {
        PeekUInt32Array(destination);
        _position = checked(_position + destination.Length * 4);
    }

    /// <summary>Fills <paramref name="destination"/> with unsigned 32-bit integers without advancing.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">uint32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadUInt32Array(Span{uint})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: uint32</seealso>
    public readonly void PeekUInt32Array(Span<uint> destination) =>
        _source.ReadUInt32ArrayAt(_position, destination);

    /// <summary>Reads an array of signed 32-bit integers and advances the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekInt32Array(int)"/>
    /// <seealso cref="ReadInt32Array(Span{int})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int32</seealso>
    public int[] ReadInt32Array(int count)
    {
        var result = PeekInt32Array(count);
        _position = checked(_position + result.Length * 4);
        return result;
    }

    /// <summary>Reads an array of signed 32-bit integers without advancing the position.</summary>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt32Array(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int32</seealso>
    public readonly int[] PeekInt32Array(int count) =>
        _source.ReadInt32ArrayAt(_position, count);

    /// <summary>Fills <paramref name="destination"/> with signed 32-bit integers and advances the position.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekInt32Array(Span{int})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int32</seealso>
    public void ReadInt32Array(Span<int> destination)
    {
        PeekInt32Array(destination);
        _position = checked(_position + destination.Length * 4);
    }

    /// <summary>Fills <paramref name="destination"/> with signed 32-bit integers without advancing.</summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">int32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadInt32Array(Span{int})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: int32</seealso>
    public readonly void PeekInt32Array(Span<int> destination) =>
        _source.ReadInt32ArrayAt(_position, destination);

    /// <summary>Reads <paramref name="count"/> 8-bit offsets and interprets each.</summary>
    /// <typeparam name="T">The interpreter's return type.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="interpreter">The function applied to each offset value.</param>
    /// <returns>A new array of interpreted values, one per offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="interpreter"/> is <c>null</c>.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset8ArrayInterpret{T}(int, Func{byte, T})"/>
    /// <seealso cref="ReadOffset16ArrayInterpret{T}(int, Func{ushort, T})"/>
    /// <seealso cref="ReadOffset24ArrayInterpret{T}(int, Func{UInt24, T})"/>
    /// <seealso cref="ReadOffset32ArrayInterpret{T}(int, Func{uint, T})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public T[] ReadOffset8ArrayInterpret<T>(int count, Func<byte, T> interpreter)
    {
        var result = PeekOffset8ArrayInterpret(count, interpreter);
        _position = checked(_position + result.Length);
        return result;
    }

    /// <summary>Reads and interprets <paramref name="count"/> 8-bit offsets without advancing.</summary>
    /// <typeparam name="T">The interpreter's return type.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="interpreter">The function applied to each offset value.</param>
    /// <returns>A new array of interpreted values, one per offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="interpreter"/> is <c>null</c>.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8ArrayInterpret{T}(int, Func{byte, T})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public readonly T[] PeekOffset8ArrayInterpret<T>(int count, Func<byte, T> interpreter) =>
        _source.ReadOffset8ArrayInterpretAt(_position, count, interpreter);

    /// <summary>Reads <paramref name="count"/> 16-bit offsets and interprets each.</summary>
    /// <typeparam name="T">The interpreter's return type.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="interpreter">The function applied to each offset value.</param>
    /// <returns>A new array of interpreted values, one per offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="interpreter"/> is <c>null</c>.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset16ArrayInterpret{T}(int, Func{ushort, T})"/>
    /// <seealso cref="ReadOffset8ArrayInterpret{T}(int, Func{byte, T})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public T[] ReadOffset16ArrayInterpret<T>(int count, Func<ushort, T> interpreter)
    {
        var result = PeekOffset16ArrayInterpret(count, interpreter);
        _position = checked(_position + result.Length * 2);
        return result;
    }

    /// <summary>Reads and interprets <paramref name="count"/> 16-bit offsets without advancing.</summary>
    /// <typeparam name="T">The interpreter's return type.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="interpreter">The function applied to each offset value.</param>
    /// <returns>A new array of interpreted values, one per offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="interpreter"/> is <c>null</c>.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset16ArrayInterpret{T}(int, Func{ushort, T})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public readonly T[] PeekOffset16ArrayInterpret<T>(int count, Func<ushort, T> interpreter) =>
        _source.ReadOffset16ArrayInterpretAt(_position, count, interpreter);

    /// <summary>Reads <paramref name="count"/> 24-bit offsets and interprets each.</summary>
    /// <typeparam name="T">The interpreter's return type.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="interpreter">The function applied to each offset value.</param>
    /// <returns>A new array of interpreted values, one per offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="interpreter"/> is <c>null</c>.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset24ArrayInterpret{T}(int, Func{UInt24, T})"/>
    /// <seealso cref="ReadOffset8ArrayInterpret{T}(int, Func{byte, T})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public T[] ReadOffset24ArrayInterpret<T>(int count, Func<UInt24, T> interpreter)
    {
        var result = PeekOffset24ArrayInterpret(count, interpreter);
        _position = checked(_position + result.Length * 3);
        return result;
    }

    /// <summary>Reads and interprets <paramref name="count"/> 24-bit offsets without advancing.</summary>
    /// <typeparam name="T">The interpreter's return type.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="interpreter">The function applied to each offset value.</param>
    /// <returns>A new array of interpreted values, one per offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="interpreter"/> is <c>null</c>.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset24ArrayInterpret{T}(int, Func{UInt24, T})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public readonly T[] PeekOffset24ArrayInterpret<T>(int count, Func<UInt24, T> interpreter) =>
        _source.ReadOffset24ArrayInterpretAt(_position, count, interpreter);

    /// <summary>Reads <paramref name="count"/> 32-bit offsets and interprets each.</summary>
    /// <typeparam name="T">The interpreter's return type.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="interpreter">The function applied to each offset value.</param>
    /// <returns>A new array of interpreted values, one per offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="interpreter"/> is <c>null</c>.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset32ArrayInterpret{T}(int, Func{uint, T})"/>
    /// <seealso cref="ReadOffset8ArrayInterpret{T}(int, Func{byte, T})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public T[] ReadOffset32ArrayInterpret<T>(int count, Func<uint, T> interpreter)
    {
        var result = PeekOffset32ArrayInterpret(count, interpreter);
        _position = checked(_position + result.Length * 4);
        return result;
    }

    /// <summary>Reads and interprets <paramref name="count"/> 32-bit offsets without advancing.</summary>
    /// <typeparam name="T">The interpreter's return type.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="interpreter">The function applied to each offset value.</param>
    /// <returns>A new array of interpreted values, one per offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="interpreter"/> is <c>null</c>.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset32ArrayInterpret{T}(int, Func{uint, T})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public readonly T[] PeekOffset32ArrayInterpret<T>(int count, Func<uint, T> interpreter) =>
        _source.ReadOffset32ArrayInterpretAt(_position, count, interpreter);

    /// <summary>Parses a record of type <typeparamref name="T"/> at the current position and advances the position past it.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="context">External values the record needs, or <c>null</c> when the record's own bytes are sufficient.</param>
    /// <returns>The parsed record.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>On success the cursor advances by however many bytes the record's <c>Parse</c> consumed.</remarks>
    /// <seealso cref="PeekRecord{T}(object?)"/>
    /// <seealso cref="ReadRecordArray{T}(int, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    public T ReadRecord<T>(object? context = null) where T : IRecord<T> => T.Parse(ref this, context);

    /// <summary>Parses <paramref name="count"/> consecutive records of type <typeparamref name="T"/> at the current position and advances the position past them.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="count">The number of records. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <returns>A new array of parsed records, one per record, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>Records are parsed sequentially; the position is advanced by the sum of the bytes each record consumed.</remarks>
    /// <seealso cref="ReadRecord{T}(object?)"/>
    /// <seealso cref="PeekRecordArray{T}(int, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    public T[] ReadRecordArray<T>(int count, object? context = null) where T : IRecord<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new T[count];
        ReadRecordArray(result, context);
        return result;
    }

    /// <summary>Parses a record of type <typeparamref name="T"/> at the current position without advancing the position.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="context">External values the record needs, or <c>null</c> when the record's own bytes are sufficient.</param>
    /// <returns>The parsed record.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The cursor's <see cref="Position"/> is not modified.</remarks>
    /// <seealso cref="ReadRecord{T}(object?)"/>
    /// <seealso cref="IRecord{T}"/>
    public readonly T PeekRecord<T>(object? context = null) where T : IRecord<T> =>
        _source.ParseRecordAt<T>(_position, context);

    /// <summary>Parses <paramref name="count"/> records of type <typeparamref name="T"/> at the current position without advancing the position.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="count">The number of records. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <returns>A new array of parsed records, one per record, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The cursor's <see cref="Position"/> is not modified.</remarks>
    /// <seealso cref="ReadRecordArray{T}(int, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    public readonly T[] PeekRecordArray<T>(int count, object? context = null) where T : IRecord<T> =>
        _source.ParseRecordArrayAt<T>(_position, count, context);

    /// <summary>Reads <paramref name="count"/> 8-bit offsets and parses a record of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <returns>A new array of parsed records, one per offset, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The offsets are relative to the source origin, not to <see cref="Position"/>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset8ArrayPeekRecord{T}(int, object?)"/>
    /// <seealso cref="ReadOffset16ArrayPeekRecord{T}(int, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public T[] ReadOffset8ArrayPeekRecord<T>(int count, object? context = null) where T : IRecord<T>
    {
        var result = PeekOffset8ArrayPeekRecord<T>(count, context);
        _position = checked(_position + count);
        return result;
    }

    /// <summary>Reads and interprets <paramref name="count"/> 8-bit offsets without advancing, and parses a record at each offset.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <returns>A new array of parsed records, one per offset, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The cursor's <see cref="Position"/> is not modified. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8ArrayPeekRecord{T}(int, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public readonly T[] PeekOffset8ArrayPeekRecord<T>(int count, object? context = null) where T : IRecord<T> =>
        _source.ReadOffset8ArrayParseRecordAt<T>(_position, count, context);

    /// <summary>Reads <paramref name="count"/> 16-bit offsets and parses a record of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <returns>A new array of parsed records, one per offset, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The offsets are relative to the source origin, not to <see cref="Position"/>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset16ArrayPeekRecord{T}(int, object?)"/>
    /// <seealso cref="ReadOffset8ArrayPeekRecord{T}(int, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public T[] ReadOffset16ArrayPeekRecord<T>(int count, object? context = null) where T : IRecord<T>
    {
        var result = PeekOffset16ArrayPeekRecord<T>(count, context);
        _position = checked(_position + (long)count * 2);
        return result;
    }

    /// <summary>Reads and interprets <paramref name="count"/> 16-bit offsets without advancing, and parses a record at each offset.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <returns>A new array of parsed records, one per offset, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The cursor's <see cref="Position"/> is not modified. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset16ArrayPeekRecord{T}(int, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public readonly T[] PeekOffset16ArrayPeekRecord<T>(int count, object? context = null) where T : IRecord<T> =>
        _source.ReadOffset16ArrayParseRecordAt<T>(_position, count, context);

    /// <summary>Reads <paramref name="count"/> 24-bit offsets and parses a record of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <returns>A new array of parsed records, one per offset, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The offsets are relative to the source origin, not to <see cref="Position"/>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset24ArrayPeekRecord{T}(int, object?)"/>
    /// <seealso cref="ReadOffset8ArrayPeekRecord{T}(int, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public T[] ReadOffset24ArrayPeekRecord<T>(int count, object? context = null) where T : IRecord<T>
    {
        var result = PeekOffset24ArrayPeekRecord<T>(count, context);
        _position = checked(_position + (long)count * 3);
        return result;
    }

    /// <summary>Reads and interprets <paramref name="count"/> 24-bit offsets without advancing, and parses a record at each offset.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <returns>A new array of parsed records, one per offset, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The cursor's <see cref="Position"/> is not modified. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset24ArrayPeekRecord{T}(int, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public readonly T[] PeekOffset24ArrayPeekRecord<T>(int count, object? context = null) where T : IRecord<T> =>
        _source.ReadOffset24ArrayParseRecordAt<T>(_position, count, context);

    /// <summary>Reads <paramref name="count"/> 32-bit offsets and parses a record of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <returns>A new array of parsed records, one per offset, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The offsets are relative to the source origin, not to <see cref="Position"/>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset32ArrayPeekRecord{T}(int, object?)"/>
    /// <seealso cref="ReadOffset8ArrayPeekRecord{T}(int, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public T[] ReadOffset32ArrayPeekRecord<T>(int count, object? context = null) where T : IRecord<T>
    {
        var result = PeekOffset32ArrayPeekRecord<T>(count, context);
        _position = checked(_position + (long)count * 4);
        return result;
    }

    /// <summary>Reads and interprets <paramref name="count"/> 32-bit offsets without advancing, and parses a record at each offset.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <returns>A new array of parsed records, one per offset, in source order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The cursor's <see cref="Position"/> is not modified. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset32ArrayPeekRecord{T}(int, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public readonly T[] PeekOffset32ArrayPeekRecord<T>(int count, object? context = null) where T : IRecord<T> =>
        _source.ReadOffset32ArrayParseRecordAt<T>(_position, count, context);

    /// <summary>Parses <paramref name="destination"/>.Length records of type <typeparamref name="T"/> at the current position and advances the position past them.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="destination">The span to fill. Its length determines the number of records.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>Records are parsed sequentially; the position is advanced by the sum of the bytes each record consumed.</remarks>
    /// <seealso cref="PeekRecordArray{T}(Span{T}, object?)"/>
    /// <seealso cref="ReadRecordArray{T}(int, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    public void ReadRecordArray<T>(scoped Span<T> destination, object? context = null)
        where T : IRecord<T>
    {
        if (destination.IsEmpty) return;
        for (int i = 0; i < destination.Length; i++)
        {
            var cursor = _source.CreateOffsetCursor(_position);
            destination[i] = T.Parse(ref cursor, context);
            _position = checked(_position + cursor.Position);
        }
    }

    /// <summary>Parses <paramref name="destination"/>.Length records of type <typeparamref name="T"/> at the current position without advancing the position.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="destination">The span to fill. Its length determines the number of records.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The cursor's <see cref="Position"/> is not modified.</remarks>
    /// <seealso cref="ReadRecordArray{T}(Span{T}, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    public readonly void PeekRecordArray<T>(Span<T> destination, object? context = null)
        where T : IRecord<T>
    {
        if (!destination.IsEmpty) _source.ParseRecordArrayAt(_position, destination, context);
    }

    /// <summary>Reads <paramref name="destination"/>.Length 8-bit offsets and parses a record of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="destination">The span to fill. Its length determines the number of offsets.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset8ArrayPeekRecord{T}(Span{T}, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public void ReadOffset8ArrayPeekRecord<T>(Span<T> destination, object? context = null)
        where T : IRecord<T>
    {
        if (destination.IsEmpty) return;
        PeekOffset8ArrayPeekRecord(destination, context);
        _position = checked(_position + destination.Length);
    }

    /// <summary>Reads and interprets <paramref name="destination"/>.Length 8-bit offsets without advancing, and parses a record at each offset.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="destination">The span to fill. Its length determines the number of offsets.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The cursor's <see cref="Position"/> is not modified. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8ArrayPeekRecord{T}(Span{T}, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public readonly void PeekOffset8ArrayPeekRecord<T>(Span<T> destination, object? context = null)
        where T : IRecord<T>
    {
        if (!destination.IsEmpty) _source.ReadOffset8ArrayParseRecordAt(_position, destination, context);
    }

    /// <summary>Reads <paramref name="destination"/>.Length 16-bit offsets and parses a record of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="destination">The span to fill. Its length determines the number of offsets.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset16ArrayPeekRecord{T}(Span{T}, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public void ReadOffset16ArrayPeekRecord<T>(Span<T> destination, object? context = null)
        where T : IRecord<T>
    {
        if (destination.IsEmpty) return;
        PeekOffset16ArrayPeekRecord(destination, context);
        _position = checked(_position + (long)destination.Length * 2);
    }

    /// <summary>Reads and interprets <paramref name="destination"/>.Length 16-bit offsets without advancing, and parses a record at each offset.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="destination">The span to fill. Its length determines the number of offsets.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The cursor's <see cref="Position"/> is not modified. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset16ArrayPeekRecord{T}(Span{T}, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public readonly void PeekOffset16ArrayPeekRecord<T>(Span<T> destination, object? context = null)
        where T : IRecord<T>
    {
        if (!destination.IsEmpty) _source.ReadOffset16ArrayParseRecordAt(_position, destination, context);
    }

    /// <summary>Reads <paramref name="destination"/>.Length 24-bit offsets and parses a record of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="destination">The span to fill. Its length determines the number of offsets.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset24ArrayPeekRecord{T}(Span{T}, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public void ReadOffset24ArrayPeekRecord<T>(Span<T> destination, object? context = null)
        where T : IRecord<T>
    {
        if (destination.IsEmpty) return;
        PeekOffset24ArrayPeekRecord(destination, context);
        _position = checked(_position + (long)destination.Length * 3);
    }

    /// <summary>Reads and interprets <paramref name="destination"/>.Length 24-bit offsets without advancing, and parses a record at each offset.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="destination">The span to fill. Its length determines the number of offsets.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The cursor's <see cref="Position"/> is not modified. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset24ArrayPeekRecord{T}(Span{T}, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public readonly void PeekOffset24ArrayPeekRecord<T>(Span<T> destination, object? context = null)
        where T : IRecord<T>
    {
        if (!destination.IsEmpty) _source.ReadOffset24ArrayParseRecordAt(_position, destination, context);
    }

    /// <summary>Reads <paramref name="destination"/>.Length 32-bit offsets and parses a record of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="destination">The span to fill. Its length determines the number of offsets.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset32ArrayPeekRecord{T}(Span{T}, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public void ReadOffset32ArrayPeekRecord<T>(Span<T> destination, object? context = null)
        where T : IRecord<T>
    {
        if (destination.IsEmpty) return;
        PeekOffset32ArrayPeekRecord(destination, context);
        _position = checked(_position + (long)destination.Length * 4);
    }

    /// <summary>Reads and interprets <paramref name="destination"/>.Length 32-bit offsets without advancing, and parses a record at each offset.</summary>
    /// <typeparam name="T">The record type. Must implement <see cref="IRecord{T}"/>.</typeparam>
    /// <param name="destination">The span to fill. Its length determines the number of offsets.</param>
    /// <param name="context">External values each record needs, or <c>null</c> when the records' own bytes are sufficient.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The cursor's <see cref="Position"/> is not modified. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset32ArrayPeekRecord{T}(Span{T}, object?)"/>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public readonly void PeekOffset32ArrayPeekRecord<T>(Span<T> destination, object? context = null)
        where T : IRecord<T>
    {
        if (!destination.IsEmpty) _source.ReadOffset32ArrayParseRecordAt(_position, destination, context);
    }

    /// <summary>Reads an unmanaged struct of type <typeparamref name="T"/> at the current position and advances by <c>sizeof(T)</c> bytes. No endianness conversion is performed.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <returns>The value read from the source bytes.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The caller is responsible for swapping multi-byte fields when the on-disk byte order does not match the host.</description></item>
    /// <item><description>For big-endian structs, use <see cref="ReadBigEndianStruct{T}"/> instead.</description></item>
    /// <item><description>See <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType Data Types</see> for the on-disk format of primitive fields.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PeekStruct{T}"/>
    /// <seealso cref="ReadBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public T ReadStruct<T>() where T : unmanaged
    {
        T value = _source.ReadStructAt<T>(_position);
        _position = checked(_position + Unsafe.SizeOf<T>());
        return value;
    }

    /// <summary>Reads an unmanaged struct of type <typeparamref name="T"/> at the current position without advancing. No endianness conversion is performed.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <returns>The value read from the source bytes.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The cursor's <see cref="Position"/> is not modified. See <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType Data Types</see> for the on-disk format of primitive fields.</remarks>
    /// <seealso cref="ReadStruct{T}"/>
    /// <seealso cref="PeekBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public readonly T PeekStruct<T>() where T : unmanaged =>
        _source.ReadStructAt<T>(_position);

    /// <summary>Reads a big-endian unmanaged struct of type <typeparamref name="T"/> at the current position and advances by <c>sizeof(T)</c> bytes, swapping multi-byte fields on little-endian hosts.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IEndianReversibleStruct{T}"/> so its multi-byte fields can be reversed.</typeparam>
    /// <returns>The value read from the source bytes, with multi-byte fields in native order.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>This is the reader to use for every OpenType header or record struct, since the format is defined as big-endian on disk. See <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType Data Types</see>.</remarks>
    /// <seealso cref="PeekBigEndianStruct{T}"/>
    /// <seealso cref="ReadStruct{T}"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public T ReadBigEndianStruct<T>() where T : unmanaged, IEndianReversibleStruct<T>
    {
        T value = _source.ReadEndianReversibleStructAt<T>(_position);
        _position = checked(_position + Unsafe.SizeOf<T>());
        return value;
    }

    /// <summary>Reads a big-endian unmanaged struct of type <typeparamref name="T"/> at the current position without advancing, swapping multi-byte fields on little-endian hosts.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IEndianReversibleStruct{T}"/> so its multi-byte fields can be reversed.</typeparam>
    /// <returns>The value read from the source bytes, with multi-byte fields in native order.</returns>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>The cursor's <see cref="Position"/> is not modified. See <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType Data Types</see>.</remarks>
    /// <seealso cref="ReadBigEndianStruct{T}"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
    public readonly T PeekBigEndianStruct<T>() where T : unmanaged, IEndianReversibleStruct<T> =>
        _source.ReadEndianReversibleStructAt<T>(_position);

    /// <summary>Reads <paramref name="count"/> unmanaged structs of type <typeparamref name="T"/> and advances by <c>count * sizeof(T)</c> bytes. No endianness conversion is performed.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="PeekStructArray{T}(int)"/>
    /// <seealso cref="ReadBigEndianStructArray{T}(int)"/>
    public T[] ReadStructArray<T>(int count) where T : unmanaged
    {
        var result = PeekStructArray<T>(count);
        _position = checked(_position + result.Length * Unsafe.SizeOf<T>());
        return result;
    }

    /// <summary>Reads <paramref name="count"/> unmanaged structs of type <typeparamref name="T"/> without advancing. No endianness conversion is performed.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="ReadStructArray{T}(int)"/>
    public readonly T[] PeekStructArray<T>(int count) where T : unmanaged =>
        _source.ReadStructArrayAt<T>(_position, count);

    /// <summary>Fills <paramref name="destination"/> with unmanaged structs of type <typeparamref name="T"/> and advances by <c>destination.Length * sizeof(T)</c> bytes. No endianness conversion is performed.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="PeekStructArray{T}(Span{T})"/>
    public void ReadStructArray<T>(Span<T> destination) where T : unmanaged
    {
        PeekStructArray(destination);
        _position = checked(_position + destination.Length * Unsafe.SizeOf<T>());
    }

    /// <summary>Fills <paramref name="destination"/> with unmanaged structs of type <typeparamref name="T"/> without advancing. No endianness conversion is performed.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="ReadStructArray{T}(Span{T})"/>
    public readonly void PeekStructArray<T>(Span<T> destination) where T : unmanaged =>
        _source.ReadStructArrayAt(_position, destination);

    /// <summary>Reads <paramref name="count"/> big-endian unmanaged structs of type <typeparamref name="T"/> and advances by <c>count * sizeof(T)</c> bytes, swapping multi-byte fields on little-endian hosts.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IEndianReversibleStruct{T}"/>.</typeparam>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read, with multi-byte fields in native order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="PeekBigEndianStructArray{T}(int)"/>
    /// <seealso cref="ReadStructArray{T}(int)"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    public T[] ReadBigEndianStructArray<T>(int count) where T : unmanaged, IEndianReversibleStruct<T>
    {
        var result = PeekBigEndianStructArray<T>(count);
        _position = checked(_position + result.Length * Unsafe.SizeOf<T>());
        return result;
    }

    /// <summary>Reads <paramref name="count"/> big-endian unmanaged structs of type <typeparamref name="T"/> without advancing, swapping multi-byte fields on little-endian hosts.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IEndianReversibleStruct{T}"/>.</typeparam>
    /// <param name="count">The number of elements. Must be non-negative.</param>
    /// <returns>A new array containing the values read, with multi-byte fields in native order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="ReadBigEndianStructArray{T}(int)"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    public readonly T[] PeekBigEndianStructArray<T>(int count) where T : unmanaged, IEndianReversibleStruct<T> =>
        _source.ReadEndianReversibleStructArrayAt<T>(_position, count);

    /// <summary>Fills <paramref name="destination"/> with big-endian unmanaged structs of type <typeparamref name="T"/> and advances by <c>destination.Length * sizeof(T)</c> bytes, swapping multi-byte fields on little-endian hosts.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IEndianReversibleStruct{T}"/>.</typeparam>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="PeekBigEndianStructArray{T}(Span{T})"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    public void ReadBigEndianStructArray<T>(Span<T> destination)
        where T : unmanaged, IEndianReversibleStruct<T>
    {
        PeekBigEndianStructArray(destination);
        _position = checked(_position + destination.Length * Unsafe.SizeOf<T>());
    }

    /// <summary>Fills <paramref name="destination"/> with big-endian unmanaged structs of type <typeparamref name="T"/> without advancing, swapping multi-byte fields on little-endian hosts.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IEndianReversibleStruct{T}"/>.</typeparam>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <seealso cref="ReadBigEndianStructArray{T}(Span{T})"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    public readonly void PeekBigEndianStructArray<T>(Span<T> destination)
        where T : unmanaged, IEndianReversibleStruct<T> =>
        _source.ReadEndianReversibleStructArrayAt(_position, destination);

    /// <summary>Reads <paramref name="count"/> 8-bit offsets and reads a struct of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array. No endianness conversion is performed on the structs.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset8ArrayPeekStruct{T}(int)"/>
    /// <seealso cref="ReadOffset8ArrayPeekBigEndianStruct{T}(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public T[] ReadOffset8ArrayPeekStruct<T>(int count) where T : unmanaged
    {
        var result = PeekOffset8ArrayPeekStruct<T>(count);
        _position = checked(_position + count);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 8-bit offsets without advancing and reads a struct of type <typeparamref name="T"/> at each offset. No endianness conversion is performed on the structs.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8ArrayPeekStruct{T}(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public readonly T[] PeekOffset8ArrayPeekStruct<T>(int count) where T : unmanaged =>
        _source.ReadOffset8ArrayPeekStructAt<T>(_position, count);

    /// <summary>Reads <paramref name="count"/> 16-bit offsets and reads a struct of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array. No endianness conversion is performed on the structs.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset16ArrayPeekStruct{T}(int)"/>
    /// <seealso cref="ReadOffset16ArrayPeekBigEndianStruct{T}(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public T[] ReadOffset16ArrayPeekStruct<T>(int count) where T : unmanaged
    {
        var result = PeekOffset16ArrayPeekStruct<T>(count);
        _position = checked(_position + (long)count * 2);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 16-bit offsets without advancing and reads a struct of type <typeparamref name="T"/> at each offset. No endianness conversion is performed on the structs.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset16ArrayPeekStruct{T}(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public readonly T[] PeekOffset16ArrayPeekStruct<T>(int count) where T : unmanaged =>
        _source.ReadOffset16ArrayPeekStructAt<T>(_position, count);

    /// <summary>Reads <paramref name="count"/> 24-bit offsets and reads a struct of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array. No endianness conversion is performed on the structs.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset24ArrayPeekStruct{T}(int)"/>
    /// <seealso cref="ReadOffset24ArrayPeekBigEndianStruct{T}(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public T[] ReadOffset24ArrayPeekStruct<T>(int count) where T : unmanaged
    {
        var result = PeekOffset24ArrayPeekStruct<T>(count);
        _position = checked(_position + (long)count * 3);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 24-bit offsets without advancing and reads a struct of type <typeparamref name="T"/> at each offset. No endianness conversion is performed on the structs.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset24ArrayPeekStruct{T}(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public readonly T[] PeekOffset24ArrayPeekStruct<T>(int count) where T : unmanaged =>
        _source.ReadOffset24ArrayPeekStructAt<T>(_position, count);

    /// <summary>Reads <paramref name="count"/> 32-bit offsets and reads a struct of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array. No endianness conversion is performed on the structs.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset32ArrayPeekStruct{T}(int)"/>
    /// <seealso cref="ReadOffset32ArrayPeekBigEndianStruct{T}(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public T[] ReadOffset32ArrayPeekStruct<T>(int count) where T : unmanaged
    {
        var result = PeekOffset32ArrayPeekStruct<T>(count);
        _position = checked(_position + (long)count * 4);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 32-bit offsets without advancing and reads a struct of type <typeparamref name="T"/> at each offset. No endianness conversion is performed on the structs.</summary>
    /// <typeparam name="T">The struct type. Must be blittable and unpadded.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset32ArrayPeekStruct{T}(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public readonly T[] PeekOffset32ArrayPeekStruct<T>(int count) where T : unmanaged =>
        _source.ReadOffset32ArrayPeekStructAt<T>(_position, count);

    /// <summary>Reads <paramref name="count"/> 8-bit offsets and reads a big-endian struct of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IEndianReversibleStruct{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read, with multi-byte fields in native order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset8ArrayPeekBigEndianStruct{T}(int)"/>
    /// <seealso cref="ReadOffset8ArrayPeekStruct{T}(int)"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public T[] ReadOffset8ArrayPeekBigEndianStruct<T>(int count)
        where T : unmanaged, IEndianReversibleStruct<T>
    {
        var result = PeekOffset8ArrayPeekBigEndianStruct<T>(count);
        _position = checked(_position + count);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 8-bit offsets without advancing and reads a big-endian struct of type <typeparamref name="T"/> at each offset.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IEndianReversibleStruct{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read, with multi-byte fields in native order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset8 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset8ArrayPeekBigEndianStruct{T}(int)"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset8</seealso>
    public readonly T[] PeekOffset8ArrayPeekBigEndianStruct<T>(int count)
        where T : unmanaged, IEndianReversibleStruct<T> =>
        _source.ReadOffset8ArrayPeekEndianReversibleStructAt<T>(_position, count);

    /// <summary>Reads <paramref name="count"/> 16-bit offsets and reads a big-endian struct of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IEndianReversibleStruct{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read, with multi-byte fields in native order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset16ArrayPeekBigEndianStruct{T}(int)"/>
    /// <seealso cref="ReadOffset16ArrayPeekStruct{T}(int)"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public T[] ReadOffset16ArrayPeekBigEndianStruct<T>(int count)
        where T : unmanaged, IEndianReversibleStruct<T>
    {
        var result = PeekOffset16ArrayPeekBigEndianStruct<T>(count);
        _position = checked(_position + (long)count * 2);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 16-bit offsets without advancing and reads a big-endian struct of type <typeparamref name="T"/> at each offset.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IEndianReversibleStruct{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read, with multi-byte fields in native order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset16 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset16ArrayPeekBigEndianStruct{T}(int)"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset16</seealso>
    public readonly T[] PeekOffset16ArrayPeekBigEndianStruct<T>(int count)
        where T : unmanaged, IEndianReversibleStruct<T> =>
        _source.ReadOffset16ArrayPeekEndianReversibleStructAt<T>(_position, count);

    /// <summary>Reads <paramref name="count"/> 24-bit offsets and reads a big-endian struct of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IEndianReversibleStruct{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read, with multi-byte fields in native order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset24ArrayPeekBigEndianStruct{T}(int)"/>
    /// <seealso cref="ReadOffset24ArrayPeekStruct{T}(int)"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public T[] ReadOffset24ArrayPeekBigEndianStruct<T>(int count)
        where T : unmanaged, IEndianReversibleStruct<T>
    {
        var result = PeekOffset24ArrayPeekBigEndianStruct<T>(count);
        _position = checked(_position + (long)count * 3);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 24-bit offsets without advancing and reads a big-endian struct of type <typeparamref name="T"/> at each offset.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IEndianReversibleStruct{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read, with multi-byte fields in native order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset24 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset24ArrayPeekBigEndianStruct{T}(int)"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset24</seealso>
    public readonly T[] PeekOffset24ArrayPeekBigEndianStruct<T>(int count)
        where T : unmanaged, IEndianReversibleStruct<T> =>
        _source.ReadOffset24ArrayPeekEndianReversibleStructAt<T>(_position, count);

    /// <summary>Reads <paramref name="count"/> 32-bit offsets and reads a big-endian struct of type <typeparamref name="T"/> at each offset (relative to the source origin). Advances the position past the offset array.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IEndianReversibleStruct{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read, with multi-byte fields in native order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="PeekOffset32ArrayPeekBigEndianStruct{T}(int)"/>
    /// <seealso cref="ReadOffset32ArrayPeekStruct{T}(int)"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public T[] ReadOffset32ArrayPeekBigEndianStruct<T>(int count)
        where T : unmanaged, IEndianReversibleStruct<T>
    {
        var result = PeekOffset32ArrayPeekBigEndianStruct<T>(count);
        _position = checked(_position + (long)count * 4);
        return result;
    }

    /// <summary>Reads <paramref name="count"/> 32-bit offsets without advancing and reads a big-endian struct of type <typeparamref name="T"/> at each offset.</summary>
    /// <typeparam name="T">The struct type. Must implement <see cref="IEndianReversibleStruct{T}"/>.</typeparam>
    /// <param name="count">The number of offsets. Must be non-negative.</param>
    /// <returns>A new array containing the values read, with multi-byte fields in native order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The read extends past <see cref="Length"/>.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Offset32 data type</see> in the OpenType specification.</remarks>
    /// <seealso cref="ReadOffset32ArrayPeekBigEndianStruct{T}(int)"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Offset32</seealso>
    public readonly T[] PeekOffset32ArrayPeekBigEndianStruct<T>(int count)
        where T : unmanaged, IEndianReversibleStruct<T> =>
        _source.ReadOffset32ArrayPeekEndianReversibleStructAt<T>(_position, count);

    /// <summary>Sets <see cref="Position"/> relative to the given <see cref="SeekOrigin"/>.</summary>
    /// <param name="offset">The byte offset relative to <paramref name="origin"/>.</param>
    /// <param name="origin"><see cref="SeekOrigin.Begin"/> anchors to offset 0 of the source; <see cref="SeekOrigin.Current"/> to <see cref="Position"/>; <see cref="SeekOrigin.End"/> to <see cref="Length"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="origin"/> is invalid or the result is negative.</exception>
    /// <remarks>The same <see cref="SeekOrigin"/> semantics used by <see cref="System.IO.Stream.Seek"/> apply here.</remarks>
    /// <seealso cref="Position"/>
    /// <seealso cref="Length"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.io.seekorigin">.NET API: <c>SeekOrigin</c></seealso>
    public void Seek(long offset, SeekOrigin origin = SeekOrigin.Begin)
    {
        long position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => checked(_position + offset),
            SeekOrigin.End => checked(_source.Length + offset),
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
        Position = position;
    }

    /// <summary>Creates a cursor at an absolute source offset. Does not affect this cursor.</summary>
    /// <param name="sourceOffset">The absolute offset within <see cref="Source"/>.</param>
    /// <returns>A new independent cursor positioned at <paramref name="sourceOffset"/>.</returns>
    /// <seealso cref="Here"/>
    /// <seealso cref="At"/>
    public readonly Cursor At(long sourceOffset) => new(_source, sourceOffset);

    /// <summary>Creates a cursor at the current position. Does not affect this cursor.</summary>
    /// <returns>A new independent cursor at the same position as this one.</returns>
    /// <seealso cref="At"/>
    public readonly Cursor Here() => new(_source, _position);
}
