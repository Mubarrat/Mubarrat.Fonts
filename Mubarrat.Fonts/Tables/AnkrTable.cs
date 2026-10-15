using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>ankr</c> table: the Apple Advanced Typography anchor point table. Defines anchor points within the coordinate space of a glyph, independent of the control points used to render it, for use by the <c>kerx</c> table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>An anchor point is a named position in a glyph's coordinate space. The <c>kerx</c> table uses them to express kerning adjustments at specific attachment sites rather than at the glyph origin or at control points.</description></item>
/// <item><description>The table holds a standard Apple Advanced Typography <see cref="LookupTable"/> that maps glyph indices onto byte offsets within a glyph data region. Each glyph data entry holds the glyph's anchor points.</description></item>
/// <item><description>Because a lookup value of zero is a valid offset into the glyph data region, a glyph with no anchor points must not appear in the lookup table.</description></item>
/// <item><description>The table is supported on macOS 10.9 and later and on iOS 7.0 and later.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">anchor point table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <!--seealso cref="KerxTable"/-->
/// <seealso cref="LookupTable"/>
/// <seealso cref="AnkrAnchorPoint"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
public sealed record AnkrTable : IFontTable<AnkrTable>, IRecord<AnkrTable>
{
    /// <summary>The only value the specification permits in the <c>version</c> field.</summary>
    /// <value>The constant <c>0</c>.</value>
    /// <seealso cref="Version"/>
    public const ushort CurrentVersion = 0;

    /// <inheritdoc/>
    public static Tag Tag => "ankr";

    /// <summary>Gets the table version. Always 0.</summary>
    /// <value>The constant <see cref="CurrentVersion"/>.</value>
    /// <remarks>The specification defines only one version and requires the field to be zero.</remarks>
    /// <seealso cref="CurrentVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the table flags.</summary>
    /// <value>Always <c>0</c> in a conforming font. The specification declares the field unused.</value>
    /// <remarks>The field is exposed for round-tripping and validation; a validator may flag a non-zero value as non-conforming.</remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
    public ushort Flags { get; init; }

    /// <summary>Gets the lookup table that maps glyph indices to offsets within the glyph data table.</summary>
    /// <value>The parsed <see cref="LookupTable"/>.</value>
    /// <seealso cref="LookupTable"/>
    /// <seealso cref="AnkrTable.TryGetAnchorPoints(int, out IReadOnlyList{AnkrAnchorPoint})"/>
    public required LookupTable Lookup { get; init; }

    /// <summary>Gets the anchor points of every glyph covered by the lookup table, keyed by glyph index.</summary>
    /// <value>A dictionary whose keys are the glyph indices the lookup table answers for and whose values are the parsed anchor point arrays.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The dictionary is populated during parse by enumerating the lookup table's glyph indices and reading each glyph's data entry from the source.</description></item>
    /// <item><description>Glyphs with no anchor points are absent from the dictionary; the specification forbids them from appearing in the lookup table, so a glyph is either present with a non-empty array or absent.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="TryGetAnchorPoints(int, out IReadOnlyList{AnkrAnchorPoint})"/>
    /// <seealso cref="AnkrAnchorPoint"/>
    public IReadOnlyDictionary<int, IReadOnlyList<AnkrAnchorPoint>> GlyphAnchors { get; init; }
        = new Dictionary<int, IReadOnlyList<AnkrAnchorPoint>>();

    /// <summary>Gets the number of glyphs that carry anchor points.</summary>
    /// <value>The number of entries in <see cref="GlyphAnchors"/>.</value>
    /// <seealso cref="GlyphAnchors"/>
    public int AnchoredGlyphCount => GlyphAnchors.Count;

    /// <summary>Retrieves the anchor points of a glyph.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <param name="anchors">When this method returns <see langword="true"/>, contains the glyph's anchor points; otherwise, an empty array.</param>
    /// <returns><see langword="true"/> when the glyph carries anchor points; otherwise, <see langword="false"/>.</returns>
    /// <remarks>A glyph with no entry in the table is reported as <see langword="false"/> rather than as a glyph with zero anchors, matching the specification's requirement that unanchored glyphs be omitted from the lookup table.</remarks>
    /// <seealso cref="GlyphAnchors"/>
    /// <seealso cref="AnkrAnchorPoint"/>
    public bool TryGetAnchorPoints(int glyphIndex, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IReadOnlyList<AnkrAnchorPoint>? anchors)
    {
        if (GlyphAnchors.TryGetValue(glyphIndex, out var result))
        {
            anchors = result;
            return true;
        }

        anchors = null;
        return false;
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the <c>ankr</c> table.</param>
    /// <param name="context">Unused. The table is self-contained and holds no offsets into other tables.</param>
    /// <returns>The parsed <c>ankr</c> table with every glyph's anchor points materialized.</returns>
    /// <exception cref="InvalidDataException">The version is not zero, or a glyph data entry declares more anchor points than the remaining source can hold.</exception>
    /// <exception cref="EndOfStreamException">The header, the lookup table, or a glyph data entry extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The parse is eager: the lookup table is parsed first, then every glyph it covers is walked and its anchor point array materialized.</description></item>
    /// <item><description>The enumeration of glyph indices covered by the lookup table is performed by pattern matching on the concrete <see cref="LookupTable"/> subtype. Adding a new lookup format requires updating the enumeration helper as well as the lookup type itself.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LookupTable"/>
    /// <seealso cref="AnkrAnchorPoint"/>
    /// <seealso cref="Header"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
    static AnkrTable IRecord<AnkrTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
            throw new InvalidDataException($"'ankr'.version is {header.Version}, expected {CurrentVersion}.");

        LookupTable lookup = cursor.Source.ParseRecordAt<LookupTable>(header.LookupTableOffset);

        var glyphAnchors = new Dictionary<int, IReadOnlyList<AnkrAnchorPoint>>();
        foreach (int glyphIndex in EnumerateLookupGlyphs(lookup))
        {
            if (!lookup.TryGetValue(glyphIndex, out uint relativeOffset))
                continue;

            var entryCursor = cursor.Source.CreateCursor((long)header.GlyphDataTableOffset + relativeOffset);
            glyphAnchors[glyphIndex] = ReadAnchorPoints(ref entryCursor);
        }

        return new()
        {
            Version = header.Version,
            Flags = header.Flags,
            Lookup = lookup,
            GlyphAnchors = glyphAnchors,
        };
    }

    /// <summary>Reads one glyph data entry: an anchor point count followed by that many anchor points.</summary>
    /// <param name="cursor">The cursor positioned at the entry's leading <c>uint32</c> anchor point count.</param>
    /// <returns>The glyph's anchor points, or an empty array when the count is zero.</returns>
    /// <exception cref="InvalidDataException">The declared anchor point count exceeds the remaining source.</exception>
    /// <exception cref="EndOfStreamException">The read extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The count is a <c>uint32</c>, but the specification requires a glyph with no anchors to be absent from the lookup table, so a zero count is treated as an empty anchor array rather than as a malformed entry.</description></item>
    /// <item><description>The bound check uses <see cref="Cursor.Remaining"/> against the total byte count the anchor array would occupy, which is <c>numPoints * 4</c>. The check is the only sanity limit applied; a well-formed entry cannot overrun.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AnkrAnchorPoint"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
    private static IReadOnlyList<AnkrAnchorPoint> ReadAnchorPoints(ref Cursor cursor)
    {
        uint numPoints = cursor.ReadUInt32();
        if (numPoints == 0) return [];

        long requiredBytes = (long)numPoints * AnkrAnchorPoint.SizeInBytes;
        if (requiredBytes > cursor.Remaining)
        {
            throw new InvalidDataException(
                $"'ankr' glyph data entry declares {numPoints} anchor points ({requiredBytes} bytes), exceeding the remaining table length of {cursor.Remaining} bytes.");
        }

        return cursor.ReadEndianReversibleStructArray<AnkrAnchorPoint>(checked((int)numPoints));
    }

    /// <summary>Enumerates the glyph indices that a lookup table can answer for.</summary>
    /// <param name="lookup">The lookup table to enumerate.</param>
    /// <returns>The glyph indices the lookup covers, in ascending order for every format.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The enumeration is implemented by pattern matching on the concrete <see cref="LookupTable"/> subtypes because the base type exposes lookup by glyph index but not the reverse direction.</description></item>
    /// <item><description>Dense formats (0, 8, 10) enumerate their covered range; sparse formats (2, 4) enumerate each segment's range; format 6 enumerates each entry's glyph.</description></item>
    /// <item><description>A new lookup format added to <see cref="LookupTable"/> requires a corresponding case here; otherwise glyphs that format covers will be silently skipped.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LookupTable"/>
    /// <seealso cref="IRecord{T}.Parse"/>
    private static IEnumerable<int> EnumerateLookupGlyphs(LookupTable lookup)
    {
        switch (lookup)
        {
            case LookupTable0 t0:
                for (int i = 0; i < t0.Values.Count; i++)
                    yield return i;
                break;

            case LookupTable2 t2:
                foreach (var segment in t2.Segments)
                    for (int glyph = segment.FirstGlyph; glyph <= segment.LastGlyph; glyph++)
                        yield return glyph;
                break;

            case LookupTable4 t4:
                foreach (var segment in t4.Segments)
                    for (int glyph = segment.FirstGlyph; glyph <= segment.LastGlyph; glyph++)
                        yield return glyph;
                break;

            case LookupTable6 t6:
                foreach (var entry in t6.Entries)
                    yield return entry.Glyph;
                break;

            case LookupTable8 t8:
                for (int i = 0; i < t8.Values.Count; i++)
                    yield return t8.FirstGlyph + i;
                break;

            case LookupTable10 t10:
                for (int i = 0; i < t10.Values.Count; i++)
                    yield return t10.FirstGlyph + i;
                break;
        }
    }

    /// <summary>The 12-byte <c>ankr</c> table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed by the lookup table at <see cref="LookupTableOffset"/> and the glyph data table at <see cref="GlyphDataTableOffset"/>. The specification requires the lookup table to begin at offset <c>0x0000000C</c>, immediately after the header.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">anchor point table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AnkrTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0. Always zero.</summary>
        /// <value>The constant <see cref="AnkrTable.CurrentVersion"/>.</value>
        public ushort Version;               // +0

        /// <summary>The table flags at byte offset 2. Always zero.</summary>
        /// <value>The constant <c>0</c> in a conforming font.</value>
        public ushort Flags;                 // +2

        /// <summary>The byte offset from the table start to the lookup table at byte offset 4.</summary>
        /// <value>The constant <c>0x0000000C</c> in a conforming font.</value>
        public uint LookupTableOffset;       // +4

        /// <summary>The byte offset from the table start to the glyph data table at byte offset 8.</summary>
        public uint GlyphDataTableOffset;    // +8

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(value.Version),
            Flags = BinaryPrimitives.ReverseEndianness(value.Flags),
            LookupTableOffset = BinaryPrimitives.ReverseEndianness(value.LookupTableOffset),
            GlyphDataTableOffset = BinaryPrimitives.ReverseEndianness(value.GlyphDataTableOffset),
        };
    }
}

/// <summary>One anchor point in an <c>ankr</c> glyph data entry. Blittable, size 4.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Anchor points are positions in the glyph's coordinate space, expressed as signed coordinates. They are independent of the glyph's control points and are not part of the outline.</description></item>
/// <item><description>The specification describes each anchor point as a two-byte signed x-coordinate followed by a two-byte signed y-coordinate.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">anchor point table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AnkrTable"/>
/// <seealso cref="AnkrTable.GlyphAnchors"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ankr.html">TrueType Reference Manual: The <c>ankr</c> table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AnkrAnchorPoint : IEndianReversibleStruct<AnkrAnchorPoint>
{
    /// <summary>The size of one anchor point, in bytes.</summary>
    /// <value>The constant <c>4</c>.</value>
    /// <remarks>The size is the sum of the two signed sixteen-bit coordinates.</remarks>
    public const int SizeInBytes = 4;

    /// <summary>The x-coordinate at byte offset 0.</summary>
    /// <value>A signed coordinate in the glyph's design space.</value>
    /// <seealso cref="Y"/>
    public short X;    // +0

    /// <summary>The y-coordinate at byte offset 2.</summary>
    /// <value>A signed coordinate in the glyph's design space.</value>
    /// <seealso cref="X"/>
    public short Y;    // +2

    /// <summary>Reverses the byte order of every field in an <see cref="AnkrAnchorPoint"/>.</summary>
    /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new anchor point with each field reversed.</returns>
    public static AnkrAnchorPoint ReverseEndianness(AnkrAnchorPoint value) => new()
    {
        X = BinaryPrimitives.ReverseEndianness(value.X),
        Y = BinaryPrimitives.ReverseEndianness(value.Y),
    };
}
