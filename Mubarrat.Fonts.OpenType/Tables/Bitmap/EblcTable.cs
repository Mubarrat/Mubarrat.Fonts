using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Tables.Bitmap;

/// <summary>The <c>EBLC</c> table (version 2.0): Embedded Bitmap Location Table. Provides locators for monochrome or grayscale bitmap glyph data.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <c>EBLC</c> table stores the index structures that locate monochrome or grayscale bitmap data; the <c>EBDT</c> table stores the data itself. Parsing this table does not read the data side; a subsequent <see cref="EbdtTable"/> parse uses the strikes produced here.</description></item>
/// <item><description>The layout is shared with <c>CBLC</c>; only the version (2.0) and the recognised set of <c>bitDepth</c> values differ. EBLC allows 1, 2, 4, and 8; CBLC additionally allows 32 for BGRA colour.</description></item>
/// <item><description>Both tables are read through the same parser, <see cref="BitmapLocationParser.Parse(ref Cursor, FontFace, Tag, ushort, ushort)"/>, which is parameterised on the expected tag and version.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">EBLC table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="EbdtTable"/>
/// <seealso cref="CblcTable"/>
/// <seealso cref="BitmapLocationParser"/>
/// <seealso cref="BitmapStrike"/>
/// <seealso cref="FontFace"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">OpenType specification: EBLC table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt">OpenType specification: EBDT table</seealso>
public sealed record EblcTable : IOpenTypeTable<EblcTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IOpenTypeTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">OpenType specification: EBLC table</seealso>
    public static Tag Tag => "EBLC";

    /// <summary>Gets the table major version. Always 2.</summary>
    /// <value>The constant <c>2</c> for a conforming EBLC table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc"><c>majorVersion</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MinorVersion"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">OpenType specification: <c>majorVersion</c></seealso>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the table minor version. Always 0.</summary>
    /// <value>The constant <c>0</c> for a conforming EBLC table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc"><c>minorVersion</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MajorVersion"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">OpenType specification: <c>minorVersion</c></seealso>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the strikes.</summary>
    /// <value>The list of <see cref="BitmapStrike"/> entries, one per pixels-per-em dimension pair declared by the table. Empty when the table declares no strikes.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The list is populated during parse from the table's strike array; each strike's index subtables are resolved eagerly at that point.</description></item>
    /// <item><description>Strike order follows the on-disk order in the table, which is not required to be sorted by ppem.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetStrike(int, int)"/>
    /// <seealso cref="BitmapStrike"/>
    public IReadOnlyList<BitmapStrike> Strikes { get; init; } = [];

    /// <summary>Returns the strike for the requested ppem, or <c>null</c>.</summary>
    /// <param name="ppemX">The horizontal pixels per em to search for.</param>
    /// <param name="ppemY">The vertical pixels per em to search for.</param>
    /// <returns>The first <see cref="BitmapStrike"/> whose <see cref="BitmapStrike.PpemX"/> and <see cref="BitmapStrike.PpemY"/> match, or <c>null</c> when no strike matches.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup is linear over <see cref="Strikes"/>. The table is typically small (a handful of strikes), so the linear scan is not usually a concern.</description></item>
    /// <item><description>When more than one strike shares the same ppem pair — unusual but not forbidden — the first match in on-disk order is returned.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Strikes"/>
    /// <seealso cref="BitmapStrike.PpemX"/>
    /// <seealso cref="BitmapStrike.PpemY"/>
    public BitmapStrike? GetStrike(int ppemX, int ppemY)
    {
        foreach (var s in Strikes)
            if (s.PpemX == ppemX && s.PpemY == ppemY) return s;
        return null;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the EBLC table.</param>
    /// <param name="context">A <see cref="FontFace"/> whose table directory provides the declared length for the strike-array bound check.</param>
    /// <returns>The parsed EBLC table with its strikes resolved.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="FontFace"/>.</exception>
    /// <exception cref="InvalidDataException">The version is not 2.0, the strike count exceeds the safety limit, or the declared table length is too small for the strike array.</exception>
    /// <exception cref="EndOfStreamException">The strike array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>This is a thin wrapper around <see cref="BitmapLocationParser.Parse(ref Cursor, FontFace, Tag, ushort, ushort)"/>, supplying the EBLC tag and the <c>2.0</c> expected version.</description></item>
    /// <item><description>Both EBLC and CBLC are read through the same parser; the parser is responsible for the header layout, strike array bounds, and the safety limit on strike count.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BitmapLocationParser.Parse(ref Cursor, FontFace, Tag, ushort, ushort)"/>
    /// <seealso cref="FontFace.GetTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">OpenType specification: EBLC table</seealso>
    static EblcTable IRecord<EblcTable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not FontFace face)
            throw new InvalidOperationException(
                $"{nameof(EblcTable)}.Parse requires a {nameof(FontFace)} context.");

        var (major, minor, strikes) = BitmapLocationParser.Parse(ref cursor, face, Tag, 2, 0);
        return new EblcTable
        {
            MajorVersion = major,
            MinorVersion = minor,
            Strikes = strikes,
        };
    }
}
