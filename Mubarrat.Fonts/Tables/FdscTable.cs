using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>fdsc</c> table: the Apple Advanced Typography font descriptors table. Provides the descriptive information an application needs to substitute one font family for another while preserving style.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>An application that supports style matching can use the descriptor pairs to select the closest equivalent in a new font family when the user replaces a run of text.</description></item>
/// <item><description>The table is a version header followed by a variable-length array of <c>&lt;tag, value&gt;</c> pairs. Each pair describes a specific attribute of the font, such as its weight, width, slant, or optical size.</description></item>
/// <item><description>Additional descriptor tags can be registered with Apple beyond the five the specification currently names. A consumer that reads a tag the specification does not name compares it against <see cref="Descriptors"/> directly.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">font descriptors table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Descriptor"/>
/// <seealso cref="FdscTag"/>
/// <seealso cref="NonAlphabeticClass"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
public sealed record FdscTable : IFontTable<FdscTable>, IRecord<FdscTable>
{
    /// <summary>The version value for the only defined font descriptors table format, 1.0.</summary>
    /// <value>The <see cref="Fixed"/> value whose raw bits are <c>0x00010000</c>.</value>
    /// <seealso cref="Version"/>
    public static readonly Fixed CurrentVersion = new(0x00010000);

    /// <inheritdoc/>
    public static Tag Tag => "fdsc";

    /// <summary>Gets the table version.</summary>
    /// <value>The <see cref="CurrentVersion"/> value for a conforming table.</value>
    /// <seealso cref="CurrentVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
    public Fixed Version { get; init; }

    /// <summary>Gets the descriptor pairs, in table order.</summary>
    /// <value>The array of <see cref="Descriptor"/> entries as stored, in table order. Empty when the table declares no descriptors.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The array length is <see cref="DescriptorCount"/>.</description></item>
    /// <item><description>Duplicate tags are not forbidden by the specification, but a consumer that reads a descriptor by tag will see only the first match in table order.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Descriptor"/>
    /// <seealso cref="DescriptorCount"/>
    /// <seealso cref="TryGetDescriptor(Tag, out Fixed)"/>
    public IReadOnlyList<Descriptor> Descriptors { get; init; } = [];

    /// <summary>Gets the number of descriptor pairs in the table.</summary>
    /// <value>The length of <see cref="Descriptors"/>.</value>
    /// <seealso cref="Descriptors"/>
    public int DescriptorCount => Descriptors.Count;

    /// <summary>Tries to find a descriptor by tag.</summary>
    /// <param name="tag">The descriptor tag to look up.</param>
    /// <param name="value">When this method returns <see langword="true"/>, contains the descriptor value; otherwise, zero.</param>
    /// <returns><see langword="true"/> when a descriptor with the given tag is present; otherwise, <see langword="false"/>.</returns>
    /// <remarks>The lookup is linear over <see cref="Descriptors"/>. When duplicate tags are present, the first match in table order is returned.</remarks>
    /// <seealso cref="Descriptors"/>
    /// <seealso cref="FdscTag"/>
    public bool TryGetDescriptor(Tag tag, out Fixed value)
    {
        foreach (var descriptor in Descriptors)
        {
            if (descriptor.Tag == tag)
            {
                value = descriptor.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>Gets the descriptor value for a tag, or a fallback when the tag is absent.</summary>
    /// <param name="tag">The descriptor tag to look up.</param>
    /// <param name="fallback">The value to return when the tag is absent. Defaults to zero.</param>
    /// <returns>The descriptor value when present; otherwise <paramref name="fallback"/>.</returns>
    /// <seealso cref="TryGetDescriptor(Tag, out Fixed)"/>
    /// <seealso cref="FdscTag"/>
    public Fixed GetDescriptor(Tag tag, Fixed fallback = default) =>
        TryGetDescriptor(tag, out var value) ? value : fallback;

    /// <summary>Gets the weight descriptor, or <c>1.0</c> when the descriptor is absent.</summary>
    /// <value>The value of the <c>wght</c> descriptor, expressed as a percentage of regular weight. The Apple documentation defines <c>1.0</c> as regular.</value>
    /// <seealso cref="FdscTag.Weight"/>
    /// <seealso cref="FdscTag.RegularWeight"/>
    /// <seealso cref="GetDescriptor(Tag, Fixed)"/>
    public Fixed Weight => GetDescriptor(FdscTag.Weight, FdscTag.RegularWeight);

    /// <summary>Gets the width descriptor, or <c>1.0</c> when the descriptor is absent.</summary>
    /// <value>The value of the <c>wdth</c> descriptor, expressed as a percentage of regular width. The Apple documentation defines <c>1.0</c> as regular.</value>
    /// <seealso cref="FdscTag.Width"/>
    /// <seealso cref="FdscTag.RegularWidth"/>
    public Fixed Width => GetDescriptor(FdscTag.Width, FdscTag.RegularWidth);

    /// <summary>Gets the slant descriptor, or <c>0.0</c> when the descriptor is absent.</summary>
    /// <value>The value of the <c>slnt</c> descriptor, expressed as an angle in degrees. Positive values are clockwise from straight up.</value>
    /// <seealso cref="FdscTag.Slant"/>
    public Fixed Slant => GetDescriptor(FdscTag.Slant);

    /// <summary>Gets the optical size descriptor, or <c>12.0</c> when the descriptor is absent.</summary>
    /// <value>The value of the <c>opsz</c> descriptor, expressed as a point size. The Apple documentation defines <c>12.0</c> as the default point size.</value>
    /// <seealso cref="FdscTag.OpticalSize"/>
    /// <seealso cref="FdscTag.DefaultOpticalSize"/>
    public Fixed OpticalSize => GetDescriptor(FdscTag.OpticalSize, FdscTag.DefaultOpticalSize);

    /// <summary>Gets the non-alphabetic class, or <see cref="NonAlphabeticClass.Alphabetic"/> when the descriptor is absent.</summary>
    /// <value>The value of the <c>nalf</c> descriptor, interpreted as a small integer rather than as a <see cref="Fixed"/> value.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The Apple documentation states that the <c>nalf</c> value is treated as an integer, not as a fixed-point value. The raw <see cref="Fixed.Bits"/> value is therefore read as the class code, not the scaled <see cref="Fixed.Value"/>.</description></item>
    /// <item><description>Values 0 through 6 are the codes the specification defines. A font that stores a code above 6 produces an enum value not named by any member; the underlying integer is still readable via a cast.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FdscTag.NonAlphabetic"/>
    /// <seealso cref="NonAlphabeticClass"/>
    public NonAlphabeticClass NonAlphabetic =>
        TryGetDescriptor(FdscTag.NonAlphabetic, out var value)
            ? (NonAlphabeticClass)value.Bits
            : NonAlphabeticClass.Alphabetic;

    /// <summary>The 8-byte <c>fdsc</c> table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed by <see cref="DescriptorCount"/> descriptor pairs, each eight bytes.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">font descriptors table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FdscTable"/>
    /// <seealso cref="Descriptor"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0, as a <c>Fixed</c> (16.16) value.</summary>
        /// <value>The <see cref="CurrentVersion"/> value for a conforming table.</value>
        public Fixed Version;            // +0

        /// <summary>The number of descriptor pairs at byte offset 4.</summary>
        /// <value>The count of <see cref="Descriptor"/> entries that follow the header.</value>
        public uint DescriptorCount;     // +4

        /// <summary>Reverses the byte order of both fields in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = Fixed.ReverseEndianness(value.Version),
            DescriptorCount = BinaryPrimitives.ReverseEndianness(value.DescriptorCount),
        };
    }

    /// <summary>One descriptor pair: a four-byte tag and its fixed-point value. Blittable, size 8.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The tag identifies the descriptor. See <see cref="FdscTag"/> for the tags the specification defines; additional tags may be registered with Apple.</description></item>
    /// <item><description>The value's interpretation depends on the tag. For <c>wght</c>, <c>wdth</c>, <c>slnt</c>, and <c>opsz</c> it is a fixed-point value; for <c>nalf</c> it is a small integer stored in the raw <see cref="Fixed.Bits"/> field rather than the scaled <see cref="Fixed.Value"/>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">font descriptors table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FdscTable"/>
    /// <seealso cref="FdscTag"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Descriptor : IEndianReversibleStruct<Descriptor>
    {
        /// <summary>The descriptor tag at byte offset 0.</summary>
        /// <value>A four-byte tag. See <see cref="FdscTag"/> for the tags the specification defines.</value>
        /// <seealso cref="FdscTag"/>
        /// <seealso cref="Value"/>
        public Tag Tag;      // +0

        /// <summary>The descriptor value at byte offset 4.</summary>
        /// <value>A fixed-point value whose interpretation depends on <see cref="Tag"/>. For <c>nalf</c> the raw bits are read as an integer rather than as a fixed-point value.</value>
        /// <seealso cref="Tag"/>
        public Fixed Value;  // +4

        /// <summary>Reverses the byte order of both fields in a <see cref="Descriptor"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new descriptor with each field reversed.</returns>
        public static Descriptor ReverseEndianness(Descriptor value) => new()
        {
            Tag = Tag.ReverseEndianness(value.Tag),
            Value = Fixed.ReverseEndianness(value.Value),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the <c>fdsc</c> table.</param>
    /// <param name="context">Unused. The table is self-contained.</param>
    /// <returns>The parsed <c>fdsc</c> table.</returns>
    /// <exception cref="InvalidDataException">The version is not 1.0.</exception>
    /// <exception cref="EndOfStreamException">The header or the descriptor array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The descriptor count is declared by the header, so the array is read in a single bulk call.</description></item>
    /// <item><description>The Apple documentation does not cap the descriptor count. A malformed font that declares a very large count is bounded by the read, which throws <see cref="EndOfStreamException"/> when the array overruns the table-scoped source.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Descriptor"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
    static FdscTable IRecord<FdscTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
        {
            throw new InvalidDataException(
                $"'fdsc'.version is 0x{header.Version.Bits:X8}, expected 0x{CurrentVersion.Bits:X8}.");
        }

        var descriptors = cursor.ReadEndianReversibleStructArray<Descriptor>(checked((int)header.DescriptorCount));

        return new()
        {
            Version = header.Version,
            Descriptors = descriptors,
        };
    }
}

/// <summary>The descriptor tags defined by the <c>fdsc</c> table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each tag is a four-byte identifier that names a font attribute. See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">font descriptors table</see> in the Apple TrueType Reference Manual for the tag-to-attribute mapping.</description></item>
/// <item><description>Additional tags can be registered with Apple beyond the five listed here. A consumer that reads an unrecognised tag compares it against <see cref="FdscTable.Descriptors"/> directly.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FdscTable"/>
/// <seealso cref="NonAlphabeticClass"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
public static class FdscTag
{
    /// <summary>The <c>wght</c> descriptor tag: percent weight relative to regular weight.</summary>
    /// <value>The tag <c>wght</c>.</value>
    /// <remarks>The value is a percentage of regular weight; the specification defines <c>1.0</c> as regular.</remarks>
    /// <seealso cref="RegularWeight"/>
    /// <seealso cref="FdscTable.Weight"/>
    public static readonly Tag Weight = "wght";

    /// <summary>The <c>wdth</c> descriptor tag: percent width relative to regular width.</summary>
    /// <value>The tag <c>wdth</c>.</value>
    /// <remarks>The value is a percentage of regular width; the specification defines <c>1.0</c> as regular.</remarks>
    /// <seealso cref="RegularWidth"/>
    /// <seealso cref="FdscTable.Width"/>
    public static readonly Tag Width = "wdth";

    /// <summary>The <c>slnt</c> descriptor tag: angle of slant in degrees.</summary>
    /// <value>The tag <c>slnt</c>.</value>
    /// <remarks>Positive values are clockwise from straight up.</remarks>
    /// <seealso cref="FdscTable.Slant"/>
    public static readonly Tag Slant = "slnt";

    /// <summary>The <c>opsz</c> descriptor tag: point size the font was designed for.</summary>
    /// <value>The tag <c>opsz</c>.</value>
    /// <remarks>The specification defines <c>12.0</c> as the default point size.</remarks>
    /// <seealso cref="DefaultOpticalSize"/>
    /// <seealso cref="FdscTable.OpticalSize"/>
    public static readonly Tag OpticalSize = "opsz";

    /// <summary>The <c>nalf</c> descriptor tag: non-alphabetic class code.</summary>
    /// <value>The tag <c>nalf</c>.</value>
    /// <remarks>The value is treated as an integer, not as a fixed-point value. See <see cref="NonAlphabeticClass"/>.</remarks>
    /// <seealso cref="NonAlphabeticClass"/>
    /// <seealso cref="FdscTable.NonAlphabetic"/>
    public static readonly Tag NonAlphabetic = "nalf";

    /// <summary>The fixed-point value representing regular weight, <c>1.0</c>.</summary>
    /// <value>The <see cref="Fixed"/> value whose raw bits are <c>0x00010000</c>.</value>
    /// <seealso cref="Weight"/>
    /// <seealso cref="FdscTable.Weight"/>
    public static readonly Fixed RegularWeight = new(0x00010000);

    /// <summary>The fixed-point value representing regular width, <c>1.0</c>.</summary>
    /// <value>The <see cref="Fixed"/> value whose raw bits are <c>0x00010000</c>.</value>
    /// <seealso cref="Width"/>
    /// <seealso cref="FdscTable.Width"/>
    public static readonly Fixed RegularWidth = new(0x00010000);

    /// <summary>The fixed-point value representing the default optical size, <c>12.0</c>.</summary>
    /// <value>The <see cref="Fixed"/> value whose raw bits are <c>0x000C0000</c>.</value>
    /// <seealso cref="OpticalSize"/>
    /// <seealso cref="FdscTable.OpticalSize"/>
    public static readonly Fixed DefaultOpticalSize = new(0x000C0000);
}

/// <summary>The non-alphabetic class codes defined for the <c>nalf</c> descriptor.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The value <c>0</c> means the font is alphabetic. Values greater than zero identify the font as containing a specific class of non-alphabetic glyphs.</description></item>
/// <item><description>The Apple documentation states that the <c>nalf</c> value is treated as an integer, not as a fixed-point value. The class codes occupy the raw bits of the descriptor's <see cref="Fixed"/> field, not its scaled value.</description></item>
/// <item><description>Values above 6 are stored as-is by a font but are not named here; the underlying integer is readable through a cast.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">font descriptors table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FdscTag.NonAlphabetic"/>
/// <seealso cref="FdscTable.NonAlphabetic"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fdsc.html">TrueType Reference Manual: The <c>fdsc</c> table</seealso>
public enum NonAlphabeticClass
{
    /// <summary>The font is alphabetic, code 0.</summary>
    /// <value>The constant <c>0</c>.</value>
    Alphabetic = 0,

    /// <summary>The font contains dingbats, code 1.</summary>
    /// <value>The constant <c>1</c>.</value>
    Dingbats = 1,

    /// <summary>The font contains pi characters, code 2.</summary>
    /// <value>The constant <c>2</c>.</value>
    PiCharacters = 2,

    /// <summary>The font contains fleurons, code 3.</summary>
    /// <value>The constant <c>3</c>.</value>
    Fleurons = 3,

    /// <summary>The font contains decorative borders, code 4.</summary>
    /// <value>The constant <c>4</c>.</value>
    DecorativeBorders = 4,

    /// <summary>The font contains international symbols, code 5.</summary>
    /// <value>The constant <c>5</c>.</value>
    InternationalSymbols = 5,

    /// <summary>The font contains math symbols, code 6.</summary>
    /// <value>The constant <c>6</c>.</value>
    MathSymbols = 6,
}
