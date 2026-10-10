using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// prop — Glyph Properties Table (Apple Advanced Typography)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>prop</c> table: a 16-bit property word for each glyph, describing its directional class, whether it floats or hangs, and how complementary bracketing glyphs and right attachment behave.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is an Apple Advanced Typography (AAT) table and is not part of OpenType. Apple documents it in the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html">TrueType Reference Manual, <c>prop</c> table</see>.</description></item>
/// <item><description>Three versions of the table have existed. Version 1.0 supports floaters, hanging punctuation, symmetric swapping, and the twelve original directional classes; version 2.0 adds the attaches-on-right property; version 3.0 adds the directional classes introduced by Unicode 3.0. This parser accepts any of the three and rejects any other major version.</description></item>
/// <item><description>The eight-byte <see cref="Header"/> is followed immediately by the lookup table, and only when <see cref="Format"/> is <see cref="FormatLookup"/>. A format of <see cref="FormatNoLookup"/> declares that no lookup data is present, so every glyph then takes <see cref="DefaultProperties"/>.</description></item>
/// <item><description>A lookup value is the glyph's property word itself — not an offset and not an index into a second array — so <see cref="GetProperties"/> needs no further indirection. The bit meanings are given by the mask constants on this type.</description></item>
/// <item><description>Any of the five lookup formats may be used. A format 2 table suits glyphs that share properties, a format 6 table suits a font in which only a few glyphs differ from the default, and formats 0 and 8 suit a font with many distinct segments. Format 0 is an untrimmed array with no declared length, so supply a <see cref="FontFace"/> as the parse context when the table may use it: the glyph count is then taken from <c>maxp.numGlyphs</c>, which is what the shared lookup dispatcher expects.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual, Table Components</see> for the lookup table formats and <see cref="LookupTable"/> for their managed representation.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html">TrueType Reference Manual: The <c>prop</c> table</seealso>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
public sealed record PropTable : IFontTable<PropTable>
{
    /// <summary>Gets the AAT table tag <c>prop</c>.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html">TrueType Reference Manual: The <c>prop</c> table</seealso>
    public static Tag Tag => "prop";

    /// <summary>Gets the table version.</summary>
    /// <remarks>The documented versions are 1.0, 2.0, and 3.0; <see cref="MajorVersion"/> is one of 1, 2, or 3.</remarks>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="MinorVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html">TrueType Reference Manual: The <c>prop</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the major half of <see cref="Version"/>, which is 1, 2, or 3.</summary>
    /// <seealso cref="Version"/>
    /// <seealso cref="MinorVersion"/>
    public ushort MajorVersion => (ushort)Version.IntegerPart;

    /// <summary>Gets the minor half of <see cref="Version"/>.</summary>
    /// <seealso cref="Version"/>
    /// <seealso cref="MajorVersion"/>
    public ushort MinorVersion => Version.FractionPart;

    /// <summary>Gets the table format: <see cref="FormatNoLookup"/> when the table carries no lookup data, or <see cref="FormatLookup"/> when the lookup table follows the header.</summary>
    /// <seealso cref="FormatNoLookup"/>
    /// <seealso cref="FormatLookup"/>
    /// <seealso cref="Lookup"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html">TrueType Reference Manual: The <c>prop</c> table</seealso>
    public ushort Format { get; init; }

    /// <summary>Gets the property word applied to a glyph that the lookup table does not cover.</summary>
    /// <remarks>When <see cref="Format"/> is <see cref="FormatNoLookup"/> every glyph takes this word, because no lookup data is present at all.</remarks>
    /// <seealso cref="GetProperties(int)"/>
    /// <seealso cref="GetDirectionalityClass(ushort)"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html">TrueType Reference Manual: The <c>prop</c> table</seealso>
    public ushort DefaultProperties { get; init; }

    /// <summary>Gets the lookup table that maps glyph indices onto property words, or <see langword="null"/> when <see cref="Format"/> is <see cref="FormatNoLookup"/>.</summary>
    /// <remarks>The lookup value is interpreted as a property word; see the mask constants on <see cref="PropTable"/> for its bit meanings.</remarks>
    /// <seealso cref="LookupTable"/>
    /// <seealso cref="GetProperties(int)"/>
    public LookupTable? Lookup { get; init; }

    /// <summary>Gets a value indicating whether the table carries lookup data, that is, whether <see cref="Format"/> is <see cref="FormatLookup"/>.</summary>
    /// <seealso cref="Format"/>
    /// <seealso cref="Lookup"/>
    public bool HasLookup => Lookup is not null;

    /// <summary>Gets the number of lookup units in the table, or zero when no lookup data is present.</summary>
    /// <remarks>
    /// The unit is the lookup table's own, so the count is a glyph count only for the untrimmed and trimmed array formats (0, 8, and 10). For formats 2, 4, and 6 it is the number of segments or entries, not the number of glyphs the table can answer for. See <see cref="LookupTable.Count"/>.
    /// </remarks>
    /// <seealso cref="LookupTable.Count"/>
    public int Count => Lookup?.Count ?? 0;

    /// <summary>Gets the property word of a glyph.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <returns>The glyph's property word, or <see cref="DefaultProperties"/> when the lookup table does not cover <paramref name="glyphIndex"/> or when no lookup data is present.</returns>
    /// <remarks>The word's bit meanings are given by the mask constants on <see cref="PropTable"/>, and the helper predicates of this type decode individual fields of it.</remarks>
    /// <seealso cref="DefaultProperties"/>
    /// <seealso cref="Lookup"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html">TrueType Reference Manual: The <c>prop</c> table</seealso>
    public ushort GetProperties(int glyphIndex)
    {
        if (Lookup is { } lookup && lookup.TryGetValue(glyphIndex, out uint value))
            return (ushort)value;

        return DefaultProperties;
    }

    /// <summary>Gets a value indicating whether a property word marks the glyph as a floater, such as a floating accent or vowel mark.</summary>
    /// <param name="properties">The property word to test.</param>
    /// <returns><see langword="true"/> when bit 15, <see cref="FloaterMask"/>, is set.</returns>
    /// <remarks>Apple stresses that this bit must be set only for glyphs whose advance width is zero.</remarks>
    /// <seealso cref="FloaterMask"/>
    public static bool IsFloater(ushort properties) => (properties & FloaterMask) != 0;

    /// <summary>Gets a value indicating whether a property word lets the glyph hang off the left edge of a horizontal line or the top edge of a vertical line.</summary>
    /// <param name="properties">The property word to test.</param>
    /// <returns><see langword="true"/> when bit 14, <see cref="HangLeftMask"/>, is set.</returns>
    /// <seealso cref="HangLeftMask"/>
    public static bool CanHangLeft(ushort properties) => (properties & HangLeftMask) != 0;

    /// <summary>Gets a value indicating whether a property word lets the glyph hang off the right edge of a horizontal line or the bottom edge of a vertical line.</summary>
    /// <param name="properties">The property word to test.</param>
    /// <returns><see langword="true"/> when bit 13, <see cref="HangRightMask"/>, is set.</returns>
    /// <seealso cref="HangRightMask"/>
    public static bool CanHangRight(ushort properties) => (properties & HangRightMask) != 0;

    /// <summary>Gets a value indicating whether a property word makes the glyph attach to the glyph physically to its right.</summary>
    /// <param name="properties">The property word to test.</param>
    /// <returns><see langword="true"/> when bit 7, <see cref="AttachOnRightMask"/>, is set.</returns>
    /// <remarks>The property was added in version 2.0, so it is invalid in a version 1.0 table.</remarks>
    /// <seealso cref="AttachOnRightMask"/>
    /// <seealso cref="Version"/>
    public static bool AttachesOnRight(ushort properties) => (properties & AttachOnRightMask) != 0;

    /// <summary>Gets the glyph's directionality class from a property word.</summary>
    /// <param name="properties">The property word to decode.</param>
    /// <returns>The five-bit directionality class, <c>properties &amp; 0x001F</c>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Classes 0, 1, and 2 are strong: left-to-right, right-to-left non-Arabic, and Arabic right-to-left respectively.</description></item>
    /// <item><description>Classes 3 through 7 are weak number types: European number, European number separator, European number terminator, Arabic number, and common number separator.</description></item>
    /// <item><description>Classes 8 through 11 are neutral: block separator, segment separator, whitespace, and other neutrals. Classes 12 through 31 are reserved, some of them defined by Unicode 3.0; using those requires a version 3.0 or later table.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="DirectionalityMask"/>
    /// <seealso cref="Version"/>
    public static ushort GetDirectionalityClass(ushort properties) => (ushort)(properties & DirectionalityMask);

    /// <summary>Gets the four-bit offset to a glyph's complementary bracketing glyph from a property word.</summary>
    /// <param name="properties">The property word to decode.</param>
    /// <returns>The offset in bits 8 through 11 of the property word, <c>(properties &amp; 0x0F00) &gt;&gt; 8</c>, or zero when the glyph has no complementary bracketing glyph.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The offset is a signed four-bit displacement from the glyph's own index to its complementary bracket, so <c>0x1100</c> on glyph 11 means the complementary glyph is at <c>11 + 1</c>.</description></item>
    /// <item><description>Whether the glyph at that offset is the left-to-right or the right-to-left version of the bracket is declared by <see cref="ComplementaryBracketMask"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BracketOffsetMask"/>
    /// <seealso cref="ComplementaryBracketMask"/>
    public static ushort GetBracketOffset(ushort properties) => (ushort)((properties & BracketOffsetMask) >> 8);

    /// <summary>Format value declaring that no lookup data is present and that every glyph takes <see cref="DefaultProperties"/>.</summary>
    /// <seealso cref="Format"/>
    /// <seealso cref="FormatLookup"/>
    public const ushort FormatNoLookup = 0;

    /// <summary>Format value declaring that a lookup table follows the header.</summary>
    /// <seealso cref="Format"/>
    /// <seealso cref="FormatNoLookup"/>
    public const ushort FormatLookup = 1;

    /// <summary>Mask of the floater property, bit 15.</summary>
    /// <remarks>A floater is a non-spacing mark positioned with reference to a preceding base glyph. The bit must be set only for glyphs with a zero advance width.</remarks>
    /// <seealso cref="IsFloater(ushort)"/>
    public const ushort FloaterMask = 0x8000;

    /// <summary>Mask of the hang-left property, bit 14.</summary>
    /// <remarks>The glyph may hang off the left edge of a horizontal line or the top edge of a vertical line.</remarks>
    /// <seealso cref="CanHangLeft(ushort)"/>
    public const ushort HangLeftMask = 0x4000;

    /// <summary>Mask of the hang-right property, bit 13.</summary>
    /// <remarks>The glyph may hang off the right edge of a horizontal line or the bottom edge of a vertical line.</remarks>
    /// <seealso cref="CanHangRight(ushort)"/>
    public const ushort HangRightMask = 0x2000;

    /// <summary>Mask of the complementary-bracket flag, bit 12.</summary>
    /// <remarks>When the bit is set and the glyph resolves into a right-to-left directional grouping, the complementary bracketing glyph named by <see cref="BracketOffsetMask"/> is substituted for this glyph.</remarks>
    /// <seealso cref="GetBracketOffset(ushort)"/>
    public const ushort ComplementaryBracketMask = 0x1000;

    /// <summary>Mask of the complementary bracketing glyph offset, bits 8 through 11.</summary>
    /// <remarks>The field is a four-bit signed displacement from the glyph's own index to its complementary bracket, or zero when the glyph has none.</remarks>
    /// <seealso cref="GetBracketOffset(ushort)"/>
    public const ushort BracketOffsetMask = 0x0F00;

    /// <summary>Mask of the attaches-on-right property, bit 7.</summary>
    /// <remarks>Added in version 2.0; invalid in a version 1.0 table. The property stops justification from adding space between the glyph and the glyph to its right.</remarks>
    /// <seealso cref="AttachesOnRight(ushort)"/>
    public const ushort AttachOnRightMask = 0x0080;

    /// <summary>Mask of the two reserved bits, bits 5 and 6, which must be zero.</summary>
    /// <remarks>The mask is reserved for future properties and is documented as having to be set to zero.</remarks>
    public const ushort ReservedMask = 0x0060;

    /// <summary>Mask of the directionality class, bits 0 through 4.</summary>
    /// <seealso cref="GetDirectionalityClass(ushort)"/>
    public const ushort DirectionalityMask = 0x001F;

    /// <summary>Size, in bytes, of the fixed-layout <see cref="Header"/>.</summary>
    /// <remarks>The lookup table, when present, begins at this offset.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Format"/>
    public const int HeaderSize = 8;

    /// <summary>The eight-byte fixed-layout <c>prop</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Fields are stored in big-endian order at the offsets defined by the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html"><c>prop</c> table specification</see>.</description></item>
    /// <item><description>The lookup table follows this header immediately, with no intervening offset field of its own; a format of <see cref="FormatNoLookup"/> means the table ends here.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html">TrueType Reference Manual: The <c>prop</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0.</summary>
        /// <remarks>1.0, 2.0, or 3.0. See <see cref="Version"/> for what each version introduced.</remarks>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html">TrueType Reference Manual: The <c>prop</c> table</seealso>
        public Fixed Version;            // +0

        /// <summary>The table format at byte offset 4: <see cref="FormatNoLookup"/> or <see cref="FormatLookup"/>.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html">TrueType Reference Manual: The <c>prop</c> table</seealso>
        public ushort Format;            // +4

        /// <summary>The default property word at byte offset 6, applied to glyphs the lookup table does not cover.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html">TrueType Reference Manual: The <c>prop</c> table</seealso>
        public ushort DefaultProperties; // +6

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = new Fixed(BinaryPrimitives.ReverseEndianness(v.Version.Bits)),
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            DefaultProperties = BinaryPrimitives.ReverseEndianness(v.DefaultProperties),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the <c>prop</c> table.</param>
    /// <param name="context">A <see cref="FontFace"/> so that a format 0 lookup can be sized by <c>maxp.numGlyphs</c>, or <see langword="null"/>; a plain <see cref="int"/> glyph count is also accepted.</param>
    /// <returns>The parsed <c>prop</c> table.</returns>
    /// <exception cref="InvalidDataException">The major version is not 1, 2, or 3, or the format is not 0 or 1.</exception>
    /// <exception cref="EndOfStreamException">The header or the lookup table extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The table's major version must be 1, 2, or 3, and its format must be 0 or 1.</description></item>
    /// <item><description>A format 0 lookup declares no length of its own, so the glyph count reaches it through <paramref name="context"/>; with no usable context it is read to the end of the table data, which is correct here only because no region follows the lookup of this table.</description></item>
    /// </list>
    /// </remarks>
    static PropTable IRecord<PropTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version.IntegerPart is < 1 or > 3)
        {
            throw new InvalidDataException(
                $"'prop'.version is 0x{unchecked((uint)header.Version.Bits):X8}, expected 1.0, 2.0, or 3.0.");
        }

        if (header.Format is not (FormatNoLookup or FormatLookup))
            throw new InvalidDataException($"'prop'.format is {header.Format}, expected 0 or 1.");

        LookupTable? lookup = header.Format == FormatLookup
            ? LookupTable.Parse(ref cursor, GetGlyphCountContext(context))
            : null;

        return new PropTable
        {
            Version = header.Version,
            Format = header.Format,
            DefaultProperties = header.DefaultProperties,
            Lookup = lookup,
        };
    }

    /// <summary>Resolves the value a nested lookup table needs in order to read a length-less format 0 array.</summary>
    /// <param name="context">The context handed to <see cref="IRecord{T}.Parse(ref Cursor, object?)"/>: a <see cref="FontFace"/>, an <see cref="int"/> glyph count, or <see langword="null"/>.</param>
    /// <returns>The font's glyph count as an <see cref="int"/> when it can be obtained, or <see langword="null"/> so that the lookup is read to the end of the table data.</returns>
    /// <remarks>A format 0 lookup is a bare array with no declared length, so it must be told how many glyphs the font has. A <see cref="FontFace"/> context supplies the count from <c>maxp.numGlyphs</c>; an <see cref="int"/> context is taken as that count directly. The count is optional because a table that never uses a format 0 lookup does not need it.</remarks>
    /// <seealso cref="LookupTable.Parse(ref Cursor, object?)"/>
    private static object? GetGlyphCountContext(object? context) => context switch
    {
        int glyphCount => glyphCount,
        FontFace face when face.Directory.ContainsKey(MaxpTable.Tag) => face.GetTable<MaxpTable>().NumGlyphs,
        _ => null,
    };
}
