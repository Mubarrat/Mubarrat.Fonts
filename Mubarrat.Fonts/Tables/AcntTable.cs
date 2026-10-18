using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// The 'acnt' table — AAT accent attachment
//
// Self-contained; no glyph count and no external context required.
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>acnt</c> table: accent attachment data that describes how secondary accent glyphs attach to a primary base glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Accented glyphs are a restricted subclass of compound glyphs: one primary component plus one or more secondary components, with all outline-independent properties inherited from the primary.</description></item>
/// <item><description>Accented glyph indices must be contiguous and start above every regular glyph index, so the table addresses them by the inclusive range <see cref="FirstAccentGlyphIndex"/>..<see cref="LastAccentGlyphIndex"/>.</description></item>
/// <item><description>Two description formats exist. Format 0 describes a base plus exactly one accent in 4 bytes. Format 1 describes a base plus two or more accents by pointing into a separate extension chain of 2-byte subentries.</description></item>
/// <item><description>Secondary data is a flat array of 3-byte entries, one per distinct accent glyph; both description formats refer to an entry by its index rather than by offset.</description></item>
/// <item><description>The table is not supported on macOS or iOS, and Apple discourages its use. The parser still reads it fully so a font can be inspected on any platform.</description></item>
/// <item><description>All multi-byte reads use the sfnt default of big-endian.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html"><c>acnt</c> table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AcntAccentedGlyph"/>
/// <seealso cref="AcntSecondaryGlyph"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
public sealed record AcntTable : IFontTable<AcntTable>, IRecord<AcntTable>
{
    /// <summary>The only version defined by the current specification, as a 16.16 <see cref="Fixed"/> value.</summary>
    /// <value>The <see cref="Fixed"/> value 1.0, whose raw 32-bit representation is <c>0x00010000</c>.</value>
    /// <seealso cref="Version"/>
    public static readonly Fixed CurrentVersion = (Fixed)0x00010000;

    /// <summary>The maximum number of secondary glyph entries the specification permits.</summary>
    /// <value>The constant <c>255</c>. Valid secondary-info indices are therefore <c>0</c> through <c>254</c>.</value>
    /// <seealso cref="SecondaryGlyphs"/>
    public const int MaxSecondaryGlyphCount = 255;

    /// <inheritdoc/>
    public static Tag Tag => "acnt";

    /// <summary>Gets the table version.</summary>
    /// <value>The 16.16 <see cref="Fixed"/> version read from the header. Always <see cref="CurrentVersion"/>; the parser rejects any other value.</value>
    /// <seealso cref="CurrentVersion"/>
    public Fixed Version { get; init; }

    /// <summary>Gets the first accented glyph index.</summary>
    /// <value>The glyph ID of the first accented glyph. Accented glyph IDs are contiguous and start above every regular glyph ID.</value>
    /// <seealso cref="LastAccentGlyphIndex"/>
    /// <seealso cref="AccentedGlyphs"/>
    public ushort FirstAccentGlyphIndex { get; init; }

    /// <summary>Gets the last accented glyph index.</summary>
    /// <value>The glyph ID of the last accented glyph, inclusive. The number of accented glyphs is <c>Last - First + 1</c>.</value>
    /// <seealso cref="FirstAccentGlyphIndex"/>
    /// <seealso cref="AccentedGlyphs"/>
    public ushort LastAccentGlyphIndex { get; init; }

    /// <summary>Gets the accented glyph descriptions, in glyph-index order.</summary>
    /// <value>Entry <c>i</c> describes the glyph at index <c>FirstAccentGlyphIndex + i</c>. Each entry is an <see cref="AcntAccentedGlyph"/>, whose runtime type discriminates the two on-disk formats.</value>
    /// <seealso cref="GetAccentedGlyph(int)"/>
    /// <seealso cref="TryGetAccentedGlyph(int, out AcntAccentedGlyph?)"/>
    /// <seealso cref="AcntAccentedGlyph"/>
    public required AcntAccentedGlyph[] AccentedGlyphs { get; init; }

    /// <summary>Gets the secondary glyph entries that the accented glyph descriptions reference.</summary>
    /// <value>Entry <c>i</c> is the accent glyph and attachment point selected by secondary-info index <c>i</c>. The array length is one greater than the highest index any description references.</value>
    /// <seealso cref="GetSecondaryGlyph(int)"/>
    /// <seealso cref="AcntSecondaryGlyph"/>
    public required AcntSecondaryGlyph[] SecondaryGlyphs { get; init; }

    /// <summary>Gets the number of accented glyphs described by the table.</summary>
    /// <value>The length of <see cref="AccentedGlyphs"/>; equal to <c>LastAccentGlyphIndex - FirstAccentGlyphIndex + 1</c>.</value>
    /// <seealso cref="AccentedGlyphs"/>
    public int AccentedGlyphCount => AccentedGlyphs.Length;

    /// <summary>Gets the number of secondary glyph entries in the table.</summary>
    /// <value>The length of <see cref="SecondaryGlyphs"/>; never greater than <see cref="MaxSecondaryGlyphCount"/>.</value>
    /// <seealso cref="SecondaryGlyphs"/>
    public int SecondaryGlyphCount => SecondaryGlyphs.Length;

    /// <summary>Gets the accented-glyph description for a glyph ID.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The description when <paramref name="glyphId"/> lies in the accented range; otherwise <see langword="null"/>.</returns>
    /// <seealso cref="TryGetAccentedGlyph(int, out AcntAccentedGlyph?)"/>
    /// <seealso cref="AccentedGlyphs"/>
    public AcntAccentedGlyph? GetAccentedGlyph(int glyphId) =>
        TryGetAccentedGlyph(glyphId, out var glyph) ? glyph : null;

    /// <summary>Attempts to get the accented-glyph description for a glyph ID.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <param name="glyph">When this method returns <see langword="true"/>, contains the description; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="glyphId"/> lies in the accented range; otherwise, <see langword="false"/>.</returns>
    /// <remarks>The method distinguishes an absent glyph from a present description whose base glyph happens to be glyph <c>0</c>.</remarks>
    /// <seealso cref="GetAccentedGlyph(int)"/>
    /// <seealso cref="AccentedGlyphs"/>
    public bool TryGetAccentedGlyph(int glyphId, out AcntAccentedGlyph? glyph)
    {
        int index = glyphId - FirstAccentGlyphIndex;
        if ((uint)index < (uint)AccentedGlyphs.Length)
        {
            glyph = AccentedGlyphs[index];
            return true;
        }

        glyph = null;
        return false;
    }

    /// <summary>Gets the secondary glyph entry for a secondary-info index.</summary>
    /// <param name="secondaryInfoIndex">The zero-based index into the secondary data array.</param>
    /// <returns>The secondary glyph entry, or <see langword="null"/> when the index is outside the array.</returns>
    /// <seealso cref="SecondaryGlyphs"/>
    public AcntSecondaryGlyph? GetSecondaryGlyph(int secondaryInfoIndex) =>
        (uint)secondaryInfoIndex < (uint)SecondaryGlyphs.Length ? SecondaryGlyphs[secondaryInfoIndex] : null;

    static AcntTable IRecord<AcntTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
            throw new InvalidDataException($"'acnt'.version is 0x{(int)header.Version:X8}, expected 0x{(int)CurrentVersion:X8}.");

        if (header.LastAccentGlyphIndex < header.FirstAccentGlyphIndex)
            throw new InvalidDataException($"'acnt'.lastAccentGlyphIndex {header.LastAccentGlyphIndex} is less than firstAccentGlyphIndex {header.FirstAccentGlyphIndex}.");

        int accentedGlyphCount = header.LastAccentGlyphIndex - header.FirstAccentGlyphIndex + 1;

        AcntAccentedGlyph[] accentedGlyphs = cursor.Source.ReadEndianReversibleHeaderRecordArrayAt<AcntAccentedGlyph, AcntDescriptionEntry>(header.DescriptionOffset, accentedGlyphCount, new AcntAccentedGlyph.Context(cursor.Source, header.ExtensionOffset));

        int maxSecondaryIndex = -1;
        foreach (AcntAccentedGlyph glyph in accentedGlyphs)
        {
            int glyphMax = glyph.GetMaxSecondaryInfoIndex();
            if (glyphMax > maxSecondaryIndex) maxSecondaryIndex = glyphMax;
        }

        if (maxSecondaryIndex >= MaxSecondaryGlyphCount)
            throw new InvalidDataException($"'acnt' references secondary-info index {maxSecondaryIndex}, but the specification permits at most {MaxSecondaryGlyphCount} entries.");

        int secondaryCount = maxSecondaryIndex + 1;
        AcntSecondaryGlyph[] secondaryGlyphs = secondaryCount > 0
            ? cursor.Source.ReadEndianReversibleStructArrayAt<AcntSecondaryGlyph>(header.SecondaryOffset, secondaryCount)
            : [];

        return new()
        {
            Version = header.Version,
            FirstAccentGlyphIndex = header.FirstAccentGlyphIndex,
            LastAccentGlyphIndex = header.LastAccentGlyphIndex,
            AccentedGlyphs = accentedGlyphs,
            SecondaryGlyphs = secondaryGlyphs,
        };
    }

    /// <summary>The 20-byte <c>acnt</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The version is a 16.16 <see cref="Fixed"/> value; the two glyph indices are <c>UInt16</c>; the three section offsets are <c>UInt32</c>.</description></item>
    /// <item><description>The three offsets are measured from the start of the table and point at the description, extension, and secondary regions in that order.</description></item>
    /// <item><description>Consumed at parse time and not retained on <see cref="AcntTable"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AcntTable"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version, a 16.16 <see cref="Fixed"/> value, at byte offset 0.</summary>
        /// <value>The constant <see cref="CurrentVersion"/>.</value>
        /// <seealso cref="CurrentVersion"/>
        /// <seealso cref="AcntTable.Version"/>
        public Fixed Version;                 // +0

        /// <summary>The first accented glyph index at byte offset 4.</summary>
        /// <value>The lowest glyph ID in the accented range.</value>
        /// <seealso cref="AcntTable.FirstAccentGlyphIndex"/>
        public ushort FirstAccentGlyphIndex; // +4

        /// <summary>The last accented glyph index at byte offset 6.</summary>
        /// <value>The highest glyph ID in the accented range, inclusive.</value>
        /// <seealso cref="AcntTable.LastAccentGlyphIndex"/>
        public ushort LastAccentGlyphIndex;  // +6

        /// <summary>The table-relative byte offset of the description data at byte offset 8.</summary>
        /// <value>The offset of the array of 4-byte <see cref="AcntDescriptionEntry"/> records.</value>
        /// <seealso cref="AcntDescriptionEntry"/>
        public uint DescriptionOffset;       // +8

        /// <summary>The table-relative byte offset of the extension data at byte offset 12.</summary>
        /// <value>The offset of the flat stream of 2-byte <see cref="AcntExtension"/> subentries.</value>
        /// <seealso cref="AcntExtension"/>
        public uint ExtensionOffset;         // +12

        /// <summary>The table-relative byte offset of the secondary data at byte offset 16.</summary>
        /// <value>The offset of the array of 3-byte <see cref="AcntSecondaryGlyph"/> entries.</value>
        /// <seealso cref="AcntSecondaryGlyph"/>
        public uint SecondaryOffset;         // +16

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>The struct mixes 32-bit and 16-bit fields, so the reversal is field-by-field rather than a single word-span pass.</remarks>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = Fixed.ReverseEndianness(value.Version),
            FirstAccentGlyphIndex = BinaryPrimitives.ReverseEndianness(value.FirstAccentGlyphIndex),
            LastAccentGlyphIndex = BinaryPrimitives.ReverseEndianness(value.LastAccentGlyphIndex),
            DescriptionOffset = BinaryPrimitives.ReverseEndianness(value.DescriptionOffset),
            ExtensionOffset = BinaryPrimitives.ReverseEndianness(value.ExtensionOffset),
            SecondaryOffset = BinaryPrimitives.ReverseEndianness(value.SecondaryOffset),
        };
    }
}

/// <summary>A 4-byte wire description entry: a format discriminant with a 15-bit primary glyph index, followed by a format-dependent payload word.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Bit 15 of <see cref="DescriptionWord"/> is the format discriminant: <c>0</c> for a single-accent description, <c>1</c> for a multi-accent description.</description></item>
/// <item><description>Bits 0–14 of <see cref="DescriptionWord"/> are the primary glyph index, shared by both formats.</description></item>
/// <item><description>For format 0, <see cref="Payload"/> holds an 8-bit primary attachment point in its high byte and an 8-bit secondary-info index in its low byte.</description></item>
/// <item><description>For format 1, <see cref="Payload"/> holds the byte offset from the start of the extension data to the description's extension chain.</description></item>
/// <item><description>Both fields are <c>UInt16</c>, so the reversal is a single word-span pass over the struct reinterpreted as two 16-bit words.</description></item>
/// <item><description>Consumed at parse time and converted to an <see cref="AcntAccentedGlyph"/>; not retained on <see cref="AcntTable"/>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AcntAccentedGlyph"/>
/// <seealso cref="AcntTable"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AcntDescriptionEntry : IEndianReversibleStruct<AcntDescriptionEntry>
{
    /// <summary>The format discriminant and 15-bit primary glyph index at byte offset 0.</summary>
    /// <value>Bit 15 is the format; bits 0–14 are the primary glyph index.</value>
    /// <seealso cref="IsMultiAccent"/>
    /// <seealso cref="PrimaryGlyphIndex"/>
    public ushort DescriptionWord; // +0

    /// <summary>The format-dependent payload word at byte offset 2.</summary>
    /// <value>For format 0, the high byte is the primary attachment point and the low byte is the secondary-info index. For format 1, the whole word is the extension-data-relative offset of the description's extension chain.</value>
    /// <seealso cref="IsMultiAccent"/>
    public ushort Payload;         // +2

    /// <summary>Gets a value indicating whether the description uses format 1.</summary>
    /// <value><see langword="true"/> when bit 15 of <see cref="DescriptionWord"/> is set.</value>
    /// <seealso cref="DescriptionWord"/>
    public readonly bool IsMultiAccent => (DescriptionWord & 0x8000) != 0;

    /// <summary>Gets the primary glyph index, shared by both formats.</summary>
    /// <value>The low fifteen bits of <see cref="DescriptionWord"/>.</value>
    /// <seealso cref="DescriptionWord"/>
    public readonly ushort PrimaryGlyphIndex => (ushort)(DescriptionWord & 0x7FFF);

    /// <summary>Reverses the byte order of both 16-bit fields in one pass.</summary>
    /// <param name="value">The value whose fields are to be reversed.</param>
    /// <returns>A new entry with both fields reversed.</returns>
    /// <remarks>The entry is entirely <c>UInt16</c> fields, so it is reinterpreted as a span of two 16-bit words and reversed with a single <see cref="BinaryPrimitives.ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})"/> call.</remarks>
    public static AcntDescriptionEntry ReverseEndianness(AcntDescriptionEntry value)
    {
        Span<ushort> words = MemoryMarshal.CreateSpan(ref value.DescriptionWord, 2);
        BinaryPrimitives.ReverseEndianness(words, words);
        return value;
    }
}

/// <summary>A 2-byte extension subentry: one accent in a multi-accent description's extension chain.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Bit 7 of <see cref="ComponentsAndSecondaryIndex"/> is the continuation flag: <c>0</c> means more subentries follow, <c>1</c> means this is the last one.</description></item>
/// <item><description>Bits 0–6 of <see cref="ComponentsAndSecondaryIndex"/> are the secondary-info index, which is limited to <c>[0, 127]</c> by the field width.</description></item>
/// <item><description><see cref="PrimaryAttachmentPoint"/> is the control point in the primary glyph to which this accent attaches.</description></item>
/// <item><description>Both fields are single bytes, so the reversal is a no-op; the type still implements <see cref="IEndianReversibleStruct{T}"/> so it composes uniformly with the reader family.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AcntMultiAccentGlyph"/>
/// <seealso cref="AcntTable"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AcntExtension : IEndianReversibleStruct<AcntExtension>
{
    /// <summary>The continuation flag and 7-bit secondary-info index at byte offset 0.</summary>
    /// <value>Bit 7 is <c>0</c> when more subentries follow and <c>1</c> when this is the last subentry; bits 0–6 are the secondary-info index.</value>
    /// <seealso cref="IsLast"/>
    /// <seealso cref="SecondaryInfoIndex"/>
    public byte ComponentsAndSecondaryIndex; // +0

    /// <summary>The primary glyph attachment point at byte offset 1.</summary>
    /// <value>The control point number in the primary glyph to which this accent attaches.</value>
    public byte PrimaryAttachmentPoint;      // +1

    /// <summary>Gets a value indicating whether this is the last subentry in the chain.</summary>
    /// <value><see langword="true"/> when bit 7 of <see cref="ComponentsAndSecondaryIndex"/> is set.</value>
    /// <seealso cref="ComponentsAndSecondaryIndex"/>
    public readonly bool IsLast => (ComponentsAndSecondaryIndex & 0x80) != 0;

    /// <summary>Gets the secondary-info index for this accent.</summary>
    /// <value>The low seven bits of <see cref="ComponentsAndSecondaryIndex"/>, in <c>[0, 127]</c>.</value>
    /// <seealso cref="ComponentsAndSecondaryIndex"/>
    /// <seealso cref="AcntTable.GetSecondaryGlyph(int)"/>
    public readonly byte SecondaryInfoIndex => (byte)(ComponentsAndSecondaryIndex & 0x7F);

    /// <summary>Reverses the byte order of every multi-byte field in an <see cref="AcntExtension"/>.</summary>
    /// <param name="value">The value to reverse.</param>
    /// <returns>The same value, unchanged.</returns>
    /// <remarks>Both fields are single bytes, which have no byte order; the method is a no-op and exists only so the type satisfies <see cref="IEndianReversibleStruct{T}"/>.</remarks>
    public static AcntExtension ReverseEndianness(AcntExtension value) => value;
}

/// <summary>A 3-byte secondary glyph entry: an accent glyph index and the attachment point on that accent.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The accent glyph index is <c>UInt16</c> and the attachment point is <c>UInt8</c>, giving a packed 3-byte record.</description></item>
/// <item><description>A secondary glyph can have only one attachment point, which is why the entry stores a single point number rather than a list.</description></item>
/// <item><description>The struct declares <c>Pack = 1</c> so its managed size is exactly three bytes; bulk reads over an array of entries rely on that size.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AcntTable.SecondaryGlyphs"/>
/// <seealso cref="AcntTable.GetSecondaryGlyph(int)"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AcntSecondaryGlyph : IEndianReversibleStruct<AcntSecondaryGlyph>
{
    /// <summary>The accent glyph index at byte offset 0.</summary>
    /// <value>The glyph ID of the accent that attaches to a primary glyph.</value>
    public ushort SecondaryGlyphIndex;          // +0

    /// <summary>The accent glyph attachment point number at byte offset 2.</summary>
    /// <value>The control point number on the accent glyph that mates with the primary glyph's attachment point.</value>
    public byte SecondaryGlyphAttachmentNumber; // +2

    /// <summary>Reverses the byte order of every multi-byte field in an <see cref="AcntSecondaryGlyph"/>.</summary>
    /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new entry with the glyph index reversed and the attachment number unchanged.</returns>
    /// <remarks>Only <see cref="SecondaryGlyphIndex"/> is multi-byte; the attachment number is a single byte and therefore has no byte order.</remarks>
    public static AcntSecondaryGlyph ReverseEndianness(AcntSecondaryGlyph value) => new()
    {
        SecondaryGlyphIndex = BinaryPrimitives.ReverseEndianness(value.SecondaryGlyphIndex),
        SecondaryGlyphAttachmentNumber = value.SecondaryGlyphAttachmentNumber,
    };
}

/// <summary>Base for the two accented-glyph description formats: <see cref="AcntSingleAccentGlyph"/> and <see cref="AcntMultiAccentGlyph"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Every description begins with a 4-byte <see cref="AcntDescriptionEntry"/>; bit 15 of the entry's description word selects the format.</description></item>
/// <item><description>Format 0 is self-contained; format 1 points into the table's extension-data region and requires the <see cref="Context"/> that carries the root source and the extension-data start.</description></item>
/// <item><description>Declares <see cref="IBaseRecord{TBase}"/> and dispatches to the derived formats through its F-bounded <c>FromHeader</c> helper, so the base reads the 4-byte header exactly once and never asks a derived parser to re-read it.</description></item>
/// <item><description>External callers use the F-bounded <see cref="IRecord{T}"/>-based helpers on <see cref="Cursor"/> and <see cref="Source"/>; the derived types are not directly parseable.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AcntSingleAccentGlyph"/>
/// <seealso cref="AcntMultiAccentGlyph"/>
/// <seealso cref="AcntTable"/>
public abstract record AcntAccentedGlyph : IEndianReversibleHeaderRecord<AcntAccentedGlyph, AcntDescriptionEntry>, IBaseRecord<AcntAccentedGlyph>
{
    /// <summary>Gets the description format discriminant.</summary>
    /// <value><c>0</c> for a single-accent description; <c>1</c> for a multi-accent description.</value>
    /// <seealso cref="AcntDescriptionEntry.IsMultiAccent"/>
    public abstract int Format { get; }

    /// <summary>Gets the primary glyph index that the accents attach to.</summary>
    /// <value>The glyph ID of the base form, shared by both description formats.</value>
    /// <seealso cref="AcntDescriptionEntry.PrimaryGlyphIndex"/>
    public abstract ushort PrimaryGlyphIndex { get; init; }

    static AcntAccentedGlyph IHeaderRecord<AcntAccentedGlyph, AcntDescriptionEntry>.FromHeader(in AcntDescriptionEntry header, object? context) => header.IsMultiAccent
        ? IBaseRecord<AcntAccentedGlyph>.FromHeader<AcntMultiAccentGlyph, AcntDescriptionEntry>(in header, context)
        : IBaseRecord<AcntAccentedGlyph>.FromHeader<AcntSingleAccentGlyph, AcntDescriptionEntry>(in header, context);

    internal abstract int GetMaxSecondaryInfoIndex();

    /// <summary>Context for parsing a multi-accent description: the root source and the source-relative start of the extension data.</summary>
    /// <param name="RootSource">The source whose origin the extension-data start is measured from; typically the table-scoped source.</param>
    /// <param name="ExtensionDataStart">The source-relative offset of the extension-data region, from which each description's own extension offset is measured.</param>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The root source is carried separately from the cursor's source so that a sliced table source still reaches the extension region through the same origin.</description></item>
    /// <item><description>The context is required only for format 1; format 0 descriptions are self-contained and ignore it.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AcntMultiAccentGlyph"/>
    public sealed record Context(Source RootSource, long ExtensionDataStart);
}

/// <summary>A format 0 accented-glyph description: one primary glyph plus exactly one accent, described entirely within its 4-byte entry.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The primary glyph's attachment point and the accent's secondary-info index are both single bytes packed into the entry's payload word.</description></item>
/// <item><description>The accent glyph itself is resolved by indexing <see cref="AcntTable.SecondaryGlyphs"/> with <see cref="SecondaryInfoIndex"/>.</description></item>
/// <item><description>Implements <see cref="IEndianReversibleHeaderDerivedRecord{TBase, TDerived, THeader}"/>; only <see cref="AcntAccentedGlyph"/> constructs it, and external callers reach it only by parsing the base type.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">format 0 description</see> section of the <c>acnt</c> table documentation.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AcntAccentedGlyph"/>
/// <seealso cref="AcntMultiAccentGlyph"/>
/// <seealso cref="AcntTable"/>
public sealed record AcntSingleAccentGlyph : AcntAccentedGlyph, IEndianReversibleHeaderDerivedRecord<AcntAccentedGlyph, AcntSingleAccentGlyph, AcntDescriptionEntry>
{
    /// <inheritdoc/>
    public override int Format => 0;

    /// <inheritdoc/>
    public override ushort PrimaryGlyphIndex { get; init; }

    /// <summary>Gets the control point number in the primary glyph to which the accent attaches.</summary>
    /// <value>The attachment point on the primary glyph. Paired with the accent glyph's own attachment number from <see cref="AcntSecondaryGlyph"/>.</value>
    /// <seealso cref="SecondaryInfoIndex"/>
    public byte PrimaryAttachmentPoint { get; init; }

    /// <summary>Gets the index of the accent entry in <see cref="AcntTable.SecondaryGlyphs"/>.</summary>
    /// <value>The zero-based index of the <see cref="AcntSecondaryGlyph"/> that describes the accent glyph and its attachment point.</value>
    /// <seealso cref="AcntTable.GetSecondaryGlyph(int)"/>
    /// <seealso cref="AcntSecondaryGlyph"/>
    public byte SecondaryInfoIndex { get; init; }

    internal override int GetMaxSecondaryInfoIndex() => SecondaryInfoIndex;

    static AcntSingleAccentGlyph IHeaderDerivedRecord<AcntAccentedGlyph, AcntSingleAccentGlyph, AcntDescriptionEntry>.FromHeader(in AcntDescriptionEntry header, object? context) => new()
    {
        PrimaryGlyphIndex = header.PrimaryGlyphIndex,
        PrimaryAttachmentPoint = (byte)(header.Payload >> 8),
        SecondaryInfoIndex = (byte)(header.Payload & 0xFF),
    };
}

/// <summary>A format 1 accented-glyph description: one primary glyph plus two or more accents, whose attachments are stored in an extension chain.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The entry stores the primary glyph index and an offset into the table's extension-data region; the extension chain at that offset lists each accent in order.</description></item>
/// <item><description>The chain is a flat sequence of 2-byte <see cref="AcntExtension"/> subentries, terminated by the first subentry whose continuation flag is set.</description></item>
/// <item><description>Each subentry carries its own primary attachment point and secondary-info index, so different accents can attach at different control points on the same primary glyph.</description></item>
/// <item><description>Implements <see cref="IEndianReversibleHeaderDerivedRecord{TBase, TDerived, THeader}"/>; only <see cref="AcntAccentedGlyph"/> constructs it, and the extension chain is read from <see cref="AcntAccentedGlyph.Context.RootSource"/> at absolute offsets.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">format 1 description</see> and <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">extension format</see> sections of the <c>acnt</c> table documentation.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AcntAccentedGlyph"/>
/// <seealso cref="AcntSingleAccentGlyph"/>
/// <seealso cref="AcntExtension"/>
/// <seealso cref="AcntTable"/>
public sealed record AcntMultiAccentGlyph : AcntAccentedGlyph, IEndianReversibleHeaderDerivedRecord<AcntAccentedGlyph, AcntMultiAccentGlyph, AcntDescriptionEntry>
{
    /// <inheritdoc/>
    public override int Format => 1;

    /// <inheritdoc/>
    public override ushort PrimaryGlyphIndex { get; init; }

    /// <summary>Gets the extension subentries that describe this glyph's accents, in attachment order.</summary>
    /// <value>One <see cref="AcntExtension"/> per accent. The array includes the terminating subentry whose continuation flag is set.</value>
    /// <seealso cref="AcntExtension"/>
    public required AcntExtension[] Extensions { get; init; }

    internal override int GetMaxSecondaryInfoIndex()
    {
        int max = -1;
        foreach (AcntExtension extension in Extensions)
            if (extension.SecondaryInfoIndex > max) max = extension.SecondaryInfoIndex;
        return max;
    }

    static AcntMultiAccentGlyph IHeaderDerivedRecord<AcntAccentedGlyph, AcntMultiAccentGlyph, AcntDescriptionEntry>.FromHeader(in AcntDescriptionEntry header, object? context)
    {
        if (context is not Context ctx)
            throw new InvalidDataException("'acnt' multi-accent description requires an AcntAccentedGlyph.Context carrying the root source and extension-data start.");

        long position = ctx.ExtensionDataStart + header.Payload;
        var extensions = new List<AcntExtension>();
        while (true)
        {
            AcntExtension extension = ctx.RootSource.ReadEndianReversibleStructAt<AcntExtension>(position);
            extensions.Add(extension);
            position += 2;
            if (extension.IsLast) break;
        }

        return new()
        {
            PrimaryGlyphIndex = header.PrimaryGlyphIndex,
            Extensions = [.. extensions],
        };
    }
}
