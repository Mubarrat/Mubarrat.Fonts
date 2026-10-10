namespace Mubarrat.Fonts.Binary;

/// <summary>A <see cref="Source"/> that reads from a <see cref="Stream"/> on demand and grows a long-term in-memory buffer as bytes are consumed.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Unlike a memory source, this type does not require the whole stream to be materialized before parsing begins.</description></item>
/// <item><description>The first read pulls only as many bytes as needed; subsequent reads within the buffered region are served from memory; reads past it pull the stream forward and extend the buffer.</description></item>
/// <item><description>Once a byte has been read, it stays buffered for the lifetime of the source.</description></item>
/// <item><description>The stream is assumed to be positioned at the first byte the source will ever read, and is not touched by any other code while the source is alive.</description></item>
/// <item><description>For seekable streams the source uses <see cref="Stream.Length"/>; for non-seekable streams the caller must supply the total length.</description></item>
/// <item><description>The buffer is a single <see cref="byte"/> array, capping the supported stream length at <see cref="int.MaxValue"/> bytes.</description></item>
/// <item><description>Reads are safe for concurrent access at different offsets; the buffer is grown under a lock while already-buffered reads proceed lock-free via a volatile state snapshot.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Source"/>
/// <seealso cref="MemorySource"/>
/// <seealso cref="FileSource"/>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.io.stream">.NET API: <c>Stream</c></seealso>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "ApiDesign",
    "RS0026:Do not add multiple public overloads with optional parameters",
    Scope = "type",
    Target = "~T:Mubarrat.Fonts.OpenType.Binary.StreamMemorySource",
    Justification = "The two constructors are disambiguated by the type of their second parameter (bool ownsStream vs long length); the trailing optional 'ownsStream' cannot introduce ambiguity between them.")]
public sealed class StreamMemorySource : Source
{
    private const int MinBufferSize = 256;

    private readonly Stream _stream;
    private readonly long _length;
    private readonly bool _ownsStream;
    private readonly Lock _lock = new();

    private volatile State _state;

    /// <summary>A snapshot of the source's current buffer and length. Internal to <see cref="StreamMemorySource"/>.</summary>
    /// <param name="buffer">The buffer containing the bytes read so far.</param>
    /// <param name="length">The number of bytes read so far. The view of <paramref name="buffer"/> is limited to this length.</param>
    /// <remarks>Instances are immutable; <see cref="_state"/> is replaced under the lock when the buffer grows. The volatile field guarantees a reader always sees a fully-published snapshot.</remarks>
    private sealed class State(byte[] buffer, int length)
    {
        /// <summary>The buffer containing the bytes read so far. Only the first <see cref="Length"/> bytes are valid.</summary>
        public readonly byte[] Buffer = buffer;

        /// <summary>The number of bytes read so far. The valid view of <see cref="Buffer"/> is <c>[0, Length)</c>.</summary>
        public readonly int Length = length;
    }

    /// <summary>Creates a source over a seekable stream. The stream's length is used as the source length.</summary>
    /// <param name="stream">The stream. Must be seekable.</param>
    /// <param name="ownsStream">When <c>true</c>, <see cref="Dispose"/> disposes the stream. The caller otherwise retains ownership.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not seekable.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The stream is longer than <see cref="int.MaxValue"/> bytes.</exception>
    /// <remarks>The stream must be positioned at the first byte this source will read; the constructor does not seek.</remarks>
    /// <seealso cref="StreamMemorySource(Stream, long, bool)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.canseek">.NET API: <c>Stream.CanSeek</c></seealso>
    public StreamMemorySource(Stream stream, bool ownsStream = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek)
            throw new ArgumentException(
                "A non-seekable stream requires an explicit length.", nameof(stream));

        _stream = stream;
        _length = stream.Length;
        _ownsStream = ownsStream;

        if (_length > int.MaxValue)
            throw new ArgumentOutOfRangeException(
                nameof(stream), _length,
                $"Stream length {_length} exceeds the maximum buffer size of {int.MaxValue} bytes.");

        _state = new State(
            new byte[Math.Min(MinBufferSize, (int)_length)], 0);
    }

    /// <summary>Creates a source over a stream of known total length.</summary>
    /// <param name="stream">The stream.</param>
    /// <param name="length">The total number of bytes that will be read.</param>
    /// <param name="ownsStream">When <c>true</c>, <see cref="Dispose"/> disposes the stream. The caller otherwise retains ownership.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative or exceeds <see cref="int.MaxValue"/>.</exception>
    /// <remarks>Use this overload for non-seekable streams, or when the caller knows the length without consulting <see cref="Stream.Length"/>.</remarks>
    /// <seealso cref="StreamMemorySource(Stream, bool)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.length">.NET API: <c>Stream.Length</c></seealso>
    public StreamMemorySource(Stream stream, long length, bool ownsStream = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        if (length > int.MaxValue)
            throw new ArgumentOutOfRangeException(
                nameof(length), length,
                $"Length {length} exceeds the maximum buffer size of {int.MaxValue} bytes.");

        _stream = stream;
        _length = length;
        _ownsStream = ownsStream;
        _state = new State(
            new byte[Math.Min(MinBufferSize, (int)_length)], 0);
    }

    /// <inheritdoc/>
    /// <value>The value captured at construction, not the current stream length. If the stream shrinks or grows after construction, this value does not change.</value>
    /// <seealso cref="StreamMemorySource(Stream, bool)"/>
    /// <seealso cref="StreamMemorySource(Stream, long, bool)"/>
    public override long Length => _length;

    /// <summary>Gets the bytes read so far as a <see cref="ReadOnlyMemory{T}"/>. The view covers only the portion of the stream that has been requested; it does not extend to bytes not yet read and does not grow with the buffer.</summary>
    /// <value>A view over <c>[0, currentBufferedLength)</c> of the internal buffer, or an empty memory when no bytes have been read yet.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The returned memory aliases the internal buffer, which may be replaced the next time the buffer grows. Capture the memory only for the duration of a single operation.</description></item>
    /// <item><description>Reading this property does not extend the buffer; it does not cause a stream read.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Source.Length"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1">.NET API: <c>ReadOnlyMemory&lt;T&gt;</c></seealso>
    public ReadOnlyMemory<byte> BufferedMemory
    {
        get
        {
            State s = _state;
            return s.Buffer.AsMemory(0, s.Length);
        }
    }

    // ───────────────────────── Reads ─────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The fast path serves already-buffered reads without taking the lock; the volatile <see cref="_state"/> read publishes the buffer reference and length atomically.</description></item>
    /// <item><description>The slow path takes <see cref="_lock"/>, re-reads the state (another thread may have extended it in the interim), extends if still necessary, and then copies from the current buffer.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="TryGetSpanAtCore(long, int, out System.ReadOnlySpan{byte})"/>
    protected override void ReadAtCore(long offset, Span<byte> destination)
    {
        if (destination.IsEmpty) return;

        long end = offset + destination.Length;

        // Fast path: fully buffered.
        State s = _state;
        if (end <= s.Length)
        {
            s.Buffer.AsSpan((int)offset, destination.Length).CopyTo(destination);
            return;
        }

        // Slow path: extend under lock, then serve.
        lock (_lock)
        {
            s = _state;
            if (end > s.Length)
                s = ExtendLocked((int)end);
            s.Buffer.AsSpan((int)offset, destination.Length).CopyTo(destination);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Only hands out a span when the requested range is already buffered; extending here would require the lock, and the caller's fallback path (<c>stackalloc</c> + <see cref="ReadAtCore"/> already covers the miss case.</description></item>
    /// <item><description>The returned span aliases the internal buffer. If the buffer is later replaced by <see cref="ExtendLocked"/>, the span continues to reference the old array — safe to read, but not current with subsequent writes.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ReadAtCore(long, System.Span{byte})"/>
    protected override bool TryGetSpanAtCore(long offset, int length, out ReadOnlySpan<byte> span)
    {
        // Only hand out a span when the requested range is already buffered. Extending
        // here would require the lock, and the caller's fallback path (stackalloc +
        // ReadAt) already covers the miss case.
        State s = _state;
        if (offset + length <= s.Length)
        {
            span = s.Buffer.AsSpan((int)offset, length);
            return true;
        }
        span = default;
        return false;
    }

    // ───────────────────────── Extension ─────────────────────────

    /// <summary>Extends the buffer under the lock.</summary>
    /// <param name="required">The minimum number of bytes that must be available in the buffer.</param>
    /// <returns>The new state of the buffer.</returns>
    /// <exception cref="EndOfStreamException"><paramref name="required"/> exceeds <see cref="Length"/>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The caller must hold <see cref="_lock"/> for the duration of this call; the method itself does not lock.</description></item>
    /// <item><description>The returned <see cref="State"/> is also stored in <see cref="_state"/>, so subsequent lock-free readers observe the extension.</description></item>
    /// <item><description>When the target length exceeds the buffer's capacity, a new array is allocated and the existing bytes are copied. The old array remains valid for any reader that already captured a reference to it.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ComputeTargetLength(State, int)"/>
    /// <seealso cref="ReadExactly(System.Span{byte})"/>
    private State ExtendLocked(int required)
    {
        State current = _state;
        if (required <= current.Length) return current;

        if (required > _length)
            throw new EndOfStreamException(
                $"Read extending to byte {required} exceeds source length {_length}.");

        int targetLength = ComputeTargetLength(current, required);

        byte[] buffer = current.Buffer;
        if (targetLength > buffer.Length)
        {
            byte[] grown = new byte[targetLength];
            buffer.AsSpan(0, current.Length).CopyTo(grown);
            buffer = grown;
        }

        int toRead = targetLength - current.Length;
        if (toRead > 0)
            ReadExactly(buffer.AsSpan(current.Length, toRead));

        var next = new State(buffer, targetLength);
        _state = next;
        return next;
    }

    /// <summary>Computes the target length for the buffer based on the current state and required length.</summary>
    /// <param name="current">The current state of the buffer.</param>
    /// <param name="required">The minimum number of bytes that must be available in the buffer.</param>
    /// <returns>The computed target length, never less than <paramref name="required"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>If <paramref name="required"/> fits within the existing capacity, the current capacity is returned so the caller fills spare space rather than growing.</description></item>
    /// <item><description>Otherwise the target is <c>max(2 × capacity, required)</c>, clamped to <see cref="_length"/> and to <see cref="int.MaxValue"/>.</description></item>
    /// <item><description>The doubling strategy keeps the number of reallocations logarithmic in the number of extensions; the clamp to <see cref="_length"/> avoids over-allocating for short streams.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ExtendLocked(int)"/>
    private int ComputeTargetLength(State current, int required)
    {
        // Spare capacity already exists: fill it.
        if (required <= current.Buffer.Length)
            return current.Buffer.Length;

        // Otherwise grow by doubling from the current capacity, rounded up to `required`.
        long doubled = (long)current.Buffer.Length * 2;
        long target = Math.Max(doubled, required);
        if (target > _length) target = _length;
        if (target > int.MaxValue) target = int.MaxValue;
        return (int)target;
    }

    /// <summary>Reads exactly the requested number of bytes from the stream into the destination span.</summary>
    /// <param name="destination">The span into which the bytes will be read.</param>
    /// <exception cref="EndOfStreamException">The stream ends before all bytes can be read.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><see cref="Stream.Read(System.Span{byte})"/> may return fewer bytes than requested; the loop retries until <paramref name="destination"/> is empty.</description></item>
    /// <item><description>The caller must hold <see cref="_lock"/>; the method advances the stream position, which is shared state.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Stream.Read(System.Span{byte})"/>
    private void ReadExactly(Span<byte> destination)
    {
        while (!destination.IsEmpty)
        {
            int n = _stream.Read(destination);
            if (n == 0)
                throw new EndOfStreamException(
                    "Stream ended before the requested bytes could be read.");
            destination = destination[n..];
        }
    }

    // ───────────────────────── Lifetime ─────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>When <c>ownsStream</c> was <c>true</c>, disposes the underlying stream; otherwise the stream remains open and owned by the caller.</description></item>
    /// <item><description>Disposal takes <see cref="_lock"/> so that an in-flight extension completes before the stream is closed.</description></item>
    /// <item><description>After this call returns, any further read throws <see cref="ObjectDisposedException"/> from the underlying stream.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Source.Dispose"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.idisposable">.NET API: <c>IDisposable</c></seealso>
    public override void Dispose()
    {
        lock (_lock)
        {
            if (_ownsStream)
                _stream.Dispose();
        }
    }
}
