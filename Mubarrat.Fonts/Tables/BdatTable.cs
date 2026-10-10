using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// bdat — Bitmap Data Table (classic Apple bitmap-only fonts)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>bdat</c> table: the glyph bitmap payloads of an Apple bitmap-only font. It is a version word followed by the bitmap data blobs that the paired <see cref="BlocTable"/> locates.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><c>bdat</c> is the classic-Apple counterpart of the OpenType <see cref="EbdtTable">EBDT</see> table, and <see cref="BlocTable">bloc</see> is the counterpart of <see cref="EblcTable">EBLC</see>. The table itself is a very simple structure: a version number followed by untyped data whose layout is determined entirely by the <c>imageFormat</c> field of the <c>bloc</c> index subtable that points at it. See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html"><c>bdat</c> chapter</see> of Apple's TrueType Reference Manual.</description></item>
/// <item><description>Apple defines six glyph bitmap formats for <c>bdat</c>: 1 (small metrics, byte-aligned image), 2 (small metrics, bit-aligned image), 4 (compressed image with white/black trees), 5 (bit-aligned image only, metrics in the <c>bloc</c> subtable), 6 (big metrics, byte-aligned image), and 7 (big metrics, bit-aligned image). Format 3 is explicitly not used.</description></item>
/// <item><description>Formats 1, 2, 5, 6, and 7 are byte-for-byte identical to EBDT image formats 1, 2, 5, 6, and 7, so they are decoded through the existing <see cref="GlyphBitmap"/> hierarchy (<see cref="GlyphBitmapFormat1"/>, <see cref="GlyphBitmapFormat2"/>, <see cref="GlyphBitmapFormat5"/>, <see cref="GlyphBitmapFormat6"/>, and <see cref="GlyphBitmapFormat7"/>) rather than by a parallel set of types.</description></item>
/// <item><description>The bitmap records carry no leading format word: the image format is stored in the <c>bloc</c> index subtable. This is stated in the manual only by construction — the format tables list the metrics and image data, with no format field — and it matches the EBDT convention.</description></item>
/// <item><description>Format 4 is a modified Huffman encoding. Apple's manual documents only its three header offsets and states that the trees and glyph data follow all of the format-4 records; it does not publish the tree encoding on that page. The offsets are exposed through <see cref="Format4Glyphs"/> and are not decompressed, so no format-4 data is silently discarded or misinterpreted.</description></item>
/// <item><description>The parse is eager and cross-table: it resolves <see cref="BlocTable"/> from the <see cref="FontFace"/> context and materializes every glyph bitmap it can decode, exactly as <see cref="EbdtTable"/> does for EBDT/EBLC. Because of that, <see cref="BitmapDataReader"/> is not reused directly here — it would reject format 4.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BlocTable"/>
/// <seealso cref="EbdtTable"/>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="BitmapStrike"/>
/// <seealso cref="IndexSubtable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">Apple TrueType Reference Manual: <c>bdat</c> table</seealso>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bloc.html">Apple TrueType Reference Manual: <c>bloc</c> table</seealso>
public sealed record BdatTable : IFontTable<BdatTable>
{
    /// <summary>The only table version defined by the manual, <c>0x00020000</c> (2.0).</summary>
    private const int InitialVersion = 0x00020000;

    /// <summary>The <c>bloc</c> image format that selects the compressed, tree-based representation.</summary>
    private const ushort CompressedImageFormat = 4;

    /// <summary>Gets the table tag <c>bdat</c>.</summary>
    /// <value>The four-byte tag <c>bdat</c>.</value>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">Apple TrueType Reference Manual: <c>bdat</c> table</seealso>
    public static Tag Tag => "bdat";

    /// <summary>Gets the table version as a 16.16 fixed-point value.</summary>
    /// <value>The raw <see cref="Fixed"/> bit pattern of the <c>version</c> field; the initial version is <c>0x00020000</c> (2.0).</value>
    /// <remarks>The parse rejects any other version with an <see cref="InvalidDataException"/>.</remarks>
    /// <seealso cref="Fixed"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">Apple TrueType Reference Manual: <c>bdat</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the decoded glyph bitmaps, keyed by (ppemX, ppemY, glyphId).</summary>
    /// <value>A dictionary whose keys carry the strike dimensions and glyph ID; the value is the parsed <see cref="GlyphBitmap"/>. Empty when no strike yields a decodable glyph.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The dictionary is populated during parse by walking every strike of the paired <see cref="BlocTable"/> and materializing each glyph's bitmap through the image format recorded in its index subtable.</description></item>
    /// <item><description>Image format 4 is absent from this dictionary; those glyphs appear in <see cref="Format4Glyphs"/> instead.</description></item>
    /// <item><description>If two strikes share identical ppem dimensions, the later strike overwrites the earlier one's entries for glyphs present in both; this mirrors <see cref="EbdtTable"/>.</description></item>
    /// <item><description>The metrics for image format 5 come from the enclosing <see cref="IndexSubtableFormat2"/>, which the strike carries in its index subtable list.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetGlyph(int, int, int)"/>
    /// <seealso cref="GlyphCount"/>
    /// <seealso cref="Format4Glyphs"/>
    /// <seealso cref="GlyphBitmap"/>
    public IReadOnlyDictionary<(int PpemX, int PpemY, int GlyphId), GlyphBitmap> Glyphs { get; init; }
        = new Dictionary<(int, int, int), GlyphBitmap>();

    /// <summary>Gets the glyphs whose bitmap data uses image format 4, the compressed tree-based representation.</summary>
    /// <value>One <see cref="BdatFormat4Glyph"/> per glyph whose index subtable declares image format 4. Empty when the font uses no compressed bitmaps.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Only the three offsets of each format-4 record are exposed. The trees and the compressed glyph data they describe are not decoded, because Apple's manual does not publish the encoding on the <c>bdat</c> page.</description></item>
    /// <item><description>The offsets are reported verbatim; see <see cref="BdatFormat4Glyph"/> for the base-setting caveat.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetFormat4Glyph(int, int, int)"/>
    /// <seealso cref="Format4GlyphCount"/>
    /// <seealso cref="BdatFormat4Glyph"/>
    public IReadOnlyList<BdatFormat4Glyph> Format4Glyphs { get; init; } = [];

    /// <summary>Gets the number of decoded glyph bitmaps in <see cref="Glyphs"/>.</summary>
    /// <value>The count of dictionary entries, one per (strike, glyph) pair whose bitmap was decoded.</value>
    /// <seealso cref="Glyphs"/>
    /// <seealso cref="Format4GlyphCount"/>
    public int GlyphCount => Glyphs.Count;

    /// <summary>Gets the number of image-format-4 glyph descriptors in <see cref="Format4Glyphs"/>.</summary>
    /// <value>The count of compressed glyph entries, which are not present in <see cref="Glyphs"/>.</value>
    /// <seealso cref="Format4Glyphs"/>
    /// <seealso cref="GlyphCount"/>
    public int Format4GlyphCount => Format4Glyphs.Count;

    /// <summary>Returns the decoded bitmap for a glyph at a given strike, or <c>null</c>.</summary>
    /// <param name="ppemX">The horizontal pixels per em of the target strike.</param>
    /// <param name="ppemY">The vertical pixels per em of the target strike.</param>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The <see cref="GlyphBitmap"/> for the glyph at the given strike, or <c>null</c> when the entry is absent or the glyph uses image format 4.</returns>
    /// <remarks>All three components of the key must match exactly; the strike dimensions are not treated as a range. Compressed glyphs are found through <see cref="GetFormat4Glyph(int, int, int)"/> instead.</remarks>
    /// <seealso cref="Glyphs"/>
    /// <seealso cref="GetFormat4Glyph(int, int, int)"/>
    /// <seealso cref="GlyphBitmap"/>
    public GlyphBitmap? GetGlyph(int ppemX, int ppemY, int glyphId) =>
        Glyphs.TryGetValue((ppemX, ppemY, glyphId), out var bitmap) ? bitmap : null;

    /// <summary>Returns the image-format-4 descriptor for a glyph at a given strike, or <c>null</c>.</summary>
    /// <param name="ppemX">The horizontal pixels per em of the target strike.</param>
    /// <param name="ppemY">The vertical pixels per em of the target strike.</param>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The matching <see cref="BdatFormat4Glyph"/>, or <c>null</c> when the glyph is not compressed at that strike.</returns>
    /// <remarks>The lookup is linear over <see cref="Format4Glyphs"/>. All three key components must match exactly.</remarks>
    /// <seealso cref="Format4Glyphs"/>
    /// <seealso cref="GetGlyph(int, int, int)"/>
    /// <seealso cref="BdatFormat4Glyph"/>
    public BdatFormat4Glyph? GetFormat4Glyph(int ppemX, int ppemY, int glyphId)
    {
        foreach (var glyph in Format4Glyphs)
            if (glyph.PpemX == ppemX && glyph.PpemY == ppemY && glyph.GlyphId == glyphId) return glyph;
        return null;
    }

    /// <summary>The 4-byte fixed-layout <c>bdat</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by the glyph bitmap data; the variable-length blobs are located by the index subtables of the paired <see cref="BlocTable"/> rather than by an array declared here.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BdatTable"/>
    /// <seealso cref="EbdtTable.Header"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">Apple TrueType Reference Manual: <c>bdat</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version as a 16.16 fixed-point value at byte offset 0.</summary>
        /// <remarks>The initial version is <c>0x00020000</c> (2.0).</remarks>
        /// <seealso cref="BdatTable.Version"/>
        public Fixed Version;   // +0

        /// <summary>Reverses the byte order of the version field in a <see cref="Header"/>.</summary>
        /// <param name="v">The header whose version field is to be reversed.</param>
        /// <returns>A new header with the version reversed.</returns>
        /// <remarks>The header has no other fields and no byte-sized fields, so the version is the only field that participates in the reversal.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = Fixed.ReverseEndianness(v.Version),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the <c>bdat</c> table.</param>
    /// <param name="context">A <see cref="FontFace"/> whose <see cref="FontFace.GetTable{T}"/> is used to resolve the paired <see cref="BlocTable"/>.</param>
    /// <returns>The parsed <c>bdat</c> table with every decodable glyph bitmap materialized.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="FontFace"/>.</exception>
    /// <exception cref="InvalidDataException">The version is not 2.0, an index subtable declares an image format that <c>bdat</c> does not define, or a glyph record is structurally invalid.</exception>
    /// <exception cref="EndOfStreamException">A glyph record or a format-4 header extends past the end of <c>cursor.Source</c>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The parse is eager: every strike in the paired <c>bloc</c> table is walked and each glyph's bitmap is materialized before this method returns. The source relationship is the same as EBDT/EBLC, so <c>bdat</c> must be parsed after <c>bloc</c> for the same face.</description></item>
    /// <item><description>Glyphs whose <c>imageFormat</c> is 4 are recorded as <see cref="BdatFormat4Glyph"/> descriptors rather than decoded; every other glyph is parsed through <see cref="GlyphBitmap"/> with the metrics carried by its index subtable.</description></item>
    /// <item><description>Entries whose data offset is negative or whose length is not positive are treated as absent glyphs and skipped, matching the behaviour of the EBDT reader.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="BlocTable"/>
    /// <seealso cref="GlyphBitmap"/>
    /// <seealso cref="GlyphBitmapContext"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">Apple TrueType Reference Manual: <c>bdat</c> table</seealso>
    static BdatTable IRecord<BdatTable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not FontFace face)
            throw new InvalidOperationException(
                $"{nameof(BdatTable)}.Parse requires a {nameof(FontFace)} context.");

        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version.Bits != InitialVersion)
            throw new InvalidDataException(
                $"'bdat'.version is 0x{header.Version.Bits:X8}, expected 0x{InitialVersion:X8}.");

        Source source = cursor.Source;
        var glyphs = new Dictionary<(int, int, int), GlyphBitmap>();
        var format4 = new List<BdatFormat4Glyph>();

        foreach (var strike in face.GetTable<BlocTable>().Strikes)
        {
            foreach (var record in strike.IndexSubtableList.Records)
            {
                IndexSubtable subtable = record.Subtable;

                switch (subtable.ImageFormat)
                {
                    case 1:
                    case 2:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                        break;
                    default:
                        throw new InvalidDataException(
                            $"'bdat' image format {subtable.ImageFormat} is not defined " +
                            "(expected 1, 2, 4, 5, 6, or 7).");
                }

                BigGlyphMetrics? subtableMetrics = GlyphBitmap.GetMetricsFromSubtable(subtable);

                for (int glyphId = record.FirstGlyphIndex; glyphId <= record.LastGlyphIndex; glyphId++)
                {
                    long offset = subtable.GetGlyphDataOffset(glyphId);
                    int length = subtable.GetGlyphDataLength(glyphId);
                    if (offset < 0 || length <= 0) continue;

                    if (subtable.ImageFormat == CompressedImageFormat)
                    {
                        BdatFormat4Header compressed = source.ReadEndianReversibleStructAt<BdatFormat4Header>(offset);
                        format4.Add(new BdatFormat4Glyph
                        {
                            PpemX = strike.PpemX,
                            PpemY = strike.PpemY,
                            GlyphId = glyphId,
                            WhiteTreeOffset = compressed.WhiteTreeOffset,
                            BlackTreeOffset = compressed.BlackTreeOffset,
                            GlyphDataOffset = compressed.GlyphDataOffset,
                        });
                        continue;
                    }

                    glyphs[(strike.PpemX, strike.PpemY, glyphId)] = source.ParseRecordAt<GlyphBitmap>(
                        offset, new GlyphBitmapContext(subtable.ImageFormat, length, subtableMetrics));
                }
            }
        }

        return new BdatTable
        {
            Version = header.Version,
            Glyphs = glyphs,
            Format4Glyphs = format4,
        };
    }
}

/// <summary>The 12-byte header of an image-format-4 glyph record: three 32-bit offsets to the white tree, the black tree, and the compressed glyph data.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Apple's manual states that the white tree, the black tree, and the glyph data are found after all of the format-4 glyph bitmap arrays, but does not state the base against which the three offsets are measured. They are therefore read and reported verbatim.</description></item>
/// <item><description>The trees are a modified Huffman encoding of the bitmap; Apple's <c>bdat</c> page does not publish the tree layout, so this library does not decode them.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BdatFormat4Glyph"/>
/// <seealso cref="BdatTable.Format4Glyphs"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">Apple TrueType Reference Manual: <c>bdat</c> table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct BdatFormat4Header : IEndianReversibleStruct<BdatFormat4Header>
{
    /// <summary>The offset of the white tree at byte offset 0.</summary>
    /// <remarks>Reported verbatim; the manual does not state the base of the offset.</remarks>
    /// <seealso cref="BlackTreeOffset"/>
    /// <seealso cref="GlyphDataOffset"/>
    public uint WhiteTreeOffset;   // +0

    /// <summary>The offset of the black tree at byte offset 4.</summary>
    /// <remarks>Reported verbatim; the manual does not state the base of the offset.</remarks>
    /// <seealso cref="WhiteTreeOffset"/>
    /// <seealso cref="GlyphDataOffset"/>
    public uint BlackTreeOffset;   // +4

    /// <summary>The offset of the compressed glyph data at byte offset 8.</summary>
    /// <remarks>Reported verbatim; the manual does not state the base of the offset.</remarks>
    /// <seealso cref="WhiteTreeOffset"/>
    /// <seealso cref="BlackTreeOffset"/>
    public uint GlyphDataOffset;   // +8

    /// <summary>Reverses the byte order of all three fields in a <see cref="BdatFormat4Header"/>.</summary>
    /// <param name="v">The header whose multi-byte fields are to be reversed.</param>
    /// <returns>A new header with each offset reversed.</returns>
    /// <remarks>All three fields are 32-bit offsets and all participate in the reversal.</remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Source.ReadEndianReversibleStructAt{T}(long)"/>
    public static BdatFormat4Header ReverseEndianness(BdatFormat4Header v) => new()
    {
        WhiteTreeOffset = BinaryPrimitives.ReverseEndianness(v.WhiteTreeOffset),
        BlackTreeOffset = BinaryPrimitives.ReverseEndianness(v.BlackTreeOffset),
        GlyphDataOffset = BinaryPrimitives.ReverseEndianness(v.GlyphDataOffset),
    };
}

/// <summary>One image-format-4 (compressed) glyph: the strike and glyph it belongs to, plus the three offsets of its format-4 header.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The glyph's bitmap is a modified Huffman encoding driven by a white tree and a black tree; the trees and the glyph data are <em>not</em> decoded by this library, because Apple's <c>bdat</c> page documents only the offsets.</description></item>
/// <item><description>The three offsets are exposed exactly as stored. Unlike image formats 1, 2, 5, 6, and 7, a format-4 record carries no metrics and no image size, so the bounding box must be recovered from the tree once a caller decodes it.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BdatTable.Format4Glyphs"/>
/// <seealso cref="BdatTable.GetFormat4Glyph(int, int, int)"/>
/// <seealso cref="BdatFormat4Header"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bdat.html">Apple TrueType Reference Manual: <c>bdat</c> table</seealso>
public sealed record BdatFormat4Glyph
{
    /// <summary>Gets the horizontal pixels per em of the strike that contains the glyph.</summary>
    /// <value>The <see cref="BitmapStrike.PpemX"/> of the owning strike.</value>
    /// <seealso cref="PpemY"/>
    /// <seealso cref="GlyphId"/>
    public int PpemX { get; init; }

    /// <summary>Gets the vertical pixels per em of the strike that contains the glyph.</summary>
    /// <value>The <see cref="BitmapStrike.PpemY"/> of the owning strike.</value>
    /// <seealso cref="PpemX"/>
    /// <seealso cref="GlyphId"/>
    public int PpemY { get; init; }

    /// <summary>Gets the glyph ID that owns the compressed bitmap.</summary>
    /// <value>The glyph ID within the owning strike.</value>
    /// <seealso cref="PpemX"/>
    /// <seealso cref="PpemY"/>
    public int GlyphId { get; init; }

    /// <summary>Gets the offset of the white tree.</summary>
    /// <value>The <c>whiteTreeOffset</c> field of the glyph's format-4 record, reported verbatim.</value>
    /// <remarks>The base of the offset is not stated by the manual; see <see cref="BdatFormat4Header.WhiteTreeOffset"/>.</remarks>
    /// <seealso cref="BlackTreeOffset"/>
    /// <seealso cref="GlyphDataOffset"/>
    public uint WhiteTreeOffset { get; init; }

    /// <summary>Gets the offset of the black tree.</summary>
    /// <value>The <c>blackTreeOffset</c> field of the glyph's format-4 record, reported verbatim.</value>
    /// <remarks>The base of the offset is not stated by the manual; see <see cref="BdatFormat4Header.BlackTreeOffset"/>.</remarks>
    /// <seealso cref="WhiteTreeOffset"/>
    /// <seealso cref="GlyphDataOffset"/>
    public uint BlackTreeOffset { get; init; }

    /// <summary>Gets the offset of the compressed glyph data.</summary>
    /// <value>The <c>glyphDataOffset</c> field of the glyph's format-4 record, reported verbatim.</value>
    /// <remarks>The base of the offset is not stated by the manual; see <see cref="BdatFormat4Header.GlyphDataOffset"/>.</remarks>
    /// <seealso cref="WhiteTreeOffset"/>
    /// <seealso cref="BlackTreeOffset"/>
    public uint GlyphDataOffset { get; init; }
}
