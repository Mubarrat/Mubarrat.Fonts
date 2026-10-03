using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Tables.Metadata;

/// <summary>The <c>kern</c> table: legacy kerning. Provides inter-character spacing adjustments for glyph pairs, as an alternative to the GPOS table's pair-adjustment lookups.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table has a header followed by one or more subtables. Each subtable declares its format and coverage in a common header; the two defined formats are format 0 (explicit pairs) and format 2 (class-based pairs).</description></item>
/// <item><description>The <c>kern</c> table is not supported in CFF-flavored OpenType fonts. CFF fonts must use GPOS for kerning.</description></item>
/// <item><description>Multiple subtables can contribute to the same glyph pair. Kerning values are additive; minimum subtables cap the accumulated value; override subtables replace it. The order of additive subtables is not significant, but the specification recommends placing minimum subtables last.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">kern table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KernSubtable"/>
/// <seealso cref="KernFormat0"/>
/// <seealso cref="KernFormat2"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern table</seealso>
public sealed record KernTable : IOpenTypeTable<KernTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IOpenTypeTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern table</seealso>
    public static Tag Tag => "kern";

    /// <summary>Gets the table version. Always 0 for OpenType.</summary>
    /// <value>The constant <c>0</c> for a conforming OpenType kern table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern"><c>version</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Subtables"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: <c>version</c></seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the subtables in table order.</summary>
    /// <value>The ordered list of <see cref="KernSubtable"/> entries. Because subtable semantics depend on order for minimum and override subtables, the list order is preserved verbatim from the source.</value>
    /// <seealso cref="SubtableCount"/>
    /// <seealso cref="GetKerningValue(ushort, ushort)"/>
    public IReadOnlyList<KernSubtable> Subtables { get; init; } = [];

    /// <summary>Gets the number of subtables.</summary>
    /// <value>The size of the <see cref="Subtables"/> list.</value>
    /// <seealso cref="Subtables"/>
    public int SubtableCount => Subtables.Count;

    /// <summary>Returns the combined horizontal kerning adjustment for a glyph pair. Applies all horizontal subtables in order: additive subtables accumulate, minimum subtables cap the accumulated value, and override subtables replace it.</summary>
    /// <param name="left">The left-hand glyph ID.</param>
    /// <param name="right">The right-hand glyph ID.</param>
    /// <returns>The combined kerning adjustment in font design units.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Vertical subtables are skipped; the method always computes the horizontal kerning value.</description></item>
    /// <item><description>The order of <see cref="Subtables"/> matters: override and minimum subtables interact with the accumulator, so the specification's recommended ordering (additive first, minimum last) produces the expected result.</description></item>
    /// <item><description>An empty subtables list or a font with no matching pairs both return <c>0</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// int kern = kernTable.GetKerningValue(leftGlyph, rightGlyph);
    /// </code>
    /// </example>
    /// <seealso cref="KernSubtable.GetKerningValue(ushort, ushort)"/>
    /// <seealso cref="KernSubtable.Horizontal"/>
    /// <seealso cref="KernSubtable.Minimum"/>
    /// <seealso cref="KernSubtable.Override"/>
    public int GetKerningValue(ushort left, ushort right)
    {
        int accumulated = 0;
        foreach (var sub in Subtables)
        {
            if (!sub.Horizontal) continue;
            int value = sub.GetKerningValue(left, right);
            if (sub.Override) accumulated = value;
            else if (sub.Minimum) accumulated = Math.Min(accumulated, value);
            else accumulated += value;
        }
        return accumulated;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the kern table.</param>
    /// <param name="context">Forwarded to the subtable parsers.</param>
    /// <returns>The parsed kern table.</returns>
    /// <exception cref="InvalidDataException">The version is not 0.</exception>
    /// <exception cref="EndOfStreamException">The header or any subtable extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Subtables are read through <see cref="Cursor.PeekRecordArray{T}(int, object?)"/>, which relies on each subtable's parse to advance the cursor by its declared length before the next record is read.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">kern table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="KernSubtable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern table</seealso>
    public static KernTable Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.Version != 0)
            throw new InvalidDataException(
                $"'kern'.version is {header.Version}, expected 0.");

        int numTables = header.NumTables;
        var subtables = cursor.PeekRecordArray<KernSubtable>(numTables);

        return new KernTable { Version = header.Version, Subtables = subtables };
    }

    /// <summary>The 4-byte <c>kern</c> table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by <c>numTables</c> subtable records; each subtable declares its own length so no offset array is needed.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">kern header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KernTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the table version. Always 0 for OpenType.</summary>
        /// <value>The constant <c>0</c> for a conforming OpenType kern table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern"><c>version</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="NumTables"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: <c>version</c></seealso>
        public ushort Version;     // +0

        /// <summary>Gets the number of subtables that follow the header.</summary>
        /// <value>The count of <see cref="KernSubtable"/> records.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern"><c>nTables</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Version"/>
        /// <seealso cref="KernSubtable"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: <c>nTables</c></seealso>
        public ushort NumTables;   // +2

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            NumTables = BinaryPrimitives.ReverseEndianness(v.NumTables),
        };
    }
}

/// <summary>Base class for a <c>kern</c> subtable. Exposes the common subtable header fields and dispatches the format-specific parse.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The common subtable header carries a version, a declared length, and a coverage word that encodes both format and behavior flags.</description></item>
/// <item><description>Two formats are defined; unknown formats are wrapped in <see cref="KernUnknown"/> so the enclosing table can still advance past them.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">kern subtable</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KernFormat0"/>
/// <seealso cref="KernFormat2"/>
/// <seealso cref="KernUnknown"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern subtable</seealso>
public abstract record KernSubtable : IRecord<KernSubtable>, IBaseRecord<KernSubtable>
{
    /// <summary>Bit flags declared by the subtable header's coverage field.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Bits 0–3 describe subtable behavior; bits 4–7 are reserved; bits 8–15 encode the format.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern"><c>coverage</c> field</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KernSubtable.Coverage"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: <c>coverage</c></seealso>
    [Flags]
    public enum KernCoverage : ushort
    {
        /// <summary>No flags set.</summary>
        None = 0,

        /// <summary>The subtable contains horizontal kerning data.</summary>
        /// <seealso cref="KernSubtable.Horizontal"/>
        Horizontal = 0x0001,

        /// <summary>The subtable contains minimum values instead of kerning values.</summary>
        /// <seealso cref="KernSubtable.Minimum"/>
        Minimum = 0x0002,

        /// <summary>Kerning is perpendicular to the flow of text.</summary>
        /// <seealso cref="KernSubtable.CrossStream"/>
        CrossStream = 0x0004,

        /// <summary>The value replaces rather than adds to the accumulated value.</summary>
        /// <seealso cref="KernSubtable.Override"/>
        Override = 0x0008,

        /// <summary>Bits 4–7: reserved.</summary>
        ReservedMask = 0x00F0,

        /// <summary>Bits 8–15: format of the subtable.</summary>
        /// <seealso cref="KernSubtable.Format"/>
        FormatMask = 0xFF00,
    }

    /// <summary>Subtable format identifiers.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Only formats 0 and 2 are currently defined by the specification; other values are tolerated by the parser and wrapped in <see cref="KernUnknown"/>.</description></item>
    /// <item><description>The format value is encoded in bits 8–15 of the coverage field; see <see cref="Format"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Format"/>
    /// <seealso cref="KernUnknown"/>
    public enum KernFormat : byte
    {
        /// <summary>Format 0: explicit kerning pairs.</summary>
        /// <seealso cref="KernFormat0"/>
        Format0 = 0,
        /// <summary>Format 2: class-based kerning.</summary>
        /// <seealso cref="KernFormat2"/>
        Format2 = 2,
    }

    /// <summary>Gets the subtable version. Set to 0 for OpenType.</summary>
    /// <value>The constant <c>0</c> for a conforming OpenType kern subtable.</value>
    /// <seealso cref="Length"/>
    /// <seealso cref="Coverage"/>
    public ushort Version { get; init; }

    /// <summary>Gets the subtable length in bytes, including the header.</summary>
    /// <value>The on-disk size of the entire subtable. The parser uses this to advance the cursor to the next subtable, regardless of how many bytes the format-specific body actually consumed.</value>
    /// <seealso cref="Version"/>
    /// <seealso cref="Coverage"/>
    public ushort Length { get; init; }

    /// <summary>Gets the coverage field.</summary>
    /// <value>The raw 16-bit coverage word; use <see cref="Format"/>, <see cref="Horizontal"/>, <see cref="Minimum"/>, <see cref="CrossStream"/>, and <see cref="Override"/> to inspect individual parts.</value>
    /// <seealso cref="Format"/>
    /// <seealso cref="KernCoverage"/>
    public ushort Coverage { get; init; }

    /// <summary>Gets the subtable format.</summary>
    /// <value>The format discriminant decoded from bits 8–15 of <see cref="Coverage"/>.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="KernFormat"/>
    public KernFormat Format => (KernFormat)((Coverage >> 8) & 0xFF);

    /// <summary>True when the subtable contains horizontal data.</summary>
    /// <value><see langword="true"/> when bit 0 of <see cref="Coverage"/> is set.</value>
    /// <seealso cref="KernCoverage.Horizontal"/>
    public bool Horizontal => (Coverage & 0x0001) != 0;

    /// <summary>True when the subtable contains minimum values.</summary>
    /// <value><see langword="true"/> when bit 1 of <see cref="Coverage"/> is set.</value>
    /// <seealso cref="KernCoverage.Minimum"/>
    public bool Minimum => (Coverage & 0x0002) != 0;

    /// <summary>True when the subtable kerns perpendicular to the text flow.</summary>
    /// <value><see langword="true"/> when bit 2 of <see cref="Coverage"/> is set.</value>
    /// <seealso cref="KernCoverage.CrossStream"/>
    public bool CrossStream => (Coverage & 0x0004) != 0;

    /// <summary>True when the subtable replaces rather than accumulates.</summary>
    /// <value><see langword="true"/> when bit 3 of <see cref="Coverage"/> is set.</value>
    /// <seealso cref="KernCoverage.Override"/>
    public bool Override => (Coverage & 0x0008) != 0;

    /// <summary>Returns the kerning value for a glyph pair, or 0 when the pair is absent.</summary>
    /// <param name="left">The left-hand glyph ID.</param>
    /// <param name="right">The right-hand glyph ID.</param>
    /// <returns>The kerning value in font design units, or <c>0</c> when the pair is not present in the subtable.</returns>
    /// <remarks>Each concrete format implements the lookup independently; see <see cref="KernFormat0.GetKerningValue(ushort, ushort)"/> and <see cref="KernFormat2.GetKerningValue(ushort, ushort)"/>.</remarks>
    /// <seealso cref="KernFormat0.GetKerningValue(ushort, ushort)"/>
    /// <seealso cref="KernFormat2.GetKerningValue(ushort, ushort)"/>
    public abstract short GetKerningValue(ushort left, ushort right);

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable.</param>
    /// <param name="context">Forwarded to the format-specific parser.</param>
    /// <returns>The format-specific subtable instance.</returns>
    /// <exception cref="EndOfStreamException">The header or any referenced data extends past the end of the subtable's declared length.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is peeked without consuming bytes, so the format-specific parser can re-read it from position 0 and advance the cursor as it needs.</description></item>
    /// <item><description>After the format body parses, the cursor is reset to the header's declared <see cref="Length"/> so the enclosing table can advance to the next subtable.</description></item>
    /// <item><description>Unrecognised format values are wrapped in <see cref="KernUnknown"/>, preserving the header and relying on the length to skip the payload.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="KernFormat0"/>
    /// <seealso cref="KernFormat2"/>
    /// <seealso cref="KernUnknown"/>
    public static KernSubtable Parse(ref Cursor cursor, object? context)
    {
        // Peek the header so the discriminant can be read without consuming bytes.
        // Each format reads the header itself from position 0.
        Header header = cursor.PeekBigEndianStruct<Header>();

        KernSubtable value = header.Format switch
        {
            KernFormat.Format0 => IBaseRecord<KernSubtable>.Parse<KernFormat0>(ref cursor, context),
            KernFormat.Format2 => IBaseRecord<KernSubtable>.Parse<KernFormat2>(ref cursor, context),
            _ => IBaseRecord<KernSubtable>.Parse<KernUnknown>(ref cursor, context),
        };

        // Reset to the declared length so the enclosing subtable iterator advances by the
        // subtable's full extent, regardless of how many bytes the format body actually read.
        cursor.Position = header.Length;
        return value;
    }

    /// <summary>The 6-byte subtable common header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All three fields are common to every subtable format; the format-specific bodies follow.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">kern subtable</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KernSubtable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern subtable</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>The size of the header, in bytes.</summary>
        /// <value>The constant <c>6</c>.</value>
        /// <remarks>Used by callers that compute subtable offsets from a fixed header size.</remarks>
        public const int Size = 6;

        /// <summary>Gets the subtable version. Set to 0 for OpenType.</summary>
        /// <value>The constant <c>0</c> for a conforming OpenType kern subtable.</value>
        /// <seealso cref="Length"/>
        /// <seealso cref="Coverage"/>
        public ushort Version;     // +0

        /// <summary>Gets the subtable length in bytes, including the header.</summary>
        /// <value>The on-disk size of the entire subtable.</value>
        /// <seealso cref="Version"/>
        /// <seealso cref="Coverage"/>
        public ushort Length;      // +2

        /// <summary>Gets the coverage field.</summary>
        /// <value>The raw 16-bit coverage word; use <see cref="Format"/> or the corresponding flags on <see cref="KernSubtable"/> to inspect individual parts.</value>
        /// <seealso cref="Format"/>
        /// <seealso cref="KernSubtable.Coverage"/>
        public ushort Coverage;    // +4

        /// <summary>Gets the subtable format decoded from the coverage field.</summary>
        /// <value>The format discriminant from bits 8–15 of <see cref="Coverage"/>.</value>
        /// <seealso cref="Coverage"/>
        /// <seealso cref="KernSubtable.KernFormat"/>
        public readonly KernFormat Format => (KernFormat)((Coverage >> 8) & 0xFF);

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            Length = BinaryPrimitives.ReverseEndianness(v.Length),
            Coverage = BinaryPrimitives.ReverseEndianness(v.Coverage),
        };
    }
}

/// <summary>A fixed-layout kerning pair record. Blittable, size 6. The pair key is <c>((uint)Left &lt;&lt; 16) | Right</c>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The combined key gives the pairs a natural ascending order: sorted by left glyph ID, then by right glyph ID. The format 0 subtable relies on this ordering for binary search.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">kern format 0</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KernFormat0"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern format 0</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct KernPair : IBigEndianStruct<KernPair>
{
    /// <summary>Left-hand glyph ID.</summary>
    /// <value>The first glyph of the kerning pair.</value>
    /// <seealso cref="Right"/>
    /// <seealso cref="Key"/>
    public ushort Left;      // +0

    /// <summary>Right-hand glyph ID.</summary>
    /// <value>The second glyph of the kerning pair.</value>
    /// <seealso cref="Left"/>
    /// <seealso cref="Key"/>
    public ushort Right;     // +2

    /// <summary>Kerning value in font design units.</summary>
    /// <value>The signed kerning adjustment applied between the pair; negative values tighten, positive values loosen.</value>
    /// <seealso cref="Key"/>
    public short Value;      // +4

    /// <summary>Gets the combined sort key used to order pairs in the table.</summary>
    /// <value>A 32-bit key formed as <c>(Left &lt;&lt; 16) | Right</c>; the numeric ordering matches ascending (Left, Right) lexicographic order.</value>
    /// <seealso cref="Left"/>
    /// <seealso cref="Right"/>
    public readonly uint Key => ((uint)Left << 16) | Right;

    /// <inheritdoc/>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new pair with each multi-byte field reversed.</returns>
    /// <remarks>All three fields are multi-byte and are reversed independently.</remarks>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    public static KernPair ReverseEndianness(KernPair v) => new()
    {
        Left = BinaryPrimitives.ReverseEndianness(v.Left),
        Right = BinaryPrimitives.ReverseEndianness(v.Right),
        Value = BinaryPrimitives.ReverseEndianness(v.Value),
    };
}

/// <summary><c>kern</c> subtable format 0: an explicit sorted list of kerning pairs.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Pairs are stored in ascending key order so a shaper can binary-search for a specific pair.</description></item>
/// <item><description>Formats 0 subtables are the simplest form of the kern table but scale poorly; format 2 is preferred for fonts with many kerning pairs.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">kern format 0</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KernSubtable"/>
/// <seealso cref="KernFormat2"/>
/// <seealso cref="KernPair"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern format 0</seealso>
public sealed record KernFormat0 : KernSubtable, IDerivedRecord<KernSubtable, KernFormat0>
{
    /// <summary>Gets the pairs, sorted by <see cref="KernPair.Key"/>.</summary>
    /// <value>The ordered list of <see cref="KernPair"/> entries; sorted ascending by <see cref="KernPair.Key"/>.</value>
    /// <seealso cref="PairCount"/>
    /// <seealso cref="KernPair"/>
    public IReadOnlyList<KernPair> Pairs { get; init; } = [];

    /// <summary>Gets the number of pairs.</summary>
    /// <value>The size of the <see cref="Pairs"/> list.</value>
    /// <seealso cref="Pairs"/>
    public int PairCount => Pairs.Count;

    /// <inheritdoc/>
    /// <param name="left">The left-hand glyph ID.</param>
    /// <param name="right">The right-hand glyph ID.</param>
    /// <returns>The kerning value for the pair, or <c>0</c> when the pair is not present.</returns>
    /// <remarks>The lookup uses binary search over <see cref="Pairs"/>, relying on the specification's required ascending ordering.</remarks>
    /// <seealso cref="Pairs"/>
    /// <seealso cref="KernPair.Key"/>
    public override short GetKerningValue(ushort left, ushort right)
    {
        uint key = ((uint)left << 16) | right;
        int lo = 0, hi = Pairs.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            uint midKey = Pairs[mid].Key;
            if (midKey == key) return Pairs[mid].Value;
            if (midKey < key) lo = mid + 1;
            else hi = mid - 1;
        }
        return 0;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable.</param>
    /// <param name="context">Unused. The subtable is self-describing.</param>
    /// <returns>The parsed format 0 subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, body header, or pair array extends past the end of the subtable's declared length.</exception>
    /// <remarks>The on-disk layout is the common subtable header, then a fixed-size binary-search descriptor, then the sorted pair array.</remarks>
    /// <seealso cref="KernSubtable.Header"/>
    /// <seealso cref="Format0Header"/>
    /// <seealso cref="KernPair"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern format 0</seealso>
    static KernFormat0 IDerivedRecord<KernSubtable, KernFormat0>.Parse(
        ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        Format0Header body = cursor.ReadBigEndianStruct<Format0Header>();
        KernPair[] pairs = cursor.ReadBigEndianStructArray<KernPair>(body.NPairs);

        return new KernFormat0
        {
            Version = header.Version,
            Length = header.Length,
            Coverage = header.Coverage,
            Pairs = pairs,
        };
    }

    /// <summary>The 8-byte format 0 body header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The four fields reproduce the standard binary-search descriptor used throughout OpenType for sorted record arrays.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">kern format 0</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KernFormat0"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern format 0</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Format0Header : IBigEndianStruct<Format0Header>
    {
        /// <summary>Gets the number of kerning pairs.</summary>
        /// <value>The count of <see cref="KernPair"/> entries in the array.</value>
        /// <seealso cref="SearchRange"/>
        public ushort NPairs;         // +0

        /// <summary>Gets the binary-search search range.</summary>
        /// <value>The largest power of two less than or equal to <see cref="NPairs"/>, multiplied by the pair record size. Provided as an optimization hint; the parser does not rely on it.</value>
        /// <seealso cref="EntrySelector"/>
        public ushort SearchRange;    // +2

        /// <summary>Gets the binary-search entry selector.</summary>
        /// <value>The base-2 logarithm of the largest power of two less than or equal to <see cref="NPairs"/>. Provided as an optimization hint; the parser does not rely on it.</value>
        /// <seealso cref="RangeShift"/>
        public ushort EntrySelector;  // +4

        /// <summary>Gets the binary-search range shift.</summary>
        /// <value>The value of <c>NPairs * recordSize - SearchRange</c>. Provided as an optimization hint; the parser does not rely on it.</value>
        /// <seealso cref="EntrySelector"/>
        public ushort RangeShift;     // +6

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All four fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Format0Header ReverseEndianness(Format0Header v) => new()
        {
            NPairs = BinaryPrimitives.ReverseEndianness(v.NPairs),
            SearchRange = BinaryPrimitives.ReverseEndianness(v.SearchRange),
            EntrySelector = BinaryPrimitives.ReverseEndianness(v.EntrySelector),
            RangeShift = BinaryPrimitives.ReverseEndianness(v.RangeShift),
        };
    }
}

/// <summary>A class table in a format 2 subtable: a first-glyph base and a contiguous array of pre-multiplied class offsets.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each entry is a byte offset into the kerning array, pre-multiplied by two so it can be summed with the right-hand offset and halved to produce an array index.</description></item>
/// <item><description>Glyphs outside the covered range are assigned the default pre-multiplied value <c>0</c>, which typically maps them to the zero row or column of the array (meaning "no kerning").</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">kern format 2</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KernFormat2"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern format 2</seealso>
public sealed record KernClassTable : IRecord<KernClassTable>
{
    /// <summary>Gets the first glyph ID covered by the table.</summary>
    /// <value>The lowest glyph ID for which <see cref="ClassValues"/> provides an entry.</value>
    /// <seealso cref="ClassValues"/>
    /// <seealso cref="GetRawValue(ushort)"/>
    public ushort FirstGlyph { get; init; }

    /// <summary>Gets the raw (pre-multiplied) class values, one per covered glyph.</summary>
    /// <value>The ordered list of <c>uint16</c> values; index <c>i</c> corresponds to glyph ID <c><see cref="FirstGlyph"/> + i</c>.</value>
    /// <seealso cref="FirstGlyph"/>
    /// <seealso cref="GetRawValue(ushort)"/>
    public IReadOnlyList<ushort> ClassValues { get; init; } = [];

    /// <summary>Returns the raw pre-multiplied value for a glyph, or 0 when uncovered.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The pre-multiplied class offset for the glyph, or <c>0</c> when <paramref name="glyphId"/> is outside the covered range.</returns>
    /// <remarks>The lookup is constant-time; the glyph's offset from <see cref="FirstGlyph"/> directly indexes into <see cref="ClassValues"/>.</remarks>
    /// <seealso cref="ClassValues"/>
    /// <seealso cref="FirstGlyph"/>
    public int GetRawValue(ushort glyphId)
    {
        int index = glyphId - FirstGlyph;
        return (uint)index < (uint)ClassValues.Count ? ClassValues[index] : 0;
    }

    /// <summary>Parses a class table from a cursor already positioned at its start.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the class table.</param>
    /// <param name="context">Unused. The class table is self-describing.</param>
    /// <returns>The parsed class table.</returns>
    /// <exception cref="EndOfStreamException">The header or class array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">kern format 2</see> in the OpenType specification.</remarks>
    /// <seealso cref="ClassTableHeader"/>
    /// <seealso cref="ClassValues"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern format 2</seealso>
    public static KernClassTable Parse(ref Cursor cursor, object? context)
    {
        ClassTableHeader header = cursor.ReadBigEndianStruct<ClassTableHeader>();
        ushort[] classes = cursor.ReadUInt16Array(header.NGlyphs);
        return new KernClassTable { FirstGlyph = header.FirstGlyph, ClassValues = classes };
    }

    /// <summary>The 4-byte class table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The class array immediately follows this header and contains <see cref="NGlyphs"/> pre-multiplied values.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">kern format 2</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KernClassTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern format 2</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct ClassTableHeader : IBigEndianStruct<ClassTableHeader>
    {
        /// <summary>Gets the first glyph ID covered by the class table.</summary>
        /// <value>The lowest glyph ID for which a class value is present.</value>
        /// <seealso cref="NGlyphs"/>
        public ushort FirstGlyph;   // +0

        /// <summary>Gets the number of glyphs covered by the class table.</summary>
        /// <value>The count of class values that follow the header.</value>
        /// <seealso cref="FirstGlyph"/>
        public ushort NGlyphs;      // +2

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static ClassTableHeader ReverseEndianness(ClassTableHeader v) => new()
        {
            FirstGlyph = BinaryPrimitives.ReverseEndianness(v.FirstGlyph),
            NGlyphs = BinaryPrimitives.ReverseEndianness(v.NGlyphs),
        };
    }
}

/// <summary><c>kern</c> subtable format 2: a two-dimensional class-indexed kerning array.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The array is indexed by <c>(leftClassTable[left] + rightClassTable[right]) / 2</c>, where the class table entries are pre-multiplied byte offsets.</description></item>
/// <item><description>Row 0 and column 0 hold zero values and represent glyphs that do not kern.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">kern format 2</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KernSubtable"/>
/// <seealso cref="KernFormat0"/>
/// <seealso cref="KernClassTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern format 2</seealso>
public sealed record KernFormat2 : KernSubtable, IDerivedRecord<KernSubtable, KernFormat2>
{
    /// <summary>Gets the width of one row, in bytes.</summary>
    /// <value>The size of a single row of the kerning array, in bytes; the array's row stride.</value>
    /// <seealso cref="KerningArray"/>
    public ushort RowWidth { get; init; }

    /// <summary>Gets the left-hand class table.</summary>
    /// <value>The <see cref="KernClassTable"/> that maps left glyphs to pre-multiplied offsets.</value>
    /// <seealso cref="RightClassTable"/>
    /// <seealso cref="KernClassTable"/>
    public KernClassTable LeftClassTable { get; init; } = null!;

    /// <summary>Gets the right-hand class table.</summary>
    /// <value>The <see cref="KernClassTable"/> that maps right glyphs to pre-multiplied offsets.</value>
    /// <seealso cref="LeftClassTable"/>
    /// <seealso cref="KernClassTable"/>
    public KernClassTable RightClassTable { get; init; } = null!;

    /// <summary>Gets the kerning values, stored row-major as FWORDs.</summary>
    /// <value>The flat array of <c>int16</c> kerning values; the class-table offsets select an index into this array.</value>
    /// <seealso cref="RowWidth"/>
    public IReadOnlyList<short> KerningArray { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="left">The left-hand glyph ID.</param>
    /// <param name="right">The right-hand glyph ID.</param>
    /// <returns>The kerning value for the pair, or <c>0</c> when the computed index is out of range.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The two class offsets are summed and halved to produce the array index; the halving removes the pre-multiplication applied when the class table was written.</description></item>
    /// <item><description>Glyphs not covered by either class table default to offset <c>0</c>, which typically selects the zero row or column.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LeftClassTable"/>
    /// <seealso cref="RightClassTable"/>
    /// <seealso cref="KerningArray"/>
    public override short GetKerningValue(ushort left, ushort right)
    {
        int leftOffset = LeftClassTable.GetRawValue(left);
        int rightOffset = RightClassTable.GetRawValue(right);
        int index = (leftOffset + rightOffset) / 2;
        return (uint)index < (uint)KerningArray.Count ? KerningArray[index] : (short)0;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable.</param>
    /// <param name="context">Unused. The subtable resolves its class tables and kerning array through its own source.</param>
    /// <returns>The parsed format 2 subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, either class table, or the kerning array extends past the end of the subtable's declared length.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Class table offsets are resolved against the subtable-scoped source; the class tables' internal offsets are relative to the subtable's start, not the parent kern table's.</description></item>
    /// <item><description>The kerning array length is derived from the subtable's declared <see cref="KernSubtable.Length"/> minus the array's offset; the format does not carry an explicit entry count.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KernSubtable.Header"/>
    /// <seealso cref="Format2Header"/>
    /// <seealso cref="KernClassTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern format 2</seealso>
    static KernFormat2 IDerivedRecord<KernSubtable, KernFormat2>.Parse(
        ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        Format2Header body = cursor.ReadBigEndianStruct<Format2Header>();

        // Class table and array offsets are relative to the subtable start, which is
        // cursor.Source's base. Resolve them without arithmetic against that base.
        Source source = cursor.Source;
        KernClassTable leftClass = source.ParseRecordAt<KernClassTable>(body.LeftClassOffset);
        KernClassTable rightClass = source.ParseRecordAt<KernClassTable>(body.RightClassOffset);

        // The kerning array extends from its declared offset to the end of the subtable.
        int entryCount = (header.Length - body.KerningArrayOffset) / sizeof(short);
        short[] array = source.ReadInt16ArrayAt(body.KerningArrayOffset, entryCount);

        return new KernFormat2
        {
            Version = header.Version,
            Length = header.Length,
            Coverage = header.Coverage,
            RowWidth = body.RowWidth,
            LeftClassTable = leftClass,
            RightClassTable = rightClass,
            KerningArray = array,
        };
    }

    /// <summary>The 8-byte format 2 body header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All three offsets are measured from the start of the subtable, not from the body header itself.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">kern format 2</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="KernFormat2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern format 2</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Format2Header : IBigEndianStruct<Format2Header>
    {
        /// <summary>Gets the width of one row of the kerning array, in bytes.</summary>
        /// <value>The row stride of the kerning array.</value>
        /// <seealso cref="LeftClassOffset"/>
        public ushort RowWidth;            // +0

        /// <summary>Gets the offset to the left-hand class table.</summary>
        /// <value>The byte offset of the left <see cref="KernClassTable"/> from the subtable start.</value>
        /// <seealso cref="RightClassOffset"/>
        public ushort LeftClassOffset;     // +2  (offset from subtable start)

        /// <summary>Gets the offset to the right-hand class table.</summary>
        /// <value>The byte offset of the right <see cref="KernClassTable"/> from the subtable start.</value>
        /// <seealso cref="LeftClassOffset"/>
        public ushort RightClassOffset;    // +4  (offset from subtable start)

        /// <summary>Gets the offset to the kerning array.</summary>
        /// <value>The byte offset of the kerning array from the subtable start.</value>
        /// <seealso cref="RowWidth"/>
        public ushort KerningArrayOffset;  // +6  (offset from subtable start)

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All four fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Format2Header ReverseEndianness(Format2Header v) => new()
        {
            RowWidth = BinaryPrimitives.ReverseEndianness(v.RowWidth),
            LeftClassOffset = BinaryPrimitives.ReverseEndianness(v.LeftClassOffset),
            RightClassOffset = BinaryPrimitives.ReverseEndianness(v.RightClassOffset),
            KerningArrayOffset = BinaryPrimitives.ReverseEndianness(v.KerningArrayOffset),
        };
    }
}

/// <summary>A <c>kern</c> subtable in an unrecognized format. The header is preserved; the payload is skipped via the declared length.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The type is a fallback for format values the parser does not recognize; the concrete data is not exposed, only the common subtable header fields.</description></item>
/// <item><description>Preserving the header lets callers detect the presence of unknown-format subtables without forcing the parser to reject the font.</description></item>
/// <item><description><see cref="GetKerningValue(ushort, ushort)"/> always returns <c>0</c> because the payload is not decoded.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="KernSubtable"/>
/// <seealso cref="KernSubtable.Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/kern">OpenType specification: kern table</seealso>
public sealed record KernUnknown
    : KernSubtable,
      IBigEndianHeaderDerivedRecord<KernSubtable, KernUnknown, KernSubtable.Header>
{
    /// <inheritdoc/>
    /// <param name="left">The left-hand glyph ID.</param>
    /// <param name="right">The right-hand glyph ID.</param>
    /// <returns>Always <c>0</c>; the payload of an unknown-format subtable is not decoded.</returns>
    /// <remarks>A shaper that encounters an unknown-format subtable has no way to interpret it; the safe default is no kerning contribution.</remarks>
    /// <seealso cref="KernSubtable.GetKerningValue(ushort, ushort)"/>
    public override short GetKerningValue(ushort left, ushort right) => 0;

    /// <inheritdoc/>
    /// <param name="header">The already-read header.</param>
    /// <param name="context">Unused.</param>
    /// <returns>A new instance populated from the header.</returns>
    /// <remarks>The parser resets the cursor to the header's declared length after this returns, so the payload bytes are skipped without being read.</remarks>
    static KernUnknown IHeaderDerivedRecord<KernSubtable, KernUnknown, Header>.FromHeader(
        in Header header, object? context) => new()
        {
            Version = header.Version,
            Length = header.Length,
            Coverage = header.Coverage,
        };
}
