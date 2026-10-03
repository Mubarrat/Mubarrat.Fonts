using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.OpenType.Tables.Metadata;

/// <summary>The <c>VDMX</c> table: vertical device metrics. Supplies precomputed yMax and yMin values at selected ppem sizes, for fonts whose TrueType instructions produce heights that differ from the scaled and rounded bounding box.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is organized as a set of aspect-ratio groupings. Each grouping maps to a VDMX group containing records at specific pixel heights.</description></item>
/// <item><description>A grouping with all three ratio fields zero acts as the default and applies when no more specific ratio matches.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">VDMX table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="VdmxRatioRange"/>
/// <seealso cref="VdmxGroup"/>
/// <seealso cref="VdmxRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">OpenType specification: VDMX table</seealso>
public sealed record VdmxTable : IOpenTypeTable<VdmxTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IOpenTypeTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">OpenType specification: VDMX table</seealso>
    public static Tag Tag => "VDMX";

    /// <summary>Gets the table version: 0 or 1.</summary>
    /// <value>The constant <c>0</c> or <c>1</c> for a conforming VDMX table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx"><c>version</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="NumRecs"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">OpenType specification: <c>version</c></seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the number of VDMX groups declared in the header.</summary>
    /// <value>Informational; the actual group count is <see cref="RatioRanges"/>.Count, which the parser reads from the header's <c>numRatios</c> field.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx"><c>numRecs</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="RatioRanges"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">OpenType specification: <c>numRecs</c></seealso>
    public ushort NumRecs { get; init; }

    /// <summary>Gets the aspect-ratio groupings, in table order.</summary>
    /// <value>The ordered list of <see cref="VdmxRatioRange"/> entries. Each entry corresponds to a <see cref="VdmxGroup"/> at the same index in <see cref="Groups"/>.</value>
    /// <seealso cref="Groups"/>
    /// <seealso cref="RatioCount"/>
    /// <seealso cref="GetGroupForRatio(int, int)"/>
    public IReadOnlyList<VdmxRatioRange> RatioRanges { get; init; } = [];

    /// <summary>Gets the VDMX groups, one per ratio range, in <see cref="RatioRanges"/> order.</summary>
    /// <value>The ordered list of <see cref="VdmxGroup"/> entries. Entry <c>i</c> is the group for the ratio range at position <c>i</c> in <see cref="RatioRanges"/>.</value>
    /// <seealso cref="RatioRanges"/>
    /// <seealso cref="VdmxGroup"/>
    public IReadOnlyList<VdmxGroup> Groups { get; init; } = [];

    /// <summary>Gets the number of ratio ranges.</summary>
    /// <value>The size of the <see cref="RatioRanges"/> list.</value>
    /// <seealso cref="RatioRanges"/>
    public int RatioCount => RatioRanges.Count;

    /// <summary>Returns the group that applies to the given aspect ratio, or <c>null</c> when no ratio matches. A <c>(0, 0, 0)</c> ratio range acts as the default and is used when encountered during the search.</summary>
    /// <param name="xRatio">The x aspect-ratio numerator.</param>
    /// <param name="yRatio">The y aspect-ratio numerator.</param>
    /// <returns>The matching <see cref="VdmxGroup"/>, or <c>null</c> when no ratio range covers the requested ratio.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup is linear over <see cref="RatioRanges"/>. The specification orders ranges so that specific ratios precede the default; the loop returns the first match.</description></item>
    /// <item><description>A default ratio range <c>(0, 0, 0)</c> is returned the moment it is encountered, without testing subsequent ranges; this matches the specification's "first default wins" convention.</description></item>
    /// <item><description>When the ratio list contains no matching range and no default, the method returns <c>null</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// VdmxGroup? group = vdmx.GetGroupForRatio(1, 1);   // square pixels
    /// VdmxRecord? rec = group?.GetRecord(12);           // at 12 ppem
    /// </code>
    /// </example>
    /// <seealso cref="RatioRanges"/>
    /// <seealso cref="Groups"/>
    /// <seealso cref="VdmxGroup.GetRecord(int)"/>
    public VdmxGroup? GetGroupForRatio(int xRatio, int yRatio)
    {
        for (int i = 0; i < RatioRanges.Count; i++)
        {
            var r = RatioRanges[i];
            if (r.XRatio == 0 && r.YStartRatio == 0 && r.YEndRatio == 0)
                return i < Groups.Count ? Groups[i] : null;

            if (xRatio == r.XRatio && yRatio >= r.YStartRatio && yRatio <= r.YEndRatio)
                return i < Groups.Count ? Groups[i] : null;
        }
        return null;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the VDMX table.</param>
    /// <param name="context">Unused. The table resolves its groups through its own source.</param>
    /// <returns>The parsed VDMX table.</returns>
    /// <exception cref="InvalidDataException">The version is not 0 or 1, or the table declares zero ratio ranges.</exception>
    /// <exception cref="EndOfStreamException">The header, ratio-range array, group-offset array, or any group extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The ratio-range array is fixed-layout and read through <see cref="Cursor.ReadStructArray{T}(int)"/>; its entries are byte-only so no endianness reversal is needed.</description></item>
    /// <item><description>The group array is read through <see cref="Cursor.ReadOffset16ArrayPeekRecord{T}(int, object?)"/>, which resolves each offset to a fully-parsed <see cref="VdmxGroup"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">VDMX table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="VdmxRatioRange"/>
    /// <seealso cref="VdmxGroup"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">OpenType specification: VDMX table</seealso>
    public static VdmxTable Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version is not (0 or 1))
            throw new InvalidDataException(
                $"'VDMX'.version is {header.Version}, expected 0 or 1.");

        if (header.NumRatios == 0)
            throw new InvalidDataException("'VDMX' has no ratio ranges.");

        var ratios = cursor.ReadStructArray<VdmxRatioRange>(header.NumRatios);
        var groups = cursor.ReadOffset16ArrayPeekRecord<VdmxGroup>(header.NumRatios);

        return new VdmxTable
        {
            Version = header.Version,
            NumRecs = header.NumRecs,
            RatioRanges = ratios,
            Groups = groups,
        };
    }

    /// <summary>The 6-byte <c>VDMX</c> table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed by <c>numRatios</c> 4-byte <see cref="VdmxRatioRange"/> entries and then by <c>numRatios</c> Offset16 group references.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">VDMX header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="VdmxTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">OpenType specification: VDMX header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the table version: 0 or 1.</summary>
        /// <value>The constant <c>0</c> or <c>1</c> for a conforming VDMX table.</value>
        /// <seealso cref="NumRecs"/>
        /// <seealso cref="NumRatios"/>
        public ushort Version;     // +0

        /// <summary>Gets the number of VDMX groups declared in the header.</summary>
        /// <value>A 16-bit count; informational only, since the actual group count comes from <see cref="NumRatios"/>.</value>
        /// <seealso cref="NumRatios"/>
        public ushort NumRecs;     // +2

        /// <summary>Gets the number of ratio ranges and, correspondingly, the number of groups.</summary>
        /// <value>The count of ratio-range entries and of group-offset entries that follow the header.</value>
        /// <seealso cref="NumRecs"/>
        public ushort NumRatios;   // +4

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            NumRecs = BinaryPrimitives.ReverseEndianness(v.NumRecs),
            NumRatios = BinaryPrimitives.ReverseEndianness(v.NumRatios),
        };
    }
}

/// <summary>One aspect-ratio grouping. Blittable, size 4.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each range is index-aligned with the groups it applies to: range <c>i</c> selects the group at position <c>i</c> in the VDMX table's group array.</description></item>
/// <item><description>The special triple <c>(xRatio, yStartRatio, yEndRatio) == (0, 0, 0)</c> marks the default grouping that applies when no more specific ratio matches.</description></item>
/// <item><description>Every field is a single byte, so no endianness reversal is needed; the type deliberately does not implement <see cref="IBigEndianStruct{T}"/>.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">VDMX ratio ranges</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="VdmxTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">OpenType specification: VDMX ratio ranges</seealso>
public record struct VdmxRatioRange
{
    /// <summary>Gets the character set code. 0 means "no subset".</summary>
    /// <value>The character subset code, or <c>0</c> for the full character set.</value>
    /// <seealso cref="XRatio"/>
    public byte CharSet;

    /// <summary>Gets the x-ratio numerator.</summary>
    /// <value>The x aspect-ratio numerator; the denominator is implicit and paired with <see cref="YStartRatio"/> and <see cref="YEndRatio"/> during lookup.</value>
    /// <seealso cref="CharSet"/>
    /// <seealso cref="YStartRatio"/>
    public byte XRatio;

    /// <summary>Gets the y-ratio range start.</summary>
    /// <value>The inclusive lower bound of the y-ratio range covered by this grouping.</value>
    /// <seealso cref="YEndRatio"/>
    public byte YStartRatio;

    /// <summary>Gets the y-ratio range end.</summary>
    /// <value>The inclusive upper bound of the y-ratio range covered by this grouping.</value>
    /// <seealso cref="YStartRatio"/>
    public byte YEndRatio;
}

/// <summary>A group of VDMX records covering a range of pixel heights.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Records are sorted by ascending <see cref="VdmxRecord.YPelHeight"/>; the parser relies on that ordering for the binary search in <see cref="GetRecord(int)"/>.</description></item>
/// <item><description>The group's <see cref="StartSize"/> and <see cref="EndSize"/> give the covered range, but the authoritative data is the per-record <see cref="VdmxRecord.YPelHeight"/> value; the two should agree.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">VDMX groups</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="VdmxTable"/>
/// <seealso cref="VdmxRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">OpenType specification: VDMX groups</seealso>
public sealed record VdmxGroup : IRecord<VdmxGroup>
{
    /// <summary>Gets the lowest yPelHeight covered by this group.</summary>
    /// <value>The inclusive lower bound of the pixel-height range covered by this group.</value>
    /// <seealso cref="EndSize"/>
    /// <seealso cref="Records"/>
    public byte StartSize { get; init; }

    /// <summary>Gets the highest yPelHeight covered by this group.</summary>
    /// <value>The inclusive upper bound of the pixel-height range covered by this group.</value>
    /// <seealso cref="StartSize"/>
    /// <seealso cref="Records"/>
    public byte EndSize { get; init; }

    /// <summary>Gets the height records, sorted by increasing <see cref="VdmxRecord.YPelHeight"/>.</summary>
    /// <value>The ordered list of <see cref="VdmxRecord"/> entries; sorted ascending by <see cref="VdmxRecord.YPelHeight"/>.</value>
    /// <seealso cref="GetRecord(int)"/>
    /// <seealso cref="VdmxRecord"/>
    public IReadOnlyList<VdmxRecord> Records { get; init; } = [];

    /// <summary>Returns the record for a specific pixel height, or <c>null</c>.</summary>
    /// <param name="yPelHeight">The pixel height to look up.</param>
    /// <returns>The <see cref="VdmxRecord"/> for the requested height, or <c>null</c> when no record covers it.</returns>
    /// <remarks>The lookup uses binary search over <see cref="Records"/>, which the specification requires to be sorted by ascending pixel height.</remarks>
    /// <example>
    /// <code>
    /// VdmxRecord? rec = group.GetRecord(12);
    /// if (rec is { } r) DrawWithMetrics(r.YMax, r.YMin);
    /// </code>
    /// </example>
    /// <seealso cref="Records"/>
    /// <seealso cref="VdmxRecord.YPelHeight"/>
    public VdmxRecord? GetRecord(int yPelHeight)
    {
        int lo = 0, hi = Records.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            int cmp = Records[mid].YPelHeight - yPelHeight;
            if (cmp == 0) return Records[mid];
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }
        return null;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the VDMX group.</param>
    /// <param name="context">Unused. The group is self-describing.</param>
    /// <returns>The parsed group with its record array materialized.</returns>
    /// <exception cref="EndOfStreamException">The header or record array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">VDMX groups</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="VdmxRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">OpenType specification: VDMX groups</seealso>
    public static VdmxGroup Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new VdmxGroup
        {
            StartSize = header.StartSize,
            EndSize = header.EndSize,
            Records = cursor.ReadBigEndianStructArray<VdmxRecord>(header.Recs),
        };
    }

    /// <summary>The 4-byte <c>VDMX</c> group header: record count, start pixel height, end pixel height. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by <c>recs</c> 6-byte <see cref="VdmxRecord"/> entries.</description></item>
    /// <item><description>Only the <see cref="Recs"/> field is multi-byte; the two pixel-height fields are single bytes and pass through <see cref="ReverseEndianness(Header)"/> unchanged.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">VDMX groups</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="VdmxGroup"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">OpenType specification: VDMX groups</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the number of VDMX records that follow the header.</summary>
        /// <value>The count of <see cref="VdmxRecord"/> entries.</value>
        /// <seealso cref="StartSize"/>
        /// <seealso cref="EndSize"/>
        public ushort Recs;       // +0

        /// <summary>Gets the lowest yPelHeight covered by this group.</summary>
        /// <value>The inclusive lower bound of the pixel-height range.</value>
        /// <seealso cref="EndSize"/>
        /// <seealso cref="Recs"/>
        public byte StartSize;    // +2

        /// <summary>Gets the highest yPelHeight covered by this group.</summary>
        /// <value>The inclusive upper bound of the pixel-height range.</value>
        /// <seealso cref="StartSize"/>
        /// <seealso cref="Recs"/>
        public byte EndSize;      // +3

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte field is to be reversed.</param>
        /// <returns>A new header with the record count reversed; the two byte fields are copied through.</returns>
        /// <remarks>Only <see cref="Recs"/> is multi-byte; the two pixel-height fields are single bytes and require no reversal.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => v with
        {
            Recs = BinaryPrimitives.ReverseEndianness(v.Recs)
        };
    }
}

/// <summary>A single VDMX record: yMax and yMin at a specific pixel height. Blittable, size 6.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The y values are in pixels, not design units; they reflect the post-instruction result of the glyph rendering at the record's pixel height.</description></item>
/// <item><description>Records within a group must be sorted by ascending <see cref="YPelHeight"/> so the group's binary search can locate them.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">VDMX records</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="VdmxGroup"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vdmx">OpenType specification: VDMX records</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VdmxRecord : IBigEndianStruct<VdmxRecord>
{
    /// <summary>Pixel height this record applies to.</summary>
    /// <value>The pixels-per-em size at which the yMax and yMin values apply.</value>
    /// <seealso cref="YMax"/>
    /// <seealso cref="YMin"/>
    public ushort YPelHeight;   // +0

    /// <summary>Maximum y value in pixels at this height.</summary>
    /// <value>The highest pixel coordinate reached by any glyph at the given height, after hinting.</value>
    /// <seealso cref="YMin"/>
    /// <seealso cref="YPelHeight"/>
    public short YMax;          // +2

    /// <summary>Minimum y value in pixels at this height.</summary>
    /// <value>The lowest pixel coordinate reached by any glyph at the given height, after hinting. Typically negative, since it lies below the baseline.</value>
    /// <seealso cref="YMax"/>
    /// <seealso cref="YPelHeight"/>
    public short YMin;          // +4

    /// <inheritdoc/>
    /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new record with each multi-byte field reversed.</returns>
    /// <remarks>The pixel-height field uses <see cref="BinaryPrimitives.ReverseEndianness(ushort)"/>; the two signed y values use <see cref="BinaryPrimitives.ReverseEndianness(short)"/>.</remarks>
    /// <seealso cref="IBigEndianStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    public static VdmxRecord ReverseEndianness(VdmxRecord v) => new()
    {
        YPelHeight = BinaryPrimitives.ReverseEndianness(v.YPelHeight),
        YMax = BinaryPrimitives.ReverseEndianness(v.YMax),
        YMin = BinaryPrimitives.ReverseEndianness(v.YMin),
    };
}
