using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Binary;

/// <summary>A top-level table: an <see cref="IRecord{T}"/> with a tag. The parent passed to <see cref="IRecord{T}.Parse"/> is always a <see cref="FontFace"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Every top-level table in an OpenType font is registered in the table directory under a 4-byte tag; see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#table-directory">OpenType specification, Table Directory</see>.</description></item>
/// <item><description>The tag is exposed as a static abstract property so that the table directory can dispatch to the correct <c>Parse</c> without a runtime dictionary lookup — each table type declares its own tag as a compile-time constant.</description></item>
/// <item><description>Implementations must extend <see cref="IRecord{T}"/>; the <c>Parse</c> method they inherit is the entry point invoked by <see cref="FontFace"/> when reading a table from the font file.</description></item>
/// </list>
/// <para>For the complete list of registered tags and their formats, see the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#font-tables">OpenType specification, Font Tables</see>.</para>
/// </remarks>
/// <typeparam name="T">The implementing table type. Must be a reference type implementing <see cref="IOpenTypeTable{T}"/>.</typeparam>
/// <seealso cref="IRecord{T}"/>
/// <seealso cref="FontFace"/>
/// <seealso cref="Tag"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#table-directory">OpenType specification: Table Directory</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#font-tables">OpenType specification: Font Tables</seealso>
public interface IOpenTypeTable<T> : IRecord<T> where T : IOpenTypeTable<T>
{
    /// <summary>The tag of the table, which is used to identify it in the font file.</summary>
    /// <value>A four-character tag such as <c>"head"</c>, <c>"cmap"</c>, or <c>"OS/2"</c>.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The tag is compared against the directory entries by <see cref="Tag.Equals(Tag)"/>; see <see cref="Tag"/> for the four-byte representation.</description></item>
    /// <item><description>Tags are case-sensitive and space-padded on the right for tags shorter than four characters (e.g. <c>"cvt "</c> for the Control Value Table).</description></item>
    /// <item><description>Implementations must return the same value on every call — the property is expected to be a <c>static readonly</c> or constant-like expression.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Tag"/>
    /// <seealso cref="FontFace"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#table-directory">OpenType specification: Table Directory</seealso>
    static abstract Tag Tag { get; }
}
