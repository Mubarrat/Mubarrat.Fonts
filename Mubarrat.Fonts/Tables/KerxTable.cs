using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Sfnt;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// The 'kerx' table — extended AAT kerning
//
// Uses the shared AAT infrastructure from AatShared.cs: STXHeader, LookupTable and
// its format-specific subclasses, AatClass.
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>kerx</c> table: extended Apple Advanced Typography kerning for horizontal and vertical text, with contextual, cross-stream, and variation-font support.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>An extended replacement for the older <c>kern</c> table. It stores a sequence of positioning subtables, each in one of five formats, and each independently opting into vertical, cross-stream, or variation kerning through a coverage word.</description></item>
/// <item><description>Subtables are additive, so their order does not affect the final positioning.</description></item>
/// <item><description>Version 2 predates the subtable glyph coverage table; version 3 adds it; version 4 adds variation-font tuple vectors, keyed off the subtable's <c>tupleCount</c>.</description></item>
/// <item><description>All multi-byte reads use the sfnt default of big-endian.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html"><c>kerx</c> table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxSubtable"/>
/// <seealso cref="KerxGlyphCoverageTable"/>
/// <seealso cref="KernTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxTable : IFontTable<KerxTable>, IRecord<KerxTable>
{
    /// <summary>The lowest version defined by the current specification.</summary>
    public const ushort MinVersion = 2;

    /// <summary>The version at which the subtable glyph coverage table was introduced.</summary>
    public const ushort CoverageTableVersion = 3;

    /// <summary>The version at which variation-font tuple kerning was introduced.</summary>
    public const ushort VariationVersion = 4;

    /// <summary>The highest version defined by the current specification.</summary>
    public const ushort MaxVersion = 4;

    /// <inheritdoc/>
    public static Tag Tag => "kerx";

    /// <summary>Gets the table version.</summary>
    /// <value>One of 2, 3, or 4. Version 3 and 4 add the subtable glyph coverage table and tuple-vector kerning respectively.</value>
    /// <seealso cref="MinVersion"/>
    /// <seealso cref="MaxVersion"/>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="HasCoverageTable"/>
    public ushort Version { get; init; }

    /// <summary>Gets the number of positioning subtables.</summary>
    /// <value>The <c>nTables</c> field from the header. Each subtable carries its own length, so the count is the only positional information the top level provides.</value>
    /// <seealso cref="Subtables"/>
    public uint NumberOfSubtables { get; init; }

    /// <summary>Gets the positioning subtables, in table order.</summary>
    /// <value>One <see cref="KerxSubtable"/> per declared subtable. Because subtables are additive, the order does not affect the final kerning result.</value>
    /// <seealso cref="KerxSubtable"/>
    /// <seealso cref="NumberOfSubtables"/>
    public required KerxSubtable[] Subtables { get; init; }

    /// <summary>Gets the subtable glyph coverage table, or <see langword="null"/> when the version predates it.</summary>
    /// <value>The parsed <see cref="KerxGlyphCoverageTable"/> when <see cref="Version"/> is 3 or greater; otherwise <see langword="null"/>.</value>
    /// <remarks>The coverage table lets a layout engine skip subtables whose glyph bitfield does not intersect the run being shaped.</remarks>
    /// <seealso cref="KerxGlyphCoverageTable"/>
    /// <seealso cref="HasCoverageTable"/>
    public KerxGlyphCoverageTable? Coverage { get; init; }

    /// <summary>Gets a value indicating whether the table carries a subtable glyph coverage table.</summary>
    /// <value><see langword="true"/> when <see cref="Version"/> is 3 or greater.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="CoverageTableVersion"/>
    public bool HasCoverageTable => Version >= CoverageTableVersion;

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the <c>kerx</c> table.</param>
    /// <param name="context">An <see cref="SfntFontFace"/> used to obtain the glyph count, which the subtable coverage bitfields and format-0 lookups require.</param>
    /// <returns>The parsed <c>kerx</c> table.</returns>
    /// <exception cref="InvalidDataException">The version is unsupported, or <paramref name="context"/> is not an <see cref="SfntFontFace"/>.</exception>
    /// <exception cref="EndOfStreamException">A read extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is read via <c>ReadEndianReversibleStruct</c> and consumed locally.</description></item>
    /// <item><description>Each subtable is parsed by <see cref="KerxSubtable.Parse(ref Cursor, object?)"/>, which dispatches on the low byte of the subtable coverage word.</description></item>
    /// <item><description>When the version is 3 or greater, the subtable glyph coverage table follows the last subtable and is parsed with the glyph count and subtable count as its context.</description></item>
    /// <item><description>The font's glyph count is required by format-0 lookups inside subtables and by the coverage bitfield size; it is obtained from the font face in <paramref name="context"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso cref="Header"/>
    /// <seealso cref="KerxSubtable.Parse(ref Cursor, object?)"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    static KerxTable IRecord<KerxTable>.Parse(ref Cursor cursor, object? context)
    {
        long tableStart = cursor.Position;
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version < MinVersion || header.Version > MaxVersion)
        {
            throw new InvalidDataException(
                $"'kerx'.version is {header.Version}, expected {MinVersion}, {CoverageTableVersion}, or {MaxVersion}.");
        }

        int numGlyphs = context is SfntFontFace face
            ? face.GetTable<MaxpTable>().NumGlyphs
            : throw new InvalidDataException(
                "'kerx' requires an SFNT font face context to obtain the glyph count.");

        var subtableContext = new KerxSubtable.Context(numGlyphs, header.Version);
        var subtables = new KerxSubtable[header.NTables];
        for (int i = 0; i < subtables.Length; i++)
            subtables[i] = cursor.ReadRecord<KerxSubtable>(subtableContext);

        KerxGlyphCoverageTable? coverage = null;
        if (header.Version >= CoverageTableVersion)
        {
            coverage = cursor.ReadRecord<KerxGlyphCoverageTable>(
                new KerxGlyphCoverageTable.Context(numGlyphs, (int)header.NTables));
        }

        return new()
        {
            Version = header.Version,
            NumberOfSubtables = header.NTables,
            Subtables = subtables,
            Coverage = coverage,
        };
    }

    /// <summary>The 8-byte <c>kerx</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The version field carries the table's overall format version; the padding field is unused and should be zero.</description></item>
    /// <item><description>Fields are reversed field-by-field because the first two are 16-bit and the third is 32-bit.</description></item>
    /// <item><description>Consumed at parse time and not retained on <see cref="KerxTable"/>.</description></item>
    /// <item><description>The <c>&lt;Clone&gt;$()</c> method generated by the <c>record struct</c> declaration must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KerxTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The extended kerning table version at byte offset 0.</summary>
        /// <value>One of 2, 3, or 4.</value>
        /// <seealso cref="KerxTable.Version"/>
        public ushort Version;   // +0

        /// <summary>Unused padding at byte offset 2, set to zero.</summary>
        /// <value>The padding word; the parser preserves the value but does not validate it.</value>
        public ushort Padding;   // +2

        /// <summary>The number of positioning subtables at byte offset 4.</summary>
        /// <value>The <c>nTables</c> field; the count of subtables that follow the header.</value>
        /// <seealso cref="KerxTable.NumberOfSubtables"/>
        public uint NTables;     // +4

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>The first two fields are 16-bit and the third is 32-bit, so the reversal is field-by-field rather than a single word-span pass.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(value.Version),
            Padding = BinaryPrimitives.ReverseEndianness(value.Padding),
            NTables = BinaryPrimitives.ReverseEndianness(value.NTables),
        };
    }
}

/// <summary>Base for the five positioning subtable formats in a <c>kerx</c> table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Every subtable begins with a 12-byte common header: a <c>length</c>, a <c>coverage</c> word, and a <c>tupleCount</c>.</description></item>
/// <item><description>The low byte of the coverage word is the format discriminant; the remaining bits describe how the subtable's adjustments apply.</description></item>
/// <item><description>Dispatch happens in <see cref="Parse(ref Cursor, object?)"/> via <see cref="IBaseRecord{TBase}"/>, mirroring the pattern used by <c>Coverage</c> and the GSUB/GPOS lookup subtable formats.</description></item>
/// <item><description>Formats 3, 5, and 7 through 255 are reserved for future use; the parser rejects them as malformed.</description></item>
/// </list>
/// <para>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">positioning subtable formats</see> section of the <c>kerx</c> table description.</para>
/// </remarks>
/// <seealso cref="KerxFormat0Subtable"/>
/// <seealso cref="KerxFormat1Subtable"/>
/// <seealso cref="KerxFormat2Subtable"/>
/// <seealso cref="KerxFormat4Subtable"/>
/// <seealso cref="KerxFormat6Subtable"/>
/// <seealso cref="IBaseRecord{TBase}"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public abstract record KerxSubtable : IRecord<KerxSubtable>, IBaseRecord<KerxSubtable>
{
    /// <summary>Gets the length of this subtable in bytes, including its 12-byte common header.</summary>
    /// <value>The <c>length</c> field from the common header. Every derived parser uses this field to advance the cursor past its own format-specific data.</value>
    /// <seealso cref="SubtableHeader.Length"/>
    public required uint Length { get; init; }

    /// <summary>Gets the raw coverage word.</summary>
    /// <value>The <c>coverage</c> field from the common header. Use the bit accessors for the meaningful flags.</value>
    /// <seealso cref="Vertical"/>
    /// <seealso cref="CrossStream"/>
    /// <seealso cref="Variation"/>
    /// <seealso cref="ProcessDirection"/>
    /// <seealso cref="Format"/>
    public required uint Coverage { get; init; }

    /// <summary>Gets the tuple count for variation kerning.</summary>
    /// <value>The <c>tupleCount</c> field from the common header. Zero for non-variation subtables; 1 or more for variation subtables, in which case the format-specific data stores tupleCount-dimensional vectors rather than single kerning values.</value>
    /// <remarks>The field is only honored when the enclosing table's version is 4 or greater; earlier versions ignore it.</remarks>
    /// <seealso cref="KerxTable.VariationVersion"/>
    public required uint TupleCount { get; init; }

    /// <summary>Gets the format discriminant.</summary>
    /// <value>The low byte of <see cref="Coverage"/>: one of 0, 1, 2, 4, or 6.</value>
    /// <seealso cref="KerxFormat0Subtable"/>
    /// <seealso cref="KerxFormat1Subtable"/>
    /// <seealso cref="KerxFormat2Subtable"/>
    /// <seealso cref="KerxFormat4Subtable"/>
    /// <seealso cref="KerxFormat6Subtable"/>
    public abstract int Format { get; }

    /// <summary>Gets a value indicating whether the subtable carries vertical kerning values.</summary>
    /// <value><see langword="true"/> when bit <c>0x80000000</c> of <see cref="Coverage"/> is set.</value>
    /// <seealso cref="CrossStream"/>
    /// <seealso cref="Coverage"/>
    public bool Vertical => (Coverage & 0x80000000u) != 0;

    /// <summary>Gets a value indicating whether the subtable carries cross-stream kerning values.</summary>
    /// <value><see langword="true"/> when bit <c>0x40000000</c> of <see cref="Coverage"/> is set.</value>
    /// <remarks>Cross-stream adjustments are perpendicular to the flow of text: vertical when the text is horizontal, horizontal when the text is vertical.</remarks>
    /// <seealso cref="Vertical"/>
    /// <seealso cref="Coverage"/>
    public bool CrossStream => (Coverage & 0x40000000u) != 0;

    /// <summary>Gets a value indicating whether the subtable carries variation kerning values.</summary>
    /// <value><see langword="true"/> when bit <c>0x20000000</c> of <see cref="Coverage"/> is set.</value>
    /// <seealso cref="TupleCount"/>
    /// <seealso cref="Coverage"/>
    public bool Variation => (Coverage & 0x20000000u) != 0;

    /// <summary>Gets a value indicating whether the state machine processes glyphs from last to first.</summary>
    /// <value><see langword="true"/> when bit <c>0x10000000</c> of <see cref="Coverage"/> is set.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>When clear, glyphs are processed from first to last; when set, from last to first.</description></item>
    /// <item><description>This flag only applies to state-table based subtables, formats 1 and 4.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KerxFormat1Subtable"/>
    /// <seealso cref="KerxFormat4Subtable"/>
    /// <seealso cref="Coverage"/>
    public bool ProcessDirection => (Coverage & 0x10000000u) != 0;

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the subtable's first byte.</param>
    /// <param name="context">A <see cref="Context"/> carrying the font's glyph count and the enclosing table's version.</param>
    /// <returns>The parsed subtable, typed as the matching derived record.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not one of 0, 1, 2, 4, or 6.</exception>
    /// <exception cref="EndOfStreamException">The subtable extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The dispatcher peeks the coverage word at byte offset 4, extracts the low byte, and forwards to the matching derived parser.</description></item>
    /// <item><description>The coverage word is peeked by seeking and restoring, so the derived parser sees the whole subtable from byte 0.</description></item>
    /// <item><description>An unrecognized format discriminant is rejected; the specification defines exactly five formats and reserves the rest.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso cref="IBaseRecord{TBase}"/>
    /// <seealso cref="Context"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    public static KerxSubtable Parse(ref Cursor cursor, object? context = null)
    {
        long start = cursor.Position;
        cursor.Position = start + 4;
        uint coverage = cursor.ReadUInt32();
        cursor.Position = start;

        int format = (int)(coverage & 0xFFu);
        return format switch
        {
            0 => IBaseRecord<KerxSubtable>.Parse<KerxFormat0Subtable>(ref cursor, context),
            1 => IBaseRecord<KerxSubtable>.Parse<KerxFormat1Subtable>(ref cursor, context),
            2 => IBaseRecord<KerxSubtable>.Parse<KerxFormat2Subtable>(ref cursor, context),
            4 => IBaseRecord<KerxSubtable>.Parse<KerxFormat4Subtable>(ref cursor, context),
            6 => IBaseRecord<KerxSubtable>.Parse<KerxFormat6Subtable>(ref cursor, context),
            _ => throw new InvalidDataException(
                $"'kerx' subtable format {format} is not one of 0, 1, 2, 4, or 6."),
        };
    }

    /// <summary>Context for parsing a <see cref="KerxSubtable"/>: the font's glyph count and the enclosing table's version.</summary>
    /// <param name="GlyphCount">Number of glyphs in the font, from <c>maxp.numGlyphs</c>.</param>
    /// <param name="Version">The enclosing <c>kerx</c> table's version. Determines whether the tupleCount field is honored and whether the coverage table is present.</param>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The glyph count is required by format-0 lookups inside class and index tables, which are dense arrays sized to the font's glyph count.</description></item>
    /// <item><description>The version gates tuple-vector interpretation; earlier versions ignore the tupleCount field even though it is present on disk.</description></item>
    /// <item><description>The context is a record type so that the two values cannot be confused with bare integers at the call site.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KerxTable.Version"/>
    /// <seealso cref="LookupTable.Context"/>
    public record Context(int GlyphCount, ushort Version);

    /// <summary>The 12-byte common subtable header shared by every <see cref="KerxSubtable"/> format.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The format discriminant is the low byte of <see cref="Coverage"/>; the remaining coverage bits describe the subtable's applicability.</description></item>
    /// <item><description>All three fields are <c>UInt32</c>, so the reversal is a single pass over the struct reinterpreted as three 32-bit words — no field-by-field code is needed.</description></item>
    /// <item><description>Consumed at parse time and not retained on <see cref="KerxSubtable"/>; its field values are exposed through the semantic record's properties.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KerxSubtable"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct SubtableHeader : IEndianReversibleStruct<SubtableHeader>
    {
        /// <summary>The subtable length in bytes, including this header, at byte offset 0.</summary>
        /// <value>The byte length of the entire subtable; every derived parser uses this field to advance the cursor past its own format-specific data.</value>
        /// <seealso cref="KerxSubtable.Length"/>
        public uint Length;     // +0

        /// <summary>The coverage word at byte offset 4.</summary>
        /// <value>The subtable's coverage flags; the low byte is the format discriminant.</value>
        /// <seealso cref="KerxSubtable.Coverage"/>
        /// <seealso cref="KerxSubtable.Format"/>
        public uint Coverage;   // +4

        /// <summary>The tuple count at byte offset 8.</summary>
        /// <value>Zero for non-variation subtables; 1 or more for variation subtables. Only honored when the enclosing table's version is 4 or greater.</value>
        /// <seealso cref="KerxSubtable.TupleCount"/>
        /// <seealso cref="KerxTable.VariationVersion"/>
        public uint TupleCount; // +8

        /// <summary>Reverses the byte order of all three 32-bit fields in one pass.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with all three fields reversed.</returns>
        /// <remarks>The header is entirely <c>UInt32</c> fields, so it is reinterpreted as a span of three 32-bit words and reversed with a single <see cref="BinaryPrimitives.ReverseEndianness(ReadOnlySpan{uint}, Span{uint})"/> call.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static SubtableHeader ReverseEndianness(SubtableHeader value)
        {
            Span<uint> words = MemoryMarshal.CreateSpan(ref Unsafe.As<SubtableHeader, uint>(ref value), 3);
            BinaryPrimitives.ReverseEndianness(words, words);
            return value;
        }
    }
}

/// <summary>A format 0 <c>kerx</c> subtable: an ordered-list format in which a lookup maps each left-hand glyph to a variable-length list of kerning pairs.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The lookup's value for a left-hand glyph is a byte offset from the start of the subtable to a list of <see cref="KerningPair"/> records.</description></item>
/// <item><description>Each pair list is terminated by a sentinel pair whose <c>left</c> and <c>right</c> fields are both <c>0xFFFF</c>; the sentinel is not included in the parsed result.</description></item>
/// <item><description>When the subtable's <c>tupleCount</c> is non-zero, each pair's kerning value is stored as a vector of <c>tupleCount</c> <c>F2Dot14</c> values rather than a single 16.16 <see cref="Fixed"/> value.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">format 0</see> section of the <c>kerx</c> table description.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxSubtable"/>
/// <seealso cref="KerningPair"/>
/// <seealso cref="LookupTable"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxFormat0Subtable : KerxSubtable, IDerivedRecord<KerxSubtable, KerxFormat0Subtable>
{
    /// <inheritdoc/>
    public override int Format => 0;

    /// <summary>Gets the lookup that maps each left-hand glyph to its kerning-pair list.</summary>
    /// <value>A lookup whose value for glyph <c>i</c> is a byte offset from the start of this subtable to a list of <see cref="KerningPair"/> records.</value>
    /// <remarks>A lookup value of zero means the glyph has no kerning pairs.</remarks>
    /// <seealso cref="GetPairs(int)"/>
    /// <seealso cref="PairsByOffset"/>
    /// <seealso cref="LookupTable"/>
    public required LookupTable LeftGlyphToPairList { get; init; }

    /// <summary>Gets the pair lists referenced by the lookup, keyed by their byte offset from the start of this subtable.</summary>
    /// <value>One entry per distinct referenced list. Multiple left-hand glyphs may share the same list and thus share one <see cref="KerningPair"/> array.</value>
    /// <seealso cref="GetPairs(int)"/>
    /// <seealso cref="KerningPair"/>
    public required IReadOnlyDictionary<ushort, KerningPair[]> PairsByOffset { get; init; }

    /// <summary>Gets the kerning-pair list that applies to a left-hand glyph.</summary>
    /// <param name="leftGlyph">The left-hand glyph ID.</param>
    /// <returns>The kerning-pair list for the glyph, or an empty array when the glyph's lookup value is zero, exceeds <see cref="ushort"/> range, or points to no list.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>A lookup value of zero means the glyph has no kerning pairs; the caller can iterate the returned empty array without a null check.</description></item>
    /// <item><description>The method returns an empty array rather than throwing when a malformed table contains a lookup value that is not a valid list offset.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LeftGlyphToPairList"/>
    /// <seealso cref="PairsByOffset"/>
    /// <seealso cref="KerningPair"/>
    public KerningPair[] GetPairs(int leftGlyph)
    {
        if (!LeftGlyphToPairList.TryGetValue(leftGlyph, out uint raw) || raw == 0 || raw > ushort.MaxValue)
            return [];

        return PairsByOffset.TryGetValue((ushort)raw, out var pairs) ? pairs : [];
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the subtable's common header.</param>
    /// <param name="context">A <see cref="KerxSubtable.Context"/> carrying the font's glyph count and the enclosing table's version.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="InvalidDataException"><paramref name="context"/> is not a <see cref="KerxSubtable.Context"/>.</exception>
    /// <exception cref="EndOfStreamException">A read extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The common header and the lookup are read from the front of the subtable; the pair lists are visited afterward by seeking to each distinct referenced offset.</description></item>
    /// <item><description>Each pair list is read sequentially until the sentinel; the sentinel itself is not stored.</description></item>
    /// <item><description>The parser carries the tuple count into <see cref="KerningPair.Context"/> so that variation pairs are read with the correct value width.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
    /// <seealso cref="KerxSubtable.Context"/>
    /// <seealso cref="KerningPair.Context"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    static KerxFormat0Subtable IDerivedRecord<KerxSubtable, KerxFormat0Subtable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not KerxSubtable.Context ctx)
        {
            throw new InvalidDataException(
                "'kerx' format 0 subtable requires a glyph-count and version context.");
        }

        long subtableStart = cursor.Position;
        SubtableHeader header = cursor.ReadEndianReversibleStruct<SubtableHeader>();
        LookupTable lookup = cursor.ReadRecord<LookupTable>(new LookupTable.Context(ctx.GlyphCount));

        var pairsByOffset = new Dictionary<ushort, KerningPair[]>();
        var pairContext = new KerningPair.Context(header.TupleCount);

        foreach (uint raw in DistinctReferencedOffsets(lookup, ctx.GlyphCount))
        {
            if (raw == 0 || raw > ushort.MaxValue)
                continue;

            cursor.Position = subtableStart + raw;
            pairsByOffset[(ushort)raw] = ReadPairList(ref cursor, pairContext);
        }

        return new()
        {
            Length = header.Length,
            Coverage = header.Coverage,
            TupleCount = header.TupleCount,
            LeftGlyphToPairList = lookup,
            PairsByOffset = pairsByOffset,
        };
    }

    /// <summary>Reads a kerning-pair list up to but not including its sentinel terminator.</summary>
    /// <param name="cursor">The cursor positioned at the first pair of the list.</param>
    /// <param name="context">The tuple-count context shared by every pair in the list.</param>
    /// <returns>The pair array, without the sentinel entry.</returns>
    /// <exception cref="EndOfStreamException">The list extends past the end of the table-scoped source without a sentinel.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Pairs are read sequentially; each is inspected for the <c>0xFFFF / 0xFFFF</c> sentinel that terminates the list.</description></item>
    /// <item><description>The list is returned as a new array rather than a span, because its length is not known until the sentinel is reached.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KerningPair"/>
    /// <seealso cref="KerningPair.IsEndOfList"/>
    private static KerningPair[] ReadPairList(ref Cursor cursor, KerningPair.Context context)
    {
        var pairs = new List<KerningPair>();
        while (true)
        {
            KerningPair pair = cursor.ReadRecord<KerningPair>(context);
            if (pair.IsEndOfList)
                break;
            pairs.Add(pair);
        }

        return [.. pairs];
    }

    /// <summary>Enumerates the distinct non-zero offsets referenced by a lookup's values across the font's glyph range.</summary>
    /// <param name="lookup">The lookup whose values are to be inspected.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <returns>Each distinct non-zero lookup value once, in encounter order.</returns>
    /// <remarks>The helper collapses the per-glyph lookup into the set of pair-list offsets the subtable actually contains.</remarks>
    /// <seealso cref="KerxFormat0Subtable.PairsByOffset"/>
    private static IEnumerable<uint> DistinctReferencedOffsets(LookupTable lookup, int glyphCount)
    {
        var seen = new HashSet<uint>();
        for (int glyph = 0; glyph < glyphCount; glyph++)
        {
            if (!lookup.TryGetValue(glyph, out uint raw) || raw == 0)
                continue;
            if (seen.Add(raw))
                yield return raw;
        }
    }
}

/// <summary>A single kerning pair: a value and the left/right glyph indices it applies to.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Fixed-layout when <c>tupleCount</c> is zero: an <c>Int32</c> value followed by two <c>UInt16</c> glyph indices, for 8 bytes total.</description></item>
/// <item><description>Variable-layout when <c>tupleCount</c> is non-zero: the value is replaced by a vector of <c>tupleCount</c> <c>F2Dot14</c> values, so the pair's on-disk size is <c>4 + tupleCount × 2</c> bytes.</description></item>
/// <item><description>The pair that terminates a list has both <see cref="Left"/> and <see cref="Right"/> equal to <c>0xFFFF</c>; its value field is present on disk but is not meaningful.</description></item>
/// <item><description>Because the pair's size depends on the tuple count, this type implements <see cref="IRecord{T}"/> rather than <see cref="IEndianReversibleStruct{T}"/>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxFormat0Subtable"/>
/// <seealso cref="KerxFormat4Subtable"/>
/// <seealso cref="IRecord{T}"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerningPair : IRecord<KerningPair>
{
    /// <summary>The sentinel value that marks the end of a kerning-pair list.</summary>
    /// <value>The constant <c>0xFFFF</c>.</value>
    /// <remarks>A pair whose <see cref="Left"/> and <see cref="Right"/> are both this value terminates the list and is not part of the kerning data.</remarks>
    /// <seealso cref="IsEndOfList"/>
    public const ushort EndOfList = 0xFFFF;

    /// <summary>Gets the kerning value when the subtable is not a variation subtable.</summary>
    /// <value>A signed 32-bit value in font design units. Meaningful only when <see cref="TupleValues"/> is empty.</value>
    /// <seealso cref="TupleValues"/>
    /// <seealso cref="IsVariation"/>
    public int KernValue { get; init; }

    /// <summary>Gets the tuple values when the subtable is a variation subtable.</summary>
    /// <value>One <c>F2Dot14</c> value per tuple in the enclosing subtable, in tuple order. Empty when the subtable is not a variation subtable.</value>
    /// <remarks>Each element is the raw 16-bit <c>F2Dot14</c> representation; the caller interprets it as a signed 2.14 fixed-point value.</remarks>
    /// <seealso cref="KernValue"/>
    /// <seealso cref="IsVariation"/>
    public required short[] TupleValues { get; init; }

    /// <summary>Gets the left-hand glyph index.</summary>
    /// <value>The glyph index on the left or preceding side of the pair. Value <c>0xFFFF</c> marks the end-of-list sentinel.</value>
    /// <seealso cref="Right"/>
    /// <seealso cref="IsEndOfList"/>
    public ushort Left { get; init; }

    /// <summary>Gets the right-hand glyph index.</summary>
    /// <value>The glyph index on the right or following side of the pair. Value <c>0xFFFF</c> marks the end-of-list sentinel.</value>
    /// <seealso cref="Left"/>
    /// <seealso cref="IsEndOfList"/>
    public ushort Right { get; init; }

    /// <summary>Gets a value indicating whether the pair is the end-of-list sentinel.</summary>
    /// <value><see langword="true"/> when both <see cref="Left"/> and <see cref="Right"/> are <see cref="EndOfList"/>.</value>
    /// <seealso cref="Left"/>
    /// <seealso cref="Right"/>
    public bool IsEndOfList => Left == EndOfList && Right == EndOfList;

    /// <summary>Gets a value indicating whether the pair carries tuple values rather than a single kerning value.</summary>
    /// <value><see langword="true"/> when <see cref="TupleValues"/> is non-empty.</value>
    /// <seealso cref="KernValue"/>
    /// <seealso cref="TupleValues"/>
    public bool IsVariation => TupleValues.Length > 0;

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the pair's first byte.</param>
    /// <param name="context">A <see cref="Context"/> carrying the subtable's tuple count.</param>
    /// <returns>The parsed pair.</returns>
    /// <exception cref="InvalidDataException"><paramref name="context"/> is not a <see cref="Context"/>.</exception>
    /// <exception cref="EndOfStreamException">The read extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>When the tuple count is zero, the value field is a single <c>Int32</c>; otherwise it is an array of <c>tupleCount</c> <c>Int16</c> values.</description></item>
    /// <item><description>Both <see cref="Left"/> and <see cref="Right"/> are read after the value field, regardless of which value layout was used.</description></item>
    /// <item><description>The value's byte order is <c>F2Dot14</c> when read as tuple values; the field is stored as <c>Int16</c>, which matches the F2Dot14 representation.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso cref="Context"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    static KerningPair IRecord<KerningPair>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
        {
            throw new InvalidDataException(
                "'kerx' kerning pair requires a tuple-count context.");
        }

        if (ctx.TupleCount == 0)
        {
            int kernValue = cursor.ReadInt32();
            ushort left = cursor.ReadUInt16();
            ushort right = cursor.ReadUInt16();
            return new()
            {
                KernValue = kernValue,
                TupleValues = [],
                Left = left,
                Right = right,
            };
        }

        short[] tupleValues = new short[ctx.TupleCount];
        for (int i = 0; i < tupleValues.Length; i++)
            tupleValues[i] = cursor.ReadInt16();

        ushort leftV = cursor.ReadUInt16();
        ushort rightV = cursor.ReadUInt16();
        return new()
        {
            TupleValues = tupleValues,
            Left = leftV,
            Right = rightV,
        };
    }

    /// <summary>Context for parsing a <see cref="KerningPair"/>: the enclosing subtable's tuple count.</summary>
    /// <param name="TupleCount">The subtable's <c>tupleCount</c> field. Zero for non-variation subtables; positive for variation subtables, in which case each pair carries that many <c>F2Dot14</c> values.</param>
    /// <remarks>The context is a record type so that the wrapped count cannot be confused with a bare integer at the call site.</remarks>
    /// <seealso cref="KerxSubtable.TupleCount"/>
    /// <seealso cref="KerxSubtable.Variation"/>
    public record Context(uint TupleCount);
}

/// <summary>A format 1 <c>kerx</c> subtable: a state-machine format in which glyph classes and state transitions drive the kerning value applied at each position.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Uses the extended AAT state-table header (<see cref="STXHeader"/>): a 32-bit class count, and 32-bit offsets to the class table, state array, and entry table.</description></item>
/// <item><description>The class table is an AAT <see cref="LookupTable"/> whose value for glyph <c>i</c> is that glyph's class in <c>[0, <see cref="StateSize"/>)</c>. Glyphs the lookup does not cover classify to class 0.</description></item>
/// <item><description>State-array entries are 16-bit indices into the entry table; each entry carries a next state, an action flags word, and one kerning value per class.</description></item>
/// <item><description>When <see cref="KerxSubtable.Variation"/> is set, each entry's kerning values are replaced by tuple vectors of <c>tupleCount</c> <c>F2Dot14</c> values.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">format 1</see> section of the <c>kerx</c> table description.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxSubtable"/>
/// <seealso cref="KerxStateEntry"/>
/// <seealso cref="STXHeader"/>
/// <seealso cref="LookupTable"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxFormat1Subtable : KerxSubtable, IDerivedRecord<KerxSubtable, KerxFormat1Subtable>
{
    /// <inheritdoc/>
    public override int Format => 1;

    /// <summary>Gets the class table that maps each glyph index to its kerning class.</summary>
    /// <value>An AAT lookup whose value for glyph <c>i</c> is a class code in <c>[0, <see cref="StateSize"/>)</c>. Glyphs the lookup does not cover classify to class 0.</value>
    /// <seealso cref="ClassifyGlyph(int)"/>
    /// <seealso cref="StateSize"/>
    /// <seealso cref="LookupTable"/>
    public required LookupTable ClassTable { get; init; }

    /// <summary>Gets the number of classes, equal to the number of kerning values per state entry.</summary>
    /// <value>The <c>classCount</c> field from the <see cref="STXHeader"/>. Also determines the number of columns per row of the state array.</value>
    /// <seealso cref="StateArray"/>
    /// <seealso cref="KerxStateEntry.KerningValues"/>
    public required int StateSize { get; init; }

    /// <summary>Gets the flat state array.</summary>
    /// <value>One <c>UInt16</c> index per <c>(state, class)</c> pair, laid out row-major: entry <c>state × StateSize + class</c> holds the index into <see cref="Entries"/> for that transition.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The state array length is derived from the distance between the state-header's state-array and entry-table offsets; the number of states is <see cref="StateCount"/>.</description></item>
    /// <item><description>Each <c>UInt16</c> is an index into <see cref="Entries"/>, not a byte offset; the extended state-table format replaced the classic form's byte offsets with indices.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="StateSize"/>
    /// <seealso cref="StateCount"/>
    /// <seealso cref="Step(int, int)"/>
    public required ushort[] StateArray { get; init; }

    /// <summary>Gets the entry array.</summary>
    /// <value>Each entry carries a next state, an action flags word, and one kerning value per class (or a tuple vector per class when the subtable is a variation subtable).</value>
    /// <seealso cref="KerxStateEntry"/>
    /// <seealso cref="Step(int, int)"/>
    public required KerxStateEntry[] Entries { get; init; }

    /// <summary>Gets the number of states in the state machine.</summary>
    /// <value>Derived as <see cref="StateArray"/>.Length divided by <see cref="StateSize"/>.</value>
    /// <seealso cref="StateArray"/>
    /// <seealso cref="StateSize"/>
    /// <seealso cref="Step(int, int)"/>
    public int StateCount => StateArray.Length / StateSize;

    /// <summary>Classifies a glyph against the class table.</summary>
    /// <param name="glyphId">The glyph ID to classify.</param>
    /// <returns>The class code in <c>[0, <see cref="StateSize"/>)</c> when the lookup covers the glyph; otherwise zero.</returns>
    /// <remarks>The extended state-table format uses an AAT lookup for classification, so glyphs the lookup does not cover implicitly fall into class 0.</remarks>
    /// <seealso cref="ClassTable"/>
    /// <seealso cref="StateSize"/>
    /// <seealso cref="Step(int, int)"/>
    public int ClassifyGlyph(int glyphId) => (int)ClassTable.GetValue(glyphId);

    /// <summary>Advances the state machine one step.</summary>
    /// <param name="stateIndex">The current state index in <c>[0, <see cref="StateCount"/>)</c>.</param>
    /// <param name="glyphClass">The class for the current glyph, obtained from <see cref="ClassifyGlyph(int)"/>.</param>
    /// <returns>The entry for the transition.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="stateIndex"/> or <paramref name="glyphClass"/> is out of range.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The returned entry's <see cref="KerxStateEntry.NewStateIndex"/> is the next state to feed back into the method.</description></item>
    /// <item><description>A consumer that honours <see cref="KerxStateEntry.DontAdvance"/> stays on the current glyph before consuming the next one.</description></item>
    /// <item><description>The kerning value applied at the transition is <c>entry.KerningValues[glyphClass]</c>; a consumer that uses the state machine should read that slot after each step.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ClassifyGlyph(int)"/>
    /// <seealso cref="KerxStateEntry"/>
    public KerxStateEntry Step(int stateIndex, int glyphClass)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stateIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(glyphClass);
        if (stateIndex >= StateCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stateIndex), stateIndex,
                $"State index {stateIndex} is out of range [0, {StateCount}).");
        }
        if (glyphClass >= StateSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(glyphClass), glyphClass,
                $"Glyph class {glyphClass} is out of range [0, {StateSize}).");
        }

        ushort entryIndex = StateArray[stateIndex * StateSize + glyphClass];
        return Entries[entryIndex];
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the subtable's common header.</param>
    /// <param name="context">A <see cref="KerxSubtable.Context"/> carrying the font's glyph count and the enclosing table's version.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="InvalidDataException"><paramref name="context"/> is not a <see cref="KerxSubtable.Context"/>.</exception>
    /// <exception cref="EndOfStreamException">A read extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The common header and the extended state header are read as single structs; the class table follows immediately after as an AAT lookup.</description></item>
    /// <item><description>The state array length is derived from the distance between the state-array and entry-table offsets; the number of states follows by dividing by the class count.</description></item>
    /// <item><description>Entries are variable-length when the subtable is a variation subtable: each carries either one or <c>tupleCount</c> kerning values per class. The entry count is derived from the remaining subtable bytes divided by entry size.</description></item>
    /// <item><description>Each entry is parsed via <see cref="IRecord{T}.Parse(ref Cursor, object?)"/> with the class count and tuple count threaded through its context.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
    /// <seealso cref="KerxSubtable.Context"/>
    /// <seealso cref="KerxStateEntry.Context"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    static KerxFormat1Subtable IDerivedRecord<KerxSubtable, KerxFormat1Subtable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not KerxSubtable.Context ctx)
        {
            throw new InvalidDataException(
                "'kerx' format 1 subtable requires a glyph-count and version context.");
        }

        long subtableStart = cursor.Position;
        SubtableHeader header = cursor.ReadEndianReversibleStruct<SubtableHeader>();
        long stateHeaderStart = cursor.Position;
        STXHeader state = cursor.ReadEndianReversibleStruct<STXHeader>();

        cursor.Position = stateHeaderStart + state.ClassTableOffset;
        LookupTable classTable = cursor.ReadRecord<LookupTable>(new LookupTable.Context(ctx.GlyphCount));

        int classCount = checked((int)state.ClassCount);
        cursor.Position = stateHeaderStart + state.StateArrayOffset;
        int stateArrayLength = checked((int)((state.EntryTableOffset - state.StateArrayOffset) / 2));
        ushort[] stateArray = cursor.ReadUInt16Array(stateArrayLength);

        cursor.Position = stateHeaderStart + state.EntryTableOffset;
        long subtableEnd = subtableStart + header.Length;
        long entryBytesRemaining = subtableEnd - cursor.Position;
        int entrySizeShorts = 2 + classCount * (header.TupleCount == 0 ? 1 : (int)header.TupleCount);
        int entryCount = checked((int)(entryBytesRemaining / (entrySizeShorts * 2)));

        var entryContext = new KerxStateEntry.Context(classCount, header.TupleCount);
        var entries = new KerxStateEntry[entryCount];
        for (int i = 0; i < entryCount; i++)
            entries[i] = cursor.ReadRecord<KerxStateEntry>(entryContext);

        return new()
        {
            Length = header.Length,
            Coverage = header.Coverage,
            TupleCount = header.TupleCount,
            ClassTable = classTable,
            StateSize = classCount,
            StateArray = stateArray,
            Entries = entries,
        };
    }
}

/// <summary>A single entry in a format 1 <c>kerx</c> subtable's entry table: a next-state index, an action flags word, and one kerning value per glyph class.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The entry is variable-length: its kerning-value array has one slot per class, and each slot holds either a single <c>Int16</c> value or a tuple vector of <c>tupleCount</c> <c>Int16</c> values.</description></item>
/// <item><description>Because the entry's on-disk size depends on the class and tuple counts, it implements <see cref="IRecord{T}"/> rather than <see cref="IEndianReversibleStruct{T}"/>.</description></item>
/// <item><description>The <see cref="NewStateIndex"/> is the on-disk <c>newState</c> index, which in the extended state-table format is a zero-based index rather than a byte offset.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">format 1 entry</see> description in the <c>kerx</c> table documentation.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxFormat1Subtable"/>
/// <seealso cref="IRecord{T}"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxStateEntry : IRecord<KerxStateEntry>
{
    /// <summary>Gets the next state index.</summary>
    /// <value>The state to transition to after applying the entry's kerning values. Zero-based, matching the row index into the state array.</value>
    /// <seealso cref="KerxFormat1Subtable.Step(int, int)"/>
    public int NewStateIndex { get; init; }

    /// <summary>Gets the raw action flags word.</summary>
    /// <value>The 16-bit <c>flags</c> field. Use the bit accessors for the meaningful bits.</value>
    /// <seealso cref="Push"/>
    /// <seealso cref="DontAdvance"/>
    public ushort Flags { get; init; }

    /// <summary>Gets the kerning values for this entry, one per class.</summary>
    /// <value>When the subtable is not a variation subtable, one <c>Int16</c> per class. When it is, <c>tupleCount</c> <c>Int16</c> values per class, laid out class-major.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>For a non-variation subtable the array length is <c>StateSize</c>; the value for class <c>c</c> is at index <c>c</c>.</description></item>
    /// <item><description>For a variation subtable the array length is <c>StateSize × TupleCount</c>; the vector for class <c>c</c> is <c>KerningValues[c * TupleCount .. (c + 1) * TupleCount]</c>.</description></item>
    /// <item><description>The tuple values are stored as <c>Int16</c> in the F2Dot14 wire representation; the caller interprets each as a signed 2.14 fixed-point value.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KerxFormat1Subtable.StateSize"/>
    /// <seealso cref="KerxSubtable.TupleCount"/>
    public required short[] KerningValues { get; init; }

    /// <summary>Gets a value indicating whether the kerning value for the current class is pushed.</summary>
    /// <value><see langword="true"/> when bit <c>0x8000</c> of <see cref="Flags"/> is set.</value>
    /// <remarks>A consumer that honours the flag records the kerning value at the current glyph position rather than discarding it.</remarks>
    /// <seealso cref="Flags"/>
    public bool Push => (Flags & 0x8000) != 0;

    /// <summary>Gets a value indicating whether the state machine should stay on the current glyph.</summary>
    /// <value><see langword="true"/> when bit <c>0x4000</c> of <see cref="Flags"/> is set.</value>
    /// <seealso cref="Flags"/>
    public bool DontAdvance => (Flags & 0x4000) != 0;

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the entry's first byte.</param>
    /// <param name="context">A <see cref="Context"/> carrying the class count and the enclosing subtable's tuple count.</param>
    /// <returns>The parsed entry.</returns>
    /// <exception cref="InvalidDataException"><paramref name="context"/> is not a <see cref="Context"/>.</exception>
    /// <exception cref="EndOfStreamException">The read extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The next-state and flags fields are read as two <c>UInt16</c> values; the kerning-value array follows.</description></item>
    /// <item><description>For a non-variation subtable, the array is read as <c>classCount</c> <c>Int16</c> values in one bulk call.</description></item>
    /// <item><description>For a variation subtable, the array is read as <c>classCount × tupleCount</c> <c>Int16</c> values, preserving the class-major layout so a consumer can index into it with the same convention the specification uses.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso cref="Context"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    static KerxStateEntry IRecord<KerxStateEntry>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
        {
            throw new InvalidDataException(
                "'kerx' state entry requires a class-count and tuple-count context.");
        }

        ushort newState = cursor.ReadUInt16();
        ushort flags = cursor.ReadUInt16();

        int valueCount = ctx.ClassCount * (ctx.TupleCount == 0 ? 1 : (int)ctx.TupleCount);
        short[] kerningValues = cursor.ReadInt16Array(valueCount);

        return new()
        {
            NewStateIndex = newState,
            Flags = flags,
            KerningValues = kerningValues,
        };
    }

    /// <summary>Context for parsing a <see cref="KerxStateEntry"/>: the class count and the enclosing subtable's tuple count.</summary>
    /// <param name="ClassCount">The number of classes in the enclosing subtable's state machine.</param>
    /// <param name="TupleCount">The enclosing subtable's <c>tupleCount</c>. Zero for non-variation subtables; positive for variation subtables.</param>
    /// <remarks>The context is a record type so that the two values cannot be confused with bare integers at the call site.</remarks>
    /// <seealso cref="KerxFormat1Subtable.StateSize"/>
    /// <seealso cref="KerxSubtable.TupleCount"/>
    public record Context(int ClassCount, uint TupleCount);
}

/// <summary>A format 2 <c>kerx</c> subtable: a two-dimensional array of kerning values indexed by the glyph-class pair.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Two AAT lookups — one for the left glyph and one for the right — map each glyph to a class; the pair of classes indexes the value array.</description></item>
/// <item><description>The array is a byte buffer at <c>array</c> with <see cref="RowWidthInBytes"/> bytes per left-class row; the element for <c>(leftClass, rightClass)</c> is at <c>array + leftClass × RowWidthInBytes + rightClass × elementSize</c>.</description></item>
/// <item><description>The element size is two bytes for a non-variation subtable, and <c>2 × tupleCount</c> bytes for a variation subtable.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">format 2</see> section of the <c>kerx</c> table description.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxSubtable"/>
/// <seealso cref="LookupTable"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxFormat2Subtable : KerxSubtable, IDerivedRecord<KerxSubtable, KerxFormat2Subtable>
{
    /// <inheritdoc/>
    public override int Format => 2;

    /// <summary>Gets the lookup that maps each left-hand glyph to its class.</summary>
    /// <value>An AAT lookup whose value for glyph <c>i</c> is the row index of that glyph's class in the value array. Glyphs the lookup does not cover classify to class 0.</value>
    /// <seealso cref="RightClassTable"/>
    /// <seealso cref="GetKernValue(int, int)"/>
    public required LookupTable LeftClassTable { get; init; }

    /// <summary>Gets the lookup that maps each right-hand glyph to its class.</summary>
    /// <value>An AAT lookup whose value for glyph <c>i</c> is the column index of that glyph's class in the value array. Glyphs the lookup does not cover classify to class 0.</value>
    /// <seealso cref="LeftClassTable"/>
    /// <seealso cref="GetKernValue(int, int)"/>
    public required LookupTable RightClassTable { get; init; }

    /// <summary>Gets the flattened kerning value array, in row-major order.</summary>
    /// <value>An <c>Int16</c> array of length <c>RowWidthInBytes / 2 × rowCount</c>, laid out with one row of <c>RowWidthInBytes / 2</c> shorts per left class.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>For a non-variation subtable, each element is a single <c>Int16</c> value.</description></item>
    /// <item><description>For a variation subtable, each element is a contiguous vector of <c>TupleCount</c> <c>Int16</c> values; the vector for <c>(leftClass, rightClass)</c> begins at the flat index <c>leftClass × rowWidthShorts + rightClass × TupleCount</c>.</description></item>
    /// <item><description>The wire representation of each value is F2Dot14 for a variation subtable and a 16.16-derived signed value for a non-variation subtable; the field is stored as <c>Int16</c> in both cases.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="RowWidthInBytes"/>
    /// <seealso cref="GetKernValue(int, int)"/>
    /// <seealso cref="GetTupleValues(int, int)"/>
    public required short[] KerningValues { get; init; }

    /// <summary>Gets the number of bytes in each row of the value array.</summary>
    /// <value>The on-disk <c>rowWidth</c> field. The row for left class <c>L</c> begins at flat short index <c>L × RowWidthInBytes / 2</c>.</value>
    /// <seealso cref="KerningValues"/>
    public uint RowWidthInBytes { get; init; }

    /// <summary>Gets the number of glyphs in the font, as supplied by the parse context.</summary>
    /// <value>The font's <c>maxp.numGlyphs</c> value. Stored so that the classification helpers can validate glyph indices.</value>
    /// <seealso cref="KerxSubtable.Context.GlyphCount"/>
    public int GlyphCount { get; init; }

    /// <summary>Gets the number of <c>Int16</c> slots per value array element.</summary>
    /// <value><c>1</c> when the subtable is not a variation subtable; <c>TupleCount</c> when it is.</value>
    /// <remarks>Convenience for callers that index into <see cref="KerningValues"/> directly rather than through the helpers.</remarks>
    /// <seealso cref="KerningValues"/>
    /// <seealso cref="KerxSubtable.TupleCount"/>
    public int ElementSizeInShorts => TupleCount == 0 ? 1 : checked((int)TupleCount);

    /// <summary>Gets the kerning value that applies to a glyph pair.</summary>
    /// <param name="leftGlyph">The left-hand glyph ID.</param>
    /// <param name="rightGlyph">The right-hand glyph ID.</param>
    /// <returns>The kerning value in font design units, or zero when the pair's classes select an entry outside the array bounds.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The method classifies both glyphs, then indexes the array; the classes are bounded by <see cref="RowWidthInBytes"/> and the array length, so a malformed table yields zero rather than an out-of-range access.</description></item>
    /// <item><description>The method is meaningful only for non-variation subtables; for a variation subtable use <see cref="GetTupleValues(int, int)"/> instead.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetTupleValues(int, int)"/>
    /// <seealso cref="LeftClassTable"/>
    /// <seealso cref="RightClassTable"/>
    public int GetKernValue(int leftGlyph, int rightGlyph)
    {
        if (!TryGetValueIndex(leftGlyph, rightGlyph, out int index))
            return 0;

        return KerningValues[index];
    }

    /// <summary>Gets the tuple vector that applies to a glyph pair.</summary>
    /// <param name="leftGlyph">The left-hand glyph ID.</param>
    /// <param name="rightGlyph">The right-hand glyph ID.</param>
    /// <returns>The vector of <see cref="KerxSubtable.TupleCount"/> <c>Int16</c> values, or an empty array when the pair's classes select an entry outside the array bounds.</returns>
    /// <remarks>The method is meaningful only for variation subtables; for a non-variation subtable use <see cref="GetKernValue(int, int)"/> instead.</remarks>
    /// <seealso cref="GetKernValue(int, int)"/>
    /// <seealso cref="KerxSubtable.TupleCount"/>
    public short[] GetTupleValues(int leftGlyph, int rightGlyph)
    {
        if (!TryGetValueIndex(leftGlyph, rightGlyph, out int index))
            return [];

        int size = ElementSizeInShorts;
        if (index + size > KerningValues.Length)
            return [];

        return KerningValues.AsSpan(index, size).ToArray();
    }

    /// <summary>Computes the flat short index for a glyph pair's value array element.</summary>
    /// <param name="leftGlyph">The left-hand glyph ID.</param>
    /// <param name="rightGlyph">The right-hand glyph ID.</param>
    /// <param name="index">When this method returns <see langword="true"/>, contains the flat index into <see cref="KerningValues"/>.</param>
    /// <returns><see langword="true"/> when the index is within the array bounds; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The left and right glyphs are classified through the class tables; glyphs the tables do not cover classify to class 0.</description></item>
    /// <item><description>The row stride is <see cref="RowWidthInBytes"/> converted to shorts; the column stride is <see cref="ElementSizeInShorts"/>.</description></item>
    /// <item><description>When the resulting index would fall outside the array, the method returns <see langword="false"/> rather than throwing; the callers translate that to a zero or empty result.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LeftClassTable"/>
    /// <seealso cref="RightClassTable"/>
    private bool TryGetValueIndex(int leftGlyph, int rightGlyph, out int index)
    {
        int leftClass = (int)LeftClassTable.GetValue(leftGlyph);
        int rightClass = (int)RightClassTable.GetValue(rightGlyph);

        int rowWidthShorts = checked((int)(RowWidthInBytes / 2));
        index = leftClass * rowWidthShorts + rightClass * ElementSizeInShorts;

        return index >= 0 && index < KerningValues.Length;
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the subtable's common header.</param>
    /// <param name="context">A <see cref="KerxSubtable.Context"/> carrying the font's glyph count and the enclosing table's version.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="InvalidDataException"><paramref name="context"/> is not a <see cref="KerxSubtable.Context"/>.</exception>
    /// <exception cref="EndOfStreamException">A read extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The common header and the three subtable offsets are read as single structs; the class lookups are parsed against the font's glyph count.</description></item>
    /// <item><description>The value array is read as a single flat <c>Int16</c> buffer sized by the distance from the array offset to the subtable end.</description></item>
    /// <item><description>The row width and the tuple count together define the array's row-major layout; both are stored on the record so that <see cref="GetKernValue(int, int)"/> and <see cref="GetTupleValues(int, int)"/> can index into the flat buffer without re-deriving them.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
    /// <seealso cref="KerxSubtable.Context"/>
    /// <seealso cref="LookupTable.Context"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    static KerxFormat2Subtable IDerivedRecord<KerxSubtable, KerxFormat2Subtable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not KerxSubtable.Context ctx)
        {
            throw new InvalidDataException(
                "'kerx' format 2 subtable requires a glyph-count and version context.");
        }

        long subtableStart = cursor.Position;
        SubtableHeader header = cursor.ReadEndianReversibleStruct<SubtableHeader>();
        Format2Header format = cursor.ReadEndianReversibleStruct<Format2Header>();

        cursor.Position = subtableStart + format.LeftClassTable;
        LookupTable leftClasses = cursor.ReadRecord<LookupTable>(new LookupTable.Context(ctx.GlyphCount));

        cursor.Position = subtableStart + format.RightClassTable;
        LookupTable rightClasses = cursor.ReadRecord<LookupTable>(new LookupTable.Context(ctx.GlyphCount));

        long arrayStart = subtableStart + format.Array;
        long subtableEnd = subtableStart + header.Length;
        int arrayShortCount = checked((int)((subtableEnd - arrayStart) / 2));

        cursor.Position = arrayStart;
        short[] kerningValues = cursor.ReadInt16Array(arrayShortCount);

        return new()
        {
            Length = header.Length,
            Coverage = header.Coverage,
            TupleCount = header.TupleCount,
            LeftClassTable = leftClasses,
            RightClassTable = rightClasses,
            KerningValues = kerningValues,
            RowWidthInBytes = format.RowWidth,
            GlyphCount = ctx.GlyphCount,
        };
    }

    /// <summary>The 16-byte format 2 subtable body that follows the common header: row width and three offsets.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All four fields are <c>UInt32</c>, so the reversal is a single pass over the struct reinterpreted as four 32-bit words.</description></item>
    /// <item><description>The three offsets are relative to the start of the subtable, not to this struct.</description></item>
    /// <item><description>Consumed at parse time and not retained on <see cref="KerxFormat2Subtable"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KerxFormat2Subtable"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal record struct Format2Header : IEndianReversibleStruct<Format2Header>
    {
        /// <summary>The number of bytes in each row of the value array at byte offset 0.</summary>
        /// <value>The byte stride between consecutive left-class rows.</value>
        /// <seealso cref="KerxFormat2Subtable.RowWidthInBytes"/>
        public uint RowWidth;         // +0

        /// <summary>Subtable-relative byte offset of the left-class lookup at byte offset 4.</summary>
        /// <value>The offset of the <see cref="LookupTable"/> that maps left-hand glyphs to class codes.</value>
        /// <seealso cref="KerxFormat2Subtable.LeftClassTable"/>
        public uint LeftClassTable;   // +4

        /// <summary>Subtable-relative byte offset of the right-class lookup at byte offset 8.</summary>
        /// <value>The offset of the <see cref="LookupTable"/> that maps right-hand glyphs to class codes.</value>
        /// <seealso cref="KerxFormat2Subtable.RightClassTable"/>
        public uint RightClassTable;  // +8

        /// <summary>Subtable-relative byte offset of the kerning value array at byte offset 12.</summary>
        /// <value>The offset of the flattened <c>Int16</c> array that holds the pairwise kerning values.</value>
        /// <seealso cref="KerxFormat2Subtable.KerningValues"/>
        public uint Array;            // +12

        /// <summary>Reverses the byte order of all four 32-bit fields in one pass.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with all four fields reversed.</returns>
        /// <remarks>The header is entirely <c>UInt32</c> fields, so it is reinterpreted as a span of four 32-bit words and reversed with a single <see cref="BinaryPrimitives.ReverseEndianness(ReadOnlySpan{uint}, Span{uint})"/> call.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static Format2Header ReverseEndianness(Format2Header value)
        {
            Span<uint> words = MemoryMarshal.CreateSpan(ref Unsafe.As<Format2Header, uint>(ref value), 4);
            BinaryPrimitives.ReverseEndianness(words, words);
            return value;
        }
    }
}

/// <summary>A format 4 <c>kerx</c> subtable: a state-machine format in which each transition selects a kerning value by index into a separate value array, rather than storing a value per class inline.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Uses the extended AAT state-table header (<see cref="STXHeader"/>): a 32-bit class count, and 32-bit offsets to the class table, state array, and entry table.</description></item>
/// <item><description>Unlike format 1, each state-entry slot holds a 16-bit index into a separate kerning value table; the value table itself is the last section of the subtable.</description></item>
/// <item><description>An index of <c>0xFFFF</c> means the slot has no kerning; otherwise the index selects an entry in <see cref="KernValues"/>.</description></item>
/// <item><description>When <see cref="KerxSubtable.Variation"/> is set, each value-array element is a tuple vector of <c>tupleCount</c> <c>F2Dot14</c> values rather than a single kerning value.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">format 4</see> section of the <c>kerx</c> table description.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxSubtable"/>
/// <seealso cref="KerxFormat4StateEntry"/>
/// <seealso cref="STXHeader"/>
/// <seealso cref="LookupTable"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxFormat4Subtable : KerxSubtable, IDerivedRecord<KerxSubtable, KerxFormat4Subtable>
{
    /// <inheritdoc/>
    public override int Format => 4;

    /// <summary>Gets the class table that maps each glyph index to its kerning class.</summary>
    /// <value>An AAT lookup whose value for glyph <c>i</c> is a class code in <c>[0, <see cref="StateSize"/>)</c>. Glyphs the lookup does not cover classify to class 0.</value>
    /// <seealso cref="ClassifyGlyph(int)"/>
    /// <seealso cref="StateSize"/>
    public required LookupTable ClassTable { get; init; }

    /// <summary>Gets the number of classes.</summary>
    /// <value>The <c>classCount</c> field from the <see cref="STXHeader"/>. Also determines the number of index slots per state entry.</value>
    /// <seealso cref="StateArray"/>
    /// <seealso cref="KerxFormat4StateEntry.KerningIndices"/>
    public required int StateSize { get; init; }

    /// <summary>Gets the flat state array.</summary>
    /// <value>One <c>UInt16</c> index per <c>(state, class)</c> pair, laid out row-major: entry <c>state × StateSize + class</c> holds the index into <see cref="Entries"/> for that transition.</value>
    /// <seealso cref="StateSize"/>
    /// <seealso cref="StateCount"/>
    /// <seealso cref="Step(int, int)"/>
    public required ushort[] StateArray { get; init; }

    /// <summary>Gets the entry array.</summary>
    /// <value>Each entry carries a next state, an action flags word, and one kerning-index slot per class.</value>
    /// <seealso cref="KerxFormat4StateEntry"/>
    /// <seealso cref="Step(int, int)"/>
    public required KerxFormat4StateEntry[] Entries { get; init; }

    /// <summary>Gets the shared kerning value array.</summary>
    /// <value>The flat <c>Int16</c> array at the end of the subtable. Each state entry's per-class index selects an element (or a tuple vector) from this array.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>For a non-variation subtable, each element is a single <c>Int16</c> value; the array index is the value the entry slot refers to directly.</description></item>
    /// <item><description>For a variation subtable, each element is a contiguous vector of <c>TupleCount</c> <c>Int16</c> values; the flat index of the vector for logical index <c>k</c> is <c>k × TupleCount</c>.</description></item>
    /// <item><description>The wire representation of each element is the F2Dot14 encoding of a signed value, or a plain <c>Int16</c> in the non-variation case.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetKernValue"/>
    /// <seealso cref="GetTupleValues"/>
    /// <seealso cref="KerxFormat4StateEntry"/>
    public required short[] KernValues { get; init; }

    /// <summary>Gets the number of states in the state machine.</summary>
    /// <value>Derived as <see cref="StateArray"/>.Length divided by <see cref="StateSize"/>.</value>
    /// <seealso cref="StateArray"/>
    /// <seealso cref="StateSize"/>
    /// <seealso cref="Step(int, int)"/>
    public int StateCount => StateArray.Length / StateSize;

    /// <summary>Classifies a glyph against the class table.</summary>
    /// <param name="glyphId">The glyph ID to classify.</param>
    /// <returns>The class code in <c>[0, <see cref="StateSize"/>)</c> when the lookup covers the glyph; otherwise zero.</returns>
    /// <seealso cref="ClassTable"/>
    /// <seealso cref="Step(int, int)"/>
    public int ClassifyGlyph(int glyphId) => (int)ClassTable.GetValue(glyphId);

    /// <summary>Advances the state machine one step.</summary>
    /// <param name="stateIndex">The current state index in <c>[0, <see cref="StateCount"/>)</c>.</param>
    /// <param name="glyphClass">The class for the current glyph, obtained from <see cref="ClassifyGlyph(int)"/>.</param>
    /// <returns>The entry for the transition.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="stateIndex"/> or <paramref name="glyphClass"/> is out of range.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The returned entry's <see cref="KerxFormat4StateEntry.NewStateIndex"/> is the next state to feed back into the method.</description></item>
    /// <item><description>The kerning index applied at the transition is <c>entry.KerningIndices[glyphClass]</c>; the value at that index in <see cref="KernValues"/> is the amount to apply. An index of <c>0xFFFF</c> means no kerning.</description></item>
    /// <item><description>A consumer that honours <see cref="KerxFormat4StateEntry.DontAdvance"/> stays on the current glyph before consuming the next one.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ClassifyGlyph(int)"/>
    /// <seealso cref="KerxFormat4StateEntry"/>
    /// <seealso cref="KernValues"/>
    public KerxFormat4StateEntry Step(int stateIndex, int glyphClass)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stateIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(glyphClass);
        if (stateIndex >= StateCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stateIndex), stateIndex,
                $"State index {stateIndex} is out of range [0, {StateCount}).");
        }
        if (glyphClass >= StateSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(glyphClass), glyphClass,
                $"Glyph class {glyphClass} is out of range [0, {StateSize}).");
        }

        ushort entryIndex = StateArray[stateIndex * StateSize + glyphClass];
        return Entries[entryIndex];
    }

    /// <summary>Gets the kerning value that applies at a transition to the current glyph class.</summary>
    /// <param name="entry">The state entry the machine just stepped into.</param>
    /// <param name="glyphClass">The class of the current glyph.</param>
    /// <returns>The kerning value in font design units, or zero when the slot has no kerning.</returns>
    /// <remarks>The method resolves the entry's per-class index, verifies it against the value array, and returns the value at that position.</remarks>
    /// <seealso cref="GetTupleValues(KerxFormat4StateEntry, int)"/>
    /// <seealso cref="KerxFormat4StateEntry.KerningIndices"/>
    /// <seealso cref="KernValues"/>
    public int GetKernValue(KerxFormat4StateEntry entry, int glyphClass)
    {
        if (!TryGetValueIndex(entry, glyphClass, out int index))
            return 0;

        return KernValues[index];
    }

    /// <summary>Gets the tuple vector that applies at a transition to the current glyph class.</summary>
    /// <param name="entry">The state entry the machine just stepped into.</param>
    /// <param name="glyphClass">The class of the current glyph.</param>
    /// <returns>The vector of <see cref="KerxSubtable.TupleCount"/> <c>Int16</c> values, or an empty array when the slot has no kerning or when the value lies outside the array.</returns>
    /// <remarks>The method is meaningful only for variation subtables; for a non-variation subtable use <see cref="GetKernValue(KerxFormat4StateEntry, int)"/> instead.</remarks>
    /// <seealso cref="GetKernValue(KerxFormat4StateEntry, int)"/>
    /// <seealso cref="KerxSubtable.TupleCount"/>
    public short[] GetTupleValues(KerxFormat4StateEntry entry, int glyphClass)
    {
        if (!TryGetValueIndex(entry, glyphClass, out int flatIndex))
            return [];

        int size = TupleCount == 0 ? 1 : checked((int)TupleCount);
        if (flatIndex + size > KernValues.Length)
            return [];

        return KernValues.AsSpan(flatIndex, size).ToArray();
    }

    /// <summary>Resolves the flat value-array index that a state entry and glyph class select.</summary>
    /// <param name="entry">The state entry the machine just stepped into.</param>
    /// <param name="glyphClass">The class of the current glyph.</param>
    /// <param name="index">When this method returns <see langword="true"/>, contains the flat index into <see cref="KernValues"/>.</param>
    /// <returns><see langword="true"/> when the entry has a kerning index at <paramref name="glyphClass"/> that lies within <see cref="KernValues"/>; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>An entry index of <c>0xFFFF</c> signals no kerning, and the method returns <see langword="false"/> without consulting the array.</description></item>
    /// <item><description>For a variation subtable the entry index is a logical value index; the method converts it to a flat index by multiplying by <see cref="KerxSubtable.TupleCount"/>.</description></item>
    /// <item><description>The result is checked against <see cref="KernValues"/>'s length so a malformed table yields no kerning rather than an out-of-range access.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetKernValue(KerxFormat4StateEntry, int)"/>
    /// <seealso cref="GetTupleValues(KerxFormat4StateEntry, int)"/>
    private bool TryGetValueIndex(KerxFormat4StateEntry entry, int glyphClass, out int index)
    {
        index = 0;
        if ((uint)glyphClass >= (uint)entry.KerningIndices.Length)
            return false;

        ushort logicalIndex = entry.KerningIndices[glyphClass];
        if (logicalIndex == KerxFormat4StateEntry.NoKerning)
            return false;

        int elementSize = TupleCount == 0 ? 1 : checked((int)TupleCount);
        index = logicalIndex * elementSize;

        return index >= 0 && index < KernValues.Length;
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the subtable's common header.</param>
    /// <param name="context">A <see cref="KerxSubtable.Context"/> carrying the font's glyph count and the enclosing table's version.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="InvalidDataException"><paramref name="context"/> is not a <see cref="KerxSubtable.Context"/>.</exception>
    /// <exception cref="EndOfStreamException">A read extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The common header and the extended state header are read as single structs; the class table follows as an AAT lookup.</description></item>
    /// <item><description>The state array length is derived from the distance between the state-array and entry-table offsets; the number of states follows by dividing by the class count.</description></item>
    /// <item><description>Entries are variable-length when the subtable is a variation subtable: each carries one index per class regardless of tuple count, so the entry size is fixed at <c>2 + 2 + classCount × 2</c> bytes.</description></item>
    /// <item><description>The kerning value array follows the entry table and extends to the end of the subtable; the array length depends on the tuple count when the subtable is a variation subtable.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
    /// <seealso cref="KerxSubtable.Context"/>
    /// <seealso cref="KerxFormat4StateEntry.Context"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    static KerxFormat4Subtable IDerivedRecord<KerxSubtable, KerxFormat4Subtable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not KerxSubtable.Context ctx)
        {
            throw new InvalidDataException(
                "'kerx' format 4 subtable requires a glyph-count and version context.");
        }

        long subtableStart = cursor.Position;
        SubtableHeader header = cursor.ReadEndianReversibleStruct<SubtableHeader>();
        long stateHeaderStart = cursor.Position;
        STXHeader state = cursor.ReadEndianReversibleStruct<STXHeader>();

        cursor.Position = stateHeaderStart + state.ClassTableOffset;
        LookupTable classTable = cursor.ReadRecord<LookupTable>(new LookupTable.Context(ctx.GlyphCount));

        int classCount = checked((int)state.ClassCount);
        cursor.Position = stateHeaderStart + state.StateArrayOffset;
        int stateArrayLength = checked((int)((state.EntryTableOffset - state.StateArrayOffset) / 2));
        ushort[] stateArray = cursor.ReadUInt16Array(stateArrayLength);

        cursor.Position = stateHeaderStart + state.EntryTableOffset;
        long subtableEnd = subtableStart + header.Length;

        int entrySizeBytes = 2 + 2 + classCount * 2;
        long entryTableEnd = subtableEnd;
        long entryTableStart = cursor.Position;
        int entryCount = checked((int)((entryTableEnd - entryTableStart) / entrySizeBytes));

        var entryContext = new KerxFormat4StateEntry.Context(classCount);
        var entries = new KerxFormat4StateEntry[entryCount];
        for (int i = 0; i < entries.Length; i++)
            entries[i] = cursor.ReadRecord<KerxFormat4StateEntry>(entryContext);

        long valueArrayStart = cursor.Position;
        int valueArrayShortCount = checked((int)((subtableEnd - valueArrayStart) / 2));
        short[] kernValues = cursor.ReadInt16Array(valueArrayShortCount);

        return new()
        {
            Length = header.Length,
            Coverage = header.Coverage,
            TupleCount = header.TupleCount,
            ClassTable = classTable,
            StateSize = classCount,
            StateArray = stateArray,
            Entries = entries,
            KernValues = kernValues,
        };
    }
}

/// <summary>A single entry in a format 4 <c>kerx</c> subtable's entry table: a next-state index, an action flags word, and one kerning-index slot per glyph class.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Unlike <see cref="KerxStateEntry"/>, the per-class slots hold indices into a shared value array rather than the kerning values themselves. This lets many transitions share one value and shortens the entry table.</description></item>
/// <item><description>The entry's on-disk size is <c>4 + classCount × 2</c> bytes, independent of the subtable's tuple count.</description></item>
/// <item><description>A kerning index of <see cref="NoKerning"/> (<c>0xFFFF</c>) means the slot has no kerning; a consumer that steps the machine skips those transitions.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">format 4 entry</see> description in the <c>kerx</c> table documentation.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxFormat4Subtable"/>
/// <seealso cref="IRecord{T}"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxFormat4StateEntry : IRecord<KerxFormat4StateEntry>
{
    /// <summary>The index value that marks a slot with no kerning.</summary>
    /// <value>The constant <c>0xFFFF</c>.</value>
    /// <remarks>When a per-class slot equals this value, the transition has no kerning and no value is applied.</remarks>
    /// <seealso cref="KerningIndices"/>
    public const ushort NoKerning = 0xFFFF;

    /// <summary>Gets the next state index.</summary>
    /// <value>The state to transition to after applying the entry's kerning indices. Zero-based, matching the row index into the state array.</value>
    /// <seealso cref="KerxFormat4Subtable.Step(int, int)"/>
    public int NewStateIndex { get; init; }

    /// <summary>Gets the raw action flags word.</summary>
    /// <value>The 16-bit <c>flags</c> field. Use the bit accessors for the meaningful bits.</value>
    /// <seealso cref="Push"/>
    /// <seealso cref="DontAdvance"/>
    public ushort Flags { get; init; }

    /// <summary>Gets the kerning-value index for each class.</summary>
    /// <value>One <c>UInt16</c> per class. The value selects an element of the enclosing subtable's kerning value array; <see cref="NoKerning"/> means no kerning.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The array length is <c>StateSize</c>; the slot for class <c>c</c> is at index <c>c</c>.</description></item>
    /// <item><description>When the enclosing subtable is a variation subtable, the index refers to a logical element whose on-disk size is <c>tupleCount</c> shorts; the resolver multiplies through accordingly.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KerxFormat4Subtable.StateSize"/>
    /// <seealso cref="NoKerning"/>
    /// <seealso cref="KerxFormat4Subtable.KernValues"/>
    public required ushort[] KerningIndices { get; init; }

    /// <summary>Gets a value indicating whether the kerning value for the current class is pushed.</summary>
    /// <value><see langword="true"/> when bit <c>0x8000</c> of <see cref="Flags"/> is set.</value>
    /// <seealso cref="Flags"/>
    public bool Push => (Flags & 0x8000) != 0;

    /// <summary>Gets a value indicating whether the state machine should stay on the current glyph.</summary>
    /// <value><see langword="true"/> when bit <c>0x4000</c> of <see cref="Flags"/> is set.</value>
    /// <seealso cref="Flags"/>
    public bool DontAdvance => (Flags & 0x4000) != 0;

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the entry's first byte.</param>
    /// <param name="context">A <see cref="Context"/> carrying the class count.</param>
    /// <returns>The parsed entry.</returns>
    /// <exception cref="InvalidDataException"><paramref name="context"/> is not a <see cref="Context"/>.</exception>
    /// <exception cref="EndOfStreamException">The read extends past the end of the table-scoped source.</exception>
    /// <remarks>The next-state and flags fields are read as two <c>UInt16</c> values; the kerning-index array follows as a single bulk call.</remarks>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso cref="Context"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    static KerxFormat4StateEntry IRecord<KerxFormat4StateEntry>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
        {
            throw new InvalidDataException(
                "'kerx' format 4 state entry requires a class-count context.");
        }

        ushort newState = cursor.ReadUInt16();
        ushort flags = cursor.ReadUInt16();
        ushort[] indices = cursor.ReadUInt16Array(ctx.ClassCount);

        return new()
        {
            NewStateIndex = newState,
            Flags = flags,
            KerningIndices = indices,
        };
    }

    /// <summary>Context for parsing a <see cref="KerxFormat4StateEntry"/>: the class count.</summary>
    /// <param name="ClassCount">The number of classes in the enclosing subtable's state machine; equals the length of <see cref="KerningIndices"/>.</param>
    /// <remarks>The context is a record type so that the wrapped count cannot be confused with a bare integer at the call site.</remarks>
    /// <seealso cref="KerxFormat4Subtable.StateSize"/>
    public record Context(int ClassCount);
}

/// <summary>A format 6 <c>kerx</c> subtable: a two-dimensional array indexed by per-glyph row and column indices.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Like format 2, but the row and column selectors are per-glyph indices in a trimmed range rather than class codes in a dense space.</description></item>
/// <item><description>Two AAT lookups, one for the left glyph and one for the right, map each glyph to its row and column. Glyphs the lookups do not cover select row or column zero.</description></item>
/// <item><description>The value array is row-major with <see cref="Format6Header.ColumnCount"/> entries per row; the element for <c>(leftRow, rightColumn)</c> is at flat index <c>leftRow × ColumnCount + rightColumn</c>.</description></item>
/// <item><description>The element size is two bytes for a non-variation subtable, and <c>2 × tupleCount</c> bytes for a variation subtable.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">format 6</see> section of the <c>kerx</c> table description.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxSubtable"/>
/// <seealso cref="LookupTable"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxFormat6Subtable : KerxSubtable, IDerivedRecord<KerxSubtable, KerxFormat6Subtable>
{
    /// <inheritdoc/>
    public override int Format => 6;

    /// <summary>Gets the lookup that maps each left-hand glyph to its row index.</summary>
    /// <value>An AAT lookup whose value for glyph <c>i</c> is the row index of that glyph in the value array. Glyphs the lookup does not cover select row zero.</value>
    /// <seealso cref="ColumnIndexTable"/>
    /// <seealso cref="GetKernValue(int, int)"/>
    public required LookupTable RowIndexTable { get; init; }

    /// <summary>Gets the lookup that maps each right-hand glyph to its column index.</summary>
    /// <value>An AAT lookup whose value for glyph <c>i</c> is the column index of that glyph in the value array. Glyphs the lookup does not cover select column zero.</value>
    /// <seealso cref="RowIndexTable"/>
    /// <seealso cref="GetKernValue(int, int)"/>
    public required LookupTable ColumnIndexTable { get; init; }

    /// <summary>Gets the number of rows in the value array.</summary>
    /// <value>The <c>rowCount</c> field from the format 6 body header. Bounds the row index selected by <see cref="RowIndexTable"/>.</value>
    /// <seealso cref="ColumnCount"/>
    /// <seealso cref="KerningValues"/>
    public int RowCount { get; init; }

    /// <summary>Gets the number of columns per row in the value array.</summary>
    /// <value>The <c>columnCount</c> field from the format 6 body header. Also the row stride when indexing <see cref="KerningValues"/>.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>For a non-variation subtable the on-disk row stride is <c>ColumnCount × 2</c> bytes.</description></item>
    /// <item><description>For a variation subtable the on-disk row stride is <c>ColumnCount × TupleCount × 2</c> bytes.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="RowCount"/>
    /// <seealso cref="KerningValues"/>
    /// <seealso cref="ElementSizeInShorts"/>
    public int ColumnCount { get; init; }

    /// <summary>Gets the flattened kerning value array, in row-major order.</summary>
    /// <value>An <c>Int16</c> array of length <c>RowCount × ColumnCount × ElementSizeInShorts</c>.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>For a non-variation subtable, each element is a single <c>Int16</c> value.</description></item>
    /// <item><description>For a variation subtable, each element is a contiguous vector of <c>TupleCount</c> <c>Int16</c> values; the vector for <c>(leftRow, rightColumn)</c> begins at the flat index <c>(leftRow × ColumnCount + rightColumn) × TupleCount</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="RowCount"/>
    /// <seealso cref="ColumnCount"/>
    /// <seealso cref="GetKernValue(int, int)"/>
    /// <seealso cref="GetTupleValues(int, int)"/>
    public required short[] KerningValues { get; init; }

    /// <summary>Gets the number of <c>Int16</c> slots per value array element.</summary>
    /// <value><c>1</c> when the subtable is not a variation subtable; <c>TupleCount</c> when it is.</value>
    /// <seealso cref="KerningValues"/>
    /// <seealso cref="KerxSubtable.TupleCount"/>
    public int ElementSizeInShorts => TupleCount == 0 ? 1 : checked((int)TupleCount);

    /// <summary>Gets the kerning value that applies to a glyph pair.</summary>
    /// <param name="leftGlyph">The left-hand glyph ID.</param>
    /// <param name="rightGlyph">The right-hand glyph ID.</param>
    /// <returns>The kerning value in font design units, or zero when the pair's indices select an entry outside the array bounds.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The method resolves both row and column indices through <see cref="RowIndexTable"/> and <see cref="ColumnIndexTable"/>, then indexes the array; out-of-range selections yield zero rather than an exception.</description></item>
    /// <item><description>The method is meaningful only for non-variation subtables; for a variation subtable use <see cref="GetTupleValues(int, int)"/> instead.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetTupleValues(int, int)"/>
    /// <seealso cref="RowIndexTable"/>
    /// <seealso cref="ColumnIndexTable"/>
    public int GetKernValue(int leftGlyph, int rightGlyph)
    {
        if (!TryGetValueIndex(leftGlyph, rightGlyph, out int index))
            return 0;

        return KerningValues[index];
    }

    /// <summary>Gets the tuple vector that applies to a glyph pair.</summary>
    /// <param name="leftGlyph">The left-hand glyph ID.</param>
    /// <param name="rightGlyph">The right-hand glyph ID.</param>
    /// <returns>The vector of <see cref="KerxSubtable.TupleCount"/> <c>Int16</c> values, or an empty array when the pair's indices select an entry outside the array bounds.</returns>
    /// <remarks>The method is meaningful only for variation subtables; for a non-variation subtable use <see cref="GetKernValue(int, int)"/> instead.</remarks>
    /// <seealso cref="GetKernValue(int, int)"/>
    /// <seealso cref="KerxSubtable.TupleCount"/>
    public short[] GetTupleValues(int leftGlyph, int rightGlyph)
    {
        if (!TryGetValueIndex(leftGlyph, rightGlyph, out int index))
            return [];

        int size = ElementSizeInShorts;
        if (index + size > KerningValues.Length)
            return [];

        return KerningValues.AsSpan(index, size).ToArray();
    }

    /// <summary>Computes the flat short index for a glyph pair's value array element.</summary>
    /// <param name="leftGlyph">The left-hand glyph ID.</param>
    /// <param name="rightGlyph">The right-hand glyph ID.</param>
    /// <param name="index">When this method returns <see langword="true"/>, contains the flat index into <see cref="KerningValues"/>.</param>
    /// <returns><see langword="true"/> when the index is within the array bounds; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The left and right glyphs are resolved through the row and column lookups; glyphs the lookups do not cover select row or column zero.</description></item>
    /// <item><description>The row stride is <see cref="ColumnCount"/> converted to shorts; the column stride is <see cref="ElementSizeInShorts"/>.</description></item>
    /// <item><description>Row and column selections beyond the declared counts are clamped out by the bounds check, so a malformed table yields no kerning rather than an out-of-range access.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="RowIndexTable"/>
    /// <seealso cref="ColumnIndexTable"/>
    private bool TryGetValueIndex(int leftGlyph, int rightGlyph, out int index)
    {
        index = 0;
        int row = (int)RowIndexTable.GetValue(leftGlyph);
        int column = (int)ColumnIndexTable.GetValue(rightGlyph);

        if ((uint)row >= (uint)RowCount || (uint)column >= (uint)ColumnCount)
            return false;

        index = (row * ColumnCount + column) * ElementSizeInShorts;
        return index >= 0 && index < KerningValues.Length;
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the subtable's common header.</param>
    /// <param name="context">A <see cref="KerxSubtable.Context"/> carrying the font's glyph count and the enclosing table's version.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="InvalidDataException"><paramref name="context"/> is not a <see cref="KerxSubtable.Context"/>.</exception>
    /// <exception cref="EndOfStreamException">A read extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The common header and the format 6 body header are read as single structs; the two index lookups are parsed against the font's glyph count.</description></item>
    /// <item><description>The value array is read as a single flat <c>Int16</c> buffer sized by the distance from the array offset to the subtable end.</description></item>
    /// <item><description>The row and column counts are stored on the record so the accessors can bound their indexing without re-deriving them from the array length.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
    /// <seealso cref="KerxSubtable.Context"/>
    /// <seealso cref="LookupTable.Context"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    static KerxFormat6Subtable IDerivedRecord<KerxSubtable, KerxFormat6Subtable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not KerxSubtable.Context ctx)
        {
            throw new InvalidDataException(
                "'kerx' format 6 subtable requires a glyph-count and version context.");
        }

        long subtableStart = cursor.Position;
        SubtableHeader header = cursor.ReadEndianReversibleStruct<SubtableHeader>();
        Format6Header format = cursor.ReadEndianReversibleStruct<Format6Header>();

        cursor.Position = subtableStart + format.RowIndexTable;
        LookupTable rowIndexTable = cursor.ReadRecord<LookupTable>(new LookupTable.Context(ctx.GlyphCount));

        cursor.Position = subtableStart + format.ColumnIndexTable;
        LookupTable columnIndexTable = cursor.ReadRecord<LookupTable>(new LookupTable.Context(ctx.GlyphCount));

        long arrayStart = subtableStart + format.KerningArray;
        long subtableEnd = subtableStart + header.Length;
        int arrayShortCount = checked((int)((subtableEnd - arrayStart) / 2));

        cursor.Position = arrayStart;
        short[] kerningValues = cursor.ReadInt16Array(arrayShortCount);

        return new()
        {
            Length = header.Length,
            Coverage = header.Coverage,
            TupleCount = header.TupleCount,
            RowIndexTable = rowIndexTable,
            ColumnIndexTable = columnIndexTable,
            RowCount = format.RowCount,
            ColumnCount = format.ColumnCount,
            KerningValues = kerningValues,
        };
    }

    /// <summary>The 16-byte format 6 subtable body that follows the common header: two counts and three offsets.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The row and column counts are 16-bit, and the three offsets are 32-bit; reversal is field-by-field because the struct is not uniform-width.</description></item>
    /// <item><description>The three offsets are relative to the start of the subtable, not to this struct.</description></item>
    /// <item><description>Consumed at parse time and not retained on <see cref="KerxFormat6Subtable"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KerxFormat6Subtable"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal record struct Format6Header : IEndianReversibleStruct<Format6Header>
    {
        /// <summary>The number of rows in the value array at byte offset 0.</summary>
        /// <value>The row stride count; bounds the row index selected by the row index lookup.</value>
        /// <seealso cref="KerxFormat6Subtable.RowCount"/>
        public ushort RowCount;         // +0

        /// <summary>The number of columns per row at byte offset 2.</summary>
        /// <value>The column count; the row-major stride of the value array.</value>
        /// <seealso cref="KerxFormat6Subtable.ColumnCount"/>
        public ushort ColumnCount;      // +2

        /// <summary>Subtable-relative byte offset of the row index lookup at byte offset 4.</summary>
        /// <value>The offset of the <see cref="LookupTable"/> that maps left-hand glyphs to row indices.</value>
        /// <seealso cref="KerxFormat6Subtable.RowIndexTable"/>
        public uint RowIndexTable;      // +4

        /// <summary>Subtable-relative byte offset of the column index lookup at byte offset 8.</summary>
        /// <value>The offset of the <see cref="LookupTable"/> that maps right-hand glyphs to column indices.</value>
        /// <seealso cref="KerxFormat6Subtable.ColumnIndexTable"/>
        public uint ColumnIndexTable;   // +8

        /// <summary>Subtable-relative byte offset of the value array at byte offset 12.</summary>
        /// <value>The offset of the flattened <c>Int16</c> array that holds the pairwise kerning values.</value>
        /// <seealso cref="KerxFormat6Subtable.KerningValues"/>
        public uint KerningArray;       // +12

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Format6Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>The first two fields are 16-bit and the remaining three are 32-bit, so the reversal is field-by-field rather than a single word-span pass.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static Format6Header ReverseEndianness(Format6Header value) => new()
        {
            RowCount = BinaryPrimitives.ReverseEndianness(value.RowCount),
            ColumnCount = BinaryPrimitives.ReverseEndianness(value.ColumnCount),
            RowIndexTable = BinaryPrimitives.ReverseEndianness(value.RowIndexTable),
            ColumnIndexTable = BinaryPrimitives.ReverseEndianness(value.ColumnIndexTable),
            KerningArray = BinaryPrimitives.ReverseEndianness(value.KerningArray),
        };
    }
}

/// <summary>The subtable glyph coverage table introduced in <c>kerx</c> version 3: a per-glyph bitfield indicating which subtables apply to which glyphs.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Lets a layout engine skip subtables whose glyph coverage does not intersect the run being shaped, before paying the cost of a state-machine step.</description></item>
/// <item><description>The bitfield is stored as one packed byte array per glyph, sized to <c>ceil(subtableCount / 8)</c> bytes; bit <c>i</c> of glyph <c>g</c>'s field indicates whether subtable <c>i</c> applies to glyph <c>g</c>.</description></item>
/// <item><description>The table's <c>bitfieldCount</c> field is the number of bits per glyph, matching the number of subtables declared in the header.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">subtable glyph coverage table</see> section of the <c>kerx</c> table description.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxTable"/>
/// <seealso cref="IRecord{T}"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxGlyphCoverageTable : IRecord<KerxGlyphCoverageTable>
{
    /// <summary>Gets the number of subtables the bitfield covers.</summary>
    /// <value>The <c>bitfieldCount</c> field. Matches the subtable count declared in the enclosing table's header.</value>
    /// <seealso cref="KerxTable.NumberOfSubtables"/>
    /// <seealso cref="IsCovered(int, int)"/>
    public int BitfieldCount { get; init; }

    /// <summary>Gets the per-glyph coverage bitfields.</summary>
    /// <value>One packed byte array per glyph; the array for glyph <c>g</c> has <c>ceil(BitfieldCount / 8)</c> bytes and bit <c>i</c> of that array indicates whether subtable <c>i</c> covers glyph <c>g</c>.</value>
    /// <remarks>The outer array length equals the font's glyph count; see <see cref="Context.GlyphCount"/>.</remarks>
    /// <seealso cref="BitfieldCount"/>
    /// <seealso cref="IsCovered(int, int)"/>
    public required byte[][] GlyphBitfields { get; init; }

    /// <summary>Determines whether a given subtable covers a given glyph.</summary>
    /// <param name="glyphId">The glyph ID to test.</param>
    /// <param name="subtableIndex">The subtable index in <c>[0, BitfieldCount)</c>.</param>
    /// <returns><see langword="true"/> when the subtable's bit is set for the glyph; otherwise <see langword="false"/>.</returns>
    /// <remarks>Out-of-range inputs return <see langword="false"/>; the caller need not pre-validate.</remarks>
    /// <seealso cref="GlyphBitfields"/>
    /// <seealso cref="BitfieldCount"/>
    public bool IsCovered(int glyphId, int subtableIndex)
    {
        if ((uint)glyphId >= (uint)GlyphBitfields.Length)
            return false;
        if ((uint)subtableIndex >= (uint)BitfieldCount)
            return false;

        byte[] field = GlyphBitfields[glyphId];
        int byteIndex = subtableIndex >> 3;
        if ((uint)byteIndex >= (uint)field.Length)
            return false;

        int bitIndex = subtableIndex & 7;
        return (field[byteIndex] & (1 << bitIndex)) != 0;
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the coverage table's first byte.</param>
    /// <param name="context">A <see cref="Context"/> carrying the font's glyph count and the subtable count declared in the enclosing table's header.</param>
    /// <returns>The parsed coverage table.</returns>
    /// <exception cref="InvalidDataException"><paramref name="context"/> is not a <see cref="Context"/>.</exception>
    /// <exception cref="EndOfStreamException">A read extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is read as a single struct; the per-glyph bitfields follow as a flat <c>byte</c> array of length <c>glyphCount × bytesPerGlyph</c>.</description></item>
    /// <item><description>Each glyph's bitfield occupies a contiguous run of <c>bytesPerGlyph</c> bytes; the parser slices the flat buffer into per-glyph arrays so the semantic record can be indexed by glyph ID directly.</description></item>
    /// <item><description>The declared <c>bitfieldCount</c> is preserved on the record; the parser does not cross-check it against the enclosing table's subtable count, leaving that to a diagnostics analyzer.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IRecord{T}"/>
    /// <seealso cref="Context"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    static KerxGlyphCoverageTable IRecord<KerxGlyphCoverageTable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
        {
            throw new InvalidDataException(
                "'kerx' glyph coverage table requires a glyph-count and subtable-count context.");
        }

        Header header = cursor.ReadEndianReversibleStruct<Header>();
        int bitfieldCount = checked((int)header.BitfieldCount);
        int bytesPerGlyph = (bitfieldCount + 7) >> 3;

        byte[] raw = cursor.ReadUInt8Array(checked(ctx.GlyphCount * bytesPerGlyph));
        var glyphBitfields = new byte[ctx.GlyphCount][];
        for (int glyph = 0; glyph < ctx.GlyphCount; glyph++)
            glyphBitfields[glyph] = raw.AsSpan(glyph * bytesPerGlyph, bytesPerGlyph).ToArray();

        return new()
        {
            BitfieldCount = bitfieldCount,
            GlyphBitfields = glyphBitfields,
        };
    }

    /// <summary>Context for parsing a <see cref="KerxGlyphCoverageTable"/>: the font's glyph count and the subtable count.</summary>
    /// <param name="GlyphCount">Number of glyphs in the font, from <c>maxp.numGlyphs</c>. Determines the outer length of <see cref="GlyphBitfields"/>.</param>
    /// <param name="SubtableCount">The subtable count declared in the enclosing table's header. Determines the expected bitfield size in bits, though the parser trusts the coverage table's own <c>bitfieldCount</c> for the actual byte count.</param>
    /// <remarks>The context is a record type so that the two values cannot be confused with bare integers at the call site.</remarks>
    /// <seealso cref="KerxTable.NumberOfSubtables"/>
    /// <seealso cref="KerxTable.CoverageTableVersion"/>
    public record Context(int GlyphCount, int SubtableCount);

    /// <summary>The 8-byte <c>kerx</c> glyph coverage table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both fields are <c>UInt32</c>, so the reversal is a single word-span pass over the struct reinterpreted as two 32-bit words.</description></item>
    /// <item><description>The <c>version</c> field is defined as zero by the specification; the parser preserves the value but does not reject non-zero.</description></item>
    /// <item><description>Consumed at parse time and not retained on <see cref="KerxGlyphCoverageTable"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KerxGlyphCoverageTable"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The coverage table version at byte offset 0, defined as zero.</summary>
        /// <value>The raw <c>version</c> field; the parser preserves it for round-tripping and diagnostics.</value>
        public uint Version;         // +0

        /// <summary>The number of subtables the bitfield covers at byte offset 4.</summary>
        /// <value>The bit count per glyph; each glyph's bitfield occupies <c>ceil(BitfieldCount / 8)</c> bytes.</value>
        /// <seealso cref="KerxGlyphCoverageTable.BitfieldCount"/>
        public uint BitfieldCount;   // +4

        /// <summary>Reverses the byte order of both 32-bit fields in one pass.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>The header is entirely <c>UInt32</c> fields, so it is reinterpreted as a span of two 32-bit words and reversed with a single <see cref="BinaryPrimitives.ReverseEndianness(ReadOnlySpan{uint}, Span{uint})"/> call.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static Header ReverseEndianness(Header value)
        {
            Span<uint> words = MemoryMarshal.CreateSpan(ref Unsafe.As<Header, uint>(ref value), 2);
            BinaryPrimitives.ReverseEndianness(words, words);
            return value;
        }
    }
}
