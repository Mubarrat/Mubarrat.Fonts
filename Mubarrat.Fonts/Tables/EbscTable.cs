using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>EBSC</c> table (version 2.0): Embedded Bitmap Scaling Table. Declares strikes that are produced by scaling an existing strike.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>A font may declare a strike at one ppem and let the rasterizer produce additional strikes at nearby ppems by scaling the source bitmap. This table records which source strike is scaled to produce each target strike, along with the line metrics that result.</description></item>
/// <item><description>Only the target strike sizes are declared here; the source strikes must already exist in the paired <c>EBLC</c>/<c>EBDT</c> tables.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">EBSC table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="BitmapScale"/>
/// <seealso cref="EblcTable"/>
/// <seealso cref="EbdtTable"/>
/// <seealso cref="FontFace"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: EBSC table</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/eblc">OpenType specification: EBLC table</seealso>
public sealed record EbscTable : IFontTable<EbscTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: EBSC table</seealso>
    public static Tag Tag => "EBSC";

    /// <summary>Gets the major version. Always 2.</summary>
    /// <value>The constant <c>2</c> for a conforming EBSC table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc"><c>majorVersion</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MinorVersion"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: <c>majorVersion</c></seealso>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version. Always 0.</summary>
    /// <value>The constant <c>0</c> for a conforming EBSC table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc"><c>minorVersion</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="MajorVersion"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: <c>minorVersion</c></seealso>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the scaling records.</summary>
    /// <value>The list of <see cref="BitmapScale"/> entries, one per declared target strike. Empty when the table declares no scaling records.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Each entry names a target ppem pair and the source ppem pair that is scaled to produce it.</description></item>
    /// <item><description>The list order follows the on-disk order in the table; it is not required to be sorted by ppem.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetScale(int, int)"/>
    /// <seealso cref="BitmapScale"/>
    public IReadOnlyList<BitmapScale> Strikes { get; init; } = [];

    /// <summary>Returns the scaling record that produces the requested target ppem, or <c>null</c>.</summary>
    /// <param name="ppemX">The target horizontal pixels per em to search for.</param>
    /// <param name="ppemY">The target vertical pixels per em to search for.</param>
    /// <returns>The first <see cref="BitmapScale"/> whose <see cref="BitmapScale.PpemX"/> and <see cref="BitmapScale.PpemY"/> match, or <c>null</c> when no record matches.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup is linear over <see cref="Strikes"/>. The table is typically small, so the linear scan is not usually a concern.</description></item>
    /// <item><description>The lookup matches the <em>target</em> ppem pair, not the source. To find what a given existing strike scales to, search by <see cref="BitmapScale.PpemX"/>/<see cref="BitmapScale.PpemY"/> of each entry, not the substitute fields.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Strikes"/>
    /// <seealso cref="BitmapScale.PpemX"/>
    /// <seealso cref="BitmapScale.PpemY"/>
    public BitmapScale? GetScale(int ppemX, int ppemY)
    {
        foreach (var s in Strikes)
            if (s.PpemX == ppemX && s.PpemY == ppemY) return s;
        return null;
    }

    /// <summary>The 8-byte EBSC v2.0 header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by <c>numSizes</c> 28-byte <see cref="BitmapScale"/> records.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">EBSC header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="EbscTable"/>
    /// <seealso cref="BitmapScale"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: EBSC header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the major version. Always 2.</summary>
        /// <value>The constant <c>2</c> for a conforming EBSC table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc"><c>majorVersion</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="MinorVersion"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: <c>majorVersion</c></seealso>
        public ushort MajorVersion;   // +0

        /// <summary>Gets the minor version. Always 0.</summary>
        /// <value>The constant <c>0</c> for a conforming EBSC table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc"><c>minorVersion</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="MajorVersion"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: <c>minorVersion</c></seealso>
        public ushort MinorVersion;   // +2

        /// <summary>Gets the number of scaling records. Must not exceed 1024.</summary>
        /// <value>The count of <see cref="BitmapScale"/> records that follow the header.</value>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc"><c>numSizes</c> field</see> in the OpenType specification.</description></item>
        /// <item><description>The parser rejects counts above 1024 as a safety limit against malformed files.</description></item>
        /// </list>
        /// </remarks>
        /// <seealso cref="BitmapScale"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: <c>numSizes</c></seealso>
        public uint NumSizes;         // +4

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are multi-byte and are reversed independently; there are no byte-only or nested structures to copy through.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: EBSC header</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            MajorVersion = BinaryPrimitives.ReverseEndianness(v.MajorVersion),
            MinorVersion = BinaryPrimitives.ReverseEndianness(v.MinorVersion),
            NumSizes = BinaryPrimitives.ReverseEndianness(v.NumSizes),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the EBSC table.</param>
    /// <param name="context">Unused. EBSC has no external table dependencies, so the face is not needed for this parse.</param>
    /// <returns>The parsed EBSC table.</returns>
    /// <exception cref="InvalidDataException">The version is not 2.0, or the record count exceeds the safety limit.</exception>
    /// <exception cref="EndOfStreamException">The header or the scaling-record array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Unlike EBLC/CBLC, EBSC does not reference other tables during parse; the <paramref name="context"/> is declared for interface uniformity but not read.</description></item>
    /// <item><description>The scaling records are read with <c>ReadStructArray</c> rather than <c>ReadBigEndianStructArray</c> because <see cref="BitmapScale"/> contains only byte-width fields apart from its nested <see cref="SbitLineMetrics"/>, which are also byte-only. No multi-byte reversal is needed.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">EBSC table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="BitmapScale"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: EBSC table</seealso>
    static EbscTable IRecord<EbscTable>.Parse(ref Cursor cursor, object? context)
    {
        Header h = cursor.ReadBigEndianStruct<Header>();

        if (h.MajorVersion != 2 || h.MinorVersion != 0)
            throw new InvalidDataException(
                $"'EBSC' version is {h.MajorVersion}.{h.MinorVersion}, expected 2.0.");
        if (h.NumSizes > 1024)
            throw new InvalidDataException(
                $"'EBSC' declares {h.NumSizes} scaling records, exceeding the safety limit.");

        return new EbscTable
        {
            MajorVersion = h.MajorVersion,
            MinorVersion = h.MinorVersion,
            Strikes = cursor.ReadStructArray<BitmapScale>(checked((int)h.NumSizes)),
        };
    }
}

/// <summary>A single BitmapScale record. Blittable, size 28.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The line metrics describe font-wide metrics <em>after</em> scaling.</description></item>
/// <item><description><see cref="PpemX"/> and <see cref="PpemY"/> are the target size.</description></item>
/// <item><description><see cref="SubstitutePpemX"/> and <see cref="SubstitutePpemY"/> identify the existing strike that will be scaled to generate this strike.</description></item>
/// <item><description>Scaling in the x and y directions is independent and may differ.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc"><c>bitmapScaleTable</c></see> section of the specification.</description></item>
/// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="EbscTable"/>
/// <seealso cref="SbitLineMetrics"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: <c>bitmapScaleTable</c></seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct BitmapScale
{
    /// <summary>Horizontal line metrics after scaling.</summary>
    /// <value>The <see cref="SbitLineMetrics"/> that describe the target strike when rendered in horizontal layout.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc"><c>hori</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Vert"/>
    /// <seealso cref="SbitLineMetrics"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: <c>hori</c></seealso>
    public SbitLineMetrics Hori;

    /// <summary>Vertical line metrics after scaling.</summary>
    /// <value>The <see cref="SbitLineMetrics"/> that describe the target strike when rendered in vertical layout.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc"><c>vert</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Hori"/>
    /// <seealso cref="SbitLineMetrics"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: <c>vert</c></seealso>
    public SbitLineMetrics Vert;

    /// <summary>Target horizontal ppem.</summary>
    /// <value>The horizontal resolution of the strike this record declares.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc"><c>ppemX</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="PpemY"/>
    /// <seealso cref="SubstitutePpemX"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: <c>ppemX</c></seealso>
    public byte PpemX;

    /// <summary>Target vertical ppem.</summary>
    /// <value>The vertical resolution of the strike this record declares.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc"><c>ppemY</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="PpemX"/>
    /// <seealso cref="SubstitutePpemY"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: <c>ppemY</c></seealso>
    public byte PpemY;

    /// <summary>Source strike horizontal ppem used as input.</summary>
    /// <value>The horizontal resolution of the existing strike that is scaled to produce this target.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc"><c>substitutePpemX</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="PpemX"/>
    /// <seealso cref="SubstitutePpemY"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: <c>substitutePpemX</c></seealso>
    public byte SubstitutePpemX;

    /// <summary>Source strike vertical ppem used as input.</summary>
    /// <value>The vertical resolution of the existing strike that is scaled to produce this target.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc"><c>substitutePpemY</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="PpemY"/>
    /// <seealso cref="SubstitutePpemX"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebsc">OpenType specification: <c>substitutePpemY</c></seealso>
    public byte SubstitutePpemY;
}
