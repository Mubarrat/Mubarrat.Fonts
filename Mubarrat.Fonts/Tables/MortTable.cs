using CommunityToolkit.HighPerformance;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Sfnt;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// The 'mort' table — classic AAT glyph metamorphosis (deprecated)
//
// Uses the shared AAT infrastructure from AatShared.cs: StateHeader, ClassTableHeader,
// LookupTable and its format-specific subclasses, AatClass.
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>mort</c> table: classic Apple Advanced Typography glyph metamorphosis.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><b>Deprecated.</b> The <c>mort</c> table is the predecessor of <c>morx</c>. It is supported on macOS but not on iOS, Apple's tooling no longer generates it, and when a font contains both <c>mort</c> and <c>morx</c> the runtime ignores the <c>mort</c> table.</description></item>
/// <item><description>Each metamorphosis chain is a set of atomic transformations selected by sub-feature flags. A chain's feature table maps requested user features onto those flags; the subtables apply their transformations when their flags intersect the computed result.</description></item>
/// <item><description>Subtables use the <em>classic</em> AAT state-table format: 16-bit byte offsets throughout, a dense class array, a byte-wide state array, and 4- to 8-byte entry records. This differs from the extended state table used by <c>kerx</c> and <c>morx</c>.</description></item>
/// <item><description>All multi-byte reads use the sfnt default of big-endian.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6mort.html"><c>mort</c> table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortChain"/>
/// <seealso cref="MortSubtable"/>
/// <!--seealso cref="MorxTable"/-->
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6mort.html">TrueType Reference Manual: The <c>mort</c> table</seealso>
public sealed record MortTable : IFontTable<MortTable>, IRecord<MortTable>
{
    /// <summary>The only version defined by the current specification, as a 16.16 <c>Fixed</c> value.</summary>
    /// <value>The bit pattern <c>0x00010000</c>, corresponding to <c>Fixed</c> 1.0.</value>
    /// <seealso cref="Version"/>
    public const uint CurrentVersion = 0x00010000;

    /// <inheritdoc/>
    public static Tag Tag => "mort";

    /// <summary>Gets the table version.</summary>
    /// <value>The 16.16 <c>Fixed</c> version read from the header. Always <see cref="CurrentVersion"/>; the parser rejects any other value.</value>
    /// <seealso cref="CurrentVersion"/>
    public uint Version { get; init; }

    /// <summary>Gets the metamorphosis chains, in table order.</summary>
    /// <value>A read-only view over the <see cref="MortChain"/> instances, one per declared chain. A font normally ships one chain; multiple chains are needed only when the font's features cannot be expressed within a single 32-bit sub-feature flag word.</value>
    /// <seealso cref="MortChain"/>
    public required IReadOnlyList<MortChain> Chains { get; init; }

    static MortTable IRecord<MortTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
            throw new InvalidDataException(
                $"'mort'.version is 0x{header.Version:X8}, expected 0x{CurrentVersion:X8}.");

        if (header.ChainCount > int.MaxValue)
            throw new InvalidDataException(
                $"'mort'.nChains is {header.ChainCount}, which exceeds the supported maximum.");

        int chainCount = (int)header.ChainCount;
        var chains = new MortChain[chainCount];
        for (int i = 0; i < chainCount; i++)
        {
            cursor.Position = (cursor.Position + 3) & ~3L;
            chains[i] = cursor.ReadRecord<MortChain>(context);
        }

        return new()
        {
            Version = header.Version,
            Chains = Array.AsReadOnly(chains),
        };
    }

    /// <summary>The 8-byte <c>mort</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both fields are <c>UInt32</c>, so the reversal is a single word-span pass over the struct reinterpreted as two 32-bit words.</description></item>
    /// <item><description>Consumed at parse time and not retained on <see cref="MortTable"/>; its field values are exposed through the semantic record's properties.</description></item>
    /// <item><description>Declared as a mutable <c>record struct</c> with <see cref="StructLayoutAttribute"/> so its fields honour the wire layout; the <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>Public because a writer fills the struct in memory, reverses it, and writes the bytes by blittable cast.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MortTable"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The 16.16 <c>Fixed</c> table version at byte offset 0.</summary>
        /// <value>The constant <see cref="CurrentVersion"/>.</value>
        /// <seealso cref="CurrentVersion"/>
        /// <seealso cref="MortTable.Version"/>
        public uint Version;     // +0

        /// <summary>The number of metamorphosis chains at byte offset 4.</summary>
        /// <value>The count of <see cref="MortChain"/> records that follow this header.</value>
        /// <seealso cref="MortTable.Chains"/>
        public uint ChainCount;  // +4

        /// <summary>Reverses the byte order of both 32-bit fields in one pass.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>The header is entirely <c>UInt32</c> fields, so it is reinterpreted as a span of two 32-bit words and reversed with a single <see cref="BinaryPrimitives.ReverseEndianness(ReadOnlySpan{uint}, Span{uint})"/> call.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static Header ReverseEndianness(Header value)
        {
            Span<uint> words = MemoryMarshal.CreateSpan(ref value.Version, 2);
            BinaryPrimitives.ReverseEndianness(words, words);
            return value;
        }
    }
}

/// <summary>A single metamorphosis chain: a feature table that selects sub-feature flags, and a set of atomic transformations that apply when their flags are set.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The chain header is followed by the feature table (an array of <see cref="MortFeatureEntry"/> records), then by the metamorphosis subtables.</description></item>
/// <item><description>Feature table entries are processed in order: the running flag word is ANDed with each matched entry's <c>disableFlags</c> and ORed with its <c>enableFlags</c>. The last entry must be the Enable Glyph Effects feature (type 0, setting 1) with enable and disable flags both zero, which clears all sub-feature flags for the chain.</description></item>
/// <item><description>Each subtable is applied only when its <see cref="MortSubtable.SubFeatureFlags"/> intersect the final flag word.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortTable"/>
/// <seealso cref="MortFeatureEntry"/>
/// <seealso cref="MortSubtable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6mort.html">TrueType Reference Manual: The <c>mort</c> table</seealso>
public sealed record MortChain : IRecord<MortChain>
{
    /// <summary>Gets the default sub-feature flags for this chain.</summary>
    /// <value>The flag word that seeds the running flags before the feature table is applied. When no features are requested, this is the flag word that determines which subtables execute.</value>
    public uint DefaultFlags { get; init; }

    /// <summary>Gets the feature table entries in table order.</summary>
    /// <value>A read-only view over the <see cref="MortFeatureEntry"/> records. Later entries take precedence over earlier ones, and the last entry must be the Enable Glyph Effects feature (type 0, setting 1) which clears all flags.</value>
    public required ReadOnlyMemory<MortFeatureEntry> FeatureEntries { get; init; }

    /// <summary>Gets the subtables in chain order.</summary>
    /// <value>A read-only view over the <see cref="MortSubtable"/> instances. Subtables are applied in order and only when their <see cref="MortSubtable.SubFeatureFlags"/> intersect the computed sub-feature flag word.</value>
    public required IReadOnlyList<MortSubtable> Subtables { get; init; }

    static MortChain IRecord<MortChain>.Parse(ref Cursor cursor, object? context)
    {
        int numGlyphs = context is SfntFontFace face
            ? face.GetTable<MaxpTable>().NumGlyphs
            : throw new InvalidDataException(
                "'mort' requires an SFNT font face context to obtain the glyph count.");

        long chainStart = cursor.Position;
        ChainHeader header = cursor.ReadEndianReversibleStruct<ChainHeader>();
        long chainEnd = chainStart + header.ChainLength;

        MortFeatureEntry[] featureEntries = cursor.ReadEndianReversibleStructArray<MortFeatureEntry>(
            header.FeatureEntryCount);

        var subtableContext = new MortSubtable.Context(numGlyphs);
        var subtables = new MortSubtable[header.SubtableCount];
        for (int i = 0; i < subtables.Length; i++)
        {
            long subtableStart = cursor.Position;
            subtables[i] = cursor.ReadRecord<MortSubtable>(subtableContext);
            cursor.Position = (subtableStart + subtables[i].Length + 3) & ~3L;
        }

        cursor.Position = chainEnd;

        return new()
        {
            DefaultFlags = header.DefaultFlags,
            FeatureEntries = featureEntries,
            Subtables = Array.AsReadOnly(subtables),
        };
    }

    /// <summary>The 12-byte metamorphosis chain header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The two flag/length fields are <c>UInt32</c> and the two counts are <c>UInt16</c>; the reversal is field-by-field because the struct is not uniform-width.</description></item>
    /// <item><description>The <c>chainLength</c> field includes the header, feature table, and subtables; the parser uses it to bound the chain and to realign to the next chain.</description></item>
    /// <item><description>Declared as a mutable <c>record struct</c> with <see cref="StructLayoutAttribute"/> so its fields honour the wire layout; the <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>Public because a writer fills the struct in memory, reverses it, and writes the bytes by blittable cast.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MortChain"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct ChainHeader : IEndianReversibleStruct<ChainHeader>
    {
        /// <summary>The chain's default sub-feature flags at byte offset 0.</summary>
        /// <value>The seed value for the running sub-feature flag word.</value>
        /// <seealso cref="MortChain.DefaultFlags"/>
        public uint DefaultFlags;         // +0

        /// <summary>The total chain length in bytes, including this header, at byte offset 4.</summary>
        /// <value>The byte length of the chain; used to bound the chain and to realign to the next chain.</value>
        /// <seealso cref="MortChain"/>
        public uint ChainLength;          // +4

        /// <summary>The number of feature table entries at byte offset 8.</summary>
        /// <value>The count of <see cref="MortFeatureEntry"/> records that immediately follow this header.</value>
        /// <seealso cref="MortChain.FeatureEntries"/>
        public ushort FeatureEntryCount;  // +8

        /// <summary>The number of subtables in this chain at byte offset 10.</summary>
        /// <value>The count of <see cref="MortSubtable"/> records that follow the feature table.</value>
        /// <seealso cref="MortChain.Subtables"/>
        public ushort SubtableCount;      // +10

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="ChainHeader"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>The first two fields are 32-bit and the remaining two are 16-bit, so the reversal is field-by-field rather than a single word-span pass.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static ChainHeader ReverseEndianness(ChainHeader value) => new()
        {
            DefaultFlags = BinaryPrimitives.ReverseEndianness(value.DefaultFlags),
            ChainLength = BinaryPrimitives.ReverseEndianness(value.ChainLength),
            FeatureEntryCount = BinaryPrimitives.ReverseEndianness(value.FeatureEntryCount),
            SubtableCount = BinaryPrimitives.ReverseEndianness(value.SubtableCount),
        };
    }
}

/// <summary>A single feature-table entry: a user feature/setting and the sub-feature flags it enables and disables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>When the user requests the feature/setting identified by <see cref="FeatureType"/> and <see cref="FeatureSetting"/>, the running sub-feature flag word is ANDed with <see cref="DisableFlags"/> and ORed with <see cref="EnableFlags"/>.</description></item>
/// <item><description>The last entry in a feature table must be the Enable Glyph Effects feature (type 0, setting 1) with both flags zero, which clears all flags for the chain.</description></item>
/// <item><description>Fixed-layout record, 12 bytes on disk. Implements <see cref="IEndianReversibleStruct{T}"/> so the whole feature table is read in one bulk call.</description></item>
/// <item><description>Reversal is field-by-field because the two identifier fields are 16-bit and the two flag fields are 32-bit.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortChain"/>
/// <seealso cref="IEndianReversibleStruct{T}"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct MortFeatureEntry : IEndianReversibleStruct<MortFeatureEntry>
{
    /// <summary>The feature type at byte offset 0.</summary>
    /// <value>The registered feature type that this entry responds to.</value>
    /// <seealso cref="FeatureSetting"/>
    public ushort FeatureType;    // +0

    /// <summary>The feature setting at byte offset 2.</summary>
    /// <value>The registered setting of <see cref="FeatureType"/> that this entry responds to.</value>
    /// <seealso cref="FeatureType"/>
    public ushort FeatureSetting; // +2

    /// <summary>The sub-feature flags enabled by this feature/setting at byte offset 4.</summary>
    /// <value>The flag word ORed into the running result when the feature/setting is requested.</value>
    /// <seealso cref="DisableFlags"/>
    public uint EnableFlags;      // +4

    /// <summary>The sub-feature flags disabled by this feature/setting at byte offset 8.</summary>
    /// <value>The flag word ANDed into the running result when the feature/setting is requested.</value>
    /// <seealso cref="EnableFlags"/>
    public uint DisableFlags;     // +8

    /// <summary>Reverses the byte order of every multi-byte field in a <see cref="MortFeatureEntry"/>.</summary>
    /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new entry with each field reversed.</returns>
    /// <remarks>The first two fields are 16-bit and the remaining two are 32-bit, so the reversal is field-by-field rather than a single word-span pass.</remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
    public static MortFeatureEntry ReverseEndianness(MortFeatureEntry value) => new()
    {
        FeatureType = BinaryPrimitives.ReverseEndianness(value.FeatureType),
        FeatureSetting = BinaryPrimitives.ReverseEndianness(value.FeatureSetting),
        EnableFlags = BinaryPrimitives.ReverseEndianness(value.EnableFlags),
        DisableFlags = BinaryPrimitives.ReverseEndianness(value.DisableFlags),
    };
}

/// <summary>The subtable type discriminant that selects one of the five metamorphosis subtable formats.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Stored in bits 0–2 of the subtable's <c>coverage</c> word.</description></item>
/// <item><description>The value <c>3</c> is reserved and must not appear.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortSubtable"/>
public enum MortSubtableType : byte
{
    /// <summary>Indic-style glyph rearrangement: reorders a contiguous glyph range according to a small verb set.</summary>
    IndicRearrangement = 0,

    /// <summary>Contextual glyph substitution: replaces the marked and current glyphs based on state.</summary>
    ContextualSubstitution = 1,

    /// <summary>Ligature substitution: replaces a sequence of components with a ligature glyph.</summary>
    LigatureSubstitution = 2,

    /// <summary>Non-contextual glyph substitution: a direct per-glyph lookup.</summary>
    NonContextualSubstitution = 4,

    /// <summary>Contextual glyph insertion: inserts extra glyphs at the marked and current positions.</summary>
    Insertion = 5,
}

/// <summary>Base for the five metamorphosis subtable formats in a <c>mort</c> table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Every subtable begins with an 8-byte common header: a <c>length</c>, a <c>coverage</c> word, and a <c>subFeatureFlags</c> word.</description></item>
/// <item><description>The low three bits of the coverage word are the format discriminant; the remaining bits describe how the subtable's transformations apply.</description></item>
/// <item><description>Dispatch happens in <see cref="IRecord{T}.Parse(ref Cursor, object?)"/> via <see cref="IBaseRecord{TBase}"/>, mirroring the pattern used by <c>Coverage</c> and the GSUB/GPOS lookup subtable formats.</description></item>
/// <item><description>The subtable length is a multiple of 4; the parser aligns the cursor to the next longword after each subtable, per the specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortIndicRearrangementSubtable"/>
/// <seealso cref="MortContextualSubstitutionSubtable"/>
/// <seealso cref="MortLigatureSubstitutionSubtable"/>
/// <seealso cref="MortNonContextualSubstitutionSubtable"/>
/// <seealso cref="MortInsertionSubtable"/>
/// <seealso cref="IBaseRecord{TBase}"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
public abstract record MortSubtable : IRecord<MortSubtable>, IBaseRecord<MortSubtable>
{
    /// <summary>Gets the length of this subtable in bytes, including its 8-byte common header.</summary>
    /// <value>The <c>length</c> field from the common header. Always a multiple of four; the parser uses it to advance the cursor past this subtable.</value>
    /// <seealso cref="SubtableHeader.Length"/>
    public required ushort Length { get; init; }

    /// <summary>Gets the raw coverage word.</summary>
    /// <value>The <c>coverage</c> field from the common header. Use the bit accessors for the meaningful flags, and <see cref="Type"/> for the discriminant.</value>
    /// <seealso cref="Vertical"/>
    /// <seealso cref="Descending"/>
    /// <seealso cref="AllDirections"/>
    /// <seealso cref="Type"/>
    public required ushort Coverage { get; init; }

    /// <summary>Gets the sub-feature flags that enable this subtable.</summary>
    /// <value>The 32-bit flag word. The subtable applies only when this word intersects the computed sub-feature flags for the enclosing chain.</value>
    /// <seealso cref="MortChain.DefaultFlags"/>
    /// <seealso cref="MortChain.FeatureEntries"/>
    public required uint SubFeatureFlags { get; init; }

    /// <summary>Gets the subtable type discriminant.</summary>
    /// <value>One of the <see cref="MortSubtableType"/> values. The parser rejects the reserved value <c>3</c> and any unassigned value.</value>
    /// <seealso cref="MortSubtableType"/>
    public abstract MortSubtableType Type { get; }

    /// <summary>Gets a value indicating whether the subtable is restricted to vertical text.</summary>
    /// <value><see langword="true"/> when bit <c>0x8000</c> of <see cref="Coverage"/> is set.</value>
    /// <remarks>When <see cref="AllDirections"/> is set, this bit is ignored by a consumer and the subtable applies to both orientations.</remarks>
    public bool Vertical => (Coverage & 0x8000) != 0;

    /// <summary>Gets a value indicating whether the subtable processes the glyph array in descending order.</summary>
    /// <value><see langword="true"/> when bit <c>0x4000</c> of <see cref="Coverage"/> is set.</value>
    public bool Descending => (Coverage & 0x4000) != 0;

    /// <summary>Gets a value indicating whether the subtable applies regardless of text orientation.</summary>
    /// <value><see langword="true"/> when bit <c>0x2000</c> of <see cref="Coverage"/> is set.</value>
    /// <remarks>When set, the <see cref="Vertical"/> bit is ignored and the subtable applies to both horizontal and vertical text. Support for this bit starts with GX 1.1; older consumers ignore it.</remarks>
    public bool AllDirections => (Coverage & 0x2000) != 0;

    static MortSubtable IRecord<MortSubtable>.Parse(ref Cursor cursor, object? context)
    {
        long start = cursor.Position;
        int type = cursor.Source.ReadUInt16At(start + 2) & 0x7;
        return type switch
        {
            0 => IBaseRecord<MortSubtable>.Parse<MortIndicRearrangementSubtable>(ref cursor, context),
            1 => IBaseRecord<MortSubtable>.Parse<MortContextualSubstitutionSubtable>(ref cursor, context),
            2 => IBaseRecord<MortSubtable>.Parse<MortLigatureSubstitutionSubtable>(ref cursor, context),
            4 => IBaseRecord<MortSubtable>.Parse<MortNonContextualSubstitutionSubtable>(ref cursor, context),
            5 => IBaseRecord<MortSubtable>.Parse<MortInsertionSubtable>(ref cursor, context),
            _ => throw new InvalidDataException(
                $"'mort' subtable type {type} is not one of 0, 1, 2, 4, or 5."),
        };
    }

    /// <summary>Context for parsing a <see cref="MortSubtable"/>: the font's glyph count.</summary>
    /// <param name="GlyphCount">Number of glyphs in the font, from <c>maxp.numGlyphs</c>.</param>
    /// <remarks>The glyph count is required by format-0 lookups inside the contextual and non-contextual subtables.</remarks>
    /// <seealso cref="LookupTable.Context"/>
    public record Context(int GlyphCount);

    /// <summary>The 8-byte common subtable header shared by every <see cref="MortSubtable"/> format.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The format discriminant is the low three bits of <see cref="Coverage"/>; the remaining coverage bits describe the subtable's applicability.</description></item>
    /// <item><description>The <c>length</c> field includes this header and is always a multiple of four; the parser uses it to bound the subtable and to realign to the next one.</description></item>
    /// <item><description>Reversal is field-by-field because the length and coverage fields are 16-bit and the sub-feature flags field is 32-bit.</description></item>
    /// <item><description>Public because a writer fills the struct in memory, reverses it, and writes the bytes by blittable cast.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MortSubtable"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct SubtableHeader : IEndianReversibleStruct<SubtableHeader>
    {
        /// <summary>The subtable length in bytes, including this header, at byte offset 0.</summary>
        /// <value>The byte length of the entire subtable; a multiple of four.</value>
        /// <seealso cref="MortSubtable.Length"/>
        public ushort Length;           // +0

        /// <summary>The coverage word at byte offset 2.</summary>
        /// <value>The subtable's coverage flags; the low three bits are the format discriminant.</value>
        /// <seealso cref="MortSubtable.Coverage"/>
        /// <seealso cref="MortSubtable.Type"/>
        public ushort Coverage;         // +2

        /// <summary>The sub-feature flags that enable this subtable at byte offset 4.</summary>
        /// <value>The flag word that must intersect the chain's computed sub-feature flags for the subtable to apply.</value>
        /// <seealso cref="MortSubtable.SubFeatureFlags"/>
        public uint SubFeatureFlags;    // +4

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="SubtableHeader"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>The first two fields are 16-bit and the third is 32-bit, so the reversal is field-by-field rather than a single word-span pass.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static SubtableHeader ReverseEndianness(SubtableHeader value) => new()
        {
            Length = BinaryPrimitives.ReverseEndianness(value.Length),
            Coverage = BinaryPrimitives.ReverseEndianness(value.Coverage),
            SubFeatureFlags = BinaryPrimitives.ReverseEndianness(value.SubFeatureFlags),
        };
    }
}

/// <summary>The shared classic AAT state-machine structure used by all state-table-based metamorphosis subtables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Unlike the extended state table used by <c>kerx</c> and <c>morx</c>, the classic form uses a dense byte class array, a byte-wide state array, and byte offsets throughout.</description></item>
/// <item><description><see cref="StateArray"/> is a two-dimensional view with <c>nStates</c> rows and <see cref="StateSize"/> columns; each element is a zero-based index into the subtable's entry table.</description></item>
/// <item><description><see cref="ClassArray"/> holds one byte per covered glyph; glyphs outside the covered range classify to <see cref="AatClass.OutOfBounds"/>.</description></item>
/// <item><description>The parser preserves both arrays as raw bytes; it does not simulate the state machine, per the parse/diagnose split.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortIndicRearrangementSubtable"/>
/// <seealso cref="MortContextualSubstitutionSubtable"/>
/// <seealso cref="MortLigatureSubstitutionSubtable"/>
/// <seealso cref="MortInsertionSubtable"/>
/// <seealso cref="StateHeader"/>
/// <seealso cref="ClassTableHeader"/>
/// <seealso cref="AatClass"/>
public sealed record MortStateMachine
{
    /// <summary>Gets the number of bytes per state row, equal to the number of classes.</summary>
    /// <value>The width of <see cref="StateArray"/>, corresponding to the <c>stateSize</c> field from the <see cref="StateHeader"/>.</value>
    public int StateSize => StateArray.Width;

    /// <summary>Gets the raw state array as a two-dimensional memory view.</summary>
    /// <value><c>StateCount × StateSize</c> bytes; element <c>[state, class]</c> is a zero-based index into the subtable's entry table.</value>
    public required ReadOnlyMemory2D<byte> StateArray { get; init; }

    /// <summary>Gets the class subtable header.</summary>
    /// <value>The <see cref="ClassTableHeader"/> (first glyph + glyph count) that prefixes the class array.</value>
    public required ClassTableHeader ClassHeader { get; init; }

    /// <summary>Gets the class array.</summary>
    /// <value>One byte per covered glyph; index <c>glyph - ClassHeader.FirstGlyph</c> is the glyph's class.</value>
    /// <remarks>Glyphs outside <c>[FirstGlyph, FirstGlyph + ClassArray.Length)</c> classify to <see cref="AatClass.OutOfBounds"/>.</remarks>
    public required ReadOnlyMemory<byte> ClassArray { get; init; }

    /// <summary>Gets the number of states in the state machine.</summary>
    /// <value>The height of <see cref="StateArray"/>.</value>
    public int StateCount => StateArray.Height;

    /// <summary>Classifies a glyph against the class array.</summary>
    /// <param name="glyphId">The glyph ID to classify.</param>
    /// <returns>The class byte in <c>[0, StateSize)</c>, or <see cref="AatClass.OutOfBounds"/> when the glyph lies outside the covered range.</returns>
    public int ClassifyGlyph(int glyphId)
    {
        int index = glyphId - ClassHeader.FirstGlyph;
        return (uint)index < (uint)ClassArray.Length ? ClassArray.Span[index] : AatClass.OutOfBounds;
    }

    /// <summary>Gets the zero-based entry-table index for a state/class pair.</summary>
    /// <param name="stateIndex">The current state index in <c>[0, StateCount)</c>.</param>
    /// <param name="glyphClass">The class byte obtained from <see cref="ClassifyGlyph(int)"/>.</param>
    /// <returns>The zero-based index into the subtable's entry array stored at <c>[stateIndex, glyphClass]</c>.</returns>
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

        return StateArray.Span[stateIndex, glyphClass];
    }

    internal static MortStateMachine Parse(ref Cursor cursor, long stateHeaderStart, StateHeader state)
    {
        cursor.Position = stateHeaderStart + state.ClassTable;
        ClassTableHeader classHeader = cursor.ReadEndianReversibleStruct<ClassTableHeader>();
        byte[] classArray = cursor.ReadUInt8Array(classHeader.GlyphCount);

        cursor.Position = stateHeaderStart + state.StateArray;
        int stateArrayLength = state.EntryTable - state.StateArray;
        byte[] stateArray = cursor.ReadUInt8Array(stateArrayLength);

        return new()
        {
            StateArray = new(stateArray, stateArrayLength / state.StateSize, state.StateSize),
            ClassHeader = classHeader,
            ClassArray = classArray,
        };
    }
}

/// <summary>An Indic rearrangement subtable: reorders a contiguous glyph range according to a small verb set, driven by a classic state machine.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The subtable consists of a <see cref="StateHeader"/>, the class and state arrays read by <see cref="MortStateMachine"/>, and a table of 4-byte <see cref="MortIndicRearrangementEntry"/> records. There are no per-glyph tables.</description></item>
/// <item><description>The entry's <see cref="MortIndicRearrangementEntry.MarkFirst"/> and <see cref="MortIndicRearrangementEntry.MarkLast"/> flags remember the two boundaries of the range to rearrange; the <see cref="MortIndicRearrangementEntry.Verb"/> flag selects one of sixteen fixed permutations.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6mort.html">Indic rearrangement subtable</see> section of the <c>mort</c> table description.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortSubtable"/>
/// <seealso cref="MortIndicRearrangementEntry"/>
/// <seealso cref="IndicRearrangementVerb"/>
/// <seealso cref="MortStateMachine"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
public sealed record MortIndicRearrangementSubtable : MortSubtable, IDerivedRecord<MortSubtable, MortIndicRearrangementSubtable>
{
    /// <inheritdoc/>
    public override MortSubtableType Type => MortSubtableType.IndicRearrangement;

    /// <summary>Gets the classic state machine that drives the rearrangement.</summary>
    /// <value>The class array, state array, and class-table header parsed from the subtable.</value>
    public required MortStateMachine StateMachine { get; init; }

    /// <summary>Gets the entry array that state transitions select into.</summary>
    /// <value>One <see cref="MortIndicRearrangementEntry"/> per entry-table slot. The array length is derived from the subtable's length minus the entry-table offset, divided by four.</value>
    public required ReadOnlyMemory<MortIndicRearrangementEntry> Entries { get; init; }

    /// <summary>Advances the state machine one step.</summary>
    /// <param name="stateIndex">The current state index in <c>[0, StateMachine.StateCount)</c>.</param>
    /// <param name="glyphClass">The class for the current glyph, obtained from <see cref="MortStateMachine.ClassifyGlyph(int)"/>.</param>
    /// <returns>The entry for the transition.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="stateIndex"/> or <paramref name="glyphClass"/> is out of range.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The returned entry's <see cref="MortIndicRearrangementEntry.NewState"/> is a byte offset from the start of the state header to the next state's row; a consumer converts it to a state index by subtracting <c>state.StateArray</c> and dividing by <c>state.StateSize</c>.</description></item>
    /// <item><description>A consumer that honours <see cref="MortIndicRearrangementEntry.DontAdvance"/> stays on the current glyph before consuming the next one.</description></item>
    /// </list>
    /// </remarks>
    public MortIndicRearrangementEntry Step(int stateIndex, int glyphClass)
    {
        int entryIndex = StateMachine.GetEntryIndex(stateIndex, glyphClass);
        return Entries.Span[entryIndex];
    }

    static MortIndicRearrangementSubtable IDerivedRecord<MortSubtable, MortIndicRearrangementSubtable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not MortSubtable.Context)
            throw new InvalidDataException(
                "'mort' Indic rearrangement subtable requires a glyph-count context.");

        long subtableStart = cursor.Position;
        SubtableHeader header = cursor.ReadEndianReversibleStruct<SubtableHeader>();

        long stateHeaderStart = cursor.Position;
        StateHeader state = cursor.ReadEndianReversibleStruct<StateHeader>();

        MortStateMachine stateMachine = MortStateMachine.Parse(ref cursor, stateHeaderStart, state);

        long subtableEnd = subtableStart + header.Length;
        int entryCount = checked((int)((subtableEnd - cursor.Position) / 4));
        MortIndicRearrangementEntry[] entries =
            cursor.ReadEndianReversibleStructArray<MortIndicRearrangementEntry>(entryCount);

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

/// <summary>A single entry in an Indic rearrangement subtable: a next-state reference and the rearrangement flags.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Fixed-layout record, 4 bytes on disk. Implements <see cref="IEndianReversibleStruct{T}"/> so the entry array is read in a single bulk call.</description></item>
/// <item><description>The <see cref="NewState"/> field is a byte offset from the start of the state header, not a state index; the parser preserves the raw offset and leaves index conversion to the consumer.</description></item>
/// <item><description>Both fields are <c>UInt16</c>, so the reversal is a single word-span pass over the struct reinterpreted as two 16-bit words.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortIndicRearrangementSubtable"/>
/// <seealso cref="IndicRearrangementVerb"/>
/// <seealso cref="IEndianReversibleStruct{T}"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct MortIndicRearrangementEntry : IEndianReversibleStruct<MortIndicRearrangementEntry>
{
    /// <summary>The next state's byte offset from the start of the state header at byte offset 0.</summary>
    /// <value>The byte position of the next state's row within the state array; the consumer converts it to a state index by subtracting the state array's offset and dividing by <c>stateSize</c>.</value>
    public ushort NewState;  // +0

    /// <summary>The rearrangement flags word at byte offset 2.</summary>
    /// <value>The on-disk <c>flags</c> field. Use the bit accessors for the meaningful bits.</value>
    public ushort Flags;     // +2

    /// <summary>Gets a value indicating whether the current glyph becomes the marked-first glyph.</summary>
    /// <value><see langword="true"/> when bit <c>0x8000</c> of <see cref="Flags"/> is set.</value>
    public readonly bool MarkFirst => (Flags & 0x8000) != 0;

    /// <summary>Gets a value indicating whether the state machine should stay on the current glyph.</summary>
    /// <value><see langword="true"/> when bit <c>0x4000</c> of <see cref="Flags"/> is set.</value>
    public readonly bool DontAdvance => (Flags & 0x4000) != 0;

    /// <summary>Gets a value indicating whether the current glyph becomes the marked-last glyph.</summary>
    /// <value><see langword="true"/> when bit <c>0x2000</c> of <see cref="Flags"/> is set.</value>
    public readonly bool MarkLast => (Flags & 0x2000) != 0;

    /// <summary>Gets the rearrangement verb.</summary>
    /// <value>The low four bits of <see cref="Flags"/>, cast to <see cref="IndicRearrangementVerb"/>.</value>
    public readonly IndicRearrangementVerb Verb => (IndicRearrangementVerb)(Flags & 0x000F);

    /// <summary>Reverses the byte order of both 16-bit fields in one pass.</summary>
    /// <param name="value">The value whose fields are to be reversed.</param>
    /// <returns>A new entry with both fields reversed.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
    public static MortIndicRearrangementEntry ReverseEndianness(MortIndicRearrangementEntry value)
    {
        Span<ushort> words = MemoryMarshal.CreateSpan(ref value.NewState, 2);
        BinaryPrimitives.ReverseEndianness(words, words);
        return value;
    }
}

/// <summary>The sixteen Indic rearrangement verbs: fixed permutations of a contiguous glyph range bounded by the marked-first and marked-last positions.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Letters <c>A</c>, <c>B</c>, <c>C</c>, <c>D</c> denote the first, second, second-to-last, and last glyphs of the marked range; <c>x</c> denotes the intervening glyphs.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6mort.html">rearrangement verb atomic transformations</see> table in the <c>mort</c> table description.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortIndicRearrangementEntry.Verb"/>
public enum IndicRearrangementVerb : byte
{
    /// <summary>No rearrangement; the marked range is used unmodified.</summary>
    None = 0,

    /// <summary>Move the first glyph to the end of the marked range: <c>Ax → xA</c>.</summary>
    MoveFirstToEnd = 1,

    /// <summary>Move the last glyph to the start of the marked range: <c>xD → Dx</c>.</summary>
    MoveLastToStart = 2,

    /// <summary>Swap the first and last glyphs of the marked range: <c>AxD → DxA</c>.</summary>
    SwapFirstAndLast = 3,

    /// <summary>Move the first two glyphs to the end of the marked range, preserving their order: <c>ABx → xAB</c>.</summary>
    MoveFirstTwoToEnd = 4,

    /// <summary>Move the first two glyphs to the end of the marked range, swapping them: <c>ABx → xBA</c>.</summary>
    MoveFirstTwoToEndSwapped = 5,

    /// <summary>Move the last two glyphs to the start of the marked range, preserving their order: <c>xCD → CDx</c>.</summary>
    MoveLastTwoToStart = 6,

    /// <summary>Move the last two glyphs to the start of the marked range, swapping them: <c>xCD → DCx</c>.</summary>
    MoveLastTwoToStartSwapped = 7,

    /// <summary>Move the first glyph past the last two glyphs of the marked range: <c>AxCD → CDxA</c>.</summary>
    MoveFirstPastLastTwo = 8,

    /// <summary>Move the first glyph past the last two glyphs of the marked range, swapping the last two: <c>AxCD → DCxA</c>.</summary>
    MoveFirstPastLastTwoSwapped = 9,

    /// <summary>Move the first two glyphs past the last glyph of the marked range: <c>ABxD → DxAB</c>.</summary>
    MoveFirstTwoPastLast = 10,

    /// <summary>Move the first two glyphs past the last glyph of the marked range, swapping the first two: <c>ABxD → DxBA</c>.</summary>
    MoveFirstTwoPastLastSwapped = 11,

    /// <summary>Swap the first two and last two glyphs of the marked range, preserving both orders: <c>ABxCD → CDxAB</c>.</summary>
    SwapFirstTwoLastTwo = 12,

    /// <summary>Swap the first two and last two glyphs, swapping the first two: <c>ABxCD → CDxBA</c>.</summary>
    SwapFirstTwoLastTwoSwappedFirst = 13,

    /// <summary>Swap the first two and last two glyphs, swapping the last two: <c>ABxCD → DCxAB</c>.</summary>
    SwapFirstTwoLastTwoSwappedLast = 14,

    /// <summary>Swap the first two and last two glyphs, swapping both pairs: <c>ABxCD → DCxBA</c>.</summary>
    SwapFirstTwoLastTwoSwappedBoth = 15,
}

/// <summary>A contextual glyph substitution subtable: replaces the marked and current glyphs with per-glyph substitutions selected by a classic state machine.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The subtable consists of a <see cref="StateHeader"/>, the class and state arrays read by <see cref="MortStateMachine"/>, a table of 8-byte <see cref="MortContextualEntry"/> records, and one or more per-glyph <see cref="LookupTable"/> substitution tables that the entries reference.</description></item>
/// <item><description>Each state entry carries two word offsets, <see cref="MortContextualEntry.MarkOffset"/> and <see cref="MortContextualEntry.CurrentOffset"/>, which point at per-glyph lookup tables. A zero offset means "no substitution for that position"; a non-zero offset is a word offset (multiply by two for bytes) from the start of the state header.</description></item>
/// <item><description>The header's <see cref="SubstitutionTableOffset"/> field is informational — it names the beginning of the substitution table region — but each entry's own offsets are authoritative.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortSubtable"/>
/// <seealso cref="MortContextualEntry"/>
/// <seealso cref="MortContextualSubstitutionTable"/>
/// <seealso cref="MortStateMachine"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
public sealed record MortContextualSubstitutionSubtable : MortSubtable, IDerivedRecord<MortSubtable, MortContextualSubstitutionSubtable>
{
    /// <inheritdoc/>
    public override MortSubtableType Type => MortSubtableType.ContextualSubstitution;

    /// <summary>Gets the informational byte offset from the state subtable start to the beginning of the substitution table region.</summary>
    /// <value>The <c>substitutionTable</c> field from the subtable body. The parser preserves the value; per-entry offsets remain authoritative.</value>
    public ushort SubstitutionTableOffset { get; init; }

    /// <summary>Gets the classic state machine that drives the substitution.</summary>
    /// <value>The class array, state array, and class-table header parsed from the subtable.</value>
    public required MortStateMachine StateMachine { get; init; }

    /// <summary>Gets the entry array that state transitions select into.</summary>
    /// <value>One <see cref="MortContextualEntry"/> per entry-table slot. The array length is derived from the subtable's length minus the entry-table offset, divided by eight.</value>
    public required ReadOnlyMemory<MortContextualEntry> Entries { get; init; }

    /// <summary>Gets the substitution tables referenced by entries, keyed by their word offset from the state-header start.</summary>
    /// <value>One <see cref="MortContextualSubstitutionTable"/> per distinct non-zero offset referenced by any entry's mark or current offset field. Multiple entries may share one substitution table.</value>
    /// <remarks>The dictionary key is the raw <c>MarkOffset</c> or <c>CurrentOffset</c> value (in words), not its byte offset, so a consumer can look up directly with <c>entry.MarkOffset</c>.</remarks>
    public required IReadOnlyDictionary<ushort, MortContextualSubstitutionTable> SubstitutionTables { get; init; }

    /// <summary>Advances the state machine one step.</summary>
    /// <param name="stateIndex">The current state index in <c>[0, StateMachine.StateCount)</c>.</param>
    /// <param name="glyphClass">The class for the current glyph, obtained from <see cref="MortStateMachine.ClassifyGlyph(int)"/>.</param>
    /// <returns>The entry for the transition.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="stateIndex"/> or <paramref name="glyphClass"/> is out of range.</exception>
    public MortContextualEntry Step(int stateIndex, int glyphClass)
    {
        int entryIndex = StateMachine.GetEntryIndex(stateIndex, glyphClass);
        return Entries.Span[entryIndex];
    }

    /// <summary>Gets the substitution table that applies to the marked glyph for an entry.</summary>
    /// <param name="entry">The state entry the machine just stepped into.</param>
    /// <returns>The substitution table, or <see langword="null"/> when the entry's mark offset is zero or points to no table.</returns>
    public MortContextualSubstitutionTable? GetMarkSubstitutionTable(MortContextualEntry entry)
    {
        if (entry.MarkOffset == 0)
            return null;

        return SubstitutionTables.TryGetValue(entry.MarkOffset, out var table) ? table : null;
    }

    /// <summary>Gets the substitution table that applies to the current glyph for an entry.</summary>
    /// <param name="entry">The state entry the machine just stepped into.</param>
    /// <returns>The substitution table, or <see langword="null"/> when the entry's current offset is zero or points to no table.</returns>
    public MortContextualSubstitutionTable? GetCurrentSubstitutionTable(MortContextualEntry entry)
    {
        if (entry.CurrentOffset == 0)
            return null;

        return SubstitutionTables.TryGetValue(entry.CurrentOffset, out var table) ? table : null;
    }

    static MortContextualSubstitutionSubtable IDerivedRecord<MortSubtable, MortContextualSubstitutionSubtable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not MortSubtable.Context ctx)
            throw new InvalidDataException(
                "'mort' contextual substitution subtable requires a glyph-count context.");

        long subtableStart = cursor.Position;
        SubtableHeader header = cursor.ReadEndianReversibleStruct<SubtableHeader>();
        ushort substitutionTableOffset = cursor.ReadUInt16();

        long stateHeaderStart = cursor.Position;
        StateHeader state = cursor.ReadEndianReversibleStruct<StateHeader>();

        MortStateMachine stateMachine = MortStateMachine.Parse(ref cursor, stateHeaderStart, state);

        long subtableEnd = subtableStart + header.Length;
        int entryCount = checked((int)((subtableEnd - cursor.Position) / 8));
        MortContextualEntry[] entries = cursor.ReadEndianReversibleStructArray<MortContextualEntry>(entryCount);

        var distinctOffsets = new HashSet<ushort>();
        foreach (MortContextualEntry entry in entries)
        {
            if (entry.MarkOffset != 0) distinctOffsets.Add(entry.MarkOffset);
            if (entry.CurrentOffset != 0) distinctOffsets.Add(entry.CurrentOffset);
        }

        var substitutionTables = new Dictionary<ushort, MortContextualSubstitutionTable>();
        var lookupContext = new LookupTable.Context(ctx.GlyphCount);
        foreach (ushort wordOffset in distinctOffsets)
        {
            cursor.Position = stateHeaderStart + (long)wordOffset * 2;
            LookupTable lookup = cursor.ReadRecord<LookupTable>(lookupContext);
            substitutionTables[wordOffset] = new() { Lookup = lookup };
        }

        cursor.Position = subtableEnd;

        return new()
        {
            Length = header.Length,
            Coverage = header.Coverage,
            SubFeatureFlags = header.SubFeatureFlags,
            SubstitutionTableOffset = substitutionTableOffset,
            StateMachine = stateMachine,
            Entries = entries,
            SubstitutionTables = substitutionTables,
        };
    }
}

/// <summary>A single entry in a contextual substitution subtable: a next-state reference, control flags, and two word offsets to per-glyph substitution tables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Fixed-layout record, 8 bytes on disk. Implements <see cref="IEndianReversibleStruct{T}"/> so the entry array is read in a single bulk call.</description></item>
/// <item><description>The <see cref="NewState"/> field is a byte offset from the start of the state header, not a state index; the parser preserves the raw offset and leaves index conversion to the consumer.</description></item>
/// <item><description>All four fields are <c>UInt16</c>, so the reversal is a single word-span pass over the struct reinterpreted as four 16-bit words.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortContextualSubstitutionSubtable"/>
/// <seealso cref="MortContextualSubstitutionTable"/>
/// <seealso cref="IEndianReversibleStruct{T}"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct MortContextualEntry : IEndianReversibleStruct<MortContextualEntry>
{
    /// <summary>The next state's byte offset from the start of the state header at byte offset 0.</summary>
    /// <value>The byte position of the next state's row within the state array; the consumer converts it to a state index by subtracting the state array's offset and dividing by <c>stateSize</c>.</value>
    public ushort NewState;       // +0

    /// <summary>The contextual substitution control flags at byte offset 2.</summary>
    /// <value>The on-disk <c>flags</c> field. Use the bit accessors for the meaningful bits.</value>
    public ushort Flags;          // +2

    /// <summary>The word offset from the state-header start to the marked glyph's substitution table at byte offset 4.</summary>
    /// <value>A word offset (multiply by two for bytes). Zero means no substitution for the marked glyph.</value>
    public ushort MarkOffset;     // +4

    /// <summary>The word offset from the state-header start to the current glyph's substitution table at byte offset 6.</summary>
    /// <value>A word offset (multiply by two for bytes). Zero means no substitution for the current glyph.</value>
    public ushort CurrentOffset;  // +6

    /// <summary>Gets a value indicating whether the current glyph becomes the marked glyph after substitutions are made.</summary>
    /// <value><see langword="true"/> when bit <c>0x8000</c> of <see cref="Flags"/> is set.</value>
    public readonly bool SetMark => (Flags & 0x8000) != 0;

    /// <summary>Gets a value indicating whether the state machine should stay on the current glyph.</summary>
    /// <value><see langword="true"/> when bit <c>0x4000</c> of <see cref="Flags"/> is set.</value>
    public readonly bool DontAdvance => (Flags & 0x4000) != 0;

    /// <summary>Reverses the byte order of all four 16-bit fields in one pass.</summary>
    /// <param name="value">The value whose fields are to be reversed.</param>
    /// <returns>A new entry with all four fields reversed.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
    public static MortContextualEntry ReverseEndianness(MortContextualEntry value)
    {
        Span<ushort> words = MemoryMarshal.CreateSpan(ref value.NewState, 4);
        BinaryPrimitives.ReverseEndianness(words, words);
        return value;
    }
}

/// <summary>A per-glyph substitution table referenced by a <see cref="MortContextualEntry"/>: an AAT lookup table that maps an original glyph ID to its replacement.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Wraps an AAT <see cref="LookupTable"/> and adds the contextual-substitution semantics: a glyph is replaced by whatever the lookup returns, or kept unchanged if the lookup does not cover it.</description></item>
/// <item><description>The same substitution table may be referenced by multiple entries, and by both the marked and current offset fields of a single entry.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortContextualSubstitutionSubtable"/>
/// <seealso cref="MortContextualEntry"/>
/// <seealso cref="LookupTable"/>
public sealed record MortContextualSubstitutionTable
{
    /// <summary>Gets the underlying AAT lookup table.</summary>
    /// <value>The lookup table that maps original glyph IDs to replacement glyph IDs.</value>
    public required LookupTable Lookup { get; init; }

    /// <summary>Substitutes a glyph, returning the original glyph ID when the lookup does not cover it.</summary>
    /// <param name="glyphId">The original glyph ID.</param>
    /// <returns>The replacement glyph ID when the lookup covers the input; otherwise <paramref name="glyphId"/> unchanged.</returns>
    public int Substitute(int glyphId) =>
        Lookup.TryGetValue(glyphId, out uint newGlyph) ? (int)newGlyph : glyphId;

    /// <summary>Attempts to substitute a glyph.</summary>
    /// <param name="glyphId">The original glyph ID.</param>
    /// <param name="newGlyphId">When this method returns <see langword="true"/>, contains the replacement glyph ID; otherwise, contains <paramref name="glyphId"/> unchanged.</param>
    /// <returns><see langword="true"/> when the lookup covers <paramref name="glyphId"/>; otherwise, <see langword="false"/>.</returns>
    public bool TrySubstitute(int glyphId, out int newGlyphId)
    {
        if (Lookup.TryGetValue(glyphId, out uint newGlyph))
        {
            newGlyphId = (int)newGlyph;
            return true;
        }

        newGlyphId = glyphId;
        return false;
    }
}

/// <summary>A ligature substitution subtable: replaces a sequence of component glyphs with a ligature glyph, driven by a classic state machine.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The subtable consists of an extended <see cref="StateHeader"/> (the standard state header followed by three body offsets), the class and state arrays, a table of 4-byte <see cref="MortLigatureEntry"/> records, a set of ligature action lists (<see cref="MortLigatureAction"/>), a set of per-component <see cref="LookupTable"/> tables, and a flat ligature glyph table.</description></item>
/// <item><description>The state entry's <see cref="MortLigatureEntry.SetComponent"/> flag pushes the current glyph onto the component stack. When the stack reaches a terminating state, the entry's <see cref="MortLigatureEntry.ActionTableOffset"/> points at an action list that consumes the stack.</description></item>
/// <item><description>Each action's <see cref="MortLigatureAction.ComponentTableOffset"/> is a word offset to a per-component lookup table. As each action is processed, its component glyph is popped from the stack and looked up in that table; the accumulated offsets index the ligature glyph table.</description></item>
/// <item><description>The spec calls out that the component stack is sixteen deep, so a ligature can have at most sixteen components.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortSubtable"/>
/// <seealso cref="MortLigatureEntry"/>
/// <seealso cref="MortLigatureAction"/>
/// <seealso cref="MortStateMachine"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
public sealed record MortLigatureSubstitutionSubtable : MortSubtable, IDerivedRecord<MortSubtable, MortLigatureSubstitutionSubtable>
{
    /// <inheritdoc/>
    public override MortSubtableType Type => MortSubtableType.LigatureSubstitution;

    /// <summary>Gets the byte offset from the state-header start to the ligature action lists.</summary>
    /// <value>The <c>ligActionTable</c> field from the extended state header.</value>
    public ushort LigActionTableOffset { get; init; }

    /// <summary>Gets the byte offset from the state-header start to the component offset tables.</summary>
    /// <value>The <c>componentTable</c> field from the extended state header.</value>
    public ushort ComponentTableOffset { get; init; }

    /// <summary>Gets the byte offset from the state-header start to the ligature glyph table.</summary>
    /// <value>The <c>ligatureTable</c> field from the extended state header. The ligature glyph table extends from this offset to the end of the subtable.</value>
    public ushort LigatureTableOffset { get; init; }

    /// <summary>Gets the classic state machine that drives the ligature formation.</summary>
    /// <value>The class array, state array, and class-table header parsed from the subtable.</value>
    public required MortStateMachine StateMachine { get; init; }

    /// <summary>Gets the entry array that state transitions select into.</summary>
    /// <value>One <see cref="MortLigatureEntry"/> per entry-table slot. The array length derives from the distance between the state header's entry-table offset and the first of the action/component/ligature sections that follows it.</value>
    public required ReadOnlyMemory<MortLigatureEntry> Entries { get; init; }

    /// <summary>Gets the ligature action lists referenced by entries, keyed by their byte offset from the state-header start.</summary>
    /// <value>One memory region of <see cref="MortLigatureAction"/> values per distinct non-zero referenced offset. Each region is terminated by an action whose <see cref="MortLigatureAction.Last"/> flag is set; the terminator is included in the region.</value>
    public required IReadOnlyDictionary<ushort, ReadOnlyMemory<MortLigatureAction>> ActionsByOffset { get; init; }

    /// <summary>Gets the per-component offset tables referenced by actions, keyed by their word offset from the state-header start.</summary>
    /// <value>One <see cref="LookupTable"/> per distinct non-zero <see cref="MortLigatureAction.ComponentTableOffset"/>.</value>
    public required IReadOnlyDictionary<uint, LookupTable> ComponentTablesByOffset { get; init; }

    /// <summary>Gets the flat ligature glyph index table.</summary>
    /// <value>The <c>UInt16</c> region that begins at <see cref="LigatureTableOffset"/> and extends to the end of the subtable. Each element is a glyph index; the sum of the action-list component offsets selects one.</value>
    public required ReadOnlyMemory<ushort> LigatureGlyphs { get; init; }

    /// <summary>Advances the state machine one step.</summary>
    /// <param name="stateIndex">The current state index in <c>[0, StateMachine.StateCount)</c>.</param>
    /// <param name="glyphClass">The class for the current glyph, obtained from <see cref="MortStateMachine.ClassifyGlyph(int)"/>.</param>
    /// <returns>The entry for the transition.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="stateIndex"/> or <paramref name="glyphClass"/> is out of range.</exception>
    public MortLigatureEntry Step(int stateIndex, int glyphClass)
    {
        int entryIndex = StateMachine.GetEntryIndex(stateIndex, glyphClass);
        return Entries.Span[entryIndex];
    }

    /// <summary>Gets the action list that applies for a state entry.</summary>
    /// <param name="entry">The state entry the machine just stepped into.</param>
    /// <returns>The action list, or an empty region when the entry's action offset is zero or points to no list.</returns>
    public ReadOnlyMemory<MortLigatureAction> GetActionList(MortLigatureEntry entry) =>
        entry.ActionTableOffset == 0 || !ActionsByOffset.TryGetValue(entry.ActionTableOffset, out var actions)
            ? ReadOnlyMemory<MortLigatureAction>.Empty
            : actions;

    static MortLigatureSubstitutionSubtable IDerivedRecord<MortSubtable, MortLigatureSubstitutionSubtable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not MortSubtable.Context ctx)
            throw new InvalidDataException(
                "'mort' ligature substitution subtable requires a glyph-count context.");

        long subtableStart = cursor.Position;
        SubtableHeader header = cursor.ReadEndianReversibleStruct<SubtableHeader>();

        long stateHeaderStart = cursor.Position;
        StateHeader state = cursor.ReadEndianReversibleStruct<StateHeader>();
        ushort ligActionTable = cursor.ReadUInt16();
        ushort componentTable = cursor.ReadUInt16();
        ushort ligatureTable = cursor.ReadUInt16();

        MortStateMachine stateMachine = MortStateMachine.Parse(ref cursor, stateHeaderStart, state);

        long subtableEnd = subtableStart + header.Length;

        long entryStart = stateHeaderStart + state.EntryTable;
        long ligActionStart = stateHeaderStart + ligActionTable;
        long componentStart = stateHeaderStart + componentTable;
        long ligatureStart = stateHeaderStart + ligatureTable;

        long entryEnd = subtableEnd;
        if (ligActionStart > entryStart && ligActionStart < entryEnd) entryEnd = ligActionStart;
        if (componentStart > entryStart && componentStart < entryEnd) entryEnd = componentStart;
        if (ligatureStart > entryStart && ligatureStart < entryEnd) entryEnd = ligatureStart;

        int entryCount = checked((int)((entryEnd - entryStart) / 4));
        cursor.Position = entryStart;
        MortLigatureEntry[] entries = cursor.ReadEndianReversibleStructArray<MortLigatureEntry>(entryCount);

        var actionListOffsets = new HashSet<ushort>();
        foreach (MortLigatureEntry entry in entries)
        {
            if (entry.ActionTableOffset != 0)
                actionListOffsets.Add(entry.ActionTableOffset);
        }

        var actionsByOffset = new Dictionary<ushort, ReadOnlyMemory<MortLigatureAction>>();
        var componentTableOffsets = new HashSet<uint>();
        foreach (ushort actionListOffset in actionListOffsets)
        {
            cursor.Position = stateHeaderStart + actionListOffset;
            var actions = new List<MortLigatureAction>();
            while (true)
            {
                MortLigatureAction action = cursor.ReadEndianReversibleStruct<MortLigatureAction>();
                actions.Add(action);
                if (action.ComponentTableOffset != 0)
                    componentTableOffsets.Add(action.ComponentTableOffset);
                if (action.Last)
                    break;
            }
            actionsByOffset[actionListOffset] = actions.ToArray();
        }

        var componentTablesByOffset = new Dictionary<uint, LookupTable>();
        var lookupContext = new LookupTable.Context(ctx.GlyphCount);
        foreach (uint tableOffset in componentTableOffsets)
        {
            cursor.Position = stateHeaderStart + (long)tableOffset * 2;
            componentTablesByOffset[tableOffset] = cursor.ReadRecord<LookupTable>(lookupContext);
        }

        cursor.Position = ligatureStart;
        int ligatureCount = checked((int)((subtableEnd - ligatureStart) / 2));
        ushort[] ligatureGlyphs = cursor.ReadUInt16Array(ligatureCount);

        cursor.Position = subtableEnd;

        return new()
        {
            Length = header.Length,
            Coverage = header.Coverage,
            SubFeatureFlags = header.SubFeatureFlags,
            LigActionTableOffset = ligActionTable,
            ComponentTableOffset = componentTable,
            LigatureTableOffset = ligatureTable,
            StateMachine = stateMachine,
            Entries = entries,
            ActionsByOffset = actionsByOffset,
            ComponentTablesByOffset = componentTablesByOffset,
            LigatureGlyphs = ligatureGlyphs,
        };
    }
}

/// <summary>A single entry in a ligature substitution subtable: a next-state reference, the ligature-formation flags, and a byte offset to an action list.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Fixed-layout record, 4 bytes on disk. Implements <see cref="IEndianReversibleStruct{T}"/> so the entry array is read in a single bulk call.</description></item>
/// <item><description>The <see cref="NewState"/> field is a byte offset from the start of the state header, not a state index; the parser preserves the raw offset and leaves index conversion to the consumer.</description></item>
/// <item><description>The <see cref="ActionTableOffset"/> is a byte offset from the state-header start; it points to a ligature action list. Zero means the entry does not terminate a ligature.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortLigatureSubstitutionSubtable"/>
/// <seealso cref="MortLigatureAction"/>
/// <seealso cref="IEndianReversibleStruct{T}"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct MortLigatureEntry : IEndianReversibleStruct<MortLigatureEntry>
{
    /// <summary>The next state's byte offset from the start of the state header at byte offset 0.</summary>
    public ushort NewState;           // +0

    /// <summary>The ligature-formation flags word at byte offset 2.</summary>
    public ushort Flags;              // +2

    /// <summary>Gets a value indicating whether the current glyph becomes a ligature component.</summary>
    /// <value><see langword="true"/> when bit <c>0x8000</c> of <see cref="Flags"/> is set.</value>
    public readonly bool SetComponent => (Flags & 0x8000) != 0;

    /// <summary>Gets a value indicating whether the state machine should stay on the current glyph.</summary>
    /// <value><see langword="true"/> when bit <c>0x4000</c> of <see cref="Flags"/> is set.</value>
    public readonly bool DontAdvance => (Flags & 0x4000) != 0;

    /// <summary>Gets the byte offset from the state-header start to the ligature action list.</summary>
    /// <value>The low fourteen bits of <see cref="Flags"/>. Zero means the entry does not terminate a ligature.</value>
    public readonly ushort ActionTableOffset => (ushort)(Flags & 0x3FFF);

    /// <summary>Reverses the byte order of both 16-bit fields in one pass.</summary>
    /// <param name="value">The value whose fields are to be reversed.</param>
    /// <returns>A new entry with both fields reversed.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
    public static MortLigatureEntry ReverseEndianness(MortLigatureEntry value)
    {
        Span<ushort> words = MemoryMarshal.CreateSpan(ref value.NewState, 2);
        BinaryPrimitives.ReverseEndianness(words, words);
        return value;
    }
}

/// <summary>A single ligature action: a flag word and a word offset to a per-component offset table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Fixed-layout record, 4 bytes on disk. Implements <see cref="IEndianReversibleStruct{T}"/> so an action list is read as a sequence of bulk struct reads.</description></item>
/// <item><description>Each action corresponds to one component of the ligature; the action's <see cref="ComponentTableOffset"/> selects the per-component lookup table.</description></item>
/// <item><description><see cref="Last"/> implies <see cref="Store"/>; a list can have intermediate stores to produce multiple ligatures from one action sequence.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortLigatureSubstitutionSubtable"/>
/// <seealso cref="MortLigatureEntry"/>
/// <seealso cref="IEndianReversibleStruct{T}"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct MortLigatureAction : IEndianReversibleStruct<MortLigatureAction>
{
    /// <summary>The action flags and offset at byte offset 0.</summary>
    public uint RawAction;  // +0

    /// <summary>Gets a value indicating whether this is the last action in the list.</summary>
    /// <value><see langword="true"/> when bit <c>0x80000000</c> of <see cref="RawAction"/> is set.</value>
    public readonly bool Last => (RawAction & 0x80000000u) != 0;

    /// <summary>Gets a value indicating whether the accumulated offset should be used to fetch a ligature glyph.</summary>
    /// <value><see langword="true"/> when bit <c>0x40000000</c> of <see cref="RawAction"/> is set.</value>
    public readonly bool Store => (RawAction & 0x40000000u) != 0;

    /// <summary>Gets the word offset from the state-header start to the per-component offset table.</summary>
    /// <value>The low thirty bits of <see cref="RawAction"/>. Zero means the component contributes no offset.</value>
    public readonly uint ComponentTableOffset => RawAction & 0x3FFFFFFFu;

    /// <summary>Reverses the byte order of the single 32-bit field.</summary>
    /// <param name="value">The value whose field is to be reversed.</param>
    /// <returns>A new action with the field reversed.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
    public static MortLigatureAction ReverseEndianness(MortLigatureAction value) => new()
    {
        RawAction = BinaryPrimitives.ReverseEndianness(value.RawAction),
    };
}

/// <summary>A non-contextual glyph substitution subtable: a direct per-glyph lookup that maps each original glyph to at most one replacement, with no state machine and no context.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The subtable body is a single AAT <see cref="LookupTable"/> after the common header; there is no state header, no entry array, and no per-glyph table beyond the lookup itself.</description></item>
/// <item><description>Because every covered glyph is replaced independently, the subtable is the cheapest metamorphosis action available and is the natural home for single-glyph substitutions that need sub-feature gating.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortSubtable"/>
/// <seealso cref="LookupTable"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
public sealed record MortNonContextualSubstitutionSubtable : MortSubtable, IDerivedRecord<MortSubtable, MortNonContextualSubstitutionSubtable>
{
    /// <inheritdoc/>
    public override MortSubtableType Type => MortSubtableType.NonContextualSubstitution;

    /// <summary>Gets the glyph-to-glyph substitution lookup.</summary>
    /// <value>The AAT lookup that maps each original glyph ID to its replacement. Glyphs the lookup does not cover are kept unchanged.</value>
    public required LookupTable Lookup { get; init; }

    /// <summary>Substitutes a glyph, returning the original glyph ID when the lookup does not cover it.</summary>
    /// <param name="glyphId">The original glyph ID.</param>
    /// <returns>The replacement glyph ID when the lookup covers the input; otherwise <paramref name="glyphId"/> unchanged.</returns>
    public int Substitute(int glyphId) =>
        Lookup.TryGetValue(glyphId, out uint newGlyph) ? (int)newGlyph : glyphId;

    /// <summary>Attempts to substitute a glyph.</summary>
    /// <param name="glyphId">The original glyph ID.</param>
    /// <param name="newGlyphId">When this method returns <see langword="true"/>, contains the replacement glyph ID; otherwise, contains <paramref name="glyphId"/> unchanged.</param>
    /// <returns><see langword="true"/> when the lookup covers <paramref name="glyphId"/>; otherwise, <see langword="false"/>.</returns>
    public bool TrySubstitute(int glyphId, out int newGlyphId)
    {
        if (Lookup.TryGetValue(glyphId, out uint newGlyph))
        {
            newGlyphId = (int)newGlyph;
            return true;
        }

        newGlyphId = glyphId;
        return false;
    }

    static MortNonContextualSubstitutionSubtable IDerivedRecord<MortSubtable, MortNonContextualSubstitutionSubtable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not MortSubtable.Context ctx)
            throw new InvalidDataException(
                "'mort' non-contextual substitution subtable requires a glyph-count context.");

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

/// <summary>A contextual glyph insertion subtable: inserts extra glyphs at the marked or current position based on a classic state machine.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The subtable consists of a <see cref="StateHeader"/>, the class and state arrays read by <see cref="MortStateMachine"/>, a table of 8-byte <see cref="MortInsertionEntry"/> records, and zero or more glyph lists that entries reference for insertion.</description></item>
/// <item><description>Each entry carries two byte offsets into the subtable's insertion-list region: <see cref="MortInsertionEntry.CurrentInsertList"/> and <see cref="MortInsertionEntry.MarkedInsertList"/>. A zero offset means "no insertion at that position".</description></item>
/// <item><description>Two flavors of insertion are supported: <em>kashida-like</em>, where the inserted glyphs are selected with the base glyph and expand during justification, and <em>split-vowel-like</em>, where they are selected with a different glyph and behave independently.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortSubtable"/>
/// <seealso cref="MortInsertionEntry"/>
/// <seealso cref="MortStateMachine"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
public sealed record MortInsertionSubtable : MortSubtable, IDerivedRecord<MortSubtable, MortInsertionSubtable>
{
    /// <inheritdoc/>
    public override MortSubtableType Type => MortSubtableType.Insertion;

    /// <summary>Gets the classic state machine that drives the insertions.</summary>
    /// <value>The class array, state array, and class-table header parsed from the subtable.</value>
    public required MortStateMachine StateMachine { get; init; }

    /// <summary>Gets the entry array that state transitions select into.</summary>
    /// <value>One <see cref="MortInsertionEntry"/> per entry-table slot. The array length is derived from the subtable's length minus the entry-table offset, divided by eight.</value>
    public required ReadOnlyMemory<MortInsertionEntry> Entries { get; init; }

    /// <summary>Gets the insertion lists referenced by entries, keyed by their byte offset from the state-header start.</summary>
    /// <value>One <c>UInt16</c> region per distinct non-zero <see cref="MortInsertionEntry.CurrentInsertList"/> or <see cref="MortInsertionEntry.MarkedInsertList"/> offset. Each region is sized to the largest count any entry declares for that offset.</value>
    public required IReadOnlyDictionary<ushort, ReadOnlyMemory<ushort>> InsertListsByOffset { get; init; }

    /// <summary>Advances the state machine one step.</summary>
    /// <param name="stateIndex">The current state index in <c>[0, StateMachine.StateCount)</c>.</param>
    /// <param name="glyphClass">The class for the current glyph, obtained from <see cref="MortStateMachine.ClassifyGlyph(int)"/>.</param>
    /// <returns>The entry for the transition.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="stateIndex"/> or <paramref name="glyphClass"/> is out of range.</exception>
    public MortInsertionEntry Step(int stateIndex, int glyphClass)
    {
        int entryIndex = StateMachine.GetEntryIndex(stateIndex, glyphClass);
        return Entries.Span[entryIndex];
    }

    /// <summary>Gets the glyphs to insert at the current position for an entry.</summary>
    /// <param name="entry">The state entry the machine just stepped into.</param>
    /// <returns>The slice of the insertion list selected by the entry's current insert count, or an empty span when the entry performs no current insertion.</returns>
    public ReadOnlySpan<ushort> GetCurrentInsertList(MortInsertionEntry entry)
    {
        if (entry.CurrentInsertList == 0 || entry.CurrentInsertCount == 0)
            return ReadOnlySpan<ushort>.Empty;
        if (!InsertListsByOffset.TryGetValue(entry.CurrentInsertList, out var list))
            return ReadOnlySpan<ushort>.Empty;
        return list.Span[..entry.CurrentInsertCount];
    }

    /// <summary>Gets the glyphs to insert at the marked position for an entry.</summary>
    /// <param name="entry">The state entry the machine just stepped into.</param>
    /// <returns>The slice of the insertion list selected by the entry's marked insert count, or an empty span when the entry performs no marked insertion.</returns>
    public ReadOnlySpan<ushort> GetMarkedInsertList(MortInsertionEntry entry)
    {
        if (entry.MarkedInsertList == 0 || entry.MarkedInsertCount == 0)
            return ReadOnlySpan<ushort>.Empty;
        if (!InsertListsByOffset.TryGetValue(entry.MarkedInsertList, out var list))
            return ReadOnlySpan<ushort>.Empty;
        return list.Span[..entry.MarkedInsertCount];
    }

    static MortInsertionSubtable IDerivedRecord<MortSubtable, MortInsertionSubtable>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not MortSubtable.Context)
            throw new InvalidDataException(
                "'mort' insertion subtable requires a glyph-count context.");

        long subtableStart = cursor.Position;
        SubtableHeader header = cursor.ReadEndianReversibleStruct<SubtableHeader>();

        long stateHeaderStart = cursor.Position;
        StateHeader state = cursor.ReadEndianReversibleStruct<StateHeader>();

        MortStateMachine stateMachine = MortStateMachine.Parse(ref cursor, stateHeaderStart, state);

        long subtableEnd = subtableStart + header.Length;
        int entryCount = checked((int)((subtableEnd - cursor.Position) / 8));
        MortInsertionEntry[] entries = cursor.ReadEndianReversibleStructArray<MortInsertionEntry>(entryCount);

        var maxCountsByOffset = new Dictionary<ushort, int>();
        void Track(ushort offset, int count)
        {
            if (offset == 0 || count == 0) return;
            if (!maxCountsByOffset.TryGetValue(offset, out int existing) || count > existing)
                maxCountsByOffset[offset] = count;
        }

        foreach (MortInsertionEntry entry in entries)
        {
            Track(entry.CurrentInsertList, entry.CurrentInsertCount);
            Track(entry.MarkedInsertList, entry.MarkedInsertCount);
        }

        var insertLists = new Dictionary<ushort, ReadOnlyMemory<ushort>>();
        foreach (var (offset, count) in maxCountsByOffset)
        {
            cursor.Position = stateHeaderStart + offset;
            insertLists[offset] = cursor.ReadUInt16Array(count);
        }

        cursor.Position = subtableEnd;

        return new()
        {
            Length = header.Length,
            Coverage = header.Coverage,
            SubFeatureFlags = header.SubFeatureFlags,
            StateMachine = stateMachine,
            Entries = entries,
            InsertListsByOffset = insertLists,
        };
    }
}

/// <summary>A single entry in a glyph insertion subtable: a next-state reference, control and flavor flags, and two byte offsets into the insertion-list region.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Fixed-layout record, 8 bytes on disk. Implements <see cref="IEndianReversibleStruct{T}"/> so the entry array is read in a single bulk call.</description></item>
/// <item><description>The <see cref="NewState"/> field is a byte offset from the start of the state header, not a state index; the parser preserves the raw offset and leaves index conversion to the consumer.</description></item>
/// <item><description>The insertion <em>count</em> for each insertion list is a five-bit field packed into <see cref="Flags"/>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MortInsertionSubtable"/>
/// <seealso cref="IEndianReversibleStruct{T}"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct MortInsertionEntry : IEndianReversibleStruct<MortInsertionEntry>
{
    /// <summary>The next state's byte offset from the start of the state header at byte offset 0.</summary>
    public ushort NewState;            // +0

    /// <summary>The action flags word at byte offset 2.</summary>
    public ushort Flags;               // +2

    /// <summary>Byte offset from the state-header start to the current-position insertion list at byte offset 4.</summary>
    /// <value>The byte position of the glyph list to insert at the current position. Zero means no insertion.</value>
    public ushort CurrentInsertList;   // +4

    /// <summary>Byte offset from the state-header start to the marked-position insertion list at byte offset 6.</summary>
    /// <value>The byte position of the glyph list to insert at the marked position. Zero means no insertion.</value>
    public ushort MarkedInsertList;    // +6

    /// <summary>Gets a value indicating whether the current glyph becomes the marked glyph.</summary>
    /// <value><see langword="true"/> when bit <c>0x8000</c> of <see cref="Flags"/> is set.</value>
    public readonly bool SetMark => (Flags & 0x8000) != 0;

    /// <summary>Gets a value indicating whether the state machine should stay on the current glyph.</summary>
    /// <value><see langword="true"/> when bit <c>0x4000</c> of <see cref="Flags"/> is set.</value>
    public readonly bool DontAdvance => (Flags & 0x4000) != 0;

    /// <summary>Gets a value indicating whether the current insertion behaves like a kashida.</summary>
    /// <value><see langword="true"/> when bit <c>0x2000</c> of <see cref="Flags"/> is set.</value>
    public readonly bool CurrentIsKashidaLike => (Flags & 0x2000) != 0;

    /// <summary>Gets a value indicating whether the marked insertion behaves like a kashida.</summary>
    /// <value><see langword="true"/> when bit <c>0x1000</c> of <see cref="Flags"/> is set.</value>
    public readonly bool MarkedIsKashidaLike => (Flags & 0x1000) != 0;

    /// <summary>Gets a value indicating whether the current insertion goes before the current glyph.</summary>
    /// <value><see langword="true"/> when bit <c>0x0800</c> of <see cref="Flags"/> is set.</value>
    public readonly bool CurrentInsertBefore => (Flags & 0x0800) != 0;

    /// <summary>Gets a value indicating whether the marked insertion goes before the marked glyph.</summary>
    /// <value><see langword="true"/> when bit <c>0x0400</c> of <see cref="Flags"/> is set.</value>
    public readonly bool MarkedInsertBefore => (Flags & 0x0400) != 0;

    /// <summary>Gets the number of glyphs to insert at the current position.</summary>
    /// <value>The five-bit <c>currentInsertCount</c> field: bits <c>0x03E0</c> of <see cref="Flags"/> shifted down to <c>[0, 31]</c>.</value>
    public readonly byte CurrentInsertCount => (byte)((Flags & 0x03E0) >> 5);

    /// <summary>Gets the number of glyphs to insert at the marked position.</summary>
    /// <value>The five-bit <c>markedInsertCount</c> field: bits <c>0x001F</c> of <see cref="Flags"/>, in <c>[0, 31]</c>.</value>
    public readonly byte MarkedInsertCount => (byte)(Flags & 0x001F);

    /// <summary>Reverses the byte order of all four 16-bit fields in one pass.</summary>
    /// <param name="value">The value whose fields are to be reversed.</param>
    /// <returns>A new entry with all four fields reversed.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
    public static MortInsertionEntry ReverseEndianness(MortInsertionEntry value)
    {
        Span<ushort> words = MemoryMarshal.CreateSpan(ref value.NewState, 4);
        BinaryPrimitives.ReverseEndianness(words, words);
        return value;
    }
}
