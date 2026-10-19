using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>trak</c> table: per-size, per-track inter-glyph spacing adjustments for AAT fonts. For a requested track number and point size, the table yields an FUnits delta applied to every glyph's advance width.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><b>Tracking is global, kerning is local.</b> A single FUnit value applies to every glyph at a given <c>(track, point size)</c>; the identity of the glyph never affects the result. Kerning (e.g. <c>kerx</c>) is the opposite: the pair of neighboring glyphs selects the adjustment.</description></item>
/// <item><description><b>Two orientations, one table.</b> Horizontal and vertical track data are stored separately and reached by independent header offsets. Either may be absent (offset zero); a conforming font should carry at least one.</description></item>
/// <item><description><b>Interpolation and extrapolation are part of the semantic model.</b> Both the track axis and the size axis are interpolated from the values stored in the table. Requests below the lowest stored size, or above the highest, are extrapolated linearly from the two nearest entries.</description></item>
/// <item><description><b>All offsets are table-relative.</b> <c>horizOffset</c>, <c>vertOffset</c>, <c>sizeTableOffset</c>, and each <c>TrackTableEntry</c>'s <c>offset</c> all measure from the start of the <c>trak</c> table, not from any nested structure. The parser reads them from the table-scoped source without slicing.</description></item>
/// <item><description><b>Longword alignment.</b> The specification requires each <c>TrackData</c> block to begin on a four-byte boundary; the parser rejects non-aligned offsets.</description></item>
/// <item><description>All multi-byte reads use the sfnt default of big-endian.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html"><c>trak</c> table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="TrakTrackData"/>
/// <seealso cref="TrakTrack"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
public sealed record TrakTable : IFontTable<TrakTable>, IRecord<TrakTable>
{
    /// <summary>The only version defined by the current specification, as a 16.16 <c>Fixed</c> value.</summary>
    /// <value>The bit pattern <c>0x00010000</c>, corresponding to <c>Fixed</c> 1.0.</value>
    /// <seealso cref="Version"/>
    public static readonly Fixed CurrentVersion = new(0x00010000);

    /// <inheritdoc/>
    public static Tag Tag => "trak";

    /// <summary>Gets the table version. Always <see cref="CurrentVersion"/>.</summary>
    /// <value>The 16.16 <c>Fixed</c> version read from the header. The parser rejects any other value.</value>
    /// <seealso cref="CurrentVersion"/>
    public Fixed Version { get; init; }

    /// <summary>Gets the format discriminant. Always <c>0</c>.</summary>
    /// <value>The format field from the header. Format <c>0</c> is the only one defined; the parser rejects any other value.</value>
    public ushort Format { get; init; }

    /// <summary>Gets the horizontal track data, or <see langword="null"/> when the table carries none.</summary>
    /// <value>The parsed <see cref="TrakTrackData"/> when <c>horizOffset</c> is non-zero; otherwise <see langword="null"/>.</value>
    /// <seealso cref="VerticalData"/>
    /// <seealso cref="TrakTrackData"/>
    public TrakTrackData? HorizontalData { get; init; }

    /// <summary>Gets the vertical track data, or <see langword="null"/> when the table carries none.</summary>
    /// <value>The parsed <see cref="TrakTrackData"/> when <c>vertOffset</c> is non-zero; otherwise <see langword="null"/>.</value>
    /// <seealso cref="HorizontalData"/>
    /// <seealso cref="TrakTrackData"/>
    public TrakTrackData? VerticalData { get; init; }

    static TrakTable IRecord<TrakTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
            throw new InvalidDataException($"'trak'.version is 0x{header.Version.Bits:X8}, expected 0x{CurrentVersion.Bits:X8}.");

        if (header.Format != 0)
            throw new InvalidDataException($"'trak'.format is {header.Format}, expected 0.");

        if (header.HorizOffset != 0 && (header.HorizOffset & 3) != 0)
            throw new InvalidDataException($"'trak'.horizOffset {header.HorizOffset} is not longword aligned.");

        if (header.VertOffset != 0 && (header.VertOffset & 3) != 0)
            throw new InvalidDataException($"'trak'.vertOffset {header.VertOffset} is not longword aligned.");

        // Offsets inside TrackData are table-relative, so the context carries the
        // table-scoped source so Parse can resolve SizeTableOffset and each entry's
        // Offset correctly without depending on the offset cursor's slice origin.
        var parentContext = new ParentContext(cursor.Source);
        return new()
        {
            Version = header.Version,
            Format = header.Format,
            HorizontalData = header.HorizOffset != 0 ? cursor.Source.ParseRecordAt<TrakTrackData>(header.HorizOffset, parentContext) : null,
            VerticalData = header.VertOffset != 0 ? cursor.Source.ParseRecordAt<TrakTrackData>(header.VertOffset, parentContext) : null,
        };
    }

    /// <summary>The 12-byte <c>trak</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The version is a 16.16 <c>Fixed</c> value; the remaining fields are <c>UInt16</c>, so the reversal is field-by-field.</description></item>
    /// <item><description>Consumed at parse time and not retained on <see cref="TrakTable"/>; only <see cref="Version"/> and <see cref="Format"/> are exposed as semantic properties.</description></item>
    /// <item><description>The <c>&lt;Clone&gt;$()</c> method generated by the <c>record struct</c> declaration must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>Public because a writer fills the struct in memory, reverses it, and writes the bytes by blittable cast.</description></item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0, as a <c>Fixed</c> 16.16 value.</summary>
        public Fixed Version;         // +0

        /// <summary>The format discriminant at byte offset 4. Always <c>0</c>.</summary>
        public ushort Format;         // +4

        /// <summary>Byte offset from the table start to the horizontal <c>TrackData</c>, or <c>0</c> when absent, at byte offset 6.</summary>
        public ushort HorizOffset;    // +6

        /// <summary>Byte offset from the table start to the vertical <c>TrackData</c>, or <c>0</c> when absent, at byte offset 8.</summary>
        public ushort VertOffset;     // +8

        /// <summary>Reserved padding at byte offset 10. Must be zero per the specification; the parser preserves the value.</summary>
        public ushort Reserved;       // +10

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>The version is reversed through <see cref="Fixed.ReverseEndianness(Fixed)"/> so the 16.16 representation is preserved.</remarks>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = Fixed.ReverseEndianness(value.Version),
            Format = BinaryPrimitives.ReverseEndianness(value.Format),
            HorizOffset = BinaryPrimitives.ReverseEndianness(value.HorizOffset),
            VertOffset = BinaryPrimitives.ReverseEndianness(value.VertOffset),
            Reserved = BinaryPrimitives.ReverseEndianness(value.Reserved),
        };
    }
}

/// <summary>The per-orientation track data: a shared size list and a set of tracks, each carrying one tracking value per stored size. Interpolates along both the track axis and the size axis on request.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The size list is shared across all tracks; each track carries one <c>Int16</c> tracking value per stored size.</description></item>
/// <item><description>Tracks are stored in ascending order by <see cref="TrakTrack.Track"/>, per the specification. The parser does not validate this; a diagnostics analyzer should.</description></item>
/// <item><description>Sizes are stored in ascending order, per the specification. <see cref="GetTrackValue(Fixed, Fixed)"/> uses binary search and produces undefined results for an unsorted list; a diagnostics analyzer should flag that condition.</description></item>
/// <item><description>The interpolation formula follows the specification directly: <c>V(track) = V(lo) + (V(hi) - V(lo)) × (track - track(lo)) / (track(hi) - track(lo))</c>, where each <c>V</c> is itself the result of the size-axis interpolation at the requested point size.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="TrakTable"/>
/// <seealso cref="TrakTrack"/>
/// <seealso cref="TrackTableEntry"/>
public sealed record TrakTrackData : IRecord<TrakTrackData>
{
    /// <summary>Gets the point sizes at which tracking values are stored.</summary>
    public required ReadOnlyMemory<Fixed> Sizes { get; init; }

    /// <summary>Gets the per-track tracking values, in the order they appear on disk.</summary>
    public required IReadOnlyList<TrakTrack> Tracks { get; init; }

    /// <summary>Gets the interpolated tracking value for a track number and point size.</summary>
    /// <param name="track">The requested track number, as a <see cref="Fixed"/> 16.16 value.</param>
    /// <param name="pointSize">The requested point size, as a <see cref="Fixed"/> 16.16 value.</param>
    /// <returns>The tracking value in FUnits, interpolated across both the track axis and the size axis.</returns>
    /// <remarks>Both axes interpolate linearly between bracketing entries and extrapolate linearly outside the stored range. Degenerate cases: a single track reduces to size-axis interpolation; a single size reduces to track-axis interpolation.</remarks>
    public double GetTrackValue(Fixed track, Fixed pointSize)
    {
        IReadOnlyList<TrakTrack> tracks = Tracks;
        ReadOnlySpan<Fixed> sizes = Sizes.Span;
        int n = tracks.Count;
        if (n == 0 || sizes.IsEmpty)
            return 0;

        int sizeBits = (int)pointSize.Bits;

        if (n == 1)
            return InterpolateSize(sizes, tracks[0].Values.Span, sizeBits);

        int trackBits = (int)track.Bits;
        int firstTrackBits = (int)tracks[0].Track.Bits;
        int lastTrackBits = (int)tracks[n - 1].Track.Bits;

        int lo, hi;
        if (trackBits <= firstTrackBits) { lo = 0; hi = 1; }
        else if (trackBits >= lastTrackBits) { lo = n - 2; hi = n - 1; }
        else
        {
            lo = 0; hi = n - 1;
            while (hi - lo > 1)
            {
                int mid = lo + ((hi - lo) >> 1);
                if ((int)tracks[mid].Track.Bits <= trackBits) lo = mid;
                else hi = mid;
            }
        }

        double v0 = InterpolateSize(sizes, tracks[lo].Values.Span, sizeBits);
        double v1 = InterpolateSize(sizes, tracks[hi].Values.Span, sizeBits);

        long t0 = (int)tracks[lo].Track.Bits;
        long t1 = (int)tracks[hi].Track.Bits;
        long den = t1 - t0;
        if (den == 0)
            return v0;

        double frac = (double)((long)trackBits - t0) / den;
        return v0 + (v1 - v0) * frac;
    }

    private static double InterpolateSize(ReadOnlySpan<Fixed> sizes, ReadOnlySpan<short> values, int sizeBits)
    {
        int n = Math.Min(sizes.Length, values.Length);
        if (n == 0) return 0;
        if (n == 1) return values[0];

        int firstSizeBits = (int)sizes[0].Bits;
        int lastSizeBits = (int)sizes[n - 1].Bits;

        int lo, hi;
        if (sizeBits <= firstSizeBits) { lo = 0; hi = 1; }
        else if (sizeBits >= lastSizeBits) { lo = n - 2; hi = n - 1; }
        else
        {
            lo = 0; hi = n - 1;
            while (hi - lo > 1)
            {
                int mid = lo + ((hi - lo) >> 1);
                if ((int)sizes[mid].Bits <= sizeBits) lo = mid;
                else hi = mid;
            }
        }

        long s0 = (int)sizes[lo].Bits;
        long s1 = (int)sizes[hi].Bits;
        long den = s1 - s0;
        if (den == 0) return values[lo];

        long v0 = values[lo];
        long v1 = values[hi];
        long num = (v1 - v0) * ((long)sizeBits - s0);
        return v0 + (double)num / den;
    }

    static TrakTrackData IRecord<TrakTrackData>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not ParentContext parentCtx)
            throw new InvalidDataException(
                "'trak' TrackData requires a ParentContext carrying the table-scoped source.");

        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.NTracks == 0)
            throw new InvalidDataException("'trak' TrackData declares zero tracks.");
        if (header.NSizes == 0)
            throw new InvalidDataException("'trak' TrackData declares zero sizes.");

        // The entry array immediately follows the header, so a sequential cursor
        // read from TrackDataStart is the right primitive. Sizes and per-track
        // values are table-relative and resolved via the parent source.
        return new()
        {
            Sizes = parentCtx.ParentSource.ReadEndianReversibleStructArrayAt<Fixed>(header.SizeTableOffset, header.NSizes),
            Tracks = Array.AsReadOnly(cursor.ReadEndianReversibleHeaderRecordArray<TrakTrack, TrackTableEntry>(header.NTracks, new Context(parentCtx.ParentSource, header.NSizes))),
        };
    }

    /// <summary>Context for parsing a <see cref="TrakTrack"/>: the table-scoped source plus the shared size count.</summary>
    /// <param name="ParentSource">The table-scoped source; each <c>TrackTableEntry.Offset</c> is resolved against it.</param>
    /// <param name="NSizes">The number of per-size tracking values in each track; the length of <see cref="TrakTrack.Values"/>.</param>
    /// <remarks>Implements <see cref="IParentContext"/> because it carries the parent source. A <see cref="ParentContext"/> alone is insufficient: <see cref="IHeaderRecord{T, THeader}.FromHeader"/> also needs the size count.</remarks>
    public record Context(Source ParentSource, int NSizes) : IParentContext;

    /// <summary>The 8-byte <c>TrackData</c> header.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The number of tracks at byte offset 0.</summary>
        public ushort NTracks;         // +0

        /// <summary>The number of point sizes at byte offset 2.</summary>
        public ushort NSizes;          // +2

        /// <summary>Byte offset from the table start to the size subtable, at byte offset 4.</summary>
        public uint SizeTableOffset;   // +4

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        public static Header ReverseEndianness(Header value) => new()
        {
            NTracks = BinaryPrimitives.ReverseEndianness(value.NTracks),
            NSizes = BinaryPrimitives.ReverseEndianness(value.NSizes),
            SizeTableOffset = BinaryPrimitives.ReverseEndianness(value.SizeTableOffset),
        };
    }
}

/// <summary>A single track: a track number, a <c>name</c> table index for its descriptive label, and one tracking value per stored size.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <see cref="Values"/> array is indexed in parallel with <see cref="TrakTrackData.Sizes"/>; <see cref="Values"/>.Length equals <see cref="TrakTrackData.Sizes"/>.Length.</description></item>
/// <item><description><see cref="NameIndex"/> follows the same range convention as <c>feat</c> feature and setting labels: a value greater than <c>255</c> and less than <c>32768</c>.</description></item>
/// <item><description>Tracking values are FUnits. They are added directly to advance widths after multiplication by the point size (see <see cref="TrakTrackData.GetTrackValue(Fixed, Fixed)"/>); the table format does not distinguish positive from negative values — negative tightens, positive loosens.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="TrakTrackData"/>
/// <seealso cref="TrakTable"/>
public sealed record TrakTrack : IEndianReversibleHeaderRecord<TrakTrack, TrackTableEntry>
{
    /// <summary>Gets the track number.</summary>
    public required Fixed Track { get; init; }

    /// <summary>Gets the <c>name</c> table index for the track's descriptive label.</summary>
    public required ushort NameIndex { get; init; }

    /// <summary>Gets the per-size tracking values in FUnits.</summary>
    /// <value>One <c>Int16</c> per stored size, indexed in parallel with <see cref="TrakTrackData.Sizes"/>.</value>
    public required ReadOnlyMemory<short> Values { get; init; }

    static TrakTrack IHeaderRecord<TrakTrack, TrackTableEntry>.FromHeader(in TrackTableEntry header, object? context)
    {
        if (context is not TrakTrackData.Context ctx)
            throw new InvalidDataException("'trak' track requires a TrakTrackData.Context carrying the parent source and size count.");

        // TrackTableEntry.Offset is table-relative; ctx.ParentSource is the table-scoped
        // source, so ReadInt16ArrayAt resolves the offset without any slice compensation.
        return new()
        {
            Track = header.Track,
            NameIndex = header.NameIndex,
            Values = ctx.ParentSource.ReadInt16ArrayAt(header.Offset, ctx.NSizes),
        };
    }
}

/// <summary>A single track-table entry on disk: a track number, a <c>name</c> table index, and a table-relative offset to the per-size value array.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <c>track</c> field is a <c>Fixed</c> 16.16 value; <see cref="NameIndex"/> and <see cref="Offset"/> are <c>UInt16</c>. The struct is therefore mixed-width and is reversed field-by-field.</description></item>
/// <item><description>The <see cref="Offset"/> field measures from the <em>start of the <c>trak</c> table</em>, not from the <c>TrackData</c> block.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="TrakTrack"/>
/// <seealso cref="TrakTrackData"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct TrackTableEntry : IEndianReversibleStruct<TrackTableEntry>
{
    /// <summary>The track number at byte offset 0.</summary>
    public Fixed Track;      // +0

    /// <summary>The <c>name</c> table index for the track's descriptive label at byte offset 4.</summary>
    public ushort NameIndex; // +4

    /// <summary>Byte offset from the table start to the per-size tracking values, at byte offset 6.</summary>
    public ushort Offset;    // +6

    /// <summary>Reverses the byte order of every multi-byte field in a <see cref="TrackTableEntry"/>.</summary>
    /// <param name="value">The value whose fields are to be reversed.</param>
    /// <returns>A new entry with each field reversed.</returns>
    public static TrackTableEntry ReverseEndianness(TrackTableEntry value) => new()
    {
        Track = Fixed.ReverseEndianness(value.Track),
        NameIndex = BinaryPrimitives.ReverseEndianness(value.NameIndex),
        Offset = BinaryPrimitives.ReverseEndianness(value.Offset),
    };
}
