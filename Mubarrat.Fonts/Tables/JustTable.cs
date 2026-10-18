using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Sfnt;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// The 'just' table — AAT justification
//
// Uses the shared AAT infrastructure from AatShared.cs: BinSrchHeader, LookupTable and
// its format-specific subclasses, StateHeader, ClassTableHeader, AatClass.
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>just</c> table: AAT justification information for stretching or shrinking a line of text to fit a given width or height.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Justification works per glyph, using width-delta factors, a justification category, and a justification priority. This table stores all three, together with per-direction lookup and postcompensation structures.</description></item>
/// <item><description>Horizontal and vertical justification are stored separately, each behind a <see cref="JustificationDirection"/>. A direction is optional; its top-level offset is zero when absent.</description></item>
/// <item><description>Per-direction data comprises a glyph-to-cluster lookup, one or more width-delta clusters, an optional postcompensation subtable, and an optional justification-category state table.</description></item>
/// <item><description>All multi-byte reads use the sfnt default of big-endian.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html"><c>just</c> table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="JustificationDirection"/>
/// <seealso cref="LookupTable"/>
/// <seealso cref="JustificationCategoryStateTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
public sealed record JustTable : IFontTable<JustTable>, IRecord<JustTable>
{
    /// <summary>The only version defined by the current specification, as a 16.16 <c>Fixed</c> value.</summary>
    /// <value>The bit pattern <c>0x00010000</c>, corresponding to <c>Fixed</c> 1.0.</value>
    /// <seealso cref="Version"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
    public const uint CurrentVersion = 0x00010000;

    /// <summary>The only format defined by the current specification.</summary>
    /// <value>The constant <c>0</c>.</value>
    /// <seealso cref="Format"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
    public const ushort CurrentFormat = 0;

    /// <inheritdoc/>
    public static Tag Tag => "just";

    /// <summary>Gets the table version.</summary>
    /// <value>The 16.16 <c>Fixed</c> version read from the header. Always <see cref="CurrentVersion"/>; the parser rejects any other value.</value>
    /// <seealso cref="CurrentVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
    public uint Version { get; init; }

    /// <summary>Gets the table format.</summary>
    /// <value>The format value read from the header. Always <see cref="CurrentFormat"/>; the parser rejects any other value.</value>
    /// <seealso cref="CurrentFormat"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
    public ushort Format { get; init; }

    /// <summary>Gets the horizontal justification data.</summary>
    /// <value>The parsed <see cref="JustificationDirection"/> for horizontal text, or <see langword="null"/> when the table's <c>horizOffset</c> field is zero.</value>
    /// <remarks>When absent, the font provides no horizontal justification information. A vertical-only table is unusual but legal.</remarks>
    /// <seealso cref="JustificationDirection"/>
    /// <seealso cref="Vertical"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
    public JustificationDirection? Horizontal { get; init; }

    /// <summary>Gets the vertical justification data.</summary>
    /// <value>The parsed <see cref="JustificationDirection"/> for vertical text, or <see langword="null"/> when the table's <c>vertOffset</c> field is zero.</value>
    /// <remarks>When absent, the font provides no vertical justification information.</remarks>
    /// <seealso cref="JustificationDirection"/>
    /// <seealso cref="Horizontal"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
    public JustificationDirection? Vertical { get; init; }

    static JustTable IRecord<JustTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
        {
            throw new InvalidDataException(
                $"'just'.version is 0x{header.Version:X8}, expected 0x{CurrentVersion:X8}.");
        }

        if (header.Format != CurrentFormat)
        {
            throw new InvalidDataException(
                $"'just'.format is {header.Format}, expected {CurrentFormat}.");
        }

        int numGlyphs = context is SfntFontFace face
            ? face.GetTable<MaxpTable>().NumGlyphs
            : throw new InvalidDataException(
                "'just' requires an SFNT font face context to obtain the glyph count.");

        // The direction headers' offsets are relative to the table start, not to the
        // direction header. The context carries the table-scoped source so the
        // direction parser can resolve those offsets without position arithmetic.
        var directionContext = new JustificationDirection.Context(cursor.Source, numGlyphs);

        JustificationDirection? horizontal = header.HorizOffset != 0
            ? cursor.Source.ParseRecordAt<JustificationDirection>(header.HorizOffset, directionContext)
            : null;
        JustificationDirection? vertical = header.VertOffset != 0
            ? cursor.Source.ParseRecordAt<JustificationDirection>(header.VertOffset, directionContext)
            : null;

        return new()
        {
            Version = header.Version,
            Format = header.Format,
            Horizontal = horizontal,
            Vertical = vertical,
        };
    }

    /// <summary>The 10-byte <c>just</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The version is a 16.16 <c>Fixed</c> value stored as <c>UInt32</c>; the remaining three fields are <c>UInt16</c>.</description></item>
    /// <item><description>Fields are reversed field-by-field because the version is 32 bits while the rest are 16 bits.</description></item>
    /// <item><description>Declared as a mutable <c>record struct</c> with <see cref="StructLayoutAttribute"/> so its fields honour the wire layout; the <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>The header is consumed at parse time and not retained on <see cref="JustTable"/>; its field values are exposed through the semantic record's properties.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="JustTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Table version, a 16.16 <c>Fixed</c> value, at byte offset 0.</summary>
        /// <value>The constant <see cref="CurrentVersion"/>.</value>
        /// <seealso cref="CurrentVersion"/>
        /// <seealso cref="Version"/>
        public uint Version;       // +0

        /// <summary>Table format at byte offset 4.</summary>
        /// <value>The constant <see cref="CurrentFormat"/>.</value>
        /// <seealso cref="CurrentFormat"/>
        /// <seealso cref="Format"/>
        public ushort Format;      // +4

        /// <summary>Table-relative byte offset of the horizontal justification data at byte offset 6, or zero if absent.</summary>
        /// <value>The byte offset from the start of the table to the horizontal <see cref="JustificationDirection"/>.</value>
        /// <seealso cref="HorizOffset"/>
        /// <seealso cref="VertOffset"/>
        public ushort HorizOffset; // +6

        /// <summary>Table-relative byte offset of the vertical justification data at byte offset 8, or zero if absent.</summary>
        /// <value>The byte offset from the start of the table to the vertical <see cref="JustificationDirection"/>.</value>
        /// <seealso cref="VertOffset"/>
        /// <seealso cref="HorizOffset"/>
        public ushort VertOffset;  // +8

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>The version is a 32-bit field while the rest are 16-bit, so the reversal is field-by-field rather than a single word-span pass.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(value.Version),
            Format = BinaryPrimitives.ReverseEndianness(value.Format),
            HorizOffset = BinaryPrimitives.ReverseEndianness(value.HorizOffset),
            VertOffset = BinaryPrimitives.ReverseEndianness(value.VertOffset),
        };
    }
}

/// <summary>Justification data for one direction: a glyph-to-cluster lookup, the width-delta clusters, and optional postcompensation and category state subtables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>All offsets in the direction header are relative to the start of the enclosing <c>just</c> table, not to the direction header itself; <see cref="Context.TableSource"/> supplies that coordinate system.</description></item>
/// <item><description>The glyph lookup follows the direction header sequentially; the width-delta cluster array, postcompensation subtable, and category state table are reached through table-relative offsets.</description></item>
/// <item><description>A direction header whose <c>justClassTableOffset</c> is zero means every glyph has justification category 0; a zero <c>pcTableOffset</c> means no postcompensation.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="JustTable"/>
/// <seealso cref="LookupTable"/>
/// <seealso cref="WidthDeltaCluster"/>
/// <seealso cref="PostcompensationSubtable"/>
/// <seealso cref="JustificationCategoryStateTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
public sealed record JustificationDirection : IRecord<JustificationDirection>
{
    /// <summary>Gets the glyph-to-cluster lookup table.</summary>
    /// <value>A lookup whose value for glyph <c>i</c> is a byte offset from the start of the width-delta-cluster region to the cluster that applies to glyph <c>i</c>.</value>
    /// <remarks>A lookup value of zero conventionally means the glyph has no cluster and therefore does not participate in justification.</remarks>
    /// <seealso cref="GetCluster(int)"/>
    /// <seealso cref="WidthDeltaClustersByOffset"/>
    /// <seealso cref="LookupTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
    public required LookupTable GlyphToClusterLookup { get; init; }

    /// <summary>Gets the width-delta clusters, keyed by their byte offset from the start of the width-delta-cluster region.</summary>
    /// <value>One entry per distinct cluster. A glyph's cluster is resolved by looking up its offset in <see cref="GlyphToClusterLookup"/> and finding the matching key.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Multiple glyphs can share a cluster, so the dictionary may have fewer entries than the glyph count.</description></item>
    /// <item><description>Every cluster that appears in <see cref="GlyphToClusterLookup"/> is reachable by its offset; clusters that no glyph references are not parsed.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetCluster(int)"/>
    /// <seealso cref="WidthDeltaCluster"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
    public required IReadOnlyDictionary<ushort, WidthDeltaCluster> WidthDeltaClustersByOffset { get; init; }

    /// <summary>Gets the postcompensation subtable, or <see langword="null"/> when none is present.</summary>
    /// <value>The parsed <see cref="PostcompensationSubtable"/>, or <see langword="null"/> when the direction header's <c>pcTableOffset</c> is zero.</value>
    /// <seealso cref="PostcompensationSubtable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
    public PostcompensationSubtable? Postcompensation { get; init; }

    /// <summary>Gets the justification-category state table, or <see langword="null"/> when the direction uses only category 0.</summary>
    /// <value>The parsed <see cref="JustificationCategoryStateTable"/>, or <see langword="null"/> when the direction header's <c>justClassTableOffset</c> is zero.</value>
    /// <seealso cref="JustificationCategoryStateTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
    public JustificationCategoryStateTable? CategoryStateTable { get; init; }

    /// <summary>Gets the width-delta cluster that applies to <paramref name="glyphId"/>.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The cluster for the glyph, or <see langword="null"/> when the glyph's lookup value is zero, exceeds <see cref="ushort"/> range, or points to no cluster.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>A lookup value of zero means the glyph has no cluster and therefore does not participate in justification.</description></item>
    /// <item><description>The method returns <see langword="null"/> rather than throwing when a malformed table contains a lookup value that is not a valid cluster offset.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GlyphToClusterLookup"/>
    /// <seealso cref="WidthDeltaClustersByOffset"/>
    /// <seealso cref="WidthDeltaCluster"/>
    public WidthDeltaCluster? GetCluster(int glyphId) => !GlyphToClusterLookup.TryGetValue(glyphId, out uint raw) || raw > ushort.MaxValue
        ? null
        : WidthDeltaClustersByOffset.TryGetValue((ushort)raw, out var cluster) ? cluster : null;

    static JustificationDirection IRecord<JustificationDirection>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
            throw new InvalidDataException("'just' direction requires a table-source and glyph-count context.");

        DirectionHeader header = cursor.ReadEndianReversibleStruct<DirectionHeader>();

        // The lookup follows the direction header sequentially. Its format-specific
        // parsers are self-contained and do not resolve sub-offsets against position.
        LookupTable lookup = cursor.ReadRecord<LookupTable>(new LookupTable.Context(ctx.GlyphCount));

        long wdcStart = header.WdcTableOffset;
        long wdcEnd = header.PcTableOffset == 0 ? ctx.TableSource.Length : header.PcTableOffset;

        var clustersByOffset = new Dictionary<ushort, WidthDeltaCluster>();
        long clusterPos = wdcStart;
        long pairSize = Unsafe.SizeOf<WidthDeltaPair>();
        while (clusterPos < wdcEnd)
        {
            ushort clusterOffset = (ushort)(clusterPos - wdcStart);
            WidthDeltaCluster cluster = ctx.TableSource.ParseRecordAt<WidthDeltaCluster>(clusterPos, null);
            clustersByOffset[clusterOffset] = cluster;

            long clusterSize = sizeof(uint) + cluster.Pairs.Length * pairSize;
            clusterPos += (clusterSize + 3L) & ~3L;
        }

        PostcompensationSubtable? postcompensation = header.PcTableOffset != 0
            ? ctx.TableSource.ParseRecordAt<PostcompensationSubtable>(
                header.PcTableOffset,
                new PostcompensationSubtable.Context(ctx.GlyphCount))
            : null;

        JustificationCategoryStateTable? stateTable = header.JustClassTableOffset != 0
            ? ctx.TableSource.ParseRecordAt<JustificationCategoryStateTable>(header.JustClassTableOffset, null)
            : null;

        return new()
        {
            GlyphToClusterLookup = lookup,
            WidthDeltaClustersByOffset = clustersByOffset,
            Postcompensation = postcompensation,
            CategoryStateTable = stateTable,
        };
    }

    /// <summary>Context for parsing a <see cref="JustificationDirection"/>: the table-scoped source and the font's glyph count.</summary>
    /// <param name="TableSource">The source whose coordinate system the direction header's offsets resolve against. This is the full <c>just</c> table source; the direction header is parsed from a slice of it, but its internal offsets are table-relative.</param>
    /// <param name="GlyphCount">Number of glyphs in the font, from <c>maxp.numGlyphs</c>.</param>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The table source is needed because the direction header's offsets are relative to the table, not to the direction header.</description></item>
    /// <item><description>The glyph count is needed by the glyph-to-cluster lookup, whose format 0 is a dense array sized to the font's glyph count.</description></item>
    /// <item><description>The context is a record type so that the wrapped values cannot be confused with bare integers at the call site.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LookupTable.Context"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
    public record Context(Source TableSource, int GlyphCount);

    /// <summary>The 6-byte direction header: three table-relative byte offsets.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Every offset is relative to the start of the enclosing <c>just</c> table, not to the direction header itself.</description></item>
    /// <item><description>All three fields are <c>UInt16</c>, so the reversal is a single pass over the struct reinterpreted as three 16-bit words.</description></item>
    /// <item><description>Declared as a mutable <c>record struct</c> with <see cref="StructLayoutAttribute"/> so its fields honour the wire layout; the <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>Public because a writer fills the struct in memory, reverses it, and writes the bytes by blittable cast.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="JustificationDirection"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct DirectionHeader : IEndianReversibleStruct<DirectionHeader>
    {
        /// <summary>Table-relative byte offset of the justification-category state table at byte offset 0, or zero if absent.</summary>
        /// <value>The byte offset of the <see cref="JustificationCategoryStateTable"/> from the start of the enclosing <c>just</c> table.</value>
        /// <seealso cref="JustificationCategoryStateTable"/>
        public ushort JustClassTableOffset; // +0

        /// <summary>Table-relative byte offset of the width-delta-cluster region at byte offset 2.</summary>
        /// <value>The byte offset of the first <see cref="WidthDeltaCluster"/> from the start of the enclosing <c>just</c> table.</value>
        /// <seealso cref="WidthDeltaCluster"/>
        public ushort WdcTableOffset;       // +2

        /// <summary>Table-relative byte offset of the postcompensation subtable at byte offset 4, or zero if absent.</summary>
        /// <value>The byte offset of the <see cref="PostcompensationSubtable"/> from the start of the enclosing <c>just</c> table.</value>
        /// <seealso cref="PostcompensationSubtable"/>
        public ushort PcTableOffset;        // +4

        /// <summary>Reverses the byte order of all three 16-bit fields in one pass.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with all three fields reversed.</returns>
        /// <remarks>The header is entirely <c>UInt16</c> fields, so it is reinterpreted as a span of three 16-bit words and reversed with a single <see cref="BinaryPrimitives.ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})"/> call.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static DirectionHeader ReverseEndianness(DirectionHeader value)
        {
            Span<ushort> words = MemoryMarshal.CreateSpan(ref value.JustClassTableOffset, 3);
            BinaryPrimitives.ReverseEndianness(words, words);
            return value;
        }
    }
}

/// <summary>A width-delta cluster: the justification-category / width-delta-entry pairs that apply to a group of glyphs.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A cluster groups glyphs whose grow and shrink behavior is described by the same set of width-delta entries, one per justification category.</description></item>
/// <item><description>Pairs are sorted ascending by <see cref="WidthDeltaPair.JustClass"/> and begin at 0, per the specification.</description></item>
/// <item><description>A glyph is associated with exactly one cluster through the direction's glyph-to-cluster lookup; the glyph's justification category selects one pair within that cluster.</description></item>
/// <item><description>The cluster begins on a longword boundary within the enclosing <c>just</c> table; the caller supplies the offset on a source whose origin matches the cluster's coordinate system.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="JustificationDirection.GetCluster(int)"/>
/// <seealso cref="WidthDeltaPair"/>
/// <seealso cref="JustWidthDeltaEntry"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
public sealed record WidthDeltaCluster : IRecord<WidthDeltaCluster>
{
    /// <summary>Gets the pairs in the cluster, sorted ascending by <see cref="WidthDeltaPair.JustClass"/>.</summary>
    /// <value>One <see cref="WidthDeltaPair"/> per justification category that applies to the cluster's glyphs. The array length equals the cluster's on-disk <c>count</c> field.</value>
    /// <remarks>Pairs typically number one to four per cluster; a linear scan in <see cref="GetEntry(uint)"/> is cheaper than a binary search at this size.</remarks>
    /// <seealso cref="GetEntry(uint)"/>
    /// <seealso cref="WidthDeltaPair"/>
    public required WidthDeltaPair[] Pairs { get; init; }

    /// <summary>Gets the width-delta entry for a justification category.</summary>
    /// <param name="justClass">The justification category, 7 bits wide.</param>
    /// <returns>The entry, or <see langword="null"/> when the cluster has no pair for <paramref name="justClass"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The pairs are small enough that a linear scan is cheaper than a binary search; a cluster typically has one to four entries.</description></item>
    /// <item><description>The lookup compares the masked <see cref="WidthDeltaPair.JustClass"/> value, so callers do not need to pre-mask <paramref name="justClass"/> themselves.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Pairs"/>
    /// <seealso cref="WidthDeltaPair.JustClass"/>
    /// <seealso cref="JustWidthDeltaEntry"/>
    public JustWidthDeltaEntry? GetEntry(uint justClass)
    {
        foreach (WidthDeltaPair pair in Pairs)
            if (pair.JustClass == justClass)
                return pair.Entry;
        return null;
    }

    static WidthDeltaCluster IRecord<WidthDeltaCluster>.Parse(ref Cursor cursor, object? context)
    {
        uint count = cursor.ReadUInt32();
        return count > int.MaxValue
            ? throw new InvalidDataException($"'just' width-delta cluster count {count} exceeds the supported maximum.")
            : new() { Pairs = cursor.ReadEndianReversibleStructArray<WidthDeltaPair>((int)count) };
    }
}

/// <summary>A width-delta pair: a justification category and the width-delta entry that applies to it.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Fixed-layout record, 24 bytes on disk. Implements <see cref="IEndianReversibleStruct{T}"/> so an entire cluster's pair array is read in one bulk call.</description></item>
/// <item><description>The <c>justClass</c> field is nominally <c>UInt32</c>, but only the low seven bits are meaningful; the high bits exist to keep the following entry longword-aligned.</description></item>
/// <item><description>The parser masks <see cref="JustClass"/> to seven bits after reading; the on-disk value is preserved raw in <see cref="RawJustClass"/>.</description></item>
/// <item><description>Reversal is field-by-field because the outer field is 32 bits, the nested entry's limits are 32-bit <see cref="Fixed"/>, and its flags are 16-bit.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="WidthDeltaCluster"/>
/// <seealso cref="JustWidthDeltaEntry"/>
/// <seealso cref="IEndianReversibleStruct{T}"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct WidthDeltaPair : IEndianReversibleStruct<WidthDeltaPair>
{
    /// <summary>The raw justification category field at byte offset 0.</summary>
    /// <value>The on-disk <c>justClass</c> value as stored, including the padding high bits. Use <see cref="JustClass"/> for the meaningful seven-bit category.</value>
    /// <seealso cref="JustClass"/>
    public uint RawJustClass;          // +0

    /// <summary>The width-delta entry for this category at byte offset 4.</summary>
    /// <value>The grow and shrink limits and flag words that apply to glyphs of the associated category.</value>
    /// <seealso cref="JustWidthDeltaEntry"/>
    /// <seealso cref="JustClass"/>
    public JustWidthDeltaEntry Entry;  // +4

    /// <summary>Gets the justification category, restricted to its seven meaningful bits.</summary>
    /// <value><see cref="RawJustClass"/> masked to <c>[0, 127]</c>.</value>
    /// <seealso cref="RawJustClass"/>
    /// <seealso cref="WidthDeltaCluster.GetEntry(uint)"/>
    public readonly uint JustClass => RawJustClass & 0x7Fu;

    /// <summary>Reverses the byte order of every multi-byte field in a <see cref="WidthDeltaPair"/>.</summary>
    /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new pair with each multi-byte field reversed, including the nested entry.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="JustWidthDeltaEntry.ReverseEndianness(JustWidthDeltaEntry)"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
    public static WidthDeltaPair ReverseEndianness(WidthDeltaPair value) => new()
    {
        RawJustClass = BinaryPrimitives.ReverseEndianness(value.RawJustClass),
        Entry = JustWidthDeltaEntry.ReverseEndianness(value.Entry),
    };
}

/// <summary>A width-delta entry: the grow and shrink limits for a glyph and the flags that control its justification behavior.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Fixed-layout record, 20 bytes on disk. Implements <see cref="IEndianReversibleStruct{T}"/> so arrays of entries are read in one bulk call.</description></item>
/// <item><description>The four limits are 16.16 <see cref="Fixed"/> ratios. Grow limits are non-negative; shrink limits are non-positive, per the specification.</description></item>
/// <item><description>The grow and shrink flag words share the same bit layout: bit <c>0x1000</c> marks the entry as taking unlimited gap, and the low four bits carry the priority.</description></item>
/// <item><description>Reversal is field-by-field because the four <see cref="Fixed"/> limits are 32-bit and the two flag words are 16-bit.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="WidthDeltaPair"/>
/// <seealso cref="JustificationPriority"/>
/// <seealso cref="IEndianReversibleStruct{T}"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct JustWidthDeltaEntry : IEndianReversibleStruct<JustWidthDeltaEntry>
{
    /// <summary>Gets the ratio by which the glyph is allowed to grow on the left or top side.</summary>
    /// <value>A 16.16 <see cref="Fixed"/> value, in ems per point, describing the maximum expansion on the leading side. Non-negative.</value>
    /// <seealso cref="AfterGrowLimit"/>
    /// <seealso cref="BeforeShrinkLimit"/>
    public Fixed BeforeGrowLimit;      // +0

    /// <summary>Gets the ratio by which the glyph is allowed to shrink on the left or top side.</summary>
    /// <value>A 16.16 <see cref="Fixed"/> value describing the maximum contraction on the leading side. Non-positive.</value>
    /// <seealso cref="BeforeGrowLimit"/>
    /// <seealso cref="AfterShrinkLimit"/>
    public Fixed BeforeShrinkLimit;    // +4

    /// <summary>Gets the ratio by which the glyph is allowed to grow on the right or bottom side.</summary>
    /// <value>A 16.16 <see cref="Fixed"/> value describing the maximum expansion on the trailing side. Non-negative.</value>
    /// <seealso cref="BeforeGrowLimit"/>
    /// <seealso cref="AfterShrinkLimit"/>
    public Fixed AfterGrowLimit;       // +8

    /// <summary>Gets the ratio by which the glyph is allowed to shrink on the right or bottom side.</summary>
    /// <value>A 16.16 <see cref="Fixed"/> value describing the maximum contraction on the trailing side. Non-positive.</value>
    /// <seealso cref="BeforeShrinkLimit"/>
    /// <seealso cref="AfterGrowLimit"/>
    public Fixed AfterShrinkLimit;     // +12

    /// <summary>Gets the raw grow-case flags word.</summary>
    /// <value>The on-disk flags word as stored. Use <see cref="GrowPriority"/> and <see cref="GrowUnlimited"/> for the meaningful bits.</value>
    /// <seealso cref="GrowPriority"/>
    /// <seealso cref="GrowUnlimited"/>
    /// <seealso cref="ShrinkFlags"/>
    public ushort GrowFlags;           // +16

    /// <summary>Gets the raw shrink-case flags word.</summary>
    /// <value>The on-disk flags word as stored. Use <see cref="ShrinkPriority"/> and <see cref="ShrinkUnlimited"/> for the meaningful bits.</value>
    /// <seealso cref="ShrinkPriority"/>
    /// <seealso cref="ShrinkUnlimited"/>
    /// <seealso cref="GrowFlags"/>
    public ushort ShrinkFlags;         // +18

    /// <summary>Gets a value indicating whether the glyph takes unlimited gap during the grow case.</summary>
    /// <value><see langword="true"/> when bit <c>0x1000</c> of <see cref="GrowFlags"/> is set.</value>
    /// <seealso cref="GrowFlags"/>
    /// <seealso cref="ShrinkUnlimited"/>
    public readonly bool GrowUnlimited => (GrowFlags & 0x1000) != 0;

    /// <summary>Gets a value indicating whether the glyph takes unlimited gap during the shrink case.</summary>
    /// <value><see langword="true"/> when bit <c>0x1000</c> of <see cref="ShrinkFlags"/> is set.</value>
    /// <seealso cref="ShrinkFlags"/>
    /// <seealso cref="GrowUnlimited"/>
    public readonly bool ShrinkUnlimited => (ShrinkFlags & 0x1000) != 0;

    /// <summary>Gets the grow-case justification priority.</summary>
    /// <value>The low four bits of <see cref="GrowFlags"/>, cast to <see cref="JustificationPriority"/>.</value>
    /// <seealso cref="JustificationPriority"/>
    /// <seealso cref="GrowFlags"/>
    /// <seealso cref="ShrinkPriority"/>
    public readonly JustificationPriority GrowPriority => (JustificationPriority)(GrowFlags & 0x000F);

    /// <summary>Gets the shrink-case justification priority.</summary>
    /// <value>The low four bits of <see cref="ShrinkFlags"/>, cast to <see cref="JustificationPriority"/>.</value>
    /// <seealso cref="JustificationPriority"/>
    /// <seealso cref="ShrinkFlags"/>
    /// <seealso cref="GrowPriority"/>
    public readonly JustificationPriority ShrinkPriority => (JustificationPriority)(ShrinkFlags & 0x000F);

    /// <summary>Reverses the byte order of every multi-byte field in a <see cref="JustWidthDeltaEntry"/>.</summary>
    /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new entry with each multi-byte field reversed.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
    public static JustWidthDeltaEntry ReverseEndianness(JustWidthDeltaEntry value) => new()
    {
        BeforeGrowLimit = Fixed.ReverseEndianness(value.BeforeGrowLimit),
        BeforeShrinkLimit = Fixed.ReverseEndianness(value.BeforeShrinkLimit),
        AfterGrowLimit = Fixed.ReverseEndianness(value.AfterGrowLimit),
        AfterShrinkLimit = Fixed.ReverseEndianness(value.AfterShrinkLimit),
        GrowFlags = BinaryPrimitives.ReverseEndianness(value.GrowFlags),
        ShrinkFlags = BinaryPrimitives.ReverseEndianness(value.ShrinkFlags),
    };
}

/// <summary>The stage at which a glyph begins to participate in justification. Lower values participate earlier.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Stored in the low four bits of the grow and shrink flag words of a <see cref="JustWidthDeltaEntry"/>.</description></item>
/// <item><description>During justification, the layout engine first processes glyphs at <see cref="Kashida"/> priority, then <see cref="Whitespace"/>, then <see cref="InterCharacter"/>.</description></item>
/// <item><description><see cref="Null"/> is not normally assigned to glyphs in a font; applications use it to override priorities during layout.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="JustWidthDeltaEntry.GrowPriority"/>
/// <seealso cref="JustWidthDeltaEntry.ShrinkPriority"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
public enum JustificationPriority : byte
{
    /// <summary>Kashida priority: the highest priority during justification, handled first.</summary>
    /// <value>The constant <c>0</c>.</value>
    Kashida = 0,

    /// <summary>Whitespace priority: the next highest priority after <see cref="Kashida"/>.</summary>
    /// <value>The constant <c>1</c>.</value>
    Whitespace = 1,

    /// <summary>Inter-character priority: assigned to remaining glyphs that participate after kashida and whitespace.</summary>
    /// <value>The constant <c>2</c>.</value>
    InterCharacter = 2,

    /// <summary>Null priority: the lowest priority, not normally assigned to glyphs in a font.</summary>
    /// <value>The constant <c>3</c>.</value>
    Null = 3,
}

/// <summary>The postcompensation subtable of a direction: a glyph-to-action lookup and the action records it references.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The lookup is one of the standard AAT lookup formats; a lookup value of zero means the glyph has no postcompensation action.</description></item>
/// <item><description>Lookup values are byte offsets from the start of the postcompensation subtable to the referenced action record.</description></item>
/// <item><description>The parser deduplicates actions by offset, so two glyphs that reference the same action share one instance in <see cref="ActionsByOffset"/>.</description></item>
/// <item><description>Only actions that some glyph references are parsed; actions present in the table but unreferenced are not reached, since they have no discoverable extent.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="JustificationDirection"/>
/// <seealso cref="ActionSubrecord"/>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
public sealed record PostcompensationSubtable : IRecord<PostcompensationSubtable>
{
    /// <summary>Gets the glyph-to-action lookup.</summary>
    /// <value>A lookup whose value for glyph <c>i</c> is a byte offset from the start of this subtable to the <see cref="ActionSubrecord"/> that applies to glyph <c>i</c>.</value>
    /// <remarks>A lookup value of zero conventionally means the glyph has no postcompensation action.</remarks>
    /// <seealso cref="GetAction(int)"/>
    /// <seealso cref="ActionsByOffset"/>
    /// <seealso cref="LookupTable"/>
    public required LookupTable GlyphToActionLookup { get; init; }

    /// <summary>Gets the action subrecords referenced by the lookup, keyed by their offset from the start of this subtable.</summary>
    /// <value>One entry per distinct referenced action. Multiple glyphs may reference the same action and thus share one subrecord instance.</value>
    /// <seealso cref="GetAction(int)"/>
    /// <seealso cref="ActionSubrecord"/>
    public required IReadOnlyDictionary<ushort, ActionSubrecord> ActionsByOffset { get; init; }

    /// <summary>Gets the postcompensation action for a glyph.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The action for the glyph, or <see langword="null"/> when the glyph's lookup value is zero, exceeds <see cref="ushort"/> range, or points to no action.</returns>
    /// <seealso cref="GlyphToActionLookup"/>
    /// <seealso cref="ActionsByOffset"/>
    /// <seealso cref="ActionSubrecord"/>
    public ActionSubrecord? GetAction(int glyphId) => !GlyphToActionLookup.TryGetValue(glyphId, out uint raw) || raw == 0 || raw > ushort.MaxValue
            ? null
            : ActionsByOffset.TryGetValue((ushort)raw, out var action) ? action : null;

    static PostcompensationSubtable IRecord<PostcompensationSubtable>.Parse(ref Cursor cursor, object? context)
    {
        int numGlyphs = context is Context ctx
            ? ctx.GlyphCount
            : throw new InvalidDataException(
                "'just' postcompensation requires a glyph count context.");

        LookupTable lookup = cursor.ReadRecord<LookupTable>(new LookupTable.Context(numGlyphs));

        var offsets = new HashSet<ushort>();
        for (int glyph = 0; glyph < numGlyphs; glyph++)
        {
            if (!lookup.TryGetValue(glyph, out uint raw) || raw == 0 || raw > ushort.MaxValue)
                continue;
            offsets.Add((ushort)raw);
        }

        var actionsByOffset = new Dictionary<ushort, ActionSubrecord>();
        foreach (ushort offset in offsets)
            actionsByOffset[offset] = cursor.Source.ParseRecordAt<ActionSubrecord>(offset, null);

        return new()
        {
            GlyphToActionLookup = lookup,
            ActionsByOffset = actionsByOffset,
        };
    }

    /// <summary>Context for parsing a <see cref="PostcompensationSubtable"/>: the font's glyph count.</summary>
    /// <param name="GlyphCount">Number of glyphs in the font, from <c>maxp.numGlyphs</c>.</param>
    /// <remarks>The glyph count is required by format-0 lookups, which are dense arrays sized to the font's glyph count.</remarks>
    /// <seealso cref="LookupTable.Context"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
    public record Context(int GlyphCount);
}

/// <summary>A postcompensation action: a <c>UInt32</c> count followed by that many <see cref="ActionSubrecord"/> records.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The specification does not define semantics for more than one subrecord in a single action, but the container allows it and the parser preserves the array.</description></item>
/// <item><description>Each subrecord is variable-length; the count is the only extent information, and each subrecord advances past itself using its declared <see cref="ActionSubrecordHeader.ActionLength"/>.</description></item>
/// <item><description>The count is a <c>UInt32</c>, but a value beyond <see cref="int.MaxValue"/> cannot be represented as an array length and is rejected as malformed.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PostcompensationSubtable"/>
/// <seealso cref="ActionSubrecord"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
public sealed record PostcompensationAction : IRecord<PostcompensationAction>
{
    /// <summary>Gets the action subrecords contained in this action.</summary>
    /// <value>The array of <see cref="ActionSubrecord"/> values that follow the leading count. Each subrecord carries its own type discriminant.</value>
    public required ActionSubrecord[] Subrecords { get; init; }

    static PostcompensationAction IRecord<PostcompensationAction>.Parse(ref Cursor cursor, object? context)
    {
        uint count = cursor.ReadUInt32();
        return count > int.MaxValue
            ? throw new InvalidDataException($"'just' postcompensation action count {count} exceeds the supported maximum.")
            : new() { Subrecords = cursor.ReadRecordArray<ActionSubrecord>((int)count) };
    }
}

/// <summary>Base for the six postcompensation action subrecord variants.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Every subrecord begins with the fixed 8-byte <see cref="ActionSubrecordHeader"/>: an <c>ActionClass</c>, an <c>ActionType</c>, and a total <c>ActionLength</c>.</description></item>
/// <item><description>The type discriminant selects one of six derived records; dispatch happens in <see cref="IRecord{T}.Parse(ref Cursor, object?)"/> via <see cref="IBaseRecord{TBase}"/>.</description></item>
/// <item><description>The declared length includes the payload and any padding needed to reach a longword boundary. Every derived parser advances the cursor to <see cref="ActionSubrecordHeader.ActionLength"/>, so the next subrecord begins at the correct offset regardless of the payload's natural size.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="DecompositionSubrecord"/>
/// <seealso cref="UnconditionalAddGlyphSubrecord"/>
/// <seealso cref="ConditionalAddGlyphSubrecord"/>
/// <seealso cref="StretchGlyphSubrecord"/>
/// <seealso cref="DuctileGlyphSubrecord"/>
/// <seealso cref="RepeatedAddGlyphSubrecord"/>
/// <seealso cref="ActionSubrecordHeader"/>
/// <seealso cref="IBaseRecord{TBase}"/>
/// <seealso cref="IDerivedRecord{TBase, TDerived}"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
public abstract record ActionSubrecord : IRecord<ActionSubrecord>, IBaseRecord<ActionSubrecord>
{
    /// <summary>Gets the justification class associated with this action.</summary>
    /// <value>The 16-bit <c>actionClass</c> field. The specification defines it as the <c>JustClass</c> value the action applies to.</value>
    /// <remarks>The glyph-to-action lookup already selects an action per glyph, so the class acts as a second filter within the action container.</remarks>
    /// <seealso cref="ActionSubrecordHeader.ActionClass"/>
    public required ushort ActionClass { get; init; }

    /// <summary>Gets the action type discriminant.</summary>
    /// <value>One of the six values defined by the specification: <c>0</c> decomposition, <c>1</c> unconditional add glyph, <c>2</c> conditional add glyph, <c>3</c> stretch glyph, <c>4</c> ductile glyph, <c>5</c> repeated add glyph.</value>
    public abstract ushort ActionType { get; }

    static ActionSubrecord IRecord<ActionSubrecord>.Parse(ref Cursor cursor, object? context)
    {
        // Peek the discriminant at offset 2 without advancing the cursor; the
        // derived parser reads the full header from offset 0.
        ushort actionType = cursor.Source.ReadUInt16At(cursor.Position + 2);
        return actionType switch
        {
            0 => IBaseRecord<ActionSubrecord>.Parse<DecompositionSubrecord>(ref cursor, null),
            1 => IBaseRecord<ActionSubrecord>.Parse<UnconditionalAddGlyphSubrecord>(ref cursor, null),
            2 => IBaseRecord<ActionSubrecord>.Parse<ConditionalAddGlyphSubrecord>(ref cursor, null),
            3 => IBaseRecord<ActionSubrecord>.Parse<StretchGlyphSubrecord>(ref cursor, null),
            4 => IBaseRecord<ActionSubrecord>.Parse<DuctileGlyphSubrecord>(ref cursor, null),
            5 => IBaseRecord<ActionSubrecord>.Parse<RepeatedAddGlyphSubrecord>(ref cursor, null),
            _ => throw new InvalidDataException(
                $"'just' postcompensation action type {actionType} is not defined."),
        };
    }
}

/// <summary>The 8-byte action subrecord header common to every <see cref="ActionSubrecord"/> variant.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Every subrecord begins with this header; its <see cref="ActionLength"/> spans the whole subrecord and is the cursor-advance authority for every derived parser.</description></item>
/// <item><description>Reversal is field-by-field because the two class/type fields are 16-bit and the length field is 32-bit.</description></item>
/// <item><description>Declared as a mutable <c>record struct</c> with <see cref="StructLayoutAttribute"/> so its fields honour the wire layout; the <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ActionSubrecord"/>
/// <seealso cref="IEndianReversibleStruct{T}"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct ActionSubrecordHeader : IEndianReversibleStruct<ActionSubrecordHeader>
{
    /// <summary>The justification class associated with the action at byte offset 0.</summary>
    /// <value>The 16-bit <c>actionClass</c> field, matching the <see cref="ActionSubrecord.ActionClass"/> the derived record exposes.</value>
    /// <seealso cref="ActionSubrecord.ActionClass"/>
    public ushort ActionClass;    // +0

    /// <summary>The action type discriminant at byte offset 2.</summary>
    /// <value>One of the six values defined by the specification; see <see cref="ActionSubrecord.ActionType"/> for the mapping.</value>
    /// <seealso cref="ActionSubrecord.ActionType"/>
    public ushort ActionType;     // +2

    /// <summary>The total subrecord length in bytes, including this header, at byte offset 4.</summary>
    /// <value>A multiple of four; the value spans the header, the payload, and any longword padding.</value>
    /// <seealso cref="ActionSubrecord"/>
    public uint ActionLength;     // +4

    /// <summary>Reverses the byte order of every multi-byte field in an <see cref="ActionSubrecordHeader"/>.</summary>
    /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new header with each field reversed.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
    public static ActionSubrecordHeader ReverseEndianness(ActionSubrecordHeader value) => new()
    {
        ActionClass = BinaryPrimitives.ReverseEndianness(value.ActionClass),
        ActionType = BinaryPrimitives.ReverseEndianness(value.ActionType),
        ActionLength = BinaryPrimitives.ReverseEndianness(value.ActionLength),
    };
}

/// <summary>A decomposition action: breaks a ligature into its component glyphs when the added interglyph space is large enough.</summary>
/// <seealso cref="ActionSubrecord"/>
/// <seealso cref="FixedPrefix"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
public sealed record DecompositionSubrecord : ActionSubrecord, IDerivedRecord<ActionSubrecord, DecompositionSubrecord>
{
    /// <inheritdoc/>
    public override ushort ActionType => 0;

    /// <summary>Gets the lower distance-factor bound; below it, the ligature decomposes.</summary>
    /// <value>A 16.16 <see cref="Fixed"/> value in ems.</value>
    /// <seealso cref="UpperLimit"/>
    public Fixed LowerLimit { get; init; }

    /// <summary>Gets the upper distance-factor bound; above it, the ligature decomposes.</summary>
    /// <value>A 16.16 <see cref="Fixed"/> value in ems.</value>
    /// <seealso cref="LowerLimit"/>
    public Fixed UpperLimit { get; init; }

    /// <summary>Gets the order in which this ligature decomposes relative to other decomposable ligatures on the line.</summary>
    /// <value>A <c>UInt16</c> used as a sort key; lower values decompose first.</value>
    /// <seealso cref="Glyphs"/>
    public ushort Order { get; init; }

    /// <summary>Gets the glyphs that the ligature decomposes into.</summary>
    /// <value>The array of <c>UInt16</c> glyph indices that follow the fixed prefix on disk.</value>
    /// <seealso cref="Order"/>
    public required ushort[] Glyphs { get; init; }

    static DecompositionSubrecord IDerivedRecord<ActionSubrecord, DecompositionSubrecord>.Parse(ref Cursor cursor, object? context)
    {
        ActionSubrecordHeader header = cursor.ReadEndianReversibleStruct<ActionSubrecordHeader>();
        FixedPrefix prefix = cursor.ReadEndianReversibleStruct<FixedPrefix>();
        ushort[] glyphs = cursor.ReadUInt16Array(prefix.DecomposedCount);
        cursor.Position = header.ActionLength;
        return new()
        {
            ActionClass = header.ActionClass,
            LowerLimit = prefix.LowerLimit,
            UpperLimit = prefix.UpperLimit,
            Order = prefix.Order,
            Glyphs = glyphs,
        };
    }

    /// <summary>The fixed 12-byte portion of a decomposition action, before the variable-length glyph array.</summary>
    /// <remarks>Follows the 8-byte <see cref="ActionSubrecordHeader"/>; reversal is field-by-field because the two limits are 32-bit <see cref="Fixed"/> values and the remaining two fields are 16-bit.</remarks>
    /// <seealso cref="DecompositionSubrecord"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct FixedPrefix : IEndianReversibleStruct<FixedPrefix>
    {
        /// <summary>The lower distance-factor bound at byte offset 0.</summary>
        public Fixed LowerLimit;       // +0

        /// <summary>The upper distance-factor bound at byte offset 4.</summary>
        public Fixed UpperLimit;       // +4

        /// <summary>The decomposition order sort key at byte offset 8.</summary>
        public ushort Order;           // +8

        /// <summary>The number of glyph indices that follow the prefix at byte offset 10.</summary>
        public ushort DecomposedCount; // +10

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="FixedPrefix"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new prefix with each field reversed.</returns>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static FixedPrefix ReverseEndianness(FixedPrefix value) => new()
        {
            LowerLimit = Fixed.ReverseEndianness(value.LowerLimit),
            UpperLimit = Fixed.ReverseEndianness(value.UpperLimit),
            Order = BinaryPrimitives.ReverseEndianness(value.Order),
            DecomposedCount = BinaryPrimitives.ReverseEndianness(value.DecomposedCount),
        };
    }
}

/// <summary>An unconditional add glyph action: adds a glyph after the current glyph whenever the advance width is growing.</summary>
/// <seealso cref="ActionSubrecord"/>
/// <seealso cref="ConditionalAddGlyphSubrecord"/>
/// <seealso cref="RepeatedAddGlyphSubrecord"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
public sealed record UnconditionalAddGlyphSubrecord : ActionSubrecord, IDerivedRecord<ActionSubrecord, UnconditionalAddGlyphSubrecord>
{
    /// <inheritdoc/>
    public override ushort ActionType => 1;

    /// <summary>Gets the glyph to add.</summary>
    /// <value>The glyph index inserted after the current glyph when the advance width is growing; typically a kashida glyph.</value>
    /// <seealso cref="ConditionalAddGlyphSubrecord.AddGlyph"/>
    /// <seealso cref="RepeatedAddGlyphSubrecord.Glyph"/>
    public ushort AddGlyph { get; init; }

    static UnconditionalAddGlyphSubrecord IDerivedRecord<ActionSubrecord, UnconditionalAddGlyphSubrecord>.Parse(ref Cursor cursor, object? context)
    {
        ActionSubrecordHeader header = cursor.ReadEndianReversibleStruct<ActionSubrecordHeader>();
        ushort addGlyph = cursor.ReadUInt16();
        cursor.Position = header.ActionLength;
        return new()
        {
            ActionClass = header.ActionClass,
            AddGlyph = addGlyph,
        };
    }
}

/// <summary>A conditional add glyph action: adds a glyph in the grow case, and substitutes the current glyph when the added space exceeds a threshold.</summary>
/// <seealso cref="ActionSubrecord"/>
/// <seealso cref="UnconditionalAddGlyphSubrecord"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
public sealed record ConditionalAddGlyphSubrecord : ActionSubrecord, IDerivedRecord<ActionSubrecord, ConditionalAddGlyphSubrecord>
{
    /// <inheritdoc/>
    public override ushort ActionType => 2;

    /// <summary>Gets the distance growth factor at which the current glyph is replaced and the factor recalculated.</summary>
    /// <value>A 16.16 <see cref="Fixed"/> value in ems.</value>
    /// <seealso cref="SubstGlyph"/>
    /// <seealso cref="AddGlyph"/>
    public Fixed SubstThreshold { get; init; }

    /// <summary>Gets the glyph to add, or <c>0xFFFF</c> when no glyph is added.</summary>
    /// <seealso cref="SubstGlyph"/>
    /// <seealso cref="SubstThreshold"/>
    public ushort AddGlyph { get; init; }

    /// <summary>Gets the glyph that replaces the current glyph when the growth factor meets or exceeds <see cref="SubstThreshold"/>.</summary>
    /// <seealso cref="SubstThreshold"/>
    /// <seealso cref="AddGlyph"/>
    public ushort SubstGlyph { get; init; }

    static ConditionalAddGlyphSubrecord IDerivedRecord<ActionSubrecord, ConditionalAddGlyphSubrecord>.Parse(ref Cursor cursor, object? context)
    {
        ActionSubrecordHeader header = cursor.ReadEndianReversibleStruct<ActionSubrecordHeader>();
        Payload payload = cursor.ReadEndianReversibleStruct<Payload>();
        cursor.Position = header.ActionLength;
        return new()
        {
            ActionClass = header.ActionClass,
            SubstThreshold = payload.SubstThreshold,
            AddGlyph = payload.AddGlyph,
            SubstGlyph = payload.SubstGlyph,
        };
    }

    /// <summary>The 8-byte payload of a conditional add glyph action.</summary>
    /// <remarks>Follows the 8-byte <see cref="ActionSubrecordHeader"/>; reversal is field-by-field because the threshold is 32-bit and the two glyph indices are 16-bit.</remarks>
    /// <seealso cref="ConditionalAddGlyphSubrecord"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Payload : IEndianReversibleStruct<Payload>
    {
        /// <summary>The distance growth factor threshold at byte offset 0.</summary>
        public Fixed SubstThreshold; // +0

        /// <summary>The glyph to add, or <c>0xFFFF</c> at byte offset 4.</summary>
        public ushort AddGlyph;      // +4

        /// <summary>The glyph to substitute at byte offset 6.</summary>
        public ushort SubstGlyph;    // +6

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Payload"/>.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new payload with each field reversed.</returns>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static Payload ReverseEndianness(Payload value) => new()
        {
            SubstThreshold = Fixed.ReverseEndianness(value.SubstThreshold),
            AddGlyph = BinaryPrimitives.ReverseEndianness(value.AddGlyph),
            SubstGlyph = BinaryPrimitives.ReverseEndianness(value.SubstGlyph),
        };
    }
}

/// <summary>A stretch glyph action: stretches the current glyph by a stretch factor instead of inserting or adding whitespace.</summary>
/// <remarks>The action carries no payload beyond the shared <see cref="ActionSubrecordHeader"/>. The specification notes that CoreText does not support this action and its use should be avoided.</remarks>
/// <seealso cref="ActionSubrecord"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
public sealed record StretchGlyphSubrecord : ActionSubrecord, IDerivedRecord<ActionSubrecord, StretchGlyphSubrecord>
{
    /// <inheritdoc/>
    public override ushort ActionType => 3;

    static StretchGlyphSubrecord IDerivedRecord<ActionSubrecord, StretchGlyphSubrecord>.Parse(ref Cursor cursor, object? context)
    {
        ActionSubrecordHeader header = cursor.ReadEndianReversibleStruct<ActionSubrecordHeader>();
        cursor.Position = header.ActionLength;
        return new() { ActionClass = header.ActionClass };
    }
}

/// <summary>A ductile glyph action: changes the glyph's shape through a font-variation axis.</summary>
/// <remarks>The specification notes that CoreText does not support this action and its use should be avoided.</remarks>
/// <seealso cref="ActionSubrecord"/>
/// <seealso cref="Tag"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
public sealed record DuctileGlyphSubrecord : ActionSubrecord, IDerivedRecord<ActionSubrecord, DuctileGlyphSubrecord>
{
    /// <inheritdoc/>
    public override ushort ActionType => 4;

    /// <summary>Gets the four-byte tag of the variation axis used for ductility.</summary>
    /// <value>The axis tag. The specification's convention is <c>duct</c>, but any axis defined in the font's <c>fvar</c> table may be used.</value>
    public Tag VariationAxis { get; init; }

    /// <summary>Gets the lowest value of the ductility axis that still yields an acceptable appearance.</summary>
    public Fixed MinimumLimit { get; init; }

    /// <summary>Gets the default value of the ductility axis, corresponding to no change in appearance.</summary>
    public Fixed NoStretchValue { get; init; }

    /// <summary>Gets the highest value of the ductility axis that still yields an acceptable appearance.</summary>
    public Fixed MaximumLimit { get; init; }

    static DuctileGlyphSubrecord IDerivedRecord<ActionSubrecord, DuctileGlyphSubrecord>.Parse(ref Cursor cursor, object? context)
    {
        ActionSubrecordHeader header = cursor.ReadEndianReversibleStruct<ActionSubrecordHeader>();
        Payload payload = cursor.ReadEndianReversibleStruct<Payload>();
        cursor.Position = header.ActionLength;
        return new()
        {
            ActionClass = header.ActionClass,
            VariationAxis = payload.VariationAxis,
            MinimumLimit = payload.MinimumLimit,
            NoStretchValue = payload.NoStretchValue,
            MaximumLimit = payload.MaximumLimit,
        };
    }

    /// <summary>The 16-byte payload of a ductile glyph action.</summary>
    /// <remarks>Follows the 8-byte <see cref="ActionSubrecordHeader"/>; the tag is reversed by <see cref="Tag.ReverseEndianness(Tag)"/> and the three limits by <see cref="Fixed.ReverseEndianness(Fixed)"/>.</remarks>
    /// <seealso cref="DuctileGlyphSubrecord"/>
    /// <seealso cref="Tag"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Payload : IEndianReversibleStruct<Payload>
    {
        /// <summary>The variation axis tag at byte offset 0.</summary>
        public Tag VariationAxis;    // +0

        /// <summary>The lowest acceptable axis value at byte offset 4.</summary>
        public Fixed MinimumLimit;   // +4

        /// <summary>The no-stretch axis value at byte offset 8.</summary>
        public Fixed NoStretchValue; // +8

        /// <summary>The highest acceptable axis value at byte offset 12.</summary>
        public Fixed MaximumLimit;   // +12

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Payload"/>.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new payload with each field reversed.</returns>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Tag.ReverseEndianness(Tag)"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static Payload ReverseEndianness(Payload value) => new()
        {
            VariationAxis = Tag.ReverseEndianness(value.VariationAxis),
            MinimumLimit = Fixed.ReverseEndianness(value.MinimumLimit),
            NoStretchValue = Fixed.ReverseEndianness(value.NoStretchValue),
            MaximumLimit = Fixed.ReverseEndianness(value.MaximumLimit),
        };
    }
}

/// <summary>A repeated add glyph action: adds a glyph after the current glyph as many times as needed to fill the gap.</summary>
/// <seealso cref="ActionSubrecord"/>
/// <seealso cref="UnconditionalAddGlyphSubrecord"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
public sealed record RepeatedAddGlyphSubrecord : ActionSubrecord, IDerivedRecord<ActionSubrecord, RepeatedAddGlyphSubrecord>
{
    /// <inheritdoc/>
    public override ushort ActionType => 5;

    /// <summary>Gets the flags word.</summary>
    /// <value>The raw <c>flags</c> field. The specification defines it as unused; the parser preserves the value for round-tripping and diagnostics.</value>
    public ushort Flags { get; init; }

    /// <summary>Gets the glyph to add.</summary>
    /// <value>The glyph index inserted after the current glyph as many times as needed to fill the growth gap.</value>
    public ushort Glyph { get; init; }

    static RepeatedAddGlyphSubrecord IDerivedRecord<ActionSubrecord, RepeatedAddGlyphSubrecord>.Parse(ref Cursor cursor, object? context)
    {
        ActionSubrecordHeader header = cursor.ReadEndianReversibleStruct<ActionSubrecordHeader>();
        Payload payload = cursor.ReadEndianReversibleStruct<Payload>();
        cursor.Position = header.ActionLength;
        return new()
        {
            ActionClass = header.ActionClass,
            Flags = payload.Flags,
            Glyph = payload.Glyph,
        };
    }

    /// <summary>The 4-byte payload of a repeated add glyph action: two 16-bit fields.</summary>
    /// <remarks>Both fields are <c>UInt16</c>, so the reversal is a single word-span pass over the struct reinterpreted as two 16-bit words.</remarks>
    /// <seealso cref="RepeatedAddGlyphSubrecord"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Payload : IEndianReversibleStruct<Payload>
    {
        /// <summary>The unused flags word at byte offset 0.</summary>
        public ushort Flags; // +0

        /// <summary>The glyph to add at byte offset 2.</summary>
        public ushort Glyph; // +2

        /// <summary>Reverses the byte order of both 16-bit fields in one pass.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new payload with both fields reversed.</returns>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static Payload ReverseEndianness(Payload value)
        {
            Span<ushort> words = MemoryMarshal.CreateSpan(ref value.Flags, 2);
            BinaryPrimitives.ReverseEndianness(words, words);
            return value;
        }
    }
}

/// <summary>The justification-category state table: walks glyphs in order and assigns each a justification category based on context.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Uses the classic AAT state-table format: a <see cref="MorphSubtableHeader"/>, a <see cref="StateHeader"/>, a class subtable introduced by <see cref="ClassTableHeader"/>, the state array, and the entry table.</description></item>
/// <item><description>Glyphs outside the class array's covered range receive <see cref="AatClass.OutOfBounds"/> rather than being rejected.</description></item>
/// <item><description>The parser preserves the class array, the state array, and the entry array as flat structures; it does not simulate the state machine, per the parse/diagnose split.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="JustTable"/>
/// <seealso cref="StateHeader"/>
/// <seealso cref="ClassTableHeader"/>
/// <seealso cref="AatClass"/>
/// <seealso cref="JustificationStateEntry"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
public sealed record JustificationCategoryStateTable : IRecord<JustificationCategoryStateTable>
{
    /// <summary>Gets the subtable coverage flags.</summary>
    /// <value>The <c>coverage</c> field from the <see cref="MorphSubtableHeader"/>. Only bit <c>0x4000</c> is interpreted for the justification category state table.</value>
    public ushort Coverage { get; init; }

    /// <summary>Gets the subtable feature flags.</summary>
    /// <value>The <c>subFeatureFlags</c> field from the <see cref="MorphSubtableHeader"/>. Ignored for justification tables; preserved for round-tripping and diagnostics.</value>
    public uint SubFeatureFlags { get; init; }

    /// <summary>Gets the first glyph ID that the class array covers.</summary>
    /// <value>The <c>firstGlyph</c> field from the <see cref="ClassTableHeader"/>. Glyphs below this value receive <see cref="AatClass.OutOfBounds"/>.</value>
    public ushort FirstGlyph { get; init; }

    /// <summary>Gets the number of classes in the state machine, equal to the number of bytes per state in the state array.</summary>
    /// <value>The <c>stateSize</c> field from the <see cref="StateHeader"/>.</value>
    public ushort StateSize { get; init; }

    /// <summary>Gets the class assigned to each glyph in the covered range.</summary>
    /// <value>One byte per covered glyph, indexed by <c>glyphId - <see cref="FirstGlyph"/></c>. Each value is a class in <c>[0, <see cref="StateSize"/>)</c>.</value>
    public required byte[] ClassArray { get; init; }

    /// <summary>Gets the flat state array.</summary>
    /// <value>One byte per <c>(state, class)</c> pair, laid out row-major: entry <c>state × StateSize + class</c> holds the index into <see cref="Entries"/> for that transition.</value>
    public required byte[] StateArray { get; init; }

    /// <summary>Gets the entry array.</summary>
    /// <value>Each entry holds the next state index and the action flags. The number of entries is determined by the subtable's declared length.</value>
    public required JustificationStateEntry[] Entries { get; init; }

    /// <summary>Gets the number of states in the state machine.</summary>
    /// <value>Derived as <see cref="StateArray"/>.Length divided by <see cref="StateSize"/>.</value>
    public int StateCount => StateArray.Length / StateSize;

    /// <summary>Classifies a glyph against the class array.</summary>
    /// <param name="glyphId">The glyph ID to classify.</param>
    /// <returns>The class in <c>[0, <see cref="StateSize"/>)</c>, or <see cref="AatClass.OutOfBounds"/> when the glyph lies outside the covered range.</returns>
    public int ClassifyGlyph(int glyphId)
    {
        int index = glyphId - FirstGlyph;
        return (uint)index < (uint)ClassArray.Length ? ClassArray[index] : AatClass.OutOfBounds;
    }

    /// <summary>Advances the state machine one step.</summary>
    /// <param name="stateIndex">The current state index in <c>[0, <see cref="StateCount"/>)</c>.</param>
    /// <param name="glyphClass">The class for the current glyph, obtained from <see cref="ClassifyGlyph(int)"/>.</param>
    /// <returns>The entry for the transition.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="stateIndex"/> or <paramref name="glyphClass"/> is out of range.</exception>
    public JustificationStateEntry Step(int stateIndex, int glyphClass)
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

        byte entryIndex = StateArray[stateIndex * StateSize + glyphClass];
        return Entries[entryIndex];
    }

    static JustificationCategoryStateTable IRecord<JustificationCategoryStateTable>.Parse(ref Cursor cursor, object? context)
    {
        MorphSubtableHeader morph = cursor.ReadEndianReversibleStruct<MorphSubtableHeader>();
        long stateHeaderStart = cursor.Position;
        StateHeader state = cursor.ReadEndianReversibleStruct<StateHeader>();

        cursor.Position = stateHeaderStart + state.ClassTable;
        ClassTableHeader classHeader = cursor.ReadEndianReversibleStruct<ClassTableHeader>();
        byte[] classArray = cursor.ReadUInt8Array(classHeader.GlyphCount);

        cursor.Position = stateHeaderStart + state.StateArray;
        byte[] stateArray = cursor.ReadUInt8Array(state.EntryTable - state.StateArray);

        cursor.Position = stateHeaderStart + state.EntryTable;
        int entryCount = checked((int)((morph.Length - cursor.Position) / 4));

        RawStateEntry[] rawEntries = cursor.ReadEndianReversibleStructArray<RawStateEntry>(entryCount);
        var entries = new JustificationStateEntry[entryCount];
        for (int i = 0; i < entryCount; i++)
        {
            entries[i] = new()
            {
                NewStateIndex = (rawEntries[i].NewStateOffset - state.StateArray) / state.StateSize,
                Flags = rawEntries[i].Flags,
            };
        }

        return new()
        {
            Coverage = morph.Coverage,
            SubFeatureFlags = morph.SubFeatureFlags,
            FirstGlyph = classHeader.FirstGlyph,
            StateSize = state.StateSize,
            ClassArray = classArray,
            StateArray = stateArray,
            Entries = entries,
        };
    }

    /// <summary>The 8-byte AAT metamorphosis subtable header that precedes a classic state header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Common to every AAT state-table subtable — <c>morx</c>, <c>kerx</c>, <c>mort</c>, and <c>just</c>. This type is a candidate for promotion to <c>AatShared.cs</c> once a second table needs it.</description></item>
    /// <item><description>The <c>length</c> spans the entire subtable, including this header, the state header, and every referenced array.</description></item>
    /// <item><description>Reversal is field-by-field because the first two fields are 16-bit and the third is 32-bit; the struct is not uniform-width.</description></item>
    /// <item><description>Public because a writer fills the struct in memory, reverses it, and writes the bytes by blittable cast.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="JustificationCategoryStateTable"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct MorphSubtableHeader : IEndianReversibleStruct<MorphSubtableHeader>
    {
        /// <summary>Total subtable length in bytes, including this header, at byte offset 0.</summary>
        /// <value>The byte length of the entire state-table subtable, from this header through the last entry.</value>
        public ushort Length;            // +0

        /// <summary>Coverage flags at byte offset 2.</summary>
        /// <value>The subtable's coverage word. Only bit <c>0x4000</c> is interpreted for this state table.</value>
        /// <seealso cref="JustificationCategoryStateTable.Coverage"/>
        public ushort Coverage;          // +2

        /// <summary>Sub-feature flags at byte offset 4.</summary>
        /// <value>The raw <c>subFeatureFlags</c> word. Ignored for justification tables; preserved for round-tripping and diagnostics.</value>
        /// <seealso cref="JustificationCategoryStateTable.SubFeatureFlags"/>
        public uint SubFeatureFlags;     // +4

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="MorphSubtableHeader"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>The first two fields are 16-bit and the third is 32-bit, so the reversal is field-by-field rather than a single word-span pass.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static MorphSubtableHeader ReverseEndianness(MorphSubtableHeader value) => new()
        {
            Length = BinaryPrimitives.ReverseEndianness(value.Length),
            Coverage = BinaryPrimitives.ReverseEndianness(value.Coverage),
            SubFeatureFlags = BinaryPrimitives.ReverseEndianness(value.SubFeatureFlags),
        };
    }

    /// <summary>The raw 4-byte state-table entry as stored on disk: a next-state byte offset and a flags word.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The <c>newStateOffset</c> is a byte offset relative to the start of the <see cref="StateHeader"/>, not a state index; the parser converts it to an index after reading all entries.</description></item>
    /// <item><description>Both fields are <c>UInt16</c>, so the reversal is a single word-span pass over the struct reinterpreted as two 16-bit words.</description></item>
    /// <item><description>Unlike other AAT state tables, the entry carries no glyph-offset array, so the on-disk width is exactly four bytes.</description></item>
    /// <item><description>Public because a writer fills the struct in memory, reverses it, and writes the bytes by blittable cast.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="JustificationCategoryStateTable"/>
    /// <seealso cref="JustificationStateEntry"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct RawStateEntry : IEndianReversibleStruct<RawStateEntry>
    {
        /// <summary>The next-state byte offset, relative to the start of the state header, at byte offset 0.</summary>
        /// <value>The byte offset of the next state's row within the state array.</value>
        public ushort NewStateOffset; // +0

        /// <summary>The raw action flags word at byte offset 2.</summary>
        /// <value>The 16-bit flags that encode the marked-glyph set and the category assignments.</value>
        /// <seealso cref="JustificationStateEntry.Flags"/>
        public ushort Flags;          // +2

        /// <summary>Reverses the byte order of both 16-bit fields in one pass.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new entry with both fields reversed.</returns>
        /// <remarks>The entry is entirely <c>UInt16</c> fields, so it is reinterpreted as a span of two 16-bit words and reversed with a single <see cref="BinaryPrimitives.ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})"/> call.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static RawStateEntry ReverseEndianness(RawStateEntry value)
        {
            Span<ushort> words = MemoryMarshal.CreateSpan(ref value.NewStateOffset, 2);
            BinaryPrimitives.ReverseEndianness(words, words);
            return value;
        }
    }
}

/// <summary>A single entry in the justification-category state table's entry array.</summary>
/// <seealso cref="JustificationCategoryStateTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6just.html">TrueType Reference Manual: The <c>just</c> table</seealso>
public readonly record struct JustificationStateEntry
{
    /// <summary>Gets the next state index.</summary>
    /// <value>The state to transition to after applying the flags. Derived at parse time from the on-disk next-state byte offset.</value>
    public int NewStateIndex { get; init; }

    /// <summary>Gets the raw flags word.</summary>
    /// <value>The 16-bit <c>action</c> field as stored.</value>
    public ushort Flags { get; init; }

    /// <summary>Gets a value indicating whether the current glyph becomes the marked glyph.</summary>
    /// <value><see langword="true"/> when bit <c>0x8000</c> is set.</value>
    public bool SetMark => (Flags & 0x8000) != 0;

    /// <summary>Gets a value indicating whether the state machine should stay on the current glyph.</summary>
    /// <value><see langword="true"/> when bit <c>0x4000</c> is set.</value>
    public bool DontAdvance => (Flags & 0x4000) != 0;

    /// <summary>Gets the justification category to assign to the marked glyph.</summary>
    /// <value>The value of bits <c>0x3F80</c>, shifted down to <c>[0, 127]</c>. Zero means no assignment.</value>
    public byte MarkCategory => (byte)((Flags >> 7) & 0x7F);

    /// <summary>Gets the justification category to assign to the current glyph.</summary>
    /// <value>The low seven bits of <see cref="Flags"/>. Zero means no assignment.</value>
    public byte CurrentCategory => (byte)(Flags & 0x7F);
}
