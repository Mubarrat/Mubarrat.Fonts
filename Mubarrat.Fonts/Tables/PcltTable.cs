using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Mubarrat.Fonts.Tables;

/// <summary>The bits of the <c>characterComplement</c> field. Each bit independently identifies a symbol collection the font supports.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Bit 0 is <em>cleared</em> when the font elements are provided in Unicode order. This is the only inverted flag in the enum; a set bit indicates the font is <em>not</em> Unicode-ordered.</description></item>
/// <item><description>Bits 22–31 are collection support flags. Bits 1–21 are unused.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">PCLT table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PcltTable.CharacterComplement"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: PCLT table</seealso>
[Flags]
public enum CharacterComplement : ulong
{
    /// <summary>No collections declared.</summary>
    /// <remarks>All bits clear, including bit 0 — which the specification interprets as Unicode-ordered.</remarks>
    None = 0,

    /// <summary>Bit 0: set indicates the font is <em>not</em> in Unicode order.</summary>
    /// <remarks>This is an inverted flag: the bit's set state means non-Unicode, not the presence of a collection.</remarks>
    NonUnicodeIndexing = 1UL << 0,

    /// <summary>Bit 22: Code Page Extensions.</summary>
    CodePageExtensions = 1UL << 22,

    /// <summary>Bit 23: PostScript Extensions.</summary>
    PostScriptExtensions = 1UL << 23,

    /// <summary>Bit 24: Macintosh Extensions.</summary>
    MacintoshExtensions = 1UL << 24,

    /// <summary>Bit 25: PCL Extensions.</summary>
    PclExtensions = 1UL << 25,

    /// <summary>Bit 26: Accent Extensions (East and West Europe).</summary>
    AccentExtensions = 1UL << 26,

    /// <summary>Bit 27: Desktop Publishing Extensions.</summary>
    DesktopPublishingExtensions = 1UL << 27,

    /// <summary>Bit 28: Latin 5 extensions.</summary>
    Latin5Extensions = 1UL << 28,

    /// <summary>Bit 29: Latin 2 extensions.</summary>
    Latin2Extensions = 1UL << 29,

    /// <summary>Bit 30: Latin 1 extensions.</summary>
    Latin1Extensions = 1UL << 30,

    /// <summary>Bit 31: ASCII.</summary>
    Ascii = 1UL << 31,
}

/// <summary>The packed <c>style</c> word of the <c>PCLT</c> table. Combines the posture (bits 0–1), appearance width (bits 2–4), and structure (bits 5–9) fields into a single value whose members sit at their actual bit offsets, so <c>(PclStyle)Style</c> is directly usable.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Use the <c>*Mask</c> members to isolate a component, then compare against that component's named value. Example: <c>(style &amp; PclStyle.PostureMask) == PclStyle.ObliqueItalic</c> tests for italic posture.</description></item>
/// <item><description>The three component fields occupy disjoint bit ranges; the enum values are placed at their on-disk bit offsets so a mask-and-compare tests the right bits without shifting.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>style</c> field</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PcltTable.Style"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>style</c></seealso>
[Flags]
public enum PclStyle : ushort
{
    /// <summary>No bits set.</summary>
    /// <remarks>Structurally equivalent to the "zero" values of all three component fields.</remarks>
    None = 0,

    // ─────────── Posture — bits 0–1 ───────────

    /// <summary>0: upright.</summary>
    /// <remarks>Value <c>0</c>, so it is not distinguishable from <see cref="None"/> without also testing the mask.</remarks>
    Upright = 0b00 << 0,
    /// <summary>1: oblique, italic.</summary>
    ObliqueItalic = 0b01 << 0,
    /// <summary>2: alternate italic (backslanted, cursive, swash).</summary>
    AlternateItalic = 0b10 << 0,
    /// <summary>3: reserved.</summary>
    PostureReserved = 0b11 << 0,
    /// <summary>Mask isolating the posture field (bits 0–1).</summary>
    PostureMask = 0b11 << 0,

    // ─────────── Appearance width — bits 2–4 ───────────

    /// <summary>0: normal.</summary>
    /// <remarks>Value <c>0</c>; test against <see cref="WidthMask"/> to distinguish from a different component's zero.</remarks>
    NormalWidth = 0b000 << 2,
    /// <summary>1: condensed.</summary>
    Condensed = 0b001 << 2,
    /// <summary>2: compressed, extra condensed.</summary>
    CompressedExtraCondensed = 0b010 << 2,
    /// <summary>3: extra compressed.</summary>
    ExtraCompressed = 0b011 << 2,
    /// <summary>4: ultra compressed.</summary>
    UltraCompressed = 0b100 << 2,
    /// <summary>5: reserved.</summary>
    WidthReserved5 = 0b101 << 2,
    /// <summary>6: expanded, extended.</summary>
    ExpandedExtended = 0b110 << 2,
    /// <summary>7: extra expanded, extra extended.</summary>
    ExtraExpandedExtraExtended = 0b111 << 2,
    /// <summary>Mask isolating the width field (bits 2–4).</summary>
    WidthMask = 0b111 << 2,

    // ─────────── Structure — bits 5–9 ───────────

    /// <summary>0: solid (normal, black).</summary>
    /// <remarks>Value <c>0</c>; test against <see cref="StructureMask"/> to distinguish from a different component's zero.</remarks>
    Solid = 0 << 5,
    /// <summary>1: outline (hollow).</summary>
    Outline = 1 << 5,
    /// <summary>2: inline (incised, engraved).</summary>
    Inline = 2 << 5,
    /// <summary>3: contour, edged (antique, distressed).</summary>
    ContourEdged = 3 << 5,
    /// <summary>4: solid with shadow.</summary>
    SolidWithShadow = 4 << 5,
    /// <summary>5: outline with shadow.</summary>
    OutlineWithShadow = 5 << 5,
    /// <summary>6: inline with shadow.</summary>
    InlineWithShadow = 6 << 5,
    /// <summary>7: contour, or edged, with shadow.</summary>
    ContourEdgedWithShadow = 7 << 5,
    /// <summary>8: pattern filled.</summary>
    PatternFilled = 8 << 5,
    /// <summary>9: pattern filled #1.</summary>
    PatternFilled1 = 9 << 5,
    /// <summary>10: pattern filled #2.</summary>
    PatternFilled2 = 10 << 5,
    /// <summary>11: pattern filled #3.</summary>
    PatternFilled3 = 11 << 5,
    /// <summary>12: pattern filled with shadow.</summary>
    PatternFilledWithShadow = 12 << 5,
    /// <summary>13: pattern filled with shadow #1.</summary>
    PatternFilledWithShadow1 = 13 << 5,
    /// <summary>14: pattern filled with shadow #2.</summary>
    PatternFilledWithShadow2 = 14 << 5,
    /// <summary>15: pattern filled with shadow #3.</summary>
    PatternFilledWithShadow3 = 15 << 5,
    /// <summary>16: inverse.</summary>
    Inverse = 16 << 5,
    /// <summary>17: inverse with border.</summary>
    InverseWithBorder = 17 << 5,
    /// <summary>Mask isolating the structure field (bits 5–9).</summary>
    StructureMask = 0b11111 << 5,
}

/// <summary>The <c>PCLT</c> table: PCL 5 printer metadata. Included for compatibility with Hewlett-Packard PCL 5 printers; the specification discourages its use in new fonts.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table has no internal subtable structure; the 54-byte record described by <see cref="Header"/> is the entire table.</description></item>
/// <item><description>Only relevant for fonts intended to be printed on PCL 5 devices; fonts without PCL 5 support should omit the table entirely.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">PCLT table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PclStyle"/>
/// <seealso cref="CharacterComplement"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: PCLT table</seealso>
public sealed record PcltTable : IFontTable<PcltTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: PCLT table</seealso>
    public static Tag Tag => "PCLT";

    /// <summary>Gets the major version. Current is 1.</summary>
    /// <value>The constant <c>1</c> for a conforming PCLT table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>majorVersion</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MinorVersion"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>majorVersion</c></seealso>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version. Current is 0.</summary>
    /// <value>The constant <c>0</c> for a conforming PCLT table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>minorVersion</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MajorVersion"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>minorVersion</c></seealso>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the 32-bit font number: bit 31 = native vs. converted; bits 24–30 = vendor code; bits 0–23 = vendor-assigned ID.</summary>
    /// <value>The font's identifier as understood by PCL 5 devices, decoded from the font number field.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>fontNumber</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>fontNumber</c></seealso>
    public uint FontNumber { get; init; }

    /// <summary>Gets the width of the space character in font design units.</summary>
    /// <value>The PCL "pitch" value, in font design units.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>pitch</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="XHeight"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>pitch</c></seealso>
    public ushort Pitch { get; init; }

    /// <summary>Gets the optical x-height in font design units.</summary>
    /// <value>The x-height as understood by PCL 5 for optical sizing purposes.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>xHeight</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="CapHeight"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>xHeight</c></seealso>
    public ushort XHeight { get; init; }

    /// <summary>Gets the raw packed <c>style</c> word.</summary>
    /// <value>The packed <see cref="PclStyle"/> value combining posture, appearance width, and structure. Use the mask members to isolate each component.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>style</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="PclStyle"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>style</c></seealso>
    public PclStyle Style { get; init; }

    /// <summary>Gets the raw packed <c>typeFamily</c> word: bits 12–15 = vendor code, bits 0–11 = typeface family code. See <see cref="VendorCode"/> and <see cref="TypefaceFamilyCode"/>.</summary>
    /// <value>The packed 16-bit type-family word.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>typeFamily</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="VendorCode"/>
    /// <seealso cref="TypefaceFamilyCode"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>typeFamily</c></seealso>
    public ushort TypeFamily { get; init; }

    /// <summary>Gets the optical cap height in font design units.</summary>
    /// <value>The cap height as understood by PCL 5 for optical sizing purposes.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>capHeight</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="XHeight"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>capHeight</c></seealso>
    public ushort CapHeight { get; init; }

    /// <summary>Gets the raw packed <c>symbolSet</c> word: bits 5–15 = symbol set number, bits 0–4 = symbol set ID offset. See <see cref="SymbolSetNumber"/> and <see cref="SymbolSetId"/>.</summary>
    /// <value>The packed 16-bit symbol-set word.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>symbolSet</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="SymbolSetNumber"/>
    /// <seealso cref="SymbolSetId"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>symbolSet</c></seealso>
    public ushort SymbolSet { get; init; }

    /// <summary>Gets the 16-byte typeface string used in PCL font listings, decoded to the first NUL.</summary>
    /// <value>The typeface description as ASCII text; trailing NUL padding is trimmed. An all-NUL field yields the empty string.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>typeface</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="DecodePaddedAscii(ReadOnlySpan{byte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>typeface</c></seealso>
    public string Typeface { get; init; } = string.Empty;

    /// <summary>Gets the symbol collections declared by the font.</summary>
    /// <value>The <see cref="CharacterComplement"/> bits that describe which PCL symbol collections the font supports.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>characterComplement</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="CharacterComplement"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>characterComplement</c></seealso>
    public CharacterComplement CharacterComplement { get; init; }

    /// <summary>Gets the 6-byte file name used in PCL font listings, decoded to the first NUL. Bytes 0–2 are the family, byte 3 the treatment, bytes 4–5 the symbol-set mnemonic.</summary>
    /// <value>The file name as ASCII text; trailing NUL padding is trimmed. An all-NUL field yields the empty string.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>fileName</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="DecodePaddedAscii(ReadOnlySpan{byte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>fileName</c></seealso>
    public string FileName { get; init; } = string.Empty;

    /// <summary>Gets the PCL stroke weight, in the range −7 to 7.</summary>
    /// <value>A signed value; negative is lighter than normal, positive is heavier.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>strokeWeight</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="WidthType"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>strokeWeight</c></seealso>
    public sbyte StrokeWeight { get; init; }

    /// <summary>Gets the PCL appearance width, in the range −5 to 5.</summary>
    /// <value>A signed value; negative is narrower than normal, positive is wider.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>widthType</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="StrokeWeight"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>widthType</c></seealso>
    public sbyte WidthType { get; init; }

    /// <summary>Gets the raw <c>serifStyle</c> byte: bits 6–7 = class, bits 0–5 = shape.</summary>
    /// <value>The packed serif-style byte; use <see cref="SerifStyleClass"/> and <see cref="SerifStyleShape"/> for the components.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt"><c>serifStyle</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="SerifStyleClass"/>
    /// <seealso cref="SerifStyleShape"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: <c>serifStyle</c></seealso>
    public byte SerifStyle { get; init; }

    /// <summary>Gets the vendor code (bits 12–15 of <see cref="TypeFamily"/>).</summary>
    /// <value>The 4-bit vendor identifier assigned by HP.</value>
    /// <seealso cref="TypeFamily"/>
    /// <seealso cref="TypefaceFamilyCode"/>
    public int VendorCode => (TypeFamily >> 12) & 0x000F;

    /// <summary>Gets the typeface family code (bits 0–11 of <see cref="TypeFamily"/>), assigned by HP.</summary>
    /// <value>The 12-bit typeface family identifier.</value>
    /// <seealso cref="TypeFamily"/>
    /// <seealso cref="VendorCode"/>
    public int TypefaceFamilyCode => TypeFamily & 0x0FFF;

    /// <summary>Gets the symbol-set number (bits 5–15 of <see cref="SymbolSet"/>).</summary>
    /// <value>The 11-bit symbol-set number identifying the character set.</value>
    /// <seealso cref="SymbolSet"/>
    /// <seealso cref="SymbolSetId"/>
    public int SymbolSetNumber => (SymbolSet >> 5) & 0x07FF;

    /// <summary>Gets the symbol-set ID (bits 0–4 of <see cref="SymbolSet"/> plus 64).</summary>
    /// <value>The symbol-set ID byte, biased by 64 so that the value ranges from 64 to 95 as ASCII-compatible codes.</value>
    /// <seealso cref="SymbolSet"/>
    /// <seealso cref="SymbolSetNumber"/>
    public int SymbolSetId => (SymbolSet & 0x001F) + 64;

    /// <summary>Gets the serif-style class (bits 6–7 of <see cref="SerifStyle"/>).</summary>
    /// <value>The 2-bit serif class.</value>
    /// <seealso cref="SerifStyle"/>
    /// <seealso cref="SerifStyleShape"/>
    public int SerifStyleClass => (SerifStyle >> 6) & 0x03;

    /// <summary>Gets the serif-style shape (bits 0–5 of <see cref="SerifStyle"/>).</summary>
    /// <value>The 6-bit serif shape code.</value>
    /// <seealso cref="SerifStyle"/>
    /// <seealso cref="SerifStyleClass"/>
    public int SerifStyleShape => SerifStyle & 0x3F;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the PCLT table.</param>
    /// <param name="context">Unused. The table is self-contained.</param>
    /// <returns>The parsed PCLT table.</returns>
    /// <exception cref="EndOfStreamException">The header extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The entire table is a single 54-byte record, read in one call through <see cref="Cursor.ReadBigEndianStruct{T}"/>.</description></item>
    /// <item><description>The two ASCII fields (<c>typeface</c> and <c>fileName</c>) are extracted from the header's raw byte storage through inline-array spans and decoded as ASCII up to the first NUL.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">PCLT table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="DecodePaddedAscii(ReadOnlySpan{byte})"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: PCLT table</seealso>
    public static PcltTable Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        // The two ASCII fields are read out of the header's raw byte storage. No unsafe,
        // no copy — the spans alias the header's inline-array fields.
        ReadOnlySpan<byte> typefaceBytes =
            MemoryMarshal.AsBytes(new ReadOnlySpan<Ascii16>(in header.Typeface));
        ReadOnlySpan<byte> fileNameBytes =
            MemoryMarshal.AsBytes(new ReadOnlySpan<Ascii6>(in header.FileName));

        return new PcltTable
        {
            MajorVersion = header.MajorVersion,
            MinorVersion = header.MinorVersion,
            FontNumber = header.FontNumber,
            Pitch = header.Pitch,
            XHeight = header.XHeight,
            Style = (PclStyle)header.Style,
            TypeFamily = header.TypeFamily,
            CapHeight = header.CapHeight,
            SymbolSet = header.SymbolSet,
            Typeface = DecodePaddedAscii(typefaceBytes),
            CharacterComplement = (CharacterComplement)header.CharacterComplement,
            FileName = DecodePaddedAscii(fileNameBytes),
            StrokeWeight = header.StrokeWeight,
            WidthType = header.WidthType,
            SerifStyle = header.SerifStyle,
        };
    }

    /// <summary>Decodes a fixed-width ASCII field, terminating at the first NUL byte. If no NUL is present, the entire field is used.</summary>
    /// <param name="bytes">The raw fixed-width ASCII bytes, NUL-padded on the right if short.</param>
    /// <returns>The decoded ASCII string, trimmed at the first NUL.</returns>
    /// <remarks>The decode uses <see cref="Encoding.ASCII"/>, so bytes with the high bit set are replaced with <c>'?'</c> rather than being interpreted as UTF-8 or Latin-1.</remarks>
    /// <example>
    /// <code>
    /// string s = PcltTable.DecodePaddedAscii(new byte[] { (byte)'H', (byte)'e', 0, 0 });
    /// // s == "He"
    /// </code>
    /// </example>
    /// <seealso cref="Encoding.ASCII"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: PCLT table</seealso>
    public static string DecodePaddedAscii(ReadOnlySpan<byte> bytes)
    {
        int length = bytes.IndexOf((byte)0);
        if (length < 0) length = bytes.Length;
        return Encoding.ASCII.GetString(bytes[..length]);
    }

    /// <summary>16-byte opaque storage for the <c>typeface</c> ASCII string.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>An inline-array struct used as a fixed-size byte buffer inside <see cref="Header"/>; the field is NUL-padded to its full width when the string is shorter.</description></item>
    /// <item><description>Exposed so that <see cref="Parse"/> can obtain a <see cref="ReadOnlySpan{T}"/> over the underlying bytes without unsafe code.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    [InlineArray(16)]
    public struct Ascii16 { private byte _element0; }

    /// <summary>6-byte opaque storage for the <c>fileName</c> field.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>An inline-array struct used as a fixed-size byte buffer inside <see cref="Header"/>; the field is NUL-padded to its full width when the string is shorter.</description></item>
    /// <item><description>Exposed so that <see cref="Parse"/> can obtain a <see cref="ReadOnlySpan{T}"/> over the underlying bytes without unsafe code.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    [InlineArray(6)]
    public struct Ascii6 { private byte _element0; }

    /// <summary>The 54-byte <c>PCLT</c> table. Sequential layout, pack 1, no padding. The two ASCII fields are InlineArray byte buffers; <c>characterComplement</c> is a <see cref="ulong"/> so it can be reversed into spec-correct bit positions.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The entire PCLT table is this single 54-byte record; there are no subtables or offset references.</description></item>
    /// <item><description>The two <see cref="Ascii16"/> and <see cref="Ascii6"/> fields are opaque byte buffers; <see cref="Parse"/> extracts their bytes without unsafe code by way of <see cref="MemoryMarshal.AsBytes{T}(ReadOnlySpan{T})"/>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">PCLT table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="PcltTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/pclt">OpenType specification: PCLT table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the major version. Current is 1.</summary>
        /// <value>The constant <c>1</c> for a conforming PCLT table.</value>
        /// <seealso cref="MinorVersion"/>
        public ushort MajorVersion;       // +0

        /// <summary>Gets the minor version. Current is 0.</summary>
        /// <value>The constant <c>0</c> for a conforming PCLT table.</value>
        /// <seealso cref="MajorVersion"/>
        public ushort MinorVersion;       // +2

        /// <summary>Gets the packed 32-bit font number.</summary>
        /// <value>The font number whose components are described on <see cref="PcltTable.FontNumber"/>.</value>
        /// <seealso cref="PcltTable.FontNumber"/>
        public uint FontNumber;           // +4

        /// <summary>Gets the space-character width, in font design units.</summary>
        /// <value>The PCL pitch value.</value>
        /// <seealso cref="XHeight"/>
        public ushort Pitch;              // +8

        /// <summary>Gets the optical x-height, in font design units.</summary>
        /// <value>The x-height as understood by PCL 5.</value>
        /// <seealso cref="Pitch"/>
        public ushort XHeight;            // +10

        /// <summary>Gets the raw packed <c>style</c> word.</summary>
        /// <value>The packed <see cref="PclStyle"/> value.</value>
        /// <seealso cref="PclStyle"/>
        public ushort Style;              // +12

        /// <summary>Gets the raw packed <c>typeFamily</c> word.</summary>
        /// <value>The packed 16-bit type-family word; see <see cref="PcltTable.VendorCode"/> and <see cref="PcltTable.TypefaceFamilyCode"/>.</value>
        /// <seealso cref="CapHeight"/>
        public ushort TypeFamily;         // +14

        /// <summary>Gets the optical cap height, in font design units.</summary>
        /// <value>The cap height as understood by PCL 5.</value>
        /// <seealso cref="XHeight"/>
        public ushort CapHeight;          // +16

        /// <summary>Gets the raw packed <c>symbolSet</c> word.</summary>
        /// <value>The packed 16-bit symbol-set word; see <see cref="PcltTable.SymbolSetNumber"/> and <see cref="PcltTable.SymbolSetId"/>.</value>
        /// <seealso cref="Typeface"/>
        public ushort SymbolSet;          // +18

        /// <summary>Gets the fixed-width ASCII typeface field.</summary>
        /// <value>A 16-byte buffer; <see cref="Parse"/> extracts and decodes its bytes.</value>
        /// <seealso cref="Ascii16"/>
        /// <seealso cref="PcltTable.Typeface"/>
        public Ascii16 Typeface;          // +20  (16 bytes ASCII)

        /// <summary>Gets the packed character-complement bitfield.</summary>
        /// <value>The raw 64-bit value; <see cref="Parse"/> casts it to <see cref="CharacterComplement"/>.</value>
        /// <seealso cref="CharacterComplement"/>
        public ulong CharacterComplement; // +36  (8-byte bitfield, big-endian)

        /// <summary>Gets the fixed-width ASCII file-name field.</summary>
        /// <value>A 6-byte buffer; <see cref="Parse"/> extracts and decodes its bytes.</value>
        /// <seealso cref="Ascii6"/>
        /// <seealso cref="PcltTable.FileName"/>
        public Ascii6 FileName;           // +44  (6 bytes ASCII)

        /// <summary>Gets the PCL stroke weight.</summary>
        /// <value>A signed value in <c>[-7, 7]</c>.</value>
        /// <seealso cref="WidthType"/>
        public sbyte StrokeWeight;        // +50

        /// <summary>Gets the PCL appearance width.</summary>
        /// <value>A signed value in <c>[-5, 5]</c>.</value>
        /// <seealso cref="StrokeWeight"/>
        public sbyte WidthType;           // +51

        /// <summary>Gets the raw serif-style byte.</summary>
        /// <value>The packed byte; see <see cref="PcltTable.SerifStyleClass"/> and <see cref="PcltTable.SerifStyleShape"/>.</value>
        /// <seealso cref="Reserved"/>
        public byte SerifStyle;           // +52

        /// <summary>Gets the reserved byte at offset +53. Always zero.</summary>
        /// <value>Always <c>0</c> in conforming fonts; present to pad the record to its declared length.</value>
        /// <seealso cref="SerifStyle"/>
        public byte Reserved;             // +53

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>The two inline-array ASCII buffers and the three trailing byte fields are copied through unchanged; only the numeric and packed word fields are reversed.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => v with
        {
            MajorVersion = BinaryPrimitives.ReverseEndianness(v.MajorVersion),
            MinorVersion = BinaryPrimitives.ReverseEndianness(v.MinorVersion),
            FontNumber = BinaryPrimitives.ReverseEndianness(v.FontNumber),
            Pitch = BinaryPrimitives.ReverseEndianness(v.Pitch),
            XHeight = BinaryPrimitives.ReverseEndianness(v.XHeight),
            Style = BinaryPrimitives.ReverseEndianness(v.Style),
            TypeFamily = BinaryPrimitives.ReverseEndianness(v.TypeFamily),
            CapHeight = BinaryPrimitives.ReverseEndianness(v.CapHeight),
            SymbolSet = BinaryPrimitives.ReverseEndianness(v.SymbolSet),
            CharacterComplement = BinaryPrimitives.ReverseEndianness(v.CharacterComplement)
        };
    }
}
