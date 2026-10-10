using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>CBLC</c> table (version 3.0): Color Bitmap Location Table. Same layout as EBLC; adds support for a <c>bitDepth</c> of 32 (BGRA color).</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <c>CBLC</c> table stores the index structures that locate colour bitmap data; the <c>CBDT</c> table stores the data itself. Parsing this table does not read the data side; a subsequent <see cref="CbdtTable"/> parse uses the strikes produced here.</description></item>
/// <item><description>The layout is shared with <c>EBLC</c>; only the version (3.0) and the recognised set of <c>bitDepth</c> values differ. EBLC allows 1, 2, 4, and 8; CBLC additionally allows 32 for BGRA colour.</description></item>
/// <item><description>Both tables are read through the same parser, <see cref="BitmapLocationParser.Parse(ref Cursor, Tag, ushort, ushort)"/>, which is parameterised on the expected tag and version.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cblc">CBLC table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CbdtTable"/>
/// <seealso cref="EblcTable"/>
/// <seealso cref="BitmapLocationParser"/>
/// <seealso cref="BitmapStrike"/>
/// <seealso cref="FontFace"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cblc">OpenType specification: CBLC table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: CBDT table</seealso>
public sealed record CblcTable : IFontTable<CblcTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cblc">OpenType specification: CBLC table</seealso>
    public static Tag Tag => "CBLC";

    /// <summary>Gets the table major version. Always 3.</summary>
    /// <value>The constant <c>3</c> for a conforming CBLC table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cblc"><c>majorVersion</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MinorVersion"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cblc">OpenType specification: <c>majorVersion</c></seealso>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the table minor version. Always 0.</summary>
    /// <value>The constant <c>0</c> for a conforming CBLC table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cblc"><c>minorVersion</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MajorVersion"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cblc">OpenType specification: <c>minorVersion</c></seealso>
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
    /// <param name="cursor">Cursor positioned at the first byte of the CBLC table.</param>
    /// <param name="context">A <see cref="FontFace"/> whose table directory provides the declared length for the strike-array bound check.</param>
    /// <returns>The parsed CBLC table with its strikes resolved.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="FontFace"/>.</exception>
    /// <exception cref="InvalidDataException">The version is not 3.0, the strike count exceeds the safety limit, or the declared table length is too small for the strike array.</exception>
    /// <exception cref="EndOfStreamException">The strike array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>This is a thin wrapper around <see cref="BitmapLocationParser.Parse(ref Cursor, Tag, ushort, ushort)"/>, supplying the CBLC tag and the <c>3.0</c> expected version.</description></item>
    /// <item><description>Both EBLC and CBLC are read through the same parser; the parser is responsible for the header layout, strike array bounds, and the safety limit on strike count.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BitmapLocationParser.Parse(ref Cursor, Tag, ushort, ushort)"/>
    /// <seealso cref="FontFace.GetTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cblc">OpenType specification: CBLC table</seealso>
    static CblcTable IRecord<CblcTable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not FontFace face)
            throw new InvalidOperationException(
                $"{nameof(CblcTable)}.Parse requires a {nameof(FontFace)} context.");

        var (major, minor, strikes) = BitmapLocationParser.Parse(ref cursor, Tag, 3, 0);
        return new CblcTable
        {
            MajorVersion = major,
            MinorVersion = minor,
            Strikes = strikes,
        };
    }
}
