using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>OS/2</c> table: metrics and other data required for OpenType fonts on Windows and OS/2. Six versions (0–5) have been defined. Fields added in later versions are <c>null</c> when the table's version predates them.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><see cref="UsWeightClass"/> and <see cref="UsWidthClass"/> are shared with the <c>fvar</c> <c>wght</c>/<c>wdth</c> axes and the <c>STAT</c> table.</description></item>
/// <item><description><see cref="FsSelection"/> must agree with <c>head.macStyle</c>; the <c>fsSelection</c> bits take precedence on Windows.</description></item>
/// <item><description><see cref="STypoAscender"/>, <see cref="STypoDescender"/>, and <see cref="STypoLineGap"/> are the preferred metrics when <see cref="FsSelection"/> has <see cref="FsSelection.UseTypoMetrics"/> set; otherwise Windows uses <see cref="UsWinAscent"/> and <see cref="UsWinDescent"/> for line layout.</description></item>
/// <item><description>The version 0 header is 78 bytes and is present in every OS/2 table. Versions 1, 2, and 5 append fixed-size trailing blocks that are read conditionally. Legacy version 0 tables that label themselves as a higher version but truncate their trailing blocks are tolerated: the missing fields stay <c>null</c>.</description></item>
/// </list>
/// <para>For the table layout, see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2">the <c>OS/2</c> table</see> in the OpenType specification.</para>
/// </remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2">OpenType specification: <c>OS/2</c> table</seealso>
/// <seealso cref="WeightClass"/>
/// <seealso cref="WidthClass"/>
/// <seealso cref="FsType"/>
/// <seealso cref="FsSelection"/>
/// <seealso cref="Panose"/>
public sealed record Os2Table : IFontTable<Os2Table>
{
    /// <inheritdoc/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2">OpenType specification: <c>OS/2</c> table</seealso>
    public static Tag Tag => "OS/2";

    /// <summary>Gets the table version, 0 through 5.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#version"><c>version</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#version">OpenType specification: <c>OS/2</c>, <c>version</c></seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the average weighted escapement of all non-zero-width glyphs.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#xavgcharwidth"><c>xAvgCharWidth</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#xavgcharwidth">OpenType specification: <c>OS/2</c>, <c>xAvgCharWidth</c></seealso>
    public short XAvgCharWidth { get; init; }

    /// <summary>Gets the weight class (1–1000).</summary>
    /// <value>See <see cref="WeightClass"/> for the conventionally named stops.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usweightclass"><c>usWeightClass</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="WeightClass"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usweightclass">OpenType specification: <c>OS/2</c>, <c>usWeightClass</c></seealso>
    public WeightClass UsWeightClass { get; init; }

    /// <summary>Gets the width class (1–9).</summary>
    /// <value>See <see cref="WidthClass"/> for the named stops.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswidthclass"><c>usWidthClass</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="WidthClass"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswidthclass">OpenType specification: <c>OS/2</c>, <c>usWidthClass</c></seealso>
    public WidthClass UsWidthClass { get; init; }

    /// <summary>Gets the embedding licensing flags.</summary>
    /// <value>See <see cref="Tables.FsType"/> for the individual bits.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fstype"><c>fsType</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="FsType"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fstype">OpenType specification: <c>OS/2</c>, <c>fsType</c></seealso>
    public FsType FsType { get; init; }

    /// <summary>Gets the recommended horizontal size of subscript glyphs, in font design units.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptxsize"><c>ySubscriptXSize</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptxsize">OpenType specification: <c>OS/2</c>, <c>ySubscriptXSize</c></seealso>
    public short YSubscriptXSize { get; init; }

    /// <summary>Gets the recommended vertical size of subscript glyphs, in font design units.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptysize"><c>ySubscriptYSize</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptysize">OpenType specification: <c>OS/2</c>, <c>ySubscriptYSize</c></seealso>
    public short YSubscriptYSize { get; init; }

    /// <summary>Gets the recommended horizontal offset for subscript glyphs, in font design units.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptxoffset"><c>ySubscriptXOffset</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptxoffset">OpenType specification: <c>OS/2</c>, <c>ySubscriptXOffset</c></seealso>
    public short YSubscriptXOffset { get; init; }

    /// <summary>Gets the recommended vertical offset for subscript glyphs, in font design units.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptyoffset"><c>ySubscriptYOffset</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptyoffset">OpenType specification: <c>OS/2</c>, <c>ySubscriptYOffset</c></seealso>
    public short YSubscriptYOffset { get; init; }

    /// <summary>Gets the recommended horizontal size of superscript glyphs, in font design units.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptxsize"><c>ySuperscriptXSize</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptxsize">OpenType specification: <c>OS/2</c>, <c>ySuperscriptXSize</c></seealso>
    public short YSuperscriptXSize { get; init; }

    /// <summary>Gets the recommended vertical size of superscript glyphs, in font design units.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptysize"><c>ySuperscriptYSize</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptysize">OpenType specification: <c>OS/2</c>, <c>ySuperscriptYSize</c></seealso>
    public short YSuperscriptYSize { get; init; }

    /// <summary>Gets the recommended horizontal offset for superscript glyphs, in font design units.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptxoffset"><c>ySuperscriptXOffset</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptxoffset">OpenType specification: <c>OS/2</c>, <c>ySuperscriptXOffset</c></seealso>
    public short YSuperscriptXOffset { get; init; }

    /// <summary>Gets the recommended vertical offset for superscript glyphs, in font design units.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptyoffset"><c>ySuperscriptYOffset</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptyoffset">OpenType specification: <c>OS/2</c>, <c>ySuperscriptYOffset</c></seealso>
    public short YSuperscriptYOffset { get; init; }

    /// <summary>Gets the width of the strikeout stroke, in font design units.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ystrikeoutsize"><c>yStrikeoutSize</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ystrikeoutsize">OpenType specification: <c>OS/2</c>, <c>yStrikeoutSize</c></seealso>
    public short YStrikeoutSize { get; init; }

    /// <summary>Gets the position of the top of the strikeout stroke relative to the baseline.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ystrikeoutposition"><c>yStrikeoutPosition</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ystrikeoutposition">OpenType specification: <c>OS/2</c>, <c>yStrikeoutPosition</c></seealso>
    public short YStrikeoutPosition { get; init; }

    /// <summary>Gets the IBM/Microsoft family class.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#sfamilyclass"><c>sFamilyClass</c> field</see> in the OpenType specification for the class/subclass encoding.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#sfamilyclass">OpenType specification: <c>OS/2</c>, <c>sFamilyClass</c></seealso>
    public short SFamilyClass { get; init; }

    /// <summary>Gets the PANOSE classification (10 bytes).</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#panose"><c>panose</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Tables.Panose"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#panose">OpenType specification: <c>OS/2</c>, <c>panose</c></seealso>
    public Panose Panose { get; init; }

    /// <summary>Gets Unicode range bits 0–31.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange1"><c>ulUnicodeRange1</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange1">OpenType specification: <c>OS/2</c>, <c>ulUnicodeRange1</c></seealso>
    public uint UlUnicodeRange1 { get; init; }

    /// <summary>Gets Unicode range bits 32–63.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange2"><c>ulUnicodeRange2</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange2">OpenType specification: <c>OS/2</c>, <c>ulUnicodeRange2</c></seealso>
    public uint UlUnicodeRange2 { get; init; }

    /// <summary>Gets Unicode range bits 64–95.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange3"><c>ulUnicodeRange3</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange3">OpenType specification: <c>OS/2</c>, <c>ulUnicodeRange3</c></seealso>
    public uint UlUnicodeRange3 { get; init; }

    /// <summary>Gets Unicode range bits 96–127.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange4"><c>ulUnicodeRange4</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange4">OpenType specification: <c>OS/2</c>, <c>ulUnicodeRange4</c></seealso>
    public uint UlUnicodeRange4 { get; init; }

    /// <summary>Gets the four-character vendor ID tag.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#achvendid"><c>achVendID</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#achvendid">OpenType specification: <c>OS/2</c>, <c>achVendID</c></seealso>
    public Tag AchVendId { get; init; }

    /// <summary>Gets the style and usage bits.</summary>
    /// <value>See <see cref="Tables.FsSelection"/> for the individual bits.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fsselection"><c>fsSelection</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="FsSelection"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fsselection">OpenType specification: <c>OS/2</c>, <c>fsSelection</c></seealso>
    public FsSelection FsSelection { get; init; }

    /// <summary>Gets the minimum Unicode value in the font's <c>cmap</c>.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usfirstcharindex"><c>usFirstCharIndex</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usfirstcharindex">OpenType specification: <c>OS/2</c>, <c>usFirstCharIndex</c></seealso>
    public ushort UsFirstCharIndex { get; init; }

    /// <summary>Gets the maximum Unicode value in the font's <c>cmap</c>.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uslastcharindex"><c>usLastCharIndex</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uslastcharindex">OpenType specification: <c>OS/2</c>, <c>usLastCharIndex</c></seealso>
    public ushort UsLastCharIndex { get; init; }

    /// <summary>Gets the preferred ascender for typographic layout.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#stypoascender"><c>sTypoAscender</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="UseTypoMetrics"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#stypoascender">OpenType specification: <c>OS/2</c>, <c>sTypoAscender</c></seealso>
    public short STypoAscender { get; init; }

    /// <summary>Gets the preferred descender for typographic layout.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#stypodescender"><c>sTypoDescender</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="UseTypoMetrics"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#stypodescender">OpenType specification: <c>OS/2</c>, <c>sTypoDescender</c></seealso>
    public short STypoDescender { get; init; }

    /// <summary>Gets the preferred line gap for typographic layout.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#stypolinegap"><c>sTypoLineGap</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="UseTypoMetrics"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#stypolinegap">OpenType specification: <c>OS/2</c>, <c>sTypoLineGap</c></seealso>
    public short STypoLineGap { get; init; }

    /// <summary>Gets the Windows ascender metric (clipping boundary).</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswinascent"><c>usWinAscent</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="UseTypoMetrics"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswinascent">OpenType specification: <c>OS/2</c>, <c>usWinAscent</c></seealso>
    public ushort UsWinAscent { get; init; }

    /// <summary>Gets the Windows descender metric (clipping boundary).</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswindescent"><c>usWinDescent</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="UseTypoMetrics"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswindescent">OpenType specification: <c>OS/2</c>, <c>usWinDescent</c></seealso>
    public ushort UsWinDescent { get; init; }

    /// <summary>Gets the code page range bits 0–31, or <c>null</c> for version 0 or when the trailing block is absent.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulcodepagerange1"><c>ulCodePageRange1</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulcodepagerange1">OpenType specification: <c>OS/2</c>, <c>ulCodePageRange1</c></seealso>
    public uint? UlCodePageRange1 { get; init; }

    /// <summary>Gets the code page range bits 32–63, or <c>null</c> for version 0 or when the trailing block is absent.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulcodepagerange2"><c>ulCodePageRange2</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulcodepagerange2">OpenType specification: <c>OS/2</c>, <c>ulCodePageRange2</c></seealso>
    public uint? UlCodePageRange2 { get; init; }

    /// <summary>Gets the x-height in font design units, or <c>null</c> for versions 0–1 or when the trailing block is absent.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#sxheight"><c>sxHeight</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#sxheight">OpenType specification: <c>OS/2</c>, <c>sxHeight</c></seealso>
    public short? SxHeight { get; init; }

    /// <summary>Gets the cap height in font design units, or <c>null</c> for versions 0–1 or when the trailing block is absent.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#scapheight"><c>sCapHeight</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#scapheight">OpenType specification: <c>OS/2</c>, <c>sCapHeight</c></seealso>
    public short? SCapHeight { get; init; }

    /// <summary>Gets the default character for the font, or <c>null</c> for versions 0–1 or when the trailing block is absent.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usdefaultchar"><c>usDefaultChar</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usdefaultchar">OpenType specification: <c>OS/2</c>, <c>usDefaultChar</c></seealso>
    public ushort? UsDefaultChar { get; init; }

    /// <summary>Gets the break character for the font, or <c>null</c> for versions 0–1 or when the trailing block is absent.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usbreakchar"><c>usBreakChar</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usbreakchar">OpenType specification: <c>OS/2</c>, <c>usBreakChar</c></seealso>
    public ushort? UsBreakChar { get; init; }

    /// <summary>Gets the maximum context length for layout lookups, or <c>null</c> for versions 0–1 or when the trailing block is absent.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usmaxcontext"><c>usMaxContext</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usmaxcontext">OpenType specification: <c>OS/2</c>, <c>usMaxContext</c></seealso>
    public ushort? UsMaxContext { get; init; }

    /// <summary>Gets the lower optical size in twentieths of a point, or <c>null</c> for versions 0–4 or when the trailing block is absent.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usloweropticalpointsize"><c>usLowerOpticalPointSize</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usloweropticalpointsize">OpenType specification: <c>OS/2</c>, <c>usLowerOpticalPointSize</c></seealso>
    public ushort? UsLowerOpticalPointSize { get; init; }

    /// <summary>Gets the upper optical size in twentieths of a point, or <c>null</c> for versions 0–4 or when the trailing block is absent.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usupperopticalpointsize"><c>usUpperOpticalPointSize</c> field</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usupperopticalpointsize">OpenType specification: <c>OS/2</c>, <c>usUpperOpticalPointSize</c></seealso>
    public ushort? UsUpperOpticalPointSize { get; init; }

    /// <summary>Gets a value indicating whether <see cref="FsSelection"/> requests typographic metrics for line layout.</summary>
    /// <value><see langword="true"/> when <see cref="FsSelection.UseTypoMetrics"/> is set.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fsselection">USE_TYPO_METRICS bit</see> in the OpenType specification.</remarks>
    /// <seealso cref="FsSelection"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fsselection">OpenType specification: <c>OS/2</c>, <c>fsSelection</c></seealso>
    public bool UseTypoMetrics => (FsSelection & FsSelection.UseTypoMetrics) != 0;

    /// <summary>Gets a value indicating whether <see cref="FsSelection"/> marks the font as oblique rather than italic.</summary>
    /// <value><see langword="true"/> when <see cref="FsSelection.Oblique"/> is set.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fsselection">OBLIQUE bit</see> in the OpenType specification.</remarks>
    /// <seealso cref="FsSelection"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fsselection">OpenType specification: <c>OS/2</c>, <c>fsSelection</c></seealso>
    public bool IsOblique => (FsSelection & FsSelection.Oblique) != 0;

    /// <inheritdoc/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2">OpenType specification: <c>OS/2</c> table</seealso>
    static Os2Table IRecord<Os2Table>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version > 5)
            throw new InvalidDataException($"'OS/2'.version is {header.Version}, expected 0 through 5.");

        // Version 1+ appends code page ranges. The guard against Remaining tolerates legacy
        // version 0 tables that truncate after usWinDescent but still claim a higher version.
        Header1? h1 = null;
        if (header.Version >= 1)
            h1 = cursor.ReadBigEndianStruct<Header1>();

        // Version 2+ appends vertical metrics and the maximum layout context length.
        Header2? h2 = null;
        if (header.Version >= 2)
            h2 = cursor.ReadBigEndianStruct<Header2>();

        // Version 5 appends optical size bounds.
        Header5? h5 = null;
        if (header.Version >= 5)
            h5 = cursor.ReadBigEndianStruct<Header5>();

        return new Os2Table
        {
            Version = header.Version,
            XAvgCharWidth = header.XAvgCharWidth,
            UsWeightClass = (WeightClass)header.UsWeightClass,
            UsWidthClass = (WidthClass)header.UsWidthClass,
            FsType = (FsType)header.FsType,
            YSubscriptXSize = header.YSubscriptXSize,
            YSubscriptYSize = header.YSubscriptYSize,
            YSubscriptXOffset = header.YSubscriptXOffset,
            YSubscriptYOffset = header.YSubscriptYOffset,
            YSuperscriptXSize = header.YSuperscriptXSize,
            YSuperscriptYSize = header.YSuperscriptYSize,
            YSuperscriptXOffset = header.YSuperscriptXOffset,
            YSuperscriptYOffset = header.YSuperscriptYOffset,
            YStrikeoutSize = header.YStrikeoutSize,
            YStrikeoutPosition = header.YStrikeoutPosition,
            SFamilyClass = header.SFamilyClass,
            Panose = header.Panose,
            UlUnicodeRange1 = header.UlUnicodeRange1,
            UlUnicodeRange2 = header.UlUnicodeRange2,
            UlUnicodeRange3 = header.UlUnicodeRange3,
            UlUnicodeRange4 = header.UlUnicodeRange4,
            AchVendId = new Tag(header.AchVendId),
            FsSelection = (FsSelection)header.FsSelection,
            UsFirstCharIndex = header.UsFirstCharIndex,
            UsLastCharIndex = header.UsLastCharIndex,
            STypoAscender = header.STypoAscender,
            STypoDescender = header.STypoDescender,
            STypoLineGap = header.STypoLineGap,
            UsWinAscent = header.UsWinAscent,
            UsWinDescent = header.UsWinDescent,
            UlCodePageRange1 = h1?.UlCodePageRange1,
            UlCodePageRange2 = h1?.UlCodePageRange2,
            SxHeight = h2?.SxHeight,
            SCapHeight = h2?.SCapHeight,
            UsDefaultChar = h2?.UsDefaultChar,
            UsBreakChar = h2?.UsBreakChar,
            UsMaxContext = h2?.UsMaxContext,
            UsLowerOpticalPointSize = h5?.UsLowerOpticalPointSize,
            UsUpperOpticalPointSize = h5?.UsUpperOpticalPointSize,
        };
    }

    /// <summary>The 78-byte version 0 header, present in every <c>OS/2</c> table. Blittable, no padding.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2">version 0 header layout</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2">OpenType specification: <c>OS/2</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version, 0 through 5.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#version"><c>version</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#version">OpenType specification: <c>OS/2</c>, <c>version</c></seealso>
        public ushort Version;

        /// <summary>The average weighted escapement.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#xavgcharwidth"><c>xAvgCharWidth</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#xavgcharwidth">OpenType specification: <c>OS/2</c>, <c>xAvgCharWidth</c></seealso>
        public short XAvgCharWidth;

        /// <summary>The weight class (1–1000).</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usweightclass"><c>usWeightClass</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usweightclass">OpenType specification: <c>OS/2</c>, <c>usWeightClass</c></seealso>
        public ushort UsWeightClass;

        /// <summary>The width class (1–9).</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswidthclass"><c>usWidthClass</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswidthclass">OpenType specification: <c>OS/2</c>, <c>usWidthClass</c></seealso>
        public ushort UsWidthClass;

        /// <summary>The embedding licensing flags.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fstype"><c>fsType</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fstype">OpenType specification: <c>OS/2</c>, <c>fsType</c></seealso>
        public ushort FsType;

        /// <summary>The recommended horizontal size of subscript glyphs.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptxsize"><c>ySubscriptXSize</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptxsize">OpenType specification: <c>OS/2</c>, <c>ySubscriptXSize</c></seealso>
        public short YSubscriptXSize;

        /// <summary>The recommended vertical size of subscript glyphs.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptysize"><c>ySubscriptYSize</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptysize">OpenType specification: <c>OS/2</c>, <c>ySubscriptYSize</c></seealso>
        public short YSubscriptYSize;

        /// <summary>The recommended horizontal offset for subscript glyphs.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptxoffset"><c>ySubscriptXOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptxoffset">OpenType specification: <c>OS/2</c>, <c>ySubscriptXOffset</c></seealso>
        public short YSubscriptXOffset;

        /// <summary>The recommended vertical offset for subscript glyphs.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptyoffset"><c>ySubscriptYOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysubscriptyoffset">OpenType specification: <c>OS/2</c>, <c>ySubscriptYOffset</c></seealso>
        public short YSubscriptYOffset;

        /// <summary>The recommended horizontal size of superscript glyphs.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptxsize"><c>ySuperscriptXSize</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptxsize">OpenType specification: <c>OS/2</c>, <c>ySuperscriptXSize</c></seealso>
        public short YSuperscriptXSize;

        /// <summary>The recommended vertical size of superscript glyphs.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptysize"><c>ySuperscriptYSize</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptysize">OpenType specification: <c>OS/2</c>, <c>ySuperscriptYSize</c></seealso>
        public short YSuperscriptYSize;

        /// <summary>The recommended horizontal offset for superscript glyphs.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptxoffset"><c>ySuperscriptXOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptxoffset">OpenType specification: <c>OS/2</c>, <c>ySuperscriptXOffset</c></seealso>
        public short YSuperscriptXOffset;

        /// <summary>The recommended vertical offset for superscript glyphs.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptyoffset"><c>ySuperscriptYOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ysuperscriptyoffset">OpenType specification: <c>OS/2</c>, <c>ySuperscriptYOffset</c></seealso>
        public short YSuperscriptYOffset;

        /// <summary>The width of the strikeout stroke.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ystrikeoutsize"><c>yStrikeoutSize</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ystrikeoutsize">OpenType specification: <c>OS/2</c>, <c>yStrikeoutSize</c></seealso>
        public short YStrikeoutSize;

        /// <summary>The position of the top of the strikeout stroke relative to the baseline.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ystrikeoutposition"><c>yStrikeoutPosition</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ystrikeoutposition">OpenType specification: <c>OS/2</c>, <c>yStrikeoutPosition</c></seealso>
        public short YStrikeoutPosition;

        /// <summary>The IBM/Microsoft family class.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#sfamilyclass"><c>sFamilyClass</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#sfamilyclass">OpenType specification: <c>OS/2</c>, <c>sFamilyClass</c></seealso>
        public short SFamilyClass;

        /// <summary>The PANOSE classification (10 bytes).</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#panose"><c>panose</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#panose">OpenType specification: <c>OS/2</c>, <c>panose</c></seealso>
        public Panose Panose;

        /// <summary>Unicode range bits 0–31.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange1"><c>ulUnicodeRange1</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange1">OpenType specification: <c>OS/2</c>, <c>ulUnicodeRange1</c></seealso>
        public uint UlUnicodeRange1;

        /// <summary>Unicode range bits 32–63.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange2"><c>ulUnicodeRange2</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange2">OpenType specification: <c>OS/2</c>, <c>ulUnicodeRange2</c></seealso>
        public uint UlUnicodeRange2;

        /// <summary>Unicode range bits 64–95.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange3"><c>ulUnicodeRange3</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange3">OpenType specification: <c>OS/2</c>, <c>ulUnicodeRange3</c></seealso>
        public uint UlUnicodeRange3;

        /// <summary>Unicode range bits 96–127.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange4"><c>ulUnicodeRange4</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulunicoderange4">OpenType specification: <c>OS/2</c>, <c>ulUnicodeRange4</c></seealso>
        public uint UlUnicodeRange4;

        /// <summary>The four-character vendor ID tag, packed as a <see cref="uint"/>.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#achvendid"><c>achVendID</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#achvendid">OpenType specification: <c>OS/2</c>, <c>achVendID</c></seealso>
        public uint AchVendId;

        /// <summary>The style and usage bits.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fsselection"><c>fsSelection</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fsselection">OpenType specification: <c>OS/2</c>, <c>fsSelection</c></seealso>
        public ushort FsSelection;

        /// <summary>The minimum Unicode value in the font's <c>cmap</c>.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usfirstcharindex"><c>usFirstCharIndex</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usfirstcharindex">OpenType specification: <c>OS/2</c>, <c>usFirstCharIndex</c></seealso>
        public ushort UsFirstCharIndex;

        /// <summary>The maximum Unicode value in the font's <c>cmap</c>.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uslastcharindex"><c>usLastCharIndex</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uslastcharindex">OpenType specification: <c>OS/2</c>, <c>usLastCharIndex</c></seealso>
        public ushort UsLastCharIndex;

        /// <summary>The preferred ascender for typographic layout.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#stypoascender"><c>sTypoAscender</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#stypoascender">OpenType specification: <c>OS/2</c>, <c>sTypoAscender</c></seealso>
        public short STypoAscender;

        /// <summary>The preferred descender for typographic layout.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#stypodescender"><c>sTypoDescender</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#stypodescender">OpenType specification: <c>OS/2</c>, <c>sTypoDescender</c></seealso>
        public short STypoDescender;

        /// <summary>The preferred line gap for typographic layout.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#stypolinegap"><c>sTypoLineGap</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#stypolinegap">OpenType specification: <c>OS/2</c>, <c>sTypoLineGap</c></seealso>
        public short STypoLineGap;

        /// <summary>The Windows ascender metric (clipping boundary).</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswinascent"><c>usWinAscent</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswinascent">OpenType specification: <c>OS/2</c>, <c>usWinAscent</c></seealso>
        public ushort UsWinAscent;

        /// <summary>The Windows descender metric (clipping boundary).</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswindescent"><c>usWinDescent</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswindescent">OpenType specification: <c>OS/2</c>, <c>usWinDescent</c></seealso>
        public ushort UsWinDescent;

        /// <inheritdoc/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2">OpenType specification: <c>OS/2</c> table</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            XAvgCharWidth = BinaryPrimitives.ReverseEndianness(v.XAvgCharWidth),
            UsWeightClass = BinaryPrimitives.ReverseEndianness(v.UsWeightClass),
            UsWidthClass = BinaryPrimitives.ReverseEndianness(v.UsWidthClass),
            FsType = BinaryPrimitives.ReverseEndianness(v.FsType),
            YSubscriptXSize = BinaryPrimitives.ReverseEndianness(v.YSubscriptXSize),
            YSubscriptYSize = BinaryPrimitives.ReverseEndianness(v.YSubscriptYSize),
            YSubscriptXOffset = BinaryPrimitives.ReverseEndianness(v.YSubscriptXOffset),
            YSubscriptYOffset = BinaryPrimitives.ReverseEndianness(v.YSubscriptYOffset),
            YSuperscriptXSize = BinaryPrimitives.ReverseEndianness(v.YSuperscriptXSize),
            YSuperscriptYSize = BinaryPrimitives.ReverseEndianness(v.YSuperscriptYSize),
            YSuperscriptXOffset = BinaryPrimitives.ReverseEndianness(v.YSuperscriptXOffset),
            YSuperscriptYOffset = BinaryPrimitives.ReverseEndianness(v.YSuperscriptYOffset),
            YStrikeoutSize = BinaryPrimitives.ReverseEndianness(v.YStrikeoutSize),
            YStrikeoutPosition = BinaryPrimitives.ReverseEndianness(v.YStrikeoutPosition),
            SFamilyClass = BinaryPrimitives.ReverseEndianness(v.SFamilyClass),
            Panose = v.Panose,
            UlUnicodeRange1 = BinaryPrimitives.ReverseEndianness(v.UlUnicodeRange1),
            UlUnicodeRange2 = BinaryPrimitives.ReverseEndianness(v.UlUnicodeRange2),
            UlUnicodeRange3 = BinaryPrimitives.ReverseEndianness(v.UlUnicodeRange3),
            UlUnicodeRange4 = BinaryPrimitives.ReverseEndianness(v.UlUnicodeRange4),
            AchVendId = BinaryPrimitives.ReverseEndianness(v.AchVendId),
            FsSelection = BinaryPrimitives.ReverseEndianness(v.FsSelection),
            UsFirstCharIndex = BinaryPrimitives.ReverseEndianness(v.UsFirstCharIndex),
            UsLastCharIndex = BinaryPrimitives.ReverseEndianness(v.UsLastCharIndex),
            STypoAscender = BinaryPrimitives.ReverseEndianness(v.STypoAscender),
            STypoDescender = BinaryPrimitives.ReverseEndianness(v.STypoDescender),
            STypoLineGap = BinaryPrimitives.ReverseEndianness(v.STypoLineGap),
            UsWinAscent = BinaryPrimitives.ReverseEndianness(v.UsWinAscent),
            UsWinDescent = BinaryPrimitives.ReverseEndianness(v.UsWinDescent),
        };
    }

    /// <summary>The 8-byte version 1 trailing block: two code page range bitfields.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulcodepagerange1">version 1 additions</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulcodepagerange1">OpenType specification: <c>OS/2</c>, version 1 additions</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header1 : IEndianReversibleStruct<Header1>
    {
        /// <summary>Code page range bits 0–31.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulcodepagerange1"><c>ulCodePageRange1</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulcodepagerange1">OpenType specification: <c>OS/2</c>, <c>ulCodePageRange1</c></seealso>
        public uint UlCodePageRange1;

        /// <summary>Code page range bits 32–63.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulcodepagerange2"><c>ulCodePageRange2</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#ulcodepagerange2">OpenType specification: <c>OS/2</c>, <c>ulCodePageRange2</c></seealso>
        public uint UlCodePageRange2;

        /// <inheritdoc/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2">OpenType specification: <c>OS/2</c> table</seealso>
        public static Header1 ReverseEndianness(Header1 v) => new()
        {
            UlCodePageRange1 = BinaryPrimitives.ReverseEndianness(v.UlCodePageRange1),
            UlCodePageRange2 = BinaryPrimitives.ReverseEndianness(v.UlCodePageRange2),
        };
    }

    /// <summary>The 10-byte version 2 trailing block: vertical metrics and layout context.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#sxheight">version 2 additions</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#sxheight">OpenType specification: <c>OS/2</c>, version 2 additions</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header2 : IEndianReversibleStruct<Header2>
    {
        /// <summary>The x-height.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#sxheight"><c>sxHeight</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#sxheight">OpenType specification: <c>OS/2</c>, <c>sxHeight</c></seealso>
        public short SxHeight;

        /// <summary>The cap height.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#scapheight"><c>sCapHeight</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#scapheight">OpenType specification: <c>OS/2</c>, <c>sCapHeight</c></seealso>
        public short SCapHeight;

        /// <summary>The default character.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usdefaultchar"><c>usDefaultChar</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usdefaultchar">OpenType specification: <c>OS/2</c>, <c>usDefaultChar</c></seealso>
        public ushort UsDefaultChar;

        /// <summary>The break character.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usbreakchar"><c>usBreakChar</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usbreakchar">OpenType specification: <c>OS/2</c>, <c>usBreakChar</c></seealso>
        public ushort UsBreakChar;

        /// <summary>The maximum context length for layout lookups.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usmaxcontext"><c>usMaxContext</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usmaxcontext">OpenType specification: <c>OS/2</c>, <c>usMaxContext</c></seealso>
        public ushort UsMaxContext;

        /// <inheritdoc/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2">OpenType specification: <c>OS/2</c> table</seealso>
        public static Header2 ReverseEndianness(Header2 v) => new()
        {
            SxHeight = BinaryPrimitives.ReverseEndianness(v.SxHeight),
            SCapHeight = BinaryPrimitives.ReverseEndianness(v.SCapHeight),
            UsDefaultChar = BinaryPrimitives.ReverseEndianness(v.UsDefaultChar),
            UsBreakChar = BinaryPrimitives.ReverseEndianness(v.UsBreakChar),
            UsMaxContext = BinaryPrimitives.ReverseEndianness(v.UsMaxContext),
        };
    }

    /// <summary>The 4-byte version 5 trailing block: optical size bounds.</summary>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usloweropticalpointsize">version 5 additions</see> in the OpenType specification.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usloweropticalpointsize">OpenType specification: <c>OS/2</c>, version 5 additions</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header5 : IEndianReversibleStruct<Header5>
    {
        /// <summary>The lower optical size.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usloweropticalpointsize"><c>usLowerOpticalPointSize</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usloweropticalpointsize">OpenType specification: <c>OS/2</c>, <c>usLowerOpticalPointSize</c></seealso>
        public ushort UsLowerOpticalPointSize;

        /// <summary>The upper optical size.</summary>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usupperopticalpointsize"><c>usUpperOpticalPointSize</c> field</see> in the OpenType specification.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usupperopticalpointsize">OpenType specification: <c>OS/2</c>, <c>usUpperOpticalPointSize</c></seealso>
        public ushort UsUpperOpticalPointSize;

        /// <inheritdoc/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2">OpenType specification: <c>OS/2</c> table</seealso>
        public static Header5 ReverseEndianness(Header5 v) => new()
        {
            UsLowerOpticalPointSize = BinaryPrimitives.ReverseEndianness(v.UsLowerOpticalPointSize),
            UsUpperOpticalPointSize = BinaryPrimitives.ReverseEndianness(v.UsUpperOpticalPointSize),
        };
    }
}

/// <summary>Values for <c>OS/2.usWidthClass</c> and the <c>wdth</c> axis. Valid range is 1–9.</summary>
/// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswidthclass"><c>usWidthClass</c> field</see> in the OpenType specification.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswidthclass">OpenType specification: <c>OS/2</c>, <c>usWidthClass</c></seealso>
public enum WidthClass : ushort
{
    /// <summary>Ultra-condensed (50% of normal).</summary>
    UltraCondensed = 1,
    /// <summary>Extra-condensed (62.5%).</summary>
    ExtraCondensed = 2,
    /// <summary>Condensed (75%).</summary>
    Condensed = 3,
    /// <summary>Semi-condensed (87.5%).</summary>
    SemiCondensed = 4,
    /// <summary>Normal (100%).</summary>
    Normal = 5,
    /// <summary>Semi-expanded (112.5%).</summary>
    SemiExpanded = 6,
    /// <summary>Expanded (125%).</summary>
    Expanded = 7,
    /// <summary>Extra-expanded (150%).</summary>
    ExtraExpanded = 8,
    /// <summary>Ultra-expanded (200%).</summary>
    UltraExpanded = 9,
}

/// <summary>Common values for <c>OS/2.usWeightClass</c> and the <c>wght</c> axis. Values from 1 to 1000 are valid; these are the conventionally named stops.</summary>
/// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usweightclass"><c>usWeightClass</c> field</see> in the OpenType specification.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#usweightclass">OpenType specification: <c>OS/2</c>, <c>usWeightClass</c></seealso>
public enum WeightClass : ushort
{
    /// <summary>Thin (100).</summary>
    Thin = 100,
    /// <summary>Extra-light / Ultra-light (200).</summary>
    ExtraLight = 200,
    /// <summary>Light (300).</summary>
    Light = 300,
    /// <summary>Normal / Regular (400).</summary>
    Normal = 400,
    /// <summary>Medium (500).</summary>
    Medium = 500,
    /// <summary>Semi-bold / Demi-bold (600).</summary>
    SemiBold = 600,
    /// <summary>Bold (700).</summary>
    Bold = 700,
    /// <summary>Extra-bold / Ultra-bold (800).</summary>
    ExtraBold = 800,
    /// <summary>Black / Heavy (900).</summary>
    Black = 900,
    /// <summary>Extra-black / Ultra-black (950).</summary>
    ExtraBlack = 950,
}

/// <summary>Bit flags for the <c>OS/2</c> table's <c>fsType</c> field, which declares embedding permissions. Bits 0–3 must be mutually exclusive for versions 3 and later; versions 0–2 permit multiple bits with the least-restrictive permission winning.</summary>
/// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fstype"><c>fsType</c> field</see> in the OpenType specification.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fstype">OpenType specification: <c>OS/2</c>, <c>fsType</c></seealso>
[Flags]
public enum FsType : ushort
{
    /// <summary>No embedding restrictions.</summary>
    None = 0,

    /// <summary>Bit 0: Reserved (deprecated; was "restricted license" in early drafts).</summary>
    Reserved = 1 << 0,

    /// <summary>Bit 1: Restricted License embedding. The font must not be embedded.</summary>
    Restricted = 1 << 1,

    /// <summary>Bit 2: Preview &amp; Print embedding. The font may be embedded read-only.</summary>
    PreviewAndPrint = 1 << 2,

    /// <summary>Bit 3: Editable embedding. The font may be embedded and modified.</summary>
    Editable = 1 << 3,

    /// <summary>Bit 8: No subsetting. The font must be embedded in its entirety.</summary>
    NoSubsetting = 1 << 8,

    /// <summary>Bit 9: Bitmap embedding only. Only the bitmap representation may be embedded.</summary>
    BitmapEmbeddingOnly = 1 << 9,
}

/// <summary>Bit flags for the <c>OS/2</c> table's <c>fsSelection</c> field. These bits must agree with <c>head.macStyle</c>; the <c>fsSelection</c> bits take precedence on Windows.</summary>
/// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fsselection"><c>fsSelection</c> field</see> in the OpenType specification.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fsselection">OpenType specification: <c>OS/2</c>, <c>fsSelection</c></seealso>
[Flags]
public enum FsSelection : ushort
{
    /// <summary>No bits set.</summary>
    None = 0,

    /// <summary>Bit 0: Italic.</summary>
    Italic = 1 << 0,

    /// <summary>Bit 1: Underscore.</summary>
    Underscore = 1 << 1,

    /// <summary>Bit 2: Negative (inverse) image.</summary>
    Negative = 1 << 2,

    /// <summary>Bit 3: Outlined.</summary>
    Outlined = 1 << 3,

    /// <summary>Bit 4: Strikeout.</summary>
    Strikeout = 1 << 4,

    /// <summary>Bit 5: Bold.</summary>
    Bold = 1 << 5,

    /// <summary>Bit 6: Regular. Must be set only for the "regular" face of a family.</summary>
    Regular = 1 << 6,

    /// <summary>Bit 7: Use typographic metrics (<c>sTypo*</c>) for line layout.</summary>
    UseTypoMetrics = 1 << 7,

    /// <summary>Bit 8: WWS (weight/width/slope) family and subfamily names are consistent.</summary>
    Wws = 1 << 8,

    /// <summary>Bit 9: Oblique. Mutually exclusive with <see cref="Italic"/>.</summary>
    Oblique = 1 << 9,
}

/// <summary>PANOSE classification (10 bytes). The first byte selects the family kind; the remaining bytes are interpreted according to that kind.</summary>
/// <remarks>
/// <para>Every field is a byte, so the struct's in-memory layout is identical on little- and big-endian hosts and requires no endianness conversion.</para>
/// <para>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#panose"><c>panose</c> field</see> in the OpenType specification.</para>
/// </remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/os2#panose">OpenType specification: <c>OS/2</c>, <c>panose</c></seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct Panose
{
    /// <summary>The family kind.</summary>
    public byte FamilyKind;

    /// <summary>First classification byte.</summary>
    public byte Byte1;

    /// <summary>Second classification byte.</summary>
    public byte Byte2;

    /// <summary>Third classification byte.</summary>
    public byte Byte3;

    /// <summary>Fourth classification byte.</summary>
    public byte Byte4;

    /// <summary>Fifth classification byte.</summary>
    public byte Byte5;

    /// <summary>Sixth classification byte.</summary>
    public byte Byte6;

    /// <summary>Seventh classification byte.</summary>
    public byte Byte7;

    /// <summary>Eighth classification byte.</summary>
    public byte Byte8;

    /// <summary>Ninth classification byte.</summary>
    public byte Byte9;
}
