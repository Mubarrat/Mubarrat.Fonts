using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// acnt — Accent Attachment Table (Apple Advanced Typography)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>acnt</c> table: a space-efficient description of how the accents of a precomposed accented glyph attach to its base glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is an Apple Advanced Typography (AAT) table and is not part of OpenType. Apple documents it in the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual, <c>acnt</c> table</see>, and states there that the table is not supported on OS X or iOS and that its use is discouraged. It is parsed for completeness and for tooling that inspects legacy Apple fonts.</description></item>
/// <item><description>The <c>acnt</c> table lets a font omit the outlines of accented glyphs entirely: only attachment point indices are stored, and the renderer composes an accented glyph from its primary (base) component and one or more secondary (accent) components.</description></item>
/// <item><description>The table begins with a 20-byte <see cref="Header"/> that locates three regions — the description data, the extension data, and the secondary data — by byte offset from the start of the table. The three regions are independent; each is read at the offset the header declares.</description></item>
/// <item><description>One <see cref="GlyphDescription"/> describes each accented glyph, in order of increasing glyph index, starting at <see cref="FirstAccentGlyphIndex"/>. A glyph with exactly one accent uses format 0, which names the accent's <see cref="SecondaryGlyph"/> directly. A glyph with two or more accents uses format 1, which names an offset into the extension data; the extension data then lists one <see cref="ExtensionComponent"/> per accent.</description></item>
/// <item><description>The secondary data is an array of <see cref="SecondaryGlyph"/> records, indexed by accent rather than by glyph. One record can therefore serve the same accent in many accented glyphs, which is where the format's space savings come from.</description></item>
/// <item><description>Point indices in the descriptions are point numbers within the primary and secondary glyphs as they appear in <c>glyf</c>. Matching the two points is the only positioning mechanism the table provides.</description></item>
/// </list>
/// <para>
/// The extension data is bit-packed rather than byte-aligned. This implementation reads it as a most-significant-bit-first bit stream. See <see cref="ExtensionComponent"/> for the field order and for the ambiguity that this reading resolves.
/// </para>
/// </remarks>
/// <seealso cref="GlyphDescription"/>
/// <seealso cref="ExtensionComponent"/>
/// <seealso cref="SecondaryGlyph"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6glyf.html">TrueType Reference Manual: The <c>glyf</c> table</seealso>
public sealed record AcntTable : IFontTable<AcntTable>
{
    /// <summary>Gets the AAT table tag <c>acnt</c>.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    public static Tag Tag => "acnt";

    /// <summary>Gets the table version.</summary>
    /// <remarks>The current version is <c>0x00010000</c>.</remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the major half of <see cref="Version"/>.</summary>
    /// <seealso cref="MinorVersion"/>
    public ushort MajorVersion => (ushort)Version.IntegerPart;

    /// <summary>Gets the minor half of <see cref="Version"/>.</summary>
    /// <seealso cref="MajorVersion"/>
    public ushort MinorVersion => Version.FractionPart;

    /// <summary>Gets the index of the first accented glyph.</summary>
    /// <remarks>The accented glyphs of a font must occupy a contiguous range of glyph indices that begins above the highest index used by a non-accented glyph.</remarks>
    /// <seealso cref="LastAccentGlyphIndex"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    public ushort FirstAccentGlyphIndex { get; init; }

    /// <summary>Gets the index of the last accented glyph.</summary>
    /// <remarks>A font may declare an empty range by setting this field below <see cref="FirstAccentGlyphIndex"/>; <see cref="GlyphCount"/> is then zero.</remarks>
    /// <seealso cref="FirstAccentGlyphIndex"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    public ushort LastAccentGlyphIndex { get; init; }

    /// <summary>Gets the byte offset of the description data from the beginning of the table.</summary>
    /// <remarks>The description data holds <see cref="GlyphCount"/> consecutive four-byte records.</remarks>
    /// <seealso cref="Glyphs"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    public uint DescriptionOffset { get; init; }

    /// <summary>Gets the byte offset of the extension data from the beginning of the table.</summary>
    /// <remarks>Format 1 description records declare offsets relative to the start of this region, not to the start of the table.</remarks>
    /// <seealso cref="ExtensionData"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    public uint ExtensionOffset { get; init; }

    /// <summary>Gets the byte offset of the secondary data from the beginning of the table.</summary>
    /// <seealso cref="SecondaryGlyphs"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    public uint SecondaryOffset { get; init; }

    /// <summary>Gets the number of accents described by the extension data.</summary>
    /// <remarks>Counts every <see cref="ExtensionComponent"/> across every format 1 description; format 0 descriptions are not included.</remarks>
    /// <seealso cref="ExtensionData"/>
    public int ExtensionCount { get; init; }

    /// <summary>Gets the description of each accented glyph, ordered by increasing glyph index and starting at <see cref="FirstAccentGlyphIndex"/>.</summary>
    /// <seealso cref="GlyphCount"/>
    /// <seealso cref="FirstAccentGlyphIndex"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    public IReadOnlyList<GlyphDescription> Glyphs { get; init; } = [];

    /// <summary>Gets the accent lists referenced by format 1 descriptions, in the order the descriptions declare them.</summary>
    /// <remarks>The list is indexed by <see cref="GlyphDescription.ExtensionIndex"/>. A format 0 description has no entry here; its single accent is named by the description itself.</remarks>
    /// <seealso cref="GlyphDescription.ExtensionIndex"/>
    /// <seealso cref="ExtensionComponent"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    public IReadOnlyList<IReadOnlyList<ExtensionComponent>> ExtensionData { get; init; } = [];

    /// <summary>Gets the secondary data entries shared by the descriptions.</summary>
    /// <remarks>Each entry names an accent glyph and the attachment point within it. Entries are addressed by <see cref="GlyphDescription.SecondaryInfoIndex"/> and <see cref="ExtensionComponent.SecondaryInfoIndex"/>. The table permits at most 255 entries because those indices are 7 bits wide.</remarks>
    /// <seealso cref="SecondaryGlyph"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    public IReadOnlyList<SecondaryGlyph> SecondaryGlyphs { get; init; } = [];

    /// <summary>Gets the number of accented glyphs described by the table.</summary>
    /// <remarks>Equal to <c>lastAccentGlyphIndex - firstAccentGlyphIndex + 1</c>, or zero when the declared range is empty.</remarks>
    /// <seealso cref="Glyphs"/>
    public int GlyphCount => Glyphs.Count;

    /// <summary>Gets the description of the accented glyph with the specified glyph index.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <returns>The matching description, or <see langword="null"/> when <paramref name="glyphIndex"/> is outside the declared accent range.</returns>
    /// <remarks>The accented glyph indices form a contiguous, ascending range, so the lookup is a direct index into <see cref="Glyphs"/>.</remarks>
    /// <seealso cref="Glyphs"/>
    public GlyphDescription? GetDescription(int glyphIndex)
    {
        if (glyphIndex < FirstAccentGlyphIndex) return null;

        int index = glyphIndex - FirstAccentGlyphIndex;
        return index < Glyphs.Count ? Glyphs[index] : null;
    }

    /// <summary>Gets the accents attached to the accented glyph with the specified glyph index.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <returns>The accent list of the glyph, or an empty list when the glyph has no description.</returns>
    /// <remarks>A format 0 description names a single accent, so its list is built on demand and has exactly one element.</remarks>
    /// <seealso cref="ExtensionData"/>
    /// <seealso cref="GlyphDescription"/>
    public IReadOnlyList<ExtensionComponent> GetAccents(int glyphIndex)
    {
        if (GetDescription(glyphIndex) is not { } description) return [];

        if (description.ExtensionIndex >= 0 && description.ExtensionIndex < ExtensionData.Count)
            return ExtensionData[description.ExtensionIndex];

        return [new ExtensionComponent
        {
            SecondaryInfoIndex = description.SecondaryInfoIndex,
            PrimaryAttachmentPoint = description.PrimaryAttachmentPoint,
        }];
    }

    /// <summary>The 20-byte fixed-layout <c>acnt</c> table header.</summary>
    /// <remarks>Fields are stored in big-endian order at the offsets defined by the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html"><c>acnt</c> table specification</see>. The header is followed by the description, extension, and secondary regions, at the offsets it declares.</remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0.</summary>
        /// <remarks><c>0x00010000</c> for the current version.</remarks>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
        public Fixed Version;                // +0

        /// <summary>The first accented glyph index at byte offset 4.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
        public ushort FirstAccentGlyphIndex; // +4

        /// <summary>The last accented glyph index at byte offset 6.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
        public ushort LastAccentGlyphIndex;  // +6

        /// <summary>The byte offset of the description data at byte offset 8.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
        public uint DescriptionOffset;       // +8

        /// <summary>The byte offset of the extension data at byte offset 12.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
        public uint ExtensionOffset;         // +12

        /// <summary>The byte offset of the secondary data at byte offset 16.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
        public uint SecondaryOffset;         // +16

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = new Fixed(BinaryPrimitives.ReverseEndianness(v.Version.Bits)),
            FirstAccentGlyphIndex = BinaryPrimitives.ReverseEndianness(v.FirstAccentGlyphIndex),
            LastAccentGlyphIndex = BinaryPrimitives.ReverseEndianness(v.LastAccentGlyphIndex),
            DescriptionOffset = BinaryPrimitives.ReverseEndianness(v.DescriptionOffset),
            ExtensionOffset = BinaryPrimitives.ReverseEndianness(v.ExtensionOffset),
            SecondaryOffset = BinaryPrimitives.ReverseEndianness(v.SecondaryOffset),
        };
    }

    /// <summary>The four-byte fixed-layout description record that introduces each accented glyph.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Bytes 0 and 1 are a packed field whose most significant bit, bit 7 of byte 0, is the format flag and whose remaining 15 bits are the primary glyph index. The specification names the two parts <c>uint1 description</c> and <c>uint15 primaryGlyphIndex</c>, in that order.</description></item>
    /// <item><description>For a format 0 description the trailing pair is the primary attachment point followed by the <see cref="SecondaryInfoIndex"/>. For a format 1 description the trailing pair is a big-endian <c>uint16</c> <see cref="ExtensionOffset"/>. The two record layouts are the same length, so the description array has a uniform four-byte stride regardless of format.</description></item>
    /// <item><description>The fields are stored in <em>native</em> order after parsing rather than in their packed on-disk form, so <see cref="Format"/>, <see cref="PrimaryGlyphIndex"/>, and <see cref="ExtensionOffset"/> are ready to use. <see cref="Raw"/> preserves the original two bytes for round-tripping and diagnostics.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AcntTable.Glyphs"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct GlyphDescription : IEndianReversibleStruct<GlyphDescription>
    {
        /// <summary>The description format at byte offset 0, after normalization.</summary>
        /// <remarks>0 for a single-accent description; 1 for a multi-accent description. Parsing converts the on-disk packed field to this form.</remarks>
        /// <seealso cref="DescriptionFormatSingle"/>
        /// <seealso cref="DescriptionFormatExtended"/>
        public ushort Format;                // +0..+1 normalizes the on-disk packed field: bit 15 is the format flag, bits 14..0 the primary glyph index

        /// <summary>The primary attachment control point number at byte offset 2. Format 0 only.</summary>
        /// <remarks>Meaningless, and normally zero, when <see cref="Format"/> is 1.</remarks>
        public byte PrimaryAttachmentPoint;  // +2

        /// <summary>Either the secondary information index (format 0) or the low byte of the extension offset (format 1) at byte offset 3.</summary>
        /// <remarks>Read <see cref="SecondaryInfoIndex"/> and <see cref="ExtensionOffset"/> instead; this field is the raw trailing byte.</remarks>
        /// <seealso cref="SecondaryInfoIndex"/>
        /// <seealso cref="ExtensionOffset"/>
        public byte Trailing;                // +3

        /// <summary>Creates a description from its normalized fields. Used by the parser, which has already decoded the packed on-disk bytes.</summary>
        /// <param name="format">The description format.</param>
        /// <param name="primaryGlyphIndex">The primary glyph index.</param>
        /// <param name="primaryAttachmentPoint">The primary attachment control point number.</param>
        /// <param name="secondaryInfoIndex">The secondary information index.</param>
        /// <param name="extensionOffset">The glyph's offset into the extension data region.</param>
        internal GlyphDescription(ushort format, ushort primaryGlyphIndex, byte primaryAttachmentPoint, byte secondaryInfoIndex, ushort extensionOffset)
        {
            Format = format;
            PrimaryGlyphIndex = primaryGlyphIndex;
            PrimaryAttachmentPoint = primaryAttachmentPoint;
            SecondaryInfoIndex = secondaryInfoIndex;
            ExtensionOffset = extensionOffset;
            Trailing = (byte)(extensionOffset & 0xFF);
        }

        /// <summary>Gets the primary glyph index from the 15 bits below the format flag.</summary>
        /// <seealso cref="AcntTable"/>
        public ushort PrimaryGlyphIndex { get; init; }

        /// <summary>Gets the index into <see cref="AcntTable.SecondaryGlyphs"/> naming the accent glyph and its attachment point. Format 0 only.</summary>
        /// <remarks>Meaningless when <see cref="Format"/> is 1.</remarks>
        /// <seealso cref="AcntTable.SecondaryGlyphs"/>
        public byte SecondaryInfoIndex { get; init; }

        /// <summary>Gets the glyph's offset into the extension data region. Format 1 only.</summary>
        /// <remarks>The offset is relative to the beginning of the extension data, not to the beginning of the table. Meaningless when <see cref="Format"/> is 0.</remarks>
        /// <seealso cref="ExtensionIndex"/>
        public ushort ExtensionOffset { get; init; }

        /// <summary>Gets the index of this glyph's accent list in <see cref="AcntTable.ExtensionData"/>, or <c>-1</c> for a format 0 description.</summary>
        /// <remarks>Assigned during parsing in the order the format 1 descriptions appear.</remarks>
        /// <seealso cref="AcntTable.ExtensionData"/>
        public int ExtensionIndex { get; init; } = -1;

        /// <summary>Gets the packed on-disk representation of the format flag and primary glyph index.</summary>
        /// <remarks>Recombines the two fields into the big-endian value a font stores at byte offset 0: <c>(format &lt;&lt; 15) | primaryGlyphIndex</c>.</remarks>
        /// <seealso cref="Format"/>
        /// <seealso cref="PrimaryGlyphIndex"/>
        public readonly ushort Raw => (ushort)((Format << 15) | (PrimaryGlyphIndex & 0x7FFF));

        /// <summary>Reverses the byte order of the packed fields in a <see cref="GlyphDescription"/> and unpacks them.</summary>
        /// <param name="v">The value as read from the source, with its multi-byte fields still in on-disk byte order.</param>
        /// <returns>The same record with <see cref="Format"/>, <see cref="PrimaryGlyphIndex"/>, and <see cref="ExtensionOffset"/> decoded.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>The on-disk format flag is bit 7 of byte 0. After reversal it lands in bit 15 of a native <see cref="ushort"/>, so it is masked there rather than at bit 7.</description></item>
        /// <item><description>The trailing two bytes are also the primary attachment point and secondary index of a format 0 record, so they are preserved verbatim as well as being read as the format 1 extension offset.</description></item>
        /// </list>
        /// </remarks>
        public static GlyphDescription ReverseEndianness(GlyphDescription v)
        {
            ushort packed = BinaryPrimitives.ReverseEndianness(v.Format);
            ushort trailing = (ushort)((v.Trailing << 8) | v.PrimaryAttachmentPoint);

            // The value read into Format is the raw packed field only once reversal has been applied; until then it holds the unswapped bytes.
            return new GlyphDescription
            {
                Format = (ushort)(packed >> 15),
                PrimaryGlyphIndex = (ushort)(packed & 0x7FFF),
                PrimaryAttachmentPoint = v.PrimaryAttachmentPoint,
                Trailing = v.Trailing,
                SecondaryInfoIndex = v.Trailing,
                ExtensionOffset = trailing,
                ExtensionIndex = -1,
            };
        }
    }

    /// <summary>One accent of an accented glyph that has two or more accents.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The extension data is documented as a one-bit <c>components</c> flag plus a <c>uint7</c> secondary index and a <c>uint8</c> primary attachment point per component. The region is therefore a big-endian bit stream, read most significant bit first.</description></item>
    /// <item><description>The flag is <c>0</c> while further components follow and <c>1</c> on the last one, so the flag sits before the first index and the list ends at the first set flag.</description></item>
    /// <item><description>The specification's storage accounting of "4 bytes + 2 bytes per accent" is consistent with this layout: the four-byte description record plus 15 bits per accent, rounded up to whole bytes, gives <c>4 + 2 * numberOfAccents</c> bytes for any accent count.</description></item>
    /// <item><description>Whether the accents' bit fields are packed per component or grouped by field — all indices first, then all attachment points — cannot be settled from the published specification, because both readings fit its storage accounting. This implementation reads them per component: flag, index, attachment point, then the next component.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AcntTable.ExtensionData"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    public sealed record ExtensionComponent
    {
        /// <summary>Gets the index into <see cref="AcntTable.SecondaryGlyphs"/> naming the accent glyph and its attachment point.</summary>
        /// <seealso cref="AcntTable.SecondaryGlyphs"/>
        public ushort SecondaryInfoIndex { get; init; }

        /// <summary>Gets the attachment point number within the primary glyph that this accent attaches to.</summary>
        public ushort PrimaryAttachmentPoint { get; init; }
    }

    /// <summary>A three-byte fixed-layout secondary data record naming an accent glyph and the attachment point within it.</summary>
    /// <remarks>Records are addressed by <see cref="GlyphDescription.SecondaryInfoIndex"/> and <see cref="ExtensionComponent.SecondaryInfoIndex"/>. Because those indices are 7 bits wide, a font can define at most 255 of these records.</remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct SecondaryGlyph : IEndianReversibleStruct<SecondaryGlyph>
    {
        /// <summary>The accent glyph's index in the <c>glyf</c> table at byte offset 0.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6glyf.html">TrueType Reference Manual: The <c>glyf</c> table</seealso>
        public ushort SecondaryGlyphIndex;           // +0

        /// <summary>The attachment point number within the accent glyph at byte offset 2.</summary>
        public byte SecondaryGlyphAttachmentNumber;  // +2

        /// <summary>Reverses the byte order of the glyph index in a <see cref="SecondaryGlyph"/>.</summary>
        public static SecondaryGlyph ReverseEndianness(SecondaryGlyph v) => new()
        {
            SecondaryGlyphIndex = BinaryPrimitives.ReverseEndianness(v.SecondaryGlyphIndex),
            SecondaryGlyphAttachmentNumber = v.SecondaryGlyphAttachmentNumber,
        };
    }

    /// <summary>Size, in bytes, of the fixed-layout <see cref="Header"/>.</summary>
    /// <seealso cref="Header"/>
    public const int HeaderSize = 20;

    /// <summary>Size, in bytes, of a <see cref="GlyphDescription"/> record, in either format.</summary>
    /// <seealso cref="GlyphDescription"/>
    public const int DescriptionSize = 4;

    /// <summary>Size, in bytes, of a <see cref="SecondaryGlyph"/> record.</summary>
    /// <seealso cref="SecondaryGlyph"/>
    public const int SecondaryGlyphSize = 3;

    /// <summary>Format value for a description with exactly one secondary component.</summary>
    /// <seealso cref="GlyphDescription.Format"/>
    public const ushort DescriptionFormatSingle = 0;

    /// <summary>Format value for a description with two or more secondary components.</summary>
    /// <seealso cref="GlyphDescription.Format"/>
    public const ushort DescriptionFormatExtended = 1;

    /// <summary>Maximum number of secondary components a font may define, imposed by the 7-bit secondary indices.</summary>
    /// <seealso cref="SecondaryGlyphs"/>
    public const int MaxSecondaryGlyphCount = 255;

    /// <summary>Maximum number of primary components a font may define, imposed by the 15-bit primary glyph index.</summary>
    /// <seealso cref="GlyphDescription.PrimaryGlyphIndex"/>
    public const int MaxPrimaryGlyphIndex = 0x7FFF;

    /// <inheritdoc/>
    static AcntTable IRecord<AcntTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        int glyphCount = header.LastAccentGlyphIndex >= header.FirstAccentGlyphIndex
            ? header.LastAccentGlyphIndex - header.FirstAccentGlyphIndex + 1
            : 0;

        GlyphDescription[] glyphs = ReadDescriptions(ref cursor, header.DescriptionOffset, glyphCount);

        var extensionData = new List<IReadOnlyList<ExtensionComponent>>();
        for (int i = 0; i < glyphs.Length; i++)
        {
            if (glyphs[i].Format != DescriptionFormatExtended) continue;

            IReadOnlyList<ExtensionComponent> accents =
                ReadExtension(ref cursor, header.ExtensionOffset, glyphs[i].ExtensionOffset);

            glyphs[i] = glyphs[i] with { ExtensionIndex = extensionData.Count };
            extensionData.Add(accents);
        }

        return new AcntTable
        {
            Version = header.Version,
            FirstAccentGlyphIndex = header.FirstAccentGlyphIndex,
            LastAccentGlyphIndex = header.LastAccentGlyphIndex,
            DescriptionOffset = header.DescriptionOffset,
            ExtensionOffset = header.ExtensionOffset,
            SecondaryOffset = header.SecondaryOffset,
            ExtensionCount = extensionData.Sum(static accents => accents.Count),
            Glyphs = glyphs,
            ExtensionData = extensionData,
            SecondaryGlyphs = ReadSecondaryGlyphs(ref cursor, header.SecondaryOffset),
        };
    }

    /// <summary>Reads the description data region: one <see cref="GlyphDescription"/> per accented glyph.</summary>
    /// <param name="cursor">The cursor over the table, positioned anywhere within it.</param>
    /// <param name="descriptionOffset">The byte offset of the description data from the beginning of the table.</param>
    /// <param name="count">The number of accented glyphs, derived from the header's accent range.</param>
    /// <returns>The descriptions, in increasing glyph-index order.</returns>
    /// <exception cref="InvalidDataException">The descriptions do not all use the same format, which the offset-free layout cannot express.</exception>
    /// <remarks>
    /// The specification gives the description array neither a length nor a per-entry offset table, so the entries must be recoverable by reading forward from <paramref name="descriptionOffset"/>. This implementation treats the region as a uniform array of four-byte records and rejects an array that mixes formats, because a mixed array is indistinguishable from a longer or shorter one.
    /// </remarks>
    private static GlyphDescription[] ReadDescriptions(ref Cursor cursor, uint descriptionOffset, int count)
    {
        if (count <= 0) return [];

        cursor.Seek(descriptionOffset);
        var descriptions = cursor.ReadBigEndianStructArray<GlyphDescription>(count);

        ushort format = descriptions[0].Format;
        for (int i = 1; i < descriptions.Length; i++)
        {
            if (descriptions[i].Format == format) continue;

            throw new InvalidDataException(
                $"'acnt' description {i} uses format {descriptions[i].Format} while description 0 uses format {format}. " +
                "The description array has no per-entry offsets, so a mixed-format array cannot be decoded.");
        }

        return descriptions;
    }

    /// <summary>Reads one format 1 description's extension data as a most-significant-bit-first bit stream.</summary>
    /// <param name="cursor">The cursor over the table, positioned anywhere within it.</param>
    /// <param name="extensionOffset">The byte offset of the extension data region from the beginning of the table.</param>
    /// <param name="glyphExtensionOffset">The glyph's offset into that region, from its format 1 description record.</param>
    /// <returns>One <see cref="ExtensionComponent"/> per accent, in the order the font stored them.</returns>
    /// <exception cref="EndOfStreamException">The bit stream extends past the end of the table before its terminating flag is set.</exception>
    /// <remarks>See <see cref="ExtensionComponent"/> for the field order and for the ambiguity this decoding resolves.</remarks>
    private static ExtensionComponent[] ReadExtension(ref Cursor cursor, uint extensionOffset, ushort glyphExtensionOffset)
    {
        cursor.Seek((long)extensionOffset + glyphExtensionOffset);

        var components = new List<ExtensionComponent>();
        int bitOffset = 0;

        while (true)
        {
            bool last = ReadBits(ref cursor, ref bitOffset, 1) != 0;
            int secondaryInfoIndex = ReadBits(ref cursor, ref bitOffset, 7);
            int primaryAttachmentPoint = ReadBits(ref cursor, ref bitOffset, 8);

            components.Add(new ExtensionComponent
            {
                SecondaryInfoIndex = (ushort)secondaryInfoIndex,
                PrimaryAttachmentPoint = (ushort)primaryAttachmentPoint,
            });

            if (last) return [.. components];
        }
    }

    /// <summary>Reads the secondary data region: a fixed-stride array of <see cref="SecondaryGlyph"/> records.</summary>
    /// <param name="cursor">The cursor over the table, positioned anywhere within it.</param>
    /// <param name="secondaryOffset">The byte offset of the secondary data from the beginning of the table.</param>
    /// <returns>The secondary records, in table order.</returns>
    /// <remarks>The region has no declared count. Its length is taken as the remainder of the table divided by the three-byte record size, capped at <see cref="MaxSecondaryGlyphCount"/> because a secondary index cannot address past that.</remarks>
    private static SecondaryGlyph[] ReadSecondaryGlyphs(ref Cursor cursor, uint secondaryOffset)
    {
        cursor.Seek(secondaryOffset);

        long available = cursor.Remaining / SecondaryGlyphSize;
        int count = (int)Math.Min(available, MaxSecondaryGlyphCount);

        return count <= 0 ? [] : cursor.ReadBigEndianStructArray<SecondaryGlyph>(count);
    }

    /// <summary>Reads <paramref name="count"/> bits of the most-significant-bit-first bit stream at <paramref name="bitOffset"/>.</summary>
    /// <param name="cursor">The cursor over the table. It is advanced past every byte the field fully consumed, and left on the byte that still holds unread bits.</param>
    /// <param name="bitOffset">The zero-based bit offset into the stream, measured from the first byte of the field sequence. Advanced past the field.</param>
    /// <param name="count">The number of bits to read. Must be between 1 and 15.</param>
    /// <returns>The field value, with the first bit read as its most significant bit.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 1 or greater than 15.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The stream is read big-endian at the byte level: within a byte the most significant bit is read first, so bit 7 comes before bit 0.</description></item>
    /// <item><description>Bits are sampled with <see cref="Cursor.PeekUInt8"/>, which does not move the cursor, and the cursor is repositioned once at the end. That keeps a field that is only a few bits wide from advancing the cursor past bytes a later field still needs.</description></item>
    /// </list>
    /// </remarks>
    private static int ReadBits(ref Cursor cursor, ref int bitOffset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 15);

        int startByte = bitOffset >> 3;
        long start = cursor.Position;
        int value = 0;

        for (int i = 0; i < count; i++)
        {
            int byteIndex = bitOffset >> 3;
            int bitInByte = 7 - (bitOffset & 7);

            byte b = cursor.Source.ReadUInt8At(start + (byteIndex - startByte));
            value = (value << 1) | (b >> bitInByte) & 1;

            bitOffset++;
        }

        // Advance past the bytes the field fully consumed; the byte that still holds unread bits is left in place.
        long consumed = (bitOffset >> 3) - startByte;
        if (consumed > 0) cursor.Position = start + consumed;

        return value;
    }
}
