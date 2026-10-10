using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// kerx — Extended Kerning Table (Apple Advanced Typography)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>kerx</c> table: the extended kerning table, which supersedes <c>kern</c> with wider offsets, contextual state tables, class-based arrays, and variation vectors.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is a header followed by subtables, each declaring its own format and coverage. Adjustments from every applicable subtable are additive, so the order of subtables is not significant.</description></item>
/// <item><description>Coverage distinguishes vertical from horizontal kerning, cross-stream from along-stream adjustments, and single values from variation vectors. A call to <see cref="KerxSubtable.GetKerningValue(ushort, ushort)"/> therefore returns the adjustment along the stream; a cross-stream subtable's value is perpendicular to it.</description></item>
/// <item><description>Formats 0, 2, and 6 are direct lookup formats; formats 1 and 4 are state-table driven and model contextual kerning and anchor-point positioning respectively. Formats 3, 5, and 7 through 255 are reserved.</description></item>
/// <item><description>Version 3 and later append a subtable glyph coverage array, and version 4 and later allow a subtable's <see cref="KerxSubtableHeader.TupleCount"/> to select variation vectors instead of single values.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html"><c>kerx</c> table</see> in the TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxSubtable"/>
/// <seealso cref="KernTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxTable : IFontTable<KerxTable>
{
    /// <summary>Gets the Apple Advanced Typography table tag <c>kerx</c>.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    public static Tag Tag => "kerx";

    /// <summary>Gets the table version. 2, 3, or 4.</summary>
    /// <remarks>Version 3 adds the subtable glyph coverage array; version 4 allows variation-vector subtables.</remarks>
    /// <seealso cref="HasSubtableGlyphCoverage"/>
    /// <seealso cref="SupportsVariationVectors"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the unused padding field.</summary>
    /// <remarks>The manual specifies 0. Surfaced rather than discarded so that a validator can flag a non-conforming writer.</remarks>
    public ushort Padding { get; init; }

    /// <summary>Gets the subtables, in table order.</summary>
    /// <seealso cref="SubtableCount"/>
    public IReadOnlyList<KerxSubtable> Subtables { get; init; } = [];

    /// <summary>Gets the subtable glyph coverage array, or <see langword="null"/> when the version does not supply one.</summary>
    /// <remarks>Present only for version 3 and later.</remarks>
    /// <seealso cref="HasSubtableGlyphCoverage"/>
    public KerxSubtableGlyphCoverage? GlyphCoverage { get; init; }

    /// <summary>Gets the number of subtables.</summary>
    /// <seealso cref="Subtables"/>
    public int SubtableCount => Subtables.Count;

    /// <summary>Gets a value indicating whether the table carries a subtable glyph coverage array.</summary>
    /// <seealso cref="Version"/>
    public bool HasSubtableGlyphCoverage => Version >= 3;

    /// <summary>Gets a value indicating whether a subtable's tuple count may select variation vectors.</summary>
    /// <remarks>Tuple counts are ignored before version 4.</remarks>
    /// <seealso cref="Version"/>
    /// <seealso cref="KerxSubtableHeader.TupleCount"/>
    public bool SupportsVariationVectors => Version >= 4;

    /// <summary>Computes the combined kerning adjustment for a glyph pair from every applicable subtable.</summary>
    /// <param name="left">The left-hand glyph index.</param>
    /// <param name="right">The right-hand glyph index.</param>
    /// <param name="vertical">Whether to consider vertical subtables rather than horizontal ones.</param>
    /// <returns>The summed adjustment in font design units.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Only subtables whose vertical flag matches <paramref name="vertical"/> contribute, so a horizontal query never sees a vertical subtable.</description></item>
    /// <item><description>Cross-stream subtables are included; their values adjust the position perpendicular to the stream, so a caller that needs the two components separately should query the subtables directly.</description></item>
    /// <item><description>A state-table subtable cannot answer a bare glyph pair without a glyph stream, so formats 1 and 4 contribute nothing here.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KerxSubtable.GetKerningValue(ushort, ushort)"/>
    public int GetKerningValue(ushort left, ushort right, bool vertical = false)
    {
        int accumulated = 0;

        foreach (var subtable in Subtables)
        {
            if (subtable.Header.IsVertical != vertical) continue;
            accumulated += subtable.GetKerningValue(left, right);
        }

        return accumulated;
    }

    /// <summary>The 8-byte fixed-layout <c>kerx</c> table header.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0. 2, 3, or 4.</summary>
        public ushort Version;    // +0

        /// <summary>The unused padding field at byte offset 2.</summary>
        public ushort Padding;    // +2

        /// <summary>The number of subtables at byte offset 4.</summary>
        public uint TableCount;   // +4

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            Padding = BinaryPrimitives.ReverseEndianness(v.Padding),
            TableCount = BinaryPrimitives.ReverseEndianness(v.TableCount),
        };
    }

    /// <summary>The 12-byte fixed-layout header that introduces every <c>kerx</c> subtable.</summary>
    /// <seealso cref="KerxSubtableHeader"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct SubtableHeader : IEndianReversibleStruct<SubtableHeader>
    {
        /// <summary>The total subtable length in bytes, including this header, at byte offset 0.</summary>
        public uint Length;       // +0

        /// <summary>The coverage flags and format word at byte offset 4.</summary>
        public uint Coverage;     // +4

        /// <summary>The tuple count at byte offset 8.</summary>
        /// <remarks>Ignored when the table version is below 4. A non-zero value means the subtable stores variation vectors rather than single values.</remarks>
        public uint TupleCount;   // +8

        /// <summary>Reverses the byte order of every field in a <see cref="SubtableHeader"/>.</summary>
        public static SubtableHeader ReverseEndianness(SubtableHeader v) => new()
        {
            Length = BinaryPrimitives.ReverseEndianness(v.Length),
            Coverage = BinaryPrimitives.ReverseEndianness(v.Coverage),
            TupleCount = BinaryPrimitives.ReverseEndianness(v.TupleCount),
        };
    }

    /// <summary>The masks of a <c>kerx</c> subtable's coverage word.</summary>
    /// <seealso cref="KerxSubtableHeader.Coverage"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    public static class Coverage
    {
        /// <summary>The mask set when the subtable holds vertical kerning values.</summary>
        public const uint Vertical = 0x80000000;

        /// <summary>The mask set when the subtable holds cross-stream kerning values.</summary>
        public const uint CrossStream = 0x40000000;

        /// <summary>The mask set when the subtable holds variation kerning values.</summary>
        public const uint Variation = 0x20000000;

        /// <summary>The mask that reverses the processing direction of a state-table subtable.</summary>
        public const uint ProcessDirection = 0x10000000;

        /// <summary>The unused bits, which the manual requires to be zero.</summary>
        public const uint UnusedMask = 0x0FFFFF00;

        /// <summary>The mask that selects the subtable format.</summary>
        public const uint FormatMask = 0x000000FF;
    }

    /// <summary>The positioning subtable formats the manual defines.</summary>
    /// <seealso cref="KerxSubtable.Format"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
    public enum SubtableFormat : byte
    {
        /// <summary>An ordered list of kerning pairs.</summary>
        OrderedPairs = 0,

        /// <summary>A state table for contextual kerning of up to eight glyphs.</summary>
        Contextual = 1,

        /// <summary>A byte-offset-based two-dimensional array of kerning values with class subtables.</summary>
        OffsetArray = 2,

        /// <summary>A state table for control point and anchor point positioning.</summary>
        AnchorPoints = 4,

        /// <summary>An index-based two-dimensional array of kerning values with class subtables.</summary>
        IndexArray = 6,
    }

    /// <summary>Reads a <c>kerx</c> table from <paramref name="cursor"/>. The cursor is advanced past the whole table.</summary>
    /// <param name="cursor">The cursor positioned at the first byte of the table.</param>
    /// <param name="context">A <see cref="FontFace"/> so that a format 0 class lookup can be sized by <c>maxp.numGlyphs</c>, or <see langword="null"/>; a plain <see cref="int"/> glyph count is also accepted.</param>
    /// <returns>The parsed table.</returns>
    /// <exception cref="InvalidDataException">The version is below 2.</exception>
    /// <exception cref="EndOfStreamException">The table's structures extend past the end of the table-scoped source.</exception>
    static KerxTable IRecord<KerxTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version < 2)
            throw new InvalidDataException($"'kerx'.version is {header.Version}, expected at least 2.");

        int contextGlyphCount = ResolveGlyphCount(context);
        int tableCount = checked((int)header.TableCount);

        var subtables = new KerxSubtable[tableCount];
        for (int i = 0; i < subtables.Length; i++)
        {
            long subtableStart = cursor.Position;
            subtables[i] = KerxSubtable.Read(ref cursor, header.Version, contextGlyphCount);

            // The declared length is authoritative; honour it even if a payload parse stopped short.
            long declaredEnd = subtableStart + subtables[i].Header.Length;
            if (declaredEnd > cursor.Position) cursor.Position = declaredEnd;
        }

        KerxSubtableGlyphCoverage? coverage = null;
        if (header.Version >= 3 && tableCount > 0)
        {
            long coverageStart = cursor.Position;
            if (coverageStart < cursor.Length)
                coverage = KerxSubtableGlyphCoverage.Read(ref cursor, coverageStart, tableCount);
        }

        return new KerxTable
        {
            Version = header.Version,
            Padding = header.Padding,
            Subtables = subtables,
            GlyphCoverage = coverage,
        };
    }

    /// <summary>Determines the font's glyph count from the parse context, when the caller supplied one.</summary>
    /// <param name="context">A <see cref="FontFace"/>, an <see cref="int"/>, or <see langword="null"/>.</param>
    /// <returns>The glyph count, or 0 when the context does not supply one.</returns>
    /// <remarks>A format 0 lookup table has no declared length, so a class or offset table in that format needs the glyph count to be read correctly. The count is optional because a table that never uses format 0 lookups does not need it.</remarks>
    internal static int ResolveGlyphCount(object? context) => context switch
    {
        FontFace face when face.Directory.ContainsKey(MaxpTable.Tag) => face.GetTable<MaxpTable>().NumGlyphs,
        int count => count,
        _ => 0,
    };
}

/// <summary>A <c>kerx</c> subtable: the common header plus one format-specific payload.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The abstract base carries the common header; the concrete subtypes are <see cref="KerxFormat0Subtable"/>, <see cref="KerxFormat1Subtable"/>, <see cref="KerxFormat2Subtable"/>, <see cref="KerxFormat4Subtable"/>, and <see cref="KerxFormat6Subtable"/>.</description></item>
/// <item><description><see cref="Read(ref Cursor, ushort, int)"/> reads the format byte of the coverage word, constructs the matching subtype, and leaves the cursor at the end of the subtable as declared by its length.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxTable.Subtables"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public abstract record KerxSubtable
{
    /// <summary>Gets the subtable's common header.</summary>
    public required KerxSubtableHeader Header { get; init; }

    /// <summary>Gets the subtable format, taken from the low byte of the coverage word.</summary>
    /// <seealso cref="KerxTable.SubtableFormat"/>
    public KerxTable.SubtableFormat Format =>
        (KerxTable.SubtableFormat)(Header.Coverage & KerxTable.Coverage.FormatMask);

    /// <summary>Gets the kerning adjustment this subtable applies to a glyph pair.</summary>
    /// <param name="left">The left-hand glyph index.</param>
    /// <param name="right">The right-hand glyph index.</param>
    /// <returns>The adjustment in font design units, or 0 when the subtable does not kern the pair.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>A state-table format cannot answer a bare glyph pair, because its actions depend on the glyph stream; those formats return 0.</description></item>
    /// <item><description>For a variation subtable the returned value is the first component of the vector, and is therefore only meaningful for the default instance. A caller that needs the variation vectors should read them from the subtable.</description></item>
    /// </list>
    /// </remarks>
    public abstract int GetKerningValue(ushort left, ushort right);

    /// <summary>Reads one subtable, dispatching on the format byte.</summary>
    /// <param name="cursor">The cursor positioned at the subtable's common header.</param>
    /// <param name="version">The enclosing table's version, which decides whether the tuple count is meaningful.</param>
    /// <param name="glyphCount">The font's glyph count, used to size a format 0 lookup, or 0 when unknown.</param>
    /// <returns>The parsed subtable, typed as the format-specific subclass.</returns>
    /// <exception cref="InvalidDataException">The format is reserved or unknown.</exception>
    /// <exception cref="EndOfStreamException">A structure extends past the end of the table-scoped source.</exception>
    public static KerxSubtable Read(ref Cursor cursor, ushort version, int glyphCount)
    {
        KerxTable.SubtableHeader raw = cursor.ReadBigEndianStruct<KerxTable.SubtableHeader>();

        // The tuple count is only meaningful from version 4 onwards.
        uint tupleCount = version >= 4 ? raw.TupleCount : 0u;

        var header = new KerxSubtableHeader
        {
            Length = raw.Length,
            Coverage = raw.Coverage,
            TupleCount = tupleCount,
        };

        long payloadStart = cursor.Position;
        var format = (KerxTable.SubtableFormat)(raw.Coverage & KerxTable.Coverage.FormatMask);

        return format switch
        {
            KerxTable.SubtableFormat.OrderedPairs => KerxFormat0Subtable.Read(ref cursor, header, payloadStart, tupleCount),
            KerxTable.SubtableFormat.Contextual => KerxFormat1Subtable.Read(ref cursor, header, payloadStart, tupleCount, glyphCount),
            KerxTable.SubtableFormat.OffsetArray => KerxFormat2Subtable.Read(ref cursor, header, payloadStart, tupleCount, glyphCount),
            KerxTable.SubtableFormat.AnchorPoints => KerxFormat4Subtable.Read(ref cursor, header, payloadStart, tupleCount, glyphCount),
            KerxTable.SubtableFormat.IndexArray => KerxFormat6Subtable.Read(ref cursor, header, payloadStart, tupleCount, glyphCount),
            _ => throw new InvalidDataException(
                $"'kerx' subtable format is {(byte)format}, expected 0, 1, 2, 4, or 6."),
        };
    }
}

/// <summary>The common <c>kerx</c> subtable header, in semantic form.</summary>
/// <seealso cref="KerxSubtable.Header"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxSubtableHeader
{
    /// <summary>Gets the total subtable length in bytes, including this header.</summary>
    public uint Length { get; init; }

    /// <summary>Gets the coverage flags and format word.</summary>
    /// <seealso cref="KerxTable.Coverage"/>
    public uint Coverage { get; init; }

    /// <summary>Gets the tuple count, which is 0 for every table below version 4 and for every non-variation subtable.</summary>
    /// <remarks>A non-zero value means the subtable stores <see cref="TupleCount"/>-component kerning vectors associated with the font's global <c>gvar</c> tuples rather than single values.</remarks>
    public uint TupleCount { get; init; }

    /// <summary>Gets a value indicating whether the subtable holds vertical kerning values.</summary>
    public bool IsVertical => (Coverage & KerxTable.Coverage.Vertical) != 0;

    /// <summary>Gets a value indicating whether the subtable holds cross-stream kerning values.</summary>
    /// <remarks>Cross-stream values adjust the position perpendicular to the text flow: vertically for horizontal text, horizontally for vertical text.</remarks>
    public bool IsCrossStream => (Coverage & KerxTable.Coverage.CrossStream) != 0;

    /// <summary>Gets a value indicating whether the subtable holds variation kerning values.</summary>
    public bool IsVariation => (Coverage & KerxTable.Coverage.Variation) != 0;

    /// <summary>Gets a value indicating whether a state-table subtable processes glyphs from last to first.</summary>
    /// <remarks>Applies only to formats 1 and 4.</remarks>
    public bool ProcessesBackwards => (Coverage & KerxTable.Coverage.ProcessDirection) != 0;
}

/// <summary>A <c>kerx</c> subtable glyph coverage array: one coverage bitfield per subtable.</summary>
/// <remarks>Identical in shape to the <c>morx</c> equivalent: an array of 32-bit offsets from the start of the array, followed by the bitfields. An offset of zero means the subtable has no bitfield.</remarks>
/// <seealso cref="MorxSubtableGlyphCoverage"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxSubtableGlyphCoverage
{
    /// <summary>Gets the coverage byte array for each subtable; a <see langword="null"/> entry means that subtable declares no coverage bitfield.</summary>
    public IReadOnlyList<byte[]?> Bitfields { get; init; } = [];

    /// <summary>Gets the number of coverage entries.</summary>
    public int Count => Bitfields.Count;

    /// <summary>Reports whether a subtable's coverage bitfield covers a glyph.</summary>
    /// <param name="subtableIndex">The subtable index within the table.</param>
    /// <param name="glyphIndex">The glyph index to test.</param>
    /// <returns><see langword="true"/> when the subtable has no bitfield, or when its bitfield covers <paramref name="glyphIndex"/>.</returns>
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
    /// <param name="subtableCount">The number of subtables, and therefore the number of offset words.</param>
    /// <returns>The parsed coverage array.</returns>
    /// <exception cref="EndOfStreamException">An offset word or bitfield extends past the end of the table-scoped source.</exception>
    internal static KerxSubtableGlyphCoverage Read(ref Cursor cursor, long arrayStart, int subtableCount)
    {
        var offsets = cursor.ReadUInt32Array(subtableCount);
        var bitfields = new byte[]?[subtableCount];

        for (int i = 0; i < subtableCount; i++)
        {
            uint offset = offsets[i];
            if (offset == 0) continue;

            long start = arrayStart + offset;
            long end = cursor.Length;

            for (int j = 0; j < subtableCount; j++)
            {
                if (offsets[j] <= offset) continue;

                long candidate = arrayStart + offsets[j];
                if (candidate < end) end = candidate;
            }

            int length = checked((int)(end - start));
            bitfields[i] = length <= 0 ? [] : cursor.Source.ReadBytesAt(start, length);
        }

        return new KerxSubtableGlyphCoverage { Bitfields = bitfields };
    }
}

/// <summary>A format 0 <c>kerx</c> subtable: an ordered list of kerning pairs with a binary-search header.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Pairs are sorted by the unsigned 32-bit value formed from the left glyph in the high word and the right glyph in the low word.</description></item>
/// <item><description>The list may end with a sentinel pair of <c>0xFFFF</c>/<c>0xFFFF</c>. The sentinel is kept in <see cref="Pairs"/> so that the declared pair count is preserved, but it never matches a real glyph pair because a lookup for glyph 0xFFFF would be a malformed query.</description></item>
/// <item><description>When <see cref="KerxSubtableHeader.TupleCount"/> is non-zero, each record's signed 16-bit field is instead the low half of a 32-bit offset from the start of the subtable to a variation vector. <see cref="KerningPair.RawValue"/> exposes that offset for a variation subtable, while <see cref="KerningPair.Value"/> exposes the single-value reading.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxSubtable"/>
/// <seealso cref="KerningPair"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxFormat0Subtable : KerxSubtable
{
    /// <summary>Gets the declared number of kerning pairs.</summary>
    public uint PairCount { get; init; }

    /// <summary>Gets the pairs, in the order the subtable stores them.</summary>
    /// <remarks>Sorted by the combined left and right glyph values so that a binary search works.</remarks>
    public IReadOnlyList<KerningPair> Pairs { get; init; } = [];

    /// <inheritdoc/>
    public override int GetKerningValue(ushort left, ushort right)
    {
        uint key = ((uint)left << 16) | right;
        int lo = 0;
        int hi = Pairs.Count - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            var pair = Pairs[mid];
            uint candidate = ((uint)pair.Left << 16) | pair.Right;

            if (candidate == key) return pair.Value;
            if (candidate < key) lo = mid + 1;
            else hi = mid - 1;
        }

        return 0;
    }

    /// <summary>Reads the payload of a format 0 subtable.</summary>
    /// <param name="cursor">The cursor positioned at the payload.</param>
    /// <param name="header">The already-read common subtable header.</param>
    /// <param name="payloadStart">The absolute offset of the payload.</param>
    /// <param name="tupleCount">The subtable's tuple count; non-zero selects the variation reading of each record's value field.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="EndOfStreamException">The pair array extends past the end of the table-scoped source.</exception>
    internal static KerxFormat0Subtable Read(ref Cursor cursor, KerxSubtableHeader header, long payloadStart, uint tupleCount)
    {
        uint pairCount = cursor.ReadUInt32();
        cursor.ReadUInt32();   // searchRange, derived and unused
        cursor.ReadUInt32();   // entrySelector, derived and unused
        cursor.ReadUInt32();   // rangeShift, derived and unused

        var pairs = new KerningPair[checked((int)pairCount)];
        for (int i = 0; i < pairs.Length; i++)
        {
            KerningPair pair = cursor.ReadBigEndianStruct<KerningPair>();
            pairs[i] = pair with { IsVariation = tupleCount != 0 };
        }

        return new KerxFormat0Subtable
        {
            Header = header,
            PairCount = pairCount,
            Pairs = pairs,
        };
    }
}

/// <summary>A format 0 <c>kerx</c> kerning pair record.</summary>
/// <seealso cref="KerxFormat0Subtable.Pairs"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct KerningPair : IEndianReversibleStruct<KerningPair>
{
    /// <summary>The size of one record, in bytes.</summary>
    public const int SizeInBytes = 6;

    /// <summary>The left-hand glyph index at byte offset 0.</summary>
    public ushort Left;      // +0

    /// <summary>The right-hand glyph index at byte offset 2.</summary>
    public ushort Right;     // +2

    /// <summary>The kerning value at byte offset 4.</summary>
    /// <remarks>Signed FUnits for a single-value subtable; for a variation subtable this field is the low half of a 32-bit offset and <see cref="Value"/> still reports the plain signed reading.</remarks>
    public short RawValue;   // +4

    /// <summary>Gets a value indicating whether this pair belongs to a variation subtable.</summary>
    /// <remarks>Set by the parser from the subtable's tuple count, since the record itself does not carry the flag.</remarks>
    public bool IsVariation { get; init; }

    /// <summary>Gets the single-value reading of the record's signed 16-bit field, in FUnits.</summary>
    /// <remarks>A positive value moves the glyphs apart; a negative value moves them together.</remarks>
    public readonly short Value => RawValue;

    /// <summary>Gets the offset to this pair's variation vector, for a variation subtable.</summary>
    /// <remarks>
    /// The manual describes the offset as a value from the beginning of the subtable to a tuple-count-dimensional vector. The record stores only its low half here; the high half is not present in the 6-byte record as documented, so this value is the offset's low 16 bits.
    /// </remarks>
    public readonly uint VariationOffset => unchecked((ushort)RawValue);

    /// <summary>Reverses the byte order of every field in a <see cref="KerningPair"/>.</summary>
    public static KerningPair ReverseEndianness(KerningPair v) => new()
    {
        Left = BinaryPrimitives.ReverseEndianness(v.Left),
        Right = BinaryPrimitives.ReverseEndianness(v.Right),
        RawValue = BinaryPrimitives.ReverseEndianness(v.RawValue),
        IsVariation = v.IsVariation,
    };
}

/// <summary>A format 1 <c>kerx</c> subtable: a state table for contextual kerning of up to eight glyphs.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The payload is an extended state table followed by a 32-bit offset to the kerning value array.</description></item>
/// <item><description>An action's index selects the first kerning value to apply; the value <c>0xFFFF</c> means no kerning. Each value pops one glyph from the kerning stack of up to eight glyphs, so the array is walked from the index until the terminator.</description></item>
/// <item><description>This format cannot answer a bare glyph pair, because its actions depend on the glyph stream and on the stack contents; <see cref="GetKerningValue(ushort, ushort)"/> therefore returns 0.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxSubtable"/>
/// <seealso cref="ContextualKerningAction"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxFormat1Subtable : KerxSubtable
{
    /// <summary>Gets the extended state table that drives the contextual kerning.</summary>
    public required AatStateTable StateTable { get; init; }

    /// <summary>Gets the kerning value array as single FUnit values.</summary>
    /// <remarks>Read until the <c>0xFFFF</c> terminator. For a variation subtable each applied value is a whole vector rather than one element of this list, so this list then holds the raw words.</remarks>
    public IReadOnlyList<short> KerningValues { get; init; } = [];

    /// <summary>Gets the kerning values that apply when the action index is used, stopping at the terminator or the end of the array.</summary>
    /// <param name="index">The action's kerning value index, or <c>0xFFFF</c> for no kerning.</param>
    /// <returns>The values to pop, in order, or an empty list when no kerning applies.</returns>
    /// <seealso cref="KerningValues"/>
    public IReadOnlyList<short> GetValues(ushort index)
    {
        if (index == 0xFFFF || index >= KerningValues.Count) return [];

        var result = new List<short>();
        for (int i = index; i < KerningValues.Count; i++)
        {
            short value = KerningValues[i];
            if (value == unchecked((short)0xFFFF)) break;
            result.Add(value);
        }

        return result;
    }

    /// <inheritdoc/>
    /// <remarks>A state-table format depends on the glyph stream, so a bare glyph pair has no answer.</remarks>
    public override int GetKerningValue(ushort left, ushort right) => 0;

    /// <summary>Reads the payload of a format 1 subtable.</summary>
    /// <param name="cursor">The cursor positioned at the payload.</param>
    /// <param name="header">The already-read common subtable header.</param>
    /// <param name="payloadStart">The absolute offset of the payload.</param>
    /// <param name="tupleCount">The subtable's tuple count, which selects vector values.</param>
    /// <param name="glyphCount">The font's glyph count, used to size a format 0 class lookup, or 0 when unknown.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="EndOfStreamException">A structure extends past the end of the table-scoped source.</exception>
    internal static KerxFormat1Subtable Read(
        ref Cursor cursor, KerxSubtableHeader header, long payloadStart, uint tupleCount, int glyphCount)
    {
        AatStateTable stateTable = AatStateTable.Read(ref cursor, payloadStart, ContextualKerningAction.SizeInBytes, glyphCount);

        uint valueTableOffset = cursor.Source.ReadUInt32At(payloadStart + 16);
        long valueTableStart = payloadStart + valueTableOffset;
        long subtableEnd = payloadStart + header.Length;

        return new KerxFormat1Subtable
        {
            Header = header,
            StateTable = stateTable,
            KerningValues = ReadKerningValues(cursor.Source, valueTableStart, subtableEnd, tupleCount),
        };
    }

    /// <summary>Reads a kerning value array up to its terminator.</summary>
    /// <param name="source">The table-scoped source.</param>
    /// <param name="start">The absolute offset of the array.</param>
    /// <param name="end">The absolute offset of the end of the owning subtable.</param>
    /// <param name="tupleCount">The subtable's tuple count, recorded with the result for callers that need the vector stride.</param>
    /// <returns>The raw value words, including the terminator when one is present.</returns>
    /// <remarks>The array's length is not declared. It is read word by word until the single-value <c>0xFFFF</c> terminator or the end of the subtable, whichever comes first; the manual uses that one word as the end marker for both single-value and vector subtables.</remarks>
    private static short[] ReadKerningValues(Source source, long start, long end, uint tupleCount)
    {
        _ = tupleCount;

        var values = new List<short>();

        for (long position = start; position + 2 <= end; position += 2)
        {
            short value = source.ReadInt16At(position);
            values.Add(value);

            if (value == unchecked((short)0xFFFF)) break;
        }

        return [.. values];
    }
}

/// <summary>One action of a format 1 contextual kerning state table.</summary>
/// <seealso cref="KerxFormat1Subtable.StateTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct ContextualKerningAction : IEndianReversibleStruct<ContextualKerningAction>
{
    /// <summary>The size of one action, in bytes.</summary>
    public const int SizeInBytes = 6;

    /// <summary>The mask that pushes the current glyph onto the kerning stack.</summary>
    public const ushort Push = 0x8000;

    /// <summary>The mask that leaves the glyph pointer on the current glyph before transitioning.</summary>
    public const ushort DontAdvance = 0x4000;

    /// <summary>The mask that clears the kerning stack.</summary>
    public const ushort Reset = 0x2000;

    /// <summary>The zero-based index of the state array row to use for the next glyph at byte offset 0.</summary>
    public ushort NewState;      // +0

    /// <summary>The action flags at byte offset 2.</summary>
    public ushort Flags;         // +2

    /// <summary>The index of the first kerning value to apply at byte offset 4, or <c>0xFFFF</c> for no kerning.</summary>
    public ushort ValueIndex;    // +4

    /// <summary>Reverses the byte order of every field in a <see cref="ContextualKerningAction"/>.</summary>
    public static ContextualKerningAction ReverseEndianness(ContextualKerningAction v) => new()
    {
        NewState = BinaryPrimitives.ReverseEndianness(v.NewState),
        Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
        ValueIndex = BinaryPrimitives.ReverseEndianness(v.ValueIndex),
    };
}

/// <summary>A format 2 <c>kerx</c> subtable: a byte-offset-based two-dimensional array of kerning values with class offset tables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Class values are pre-multiplied: the right-hand table yields a byte offset within a row and the left-hand table yields a byte offset to the row, so a lookup is a single addition and read.</description></item>
/// <item><description>The kerning array's extent is not declared, so this implementation records the row width and array offset and reads a value on demand. A query whose class values fall outside the subtable is treated as no kerning rather than as an error.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxSubtable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxFormat2Subtable : KerxSubtable
{
    /// <summary>Gets the number of bytes in each row of the kerning value array.</summary>
    public uint RowWidth { get; init; }

    /// <summary>Gets the lookup table that maps a left-hand glyph onto its byte offset within the array.</summary>
    /// <remarks>The lookup's values are pre-multiplied by the row width and offset by the array's position; a value of 0 means the glyph does not kern.</remarks>
    public required LookupTable LeftClassTable { get; init; }

    /// <summary>Gets the lookup table that maps a right-hand glyph onto its byte offset within a row.</summary>
    /// <remarks>The lookup's values are pre-multiplied by the size of one kerning value, two bytes.</remarks>
    public required LookupTable RightClassTable { get; init; }

    /// <summary>Gets the absolute offset of the kerning value array within the table's source.</summary>
    public long ArrayOffset { get; init; }

    /// <summary>Gets the byte source the kerning array lives in.</summary>
    /// <remarks>Retained so that a value can be read on demand, since the array's extent is not declared.</remarks>
    public Source? Source { get; init; }

    /// <summary>Gets the absolute offset of the end of the owning subtable.</summary>
    /// <remarks>Used to bound an on-demand read of the kerning array.</remarks>
    public long SubtableEnd { get; init; }

    /// <inheritdoc/>
    public override int GetKerningValue(ushort left, ushort right)
    {
        uint leftOffset = LeftClassTable.GetValue(left);
        uint rightOffset = RightClassTable.GetValue(right);
        if (leftOffset == 0 && rightOffset == 0) return 0;

        return ReadValue(ArrayOffset + leftOffset + rightOffset);
    }

    /// <summary>Reads one kerning value from the array, or 0 when the address falls outside the subtable.</summary>
    /// <param name="address">The absolute offset of the value.</param>
    /// <returns>The signed kerning value, or 0 when the address is out of range.</returns>
    /// <remarks>An out-of-range address means the class tables and the array disagree; the pair is treated as unkerned rather than raising an error, because a malformed subtable should not break layout of the rest of the font.</remarks>
    private int ReadValue(long address)
    {
        if (Source is null) return 0;
        if (address < ArrayOffset || address + 2 > SubtableEnd) return 0;

        return Source.ReadInt16At(address);
    }

    /// <summary>Reads the payload of a format 2 subtable.</summary>
    /// <param name="cursor">The cursor positioned at the payload.</param>
    /// <param name="header">The already-read common subtable header.</param>
    /// <param name="payloadStart">The absolute offset of the payload.</param>
    /// <param name="tupleCount">The subtable's tuple count; non-zero means the cells hold vector offsets.</param>
    /// <param name="glyphCount">The font's glyph count, used to size a format 0 class lookup, or 0 when unknown.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="EndOfStreamException">A class table extends past the end of the table-scoped source.</exception>
    internal static KerxFormat2Subtable Read(
        ref Cursor cursor, KerxSubtableHeader header, long payloadStart, uint tupleCount, int glyphCount)
    {
        uint rowWidth = cursor.ReadUInt32();
        uint leftTableOffset = cursor.ReadUInt32();
        uint rightTableOffset = cursor.ReadUInt32();
        uint arrayOffset = cursor.ReadUInt32();

        object? lookupContext = glyphCount > 0 ? glyphCount : null;
        long subtableEnd = payloadStart + header.Length;

        return new KerxFormat2Subtable
        {
            Header = header,
            RowWidth = rowWidth,
            LeftClassTable = ReadLookup(cursor.Source, payloadStart + leftTableOffset, lookupContext),
            RightClassTable = ReadLookup(cursor.Source, payloadStart + rightTableOffset, lookupContext),
            ArrayOffset = payloadStart + arrayOffset,
            Source = cursor.Source,
            SubtableEnd = subtableEnd,
        };
    }

    /// <summary>Reads a class lookup table at an absolute offset.</summary>
    /// <param name="source">The table-scoped source.</param>
    /// <param name="offset">The absolute offset of the lookup table.</param>
    /// <param name="context">The glyph count for a format 0 lookup, or <see langword="null"/>.</param>
    /// <returns>The parsed lookup table.</returns>
    private static LookupTable ReadLookup(Source source, long offset, object? context)
    {
        var lookupCursor = source.CreateCursor(offset);
        return LookupTable.Parse(ref lookupCursor, context);
    }
}

/// <summary>A format 4 <c>kerx</c> subtable: a state table for control point and anchor point positioning.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The payload is an extended state table followed by a 32-bit offset to the action value array. The action's index points into a list of 32-bit values that each pack a type, an offset within the glyph's point array, and an adjustment.</description></item>
/// <item><description>Like format 1 this subtable cannot answer a bare glyph pair, because its actions depend on the glyph stream; <see cref="GetKerningValue(ushort, ushort)"/> returns 0.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxSubtable"/>
/// <seealso cref="AnchorPointAction"/>
/// <seealso cref="AnchorPointValue"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxFormat4Subtable : KerxSubtable
{
    /// <summary>Gets the extended state table that drives the anchor point positioning.</summary>
    public required AatStateTable StateTable { get; init; }

    /// <summary>Gets the anchor point action values, in table order.</summary>
    /// <seealso cref="AnchorPointValue"/>
    public IReadOnlyList<AnchorPointValue> Values { get; init; } = [];

    /// <summary>Gets the action values that apply when the action index is used, stopping at the terminator or the end of the array.</summary>
    /// <param name="index">The action's value index, or <c>0xFFFF</c> for no action.</param>
    /// <returns>The values to apply, in order, or an empty list when no action applies.</returns>
    public IReadOnlyList<AnchorPointValue> GetValues(ushort index)
    {
        if (index == 0xFFFF || index >= Values.Count) return [];

        var result = new List<AnchorPointValue>();
        for (int i = index; i < Values.Count; i++)
        {
            var value = Values[i];
            if (value.IsTerminator) break;
            result.Add(value);
        }

        return result;
    }

    /// <inheritdoc/>
    /// <remarks>A state-table format depends on the glyph stream, so a bare glyph pair has no answer.</remarks>
    public override int GetKerningValue(ushort left, ushort right) => 0;

    /// <summary>Reads the payload of a format 4 subtable.</summary>
    /// <param name="cursor">The cursor positioned at the payload.</param>
    /// <param name="header">The already-read common subtable header.</param>
    /// <param name="payloadStart">The absolute offset of the payload.</param>
    /// <param name="tupleCount">The subtable's tuple count, which selects vector values.</param>
    /// <param name="glyphCount">The font's glyph count, used to size a format 0 class lookup, or 0 when unknown.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="EndOfStreamException">A structure extends past the end of the table-scoped source.</exception>
    internal static KerxFormat4Subtable Read(
        ref Cursor cursor, KerxSubtableHeader header, long payloadStart, uint tupleCount, int glyphCount)
    {
        AatStateTable stateTable = AatStateTable.Read(ref cursor, payloadStart, AnchorPointAction.SizeInBytes, glyphCount);

        uint valueTableOffset = cursor.Source.ReadUInt32At(payloadStart + 16);
        long valueTableStart = payloadStart + valueTableOffset;
        long subtableEnd = payloadStart + header.Length;

        int stride = tupleCount > 0 ? checked((int)tupleCount) : 1;
        var values = new List<AnchorPointValue>();

        for (long position = valueTableStart; position + 4 <= subtableEnd; position += 4)
        {
            uint raw = cursor.Source.ReadUInt32At(position);
            values.Add(AnchorPointValue.FromRaw(raw));

            // A value of 0xFFFFFFFF is the end marker, which can only appear on a vector boundary.
            if (raw == 0xFFFFFFFFu && (values.Count - 1) % stride == 0) break;
        }

        return new KerxFormat4Subtable
        {
            Header = header,
            StateTable = stateTable,
            Values = values,
        };
    }
}

/// <summary>One action of a format 4 anchor point state table.</summary>
/// <remarks>The action mirrors the format 1 action: a new state index, a flags word, and an index into the anchor point value array.</remarks>
/// <seealso cref="KerxFormat4Subtable.StateTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct AnchorPointAction : IEndianReversibleStruct<AnchorPointAction>
{
    /// <summary>The size of one action, in bytes.</summary>
    public const int SizeInBytes = 6;

    /// <summary>The zero-based index of the state array row to use for the next glyph at byte offset 0.</summary>
    public ushort NewState;     // +0

    /// <summary>The action flags at byte offset 2.</summary>
    public ushort Flags;        // +2

    /// <summary>The index of the first anchor point value to apply at byte offset 4, or <c>0xFFFF</c> for no action.</summary>
    public ushort ValueIndex;   // +4

    /// <summary>Reverses the byte order of every field in an <see cref="AnchorPointAction"/>.</summary>
    public static AnchorPointAction ReverseEndianness(AnchorPointAction v) => new()
    {
        NewState = BinaryPrimitives.ReverseEndianness(v.NewState),
        Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
        ValueIndex = BinaryPrimitives.ReverseEndianness(v.ValueIndex),
    };
}

/// <summary>One 32-bit anchor point action value of a format 4 <c>kerx</c> subtable.</summary>
/// <remarks>
/// The value packs an action type, the index of the point within the glyph's point array, and a signed adjustment. The adjustment is either an x or a y coordinate offset or a point index, depending on the type.
/// </remarks>
/// <seealso cref="KerxFormat4Subtable.Values"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public readonly record struct AnchorPointValue
{
    /// <summary>The raw action value word.</summary>
    /// <remarks>Kept so that a caller can inspect bits this type does not name.</remarks>
    public uint Value { get; init; }

    /// <summary>Gets the action type, taken from the top four bits.</summary>
    /// <remarks>Type 0 moves point <see cref="PointIndex"/> by <see cref="Adjustment"/> along x; type 1 does the same along y; type 2 moves the point to the x coordinate of the point named by the adjustment; type 4 is a control point action.</remarks>
    public byte ActionType => (byte)(Value >> 28);

    /// <summary>Gets the index of the control point within the glyph's point array.</summary>
    public ushort PointIndex => (ushort)((Value >> 16) & 0x0FFF);

    /// <summary>Gets the signed adjustment, a 16-bit FUnit value.</summary>
    public short Adjustment => unchecked((short)Value);

    /// <summary>Gets a value indicating whether this value terminates the action list.</summary>
    /// <remarks>The manual uses <c>0xFFFFFFFF</c> as the end-of-list marker.</remarks>
    public bool IsTerminator => Value == 0xFFFFFFFFu;

    /// <summary>Decodes a raw action value word.</summary>
    /// <param name="value">The raw big-endian-decoded word.</param>
    /// <returns>The decoded value.</returns>
    public static AnchorPointValue FromRaw(uint value) => new() { Value = value };
}

/// <summary>A format 6 <c>kerx</c> subtable: an index-based two-dimensional array of kerning values with class index tables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Unlike format 2, the class tables yield indices rather than pre-multiplied byte offsets, so the address is computed from the row width and the value size.</description></item>
/// <item><description>As with format 2 the array's extent is not declared, so a value is read on demand and an out-of-range address is treated as no kerning.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KerxSubtable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6kerx.html">TrueType Reference Manual: The <c>kerx</c> table</seealso>
public sealed record KerxFormat6Subtable : KerxSubtable
{
    /// <summary>Gets the number of bytes in each row of the kerning value array.</summary>
    public uint RowWidth { get; init; }

    /// <summary>Gets the lookup table that maps a left-hand glyph onto its row index.</summary>
    public required LookupTable LeftClassTable { get; init; }

    /// <summary>Gets the lookup table that maps a right-hand glyph onto its column index.</summary>
    public required LookupTable RightClassTable { get; init; }

    /// <summary>Gets the absolute offset of the kerning value array within the table's source.</summary>
    public long ArrayOffset { get; init; }

    /// <summary>Gets the byte source the kerning array lives in.</summary>
    public Source? Source { get; init; }

    /// <summary>Gets the absolute offset of the end of the owning subtable.</summary>
    public long SubtableEnd { get; init; }

    /// <inheritdoc/>
    public override int GetKerningValue(ushort left, ushort right)
    {
        uint row = LeftClassTable.GetValue(left);
        uint column = RightClassTable.GetValue(right);

        if (row == 0 && column == 0) return 0;

        long address = ArrayOffset + (long)row * RowWidth + (long)column * 2;
        if (Source is null) return 0;
        if (address < ArrayOffset || address + 2 > SubtableEnd) return 0;

        return Source.ReadInt16At(address);
    }

    /// <summary>Reads the payload of a format 6 subtable.</summary>
    /// <param name="cursor">The cursor positioned at the payload.</param>
    /// <param name="header">The already-read common subtable header.</param>
    /// <param name="payloadStart">The absolute offset of the payload.</param>
    /// <param name="tupleCount">The subtable's tuple count; non-zero means the cells hold vector offsets.</param>
    /// <param name="glyphCount">The font's glyph count, used to size a format 0 class lookup, or 0 when unknown.</param>
    /// <returns>The parsed subtable.</returns>
    /// <exception cref="EndOfStreamException">A class table extends past the end of the table-scoped source.</exception>
    internal static KerxFormat6Subtable Read(
        ref Cursor cursor, KerxSubtableHeader header, long payloadStart, uint tupleCount, int glyphCount)
    {
        uint rowWidth = cursor.ReadUInt32();
        uint leftTableOffset = cursor.ReadUInt32();
        uint rightTableOffset = cursor.ReadUInt32();
        uint arrayOffset = cursor.ReadUInt32();

        object? lookupContext = glyphCount > 0 ? glyphCount : null;

        var leftCursor = cursor.Source.CreateCursor(payloadStart + leftTableOffset);
        var rightCursor = cursor.Source.CreateCursor(payloadStart + rightTableOffset);

        return new KerxFormat6Subtable
        {
            Header = header,
            RowWidth = rowWidth,
            LeftClassTable = LookupTable.Parse(ref leftCursor, lookupContext),
            RightClassTable = LookupTable.Parse(ref rightCursor, lookupContext),
            ArrayOffset = payloadStart + arrayOffset,
            Source = cursor.Source,
            SubtableEnd = payloadStart + header.Length,
        };
    }
}
