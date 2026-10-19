using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Sfnt;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>prop</c> table: glyph properties for AAT fonts. Associates each glyph with a 16-bit property word describing floaters, hanging punctuation, bidi mirroring, bracket complements, right-attachment, and the Unicode directionality class.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The property word is uniform across all three versions: <c>0x8000</c> floater, <c>0x4000</c> left/top-hanging, <c>0x2000</c> right/bottom-hanging, <c>0x1000</c> bidi-mirrored, <c>0x0F00</c> a sign-extended 4-bit complementary-bracket glyph offset, <c>0x0080</c> attach-on-right (version 2.0 and later), <c>0x0060</c> reserved (must be zero), and <c>0x001F</c> the directionality class.</description></item>
/// <item><description><b>Version gating.</b> 1.0 supports the four original directionality classes and the four upper property bits; 2.0 adds the attach-on-right bit; 3.0 uses all five directionality bits for Unicode 3.0 additions.</description></item>
/// <item><description><b>Lookup value semantics differ from <c>opbd</c>.</b> For lookup formats 0, 2, 6, and 8, the stored value <em>is</em> the property word. Only format 4 stores an offset to a per-glyph value array. The parser relies on <see cref="LookupTable.TryGetValue"/> resolving format 4's array indirection internally, so the returned <c>uint</c> is always the property word.</description></item>
/// <item><description><b>Format 0 means no lookup.</b> When the header's format field is <c>0</c>, no lookup table follows and every glyph uses <see cref="DefaultProperties"/>.</description></item>
/// <item><description>All multi-byte reads use the sfnt default of big-endian.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html"><c>prop</c> table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PropProperties"/>
/// <seealso cref="PropDirectionalityClass"/>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html">TrueType Reference Manual: The <c>prop</c> table</seealso>
public sealed record PropTable : IFontTable<PropTable>, IRecord<PropTable>
{
    /// <summary>The first version defined by the specification.</summary>
    public const ushort Version1 = 1;

    /// <summary>The version that adds the attach-on-right property bit.</summary>
    public const ushort Version2 = 2;

    /// <summary>The current version, which adds the Unicode 3.0 directionality classes.</summary>
    public const ushort Version3 = 3;

    /// <inheritdoc/>
    public static Tag Tag => "prop";

    /// <summary>Gets the table version.</summary>
    /// <value>The 16.16 <c>Fixed</c> version read from the header. One of 1.0, 2.0, or 3.0; the parser rejects any other value.</value>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="Version1"/>
    /// <seealso cref="Version2"/>
    /// <seealso cref="Version3"/>
    public Fixed Version { get; init; }

    /// <summary>Gets the major version component of <see cref="Version"/>.</summary>
    /// <value>1, 2, or 3, derived from the high 16 bits of the raw <see cref="Fixed"/> value.</value>
    /// <seealso cref="Version"/>
    public ushort MajorVersion => (ushort)(Version.Bits >> 16);

    /// <summary>Gets the property word applied to glyphs that the lookup does not cover.</summary>
    /// <value>The raw <c>default properties</c> field from the header. Exposed as a <see cref="PropProperties"/> wrapper for the bit accessors.</value>
    /// <seealso cref="Default"/>
    /// <seealso cref="GetProperties(int)"/>
    public ushort DefaultProperties { get; init; }

    /// <summary>Gets the default property word as a <see cref="PropProperties"/> wrapper.</summary>
    /// <value>Equivalent to <c>new PropProperties(DefaultProperties)</c>.</value>
    /// <seealso cref="DefaultProperties"/>
    /// <seealso cref="GetProperties(int)"/>
    public PropProperties Default => new(DefaultProperties);

    /// <summary>Gets the lookup table that maps glyphs to property words, or <see langword="null"/> when the table carries no lookup.</summary>
    /// <value>
    /// <list type="bullet">
    /// <item><description>When the header's format is <c>0</c>: <see langword="null"/>, and every glyph uses <see cref="Default"/>.</description></item>
    /// <item><description>When the header's format is <c>1</c>: a <see cref="LookupTable"/> whose value for a covered glyph is the glyph's property word. For format 4 lookups, the value array's element at the glyph's segment offset is returned, not the raw segment offset.</description></item>
    /// </list>
    /// </value>
    /// <seealso cref="GetProperties(int)"/>
    /// <seealso cref="LookupTable"/>
    public LookupTable? Lookup { get; init; }

    /// <summary>Gets the property word for a glyph.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <returns>The glyph's property word, wrapped in a <see cref="PropProperties"/>; the <see cref="Default"/> when the lookup is absent or does not cover <paramref name="glyphId"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup value is cast to <c>ushort</c> after the lookup; for a well-formed table it always fits. A malformed lookup value whose high 16 bits are non-zero is truncated, not rejected.</description></item>
    /// <item><description>Negative glyph IDs are not covered by any lookup and return <see cref="Default"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Lookup"/>
    /// <seealso cref="Default"/>
    /// <seealso cref="PropProperties"/>
    public PropProperties GetProperties(int glyphId) =>
        Lookup is not null && Lookup.TryGetValue(glyphId, out uint raw)
            ? new((ushort)raw)
            : Default;

    static PropTable IRecord<PropTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        ushort major = (ushort)(header.Version.Bits >> 16);
        ushort minor = (ushort)(header.Version.Bits & 0xFFFF);
        if (minor != 0 || major < Version1 || major > Version3)
            throw new InvalidDataException($"'prop'.version is {major}.{minor}, expected 1.0, 2.0, or 3.0.");

        if (header.Format > 1)
            throw new InvalidDataException($"'prop'.format is {header.Format}, expected 0 (no lookup) or 1 (lookup present).");

        LookupTable? lookup = header.Format == 1
            ? cursor.ReadRecord<LookupTable>(new LookupTable.Context(context is SfntFontFace face
                ? face.GetTable<MaxpTable>().NumGlyphs
                : throw new InvalidDataException("'prop' with lookup data requires an SFNT font face context to obtain the glyph count.")))
            : null;

        return new()
        {
            Version = header.Version,
            DefaultProperties = header.DefaultProperties,
            Lookup = lookup,
        };
    }

    /// <summary>The 8-byte <c>prop</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The version is a 16.16 <c>Fixed</c> value; the format and default-properties fields are <c>UInt16</c>, so the reversal is field-by-field.</description></item>
    /// <item><description>The lookup table (when present) begins immediately after this header.</description></item>
    /// <item><description>The <c>&lt;Clone&gt;$()</c> method generated by the <c>record struct</c> declaration must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0, as a <c>Fixed</c> 16.16 value.</summary>
        /// <value>One of 1.0, 2.0, or 3.0.</value>
        /// <seealso cref="PropTable.Version"/>
        public Fixed Version;              // +0

        /// <summary>The format discriminant at byte offset 4.</summary>
        /// <value><c>0</c> when no lookup follows, <c>1</c> when a lookup is present.</value>
        /// <seealso cref="PropTable.Lookup"/>
        public ushort Format;              // +4

        /// <summary>The default property word at byte offset 6.</summary>
        /// <value>Applied to glyphs the lookup does not cover.</value>
        /// <seealso cref="PropTable.DefaultProperties"/>
        public ushort DefaultProperties;   // +6

        /// <summary>Reverses the byte order of every multi-byte field.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>The version is reversed through <see cref="Fixed.ReverseEndianness(Fixed)"/> so the 16.16 representation is preserved; the two <c>UInt16</c> fields use <see cref="BinaryPrimitives.ReverseEndianness(ushort)"/>.</remarks>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = Fixed.ReverseEndianness(value.Version),
            Format = BinaryPrimitives.ReverseEndianness(value.Format),
            DefaultProperties = BinaryPrimitives.ReverseEndianness(value.DefaultProperties),
        };
    }
}

/// <summary>A 16-bit glyph property word with bit- and field-accessors for the flags the specification defines.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The word is not a wire struct — it wraps the semantic <c>ushort</c> value produced by <see cref="PropTable.GetProperties(int)"/> or <see cref="PropTable.DefaultProperties"/>.</description></item>
/// <item><description><b>Version gating.</b> <see cref="AttachesOnRight"/> is only meaningful for version 2.0 and later; a version 1.0 table that sets the bit is malformed per the specification. <see cref="DirectionalityClass"/> uses only the low four bits in version 1.0, and all five bits in version 3.0.</description></item>
/// <item><description><b>Bracket offset.</b> The 4-bit field at <c>0x0F00</c> is sign-extended: <c>0x0</c> means offset 0 (no complementary glyph), <c>0x1</c> through <c>0x7</c> mean <c>+1</c> through <c>+7</c>, and <c>0x8</c> through <c>0xF</c> mean <c>-8</c> through <c>-1</c>. See <see cref="BracketOffset"/>.</description></item>
/// <item><description>The reserved bits <c>0x0060</c> must be zero; the wrapper preserves whatever value they carry, and a diagnostics analyzer is the appropriate place to flag non-zero reserved bits.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PropTable"/>
/// <seealso cref="PropDirectionalityClass"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html">TrueType Reference Manual: The <c>prop</c> table</seealso>
public readonly record struct PropProperties(ushort Value)
{
    /// <summary>Gets a value indicating whether the glyph is a floater (for example a floating accent or vowel mark).</summary>
    /// <value><see langword="true"/> when bit <c>0x8000</c> is set.</value>
    /// <remarks>The specification requires this bit only for glyphs with zero advance width.</remarks>
    public bool IsFloater => (Value & 0x8000) != 0;

    /// <summary>Gets a value indicating whether the glyph can hang off the left edge of a horizontal line or the top edge of a vertical line.</summary>
    /// <value><see langword="true"/> when bit <c>0x4000</c> is set.</value>
    public bool CanHangLeft => (Value & 0x4000) != 0;

    /// <summary>Gets a value indicating whether the glyph can hang off the right edge of a horizontal line or the bottom edge of a vertical line.</summary>
    /// <value><see langword="true"/> when bit <c>0x2000</c> is set.</value>
    public bool CanHangRight => (Value & 0x2000) != 0;

    /// <summary>Gets a value indicating whether the complementary bracketing glyph should be substituted when the glyph ends up in a right-to-left directional grouping.</summary>
    /// <value><see langword="true"/> when bit <c>0x1000</c> is set.</value>
    /// <remarks>
    /// The complementary glyph is located by adding <see cref="BracketOffset"/> to the current glyph ID. A well-formed font sets this bit only for bracketing glyphs with a non-zero offset.
    /// </remarks>
    /// <seealso cref="BracketOffset"/>
    public bool IsBidiMirrored => (Value & 0x1000) != 0;

    /// <summary>Gets the signed 4-bit offset to the complementary bracketing glyph.</summary>
    /// <value>An integer in <c>[-8, +7]</c>. <c>0</c> means the glyph has no complementary bracketing glyph.</value>
    /// <remarks>
    /// The 4-bit field at <c>0x0F00</c> is sign-extended: the top bit of the 4-bit field is the sign bit. The bit trick <c>(short)(Value &lt;&lt; 4) &gt;&gt; 12</c> performs the extraction and sign extension in two instructions: shifting left by 4 moves the 4-bit field into the top 4 bits of a <c>short</c> (promoting the <c>ushort</c> to <c>int</c> first), casting to <c>short</c> truncates to 16 bits, and the arithmetic right shift by 12 promotes back to <c>int</c> and sign-extends.
    /// </remarks>
    /// <seealso cref="IsBidiMirrored"/>
    public int BracketOffset => (short)(Value << 4) >> 12;

    /// <summary>Gets a value indicating whether the glyph always attaches to the glyph physically to its right.</summary>
    /// <value><see langword="true"/> when bit <c>0x0080</c> is set.</value>
    /// <remarks>Introduced in version 2.0 of the table; a version 1.0 font that sets this bit is malformed.</remarks>
    public bool AttachesOnRight => (Value & 0x0080) != 0;

    /// <summary>Gets a value indicating whether any of the reserved bits <c>0x0060</c> are set.</summary>
    /// <value><see langword="true"/> when at least one reserved bit is set.</value>
    /// <remarks>The specification requires these bits to be zero. The wrapper exposes them for a diagnostics analyzer; the parser does not reject fonts that set them.</remarks>
    public bool HasReservedBitsSet => (Value & 0x0060) != 0;

    /// <summary>Gets the Unicode directionality class stored in the low five bits.</summary>
    /// <value>The raw 5-bit class value. Values 0–11 correspond to the classes the specification enumerates; 12–31 are reserved and populated by Unicode 3.0 additions.</value>
    /// <seealso cref="PropDirectionalityClass"/>
    /// <seealso cref="IsReservedDirectionalityClass"/>
    public PropDirectionalityClass DirectionalityClass => (PropDirectionalityClass)(Value & 0x001F);

    /// <summary>Gets a value indicating whether <see cref="DirectionalityClass"/> holds a value the specification enumerates.</summary>
    /// <value><see langword="true"/> when the low five bits are 12 or greater.</value>
    /// <remarks>Values 12–31 are reserved for Unicode 3.0 and later additions; the wrapper exposes the raw class via <see cref="DirectionalityClass"/> regardless of whether it is enumerated.</remarks>
    public bool IsReservedDirectionalityClass => (Value & 0x001F) >= 12;
}

/// <summary>The Unicode directionality classes the specification defines for the low five bits of a <see cref="PropProperties"/> word.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Classes 0–2 are <em>strong</em> (left-to-right or right-to-left), 3–7 are <em>weak</em> (numeric), and 8–11 are <em>neutral</em> (block, segment, whitespace, and other neutral).</description></item>
/// <item><description>Classes 12–31 are reserved and reserved for Unicode 3.0 additions; the wrapper exposes them via <see cref="PropProperties.DirectionalityClass"/> as casts of the raw value.</description></item>
/// <item><description>Version 1.0 tables only use the low four bits; the fifth bit was added in version 3.0.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PropProperties.DirectionalityClass"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html">TrueType Reference Manual: The <c>prop</c> table</seealso>
public enum PropDirectionalityClass : byte
{
    /// <summary>Strong left-to-right.</summary>
    StrongLtr = 0,

    /// <summary>Strong right-to-left, non-Arabic.</summary>
    StrongRtl = 1,

    /// <summary>Arabic letters, right-to-left.</summary>
    Arabic = 2,

    /// <summary>European number.</summary>
    EuropeanNumber = 3,

    /// <summary>European number separator.</summary>
    EuropeanNumberSeparator = 4,

    /// <summary>European number terminator.</summary>
    EuropeanNumberTerminator = 5,

    /// <summary>Arabic number.</summary>
    ArabicNumber = 6,

    /// <summary>Common number separator.</summary>
    CommonNumberSeparator = 7,

    /// <summary>Block separator.</summary>
    BlockSeparator = 8,

    /// <summary>Segment separator.</summary>
    SegmentSeparator = 9,

    /// <summary>Whitespace.</summary>
    Whitespace = 10,

    /// <summary>Other neutrals.</summary>
    OtherNeutral = 11,
}
