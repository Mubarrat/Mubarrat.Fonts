using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Sfnt;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>opbd</c> table: optical bounds for glyph edges. Supplies the delta or control-point information a layout engine uses to optically align the first and last glyphs of a line.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>For every glyph with optical edge data, the table supplies four <c>Int16</c> values — left, top, right, bottom — that specify how far the glyph must be moved for its optical edge to align with the line's visual edge. Only glyphs at the boundaries of a line are affected; interior glyphs are unchanged.</description></item>
/// <item><description><b>Format 0 (distance):</b> values are FUnits deltas, sign-matched to the font's coordinate system (negative = left/down, positive = right/up). A value of <c>0</c> means "this side has no optical edge".</description></item>
/// <item><description><b>Format 1 (control point):</b> values are control-point indices into the <c>glyf</c> table. The special value <c>-1</c> means "this side has no optical edge"; every other value (including <c>0</c>) is a valid control-point index.</description></item>
/// <item><description>The lookup table maps glyphs to <c>UInt16</c> offsets. Each offset is measured from the <em>start of the <c>opbd</c> table</em>, not from the lookup table itself, and points at a four-<c>Int16</c> record.</description></item>
/// <item><description>All multi-byte reads use the sfnt default of big-endian.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html"><c>opbd</c> table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="OpticalBounds"/>
/// <seealso cref="OpbdFormat"/>
/// <seealso cref="LookupTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual: The <c>opbd</c> table</seealso>
public sealed record OpbdTable : IFontTable<OpbdTable>, IRecord<OpbdTable>
{
    /// <summary>The only version defined by the current specification, as a 16.16 <c>Fixed</c> value.</summary>
    /// <value>The bit pattern <c>0x00010000</c>, corresponding to <c>Fixed</c> 1.0.</value>
    /// <seealso cref="Version"/>
    public static readonly Fixed CurrentVersion = new(0x00010000);

    /// <inheritdoc/>
    public static Tag Tag => "opbd";

    /// <summary>Gets the table version.</summary>
    /// <value>The 16.16 <c>Fixed</c> version read from the header. Always <see cref="CurrentVersion"/>; the parser rejects any other value.</value>
    /// <seealso cref="CurrentVersion"/>
    public Fixed Version { get; init; }

    /// <summary>Gets the format of the four optical-bounds values.</summary>
    /// <value><see cref="OpbdFormat.Distance"/> when the values are FUnits deltas; <see cref="OpbdFormat.ControlPoint"/> when they are control-point indices into the <c>glyf</c> table. The parser rejects any other value.</value>
    /// <seealso cref="IsDistance"/>
    /// <seealso cref="IsControlPoint"/>
    /// <seealso cref="HasLeftEdge(OpticalBounds)"/>
    public OpbdFormat Format { get; init; }

    /// <summary>Gets the lookup that maps each glyph to the offset of its optical-bounds record.</summary>
    /// <value>A lookup whose value for glyph <c>i</c> is a byte offset from the start of this table to the glyph's <see cref="OpticalBounds"/>. A lookup value of zero means the glyph has no optical bounds.</value>
    /// <remarks>Because multiple glyphs may reference the same record, the value is an offset, not an index. Consumers should prefer <see cref="TryGetBounds(int, out OpticalBounds)"/> over interpreting the lookup directly.</remarks>
    /// <seealso cref="TryGetBounds(int, out OpticalBounds)"/>
    /// <seealso cref="BoundsByOffset"/>
    /// <seealso cref="LookupTable"/>
    public required LookupTable Lookup { get; init; }

    /// <summary>Gets the optical-bounds records, keyed by their byte offset from the start of this table.</summary>
    /// <value>One entry per distinct referenced offset. Multiple glyphs may share a record and therefore a single entry.</value>
    /// <seealso cref="TryGetBounds(int, out OpticalBounds)"/>
    /// <seealso cref="Lookup"/>
    /// <seealso cref="OpticalBounds"/>
    public required IReadOnlyDictionary<ushort, OpticalBounds> BoundsByOffset { get; init; }

    /// <summary>Gets a value indicating whether the table uses format 0 (distance).</summary>
    /// <value><see langword="true"/> when <see cref="Format"/> is <see cref="OpbdFormat.Distance"/>.</value>
    /// <seealso cref="Format"/>
    /// <seealso cref="IsControlPoint"/>
    public bool IsDistance => Format == OpbdFormat.Distance;

    /// <summary>Gets a value indicating whether the table uses format 1 (control point).</summary>
    /// <value><see langword="true"/> when <see cref="Format"/> is <see cref="OpbdFormat.ControlPoint"/>.</value>
    /// <seealso cref="Format"/>
    /// <seealso cref="IsDistance"/>
    public bool IsControlPoint => Format == OpbdFormat.ControlPoint;

    /// <summary>Tries to get the optical bounds for a glyph.</summary>
    /// <param name="glyphId">The glyph ID. Negative values, glyphs not covered by the lookup, and glyphs whose lookup value is zero are treated as "no bounds".</param>
    /// <param name="bounds">When this method returns <see langword="true"/>, contains the glyph's <see cref="OpticalBounds"/>; otherwise, <c>default</c>.</param>
    /// <returns><see langword="true"/> when the glyph has an optical-bounds record; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup value must fit in <c>UInt16</c>. A malformed lookup value greater than <see cref="ushort.MaxValue"/> is treated as "no bounds", matching how <see cref="LookupTable"/> consumers elsewhere in the codebase defend against non-format-6 lookup tables.</description></item>
    /// <item><description>Bounds records that lie outside the table are not re-validated here; they would have thrown <see cref="EndOfStreamException"/> during parse. Only records whose offsets were successfully read at parse time appear in <see cref="BoundsByOffset"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Lookup"/>
    /// <seealso cref="BoundsByOffset"/>
    /// <seealso cref="HasLeftEdge(OpticalBounds)"/>
    public bool TryGetBounds(int glyphId, out OpticalBounds bounds)
    {
        if (Lookup.TryGetValue(glyphId, out uint raw)
            && raw != 0 && raw <= ushort.MaxValue
            && BoundsByOffset.TryGetValue((ushort)raw, out bounds))
        {
            return true;
        }

        bounds = default;
        return false;
    }

    /// <summary>Determines whether the left side of a glyph has an optical edge.</summary>
    /// <param name="bounds">The glyph's optical bounds.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><description>For format 0: <see langword="true"/> when <see cref="OpticalBounds.Left"/> is non-zero.</description></item>
    /// <item><description>For format 1: <see langword="true"/> when <see cref="OpticalBounds.Left"/> is not <c>-1</c>.</description></item>
    /// </list>
    /// </returns>
    /// <seealso cref="OpticalBounds.Left"/>
    /// <seealso cref="Format"/>
    public bool HasLeftEdge(OpticalBounds bounds) => HasEdge(bounds.Left);

    /// <summary>Determines whether the top side of a glyph has an optical edge.</summary>
    /// <param name="bounds">The glyph's optical bounds.</param>
    /// <returns><see langword="true"/> under the same per-format rule as <see cref="HasLeftEdge(OpticalBounds)"/>.</returns>
    /// <seealso cref="OpticalBounds.Top"/>
    public bool HasTopEdge(OpticalBounds bounds) => HasEdge(bounds.Top);

    /// <summary>Determines whether the right side of a glyph has an optical edge.</summary>
    /// <param name="bounds">The glyph's optical bounds.</param>
    /// <returns><see langword="true"/> under the same per-format rule as <see cref="HasLeftEdge(OpticalBounds)"/>.</returns>
    /// <seealso cref="OpticalBounds.Right"/>
    public bool HasRightEdge(OpticalBounds bounds) => HasEdge(bounds.Right);

    /// <summary>Determines whether the bottom side of a glyph has an optical edge.</summary>
    /// <param name="bounds">The glyph's optical bounds.</param>
    /// <returns><see langword="true"/> under the same per-format rule as <see cref="HasLeftEdge(OpticalBounds)"/>.</returns>
    /// <seealso cref="OpticalBounds.Bottom"/>
    public bool HasBottomEdge(OpticalBounds bounds) => HasEdge(bounds.Bottom);

    private bool HasEdge(short value) => Format switch
    {
        OpbdFormat.Distance => value != 0,
        OpbdFormat.ControlPoint => value != -1,
        _ => false,
    };

    static OpbdTable IRecord<OpbdTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
            throw new InvalidDataException($"'opbd'.version is 0x{header.Version.Bits:X8}, expected 0x{CurrentVersion.Bits:X8}.");

        if (header.Format > 1)
            throw new InvalidDataException($"'opbd'.format is {header.Format}, expected 0 (distance) or 1 (control point).");

        int numGlyphs = context is SfntFontFace face
            ? face.GetTable<MaxpTable>().NumGlyphs
            : throw new InvalidDataException("'opbd' requires an SFNT font face context to obtain the glyph count.");

        LookupTable lookup = cursor.ReadRecord<LookupTable>(new LookupTable.Context(numGlyphs));

        // Collect distinct referenced offsets. A value of zero means "no bounds"; a
        // value that does not fit in UInt16 cannot come from a well-formed format-6
        // lookup table and is skipped.
        var distinctOffsets = new HashSet<ushort>();
        for (int glyph = 0; glyph < numGlyphs; glyph++)
        {
            if (lookup.TryGetValue(glyph, out uint raw) && raw != 0 && raw <= ushort.MaxValue)
                distinctOffsets.Add((ushort)raw);
        }

        // The lookup values are offsets from the start of the opbd table, so
        // ReadEndianReversibleStructAt on the table-scoped source is exactly the right
        // base — no additional WithOffset needed.
        var boundsByOffset = new Dictionary<ushort, OpticalBounds>(distinctOffsets.Count);
        foreach (ushort offset in distinctOffsets)
            boundsByOffset.Add(offset, cursor.Source.ReadEndianReversibleStructAt<OpticalBounds>(offset));

        return new()
        {
            Version = header.Version,
            Format = (OpbdFormat)header.Format,
            Lookup = lookup,
            BoundsByOffset = boundsByOffset,
        };
    }

    /// <summary>The 6-byte <c>opbd</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The version field is a 16.16 <c>Fixed</c> value and the format field is a <c>UInt16</c>, so the reversal is field-by-field.</description></item>
    /// <item><description>Consumed at parse time and not retained on <see cref="OpbdTable"/>; its field values are exposed through the semantic record's properties.</description></item>
    /// <item><description>The <c>&lt;Clone&gt;$()</c> method generated by the <c>record struct</c> declaration must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>Public because a writer fills the struct in memory, reverses it, and writes the bytes by blittable cast.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="OpbdTable"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0, as a <c>Fixed</c> 16.16 value.</summary>
        /// <value>The constant <see cref="CurrentVersion"/>.</value>
        /// <seealso cref="CurrentVersion"/>
        /// <seealso cref="OpbdTable.Version"/>
        public Fixed Version;   // +0

        /// <summary>The format discriminant at byte offset 4.</summary>
        /// <value><c>0</c> for distance, <c>1</c> for control point.</value>
        /// <seealso cref="OpbdTable.Format"/>
        public ushort Format;   // +4

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>The version is reversed through <see cref="Fixed.ReverseEndianness(Fixed)"/> so the 16.16 representation is preserved.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = Fixed.ReverseEndianness(value.Version),
            Format = BinaryPrimitives.ReverseEndianness(value.Format),
        };
    }
}

/// <summary>The format discriminant of an <see cref="OpbdTable"/>: how the four <c>Int16</c> optical-bounds values are interpreted.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The two formats differ in the <em>meaning</em> of the four values, not in their width. Both store four <c>Int16</c> values per glyph.</description></item>
/// <item><description>They also differ in what "no edge" looks like: format 0 uses <c>0</c>; format 1 uses <c>-1</c>. See <see cref="OpbdTable.HasLeftEdge(OpticalBounds)"/> and its siblings for the format-aware accessors.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="OpbdTable.Format"/>
/// <seealso cref="OpticalBounds"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6opbd.html">TrueType Reference Manual: The <c>opbd</c> table</seealso>
public enum OpbdFormat : ushort
{
    /// <summary>Values are FUnits delta distances. The value <c>0</c> marks a side with no optical edge.</summary>
    Distance = 0,

    /// <summary>Values are control-point indices into the <c>glyf</c> table. The value <c>-1</c> marks a side with no optical edge.</summary>
    ControlPoint = 1,
}

/// <summary>The four <c>Int16</c> optical-bounds values for a single glyph: left, top, right, bottom.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The semantic meaning of each value depends on the enclosing table's <see cref="OpbdTable.Format"/>: <see cref="OpbdFormat.Distance"/> treats them as FUnits deltas; <see cref="OpbdFormat.ControlPoint"/> treats them as <c>glyf</c> control-point indices.</description></item>
/// <item><description>The "no edge" sentinel also depends on format: format 0 uses <c>0</c>; format 1 uses <c>-1</c>. Use <see cref="OpbdTable.HasLeftEdge(OpticalBounds)"/> and its siblings rather than comparing raw field values.</description></item>
/// <item><description>The struct is exactly 8 bytes on disk and begins on a <c>UInt16</c> boundary, which every <c>UInt16</c> offset produced by a well-formed lookup table satisfies.</description></item>
/// <item><description>All four fields are <c>Int16</c>, so <see cref="ReverseEndianness(OpticalBounds)"/> reverses the struct as four consecutive 16-bit words in a single pass.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="OpbdTable"/>
/// <seealso cref="OpbdFormat"/>
/// <seealso cref="IEndianReversibleStruct{T}"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct OpticalBounds : IEndianReversibleStruct<OpticalBounds>
{
    /// <summary>The left-side optical value at byte offset 0.</summary>
    /// <value>For format 0, a delta in FUnits (negative moves left). For format 1, a control-point index into the <c>glyf</c> table.</value>
    /// <seealso cref="OpbdTable.HasLeftEdge(OpticalBounds)"/>
    public short Left;    // +0

    /// <summary>The top-side optical value at byte offset 2.</summary>
    /// <value>For format 0, a delta in FUnits (negative moves up). For format 1, a control-point index into the <c>glyf</c> table.</value>
    /// <seealso cref="OpbdTable.HasTopEdge(OpticalBounds)"/>
    public short Top;     // +2

    /// <summary>The right-side optical value at byte offset 4.</summary>
    /// <value>For format 0, a delta in FUnits (positive moves right). For format 1, a control-point index into the <c>glyf</c> table.</value>
    /// <seealso cref="OpbdTable.HasRightEdge(OpticalBounds)"/>
    public short Right;   // +4

    /// <summary>The bottom-side optical value at byte offset 6.</summary>
    /// <value>For format 0, a delta in FUnits (positive moves up). For format 1, a control-point index into the <c>glyf</c> table.</value>
    /// <seealso cref="OpbdTable.HasBottomEdge(OpticalBounds)"/>
    public short Bottom;  // +6

    /// <summary>Reverses the byte order of all four 16-bit fields in one pass.</summary>
    /// <param name="value">The value whose fields are to be reversed.</param>
    /// <returns>A new record with all four fields reversed.</returns>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    public static OpticalBounds ReverseEndianness(OpticalBounds value)
    {
        var words = MemoryMarshal.CreateSpan(ref value.Left, 4);
        BinaryPrimitives.ReverseEndianness(words, words);
        return value;
    }
}
