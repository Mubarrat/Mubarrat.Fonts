using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>bloc</c> table: the Apple Advanced Typography bitmap location table. Provides locators for monochrome or grayscale bitmap glyph data, paired with the <c>bdat</c> table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table's structure is identical to the OpenType <c>EBLC</c> table; the two are read through the shared <see cref="BitmapLocationParser"/>. This type exists to give the Apple tag its own identity and to pair with <see cref="BdatTable"/> rather than <see cref="EbdtTable"/>.</description></item>
/// <item><description>A <c>bloc</c> table and a <c>bdat</c> table are inextricably connected: if one is present, the other must be, and a change to one almost always requires a change to the other. The two are best read and written together.</description></item>
/// <item><description>Unlike <c>EBLC</c>, the <c>bloc</c> table is used in bitmap-only fonts. A font that carries a <c>bloc</c> table must also carry a <see cref="BhedTable"/> rather than a <see cref="HeadTable"/>, and must not carry <c>glyf</c> or <c>loca</c> tables.</description></item>
/// <item><description>The table declares strikes, each covering a glyph range at a particular pixels-per-em size. Each strike holds index subtables that locate the individual glyph bitmaps in the paired <c>bdat</c> table.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bloc.html">bitmap location table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BdatTable"/>
/// <seealso cref="BhedTable"/>
/// <seealso cref="EblcTable"/>
/// <seealso cref="BitmapLocationParser"/>
/// <seealso cref="BitmapStrike"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bloc.html">TrueType Reference Manual: The <c>bloc</c> table</seealso>
public sealed record BlocTable : IFontTable<BlocTable>, IRecord<BlocTable>
{
    /// <summary>The version value for the only defined bitmap location table format, 2.0.</summary>
    /// <value>The <see cref="Fixed"/> value whose raw bits are <c>0x00020000</c>.</value>
    /// <remarks>The specification documents <c>0x00020000</c> as the initial version of the table. The wire field is a <c>fixed32</c>.</remarks>
    public static readonly Fixed CurrentVersion = new(0x00020000);

    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bloc.html">TrueType Reference Manual: The <c>bloc</c> table</seealso>
    public static Tag Tag => "bloc";

    /// <summary>Gets the table major version. Always 2.</summary>
    /// <value>The constant <c>2</c> for a conforming <c>bloc</c> table.</value>
    /// <remarks>The field is shared with <see cref="EblcTable.MajorVersion"/>; see that property for the specification reference. The <c>bloc</c> table's structure is identical to <c>EBLC</c>'s, but the major version constant differs between the two because the tags have historically been documented separately.</remarks>
    /// <seealso cref="EblcTable.MajorVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bloc.html">TrueType Reference Manual: The <c>bloc</c> table</seealso>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the table minor version. Always 0.</summary>
    /// <value>The constant <c>0</c> for a conforming <c>bloc</c> table.</value>
    /// <remarks>The field is shared with <see cref="EblcTable.MinorVersion"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="EblcTable.MinorVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bloc.html">TrueType Reference Manual: The <c>bloc</c> table</seealso>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the strikes.</summary>
    /// <value>The list of <see cref="BitmapStrike"/> entries, one per pixels-per-em dimension pair declared by the table. Empty when the table declares no strikes.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The list is populated during parse from the table's strike array; each strike's index subtables are resolved eagerly at that point.</description></item>
    /// <item><description>The Apple documentation states that the strike sizes must be sorted in ascending order. The parser does not enforce the ordering; a validator may flag an out-of-order list.</description></item>
    /// <item><description>Strike order follows the on-disk order in the table, which is normally sorted by ppem.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetStrike(int, int)"/>
    /// <seealso cref="BitmapStrike"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bloc.html">TrueType Reference Manual: The <c>bloc</c> table</seealso>
    public IReadOnlyList<BitmapStrike> Strikes { get; init; } = [];

    /// <summary>Returns the strike for the requested ppem, or <c>null</c>.</summary>
    /// <param name="ppemX">The horizontal pixels per em to search for.</param>
    /// <param name="ppemY">The vertical pixels per em to search for.</param>
    /// <returns>The first <see cref="BitmapStrike"/> whose <see cref="BitmapStrike.PpemX"/> and <see cref="BitmapStrike.PpemY"/> match, or <c>null</c> when no strike matches.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup is linear over <see cref="Strikes"/>. The table is typically small, so the linear scan is not usually a concern.</description></item>
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
    /// <param name="cursor">Cursor positioned at the first byte of the <c>bloc</c> table.</param>
    /// <param name="context">A <see cref="FontFace"/> whose table directory provides the declared length for the strike-array bound check.</param>
    /// <returns>The parsed <c>bloc</c> table with its strikes resolved.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="FontFace"/>.</exception>
    /// <exception cref="InvalidDataException">The version is not 2.0, the strike count exceeds the safety limit, or the declared table length is too small for the strike array.</exception>
    /// <exception cref="EndOfStreamException">The strike array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>This is a thin wrapper around <see cref="BitmapLocationParser.Parse(ref Cursor, Tag, ushort, ushort)"/>, supplying the <c>bloc</c> tag and the <c>2.0</c> expected version.</description></item>
    /// <item><description>Both <c>EBLC</c> and <c>bloc</c> are read through the same parser; the parser is responsible for the header layout, strike array bounds, and the safety limit on strike count.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BitmapLocationParser.Parse(ref Cursor, Tag, ushort, ushort)"/>
    /// <seealso cref="FontFace.GetTable{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bloc.html">TrueType Reference Manual: The <c>bloc</c> table</seealso>
    static BlocTable IRecord<BlocTable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not FontFace face)
        {
            throw new InvalidOperationException(
                $"{nameof(BlocTable)}.Parse requires a {nameof(FontFace)} context.");
        }

        var (major, minor, strikes) = BitmapLocationParser.Parse(ref cursor, Tag, 2, 0);
        return new BlocTable
        {
            MajorVersion = major,
            MinorVersion = minor,
            Strikes = strikes,
        };
    }
}
