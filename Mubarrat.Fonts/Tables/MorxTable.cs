using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// morx — Extended Glyph Metamorphosis Table (Apple Advanced Typography)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>morx</c> table: the extended glyph metamorphosis table, which describes contextual transformations of the glyph stream such as ligature formation, cursive connection, and glyph rearrangement.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table supersedes the older <c>mort</c> table and is driven by the same model: a set of chains, each holding a feature table and a sequence of subtables. A chain's feature table turns a set of requested feature settings into the sub-feature flag word that decides which of its subtables run.</description></item>
/// <item><description>Sub-features are computed by starting from <see cref="MorxChain.DefaultFlags"/> and, for every feature entry whose setting the caller requested, ANDing in <see cref="MorxFeatureEntry.DisableFlags"/> and ORing in <see cref="MorxFeatureEntry.EnableFlags"/>. Entries later in the feature table take precedence, so a chain conventionally ends with the "enable glyph effects off" entry that clears every flag.</description></item>
/// <item><description>Each subtable declares a <see cref="MorxSubtableHeader.SubFeatureFlags"/> mask. The subtable runs when that mask ANDed with the computed sub-feature flags is non-zero.</description></item>
/// <item><description>The five subtable types are rearrangement (<see cref="MorxRearrangementSubtable"/>), contextual (<see cref="MorxContextualSubtable"/>), ligature (<see cref="MorxLigatureSubtable"/>), noncontextual (<see cref="MorxNoncontextualSubtable"/>), and insertion (<see cref="MorxInsertionSubtable"/>).</description></item>
/// <item><description>Each subtable that uses a state machine does so through the extended state header defined by this table, which carries 32-bit offsets and 16-bit state array entries rather than the narrower form the older <c>mort</c> table used. <see cref="AatStateTable"/> models the same structure; this table declares its own header because it also carries the subtable-specific offset words that follow it.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html"><c>morx</c> table</see> in the TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MorxChain"/>
/// <seealso cref="MorxSubtable"/>
/// <seealso cref="AatStateTable"/>
/// <seealso cref="LtagTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
public sealed record MorxTable : IFontTable<MorxTable>
{
    /// <summary>Gets the Apple Advanced Typography table tag <c>morx</c>.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
    public static Tag Tag => "morx";

    /// <summary>Gets the table version. Either 2 or 3.</summary>
    /// <remarks>Version 2 is the original extended layout. Version 3 adds the per-chain subtable glyph coverage array that lets a layout engine skip a subtable whose glyphs do not occur in the run being shaped.</remarks>
    /// <seealso cref="HasSubtableGlyphCoverage"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the chains, in table order.</summary>
    /// <remarks>Chain order is significant: the transformations of one chain feed the next.</remarks>
    /// <seealso cref="ChainCount"/>
    public IReadOnlyList<MorxChain> Chains { get; init; } = [];

    /// <summary>Gets the number of chains.</summary>
    /// <seealso cref="Chains"/>
    public int ChainCount => Chains.Count;

    /// <summary>Gets a value indicating whether the table carries a subtable glyph coverage array after the last subtable of each chain.</summary>
    /// <remarks>The array is present when <see cref="Version"/> is 3 or greater.</remarks>
    /// <seealso cref="Version"/>
    public bool HasSubtableGlyphCoverage => Version >= 3;

    /// <summary>Computes the sub-feature flag word a chain produces for a set of requested feature settings.</summary>
    /// <param name="chainIndex">The index of the chain to evaluate.</param>
    /// <param name="isRequested">A predicate that reports whether a given feature type and setting were requested by the caller.</param>
    /// <returns>The computed sub-feature flag word.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="chainIndex"/> is not a valid chain index.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="isRequested"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The result starts at the chain's <see cref="MorxChain.DefaultFlags"/> and each requested entry is applied in table order, so later entries win.</description></item>
    /// <item><description>The predicate is supplied rather than a list of requested features because callers hold their requests in different shapes — a feature registry result, a CoreText feature array, or a caller-supplied tuple list.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MorxChain.DefaultFlags"/>
    /// <seealso cref="MorxFeatureEntry"/>
    public uint ComputeSubFeatureFlags(int chainIndex, Func<ushort, ushort, bool> isRequested)
    {
        ArgumentNullException.ThrowIfNull(isRequested);
        ArgumentOutOfRangeException.ThrowIfNegative(chainIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(chainIndex, Chains.Count);

        var chain = Chains[chainIndex];
        uint result = chain.DefaultFlags;

        foreach (var feature in chain.Features)
        {
            if (!isRequested(feature.FeatureType, feature.FeatureSetting)) continue;
            result = (result & feature.DisableFlags) | feature.EnableFlags;
        }

        return result;
    }

    /// <summary>Computes the sub-feature flag word for a set of requested feature type and setting pairs.</summary>
    /// <param name="chainIndex">The index of the chain to evaluate.</param>
    /// <param name="requested">The requested feature type and setting pairs. May be empty.</param>
    /// <returns>The computed sub-feature flag word.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="chainIndex"/> is not a valid chain index.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="requested"/> is <see langword="null"/>.</exception>
    /// <remarks>Convenience overload over <see cref="ComputeSubFeatureFlags(int, Func{ushort, ushort, bool})"/> for callers that already hold their requests as a sequence of pairs.</remarks>
    /// <seealso cref="ComputeSubFeatureFlags(int, Func{ushort, ushort, bool})"/>
    public uint ComputeSubFeatureFlags(int chainIndex, IEnumerable<(ushort FeatureType, ushort FeatureSetting)> requested)
    {
        ArgumentNullException.ThrowIfNull(requested);

        var set = new HashSet<(ushort, ushort)>(requested);
        return ComputeSubFeatureFlags(chainIndex, (type, setting) => set.Contains((type, setting)));
    }

    /// <summary>The mask of the coverage word that selects vertical-only processing.</summary>
    /// <seealso cref="MorxSubtableHeader.Coverage"/>
    public const uint CoverageVertical = 0x80000000;

    /// <summary>The mask of the coverage word that selects descending glyph order.</summary>
    /// <seealso cref="MorxSubtableHeader.Coverage"/>
    public const uint CoverageDescending = 0x40000000;

    /// <summary>The mask of the coverage word that makes a subtable apply to both horizontal and vertical text.</summary>
    /// <seealso cref="MorxSubtableHeader.Coverage"/>
    public const uint CoverageBoth = 0x20000000;

    /// <summary>The mask of the coverage word that selects logical rather than layout order.</summary>
    /// <seealso cref="MorxSubtableHeader.Coverage"/>
    public const uint CoverageLogical = 0x10000000;

    /// <summary>The reserved bits of the coverage word, which the manual requires to be zero.</summary>
    /// <seealso cref="MorxSubtableHeader.Coverage"/>
    public const uint CoverageReservedMask = 0x0FFFFF00;

    /// <summary>The mask of the coverage word that selects the subtable type.</summary>
    /// <seealso cref="MorxSubtableHeader.Coverage"/>
    public const uint CoverageTypeMask = 0x000000FF;

    /// <summary>The subtable types a <c>morx</c> chain may contain.</summary>
    /// <seealso cref="MorxSubtable.Type"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
    public enum SubtableType : byte
    {
        /// <summary>A rearrangement subtable: reorders glyphs without changing them.</summary>
        Rearrangement = 0,

        /// <summary>A contextual subtable: substitutes glyphs according to context.</summary>
        Contextual = 1,

        /// <summary>A ligature subtable: replaces a glyph sequence with a single ligature glyph.</summary>
        Ligature = 2,

        /// <summary>A noncontextual, or swash, subtable: substitutes each glyph independently.</summary>
        Noncontextual = 4,

        /// <summary>An insertion subtable: inserts glyphs into the stream.</summary>
        Insertion = 5,
    }

    /// <summary>Reads a <c>morx</c> table from <paramref name="cursor"/>. The cursor is advanced past the whole table.</summary>
    /// <param name="cursor">The cursor positioned at the first byte of the table.</param>
    /// <param name="context">Ignored; the table is self-describing.</param>
    /// <returns>The parsed table.</returns>
    /// <exception cref="InvalidDataException">The version is not 2 or 3.</exception>
    /// <exception cref="EndOfStreamException">The table's structures extend past the end of the table-scoped source.</exception>
    static MorxTable IRecord<MorxTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version is not (2 or 3))
        {
            throw new InvalidDataException(
                $"'morx'.version is {header.Version}, expected 2 or 3.");
        }

        int contextGlyphCount = KerxTable.ResolveGlyphCount(context);

        int chainCount = checked((int)header.ChainCount);
        var chains = new MorxChain[chainCount];
        for (int i = 0; i < chains.Length; i++)
        {
            long chainStart = cursor.Position;
            chains[i] = MorxChain.Parse(ref cursor, header.Version, contextGlyphCount);

            // chainLength is authoritative; honour it even when the subtables left the cursor elsewhere.
            long consumed = cursor.Position - chainStart;
            if (chains[i].Length > consumed) cursor.Position = chainStart + chains[i].Length;
        }

        return new MorxTable { Version = header.Version, Chains = chains };
    }

    /// <summary>The 8-byte fixed-layout <c>morx</c> table header.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0. Either 2 or 3.</summary>
        public ushort Version;   // +0

        /// <summary>The unused field at byte offset 2.</summary>
        /// <remarks>The manual specifies 0. Surfaced rather than discarded so a validator can flag a non-conforming writer.</remarks>
        public ushort Unused;    // +2

        /// <summary>The number of metamorphosis chains at byte offset 4.</summary>
        public uint ChainCount;  // +4

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            Unused = BinaryPrimitives.ReverseEndianness(v.Unused),
            ChainCount = BinaryPrimitives.ReverseEndianness(v.ChainCount),
        };
    }

    /// <summary>The 16-byte fixed-layout chain header that introduces each metamorphosis chain.</summary>
    /// <seealso cref="MorxChain"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct ChainHeader : IEndianReversibleStruct<ChainHeader>
    {
        /// <summary>The default sub-feature flags at byte offset 0.</summary>
        public uint DefaultFlags;     // +0

        /// <summary>The total length of the chain in bytes, including this header, at byte offset 4.</summary>
        /// <remarks>The manual requires a multiple of four.</remarks>
        public uint ChainLength;      // +4

        /// <summary>The number of feature table entries at byte offset 8.</summary>
        public uint FeatureCount;     // +8

        /// <summary>The number of subtables in the chain at byte offset 12.</summary>
        public uint SubtableCount;    // +12

        /// <summary>Reverses the byte order of every field in a <see cref="ChainHeader"/>.</summary>
        public static ChainHeader ReverseEndianness(ChainHeader v) => new()
        {
            DefaultFlags = BinaryPrimitives.ReverseEndianness(v.DefaultFlags),
            ChainLength = BinaryPrimitives.ReverseEndianness(v.ChainLength),
            FeatureCount = BinaryPrimitives.ReverseEndianness(v.FeatureCount),
            SubtableCount = BinaryPrimitives.ReverseEndianness(v.SubtableCount),
        };
    }

    /// <summary>One entry of a chain's feature table.</summary>
    /// <seealso cref="MorxChain.Features"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct FeatureEntry : IEndianReversibleStruct<FeatureEntry>
    {
        /// <summary>The feature type at byte offset 0.</summary>
        /// <remarks>Feature type 39 carries language-specific glyph selection and is interpreted together with the <c>ltag</c> table.</remarks>
        /// <seealso cref="LtagTable"/>
        public ushort FeatureType;    // +0

        /// <summary>The feature setting, also called the selector, at byte offset 2.</summary>
        public ushort FeatureSetting; // +2

        /// <summary>The flags the entry enables at byte offset 4.</summary>
        public uint EnableFlags;      // +4

        /// <summary>The flags the entry disables at byte offset 8.</summary>
        public uint DisableFlags;     // +8

        /// <summary>Reverses the byte order of every field in a <see cref="FeatureEntry"/>.</summary>
        public static FeatureEntry ReverseEndianness(FeatureEntry v) => new()
        {
            FeatureType = BinaryPrimitives.ReverseEndianness(v.FeatureType),
            FeatureSetting = BinaryPrimitives.ReverseEndianness(v.FeatureSetting),
            EnableFlags = BinaryPrimitives.ReverseEndianness(v.EnableFlags),
            DisableFlags = BinaryPrimitives.ReverseEndianness(v.DisableFlags),
        };
    }

    /// <summary>The 12-byte fixed-layout header that introduces every <c>morx</c> subtable.</summary>
    /// <seealso cref="MorxSubtable.Header"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct SubtableHeader : IEndianReversibleStruct<SubtableHeader>
    {
        /// <summary>The total subtable length in bytes, including this header, at byte offset 0.</summary>
        public uint Length;            // +0

        /// <summary>The coverage flags and subtable type word at byte offset 4.</summary>
        public uint Coverage;          // +4

        /// <summary>The sub-feature flag mask at byte offset 8.</summary>
        public uint SubFeatureFlags;   // +8

        /// <summary>Reverses the byte order of every field in a <see cref="SubtableHeader"/>.</summary>
        public static SubtableHeader ReverseEndianness(SubtableHeader v) => new()
        {
            Length = BinaryPrimitives.ReverseEndianness(v.Length),
            Coverage = BinaryPrimitives.ReverseEndianness(v.Coverage),
            SubFeatureFlags = BinaryPrimitives.ReverseEndianness(v.SubFeatureFlags),
        };
    }
}

/// <summary>One metamorphosis chain: a default sub-feature flag word, a feature table, and the subtables that implement the chain's transformations.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The chain header declares <see cref="Length"/> so that a reader can move to the next chain without having parsed every subtable. <see cref="MorxTable"/> uses that field rather than trusting the parsed subtables to consume exactly the right number of bytes.</description></item>
/// <item><description>When the <c>morx</c> version is 3 or greater, a subtable glyph coverage array follows the last subtable inside the chain.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MorxTable"/>
/// <seealso cref="MorxFeatureEntry"/>
/// <seealso cref="MorxSubtable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
public sealed record MorxChain
{
    /// <summary>Gets the chain's default sub-feature flags.</summary>
    /// <remarks>The default flags are the starting point for the sub-feature computation and describe the transformations that happen when the caller requests nothing.</remarks>
    /// <seealso cref="Features"/>
    public uint DefaultFlags { get; init; }

    /// <summary>Gets the total chain length in bytes, including the chain header.</summary>
    /// <seealso cref="MorxTable.ChainCount"/>
    public uint Length { get; init; }

    /// <summary>Gets the feature table entries, in table order.</summary>
    /// <remarks>Later entries take precedence over earlier ones; see <see cref="MorxTable.ComputeSubFeatureFlags(int, Func{ushort, ushort, bool})"/> and <see cref="MorxTable.ComputeSubFeatureFlags(int, IEnumerable{ValueTuple{ushort, ushort}})"/>.</remarks>
    /// <seealso cref="MorxFeatureEntry"/>
    public IReadOnlyList<MorxFeatureEntry> Features { get; init; } = [];

    /// <summary>Gets the chain's subtables, in table order.</summary>
    /// <seealso cref="MorxSubtable"/>
    public IReadOnlyList<MorxSubtable> Subtables { get; init; } = [];

    /// <summary>Gets the subtable glyph coverage array, or <see langword="null"/> when the table version does not supply one.</summary>
    /// <remarks>Present only for <c>morx</c> version 3 and later.</remarks>
    /// <seealso cref="MorxTable.HasSubtableGlyphCoverage"/>
    /// <seealso cref="MorxSubtableGlyphCoverage"/>
    public MorxSubtableGlyphCoverage? GlyphCoverage { get; init; }

    /// <summary>Gets the number of feature entries.</summary>
    /// <seealso cref="Features"/>
    public int FeatureCount => Features.Count;

    /// <summary>Gets the number of subtables.</summary>
    /// <seealso cref="Subtables"/>
    public int SubtableCount => Subtables.Count;

    /// <summary>Reads one chain, leaving the cursor at the first byte after its subtables.</summary>
    /// <param name="cursor">The cursor positioned at the chain header.</param>
    /// <param name="version">The enclosing <c>morx</c> table version, which decides whether a coverage array follows.</param>
    /// <param name="contextGlyphCount">The font's glyph count, used to size a format 0 class lookup in the chain's subtables, or 0 when unknown.</param>
    /// <returns>The parsed chain.</returns>
    /// <exception cref="EndOfStreamException">A structure extends past the end of the table-scoped source.</exception>
    /// <remarks>The per-subtable glyph coverage array is only read when <paramref name="version"/> is 3 or greater, and it is read at the chain's declared end rather than immediately after the last parsed subtable.</remarks>
    internal static MorxChain Parse(ref Cursor cursor, ushort version, int contextGlyphCount)
    {
        long chainStart = cursor.Position;

        MorxTable.ChainHeader header = cursor.ReadBigEndianStruct<MorxTable.ChainHeader>();

        var features = new MorxFeatureEntry[header.FeatureCount];
        for (int i = 0; i < features.Length; i++)
        {
            MorxTable.FeatureEntry entry = cursor.ReadBigEndianStruct<MorxTable.FeatureEntry>();
            features[i] = new MorxFeatureEntry
            {
                FeatureType = entry.FeatureType,
                FeatureSetting = entry.FeatureSetting,
                EnableFlags = entry.EnableFlags,
                DisableFlags = entry.DisableFlags,
            };
        }

        var subtables = new MorxSubtable[header.SubtableCount];
        for (int i = 0; i < subtables.Length; i++)
            subtables[i] = MorxSubtable.Parse(ref cursor, version, contextGlyphCount);

        MorxSubtableGlyphCoverage? coverage = null;
        if (version >= 3 && header.SubtableCount > 0)
        {
            long afterSubtables = cursor.Position;
            long chainEnd = chainStart + header.ChainLength;
            if (afterSubtables < chainEnd)
                coverage = MorxSubtableGlyphCoverage.Parse(ref cursor, afterSubtables, header.SubtableCount);
        }

        return new MorxChain
        {
            DefaultFlags = header.DefaultFlags,
            Length = header.ChainLength,
            Features = features,
            Subtables = subtables,
            GlyphCoverage = coverage,
        };
    }
}

/// <summary>One entry of a <c>morx</c> chain's feature table.</summary>
/// <seealso cref="MorxChain.Features"/>
/// <seealso cref="MorxTable.FeatureEntry"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
public sealed record MorxFeatureEntry
{
    /// <summary>Gets the feature type.</summary>
    /// <remarks>Feature type 39 selects language-specific glyphs and is interpreted together with the <c>ltag</c> table; its setting is one more than an index into that table.</remarks>
    /// <seealso cref="LtagTable"/>
    public ushort FeatureType { get; init; }

    /// <summary>Gets the feature setting, also called the selector.</summary>
    public ushort FeatureSetting { get; init; }

    /// <summary>Gets the sub-feature flags this entry turns on.</summary>
    public uint EnableFlags { get; init; }

    /// <summary>Gets the sub-feature flags this entry turns off.</summary>
    public uint DisableFlags { get; init; }
}

/// <summary>A <c>morx</c> subtable glyph coverage array: one coverage bitfield per subtable, used to skip a subtable whose glyphs do not occur in the run being shaped.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The array begins with one 32-bit offset per subtable, measured from the start of the array. An offset of zero means the corresponding subtable has no coverage bitfield and must always be run.</description></item>
/// <item><description>Each bitfield is <c>(glyphCount + 7) / 8</c> bytes, padded to a four-byte boundary. Glyph <c>g</c> is covered when bit <c>g &amp; 7</c> of byte <c>g / 8</c> is set.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">subtable glyph coverage table</see> section of the <c>morx</c> table.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MorxChain.GlyphCoverage"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
public sealed record MorxSubtableGlyphCoverage
{
    /// <summary>Gets the coverage byte array for each subtable; a <see langword="null"/> entry means that subtable declares no coverage bitfield.</summary>
    /// <remarks>Index <c>i</c> corresponds to subtable <c>i</c> of the chain.</remarks>
    public IReadOnlyList<byte[]?> Bitfields { get; init; } = [];

    /// <summary>Gets the number of coverage entries.</summary>
    /// <seealso cref="Bitfields"/>
    public int Count => Bitfields.Count;

    /// <summary>Reports whether a subtable's coverage bitfield covers a glyph.</summary>
    /// <param name="subtableIndex">The subtable index within the chain.</param>
    /// <param name="glyphIndex">The glyph index to test.</param>
    /// <returns><see langword="true"/> when the subtable has no bitfield, or when its bitfield covers <paramref name="glyphIndex"/>.</returns>
    /// <remarks>A subtable with no bitfield is reported as covering every glyph, because the array cannot prove it irrelevant.</remarks>
    /// <seealso cref="Bitfields"/>
    public bool Covers(int subtableIndex, int glyphIndex)
    {
        if ((uint)subtableIndex >= (uint)Bitfields.Count) return true;

        byte[]? bitfield = Bitfields[subtableIndex];
        if (bitfield is null) return true;

        int byteIndex = glyphIndex >> 3;
        if ((uint)byteIndex >= (uint)bitfield.Length) return false;

        return (bitfield[byteIndex] & (1 << (glyphIndex & 7))) != 0;
    }

    /// <summary>Reads a coverage array beginning at <paramref name="cursor"/>'s position.</summary>
    /// <param name="cursor">The cursor positioned at the array's first offset word.</param>
    /// <param name="arrayStart">The absolute offset of the array, which its internal offsets are measured from.</param>
    /// <param name="subtableCount">The number of subtables in the chain, and therefore the number of offset words.</param>
    /// <returns>The parsed coverage array.</returns>
    /// <exception cref="EndOfStreamException">An offset word or bitfield extends past the end of the table-scoped source.</exception>
    /// <remarks>Bitfield lengths are derived from the distance to the next bitfield, since the array declares no lengths. The last bitfield therefore runs to the end of the source.</remarks>
    internal static MorxSubtableGlyphCoverage Parse(ref Cursor cursor, long arrayStart, uint subtableCount)
    {
        int count = checked((int)subtableCount);
        var offsets = cursor.ReadUInt32Array(count);

        var bitfields = new byte[]?[count];
        for (int i = 0; i < count; i++)
        {
            uint offset = offsets[i];
            if (offset == 0) continue;

            long start = arrayStart + offset;

            // The bitfield runs to the next declared bitfield, or to the end of the source for the last one.
            long end = cursor.Length;
            for (int j = 0; j < count; j++)
            {
                if (offsets[j] <= offset) continue;

                long candidate = arrayStart + offsets[j];
                if (candidate < end) end = candidate;
            }

            int length = checked((int)(end - start));
            bitfields[i] = length <= 0 ? [] : cursor.Source.ReadBytesAt(start, length);
        }

        return new MorxSubtableGlyphCoverage { Bitfields = bitfields };
    }
}

/// <summary>A <c>morx</c> chain subtable: the common header plus one format-specific payload.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The abstract base carries the common header; the concrete subtypes are <see cref="MorxRearrangementSubtable"/>, <see cref="MorxContextualSubtable"/>, <see cref="MorxLigatureSubtable"/>, <see cref="MorxNoncontextualSubtable"/>, and <see cref="MorxInsertionSubtable"/>.</description></item>
/// <item><description><see cref="MorxSubtable.Parse(ref Cursor, ushort, int)"/> reads the leading type byte of the coverage word, constructs the matching subtype, and leaves the cursor at the end of the subtable as declared by the subtable length.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MorxChain.Subtables"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
public abstract record MorxSubtable
{
    /// <summary>Gets the subtable's common header.</summary>
    /// <seealso cref="Header"/>
    public required MorxSubtableHeader Header { get; init; }

    /// <summary>Gets the subtable type, taken from the low byte of the coverage word.</summary>
    /// <seealso cref="MorxTable.SubtableType"/>
    public MorxTable.SubtableType Type => (MorxTable.SubtableType)(Header.Coverage & MorxTable.CoverageTypeMask);

    /// <summary>Gets a value indicating whether the subtable applies to vertical text.</summary>
    /// <remarks>False when the subtable applies to both orientations.</remarks>
    /// <seealso cref="AppliesToBoth"/>
    public bool IsVertical => (Header.Coverage & MorxTable.CoverageVertical) != 0
        && (Header.Coverage & MorxTable.CoverageBoth) == 0;

    /// <summary>Gets a value indicating whether the subtable applies to both horizontal and vertical text.</summary>
    /// <seealso cref="IsVertical"/>
    public bool AppliesToBoth => (Header.Coverage & MorxTable.CoverageBoth) != 0;

    /// <summary>Gets a value indicating whether the subtable processes glyphs in descending order.</summary>
    public bool IsDescending => (Header.Coverage & MorxTable.CoverageDescending) != 0;

    /// <summary>Gets a value indicating whether the subtable processes glyphs in logical rather than layout order.</summary>
    public bool IsLogical => (Header.Coverage & MorxTable.CoverageLogical) != 0;

    /// <summary>Reads one subtable, dispatching on the subtable type.</summary>
    /// <param name="cursor">The cursor positioned at the subtable's common header.</param>
    /// <param name="version">The enclosing <c>morx</c> table version, recorded on the returned subtable's header.</param>
    /// <param name="glyphCount">The font's glyph count, used to size a format 0 class lookup, or 0 when unknown.</param>
    /// <returns>The parsed subtable, typed as the format-specific subclass.</returns>
    /// <exception cref="InvalidDataException">The subtable type is reserved or unknown.</exception>
    /// <exception cref="EndOfStreamException">A structure extends past the end of the table-scoped source.</exception>
    /// <remarks>The cursor is left at the first byte after the subtable, using the subtable's declared length, so that a caller can read a sequence of subtables back to back.</remarks>
    /// <seealso cref="MorxChain.Subtables"/>
    public static MorxSubtable Parse(ref Cursor cursor, ushort version, int glyphCount = 0)
    {
        long start = cursor.Position;

        MorxTable.SubtableHeader header = cursor.ReadBigEndianStruct<MorxTable.SubtableHeader>();
        long payloadStart = cursor.Position;

        var type = (MorxTable.SubtableType)(header.Coverage & MorxTable.CoverageTypeMask);

        MorxSubtable subtable = type switch
        {
            MorxTable.SubtableType.Rearrangement => MorxRearrangementSubtable.Parse(ref cursor, header, payloadStart, glyphCount),
            MorxTable.SubtableType.Contextual => MorxContextualSubtable.Parse(ref cursor, header, payloadStart, glyphCount),
            MorxTable.SubtableType.Ligature => MorxLigatureSubtable.Parse(ref cursor, header, payloadStart, glyphCount),
            MorxTable.SubtableType.Noncontextual => MorxNoncontextualSubtable.Parse(ref cursor, header, glyphCount),
            MorxTable.SubtableType.Insertion => MorxInsertionSubtable.Parse(ref cursor, header, payloadStart, glyphCount),
            _ => throw new InvalidDataException(
                $"'morx' subtable type is {(byte)type}, expected 0, 1, 2, 4, or 5."),
        };

        // The declared length is authoritative; honour it even if a payload parse stopped short.
        long declaredEnd = start + header.Length;
        if (declaredEnd > cursor.Position) cursor.Position = declaredEnd;

        return subtable;
    }
}

/// <summary>The common <c>morx</c> subtable header, in semantic form.</summary>
/// <seealso cref="MorxSubtable.Header"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
public sealed record MorxSubtableHeader
{
    /// <summary>Gets the total subtable length in bytes, including this header.</summary>
    public uint Length { get; init; }

    /// <summary>Gets the coverage flags and subtable type word.</summary>
    /// <seealso cref="MorxTable.CoverageTypeMask"/>
    public uint Coverage { get; init; }

    /// <summary>Gets the sub-feature flag mask that decides whether the subtable runs.</summary>
    /// <remarks>The subtable runs when this mask ANDed with the chain's computed sub-feature flags is non-zero.</remarks>
    public uint SubFeatureFlags { get; init; }
}

/// <summary>A <c>morx</c> rearrangement subtable: a state machine that reorders runs of glyphs without substituting them.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The payload is only an extended state table; the entry table rows are two 16-bit fields, <see cref="RearrangementEntry.NewState"/> and <see cref="RearrangementEntry.Flags"/>.</description></item>
/// <item><description>The flag bits implement the verbs the rearrangement state machine defines — <c>markFirst</c>, <c>dontAdvance</c>, <c>markLast</c>, <c>verb</c>, and the <c>mark</c> field that names the glyph a verb acts on. The verbs reorder the marked run.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html"><c>morx</c> table</see> for the verb encoding.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MorxSubtable"/>
/// <seealso cref="RearrangementEntry"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
public sealed record MorxRearrangementSubtable : MorxSubtable
{
    /// <summary>Gets the extended state table that drives the rearrangement.</summary>
    /// <seealso cref="AatStateTable"/>
    public required AatStateTable StateTable { get; init; }

    /// <summary>Reads the payload of a rearrangement subtable.</summary>
    /// <param name="cursor">The cursor positioned at the payload, which is the state table header.</param>
    /// <param name="header">The already-read common subtable header.</param>
    /// <param name="payloadStart">The absolute offset of the payload.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="EndOfStreamException">A structure extends past the end of the table-scoped source.</exception>
    internal static MorxRearrangementSubtable Parse(ref Cursor cursor, MorxTable.SubtableHeader header, long payloadStart, int glyphCount)
    {
        AatStateTable stateTable = AatStateTable.Read(ref cursor, payloadStart, RearrangementEntry.SizeInBytes, glyphCount);

        return new MorxRearrangementSubtable
        {
            Header = new MorxSubtableHeader
            {
                Length = header.Length,
                Coverage = header.Coverage,
                SubFeatureFlags = header.SubFeatureFlags,
            },
            StateTable = stateTable,
        };
    }

    /// <summary>One entry of a rearrangement subtable's entry table.</summary>
    /// <seealso cref="MorxRearrangementSubtable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct RearrangementEntry : IEndianReversibleStruct<RearrangementEntry>
    {
        /// <summary>The size of one entry, in bytes.</summary>
        public const int SizeInBytes = 4;

        /// <summary>The zero-based index of the state array row to use for the next glyph at byte offset 0.</summary>
        public ushort NewState;  // +0

        /// <summary>The action flags at byte offset 2.</summary>
        public ushort Flags;     // +2

        /// <summary>Reverses the byte order of both fields in a <see cref="RearrangementEntry"/>.</summary>
        public static RearrangementEntry ReverseEndianness(RearrangementEntry v) => new()
        {
            NewState = BinaryPrimitives.ReverseEndianness(v.NewState),
            Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
        };
    }
}

/// <summary>A <c>morx</c> contextual subtable: a state machine that substitutes glyphs according to their context.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The payload is an extended state table followed by a 32-bit offset to the per-glyph lookup table space.</description></item>
/// <item><description>An entry's <see cref="ContextualEntry.MarkIndex"/> and <see cref="ContextualEntry.CurrentIndex"/> are indices into an array of 32-bit offsets at the start of that lookup space, and each selected lookup table then maps a glyph index onto its substitute. A value of <c>-1</c> means no substitution.</description></item>
/// <item><description>The number of per-glyph lookup tables is not declared. It is derived here from the span between the first and last offset in the array, and a lookup whose table cannot be reached is left out rather than guessed at.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MorxSubtable"/>
/// <seealso cref="ContextualEntry"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
public sealed record MorxContextualSubtable : MorxSubtable
{
    /// <summary>Gets the extended state table that drives the substitutions.</summary>
    public required AatStateTable StateTable { get; init; }

    /// <summary>Gets the per-glyph lookup tables, indexed by the entry table's mark and current indices.</summary>
    /// <remarks>An index that could not be resolved to a table is absent; a caller should treat a missing index as "no substitution".</remarks>
    public IReadOnlyDictionary<int, LookupTable> SubstitutionTables { get; init; } =
        new Dictionary<int, LookupTable>();

    /// <summary>Resolves the substitute for a glyph through one of the per-glyph lookup tables.</summary>
    /// <param name="tableIndex">The mark or current index from the entry table.</param>
    /// <param name="glyphIndex">The glyph index to substitute.</param>
    /// <returns>The substitute glyph index, or <paramref name="glyphIndex"/> unchanged when there is no substitution.</returns>
    /// <remarks>An index of <c>-1</c>, an index that names no table, or a table that does not cover the glyph all leave the glyph unchanged.</remarks>
    public int GetSubstitute(int tableIndex, int glyphIndex)
    {
        if (tableIndex < 0) return glyphIndex;
        if (!SubstitutionTables.TryGetValue(tableIndex, out var table)) return glyphIndex;

        return table.TryGetValue(glyphIndex, out uint value) ? (int)value : glyphIndex;
    }

    /// <summary>Reads the payload of a contextual subtable.</summary>
    /// <param name="cursor">The cursor positioned at the payload.</param>
    /// <param name="header">The already-read common subtable header.</param>
    /// <param name="payloadStart">The absolute offset of the payload.</param>
    /// <param name="glyphCount">The font's glyph count, used to size the state table.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="EndOfStreamException">A structure extends past the end of the table-scoped source.</exception>
    internal static MorxContextualSubtable Parse(ref Cursor cursor, MorxTable.SubtableHeader header, long payloadStart, int glyphCount)
    {
        AatStateTable stateTable = AatStateTable.Read(ref cursor, payloadStart, ContextualEntry.SizeInBytes, glyphCount);

        uint lookupSpaceOffset = cursor.Source.ReadUInt32At(payloadStart + 16);
        long lookupSpaceStart = payloadStart + lookupSpaceOffset;

        var tables = new Dictionary<int, LookupTable>();
        ReadPerGlyphLookups(cursor.Source, lookupSpaceStart, header.Length, payloadStart, tables);

        return new MorxContextualSubtable
        {
            Header = new MorxSubtableHeader
            {
                Length = header.Length,
                Coverage = header.Coverage,
                SubFeatureFlags = header.SubFeatureFlags,
            },
            StateTable = stateTable,
            SubstitutionTables = tables,
        };
    }

    /// <summary>Reads the per-glyph lookup table space, whose table count is implied by its offset array.</summary>
    /// <param name="source">The table-scoped source.</param>
    /// <param name="lookupSpaceStart">The absolute offset of the offset array.</param>
    /// <param name="subtableLength">The owning subtable's declared length, used to bound the scan.</param>
    /// <param name="payloadStart">The absolute offset of the subtable payload.</param>
    /// <param name="tables">The dictionary to fill, keyed by lookup index.</param>
    /// <remarks>
    /// The manual states that no count is stored because the runtime does not need one. The array is therefore scanned from index 0 while each successive offset still points inside the subtable, and the scan stops at the first offset that does not increase on the previous one.
    /// </remarks>
    private static void ReadPerGlyphLookups(
        Source source, long lookupSpaceStart, uint subtableLength, long payloadStart, Dictionary<int, LookupTable> tables)
    {
        long subtableEnd = payloadStart + subtableLength;

        for (int index = 0; ; index++)
        {
            long entryPosition = lookupSpaceStart + (long)index * 4;
            if (entryPosition + 4 > subtableEnd) break;

            uint offset = source.ReadUInt32At(entryPosition);
            long tableStart = lookupSpaceStart + offset;

            if (tableStart + 2 > subtableEnd) break;

            var lookupCursor = source.CreateCursor(tableStart);
            tables[index] = LookupTable.Parse(ref lookupCursor, null);

            // Stop when the next offset word would be the table we just read, which means the array ended.
            long nextEntry = lookupSpaceStart + (long)(index + 1) * 4;
            if (nextEntry >= tableStart) break;
        }
    }

    /// <summary>One entry of a contextual subtable's entry table.</summary>
    /// <seealso cref="MorxContextualSubtable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct ContextualEntry : IEndianReversibleStruct<ContextualEntry>
    {
        /// <summary>The size of one entry, in bytes.</summary>
        public const int SizeInBytes = 8;

        /// <summary>The zero-based index of the state array row to use for the next glyph at byte offset 0.</summary>
        public ushort NewState;     // +0

        /// <summary>The table-specific flags at byte offset 2.</summary>
        public ushort Flags;        // +2

        /// <summary>The per-glyph lookup index for the marked glyph at byte offset 4, or <c>-1</c> for none.</summary>
        public short MarkIndex;     // +4

        /// <summary>The per-glyph lookup index for the current glyph at byte offset 6, or <c>-1</c> for none.</summary>
        public short CurrentIndex;  // +6

        /// <summary>Reverses the byte order of every field in a <see cref="ContextualEntry"/>.</summary>
        public static ContextualEntry ReverseEndianness(ContextualEntry v) => new()
        {
            NewState = BinaryPrimitives.ReverseEndianness(v.NewState),
            Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
            MarkIndex = BinaryPrimitives.ReverseEndianness(v.MarkIndex),
            CurrentIndex = BinaryPrimitives.ReverseEndianness(v.CurrentIndex),
        };
    }
}

/// <summary>A <c>morx</c> ligature subtable: a state machine that replaces a sequence of glyphs with a single ligature glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The payload is an extended state table followed by three 32-bit offsets — to the ligature action table, the component table, and the ligature list. A subtable that performs actions uses all three; one that only pushes glyphs onto the component stack uses none of them.</description></item>
/// <item><description>Processing pops the pushed glyphs in reverse order. Each <see cref="LigatureAction"/> sign-extends a 30-bit offset, adds the glyph index to get an index into the component table, accumulates those component values, and stores the resulting entry of the ligature list when the action's <c>store</c> bit is set.</description></item>
/// <item><description>Because the three tables are addressed by index rather than by count, their extents are not declared. This implementation reads the component and ligature tables up to the end of the subtable, and leaves the action table's length implied by the <see cref="LigatureEntry.LigActionIndex"/> values in use.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MorxSubtable"/>
/// <seealso cref="LigatureEntry"/>
/// <seealso cref="LigatureAction"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
public sealed record MorxLigatureSubtable : MorxSubtable
{
    /// <summary>Gets the extended state table that drives the ligature formation.</summary>
    public required AatStateTable StateTable { get; init; }

    /// <summary>Gets the ligature actions, in table order.</summary>
    /// <remarks>An entry's <see cref="LigatureEntry.LigActionIndex"/> is an index into this list; actions are consumed until one sets <see cref="LigatureAction.IsLast"/>.</remarks>
    /// <seealso cref="LigatureAction"/>
    public IReadOnlyList<LigatureAction> LigatureActions { get; init; } = [];

    /// <summary>Gets the component table: per-glyph values that are accumulated to form an index into <see cref="Ligatures"/>.</summary>
    public IReadOnlyList<ushort> Components { get; init; } = [];

    /// <summary>Gets the ligature list: the output glyph indices.</summary>
    public IReadOnlyList<ushort> Ligatures { get; init; } = [];

    /// <summary>Reads the payload of a ligature subtable.</summary>
    /// <param name="cursor">The cursor positioned at the payload.</param>
    /// <param name="header">The already-read common subtable header.</param>
    /// <param name="payloadStart">The absolute offset of the payload.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="EndOfStreamException">A structure extends past the end of the table-scoped source.</exception>
    internal static MorxLigatureSubtable Parse(ref Cursor cursor, MorxTable.SubtableHeader header, long payloadStart, int glyphCount)
    {
        AatStateTable stateTable = AatStateTable.Read(ref cursor, payloadStart, LigatureEntry.SizeInBytes, glyphCount);

        uint ligActionOffset = cursor.Source.ReadUInt32At(payloadStart + 16);
        uint componentOffset = cursor.Source.ReadUInt32At(payloadStart + 20);
        uint ligatureOffset = cursor.Source.ReadUInt32At(payloadStart + 24);

        long subtableEnd = payloadStart + header.Length;

        long actionStart = payloadStart + ligActionOffset;
        long componentStart = payloadStart + componentOffset;
        long ligatureStart = payloadStart + ligatureOffset;

        // The action table runs until the component table begins; the manual lays the three tables out in that order.
        int actionCount = actionStart < componentStart
            ? checked((int)((componentStart - actionStart) / 4))
            : 0;

        var actions = actionCount > 0
            ? cursor.Source.ReadUInt32ArrayAt(actionStart, actionCount)
            : [];

        int componentCount = componentStart < ligatureStart
            ? checked((int)((ligatureStart - componentStart) / 2))
            : 0;

        var components = componentCount > 0
            ? cursor.Source.ReadUInt16ArrayAt(componentStart, componentCount)
            : [];

        int ligatureCount = ligatureStart < subtableEnd
            ? checked((int)((subtableEnd - ligatureStart) / 2))
            : 0;

        var ligatures = ligatureCount > 0
            ? cursor.Source.ReadUInt16ArrayAt(ligatureStart, ligatureCount)
            : [];

        var semanticActions = new LigatureAction[actions.Length];
        for (int i = 0; i < actions.Length; i++)
            semanticActions[i] = LigatureAction.FromRaw(actions[i]);

        return new MorxLigatureSubtable
        {
            Header = new MorxSubtableHeader
            {
                Length = header.Length,
                Coverage = header.Coverage,
                SubFeatureFlags = header.SubFeatureFlags,
            },
            StateTable = stateTable,
            LigatureActions = semanticActions,
            Components = components,
            Ligatures = ligatures,
        };
    }

    /// <summary>One ligature action: a 32-bit word that decomposes into flags and a signed 30-bit component offset.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The offset is added to the glyph index to produce an index into the component table, so it is negative in a typical font: the component table is indexed by the glyph's position relative to the first glyph of the group.</description></item>
    /// <item><description><see cref="IsLast"/> terminates the action list for a group and implies that the accumulated index is stored.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MorxLigatureSubtable.LigatureActions"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
    public readonly record struct LigatureAction
    {
        /// <summary>The raw action word.</summary>
        /// <remarks>Kept so that a caller can round-trip or inspect bits this type does not name.</remarks>
        public uint Value { get; init; }

        /// <summary>Gets a value indicating whether this is the last action in its group, which also implies storage.</summary>
        public bool IsLast => (Value & 0x80000000u) != 0;

        /// <summary>Gets a value indicating whether the accumulated ligature is stored in place of the marked glyph.</summary>
        public bool Store => (Value & 0x40000000u) != 0;

        /// <summary>Gets the signed 30-bit offset that is added to a glyph index to index the component table.</summary>
        /// <remarks>The 30-bit field is sign-extended to 32 bits, as the manual specifies.</remarks>
        public int Offset => (int)(Value & 0x3FFFFFFFu) << 2 >> 2;

        /// <summary>Decodes a raw action word.</summary>
        /// <param name="value">The raw little-endian-decoded action word.</param>
        /// <returns>The decoded action.</returns>
        public static LigatureAction FromRaw(uint value) => new() { Value = value };
    }

    /// <summary>One entry of a ligature subtable's entry table.</summary>
    /// <seealso cref="MorxLigatureSubtable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct LigatureEntry : IEndianReversibleStruct<LigatureEntry>
    {
        /// <summary>The size of one entry, in bytes.</summary>
        public const int SizeInBytes = 6;

        /// <summary>The mask of <see cref="Flags"/> that pushes the current glyph onto the component stack.</summary>
        public const ushort SetComponent = 0x8000;

        /// <summary>The mask of <see cref="Flags"/> that leaves the glyph pointer on the current glyph for the next iteration.</summary>
        public const ushort DontAdvance = 0x4000;

        /// <summary>The mask of <see cref="Flags"/> that makes the entry run the ligature action list.</summary>
        public const ushort PerformAction = 0x2000;

        /// <summary>The zero-based index of the state array row to use for the next glyph at byte offset 0.</summary>
        public ushort NewState;        // +0

        /// <summary>The action flags at byte offset 2.</summary>
        public ushort Flags;           // +2

        /// <summary>The index of the first ligature action to process at byte offset 4.</summary>
        public ushort LigActionIndex;  // +4

        /// <summary>Reverses the byte order of every field in a <see cref="LigatureEntry"/>.</summary>
        public static LigatureEntry ReverseEndianness(LigatureEntry v) => new()
        {
            NewState = BinaryPrimitives.ReverseEndianness(v.NewState),
            Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
            LigActionIndex = BinaryPrimitives.ReverseEndianness(v.LigActionIndex),
        };
    }
}

/// <summary>A <c>morx</c> noncontextual, or swash, subtable: a lookup table that substitutes each glyph independently of its context.</summary>
/// <remarks>The payload is a single Apple Advanced Typography lookup table mapping glyph index onto substitute glyph index. There is no state machine.</remarks>
/// <seealso cref="MorxSubtable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
public sealed record MorxNoncontextualSubtable : MorxSubtable
{
    /// <summary>Gets the lookup table that maps a glyph index onto its substitute.</summary>
    /// <remarks>A glyph the lookup does not cover is left unchanged.</remarks>
    /// <seealso cref="GetSubstitute(int)"/>
    public required LookupTable SubstitutionTable { get; init; }

    /// <summary>Resolves the substitute for a glyph.</summary>
    /// <param name="glyphIndex">The glyph index to substitute.</param>
    /// <returns>The substitute glyph index, or <paramref name="glyphIndex"/> unchanged when the lookup does not cover it.</returns>
    /// <seealso cref="SubstitutionTable"/>
    public int GetSubstitute(int glyphIndex) =>
        SubstitutionTable.TryGetValue(glyphIndex, out uint value) ? (int)value : glyphIndex;

    /// <summary>Reads the payload of a noncontextual subtable.</summary>
    /// <param name="cursor">The cursor positioned at the payload, which is the lookup table.</param>
    /// <param name="header">The already-read common subtable header.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="EndOfStreamException">The lookup table extends past the end of the table-scoped source.</exception>
    /// <remarks>The payload of a noncontextual subtable is the lookup table itself, so the payload's position is the cursor's position and no offset is needed.</remarks>
    internal static MorxNoncontextualSubtable Parse(ref Cursor cursor, MorxTable.SubtableHeader header, int glyphCount)
    {
        LookupTable lookup = LookupTable.Parse(ref cursor, null);

        return new MorxNoncontextualSubtable
        {
            Header = new MorxSubtableHeader
            {
                Length = header.Length,
                Coverage = header.Coverage,
                SubFeatureFlags = header.SubFeatureFlags,
            },
            SubstitutionTable = lookup,
        };
    }
}

/// <summary>A <c>morx</c> insertion subtable: a state machine that inserts glyph sequences into the stream.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The payload is an extended state table followed by a 32-bit offset to the insertion glyph table, which is an array of 16-bit glyph indices.</description></item>
/// <item><description>An entry's <see cref="InsertionEntry.MarkInsertIndex"/> and <see cref="InsertionEntry.CurrentInsertIndex"/> are zero-based indices into that array, each naming the first glyph of an insertion list. A value of <c>-1</c> means no list is used, and the list ends at the first glyph whose high bit is set.</description></item>
/// <item><description>The manual notes that the number of insertion glyph indices is not declared; the table is read up to the end of the subtable here.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MorxSubtable"/>
/// <seealso cref="InsertionEntry"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
public sealed record MorxInsertionSubtable : MorxSubtable
{
    /// <summary>Gets the extended state table that drives the insertions.</summary>
    public required AatStateTable StateTable { get; init; }

    /// <summary>Gets the insertion glyph table.</summary>
    /// <remarks>The last entry of each insertion list has its high bit set; masking with <c>0x3FFF</c> yields the glyph index.</remarks>
    /// <seealso cref="GetInsertionList(short)"/>
    public IReadOnlyList<ushort> InsertionGlyphs { get; init; } = [];

    /// <summary>Reads the insertion list that begins at an index into <see cref="InsertionGlyphs"/>.</summary>
    /// <param name="index">The index from an entry table's mark or current insert index, or <c>-1</c> for none.</param>
    /// <returns>The glyph indices of the list, in order, or an empty list when <paramref name="index"/> is negative or out of range.</returns>
    /// <remarks>The list ends at the first entry whose high bit is set; that entry is included, with the bit masked off.</remarks>
    /// <seealso cref="InsertionGlyphs"/>
    public IReadOnlyList<ushort> GetInsertionList(short index)
    {
        if (index < 0 || index >= InsertionGlyphs.Count) return [];

        var result = new List<ushort>();
        for (int i = index; i < InsertionGlyphs.Count; i++)
        {
            ushort value = InsertionGlyphs[i];
            result.Add((ushort)(value & 0x3FFF));
            if ((value & 0x8000) != 0) break;
        }

        return result;
    }

    /// <summary>Reads the payload of an insertion subtable.</summary>
    /// <param name="cursor">The cursor positioned at the payload.</param>
    /// <param name="header">The already-read common subtable header.</param>
    /// <param name="payloadStart">The absolute offset of the payload.</param>
    /// <param name="glyphCount">The number of glyphs in the font.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="EndOfStreamException">A structure extends past the end of the table-scoped source.</exception>
    internal static MorxInsertionSubtable Parse(ref Cursor cursor, MorxTable.SubtableHeader header, long payloadStart, int glyphCount)
    {
        AatStateTable stateTable = AatStateTable.Read(ref cursor, payloadStart, InsertionEntry.SizeInBytes, glyphCount);

        uint insertionOffset = cursor.Source.ReadUInt32At(payloadStart + 16);
        long insertionStart = payloadStart + insertionOffset;
        long subtableEnd = payloadStart + header.Length;

        int count = insertionStart < subtableEnd
            ? checked((int)((subtableEnd - insertionStart) / 2))
            : 0;

        ushort[] glyphs = count > 0
            ? cursor.Source.ReadUInt16ArrayAt(insertionStart, count)
            : [];

        return new MorxInsertionSubtable
        {
            Header = new MorxSubtableHeader
            {
                Length = header.Length,
                Coverage = header.Coverage,
                SubFeatureFlags = header.SubFeatureFlags,
            },
            StateTable = stateTable,
            InsertionGlyphs = glyphs,
        };
    }

    /// <summary>One entry of an insertion subtable's entry table.</summary>
    /// <seealso cref="MorxInsertionSubtable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html">TrueType Reference Manual: The <c>morx</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct InsertionEntry : IEndianReversibleStruct<InsertionEntry>
    {
        /// <summary>The size of one entry, in bytes.</summary>
        public const int SizeInBytes = 8;

        /// <summary>The zero-based index of the state array row to use for the next glyph at byte offset 0.</summary>
        public ushort NewState;           // +0

        /// <summary>The action flags at byte offset 2.</summary>
        public ushort Flags;              // +2

        /// <summary>The index of the insertion list for the marked glyph at byte offset 4, or <c>-1</c> for none.</summary>
        public short MarkInsertIndex;     // +4

        /// <summary>The index of the insertion list for the current glyph at byte offset 6, or <c>-1</c> for none.</summary>
        public short CurrentInsertIndex;  // +6

        /// <summary>Reverses the byte order of every field in an <see cref="InsertionEntry"/>.</summary>
        public static InsertionEntry ReverseEndianness(InsertionEntry v) => new()
        {
            NewState = BinaryPrimitives.ReverseEndianness(v.NewState),
            Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
            MarkInsertIndex = BinaryPrimitives.ReverseEndianness(v.MarkInsertIndex),
            CurrentInsertIndex = BinaryPrimitives.ReverseEndianness(v.CurrentInsertIndex),
        };
    }
}

