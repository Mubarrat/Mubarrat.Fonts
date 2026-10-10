using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>Represents the OpenType <c>head</c> table containing global font information.</summary>
/// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head">The OpenType specification</see> defines the table as a 54-byte big-endian record containing global metrics, timestamps, flags, and glyph-data format information.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head">OpenType <c>head</c> table specification</seealso>
public sealed record HeadTable : IFontTable<HeadTable>, IEndianReversibleHeaderRecord<HeadTable, HeadTable.Header>
{
    /// <summary>The required value of the <c>magicNumber</c> field.</summary>
    /// <value><c>0x5F0F3CF5</c>.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#magicnumber">The specification requires this constant value.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#magicnumber">OpenType <c>magicNumber</c></seealso>
    public const uint Magic = 0x5F0F3CF5;

    /// <summary>The minimum valid value of <see cref="UnitsPerEm"/>.</summary>
    /// <value><c>16</c>.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#unitsperem">The OpenType specification defines a valid range of 16 through 16384.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#unitsperem">OpenType <c>unitsPerEm</c></seealso>
    public const ushort MinUnitsPerEm = 16;

    /// <summary>The maximum valid value of <see cref="UnitsPerEm"/>.</summary>
    /// <value><c>16384</c>.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#unitsperem">The OpenType specification defines a valid range of 16 through 16384.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#unitsperem">OpenType <c>unitsPerEm</c></seealso>
    public const ushort MaxUnitsPerEm = 16384;

    /// <summary>The number of seconds between the OpenType LONGDATETIME epoch and the Unix epoch.</summary>
    /// <value><c>2082844800</c> seconds.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#longdatetime">OpenType LONGDATETIME values are signed 64-bit seconds from 1904-01-01 00:00:00 UTC.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#longdatetime">OpenType LONGDATETIME</seealso>
    public const long OpenTypeEpochOffset = 2082844800L;

    /// <summary>Gets the OpenType table tag.</summary>
    /// <value>The tag <c>head</c>.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#table-directory">The tag identifies the table in the font's table directory.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head">OpenType <c>head</c> table specification</seealso>
    public static Tag Tag => "head";

    /// <summary>Gets the major version of the <c>head</c> table.</summary>
    /// <value>The required value is <c>1</c>.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#version">The <c>head</c> table version is encoded as separate major and minor unsigned 16-bit values.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#version">OpenType <c>head</c> version</seealso>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version of the <c>head</c> table.</summary>
    /// <value>The required value is <c>0</c>.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#version">The <c>head</c> table version is encoded as separate major and minor unsigned 16-bit values.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#version">OpenType <c>head</c> version</seealso>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the font revision in 16.16 fixed-point format.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#fontrevision">The value is set by the font manufacturer and is not used by Windows to determine the displayed font version.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#fontrevision">OpenType <c>fontRevision</c></seealso>
    public Fixed FontRevision { get; init; }

    /// <summary>Gets the checksum adjustment value.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#font-files">The value is calculated with this field set to zero so that the complete font checksum equals <c>0xB1B0AFBA</c>.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#font-files">OpenType font checksum</seealso>
    public uint ChecksumAdjustment { get; init; }

    /// <summary>Gets the required <c>head</c> table magic number.</summary>
    /// <value>The value must equal <see cref="Magic"/>.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#magicnumber">This field provides a constant value used to identify a valid <c>head</c> table.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#magicnumber">OpenType <c>magicNumber</c></seealso>
    public uint MagicNumber { get; init; }

    /// <summary>Gets the flags stored in the <c>flags</c> field.</summary>
    /// <value>A combination of <see cref="HeadFlags"/> values.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#flags">The flags describe properties of the font and its TrueType instructions.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#flags">OpenType <c>flags</c></seealso>
    public HeadFlags Flags { get; init; }

    /// <summary>Gets the number of font design units per em.</summary>
    /// <value>A value from <see cref="MinUnitsPerEm"/> through <see cref="MaxUnitsPerEm"/>, inclusive.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#unitsperem">A power of two is recommended for TrueType fonts because it can enable implementation optimizations.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#unitsperem">OpenType <c>unitsPerEm</c></seealso>
    public ushort UnitsPerEm { get; init; }

    /// <summary>Gets the font creation timestamp.</summary>
    /// <value>The timestamp converted from the OpenType LONGDATETIME representation to UTC <see cref="DateTime"/>.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#created">The stored value is measured in seconds from 1904-01-01 00:00:00 UTC.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#created">OpenType <c>created</c></seealso>
    public DateTime Created { get; init; }

    /// <summary>Gets the font modification timestamp.</summary>
    /// <value>The timestamp converted from the OpenType LONGDATETIME representation to UTC <see cref="DateTime"/>.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#modified">The stored value is measured in seconds from 1904-01-01 00:00:00 UTC.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#modified">OpenType <c>modified</c></seealso>
    public DateTime Modified { get; init; }

    /// <summary>Gets the minimum x coordinate of the font bounding box.</summary>
    /// <value>The minimum x coordinate in font design units.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#xmin">The value is the smallest x coordinate found in glyph bounding boxes.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#xmin">OpenType <c>xMin</c></seealso>
    public short XMin { get; init; }

    /// <summary>Gets the minimum y coordinate of the font bounding box.</summary>
    /// <value>The minimum y coordinate in font design units.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#ymin">The value is the smallest y coordinate found in glyph bounding boxes.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#ymin">OpenType <c>yMin</c></seealso>
    public short YMin { get; init; }

    /// <summary>Gets the maximum x coordinate of the font bounding box.</summary>
    /// <value>The maximum x coordinate in font design units.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#xmax">The value is the largest x coordinate found in glyph bounding boxes.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#xmax">OpenType <c>xMax</c></seealso>
    public short XMax { get; init; }

    /// <summary>Gets the maximum y coordinate of the font bounding box.</summary>
    /// <value>The maximum y coordinate in font design units.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#ymax">The value is the largest y coordinate found in glyph bounding boxes.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#ymax">OpenType <c>yMax</c></seealso>
    public short YMax { get; init; }

    /// <summary>Gets the Macintosh style flags.</summary>
    /// <value>A combination of <see cref="MacStyle"/> values.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#macstyle">The <c>macStyle</c> bits describe Macintosh style attributes and should agree with the corresponding <c>OS/2</c> selection information.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#macstyle">OpenType <c>macStyle</c></seealso>
    public MacStyle MacStyle { get; init; }

    /// <summary>Gets the smallest recommended readable size in pixels.</summary>
    /// <value>The minimum recommended pixels-per-em value.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#lowestrecppem">The value indicates the smallest recommended readable size for the font.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#lowestrecppem">OpenType <c>lowestRecPPEM</c></seealso>
    public ushort LowestRecPPEM { get; init; }

    /// <summary>Gets the deprecated font direction hint.</summary>
    /// <value>A <see cref="FontDirectionHint"/> value.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#fontdirectionhint">The field is deprecated and fonts should use <see cref="FontDirectionHint.LikeLeftToRightWithNeutrals"/>.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#fontdirectionhint">OpenType <c>fontDirectionHint</c></seealso>
    public FontDirectionHint FontDirectionHint { get; init; }

    /// <summary>Gets the offset format used by the <c>loca</c> table.</summary>
    /// <value><see cref="IndexToLocFormat.ShortOffsets"/> for short offsets or <see cref="IndexToLocFormat.LongOffsets"/> for long offsets.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#indextolocformat">The value determines whether <c>loca</c> entries are represented using 16-bit or 32-bit offsets.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#indextolocformat">OpenType <c>indexToLocFormat</c></seealso>
    public IndexToLocFormat IndexToLocFormat { get; init; }

    /// <summary>Gets the glyph data format.</summary>
    /// <value><see cref="GlyphDataFormat.Current"/> for the current TrueType glyph data format.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#glyphdataformat">The field is reserved for future use and must be zero for current TrueType outlines.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#glyphdataformat">OpenType <c>glyphDataFormat</c></seealso>
    public GlyphDataFormat GlyphDataFormat { get; init; }

    /// <summary>Gets a value indicating whether the <c>loca</c> table uses 32-bit offsets.</summary>
    /// <value><see langword="true"/> when <see cref="IndexToLocFormat"/> is <see cref="IndexToLocFormat.LongOffsets"/>; otherwise, <see langword="false"/>.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/loca">Long offsets are stored as unsigned 32-bit values in the <c>loca</c> table.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/loca">OpenType <c>loca</c> table</seealso>
    public bool UsesLongLoca => IndexToLocFormat == IndexToLocFormat.LongOffsets;

    /// <summary>Gets a value indicating whether the left sidebearing point is required to be at x = 0.</summary>
    /// <value><see langword="true"/> when <see cref="HeadFlags.LeftSidebearingAtX0"/> is set; otherwise, <see langword="false"/>.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#flags">When set, the first point of the left sidebearing is at x = 0 for TrueType rasterization.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#flags">OpenType <c>flags</c></seealso>
    public bool LeftSidebearingAtOrigin => (Flags & HeadFlags.LeftSidebearingAtX0) != 0;

    /// <inheritdoc/>
    static HeadTable IHeaderRecord<HeadTable, Header>.FromHeader(in Header header, object? context) => new()
    {
        MajorVersion = header.MajorVersion,
        MinorVersion = header.MinorVersion,
        FontRevision = header.FontRevision,
        ChecksumAdjustment = header.ChecksumAdjustment,
        MagicNumber = header.MagicNumber,
        Flags = (HeadFlags)header.Flags,
        UnitsPerEm = header.UnitsPerEm,
        Created = DateTime.UnixEpoch.AddSeconds(header.Created - OpenTypeEpochOffset),
        Modified = DateTime.UnixEpoch.AddSeconds(header.Modified - OpenTypeEpochOffset),
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

    /// <summary>Represents the 54-byte big-endian wire representation of the <c>head</c> table.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head">The fields are stored sequentially with no padding and follow the OpenType <c>head</c> table layout exactly.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head">OpenType <c>head</c> table specification</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Bytes 0-1: major version.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#version">The field contains the major version of the table.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#version">OpenType <c>version</c></seealso>
        public ushort MajorVersion;

        /// <summary>Bytes 2-3: minor version.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#version">The field contains the minor version of the table.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#version">OpenType <c>version</c></seealso>
        public ushort MinorVersion;

        /// <summary>Bytes 4-7: font revision in 16.16 fixed-point format.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#fontrevision">The raw field uses OpenType Fixed representation.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#fontrevision">OpenType <c>fontRevision</c></seealso>
        public Fixed FontRevision;

        /// <summary>Bytes 8-11: checksum adjustment.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#font-files">The field participates in the font checksum calculation.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#font-files">OpenType font checksum</seealso>
        public uint ChecksumAdjustment;

        /// <summary>Bytes 12-15: required magic number.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#magicnumber">The value must be <c>0x5F0F3CF5</c>.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#magicnumber">OpenType <c>magicNumber</c></seealso>
        public uint MagicNumber;

        /// <summary>Bytes 16-17: <c>head</c> table flags.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#flags">The field contains the bit flags defined by the <c>head</c> table.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#flags">OpenType <c>flags</c></seealso>
        public ushort Flags;

        /// <summary>Bytes 18-19: units per em.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#unitsperem">The field specifies the number of design units in one em.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#unitsperem">OpenType <c>unitsPerEm</c></seealso>
        public ushort UnitsPerEm;

        /// <summary>Bytes 20-27: creation time as LONGDATETIME.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#created">The value is measured in seconds from the OpenType epoch.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#created">OpenType <c>created</c></seealso>
        public long Created;

        /// <summary>Bytes 28-35: modification time as LONGDATETIME.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#modified">The value is measured in seconds from the OpenType epoch.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#modified">OpenType <c>modified</c></seealso>
        public long Modified;

        /// <summary>Bytes 36-37: minimum x coordinate.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#xmin">The field contains the minimum x coordinate of the font bounding box.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#xmin">OpenType <c>xMin</c></seealso>
        public short XMin;

        /// <summary>Bytes 38-39: minimum y coordinate.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#ymin">The field contains the minimum y coordinate of the font bounding box.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#ymin">OpenType <c>yMin</c></seealso>
        public short YMin;

        /// <summary>Bytes 40-41: maximum x coordinate.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#xmax">The field contains the maximum x coordinate of the font bounding box.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#xmax">OpenType <c>xMax</c></seealso>
        public short XMax;

        /// <summary>Bytes 42-43: maximum y coordinate.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#ymax">The field contains the maximum y coordinate of the font bounding box.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#ymax">OpenType <c>yMax</c></seealso>
        public short YMax;

        /// <summary>Bytes 44-45: Macintosh style flags.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#macstyle">The field contains the bit flags represented by <see cref="MacStyle"/>.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#macstyle">OpenType <c>macStyle</c></seealso>
        public ushort MacStyle;

        /// <summary>Bytes 46-47: smallest recommended readable size in pixels.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#lowestrecppem">The field specifies the smallest recommended pixels-per-em size.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#lowestrecppem">OpenType <c>lowestRecPPEM</c></seealso>
        public ushort LowestRecPPEM;

        /// <summary>Bytes 48-49: deprecated font direction hint.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#fontdirectionhint">The field contains one of the deprecated directional hint values.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#fontdirectionhint">OpenType <c>fontDirectionHint</c></seealso>
        public short FontDirectionHint;

        /// <summary>Bytes 50-51: <c>loca</c> offset format.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#indextolocformat">The field determines whether <c>loca</c> uses short or long offsets.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#indextolocformat">OpenType <c>indexToLocFormat</c></seealso>
        public short IndexToLocFormat;

        /// <summary>Bytes 52-53: glyph data format.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#glyphdataformat">The field must be zero for current TrueType glyph data.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#glyphdataformat">OpenType <c>glyphDataFormat</c></seealso>
        public short GlyphDataFormat;

        /// <summary>Reverses the byte order of all multi-byte fields in the record.</summary>
        /// <param name="value">The record whose fields are to be byte-swapped.</param>
        /// <returns>A record with every multi-byte field converted to the opposite byte order.</returns>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType scalar fields are stored in big-endian byte order.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType data types</seealso>
        public static Header ReverseEndianness(Header value) => new()
        {
            MajorVersion = BinaryPrimitives.ReverseEndianness(value.MajorVersion),
            MinorVersion = BinaryPrimitives.ReverseEndianness(value.MinorVersion),
            FontRevision = Fixed.ReverseEndianness(value.FontRevision),
            ChecksumAdjustment = BinaryPrimitives.ReverseEndianness(value.ChecksumAdjustment),
            MagicNumber = BinaryPrimitives.ReverseEndianness(value.MagicNumber),
            Flags = BinaryPrimitives.ReverseEndianness(value.Flags),
            UnitsPerEm = BinaryPrimitives.ReverseEndianness(value.UnitsPerEm),
            Created = BinaryPrimitives.ReverseEndianness(value.Created),
            Modified = BinaryPrimitives.ReverseEndianness(value.Modified),
            XMin = BinaryPrimitives.ReverseEndianness(value.XMin),
            YMin = BinaryPrimitives.ReverseEndianness(value.YMin),
            XMax = BinaryPrimitives.ReverseEndianness(value.XMax),
            YMax = BinaryPrimitives.ReverseEndianness(value.YMax),
            MacStyle = BinaryPrimitives.ReverseEndianness(value.MacStyle),
            LowestRecPPEM = BinaryPrimitives.ReverseEndianness(value.LowestRecPPEM),
            FontDirectionHint = BinaryPrimitives.ReverseEndianness(value.FontDirectionHint),
            IndexToLocFormat = BinaryPrimitives.ReverseEndianness(value.IndexToLocFormat),
            GlyphDataFormat = BinaryPrimitives.ReverseEndianness(value.GlyphDataFormat),
        };
    }
}

/// <summary>Represents the Macintosh style flags stored in the <c>head</c> table.</summary>
/// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#macstyle">The flags describe Macintosh style attributes.</see></remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#macstyle">OpenType <c>macStyle</c></seealso>
[Flags]
public enum MacStyle : ushort
{
    /// <summary>No style flags are set.</summary>
    None = 0,

    /// <summary>Bit 0: bold.</summary>
    Bold = 1 << 0,

    /// <summary>Bit 1: italic.</summary>
    Italic = 1 << 1,

    /// <summary>Bit 2: underline.</summary>
    Underline = 1 << 2,

    /// <summary>Bit 3: outline.</summary>
    Outline = 1 << 3,

    /// <summary>Bit 4: shadow.</summary>
    Shadow = 1 << 4,

    /// <summary>Bit 5: condensed.</summary>
    Condensed = 1 << 5,

    /// <summary>Bit 6: extended.</summary>
    Extended = 1 << 6,
}

/// <summary>Represents the deprecated <c>fontDirectionHint</c> values of the <c>head</c> table.</summary>
/// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#fontdirectionhint">The field is deprecated and fonts should use the value represented by <see cref="LikeLeftToRightWithNeutrals"/>.</see></remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#fontdirectionhint">OpenType <c>fontDirectionHint</c></seealso>
public enum FontDirectionHint : short
{
    /// <summary>Strongly right-to-left glyphs with neutral glyphs.</summary>
    LikeRightToLeftWithNeutrals = -2,

    /// <summary>Only strongly right-to-left glyphs.</summary>
    StrongRightToLeft = -1,

    /// <summary>Fully mixed directional glyphs.</summary>
    FullyMixed = 0,

    /// <summary>Only strongly left-to-right glyphs.</summary>
    StrongLeftToRight = 1,

    /// <summary>Strongly left-to-right glyphs with neutral glyphs.</summary>
    LikeLeftToRightWithNeutrals = 2,
}

/// <summary>Represents the bit flags stored in the <c>head</c> table's <c>flags</c> field.</summary>
/// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#flags">The flags describe baseline, sidebearing, instruction, optimization, and font-type properties.</see></remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/head#flags">OpenType <c>flags</c></seealso>
[Flags]
public enum HeadFlags : ushort
{
    /// <summary>No flags are set.</summary>
    None = 0,

    /// <summary>Bit 0: the font baseline is at y = 0.</summary>
    BaselineAtY0 = 1 << 0,

    /// <summary>Bit 1: the left sidebearing point is at x = 0.</summary>
    LeftSidebearingAtX0 = 1 << 1,

    /// <summary>Bit 2: instructions may depend on point size.</summary>
    InstructionsDependOnPointSize = 1 << 2,

    /// <summary>Bit 3: scaler computations should use integer pixels per em.</summary>
    ForceIntegerPPEM = 1 << 3,

    /// <summary>Bit 4: instructions may alter advance widths.</summary>
    InstructionsAlterAdvanceWidth = 1 << 4,

    /// <summary>Bit 5: Apple vertical layout.</summary>
    AppleVerticalLayout = 1 << 5,

    /// <summary>Bit 11: the font data is lossless after an optimizing transformation or compression.</summary>
    Lossless = 1 << 11,

    /// <summary>Bit 12: the font has been converted while preserving compatible metrics.</summary>
    Converted = 1 << 12,

    /// <summary>Bit 13: the font has been optimized for ClearType.</summary>
    ClearTypeOptimized = 1 << 13,

    /// <summary>Bit 14: the font is a Last Resort font.</summary>
    LastResort = 1 << 14,
}
