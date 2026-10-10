using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// bloc — Bitmap Location Table (classic Apple bitmap-only fonts)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>bloc</c> table: the bitmap location table of an Apple bitmap-only font. It describes which point sizes have bitmaps and where each glyph's bitmap data lives inside the paired <see cref="BdatTable"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><c>bloc</c> is the classic-Apple counterpart of the OpenType <see cref="EblcTable">EBLC</see> table, and <see cref="BdatTable">bdat</see> is the counterpart of <see cref="EbdtTable">EBDT</see>. A font that has one of <c>bloc</c>/<c>bdat</c> must have the other. See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bloc.html"><c>bloc</c> chapter</see> of Apple's TrueType Reference Manual.</description></item>
/// <item><description>The wire layout of the strike records, the index subtable arrays, the index subheaders, and the index subtable formats 1 through 3 is byte-for-byte identical to EBLC's. This type therefore reuses <see cref="BitmapStrike"/>, <see cref="IndexSubtableList"/>, <see cref="IndexSubtableRecord"/>, <see cref="SbitLineMetrics"/>, <see cref="IndexSubHeader"/>, and the <see cref="IndexSubtable"/> format hierarchy rather than redeclaring them.</description></item>
/// <item><description>Every offset stored in the table is relative to the start of the <c>bloc</c> table itself, not to the containing font file; the table-scoped <see cref="Source"/> supplied by the loader enforces that convention. The one exception is the per-entry index subtable offset, which is relative to the start of the enclosing index subtable array — exactly as in EBLC.</description></item>
/// <item><description>Apple's manual defines index subtable formats 1, 2, and 3 for <c>bloc</c> and notes that sparse glyph sets are theoretically permitted but may not be supported by implementations. The shared <see cref="IndexSubtable"/> model additionally decodes formats 4 and 5, so such a table is parsed rather than rejected.</description></item>
/// <item><description>Strike sizes must be sorted in ascending order, but this parser does not enforce the ordering.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BdatTable"/>
/// <seealso cref="EblcTable"/>
/// <seealso cref="BitmapStrike"/>
/// <seealso cref="IndexSubtable"/>
/// <seealso cref="BitmapLocationParser"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bloc.html">Apple TrueType Reference Manual: <c>bloc</c> table</seealso>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">Apple TrueType Reference Manual: <c>bdat</c> table</seealso>
public sealed record BlocTable : IFontTable<BlocTable>
{
    /// <summary>The only table version defined by the manual, <c>0x00020000</c> (2.0).</summary>
    private const int InitialVersion = 0x00020000;

    /// <summary>The safety limit on the number of bitmap size tables read from the header.</summary>
    private const int MaxStrikes = 1024;

    /// <summary>Gets the table tag <c>bloc</c>.</summary>
    /// <value>The four-byte tag <c>bloc</c>.</value>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bloc.html">Apple TrueType Reference Manual: <c>bloc</c> table</seealso>
    public static Tag Tag => "bloc";

    /// <summary>Gets the table version as a 16.16 fixed-point value.</summary>
    /// <value>The raw <see cref="Fixed"/> bit pattern of the <c>version</c> field; the only defined value is <c>0x00020000</c> (2.0).</value>
    /// <remarks>The parse rejects any other version with an <see cref="InvalidDataException"/>.</remarks>
    /// <seealso cref="Fixed"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bloc.html">Apple TrueType Reference Manual: <c>bloc</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the bitmap size tables (strikes), one per bitmap point size described by the font.</summary>
    /// <value>The list of <see cref="BitmapStrike"/> entries, in on-disk order. Empty when the table declares no strikes.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Each strike carries the target <see cref="BitmapStrike.PpemX"/>, <see cref="BitmapStrike.PpemY"/>, <see cref="BitmapStrike.BitDepth"/>, the horizontal and vertical <see cref="SbitLineMetrics"/>, the covered glyph range, and the resolved <see cref="BitmapStrike.IndexSubtableList"/>.</description></item>
    /// <item><description>The list is populated eagerly, including every index subtable, so a strike returned from this table is immediately usable for glyph lookups.</description></item>
    /// <item><description>Apple requires the strikes to be sorted by ascending size; this implementation preserves the on-disk order without checking it.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="StrikeCount"/>
    /// <seealso cref="GetStrike(int, int)"/>
    /// <seealso cref="BitmapStrike"/>
    public IReadOnlyList<BitmapStrike> Strikes { get; init; } = [];

    /// <summary>Gets the number of bitmap size tables in the table.</summary>
    /// <value>The value of <see cref="Strikes"/>'s count, which is the <c>numSizes</c> field of the header.</value>
    /// <seealso cref="Strikes"/>
    public int StrikeCount => Strikes.Count;

    /// <summary>Returns the strike whose target pixels-per-em matches the requested pair, or <c>null</c>.</summary>
    /// <param name="ppemX">The horizontal pixels per em to search for.</param>
    /// <param name="ppemY">The vertical pixels per em to search for.</param>
    /// <returns>The first <see cref="BitmapStrike"/> whose <see cref="BitmapStrike.PpemX"/> and <see cref="BitmapStrike.PpemY"/> match, or <c>null</c> when no strike matches.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup is linear over <see cref="Strikes"/>. A font typically declares only a handful of sizes, so the scan is not a concern.</description></item>
    /// <item><description>When more than one strike shares the same ppem pair, the first match in on-disk order is returned.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetStrike(int)"/>
    /// <seealso cref="GetSubtable(int, int, int)"/>
    /// <seealso cref="Strikes"/>
    public BitmapStrike? GetStrike(int ppemX, int ppemY)
    {
        foreach (var strike in Strikes)
            if (strike.PpemX == ppemX && strike.PpemY == ppemY) return strike;
        return null;
    }

    /// <summary>Returns the square strike for a single pixels-per-em value, or <c>null</c>.</summary>
    /// <param name="ppem">The pixels per em to search for on both axes.</param>
    /// <returns>The first strike whose horizontal and vertical pixels per em both equal <paramref name="ppem"/>, or <c>null</c>.</returns>
    /// <remarks>Equivalent to <c><see cref="GetStrike(int, int)"/>(ppem, ppem)</c>.</remarks>
    /// <seealso cref="GetStrike(int, int)"/>
    public BitmapStrike? GetStrike(int ppem) => GetStrike(ppem, ppem);

    /// <summary>Returns the index subtable that holds a glyph's bitmap location for a given strike, or <c>null</c>.</summary>
    /// <param name="glyphId">The glyph ID to search for.</param>
    /// <param name="ppemX">The horizontal pixels per em of the target strike.</param>
    /// <param name="ppemY">The vertical pixels per em of the target strike.</param>
    /// <returns>The <see cref="IndexSubtable"/> covering <paramref name="glyphId"/> at the requested strike, or <c>null</c> when the strike or the glyph is absent.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The strike is located through <see cref="GetStrike(int, int)"/> and the glyph range through <see cref="BitmapStrike.FindSubtable(ushort)"/>.</description></item>
    /// <item><description>A glyph ID above <c>ushort.MaxValue</c> cannot appear in any subtable and yields <c>null</c> after truncation.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetGlyphDataOffset(int, int, int)"/>
    /// <seealso cref="GetGlyphDataLength(int, int, int)"/>
    /// <seealso cref="IndexSubtable"/>
    public IndexSubtable? GetSubtable(int glyphId, int ppemX, int ppemY) =>
        GetStrike(ppemX, ppemY)?.FindSubtable((ushort)glyphId);

    /// <summary>Returns the offset of a glyph's bitmap data inside the paired <c>bdat</c> table, or <c>-1</c> when the glyph has no bitmap at that size.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <param name="ppemX">The horizontal pixels per em of the target strike.</param>
    /// <param name="ppemY">The vertical pixels per em of the target strike.</param>
    /// <returns>The byte offset of the glyph's bitmap data relative to the start of the <c>bdat</c> table, or <c>-1</c> when no bitmap is present.</returns>
    /// <remarks>The offset is resolved through <see cref="GetSubtable(int, int, int)"/> and <see cref="IndexSubtable.GetGlyphDataOffset(int)"/>. A zero-length entry marks an absent glyph, in which case the corresponding length is <c>0</c>.</remarks>
    /// <seealso cref="GetGlyphDataLength(int, int, int)"/>
    /// <seealso cref="BdatTable"/>
    public long GetGlyphDataOffset(int glyphId, int ppemX, int ppemY) =>
        GetSubtable(glyphId, ppemX, ppemY)?.GetGlyphDataOffset(glyphId) ?? -1;

    /// <summary>Returns the byte length of a glyph's bitmap data, or <c>0</c> when the glyph has no bitmap at that size.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <param name="ppemX">The horizontal pixels per em of the target strike.</param>
    /// <param name="ppemY">The vertical pixels per em of the target strike.</param>
    /// <returns>The byte length of the glyph's bitmap data, or <c>0</c> when no bitmap is present.</returns>
    /// <remarks>The length is resolved through <see cref="GetSubtable(int, int, int)"/> and <see cref="IndexSubtable.GetGlyphDataLength(int)"/>; a zero result means the glyph is absent from the strike.</remarks>
    /// <seealso cref="GetGlyphDataOffset(int, int, int)"/>
    public int GetGlyphDataLength(int glyphId, int ppemX, int ppemY) =>
        GetSubtable(glyphId, ppemX, ppemY)?.GetGlyphDataLength(glyphId) ?? 0;

    /// <summary>The 8-byte fixed-layout <c>bloc</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by <see cref="NumStrikes"/> 48-byte bitmap size tables, each of which is byte-identical to an EBLC BitmapSize record and is therefore read as <see cref="BitmapStrike.Header"/>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BlocTable"/>
    /// <seealso cref="BitmapStrike.Header"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bloc.html">Apple TrueType Reference Manual: <c>bloc</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version as a 16.16 fixed-point value at byte offset 0.</summary>
        /// <remarks>The only defined value is <c>0x00020000</c> (2.0).</remarks>
        /// <seealso cref="BlocTable.Version"/>
        public Fixed Version;        // +0

        /// <summary>The number of bitmap size tables that follow the header at byte offset 4.</summary>
        /// <remarks>Apple names this field <c>numSizes</c>; it counts the 48-byte strike records that begin at byte offset 8, not an array of offsets. The parser enforces a safety limit of 1024.</remarks>
        /// <seealso cref="BlocTable.Strikes"/>
        /// <seealso cref="BitmapStrike.Header"/>
        public uint NumStrikes;      // +4

        /// <summary>Reverses the byte order of both fields in a <see cref="Header"/>.</summary>
        /// <param name="v">The header whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with the version and strike count reversed.</returns>
        /// <remarks>There are no byte-sized fields; both fields participate in the reversal.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = Fixed.ReverseEndianness(v.Version),
            NumStrikes = BinaryPrimitives.ReverseEndianness(v.NumStrikes),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the <c>bloc</c> table.</param>
    /// <param name="context">Not read; the table is self-contained. The <see cref="FontFace"/> supplied by the table loader is ignored.</param>
    /// <returns>The parsed <c>bloc</c> table with every strike and index subtable resolved.</returns>
    /// <exception cref="EndOfStreamException">The strike array or one of its index subtables extends past the end of <c>cursor.Source</c>.</exception>
    /// <exception cref="InvalidDataException">The version is not 2.0, the strike count exceeds the safety limit of 1024, the declared table length is too small for the strike array, or an index subtable declares an undefined format.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The declared table length — the length of the table-scoped <see cref="Source"/> — bounds the strike array before it is allocated, so a corrupt <c>numSizes</c> cannot cause a large allocation.</description></item>
    /// <item><description>Each strike's index subtable array is resolved eagerly through <see cref="ParentContext"/>, so the offsets it stores are interpreted relative to the array rather than to the table, exactly as EBLC requires.</description></item>
    /// <item><description>The shared <see cref="IndexSubtable"/> dispatcher defines formats 1 through 5; Apple's manual defines only 1 through 3 for <c>bloc</c>. Formats 4 and 5 are parsed rather than rejected.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="BitmapStrike"/>
    /// <seealso cref="BitmapLocationParser.Parse(ref Cursor, Tag, ushort, ushort)"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bloc.html">Apple TrueType Reference Manual: <c>bloc</c> table</seealso>
    static BlocTable IRecord<BlocTable>.Parse(ref Cursor cursor, object? context)
    {
        long tableLength = cursor.Source.Length;

        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version.Bits != InitialVersion)
            throw new InvalidDataException(
                $"'bloc'.version is 0x{header.Version.Bits:X8}, expected 0x{InitialVersion:X8}.");

        if (header.NumStrikes > MaxStrikes)
            throw new InvalidDataException(
                $"'bloc'.numSizes is {header.NumStrikes}, exceeding the safety limit of {MaxStrikes}.");

        // Bound the strike array against the table's declared length before allocating.
        // Unsafe.SizeOf is a JIT constant for an unmanaged struct.
        long neededBytes = cursor.Position + (long)header.NumStrikes * Unsafe.SizeOf<BitmapStrike.Header>();
        if (neededBytes > tableLength)
            throw new InvalidDataException(
                $"'bloc' needs {neededBytes} bytes for {header.NumStrikes} bitmap size tables but the " +
                $"declared table length is {tableLength}.");

        BitmapStrike[] strikes = cursor.ReadBigEndianHeaderRecordArray<BitmapStrike, BitmapStrike.Header>(
            (int)header.NumStrikes, new ParentContext(cursor.Source));

        return new BlocTable
        {
            Version = header.Version,
            Strikes = strikes,
        };
    }
}
