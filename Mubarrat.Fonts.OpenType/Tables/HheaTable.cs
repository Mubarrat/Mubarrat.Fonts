using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Tables;

/// <summary>Represents the OpenType <c>hhea</c> table containing horizontal header metrics and the number of horizontal metric records.</summary>
/// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea">The table provides horizontal layout metrics and determines how many <c>longHorMetric</c> records are present in <c>hmtx</c>.</see> <list type="bullet"><item><description><see cref="NumberOfHMetrics"/> determines the number of full horizontal metric records in <c>hmtx</c>.</description></item><item><description><see cref="Ascender"/>, <see cref="Descender"/>, and <see cref="LineGap"/> are primarily associated with Apple's horizontal layout model.</description></item><item><description>The minimum sidebearing and extent fields describe glyph metrics and ignore glyphs without contours.</description></item></list></remarks>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea">OpenType <c>hhea</c> table specification</seealso>
public sealed record HheaTable : IOpenTypeTable<HheaTable>, IBigEndianHeaderRecord<HheaTable, HheaTable.Header>
{
    /// <summary>Gets the OpenType table tag.</summary>
    /// <value>The tag <c>hhea</c>.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#table-directory">The tag identifies the table in the font's table directory.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea">OpenType <c>hhea</c> table specification</seealso>
    public static Tag Tag => "hhea";

    /// <summary>Gets the version of the <c>hhea</c> table.</summary>
    /// <value>The required value is <c>0x00010000</c>.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#version">The field is a 16.16 fixed-point version value.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#version">OpenType <c>version</c></seealso>
    public uint Version { get; init; }

    /// <summary>Gets the typographic ascent.</summary>
    /// <value>The distance from the baseline to the highest ascender, in font design units.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#ascender">The field defines the typographic ascender used by the horizontal header metrics.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#ascender">OpenType <c>ascender</c></seealso>
    public short Ascender { get; init; }

    /// <summary>Gets the typographic descent.</summary>
    /// <value>The distance from the baseline to the lowest descender, in font design units.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#descender">The field defines the typographic descender used by the horizontal header metrics.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#descender">OpenType <c>descender</c></seealso>
    public short Descender { get; init; }

    /// <summary>Gets the typographic line gap.</summary>
    /// <value>The additional vertical space between lines, in font design units.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#linegap">Negative values are treated as zero by some historical Apple and Windows systems.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#linegap">OpenType <c>lineGap</c></seealso>
    public short LineGap { get; init; }

    /// <summary>Gets the maximum advance width.</summary>
    /// <value>The maximum advance width among the horizontal metrics, in font design units.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#advancewidthmax">The value is the maximum advance width represented in the <c>hmtx</c> table.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#advancewidthmax">OpenType <c>advanceWidthMax</c></seealso>
    public ushort AdvanceWidthMax { get; init; }

    /// <summary>Gets the minimum left sidebearing.</summary>
    /// <value>The minimum left sidebearing among glyphs with contours, in font design units.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#minleftsidebearing">The value is the minimum left sidebearing represented by the glyph metrics.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#minleftsidebearing">OpenType <c>minLeftSideBearing</c></seealso>
    public short MinLeftSideBearing { get; init; }

    /// <summary>Gets the minimum right sidebearing.</summary>
    /// <value>The minimum value of <c>advanceWidth - lsb - (xMax - xMin)</c>, in font design units.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#minrightsidebearing">The value is calculated from the advance width, left sidebearing, and glyph bounding box.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#minrightsidebearing">OpenType <c>minRightSideBearing</c></seealso>
    public short MinRightSideBearing { get; init; }

    /// <summary>Gets the maximum horizontal extent.</summary>
    /// <value>The maximum value of <c>lsb + (xMax - xMin)</c>, in font design units.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#xmaxextent">The value represents the maximum horizontal extent of glyphs with contours.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#xmaxextent">OpenType <c>xMaxExtent</c></seealso>
    public short XMaxExtent { get; init; }

    /// <summary>Gets the numerator of the cursor slope.</summary>
    /// <value>The rise component of the cursor slope expressed as <c>rise/run</c>.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#caretsloperise">A value of 1 is used for a vertical cursor.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#caretsloperise">OpenType <c>caretSlopeRise</c></seealso>
    public short CaretSlopeRise { get; init; }

    /// <summary>Gets the denominator of the cursor slope.</summary>
    /// <value>The run component of the cursor slope expressed as <c>rise/run</c>.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#caretsloperun">A value of 0 is used for a vertical cursor.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#caretsloperun">OpenType <c>caretSlopeRun</c></seealso>
    public short CaretSlopeRun { get; init; }

    /// <summary>Gets the cursor offset.</summary>
    /// <value>The horizontal offset applied to a slanted text cursor.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#caretoffset">The value is normally zero for non-slanted fonts.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#caretoffset">OpenType <c>caretOffset</c></seealso>
    public short CaretOffset { get; init; }

    /// <summary>Gets the metric data format.</summary>
    /// <value>The required value is <c>0</c> for the current format.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#metricdataformat">The field is reserved for future use and must be zero for the current format.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#metricdataformat">OpenType <c>metricDataFormat</c></seealso>
    public short MetricDataFormat { get; init; }

    /// <summary>Gets the number of <c>longHorMetric</c> records in the <c>hmtx</c> table.</summary>
    /// <value>The number of full horizontal metric records.</value>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#numberofhmetrics">If the value is less than the number of glyphs, the advance width of the final full metric record applies to all remaining glyphs.</see></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#numberofhmetrics">OpenType <c>numberOfHMetrics</c></seealso>
    public ushort NumberOfHMetrics { get; init; }

    /// <inheritdoc/>
    static HheaTable IHeaderRecord<HheaTable, Header>.FromHeader(in Header header, object? context) => new()
    {
        Version = header.Version,
        Ascender = header.Ascender,
        Descender = header.Descender,
        LineGap = header.LineGap,
        AdvanceWidthMax = header.AdvanceWidthMax,
        MinLeftSideBearing = header.MinLeftSideBearing,
        MinRightSideBearing = header.MinRightSideBearing,
        XMaxExtent = header.XMaxExtent,
        CaretSlopeRise = header.CaretSlopeRise,
        CaretSlopeRun = header.CaretSlopeRun,
        CaretOffset = header.CaretOffset,
        MetricDataFormat = header.MetricDataFormat,
        NumberOfHMetrics = header.NumberOfHMetrics,
    };

    /// <summary>Represents the 36-byte big-endian wire representation of the <c>hhea</c> table.</summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea">The structure is packed with no padding and follows the OpenType field layout exactly.</see> <list type="bullet"><item><description>Bytes 0-3 contain <see cref="Version"/>.</description></item><item><description>Bytes 4-23 contain horizontal metrics and caret slope values.</description></item><item><description>Bytes 24-31 contain four reserved fields that must be zero.</description></item><item><description>Bytes 32-35 contain <see cref="MetricDataFormat"/> and <see cref="NumberOfHMetrics"/>.</description></item></list></remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea">OpenType <c>hhea</c> table specification</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Bytes 0-3: table version.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#version">The value is normally <c>0x00010000</c>.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#version">OpenType <c>version</c></seealso>
        public uint Version;

        /// <summary>Bytes 4-5: typographic ascent.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#ascender">The value is measured in font design units.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#ascender">OpenType <c>ascender</c></seealso>
        public short Ascender;

        /// <summary>Bytes 6-7: typographic descent.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#descender">The value is measured in font design units.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#descender">OpenType <c>descender</c></seealso>
        public short Descender;

        /// <summary>Bytes 8-9: typographic line gap.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#linegap">The value is measured in font design units.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#linegap">OpenType <c>lineGap</c></seealso>
        public short LineGap;

        /// <summary>Bytes 10-11: maximum advance width.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#advancewidthmax">The value is the maximum advance width in the horizontal metrics.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#advancewidthmax">OpenType <c>advanceWidthMax</c></seealso>
        public ushort AdvanceWidthMax;

        /// <summary>Bytes 12-13: minimum left sidebearing.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#minleftsidebearing">The value is measured in font design units.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#minleftsidebearing">OpenType <c>minLeftSideBearing</c></seealso>
        public short MinLeftSideBearing;

        /// <summary>Bytes 14-15: minimum right sidebearing.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#minrightsidebearing">The value is derived from advance width, left sidebearing, and glyph extent.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#minrightsidebearing">OpenType <c>minRightSideBearing</c></seealso>
        public short MinRightSideBearing;

        /// <summary>Bytes 16-17: maximum horizontal extent.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#xmaxextent">The value is the maximum horizontal extent of glyphs with contours.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#xmaxextent">OpenType <c>xMaxExtent</c></seealso>
        public short XMaxExtent;

        /// <summary>Bytes 18-19: cursor slope rise.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#caretsloperise">The field is the rise component of the cursor slope.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#caretsloperise">OpenType <c>caretSlopeRise</c></seealso>
        public short CaretSlopeRise;

        /// <summary>Bytes 20-21: cursor slope run.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#caretsloperun">The field is the run component of the cursor slope.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#caretsloperun">OpenType <c>caretSlopeRun</c></seealso>
        public short CaretSlopeRun;

        /// <summary>Bytes 22-23: cursor offset.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#caretoffset">The field specifies the offset required for a slanted cursor.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#caretoffset">OpenType <c>caretOffset</c></seealso>
        public short CaretOffset;

        /// <summary>Bytes 24-25: reserved field.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea">The field is reserved and must be zero.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea">OpenType <c>hhea</c> table</seealso>
        public short Reserved1;

        /// <summary>Bytes 26-27: reserved field.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea">The field is reserved and must be zero.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea">OpenType <c>hhea</c> table</seealso>
        public short Reserved2;

        /// <summary>Bytes 28-29: reserved field.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea">The field is reserved and must be zero.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea">OpenType <c>hhea</c> table</seealso>
        public short Reserved3;

        /// <summary>Bytes 30-31: reserved field.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea">The field is reserved and must be zero.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea">OpenType <c>hhea</c> table</seealso>
        public short Reserved4;

        /// <summary>Bytes 32-33: metric data format.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#metricdataformat">The value must be zero for the current format.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#metricdataformat">OpenType <c>metricDataFormat</c></seealso>
        public short MetricDataFormat;

        /// <summary>Bytes 34-35: number of horizontal metric records.</summary>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#numberofhmetrics">The value determines how many <c>longHorMetric</c> records are present in <c>hmtx</c>.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/hhea#numberofhmetrics">OpenType <c>numberOfHMetrics</c></seealso>
        public ushort NumberOfHMetrics;

        /// <summary>Reverses the byte order of every multi-byte field in the record.</summary>
        /// <param name="v">The record whose fields are to be byte-swapped.</param>
        /// <returns>A record with every multi-byte field converted to the opposite byte order.</returns>
        /// <remarks><see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType scalar fields are stored in big-endian byte order.</see></remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType data types</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            Ascender = BinaryPrimitives.ReverseEndianness(v.Ascender),
            Descender = BinaryPrimitives.ReverseEndianness(v.Descender),
            LineGap = BinaryPrimitives.ReverseEndianness(v.LineGap),
            AdvanceWidthMax = BinaryPrimitives.ReverseEndianness(v.AdvanceWidthMax),
            MinLeftSideBearing = BinaryPrimitives.ReverseEndianness(v.MinLeftSideBearing),
            MinRightSideBearing = BinaryPrimitives.ReverseEndianness(v.MinRightSideBearing),
            XMaxExtent = BinaryPrimitives.ReverseEndianness(v.XMaxExtent),
            CaretSlopeRise = BinaryPrimitives.ReverseEndianness(v.CaretSlopeRise),
            CaretSlopeRun = BinaryPrimitives.ReverseEndianness(v.CaretSlopeRun),
            CaretOffset = BinaryPrimitives.ReverseEndianness(v.CaretOffset),
            Reserved1 = BinaryPrimitives.ReverseEndianness(v.Reserved1),
            Reserved2 = BinaryPrimitives.ReverseEndianness(v.Reserved2),
            Reserved3 = BinaryPrimitives.ReverseEndianness(v.Reserved3),
            Reserved4 = BinaryPrimitives.ReverseEndianness(v.Reserved4),
            MetricDataFormat = BinaryPrimitives.ReverseEndianness(v.MetricDataFormat),
            NumberOfHMetrics = BinaryPrimitives.ReverseEndianness(v.NumberOfHMetrics),
        };
    }
}
