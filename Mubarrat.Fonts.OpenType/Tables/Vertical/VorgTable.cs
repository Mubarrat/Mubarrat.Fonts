using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Tables.Vertical;

// ═══════════════════════════════════════════════════════════════════════════════════════
// VORG — Vertical Origin Table (CFF fonts only)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>VORG</c> table: vertical origin coordinates for glyphs in a CFF-flavored OpenType font.</summary>
/// <remarks>The table is optional and meaningful only for CFF-flavored fonts. It supplies the y coordinate of the vertical origin and complements <c>vmtx</c>, which stores advance heights. The <c>vertOriginYMetrics</c> array is sorted by increasing glyph ID; glyphs without an override use <see cref="DefaultVertOriginY"/>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"><c>VORG</c> specification</see>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"/>
public sealed record VorgTable : IOpenTypeTable<VorgTable>
{
    /// <summary>Gets the OpenType table tag <c>VORG</c>.</summary>
    public static Tag Tag => "VORG";

    /// <summary>Gets the major version.</summary>
    /// <remarks>Must be 1.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version.</summary>
    /// <remarks>Must be 0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the default vertical origin y coordinate.</summary>
    /// <remarks>This value is used for glyphs without a corresponding entry in <see cref="VertOriginYMetrics"/>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"/>
    public short DefaultVertOriginY { get; init; }

    /// <summary>Gets the per-glyph vertical origin overrides, sorted by increasing glyph ID.</summary>
    /// <remarks>A glyph whose origin equals <see cref="DefaultVertOriginY"/> can be omitted from this array.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"/>
    public IReadOnlyList<VertOriginYMetric> VertOriginYMetrics { get; init; } = [];

    /// <summary>Gets the number of per-glyph vertical origin overrides.</summary>
    public int Count => VertOriginYMetrics.Count;

    /// <summary>Gets the vertical origin y coordinate of <paramref name="glyphId"/>.</summary>
    /// <remarks>Returns the per-glyph override when present; otherwise returns <see cref="DefaultVertOriginY"/>. The override array is searched by binary search.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"/>
    public short GetVerticalOrigin(int glyphId)
    {
        // Binary search: the array is sorted by glyph ID.
        int lo = 0, hi = VertOriginYMetrics.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            var rec = VertOriginYMetrics[mid];
            int cmp = rec.GlyphIndex - glyphId;
            if (cmp == 0) return rec.VertOriginY;
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }
        return DefaultVertOriginY;
    }

    /// <summary>The 8-byte fixed-layout <c>VORG</c> table header.</summary>
    /// <remarks>Fields are stored in big-endian order at the offsets defined by the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"><c>VORG</c> specification</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>The major version at byte offset 0.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"/>
        public ushort Major;                 // +0

        /// <summary>The minor version at byte offset 2.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"/>
        public ushort Minor;                 // +2

        /// <summary>The default vertical origin y coordinate at byte offset 4.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"/>
        public short DefaultVertOriginY;     // +4

        /// <summary>The number of <c>vertOriginYMetrics</c> records at byte offset 6.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"/>
        public ushort NumVertOriginYMetrics; // +6

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        public static Header ReverseEndianness(Header v) => new()
        {
            Major = BinaryPrimitives.ReverseEndianness(v.Major),
            Minor = BinaryPrimitives.ReverseEndianness(v.Minor),
            DefaultVertOriginY = BinaryPrimitives.ReverseEndianness(v.DefaultVertOriginY),
            NumVertOriginYMetrics = BinaryPrimitives.ReverseEndianness(v.NumVertOriginYMetrics),
        };
    }

    /// <inheritdoc/>
    static VorgTable IRecord<VorgTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new VorgTable
        {
            MajorVersion = header.Major,
            MinorVersion = header.Minor,
            DefaultVertOriginY = header.DefaultVertOriginY,
            VertOriginYMetrics = cursor.ReadBigEndianStructArray<VertOriginYMetric>(header.NumVertOriginYMetrics),
        };
    }
}

/// <summary>A 4-byte fixed-layout <c>vertOriginYMetrics</c> record containing a glyph ID and its vertical origin y coordinate.</summary>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct VertOriginYMetric : IBigEndianStruct<VertOriginYMetric>
{
    /// <summary>The glyph ID at byte offset 0.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"/>
    public ushort GlyphIndex;      // +0

    /// <summary>The vertical origin y coordinate at byte offset 2, in font units.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vorg"/>
    public short VertOriginY;      // +2

    /// <summary>Reverses the byte order of both fields in a <see cref="VertOriginYMetric"/>.</summary>
    public static VertOriginYMetric ReverseEndianness(VertOriginYMetric v) => new()
    {
        GlyphIndex = BinaryPrimitives.ReverseEndianness(v.GlyphIndex),
        VertOriginY = BinaryPrimitives.ReverseEndianness(v.VertOriginY),
    };
}
