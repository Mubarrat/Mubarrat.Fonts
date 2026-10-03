using Mubarrat.Fonts.OpenType.Binary;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using static Mubarrat.Fonts.OpenType.Tables.Layout.ClassDef;

namespace Mubarrat.Fonts.OpenType.Tables.Layout;

/// <summary>Class Definition: maps glyphs to class numbers, defaulting to 0.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Two formats exist. Format 1 assigns consecutive class values to a contiguous range of glyph IDs starting at a base glyph. Format 2 assigns a single class value to each of a list of glyph-ID ranges.</description></item>
/// <item><description>Format 1 stores its class values as an inline array; format 2 stores ranges as a sorted, non-overlapping array of <see cref="ClassRange"/> records.</description></item>
/// <item><description>A glyph that appears in no record is assigned class 0. This is the same default used for a glyph that falls outside the range of a format-1 table.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">Class Definition table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ClassDefFormat1"/>
/// <seealso cref="ClassDefFormat2"/>
/// <seealso cref="ClassRange"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">OpenType specification: Class Definition table</seealso>
public abstract record ClassDef : IRecord<ClassDef>, IBaseRecord<ClassDef>
{
    /// <summary>Returns the class for a glyph, or 0 when the glyph is not assigned one.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The class number for the glyph, or <c>0</c> when the glyph is not covered by the table.</returns>
    /// <remarks>Both format implementations use <c>0</c> as the default for uncovered glyphs, matching the specification.</remarks>
    /// <seealso cref="ClassDefFormat1.GetClass(int)"/>
    /// <seealso cref="ClassDefFormat2.GetClass(int)"/>
    public abstract int GetClass(int glyphId);

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the ClassDef table.</param>
    /// <param name="context">Context forwarded to the format-specific parser; usually unused.</param>
    /// <returns>The format-specific ClassDef.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1 or 2.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">Class Definition table</see> in the OpenType specification.</remarks>
    /// <seealso cref="IBaseRecord{TBase}"/>
    /// <seealso cref="ClassDefFormat1"/>
    /// <seealso cref="ClassDefFormat2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">OpenType specification: Class Definition table</seealso>
    static ClassDef IRecord<ClassDef>.Parse(ref Cursor cursor, object? context)
    {
        ushort format = cursor.ReadUInt16();
        return format switch
        {
            1 => IBaseRecord<ClassDef>.Parse<ClassDefFormat1>(ref cursor, context),
            2 => IBaseRecord<ClassDef>.Parse<ClassDefFormat2>(ref cursor, context),
            _ => throw new InvalidDataException($"ClassDef format {format} is not defined."),
        };
    }

    /// <summary>Fixed-layout class range record. Blittable, size 6, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Used only by <see cref="ClassDefFormat2"/>; format 1 stores raw class values instead of range records.</description></item>
    /// <item><description>Ranges in a format-2 table must be sorted ascending by <see cref="StartGlyphId"/> and must not overlap.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">Class Definition table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ClassDefFormat2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">OpenType specification: Class Definition table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct ClassRange : IBigEndianStruct<ClassRange>
    {
        /// <summary>Gets the first glyph ID in the range.</summary>
        /// <value>The inclusive lower bound of the glyph ID range that shares <see cref="Class"/>.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table"><c>startGlyphID</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="EndGlyphId"/>
        /// <seealso cref="Class"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">OpenType specification: <c>startGlyphID</c></seealso>
        public ushort StartGlyphId;   // +0

        /// <summary>Gets the last glyph ID in the range (inclusive).</summary>
        /// <value>The inclusive upper bound of the glyph ID range that shares <see cref="Class"/>.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table"><c>endGlyphID</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="StartGlyphId"/>
        /// <seealso cref="Class"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">OpenType specification: <c>endGlyphID</c></seealso>
        public ushort EndGlyphId;     // +2

        /// <summary>Gets the class value for the range.</summary>
        /// <value>The class number assigned to every glyph in <see cref="StartGlyphId"/>..<see cref="EndGlyphId"/>.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table"><c>class</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="StartGlyphId"/>
        /// <seealso cref="EndGlyphId"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">OpenType specification: <c>class</c></seealso>
        public ushort Class;   // +4

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new range with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">OpenType specification: Class Definition table</seealso>
        public static ClassRange ReverseEndianness(ClassRange v) => new()
        {
            StartGlyphId = BinaryPrimitives.ReverseEndianness(v.StartGlyphId),
            EndGlyphId = BinaryPrimitives.ReverseEndianness(v.EndGlyphId),
            Class = BinaryPrimitives.ReverseEndianness(v.Class),
        };
    }
}

/// <summary>ClassDef format 1: consecutive class values for a glyph range.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table names a starting glyph ID and provides one class value per consecutive glyph. The glyph <c>StartGlyphId + i</c> is assigned <c>ClassValues[i]</c>.</description></item>
/// <item><description>Glyphs below <see cref="StartGlyphId"/> or beyond the end of <see cref="ClassValues"/> default to class 0.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">Class Definition table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ClassDef"/>
/// <seealso cref="ClassDefFormat2"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">OpenType specification: Class Definition table</seealso>
public sealed record ClassDefFormat1 : ClassDef, IDerivedRecord<ClassDef, ClassDefFormat1>
{
    /// <summary>Gets the first glyph ID covered by the class-value array.</summary>
    /// <value>The glyph ID whose class is <c>ClassValues[0]</c>. Glyph IDs below this value default to class 0.</value>
    /// <seealso cref="ClassValues"/>
    public ushort StartGlyphId { get; init; }

    /// <summary>Gets the class value for each consecutive glyph starting at <see cref="StartGlyphId"/>.</summary>
    /// <value>The array of class values, one per glyph in the covered range.</value>
    /// <seealso cref="StartGlyphId"/>
    /// <seealso cref="GetClass(int)"/>
    public ushort[] ClassValues { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The class for the glyph, or <c>0</c> when the glyph is below <see cref="StartGlyphId"/> or beyond the end of <see cref="ClassValues"/>.</returns>
    /// <remarks>The lookup is constant-time: the glyph ID's offset from <see cref="StartGlyphId"/> is used to index directly into <see cref="ClassValues"/>.</remarks>
    /// <seealso cref="ClassValues"/>
    /// <seealso cref="StartGlyphId"/>
    public override int GetClass(int glyphId)
    {
        int i = glyphId - StartGlyphId;
        return (uint)i < (uint)ClassValues.Length ? ClassValues[i] : 0;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-1 body (after the format discriminant).</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed format-1 table.</returns>
    /// <exception cref="EndOfStreamException">The header or class-value array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">Class Definition table</see> in the OpenType specification.</remarks>
    /// <seealso cref="ClassValues"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">OpenType specification: Class Definition table</seealso>
    static ClassDefFormat1 IDerivedRecord<ClassDef, ClassDefFormat1>.Parse(ref Cursor cursor, object? context) => new()
    {
        StartGlyphId = cursor.ReadUInt16(),
        ClassValues = cursor.ReadUInt16Array(cursor.ReadUInt16())
    };
}

/// <summary>ClassDef format 2: glyph ranges with class values.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table stores an array of <see cref="ClassRange"/> records, each covering a contiguous glyph ID range with a single class value.</description></item>
/// <item><description>Ranges are sorted ascending by <see cref="ClassRange.StartGlyphId"/> and must not overlap, allowing the <see cref="GetClass(int)"/> lookup to use binary search.</description></item>
/// <item><description>Glyphs not covered by any range default to class 0.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">Class Definition table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ClassDef"/>
/// <seealso cref="ClassDefFormat1"/>
/// <seealso cref="ClassRange"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">OpenType specification: Class Definition table</seealso>
public sealed record ClassDefFormat2 : ClassDef, IDerivedRecord<ClassDef, ClassDefFormat2>
{
    /// <summary>Gets the class-range records.</summary>
    /// <value>The ordered array of <see cref="ClassRange"/> records. Ranges are sorted ascending by <see cref="ClassRange.StartGlyphId"/>.</value>
    /// <seealso cref="ClassRange"/>
    /// <seealso cref="GetClass(int)"/>
    public ClassRange[] ClassRangeRecords { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The class for the glyph, or <c>0</c> when no range covers it.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup uses binary search over <see cref="ClassRangeRecords"/>, which the specification requires to be sorted by <see cref="ClassRange.StartGlyphId"/>.</description></item>
    /// <item><description>Ranges are treated as non-overlapping; the first match found by the binary search wins.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ClassRangeRecords"/>
    /// <seealso cref="ClassRange"/>
    public override int GetClass(int glyphId)
    {
        int lo = 0, hi = ClassRangeRecords.Length - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            ref readonly ClassRange r = ref ClassRangeRecords[mid];
            if (glyphId < r.StartGlyphId) hi = mid - 1;
            else if (glyphId > r.EndGlyphId) lo = mid + 1;
            else return r.Class;
        }
        return 0;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-2 body (after the format discriminant).</param>
    /// <param name="context">Unused.</param>
    /// <returns>The parsed format-2 table.</returns>
    /// <exception cref="EndOfStreamException">The count or range array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">Class Definition table</see> in the OpenType specification.</remarks>
    /// <seealso cref="ClassRangeRecords"/>
    /// <seealso cref="ClassRange"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table">OpenType specification: Class Definition table</seealso>
    static ClassDefFormat2 IDerivedRecord<ClassDef, ClassDefFormat2>.Parse(ref Cursor cursor, object? context) => new() { ClassRangeRecords = cursor.ReadBigEndianStructArray<ClassRange>(cursor.ReadUInt16()) };
}
