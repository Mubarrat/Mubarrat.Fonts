using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Sfnt;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// The 'lcar' table — AAT ligature caret division points
//
// Uses the shared AAT infrastructure from AatShared.cs: LookupTable and its
// format-specific subclasses.
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>lcar</c> table: ligature caret division points that tell an application where to split a ligature glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each ligature glyph carries zero or more <em>division points</em> along the baseline. An application that divides a ligature by <c>k</c> insertion points draws the caret at the <c>k</c> declared positions rather than at evenly spaced ones, which avoids placing the caret over a visually wide or narrow part of the ligature.</description></item>
/// <item><description>The number of division points is generally one less than the number of characters that compose the ligature: an <c>fr</c> ligature has one, an <c>ffl</c> ligature has two.</description></item>
/// <item><description>Two formats exist. Format 0 stores each division point as an FUnit distance along the baseline from <c>x = 0</c>. Format 1 stores each division point as a control point number from the glyph's <c>glyf</c> outline; the projection of that control point onto the baseline is the division point. Format 1 is preferable when the glyph is hinted, because the control point can move with hinting while a raw distance cannot.</description></item>
/// <item><description>All multi-byte reads use the sfnt default of big-endian.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html"><c>lcar</c> table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LigCaretClassEntry"/>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">TrueType Reference Manual: The <c>lcar</c> table</seealso>
public sealed record LcarTable : IFontTable<LcarTable>, IRecord<LcarTable>
{
    /// <summary>The only version defined by the current specification, as a 16.16 <see cref="Fixed"/> value.</summary>
    /// <value>The <see cref="Fixed"/> value 1.0, whose raw bits are <c>0x00010000</c>.</value>
    /// <seealso cref="Version"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">TrueType Reference Manual: The <c>lcar</c> table</seealso>
    public static readonly Fixed CurrentVersion = (Fixed)0x00010000;

    /// <summary>The format value indicating that division points are FUnit distances.</summary>
    /// <value>The constant <c>0</c>.</value>
    /// <seealso cref="Format"/>
    /// <seealso cref="IsDistanceFormat"/>
    public const ushort DistanceFormat = 0;

    /// <summary>The format value indicating that division points are <c>glyf</c> control point numbers.</summary>
    /// <value>The constant <c>1</c>.</value>
    /// <seealso cref="Format"/>
    /// <seealso cref="IsControlPointFormat"/>
    public const ushort ControlPointFormat = 1;

    /// <inheritdoc/>
    public static Tag Tag => "lcar";

    /// <summary>Gets the table version.</summary>
    /// <value>The 16.16 <see cref="Fixed"/> version read from the header. Always <see cref="CurrentVersion"/>; the parser rejects any other value.</value>
    /// <seealso cref="CurrentVersion"/>
    public Fixed Version { get; init; }

    /// <summary>Gets the division-point interpretation format.</summary>
    /// <value><see cref="DistanceFormat"/> when division points are FUnit distances; <see cref="ControlPointFormat"/> when they are <c>glyf</c> control point numbers. The parser rejects any other value.</value>
    /// <seealso cref="IsDistanceFormat"/>
    /// <seealso cref="IsControlPointFormat"/>
    /// <seealso cref="DistanceFormat"/>
    /// <seealso cref="ControlPointFormat"/>
    public ushort Format { get; init; }

    /// <summary>Gets the lookup that maps each ligature glyph to its caret entry.</summary>
    /// <value>A lookup whose value for glyph <c>i</c> is a byte offset from the start of this table to a <see cref="LigCaretClassEntry"/> record. A value of zero means the glyph has no declared caret positions.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Glyphs that are not ligatures in the font, or ligatures for which the font declares no division points, need not appear in the lookup; a lookup value of zero from an uncovered glyph, or an explicit zero from a covered one, both mean "no caret positions".</description></item>
    /// <item><description>Two glyphs may share the same lookup value; the parser deduplicates by offset so their entries are stored once in <see cref="EntriesByOffset"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetCaretEntry(int)"/>
    /// <seealso cref="EntriesByOffset"/>
    /// <seealso cref="LookupTable"/>
    public required LookupTable GlyphToCaretLookup { get; init; }

    /// <summary>Gets the caret entries referenced by the lookup, keyed by their byte offset from the start of this table.</summary>
    /// <value>One <see cref="LigCaretClassEntry"/> per distinct referenced offset. Multiple glyphs may share one entry.</value>
    /// <seealso cref="GetCaretEntry(int)"/>
    /// <seealso cref="LigCaretClassEntry"/>
    public required IReadOnlyDictionary<ushort, LigCaretClassEntry> EntriesByOffset { get; init; }

    /// <summary>Gets a value indicating whether division points are FUnit distances.</summary>
    /// <value><see langword="true"/> when <see cref="Format"/> equals <see cref="DistanceFormat"/>.</value>
    /// <remarks>When set, each <see cref="LigCaretClassEntry.Partials"/> element is an FUnit distance along the baseline from <c>x = 0</c>.</remarks>
    /// <seealso cref="Format"/>
    /// <seealso cref="IsControlPointFormat"/>
    /// <seealso cref="DistanceFormat"/>
    public bool IsDistanceFormat => Format == DistanceFormat;

    /// <summary>Gets a value indicating whether division points are <c>glyf</c> control point numbers.</summary>
    /// <value><see langword="true"/> when <see cref="Format"/> equals <see cref="ControlPointFormat"/>.</value>
    /// <remarks>When set, each <see cref="LigCaretClassEntry.Partials"/> element is a control point index into the ligature glyph's outline; the projection of that control point onto the baseline is the division point.</remarks>
    /// <seealso cref="Format"/>
    /// <seealso cref="IsDistanceFormat"/>
    /// <seealso cref="ControlPointFormat"/>
    public bool IsControlPointFormat => Format == ControlPointFormat;

    /// <summary>Gets the caret entry for a ligature glyph.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The glyph's caret entry, or <see langword="null"/> when the lookup does not cover the glyph, its value is zero, or the value points to no entry.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>A <see langword="null"/> result means the glyph has no declared caret division points; an application should fall back to its default ligature-splitting behavior.</description></item>
    /// <item><description>The method returns <see langword="null"/> rather than throwing when a malformed table contains a lookup value that is not a valid entry offset.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GlyphToCaretLookup"/>
    /// <seealso cref="EntriesByOffset"/>
    /// <seealso cref="LigCaretClassEntry"/>
    public LigCaretClassEntry? GetCaretEntry(int glyphId) =>
        !GlyphToCaretLookup.TryGetValue(glyphId, out uint raw) || raw == 0 || raw > ushort.MaxValue
            ? null
            : EntriesByOffset.TryGetValue((ushort)raw, out var entry)
                ? entry
                : null;

    static LcarTable IRecord<LcarTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
            throw new InvalidDataException(
                $"'lcar'.version is 0x{(int)header.Version:X8}, expected 0x{(int)CurrentVersion:X8}.");
        if (header.Format != DistanceFormat && header.Format != ControlPointFormat)
            throw new InvalidDataException(
                $"'lcar'.format is {header.Format}, expected {DistanceFormat} or {ControlPointFormat}.");

        int numGlyphs = context is SfntFontFace face
            ? face.GetTable<MaxpTable>().NumGlyphs
            : throw new InvalidDataException(
                "'lcar' requires an SFNT font face context to obtain the glyph count.");

        LookupTable lookup = cursor.ReadRecord<LookupTable>(new LookupTable.Context(numGlyphs));

        var entriesByOffset = new Dictionary<ushort, LigCaretClassEntry>();
        foreach (ushort offset in DistinctReferencedOffsets(lookup, numGlyphs))
            entriesByOffset[offset] = cursor.Source.ParseRecordAt<LigCaretClassEntry>(offset);

        return new()
        {
            Version = header.Version,
            Format = header.Format,
            GlyphToCaretLookup = lookup,
            EntriesByOffset = entriesByOffset,
        };
    }

    private static IEnumerable<ushort> DistinctReferencedOffsets(LookupTable lookup, int glyphCount)
    {
        var seen = new HashSet<ushort>();
        for (int glyph = 0; glyph < glyphCount; glyph++)
        {
            if (!lookup.TryGetValue(glyph, out uint raw) || raw == 0 || raw > ushort.MaxValue)
                continue;
            ushort offset = (ushort)raw;
            if (seen.Add(offset))
                yield return offset;
        }
    }

    /// <summary>The 6-byte <c>lcar</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The version is a 16.16 <see cref="Fixed"/> value; the format is a <c>UInt16</c>. Fields are reversed field-by-field because they are not the same width.</description></item>
    /// <item><description>Consumed at parse time and not retained on <see cref="LcarTable"/>; its field values are exposed through the semantic record's properties.</description></item>
    /// <item><description>Declared as a mutable <c>record struct</c> with <see cref="StructLayoutAttribute"/> so its fields honour the wire layout; the <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LcarTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">TrueType Reference Manual: The <c>lcar</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Table version, a 16.16 <see cref="Fixed"/> value, at byte offset 0.</summary>
        /// <value>The constant <see cref="CurrentVersion"/>.</value>
        /// <seealso cref="CurrentVersion"/>
        /// <seealso cref="LcarTable.Version"/>
        public Fixed Version;   // +0

        /// <summary>Division-point interpretation format at byte offset 4.</summary>
        /// <value>One of <see cref="DistanceFormat"/> or <see cref="ControlPointFormat"/>.</value>
        /// <seealso cref="DistanceFormat"/>
        /// <seealso cref="ControlPointFormat"/>
        /// <seealso cref="LcarTable.Format"/>
        public ushort Format;   // +4

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>The version is a 32-bit <see cref="Fixed"/> value while the format is 16-bit, so the reversal is field-by-field rather than a single word-span pass.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = Fixed.ReverseEndianness(value.Version),
            Format = BinaryPrimitives.ReverseEndianness(value.Format),
        };
    }
}

/// <summary>A caret entry for one ligature glyph: the division points at which an application splits the ligature.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><see cref="Partials"/> has one element per division point. Its length is generally one less than the number of characters that compose the ligature: an <c>fr</c> ligature has one partial, an <c>ffl</c> ligature has two.</description></item>
/// <item><description>The interpretation of each partial depends on the enclosing <see cref="LcarTable.Format"/>: FUnit distances along the baseline when <see cref="LcarTable.IsDistanceFormat"/>, <c>glyf</c> control point numbers when <see cref="LcarTable.IsControlPointFormat"/>.</description></item>
/// <item><description>The wire form is a <c>UInt16</c> count followed by that many <c>Int16</c> values; the count is self-describing, so no context is required to parse an entry.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">LigCaretClassEntry records</see> section of the <c>lcar</c> table description.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LcarTable"/>
/// <seealso cref="LcarTable.GetCaretEntry(int)"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6lcar.html">TrueType Reference Manual: The <c>lcar</c> table</seealso>
public sealed record LigCaretClassEntry : IRecord<LigCaretClassEntry>
{
    /// <summary>Gets the division points for the ligature glyph, in left-to-right order along the baseline.</summary>
    /// <value>One <c>Int16</c> per division point, as a read-only view. The length equals the on-disk <c>count</c> field.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>When <see cref="LcarTable.IsDistanceFormat"/>, each value is an FUnit distance measured along the baseline from <c>x = 0</c> to the division point.</description></item>
    /// <item><description>When <see cref="LcarTable.IsControlPointFormat"/>, each value is a control point index into the ligature glyph's outline; the projection of that control point onto the baseline is the division point.</description></item>
    /// <item><description>The on-disk type is <c>Int16</c> even though distances and control point indices are nominally non-negative; the parser preserves the raw value without clamping or validating.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LcarTable.IsDistanceFormat"/>
    /// <seealso cref="LcarTable.IsControlPointFormat"/>
    /// <seealso cref="LcarTable.Format"/>
    public required ReadOnlyMemory<short> Partials { get; init; }

    /// <summary>Gets the number of division points in the entry.</summary>
    /// <value>The length of <see cref="Partials"/>.</value>
    /// <remarks>The value is the number of insertion points at which an application splits the ligature, and is generally one less than the number of characters that compose the ligature.</remarks>
    /// <seealso cref="Partials"/>
    public int Count => Partials.Length;

    static LigCaretClassEntry IRecord<LigCaretClassEntry>.Parse(ref Cursor cursor, object? context)
    {
        ushort count = cursor.ReadUInt16();
        return new() { Partials = cursor.ReadInt16Array(count) };
    }
}
