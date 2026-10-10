using Mubarrat.Fonts.Binary;

namespace Mubarrat.Fonts.Tables;

/// <summary>Base for Device and VariationIndex tables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Device tables give per-ppem delta adjustments that a rasterizer applies on top of a metric. VariationIndex tables give a delta-set index into an ItemVariationStore for use in variable fonts.</description></item>
/// <item><description>The two are distinguished by the third word of the record: <c>deltaFormat == 0x8000</c> signals a VariationIndex; any of <c>1</c>, <c>2</c>, or <c>3</c> signals a Device table with the corresponding packing.</description></item>
/// <item><description>This type uses a discriminator switch inside <see cref="IRecord{T}.Parse"/> rather than <see cref="IBaseRecord{TBase}"/>, because both variants share the same 6-byte prefix that the parser must consume to read the discriminant.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#device-tables">Device tables</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="VariationIndex"/>
/// <seealso cref="DeviceFormat"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#device-tables">OpenType specification: Device tables</seealso>
public abstract record Device : IRecord<Device>
{
    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the device record.</param>
    /// <param name="context">Unused. Both variants are self-describing once the discriminant is read.</param>
    /// <returns>A <see cref="VariationIndex"/> when the discriminant is <c>0x8000</c>, otherwise a <see cref="DeviceFormat"/>.</returns>
    /// <exception cref="InvalidDataException">The delta format word is not <c>0x8000</c>, <c>1</c>, <c>2</c>, or <c>3</c>.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The first three <c>uint16</c> words are consumed by this method for both variants; for a VariationIndex they are the outer and inner indices, and for a Device table they are the start size, end size, and delta format.</description></item>
    /// <item><description>When the discriminant indicates a Device table, the remaining words are read by <see cref="DeviceFormat.ParseBody"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#device-tables">Device tables</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="VariationIndex"/>
    /// <seealso cref="DeviceFormat"/>
    /// <seealso cref="DeviceFormat.ParseBody"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#device-tables">OpenType specification: Device tables</seealso>
    static Device IRecord<Device>.Parse(ref Cursor cursor, object? context)
    {
        ushort startSize = cursor.ReadUInt16();
        ushort endSize = cursor.ReadUInt16();
        ushort deltaFormat = cursor.ReadUInt16();

        if (deltaFormat == 0x8000)
            return new VariationIndex { OuterIndex = startSize, InnerIndex = endSize };

        return DeviceFormat.ParseBody(ref cursor, startSize, endSize, deltaFormat);
    }
}

/// <summary>VariationIndex: a delta-set index pair into an ItemVariationStore.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Used in variable fonts where a metric must vary with design space. The pair <c>(<see cref="OuterIndex"/>, <see cref="InnerIndex"/>)</c> addresses a single row of delta values inside the font's item variation store.</description></item>
/// <item><description>The record shares the 6-byte prefix of a Device table; the discriminant is <c>deltaFormat == 0x8000</c>, which stores the outer index in the position of the start-size word and the inner index in the position of the end-size word.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#variation-index-table">VariationIndex table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Device"/>
/// <seealso cref="DeviceFormat"/>
/// <seealso cref="ItemVariationStore"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#variation-index-table">OpenType specification: VariationIndex table</seealso>
public sealed record VariationIndex : Device
{
    /// <summary>Gets the outer index into the ItemVariationStore.</summary>
    /// <value>The zero-based index of the ItemVariationData subtable within the enclosing item variation store.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#variation-index-table"><c>deltaSetOuterIndex</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="InnerIndex"/>
    /// <seealso cref="ItemVariationStore"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#variation-index-table">OpenType specification: <c>deltaSetOuterIndex</c></seealso>
    public ushort OuterIndex { get; init; }

    /// <summary>Gets the inner index into the selected ItemVariationData subtable.</summary>
    /// <value>The zero-based index of the delta row within the selected ItemVariationData.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#variation-index-table"><c>deltaSetInnerIndex</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="OuterIndex"/>
    /// <seealso cref="ItemVariationStore"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#variation-index-table">OpenType specification: <c>deltaSetInnerIndex</c></seealso>
    public ushort InnerIndex { get; init; }
}

/// <summary>Device table with packed per-ppem delta adjustments.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table gives one signed delta per ppem in <c>[<see cref="StartSize"/>, <see cref="EndSize"/>]</c>. The deltas are packed at 2, 4, or 8 bits each, selected by <see cref="DeltaFormat"/>.</description></item>
/// <item><description>Two-bit deltas cover the range [-2, 1]; four-bit deltas cover [-8, 7]; eight-bit deltas cover [-128, 127]. A font uses the narrowest format that holds its largest delta.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#device-table">Device table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Device"/>
/// <seealso cref="VariationIndex"/>
/// <seealso cref="ParseBody"/>
/// <seealso cref="GetDelta(int)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#device-table">OpenType specification: Device table</seealso>
public sealed record DeviceFormat : Device
{
    /// <summary>Gets the first ppem.</summary>
    /// <value>The lowest ppem for which a delta is provided. Together with <see cref="EndSize"/> it defines the length of <see cref="Deltas"/>.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#device-table"><c>startSize</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="EndSize"/>
    /// <seealso cref="Deltas"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#device-table">OpenType specification: <c>startSize</c></seealso>
    public ushort StartSize { get; init; }

    /// <summary>Gets the last ppem.</summary>
    /// <value>The highest ppem for which a delta is provided. Together with <see cref="StartSize"/> it defines the length of <see cref="Deltas"/>.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#device-table"><c>endSize</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="StartSize"/>
    /// <seealso cref="Deltas"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#device-table">OpenType specification: <c>endSize</c></seealso>
    public ushort EndSize { get; init; }

    /// <summary>Gets the delta format (1, 2, or 3).</summary>
    /// <value><c>1</c> for two-bit deltas, <c>2</c> for four-bit deltas, <c>3</c> for eight-bit deltas.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#device-table"><c>deltaFormat</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Deltas"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#device-table">OpenType specification: <c>deltaFormat</c></seealso>
    public ushort DeltaFormat { get; init; }

    /// <summary>Gets the decoded deltas, one per ppem in [StartSize, EndSize].</summary>
    /// <value>The unpacked, sign-extended deltas. Length equals <c><see cref="EndSize"/> - <see cref="StartSize"/> + 1</c>.</value>
    /// <remarks>Entries are indexed by <c>ppem - <see cref="StartSize"/></c>; use <see cref="GetDelta(int)"/> to look up by ppem directly.</remarks>
    /// <seealso cref="StartSize"/>
    /// <seealso cref="EndSize"/>
    /// <seealso cref="GetDelta(int)"/>
    public int[] Deltas { get; init; } = [];

    /// <summary>Returns the delta for a ppem, or 0 when outside the range.</summary>
    /// <param name="ppem">The pixels-per-em size to look up.</param>
    /// <returns>The signed delta for the ppem, or <c>0</c> when the ppem falls outside <c>[<see cref="StartSize"/>, <see cref="EndSize"/>]</c>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup is constant-time: the ppem's offset from <see cref="StartSize"/> indexes directly into <see cref="Deltas"/>.</description></item>
    /// <item><description>Returning <c>0</c> for out-of-range ppems matches the specification's recommendation for ppem sizes outside the device table's declared range.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// int adjustment = device.GetDelta(12);
    /// </code>
    /// </example>
    /// <seealso cref="Deltas"/>
    /// <seealso cref="StartSize"/>
    /// <seealso cref="EndSize"/>
    public int GetDelta(int ppem)
    {
        int i = ppem - StartSize;
        return (uint)i < (uint)Deltas.Length ? Deltas[i] : 0;
    }

    /// <summary>Parses the body of a Device table after its three-word header has been consumed.</summary>
    /// <param name="cursor">Cursor positioned immediately after the <c>deltaFormat</c> word.</param>
    /// <param name="startSize">The first ppem, already read by <see cref="IRecord{T}.Parse"/>.</param>
    /// <param name="endSize">The last ppem, already read by <see cref="IRecord{T}.Parse"/>.</param>
    /// <param name="deltaFormat">The delta format discriminant, already read by <see cref="IRecord{T}.Parse"/>.</param>
    /// <returns>A <see cref="DeviceFormat"/> with its deltas unpacked and sign-extended.</returns>
    /// <exception cref="InvalidDataException"><paramref name="deltaFormat"/> is not 1, 2, or 3.</exception>
    /// <exception cref="EndOfStreamException">The packed word array extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Deltas are packed most-significant-first within each <c>uint16</c> word: the first delta occupies the highest bits, and the last delta of a word occupies the lowest bits.</description></item>
    /// <item><description>Each delta is a signed two's-complement value of <c>bits</c> width; the parser sign-extends it by subtracting <c>2^bits</c> when the sign bit is set.</description></item>
    /// <item><description>The word count is <c>ceil(count / (16 / bits))</c>; trailing bits in the final word beyond <c>count</c> are ignored.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#device-table">Device table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IRecord{T}.Parse"/>
    /// <seealso cref="Deltas"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#device-table">OpenType specification: Device table</seealso>
    public static DeviceFormat ParseBody(ref Cursor cursor, ushort startSize, ushort endSize, ushort deltaFormat)
    {
        int bits = deltaFormat switch
        {
            1 => 2,
            2 => 4,
            3 => 8,
            _ => throw new InvalidDataException($"Device deltaFormat {deltaFormat} is not defined."),
        };
        int count = endSize - startSize + 1;
        int perWord = 16 / bits;
        ushort[] words = cursor.ReadUInt16Array((count + perWord - 1) / perWord);

        var deltas = new int[count];
        int shift = 16 - bits, mask = (1 << bits) - 1, signBit = 1 << (bits - 1);
        for (int i = 0; i < count; i++)
        {
            int raw = (words[i / perWord] >> (shift - (i % perWord) * bits)) & mask;
            deltas[i] = (raw & signBit) != 0 ? raw - (1 << bits) : raw;
        }
        return new DeviceFormat { StartSize = startSize, EndSize = endSize, DeltaFormat = deltaFormat, Deltas = deltas };
    }
}
