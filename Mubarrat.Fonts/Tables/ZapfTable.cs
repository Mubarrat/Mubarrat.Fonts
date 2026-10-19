using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Sfnt;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════
// The 'Zapf' table — glyph information
//
// Associates each glyph with the Unicode text it represents, a set of named
// glyph identifiers, an optional FeatureInfo (the layout context and AAT/OT
// features that select it), and an optional GlyphGroup / GlyphGroupOffsetArray
// (the sets of glyphs the designer considers related).
//
// Named with permission after Hermann Zapf. Authoring artifact — not consumed
// by layout engines, but useful for diagnostic tools, Character Palette-style
// glyph pickers, and text-recovery from glyph streams.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>Zapf</c> table: per-glyph Unicode text, named identifiers, AAT/OT feature selections, and designer-curated glyph groupings.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><b>Version-conditional lookup.</b> Version 1 stores a dense <c>UInt32</c> array of offsets sized by the font's glyph count; version 2 stores a standard <see cref="LookupTable"/> whose values are the same offsets. The semantic accessor <see cref="TryGetGlyphInfo(int, out ZapfGlyphInfo)"/> abstracts both.</description></item>
/// <item><description><b>Two offset bases.</b> GlyphInfo offsets are table-relative (from the table start); group and feature offsets are extraInfo-relative (from <see cref="ZapfTable.Header.ExtraInfo"/>). The two bases never mix — every offset carries its own base, and the parser resolves each against the correct one.</description></item>
/// <item><description><b>Shared targets.</b> Many glyphs may share one GlyphInfo, one FeatureInfo, or one GlyphGroup. The parser reads each distinct offset once and exposes the results keyed by that offset; use <see cref="GetGroup"/>/<see cref="GetFeatures"/> to resolve a glyph's targets.</description></item>
/// <item><description><b>Canonical glyph.</b> The <see cref="ZapfGlyphInfo.IsCanonical"/> flag (<c>0x80</c> in the GlyphInfo flags byte) marks the preferred glyph for a Unicode string. It can enumerate the covered code points in a single pass over the table — much faster than probing every Unicode code point against <c>cmap</c>.</description></item>
/// <item><description>All multi-byte reads use the sfnt default of big-endian.</description></item>
/// <item><description>Apple warns that <c>Zapf</c> is an authoring artifact and should be removed from shipping fonts.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ZapfGlyphInfo"/>
/// <seealso cref="ZapfGlyphIdentifier"/>
/// <seealso cref="ZapfFeatureInfo"/>
/// <seealso cref="ZapfGroupData"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6Zapf.html">TrueType Reference Manual: The <c>Zapf</c> table</seealso>
public sealed record ZapfTable : IFontTable<ZapfTable>, IRecord<ZapfTable>
{
    /// <summary>The lowest version defined by the specification.</summary>
    public const ushort MinVersion = 1;

    /// <summary>The highest version defined by the specification.</summary>
    public const ushort MaxVersion = 2;

    /// <summary>The sentinel value for an absent offset. Used by the header, by <see cref="ZapfGlyphInfo"/>, and by <see cref="ZapfGlyphGroupOffsetArray"/>.</summary>
    public const uint NoOffset = 0xFFFFFFFFu;

    /// <inheritdoc/>
    public static Tag Tag => "Zapf";

    /// <summary>Gets the table version.</summary>
    /// <value>1 or 2. Version 1 stores a dense offset array; version 2 stores a <see cref="LookupTable"/>.</value>
    public ushort Version { get; init; }

    /// <summary>Gets the version-2 lookup table, or <see langword="null"/> for a version-1 table.</summary>
    /// <value>A <see cref="LookupTable"/> whose value for a covered glyph is a table-relative offset to that glyph's <see cref="ZapfGlyphInfo"/>; <see langword="null"/> when <see cref="Version"/> is 1.</value>
    /// <seealso cref="Version1Offsets"/>
    /// <seealso cref="TryGetGlyphInfo(int, out ZapfGlyphInfo)"/>
    public LookupTable? Lookup { get; init; }

    /// <summary>Gets the version-1 dense offset array, or an empty memory for a version-2 table.</summary>
    /// <value>One <c>UInt32</c> per glyph, in glyph order; each value is a table-relative offset to that glyph's <see cref="ZapfGlyphInfo"/>, or <c>0</c> when the glyph has no entry. Empty when <see cref="Version"/> is 2.</value>
    /// <seealso cref="Lookup"/>
    /// <seealso cref="TryGetGlyphInfo(int, out ZapfGlyphInfo)"/>
    public ReadOnlyMemory<uint> Version1Offsets { get; init; }

    /// <summary>Gets the glyph-info records, keyed by their table-relative offset.</summary>
    /// <value>One entry per distinct offset referenced by the lookup or by the version-1 array. Multiple glyphs may share one <see cref="ZapfGlyphInfo"/>; the dictionary has one entry per shared target.</value>
    /// <seealso cref="TryGetGlyphInfo(int, out ZapfGlyphInfo)"/>
    public required IReadOnlyDictionary<uint, ZapfGlyphInfo> GlyphInfoByOffset { get; init; }

    /// <summary>Gets the feature-info records, keyed by their extraInfo-relative offset.</summary>
    /// <value>One entry per distinct non-sentinel <see cref="ZapfGlyphInfo.FeatureOffset"/>. Multiple glyphs may share one <see cref="ZapfFeatureInfo"/>.</value>
    /// <seealso cref="GetFeatures"/>
    public required IReadOnlyDictionary<uint, ZapfFeatureInfo> FeaturesByOffset { get; init; }

    /// <summary>Gets the group-data records, keyed by their extraInfo-relative offset.</summary>
    /// <value>One entry per distinct non-sentinel <see cref="ZapfGlyphInfo.GroupOffset"/>, plus one per <see cref="ZapfGlyphGroupOffsetArray"/>-referenced child group. A glyph's group offset may resolve to either a <see cref="ZapfGlyphGroup"/> or a <see cref="ZapfGlyphGroupOffsetArray"/>.</value>
    /// <seealso cref="GetGroup"/>
    /// <seealso cref="GetReferencedGroup"/>
    public required IReadOnlyDictionary<uint, ZapfGroupData> GroupsByOffset { get; init; }

    /// <summary>Tries to get the glyph info for a glyph.</summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <param name="info">When this method returns <see langword="true"/>, contains the glyph's info; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the glyph has an info record; otherwise, <see langword="false"/>.</returns>
    public bool TryGetGlyphInfo(int glyphId, [NotNullWhen(true)] out ZapfGlyphInfo? info)
    {
        uint offset = 0;
        if (Version >= 2)
        {
            if (Lookup is null || !Lookup.TryGetValue(glyphId, out offset) || offset == 0)
            {
                info = null;
                return false;
            }
        }
        else
        {
            ReadOnlySpan<uint> offsets = Version1Offsets.Span;
            if ((uint)glyphId >= (uint)offsets.Length || (offset = offsets[glyphId]) == 0)
            {
                info = null;
                return false;
            }
        }

        return GlyphInfoByOffset.TryGetValue(offset, out info);
    }

    /// <summary>Gets the group data referenced by an info record's <see cref="ZapfGlyphInfo.GroupOffset"/>.</summary>
    /// <param name="info">The info record.</param>
    /// <returns>The referenced <see cref="ZapfGroupData"/>, or <see langword="null"/> when the info's group offset is <see cref="NoOffset"/> or does not resolve.</returns>
    /// <seealso cref="GetReferencedGroup"/>
    public ZapfGroupData? GetGroup(ZapfGlyphInfo info) =>
        info.GroupOffset == NoOffset ? null
        : GroupsByOffset.TryGetValue(info.GroupOffset, out var data) ? data
        : null;

    /// <summary>Gets the group referenced by a <see cref="ZapfGlyphGroupOffsetArray"/> entry.</summary>
    /// <param name="offsetArray">The offset array.</param>
    /// <param name="index">The index in <see cref="ZapfGlyphGroupOffsetArray.GroupOffsets"/>.</param>
    /// <returns>The referenced <see cref="ZapfGlyphGroup"/>, or <see langword="null"/> when the entry is <see cref="NoOffset"/> or does not resolve.</returns>
    /// <seealso cref="GetGroup"/>
    public ZapfGlyphGroup? GetReferencedGroup(ZapfGlyphGroupOffsetArray offsetArray, int index)
    {
        ReadOnlySpan<uint> offsets = offsetArray.GroupOffsets.Span;
        if ((uint)index >= (uint)offsets.Length) return null;
        uint offset = offsets[index];
        if (offset == NoOffset) return null;
        return GroupsByOffset.TryGetValue(offset, out var data) && data is ZapfGlyphGroup group
            ? group
            : null;
    }

    /// <summary>Gets the feature info referenced by an info record's <see cref="ZapfGlyphInfo.FeatureOffset"/>.</summary>
    /// <param name="info">The info record.</param>
    /// <returns>The referenced <see cref="ZapfFeatureInfo"/>, or <see langword="null"/> when the info's feature offset is <see cref="NoOffset"/> or does not resolve.</returns>
    public ZapfFeatureInfo? GetFeatures(ZapfGlyphInfo info) =>
        info.FeatureOffset == NoOffset ? null
        : FeaturesByOffset.TryGetValue(info.FeatureOffset, out var features) ? features
        : null;

    static ZapfTable IRecord<ZapfTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version < MinVersion || header.Version > MaxVersion)
            throw new InvalidDataException(
                $"'Zapf'.version is {header.Version}, expected {MinVersion} or {MaxVersion}.");

        int numGlyphs = context is SfntFontFace face
            ? face.GetTable<MaxpTable>().NumGlyphs
            : throw new InvalidDataException(
                "'Zapf' requires an SFNT font face context to obtain the glyph count.");

        // Version-conditional lookup. Both forms ultimately yield a table-relative
        // offset per glyph; the semantic accessor abstracts the difference.
        LookupTable? lookup = null;
        ReadOnlyMemory<uint> v1Offsets = default;
        cursor.Position = 8;
        if (header.Version >= 2)
            lookup = cursor.ReadRecord<LookupTable>(new LookupTable.Context(numGlyphs));
        else
            v1Offsets = cursor.ReadUInt32Array(numGlyphs);

        // Collect the distinct glyph-info offsets, skipping the sentinel and the
        // (never-valid-as-a-GlyphInfo) offset zero that points at the header itself.
        var glyphInfoOffsets = new HashSet<uint>();
        for (int glyph = 0; glyph < numGlyphs; glyph++)
        {
            uint offset;
            if (header.Version >= 2)
            {
                if (!lookup!.TryGetValue(glyph, out offset)) continue;
            }
            else
                offset = v1Offsets.Span[glyph];
            if (offset != 0) glyphInfoOffsets.Add(offset);
        }

        // Parse each distinct GlyphInfo; collect the distinct group and feature
        // offsets it references. Group and feature offsets share the extraInfo
        // base, so both are stored in extraInfo-relative form and resolved
        // against header.ExtraInfo when their targets are read.
        var glyphInfos = new Dictionary<uint, ZapfGlyphInfo>(glyphInfoOffsets.Count);
        var groupOffsets = new HashSet<uint>();
        var featureOffsets = new HashSet<uint>();
        foreach (uint offset in glyphInfoOffsets)
        {
            cursor.Position = offset;
            ZapfGlyphInfo info = cursor.ReadRecord<ZapfGlyphInfo>(null);
            glyphInfos.Add(offset, info);
            if (info.GroupOffset != NoOffset) groupOffsets.Add(info.GroupOffset);
            if (info.FeatureOffset != NoOffset) featureOffsets.Add(info.FeatureOffset);
        }

        // Feature infos are self-contained.
        var features = new Dictionary<uint, ZapfFeatureInfo>(featureOffsets.Count);
        foreach (uint offset in featureOffsets)
        {
            cursor.Position = checked(header.ExtraInfo + (long)offset);
            features.Add(offset, cursor.ReadRecord<ZapfFeatureInfo>(null));
        }

        // Group data. A GlyphGroupOffsetArray references GlyphGroups by further
        // extraInfo-relative offsets; use an iterative worklist so a shared child
        // group is parsed exactly once even when many parents reference it.
        var groups = new Dictionary<uint, ZapfGroupData>(groupOffsets.Count);
        var pending = new Queue<uint>(groupOffsets);
        while (pending.Count > 0)
        {
            uint offset = pending.Dequeue();
            if (groups.ContainsKey(offset)) continue;

            cursor.Position = checked(header.ExtraInfo + (long)offset);
            ZapfGroupData data = cursor.ReadRecord<ZapfGroupData>();
            groups.Add(offset, data);

            if (data is ZapfGlyphGroupOffsetArray array)
            {
                foreach (uint child in array.GroupOffsets.Span)
                {
                    if (child != NoOffset && !groups.ContainsKey(child))
                        pending.Enqueue(child);
                }
            }
        }

        return new()
        {
            Version = header.Version,
            Lookup = lookup,
            Version1Offsets = v1Offsets,
            GlyphInfoByOffset = glyphInfos,
            FeaturesByOffset = features,
            GroupsByOffset = groups,
        };
    }

    /// <summary>The 8-byte <c>Zapf</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The version and unused fields are <c>UInt16</c>; the extra-info offset is <c>UInt32</c>, so the reversal is field-by-field.</description></item>
    /// <item><description>The lookup table (version 2) or the dense offset array (version 1) begins immediately after this header.</description></item>
    /// <item><description>The <c>&lt;Clone&gt;$()</c> method generated by the <c>record struct</c> declaration must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0. 1 or 2.</summary>
        public ushort Version;     // +0

        /// <summary>Unused at byte offset 2. Must be zero.</summary>
        public ushort Unused;      // +2

        /// <summary>Byte offset from the table start to the extra-info space, at byte offset 4.</summary>
        /// <value>All group and feature offsets in <see cref="ZapfGlyphInfo"/> are relative to this position, not to the table start.</value>
        public uint ExtraInfo;     // +4

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(value.Version),
            Unused = BinaryPrimitives.ReverseEndianness(value.Unused),
            ExtraInfo = BinaryPrimitives.ReverseEndianness(value.ExtraInfo),
        };
    }
}

/// <summary>The per-glyph information record: the Unicode text the glyph represents, the canonical-glyph flag, the named glyph identifiers, and the offsets to the glyph's optional group and feature data.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><b>Variable-length record.</b> The fixed prefix is 10 bytes (two <c>UInt32</c>, two <c>UInt8</c>); the body is the UTF-16 array, a <c>UInt16</c> identifier count, and one variable-length <see cref="ZapfGlyphIdentifier"/> per identifier. The record's own start comes from the lookup, so the parser never has to compute the record's size — but a writer must pad to the next 32-bit boundary.</description></item>
/// <item><description><b>Group and feature offsets are extraInfo-relative.</b> They are stored raw here, and <see cref="ZapfTable.GetGroup"/> / <see cref="ZapfTable.GetFeatures"/> resolve them against the table's <see cref="ZapfTable.Header.ExtraInfo"/>.</description></item>
/// <item><description><b>Zero Unicodes.</b> A glyph with zero Unicode values (for example glyph 0) does not represent text; <see cref="UnicodeString"/> is empty and <see cref="IsCanonical"/> is meaningless in the absence of a text string.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ZapfTable"/>
/// <seealso cref="ZapfGlyphIdentifier"/>
public sealed record ZapfGlyphInfo : IRecord<ZapfGlyphInfo>
{
    /// <summary>Gets the extraInfo-relative offset to the glyph's group data, or <see cref="ZapfTable.NoOffset"/>.</summary>
    /// <value>The offset is relative to <see cref="ZapfTable.Header.ExtraInfo"/>, not to the table start. Use <see cref="ZapfTable.GetGroup"/> to resolve it.</value>
    public uint GroupOffset { get; init; }

    /// <summary>Gets the extraInfo-relative offset to the glyph's feature data, or <see cref="ZapfTable.NoOffset"/>.</summary>
    /// <value>The offset is relative to <see cref="ZapfTable.Header.ExtraInfo"/>, not to the table start. Use <see cref="ZapfTable.GetFeatures"/> to resolve it.</value>
    public uint FeatureOffset { get; init; }

    /// <summary>Gets the raw GlyphInfo flags byte.</summary>
    /// <value>The flags byte. The only bit currently defined is <c>0x80</c>, surfaced through <see cref="IsCanonical"/>.</value>
    public byte Flags { get; init; }

    /// <summary>Gets the raw UTF-16 Unicode values the glyph represents.</summary>
    /// <value>Zero or more UTF-16 code units. For a ligature, the values are the decomposed sequence of base characters (e.g. <c>f</c>, <c>i</c> for the <c>fi</c> ligature), not the precomposed compatibility code point (e.g. <c>U+FB01</c>).</value>
    public required ReadOnlyMemory<ushort> Unicodes { get; init; }

    /// <summary>Gets the UTF-16 string the glyph represents.</summary>
    /// <value>Decoded from <see cref="Unicodes"/>; empty when the glyph represents no text.</value>
    public required string UnicodeString { get; init; }

    /// <summary>Gets the named identifiers for this glyph, in the order they appear on disk.</summary>
    /// <value>Zero or more <see cref="ZapfGlyphIdentifier"/> records. Identifiers are not padded individually; only the enclosing GlyphInfo is 32-bit aligned.</value>
    public required IReadOnlyList<ZapfGlyphIdentifier> Identifiers { get; init; }

    /// <summary>Gets a value indicating whether this glyph is the canonical glyph for its Unicode string.</summary>
    /// <value><see langword="true"/> when bit <c>0x80</c> of <see cref="Flags"/> is set.</value>
    /// <remarks>For a single-code-point glyph, the flag should be set if and only if the <c>cmap</c> maps that code point to this glyph. For a multi-code-point ligature, exactly one glyph per Unicode string should carry the flag.</remarks>
    public bool IsCanonical => (Flags & 0x80) != 0;

    static ZapfGlyphInfo IRecord<ZapfGlyphInfo>.Parse(ref Cursor cursor, object? context)
    {
        uint groupOffset = cursor.ReadUInt32();
        uint featureOffset = cursor.ReadUInt32();
        byte flags = cursor.ReadUInt8();
        ushort[] unicodes = cursor.ReadUInt16Array(cursor.ReadUInt8());
        return new()
        {
            GroupOffset = groupOffset,
            FeatureOffset = featureOffset,
            Flags = flags,
            Unicodes = unicodes,
            UnicodeString = unicodes.Length == 0 ? string.Empty : Encoding.Unicode.GetString(MemoryMarshal.AsBytes(unicodes.AsSpan())),
            Identifiers = Array.AsReadOnly(cursor.ReadRecordArray<ZapfGlyphIdentifier>(cursor.ReadUInt16())),
        };
    }
}

/// <summary>A single named identifier for a glyph: a UTF-8 Pascal string, a two-byte binary value, or a two-byte flags word.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><b>Kind determines encoding.</b> <c>0–63</c>: Pascal string (a length byte followed by that many UTF-8 bytes). <c>64–127</c>: two-byte big-endian value. <c>128–255</c>: reserved (the parser rejects these).</description></item>
/// <item><description><b>Not padded.</b> Identifiers are tightly packed within the enclosing GlyphInfo. Only the GlyphInfo as a whole is 32-bit aligned.</description></item>
/// <item><description><b>Kind 127 is legacy.</b> In version 1 of <c>Zapf</c>, a kind-127 identifier with value <c>0x8000</c> marked the canonical glyph. In version 2, use <see cref="ZapfGlyphInfo.IsCanonical"/> instead.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ZapfGlyphInfo"/>
public sealed record ZapfGlyphIdentifier : IRecord<ZapfGlyphIdentifier>
{
    /// <summary>Gets the raw kind byte.</summary>
    /// <value>
    /// <list type="bullet">
    /// <item><description><c>0–63</c>: a Pascal string whose kind identifies the naming convention (see <see cref="ZapfGlyphIdentifierKind"/> for the defined values).</description></item>
    /// <item><description><c>64–127</c>: a two-byte big-endian value. <c>64–67</c> are CIDs; <c>68–72</c> are <c>name</c> table indices; <c>127</c> is a legacy flags word.</description></item>
    /// <item><description><c>128–255</c>: reserved; the parser rejects these.</description></item>
    /// </list>
    /// </value>
    public byte Kind { get; init; }

    /// <summary>Gets the identifier's string value, or <see langword="null"/> when the kind is in the binary range.</summary>
    /// <value>The UTF-8 Pascal string when <see cref="Kind"/> is <c>0–63</c>; otherwise <see langword="null"/>.</value>
    public string? Name { get; init; }

    /// <summary>Gets the identifier's two-byte binary value, or <c>0</c> when the kind is in the string range.</summary>
    /// <value>The two-byte big-endian value when <see cref="Kind"/> is <c>64–127</c>; otherwise <c>0</c>.</value>
    public ushort Value { get; init; }

    /// <summary>Gets a value indicating whether this identifier carries a string.</summary>
    public bool IsName => Kind < 64;

    /// <summary>Gets a value indicating whether this identifier carries a two-byte binary value.</summary>
    public bool IsBinaryValue => Kind >= 64 && Kind < 128;

    static ZapfGlyphIdentifier IRecord<ZapfGlyphIdentifier>.Parse(ref Cursor cursor, object? context)
    {
        byte kind = cursor.ReadUInt8();
        if (kind < 64)
            return new() { Kind = kind, Name = Encoding.UTF8.GetString(cursor.ReadUInt8Array(cursor.ReadUInt8())) };
        if (kind < 128)
            return new() { Kind = kind, Value = cursor.ReadUInt16() };
        throw new InvalidDataException($"'Zapf' GlyphIdentifier kind {kind} is reserved (128–255).");
    }
}

/// <summary>The defined values for <see cref="ZapfGlyphIdentifier.Kind"/>. The parser accepts the full <c>0–127</c> range; values not listed here are preserved as their raw byte.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><c>0–4</c> are UTF-8 Pascal strings; <c>5–63</c> are reserved by the specification's range convention.</description></item>
/// <item><description><c>64–72</c> are two-byte binary values; <c>73–126</c> are reserved by the range convention; <c>127</c> is a legacy flags word.</description></item>
/// <item><description><c>128–255</c> are reserved and rejected by <see cref="ZapfGlyphIdentifier"/>'s parser.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ZapfGlyphIdentifier.Kind"/>
public enum ZapfGlyphIdentifierKind : byte
{
    /// <summary>A PostScript name that complies with all naming conventions.</summary>
    UniversalPostScriptName = 0,

    /// <summary>An Apple PostScript name.</summary>
    ApplePostScriptName = 1,

    /// <summary>An Adobe Glyph List (AGL) PostScript name.</summary>
    AdobeAglPostScriptName = 2,

    /// <summary>An AFII PostScript name.</summary>
    AfiiPostScriptName = 3,

    /// <summary>A character name using Unicode character-naming conventions.</summary>
    UnicodeCharacterName = 4,

    /// <summary>A Japanese CID.</summary>
    JapaneseCid = 64,

    /// <summary>A Traditional Chinese CID.</summary>
    TraditionalChineseCid = 65,

    /// <summary>A Simplified Chinese CID.</summary>
    SimplifiedChineseCid = 66,

    /// <summary>A Korean CID.</summary>
    KoreanCid = 67,

    /// <summary>A <c>name</c> table index for a version-history note.</summary>
    VersionHistoryNote = 68,

    /// <summary>A <c>name</c> table index for the designer's short name.</summary>
    DesignerShortName = 69,

    /// <summary>A <c>name</c> table index for the designer's long name.</summary>
    DesignerLongName = 70,

    /// <summary>A <c>name</c> table index for the designer's usage notes.</summary>
    DesignerUsageNotes = 71,

    /// <summary>A <c>name</c> table index for the designer's historical notes.</summary>
    DesignerHistoricalNotes = 72,

    /// <summary>A two-byte flags word. In version 1, value <c>0x8000</c> marked the canonical glyph; in version 2, use <see cref="ZapfGlyphInfo.IsCanonical"/>.</summary>
    Flags = 127,
}

/// <summary>The layout-engine inputs that force a glyph's appearance, plus the AAT and OpenType feature selections that produce it.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><b>Context.</b> A bitfield identifying the line-, word-, or auto-fraction position where the glyph is expected. Multiple bits may be set; a value of zero means context is irrelevant.</description></item>
/// <item><description><b>AAT features.</b> One or more <c>(type, selector)</c> pairs (the same pair the <c>morx</c> feature table uses) that must be active to produce the glyph.</description></item>
/// <item><description><b>OpenType tags.</b> Zero or more four-character feature tags that select the glyph in an OpenType shaping model.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ZapfTable"/>
/// <seealso cref="ZapfFeatureContext"/>
/// <seealso cref="ZapfAatFeature"/>
public sealed record ZapfFeatureInfo : IRecord<ZapfFeatureInfo>
{
    /// <summary>Gets the context bitfield.</summary>
    /// <value>A combination of <see cref="ZapfFeatureContext"/> bits. Zero means context is irrelevant.</value>
    public ZapfFeatureContext Context { get; init; }

    /// <summary>Gets the AAT feature <c>(type, selector)</c> pairs that select this glyph.</summary>
    /// <value>One or more pairs, in the order they appear on disk.</value>
    public required ReadOnlyMemory<ZapfAatFeature> AatFeatures { get; init; }

    /// <summary>Gets the OpenType feature tags that select this glyph.</summary>
    /// <value>Zero or more tags, in the order they appear on disk.</value>
    public required ReadOnlyMemory<Tag> OpenTypeTags { get; init; }

    static ZapfFeatureInfo IRecord<ZapfFeatureInfo>.Parse(ref Cursor cursor, object? context) => new()
    {
        Context = (ZapfFeatureContext)cursor.ReadUInt16(),
        AatFeatures = cursor.ReadEndianReversibleStructArray<ZapfAatFeature>(cursor.ReadUInt16()),
        OpenTypeTags = cursor.ReadEndianReversibleStructArray<Tag>(checked((int)cursor.ReadUInt32())),
    };
}

/// <summary>The bitfield stored in <see cref="ZapfFeatureInfo.Context"/>.</summary>
/// <remarks>Multiple bits may be set; a zero value means context is irrelevant.</remarks>
[Flags]
public enum ZapfFeatureContext : ushort
{
    /// <summary>Context is irrelevant.</summary>
    None = 0,

    /// <summary>The glyph appears at the start of a line.</summary>
    LineInitial = 0x0001,

    /// <summary>The glyph appears in the medial portion of a line.</summary>
    LineMedial = 0x0002,

    /// <summary>The glyph appears at the end of a line.</summary>
    LineFinal = 0x0004,

    /// <summary>The glyph appears at the start of a word.</summary>
    WordInitial = 0x0008,

    /// <summary>The glyph appears in the medial portion of a word.</summary>
    WordMedial = 0x0010,

    /// <summary>The glyph appears at the end of a word.</summary>
    WordFinal = 0x0020,

    /// <summary>The glyph appears as an auto-fraction numerator.</summary>
    AutoFractionNumerator = 0x0040,

    /// <summary>The glyph appears as an auto-fraction denominator.</summary>
    AutoFractionDenominator = 0x0080,
}

/// <summary>A single AAT <c>(type, selector)</c> pair: the same pair format used by <c>morx</c>'s feature table.</summary>
/// <remarks>Both fields are <c>UInt16</c>, so the reversal is a single word-span pass over two 16-bit words.</remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct ZapfAatFeature : IEndianReversibleStruct<ZapfAatFeature>
{
    /// <summary>The AAT feature type at byte offset 0.</summary>
    public ushort Type;      // +0

    /// <summary>The AAT feature selector at byte offset 2.</summary>
    public ushort Selector;  // +2

    /// <summary>Reverses the byte order of both 16-bit fields in one pass.</summary>
    /// <param name="value">The value whose fields are to be reversed.</param>
    /// <returns>A new pair with both fields reversed.</returns>
    public static ZapfAatFeature ReverseEndianness(ZapfAatFeature value)
    {
        Span<ushort> words = MemoryMarshal.CreateSpan(ref value.Type, 2);
        BinaryPrimitives.ReverseEndianness(words, words);
        return value;
    }
}

/// <summary>Base for the two forms a glyph's <see cref="ZapfGlyphInfo.GroupOffset"/> can reference: a single <see cref="ZapfGlyphGroup"/>, or a <see cref="ZapfGlyphGroupOffsetArray"/> that names several groups.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The two forms share a two-byte <c>numGroups</c> prefix; bit 14 of the first word discriminates. Clear means <see cref="ZapfGlyphGroup"/>; set means <see cref="ZapfGlyphGroupOffsetArray"/>.</description></item>
/// <item><description>Dispatch is a peek at the first <c>UInt16</c>, mirroring <see cref="MortSubtable"/>, <see cref="KerxSubtable"/>, and <see cref="HvglPart"/>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ZapfGlyphGroup"/>
/// <seealso cref="ZapfGlyphGroupOffsetArray"/>
public abstract record ZapfGroupData : IRecord<ZapfGroupData>, IBaseRecord<ZapfGroupData>
{
    static ZapfGroupData IRecord<ZapfGroupData>.Parse(ref Cursor cursor, object? context) => (cursor.PeekUInt16() & 0x4000) != 0
        ? IBaseRecord<ZapfGroupData>.Parse<ZapfGlyphGroupOffsetArray>(ref cursor, context)
        : IBaseRecord<ZapfGroupData>.Parse<ZapfGlyphGroup>(ref cursor, context);
}

/// <summary>A named collection of one or more <see cref="ZapfGlyphSubgroup"/> records; the "single group" form of <see cref="ZapfGroupData"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <c>numGroups</c> word's low 14 bits give the subgroup count; bit 15 signals that every subgroup is preceded by a two-byte flag word; bit 14 must be zero (set-bit 14 would dispatch to <see cref="ZapfGlyphGroupOffsetArray"/>).</description></item>
/// <item><description>Subgroup flag bits: <c>0x8000</c> (<see cref="ZapfGlyphSubgroup.IsAligned"/>) pads the subgroup to a 32-bit boundary; <c>0x4000</c> (<see cref="ZapfGlyphSubgroup.IsSubdivided"/>) marks a subdivision of a larger group.</description></item>
/// <item><description>The first subgroup's <see cref="ZapfGlyphSubgroup.Glyphs"/> may be empty; when it is, its <see cref="ZapfGlyphSubgroup.NameIndex"/> names the whole group.</description></item>
/// </list>
/// </remarks>
public sealed record ZapfGlyphGroup : ZapfGroupData, IDerivedRecord<ZapfGroupData, ZapfGlyphGroup>
{
    /// <summary>Gets the subgroups, in on-disk order.</summary>
    /// <value>One <see cref="ZapfGlyphSubgroup"/> per named sub-collection. The first subgroup may carry the group's overall name with an empty glyph list.</value>
    public required IReadOnlyList<ZapfGlyphSubgroup> Subgroups { get; init; }

    static ZapfGlyphGroup IDerivedRecord<ZapfGroupData, ZapfGlyphGroup>.Parse(ref Cursor cursor, object? context)
    {
        ushort numGroupsRaw = cursor.ReadUInt16();
        if ((numGroupsRaw & 0x4000) != 0)
            throw new InvalidDataException($"'Zapf' GlyphGroup numGroups 0x{numGroupsRaw:X4} has bit 14 set; that form is GlyphGroupOffsetArray.");

        bool hasFlags = (numGroupsRaw & 0x8000) != 0;
        int count = numGroupsRaw & 0x3FFF;
        var subgroups = new ZapfGlyphSubgroup[count];
        for (int i = 0; i < count; i++)
        {
            bool isAligned = false, isSubdivided = false;
            if (hasFlags)
            {
                ushort flag = cursor.ReadUInt16();
                isAligned = (flag & 0x8000) != 0;
                isSubdivided = (flag & 0x4000) != 0;
            }

            subgroups[i] = cursor.ReadRecord<ZapfGlyphSubgroup>(new ZapfGlyphSubgroup.Context(isAligned, isSubdivided));

            if (isAligned)
                cursor.Position = (cursor.Position + 3) & ~3L;
        }

        return new() { Subgroups = Array.AsReadOnly(subgroups) };
    }
}

/// <summary>A named set of glyph indices within a <see cref="ZapfGlyphGroup"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <see cref="NameIndex"/> is a <c>name</c> table index; zero means "unnamed". When <see cref="Glyphs"/> is empty, this subgroup's name applies to the entire enclosing group.</description></item>
/// <item><description><see cref="IsAligned"/> and <see cref="IsSubdivided"/> are read by the enclosing <see cref="ZapfGlyphGroup"/> from the optional flag word before the subgroup; they are not part of the subgroup's own bytes.</description></item>
/// </list>
/// </remarks>
public sealed record ZapfGlyphSubgroup : IRecord<ZapfGlyphSubgroup>
{
    /// <summary>Gets the <c>name</c> table index for the subgroup's label.</summary>
    /// <value>A <c>name</c> table index, or zero when the subgroup has no name.</value>
    public ushort NameIndex { get; init; }

    /// <summary>Gets a value indicating whether the subgroup was padded to a 32-bit boundary.</summary>
    /// <value><see langword="true"/> when the flag word's bit <c>0x8000</c> was set.</value>
    public bool IsAligned { get; init; }

    /// <summary>Gets a value indicating whether the subgroup is a subdivision of a larger group.</summary>
    /// <value><see langword="true"/> when the flag word's bit <c>0x4000</c> was set.</value>
    public bool IsSubdivided { get; init; }

    /// <summary>Gets the glyph indices in this subgroup.</summary>
    /// <value>Zero or more glyph indices. Empty when this subgroup names the enclosing group without contributing glyphs of its own.</value>
    public required ReadOnlyMemory<ushort> Glyphs { get; init; }

    static ZapfGlyphSubgroup IRecord<ZapfGlyphSubgroup>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not Context ctx)
            throw new InvalidDataException("'Zapf' GlyphSubgroup requires a Context carrying the flag bits.");
        return new()
        {
            NameIndex = cursor.ReadUInt16(),
            IsAligned = ctx.IsAligned,
            IsSubdivided = ctx.IsSubdivided,
            Glyphs = cursor.ReadUInt16Array(cursor.ReadUInt16()),
        };
    }

    /// <summary>Context for parsing a <see cref="ZapfGlyphSubgroup"/>: the flag bits read by the enclosing <see cref="ZapfGlyphGroup"/>.</summary>
    /// <param name="IsAligned">Whether the subgroup's flag word had bit <c>0x8000</c> set.</param>
    /// <param name="IsSubdivided">Whether the subgroup's flag word had bit <c>0x4000</c> set.</param>
    public record Context(bool IsAligned, bool IsSubdivided);
}

/// <summary>An array of offsets to <see cref="ZapfGlyphGroup"/> records; the "multiple groups" form of <see cref="ZapfGroupData"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <c>numGroups</c> word's low 14 bits give the count; bit 15 must be zero and bit 14 must be one.</description></item>
/// <item><description>The first offset names the "alternate forms" group — the set of glyphs the operating system presents as alternates for the glyph in the Character Palette. <see cref="ZapfTable.NoOffset"/> in that slot means "no alternate forms".</description></item>
/// <item><description>Each offset in <see cref="GroupOffsets"/> is extraInfo-relative; resolve via <see cref="ZapfTable.GetReferencedGroup"/>.</description></item>
/// </list>
/// </remarks>
public sealed record ZapfGlyphGroupOffsetArray : ZapfGroupData, IDerivedRecord<ZapfGroupData, ZapfGlyphGroupOffsetArray>
{
    /// <summary>Gets the extraInfo-relative offsets to the referenced <see cref="ZapfGlyphGroup"/> records.</summary>
    /// <value>One offset per referenced group; each is relative to <see cref="ZapfTable.Header.ExtraInfo"/>. A <see cref="ZapfTable.NoOffset"/> entry means the corresponding group is absent.</value>
    public required ReadOnlyMemory<uint> GroupOffsets { get; init; }

    static ZapfGlyphGroupOffsetArray IDerivedRecord<ZapfGroupData, ZapfGlyphGroupOffsetArray>.Parse(ref Cursor cursor, object? context)
    {
        ushort numGroupsRaw = cursor.ReadUInt16();
        if ((numGroupsRaw & 0xC000) != 0x4000)
            throw new InvalidDataException(
                $"'Zapf' GlyphGroupOffsetArray numGroups 0x{numGroupsRaw:X4} must have bit 14 set and bit 15 clear.");

        _ = cursor.ReadUInt16(); // padding, must be zero
        return new() { GroupOffsets = cursor.ReadUInt32Array(numGroupsRaw & 0x3FFF) };
    }
}
