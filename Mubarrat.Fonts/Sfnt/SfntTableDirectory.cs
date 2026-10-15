using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Sfnt;

/// <summary>
/// Represents the SFNT table directory, mapping table tags to their records.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The directory begins with a 12-byte offset table header followed by one 16-byte record per table. This type exposes the parsed form: the scaler marker and the dictionary of records keyed by tag.</description></item>
/// <item><description>The leading four bytes of the offset table header were originally the version number of the sfnt format. Every 1.0 font carried <c>0x00010000</c>.</description></item>
/// <item><description>Apple Advanced Typography reinterpreted the field as a scaler type so that a single dispatcher could route fonts to the correct rendering backend. Under that reading, TrueType fonts carry <c>'true'</c> and PostScript fonts carry <c>'typ1'</c> or, later, <c>'OTTO'</c>.</description></item>
/// <item><description>OpenType standardized the field as a scaler type and re-endorsed <c>0x00010000</c> for TrueType outlines. It deprecates <c>'true'</c> and <c>'typ1'</c> for new fonts but does not forbid parsing them. See <see cref="SfntVersion"/> and the four scaler constants on this type.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SfntTableRecord"/>
/// <seealso cref="IFontTableEntry"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType font</seealso>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6.html">TrueType Reference Manual: The <c>sfnt</c> directory</seealso>
public sealed record SfntTableDirectory : IReadOnlyDictionary<Tag, SfntTableRecord>, IReadOnlyDictionary<Tag, IFontTableEntry>, IRecord<SfntTableDirectory>
{
    /// <summary>
    /// The SFNT version marker for TrueType outlines, <c>0x00010000</c>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The value is the standard marker for TrueType outlines and is what modern tools write.</description></item>
    /// <item><description>The field was originally interpreted as a version number, and this value was the version of the original sfnt format. The reinterpretation of the field as a scaler type is documented in the Apple TrueType Reference Manual, but the value itself has never changed.</description></item>
    /// <item><description>Fonts carrying this value place their glyph data in a <c>glyf</c> table indexed by <c>loca</c>. See <see cref="HasTrueTypeOutlines"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SfntVersionCff"/>
    /// <seealso cref="SfntVersionAppleTrueType"/>
    /// <seealso cref="HasTrueTypeOutlines"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType font</seealso>
    public const uint SfntVersionTrueType = 0x00010000;

    /// <summary>
    /// The SFNT version marker for CFF outlines, <c>'OTTO'</c>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The value tells a consumer that the font carries CFF (PostScript Type 2) outlines in its <c>CFF </c> table rather than TrueType outlines in its <c>glyf</c> table.</description></item>
    /// <item><description>The four bytes are the ASCII characters <c>O</c>, <c>T</c>, <c>T</c>, <c>O</c>, packed most-significant-byte first.</description></item>
    /// <item><description>See <see cref="HasCffOutlines"/> for the derived predicate.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SfntVersionTrueType"/>
    /// <seealso cref="SfntVersionType1"/>
    /// <seealso cref="HasCffOutlines"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType font</seealso>
    public const uint SfntVersionCff = 0x4F54544F;

    /// <summary>
    /// The Apple-defined SFNT version marker for TrueType outlines, <c>'true'</c>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The value was introduced by Apple Advanced Typography when the leading four bytes of the sfnt directory were reinterpreted from a version number into a scaler type.</description></item>
    /// <item><description>The OpenType specification deprecates the value for new fonts and recommends <see cref="SfntVersionTrueType"/> instead. A conforming parser accepts both.</description></item>
    /// <item><description>The Apple TrueType Reference Manual prints the value as <c>0x74727564</c>, which spells <c>trud</c> and is an erratum. The correct value is <c>0x74727565</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SfntVersionTrueType"/>
    /// <seealso cref="SfntVersionType1"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6.html">TrueType Reference Manual: The <c>sfnt</c> directory</seealso>
    public const uint SfntVersionAppleTrueType = 0x74727565;

    /// <summary>
    /// The Apple-defined SFNT version marker for PostScript Type 1 outlines, <c>'typ1'</c>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The value predates OpenType and is rarely seen in fonts produced after 1997.</description></item>
    /// <item><description>The OpenType specification deprecates the value for new fonts and recommends <see cref="SfntVersionCff"/> for PostScript outlines.</description></item>
    /// <item><description>A conforming parser accepts the value but will typically have no tables to parse under it, because the old Type 1 outline tables are not part of OpenType.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SfntVersionCff"/>
    /// <seealso cref="SfntVersionAppleTrueType"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6.html">TrueType Reference Manual: The <c>sfnt</c> directory</seealso>
    public const uint SfntVersionType1 = 0x74797031;

    /// <summary>
    /// Gets a value indicating whether the font carries TrueType outlines.
    /// </summary>
    /// <value><see langword="true"/> when <see cref="SfntVersion"/> is <see cref="SfntVersionTrueType"/> or <see cref="SfntVersionAppleTrueType"/>.</value>
    /// <remarks>TrueType-outline fonts carry their glyph data in a <c>glyf</c> table and a <c>loca</c> index.</remarks>
    /// <seealso cref="SfntVersionTrueType"/>
    /// <seealso cref="SfntVersionAppleTrueType"/>
    /// <seealso cref="HasCffOutlines"/>
    public bool HasTrueTypeOutlines =>
        SfntVersion is SfntVersionTrueType or SfntVersionAppleTrueType;

    /// <summary>
    /// Gets a value indicating whether the font carries CFF outlines.
    /// </summary>
    /// <value><see langword="true"/> when <see cref="SfntVersion"/> is <see cref="SfntVersionCff"/>.</value>
    /// <remarks>CFF-outline fonts carry their glyph data in a <c>CFF </c> table rather than a <c>glyf</c> table.</remarks>
    /// <seealso cref="SfntVersionCff"/>
    /// <seealso cref="HasTrueTypeOutlines"/>
    public bool HasCffOutlines => SfntVersion == SfntVersionCff;

    private readonly Dictionary<Tag, SfntTableRecord> _records;

    /// <summary>
    /// Initializes a new instance of the <see cref="SfntTableDirectory"/> class.
    /// </summary>
    /// <param name="sfntVersion">The SFNT version marker.</param>
    /// <param name="records">The records indexed by their table tags.</param>
    public SfntTableDirectory(uint sfntVersion, Dictionary<Tag, SfntTableRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        SfntVersion = sfntVersion;
        _records = records;
    }

    /// <summary>
    /// Gets the four-byte value from the start of the sfnt directory that identifies the scaler for the font.
    /// </summary>
    /// <value>
    /// One of <see cref="SfntVersionTrueType"/>, <see cref="SfntVersionCff"/>, <see cref="SfntVersionAppleTrueType"/>, <see cref="SfntVersionType1"/>, or any other four-byte value present in a non-conforming font.
    /// </value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The field was originally the version number of the sfnt format; every 1.0 font carried <c>0x00010000</c>.</description></item>
    /// <item><description>Apple Advanced Typography reinterpreted the field as a scaler type so that a single dispatcher could route fonts to the correct rendering backend.</description></item>
    /// <item><description>OpenType standardized the field as a scaler type and re-endorsed <c>0x00010000</c> for TrueType outlines, deprecating <c>'true'</c> and <c>'typ1'</c> for new fonts but not forbidding their presence in existing ones.</description></item>
    /// <item><description>The value is not validated by the parser. Any four-byte value is accepted because the field is genuinely open-ended, and an unrecognised scaler does not prevent the table directory itself from being parsed.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SfntVersionTrueType"/>
    /// <seealso cref="SfntVersionCff"/>
    /// <seealso cref="SfntVersionAppleTrueType"/>
    /// <seealso cref="SfntVersionType1"/>
    /// <seealso cref="HasTrueTypeOutlines"/>
    /// <seealso cref="HasCffOutlines"/>
    public uint SfntVersion { get; }

    /// <inheritdoc />
    public int Count => _records.Count;

    /// <inheritdoc />
    public IEnumerable<Tag> Keys => _records.Keys;

    /// <inheritdoc />
    public IEnumerable<SfntTableRecord> Values => _records.Values;

    /// <summary>
    /// Gets the table records in this directory.
    /// </summary>
    public IReadOnlyCollection<SfntTableRecord> Records => _records.Values;

    IEnumerable<IFontTableEntry> IReadOnlyDictionary<Tag, IFontTableEntry>.Values => (IEnumerable<IFontTableEntry>)Values;

    IFontTableEntry IReadOnlyDictionary<Tag, IFontTableEntry>.this[Tag key] => _records[key];

    /// <inheritdoc />
    public SfntTableRecord this[Tag key] => _records[key];

    /// <inheritdoc />
    public bool ContainsKey(Tag key) => _records.ContainsKey(key);

    /// <inheritdoc />
    public bool TryGetValue(Tag key, [MaybeNullWhen(false)] out SfntTableRecord value) => _records.TryGetValue(key, out value);

    bool IReadOnlyDictionary<Tag, IFontTableEntry>.TryGetValue(Tag key, [MaybeNullWhen(false)] out IFontTableEntry value)
    {
        if (_records.TryGetValue(key, out SfntTableRecord record))
        {
            value = record;
            return true;
        }
        value = null;
        return false;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<Tag, SfntTableRecord>> GetEnumerator() => _records.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<KeyValuePair<Tag, IFontTableEntry>> IEnumerable<KeyValuePair<Tag, IFontTableEntry>>.GetEnumerator() => _records.Select(static pair => new KeyValuePair<Tag, IFontTableEntry>(pair.Key, pair.Value)).GetEnumerator();

    /// <summary>
    /// The 12-byte SFNT offset table header that precedes the table records. Blittable, no padding.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><see cref="SfntVersion"/> identifies the scaler for the font. See the four scaler constants on <see cref="SfntTableDirectory"/>.</description></item>
    /// <item><description><see cref="SearchRange"/>, <see cref="EntrySelector"/>, and <see cref="RangeShift"/> are precomputed search hints derived from <see cref="NumTables"/>. They are required in the serialized header but are not used for dictionary-based lookup.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">organization of an OpenType font</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SfntTableDirectory"/>
    /// <seealso cref="SfntTableRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType font</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>
        /// The four-byte scaler marker from the start of the offset table. See the four scaler constants on <see cref="SfntTableDirectory"/>.
        /// </summary>
        /// <value>One of <see cref="SfntTableDirectory.SfntVersionTrueType"/>, <see cref="SfntTableDirectory.SfntVersionCff"/>, <see cref="SfntTableDirectory.SfntVersionAppleTrueType"/>, <see cref="SfntTableDirectory.SfntVersionType1"/>, or any other four-byte value.</value>
        /// <seealso cref="SfntTableDirectory.SfntVersion"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#organization-of-an-opentype-font">OpenType specification: Organization of an OpenType font</seealso>
        public uint SfntVersion;

        /// <summary>
        /// The number of table records.
        /// </summary>
        public ushort NumTables;

        /// <summary>
        /// The maximum number of bytes searched by the original binary-search algorithm.
        /// </summary>
        public ushort SearchRange;

        /// <summary>
        /// The log base two of the largest power of two not exceeding the table count.
        /// </summary>
        public ushort EntrySelector;

        /// <summary>
        /// The number of bytes beyond the largest power-of-two search range.
        /// </summary>
        public ushort RangeShift;

        /// <inheritdoc />
        public static Header ReverseEndianness(Header value) => new()
        {
            SfntVersion = BinaryPrimitives.ReverseEndianness(value.SfntVersion),
            NumTables = BinaryPrimitives.ReverseEndianness(value.NumTables),
            SearchRange = BinaryPrimitives.ReverseEndianness(value.SearchRange),
            EntrySelector = BinaryPrimitives.ReverseEndianness(value.EntrySelector),
            RangeShift = BinaryPrimitives.ReverseEndianness(value.RangeShift),
        };
    }

    /// <summary>
    /// Parses the SFNT offset table and its table records from the current cursor position.
    /// </summary>
    static SfntTableDirectory IRecord<SfntTableDirectory>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();
        return new SfntTableDirectory(header.SfntVersion, cursor.ReadEndianReversibleStructArray<SfntTableRecord>(header.NumTables).ToDictionary(static record => record.Tag));
    }
}
