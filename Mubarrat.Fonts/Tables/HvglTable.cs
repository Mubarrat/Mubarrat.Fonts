using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>hvgl</c> table: glyph and part definitions for Hierarchical Variation Fonts. Stores a depth-first tree of parts — shapes and composites — addressed by a flat 32-bit part index.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><b>Little-endian throughout.</b> Every multi-byte read passes <c>isLittleEndian: true</c>, unlike every other sfnt table. Floating-point values are IEEE 754; composites use <c>Float32</c>, shapes use <c>Float64</c>.</description></item>
/// <item><description>Parts are addressed through a flat <c>UInt32</c> index of offsets, one per part plus a sentinel. Each part begins with a <c>UInt16</c> flags word whose bit 0 selects the format: <c>0</c> for a shape, <c>1</c> for a composite.</description></item>
/// <item><description>Shape data must be <c>Float64</c>-aligned within the table; composite data must be <c>Float32</c>-aligned. Padding to those boundaries is embedded in the table and honoured by the parser.</description></item>
/// <item><description>Sparse matrices and sparse vectors are preserved in their on-disk (compressed sparse column / parallel index list) form. The parser establishes structure; it does not evaluate axes or blend.</description></item>
/// <item><description>Version is checked strictly against <see cref="CurrentMajorVersion"/>.<see cref="CurrentMinorVersion"/>; non-zero format flags are stored rather than rejected, per the parse/diagnose split.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6hvgl.html"><c>hvgl</c> table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="HvglPart"/>
/// <seealso cref="HvglShapePart"/>
/// <seealso cref="HvglCompositePart"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6hvgl.html">TrueType Reference Manual: The <c>hvgl</c> table</seealso>
public sealed record HvglTable : IFontTable<HvglTable>, IRecord<HvglTable>
{
    /// <summary>The major version defined by the current specification.</summary>
    public const ushort CurrentMajorVersion = 3;

    /// <summary>The minor version defined by the current specification.</summary>
    public const ushort CurrentMinorVersion = 1;

    /// <inheritdoc/>
    public static Tag Tag => "hvgl";

    /// <summary>Gets the major version from the table header. Always <see cref="CurrentMajorVersion"/>.</summary>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version from the table header. Always <see cref="CurrentMinorVersion"/>.</summary>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the raw format flags from the table header.</summary>
    /// <remarks>The parser stores the value rather than rejecting non-zero flags; a diagnostics analyzer is the appropriate place to flag them.</remarks>
    public uint FormatFlags { get; init; }

    /// <summary>Gets the total number of parts: every shape and every composite, including those not externally visible.</summary>
    public uint NumberOfParts { get; init; }

    /// <summary>Gets the number of externally visible parts. Visible parts are always first by part index.</summary>
    public uint NumberOfGlyphs { get; init; }

    /// <summary>Gets every part in the table.</summary>
    /// <value>An array whose entry <c>i</c> is the part at index <c>i</c>, in depth-first order. The first <see cref="NumberOfGlyphs"/> entries are the externally visible glyphs.</value>
    public required HvglPart[] Parts { get; init; }

    /// <summary>Gets the externally visible glyph parts, in glyph index order.</summary>
    public ReadOnlySpan<HvglPart> Glyphs => Parts.AsSpan(0, checked((int)NumberOfGlyphs));

    /// <summary>Tries to get the part at <paramref name="index"/>.</summary>
    /// <param name="index">The part index.</param>
    /// <param name="part">When this method returns <see langword="true"/>, contains the part; otherwise, <c>null</c>.</param>
    /// <returns><see langword="true"/> when <paramref name="index"/> is within range; otherwise, <see langword="false"/>.</returns>
    public bool TryGetPart(int index, out HvglPart? part)
    {
        if ((uint)index < (uint)Parts.Length) { part = Parts[index]; return true; }
        part = null;
        return false;
    }

    /// <summary>Gets the externally visible glyph part at <paramref name="glyphIndex"/>.</summary>
    /// <param name="glyphIndex">The glyph index in <c>[0, NumberOfGlyphs)</c>.</param>
    /// <returns>The glyph part.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="glyphIndex"/> is negative or beyond <see cref="NumberOfGlyphs"/>.</exception>
    public HvglPart GetGlyph(int glyphIndex)
    {
        if ((uint)glyphIndex >= NumberOfGlyphs)
            throw new ArgumentOutOfRangeException(
                nameof(glyphIndex), glyphIndex,
                $"Glyph index {glyphIndex} is outside the visible range [0, {NumberOfGlyphs}).");
        return Parts[glyphIndex];
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidDataException">The table's version is not <see cref="CurrentMajorVersion"/>.<see cref="CurrentMinorVersion"/>, or the part count exceeds the representable range.</exception>
    /// <exception cref="EndOfStreamException">A read extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is read via <c>ReadEndianReversibleStruct</c> with <c>isLittleEndian: true</c>, which reverses multi-byte fields only when the host order differs from the wire order. The header is consumed locally and not retained on the record.</description></item>
    /// <item><description>The part index is read as a single bulk <c>UInt32</c> array; each entry is an offset from the part-index base, and the trailing sentinel is not retained.</description></item>
    /// <item><description>Each part is dispatched through <see cref="IRecord{T}.Parse(ref Cursor, object?)"/> after seeking to its offset. Parts are not assumed to be contiguous.</description></item>
    /// </list>
    /// </remarks>
    static HvglTable IRecord<HvglTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>(isLittleEndian: true);

        if (header.MajorVersion != CurrentMajorVersion || header.MinorVersion != CurrentMinorVersion)
            throw new InvalidDataException(
                $"'hvgl'.version is {header.MajorVersion}.{header.MinorVersion}, expected {CurrentMajorVersion}.{CurrentMinorVersion}.");
        if (header.NumberOfParts > int.MaxValue)
            throw new InvalidDataException(
                $"'hvgl'.numberOfParts is {header.NumberOfParts}, which exceeds the supported maximum.");

        return new()
        {
            MajorVersion = header.MajorVersion,
            MinorVersion = header.MinorVersion,
            FormatFlags = header.FormatFlags,
            NumberOfParts = header.NumberOfParts,
            NumberOfGlyphs = header.NumberOfGlyphs,
            Parts = cursor.Source.ReadOffset32ArrayParseRecordAt<HvglPart>(header.PartIndexOffset, (int)header.NumberOfParts + 1, isLittleEndian: true),
        };
    }

    /// <summary>The 24-byte <c>hvgl</c> table header. Consumed at parse time and not retained on <see cref="HvglTable"/>.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All fields are little-endian on disk. Declared as a mutable <c>record struct</c> with <see cref="StructLayoutAttribute"/> so the field layout matches the wire layout exactly.</description></item>
    /// <item><description>The two version fields are <c>UInt16</c>; the remaining fields are <c>UInt32</c>, so field-by-field reversal is used rather than a word-width cast.</description></item>
    /// <item><description>The <c>&lt;Clone&gt;$()</c> method generated by the <c>record struct</c> declaration must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Major version at byte offset 0.</summary>
        public ushort MajorVersion;       // +0

        /// <summary>Minor version at byte offset 2.</summary>
        public ushort MinorVersion;       // +2

        /// <summary>Format flags at byte offset 4.</summary>
        public uint FormatFlags;          // +4

        /// <summary>Total number of parts at byte offset 8.</summary>
        public uint NumberOfParts;        // +8

        /// <summary>Byte offset of the part index from the start of the table, at byte offset 12.</summary>
        public uint PartIndexOffset;      // +12

        /// <summary>Number of externally visible parts at byte offset 16.</summary>
        public uint NumberOfGlyphs;       // +16

        /// <summary>Unused; currently zero, at byte offset 20.</summary>
        public uint Unused;               // +20

        /// <summary>Reverses the byte order of every multi-byte field.</summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        public static Header ReverseEndianness(Header value) => new()
        {
            MajorVersion = BinaryPrimitives.ReverseEndianness(value.MajorVersion),
            MinorVersion = BinaryPrimitives.ReverseEndianness(value.MinorVersion),
            FormatFlags = BinaryPrimitives.ReverseEndianness(value.FormatFlags),
            NumberOfParts = BinaryPrimitives.ReverseEndianness(value.NumberOfParts),
            PartIndexOffset = BinaryPrimitives.ReverseEndianness(value.PartIndexOffset),
            NumberOfGlyphs = BinaryPrimitives.ReverseEndianness(value.NumberOfGlyphs),
            Unused = BinaryPrimitives.ReverseEndianness(value.Unused),
        };
    }
}

/// <summary>Base for the two part kinds in an <c>hvgl</c> table: <see cref="HvglShapePart"/> and <see cref="HvglCompositePart"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Every part begins with a <c>UInt16</c> flags word. Bit 0 is <c>0</c> for a shape and <c>1</c> for a composite; all other bits are currently zero and ignored.</description></item>
/// <item><description>Dispatch happens in <see cref="IRecord{T}.Parse(ref Cursor, object?)"/> through the <see cref="IBaseRecord{TBase}"/> helper, mirroring the pattern used by <c>Coverage</c> and the GSUB/GPOS lookup subtable formats.</description></item>
/// <item><description>The flags word is little-endian, so the dispatch peek passes <c>isLittleEndian: true</c>; a misread would select the wrong part kind because bit 0 of <c>0x0001</c> lives in byte 0 of the wire encoding.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="HvglShapePart"/>
/// <seealso cref="HvglCompositePart"/>
public abstract record HvglPart : IRecord<HvglPart>, IBaseRecord<HvglPart>
{
    /// <summary>Gets the raw flags word from the part's header.</summary>
    /// <value>The flags value, whose bit 0 encodes the part kind.</value>
    public abstract ushort Flags { get; init; }

    /// <summary>Reads one part from <paramref name="cursor"/>, dispatching on bit 0 of the leading flags word.</summary>
    /// <param name="cursor">The cursor positioned at the part's first byte.</param>
    /// <param name="context">Unused; parts are self-contained.</param>
    /// <returns>A <see cref="HvglShapePart"/> when bit 0 is clear, otherwise a <see cref="HvglCompositePart"/>.</returns>
    /// <exception cref="EndOfStreamException">The part extends past the end of the table-scoped source.</exception>
    /// <seealso cref="IBaseRecord{TBase}"/>
    static HvglPart IRecord<HvglPart>.Parse(ref Cursor cursor, object? context) =>
        (cursor.PeekUInt16(isLittleEndian: true) & 1) == 0
            ? IBaseRecord<HvglPart>.Parse<HvglShapePart>(ref cursor, context)
            : IBaseRecord<HvglPart>.Parse<HvglCompositePart>(ref cursor, context);
}

/// <summary>A shape part: a leaf-level set of paths, each containing quadratic Bezier segments, with a master coordinate vector and a delta matrix for axis variation.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Shape data must begin on a <c>Float64</c> boundary within the <c>hvgl</c> table.</description></item>
/// <item><description>For curve-type segments, the on-curve point is not stored; its x-coordinate holds the parallel factor, and its y-coordinate is zero. The delta matrix stores the delta of the parallel factor.</description></item>
/// <item><description>The master coordinate vector holds four <c>Float64</c> values per segment, in order: on-curve x, on-curve y, off-curve x, off-curve y.</description></item>
/// <item><description>The delta matrix is column-major, with <c>4 × TotalSegmentCount</c> rows and <c>2 × NumberOfAxes</c> columns.</description></item>
/// <item><description>The shape header is consumed at parse time and not retained on the record; its fields are exposed as individual properties.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="HvglPart"/>
/// <seealso cref="HvglShapePart.Header"/>
public sealed record HvglShapePart : HvglPart, IDerivedRecord<HvglPart, HvglShapePart>
{
    /// <inheritdoc/>
    public override ushort Flags { get; init; }

    /// <summary>Gets the number of axes this shape is registered against.</summary>
    public ushort NumberOfAxes { get; init; }

    /// <summary>Gets the number of paths in the shape.</summary>
    /// <value>The count of distinct contours. The specification notes the value is typically <c>1</c>.</value>
    public ushort NumberOfPaths { get; init; }

    /// <summary>Gets the total number of segments across all paths.</summary>
    /// <value>The sum of <see cref="PathSizes"/>. Paths with fewer than three segments are not rendered.</value>
    public ushort TotalSegmentCount { get; init; }

    /// <summary>Gets the segment count for each path, in path order.</summary>
    public required ushort[] PathSizes { get; init; }

    /// <summary>Gets the blend type for each segment, in segment order.</summary>
    /// <value>One byte per segment. Values are defined by the specification: <c>0</c> curve, <c>1</c> corner or tangent surrounded by identical types, <c>2</c> isolated tangent, <c>3</c> first of two adjacent tangents, <c>4</c> second of two adjacent tangents.</value>
    public required byte[] BlendTypes { get; init; }

    /// <summary>Gets the master coordinate vector.</summary>
    /// <value>Four <c>Float64</c> values per segment: on-curve x, on-curve y, off-curve x, off-curve y.</value>
    public required double[] MasterCoordinates { get; init; }

    /// <summary>Gets the delta coordinate matrix in column-major order.</summary>
    /// <value><c>4 × TotalSegmentCount</c> rows by <c>2 × NumberOfAxes</c> columns, stored as a flat column-major array.</value>
    public required double[] DeltaCoordinates { get; init; }

    /// <inheritdoc/>
    /// <exception cref="EndOfStreamException">A read extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is read via <c>ReadEndianReversibleStruct</c> with <c>isLittleEndian: true</c>; only <see cref="Flags"/> and the field values used to construct the record are extracted from it.</description></item>
    /// <item><description>Path sizes and blend types follow the header as bulk arrays. The cursor is advanced up to the next <c>Float64</c> boundary before the master coordinates and delta matrix, satisfying the specification's alignment requirement for shape data.</description></item>
    /// <item><description>All numeric reads pass <c>isLittleEndian: true</c>; the reversal is applied internally by the reader only when the host order differs.</description></item>
    /// </list>
    /// </remarks>
    static HvglShapePart IDerivedRecord<HvglPart, HvglShapePart>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>(isLittleEndian: true);

        ushort[] pathSizes = cursor.ReadUInt16Array(header.NumberOfPaths, isLittleEndian: true);
        byte[] blendTypes = cursor.ReadUInt8Array(header.TotalSegmentCount);

        cursor.Position = (cursor.Position + 7) & ~7L;

        double[] masterCoords = cursor.ReadDoubleArray(4 * header.TotalSegmentCount, isLittleEndian: true);
        double[] deltaMatrix = cursor.ReadDoubleArray(4 * header.TotalSegmentCount * 2 * header.NumberOfAxes, isLittleEndian: true);

        return new()
        {
            Flags = header.Flags,
            NumberOfAxes = header.NumberOfAxes,
            NumberOfPaths = header.NumberOfPaths,
            TotalSegmentCount = header.TotalSegmentCount,
            PathSizes = pathSizes,
            BlendTypes = blendTypes,
            MasterCoordinates = masterCoords,
            DeltaCoordinates = deltaMatrix,
        };
    }

    /// <summary>The 8-byte <c>hvgl</c> shape header. Consumed at parse time and not retained on <see cref="HvglShapePart"/>.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All four fields are <c>UInt16</c>, so the reversal is a single pass over the struct reinterpreted as four 16-bit words — no field-by-field code is needed.</description></item>
    /// <item><description>The header is a transport type only. The semantic record exposes its field values directly.</description></item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Part flags at offset 0; bit 0 is <c>0</c> for a shape.</summary>
        public ushort Flags;              // +0

        /// <summary>Number of axes at offset 2.</summary>
        public ushort NumberOfAxes;       // +2

        /// <summary>Number of paths at offset 4.</summary>
        public ushort NumberOfPaths;      // +4

        /// <summary>Total segment count across all paths, at offset 6.</summary>
        public ushort TotalSegmentCount;  // +6

        /// <summary>Reverses the byte order of every field in one pass.</summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>A new header with all four fields reversed.</returns>
        /// <remarks>The header is entirely <c>UInt16</c> fields, so it is reinterpreted as a span of four 16-bit words and reversed with a single <see cref="BinaryPrimitives.ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})"/> call.</remarks>
        public static Header ReverseEndianness(Header value)
        {
            Span<ushort> words = MemoryMarshal.CreateSpan(ref Unsafe.As<Header, ushort>(ref value), 4);
            BinaryPrimitives.ReverseEndianness(words, words);
            return value;
        }
    }
}

/// <summary>A composite part: an internal tree node whose immediate subparts may be shapes or other composites, together with sparse axis-value and transform data.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Composite data must begin on a <c>Float32</c> boundary within the <c>hvgl</c> table.</description></item>
/// <item><description>Subpart references and every matrix row index are expressed in depth-first order over the composite's structure tree, excluding the composite's own root. The first immediate subpart always has row and axis offsets of zero.</description></item>
/// <item><description>Sparse matrices and vectors are preserved in their on-disk forms — a sparse vector is row indices parallel to values; a matrix is CSC with column starts, row indices, and parallel values. The parser does not densify or evaluate them, per the parse/evaluate split.</description></item>
/// <item><description>All header offsets are stored divided by four; the parser multiplies back to recover byte offsets from the start of the composite.</description></item>
/// <item><description>The composite header is consumed at parse time and not retained on the record; its field values are exposed as individual properties where they are useful to a consumer.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6hvgl.html">composite part data</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="HvglPart"/>
/// <seealso cref="HvglShapePart"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6hvgl.html">TrueType Reference Manual: The <c>hvgl</c> table</seealso>
public sealed record HvglCompositePart : HvglPart, IDerivedRecord<HvglPart, HvglCompositePart>
{
    /// <inheritdoc/>
    public override ushort Flags { get; init; }

    /// <summary>Gets the number of axes belonging to this composite.</summary>
    public ushort NumberOfAxes { get; init; }

    /// <summary>Gets the total number of parts in this composite's structure tree, including the composite's own root.</summary>
    public ushort TotalPartsInStructureTree { get; init; }

    /// <summary>Gets the sum of axis counts over every node in the structure tree, including the root.</summary>
    public ushort TotalAxesInStructureTree { get; init; }

    /// <summary>Gets the maximum of <c>2 × axes</c> over every part in the structure tree.</summary>
    public ushort MaxNumberOfExtremes { get; init; }

    /// <summary>Gets the immediate subparts, in order.</summary>
    /// <value>One entry per direct child. The first entry always has <see cref="SubpartEntry.TreePartOffset"/> and <see cref="SubpartEntry.TreeAxisOffset"/> equal to zero.</value>
    public required SubpartEntry[] Subparts { get; init; }

    // ─── Sparse axis-value deltas ───

    /// <summary>Gets the CSC column starts for the extremum axis-value matrix.</summary>
    /// <value><c>2 × NumberOfAxes + 1</c> entries, non-decreasing, with a trailing sentinel equal to <see cref="ExtremumRowIndices"/>'s length.</value>
    public required ushort[] ExtremumColumnStarts { get; init; }

    /// <summary>Gets the master axis-value row indices.</summary>
    /// <value>Ascending row indices, parallel to <see cref="MasterAxisValueDeltas"/>.</value>
    public required ushort[] MasterRowIndices { get; init; }

    /// <summary>Gets the extremum axis-value row indices in CSC order.</summary>
    /// <value>Ascending within each column, grouped by the offsets in <see cref="ExtremumColumnStarts"/>, parallel to <see cref="ExtremumAxisValueDeltas"/>.</value>
    public required ushort[] ExtremumRowIndices { get; init; }

    /// <summary>Gets the non-zero master axis-value deltas.</summary>
    /// <value>One <c>Float32</c> per entry of <see cref="MasterRowIndices"/>.</value>
    public required float[] MasterAxisValueDeltas { get; init; }

    /// <summary>Gets the non-zero extremum axis-value deltas.</summary>
    /// <value>One <c>Float32</c> per entry of <see cref="ExtremumRowIndices"/>.</value>
    public required float[] ExtremumAxisValueDeltas { get; init; }

    // ─── Sparse translations ───

    /// <summary>Gets the master translation deltas, interleaved as x, y pairs.</summary>
    /// <value><c>2 × CountOfMasterTranslations</c> floats; entry <c>2i</c> is x, entry <c>2i + 1</c> is y, parallel to <see cref="MasterTranslationIndices"/>.</value>
    public required float[] MasterTranslationDeltas { get; init; }

    /// <summary>Gets the extremum translation deltas, interleaved as x, y pairs.</summary>
    /// <value><c>2 × CountOfExtremumTranslations</c> floats; entry <c>2i</c> is x, entry <c>2i + 1</c> is y, parallel to <see cref="ExtremumTranslationIndices"/>.</value>
    public required float[] ExtremumTranslationDeltas { get; init; }

    /// <summary>Gets the extremum translation row and column indices, interleaved as (row, column) pairs.</summary>
    /// <value><c>2 × CountOfExtremumTranslations</c> ushorts; entry <c>2i</c> is row, entry <c>2i + 1</c> is column. Ascending by row, then by column within each row.</value>
    public required ushort[] ExtremumTranslationIndices { get; init; }

    /// <summary>Gets the master translation row indices.</summary>
    /// <value><c>CountOfMasterTranslations</c> ushorts, ascending by row.</value>
    public required ushort[] MasterTranslationIndices { get; init; }

    // ─── Sparse rotations ───

    /// <summary>Gets the master rotation deltas, in radians, measured counterclockwise.</summary>
    /// <value><c>CountOfMasterRotations</c> floats, parallel to <see cref="MasterRotationIndices"/>.</value>
    public required float[] MasterRotationDeltas { get; init; }

    /// <summary>Gets the extremum rotation deltas, in radians, measured counterclockwise.</summary>
    /// <value><c>CountOfExtremumRotations</c> floats, parallel to <see cref="ExtremumRotationIndices"/>.</value>
    public required float[] ExtremumRotationDeltas { get; init; }

    /// <summary>Gets the extremum rotation row and column indices, interleaved as (row, column) pairs.</summary>
    /// <value><c>2 × CountOfExtremumRotations</c> ushorts; entry <c>2i</c> is row, entry <c>2i + 1</c> is column. Ascending by row, then by column within each row.</value>
    public required ushort[] ExtremumRotationIndices { get; init; }

    /// <summary>Gets the master rotation row indices.</summary>
    /// <value><c>CountOfMasterRotations</c> ushorts, ascending by row.</value>
    public required ushort[] MasterRotationIndices { get; init; }

    /// <inheritdoc/>
    /// <exception cref="EndOfStreamException">A read extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is read with <c>ReadEndianReversibleStruct</c>; the subpart array with <c>ReadEndianReversibleStructArray</c>. Both pass <c>isLittleEndian: true</c>.</description></item>
    /// <item><description>Offsets in the composite header are stored divided by four and multiplied back to recover absolute byte positions; the cursor is positioned with those absolute offsets, so the composite's sections need not be contiguous.</description></item>
    /// <item><description>The three CSC sections (extremum column starts, master row indices, extremum row indices) are stored contiguously and read sequentially as three separate calls, each sized by its own count.</description></item>
    /// <item><description>Translations and rotations interleave their pair members on disk; the parser preserves the interleaving rather than splitting into parallel arrays, so the returned data matches the wire layout byte-for-byte.</description></item>
    /// </list>
    /// </remarks>
    static HvglCompositePart IDerivedRecord<HvglPart, HvglCompositePart>.Parse(ref Cursor cursor, object? context)
    {
        long start = cursor.Position;
        Header header = cursor.ReadEndianReversibleStruct<Header>(isLittleEndian: true);

        cursor.Position = start + header.OffsetToSubpartArray * 4L;
        SubpartEntry[] subparts = cursor.ReadEndianReversibleStructArray<SubpartEntry>(
            header.NumberOfSubparts, isLittleEndian: true);

        cursor.Position = start + header.OffsetToExtremumColumnStarts * 4L;
        ushort[] columnStarts = cursor.ReadUInt16Array(2 * header.NumberOfAxes + 1, isLittleEndian: true);
        ushort[] masterRowIndices = cursor.ReadUInt16Array(header.CountOfMasterAxisValues, isLittleEndian: true);
        ushort[] extremumRowIndices = cursor.ReadUInt16Array(header.CountOfExtremumAxisValues, isLittleEndian: true);

        cursor.Position = start + header.OffsetToMasterAxisDeltas * 4L;
        float[] masterAxisDeltas = cursor.ReadSingleArray(header.CountOfMasterAxisValues, isLittleEndian: true);

        cursor.Position = start + header.OffsetToExtremumAxisDeltas * 4L;
        float[] extremumAxisDeltas = cursor.ReadSingleArray(header.CountOfExtremumAxisValues, isLittleEndian: true);

        cursor.Position = start + header.OffsetToAllTranslations * 4L;
        float[] masterTransDeltas = cursor.ReadSingleArray(2 * header.CountOfMasterTranslations, isLittleEndian: true);
        float[] extremumTransDeltas = cursor.ReadSingleArray(2 * header.CountOfExtremumTranslations, isLittleEndian: true);
        ushort[] extremumTransIndices = cursor.ReadUInt16Array(2 * header.CountOfExtremumTranslations, isLittleEndian: true);
        ushort[] masterTransIndices = cursor.ReadUInt16Array(header.CountOfMasterTranslations, isLittleEndian: true);

        cursor.Position = start + header.OffsetToAllRotations * 4L;
        float[] masterRotDeltas = cursor.ReadSingleArray(header.CountOfMasterRotations, isLittleEndian: true);
        float[] extremumRotDeltas = cursor.ReadSingleArray(header.CountOfExtremumRotations, isLittleEndian: true);
        ushort[] extremumRotIndices = cursor.ReadUInt16Array(2 * header.CountOfExtremumRotations, isLittleEndian: true);
        ushort[] masterRotIndices = cursor.ReadUInt16Array(header.CountOfMasterRotations, isLittleEndian: true);

        return new()
        {
            Flags = header.Flags,
            NumberOfAxes = header.NumberOfAxes,
            TotalPartsInStructureTree = header.TotalPartsInStructureTree,
            TotalAxesInStructureTree = header.TotalAxesInStructureTree,
            MaxNumberOfExtremes = header.MaxNumberOfExtremes,
            Subparts = subparts,
            ExtremumColumnStarts = columnStarts,
            MasterRowIndices = masterRowIndices,
            ExtremumRowIndices = extremumRowIndices,
            MasterAxisValueDeltas = masterAxisDeltas,
            ExtremumAxisValueDeltas = extremumAxisDeltas,
            MasterTranslationDeltas = masterTransDeltas,
            ExtremumTranslationDeltas = extremumTransDeltas,
            ExtremumTranslationIndices = extremumTransIndices,
            MasterTranslationIndices = masterTransIndices,
            MasterRotationDeltas = masterRotDeltas,
            ExtremumRotationDeltas = extremumRotDeltas,
            ExtremumRotationIndices = extremumRotIndices,
            MasterRotationIndices = masterRotIndices,
        };
    }

    /// <summary>The 36-byte composite header: eighteen little-endian <c>UInt16</c> fields. Consumed at parse time and not retained on <see cref="HvglCompositePart"/>.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Every field is <c>UInt16</c>, so the reversal is a single pass over the struct reinterpreted as eighteen 16-bit words — no field-by-field code is needed.</description></item>
    /// <item><description>The byte size is an even number of 16-bit integers, so the data that follows the header naturally begins on a 4-byte boundary.</description></item>
    /// <item><description>Offsets are stored divided by four and must be multiplied back to recover byte offsets from the start of the composite.</description></item>
    /// <item><description>This is a transport type. The semantic record exposes the header's useful field values directly and does not retain a copy of the header itself.</description></item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Part flags at offset 0; bit 0 is <c>1</c> for a composite.</summary>
        public ushort Flags;                        // +0

        /// <summary>Number of axes at offset 2.</summary>
        public ushort NumberOfAxes;                 // +2

        /// <summary>Number of direct subparts at offset 4; the size of the subpart array.</summary>
        public ushort NumberOfSubparts;             // +4

        /// <summary>Total number of parts in the structure tree, including the composite's own root, at offset 6.</summary>
        public ushort TotalPartsInStructureTree;    // +6

        /// <summary>Sum of axis counts over every node in the structure tree, including the root, at offset 8.</summary>
        public ushort TotalAxesInStructureTree;     // +8

        /// <summary>Maximum of <c>2 × axes</c> over every part in the structure tree, at offset 10.</summary>
        public ushort MaxNumberOfExtremes;          // +10

        /// <summary>Number of non-zero master axis-value deltas at offset 12.</summary>
        public ushort CountOfMasterAxisValues;      // +12

        /// <summary>Number of non-zero extremum axis-value deltas at offset 14.</summary>
        public ushort CountOfExtremumAxisValues;    // +14

        /// <summary>Number of non-zero master translations at offset 16.</summary>
        public ushort CountOfMasterTranslations;    // +16

        /// <summary>Number of non-zero master rotations at offset 18.</summary>
        public ushort CountOfMasterRotations;       // +18

        /// <summary>Number of non-zero extremum translations at offset 20.</summary>
        public ushort CountOfExtremumTranslations;  // +20

        /// <summary>Number of non-zero extremum rotations at offset 22.</summary>
        public ushort CountOfExtremumRotations;     // +22

        /// <summary>Byte offset of the subpart array from the start of the composite, divided by four, at offset 24.</summary>
        public ushort OffsetToSubpartArray;         // +24

        /// <summary>Byte offset of the extremum column starts from the start of the composite, divided by four, at offset 26.</summary>
        public ushort OffsetToExtremumColumnStarts; // +26

        /// <summary>Byte offset of the master axis-value deltas from the start of the composite, divided by four, at offset 28.</summary>
        public ushort OffsetToMasterAxisDeltas;     // +28

        /// <summary>Byte offset of the extremum axis-value deltas from the start of the composite, divided by four, at offset 30.</summary>
        public ushort OffsetToExtremumAxisDeltas;   // +30

        /// <summary>Byte offset of the translation section from the start of the composite, divided by four, at offset 32.</summary>
        public ushort OffsetToAllTranslations;      // +32

        /// <summary>Byte offset of the rotation section from the start of the composite, divided by four, at offset 34.</summary>
        public ushort OffsetToAllRotations;         // +34

        /// <summary>Reverses the byte order of every field in one pass.</summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>A new header with all eighteen 16-bit fields reversed.</returns>
        /// <remarks>The header is entirely <c>UInt16</c> fields, so it is reinterpreted as a span of eighteen 16-bit words and reversed with a single <see cref="BinaryPrimitives.ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})"/> call.</remarks>
        public static Header ReverseEndianness(Header value)
        {
            Span<ushort> words = MemoryMarshal.CreateSpan(ref Unsafe.As<Header, ushort>(ref value), 18);
            BinaryPrimitives.ReverseEndianness(words, words);
            return value;
        }
    }

    /// <summary>An 8-byte subpart entry: a part index and the row offsets of the subpart's data in the composite's matrices.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The first immediate subpart of a composite always has both row offsets equal to zero.</description></item>
    /// <item><description>The entry mixes <c>UInt32</c> and <c>UInt16</c> fields, so <see cref="ReverseEndianness(SubpartEntry)"/> reverses field-by-field rather than as a word span.</description></item>
    /// <item><description>The subpart array must be aligned on a <c>UInt32</c> boundary within the composite.</description></item>
    /// </list>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct SubpartEntry : IEndianReversibleStruct<SubpartEntry>
    {
        /// <summary>The part table index this subpart renders, at offset 0.</summary>
        public uint PartTableIndex;    // +0

        /// <summary>Row offset of this subpart's data in the composite's transform matrices, at offset 4.</summary>
        public ushort TreePartOffset;  // +4

        /// <summary>Row offset of this subpart's data in the composite's axis-value matrices, at offset 6.</summary>
        public ushort TreeAxisOffset;  // +6

        /// <summary>Reverses the byte order of every multi-byte field.</summary>
        /// <param name="value">The value to reverse.</param>
        /// <returns>A new entry with each multi-byte field reversed.</returns>
        public static SubpartEntry ReverseEndianness(SubpartEntry value) => new()
        {
            PartTableIndex = BinaryPrimitives.ReverseEndianness(value.PartTableIndex),
            TreePartOffset = BinaryPrimitives.ReverseEndianness(value.TreePartOffset),
            TreeAxisOffset = BinaryPrimitives.ReverseEndianness(value.TreeAxisOffset),
        };
    }
}
