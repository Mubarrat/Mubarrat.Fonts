using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>gasp</c> table: Grid-fitting and Scan-conversion Procedure. Declares the rasterizer behavior the font designer intends at different pixel sizes.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is a short list of <c>(maxPPEM, behavior)</c> pairs. To look up the behavior at a given ppem, find the first range whose upper limit is at least that ppem.</description></item>
/// <item><description>Two versions are defined: version 0 (original TrueType) and version 1 (adds <see cref="GaspBehavior.SymmetricGridFit"/> and <see cref="GaspBehavior.SymmetricSmoothing"/> for ClearType).</description></item>
/// <item><description>The table is advisory: a renderer may ignore it and use its own heuristics, but honoring it improves the font's appearance across the range of sizes the designer tuned.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">gasp table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GaspRange"/>
/// <seealso cref="GaspBehavior"/>
/// <seealso cref="GetBehavior(int)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: gasp table</seealso>
public sealed record GaspTable : IFontTable<GaspTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: gasp table</seealso>
    public static Tag Tag => "gasp";

    /// <summary>Gets the table version: 0 or 1.</summary>
    /// <value>The constant <c>0</c> for the original TrueType definition, or <c>1</c> for the version that supports ClearType flags.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp"><c>version</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Ranges"/>
    /// <seealso cref="GaspBehavior.SymmetricGridFit"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: <c>version</c></seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the ranges.</summary>
    /// <value>The ordered list of <see cref="GaspRange"/> entries. Ranges are sorted by ascending <see cref="GaspRange.MaxPPEM"/>, and the last range typically has an upper limit of <c>0xFFFF</c>.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Ranges are cumulative from the low end: a range covers every ppem up to and including <see cref="GaspRange.MaxPPEM"/>, except for those claimed by an earlier range.</description></item>
    /// <item><description>The specification requires the ranges to be sorted ascending and non-overlapping.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetBehavior(int)"/>
    /// <seealso cref="RangeCount"/>
    /// <seealso cref="GaspRange"/>
    public IReadOnlyList<GaspRange> Ranges { get; init; } = [];

    /// <summary>Gets the number of ranges.</summary>
    /// <value>The size of the <see cref="Ranges"/> list.</value>
    /// <seealso cref="Ranges"/>
    public int RangeCount => Ranges.Count;

    /// <summary>Returns the behavior that applies at <paramref name="ppem"/>.</summary>
    /// <param name="ppem">The pixels-per-em size to look up.</param>
    /// <returns>The <see cref="GaspBehavior"/> flags for the first range whose <see cref="GaspRange.MaxPPEM"/> is at least <paramref name="ppem"/>, or <see cref="GaspBehavior.None"/> when no range covers it.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup is linear over <see cref="Ranges"/>. Ranges are typically few (two or three), so the linear scan is not usually a concern.</description></item>
    /// <item><description>Returns <see cref="GaspBehavior.None"/> when the ppem exceeds every declared range; a conforming font typically declares a final range ending at <c>0xFFFF</c> to avoid this fallback.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// GaspBehavior behavior = gasp.GetBehavior(12);
    /// bool hinting = behavior.HasFlag(GaspBehavior.GridFit);
    /// </code>
    /// </example>
    /// <seealso cref="Ranges"/>
    /// <seealso cref="GaspBehavior"/>
    /// <seealso cref="GaspRange.MaxPPEM"/>
    public GaspBehavior GetBehavior(int ppem)
    {
        foreach (var r in Ranges)
            if (ppem <= r.MaxPPEM) return r.Behavior;
        return GaspBehavior.None;
    }

    /// <summary>The 4-byte <c>gasp</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by <c>numRanges</c> 4-byte <see cref="GaspRange"/> records.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">gasp header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GaspTable"/>
    /// <seealso cref="GaspRange"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: gasp header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the table version: 0 or 1.</summary>
        /// <value>The constant <c>0</c> for the original TrueType definition, or <c>1</c> for the version that supports ClearType flags.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp"><c>version</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="NumRanges"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: <c>version</c></seealso>
        public ushort Version;

        /// <summary>Gets the number of ranges that follow the header.</summary>
        /// <value>The count of <see cref="GaspRange"/> records.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp"><c>numRanges</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Version"/>
        /// <seealso cref="GaspRange"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: <c>numRanges</c></seealso>
        public ushort NumRanges;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: gasp header</seealso>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(value.Version),
            NumRanges = BinaryPrimitives.ReverseEndianness(value.NumRanges),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the gasp table.</param>
    /// <param name="context">Unused. The gasp table is self-contained.</param>
    /// <returns>The parsed gasp table.</returns>
    /// <exception cref="EndOfStreamException">The header or range array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The range array length is taken from <see cref="Header.NumRanges"/>; the reader consumes exactly that many 4-byte records.</description></item>
    /// <item><description>The current parser does not validate the version against the range count, nor does it reject version-1-only behavior flags on a version-0 table. Malformed inputs are accepted as-is.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">gasp table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="GaspRange"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: gasp table</seealso>
    static GaspTable IRecord<GaspTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new() { Version = header.Version, Ranges = cursor.ReadBigEndianStructArray<GaspRange>(header.NumRanges) };
    }
}

/// <summary>A single <c>gasp</c> range: an upper ppem bound plus the rasterizer behavior to use up to that size.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Ranges are cumulative from the low end of the ppem scale. A range covers every ppem from just above the previous range's <see cref="MaxPPEM"/> up to and including its own <see cref="MaxPPEM"/>.</description></item>
/// <item><description>The specification requires ranges to be sorted ascending by <see cref="MaxPPEM"/> and to be non-overlapping.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">gasp ranges</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GaspTable"/>
/// <seealso cref="GaspBehavior"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: gasp ranges</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct GaspRange : IEndianReversibleStruct<GaspRange>
{
    /// <summary>Upper limit of the range, in ppem.</summary>
    /// <value>The inclusive upper bound of the ppem range; <c>0xFFFF</c> for the final, catch-all range.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp"><c>rangeMaxPPEM</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="RangeGaspBehavior"/>
    /// <seealso cref="Behavior"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: <c>rangeMaxPPEM</c></seealso>
    public ushort MaxPPEM;

    /// <summary>Bit flags describing the desired rasterizer behavior.</summary>
    /// <value>The raw 16-bit flag word; see <see cref="Behavior"/> for the decoded <see cref="GaspBehavior"/> value.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp"><c>rangeGaspBehavior</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MaxPPEM"/>
    /// <seealso cref="Behavior"/>
    /// <seealso cref="GaspBehavior"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: <c>rangeGaspBehavior</c></seealso>
    public ushort RangeGaspBehavior;

    /// <summary>Gets the decoded behavior flags.</summary>
    /// <value>The <see cref="RangeGaspBehavior"/> value reinterpreted as a <see cref="GaspBehavior"/> flag set.</value>
    /// <seealso cref="RangeGaspBehavior"/>
    /// <seealso cref="GaspBehavior"/>
    public readonly GaspBehavior Behavior => (GaspBehavior)RangeGaspBehavior;

    /// <inheritdoc/>
    /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
    /// <returns>A new range with both fields reversed.</returns>
    /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: gasp ranges</seealso>
    public static GaspRange ReverseEndianness(GaspRange value) => new()
    {
        MaxPPEM = BinaryPrimitives.ReverseEndianness(value.MaxPPEM),
        RangeGaspBehavior = BinaryPrimitives.ReverseEndianness(value.RangeGaspBehavior),
    };
}

/// <summary>Bit flags for a <c>gasp</c> range's behavior field. The on-disk representation is a 16-bit word; this enum mirrors it.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><see cref="GridFit"/>, <see cref="DoGray"/>, and combinations of those two are valid in both table versions. The remaining two flags are version-1 additions for ClearType rendering.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp"><c>rangeGaspBehavior</c> flags</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GaspRange.Behavior"/>
/// <seealso cref="GaspRange.RangeGaspBehavior"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: <c>rangeGaspBehavior</c> flags</seealso>
[Flags]
public enum GaspBehavior : ushort
{
    /// <summary>No behavior flags set.</summary>
    /// <remarks>The renderer is left to choose its own behavior for the range. Valid on both versions.</remarks>
    None = 0,

    /// <summary>Use gridfitting (hinting).</summary>
    /// <remarks>Requests that the renderer apply the font's hinting instructions at this size range. Valid on both versions. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp"><c>GASP_GRIDFIT</c> flag</see>.</remarks>
    /// <seealso cref="SymmetricGridFit"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: <c>GASP_GRIDFIT</c></seealso>
    GridFit = 0x0001,

    /// <summary>Use grayscale rendering.</summary>
    /// <remarks>Requests grayscale antialiasing at this size range rather than bilevel rendering. Valid on both versions. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp"><c>GASP_DOGRAY</c> flag</see>.</remarks>
    /// <seealso cref="SymmetricSmoothing"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: <c>GASP_DOGRAY</c></seealso>
    DoGray = 0x0002,

    /// <summary>Gridfit with ClearType symmetric smoothing. Version 1 only.</summary>
    /// <remarks>Valid only on version-1 tables; on a version-0 table this bit is reserved and should be zero. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp"><c>GASP_SYMMETRIC_GRIDFIT</c> flag</see>.</remarks>
    /// <seealso cref="GridFit"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: <c>GASP_SYMMETRIC_GRIDFIT</c></seealso>
    SymmetricGridFit = 0x0004,

    /// <summary>Smoothing along multiple axes with ClearType. Version 1 only.</summary>
    /// <remarks>Valid only on version-1 tables; on a version-0 table this bit is reserved and should be zero. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp"><c>GASP_SYMMETRIC_SMOOTHING</c> flag</see>.</remarks>
    /// <seealso cref="DoGray"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gasp">OpenType specification: <c>GASP_SYMMETRIC_SMOOTHING</c></seealso>
    SymmetricSmoothing = 0x0008,

    /// <summary>Reserved bits.</summary>
    /// <remarks>Bits 4–15 are reserved and must be zero in conforming fonts.</remarks>
    Reserved = 0xFFF0,
}
