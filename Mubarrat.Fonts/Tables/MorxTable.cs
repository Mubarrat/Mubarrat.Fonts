using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Sfnt;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════
// The 'morx' table — extended AAT glyph metamorphosis
//
// The replacement for the deprecated 'mort' table. Uses the shared extended
// state table (STXHeader) and LookupTable from AatShared.cs, and widens every
// count and offset to 32 bits. Big-endian throughout, like the rest of sfnt.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>morx</c> table: extended Apple Advanced Typography glyph metamorphosis. Replaces the deprecated <c>mort</c> table with 32-bit counts, 32-bit offsets, extended state tables, and an optional per-chain glyph coverage table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><b>Extended state tables.</b> Every state-machine subtable uses <see cref="STXHeader"/>: a <c>LookupTable</c> class table and 16-bit zero-based state-array indices into the entry table, instead of the classic table's 8-bit class array and byte offsets.</description></item>
/// <item><description><b>Version 3 adds coverage.</b> When <see cref="Version"/> is 3, each chain carries a <see cref="MorxGlyphCoverageTable"/> after its last subtable, letting a layout engine skip subtables that cannot affect the current glyph run.</description></item>
/// <item><description><b>Subtables are ordered.</b> Unlike <c>kerx</c>, whose subtables are additive, <c>morx</c> subtables transform the glyph array and are therefore applied in chain order.</description></item>
/// <item><description><b>Lookup value zero is a sentinel.</b> A lookup value of <c>0</c> means "make no substitution"; it cannot be used to replace a glyph with glyph 0.</description></item>
/// <item><description>All multi-byte reads use the sfnt default of big-endian.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html"><c>morx</c> table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MorxChain"/>
/// <seealso cref="MorxSubtable"/>
/// <seealso cref="MortTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
public sealed record MorxTable : IFontTable<MorxTable>, IRecord<MorxTable>
{
    /// <summary>The lowest version defined by the specification.</summary>
    public const ushort MinVersion = 2;

    /// <summary>The version at which per-chain glyph coverage tables were introduced.</summary>
    public const ushort CoverageTableVersion = 3;

    /// <summary>The highest version defined by the current specification.</summary>
    public const ushort MaxVersion = 3;

    /// <inheritdoc/>
    public static Tag Tag => "morx";

    /// <summary>Gets the table version.</summary>
    /// <value>2 or 3. Version 3 adds a <see cref="MorxGlyphCoverageTable"/> after the subtables of each chain.</value>
    /// <seealso cref="MinVersion"/>
    /// <seealso cref="MaxVersion"/>
    /// <seealso cref="CoverageTableVersion"/>
    public ushort Version { get; init; }

    /// <summary>Gets the number of metamorphosis chains.</summary>
    /// <value>The <c>nChains</c> field from the header. Equal to <c>Chains.Count</c>.</value>
    /// <seealso cref="Chains"/>
    public uint NumberOfChains { get; init; }

    /// <summary>Gets the metamorphosis chains, in table order.</summary>
    /// <value>A read-only view over the <see cref="MorxChain"/> instances.</value>
    /// <seealso cref="MorxChain"/>
    public required IReadOnlyList<MorxChain> Chains { get; init; }

    static MorxTable IRecord<MorxTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version < MinVersion || header.Version > MaxVersion)
            throw new InvalidDataException(
                $"'morx'.version is {header.Version}, expected {MinVersion} or {MaxVersion}.");

        if (header.NChains > int.MaxValue)
            throw new InvalidDataException(
                $"'morx'.nChains is {header.NChains}, which exceeds the supported maximum.");

        int numGlyphs = context is SfntFontFace face
            ? face.GetTable<MaxpTable>().NumGlyphs
            : throw new InvalidDataException(
                "'morx' requires an SFNT font face context to obtain the glyph count.");

        return new()
        {
            Version = header.Version,
            NumberOfChains = header.NChains,
            Chains = Array.AsReadOnly(cursor.ReadRecordArray<MorxChain>((int)header.NChains, new MorxChain.Context(numGlyphs, header.Version))),
        };
    }

    /// <summary>The 8-byte <c>morx</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The version and unused fields are <c>UInt16</c>; the chain count is <c>UInt32</c>, so the reversal is field-by-field.</description></item>
    /// <item><description>Consumed at parse time and not retained on <see cref="MorxTable"/>.</description></item>
    /// <item><description>The <c>&lt;Clone&gt;$()</c> method generated by the <c>record struct</c> declaration must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0.</summary>
        /// <value>2 or 3.</value>
        /// <seealso cref="MorxTable.Version"/>
        public ushort Version;  // +0

        /// <summary>Unused padding at byte offset 2, set to zero.</summary>
        /// <value>The padding word; preserved but not validated.</value>
        public ushort Unused;   // +2

        /// <summary>The number of chains at byte offset 4.</summary>
        /// <value>The count of <see cref="MorxChain"/> records that follow this header.</value>
        /// <seealso cref="MorxTable.NumberOfChains"/>
        public uint NChains;    // +4

        /// <summary>Reverses the byte order of every multi-byte field.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>The struct mixes 16-bit and 32-bit fields, so the reversal is field-by-field.</remarks>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(value.Version),
            Unused = BinaryPrimitives.ReverseEndianness(value.Unused),
            NChains = BinaryPrimitives.ReverseEndianness(value.NChains),
        };
    }
}

/// <summary>A single metamorphosis chain: a feature table that computes sub-feature flags, the subtables that apply when those flags are set, and — for version 3 — a glyph coverage table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The chain header is followed by <c>nFeatureEntries</c> 12-byte feature entries, then <c>nSubtables</c> variable-length subtables, then (for version 3) the coverage table.</description></item>
/// <item><description><c>chainLength</c> includes the header, the feature table, the subtables, and the coverage table, and is always a multiple of four, so the next chain begins on a four-byte boundary.</description></item>
/// <item><description>Feature entries are processed in order: the running flag word is ANDed with each matched entry's <c>disableFlags</c> and ORed with its <c>enableFlags</c>. The last entry is the Enable Glyph Effects feature (type 0, setting 1) with both flags zero, which clears the chain.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MorxTable"/>
/// <seealso cref="MorxFeatureEntry"/>
/// <seealso cref="MorxSubtable"/>
/// <seealso cref="MorxGlyphCoverageTable"/>
public sealed record MorxChain : IRecord<MorxChain>
{
    /// <summary>Gets the default sub-feature flags for this chain.</summary>
    /// <value>The flag word that seeds the running flags before the feature table is applied.</value>
    public uint DefaultFlags { get; init; }

    /// <summary>Gets the feature table entries in table order.</summary>
    /// <value>A read-only view over the <see cref="MorxFeatureEntry"/> records. Later entries take precedence.</value>
    public required ReadOnlyMemory<MorxFeatureEntry> FeatureEntries { get; init; }

    /// <summary>Gets the subtables in chain order.</summary>
    /// <value>A read-only view over the <see cref="MorxSubtable"/> instances. Unlike <c>kerx</c>, <c>morx</c> subtables are applied in order because each transforms the glyph stream.</value>
    public required IReadOnlyList<MorxSubtable> Subtables { get; init; }

    /// <summary>Gets the per-chain glyph coverage table, or <see langword="null"/> when the table version is 2.</summary>
    /// <value>The parsed <see cref="MorxGlyphCoverageTable"/> when the enclosing table's version is 3; otherwise <see langword="null"/>.</value>
    /// <seealso cref="MorxTable.CoverageTableVersion"/>
    public MorxGlyphCoverageTable? Coverage { get; init; }

    static MorxChain IRecord<MorxChain>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
            throw new InvalidDataException(
                "'morx' chain requires a glyph-count and version context.");

        long chainStart = cursor.Position;
        ChainHeader header = cursor.ReadEndianReversibleStruct<ChainHeader>();

        if ((header.ChainLength & 3) != 0)
            throw new InvalidDataException(
                $"'morx' chain length {header.ChainLength} is not a multiple of four.");

        long chainEnd = chainStart + header.ChainLength;

        if (header.NFeatureEntries > int.MaxValue)
            throw new InvalidDataException(
                $"'morx' chain feature count {header.NFeatureEntries} exceeds the supported maximum.");
        if (header.NSubtables > int.MaxValue)
            throw new InvalidDataException(
                $"'morx' chain subtable count {header.NSubtables} exceeds the supported maximum.");

        MorxFeatureEntry[] features = cursor.ReadEndianReversibleStructArray<MorxFeatureEntry>(
            (int)header.NFeatureEntries);

        var subtableContext = new MorxSubtable.Context(ctx.GlyphCount, ctx.Version);
        var subtables = new MorxSubtable[(int)header.NSubtables];
        for (int i = 0; i < subtables.Length; i++)
            subtables[i] = cursor.ReadRecord<MorxSubtable>(subtableContext);

        MorxGlyphCoverageTable? coverage = ctx.Version >= MorxTable.CoverageTableVersion
            ? cursor.ReadRecord<MorxGlyphCoverageTable>(new MorxGlyphCoverageTable.Context(ctx.GlyphCount, subtables.Length))
            : null;

        cursor.Position = chainEnd;

        return new()
        {
            DefaultFlags = header.DefaultFlags,
            FeatureEntries = features,
            Subtables = Array.AsReadOnly(subtables),
            Coverage = coverage,
        };
    }

    /// <summary>Context for parsing a <see cref="MorxChain"/>: the font's glyph count and the enclosing table's version.</summary>
    /// <param name="GlyphCount">Number of glyphs in the font, from <c>maxp.numGlyphs</c>.</param>
    /// <param name="Version">The enclosing <c>morx</c> table's version. Determines whether a coverage table follows the subtables.</param>
    public record Context(int GlyphCount, ushort Version);

    /// <summary>The 16-byte metamorphosis chain header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All four fields are <c>UInt32</c>, so the reversal is a single word-span pass over four 32-bit words.</description></item>
    /// <item><description><c>chainLength</c> includes the header, feature table, subtables, and coverage table, and must be a multiple of four.</description></item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct ChainHeader : IEndianReversibleStruct<ChainHeader>
    {
        /// <summary>The chain's default sub-feature flags at byte offset 0.</summary>
        public uint DefaultFlags;      // +0

        /// <summary>The total chain length in bytes at byte offset 4.</summary>
        public uint ChainLength;       // +4

        /// <summary>The number of feature table entries at byte offset 8.</summary>
        public uint NFeatureEntries;   // +8

        /// <summary>The number of subtables at byte offset 12.</summary>
        public uint NSubtables;        // +12

        /// <summary>Reverses the byte order of all four 32-bit fields in one pass.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with all four fields reversed.</returns>
        public static ChainHeader ReverseEndianness(ChainHeader value)
        {
            Span<uint> words = MemoryMarshal.CreateSpan(ref value.DefaultFlags, 4);
            BinaryPrimitives.ReverseEndianness(words, words);
            return value;
        }
    }
}

/// <summary>A feature-table entry: a user feature/setting and the sub-feature flags it enables and disables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>When the requested feature/setting matches, the running flag word is ANDed with <see cref="DisableFlags"/> and ORed with <see cref="EnableFlags"/>.</description></item>
/// <item><description>The last entry in every feature table must be the Enable Glyph Effects feature (type 0, setting 1) with both flags zero.</description></item>
/// <item><description>The struct mixes two <c>UInt16</c> and two <c>UInt32</c> fields, so reversal is field-by-field.</description></item>
/// </list>
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct MorxFeatureEntry : IEndianReversibleStruct<MorxFeatureEntry>
{
    /// <summary>The feature type at byte offset 0.</summary>
    public ushort FeatureType;     // +0

    /// <summary>The feature setting at byte offset 2.</summary>
    public ushort FeatureSetting;  // +2

    /// <summary>The flags enabled by this feature/setting at byte offset 4.</summary>
    public uint EnableFlags;       // +4

    /// <summary>The flags disabled by this feature/setting at byte offset 8.</summary>
    public uint DisableFlags;      // +8

    /// <summary>Reverses the byte order of every multi-byte field.</summary>
    /// <param name="value">The value whose fields are to be reversed.</param>
    /// <returns>A new entry with each field reversed.</returns>
    public static MorxFeatureEntry ReverseEndianness(MorxFeatureEntry value) => new()
    {
        FeatureType = BinaryPrimitives.ReverseEndianness(value.FeatureType),
        FeatureSetting = BinaryPrimitives.ReverseEndianness(value.FeatureSetting),
        EnableFlags = BinaryPrimitives.ReverseEndianness(value.EnableFlags),
        DisableFlags = BinaryPrimitives.ReverseEndianness(value.DisableFlags),
    };
}

/// <summary>Base for the five metamorphosis subtable formats in a <c>morx</c> table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Every subtable begins with a 12-byte common header: <c>length</c>, <c>coverage</c>, and <c>subFeatureFlags</c>, all <c>UInt32</c>.</description></item>
/// <item><description>The low byte of the coverage word is the format discriminant; the upper bits describe applicability and processing order.</description></item>
/// <item><description>State-machine formats (0, 1, 2, 5) use the extended state table (<see cref="STXHeader"/>): a <c>LookupTable</c> class table and 16-bit zero-based entry indices.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MorxRearrangementSubtable"/>
/// <seealso cref="MorxContextualSubtable"/>
/// <seealso cref="MorxLigatureSubtable"/>
/// <seealso cref="MorxNonContextualSubtable"/>
/// <seealso cref="MorxInsertionSubtable"/>
/// <seealso cref="IBaseRecord{TBase}"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
public abstract record MorxSubtable : IRecord<MorxSubtable>, IBaseRecord<MorxSubtable>
{
    /// <summary>Gets the subtable length in bytes, including the 12-byte common header.</summary>
    /// <value>The <c>length</c> field. Every derived parser uses it to advance to the next subtable.</value>
    public required uint Length { get; init; }

    /// <summary>Gets the raw coverage word.</summary>
    /// <value>The <c>coverage</c> field. Use the bit accessors for the meaningful flags and <see cref="Format"/> for the discriminant.</value>
    public required uint Coverage { get; init; }

    /// <summary>Gets the sub-feature flags that enable this subtable.</summary>
    /// <value>The 32-bit mask. The subtable applies when this word intersects the computed sub-feature flags for the enclosing chain.</value>
    public required uint SubFeatureFlags { get; init; }

    /// <summary>Gets the format discriminant.</summary>
    /// <value>The low byte of <see cref="Coverage"/>: 0, 1, 2, 4, or 5.</value>
    public abstract int Format { get; }

    /// <summary>Gets a value indicating whether the subtable is restricted to vertical text.</summary>
    /// <value><see langword="true"/> when bit <c>0x80000000</c> of <see cref="Coverage"/> is set.</value>
    public bool Vertical => (Coverage & 0x80000000u) != 0;

    /// <summary>Gets a value indicating whether the subtable processes glyphs in descending order.</summary>
    /// <value><see langword="true"/> when bit <c>0x40000000</c> of <see cref="Coverage"/> is set.</value>
    public bool Descending => (Coverage & 0x40000000u) != 0;

    /// <summary>Gets a value indicating whether the subtable applies to both horizontal and vertical text.</summary>
    /// <value><see langword="true"/> when bit <c>0x20000000</c> of <see cref="Coverage"/> is set.</value>
    /// <remarks>When set, <see cref="Vertical"/> is ignored.</remarks>
    public bool AllDirections => (Coverage & 0x20000000u) != 0;

    /// <summary>Gets a value indicating whether the subtable processes glyphs in logical order rather than layout order.</summary>
    /// <value><see langword="true"/> when bit <c>0x10000000</c> of <see cref="Coverage"/> is set.</value>
    public bool LogicalOrder => (Coverage & 0x10000000u) != 0;

    static MorxSubtable IRecord<MorxSubtable>.Parse(ref Cursor cursor, object? context)
    {
        int format = (int)(cursor.Source.ReadUInt32At(4) & 0xFFu);
        return format switch
        {
            0 => IBaseRecord<MorxSubtable>.Parse<MorxRearrangementSubtable>(ref cursor, context),
            1 => IBaseRecord<MorxSubtable>.Parse<MorxContextualSubtable>(ref cursor, context),
            2 => IBaseRecord<MorxSubtable>.Parse<MorxLigatureSubtable>(ref cursor, context),
            4 => IBaseRecord<MorxSubtable>.Parse<MorxNonContextualSubtable>(ref cursor, context),
            5 => IBaseRecord<MorxSubtable>.Parse<MorxInsertionSubtable>(ref cursor, context),
            _ => throw new InvalidDataException(
                $"'morx' subtable format {format} is not one of 0, 1, 2, 4, or 5."),
        };
    }

    /// <summary>Returns the nearest section start after <paramref name="current"/>, or <paramref name="subtableEnd"/> when none is closer.</summary>
    /// <param name="current">The section start whose end is being computed.</param>
    /// <param name="subtableEnd">The end of the enclosing subtable.</param>
    /// <param name="starts">Every section start in the subtable, including <paramref name="current"/>.</param>
    /// <returns>The smallest entry of <paramref name="starts"/> greater than <paramref name="current"/>, or <paramref name="subtableEnd"/>.</returns>
    protected static long NextSection(long current, long subtableEnd, ReadOnlySpan<long> starts)
    {
        long next = subtableEnd;
        foreach (long s in starts)
        {
            if (s > current && s < next) next = s;
        }
        return next;
    }

    /// <summary>Context for parsing a <see cref="MorxSubtable"/>: the font's glyph count and the enclosing table's version.</summary>
    /// <param name="GlyphCount">Number of glyphs in the font, from <c>maxp.numGlyphs</c>.</param>
    /// <param name="Version">The enclosing <c>morx</c> table's version.</param>
    public record Context(int GlyphCount, ushort Version);

    /// <summary>The 12-byte common subtable header shared by every <see cref="MorxSubtable"/> format.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All three fields are <c>UInt32</c>, so the reversal is a single word-span pass over three 32-bit words.</description></item>
    /// <item><description>The low byte of <see cref="Coverage"/> is the format discriminant.</description></item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct SubtableHeader : IEndianReversibleStruct<SubtableHeader>
    {
        /// <summary>The subtable length in bytes at byte offset 0.</summary>
        public uint Length;          // +0

        /// <summary>The coverage word at byte offset 4.</summary>
        public uint Coverage;        // +4

        /// <summary>The sub-feature flags at byte offset 8.</summary>
        public uint SubFeatureFlags; // +8

        /// <summary>Reverses the byte order of all three 32-bit fields in one pass.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with all three fields reversed.</returns>
        public static SubtableHeader ReverseEndianness(SubtableHeader value)
        {
            Span<uint> words = MemoryMarshal.CreateSpan(ref value.Length, 3);
            BinaryPrimitives.ReverseEndianness(words, words);
            return value;
        }
    }
}

/// <summary>The extended AAT state machine shared by the state-table-based metamorphosis subtables: a <see cref="LookupTable"/> class table, a flat <c>UInt16</c> state array of entry indices, and the format-specific entry table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The class table maps each glyph to a class in <c>[0, StateSize)</c>; glyphs the lookup does not cover classify to class 0.</description></item>
/// <item><description>The state array is flat: entry <c>state × StateSize + class</c> holds a zero-based index into the entry table.</description></item>
/// <item><description>Unlike the classic <c>mort</c> state machine, the state array elements are 16-bit indices, not byte offsets; the class table is a lookup table, not a dense byte array.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MorxRearrangementSubtable"/>
/// <seealso cref="MorxContextualSubtable"/>
/// <seealso cref="MorxLigatureSubtable"/>
/// <seealso cref="MorxInsertionSubtable"/>
/// <seealso cref="STXHeader"/>
/// <seealso cref="LookupTable"/>
public sealed record MorxStateMachine
{
    /// <summary>Gets the class table that maps glyph indices to classes.</summary>
    /// <value>An AAT lookup whose value for glyph <c>i</c> is a class in <c>[0, StateSize)</c>.</value>
    public required LookupTable ClassTable { get; init; }

    /// <summary>Gets the number of classes, equal to the number of entry-table slots per state.</summary>
    /// <value>The <c>nClasses</c> field from the <see cref="STXHeader"/>.</value>
    public required int StateSize { get; init; }

    /// <summary>Gets the flat state array.</summary>
    /// <value>One <c>UInt16</c> entry-table index per <c>(state, class)</c> pair, laid out row-major.</value>
    public required ReadOnlyMemory<ushort> StateArray { get; init; }

    /// <summary>Gets the number of states.</summary>
    /// <value><see cref="StateArray"/>.Length divided by <see cref="StateSize"/>, or zero when <see cref="StateSize"/> is zero.</value>
    public int StateCount => StateSize == 0 ? 0 : StateArray.Length / StateSize;

    /// <summary>Classifies a glyph against the class table.</summary>
    /// <param name="glyphId">The glyph ID to classify.</param>
    /// <returns>The class code in <c>[0, StateSize)</c> when the lookup covers the glyph; otherwise zero.</returns>
    public int ClassifyGlyph(int glyphId) => (int)ClassTable.GetValue(glyphId);

    /// <summary>Gets the zero-based entry-table index for a state/class pair.</summary>
    /// <param name="stateIndex">The state index in <c>[0, StateCount)</c>.</param>
    /// <param name="glyphClass">The class byte obtained from <see cref="ClassifyGlyph(int)"/>.</param>
    /// <returns>The index into the subtable's entry array.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="stateIndex"/> or <paramref name="glyphClass"/> is out of range.</exception>
    public int GetEntryIndex(int stateIndex, int glyphClass)
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
        return StateArray.Span[stateIndex * StateSize + glyphClass];
    }

    internal static MorxStateMachine Parse(
        ref Cursor cursor,
        long stateHeaderStart,
        STXHeader stx,
        long stateArrayEnd,
        int glyphCount)
    {
        cursor.Position = stateHeaderStart + stx.ClassTableOffset;
        LookupTable classTable = cursor.ReadRecord<LookupTable>(new LookupTable.Context(glyphCount));

        long stateArrayStart = stateHeaderStart + stx.StateArrayOffset;
        int stateArrayCount = checked((int)((stateArrayEnd - stateArrayStart) / 2));
        cursor.Position = stateArrayStart;
        ushort[] stateArray = cursor.ReadUInt16Array(stateArrayCount);

        return new()
        {
            ClassTable = classTable,
            StateSize = checked((int)stx.ClassCount),
            StateArray = stateArray,
        };
    }
}

/// <summary>An Indic rearrangement subtable: reorders a contiguous glyph range according to a classic rearrangement verb, driven by an extended state machine.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The subtable is the 12-byte header followed by the extended state table and a 4-byte entry table. There are no per-glyph tables.</description></item>
/// <item><description>The entry's <see cref="MorxRearrangementEntry.MarkFirst"/> and <see cref="MorxRearrangementEntry.MarkLast"/> flags remember the two boundaries; <see cref="MorxRearrangementEntry.Verb"/> selects one of sixteen permutations.</description></item>
/// </list>
/// </remarks>
public sealed record MorxRearrangementSubtable : MorxSubtable, IDerivedRecord<MorxSubtable, MorxRearrangementSubtable>
{
    /// <inheritdoc/>
    public override int Format => 0;

    /// <summary>Gets the extended state machine that drives the rearrangement.</summary>
    public required MorxStateMachine StateMachine { get; init; }

    /// <summary>Gets the entry array that state transitions select into.</summary>
    /// <value>One 4-byte <see cref="MorxRearrangementEntry"/> per entry-table slot.</value>
    public required ReadOnlyMemory<MorxRearrangementEntry> Entries { get; init; }

    /// <summary>Advances the state machine one step.</summary>
    /// <param name="stateIndex">The current state index.</param>
    /// <param name="glyphClass">The class for the current glyph.</param>
    /// <returns>The entry for the transition.</returns>
    public MorxRearrangementEntry Step(int stateIndex, int glyphClass) =>
        Entries.Span[StateMachine.GetEntryIndex(stateIndex, glyphClass)];

    static MorxRearrangementSubtable IDerivedRecord<MorxSubtable, MorxRearrangementSubtable>.Parse(
        ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
            throw new InvalidDataException(
                "'morx' rearrangement subtable requires a glyph-count and version context.");

        long subtableStart = cursor.Position;
        SubtableHeader header = cursor.ReadEndianReversibleStruct<SubtableHeader>();
        long subtableEnd = subtableStart + header.Length;

        long stateHeaderStart = cursor.Position;
        STXHeader stx = cursor.ReadEndianReversibleStruct<STXHeader>();

        long classTableStart = stateHeaderStart + stx.ClassTableOffset;
        long stateArrayStart = stateHeaderStart + stx.StateArrayOffset;
        long entryTableStart = stateHeaderStart + stx.EntryTableOffset;

        Span<long> sections = stackalloc long[] { classTableStart, stateArrayStart, entryTableStart };
        long stateArrayEnd = NextSection(stateArrayStart, subtableEnd, sections);
        long entryTableEnd = NextSection(entryTableStart, subtableEnd, sections);

        MorxStateMachine stateMachine = MorxStateMachine.Parse(
            ref cursor, stateHeaderStart, stx, stateArrayEnd, ctx.GlyphCount);

        int entryCount = checked((int)((entryTableEnd - entryTableStart) / 4));
        cursor.Position = entryTableStart;
        MorxRearrangementEntry[] entries =
            cursor.ReadEndianReversibleStructArray<MorxRearrangementEntry>(entryCount);

        cursor.Position = subtableEnd;

        return new()
        {
            Length = header.Length,
            Coverage = header.Coverage,
            SubFeatureFlags = header.SubFeatureFlags,
            StateMachine = stateMachine,
            Entries = entries,
        };
    }
}

/// <summary>A single entry in an Indic rearrangement subtable: a next-state index and the rearrangement flags.</summary>
/// <remarks>All fields are <c>UInt16</c>, so the reversal is a single word-span pass over two 16-bit words.</remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct MorxRearrangementEntry : IEndianReversibleStruct<MorxRearrangementEntry>
{
    /// <summary>The zero-based next-state index at byte offset 0.</summary>
    public ushort NewStateIndex;  // +0

    /// <summary>The rearrangement flags word at byte offset 2.</summary>
    public ushort Flags;          // +2

    /// <summary>Gets a value indicating whether the current glyph becomes the marked-first glyph.</summary>
    public readonly bool MarkFirst => (Flags & 0x8000) != 0;

    /// <summary>Gets a value indicating whether the state machine should stay on the current glyph.</summary>
    public readonly bool DontAdvance => (Flags & 0x4000) != 0;

    /// <summary>Gets a value indicating whether the current glyph becomes the marked-last glyph.</summary>
    public readonly bool MarkLast => (Flags & 0x2000) != 0;

    /// <summary>Gets the rearrangement verb.</summary>
    /// <value>The low four bits of <see cref="Flags"/>.</value>
    public readonly IndicRearrangementVerb Verb => (IndicRearrangementVerb)(Flags & 0x000F);

    /// <summary>Reverses the byte order of both 16-bit fields in one pass.</summary>
    /// <param name="value">The value whose fields are to be reversed.</param>
    /// <returns>A new entry with both fields reversed.</returns>
    public static MorxRearrangementEntry ReverseEndianness(MorxRearrangementEntry value)
    {
        Span<ushort> words = MemoryMarshal.CreateSpan(ref value.NewStateIndex, 2);
        BinaryPrimitives.ReverseEndianness(words, words);
        return value;
    }
}

/// <summary>A contextual glyph substitution subtable: replaces the marked and current glyphs using per-glyph lookup tables selected by an extended state machine.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>After the extended state header, a <c>UInt32</c> offset points to the substitution table area, which begins with an array of <c>UInt32</c> offsets — one per lookup table — relative to that area.</description></item>
/// <item><description>Each state entry carries two <c>UInt16</c> indices into that offset array; <c>0xFFFF</c> means no substitution for that position.</description></item>
/// <item><description>A lookup value of zero means "make no substitution."</description></item>
/// </list>
/// </remarks>
public sealed record MorxContextualSubtable : MorxSubtable, IDerivedRecord<MorxSubtable, MorxContextualSubtable>
{
    /// <inheritdoc/>
    public override int Format => 1;

    /// <summary>Gets the extended state machine that drives the substitution.</summary>
    public required MorxStateMachine StateMachine { get; init; }

    /// <summary>Gets the entry array that state transitions select into.</summary>
    /// <value>One 8-byte <see cref="MorxContextualEntry"/> per entry-table slot.</value>
    public required ReadOnlyMemory<MorxContextualEntry> Entries { get; init; }

    /// <summary>Gets the substitution lookup tables, indexed by the entry's mark/current index.</summary>
    /// <value>One <see cref="LookupTable"/> or <see langword="null"/> per referenced index. A null entry means the index has no associated lookup table.</value>
    public required IReadOnlyList<LookupTable?> SubstitutionTables { get; init; }

    /// <summary>Advances the state machine one step.</summary>
    /// <param name="stateIndex">The current state index.</param>
    /// <param name="glyphClass">The class for the current glyph.</param>
    /// <returns>The entry for the transition.</returns>
    public MorxContextualEntry Step(int stateIndex, int glyphClass) =>
        Entries.Span[StateMachine.GetEntryIndex(stateIndex, glyphClass)];

    /// <summary>Gets the substitution table that applies to the marked glyph for an entry.</summary>
    /// <param name="entry">The state entry.</param>
    /// <returns>The lookup table, or <see langword="null"/> when the entry's mark index is <c>0xFFFF</c> or has no table.</returns>
    public LookupTable? GetMarkSubstitutionTable(MorxContextualEntry entry) =>
        entry.MarkIndex != MorxContextualEntry.NoSubstitution &&
        entry.MarkIndex < SubstitutionTables.Count
            ? SubstitutionTables[entry.MarkIndex]
            : null;

    /// <summary>Gets the substitution table that applies to the current glyph for an entry.</summary>
    /// <param name="entry">The state entry.</param>
    /// <returns>The lookup table, or <see langword="null"/> when the entry's current index is <c>0xFFFF</c> or has no table.</returns>
    public LookupTable? GetCurrentSubstitutionTable(MorxContextualEntry entry) =>
        entry.CurrentIndex != MorxContextualEntry.NoSubstitution &&
        entry.CurrentIndex < SubstitutionTables.Count
            ? SubstitutionTables[entry.CurrentIndex]
            : null;

    static MorxContextualSubtable IDerivedRecord<MorxSubtable, MorxContextualSubtable>.Parse(
        ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
            throw new InvalidDataException(
                "'morx' contextual subtable requires a glyph-count and version context.");

        long subtableStart = cursor.Position;
        SubtableHeader header = cursor.ReadEndianReversibleStruct<SubtableHeader>();
        long subtableEnd = subtableStart + header.Length;

        long stateHeaderStart = cursor.Position;
        STXHeader stx = cursor.ReadEndianReversibleStruct<STXHeader>();
        uint substitutionTable = cursor.ReadUInt32();

        long substitutionTableStart = subtableStart + substitutionTable;
        long classTableStart = stateHeaderStart + stx.ClassTableOffset;
        long stateArrayStart = stateHeaderStart + stx.StateArrayOffset;
        long entryTableStart = stateHeaderStart + stx.EntryTableOffset;

        Span<long> sections = [classTableStart, stateArrayStart, entryTableStart, substitutionTableStart];
        long stateArrayEnd = NextSection(stateArrayStart, subtableEnd, sections);
        long entryTableEnd = NextSection(entryTableStart, subtableEnd, sections);

        MorxStateMachine stateMachine = MorxStateMachine.Parse(
            ref cursor, stateHeaderStart, stx, stateArrayEnd, ctx.GlyphCount);

        int entryCount = checked((int)((entryTableEnd - entryTableStart) / 8));
        cursor.Position = entryTableStart;
        MorxContextualEntry[] entries =
            cursor.ReadEndianReversibleStructArray<MorxContextualEntry>(entryCount);

        int maxIndex = -1;
        foreach (MorxContextualEntry entry in entries)
        {
            if (entry.MarkIndex != MorxContextualEntry.NoSubstitution && entry.MarkIndex > maxIndex)
                maxIndex = entry.MarkIndex;
            if (entry.CurrentIndex != MorxContextualEntry.NoSubstitution && entry.CurrentIndex > maxIndex)
                maxIndex = entry.CurrentIndex;
        }

        var substitutionTables = new LookupTable?[maxIndex + 1];
        if (maxIndex >= 0)
        {
            int offsetCount = maxIndex + 1;
            long offsetsBytes = (long)offsetCount * 4;
            if (substitutionTableStart + offsetsBytes > subtableEnd)
            {
                throw new InvalidDataException(
                    $"'morx' contextual substitution offset array ({offsetCount} entries) extends past the subtable.");
            }

            uint[] offsets = cursor.Source.ReadUInt32ArrayAt(substitutionTableStart, offsetCount);
            var lookupContext = new LookupTable.Context(ctx.GlyphCount);
            for (int i = 0; i < offsetCount; i++)
            {
                if (offsets[i] == 0) continue;
                long lookupStart = substitutionTableStart + offsets[i];
                if (lookupStart + 4 > subtableEnd)
                {
                    throw new InvalidDataException(
                        $"'morx' contextual substitution lookup {i} lies outside the subtable.");
                }
                substitutionTables[i] = cursor.Source.ParseRecordAt<LookupTable>(lookupStart, lookupContext);
            }
        }

        cursor.Position = subtableEnd;

        return new()
        {
            Length = header.Length,
            Coverage = header.Coverage,
            SubFeatureFlags = header.SubFeatureFlags,
            StateMachine = stateMachine,
            Entries = entries,
            SubstitutionTables = substitutionTables,
        };
    }
}

/// <summary>A single entry in a contextual substitution subtable: a next-state index, control flags, and two substitution-table indices.</summary>
/// <remarks>All four fields are <c>UInt16</c>, so the reversal is a single word-span pass over four 16-bit words.</remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct MorxContextualEntry : IEndianReversibleStruct<MorxContextualEntry>
{
    /// <summary>The sentinel that means "no substitution table" for <see cref="MarkIndex"/> or <see cref="CurrentIndex"/>.</summary>
    public const ushort NoSubstitution = 0xFFFF;

    /// <summary>The zero-based next-state index at byte offset 0.</summary>
    public ushort NewStateIndex;  // +0

    /// <summary>The control flags at byte offset 2.</summary>
    public ushort Flags;          // +2

    /// <summary>The substitution-table index for the marked glyph at byte offset 4.</summary>
    /// <value>An index into the enclosing subtable's substitution table array, or <see cref="NoSubstitution"/>.</value>
    public ushort MarkIndex;      // +4

    /// <summary>The substitution-table index for the current glyph at byte offset 6.</summary>
    /// <value>An index into the enclosing subtable's substitution table array, or <see cref="NoSubstitution"/>.</value>
    public ushort CurrentIndex;   // +6

    /// <summary>Gets a value indicating whether the current glyph becomes the marked glyph after substitution.</summary>
    public readonly bool SetMark => (Flags & 0x8000) != 0;

    /// <summary>Gets a value indicating whether the state machine should stay on the current glyph.</summary>
    public readonly bool DontAdvance => (Flags & 0x4000) != 0;

    /// <summary>Reverses the byte order of all four 16-bit fields in one pass.</summary>
    /// <param name="value">The value whose fields are to be reversed.</param>
    /// <returns>A new entry with all four fields reversed.</returns>
    public static MorxContextualEntry ReverseEndianness(MorxContextualEntry value)
    {
        Span<ushort> words = MemoryMarshal.CreateSpan(ref value.NewStateIndex, 4);
        BinaryPrimitives.ReverseEndianness(words, words);
        return value;
    }
}

/// <summary>A ligature substitution subtable: replaces a sequence of component glyphs with a ligature glyph, driven by an extended state machine.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>After the extended state header, three <c>UInt32</c> offsets point at the ligature action table, the component table, and the ligature list. All three are relative to the state header start and may appear in any order.</description></item>
/// <item><description>Each entry carries a <c>UInt16</c> index into the action table; each action carries a 30-bit signed offset added to the glyph ID to index the component table; the component values are summed to index the ligature list.</description></item>
/// </list>
/// </remarks>
public sealed record MorxLigatureSubtable : MorxSubtable, IDerivedRecord<MorxSubtable, MorxLigatureSubtable>
{
    /// <inheritdoc/>
    public override int Format => 2;

    /// <summary>Gets the extended state machine that drives the ligature formation.</summary>
    public required MorxStateMachine StateMachine { get; init; }

    /// <summary>Gets the entry array that state transitions select into.</summary>
    /// <value>One 6-byte <see cref="MorxLigatureEntry"/> per entry-table slot.</value>
    public required ReadOnlyMemory<MorxLigatureEntry> Entries { get; init; }

    /// <summary>Gets the flat ligature action table.</summary>
    /// <value>One <see cref="MorxLigatureAction"/> per action-table slot. Entries index this array by <see cref="MorxLigatureEntry.LigActionIndex"/>.</value>
    public required ReadOnlyMemory<MorxLigatureAction> Actions { get; init; }

    /// <summary>Gets the component table.</summary>
    /// <value>One <c>UInt16</c> per component-table slot. Each action's signed offset selects an entry by adding the offset to the glyph ID.</value>
    public required ReadOnlyMemory<ushort> ComponentTable { get; init; }

    /// <summary>Gets the flat ligature glyph list.</summary>
    /// <value>One <c>UInt16</c> glyph index per ligature-table slot. The sum of the component values selects an entry.</value>
    public required ReadOnlyMemory<ushort> LigatureGlyphs { get; init; }

    /// <summary>Advances the state machine one step.</summary>
    /// <param name="stateIndex">The current state index.</param>
    /// <param name="glyphClass">The class for the current glyph.</param>
    /// <returns>The entry for the transition.</returns>
    public MorxLigatureEntry Step(int stateIndex, int glyphClass) =>
        Entries.Span[StateMachine.GetEntryIndex(stateIndex, glyphClass)];

    static MorxLigatureSubtable IDerivedRecord<MorxSubtable, MorxLigatureSubtable>.Parse(
        ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
            throw new InvalidDataException(
                "'morx' ligature subtable requires a glyph-count and version context.");

        long subtableStart = cursor.Position;
        SubtableHeader header = cursor.ReadEndianReversibleStruct<SubtableHeader>();
        long subtableEnd = subtableStart + header.Length;

        long stateHeaderStart = cursor.Position;
        STXHeader stx = cursor.ReadEndianReversibleStruct<STXHeader>();
        uint ligActionOffset = cursor.ReadUInt32();
        uint componentOffset = cursor.ReadUInt32();
        uint ligatureOffset = cursor.ReadUInt32();

        long classTableStart = stateHeaderStart + stx.ClassTableOffset;
        long stateArrayStart = stateHeaderStart + stx.StateArrayOffset;
        long entryTableStart = stateHeaderStart + stx.EntryTableOffset;
        long ligActionStart = stateHeaderStart + ligActionOffset;
        long componentStart = stateHeaderStart + componentOffset;
        long ligatureStart = stateHeaderStart + ligatureOffset;

        Span<long> sections = stackalloc long[]
        {
            classTableStart, stateArrayStart, entryTableStart,
            ligActionStart, componentStart, ligatureStart
        };
        long stateArrayEnd = NextSection(stateArrayStart, subtableEnd, sections);
        long entryTableEnd = NextSection(entryTableStart, subtableEnd, sections);
        long ligActionEnd = NextSection(ligActionStart, subtableEnd, sections);
        long componentEnd = NextSection(componentStart, subtableEnd, sections);
        long ligatureEnd = NextSection(ligatureStart, subtableEnd, sections);

        MorxStateMachine stateMachine = MorxStateMachine.Parse(
            ref cursor, stateHeaderStart, stx, stateArrayEnd, ctx.GlyphCount);

        int entryCount = checked((int)((entryTableEnd - entryTableStart) / 6));
        cursor.Position = entryTableStart;
        MorxLigatureEntry[] entries =
            cursor.ReadEndianReversibleStructArray<MorxLigatureEntry>(entryCount);

        int actionCount = checked((int)((ligActionEnd - ligActionStart) / 4));
        cursor.Position = ligActionStart;
        MorxLigatureAction[] actions =
            cursor.ReadEndianReversibleStructArray<MorxLigatureAction>(actionCount);

        int componentCount = checked((int)((componentEnd - componentStart) / 2));
        cursor.Position = componentStart;
        ushort[] components = cursor.ReadUInt16Array(componentCount);

        int ligatureCount = checked((int)((ligatureEnd - ligatureStart) / 2));
        cursor.Position = ligatureStart;
        ushort[] ligatures = cursor.ReadUInt16Array(ligatureCount);

        cursor.Position = subtableEnd;

        return new()
        {
            Length = header.Length,
            Coverage = header.Coverage,
            SubFeatureFlags = header.SubFeatureFlags,
            StateMachine = stateMachine,
            Entries = entries,
            Actions = actions,
            ComponentTable = components,
            LigatureGlyphs = ligatures,
        };
    }
}

/// <summary>A single entry in a ligature subtable: a next-state index, ligature-formation flags, and an index into the action table.</summary>
/// <remarks>All three fields are <c>UInt16</c>, so the reversal is a single word-span pass over three 16-bit words.</remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct MorxLigatureEntry : IEndianReversibleStruct<MorxLigatureEntry>
{
    /// <summary>The zero-based next-state index at byte offset 0.</summary>
    public ushort NextStateIndex;  // +0

    /// <summary>The ligature-formation flags at byte offset 2.</summary>
    public ushort EntryFlags;      // +2

    /// <summary>The index of the first action for this group at byte offset 4.</summary>
    /// <value>An index into the subtable's action table.</value>
    public ushort LigActionIndex;  // +4

    /// <summary>Gets a value indicating whether the current glyph is pushed onto the component stack.</summary>
    public readonly bool SetComponent => (EntryFlags & 0x8000) != 0;

    /// <summary>Gets a value indicating whether the state machine should stay on the current glyph.</summary>
    public readonly bool DontAdvance => (EntryFlags & 0x4000) != 0;

    /// <summary>Gets a value indicating whether the entry terminates a ligature group.</summary>
    public readonly bool PerformAction => (EntryFlags & 0x2000) != 0;

    /// <summary>Reverses the byte order of all three 16-bit fields in one pass.</summary>
    /// <param name="value">The value whose fields are to be reversed.</param>
    /// <returns>A new entry with all three fields reversed.</returns>
    public static MorxLigatureEntry ReverseEndianness(MorxLigatureEntry value)
    {
        Span<ushort> words = MemoryMarshal.CreateSpan(ref value.NextStateIndex, 3);
        BinaryPrimitives.ReverseEndianness(words, words);
        return value;
    }
}

/// <summary>A single ligature action: a flag word and a signed 30-bit component-table offset.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The high two bits are <see cref="Last"/> and <see cref="Store"/>; the low thirty bits are a signed offset added to the glyph ID to select a component-table entry.</description></item>
/// <item><description>The action is a single <c>UInt32</c>, so reversal is a single <see cref="BinaryPrimitives.ReverseEndianness(uint)"/> call.</description></item>
/// </list>
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct MorxLigatureAction : IEndianReversibleStruct<MorxLigatureAction>
{
    /// <summary>The raw action word at byte offset 0.</summary>
    public uint RawAction;  // +0

    /// <summary>Gets a value indicating whether this is the last action in the list.</summary>
    public readonly bool Last => (RawAction & 0x80000000u) != 0;

    /// <summary>Gets a value indicating whether the accumulated index selects a ligature glyph.</summary>
    public readonly bool Store => (RawAction & 0x40000000u) != 0;

    /// <summary>Gets the signed 30-bit offset added to the glyph ID to index the component table.</summary>
    /// <value>The low thirty bits of <see cref="RawAction"/>, sign-extended to 32 bits.</value>
    public readonly int Offset => (int)(RawAction << 2) >> 2;

    /// <summary>Reverses the byte order of the single 32-bit field.</summary>
    /// <param name="value">The value whose field is to be reversed.</param>
    /// <returns>A new action with the field reversed.</returns>
    public static MorxLigatureAction ReverseEndianness(MorxLigatureAction value) => new()
    {
        RawAction = BinaryPrimitives.ReverseEndianness(value.RawAction),
    };
}

/// <summary>A non-contextual glyph substitution subtable: a direct per-glyph lookup with no state machine.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The subtable body is a single <see cref="LookupTable"/> after the 12-byte common header.</description></item>
/// <item><description>A lookup value of zero means "make no substitution"; glyphs the lookup does not cover are kept unchanged.</description></item>
/// </list>
/// </remarks>
public sealed record MorxNonContextualSubtable : MorxSubtable, IDerivedRecord<MorxSubtable, MorxNonContextualSubtable>
{
    /// <inheritdoc/>
    public override int Format => 4;

    /// <summary>Gets the glyph-to-glyph substitution lookup.</summary>
    public required LookupTable Lookup { get; init; }

    /// <summary>Substitutes a glyph, returning the original when the lookup does not cover it or returns zero.</summary>
    /// <param name="glyphId">The original glyph ID.</param>
    /// <returns>The replacement glyph ID, or <paramref name="glyphId"/> when no substitution applies.</returns>
    public int Substitute(int glyphId) =>
        Lookup.TryGetValue(glyphId, out uint newGlyph) && newGlyph != 0
            ? (int)newGlyph
            : glyphId;

    static MorxNonContextualSubtable IDerivedRecord<MorxSubtable, MorxNonContextualSubtable>.Parse(
        ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
            throw new InvalidDataException(
                "'morx' non-contextual subtable requires a glyph-count and version context.");

        long subtableStart = cursor.Position;
        SubtableHeader header = cursor.ReadEndianReversibleStruct<SubtableHeader>();
        LookupTable lookup = cursor.ReadRecord<LookupTable>(new LookupTable.Context(ctx.GlyphCount));
        cursor.Position = subtableStart + header.Length;

        return new()
        {
            Length = header.Length,
            Coverage = header.Coverage,
            SubFeatureFlags = header.SubFeatureFlags,
            Lookup = lookup,
        };
    }
}

/// <summary>A contextual glyph insertion subtable: inserts extra glyphs at the marked or current position, driven by an extended state machine.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>After the extended state header, a <c>UInt32</c> offset points at the flat insertion glyph table, relative to the state header start.</description></item>
/// <item><description>Each entry carries two <c>UInt16</c> indices into that table and two 5-bit insertion counts packed into its flags word; <c>0xFFFF</c> means no insertion for that position.</description></item>
/// </list>
/// </remarks>
public sealed record MorxInsertionSubtable : MorxSubtable, IDerivedRecord<MorxSubtable, MorxInsertionSubtable>
{
    /// <inheritdoc/>
    public override int Format => 5;

    /// <summary>Gets the extended state machine that drives the insertions.</summary>
    public required MorxStateMachine StateMachine { get; init; }

    /// <summary>Gets the entry array that state transitions select into.</summary>
    /// <value>One 8-byte <see cref="MorxInsertionEntry"/> per entry-table slot.</value>
    public required ReadOnlyMemory<MorxInsertionEntry> Entries { get; init; }

    /// <summary>Gets the flat insertion glyph table.</summary>
    /// <value>One <c>UInt16</c> glyph index per insertion-table slot. Entries select a slice by <see cref="MorxInsertionEntry.CurrentInsertIndex"/> / <see cref="MorxInsertionEntry.MarkedInsertIndex"/> and a count.</value>
    public required ReadOnlyMemory<ushort> InsertionGlyphs { get; init; }

    /// <summary>Advances the state machine one step.</summary>
    /// <param name="stateIndex">The current state index.</param>
    /// <param name="glyphClass">The class for the current glyph.</param>
    /// <returns>The entry for the transition.</returns>
    public MorxInsertionEntry Step(int stateIndex, int glyphClass) =>
        Entries.Span[StateMachine.GetEntryIndex(stateIndex, glyphClass)];

    /// <summary>Gets the glyphs to insert at the current position for an entry.</summary>
    /// <param name="entry">The state entry.</param>
    /// <returns>The selected slice of the insertion glyph table, or an empty span when no insertion applies.</returns>
    public ReadOnlySpan<ushort> GetCurrentInsertList(MorxInsertionEntry entry)
    {
        if (entry.CurrentInsertIndex == MorxInsertionEntry.NoInsertion || entry.CurrentInsertCount == 0)
            return ReadOnlySpan<ushort>.Empty;
        int start = entry.CurrentInsertIndex;
        int count = entry.CurrentInsertCount;
        return (uint)start + (uint)count > (uint)InsertionGlyphs.Length
            ? ReadOnlySpan<ushort>.Empty
            : InsertionGlyphs.Span.Slice(start, count);
    }

    /// <summary>Gets the glyphs to insert at the marked position for an entry.</summary>
    /// <param name="entry">The state entry.</param>
    /// <returns>The selected slice of the insertion glyph table, or an empty span when no insertion applies.</returns>
    public ReadOnlySpan<ushort> GetMarkedInsertList(MorxInsertionEntry entry)
    {
        if (entry.MarkedInsertIndex == MorxInsertionEntry.NoInsertion || entry.MarkedInsertCount == 0)
            return ReadOnlySpan<ushort>.Empty;
        int start = entry.MarkedInsertIndex;
        int count = entry.MarkedInsertCount;
        return (uint)start + (uint)count > (uint)InsertionGlyphs.Length
            ? ReadOnlySpan<ushort>.Empty
            : InsertionGlyphs.Span.Slice(start, count);
    }

    static MorxInsertionSubtable IDerivedRecord<MorxSubtable, MorxInsertionSubtable>.Parse(
        ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
            throw new InvalidDataException(
                "'morx' insertion subtable requires a glyph-count and version context.");

        long subtableStart = cursor.Position;
        SubtableHeader header = cursor.ReadEndianReversibleStruct<SubtableHeader>();
        long subtableEnd = subtableStart + header.Length;

        long stateHeaderStart = cursor.Position;
        STXHeader stx = cursor.ReadEndianReversibleStruct<STXHeader>();
        uint insertionActionOffset = cursor.ReadUInt32();

        long insertionTableStart = stateHeaderStart + insertionActionOffset;
        long classTableStart = stateHeaderStart + stx.ClassTableOffset;
        long stateArrayStart = stateHeaderStart + stx.StateArrayOffset;
        long entryTableStart = stateHeaderStart + stx.EntryTableOffset;

        Span<long> sections = stackalloc long[]
        {
            classTableStart, stateArrayStart, entryTableStart, insertionTableStart
        };
        long stateArrayEnd = NextSection(stateArrayStart, subtableEnd, sections);
        long entryTableEnd = NextSection(entryTableStart, subtableEnd, sections);
        long insertionTableEnd = NextSection(insertionTableStart, subtableEnd, sections);

        MorxStateMachine stateMachine = MorxStateMachine.Parse(
            ref cursor, stateHeaderStart, stx, stateArrayEnd, ctx.GlyphCount);

        int entryCount = checked((int)((entryTableEnd - entryTableStart) / 8));
        cursor.Position = entryTableStart;
        MorxInsertionEntry[] entries =
            cursor.ReadEndianReversibleStructArray<MorxInsertionEntry>(entryCount);

        int insertionCount = checked((int)((insertionTableEnd - insertionTableStart) / 2));
        cursor.Position = insertionTableStart;
        ushort[] insertionGlyphs = cursor.ReadUInt16Array(insertionCount);

        cursor.Position = subtableEnd;

        return new()
        {
            Length = header.Length,
            Coverage = header.Coverage,
            SubFeatureFlags = header.SubFeatureFlags,
            StateMachine = stateMachine,
            Entries = entries,
            InsertionGlyphs = insertionGlyphs,
        };
    }
}

/// <summary>A single entry in an insertion subtable: a next-state index, control and flavor flags, and two insertion-table indices.</summary>
/// <remarks>All four fields are <c>UInt16</c>, so the reversal is a single word-span pass over four 16-bit words.</remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct MorxInsertionEntry : IEndianReversibleStruct<MorxInsertionEntry>
{
    /// <summary>The sentinel that means "no insertion" for <see cref="CurrentInsertIndex"/> or <see cref="MarkedInsertIndex"/>.</summary>
    public const ushort NoInsertion = 0xFFFF;

    /// <summary>The zero-based next-state index at byte offset 0.</summary>
    public ushort NewStateIndex;        // +0

    /// <summary>The action flags at byte offset 2.</summary>
    public ushort Flags;                // +2

    /// <summary>The insertion-table index for the current position at byte offset 4.</summary>
    public ushort CurrentInsertIndex;   // +4

    /// <summary>The insertion-table index for the marked position at byte offset 6.</summary>
    public ushort MarkedInsertIndex;    // +6

    /// <summary>Gets a value indicating whether the current glyph becomes the marked glyph.</summary>
    public readonly bool SetMark => (Flags & 0x8000) != 0;

    /// <summary>Gets a value indicating whether the state machine should stay on the current glyph.</summary>
    public readonly bool DontAdvance => (Flags & 0x4000) != 0;

    /// <summary>Gets a value indicating whether the current insertion behaves like a kashida.</summary>
    public readonly bool CurrentIsKashidaLike => (Flags & 0x2000) != 0;

    /// <summary>Gets a value indicating whether the marked insertion behaves like a kashida.</summary>
    public readonly bool MarkedIsKashidaLike => (Flags & 0x1000) != 0;

    /// <summary>Gets a value indicating whether the current insertion goes before the current glyph.</summary>
    public readonly bool CurrentInsertBefore => (Flags & 0x0800) != 0;

    /// <summary>Gets a value indicating whether the marked insertion goes before the marked glyph.</summary>
    public readonly bool MarkedInsertBefore => (Flags & 0x0400) != 0;

    /// <summary>Gets the number of glyphs to insert at the current position.</summary>
    public readonly byte CurrentInsertCount => (byte)((Flags & 0x03E0) >> 5);

    /// <summary>Gets the number of glyphs to insert at the marked position.</summary>
    public readonly byte MarkedInsertCount => (byte)(Flags & 0x001F);

    /// <summary>Reverses the byte order of all four 16-bit fields in one pass.</summary>
    /// <param name="value">The value whose fields are to be reversed.</param>
    /// <returns>A new entry with all four fields reversed.</returns>
    public static MorxInsertionEntry ReverseEndianness(MorxInsertionEntry value)
    {
        Span<ushort> words = MemoryMarshal.CreateSpan(ref value.NewStateIndex, 4);
        BinaryPrimitives.ReverseEndianness(words, words);
        return value;
    }
}

/// <summary>The per-chain subtable glyph coverage table introduced in version 3: one packed bitfield per subtable indicating which glyphs that subtable can affect.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>An array of <c>UInt32</c> offsets, one per subtable, each relative to the start of the coverage table. Offset zero means the subtable has no coverage bitfield.</description></item>
/// <item><description>Each bitfield is <c>(numGlyphs + 7) / 8</c> bytes; bit <c>g &amp; 7</c> of byte <c>g &gt;&gt; 3</c> is set when glyph <c>g</c> is relevant to that subtable.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MorxChain.Coverage"/>
public sealed record MorxGlyphCoverageTable : IRecord<MorxGlyphCoverageTable>
{
    /// <summary>Gets the per-subtable coverage bitfields.</summary>
    /// <value>One entry per subtable, in subtable order. An empty memory means the subtable has no coverage bitfield.</value>
    public required IReadOnlyList<ReadOnlyMemory<byte>> Bitfields { get; init; }

    /// <summary>Gets the number of subtables the table covers.</summary>
    /// <value><see cref="Bitfields"/>.Count, equal to the chain's subtable count.</value>
    public int SubtableCount => Bitfields.Count;

    /// <summary>Determines whether a given subtable covers a given glyph.</summary>
    /// <param name="glyphId">The glyph ID to test.</param>
    /// <param name="subtableIndex">The subtable index in <c>[0, SubtableCount)</c>.</param>
    /// <returns><see langword="true"/> when the subtable's bit is set for the glyph; otherwise <see langword="false"/>.</returns>
    /// <remarks>Out-of-range inputs return <see langword="false"/>; the caller need not pre-validate.</remarks>
    public bool IsCovered(int glyphId, int subtableIndex)
    {
        if (glyphId < 0 || (uint)subtableIndex >= (uint)Bitfields.Count)
            return false;

        ReadOnlyMemory<byte> bitfield = Bitfields[subtableIndex];
        if (bitfield.IsEmpty)
            return false;

        int byteIndex = glyphId >> 3;
        return (uint)byteIndex < (uint)bitfield.Length
            && (bitfield.Span[byteIndex] & (1 << (glyphId & 7))) != 0;
    }

    static MorxGlyphCoverageTable IRecord<MorxGlyphCoverageTable>.Parse(
        ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
            throw new InvalidDataException(
                "'morx' glyph coverage table requires a glyph-count and subtable-count context.");

        long coverageStart = cursor.Position;
        uint[] offsets = cursor.ReadUInt32Array(ctx.SubtableCount);
        int bitfieldSize = (ctx.GlyphCount + 7) >> 3;

        var bitfields = new ReadOnlyMemory<byte>[ctx.SubtableCount];
        for (int i = 0; i < offsets.Length; i++)
        {
            if (offsets[i] == 0)
            {
                bitfields[i] = ReadOnlyMemory<byte>.Empty;
                continue;
            }

            long bitfieldStart = coverageStart + offsets[i];
            bitfields[i] = cursor.Source.ReadUInt8ArrayAt(bitfieldStart, bitfieldSize);
        }

        return new() { Bitfields = bitfields };
    }

    /// <summary>Context for parsing a <see cref="MorxGlyphCoverageTable"/>: the font's glyph count and the chain's subtable count.</summary>
    /// <param name="GlyphCount">Number of glyphs in the font, from <c>maxp.numGlyphs</c>.</param>
    /// <param name="SubtableCount">The number of subtables in the chain; the number of offsets and bitfields.</param>
    public record Context(int GlyphCount, int SubtableCount);
}
