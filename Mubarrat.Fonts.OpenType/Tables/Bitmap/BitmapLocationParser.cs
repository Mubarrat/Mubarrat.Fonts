using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.OpenType.Tables.Bitmap;

/// <summary>Shared parsing logic for the <c>EBLC</c> and <c>CBLC</c> tables. The two tables have identical layouts; they differ only in version number and whether bit depth 32 (BGRA color) is meaningful.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><see cref="EblcTable"/> and <see cref="CblcTable"/> are thin wrappers around <see cref="Parse(ref Cursor, FontFace, Tag, ushort, ushort)"/>, supplying their own tag and expected version.</description></item>
/// <item><description>The two tables share a version scheme: EBLC uses version 2.0, CBLC uses version 3.0. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">EBLC</see> and <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cblc">CBLC</see> chapters of the specification.</description></item>
/// <item><description>Every offset stored in the parsed structures is relative to the start of the EBLC/CBLC table itself, not to the containing font file. The table-scoped <see cref="Source"/> passed to this parser enforces that convention.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="EblcTable"/>
/// <seealso cref="CblcTable"/>
/// <seealso cref="BitmapStrike"/>
/// <seealso cref="IndexSubtable"/>
/// <seealso cref="FontFace"/>
/// <seealso cref="Source"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">OpenType specification: EBLC table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cblc">OpenType specification: CBLC table</seealso>
public static class BitmapLocationParser
{
    /// <summary>Parses the header and strike array of an EBLC/CBLC table.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the table.</param>
    /// <param name="face">The containing font face, used to read the table's declared length.</param>
    /// <param name="tag">The table's sfnt tag (<c>"EBLC"</c> or <c>"CBLC"</c>).</param>
    /// <param name="expectedMajor">The expected major version: 2 for EBLC, 3 for CBLC.</param>
    /// <param name="expectedMinor">The expected minor version: 0 for both.</param>
    /// <returns>The parsed version numbers and strikes.</returns>
    /// <exception cref="InvalidDataException">The version does not match, the strike count exceeds the safety limit, or the declared table length is too small for the strike array.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The table's declared length from the sfnt directory bounds the strike array. Because <c>cursor.Source</c> is table-scoped (see <see cref="FontFace.GetTable{T}"/>), <c>cursor.Position</c> measures from the table start.</description></item>
    /// <item><description><c>cursor.Remaining</c> would instead measure to the end of the font, not the table; the length bound therefore uses <see cref="FontFace.GetTableLength(Tag)"/> explicitly.</description></item>
    /// <item><description>Strike counts above 1024 are rejected before allocation; a value at or below the limit is still validated against the table's declared length before the array is materialized.</description></item>
    /// </list>
    /// <para>For the header and strike layout, see the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">EBLC table</see> chapter in the OpenType specification.</para>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="BitmapStrike"/>
    /// <seealso cref="FontFace.GetTableLength(Tag)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">OpenType specification: EBLC table</seealso>
    public static (ushort Major, ushort Minor, BitmapStrike[] Strikes) Parse(
        ref Cursor cursor,
        FontFace face,
        Tag tag,
        ushort expectedMajor,
        ushort expectedMinor)
    {
        // The table's declared length from the sfnt directory bounds the strike array.
        // cursor.Source is table-scoped (see FontFace.GetTable<T>), so cursor.Position
        // measures from the table start. cursor.Remaining would instead measure to the
        // end of the font, not the table.
        int tableLength = face.GetTableLength(tag);

        Header h = cursor.ReadBigEndianStruct<Header>();

        if (h.MajorVersion != expectedMajor || h.MinorVersion != expectedMinor)
            throw new InvalidDataException(
                $"'{tag}' version is {h.MajorVersion}.{h.MinorVersion}, " +
                $"expected {expectedMajor}.{expectedMinor}.");

        if (h.NumSizes > 1024)
            throw new InvalidDataException(
                $"'{tag}' declares {h.NumSizes} strikes, exceeding the safety limit.");

        // Bound the strike array against the table's declared length before allocating.
        // Unsafe.SizeOf is a JIT constant for an unmanaged struct.
        long neededBytes = cursor.Position + (long)h.NumSizes * Unsafe.SizeOf<BitmapStrike.Header>();
        if (neededBytes > tableLength)
            throw new InvalidDataException(
                $"'{tag}' needs {neededBytes} bytes for {h.NumSizes} strikes but the " +
                $"declared table length is {tableLength}.");

        BitmapStrike[] strikes = cursor.ReadBigEndianHeaderRecordArray<BitmapStrike, BitmapStrike.Header>(
            (int)h.NumSizes, new ParentContext(cursor.Source));

        return (h.MajorVersion, h.MinorVersion, strikes);
    }

    /// <summary>The 8-byte EBLC/CBLC header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by <c>numSizes</c> 48-byte BitmapSize records.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// </list>
    /// <para>For the field layout, see the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">EBLC header</see> in the OpenType specification.</para>
    /// </remarks>
    /// <seealso cref="BitmapStrike.Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">OpenType specification: EBLC header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Major version: 2 for EBLC, 3 for CBLC.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc"><c>majorVersion</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="MinorVersion"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">OpenType specification: <c>majorVersion</c></seealso>
        public ushort MajorVersion;   // +0

        /// <summary>Minor version: 0 for both EBLC and CBLC.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc"><c>minorVersion</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="MajorVersion"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">OpenType specification: <c>minorVersion</c></seealso>
        public ushort MinorVersion;   // +2

        /// <summary>Number of BitmapSize records (strikes) in the table. The safety limit is 1024.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc"><c>numSizes</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="BitmapStrike"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">OpenType specification: <c>numSizes</c></seealso>
        public uint NumSizes;         // +4

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>The three multi-byte fields are reversed independently; there are no nested structures to recurse into. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType data types</see> for the on-disk widths.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">OpenType specification: EBLC header</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            MajorVersion = BinaryPrimitives.ReverseEndianness(v.MajorVersion),
            MinorVersion = BinaryPrimitives.ReverseEndianness(v.MinorVersion),
            NumSizes = BinaryPrimitives.ReverseEndianness(v.NumSizes),
        };
    }
}

/// <summary>One strike: the <c>BitmapSize</c> record plus its resolved <see cref="IndexSubtableList"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A strike collects every bitmap glyph that shares the same pixels-per-em dimensions. Each strike has its own set of index subtables, which are the objects that actually locate glyph data in the <c>EBDT</c>/<c>CBDT</c> table.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">EBLC, BitmapSize records</see> section of the specification.</description></item>
/// <item><description>The record implements <see cref="IBigEndianHeaderRecord{T, THeader}"/>, so its <c>Parse</c> reads a <see cref="Header"/> and forwards to <c>FromHeader</c>. The <c>IndexSubtableList</c> is resolved eagerly, using the table-scoped source carried by the <see cref="ParentContext"/>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BitmapLocationParser"/>
/// <seealso cref="IndexSubtableList"/>
/// <seealso cref="Header"/>
/// <seealso cref="SbitLineMetrics"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">OpenType specification: BitmapSize records</seealso>
public sealed record BitmapStrike : IBigEndianHeaderRecord<BitmapStrike, BitmapStrike.Header>
{
    /// <summary>Gets the strike's index subtable list.</summary>
    /// <value>The resolved list of <see cref="IndexSubtableRecord"/> entries. Populated during parse from <see cref="Header.IndexSubtableListOffset"/>.</value>
    /// <seealso cref="IndexSubtableList"/>
    /// <seealso cref="FindSubtable(ushort)"/>
    public IndexSubtableList IndexSubtableList { get; init; } = null!;

    /// <summary>Gets the color reference. Not used; set to 0.</summary>
    /// <value>Always zero in conforming fonts.</value>
    /// <seealso cref="Header.ColorRef"/>
    public uint ColorRef { get; init; }

    /// <summary>Gets the horizontal line metrics.</summary>
    /// <value>The <see cref="SbitLineMetrics"/> used when this strike is rendered in horizontal layout.</value>
    /// <seealso cref="Vert"/>
    /// <seealso cref="SbitLineMetrics"/>
    public SbitLineMetrics Hori { get; init; }

    /// <summary>Gets the vertical line metrics.</summary>
    /// <value>The <see cref="SbitLineMetrics"/> used when this strike is rendered in vertical layout.</value>
    /// <seealso cref="Hori"/>
    /// <seealso cref="SbitLineMetrics"/>
    public SbitLineMetrics Vert { get; init; }

    /// <summary>Gets the lowest glyph ID in this strike.</summary>
    /// <value>The inclusive lower bound of the glyph ID range covered by this strike.</value>
    /// <seealso cref="EndGlyphIndex"/>
    public ushort StartGlyphIndex { get; init; }

    /// <summary>Gets the highest glyph ID in this strike (inclusive).</summary>
    /// <value>The inclusive upper bound of the glyph ID range covered by this strike.</value>
    /// <seealso cref="StartGlyphIndex"/>
    public ushort EndGlyphIndex { get; init; }

    /// <summary>Gets the horizontal pixels per em.</summary>
    /// <value>The horizontal resolution of this strike, in pixels per em.</value>
    /// <seealso cref="PpemY"/>
    public byte PpemX { get; init; }

    /// <summary>Gets the vertical pixels per em.</summary>
    /// <value>The vertical resolution of this strike, in pixels per em.</value>
    /// <seealso cref="PpemX"/>
    public byte PpemY { get; init; }

    /// <summary>Gets the bit depth: 1 (black/white), 2 (4 gray levels), 4 (16 gray levels), 8 (256 gray levels), or 32 (BGRA color, CBLC only).</summary>
    /// <value>The per-pixel bit width used by bitmaps in this strike.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records"><c>bitDepth</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="IsColor"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">OpenType specification: <c>bitDepth</c></seealso>
    public byte BitDepth { get; init; }

    /// <summary>Gets the bitmap flags.</summary>
    /// <value>The <see cref="BitmapFlags"/> that indicate which layout directions the small metrics apply to.</value>
    /// <seealso cref="BitmapFlags"/>
    /// <seealso cref="SmallGlyphMetrics"/>
    public BitmapFlags Flags { get; init; }

    /// <summary>True when this is a 32-bit BGRA color strike (CBLC only).</summary>
    /// <value><see langword="true"/> when <see cref="BitDepth"/> equals 32.</value>
    /// <seealso cref="BitDepth"/>
    public bool IsColor => BitDepth == 32;

    /// <summary>Finds the index subtable that contains the specified glyph ID.</summary>
    /// <param name="glyphId">The glyph ID to search for.</param>
    /// <returns>The index subtable that contains the glyph, or <c>null</c> if not found.</returns>
    /// <remarks>The lookup is linear over <see cref="IndexSubtableList"/>. Records are sorted by <see cref="IndexSubtableRecord.FirstGlyphIndex"/> per the specification, so a binary search would also be valid; the linear form is chosen for simplicity.</remarks>
    /// <seealso cref="IndexSubtableList"/>
    /// <seealso cref="IndexSubtableRecord"/>
    public IndexSubtable? FindSubtable(ushort glyphId) => IndexSubtableList.Records.FirstOrDefault(r => r.FirstGlyphIndex <= glyphId && r.LastGlyphIndex >= glyphId)?.Subtable;

    /// <inheritdoc/>
    /// <param name="header">The already-read BitmapSize record.</param>
    /// <param name="context">A <see cref="ParentContext"/> whose <see cref="IParentContext.ParentSource"/> is the table-scoped source.</param>
    /// <returns>A new strike with its index subtable list resolved.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="ParentContext"/>.</exception>
    /// <remarks>The <see cref="Header.IndexSubtableListOffset"/> is measured from the table start; <see cref="Source.ParseRecordAt{T}(long, object?)"/> is invoked on the table-scoped source so the offset resolves correctly.</remarks>
    /// <seealso cref="IndexSubtableList"/>
    /// <seealso cref="ParentContext"/>
    static BitmapStrike IHeaderRecord<BitmapStrike, Header>.FromHeader(in Header header, object? context) => new()
    {
        IndexSubtableList = ((ParentContext)context!).ParentSource.ParseRecordAt<IndexSubtableList>(header.IndexSubtableListOffset, new IndexSubtableList.Context(header.NumberOfIndexSubtables)),
        ColorRef = header.ColorRef,
        Hori = header.Hori,
        Vert = header.Vert,
        StartGlyphIndex = header.StartGlyphIndex,
        EndGlyphIndex = header.EndGlyphIndex,
        PpemX = header.PpemX,
        PpemY = header.PpemY,
        BitDepth = header.BitDepth,
        Flags = header.Flags,
    };

    /// <summary>The 48-byte BitmapSize record. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All offsets in the record are relative to the start of the enclosing EBLC/CBLC table.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// </list>
    /// <para>For the field layout, see the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">EBLC, BitmapSize records</see> in the OpenType specification.</para>
    /// </remarks>
    /// <seealso cref="BitmapStrike"/>
    /// <seealso cref="BitmapLocationParser.Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">OpenType specification: BitmapSize records</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Offset to the IndexSubtableList from the beginning of EBLC/CBLC.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records"><c>indexSubTableArrayOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="IndexSubtableListSize"/>
        /// <seealso cref="NumberOfIndexSubtables"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">OpenType specification: <c>indexSubTableArrayOffset</c></seealso>
        public uint IndexSubtableListOffset;

        /// <summary>Total size in bytes of the IndexSubtableList, including its array of IndexSubtables.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records"><c>indexTablesSize</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="IndexSubtableListOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">OpenType specification: <c>indexTablesSize</c></seealso>
        public uint IndexSubtableListSize;

        /// <summary>Number of IndexSubtable records in the IndexSubtableList.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records"><c>numberOfIndexSubTables</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="IndexSubtableListOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">OpenType specification: <c>numberOfIndexSubTables</c></seealso>
        public uint NumberOfIndexSubtables;

        /// <summary>Not used; set to 0.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records"><c>colorRef</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">OpenType specification: <c>colorRef</c></seealso>
        public uint ColorRef;

        /// <summary>Line metrics for text rendered horizontally.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#sbitlinemetrics"><c>hori</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Vert"/>
        /// <seealso cref="SbitLineMetrics"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#sbitlinemetrics">OpenType specification: <c>sbitLineMetrics</c></seealso>
        public SbitLineMetrics Hori;

        /// <summary>Line metrics for text rendered vertically.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#sbitlinemetrics"><c>vert</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Hori"/>
        /// <seealso cref="SbitLineMetrics"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#sbitlinemetrics">OpenType specification: <c>sbitLineMetrics</c></seealso>
        public SbitLineMetrics Vert;

        /// <summary>Lowest glyph ID in this strike.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records"><c>startGlyphIndex</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="EndGlyphIndex"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">OpenType specification: <c>startGlyphIndex</c></seealso>
        public ushort StartGlyphIndex;

        /// <summary>Highest glyph ID in this strike (inclusive).</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records"><c>endGlyphIndex</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="StartGlyphIndex"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">OpenType specification: <c>endGlyphIndex</c></seealso>
        public ushort EndGlyphIndex;

        /// <summary>Horizontal pixels per em.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records"><c>ppemX</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="PpemY"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">OpenType specification: <c>ppemX</c></seealso>
        public byte PpemX;

        /// <summary>Vertical pixels per em.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records"><c>ppemY</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="PpemX"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">OpenType specification: <c>ppemY</c></seealso>
        public byte PpemY;

        /// <summary>Bit depth: 1 (black/white), 2 (4 gray levels), 4 (16 gray levels), 8 (256 gray levels), or 32 (BGRA color, CBLC only).</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records"><c>bitDepth</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="BitmapStrike.IsColor"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">OpenType specification: <c>bitDepth</c></seealso>
        public byte BitDepth;

        /// <summary>Bitmap flags. Bit 0 = HORIZONTAL_METRICS; bit 1 = VERTICAL_METRICS.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records"><c>flags</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="BitmapFlags"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">OpenType specification: <c>flags</c></seealso>
        public BitmapFlags Flags;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>Byte-width fields are copied through unchanged; only the multi-byte fields are reversed.</description></item>
        /// <item><description>The nested <see cref="SbitLineMetrics"/> fields are already byte-only and are copied through.</description></item>
        /// </list>
        /// </remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">OpenType specification: BitmapSize records</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            IndexSubtableListOffset = BinaryPrimitives.ReverseEndianness(v.IndexSubtableListOffset),
            IndexSubtableListSize = BinaryPrimitives.ReverseEndianness(v.IndexSubtableListSize),
            NumberOfIndexSubtables = BinaryPrimitives.ReverseEndianness(v.NumberOfIndexSubtables),
            ColorRef = BinaryPrimitives.ReverseEndianness(v.ColorRef),
            Hori = v.Hori,
            Vert = v.Vert,
            StartGlyphIndex = BinaryPrimitives.ReverseEndianness(v.StartGlyphIndex),
            EndGlyphIndex = BinaryPrimitives.ReverseEndianness(v.EndGlyphIndex),
            PpemX = v.PpemX,
            PpemY = v.PpemY,
            BitDepth = v.BitDepth,
            Flags = v.Flags,
        };
    }
}

/// <summary>Bitmap flags in the BitmapSize record's <c>flags</c> field. The on-disk type is <c>int8</c>; this enum mirrors it.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>At least one of <see cref="HorizontalMetrics"/> or <see cref="VerticalMetrics"/> must be set for a strike to be usable.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records"><c>flags</c> field</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BitmapStrike.Flags"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#bitmapSize-records">OpenType specification: <c>flags</c></seealso>
[Flags]
public enum BitmapFlags : sbyte
{
    /// <summary>No flags set.</summary>
    /// <remarks>Invalid per the specification: a strike with this value declares no layout direction.</remarks>
    None = 0,

    /// <summary>Small metrics in this strike apply to horizontal layout.</summary>
    HorizontalMetrics = 0x01,

    /// <summary>Small metrics in this strike apply to vertical layout.</summary>
    VerticalMetrics = 0x02,

    /// <summary>Reserved bits 2–7. Must be zero.</summary>
    Reserved = unchecked((sbyte)0xFC),
}

/// <summary>The IndexSubtableList for one strike: an array of <see cref="IndexSubtableRecord"/> entries, each covering a glyph ID range.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The list is addressed by an offset relative to the EBLC/CBLC table start, and the entries' offsets into their subtables are relative to the list's start. The parse path re-bases the source accordingly.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtablearray">EBLC, <c>indexSubTableArray</c></see> section of the specification.</description></item>
/// <item><description>The record count is not stored in the list itself; it comes from the enclosing <see cref="BitmapStrike.Header.NumberOfIndexSubtables"/> via <see cref="Context"/>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BitmapStrike"/>
/// <seealso cref="IndexSubtableRecord"/>
/// <seealso cref="Context"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtablearray">OpenType specification: <c>indexSubTableArray</c></seealso>
public sealed record IndexSubtableList : IRecord<IndexSubtableList>
{
    /// <summary>Gets the index subtable records.</summary>
    /// <value>The list of records, one per covered glyph range. Entries are sorted by <see cref="IndexSubtableRecord.FirstGlyphIndex"/> and must not overlap.</value>
    /// <seealso cref="IndexSubtableRecord"/>
    public IReadOnlyList<IndexSubtableRecord> Records { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the list.</param>
    /// <param name="context">A <see cref="Context"/> carrying the number of records.</param>
    /// <returns>The parsed list.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="Context"/>.</exception>
    /// <exception cref="EndOfStreamException">The list extends past the end of the table-scoped source.</exception>
    /// <remarks>Each entry's <c>indexSubTableOffset</c> is interpreted relative to the start of this list, which is why the parse passes <c>cursor.Source</c> (list-scoped) as the parent context for the individual records.</remarks>
    /// <seealso cref="Context"/>
    /// <seealso cref="IndexSubtableRecord"/>
    static IndexSubtableList IRecord<IndexSubtableList>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
            throw new InvalidOperationException(
                $"{nameof(IndexSubtableList)}.Parse requires a {nameof(Context)} " +
                "carrying the number of records.");

        return new IndexSubtableList
        {
            Records = cursor.ReadBigEndianHeaderRecordArray<IndexSubtableRecord, IndexSubtableRecord.Header>(checked((int)ctx.NumberOfIndexSubtables), new ParentContext(cursor.Source)),
        };
    }

    /// <summary>Context carrying the number of IndexSubtable records, from the enclosing BitmapSize record's <c>numberOfIndexSubtables</c> field.</summary>
    /// <param name="NumberOfIndexSubtables">The number of <see cref="IndexSubtableRecord"/> entries to read.</param>
    /// <remarks>The count is stored in the enclosing strike rather than the list, so it must be threaded through the parse context.</remarks>
    /// <seealso cref="BitmapStrike.Header.NumberOfIndexSubtables"/>
    /// <seealso cref="IndexSubtableRecord"/>
    public record Context(uint NumberOfIndexSubtables);
}

/// <summary>An IndexSubtable plus its glyph ID range.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record pairs an <see cref="IndexSubtable"/> with the contiguous glyph ID range it covers. The subtable's own <c>FirstGlyphIndex</c>/<c>LastGlyphIndex</c> properties are populated from this record's header, not from the subtable's own data.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtablearray">EBLC, <c>indexSubTableArray</c></see> section of the specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IndexSubtable"/>
/// <seealso cref="IndexSubtableList"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtablearray">OpenType specification: <c>indexSubTableArray</c></seealso>
public sealed record IndexSubtableRecord : IBigEndianHeaderRecord<IndexSubtableRecord, IndexSubtableRecord.Header>
{
    /// <summary>Gets the first glyph ID covered by this entry.</summary>
    /// <value>The inclusive lower bound of the range.</value>
    /// <seealso cref="LastGlyphIndex"/>
    /// <seealso cref="Subtable"/>
    public ushort FirstGlyphIndex { get; init; }

    /// <summary>Gets the last glyph ID covered by this entry (inclusive).</summary>
    /// <value>The inclusive upper bound of the range.</value>
    /// <seealso cref="FirstGlyphIndex"/>
    /// <seealso cref="Subtable"/>
    public ushort LastGlyphIndex { get; init; }

    /// <summary>Gets the parsed subtable.</summary>
    /// <value>The <see cref="IndexSubtable"/> whose format-specific data locates bitmaps for glyphs in <see cref="FirstGlyphIndex"/>..<see cref="LastGlyphIndex"/>.</value>
    /// <seealso cref="IndexSubtable"/>
    public IndexSubtable Subtable { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="header">The already-read record header.</param>
    /// <param name="context">A <see cref="ParentContext"/> whose <see cref="IParentContext.ParentSource"/> is the list-scoped source.</param>
    /// <returns>A new record with its subtable resolved.</returns>
    /// <exception cref="InvalidDataException">The subtable's format is not 1–5.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>IndexSubtableOffset</c> is relative to the start of the IndexSubtableList, not to the EBLC/CBLC table. The <see cref="ParentContext"/> carries the list-scoped source, so the offset resolves correctly.</description></item>
    /// <item><description>The subtable context carries the record's own glyph range so that the subtable can populate its <c>FirstGlyphIndex</c>/<c>LastGlyphIndex</c> properties.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IndexSubtable"/>
    /// <seealso cref="IndexSubtableContext"/>
    /// <seealso cref="ParentContext"/>
    static IndexSubtableRecord IHeaderRecord<IndexSubtableRecord, Header>.FromHeader(
        in Header header, object? context) => new()
        {
            FirstGlyphIndex = header.FirstGlyphIndex,
            LastGlyphIndex = header.LastGlyphIndex,
            // IndexSubtableOffset is relative to the start of the IndexSubtableList.
            // ParentSource is list-scoped because ParseRecordAt re-based the source
            // when IndexSubtableList.Parse was invoked.
            Subtable = ((ParentContext)context!).ParentSource.ParseRecordAt<IndexSubtable>(
            header.IndexSubtableOffset,
            new IndexSubtableContext(header.FirstGlyphIndex, header.LastGlyphIndex)),
        };

    /// <summary>An entry in an IndexSubtableList. Blittable, size 8.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><c>IndexSubtableOffset</c> is relative to the start of the IndexSubtableList, not to the containing EBLC/CBLC table.</description></item>
    /// <item><description>Records are sorted by <c>FirstGlyphIndex</c> and must not overlap.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// </list>
    /// <para>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtablearray">EBLC, <c>indexSubTableArray</c></see> in the OpenType specification.</para>
    /// </remarks>
    /// <seealso cref="IndexSubtableRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtablearray">OpenType specification: <c>indexSubTableArray</c></seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>First glyph ID in the covered range.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtablearray"><c>firstGlyphIndex</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="LastGlyphIndex"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtablearray">OpenType specification: <c>firstGlyphIndex</c></seealso>
        public ushort FirstGlyphIndex;

        /// <summary>Last glyph ID in the covered range (inclusive).</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtablearray"><c>lastGlyphIndex</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="FirstGlyphIndex"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtablearray">OpenType specification: <c>lastGlyphIndex</c></seealso>
        public ushort LastGlyphIndex;

        /// <summary>Offset to the IndexSubtable from the start of the IndexSubtableList.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtablearray"><c>additionalOffsetToIndexSubtable</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="IndexSubtable"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtablearray">OpenType specification: <c>additionalOffsetToIndexSubtable</c></seealso>
        public uint IndexSubtableOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtablearray">OpenType specification: <c>indexSubTableArray</c></seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            FirstGlyphIndex = BinaryPrimitives.ReverseEndianness(v.FirstGlyphIndex),
            LastGlyphIndex = BinaryPrimitives.ReverseEndianness(v.LastGlyphIndex),
            IndexSubtableOffset = BinaryPrimitives.ReverseEndianness(v.IndexSubtableOffset),
        };
    }
}

/// <summary>Line metrics for a bitmap strike. Blittable, size 12.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>All fields are single bytes.</description></item>
/// <item><description>The <c>ascender</c> and <c>descender</c> describe the strike's vertical extent.</description></item>
/// <item><description>The caret slope fields determine the angle at which the caret is drawn.</description></item>
/// <item><description>The <c>minOriginSB</c>, <c>minAdvanceSB</c>, <c>maxBeforeBL</c>, and <c>minAfterBL</c> values are available to applications that pre-allocate buffers.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#sbitlinemetrics"><c>sbitLineMetrics</c></see> section of the specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BitmapStrike.Hori"/>
/// <seealso cref="BitmapStrike.Vert"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#sbitlinemetrics">OpenType specification: <c>sbitLineMetrics</c></seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct SbitLineMetrics
{
    /// <summary>Ascender in pixels.</summary>
    /// <value>The distance from the baseline to the top of the strike's em box, measured upward.</value>
    /// <seealso cref="Descender"/>
    public sbyte Ascender;

    /// <summary>Descender in pixels.</summary>
    /// <value>The distance from the baseline to the bottom of the strike's em box, measured downward. Typically negative.</value>
    /// <seealso cref="Ascender"/>
    public sbyte Descender;

    /// <summary>Maximum width in pixels.</summary>
    /// <value>The widest advance in this strike, in pixels.</value>
    /// <seealso cref="MinAdvanceSB"/>
    public byte WidthMax;

    /// <summary>Caret slope numerator.</summary>
    /// <value>The numerator of the caret slope ratio.</value>
    /// <seealso cref="CaretSlopeDenominator"/>
    public sbyte CaretSlopeNumerator;

    /// <summary>Caret slope denominator.</summary>
    /// <value>The denominator of the caret slope ratio.</value>
    /// <seealso cref="CaretSlopeNumerator"/>
    public sbyte CaretSlopeDenominator;

    /// <summary>Caret offset in pixels.</summary>
    /// <value>The horizontal offset of the caret from the insertion point.</value>
    /// <seealso cref="CaretSlopeNumerator"/>
    public sbyte CaretOffset;

    /// <summary>Minimum origin side bearing.</summary>
    /// <value>The smallest signed horizontal offset from the glyph origin to the left edge of any bitmap in this strike.</value>
    /// <seealso cref="MinAdvanceSB"/>
    public sbyte MinOriginSB;

    /// <summary>Minimum advance side bearing.</summary>
    /// <value>The smallest signed horizontal offset from the right edge of any bitmap to the pen position after advance.</value>
    /// <seealso cref="MinOriginSB"/>
    public sbyte MinAdvanceSB;

    /// <summary>Maximum before baseline.</summary>
    /// <value>The largest upward extent above the baseline for any bitmap in this strike.</value>
    /// <seealso cref="MinAfterBL"/>
    public sbyte MaxBeforeBL;

    /// <summary>Minimum after baseline.</summary>
    /// <value>The largest downward extent below the baseline for any bitmap in this strike. Typically negative.</value>
    /// <seealso cref="MaxBeforeBL"/>
    public sbyte MinAfterBL;

    /// <summary>Padding byte 1.</summary>
    /// <value>Always zero in conforming fonts; present to align the struct to 12 bytes.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#sbitlinemetrics"><c>pad1</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Pad2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#sbitlinemetrics">OpenType specification: <c>pad1</c></seealso>
    public sbyte Pad1;

    /// <summary>Padding byte 2.</summary>
    /// <value>Always zero in conforming fonts; present to align the struct to 12 bytes.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#sbitlinemetrics"><c>pad2</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Pad1"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#sbitlinemetrics">OpenType specification: <c>pad2</c></seealso>
    public sbyte Pad2;
}

/// <summary>Base class for the five IndexSubtable formats. An IndexSubtable provides the locations of bitmap data for a contiguous or sparse glyph ID range within a strike.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The 8-byte <c>IndexSubHeader</c> is common to every format and is consumed by the dispatcher.</description></item>
/// <item><description>Derived formats continue from the byte after it. The glyph range (<see cref="FirstGlyphIndex"/>, <see cref="LastGlyphIndex"/>) is supplied by the enclosing <see cref="IndexSubtableRecord"/> via <see cref="IndexSubtableContext"/>; format 4 and format 5 derive it from their own data instead.</description></item>
/// <item><description>The five formats trade off index array width against the assumption that glyph IDs and image sizes are contiguous: formats 1 and 3 use per-glyph offset arrays, format 2 uses a single image size for a contiguous range, and formats 4 and 5 support sparse glyph ID sets.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable-formats">EBLC, IndexSubTable formats</see> section of the specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IndexSubtableFormat1"/>
/// <seealso cref="IndexSubtableFormat2"/>
/// <seealso cref="IndexSubtableFormat3"/>
/// <seealso cref="IndexSubtableFormat4"/>
/// <seealso cref="IndexSubtableFormat5"/>
/// <seealso cref="IndexSubHeader"/>
/// <seealso cref="IndexSubtableContext"/>
/// <seealso cref="IBaseRecord{TBase}"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable-formats">OpenType specification: IndexSubTable formats</seealso>
public abstract record IndexSubtable : IRecord<IndexSubtable>, IBaseRecord<IndexSubtable>
{
    /// <summary>Gets the IndexSubtable format (1–5).</summary>
    /// <value>The format discriminant from <see cref="IndexSubHeader.IndexFormat"/>.</value>
    /// <seealso cref="IndexSubHeader.IndexFormat"/>
    public ushort IndexFormat { get; init; }

    /// <summary>Gets the EBDT/CBDT image format used by every glyph in the range.</summary>
    /// <value>The image format number (1–9 for EBDT, 17–19 for CBDT), carried through from <see cref="IndexSubHeader.ImageFormat"/>.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">EBDT image formats</see> in the OpenType specification.</remarks>
    /// <seealso cref="IndexSubHeader.ImageFormat"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">OpenType specification: EBDT image formats</seealso>
    public ushort ImageFormat { get; init; }

    /// <summary>Gets the offset from the EBDT/CBDT table start to the first glyph's data.</summary>
    /// <value>The absolute byte offset of the first glyph bitmap in the EBDT/CBDT table.</value>
    /// <seealso cref="GetGlyphDataOffset(int)"/>
    public uint ImageDataOffset { get; init; }

    /// <summary>Gets the first glyph ID in this subtable's range.</summary>
    /// <value>The inclusive lower bound of the range, from the enclosing <see cref="IndexSubtableRecord"/>.</value>
    /// <seealso cref="LastGlyphIndex"/>
    public ushort FirstGlyphIndex { get; init; }

    /// <summary>Gets the last glyph ID in this subtable's range (inclusive).</summary>
    /// <value>The inclusive upper bound of the range, from the enclosing <see cref="IndexSubtableRecord"/>.</value>
    /// <seealso cref="FirstGlyphIndex"/>
    public ushort LastGlyphIndex { get; init; }

    /// <summary>Returns the absolute offset within the EBDT/CBDT table of <paramref name="glyphId"/>'s bitmap data, or <c>-1</c> when the glyph is absent from this subtable.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The absolute byte offset of the glyph's bitmap data, or <c>-1</c> when the glyph is not present in this subtable.</returns>
    /// <remarks>The offset is relative to the EBDT/CBDT table start. Formats that use per-glyph offset arrays add <see cref="ImageDataOffset"/> to the array entry; formats that use a fixed image size compute the offset arithmetically.</remarks>
    /// <seealso cref="GetGlyphDataLength(int)"/>
    /// <seealso cref="ImageDataOffset"/>
    public abstract long GetGlyphDataOffset(int glyphId);

    /// <summary>Returns the byte length of <paramref name="glyphId"/>'s bitmap data, or <c>0</c> when the glyph is absent.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The byte length of the glyph's bitmap data, or <c>0</c> when the glyph is not present in this subtable.</returns>
    /// <seealso cref="GetGlyphDataOffset(int)"/>
    public abstract int GetGlyphDataLength(int glyphId);

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the shared <see cref="IndexSubHeader"/>.</param>
    /// <param name="context">An <see cref="IndexSubtableContext"/> carrying the glyph range declared by the enclosing record.</param>
    /// <returns>The format-specific subtable.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not an <see cref="IndexSubtableContext"/>.</exception>
    /// <exception cref="InvalidDataException">The format discriminant is not in 1–5.</exception>
    /// <remarks>The dispatcher consumes the shared <see cref="IndexSubHeader"/> and forwards the remaining cursor to the format-specific parser through <see cref="IBaseRecord{TBase}.Parse{TDerived}(ref Cursor, object?)"/>.</remarks>
    /// <seealso cref="IndexSubtableFormat1"/>
    /// <seealso cref="IndexSubtableFormat2"/>
    /// <seealso cref="IndexSubtableFormat3"/>
    /// <seealso cref="IndexSubtableFormat4"/>
    /// <seealso cref="IndexSubtableFormat5"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable-formats">OpenType specification: IndexSubTable formats</seealso>
    static IndexSubtable IRecord<IndexSubtable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not IndexSubtableContext ctx)
            throw new InvalidOperationException(
                $"{nameof(IndexSubtable)}.Parse requires an {nameof(IndexSubtableContext)} context.");

        IndexSubHeader header = cursor.ReadBigEndianStruct<IndexSubHeader>();

        var formatContext = new IndexSubtableFormatContext(
            ctx.FirstGlyphIndex, ctx.LastGlyphIndex, header);

        return header.IndexFormat switch
        {
            1 => IBaseRecord<IndexSubtable>.Parse<IndexSubtableFormat1>(ref cursor, formatContext),
            2 => IBaseRecord<IndexSubtable>.Parse<IndexSubtableFormat2>(ref cursor, formatContext),
            3 => IBaseRecord<IndexSubtable>.Parse<IndexSubtableFormat3>(ref cursor, formatContext),
            4 => IBaseRecord<IndexSubtable>.Parse<IndexSubtableFormat4>(ref cursor, formatContext),
            5 => IBaseRecord<IndexSubtable>.Parse<IndexSubtableFormat5>(ref cursor, formatContext),
            _ => throw new InvalidDataException(
                $"IndexSubtable format {header.IndexFormat} is not defined (expected 1–5)."),
        };
    }
}

/// <summary>The common header at the start of every IndexSubtable format. Blittable, size 8.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><c>ImageDataOffset</c> is the offset from the beginning of the EBDT/CBDT table to the image data for the covered glyph range.</description></item>
/// <item><description>The header is followed by format-specific data whose layout is determined by <see cref="IndexFormat"/>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubheader"><c>indexSubHeader</c></see> section of the specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IndexSubtable"/>
/// <seealso cref="IndexSubtableFormat1"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubheader">OpenType specification: <c>indexSubHeader</c></seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct IndexSubHeader : IBigEndianStruct<IndexSubHeader>
{
    /// <summary>Format of the IndexSubtable: 1, 2, 3, 4, or 5.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubheader"><c>indexFormat</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="ImageFormat"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubheader">OpenType specification: <c>indexFormat</c></seealso>
    public ushort IndexFormat;

    /// <summary>Format of the EBDT/CBDT image data: 1–9 (EBDT) or 17–19 (CBDT).</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubheader"><c>imageFormat</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="IndexFormat"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubheader">OpenType specification: <c>imageFormat</c></seealso>
    public ushort ImageFormat;

    /// <summary>Offset to the image data in the EBDT/CBDT table.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubheader"><c>imageDataOffset</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="IndexSubtable.ImageDataOffset"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubheader">OpenType specification: <c>imageDataOffset</c></seealso>
    public uint ImageDataOffset;

    /// <inheritdoc/>
    /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new header with each multi-byte field reversed.</returns>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubheader">OpenType specification: <c>indexSubHeader</c></seealso>
    public static IndexSubHeader ReverseEndianness(IndexSubHeader value) => new()
    {
        IndexFormat = BinaryPrimitives.ReverseEndianness(value.IndexFormat),
        ImageFormat = BinaryPrimitives.ReverseEndianness(value.ImageFormat),
        ImageDataOffset = BinaryPrimitives.ReverseEndianness(value.ImageDataOffset)
    };
}

/// <summary>Context supplied by the enclosing <see cref="IndexSubtableRecord"/> to <see cref="IRecord{T}.Parse"/>. Carries the glyph ID range that the record declares.</summary>
/// <param name="FirstGlyphIndex">First glyph ID covered by the record.</param>
/// <param name="LastGlyphIndex">Last glyph ID covered by the record (inclusive).</param>
/// <remarks>The context is a record type, so it is a reference type and passing it through <see cref="IRecord{T}.Parse"/> does not copy the range.</remarks>
/// <seealso cref="IndexSubtable"/>
/// <seealso cref="IndexSubtableRecord"/>
public record IndexSubtableContext(ushort FirstGlyphIndex, ushort LastGlyphIndex);

/// <summary>Context threaded from the dispatcher to each format after the shared <c>IndexSubHeader</c> has been consumed. Carries the record's glyph range plus the header fields the format needs to set on its base properties.</summary>
/// <param name="FirstGlyphIndex">First glyph ID covered by the record.</param>
/// <param name="LastGlyphIndex">Last glyph ID covered by the record (inclusive).</param>
/// <param name="Header">The already-read shared <see cref="IndexSubHeader"/>.</param>
/// <remarks>The dispatcher reads the header once and forwards it, so each format implementation does not re-read the shared prefix.</remarks>
/// <seealso cref="IndexSubHeader"/>
/// <seealso cref="IndexSubtable"/>
public record IndexSubtableFormatContext(
    ushort FirstGlyphIndex,
    ushort LastGlyphIndex,
    IndexSubHeader Header);

// ═══════════════════════════════════════════════════════════════════════════
// Format 1 — per-glyph Offset32 array
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>IndexSubtable format 1: per-glyph <c>Offset32</c> array. Glyphs have variable metrics; data is 4-byte aligned within the EBDT table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The offset array has one entry per glyph in the range plus a sentinel entry at the end; a glyph's data length is the difference between its entry and the next.</description></item>
/// <item><description>Used when glyphs in the range have varying image sizes and the strike is large enough that 16-bit offsets would overflow.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable1">EBLC, IndexSubTable1</see> section of the specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IndexSubtable"/>
/// <seealso cref="IndexSubtableFormat3"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable1">OpenType specification: IndexSubTable1</seealso>
public sealed record IndexSubtableFormat1
    : IndexSubtable,
      IDerivedRecord<IndexSubtable, IndexSubtableFormat1>
{
    /// <summary>Offsets relative to <see cref="IndexSubtable.ImageDataOffset"/>; one sentinel at the end.</summary>
    /// <value>The per-glyph offset array, with <c>LastGlyphIndex - FirstGlyphIndex + 2</c> entries; the final entry bounds the last glyph's data.</value>
    /// <seealso cref="GetGlyphDataOffset(int)"/>
    /// <seealso cref="GetGlyphDataLength(int)"/>
    public IReadOnlyList<uint> SbitOffsets { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the shared <see cref="IndexSubHeader"/>.</param>
    /// <param name="context">An <see cref="IndexSubtableFormatContext"/> carrying the range and the already-read header.</param>
    /// <returns>The parsed format-1 subtable.</returns>
    /// <exception cref="EndOfStreamException">The offset array extends past the end of the table-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable1">IndexSubTable1 layout</see> in the OpenType specification.</remarks>
    /// <seealso cref="IndexSubtableFormatContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable1">OpenType specification: IndexSubTable1</seealso>
    static IndexSubtableFormat1 IDerivedRecord<IndexSubtable, IndexSubtableFormat1>.Parse(
        ref Cursor cursor, object? context)
    {
        var ctx = (IndexSubtableFormatContext)context!;

        // numOffsets = lastGlyphIndex - firstGlyphIndex + 2 (one per glyph plus sentinel).
        int numOffsets = ctx.LastGlyphIndex - ctx.FirstGlyphIndex + 2;
        uint[] offsets = cursor.ReadUInt32Array(numOffsets);

        return new IndexSubtableFormat1
        {
            IndexFormat = ctx.Header.IndexFormat,
            ImageFormat = ctx.Header.ImageFormat,
            ImageDataOffset = ctx.Header.ImageDataOffset,
            FirstGlyphIndex = ctx.FirstGlyphIndex,
            LastGlyphIndex = ctx.LastGlyphIndex,
            SbitOffsets = offsets,
        };
    }

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The absolute offset within the EBDT/CBDT table, or <c>-1</c> when the glyph is outside the range.</returns>
    /// <seealso cref="GetGlyphDataLength(int)"/>
    public override long GetGlyphDataOffset(int glyphId)
    {
        int i = glyphId - FirstGlyphIndex;
        if ((uint)i >= (uint)(SbitOffsets.Count - 1)) return -1;
        return ImageDataOffset + SbitOffsets[i];
    }

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The byte length of the glyph's bitmap data, or <c>0</c> when the glyph is outside the range.</returns>
    /// <seealso cref="GetGlyphDataOffset(int)"/>
    public override int GetGlyphDataLength(int glyphId)
    {
        int i = glyphId - FirstGlyphIndex;
        if ((uint)i >= (uint)(SbitOffsets.Count - 1)) return 0;
        return (int)(SbitOffsets[i + 1] - SbitOffsets[i]);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 2 — fixed image size, shared metrics
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>IndexSubtable format 2: fixed image size and shared metrics for every glyph in the range. Saves space in fonts where all glyphs in a range have identical metrics.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The image size and metrics are stored once, and every glyph's data is located arithmetically: <c>ImageDataOffset + (glyphId - FirstGlyphIndex) * ImageSize</c>.</description></item>
/// <item><description>Used for ranges of monospaced bitmap glyphs, such as the CJK ideographs of a monospaced bitmap font.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable2">EBLC, IndexSubTable2</see> section of the specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IndexSubtable"/>
/// <seealso cref="BigGlyphMetrics"/>
/// <seealso cref="IndexSubtableFormat5"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable2">OpenType specification: IndexSubTable2</seealso>
public sealed record IndexSubtableFormat2
    : IndexSubtable,
      IDerivedRecord<IndexSubtable, IndexSubtableFormat2>
{
    /// <summary>Byte length of every glyph's data.</summary>
    /// <value>The constant image size shared by all glyphs in the range.</value>
    /// <seealso cref="GetGlyphDataLength(int)"/>
    public uint ImageSize { get; init; }

    /// <summary>Metrics shared by every glyph in the range.</summary>
    /// <value>The <see cref="BigGlyphMetrics"/> that describe every glyph bitmap in this subtable.</value>
    /// <seealso cref="BigGlyphMetrics"/>
    public BigGlyphMetrics BigMetrics { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the shared <see cref="IndexSubHeader"/>.</param>
    /// <param name="context">An <see cref="IndexSubtableFormatContext"/> carrying the range and the already-read header.</param>
    /// <returns>The parsed format-2 subtable.</returns>
    /// <exception cref="EndOfStreamException">The image size or metrics extend past the end of the table-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable2">IndexSubTable2 layout</see> in the OpenType specification.</remarks>
    /// <seealso cref="IndexSubtableFormatContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable2">OpenType specification: IndexSubTable2</seealso>
    static IndexSubtableFormat2 IDerivedRecord<IndexSubtable, IndexSubtableFormat2>.Parse(
        ref Cursor cursor, object? context)
    {
        var ctx = (IndexSubtableFormatContext)context!;
        uint imageSize = cursor.ReadUInt32();
        BigGlyphMetrics metrics = cursor.ReadStruct<BigGlyphMetrics>();

        return new IndexSubtableFormat2
        {
            IndexFormat = ctx.Header.IndexFormat,
            ImageFormat = ctx.Header.ImageFormat,
            ImageDataOffset = ctx.Header.ImageDataOffset,
            FirstGlyphIndex = ctx.FirstGlyphIndex,
            LastGlyphIndex = ctx.LastGlyphIndex,
            ImageSize = imageSize,
            BigMetrics = metrics,
        };
    }

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The absolute offset within the EBDT/CBDT table, or <c>-1</c> when the glyph is outside the range.</returns>
    /// <remarks>The offset is computed arithmetically from the glyph's ordinal position within the range, without a lookup table.</remarks>
    /// <seealso cref="GetGlyphDataLength(int)"/>
    public override long GetGlyphDataOffset(int glyphId)
    {
        int i = glyphId - FirstGlyphIndex;
        if (i < 0 || glyphId > LastGlyphIndex) return -1;
        return ImageDataOffset + (long)i * ImageSize;
    }

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns><see cref="ImageSize"/> when the glyph is within the range, or <c>0</c> otherwise.</returns>
    /// <seealso cref="ImageSize"/>
    /// <seealso cref="GetGlyphDataOffset(int)"/>
    public override int GetGlyphDataLength(int glyphId) =>
        glyphId >= FirstGlyphIndex && glyphId <= LastGlyphIndex ? (int)ImageSize : 0;
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 3 — per-glyph Offset16 array
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>IndexSubtable format 3: per-glyph <c>Offset16</c> array. Same use case as format 1 but for strikes under 64 KB. Data is 2-byte aligned.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The offset array has one entry per glyph in the range plus a sentinel entry at the end; a glyph's data length is the difference between its entry and the next.</description></item>
/// <item><description>Offsets are <see cref="ushort"/>; the strike's total image data must fit in 64 KB from <see cref="IndexSubtable.ImageDataOffset"/>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable3">EBLC, IndexSubTable3</see> section of the specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IndexSubtable"/>
/// <seealso cref="IndexSubtableFormat1"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable3">OpenType specification: IndexSubTable3</seealso>
public sealed record IndexSubtableFormat3
    : IndexSubtable,
      IDerivedRecord<IndexSubtable, IndexSubtableFormat3>
{
    /// <summary>Offsets relative to <see cref="IndexSubtable.ImageDataOffset"/>; one sentinel at the end.</summary>
    /// <value>The per-glyph offset array, with <c>LastGlyphIndex - FirstGlyphIndex + 2</c> entries; the final entry bounds the last glyph's data.</value>
    /// <seealso cref="GetGlyphDataOffset(int)"/>
    /// <seealso cref="GetGlyphDataLength(int)"/>
    public IReadOnlyList<ushort> SbitOffsets { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the shared <see cref="IndexSubHeader"/>.</param>
    /// <param name="context">An <see cref="IndexSubtableFormatContext"/> carrying the range and the already-read header.</param>
    /// <returns>The parsed format-3 subtable.</returns>
    /// <exception cref="EndOfStreamException">The offset array extends past the end of the table-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable3">IndexSubTable3 layout</see> in the OpenType specification.</remarks>
    /// <seealso cref="IndexSubtableFormatContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable3">OpenType specification: IndexSubTable3</seealso>
    static IndexSubtableFormat3 IDerivedRecord<IndexSubtable, IndexSubtableFormat3>.Parse(
        ref Cursor cursor, object? context)
    {
        var ctx = (IndexSubtableFormatContext)context!;
        int numOffsets = ctx.LastGlyphIndex - ctx.FirstGlyphIndex + 2;
        ushort[] offsets = cursor.ReadUInt16Array(numOffsets);

        return new IndexSubtableFormat3
        {
            IndexFormat = ctx.Header.IndexFormat,
            ImageFormat = ctx.Header.ImageFormat,
            ImageDataOffset = ctx.Header.ImageDataOffset,
            FirstGlyphIndex = ctx.FirstGlyphIndex,
            LastGlyphIndex = ctx.LastGlyphIndex,
            SbitOffsets = offsets,
        };
    }

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The absolute offset within the EBDT/CBDT table, or <c>-1</c> when the glyph is outside the range.</returns>
    /// <seealso cref="GetGlyphDataLength(int)"/>
    public override long GetGlyphDataOffset(int glyphId)
    {
        int i = glyphId - FirstGlyphIndex;
        if ((uint)i >= (uint)(SbitOffsets.Count - 1)) return -1;
        return ImageDataOffset + SbitOffsets[i];
    }

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The byte length of the glyph's bitmap data, or <c>0</c> when the glyph is outside the range.</returns>
    /// <seealso cref="GetGlyphDataOffset(int)"/>
    public override int GetGlyphDataLength(int glyphId)
    {
        int i = glyphId - FirstGlyphIndex;
        if ((uint)i >= (uint)(SbitOffsets.Count - 1)) return 0;
        return SbitOffsets[i + 1] - SbitOffsets[i];
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 4 — sparse glyph IDs, per-glyph Offset16
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>IndexSubtable format 4: sparse glyph IDs with per-glyph <c>Offset16</c> entries. Used when glyph IDs are not contiguous.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The subtable stores an explicit (glyphID, offset) pair for every glyph; the parser derives <c>FirstGlyphIndex</c> and <c>LastGlyphIndex</c> from the first and last pairs.</description></item>
/// <item><description>A sentinel entry follows the last real glyph so the length of the last glyph's data can be computed.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable4">EBLC, IndexSubTable4</see> section of the specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IndexSubtable"/>
/// <seealso cref="GlyphIdOffsetPair"/>
/// <seealso cref="IndexSubtableFormat5"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable4">OpenType specification: IndexSubTable4</seealso>
public sealed record IndexSubtableFormat4
    : IndexSubtable,
      IDerivedRecord<IndexSubtable, IndexSubtableFormat4>
{
    /// <summary>Gets the (glyphID, offset) pairs, sorted by glyph ID, plus a sentinel.</summary>
    /// <value>The per-glyph lookup table. Entries are sorted ascending by <see cref="GlyphIdOffsetPair.GlyphId"/>; the final entry is a sentinel that bounds the last glyph's data.</value>
    /// <seealso cref="GlyphIdOffsetPair"/>
    /// <seealso cref="GetGlyphDataOffset(int)"/>
    public IReadOnlyList<GlyphIdOffsetPair> GlyphArray { get; init; } = [];

    /// <summary>A (glyphID, offset) pair. Blittable, size 4.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Pairs are sorted ascending by <see cref="GlyphId"/>; the parser relies on this for the sentinel-based length computation.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IndexSubtableFormat4"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable4">OpenType specification: IndexSubTable4</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct GlyphIdOffsetPair : IBigEndianStruct<GlyphIdOffsetPair>
    {
        /// <summary>Glyph ID.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable4"><c>glyphID</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="SbitOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable4">OpenType specification: <c>glyphID</c></seealso>
        public ushort GlyphId;      // +0

        /// <summary>Offset to the bitmap data, relative to <c>ImageDataOffset</c>.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable4"><c>offset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="GlyphId"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable4">OpenType specification: <c>offset</c></seealso>
        public ushort SbitOffset;   // +2

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new pair with each multi-byte field reversed.</returns>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable4">OpenType specification: IndexSubTable4</seealso>
        public static GlyphIdOffsetPair ReverseEndianness(GlyphIdOffsetPair v) => new()
        {
            GlyphId = BinaryPrimitives.ReverseEndianness(v.GlyphId),
            SbitOffset = BinaryPrimitives.ReverseEndianness(v.SbitOffset),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the shared <see cref="IndexSubHeader"/>.</param>
    /// <param name="context">An <see cref="IndexSubtableFormatContext"/> carrying the range and the already-read header.</param>
    /// <returns>The parsed format-4 subtable.</returns>
    /// <exception cref="EndOfStreamException">The pair array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The glyph count is read from the cursor, then <c>count + 1</c> pairs are consumed; the extra entry is the sentinel.</description></item>
    /// <item><description>The subtable's <see cref="IndexSubtable.FirstGlyphIndex"/> and <see cref="IndexSubtable.LastGlyphIndex"/> are derived from the first and last pair, not from the record context.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable4">IndexSubTable4 layout</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IndexSubtableFormatContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable4">OpenType specification: IndexSubTable4</seealso>
    static IndexSubtableFormat4 IDerivedRecord<IndexSubtable, IndexSubtableFormat4>.Parse(
        ref Cursor cursor, object? context)
    {
        var ctx = (IndexSubtableFormatContext)context!;
        int numGlyphs = checked((int)cursor.ReadUInt32());

        // glyphArray has numGlyphs + 1 entries (sentinel).
        GlyphIdOffsetPair[] pairs = cursor.ReadBigEndianStructArray<GlyphIdOffsetPair>(numGlyphs + 1);

        // Format 4 derives its own glyph range from the array; the enclosing record's
        // range is advisory only.
        return new IndexSubtableFormat4
        {
            IndexFormat = ctx.Header.IndexFormat,
            ImageFormat = ctx.Header.ImageFormat,
            ImageDataOffset = ctx.Header.ImageDataOffset,
            FirstGlyphIndex = pairs.Length > 0 ? pairs[0].GlyphId : (ushort)0,
            LastGlyphIndex = pairs.Length > 0 ? pairs[^1].GlyphId : (ushort)0,
            GlyphArray = pairs,
        };
    }

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The absolute offset within the EBDT/CBDT table, or <c>-1</c> when the glyph is not present.</returns>
    /// <remarks>The lookup is linear over <see cref="GlyphArray"/>. Because the array is sorted by glyph ID, a binary search would also be valid.</remarks>
    /// <seealso cref="GetGlyphDataLength(int)"/>
    public override long GetGlyphDataOffset(int glyphId)
    {
        for (int i = 0; i < GlyphArray.Count - 1; i++)
            if (GlyphArray[i].GlyphId == glyphId)
                return ImageDataOffset + GlyphArray[i].SbitOffset;
        return -1;
    }

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The byte length of the glyph's bitmap data, or <c>0</c> when the glyph is not present.</returns>
    /// <seealso cref="GetGlyphDataOffset(int)"/>
    public override int GetGlyphDataLength(int glyphId)
    {
        for (int i = 0; i < GlyphArray.Count - 1; i++)
            if (GlyphArray[i].GlyphId == glyphId)
                return GlyphArray[i + 1].SbitOffset - GlyphArray[i].SbitOffset;
        return 0;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 5 — sparse glyph IDs, fixed image size
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>IndexSubtable format 5: sparse glyph IDs with a fixed image size and shared metrics. The glyph ID array is sorted by glyph ID.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The subtable stores the image size and metrics once, plus an explicit glyph ID array; every glyph's data is located arithmetically from its ordinal position in the array.</description></item>
/// <item><description>A sentinel glyph ID is stored at the end of the array, matching the pattern used by formats 1, 3, and 4.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable5">EBLC, IndexSubTable5</see> section of the specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IndexSubtable"/>
/// <seealso cref="IndexSubtableFormat2"/>
/// <seealso cref="IndexSubtableFormat4"/>
/// <seealso cref="BigGlyphMetrics"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable5">OpenType specification: IndexSubTable5</seealso>
public sealed record IndexSubtableFormat5
    : IndexSubtable,
      IDerivedRecord<IndexSubtable, IndexSubtableFormat5>
{
    /// <summary>Byte length of every glyph's data.</summary>
    /// <value>The constant image size shared by all glyphs in the subtable.</value>
    /// <seealso cref="GetGlyphDataLength(int)"/>
    public uint ImageSize { get; init; }

    /// <summary>Metrics shared by every glyph in the range.</summary>
    /// <value>The <see cref="BigGlyphMetrics"/> that describe every glyph bitmap in this subtable.</value>
    /// <seealso cref="BigGlyphMetrics"/>
    public BigGlyphMetrics BigMetrics { get; init; }

    /// <summary>Glyph IDs present in this subtable, sorted ascending.</summary>
    /// <value>The glyph ID array, one entry per real glyph plus a sentinel.</value>
    /// <seealso cref="GetGlyphDataOffset(int)"/>
    public IReadOnlyList<ushort> GlyphIdArray { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned immediately after the shared <see cref="IndexSubHeader"/>.</param>
    /// <param name="context">An <see cref="IndexSubtableFormatContext"/> carrying the range and the already-read header.</param>
    /// <returns>The parsed format-5 subtable.</returns>
    /// <exception cref="EndOfStreamException">The glyph ID array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The glyph count is read from the cursor, then that many glyph IDs are consumed.</description></item>
    /// <item><description>The subtable's <see cref="IndexSubtable.FirstGlyphIndex"/> and <see cref="IndexSubtable.LastGlyphIndex"/> are derived from the first and last entries of the glyph ID array.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable5">IndexSubTable5 layout</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IndexSubtableFormatContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtable5">OpenType specification: IndexSubTable5</seealso>
    static IndexSubtableFormat5 IDerivedRecord<IndexSubtable, IndexSubtableFormat5>.Parse(
        ref Cursor cursor, object? context)
    {
        var ctx = (IndexSubtableFormatContext)context!;
        uint imageSize = cursor.ReadUInt32();
        BigGlyphMetrics metrics = cursor.ReadStruct<BigGlyphMetrics>();
        int numGlyphs = checked((int)cursor.ReadUInt32());
        ushort[] glyphs = cursor.ReadUInt16Array(numGlyphs);

        // Format 5 also derives its own range from the array.
        return new IndexSubtableFormat5
        {
            IndexFormat = ctx.Header.IndexFormat,
            ImageFormat = ctx.Header.ImageFormat,
            ImageDataOffset = ctx.Header.ImageDataOffset,
            FirstGlyphIndex = glyphs.Length > 0 ? glyphs[0] : (ushort)0,
            LastGlyphIndex = glyphs.Length > 0 ? glyphs[^1] : (ushort)0,
            ImageSize = imageSize,
            BigMetrics = metrics,
            GlyphIdArray = glyphs,
        };
    }

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The absolute offset within the EBDT/CBDT table, or <c>-1</c> when the glyph is not present.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup is linear over <see cref="GlyphIdArray"/>.</description></item>
    /// <item><description>The offset is computed from the glyph's ordinal position in the array, not from its value; non-contiguous glyph IDs are supported because the array is a sparse set.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetGlyphDataLength(int)"/>
    public override long GetGlyphDataOffset(int glyphId)
    {
        for (int i = 0; i < GlyphIdArray.Count; i++)
            if (GlyphIdArray[i] == glyphId)
                return ImageDataOffset + (long)i * ImageSize;
        return -1;
    }

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns><see cref="ImageSize"/> when the glyph is present, or <c>0</c> otherwise.</returns>
    /// <seealso cref="ImageSize"/>
    /// <seealso cref="GetGlyphDataOffset(int)"/>
    public override int GetGlyphDataLength(int glyphId)
    {
        for (int i = 0; i < GlyphIdArray.Count; i++)
            if (GlyphIdArray[i] == glyphId)
                return (int)ImageSize;
        return 0;
    }
}
