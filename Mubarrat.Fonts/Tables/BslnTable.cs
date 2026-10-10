using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// bsln — Baseline Table (Apple Advanced Typography)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>bsln</c> table: the additional baselines an Apple Advanced Typography font can align runs of text to, together with the baseline position each glyph naturally sits on.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is an Apple Advanced Typography (AAT) table and is not part of OpenType. Apple documents it in the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual, <c>bsln</c> table</see>. It is parsed for completeness and for tooling that inspects Apple fonts.</description></item>
/// <item><description>Two kinds of information are stored: <em>baseline positions</em>, which let runs of text be aligned to one another, and per-glyph <em>baseline values</em>, which identify the baseline position a glyph belongs to by default. A font that mixes Latin and ideographic glyphs uses the values to send the Latin glyphs to <see cref="RomanBaseline"/> and the ideographic glyphs to <see cref="IdeographicCenteredBaseline"/>.</description></item>
/// <item><description>Baseline values 0 through 4 are the predefined positions documented on this type as <see cref="RomanBaseline"/> and its siblings; values 5 through 31 are reserved for future use. Apple states that values 0 through 4 should be provided for every horizontal AAT font.</description></item>
/// <item><description>The four formats divide along two axes. Formats 0 and 1 describe baseline positions as <em>distance deltas</em> in FUnits; formats 2 and 3 name a <em>standard glyph</em> whose control points define the positions after hinting, which Apple notes retains baseline information better at small sizes. Formats 0 and 2 declare no per-glyph mapping, so every glyph uses <see cref="DefaultBaseline"/>; formats 1 and 3 add a <see cref="Mapping"/> lookup.</description></item>
/// <item><description>Only one format is present in a font, and for <see cref="DefaultBaseline"/> the manual states that the value applies to all glyphs in formats 0 and 2, and only to the glyphs the mapping does not cover in formats 1 and 3.</description></item>
/// <item><description>The format-specific part follows the eight-byte <see cref="Header"/> immediately: 32 <see cref="Deltas"/> in formats 0 and 1, or a standard glyph index and 32 <see cref="ControlPoints"/> in formats 2 and 3, followed by the mapping lookup in formats 1 and 3.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Header"/>
/// <seealso cref="LookupTable"/>
/// <seealso cref="GetBaseline(int)"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Tables.html">TrueType Reference Manual: Table Components</seealso>
public sealed record BslnTable : IFontTable<BslnTable>
{
    /// <summary>Gets the AAT table tag <c>bsln</c>.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
    public static Tag Tag => "bsln";

    /// <summary>The number of baseline positions a <c>bsln</c> table declares. A font must assign all 32 entries of <see cref="Deltas"/> or <see cref="ControlPoints"/>.</summary>
    /// <seealso cref="Deltas"/>
    /// <seealso cref="ControlPoints"/>
    public const int BaselineSlotCount = 32;

    /// <summary>The highest baseline value the table may assign to a glyph, <c>31</c>.</summary>
    /// <remarks><see cref="Header.DefaultBaseline"/> is documented as a value from 0 through 31; a larger value is structurally impossible.</remarks>
    /// <seealso cref="DefaultBaseline"/>
    public const ushort MaxBaselineValue = 31;

    /// <summary>The format value for a distance-based table with no mapping, format 0.</summary>
    /// <remarks>All glyphs use <see cref="DefaultBaseline"/>; the baseline positions are the FUnit <see cref="Deltas"/>.</remarks>
    /// <seealso cref="Format"/>
    public const ushort FormatDistanceNoMapping = 0;

    /// <summary>The format value for a distance-based table with a mapping, format 1.</summary>
    /// <remarks>The baseline positions are the FUnit <see cref="Deltas"/>; glyphs are mapped to baseline values by <see cref="Mapping"/>, and glyphs the mapping does not cover use <see cref="DefaultBaseline"/>.</remarks>
    /// <seealso cref="Format"/>
    public const ushort FormatDistanceWithMapping = 1;

    /// <summary>The format value for a control-point-based table with no mapping, format 2.</summary>
    /// <remarks>All glyphs use <see cref="DefaultBaseline"/>; the baseline positions are the <see cref="ControlPoints"/> of <see cref="StandardGlyph"/>.</remarks>
    /// <seealso cref="Format"/>
    public const ushort FormatControlPointNoMapping = 2;

    /// <summary>The format value for a control-point-based table with a mapping, format 3.</summary>
    /// <remarks>The baseline positions are the <see cref="ControlPoints"/> of <see cref="StandardGlyph"/>; glyphs are mapped to baseline values by <see cref="Mapping"/>, and glyphs the mapping does not cover use <see cref="DefaultBaseline"/>.</remarks>
    /// <seealso cref="Format"/>
    public const ushort FormatControlPointWithMapping = 3;

    /// <summary>The Roman baseline value, <c>0</c>.</summary>
    /// <remarks>The alignment used in most Latin-script languages: most of the glyph is above the baseline, descenders may fall below it, and the baseline sits near the bottom of the line.</remarks>
    /// <seealso cref="IdeographicCenteredBaseline"/>
    public const ushort RomanBaseline = 0;

    /// <summary>The ideographic centered baseline value, <c>1</c>.</summary>
    /// <remarks>The behavior used by Chinese, Japanese, and Korean ideographic scripts, which center themselves halfway through the line height.</remarks>
    /// <seealso cref="IdeographicLowBaseline"/>
    public const ushort IdeographicCenteredBaseline = 1;

    /// <summary>The ideographic low baseline value, <c>2</c>.</summary>
    /// <remarks>The same behavior as <see cref="IdeographicCenteredBaseline"/>, with the glyphs lowered slightly so that ideographs adjacent to Latin characters appear to descend a little below the Roman baseline.</remarks>
    /// <seealso cref="IdeographicCenteredBaseline"/>
    public const ushort IdeographicLowBaseline = 2;

    /// <summary>The hanging baseline value, <c>3</c>.</summary>
    /// <remarks>The alignment used by Devanagari and derived scripts, where the bulk of the glyph sits below the baseline and the baseline itself appears near the top of the line. Also used for drop capitals.</remarks>
    /// <seealso cref="MathBaseline"/>
    public const ushort HangingBaseline = 3;

    /// <summary>The math baseline value, <c>4</c>.</summary>
    /// <remarks>The alignment used for setting mathematics, where operators such as the minus sign need centering. Usually set at half the x-height of a font.</remarks>
    /// <seealso cref="HangingBaseline"/>
    public const ushort MathBaseline = 4;

    /// <summary>The control point number that marks the absence of a control point for a baseline, <c>0xFFFF</c>.</summary>
    /// <seealso cref="ControlPoints"/>
    /// <seealso cref="GetControlPoint(int)"/>
    public const ushort NoControlPoint = 0xFFFF;

    /// <summary>The value reported by <see cref="StandardGlyph"/> when the format declares no standard glyph.</summary>
    /// <remarks>Formats 0 and 1 are distance-based and name no standard glyph.</remarks>
    /// <seealso cref="StandardGlyph"/>
    public const int NoStandardGlyph = -1;

    /// <summary>Size, in bytes, of the fixed-layout <see cref="Header"/>.</summary>
    /// <remarks>The format-specific part begins at this offset from the start of the table; no offset field introduces it.</remarks>
    /// <seealso cref="Header"/>
    public const int HeaderSize = 8;

    /// <summary>Gets the table version.</summary>
    /// <remarks>The current version is <c>0x00010000</c> (1.0). Parsing rejects a table whose major version is not 1.</remarks>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="MinorVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the major half of <see cref="Version"/>, which must be 1.</summary>
    /// <seealso cref="Version"/>
    /// <seealso cref="MinorVersion"/>
    public ushort MajorVersion => (ushort)Version.IntegerPart;

    /// <summary>Gets the minor half of <see cref="Version"/>.</summary>
    /// <seealso cref="Version"/>
    /// <seealso cref="MajorVersion"/>
    public ushort MinorVersion => Version.FractionPart;

    /// <summary>Gets the format of the table: one of <see cref="FormatDistanceNoMapping"/>, <see cref="FormatDistanceWithMapping"/>, <see cref="FormatControlPointNoMapping"/>, or <see cref="FormatControlPointWithMapping"/>.</summary>
    /// <remarks>Only one baseline format may be selected for a font.</remarks>
    /// <seealso cref="FormatDistanceNoMapping"/>
    /// <seealso cref="FormatDistanceWithMapping"/>
    /// <seealso cref="FormatControlPointNoMapping"/>
    /// <seealso cref="FormatControlPointWithMapping"/>
    public ushort Format { get; init; }

    /// <summary>Gets the default baseline value for glyphs that the table does not map individually.</summary>
    /// <remarks>A value from 0 through 31. In formats 0 and 2 it applies to every glyph of the font; in formats 1 and 3 it applies only to the glyphs <see cref="Mapping"/> does not cover.</remarks>
    /// <seealso cref="MaxBaselineValue"/>
    /// <seealso cref="GetBaseline(int)"/>
    public ushort DefaultBaseline { get; init; }

    /// <summary>Gets the FUnit distance deltas from the font's natural baseline to the other baselines, for formats 0 and 1.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The list holds <see cref="BaselineSlotCount"/> entries; index <c>i</c> is the delta for baseline value <c>i</c>. Index <see cref="RomanBaseline"/> is zero because the Roman baseline is the font's natural baseline.</description></item>
    /// <item><description>The values are signed: "up" is towards more positive font coordinates, so a hanging baseline above the natural baseline is a positive delta.</description></item>
    /// <item><description>The list is empty for the control-point-based <see cref="FormatControlPointNoMapping"/> and <see cref="FormatControlPointWithMapping"/> formats, which use <see cref="ControlPoints"/> instead.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetBaselineDelta(int)"/>
    /// <seealso cref="ControlPoints"/>
    public IReadOnlyList<short> Deltas { get; init; } = [];

    /// <summary>Gets the control point numbers that define the baseline positions, for formats 2 and 3.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The list holds <see cref="BaselineSlotCount"/> entries; index <c>i</c> is the control point for baseline value <c>i</c>.</description></item>
    /// <item><description><see cref="NoControlPoint"/> (<c>0xFFFF</c>) means the standard glyph has no corresponding control point for that baseline.</description></item>
    /// <item><description>Control points are numbers within <see cref="StandardGlyph"/>, resolved after instructions have been applied to it. The list is empty for the distance-based <see cref="FormatDistanceNoMapping"/> and <see cref="FormatDistanceWithMapping"/> formats, which use <see cref="Deltas"/> instead.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="StandardGlyph"/>
    /// <seealso cref="GetControlPoint(int)"/>
    /// <seealso cref="NoControlPoint"/>
    public IReadOnlyList<ushort> ControlPoints { get; init; } = [];

    /// <summary>Gets the glyph index of the standard glyph whose control points define the baseline positions, for formats 2 and 3.</summary>
    /// <remarks>This glyph must contain the control points listed in <see cref="ControlPoints"/>. The value is <see cref="NoStandardGlyph"/> for the distance-based formats 0 and 1.</remarks>
    /// <seealso cref="ControlPoints"/>
    /// <seealso cref="NoStandardGlyph"/>
    public int StandardGlyph { get; init; } = NoStandardGlyph;

    /// <summary>Gets the lookup table that maps glyphs to their baseline values, for formats 1 and 3.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup value is a 16-bit baseline value. Apple defines the interpretation for each lookup format: format 0 is an array holding one baseline value per glyph in the font; format 2 stores the baseline value directly in each segment; format 4 stores the offset of a per-glyph array of baseline values; format 6 stores one baseline value per glyph entry; format 8 stores a trimmed array of baseline values.</description></item>
    /// <item><description>The property is <see langword="null"/> for the unmapped formats 0 and 2, where every glyph uses <see cref="DefaultBaseline"/>.</description></item>
    /// <item><description>Because a format 0 lookup declares no length of its own, the parser needs the font's glyph count to read one. It takes that count from the <c>context</c> passed to <see cref="IRecord{T}.Parse(ref Cursor, object?)"/> when the context is an <see cref="int"/> or a <see cref="FontFace"/>; otherwise the lookup is read to the end of the table.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LookupTable"/>
    /// <seealso cref="GetBaseline(int)"/>
    /// <seealso cref="HasMapping"/>
    public LookupTable? Mapping { get; init; }

    /// <summary>Gets a value indicating whether the format maps glyphs to baseline values individually.</summary>
    /// <remarks><see langword="true"/> for formats 1 and 3, which carry a <see cref="Mapping"/>; <see langword="false"/> for formats 0 and 2.</remarks>
    /// <seealso cref="Mapping"/>
    public bool HasMapping => Mapping is not null;

    /// <summary>Gets a value indicating whether the baseline positions are given as control point numbers rather than FUnit deltas.</summary>
    /// <remarks><see langword="true"/> for formats 2 and 3, which define the positions through the <see cref="ControlPoints"/> of <see cref="StandardGlyph"/>; <see langword="false"/> for the distance-based formats 0 and 1.</remarks>
    /// <seealso cref="Format"/>
    /// <seealso cref="ControlPoints"/>
    public bool IsControlPointFormat => Format is FormatControlPointNoMapping or FormatControlPointWithMapping;

    /// <summary>Gets the number of baseline positions the table declares.</summary>
    /// <remarks><see cref="BaselineSlotCount"/> for a conforming table, since either <see cref="Deltas"/> or <see cref="ControlPoints"/> is populated with that many entries. Zero only if the format-specific part declares nothing.</remarks>
    /// <seealso cref="Deltas"/>
    /// <seealso cref="ControlPoints"/>
    public int BaselineCount => Deltas.Count != 0 ? Deltas.Count : ControlPoints.Count;

    /// <summary>Gets the baseline value of a glyph.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <returns>The glyph's baseline value, or <see cref="DefaultBaseline"/> when the table declares no mapping or the mapping does not cover the glyph.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Formats 1 and 3 answer from <see cref="Mapping"/>; the raw 16-bit lookup value is returned as-is, and a conforming font stores a value from 0 through 31.</description></item>
    /// <item><description>Formats 0 and 2, and any glyph outside the mapped range, answer with <see cref="DefaultBaseline"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// ushort baseline = bsln.GetBaseline(glyphId);
    /// bool ideographic = baseline == BslnTable.IdeographicCenteredBaseline;
    /// </code>
    /// </example>
    /// <seealso cref="Mapping"/>
    /// <seealso cref="DefaultBaseline"/>
    public ushort GetBaseline(int glyphIndex) =>
        Mapping is not null && Mapping.TryGetValue(glyphIndex, out uint value)
            ? (ushort)value
            : DefaultBaseline;

    /// <summary>Gets the FUnit distance delta that defines a baseline position.</summary>
    /// <param name="baselineValue">The baseline value to look up, from 0 through <see cref="MaxBaselineValue"/>.</param>
    /// <returns>The delta for that baseline, or <c>0</c> when the table is control-point-based or the value is out of range.</returns>
    /// <remarks>The Roman baseline is the font's natural baseline, so <see cref="RomanBaseline"/> normally has a delta of zero.</remarks>
    /// <seealso cref="Deltas"/>
    /// <seealso cref="GetControlPoint(int)"/>
    public short GetBaselineDelta(int baselineValue) =>
        (uint)baselineValue < (uint)Deltas.Count ? Deltas[baselineValue] : (short)0;

    /// <summary>Gets the control point that defines a baseline position.</summary>
    /// <param name="baselineValue">The baseline value to look up, from 0 through <see cref="MaxBaselineValue"/>.</param>
    /// <returns>The control point number for that baseline, or <see cref="NoControlPoint"/> when the table is distance-based or the value is out of range.</returns>
    /// <seealso cref="ControlPoints"/>
    /// <seealso cref="GetBaselineDelta(int)"/>
    public ushort GetControlPoint(int baselineValue) =>
        (uint)baselineValue < (uint)ControlPoints.Count ? ControlPoints[baselineValue] : NoControlPoint;

    /// <summary>The 8-byte fixed-layout <c>bsln</c> table header.</summary>
    /// <remarks>Fields are stored in big-endian order at the offsets the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html"><c>bsln</c> table specification</see> defines. The header is followed immediately by the format-specific part.</remarks>
    /// <seealso cref="BslnTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0.</summary>
        /// <remarks><c>0x00010000</c> (1.0) for the current version.</remarks>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
        public Fixed Version;          // +0

        /// <summary>The format of the baseline table at byte offset 4.</summary>
        /// <remarks>One of 0, 1, 2, or 3. Only one baseline format may be selected for a font.</remarks>
        /// <seealso cref="BslnTable.FormatDistanceNoMapping"/>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
        public ushort Format;          // +4

        /// <summary>The default baseline value for glyphs without individual mapping at byte offset 6.</summary>
        /// <remarks>A value from 0 through 31.</remarks>
        /// <seealso cref="BslnTable.MaxBaselineValue"/>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
        public ushort DefaultBaseline; // +6

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are multi-byte; <see cref="Version"/> is a 16.16 <see cref="Fixed"/> whose 32 bits are reversed as a unit.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = new Fixed(BinaryPrimitives.ReverseEndianness(v.Version.Bits)),
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            DefaultBaseline = BinaryPrimitives.ReverseEndianness(v.DefaultBaseline),
        };
    }

    /// <summary>Resolves the value a nested lookup table needs in order to read a length-less format 0 array.</summary>
    /// <param name="context">The context handed to <see cref="IRecord{T}.Parse(ref Cursor, object?)"/>: a <see cref="FontFace"/>, an <see cref="int"/> glyph count, or <see langword="null"/>.</param>
    /// <returns>The font's glyph count as an <see cref="int"/> when it can be obtained, or <see langword="null"/> so that the lookup is read to the end of the table.</returns>
    /// <remarks>A format 0 lookup is a bare array with no declared length, so it must be told how many glyphs the font has. A <see cref="FontFace"/> context supplies the count from <c>maxp.numGlyphs</c>, the same way the <c>lcar</c>, <c>opbd</c>, and <c>prop</c> tables obtain it; an <see cref="int"/> context is taken as that count directly. The count is optional because a table that never uses a format 0 lookup does not need it.</remarks>
    /// <seealso cref="Mapping"/>
    /// <seealso cref="LookupTable.Parse(ref Cursor, object?)"/>
    private static object? GetGlyphCountContext(object? context) => context switch
    {
        int glyphCount => glyphCount,
        FontFace face when face.Directory.ContainsKey(MaxpTable.Tag) => (int)face.GetTable<MaxpTable>().NumGlyphs,
        _ => null,
    };

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the bsln table.</param>
    /// <param name="context">A <see cref="FontFace"/> so that a format 0 mapping lookup can be sized by <c>maxp.numGlyphs</c>, or <see langword="null"/>; a plain <see cref="int"/> glyph count is also accepted.</param>
    /// <returns>The parsed bsln table.</returns>
    /// <exception cref="InvalidDataException">The major version is not 1, the format is not 0, 1, 2, or 3, or <c>defaultBaseline</c> is greater than 31.</exception>
    /// <exception cref="EndOfStreamException">The header, the format-specific part, or the mapping lookup extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The format decides how much of the format-specific part is read: 32 deltas, 32 deltas plus a lookup, a standard glyph plus 32 control points, or a standard glyph plus 32 control points plus a lookup.</description></item>
    /// <item><description>Nothing after the format-specific part is read, so any trailing padding is ignored.</description></item>
    /// <item><description>A format 0 mapping lookup declares no length of its own, so the glyph count reaches it through <paramref name="context"/>; with no usable context it is read to the end of the table data.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Mapping"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6bsln.html">TrueType Reference Manual: The <c>bsln</c> table</seealso>
    static BslnTable IRecord<BslnTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version.IntegerPart != 1)
        {
            throw new InvalidDataException(
                $"'bsln'.version is 0x{unchecked((uint)header.Version.Bits):X8}, expected a 1.x table.");
        }

        if (header.Format is not (FormatDistanceNoMapping or FormatDistanceWithMapping
            or FormatControlPointNoMapping or FormatControlPointWithMapping))
        {
            throw new InvalidDataException($"'bsln'.format is {header.Format}, expected 0, 1, 2, or 3.");
        }

        if (header.DefaultBaseline > MaxBaselineValue)
            throw new InvalidDataException($"'bsln'.defaultBaseline is {header.DefaultBaseline}, expected 0 through {MaxBaselineValue}.");

        IReadOnlyList<short> deltas = [];
        IReadOnlyList<ushort> controlPoints = [];
        int standardGlyph = NoStandardGlyph;
        LookupTable? mapping = null;

        switch (header.Format)
        {
            case FormatDistanceNoMapping:
                deltas = cursor.ReadInt16Array(BaselineSlotCount);
                break;

            case FormatDistanceWithMapping:
                deltas = cursor.ReadInt16Array(BaselineSlotCount);
                mapping = LookupTable.Parse(ref cursor, GetGlyphCountContext(context));
                break;

            case FormatControlPointNoMapping:
                standardGlyph = cursor.ReadUInt16();
                controlPoints = cursor.ReadUInt16Array(BaselineSlotCount);
                break;

            case FormatControlPointWithMapping:
                standardGlyph = cursor.ReadUInt16();
                controlPoints = cursor.ReadUInt16Array(BaselineSlotCount);
                mapping = LookupTable.Parse(ref cursor, GetGlyphCountContext(context));
                break;
        }

        return new BslnTable
        {
            Version = header.Version,
            Format = header.Format,
            DefaultBaseline = header.DefaultBaseline,
            Deltas = deltas,
            ControlPoints = controlPoints,
            StandardGlyph = standardGlyph,
            Mapping = mapping,
        };
    }
}
