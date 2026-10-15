using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>Represents the Apple bitmap-only header table, <c>bhed</c>. Byte-for-byte identical to the OpenType <c>head</c> table; its presence signals that the font carries no outline tables and only embedded bitmaps.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The wire layout is identical to the OpenType <c>head</c> table, and the shared layout is declared once as <see cref="HeadTable.Header"/>. This type exposes the same eighteen semantic properties as <see cref="HeadTable"/>; the field-level documentation on those properties applies to both tables.</description></item>
/// <item><description>macOS uses the presence of a <c>bhed</c> table as a flag that the font carries no glyph outlines and only embedded bitmaps. A font with a <c>bhed</c> table must not contain a <c>head</c> or <c>glyf</c> table, and must contain <c>bdat</c> and <c>bloc</c> tables instead.</description></item>
/// <item><description>If a font does have outlines, it must not contain a <c>bhed</c> table. The two header tables are mutually exclusive.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bhed.html">bitmap-only font header table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="HeadTable"/>
/// <seealso cref="BdatTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bhed.html">TrueType Reference Manual: The <c>bhed</c> table</seealso>
public sealed record BhedTable : IFontTable<BhedTable>, IEndianReversibleHeaderRecord<BhedTable, HeadTable.Header>
{
    /// <summary>Gets the Apple bitmap-only header tag.</summary>
    /// <value>The tag <c>bhed</c>.</value>
    /// <remarks>The tag identifies the table in the font's table directory and distinguishes it from the <c>head</c> table that <see cref="HeadTable"/> uses.</remarks>
    /// <seealso cref="HeadTable.Tag"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bhed.html">TrueType Reference Manual: The <c>bhed</c> table</seealso>
    public static Tag Tag => "bhed";

    /// <summary>Gets the major version of the <c>bhed</c> table.</summary>
    /// <value>The required value is <c>1</c>.</value>
    /// <remarks>The field is shared with <see cref="HeadTable.MajorVersion"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="HeadTable.MajorVersion"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version of the <c>bhed</c> table.</summary>
    /// <value>The required value is <c>0</c>.</value>
    /// <remarks>The field is shared with <see cref="HeadTable.MinorVersion"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="HeadTable.MinorVersion"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the font revision in 16.16 fixed-point format.</summary>
    /// <remarks>The field is shared with <see cref="HeadTable.FontRevision"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="HeadTable.FontRevision"/>
    public Fixed FontRevision { get; init; }

    /// <summary>Gets the checksum adjustment value.</summary>
    /// <remarks>The field is shared with <see cref="HeadTable.ChecksumAdjustment"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="HeadTable.ChecksumAdjustment"/>
    public uint ChecksumAdjustment { get; init; }

    /// <summary>Gets the required <c>head</c> table magic number.</summary>
    /// <value>The value must equal <see cref="HeadTable.Magic"/>.</value>
    /// <remarks>The field is shared with <see cref="HeadTable.MagicNumber"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="HeadTable.MagicNumber"/>
    /// <seealso cref="HeadTable.Magic"/>
    public uint MagicNumber { get; init; }

    /// <summary>Gets the flags stored in the <c>flags</c> field.</summary>
    /// <value>A combination of <see cref="HeadFlags"/> values.</value>
    /// <remarks>The field is shared with <see cref="HeadTable.Flags"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="HeadFlags"/>
    /// <seealso cref="HeadTable.Flags"/>
    public HeadFlags Flags { get; init; }

    /// <summary>Gets the number of font design units per em.</summary>
    /// <value>A value from <see cref="HeadTable.MinUnitsPerEm"/> through <see cref="HeadTable.MaxUnitsPerEm"/>, inclusive.</value>
    /// <remarks>The field is shared with <see cref="HeadTable.UnitsPerEm"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="HeadTable.UnitsPerEm"/>
    public ushort UnitsPerEm { get; init; }

    /// <summary>Gets the font creation timestamp.</summary>
    /// <value>The timestamp converted from the LONGDATETIME representation to UTC <see cref="DateTime"/>.</value>
    /// <remarks>The field is shared with <see cref="HeadTable.Created"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="HeadTable.Created"/>
    public DateTime Created { get; init; }

    /// <summary>Gets the font modification timestamp.</summary>
    /// <value>The timestamp converted from the LONGDATETIME representation to UTC <see cref="DateTime"/>.</value>
    /// <remarks>The field is shared with <see cref="HeadTable.Modified"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="HeadTable.Modified"/>
    public DateTime Modified { get; init; }

    /// <summary>Gets the minimum x coordinate of the font bounding box.</summary>
    /// <remarks>The field is shared with <see cref="HeadTable.XMin"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="HeadTable.XMin"/>
    public short XMin { get; init; }

    /// <summary>Gets the minimum y coordinate of the font bounding box.</summary>
    /// <remarks>The field is shared with <see cref="HeadTable.YMin"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="HeadTable.YMin"/>
    public short YMin { get; init; }

    /// <summary>Gets the maximum x coordinate of the font bounding box.</summary>
    /// <remarks>The field is shared with <see cref="HeadTable.XMax"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="HeadTable.XMax"/>
    public short XMax { get; init; }

    /// <summary>Gets the maximum y coordinate of the font bounding box.</summary>
    /// <remarks>The field is shared with <see cref="HeadTable.YMax"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="HeadTable.YMax"/>
    public short YMax { get; init; }

    /// <summary>Gets the Macintosh style flags.</summary>
    /// <value>A combination of <see cref="MacStyle"/> values.</value>
    /// <remarks>The field is shared with <see cref="HeadTable.MacStyle"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="MacStyle"/>
    /// <seealso cref="HeadTable.MacStyle"/>
    public MacStyle MacStyle { get; init; }

    /// <summary>Gets the smallest recommended readable size in pixels.</summary>
    /// <remarks>The field is shared with <see cref="HeadTable.LowestRecPPEM"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="HeadTable.LowestRecPPEM"/>
    public ushort LowestRecPPEM { get; init; }

    /// <summary>Gets the deprecated font direction hint.</summary>
    /// <value>A <see cref="FontDirectionHint"/> value.</value>
    /// <remarks>The field is shared with <see cref="HeadTable.FontDirectionHint"/>; see that property for the specification reference. The Apple documentation notes that bitmap-only fonts are not expected to set this field to any meaningful value.</remarks>
    /// <seealso cref="FontDirectionHint"/>
    /// <seealso cref="HeadTable.FontDirectionHint"/>
    public FontDirectionHint FontDirectionHint { get; init; }

    /// <summary>Gets the offset format used by the <c>loca</c> table.</summary>
    /// <value><see cref="IndexToLocFormat.ShortOffsets"/> or <see cref="IndexToLocFormat.LongOffsets"/>.</value>
    /// <remarks>The field is shared with <see cref="HeadTable.IndexToLocFormat"/>; see that property for the specification reference. A bitmap-only font has no <c>loca</c> table, so the value is inert but still present in the wire layout.</remarks>
    /// <seealso cref="IndexToLocFormat"/>
    /// <seealso cref="HeadTable.IndexToLocFormat"/>
    public IndexToLocFormat IndexToLocFormat { get; init; }

    /// <summary>Gets the glyph data format.</summary>
    /// <value><see cref="GlyphDataFormat.Current"/> for the only defined value.</value>
    /// <remarks>The field is shared with <see cref="HeadTable.GlyphDataFormat"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="GlyphDataFormat"/>
    /// <seealso cref="HeadTable.GlyphDataFormat"/>
    public GlyphDataFormat GlyphDataFormat { get; init; }

    /// <summary>Gets a value indicating whether the <c>loca</c> table uses 32-bit offsets.</summary>
    /// <value><see langword="true"/> when <see cref="IndexToLocFormat"/> is <see cref="IndexToLocFormat.LongOffsets"/>; otherwise, <see langword="false"/>.</value>
    /// <remarks>A bitmap-only font has no <c>loca</c> table, so the value is informational only.</remarks>
    /// <seealso cref="HeadTable.UsesLongLoca"/>
    public bool UsesLongLoca => IndexToLocFormat == IndexToLocFormat.LongOffsets;

    /// <summary>Gets a value indicating whether the left sidebearing point is required to be at x = 0.</summary>
    /// <value><see langword="true"/> when <see cref="HeadFlags.LeftSidebearingAtX0"/> is set; otherwise, <see langword="false"/>.</value>
    /// <remarks>The field is shared with <see cref="HeadTable.LeftSidebearingAtOrigin"/>; see that property for the specification reference.</remarks>
    /// <seealso cref="HeadFlags"/>
    /// <seealso cref="HeadTable.LeftSidebearingAtOrigin"/>
    public bool LeftSidebearingAtOrigin => (Flags & HeadFlags.LeftSidebearingAtX0) != 0;

    /// <inheritdoc/>
    /// <param name="header">The already-read <c>head</c>-layout header.</param>
    /// <param name="context">Ignored; the header is self-contained.</param>
    /// <returns>A new <see cref="BhedTable"/> populated from the header fields.</returns>
    /// <remarks>The shared header struct is declared by <see cref="HeadTable.Header"/> and carries the same field set for both tables, because the two tables are byte-for-byte identical on disk.</remarks>
    static BhedTable IHeaderRecord<BhedTable, HeadTable.Header>.FromHeader(in HeadTable.Header header, object? context) => new()
    {
        MajorVersion = header.MajorVersion,
        MinorVersion = header.MinorVersion,
        FontRevision = header.FontRevision,
        ChecksumAdjustment = header.ChecksumAdjustment,
        MagicNumber = header.MagicNumber,
        Flags = (HeadFlags)header.Flags,
        UnitsPerEm = header.UnitsPerEm,
        Created = DateTime.UnixEpoch.AddSeconds(header.Created - HeadTable.OpenTypeEpochOffset),
        Modified = DateTime.UnixEpoch.AddSeconds(header.Modified - HeadTable.OpenTypeEpochOffset),
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
