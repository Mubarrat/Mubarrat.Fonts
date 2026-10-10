using Mubarrat.Fonts.Binary;

namespace Mubarrat.Fonts.Tables;

/// <summary>Reads all glyph bitmaps addressed by a set of strikes from an EBDT/CBDT table source.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <c>EBDT</c>/<c>CBDT</c> table stores the actual glyph bitmaps; the <c>EBLC</c>/<c>CBLC</c> table stores the index structures that locate them. This reader walks the index side and pulls bitmaps from the data side. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">EBDT</see> and <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">EBLC</see> chapters of the specification.</description></item>
/// <item><description>The returned dictionary is keyed by <c>(ppemX, ppemY, glyphId)</c>; if two strikes share the same ppem dimensions, the later strike in <c>strikes</c> overwrites the earlier one for any glyph present in both.</description></item>
/// <item><description>The composite key carries the ppem values rather than the strike object itself so the result is comparable across strikes and can be serialized without retaining the strike graph.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// using var ebdt = FileSource.Open("font.ttf");
/// var bitmaps = BitmapDataReader.ReadAll(ebdt, ebdtTable.Strikes);
/// GlyphBitmap glyph = bitmaps[(16, 16, 42)];
/// </code>
/// </example>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="BitmapStrike"/>
/// <seealso cref="IndexSubtable"/>
/// <seealso cref="GlyphBitmapContext"/>
/// <seealso cref="Source"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">OpenType specification: EBDT table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc#indexsubtables">OpenType specification: EBLC, IndexSubtables</seealso>
public static class BitmapDataReader
{
    /// <summary>Reads all glyph bitmaps addressed by a set of strikes from an EBDT/CBDT table source.</summary>
    /// <param name="source">The source of the EBDT/CBDT table data.</param>
    /// <param name="strikes">The set of bitmap strikes to read.</param>
    /// <returns>A dictionary of glyph bitmaps, keyed by <c>(ppemX, ppemY, glyphId)</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="strikes"/> is <c>null</c>.</exception>
    /// <exception cref="EndOfStreamException">A glyph bitmap extends past the end of <paramref name="source"/>.</exception>
    /// <exception cref="InvalidDataException">A subtable's image format is not recognised, or its declared data is structurally invalid.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Records with a negative offset or non-positive length are skipped rather than throwing; this tolerates subtables that use sentinel values to mark absent glyphs.</description></item>
    /// <item><description>The context passed to <see cref="GlyphBitmap"/> carries the subtable's image format, the glyph's data length, and the subtable's metrics, so the parse can decode the varying per-format bitmap layouts. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">EBDT image formats</see> in the specification.</description></item>
    /// <item><description>Bitmaps are parsed eagerly and materialized into the dictionary; the caller owns the resulting instances and the lifetime of <paramref name="source"/> must outlast this call.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GlyphBitmap"/>
    /// <seealso cref="GlyphBitmapContext"/>
    /// <seealso cref="IndexSubtable.GetGlyphDataOffset(int)"/>
    /// <seealso cref="IndexSubtable.GetGlyphDataLength(int)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">OpenType specification: EBDT table</seealso>
    public static Dictionary<(int, int, int), GlyphBitmap> ReadAll(
        Source source, IReadOnlyList<BitmapStrike> strikes)
    {
        var glyphs = new Dictionary<(int, int, int), GlyphBitmap>();

        foreach (var strike in strikes)
        {
            foreach (var record in strike.IndexSubtableList.Records)
            {
                IndexSubtable sub = record.Subtable;
                for (int glyphId = record.FirstGlyphIndex; glyphId <= record.LastGlyphIndex; glyphId++)
                {
                    long offset = sub.GetGlyphDataOffset(glyphId);
                    int length = sub.GetGlyphDataLength(glyphId);
                    if (offset < 0 || length <= 0) continue;

                    glyphs[(strike.PpemX, strike.PpemY, glyphId)] = source.ParseRecordAt<GlyphBitmap>(offset,
                        new GlyphBitmapContext(sub.ImageFormat, length, GlyphBitmap.GetMetricsFromSubtable(sub)));
                }
            }
        }

        return glyphs;
    }
}
