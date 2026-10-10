using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// bhed — Bitmap Font Header Table (classic Apple bitmap-only fonts)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>bhed</c> table: the global header of an Apple bitmap-only font, and the bitmap-font counterpart of the OpenType <see cref="HeadTable">head</see> table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is byte-for-byte identical with <c>head</c>; only the tag differs. OS X uses the presence of <c>bhed</c> as a flag that the font contains no glyph outlines and only embedded bitmaps. See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bhed.html"><c>bhed</c> chapter</see> of Apple's TrueType Reference Manual.</description></item>
/// <item><description>A font carrying <c>bhed</c> must also carry <see cref="BdatTable"/> and <see cref="BlocTable"/>, and must have neither a <c>head</c> nor a <c>glyf</c> table.</description></item>
/// <item><description>The field semantics match <see cref="HeadTable"/> field for field. The <c>indexToLocFormat</c> and <c>glyphDataFormat</c> fields are meaningless in a bitmap-only font but are retained so that the layout stays identical to <c>head</c>.</description></item>
/// <item><description>Unlike <see cref="HeadTable"/>, this type validates the required <c>magicNumber</c> constant during parse and throws <see cref="InvalidDataException"/> when it does not match.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="HeadTable"/>
/// <seealso cref="BdatTable"/>
/// <seealso cref="BlocTable"/>
/// <seealso cref="HeadFlags"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bhed.html">Apple TrueType Reference Manual: <c>bhed</c> table</seealso>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6head.html">Apple TrueType Reference Manual: <c>head</c> table</seealso>
public sealed record BhedTable : IFontTable<BhedTable>
{
    /// <summary>Gets the table tag <c>bhed</c>.</summary>
    /// <value>The four-byte tag <c>bhed</c>.</value>
    /// <remarks>The tag is lower case, matching the other Apple Advanced Typography table tags. See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bhed.html"><c>bhed</c> chapter</see> of Apple's TrueType Reference Manual.</remarks>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bhed.html">Apple TrueType Reference Manual: <c>bhed</c> table</seealso>
    public static Tag Tag => "bhed";

    /// <summary>Gets the table version as a 16.16 fixed-point value.</summary>
    /// <value>The raw <see cref="Fixed"/> bit pattern of the <c>version</c> field, normally <c>1.0</c> (<c>0x00010000</c>).</value>
    /// <remarks><see cref="MajorVersion"/> and <see cref="MinorVersion"/> expose the same value split into its two 16-bit halves.</remarks>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="MinorVersion"/>
    /// <seealso cref="Fixed"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bhed.html">Apple TrueType Reference Manual: <c>bhed</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the major half of <see cref="Version"/>.</summary>
    /// <value>The high 16 bits of the <c>version</c> field, normally <c>1</c>.</value>
    /// <seealso cref="Version"/>
    /// <seealso cref="MinorVersion"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor half of <see cref="Version"/>.</summary>
    /// <value>The low 16 bits of the <c>version</c> field, normally <c>0</c>.</value>
    /// <seealso cref="Version"/>
    /// <seealso cref="MajorVersion"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the font revision in 16.16 fixed-point format.</summary>
    /// <value>The value set by the font manufacturer.</value>
    /// <remarks>Mirrors <see cref="HeadTable.FontRevision"/>.</remarks>
    /// <seealso cref="Fixed"/>
    /// <seealso cref="HeadTable.FontRevision"/>
    public Fixed FontRevision { get; init; }

    /// <summary>Gets the checksum adjustment value.</summary>
    /// <value>The value that makes the checksum of the whole font equal <c>0xB1B0AFBA</c> when the field itself is treated as zero.</value>
    /// <remarks>Mirrors <see cref="HeadTable.ChecksumAdjustment"/>.</remarks>
    /// <seealso cref="HeadTable.ChecksumAdjustment"/>
    public uint ChecksumAdjustment { get; init; }

    /// <summary>Gets the magic number of the table.</summary>
    /// <value>Always equal to <see cref="HeadTable.Magic"/> (<c>0x5F0F3CF5</c>) in a conforming font.</value>
    /// <remarks>The parse rejects any other value with an <see cref="InvalidDataException"/>.</remarks>
    /// <seealso cref="HeadTable.Magic"/>
    public uint MagicNumber { get; init; }

    /// <summary>Gets the font header flags.</summary>
    /// <value>A combination of <see cref="HeadFlags"/> bits.</value>
    /// <remarks>Mirrors <see cref="HeadTable.Flags"/>; the flags carry the same meaning as in <c>head</c>.</remarks>
    /// <seealso cref="HeadFlags"/>
    /// <seealso cref="HeadTable.Flags"/>
    public HeadFlags Flags { get; init; }

    /// <summary>Gets the number of font design units per em.</summary>
    /// <value>The design-unit resolution of the font. Bitmap-only fonts frequently publish no meaningful value.</value>
    /// <remarks>Mirrors <see cref="HeadTable.UnitsPerEm"/>. Because a <c>bhed</c> font has no outlines, the value is not validated against the <c>head</c> range of 16 through 16384.</remarks>
    /// <seealso cref="HeadTable.UnitsPerEm"/>
    public ushort UnitsPerEm { get; init; }

    /// <summary>Gets the font creation timestamp.</summary>
    /// <value>The <c>created</c> field converted from the OpenType LONGDATETIME epoch (1904-01-01 00:00:00 UTC) to a UTC <see cref="DateTime"/>.</value>
    /// <seealso cref="Modified"/>
    /// <seealso cref="Cursor.OpenTypeEpochOffset"/>
    public DateTime Created { get; init; }

    /// <summary>Gets the font modification timestamp.</summary>
    /// <value>The <c>modified</c> field converted from the OpenType LONGDATETIME epoch (1904-01-01 00:00:00 UTC) to a UTC <see cref="DateTime"/>.</value>
    /// <seealso cref="Created"/>
    /// <seealso cref="Cursor.OpenTypeEpochOffset"/>
    public DateTime Modified { get; init; }

    /// <summary>Gets the minimum x coordinate of the font bounding box.</summary>
    /// <value>The smallest x coordinate, in pixels for a bitmap-only font.</value>
    /// <seealso cref="YMin"/>
    /// <seealso cref="XMax"/>
    public short XMin { get; init; }

    /// <summary>Gets the minimum y coordinate of the font bounding box.</summary>
    /// <value>The smallest y coordinate, in pixels for a bitmap-only font.</value>
    /// <seealso cref="XMin"/>
    /// <seealso cref="YMax"/>
    public short YMin { get; init; }

    /// <summary>Gets the maximum x coordinate of the font bounding box.</summary>
    /// <value>The largest x coordinate, in pixels for a bitmap-only font.</value>
    /// <seealso cref="XMin"/>
    /// <seealso cref="YMax"/>
    public short XMax { get; init; }

    /// <summary>Gets the maximum y coordinate of the font bounding box.</summary>
    /// <value>The largest y coordinate, in pixels for a bitmap-only font.</value>
    /// <seealso cref="XMax"/>
    /// <seealso cref="YMin"/>
    public short YMax { get; init; }

    /// <summary>Gets the Macintosh style flags.</summary>
    /// <value>A combination of <c>MacStyle</c> bits.</value>
    /// <remarks>Mirrors <see cref="HeadTable.MacStyle"/>.</remarks>
    /// <seealso cref="HeadTable.MacStyle"/>
    public MacStyle MacStyle { get; init; }

    /// <summary>Gets the smallest recommended readable size in pixels.</summary>
    /// <value>The minimum recommended pixels-per-em value.</value>
    /// <remarks>Mirrors <see cref="HeadTable.LowestRecPPEM"/>.</remarks>
    /// <seealso cref="HeadTable.LowestRecPPEM"/>
    public ushort LowestRecPPEM { get; init; }

    /// <summary>Gets the deprecated font direction hint.</summary>
    /// <value>The <c>fontDirectionHint</c> value; <see cref="FontDirectionHint.LikeLeftToRightWithNeutrals"/> for modern fonts.</value>
    /// <remarks>Mirrors <see cref="HeadTable.FontDirectionHint"/>.</remarks>
    /// <seealso cref="HeadTable.FontDirectionHint"/>
    public FontDirectionHint FontDirectionHint { get; init; }

    /// <summary>Gets the <c>loca</c> offset format.</summary>
    /// <value>The <c>indexToLocFormat</c> value.</value>
    /// <remarks>The field is carried for layout compatibility with <c>head</c>; a <c>bhed</c> font has no <c>loca</c> table, so the value has no effect.</remarks>
    /// <seealso cref="HeadTable.IndexToLocFormat"/>
    public IndexToLocFormat IndexToLocFormat { get; init; }

    /// <summary>Gets the glyph data format.</summary>
    /// <value>The <c>glyphDataFormat</c> value; zero for the current TrueType glyph data format.</value>
    /// <remarks>The field is carried for layout compatibility with <c>head</c>; a <c>bhed</c> font has no <c>glyf</c> table, so the value has no effect.</remarks>
    /// <seealso cref="HeadTable.GlyphDataFormat"/>
    public GlyphDataFormat GlyphDataFormat { get; init; }

    /// <summary>The 54-byte fixed-layout <c>bhed</c> table header, identical in shape to the <c>head</c> header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Every field is stored big-endian; all multi-byte fields are reversed by <see cref="ReverseEndianness"/> on little-endian hosts.</description></item>
    /// <item><description>The 32-bit <see cref="Version"/> field is stored as a <see cref="Fixed"/> value; the equivalent split into <see cref="BhedTable.MajorVersion"/> and <see cref="BhedTable.MinorVersion"/> is performed when the semantic record is built.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BhedTable"/>
    /// <seealso cref="HeadTable.Header"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bhed.html">Apple TrueType Reference Manual: <c>bhed</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version as a 16.16 fixed-point value at byte offset 0.</summary>
        /// <remarks>The initial version is <c>0x00010000</c> (1.0).</remarks>
        /// <seealso cref="BhedTable.Version"/>
        public Fixed Version;                // +0

        /// <summary>The font revision in 16.16 fixed-point format at byte offset 4.</summary>
        /// <seealso cref="BhedTable.FontRevision"/>
        public Fixed FontRevision;           // +4

        /// <summary>The checksum adjustment at byte offset 8.</summary>
        /// <seealso cref="BhedTable.ChecksumAdjustment"/>
        public uint ChecksumAdjustment;      // +8

        /// <summary>The required magic number at byte offset 12.</summary>
        /// <remarks>The value must be <see cref="HeadTable.Magic"/>.</remarks>
        /// <seealso cref="BhedTable.MagicNumber"/>
        /// <seealso cref="HeadTable.Magic"/>
        public uint MagicNumber;             // +12

        /// <summary>The font header flags at byte offset 16.</summary>
        /// <seealso cref="BhedTable.Flags"/>
        /// <seealso cref="HeadFlags"/>
        public ushort Flags;                 // +16

        /// <summary>The number of design units per em at byte offset 18.</summary>
        /// <seealso cref="BhedTable.UnitsPerEm"/>
        public ushort UnitsPerEm;            // +18

        /// <summary>The creation time as a LONGDATETIME at byte offset 20.</summary>
        /// <seealso cref="BhedTable.Created"/>
        public long Created;                 // +20

        /// <summary>The modification time as a LONGDATETIME at byte offset 28.</summary>
        /// <seealso cref="BhedTable.Modified"/>
        public long Modified;                // +28

        /// <summary>The minimum x coordinate of the font bounding box at byte offset 36.</summary>
        /// <seealso cref="BhedTable.XMin"/>
        public short XMin;                   // +36

        /// <summary>The minimum y coordinate of the font bounding box at byte offset 38.</summary>
        /// <seealso cref="BhedTable.YMin"/>
        public short YMin;                   // +38

        /// <summary>The maximum x coordinate of the font bounding box at byte offset 40.</summary>
        /// <seealso cref="BhedTable.XMax"/>
        public short XMax;                   // +40

        /// <summary>The maximum y coordinate of the font bounding box at byte offset 42.</summary>
        /// <seealso cref="BhedTable.YMax"/>
        public short YMax;                   // +42

        /// <summary>The Macintosh style flags at byte offset 44.</summary>
        /// <seealso cref="BhedTable.MacStyle"/>
        public ushort MacStyle;              // +44

        /// <summary>The smallest recommended readable size in pixels at byte offset 46.</summary>
        /// <seealso cref="BhedTable.LowestRecPPEM"/>
        public ushort LowestRecPPEM;         // +46

        /// <summary>The deprecated font direction hint at byte offset 48.</summary>
        /// <seealso cref="BhedTable.FontDirectionHint"/>
        public short FontDirectionHint;      // +48

        /// <summary>The <c>loca</c> offset format at byte offset 50.</summary>
        /// <seealso cref="BhedTable.IndexToLocFormat"/>
        public short IndexToLocFormat;       // +50

        /// <summary>The glyph data format at byte offset 52.</summary>
        /// <seealso cref="BhedTable.GlyphDataFormat"/>
        public short GlyphDataFormat;        // +52

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        /// <param name="v">The header whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>There are no byte-sized fields in the header, so every field is passed through <c>BinaryPrimitives.ReverseEndianness</c> or <see cref="Fixed.ReverseEndianness"/>.</description></item>
        /// <item><description>The reversal is unconditional; whether it is applied at all is decided by <see cref="Cursor.ReadBigEndianStruct{T}"/>.</description></item>
        /// </list>
        /// </remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = Fixed.ReverseEndianness(v.Version),
            FontRevision = Fixed.ReverseEndianness(v.FontRevision),
            ChecksumAdjustment = BinaryPrimitives.ReverseEndianness(v.ChecksumAdjustment),
            MagicNumber = BinaryPrimitives.ReverseEndianness(v.MagicNumber),
            Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
            UnitsPerEm = BinaryPrimitives.ReverseEndianness(v.UnitsPerEm),
            Created = BinaryPrimitives.ReverseEndianness(v.Created),
            Modified = BinaryPrimitives.ReverseEndianness(v.Modified),
            XMin = BinaryPrimitives.ReverseEndianness(v.XMin),
            YMin = BinaryPrimitives.ReverseEndianness(v.YMin),
            XMax = BinaryPrimitives.ReverseEndianness(v.XMax),
            YMax = BinaryPrimitives.ReverseEndianness(v.YMax),
            MacStyle = BinaryPrimitives.ReverseEndianness(v.MacStyle),
            LowestRecPPEM = BinaryPrimitives.ReverseEndianness(v.LowestRecPPEM),
            FontDirectionHint = BinaryPrimitives.ReverseEndianness(v.FontDirectionHint),
            IndexToLocFormat = BinaryPrimitives.ReverseEndianness(v.IndexToLocFormat),
            GlyphDataFormat = BinaryPrimitives.ReverseEndianness(v.GlyphDataFormat),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the <c>bhed</c> table.</param>
    /// <param name="context">Not read; the table is self-contained. The <see cref="FontFace"/> supplied by the table loader is ignored.</param>
    /// <returns>The parsed <c>bhed</c> table.</returns>
    /// <exception cref="EndOfStreamException">The 54-byte header extends past the end of <c>cursor.Source</c>.</exception>
    /// <exception cref="InvalidDataException">The <c>magicNumber</c> field is not <see cref="HeadTable.Magic"/>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is read with <see cref="Cursor.ReadBigEndianStruct{T}"/>, so its multi-byte fields are already in native byte order when the semantic record is built.</description></item>
    /// <item><description>The <c>created</c> and <c>modified</c> LONGDATETIME fields are converted from the 1904 epoch through <see cref="Cursor.OpenTypeEpochOffset"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="HeadTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bhed.html">Apple TrueType Reference Manual: <c>bhed</c> table</seealso>
    static BhedTable IRecord<BhedTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        // The magic number is a required constant; a mismatch means the bytes are not a bhed/head header.
        if (header.MagicNumber != HeadTable.Magic)
            throw new InvalidDataException(
                $"'bhed'.magicNumber is 0x{header.MagicNumber:X8}, expected 0x{HeadTable.Magic:X8}.");

        return new BhedTable
        {
            Version = header.Version,
            MajorVersion = (ushort)header.Version.IntegerPart,
            MinorVersion = header.Version.FractionPart,
            FontRevision = header.FontRevision,
            ChecksumAdjustment = header.ChecksumAdjustment,
            MagicNumber = header.MagicNumber,
            Flags = (HeadFlags)header.Flags,
            UnitsPerEm = header.UnitsPerEm,
            Created = DateTime.UnixEpoch.AddSeconds(header.Created - Cursor.OpenTypeEpochOffset),
            Modified = DateTime.UnixEpoch.AddSeconds(header.Modified - Cursor.OpenTypeEpochOffset),
            XMin = header.XMin,
            YMin = header.YMin,
            XMax = header.XMax,
            YMax = header.YMax,
            MacStyle = (MacStyle)header.MacStyle,
            LowestRecPPEM = header.LowestRecPPEM,
            FontDirectionHint = (FontDirectionHint)header.FontDirectionHint,
            IndexToLocFormat = (IndexToLocFormat)header.IndexToLocFormat,
            GlyphDataFormat = (GlyphDataFormat)header.GlyphDataFormat,
        };
    }
}
