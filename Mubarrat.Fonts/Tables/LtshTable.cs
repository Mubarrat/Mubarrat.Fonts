using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>LTSH</c> table: linear threshold. For each glyph, records the ppem size at and above which the glyph's advance width can be assumed to scale linearly rather than being adjusted by instructions.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is only meaningful when bit 4 of <c>head.flags</c> is set. A <c>yPixels</c> value of 1 means the glyph scales linearly at all sizes, used for glyphs without sidebearing instructions.</description></item>
/// <item><description>The table's <c>numGlyphs</c> field must match <c>maxp.numGlyphs</c>; the parser rejects the table otherwise.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ltsh">LTSH table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MaxpTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ltsh">OpenType specification: LTSH table</seealso>
public sealed record LtshTable : IFontTable<LtshTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ltsh">OpenType specification: LTSH table</seealso>
    public static Tag Tag => "LTSH";

    /// <summary>Gets the table version. Always 0.</summary>
    /// <value>The constant <c>0</c> for a conforming LTSH table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ltsh"><c>version</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="YPixels"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ltsh">OpenType specification: <c>version</c></seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the per-glyph linear threshold, in ppem. Length is <c>maxp.numGlyphs</c>.</summary>
    /// <value>The ordered list of per-glyph thresholds; index <c>i</c> is glyph ID <c>i</c>. Values are ppem sizes; a value of <c>1</c> means the glyph scales linearly at every size.</value>
    /// <seealso cref="GetThreshold(int)"/>
    /// <seealso cref="NumGlyphs"/>
    public IReadOnlyList<byte> YPixels { get; init; } = [];

    /// <summary>Gets the number of glyphs covered by the table.</summary>
    /// <value>The size of the <see cref="YPixels"/> list.</value>
    /// <seealso cref="YPixels"/>
    public int NumGlyphs => YPixels.Count;

    /// <summary>Returns the linear threshold for a glyph, or 1 (always linear) when the glyph ID is out of range.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The ppem size at and above which the glyph is assumed to scale linearly, or <c>1</c> when <paramref name="glyphId"/> is out of range.</returns>
    /// <remarks>The lookup is constant-time; the glyph ID directly indexes into <see cref="YPixels"/>. Returning <c>1</c> for out-of-range IDs matches the specification's "always linear" default.</remarks>
    /// <example>
    /// <code>
    /// byte threshold = ltsh.GetThreshold(glyphId);
    /// </code>
    /// </example>
    /// <seealso cref="YPixels"/>
    public byte GetThreshold(int glyphId) =>
        (uint)glyphId < (uint)YPixels.Count ? YPixels[glyphId] : (byte)1;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the LTSH table.</param>
    /// <param name="context">A <see cref="FontFace"/> whose <c>maxp</c> table provides the glyph count.</param>
    /// <returns>The parsed LTSH table.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="FontFace"/>.</exception>
    /// <exception cref="InvalidDataException">The version is not 0, or the table's glyph count does not match <c>maxp.numGlyphs</c>.</exception>
    /// <exception cref="EndOfStreamException">The header or per-glyph array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The declared glyph count in the LTSH table must equal <c>maxp.numGlyphs</c>; a mismatch is a structural error and the parser rejects it rather than reading a mismatched array.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ltsh">LTSH table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="YPixels"/>
    /// <seealso cref="FontFace.GetTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ltsh">OpenType specification: LTSH table</seealso>
    public static LtshTable Parse(ref Cursor cursor, object? context)
    {
        var face = (FontFace)context!;
        int maxpGlyphs = face.GetTable<MaxpTable>().NumGlyphs;

        ushort version = cursor.ReadUInt16();
        if (version != 0)
            throw new InvalidDataException($"'LTSH'.version is {version}, expected 0.");

        int numGlyphs = cursor.ReadUInt16();
        if (numGlyphs != maxpGlyphs)
            throw new InvalidDataException($"'LTSH'.numGlyphs is {numGlyphs}, but 'maxp'.numGlyphs is {maxpGlyphs}.");

        var yPixels = cursor.ReadUInt8Array(numGlyphs);

        return new LtshTable { Version = version, YPixels = yPixels };
    }
}
