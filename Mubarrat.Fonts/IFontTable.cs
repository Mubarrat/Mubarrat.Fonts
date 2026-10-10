using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts;

/// <summary>
/// Represents a top-level, tagged table belonging to a font face.
/// </summary>
/// <remarks>
/// <para>A font table is a logically distinct unit of font data identified by a four-byte <see cref="Tag"/>. Examples include the font header (<c>head</c>), character-to-glyph mapping (<c>cmap</c>), glyph substitution (<c>GSUB</c>), glyph positioning (<c>GPOS</c>), and Apple's extended glyph metamorphosis (<c>morx</c>) tables.</para>
/// <para>Implementations provide a static <see cref="Tag"/> property that identifies the table type. This allows generic font-reading APIs to determine the expected table tag through static interface dispatch, without requiring each table type to register its tag in a separate runtime registry.</para>
/// <para>The interface inherits <see cref="IRecord{T}"/>, which defines the parsing contract for the implementing type. The concrete parser is responsible for interpreting the table's serialized representation and constructing its corresponding object model.</para>
/// <para>This interface does not prescribe the physical font-file format or the specification that defines a table. A table may be defined by OpenType, TrueType, Apple's Advanced Typography (AAT), or another supported font specification. Container-specific code is responsible for locating and exposing the table's serialized data to its parser.</para>
/// <para>Only top-level tables identified by entries in a font's table directory should implement this interface. Nested structures, lookup records, and subtables ordinarily belong to their containing table and do not represent independent directory entries.</para>
/// </remarks>
/// <typeparam name="T">The concrete table type implementing this interface. The type must implement <see cref="IFontTable{T}"/> itself and satisfy the parsing contract defined by <see cref="IRecord{T}"/>.</typeparam>
/// <example>
/// <para>A table implementation declares its tag as a static property and provides its parsing implementation through <see cref="IRecord{T}"/>:</para>
/// <code>
/// public sealed record HeadTable : IFontTable&lt;HeadTable&gt;
/// {
///     public static Tag Tag => "head";
///
///     // Parsed table properties and the IRecord&lt;HeadTable&gt; parsing implementation.
/// }
/// </code>
/// <para>A generic font-face API can then request the table by its type:</para>
/// <code>
/// HeadTable head = face.GetTable&lt;HeadTable&gt;();
/// </code>
/// </example>
/// <seealso cref="IRecord{T}"/>
/// <seealso cref="Tag"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#table-directory">OpenType specification: Table Directory</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#font-tables">OpenType specification: Font Tables</seealso>
public interface IFontTable<T> : IRecord<T> where T : IFontTable<T>
{
    /// <summary>
    /// Gets the four-byte tag identifying this table in a font's table directory.
    /// </summary>
    /// <remarks>
    /// <para>The tag identifies the table type, not an individual occurrence of that table. For example, <c>head</c> identifies the font header table, while <c>GPOS</c> identifies the glyph positioning table.</para>
    /// <para>Tags are four-byte values and are case-sensitive. When a tag contains fewer than four characters, any required trailing spaces are part of the tag. For example, the Control Value Table uses <c>"cvt "</c>, including its trailing space.</para>
    /// <para>Implementations must return the same tag for every access. The value should therefore be a constant-like expression rather than a value derived from mutable state or an individual font instance.</para>
    /// <para>The property is static and abstract so generic consumers can obtain the tag from the table type through static interface dispatch, without constructing a table instance or using reflection.</para>
    /// </remarks>
    /// <value>The four-byte <see cref="Tag"/> identifying the implementing table type.</value>
    /// <seealso cref="Tag"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#table-directory">OpenType specification: Table Directory</seealso>
    static abstract Tag Tag { get; }
}
