namespace Mubarrat.Fonts.OpenType.Binary;

/// <summary>A <see cref="Source"/> backed by an in-memory buffer. The buffer is retained for the lifetime of the source; ownership remains with the caller.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The buffer is not copied. Mutations to the underlying array (if it is exposed elsewhere) are visible through this source.</description></item>
/// <item><description><see cref="Dispose"/> is a no-op: the caller retains ownership of the buffer and is responsible for any cleanup.</description></item>
/// <item><description>Reads are the fastest of the built-in <see cref="Source"/> implementations because they reduce to a <see cref="System.Span{T}.CopyTo"/> over the backing memory; <see cref="TryGetSpanAtCore"/> additionally lets scalar readers bypass the copy entirely.</description></item>
/// <item><description>Offsets and lengths are internally narrowed to <see cref="int"/>. Buffers larger than <see cref="int.MaxValue"/> bytes are not supported and will throw <see cref="OverflowException"/> on read.</description></item>
/// </list>
/// <para>Use this source when the entire font file is already resident in memory; for files or streams, see <see cref="FileSource"/>.</para>
/// </remarks>
/// <seealso cref="Source"/>
/// <seealso cref="FileSource"/>
/// <seealso cref="SliceSource"/>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1">.NET API: <c>ReadOnlyMemory&lt;T&gt;</c></seealso>
public sealed class MemorySource : Source
{
    private readonly ReadOnlyMemory<byte> _memory;

    /// <summary>Creates a source over <paramref name="memory"/>.</summary>
    /// <param name="memory">The backing buffer. Not copied; the caller retains ownership.</param>
    /// <remarks>The length of the source is fixed at construction to <c>memory.Length</c>. If the underlying buffer is a segment of a larger array, only that segment is reachable.</remarks>
    /// <seealso cref="Length"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1">.NET API: <c>ReadOnlyMemory&lt;T&gt;</c></seealso>
    public MemorySource(ReadOnlyMemory<byte> memory) => _memory = memory;

    /// <inheritdoc/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1.length">.NET API: <c>ReadOnlyMemory&lt;T&gt;.Length</c></seealso>
    public override long Length => _memory.Length;

    /// <inheritdoc/>
    /// <exception cref="OverflowException"><paramref name="offset"/> or the read range exceeds <see cref="int.MaxValue"/>.</exception>
    /// <remarks>The read is bounds-checked against <see cref="Length"/> by <see cref="Source.ReadAt(long, System.Span{byte})"/> before this method is called; the cast to <see cref="int"/> here is safe only for buffers that fit in 32-bit address space.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.span-1.copytosystem-span((-0">.NET API: <c>Span&lt;T&gt;.CopyTo</c></seealso>
    protected override void ReadAtCore(long offset, Span<byte> destination) =>
        _memory.Span.Slice(checked((int)offset), destination.Length).CopyTo(destination);

    /// <inheritdoc/>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Returns <see langword="true"/> with a zero-copy view into the backing buffer for every in-range request.</description></item>
    /// <item><description>Returns <see langword="false"/> for out-of-range requests; the caller falls back to <see cref="ReadAtCore"/>.</description></item>
    /// <item><description>The returned span aliases the backing buffer. Do not write to it; the type system forbids writes because the return type is <see cref="System.ReadOnlySpan{T}"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="OverflowException"><paramref name="offset"/> or <paramref name="length"/> exceeds <see cref="int.MaxValue"/>.</exception>
    /// <seealso cref="Source.TryGetSpanAt(long, int, out System.ReadOnlySpan{byte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.readonlymemory-1.span">.NET API: <c>ReadOnlyMemory&lt;T&gt;.Span</c></seealso>
    protected override bool TryGetSpanAtCore(long offset, int length, out ReadOnlySpan<byte> span)
    {
        if (offset < 0 || length < 0 || offset + length > Length)
        {
            span = default;
            return false;
        }
        span = _memory.Span.Slice(checked((int)offset), length);
        return true;
    }

    /// <inheritdoc/>
    /// <remarks>This override is a no-op. The buffer passed to the constructor remains owned by the caller and must be released by the caller when appropriate.</remarks>
    /// <seealso cref="Source.Dispose"/>
    /// <seealso cref="FileSource.Dispose"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.idisposable">.NET API: <c>IDisposable</c></seealso>
    public override void Dispose() { /* buffer owned by caller */ }
}
