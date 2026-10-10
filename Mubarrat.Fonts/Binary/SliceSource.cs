namespace Mubarrat.Fonts.Binary;

/// <summary>A <see cref="Source"/> view that exposes a slice of another source under its own coordinate system: reading offset <c>O</c> reads offset <c>O + <see cref="Shift"/></c> from <see cref="Outer"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The two-argument constructor creates a slice from <see cref="Shift"/> to the end of <see cref="Outer"/>.</description></item>
/// <item><description>The three-argument constructor creates a slice over an explicit region.</description></item>
/// <item><description>The four-argument constructor additionally controls whether the slice is <see cref="IsIsolated"/>.</description></item>
/// </list>
/// <para>Slices collapse on composition except when the outer is non-isolated and the inner is isolated. The four cases:</para>
/// <list type="bullet">
/// <item><description>Non-isolated over non-isolated: collapse.</description></item>
/// <item><description>Isolated over isolated: collapse.</description></item>
/// <item><description>Isolated over non-isolated: collapse.</description></item>
/// <item><description>Non-isolated over isolated: do not collapse — an isolated inner is a hard boundary that a non-isolated outer is not permitted to escape.</description></item>
/// </list>
/// <para>The constructor maintains a depth invariant: no chain exceeds two slices, and the only reachable two-slice chain is <c>S over I</c> where <c>I</c> is isolated and its <see cref="Outer"/> is not a <see cref="SliceSource"/>. A chain such as <c>I over S over I</c> collapses fully to a single slice over the raw source.</para>
/// <para><see cref="IsIsolated"/> controls whether this slice enforces its own <c>[0, Length)</c> bound. Non-isolated (the default): the slice shifts and delegates, and the read succeeds whenever the resolved absolute offset is valid in <see cref="Outer"/>. Isolated: offsets below zero and reads past <see cref="Length"/> throw from this slice before <see cref="Outer"/> is consulted.</para>
/// <para><see cref="Dispose"/> does not dispose <see cref="Outer"/>; the outer source may be shared.</para>
/// <para>Table-internal offsets in OpenType are relative to the table's start, not the file start, which is what makes slicing the natural way to model a table read; see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification, Organization of an OpenType Font</see>.</para>
/// </remarks>
/// <seealso cref="Source"/>
/// <seealso cref="MemorySource"/>
/// <seealso cref="FileSource"/>
/// <seealso cref="Source.WithOffset(long)"/>
/// <seealso cref="Source.WithOffsetAndLength(long, long, bool)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType Font</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Data Types</seealso>
public sealed class SliceSource : Source
{
    /// <summary>Creates a slice from <paramref name="shift"/> to the end of <paramref name="outer"/>.</summary>
    /// <param name="outer">The source this slice is a slice of.</param>
    /// <param name="shift">The signed offset added to every read offset.</param>
    /// <exception cref="ArgumentNullException"><paramref name="outer"/> is <c>null</c>.</exception>
    /// <remarks>The slice's <see cref="Length"/> is computed by <see cref="Available(Source, long)"/> and is always non-negative, even when <paramref name="shift"/> places the start beyond the end of <paramref name="outer"/>.</remarks>
    /// <seealso cref="SliceSource(Source, long, long)"/>
    /// <seealso cref="SliceSource(Source, long, long, bool)"/>
    /// <seealso cref="Available(Source, long)"/>
    /// <seealso cref="Source.WithOffset(long)"/>
    public SliceSource(Source outer, long shift)
        : this(outer, shift, Available(outer, shift), isolated: false)
    {
    }

    /// <summary>Creates a slice over an explicit region of <paramref name="outer"/>.</summary>
    /// <param name="outer">The source this slice is a slice of.</param>
    /// <param name="shift">The signed offset added to every read offset.</param>
    /// <param name="length">The number of bytes reachable from offset 0.</param>
    /// <exception cref="ArgumentNullException"><paramref name="outer"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
    /// <remarks>This constructor is equivalent to the four-argument form with <c>isolated</c> set to <c>false</c>.</remarks>
    /// <seealso cref="SliceSource(Source, long)"/>
    /// <seealso cref="SliceSource(Source, long, long, bool)"/>
    /// <seealso cref="Source.WithOffsetAndLength(long, long, bool)"/>
    public SliceSource(Source outer, long shift, long length)
        : this(outer, shift, length, isolated: false)
    {
    }

    /// <summary>Creates a slice over an explicit region of <paramref name="outer"/>, optionally isolating it from the outer source's coordinate system.</summary>
    /// <param name="outer">The source this slice is a slice of.</param>
    /// <param name="shift">The signed offset added to every read offset.</param>
    /// <param name="length">The number of bytes reachable from offset 0.</param>
    /// <param name="isolated">When <c>true</c>, offsets below zero and reads past <paramref name="length"/> throw from this slice without consulting <paramref name="outer"/>. When <c>false</c>, the slice shifts and delegates; the read succeeds whenever the resolved absolute offset is valid in <paramref name="outer"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="outer"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
    /// <exception cref="OverflowException"><paramref name="shift"/> combined with an existing slice's shift exceeds <see cref="long"/> range.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>When <paramref name="outer"/> is not a <see cref="SliceSource"/>, the constructor takes a fast path and stores the arguments unchanged.</description></item>
    /// <item><description>When <paramref name="outer"/> is a <see cref="SliceSource"/>, the constructor collapses the two slices unless the pair is <em>non-isolated over isolated</em>. See the type-level remarks for the collapse rules.</description></item>
    /// <item><description>The collapse never exceeds depth 2; under the invariant, a level-2 collapse only occurs when the intermediate is an isolated slice over a raw source.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SliceSource(Source, long)"/>
    /// <seealso cref="SliceSource(Source, long, long)"/>
    /// <seealso cref="IsIsolated"/>
    /// <seealso cref="Source.WithOffsetAndLength(long, long, bool)"/>
    public SliceSource(Source outer, long shift, long length, bool isolated)
    {
        ArgumentNullException.ThrowIfNull(outer);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        // Fast path: the outer is not a slice, so nothing collapses. Covers table loads
        // and subtable offsets against a raw source, the common case.
        if (outer is not SliceSource existing)
        {
            Outer = outer;
            Shift = shift;
            Length = length;
            IsIsolated = isolated;
            return;
        }

        // The one pair that never collapses: a non-isolated outer over an isolated
        // inner. Checked before any arithmetic so this case stays branch-only.
        if (!isolated && existing.IsIsolated)
        {
            Outer = existing;
            Shift = shift;
            Length = length;
            IsIsolated = false;
            return;
        }

        // Level 1 collapse. Unconditional here: either the outer is isolated or the
        // inner is not, and both satisfy the collapse rule.
        Source collapsedOuter = existing.Outer;
        long collapsedShift = checked(existing.Shift + shift);
        long collapsedLength = Math.Min(length, RemainingAfter(existing.Length, shift));

        // Level 2 collapse. Reached only when the first collapse exposed another slice,
        // which under the depth-2 invariant is an isolated slice over a raw source. The
        // same isolation rule applies: a non-isolated outer cannot absorb an isolated
        // inner.
        if (collapsedOuter is SliceSource existing2 && (isolated || !existing2.IsIsolated))
        {
            collapsedLength = Math.Min(collapsedLength, RemainingAfter(existing2.Length, collapsedShift));
            collapsedShift = checked(existing2.Shift + collapsedShift);
            collapsedOuter = existing2.Outer;
        }

        Outer = collapsedOuter;
        Shift = collapsedShift;
        Length = collapsedLength;
        IsIsolated = isolated;
    }

    /// <summary>Gets the source this slice is a slice of.</summary>
    /// <value>The outermost non-slice source after any collapse performed at construction. Never a <see cref="SliceSource"/> after a collapse.</value>
    /// <remarks>The instance does not own <see cref="Outer"/>; disposing this slice does not dispose it.</remarks>
    /// <seealso cref="Shift"/>
    /// <seealso cref="Length"/>
    /// <seealso cref="IsIsolated"/>
    public Source Outer { get; }

    /// <summary>Gets the signed offset added to every read offset.</summary>
    /// <value>A signed byte offset, relative to the origin of <see cref="Outer"/>.</value>
    /// <seealso cref="Outer"/>
    /// <seealso cref="Length"/>
    public long Shift { get; }

    /// <summary>Gets the number of bytes in the slice's coordinate system. Never negative.</summary>
    /// <value>The declared slice length, as supplied to the constructor after any clamping performed by <see cref="Available(Source, long)"/> or <see cref="RemainingAfter(long, long)"/>.</value>
    /// <remarks>The value is the declared slice length. For a non-isolated slice it is informational — reads past it are permitted and resolve against <see cref="Outer"/>. For an isolated slice it is a hard upper bound and reads past it throw.</remarks>
    /// <seealso cref="IsIsolated"/>
    /// <seealso cref="Shift"/>
    /// <seealso cref="Outer"/>
    public override long Length { get; }

    /// <summary>Gets a value indicating whether this slice enforces its own bound.</summary>
    /// <value><see langword="true"/> when this slice was constructed with <c>isolated: true</c> and no collapse converted it to non-isolated; <see langword="false"/> otherwise.</value>
    /// <remarks>When <c>true</c>, reads at offsets below zero throw <see cref="ArgumentOutOfRangeException"/> and reads past <see cref="Length"/> throw <see cref="EndOfStreamException"/>, both from this slice without consulting <see cref="Outer"/>. When <c>false</c>, the slice shifts and delegates; the read succeeds whenever the resolved absolute offset is valid in <see cref="Outer"/>.</remarks>
    /// <seealso cref="Length"/>
    /// <seealso cref="Outer"/>
    /// <seealso cref="ReadAt(long, System.Span{byte})"/>
    public bool IsIsolated { get; }

    /// <inheritdoc/>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="IsIsolated"/> is <c>true</c> and <paramref name="offset"/> is negative, or the offset combined with <see cref="Shift"/> resolves to a negative absolute offset regardless of isolation.</exception>
    /// <exception cref="EndOfStreamException"><see cref="IsIsolated"/> is <c>true</c> and the read extends past <see cref="Length"/>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>For an isolated slice, the bound check uses a reorganized upper-bound comparison so <c>offset + destination.Length</c> never runs before the comparison — this avoids overflow on very large offsets.</description></item>
    /// <item><description>For a non-isolated slice, the only bound check is the negative-absolute check in <see cref="ReadAtCore"/>; the actual read is delegated to <see cref="Outer"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ReadAtCore(long, System.Span{byte})"/>
    /// <seealso cref="IsIsolated"/>
    /// <seealso cref="Length"/>
    /// <seealso cref="Source.ReadAt(long, System.Span{byte})"/>
    public override void ReadAt(long offset, scoped Span<byte> destination)
    {
        if (destination.IsEmpty) return;

        if (IsIsolated)
        {
            // Isolated: this slice's [0, Length) is the entire coordinate system.
            // Negative offsets and reads past Length throw here, before Outer is
            // consulted. The upper-bound check is reorganized so
            // offset + destination.Length never runs before the comparison.
            ArgumentOutOfRangeException.ThrowIfNegative(offset);

            if (destination.Length > Length || offset > Length - destination.Length)
                throw new EndOfStreamException(
                    $"Read of {destination.Length} bytes at offset {offset} exceeds length {Length}.");
        }

        ReadAtCore(offset, destination);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> combined with <see cref="Shift"/> resolves to a negative absolute offset.</exception>
    /// <remarks>The read is delegated to <see cref="Outer"/> at the resolved absolute offset. No upper-bound check is performed here; the outer source enforces its own <see cref="Source.Length"/>.</remarks>
    /// <seealso cref="ReadAt(long, System.Span{byte})"/>
    /// <seealso cref="Shift"/>
    /// <seealso cref="Outer"/>
    protected override void ReadAtCore(long offset, Span<byte> destination)
    {
        long absolute = checked(offset + Shift);

        if (absolute < 0)
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                $"Offset {offset} with shift {Shift} resolves to absolute {absolute}, which is negative.");

        Outer.ReadAt(absolute, destination);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The zero-copy path is only available if <see cref="Outer"/> itself supports it; this method forwards the request and returns whatever the outer returns.</description></item>
    /// <item><description>Returns <see langword="false"/> when <paramref name="offset"/> combined with <see cref="Shift"/> is negative or when <paramref name="length"/> is negative.</description></item>
    /// <item><description>The returned span, when non-empty, aliases <see cref="Outer"/>'s backing storage. It must not be written to.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Source.TryGetSpanAt(long, int, out System.ReadOnlySpan{byte})"/>
    /// <seealso cref="Outer"/>
    /// <seealso cref="Shift"/>
    protected override bool TryGetSpanAtCore(long offset, int length, out ReadOnlySpan<byte> span)
    {
        long absolute = checked(offset + Shift);

        if (absolute < 0 || length < 0)
        {
            span = default;
            return false;
        }

        return Outer.TryGetSpanAt(absolute, length, out span);
    }

    /// <summary>Does not dispose the outer source. Dispose the underlying source directly if it is not shared.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>This override is intentionally a no-op. The <see cref="Outer"/> source is shared and its lifetime is not owned by this instance.</description></item>
    /// <item><description>Dispose the root <see cref="Source"/> (typically a <see cref="FileSource"/>) directly when parsing is complete; slices over it become unusable at that point.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Source.Dispose"/>
    /// <seealso cref="Outer"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.idisposable">.NET API: <c>IDisposable</c></seealso>
    public override void Dispose()
    {
        // Intentionally does not dispose _outer.
    }

    /// <summary>Computes the reachable length from <paramref name="shift"/> to the end of <paramref name="outer"/>, clamped to zero. Used by the two-argument constructor so the open-ended slice still reports a non-negative <see cref="Length"/>.</summary>
    /// <param name="outer">The source whose length is measured.</param>
    /// <param name="shift">The signed offset to subtract from <paramref name="outer"/>'s length.</param>
    /// <returns><c>outer.Length - shift</c> when positive; otherwise <c>0</c>.</returns>
    /// <exception cref="NullReferenceException"><paramref name="outer"/> is <c>null</c>.</exception>
    /// <remarks>The result is used only to populate <see cref="Length"/>; it does not affect the reachable region of a non-isolated slice, which can still read past <see cref="Length"/>.</remarks>
    /// <seealso cref="SliceSource(Source, long)"/>
    /// <seealso cref="RemainingAfter(long, long)"/>
    /// <seealso cref="Source.WithOffset(long)"/>
    public static long Available(Source outer, long shift)
    {
        long available = outer.Length - shift;
        return available > 0 ? available : 0;
    }

    /// <summary>Computes how many bytes remain in an existing slice when a new slice begins <paramref name="shift"/> bytes into it. Saturates at <see cref="long.MaxValue"/> when the subtraction would overflow, and clamps to zero when the shift lands past the existing slice's end.</summary>
    /// <param name="existingLength">The length of the slice being narrowed.</param>
    /// <param name="shift">The signed offset into the existing slice.</param>
    /// <returns>The remaining bytes; never negative; saturates at <see cref="long.MaxValue"/> on overflow.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>existingLength - shift</c> overflows only when <paramref name="shift"/> is negative and its magnitude is large enough that the sum wraps past <see cref="long.MaxValue"/>.</description></item>
    /// <item><description>Saturation is the correct behavior there: the caller's own length argument bounds the result either way.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Available(Source, long)"/>
    /// <seealso cref="SliceSource(Source, long, long, bool)"/>
    /// <seealso cref="Source.WithOffsetAndLength(long, long, bool)"/>
    public static long RemainingAfter(long existingLength, long shift)
    {
        // existingLength - shift overflows only when shift is negative and |shift| is
        // large enough that the sum wraps. Saturating is correct there; the caller's
        // length argument bounds the result either way.
        long remaining = shift < 0 && existingLength > long.MaxValue + shift
            ? long.MaxValue
            : existingLength - shift;

        return remaining > 0 ? remaining : 0;
    }
}
