using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>acnt</c> table: the Apple Advanced Typography accent attachment table. Provides a space-efficient method of combining component glyphs into compound glyphs to form accents.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>macOS and iOS do not support the table, and the specification text states that its use is discouraged. It is documented here for completeness and for round-tripping older fonts, not as a recommended way to build accented glyphs. Contextual accent composition in modern AAT fonts is expressed through the metamorphosis tables instead.</description></item>
/// <item><description>The table describes accented glyphs as a primary component plus one or more secondary components, positioned by matching attachment points. Properties other than the outline — metrics, kerning values, PostScript names — are inherited from or derivable from the components.</description></item>
/// <item><description>Accented glyph indices must be contiguous and start at an index greater than the highest regular glyph index. <see cref="FirstAccentGlyphIndex"/> and <see cref="LastAccentGlyphIndex"/> declare that range.</description></item>
/// <item><description>A font may declare at most <see cref="MaxPrimaryComponents"/> primary components and at most <see cref="MaxSecondaryComponents"/> secondary components.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">accent attachment table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AcntGlyphDescription"/>
/// <seealso cref="AcntSecondaryEntry"/>
/// <seealso cref="AcntExtensionEntry"/>
/// <seealso cref="GlyfTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
public sealed record AcntTable : IFontTable<AcntTable>, IRecord<AcntTable>
{
    /// <summary>The version value for the only defined accent attachment table format, 1.0.</summary>
    /// <value>The <see cref="Fixed"/> value whose raw bits are <c>0x00010000</c>.</value>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    public static readonly Fixed CurrentVersion = new(0x00010000);

    /// <summary>The maximum number of accented glyphs a font may declare, 32 768.</summary>
    /// <remarks>The limit is quoted by the specification and follows from the fifteen-bit <c>primaryGlyphIndex</c> field shared by both description formats.</remarks>
    public const int MaxPrimaryComponents = 32768;

    /// <summary>The maximum number of secondary components a font may declare, 255.</summary>
    /// <remarks>The limit is quoted by the specification and follows from the seven-bit <c>secondaryInfoIndex</c> field of an extension entry.</remarks>
    public const int MaxSecondaryComponents = 255;

    /// <inheritdoc/>
    public static Tag Tag => "acnt";

    /// <summary>Gets the table version.</summary>
    /// <value>The <see cref="CurrentVersion"/> value for a conforming table.</value>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the first accented glyph index.</summary>
    /// <value>The lowest glyph index described by this table. Glyph indices for accented glyphs must be contiguous and must exceed the highest index of any regular glyph.</value>
    /// <seealso cref="LastAccentGlyphIndex"/>
    /// <seealso cref="AccentGlyphCount"/>
    public ushort FirstAccentGlyphIndex { get; init; }

    /// <summary>Gets the last accented glyph index, inclusive.</summary>
    /// <value>The highest glyph index described by this table.</value>
    /// <seealso cref="FirstAccentGlyphIndex"/>
    /// <seealso cref="AccentGlyphCount"/>
    public ushort LastAccentGlyphIndex { get; init; }

    /// <summary>Gets the per-glyph description entries, indexed by <c>glyphId - <see cref="FirstAccentGlyphIndex"/></c>.</summary>
    /// <value>An array of <see cref="AcntGlyphDescription"/> entries; entry <c>i</c> describes the glyph at index <c>FirstAccentGlyphIndex + i</c>.</value>
    /// <seealso cref="AcntGlyphDescription"/>
    /// <seealso cref="AcntSimpleDescription"/>
    /// <seealso cref="AcntExtendedDescription"/>
    public IReadOnlyList<AcntGlyphDescription> GlyphDescriptions { get; init; } = [];

    /// <summary>Gets the secondary glyph entries, indexed by the secondary information index used in descriptions and extensions.</summary>
    /// <value>An array of <see cref="AcntSecondaryEntry"/> entries, each naming a secondary glyph and its attachment point. The array length is bounded by <see cref="MaxSecondaryComponents"/>.</value>
    /// <seealso cref="AcntSecondaryEntry"/>
    /// <seealso cref="AcntSimpleDescription.SecondaryInfoIndex"/>
    /// <seealso cref="AcntExtensionEntry.SecondaryInfoIndex"/>
    public IReadOnlyList<AcntSecondaryEntry> SecondaryInfo { get; init; } = [];

    /// <summary>Gets the number of accented glyphs described by the table.</summary>
    /// <value>Equal to <c><see cref="LastAccentGlyphIndex"/> - <see cref="FirstAccentGlyphIndex"/> + 1</c>.</value>
    /// <seealso cref="FirstAccentGlyphIndex"/>
    /// <seealso cref="LastAccentGlyphIndex"/>
    /// <seealso cref="GlyphDescriptions"/>
    public int AccentGlyphCount => LastAccentGlyphIndex - FirstAccentGlyphIndex + 1;

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the <c>acnt</c> table.</param>
    /// <param name="context">Unused. The table is self-contained and holds no offsets into other tables.</param>
    /// <returns>The parsed <c>acnt</c> table with every description and extension entry materialized.</returns>
    /// <exception cref="InvalidDataException">The version is not 1.0, an offset field is inconsistent with the table length, the declared glyph or secondary count exceeds the specification limits, or the secondary data region is not a whole number of entries.</exception>
    /// <exception cref="EndOfStreamException">A description, extension sequence, or secondary entry extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The parse is eager: every description entry and every referenced extension sequence is materialized before the method returns.</description></item>
    /// <item><description>The table carries three offset fields — <see cref="Header.DescriptionOffset"/>, <see cref="Header.ExtensionOffset"/>, and <see cref="Header.SecondaryOffset"/> — that locate the three data regions. The parser reads descriptions from the cursor, then reads extension data by absolute offset from the extension region, then reads the secondary array by absolute offset.</description></item>
    /// <item><description>The number of secondary entries is not declared in the header; it is derived from the byte count between <see cref="Header.SecondaryOffset"/> and the end of the table, divided by the fixed entry size of <see cref="AcntSecondaryEntry"/>.</description></item>
    /// <item><description>Extension sequences are bounded by the start of the secondary data region, since the specification lays the three regions out in that order.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AcntGlyphContext"/>
    /// <seealso cref="AcntExtensionEntry.ReadAll(Source, long, long)"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    static AcntTable IRecord<AcntTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
        {
            throw new InvalidDataException(
                $"'acnt'.version is 0x{header.Version.Bits:X8}, expected 0x{CurrentVersion.Bits:X8}.");
        }

        long tableLength = cursor.Source.Length;
        long extensionStart = header.ExtensionOffset;
        long secondaryStart = header.SecondaryOffset;

        if (extensionStart > tableLength || secondaryStart > tableLength)
        {
            throw new InvalidDataException(
                $"'acnt' offset fields exceed the table length of {tableLength} bytes.");
        }

        if (secondaryStart < extensionStart)
        {
            throw new InvalidDataException(
                $"'acnt'.secondaryOffset {secondaryStart} precedes extensionOffset {extensionStart}.");
        }

        int glyphCount = header.LastAccentGlyphIndex - header.FirstAccentGlyphIndex + 1;
        if (glyphCount > MaxPrimaryComponents)
        {
            throw new InvalidDataException(
                $"'acnt' declares {glyphCount} accented glyphs, exceeding the limit of {MaxPrimaryComponents}.");
        }

        var descriptions = new AcntGlyphDescription[glyphCount];
        var glyphContext = new AcntGlyphContext(header.ExtensionOffset, secondaryStart);
        for (int i = 0; i < glyphCount; i++)
            descriptions[i] = cursor.ReadRecord<AcntGlyphDescription>(glyphContext);

        long secondaryBytes = tableLength - secondaryStart;
        if (secondaryBytes % AcntSecondaryEntry.SizeInBytes != 0)
        {
            throw new InvalidDataException(
                $"'acnt' secondary data region is {secondaryBytes} bytes, not a multiple of {AcntSecondaryEntry.SizeInBytes}.");
        }

        int secondaryCount = (int)(secondaryBytes / AcntSecondaryEntry.SizeInBytes);
        if (secondaryCount > MaxSecondaryComponents)
        {
            throw new InvalidDataException(
                $"'acnt' declares {secondaryCount} secondary entries, exceeding the limit of {MaxSecondaryComponents}.");
        }

        var secondaryCursor = cursor.Source.CreateCursor(secondaryStart);
        var secondary = secondaryCursor.ReadEndianReversibleStructArray<AcntSecondaryEntry>(secondaryCount);

        return new()
        {
            Version = header.Version,
            FirstAccentGlyphIndex = header.FirstAccentGlyphIndex,
            LastAccentGlyphIndex = header.LastAccentGlyphIndex,
            GlyphDescriptions = descriptions,
            SecondaryInfo = secondary,
        };
    }

    /// <summary>The 20-byte accent attachment table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed by the three data regions in an implementation-defined order, located by the three offset fields.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">accent attachment table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AcntTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0, as a <c>Fixed</c> (16.16) value.</summary>
        /// <value>The <see cref="CurrentVersion"/> value for a conforming table.</value>
        public Fixed Version;                 // +0

        /// <summary>The first accented glyph index at byte offset 4.</summary>
        /// <seealso cref="AcntTable.FirstAccentGlyphIndex"/>
        public ushort FirstAccentGlyphIndex;  // +4

        /// <summary>The last accented glyph index, inclusive, at byte offset 6.</summary>
        /// <seealso cref="AcntTable.LastAccentGlyphIndex"/>
        public ushort LastAccentGlyphIndex;   // +6

        /// <summary>The byte offset from the table start to the description data at byte offset 8.</summary>
        public uint DescriptionOffset;        // +8

        /// <summary>The byte offset from the table start to the extension data at byte offset 12.</summary>
        /// <seealso cref="AcntExtendedDescription.ExtensionOffset"/>
        public uint ExtensionOffset;          // +12

        /// <summary>The byte offset from the table start to the secondary data at byte offset 16.</summary>
        /// <seealso cref="AcntTable.SecondaryInfo"/>
        public uint SecondaryOffset;          // +16

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
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

/// <summary>Context threaded from the <c>acnt</c> table parser into the description dispatcher so a format 1 description can resolve its extension data.</summary>
/// <param name="ExtensionOffset">The byte offset of the extension data region from the table start, from <see cref="AcntTable.Header.ExtensionOffset"/>.</param>
/// <param name="ExtensionEnd">The exclusive byte offset at which the extension data region ends, equal to <see cref="AcntTable.Header.SecondaryOffset"/>.</param>
/// <remarks>
/// <list type="bullet">
/// <item><description>Only <see cref="AcntExtendedDescription"/> consumes the context; <see cref="AcntSimpleDescription"/> ignores it.</description></item>
/// <item><description>The end offset bounds the extension entry sequence so a malformed entry list cannot read past the extension region into the secondary data.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AcntExtendedDescription"/>
/// <seealso cref="AcntTable.Header"/>
public record AcntGlyphContext(uint ExtensionOffset, long ExtensionEnd);

/// <summary>A per-glyph description entry in an <see cref="AcntTable"/>. Either a <see cref="AcntSimpleDescription"/> for a glyph formed from a primary and one secondary component, or an <see cref="AcntExtendedDescription"/> for a glyph formed from a primary and two or more secondary components.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Both formats are four bytes. The dispatcher peeks at the high bit of the first <c>uint16</c>, which is the description format discriminant; the low fifteen bits are the <see cref="PrimaryGlyphIndex"/> in both formats.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">accent attachment table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AcntSimpleDescription"/>
/// <seealso cref="AcntExtendedDescription"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
public abstract record AcntGlyphDescription : IRecord<AcntGlyphDescription>, IBaseRecord<AcntGlyphDescription>
{
    /// <summary>Gets the primary glyph index for this accented glyph.</summary>
    /// <value>The glyph index of the baseform glyph in the <c>glyf</c> table. The low fifteen bits of the shared leading word; the high bit is the format discriminant.</value>
    /// <seealso cref="GlyfTable"/>
    public ushort PrimaryGlyphIndex { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the four-byte description entry.</param>
    /// <param name="context">An <see cref="AcntGlyphContext"/> carrying the extension data bounds, or <see langword="null"/> when only the description is needed.</param>
    /// <returns>An <see cref="AcntSimpleDescription"/> when the format bit is clear, or an <see cref="AcntExtendedDescription"/> when it is set.</returns>
    /// <exception cref="InvalidOperationException">The dispatcher selected a format 1 entry and <paramref name="context"/> is not an <see cref="AcntGlyphContext"/>.</exception>
    /// <exception cref="EndOfStreamException">The description or its extension data extends past the end of the table-scoped source.</exception>
    /// <remarks>The dispatcher peeks at the leading word so both derived parsers can read the full four bytes themselves, extracting the shared <see cref="PrimaryGlyphIndex"/> from the same word the discriminant lives in.</remarks>
    /// <seealso cref="AcntSimpleDescription"/>
    /// <seealso cref="AcntExtendedDescription"/>
    static AcntGlyphDescription IRecord<AcntGlyphDescription>.Parse(ref Cursor cursor, object? context)
    {
        ushort word = cursor.PeekUInt16();

        return (word & 0x8000) != 0
            ? IBaseRecord<AcntGlyphDescription>.Parse<AcntExtendedDescription>(ref cursor, context)
            : IBaseRecord<AcntGlyphDescription>.Parse<AcntSimpleDescription>(ref cursor, context);
    }
}

/// <summary>An <c>acnt</c> format 0 description: a glyph formed from a primary component and a single secondary component.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The description names one point in the primary glyph and one entry in the secondary data table. The secondary data entry in turn names the accent glyph and its own attachment point.</description></item>
/// <item><description>The two attachment points are aligned when the accented glyph is composed, which is the only positioning mechanism the format defines.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">accent attachment table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AcntGlyphDescription"/>
/// <seealso cref="AcntExtendedDescription"/>
/// <seealso cref="AcntSecondaryEntry"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
public sealed record AcntSimpleDescription : AcntGlyphDescription, IDerivedRecord<AcntGlyphDescription, AcntSimpleDescription>
{
    /// <summary>Gets the primary attachment control point number.</summary>
    /// <value>The index of the point in the primary glyph to which the secondary component is attached.</value>
    /// <seealso cref="SecondaryInfoIndex"/>
    /// <seealso cref="AcntSecondaryEntry.SecondaryGlyphAttachmentNumber"/>
    public byte PrimaryAttachmentPoint { get; init; }

    /// <summary>Gets the secondary information index.</summary>
    /// <value>An index into <see cref="AcntTable.SecondaryInfo"/> naming the secondary glyph and its attachment point.</value>
    /// <seealso cref="AcntTable.SecondaryInfo"/>
    /// <seealso cref="PrimaryAttachmentPoint"/>
    public byte SecondaryInfoIndex { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the four-byte description entry.</param>
    /// <param name="context">Ignored; a format 0 description is self-contained and has no extension data.</param>
    /// <returns>The parsed format 0 description.</returns>
    /// <exception cref="EndOfStreamException">The description extends past the end of the table-scoped source.</exception>
    static AcntSimpleDescription IDerivedRecord<AcntGlyphDescription, AcntSimpleDescription>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        return new()
        {
            PrimaryGlyphIndex = (ushort)(header.Description & 0x7FFF),
            PrimaryAttachmentPoint = header.PrimaryAttachmentPoint,
            SecondaryInfoIndex = header.SecondaryInfoIndex,
        };
    }

    /// <summary>The 4-byte format 0 description entry. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The leading <c>uint16</c> packs the format discriminant in its high bit and the fifteen-bit <see cref="AcntGlyphDescription.PrimaryGlyphIndex"/> in its low bits.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">accent attachment table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AcntSimpleDescription"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The packed description word at byte offset 0: the format bit and the fifteen-bit primary glyph index.</summary>
        public ushort Description;              // +0

        /// <summary>The primary attachment control point number at byte offset 2.</summary>
        public byte PrimaryAttachmentPoint;     // +2

        /// <summary>The secondary information index at byte offset 3.</summary>
        public byte SecondaryInfoIndex;         // +3

        /// <summary>Reverses the byte order of the packed word and copies the byte-only fields through.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with <see cref="Description"/> reversed.</returns>
        public static Header ReverseEndianness(Header value) => new()
        {
            Description = BinaryPrimitives.ReverseEndianness(value.Description),
            PrimaryAttachmentPoint = value.PrimaryAttachmentPoint,
            SecondaryInfoIndex = value.SecondaryInfoIndex,
        };
    }
}

/// <summary>An <c>acnt</c> format 1 description: a glyph formed from a primary component and two or more secondary components.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The description points at an entry in the extension data region. Each extension entry names an accent through <see cref="AcntExtensionEntry.SecondaryInfoIndex"/> and a point in the primary glyph through <see cref="AcntExtensionEntry.PrimaryAttachmentPoint"/>.</description></item>
/// <item><description>The extension entry list is terminated by an entry whose <see cref="AcntExtensionEntry.IsLast"/> flag is set.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">accent attachment table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AcntGlyphDescription"/>
/// <seealso cref="AcntSimpleDescription"/>
/// <seealso cref="AcntExtensionEntry"/>
/// <seealso cref="AcntSecondaryEntry"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
public sealed record AcntExtendedDescription : AcntGlyphDescription, IDerivedRecord<AcntGlyphDescription, AcntExtendedDescription>
{
    /// <summary>Gets the byte offset from the start of the extension data region to this glyph's extension entries.</summary>
    /// <value>A byte offset relative to <see cref="AcntTable.Header.ExtensionOffset"/>. The offset is not relative to the table start.</value>
    /// <seealso cref="Accents"/>
    public ushort ExtensionOffset { get; init; }

    /// <summary>Gets the extension entries for this glyph, in attachment order.</summary>
    /// <value>An array of <see cref="AcntExtensionEntry"/> entries; the final entry has <see cref="AcntExtensionEntry.IsLast"/> set.</value>
    /// <remarks>The entries are materialized during parse so that consumers do not need to re-read the extension region.</remarks>
    /// <seealso cref="AcntExtensionEntry"/>
    /// <seealso cref="ExtensionOffset"/>
    public IReadOnlyList<AcntExtensionEntry> Accents { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the four-byte description entry.</param>
    /// <param name="context">An <see cref="AcntGlyphContext"/> carrying the extension data bounds.</param>
    /// <returns>The parsed format 1 description with its extension entries materialized.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not an <see cref="AcntGlyphContext"/>.</exception>
    /// <exception cref="InvalidDataException">The extension offset points outside the extension data region, or an entry sequence is unterminated.</exception>
    /// <exception cref="EndOfStreamException">The description or its extension entries extend past the end of the table-scoped source.</exception>
    static AcntExtendedDescription IDerivedRecord<AcntGlyphDescription, AcntExtendedDescription>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not AcntGlyphContext ctx)
        {
            throw new InvalidOperationException(
                $"{nameof(AcntExtendedDescription)} requires an {nameof(AcntGlyphContext)} to resolve its extension data.");
        }

        Header header = cursor.ReadEndianReversibleStruct<Header>();

        long start = ctx.ExtensionOffset + header.ExtensionOffset;
        if (start < ctx.ExtensionOffset || start >= ctx.ExtensionEnd)
        {
            throw new InvalidDataException(
                $"'acnt' extension offset {header.ExtensionOffset} is outside the extension data region [{ctx.ExtensionOffset}, {ctx.ExtensionEnd}).");
        }

        return new()
        {
            PrimaryGlyphIndex = (ushort)(header.Description & 0x7FFF),
            ExtensionOffset = header.ExtensionOffset,
            Accents = AcntExtensionEntry.ReadAll(cursor.Source, start, ctx.ExtensionEnd),
        };
    }

    /// <summary>The 4-byte format 1 description entry. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The leading <c>uint16</c> packs the format discriminant in its high bit and the fifteen-bit <see cref="AcntGlyphDescription.PrimaryGlyphIndex"/> in its low bits.</description></item>
    /// <item><description>The trailing <c>uint16</c> is a byte offset relative to the start of the extension data region, not to the table start.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">accent attachment table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AcntExtendedDescription"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The packed description word at byte offset 0: the format bit and the fifteen-bit primary glyph index.</summary>
        public ushort Description;        // +0

        /// <summary>The byte offset to the extension entries at byte offset 2, relative to the start of the extension data region.</summary>
        public ushort ExtensionOffset;    // +2

        /// <summary>Reverses the byte order of both fields in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        public static Header ReverseEndianness(Header value) => new()
        {
            Description = BinaryPrimitives.ReverseEndianness(value.Description),
            ExtensionOffset = BinaryPrimitives.ReverseEndianness(value.ExtensionOffset),
        };
    }
}

/// <summary>One extension entry in an <c>acnt</c> format 1 description: a reference to a secondary component and a point in the primary glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each entry is two bytes. The first byte carries the seven-bit <see cref="SecondaryInfoIndex"/> in its low bits and the <see cref="IsLast"/> flag in its high bit; the second byte is <see cref="PrimaryAttachmentPoint"/>.</description></item>
/// <item><description>Entries are read in sequence until one has <see cref="IsLast"/> set, which terminates the accent list for the glyph.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">accent attachment table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AcntExtendedDescription"/>
/// <seealso cref="AcntSecondaryEntry"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
public sealed record AcntExtensionEntry
{
    /// <summary>Gets the secondary information index.</summary>
    /// <value>A seven-bit index into <see cref="AcntTable.SecondaryInfo"/> naming the secondary glyph and its attachment point.</value>
    /// <seealso cref="AcntTable.SecondaryInfo"/>
    /// <seealso cref="PrimaryAttachmentPoint"/>
    /// <seealso cref="IsLast"/>
    public byte SecondaryInfoIndex { get; init; }

    /// <summary>Gets the primary attachment control point number.</summary>
    /// <value>The index of the point in the primary glyph to which the secondary component is attached.</value>
    /// <seealso cref="SecondaryInfoIndex"/>
    public byte PrimaryAttachmentPoint { get; init; }

    /// <summary>Gets a value indicating whether this is the last component of the glyph.</summary>
    /// <value><see langword="true"/> when the entry terminates the extension sequence for its glyph.</value>
    /// <remarks>The flag is documented as "components" in the specification's packed field list; the semantics are "there are more components" when clear and "this is the last" when set.</remarks>
    /// <seealso cref="SecondaryInfoIndex"/>
    public bool IsLast { get; init; }

    /// <summary>Reads an extension entry sequence from <paramref name="start"/> up to but not including <paramref name="end"/>.</summary>
    /// <param name="source">The table-scoped source.</param>
    /// <param name="start">The absolute byte offset of the first entry.</param>
    /// <param name="end">The exclusive upper bound of the extension data region.</param>
    /// <returns>An array of extension entries; the final entry has <see cref="IsLast"/> set.</returns>
    /// <exception cref="InvalidDataException">The sequence extends past <paramref name="end"/> without an entry whose <see cref="IsLast"/> flag is set.</exception>
    /// <exception cref="EndOfStreamException">The read extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The reader stops at the first entry with <see cref="IsLast"/> set; a well-formed sequence is terminated by exactly one such entry.</description></item>
    /// <item><description>The <paramref name="end"/> bound prevents a malformed sequence from reading into the secondary data region that follows the extension data.</description></item>
    /// <item><description>The read is eager: the entire sequence is materialized in a single pass over the source.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AcntExtendedDescription.Accents"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    public static AcntExtensionEntry[] ReadAll(Source source, long start, long end)
    {
        var entries = new List<AcntExtensionEntry>();
        var cursor = source.CreateCursor(start);

        while (true)
        {
            if (cursor.Position + Header.SizeInBytes > end)
            {
                throw new InvalidDataException(
                    $"'acnt' extension entry sequence at offset {start} is unterminated within the region ending at {end}.");
            }

            Header header = cursor.ReadStruct<Header>();

            bool isLast = (header.Packed & 0x80) != 0;
            entries.Add(new()
            {
                SecondaryInfoIndex = (byte)(header.Packed & 0x7F),
                PrimaryAttachmentPoint = header.PrimaryAttachmentPoint,
                IsLast = isLast,
            });

            if (isLast) break;
        }

        return [.. entries];
    }

    /// <summary>The 2-byte extension entry. Blittable, no padding. All fields are byte-width, so the struct requires no endianness conversion and does not implement <see cref="IEndianReversibleStruct{T}"/>.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The leading byte packs the <c>components</c> flag in its high bit and the seven-bit <c>secondaryInfoIndex</c> in its low bits.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">accent attachment table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AcntExtensionEntry"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header
    {
        /// <summary>The size of one extension entry header, in bytes.</summary>
        /// <value>The constant <c>2</c>.</value>
        public const int SizeInBytes = 2;

        /// <summary>The packed byte at byte offset 0: the last-component flag and the seven-bit secondary information index.</summary>
        public byte Packed;                 // +0

        /// <summary>The primary attachment control point number at byte offset 1.</summary>
        public byte PrimaryAttachmentPoint; // +1
    }
}

/// <summary>One secondary component entry in an <c>acnt</c> table: the glyph index of the accent and the point in that glyph that attaches to the primary component.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each entry is three bytes and is indexed by the seven-bit <see cref="AcntSimpleDescription.SecondaryInfoIndex"/> or <see cref="AcntExtensionEntry.SecondaryInfoIndex"/> that references it.</description></item>
/// <item><description>Accent placement is defined entirely by matching the secondary glyph's attachment point with the corresponding primary attachment point.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">accent attachment table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AcntTable.SecondaryInfo"/>
/// <seealso cref="AcntSimpleDescription"/>
/// <seealso cref="AcntExtensionEntry"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6acnt.html">TrueType Reference Manual: The <c>acnt</c> table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AcntSecondaryEntry : IEndianReversibleStruct<AcntSecondaryEntry>
{
    /// <summary>The size of one secondary entry, in bytes.</summary>
    /// <value>The constant <c>3</c>.</value>
    /// <remarks>The size is fixed; the count of secondary entries in the table is derived by dividing the secondary data region length by this constant.</remarks>
    public const int SizeInBytes = 3;

    /// <summary>The glyph index of the secondary component at byte offset 0.</summary>
    /// <value>A glyph index in the <c>glyf</c> table.</value>
    /// <seealso cref="SecondaryGlyphAttachmentNumber"/>
    /// <seealso cref="GlyfTable"/>
    public ushort SecondaryGlyphIndex;         // +0

    /// <summary>The attachment point index within the secondary glyph at byte offset 2.</summary>
    /// <value>The index of the point in the secondary glyph that attaches to the primary component.</value>
    /// <seealso cref="SecondaryGlyphIndex"/>
    public byte SecondaryGlyphAttachmentNumber; // +2

    /// <inheritdoc/>
    /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new entry with each multi-byte field reversed.</returns>
    public static AcntSecondaryEntry ReverseEndianness(AcntSecondaryEntry value) => new()
    {
        SecondaryGlyphIndex = BinaryPrimitives.ReverseEndianness(value.SecondaryGlyphIndex),
        SecondaryGlyphAttachmentNumber = value.SecondaryGlyphAttachmentNumber,
    };
}
