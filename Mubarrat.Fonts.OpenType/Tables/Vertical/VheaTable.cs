using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Tables.Vertical;

/// <summary>The <c>vhea</c> table: vertical metrics header information and the number of vertical metrics in <c>vmtx</c>.</summary>
/// <remarks>The table is 36 bytes and corresponds to the horizontal <c>hhea</c> table. Version 1.1 uses the names <c>ascender</c>, <c>descender</c>, and <c>lineGap</c>; version 1.0 uses <c>ascent</c>, <c>descent</c>, and a reserved line-gap field. The <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea">OpenType <c>vhea</c> specification</see> defines the field semantics and their relationship to <c>vmtx</c>.</remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"/>
public sealed record VheaTable : IOpenTypeTable<VheaTable>, IBigEndianHeaderRecord<VheaTable, VheaTable.Header>
{
    /// <summary>Gets the OpenType table tag <c>vhea</c>.</summary>
    public static Tag Tag => "vhea";

    /// <summary>Gets the table version. Version 1.0 is <c>0x00010000</c>; version 1.1 is <c>0x00011000</c>.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public uint Version { get; init; }

    /// <summary>Gets the vertical typographic ascender, measured from the centerline to the previous line's descent.</summary>
    /// <remarks>In version 1.0 this field is named <c>ascent</c>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public short Ascender { get; init; }

    /// <summary>Gets the vertical typographic descender, measured from the centerline to the next line's ascent.</summary>
    /// <remarks>In version 1.0 this field is named <c>descent</c>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public short Descender { get; init; }

    /// <summary>Gets the vertical typographic line gap.</summary>
    /// <remarks>Reserved and set to 0 in version 1.0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public short LineGap { get; init; }

    /// <summary>Gets the maximum advance height in the font, in font units.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public short AdvanceHeightMax { get; init; }

    /// <summary>Gets the minimum top sidebearing in the font, in font units.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public short MinTopSideBearing { get; init; }

    /// <summary>Gets the minimum bottom sidebearing in the font, in font units.</summary>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public short MinBottomSideBearing { get; init; }

    /// <summary>Gets the maximum extent of a glyph perpendicular to the vertical advance direction.</summary>
    /// <remarks>Defined as <c>minTopSideBearing + (yMax - yMin)</c>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public short YMaxExtent { get; init; }

    /// <summary>Gets the caret slope rise.</summary>
    /// <remarks>For vertical fonts, a horizontal caret is preferred, with a rise of 0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public short CaretSlopeRise { get; init; }

    /// <summary>Gets the caret slope run.</summary>
    /// <remarks>The value is 1 for a non-slanted vertical font.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public short CaretSlopeRun { get; init; }

    /// <summary>Gets the amount by which a slanted caret or highlight is shifted.</summary>
    /// <remarks>The value is 0 for a non-slanted font.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public short CaretOffset { get; init; }

    /// <summary>Gets the first reserved field.</summary>
    /// <remarks>Set to 0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public short Reserved1 { get; init; }

    /// <summary>Gets the second reserved field.</summary>
    /// <remarks>Set to 0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public short Reserved2 { get; init; }

    /// <summary>Gets the third reserved field.</summary>
    /// <remarks>Set to 0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public short Reserved3 { get; init; }

    /// <summary>Gets the fourth reserved field.</summary>
    /// <remarks>Set to 0.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public short Reserved4 { get; init; }

    /// <summary>Gets the metric data format.</summary>
    /// <remarks>Set to 0 for the current format.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    public short MetricDataFormat { get; init; }

    /// <summary>Gets the number of <c>longVerMetric</c> records in the <c>vmtx</c> table.</summary>
    /// <remarks>If less than <c>maxp.numGlyphs</c>, the advance height of the last metric record applies to all remaining glyphs. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"><c>vmtx</c> specification</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vmtx"/>
    public ushort NumberOfVMetrics { get; init; }

    /// <summary>The 36-byte fixed-layout <c>vhea</c> table header.</summary>
    /// <remarks>Fields are stored in big-endian order at the offsets defined by the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"><c>vhea</c> specification</see>.</remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>The table version at byte offset 0.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public uint Version;                 // +0

        /// <summary>The vertical ascender at byte offset 4.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public short Ascender;               // +4

        /// <summary>The vertical descender at byte offset 6.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public short Descender;              // +6

        /// <summary>The vertical line gap at byte offset 8.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public short LineGap;                // +8

        /// <summary>The maximum advance height at byte offset 10.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public short AdvanceHeightMax;       // +10

        /// <summary>The minimum top sidebearing at byte offset 12.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public short MinTopSideBearing;      // +12

        /// <summary>The minimum bottom sidebearing at byte offset 14.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public short MinBottomSideBearing;   // +14

        /// <summary>The maximum glyph extent at byte offset 16.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public short YMaxExtent;             // +16

        /// <summary>The caret slope rise at byte offset 18.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public short CaretSlopeRise;         // +18

        /// <summary>The caret slope run at byte offset 20.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public short CaretSlopeRun;          // +20

        /// <summary>The caret offset at byte offset 22.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public short CaretOffset;            // +22

        /// <summary>The first reserved field at byte offset 24.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public short Reserved1;              // +24

        /// <summary>The second reserved field at byte offset 26.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public short Reserved2;              // +26

        /// <summary>The third reserved field at byte offset 28.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public short Reserved3;              // +28

        /// <summary>The fourth reserved field at byte offset 30.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public short Reserved4;              // +30

        /// <summary>The metric data format at byte offset 32.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public short MetricDataFormat;       // +32

        /// <summary>The number of vertical metrics at byte offset 34.</summary>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/vhea"/>
        public ushort NumberOfVMetrics;      // +34

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            Ascender = BinaryPrimitives.ReverseEndianness(v.Ascender),
            Descender = BinaryPrimitives.ReverseEndianness(v.Descender),
            LineGap = BinaryPrimitives.ReverseEndianness(v.LineGap),
            AdvanceHeightMax = BinaryPrimitives.ReverseEndianness(v.AdvanceHeightMax),
            MinTopSideBearing = BinaryPrimitives.ReverseEndianness(v.MinTopSideBearing),
            MinBottomSideBearing = BinaryPrimitives.ReverseEndianness(v.MinBottomSideBearing),
            YMaxExtent = BinaryPrimitives.ReverseEndianness(v.YMaxExtent),
            CaretSlopeRise = BinaryPrimitives.ReverseEndianness(v.CaretSlopeRise),
            CaretSlopeRun = BinaryPrimitives.ReverseEndianness(v.CaretSlopeRun),
            CaretOffset = BinaryPrimitives.ReverseEndianness(v.CaretOffset),
            Reserved1 = BinaryPrimitives.ReverseEndianness(v.Reserved1),
            Reserved2 = BinaryPrimitives.ReverseEndianness(v.Reserved2),
            Reserved3 = BinaryPrimitives.ReverseEndianness(v.Reserved3),
            Reserved4 = BinaryPrimitives.ReverseEndianness(v.Reserved4),
            MetricDataFormat = BinaryPrimitives.ReverseEndianness(v.MetricDataFormat),
            NumberOfVMetrics = BinaryPrimitives.ReverseEndianness(v.NumberOfVMetrics),
        };
    }

    /// <inheritdoc/>
    static VheaTable IHeaderRecord<VheaTable, Header>.FromHeader(in Header header, object? context) => new()
    {
        Version = header.Version,
        Ascender = header.Ascender,
        Descender = header.Descender,
        LineGap = header.LineGap,
        AdvanceHeightMax = header.AdvanceHeightMax,
        MinTopSideBearing = header.MinTopSideBearing,
        MinBottomSideBearing = header.MinBottomSideBearing,
        YMaxExtent = header.YMaxExtent,
        CaretSlopeRise = header.CaretSlopeRise,
        CaretSlopeRun = header.CaretSlopeRun,
        CaretOffset = header.CaretOffset,
        Reserved1 = header.Reserved1,
        Reserved2 = header.Reserved2,
        Reserved3 = header.Reserved3,
        Reserved4 = header.Reserved4,
        MetricDataFormat = header.MetricDataFormat,
        NumberOfVMetrics = header.NumberOfVMetrics,
    };
}
