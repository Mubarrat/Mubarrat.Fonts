using Mubarrat.Fonts.OpenType.Binary;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using static Mubarrat.Fonts.OpenType.Tables.Layout.Coverage;

namespace Mubarrat.Fonts.OpenType.Tables.Layout;

/// <summary>A Coverage table: identifies covered glyphs and assigns each a Coverage Index.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Coverage tables are the primary mechanism by which OpenType layout subtables select the glyphs they apply to. Every covered glyph receives a stable Coverage Index starting at 0.</description></item>
/// <item><description>Two formats exist. Format 1 stores an explicit sorted array of glyph IDs; format 2 stores ranges of glyph IDs with the starting Coverage Index of each range.</description></item>
/// <item><description>Coverage Index values are used throughout the layout tables as positions into parallel arrays: for example, the rule set at index <c>i</c> applies to the glyph whose coverage index is <c>i</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-table">Coverage table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="CoverageFormat1"/>
/// <seealso cref="CoverageFormat2"/>
/// <seealso cref="RangeRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-table">OpenType specification: Coverage table</seealso>
public abstract record Coverage : IRecord<Coverage>, IBaseRecord<Coverage>
{
    /// <summary>Gets the number of covered glyphs.</summary>
    /// <value>The count of glyphs the table covers. Coverage Index values range over <c>[0, <see cref="GlyphCount"/>)</c>.</value>
    /// <seealso cref="GetCoverageIndex(int)"/>
    public abstract int GlyphCount { get; }

    /// <summary>Returns the Coverage Index for a glyph, or -1 when the glyph is not covered.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The Coverage Index for the glyph, or <c>-1</c> when the glyph is not covered by this table.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both format implementations return <c>-1</c> for uncovered glyphs; a caller that uses the result as an array index must check the return value first.</description></item>
    /// <item><description>Coverage Index values are stable for a given table; the same glyph always maps to the same index.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CoverageFormat1.GetCoverageIndex(int)"/>
    /// <seealso cref="CoverageFormat2.GetCoverageIndex(int)"/>
    /// <seealso cref="GlyphCount"/>
    public abstract int GetCoverageIndex(int glyphId);

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the Coverage table.</param>
    /// <param name="context">Context forwarded to the format-specific parser; usually unused.</param>
    /// <returns>The format-specific Coverage table.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1 or 2.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-table">Coverage table</see> in the OpenType specification.</remarks>
    /// <seealso cref="IBaseRecord{TBase}"/>
    /// <seealso cref="CoverageFormat1"/>
    /// <seealso cref="CoverageFormat2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-table">OpenType specification: Coverage table</seealso>
    static Coverage IRecord<Coverage>.Parse(ref Cursor cursor, object? context)
    {
        ushort format = cursor.ReadUInt16();
        return format switch
        {
            1 => IBaseRecord<Coverage>.Parse<CoverageFormat1>(ref cursor, context),
            2 => IBaseRecord<Coverage>.Parse<CoverageFormat2>(ref cursor, context),
            _ => throw new InvalidDataException($"Coverage format {format} is not defined."),
        };
    }

    /// <summary>Fixed-layout range record. Blittable, size 6, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Used only by <see cref="CoverageFormat2"/>; format 1 stores raw glyph IDs instead of range records.</description></item>
    /// <item><description>Ranges in a format-2 table must be sorted ascending by <see cref="StartGlyphId"/> and must not overlap.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2">Coverage Format 2</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="CoverageFormat2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2">OpenType specification: Coverage Format 2</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct RangeRecord : IBigEndianStruct<RangeRecord>
    {
        /// <summary>Gets the first glyph ID in the range.</summary>
        /// <value>The inclusive lower bound of the glyph ID range.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2"><c>startGlyphID</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="EndGlyphId"/>
        /// <seealso cref="StartCoverageIndex"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2">OpenType specification: <c>startGlyphID</c></seealso>
        public ushort StartGlyphId;   // +0

        /// <summary>Gets the last glyph ID in the range (inclusive).</summary>
        /// <value>The inclusive upper bound of the glyph ID range.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2"><c>endGlyphID</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="StartGlyphId"/>
        /// <seealso cref="StartCoverageIndex"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2">OpenType specification: <c>endGlyphID</c></seealso>
        public ushort EndGlyphId;     // +2

        /// <summary>Gets the starting Coverage Index for the range.</summary>
        /// <value>The Coverage Index assigned to <see cref="StartGlyphId"/>. Glyph <c>StartGlyphId + k</c> gets index <c>StartCoverageIndex + k</c>.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2"><c>startCoverageIndex</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="StartGlyphId"/>
        /// <seealso cref="EndGlyphId"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2">OpenType specification: <c>startCoverageIndex</c></seealso>
        public ushort StartCoverageIndex;   // +4

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new range with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2">OpenType specification: Coverage Format 2</seealso>
        public static RangeRecord ReverseEndianness(RangeRecord v) => new()
        {
            StartGlyphId = BinaryPrimitives.ReverseEndianness(v.StartGlyphId),
            EndGlyphId = BinaryPrimitives.ReverseEndianness(v.EndGlyphId),
            StartCoverageIndex = BinaryPrimitives.ReverseEndianness(v.StartCoverageIndex),
        };
    }
}

/// <summary>Coverage format 1: a sorted array of glyph IDs.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each entry's Coverage Index is its position in the array. Lookup is a binary search over the sorted array.</description></item>
/// <item><description>Format 1 is preferred when the covered glyphs are few or sparse; format 2 is preferred when they form dense contiguous runs.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-1">Coverage Format 1</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Coverage"/>
/// <seealso cref="CoverageFormat2"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-1">OpenType specification: Coverage Format 1</seealso>
public sealed record CoverageFormat1 : Coverage, IDerivedRecord<Coverage, CoverageFormat1>
{
    /// <inheritdoc/>
    /// <value>The size of <see cref="GlyphArray"/>.</value>
    /// <seealso cref="GlyphArray"/>
    public override int GlyphCount => GlyphArray.Length;

    /// <summary>Gets the sorted array of covered glyph IDs.</summary>
    /// <value>The glyph IDs in ascending order. A glyph's Coverage Index is its position in this array.</value>
    /// <seealso cref="GlyphCount"/>
    /// <seealso cref="GetCoverageIndex(int)"/>
    public ushort[] GlyphArray { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The glyph's position in <see cref="GlyphArray"/>, or <c>-1</c> when the glyph is not present.</returns>
    /// <remarks>The lookup uses binary search over <see cref="GlyphArray"/>, which the specification requires to be sorted ascending.</remarks>
    /// <seealso cref="GlyphArray"/>
    public override int GetCoverageIndex(int glyphId)
    {
        int lo = 0, hi = GlyphArray.Length - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            int c = GlyphArray[mid] - glyphId;
            if (c == 0) return mid;
            if (c < 0) lo = mid + 1;
            else hi = mid - 1;
        }
        return -1;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-1 body (after the format discriminant).</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed format-1 Coverage table.</returns>
    /// <exception cref="EndOfStreamException">The glyph count or array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-1">Coverage Format 1</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphArray"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-1">OpenType specification: Coverage Format 1</seealso>
    static CoverageFormat1 IDerivedRecord<Coverage, CoverageFormat1>.Parse(ref Cursor cursor, object? context) => new() { GlyphArray = cursor.ReadUInt16Array(cursor.ReadUInt16()) };
}

/// <summary>Coverage format 2: glyph ID ranges with starting Coverage Indices.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each range record covers a contiguous glyph ID span and assigns consecutive Coverage Index values starting from <see cref="RangeRecord.StartCoverageIndex"/>.</description></item>
/// <item><description>Ranges are sorted ascending by <see cref="RangeRecord.StartGlyphId"/> and must not overlap, allowing <see cref="GetCoverageIndex(int)"/> to use binary search.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2">Coverage Format 2</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Coverage"/>
/// <seealso cref="CoverageFormat1"/>
/// <seealso cref="RangeRecord"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2">OpenType specification: Coverage Format 2</seealso>
public sealed record CoverageFormat2 : Coverage, IDerivedRecord<Coverage, CoverageFormat2>
{
    /// <summary>Gets the glyph ID ranges.</summary>
    /// <value>The ordered array of <see cref="RangeRecord"/> entries. Ranges are sorted ascending by <see cref="RangeRecord.StartGlyphId"/> and do not overlap.</value>
    /// <seealso cref="RangeRecord"/>
    /// <seealso cref="GetCoverageIndex(int)"/>
    public RangeRecord[] RangeRecords { get; init; } = [];

    /// <inheritdoc/>
    /// <value>The Coverage Index just past the last entry of the final range: <c>StartCoverageIndex + (EndGlyphId - StartGlyphId) + 1</c>.</value>
    /// <remarks>The count is derived from the last range record; it is not stored explicitly in the table.</remarks>
    /// <seealso cref="RangeRecords"/>
    public override int GlyphCount
    {
        get
        {
            if (RangeRecords.Length != 0)
            {
                ref readonly RangeRecord lastRange = ref RangeRecords[^1];
                return lastRange.StartCoverageIndex + (lastRange.EndGlyphId - lastRange.StartGlyphId) + 1;
            }
            return 0;
        }
    }

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The Coverage Index for the glyph, or <c>-1</c> when no range covers it.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup uses binary search over <see cref="RangeRecords"/>, which the specification requires to be sorted by <see cref="RangeRecord.StartGlyphId"/>.</description></item>
    /// <item><description>Within the matching range, the index is <c>StartCoverageIndex + (glyphId - StartGlyphId)</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="RangeRecords"/>
    /// <seealso cref="RangeRecord"/>
    public override int GetCoverageIndex(int glyphId)
    {
        int lo = 0, hi = RangeRecords.Length - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            ref readonly RangeRecord r = ref RangeRecords[mid];
            if (glyphId < r.StartGlyphId) hi = mid - 1;
            else if (glyphId > r.EndGlyphId) lo = mid + 1;
            else return r.StartCoverageIndex + (glyphId - r.StartGlyphId);
        }
        return -1;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-2 body (after the format discriminant).</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed format-2 Coverage table.</returns>
    /// <exception cref="EndOfStreamException">The count or range array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2">Coverage Format 2</see> in the OpenType specification.</remarks>
    /// <seealso cref="RangeRecords"/>
    /// <seealso cref="RangeRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2">OpenType specification: Coverage Format 2</seealso>
    static CoverageFormat2 IDerivedRecord<Coverage, CoverageFormat2>.Parse(ref Cursor cursor, object? context) => new() { RangeRecords = cursor.ReadBigEndianStructArray<RangeRecord>(cursor.ReadUInt16()) };
}
