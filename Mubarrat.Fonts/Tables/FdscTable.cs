using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// fdsc — Font Descriptors Table (Apple Advanced Typography)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>fdsc</c> table: the descriptive information an application needs in order to substitute a font family for a run of text while preserving the original style.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is an Apple Advanced Typography (AAT) table and is not part of OpenType. Apple documents it in the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual, <c>fdsc</c> table</see>.</description></item>
/// <item><description>The table is a list of <c>&lt;tag, value&gt;</c> pairs, one pair per descriptor, that characterize the font: weight, width, slant, optical size, and whether the font is non-alphabetic. An application can match a run of text from one family to another by comparing the descriptors of the two fonts. The manual advises that all fonts should carry an <c>fdsc</c> table so that applications supporting family conversion work correctly.</description></item>
/// <item><description>Apple maintains a registry of descriptor tags; a font may also register additional tags with Apple. Unknown tags are preserved in <see cref="Descriptors"/> so that a caller sees exactly what the font declared.</description></item>
/// <item><description>Descriptors are typed by their tag, not by the on-disk field: <c>wght</c>, <c>wdth</c>, <c>slnt</c>, and <c>opsz</c> hold 16.16 fixed-point values, while <c>nalf</c> holds a plain integer. Read <see cref="FontDescriptor.Value"/> for the fixed-point tags and <see cref="FontDescriptor.RawValue"/> for the integer ones.</description></item>
/// <item><description>The manual's worked example is a version <c>0x00010000</c> table with two descriptors: <c>wght</c> = <c>+0.8</c> (<c>0x0000CCCC</c>) and <c>wdth</c> = <c>+1.0</c> (<c>0x00010000</c>).</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// Fixed? weight = fdsc.Weight;                  // the 'wght' descriptor, or null when absent
/// int? category = fdsc.NonAlphabeticCode;       // 0 for alphabetic, 1..6 for the defined categories
/// </code>
/// </example>
/// <seealso cref="FontDescriptor"/>
/// <seealso cref="FdscNonAlphabeticCode"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
public sealed record FdscTable : IFontTable<FdscTable>
{
    /// <summary>Gets the AAT table tag <c>fdsc</c>.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
    public static Tag Tag => "fdsc";

    /// <summary>Gets the table version.</summary>
    /// <remarks>The manual defines <c>0x00010000</c> (1.0) as the current version; the parser rejects any other value.</remarks>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="MinorVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the integer part of <see cref="Version"/>, which is 1 for a conforming table.</summary>
    /// <seealso cref="Version"/>
    /// <seealso cref="MinorVersion"/>
    public ushort MajorVersion => (ushort)Version.IntegerPart;

    /// <summary>Gets the fractional part of <see cref="Version"/>, which is 0 for a conforming table.</summary>
    /// <seealso cref="Version"/>
    /// <seealso cref="MajorVersion"/>
    public ushort MinorVersion => Version.FractionPart;

    /// <summary>Gets the style descriptors that characterize the font, in the order the table declares them.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Each descriptor is a <c>&lt;tag, value&gt;</c> pair. The manual does not forbid a repeated tag and does not require the list to be sorted, so the order on disk is preserved verbatim.</description></item>
    /// <item><description>An empty list is structurally valid; the manual states that all fonts should carry the table, but not how many descriptors it must hold.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Count"/>
    /// <seealso cref="Find(Tag)"/>
    /// <seealso cref="FontDescriptor"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
    public IReadOnlyList<FontDescriptor> Descriptors { get; init; } = [];

    /// <summary>Gets the number of style descriptors.</summary>
    /// <seealso cref="Descriptors"/>
    public int Count => Descriptors.Count;

    /// <summary>Returns the first descriptor with the specified <paramref name="tag"/>, or <c>null</c> when the font declares none.</summary>
    /// <param name="tag">The descriptor tag to look up, e.g. <c>"wght"</c>, <c>"wdth"</c>, <c>"slnt"</c>, <c>"opsz"</c>, or <c>"nalf"</c>.</param>
    /// <returns>The first matching <see cref="FontDescriptor"/>, or <c>null</c> when no descriptor declares the tag.</returns>
    /// <remarks>The lookup is linear over <see cref="Descriptors"/>. A repeated tag is not forbidden by the manual, so the first match wins.</remarks>
    /// <seealso cref="Descriptors"/>
    /// <seealso cref="FontDescriptor"/>
    public FontDescriptor? Find(Tag tag)
    {
        foreach (var descriptor in Descriptors)
            if (descriptor.Tag == tag) return descriptor;
        return null;
    }

    /// <summary>Gets the <c>wght</c> descriptor: percent weight relative to regular weight, where 1.0 is regular.</summary>
    /// <returns>The fixed-point weight, or <c>null</c> when the font declares no <c>wght</c> descriptor.</returns>
    /// <seealso cref="Find(Tag)"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
    public Fixed? Weight => Find("wght")?.Value;

    /// <summary>Gets the <c>wdth</c> descriptor: percent width relative to regular width, where 1.0 is regular.</summary>
    /// <returns>The fixed-point width, or <c>null</c> when the font declares no <c>wdth</c> descriptor.</returns>
    /// <seealso cref="Find(Tag)"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
    public Fixed? Width => Find("wdth")?.Value;

    /// <summary>Gets the <c>slnt</c> descriptor: angle of slant in degrees, where positive is clockwise from straight up.</summary>
    /// <returns>The fixed-point slant angle in degrees, or <c>null</c> when the font declares no <c>slnt</c> descriptor.</returns>
    /// <seealso cref="Find(Tag)"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
    public Fixed? Slant => Find("slnt")?.Value;

    /// <summary>Gets the <c>opsz</c> descriptor: the point size the font was designed for.</summary>
    /// <returns>The fixed-point optical size in points, or <c>null</c> when the font declares no <c>opsz</c> descriptor.</returns>
    /// <seealso cref="Find(Tag)"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
    public Fixed? OpticalSize => Find("opsz")?.Value;

    /// <summary>Gets the <c>nalf</c> descriptor: the font's non-alphabetic category.</summary>
    /// <returns>The integer category code, or <c>null</c> when the font declares no <c>nalf</c> descriptor.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The manual states that the <c>nalf</c> value is treated as an integer rather than as a 16.16 <c>Fixed</c>, which is why this accessor reads <see cref="FontDescriptor.RawValue"/> and not <see cref="FontDescriptor.Value"/>.</description></item>
    /// <item><description>Zero means the font is alphabetic. The codes 1 through 6 are named by <see cref="FdscNonAlphabeticCode"/>; higher values are reserved for registration with Apple, so the accessor is typed as an integer rather than as the enumeration.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Find(Tag)"/>
    /// <seealso cref="FdscNonAlphabeticCode"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
    public int? NonAlphabeticCode => Find("nalf")?.RawValue;

    /// <summary>Size, in bytes, of a <see cref="FontDescriptorRecord"/>.</summary>
    private const int DescriptorSize = 8;

    /// <summary>The eight-byte fixed-layout <c>fdsc</c> table header.</summary>
    /// <remarks>Fields are stored in big-endian order at the offsets defined by the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html"><c>fdsc</c> table specification</see>. The header is followed immediately by <c>descriptorCount</c> eight-byte <see cref="FontDescriptorRecord"/> entries.</remarks>
    /// <seealso cref="FdscTable"/>
    /// <seealso cref="FontDescriptorRecord"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0. <c>0x00010000</c> for the current version.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
        public Fixed Version;           // +0

        /// <summary>The number of style descriptors that follow at byte offset 4.</summary>
        /// <remarks>Each descriptor is an eight-byte <see cref="FontDescriptorRecord"/>.</remarks>
        /// <seealso cref="FontDescriptorRecord"/>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
        public uint DescriptorCount;    // +4

        /// <summary>Reverses the byte order of both fields in a <see cref="Header"/>.</summary>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are 32 bits wide and are reversed independently; the header has no byte-only fields.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = Fixed.ReverseEndianness(v.Version),
            DescriptorCount = BinaryPrimitives.ReverseEndianness(v.DescriptorCount),
        };
    }

    /// <summary>An eight-byte fixed-layout <c>fdsc</c> descriptor record: one <c>&lt;tag, value&gt;</c> pair.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The manual names this structure <c>gxFontDescriptor</c>.</description></item>
    /// <item><description>The tag names the attribute and the value carries it. The value's type depends on the tag: the fixed-point descriptors store a 16.16 value, while <c>nalf</c> stores a plain integer in the same 32 bits.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FdscTable"/>
    /// <seealso cref="FontDescriptor"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct FontDescriptorRecord : IEndianReversibleStruct<FontDescriptorRecord>
    {
        /// <summary>The four-byte descriptor tag at byte offset 0.</summary>
        /// <remarks>Known tags are <c>wght</c>, <c>wdth</c>, <c>slnt</c>, <c>opsz</c>, and <c>nalf</c>.</remarks>
        /// <seealso cref="Value"/>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
        public Tag Tag;      // +0

        /// <summary>The 32-bit descriptor value at byte offset 4.</summary>
        /// <remarks>Interpret the raw bits according to <see cref="Tag"/>; see <see cref="FontDescriptor.Value"/> and <see cref="FontDescriptor.RawValue"/>.</remarks>
        /// <seealso cref="Tag"/>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
        public Fixed Value;  // +4

        /// <summary>Reverses the byte order of both fields in a <see cref="FontDescriptorRecord"/>.</summary>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new record with the tag and the value reversed.</returns>
        /// <remarks>The tag is reversed with <see cref="Tag.ReverseEndianness(Tag)"/> and the value with <see cref="Fixed.ReverseEndianness(Fixed)"/>.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static FontDescriptorRecord ReverseEndianness(FontDescriptorRecord v) => new()
        {
            Tag = Tag.ReverseEndianness(v.Tag),
            Value = Fixed.ReverseEndianness(v.Value),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the <c>fdsc</c> table.</param>
    /// <param name="context">Unused. The table is self-contained.</param>
    /// <returns>The parsed <c>fdsc</c> table with its descriptors resolved.</returns>
    /// <exception cref="InvalidDataException">The version is not <c>0x00010000</c>, or the descriptor array exceeds the table's extent.</exception>
    /// <exception cref="EndOfStreamException">The header or the descriptor array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The descriptor array is bounded against the table's declared extent before allocation, so an absurd <c>descriptorCount</c> is rejected rather than producing a huge allocation.</description></item>
    /// <item><description>A table with a <c>descriptorCount</c> of 0 parses to an empty <see cref="Descriptors"/> list and is not an error.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="FontDescriptorRecord"/>
    /// <seealso cref="FontDescriptor"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
    static FdscTable IRecord<FdscTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version.Bits != 0x00010000)
            throw new InvalidDataException(
                $"'fdsc'.version is 0x{header.Version.Bits:X8}, expected 0x00010000.");

        long tableExtent = cursor.Source.Length;
        long recordsEnd = cursor.Position + (long)header.DescriptorCount * DescriptorSize;

        if (recordsEnd > tableExtent)
            throw new InvalidDataException(
                $"'fdsc'.descriptorCount is {header.DescriptorCount}, which exceeds the table's extent.");

        // The bound above proves DescriptorCount fits in an int: a table cannot be longer than int.MaxValue bytes.
        FontDescriptorRecord[] records = cursor.ReadBigEndianStructArray<FontDescriptorRecord>((int)header.DescriptorCount);

        var descriptors = new FontDescriptor[records.Length];
        for (int i = 0; i < records.Length; i++)
            descriptors[i] = new FontDescriptor { Tag = records[i].Tag, Value = records[i].Value };

        return new FdscTable
        {
            Version = header.Version,
            Descriptors = descriptors,
        };
    }
}

/// <summary>One style descriptor of an <c>fdsc</c> table: a descriptor tag and the value the font declares for it.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The pair is untyped on disk, so the same 32 bits mean different things for different tags. <see cref="Value"/> decodes them as 16.16 fixed-point, which is correct for <c>wght</c>, <c>wdth</c>, <c>slnt</c>, and <c>opsz</c>; <see cref="RawValue"/> is the integer reading, which is the correct one for <c>nalf</c>.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FdscTable"/>
/// <seealso cref="FdscTable.Descriptors"/>
/// <seealso cref="FdscNonAlphabeticCode"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
public sealed record FontDescriptor
{
    /// <summary>Gets the descriptor's tag, which names the attribute.</summary>
    /// <value>The four-byte tag, e.g. <c>"wght"</c> for weight or <c>"opsz"</c> for optical size.</value>
    /// <seealso cref="Value"/>
    /// <seealso cref="RawValue"/>
    public Tag Tag { get; init; }

    /// <summary>Gets the descriptor value interpreted as a 16.16 fixed-point number.</summary>
    /// <value>The raw 32-bit payload reinterpreted as a <see cref="Fixed"/> value.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>This reading is correct for the fixed-point descriptors <c>wght</c>, <c>wdth</c>, <c>slnt</c>, and <c>opsz</c>. The manual gives <c>wght</c> a regular value of 1.0, <c>wdth</c> 1.0, <c>slnt</c> 0.0, and <c>opsz</c> 12.0.</description></item>
    /// <item><description>It is the wrong reading for <c>nalf</c>, whose value the manual defines as an integer rather than a fixed-point number; use <see cref="RawValue"/> for that tag.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="RawValue"/>
    /// <seealso cref="Tag"/>
    /// <seealso cref="FdscTable.Find(Tag)"/>
    public Fixed Value { get; init; }

    /// <summary>Gets the raw 32-bit descriptor payload.</summary>
    /// <value>The descriptor value as the bits a font stored, without reinterpretation.</value>
    /// <remarks>This is the correct reading for tags the manual defines as integer-valued, in particular <c>nalf</c>, whose codes are named by <see cref="FdscNonAlphabeticCode"/>.</remarks>
    /// <seealso cref="Value"/>
    /// <seealso cref="FdscNonAlphabeticCode"/>
    public int RawValue => Value.Bits;

    /// <inheritdoc/>
    /// <returns>A diagnostic string of the form <c>'wght' 0.8</c>.</returns>
    /// <remarks>The value is formatted as 16.16 fixed-point regardless of the tag, so the output is meaningful only for the fixed-point descriptors. The format is intended for diagnostics; it is not a stable serialization format.</remarks>
    /// <seealso cref="Tag"/>
    /// <seealso cref="Value"/>
    public override string ToString() => $"'{Tag}' {Value}";
}

/// <summary>The non-alphabetic category codes that the <c>nalf</c> font descriptor can carry.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The manual defines codes 0 through 6 and reserves higher values for registration with Apple, which is why <see cref="FdscTable.NonAlphabeticCode"/> exposes the value as an integer rather than as this type.</description></item>
/// <item><description>The <c>nalf</c> value is an integer, not a 16.16 <c>Fixed</c>; read it through <see cref="FontDescriptor.RawValue"/>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FdscTable.NonAlphabeticCode"/>
/// <seealso cref="FontDescriptor.RawValue"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
public enum FdscNonAlphabeticCode
{
    /// <summary>The font is alphabetic (code 0).</summary>
    Alphabetic = 0,

    /// <summary>The font contains dingbats (code 1).</summary>
    Dingbats = 1,

    /// <summary>The font contains pi characters (code 2).</summary>
    PiCharacters = 2,

    /// <summary>The font contains fleurons (code 3).</summary>
    Fleurons = 3,

    /// <summary>The font contains decorative borders (code 4).</summary>
    DecorativeBorders = 4,

    /// <summary>The font contains international symbols (code 5).</summary>
    InternationalSymbols = 5,

    /// <summary>The font contains math symbols (code 6).</summary>
    MathSymbols = 6,
}
