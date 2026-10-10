using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// trak — Tracking Table (Apple Advanced Typography)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>trak</c> table: per-size interglyph spacing adjustments for a set of named tracks, applied uniformly to every glyph rather than as pair kerning.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is an Apple Advanced Typography (AAT) table and is not part of OpenType. Apple documents it in the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual, <c>trak</c> table</see>.</description></item>
/// <item><description>Tracking differs from kerning in what it depends on. Kerning chooses a value from the identity of two neighbouring glyphs; tracking chooses a value from the point size and a track number, and applies the same adjustment to every glyph in the run. The track numbers are arbitrary, although the values -1, 0, and +1 conventionally mean loose, normal, and tight.</description></item>
/// <item><description>The per-size values are stored in FUnits per em. For a font of 2048 units per em a value of 10 is 10/2048 em; a renderer converts the FUnit value to ems, multiplies it by the point size, and adds the result to the advance of every glyph. A negative value tightens and a positive value loosens.</description></item>
/// <item><description>The twelve-byte <see cref="Header"/> locates one <see cref="TrackData"/> blob for horizontal text and one for vertical text. Either offset may be zero, meaning that no data for that direction is present, and the machine-readable offsets are exposed as <see cref="HorizontalOffset"/> and <see cref="VerticalOffset"/>.</description></item>
/// <item><description>Every offset inside a <see cref="TrackData"/> blob — the size table offset of the blob's own header and the per-size value offset of each <see cref="TrackTableEntry"/> — is measured from the start of the <c>trak</c> table, not from the start of the blob. Apple's worked example offsets confirm this, as do HarfBuzz and the fontations reader.</description></item>
/// <item><description>Apple recommends at least two point sizes and at least the tracks -1, 0, and +1. A combination the font does not store is meant to be interpolated or extrapolated by the consumer, which this type does not do: <see cref="TrackData.GetTrackingValue"/> reports only exact matches.</description></item>
/// <item><description>Each track also names a descriptive phrase, such as "loose" or "very tight", through a <c>name</c> table ID. The phrase is not functional; it exists so an application can present the track to a user.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="TrackData"/>
/// <seealso cref="TrackTableEntry"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6name.html">TrueType Reference Manual: The <c>name</c> table</seealso>
public sealed record TrakTable : IFontTable<TrakTable>
{
    /// <summary>Gets the AAT table tag <c>trak</c>.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
    public static Tag Tag => "trak";

    /// <summary>Gets the table version.</summary>
    /// <remarks>The current version is 1.0; <see cref="MajorVersion"/> must be 1.</remarks>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="MinorVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the major half of <see cref="Version"/>, which must be 1.</summary>
    /// <seealso cref="Version"/>
    /// <seealso cref="MinorVersion"/>
    public ushort MajorVersion => (ushort)Version.IntegerPart;

    /// <summary>Gets the minor half of <see cref="Version"/>.</summary>
    /// <seealso cref="Version"/>
    /// <seealso cref="MajorVersion"/>
    public ushort MinorVersion => Version.FractionPart;

    /// <summary>Gets the table format, which must be <see cref="Format0"/>.</summary>
    /// <remarks>Format 0 is the only format the specification defines.</remarks>
    /// <seealso cref="Format0"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
    public ushort Format { get; init; }

    /// <summary>Gets the offset of the horizontal <see cref="TrackData"/> blob from the start of the table, or zero when the font stores none.</summary>
    /// <seealso cref="Horizontal"/>
    /// <seealso cref="VerticalOffset"/>
    public ushort HorizontalOffset { get; init; }

    /// <summary>Gets the offset of the vertical <see cref="TrackData"/> blob from the start of the table, or zero when the font stores none.</summary>
    /// <seealso cref="Vertical"/>
    /// <seealso cref="HorizontalOffset"/>
    public ushort VerticalOffset { get; init; }

    /// <summary>Gets the reserved field, which the specification requires to be zero.</summary>
    /// <remarks>The field is preserved rather than validated; a consumer round-tripping the table needs it.</remarks>
    public ushort Reserved { get; init; }

    /// <summary>Gets the track data for horizontal text, or <see langword="null"/> when <see cref="HorizontalOffset"/> is zero.</summary>
    /// <seealso cref="TrackData"/>
    /// <seealso cref="HasHorizontal"/>
    public TrackData? Horizontal { get; init; }

    /// <summary>Gets the track data for vertical text, or <see langword="null"/> when <see cref="VerticalOffset"/> is zero.</summary>
    /// <seealso cref="TrackData"/>
    /// <seealso cref="HasVertical"/>
    public TrackData? Vertical { get; init; }

    /// <summary>Gets a value indicating whether the table carries track data for horizontal text.</summary>
    /// <seealso cref="Horizontal"/>
    public bool HasHorizontal => Horizontal is not null;

    /// <summary>Gets a value indicating whether the table carries track data for vertical text.</summary>
    /// <seealso cref="Vertical"/>
    public bool HasVertical => Vertical is not null;

    /// <summary>Format value 0, the only format the specification defines.</summary>
    /// <seealso cref="Format"/>
    public const ushort Format0 = 0;

    /// <summary>Size, in bytes, of the fixed-layout <see cref="Header"/>.</summary>
    /// <remarks>The first <see cref="TrackData"/> blob may begin at this offset, because the blobs are longword aligned.</remarks>
    /// <seealso cref="Header"/>
    public const int HeaderSize = 12;

    /// <summary>The twelve-byte fixed-layout <c>trak</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Fields are stored in big-endian order at the offsets defined by the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html"><c>trak</c> table specification</see>.</description></item>
    /// <item><description>The two data offsets are measured from the start of the table, and their order in the table is arbitrary because both are explicit.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0.</summary>
        /// <remarks>1.0 for the current version.</remarks>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
        public Fixed Version;       // +0

        /// <summary>The table format at byte offset 4, which must be <see cref="Format0"/>.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
        public ushort Format;       // +4

        /// <summary>The offset of the horizontal track data from the start of the table, or zero when absent, at byte offset 6.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
        public ushort HorizOffset;  // +6

        /// <summary>The offset of the vertical track data from the start of the table, or zero when absent, at byte offset 8.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
        public ushort VertOffset;   // +8

        /// <summary>The reserved field at byte offset 10, which must be zero.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
        public ushort Reserved;     // +10

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = new Fixed(BinaryPrimitives.ReverseEndianness(v.Version.Bits)),
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            HorizOffset = BinaryPrimitives.ReverseEndianness(v.HorizOffset),
            VertOffset = BinaryPrimitives.ReverseEndianness(v.VertOffset),
            Reserved = BinaryPrimitives.ReverseEndianness(v.Reserved),
        };
    }

    /// <summary>The tracking data for one direction of text: a set of tracks and the point sizes they are tabulated at.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The blob must be longword aligned within the table. Its eight-byte header declares the track count, the size count, and the offset of the size array.</description></item>
    /// <item><description>Both the size array offset and the per-size value offsets of the tracks are measured from the start of the <c>trak</c> table, so this type is only meaningful together with the table that produced it.</description></item>
    /// <item><description>The tracks are sorted by track value in the fonts Apple's example describes, but the order is not enforced here; the lookups scan the list.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="TrakTable.Horizontal"/>
    /// <seealso cref="TrakTable.Vertical"/>
    /// <seealso cref="TrackTableEntry"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
    public sealed record TrackData
    {
        /// <summary>Gets the number of tracks the blob declares.</summary>
        /// <remarks>Equals <c>Tracks.Count</c> for a structurally valid blob.</remarks>
        /// <seealso cref="Tracks"/>
        public int TrackCount { get; init; }

        /// <summary>Gets the number of point sizes the blob declares.</summary>
        /// <remarks>Equals <c>Sizes.Count</c> for a structurally valid blob, unless <see cref="SizeTableOffset"/> is zero, in which case <see cref="Sizes"/> is empty.</remarks>
        /// <seealso cref="Sizes"/>
        public int SizeCount { get; init; }

        /// <summary>Gets the declared offset of the size array from the start of the <c>trak</c> table.</summary>
        /// <remarks>The declared value is preserved even though the array itself is exposed through <see cref="Sizes"/>.</remarks>
        /// <seealso cref="Sizes"/>
        public uint SizeTableOffset { get; init; }

        /// <summary>Gets the tracks of the blob, in the order the font stores them.</summary>
        /// <remarks>Each track carries one per-size tracking value for every entry of <see cref="Sizes"/>.</remarks>
        /// <seealso cref="TrackTableEntry"/>
        /// <seealso cref="GetTrack(Fixed)"/>
        public IReadOnlyList<TrackTableEntry> Tracks { get; init; } = [];

        /// <summary>Gets the point sizes the tracks are tabulated at, in the order the font stores them.</summary>
        /// <remarks>Each entry is a 16.16 fixed-point point size, so 12.0 and 24.0 are stored as <c>0x000C0000</c> and <c>0x00180000</c>. Apple recommends at least two sizes.</remarks>
        /// <seealso cref="GetSize(int)"/>
        /// <seealso cref="GetTrackingValue(Fixed, Fixed)"/>
        public IReadOnlyList<Fixed> Sizes { get; init; } = [];

        /// <summary>Gets the track with the specified track value.</summary>
        /// <param name="track">The track value to find, normally -1, 0, or +1.</param>
        /// <returns>The matching track, or <see langword="null"/> when the blob does not tabulate that track.</returns>
        /// <remarks>The list is scanned because the specification does not require the tracks to be sorted, even though the worked example stores them in ascending order.</remarks>
        /// <seealso cref="Tracks"/>
        /// <seealso cref="GetTrackingValues(Fixed)"/>
        public TrackTableEntry? GetTrack(Fixed track)
        {
            for (int i = 0; i < Tracks.Count; i++)
                if (Tracks[i].Track == track) return Tracks[i];

            return null;
        }

        /// <summary>Gets the per-size tracking values of a track.</summary>
        /// <param name="track">The track value to find.</param>
        /// <returns>The track's values, one per entry of <see cref="Sizes"/>, or an empty list when the blob does not tabulate the track.</returns>
        /// <remarks>Each value is a signed FUnit adjustment; index <c>i</c> corresponds to point size <c>Sizes[i]</c>.</remarks>
        /// <seealso cref="GetTrackingValue(Fixed, Fixed)"/>
        /// <seealso cref="TrackTableEntry.Values"/>
        public IReadOnlyList<short> GetTrackingValues(Fixed track)
        {
            TrackTableEntry? entry = GetTrack(track);
            if (entry is null) return [];

            return entry.Values;
        }

        /// <summary>Gets the tracking adjustment of a track at an exact point size.</summary>
        /// <param name="track">The track value to find.</param>
        /// <param name="pointSize">The point size to find, which must equal an entry of <see cref="Sizes"/> exactly.</param>
        /// <returns>The FUnit adjustment, or <see langword="null"/> when the blob tabulates neither the track nor the size.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>Only exact matches are reported. The specification expects a consumer to interpolate between, or extrapolate beyond, the stored sizes, which is a shaping policy this reader deliberately does not encode.</description></item>
        /// <item><description>The sign convention is that a negative value tightens and a positive value loosens the advance of every glyph.</description></item>
        /// </list>
        /// </remarks>
        /// <seealso cref="GetTrackingValues(Fixed)"/>
        /// <seealso cref="Sizes"/>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
        public short? GetTrackingValue(Fixed track, Fixed pointSize)
        {
            TrackTableEntry? entry = GetTrack(track);
            if (entry is null) return null;

            for (int i = 0; i < Sizes.Count && i < entry.Values.Count; i++)
                if (Sizes[i] == pointSize) return entry.Values[i];

            return null;
        }

        /// <summary>Gets the point size at a position in <see cref="Sizes"/>.</summary>
        /// <param name="sizeIndex">The zero-based index into <see cref="Sizes"/>.</param>
        /// <returns>The point size, or <see langword="null"/> when <paramref name="sizeIndex"/> is out of range.</returns>
        /// <seealso cref="Sizes"/>
        public Fixed? GetSize(int sizeIndex) =>
            (uint)sizeIndex < (uint)Sizes.Count ? Sizes[sizeIndex] : null;

        /// <summary>The eight-byte fixed-layout <see cref="TrackData"/> header.</summary>
        /// <remarks>The track entries follow this header immediately, and the size array lies at <see cref="SizeTableOffset"/>.</remarks>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public record struct Header : IEndianReversibleStruct<Header>
        {
            /// <summary>The number of tracks in the blob at byte offset 0.</summary>
            /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
            public ushort NTracks;          // +0

            /// <summary>The number of point sizes in the blob at byte offset 2.</summary>
            /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
            public ushort NSizes;           // +2

            /// <summary>The offset of the size array from the start of the <c>trak</c> table at byte offset 4.</summary>
            /// <remarks>The offset is measured from the start of the tracking table, not from the start of this header, which is the reading Apple's worked example uses.</remarks>
            /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
            public uint SizeTableOffset;    // +4

            /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
            public static Header ReverseEndianness(Header v) => new()
            {
                NTracks = BinaryPrimitives.ReverseEndianness(v.NTracks),
                NSizes = BinaryPrimitives.ReverseEndianness(v.NSizes),
                SizeTableOffset = BinaryPrimitives.ReverseEndianness(v.SizeTableOffset),
            };
        }
    }

    /// <summary>One track of a <see cref="TrackData"/> blob: a track value, the name of a descriptive phrase, and the per-size adjustments.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The track value is a 16.16 fixed-point number, normally -1, 0, or +1 for loose, normal, and tight.</description></item>
    /// <item><description>The name index addresses the <c>name</c> table; the specification requires a value greater than 255 and less than 32768, so that the phrase is a font-supplied string rather than one of the standard names.</description></item>
    /// <item><description>The per-size values are signed FUnits, one per entry of <see cref="TrackData.Sizes"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="TrackData.Tracks"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6name.html">TrueType Reference Manual: The <c>name</c> table</seealso>
    public sealed record TrackTableEntry
    {
        /// <summary>Gets the track value, a 16.16 fixed-point number.</summary>
        /// <remarks>Values of -1, 0, and +1 conventionally mean loose, normal, and tight.</remarks>
        public Fixed Track { get; init; }

        /// <summary>Gets the <c>name</c> table ID of the phrase that describes the track, such as "loose" or "very tight".</summary>
        /// <remarks>The specification requires a value greater than 255 and less than 32768. The phrase is not functional.</remarks>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6name.html">TrueType Reference Manual: The <c>name</c> table</seealso>
        public ushort NameIndex { get; init; }

        /// <summary>Gets the declared offset of the per-size values from the start of the <c>trak</c> table.</summary>
        /// <remarks>The declared value is preserved even though the values themselves are exposed through <see cref="Values"/>.</remarks>
        /// <seealso cref="Values"/>
        public ushort Offset { get; init; }

        /// <summary>Gets the per-size tracking values, one per entry of the owning <see cref="TrackData.Sizes"/>.</summary>
        /// <remarks>Each value is a signed FUnit adjustment, and the list is empty when <see cref="Offset"/> is zero.</remarks>
        /// <seealso cref="GetTrackingValue(int)"/>
        /// <seealso cref="TrackData.Sizes"/>
        public IReadOnlyList<short> Values { get; init; } = [];

        /// <summary>Gets the tracking adjustment at a position in the owning size array.</summary>
        /// <param name="sizeIndex">The zero-based index into <see cref="TrackData.Sizes"/>, which is also the index into <see cref="Values"/>.</param>
        /// <returns>The FUnit adjustment, or <see langword="null"/> when <paramref name="sizeIndex"/> is out of range.</returns>
        /// <seealso cref="Values"/>
        /// <seealso cref="TrackData.Sizes"/>
        public short? GetTrackingValue(int sizeIndex) =>
            (uint)sizeIndex < (uint)Values.Count ? Values[sizeIndex] : null;

        /// <summary>The eight-byte fixed-layout <see cref="TrackTableEntry"/> record.</summary>
        /// <remarks>The entries follow the <see cref="TrackData.Header"/> immediately, with no padding between them.</remarks>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public record struct Header : IEndianReversibleStruct<Header>
        {
            /// <summary>The track value at byte offset 0, in 16.16 fixed-point format.</summary>
            /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
            public Fixed Track;      // +0

            /// <summary>The <c>name</c> table ID of the track's descriptive phrase at byte offset 4.</summary>
            /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
            public ushort NameIndex; // +4

            /// <summary>The offset of the per-size values from the start of the <c>trak</c> table at byte offset 6.</summary>
            /// <remarks>The offset is measured from the start of the tracking table, not from the start of this record, which is the reading Apple's worked example uses.</remarks>
            /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6trak.html">TrueType Reference Manual: The <c>trak</c> table</seealso>
            public ushort Offset;    // +6

            /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
            public static Header ReverseEndianness(Header v) => new()
            {
                Track = new Fixed(BinaryPrimitives.ReverseEndianness(v.Track.Bits)),
                NameIndex = BinaryPrimitives.ReverseEndianness(v.NameIndex),
                Offset = BinaryPrimitives.ReverseEndianness(v.Offset),
            };
        }
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the <c>trak</c> table.</param>
    /// <param name="context">Ignored; the table declares every offset it uses.</param>
    /// <returns>The parsed <c>trak</c> table.</returns>
    /// <exception cref="InvalidDataException">The major version is not 1, or the format is not <see cref="Format0"/>.</exception>
    /// <exception cref="EndOfStreamException">The header, a track entry array, the size array, or a per-size value array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The table's major version must be 1 and its format must be <see cref="Format0"/>.</description></item>
    /// <item><description>Each of the two declared data offsets, when non-zero, is resolved against the table-scoped source so that the size array and per-size value offsets inside the blob resolve from the start of the <c>trak</c> table.</description></item>
    /// </list>
    /// </remarks>
    static TrakTable IRecord<TrakTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version.IntegerPart != 1)
        {
            throw new InvalidDataException(
                $"'trak'.version is 0x{unchecked((uint)header.Version.Bits):X8}, expected a 1.x table.");
        }

        if (header.Format != Format0)
            throw new InvalidDataException($"'trak'.format is {header.Format}, expected 0.");

        return new TrakTable
        {
            Version = header.Version,
            Format = header.Format,
            HorizontalOffset = header.HorizOffset,
            VerticalOffset = header.VertOffset,
            Reserved = header.Reserved,
            Horizontal = header.HorizOffset == 0 ? null : ParseTrackData(cursor.Source, header.HorizOffset),
            Vertical = header.VertOffset == 0 ? null : ParseTrackData(cursor.Source, header.VertOffset),
        };
    }

    /// <summary>Reads one horizontal or vertical <see cref="TrackData"/> blob.</summary>
    /// <param name="source">The table-scoped source, so that the blob's offsets resolve from the start of the <c>trak</c> table.</param>
    /// <param name="trackDataOffset">The offset of the blob from the start of the table.</param>
    /// <returns>The parsed blob.</returns>
    /// <exception cref="EndOfStreamException">A track entry, the size array, or a per-size value array extends past the end of the table data.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The size array is read from the blob's declared offset only when that offset is non-zero; a zero offset would otherwise read the table header as size values.</description></item>
    /// <item><description>A track whose value offset is zero, or a blob whose size array is empty, yields an empty value list rather than a guess.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="TrackData"/>
    private static TrackData ParseTrackData(Source source, long trackDataOffset)
    {
        Cursor cursor = source.CreateOffsetCursor(trackDataOffset);
        TrackData.Header header = cursor.ReadBigEndianStruct<TrackData.Header>();

        TrackTableEntry.Header[] wireEntries =
            cursor.ReadBigEndianStructArray<TrackTableEntry.Header>(header.NTracks);

        Fixed[] sizes = ReadSizes(source, header.SizeTableOffset, header.NSizes);

        var tracks = new TrackTableEntry[wireEntries.Length];
        for (int i = 0; i < wireEntries.Length; i++)
        {
            TrackTableEntry.Header wireEntry = wireEntries[i];

            short[] values = [];
            if (wireEntry.Offset != 0 && sizes.Length != 0)
                values = source.ReadInt16ArrayAt(wireEntry.Offset, sizes.Length);

            tracks[i] = new TrackTableEntry
            {
                Track = wireEntry.Track,
                NameIndex = wireEntry.NameIndex,
                Offset = wireEntry.Offset,
                Values = values,
            };
        }

        return new TrackData
        {
            TrackCount = header.NTracks,
            SizeCount = header.NSizes,
            SizeTableOffset = header.SizeTableOffset,
            Tracks = tracks,
            Sizes = sizes,
        };
    }

    /// <summary>Reads the size array of a <see cref="TrackData"/> blob.</summary>
    /// <param name="source">The table-scoped source.</param>
    /// <param name="sizeTableOffset">The offset of the size array from the start of the <c>trak</c> table.</param>
    /// <param name="count">The number of size values to read.</param>
    /// <returns>The point sizes, or an empty array when the offset or the count is zero.</returns>
    /// <exception cref="EndOfStreamException">The array extends past the end of the table data.</exception>
    /// <remarks>Each stored value is a 16.16 fixed-point point size; the bits are reinterpreted as a <see cref="Fixed"/> without scaling.</remarks>
    private static Fixed[] ReadSizes(Source source, uint sizeTableOffset, int count)
    {
        if (sizeTableOffset == 0 || count <= 0) return [];

        uint[] bits = source.ReadUInt32ArrayAt(sizeTableOffset, count);
        var sizes = new Fixed[bits.Length];
        for (int i = 0; i < bits.Length; i++)
            sizes[i] = Fixed.FromBits(unchecked((int)bits[i]));

        return sizes;
    }
}
