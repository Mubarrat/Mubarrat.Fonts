using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Tables.Metadata;

/// <summary>The <c>meta</c> table: miscellaneous metadata. Contains a set of tag-identified values that may be textual (UTF-8) or binary.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Registered tags include <c>dlng</c> (design languages) and <c>slng</c> (supported languages). Both are comma-separated lists of BCP 47-derived ScriptLangTags. Private tags begin with an uppercase letter.</description></item>
/// <item><description>Values are untyped on disk: a producer may store UTF-8 text, a binary blob, or a version string. <see cref="MetaDataMap.AsString"/> interprets the bytes as UTF-8, which is the correct behavior only for tags documented as text.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">meta table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MetaDataMap"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">OpenType specification: meta table</seealso>
public sealed record MetaTable : IOpenTypeTable<MetaTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IOpenTypeTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">OpenType specification: meta table</seealso>
    public static Tag Tag => "meta";

    /// <summary>Gets the table version. Always 1.</summary>
    /// <value>The constant <c>1</c> for a conforming meta table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta"><c>version</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Flags"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">OpenType specification: <c>version</c></seealso>
    public uint Version { get; init; }

    /// <summary>Gets the flags word. Currently unused; 0.</summary>
    /// <value>Reserved by the specification; conforming writers emit <c>0</c>.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta"><c>flags</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Version"/>
    /// <seealso cref="Reserved"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">OpenType specification: <c>flags</c></seealso>
    public uint Flags { get; init; }

    /// <summary>Gets the reserved field at header offset +8. The specification requires 0; exposed so a caller can detect a non-conforming writer.</summary>
    /// <value>Reserved by the specification; conforming writers emit <c>0</c>.</value>
    /// <remarks>The field is surfaced rather than discarded so that strict validators can flag a non-conforming value.</remarks>
    /// <seealso cref="Flags"/>
    public uint Reserved { get; init; }

    /// <summary>Gets the data map records, one per metadata value.</summary>
    /// <value>The ordered list of <see cref="MetaDataMap"/> entries, in the order they appear in the table.</value>
    /// <seealso cref="Find(Tag)"/>
    /// <seealso cref="Count"/>
    /// <seealso cref="MetaDataMap"/>
    public IReadOnlyList<MetaDataMap> DataMaps { get; init; } = [];

    /// <summary>Gets the number of data maps.</summary>
    /// <value>The size of the <see cref="DataMaps"/> list.</value>
    /// <seealso cref="DataMaps"/>
    public int Count => DataMaps.Count;

    /// <summary>Returns the first data map with <paramref name="tag"/>, or <c>null</c>.</summary>
    /// <param name="tag">The metadata tag to look up, e.g. <c>"dlng"</c>, <c>"slng"</c>.</param>
    /// <returns>The first matching <see cref="MetaDataMap"/>, or <c>null</c> when no map declares the tag.</returns>
    /// <remarks>The lookup is linear over <see cref="DataMaps"/>. Duplicate tags are not forbidden by the specification; the first match wins.</remarks>
    /// <seealso cref="DataMaps"/>
    /// <seealso cref="MetaDataMap"/>
    public MetaDataMap? Find(Tag tag)
    {
        foreach (var m in DataMaps)
            if (m.Tag == tag) return m;
        return null;
    }

    /// <summary>Returns the value for a text-typed tag as a UTF-8 string, or <c>null</c> when the tag is absent.</summary>
    /// <param name="tag">The metadata tag to look up.</param>
    /// <returns>The decoded UTF-8 string for the tag, or <c>null</c> when the tag is absent.</returns>
    /// <remarks>The method does not validate that the payload is text; a binary value is decoded as UTF-8 and may contain replacement characters.</remarks>
    /// <seealso cref="Find(Tag)"/>
    /// <seealso cref="MetaDataMap.AsString"/>
    public string? GetText(Tag tag) => Find(tag)?.AsString();

    /// <summary>Returns the design languages declared by <c>dlng</c>, or an empty list when the tag is absent. Splits the value on commas and trims whitespace.</summary>
    /// <returns>The parsed BCP 47-derived design language list, or an empty list when <c>dlng</c> is absent or empty.</returns>
    /// <remarks>An empty entry between commas is dropped, so trailing or repeated commas do not produce empty strings in the result.</remarks>
    /// <seealso cref="GetText(Tag)"/>
    /// <seealso cref="SplitList(string?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">OpenType specification: meta table</seealso>
    public IReadOnlyList<string> GetDesignLanguages() => SplitList(GetText("dlng"));

    /// <summary>Returns the supported languages declared by <c>slng</c>, or an empty list when the tag is absent. Splits the value on commas and trims whitespace.</summary>
    /// <returns>The parsed BCP 47-derived supported language list, or an empty list when <c>slng</c> is absent or empty.</returns>
    /// <remarks>An empty entry between commas is dropped, so trailing or repeated commas do not produce empty strings in the result.</remarks>
    /// <seealso cref="GetText(Tag)"/>
    /// <seealso cref="SplitList(string?)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">OpenType specification: meta table</seealso>
    public IReadOnlyList<string> GetSupportedLanguages() => SplitList(GetText("slng"));

    /// <summary>Returns <c>true</c> if <paramref name="tag"/> satisfies the specification's tag character rules: must begin with a letter; only letters, digits, or trailing spaces.</summary>
    /// <param name="tag">The tag to validate.</param>
    /// <returns><see langword="true"/> when the tag matches the specification's rules for a valid meta tag.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The first character must be a letter; subsequent characters may be letters or digits, optionally followed by spaces that pad the tag to four characters. A non-space character after a space is rejected.</description></item>
    /// <item><description>The implementation converts the tag to a <see cref="string"/> and copies it into a stack span; the conversion allocates. A caller iterating many tags may prefer to test the characters directly through <see cref="Tag.this[int]"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// bool ok = MetaTable.IsValidTag("dlng");   // true
    /// bool bad = MetaTable.IsValidTag("1abc");  // false: starts with a digit
    /// </code>
    /// </example>
    /// <seealso cref="Tag"/>
    /// <seealso cref="Tag.this[int]"/>
    public static bool IsValidTag(Tag tag)
    {
        Span<char> chars = stackalloc char[4];
        ((string)tag).AsSpan().CopyTo(chars); // assumed helper — replace with the layer's actual Tag-to-chars API
        if (!char.IsLetter(chars[0])) return false;
        bool sawSpace = false;
        for (int i = 0; i < 4; i++)
        {
            char c = chars[i];
            if (c == ' ') { sawSpace = true; continue; }
            if (sawSpace) return false; // a non-space after a space
            if (!char.IsLetterOrDigit(c)) return false;
        }
        return true;
    }

    /// <summary>Splits a comma-separated list into trimmed, non-empty entries.</summary>
    /// <param name="value">The value to split, or <c>null</c> or empty for an empty result.</param>
    /// <returns>The parsed entries, or an empty array when <paramref name="value"/> is <c>null</c>, empty, or contains only separators and whitespace.</returns>
    /// <remarks>Both <see cref="GetDesignLanguages"/> and <see cref="GetSupportedLanguages"/> delegate to this helper; it is exposed so a caller can parse a related list that uses the same comma-separated convention.</remarks>
    /// <seealso cref="GetDesignLanguages"/>
    /// <seealso cref="GetSupportedLanguages"/>
    public static string[] SplitList(string? value)
    {
        if (string.IsNullOrEmpty(value)) return [];
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the meta table.</param>
    /// <param name="context">Unused. The table is self-contained.</param>
    /// <returns>The parsed meta table with its data maps resolved.</returns>
    /// <exception cref="InvalidDataException">The version is not 1, the data-map count exceeds the table's extent, or any data payload extends past the table end.</exception>
    /// <exception cref="EndOfStreamException">The header, data-map records, or any data payload extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The data-map array is bounded against the table's declared extent before allocation, so an absurd <c>dataMapsCount</c> is rejected rather than producing a huge allocation.</description></item>
    /// <item><description>Data payloads are read eagerly into byte arrays and stored on the corresponding <see cref="MetaDataMap"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">meta table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="DataMapRecord"/>
    /// <seealso cref="MetaDataMap"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">OpenType specification: meta table</seealso>
    public static MetaTable Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version != 1)
            throw new InvalidDataException($"'meta'.version is {header.Version}, expected 1.");

        long tableExtent = cursor.Source.Length;

        // Reject an absurd dataMapsCount before allocating the array.
        long recordsEnd = Unsafe.SizeOf<Header>() + header.DataMapsCount * Unsafe.SizeOf<DataMapRecord>();
        if (recordsEnd > tableExtent)
            throw new InvalidDataException(
                $"'meta'.dataMapsCount is {header.DataMapsCount}, which exceeds the table's extent.");

        int count = (int)header.DataMapsCount;
        DataMapRecord[] records = cursor.ReadBigEndianStructArray<DataMapRecord>(count);

        var maps = new MetaDataMap[count];
        for (int i = 0; i < count; i++)
        {
            DataMapRecord rec = records[i];
            uint dataOffset = rec.DataOffset;
            uint dataLength = rec.DataLength;

            if ((long)dataOffset + dataLength > tableExtent)
                throw new InvalidDataException($"'meta' data map '{rec.Tag}' extends past the table end.");

            var data = cursor.Source.ReadBytesAt(dataOffset, checked((int)dataLength));
            maps[i] = new MetaDataMap { Tag = rec.Tag, Data = data };
        }

        return new MetaTable
        {
            Version = header.Version,
            Flags = header.Flags,
            Reserved = header.Reserved,
            DataMaps = maps,
        };
    }

    /// <summary>The 16-byte <c>meta</c> table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by <c>dataMapsCount</c> 12-byte <see cref="DataMapRecord"/> entries.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">meta header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MetaTable"/>
    /// <seealso cref="DataMapRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">OpenType specification: meta header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the table version. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming meta table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta"><c>version</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Flags"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">OpenType specification: <c>version</c></seealso>
        public uint Version;        // +0

        /// <summary>Gets the flags word. Currently unused.</summary>
        /// <value>Reserved by the specification; conforming writers emit <c>0</c>.</value>
        /// <seealso cref="Version"/>
        /// <seealso cref="Reserved"/>
        public uint Flags;          // +4

        /// <summary>Gets the reserved field. Always 0.</summary>
        /// <value>Reserved by the specification; conforming writers emit <c>0</c>.</value>
        /// <seealso cref="Flags"/>
        /// <seealso cref="DataMapsCount"/>
        public uint Reserved;       // +8  (spec: set to 0)

        /// <summary>Gets the number of data map records that follow the header.</summary>
        /// <value>The count of <see cref="DataMapRecord"/> entries.</value>
        /// <seealso cref="Reserved"/>
        /// <seealso cref="DataMapRecord"/>
        public uint DataMapsCount;  // +12

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All four fields are <c>uint32</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
            Reserved = BinaryPrimitives.ReverseEndianness(v.Reserved),
            DataMapsCount = BinaryPrimitives.ReverseEndianness(v.DataMapsCount),
        };
    }

    /// <summary>A single 12-byte <c>meta</c> data map record. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Each record names a tag and points at the value bytes elsewhere in the table; the parser resolves the offset into a <see cref="MetaDataMap"/>.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">meta data map record</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MetaTable"/>
    /// <seealso cref="MetaDataMap"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">OpenType specification: meta data map record</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct DataMapRecord : IBigEndianStruct<DataMapRecord>
    {
        /// <summary>Gets the metadata tag.</summary>
        /// <value>The four-character identifier for this value, e.g. <c>"dlng"</c>, <c>"slng"</c>, or a private tag.</value>
        /// <seealso cref="DataOffset"/>
        /// <seealso cref="DataLength"/>
        public Tag Tag { get; init; }            // +0

        /// <summary>Gets the offset to the value bytes, measured from the meta table start.</summary>
        /// <value>The byte offset of the first value byte within the meta table.</value>
        /// <seealso cref="Tag"/>
        /// <seealso cref="DataLength"/>
        public uint DataOffset;     // +4  (Offset32 from table start)

        /// <summary>Gets the byte length of the value.</summary>
        /// <value>The size of the value payload in bytes.</value>
        /// <seealso cref="DataOffset"/>
        public uint DataLength;     // +8

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new record with each multi-byte field reversed.</returns>
        /// <remarks>The tag uses <see cref="Tag.ReverseEndianness(Tag)"/>; the two <c>uint32</c> fields are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static DataMapRecord ReverseEndianness(DataMapRecord v) => new()
        {
            Tag = Tag.ReverseEndianness(v.Tag),
            DataOffset = BinaryPrimitives.ReverseEndianness(v.DataOffset),
            DataLength = BinaryPrimitives.ReverseEndianness(v.DataLength),
        };
    }
}

/// <summary>A single <c>meta</c> data map: a tag and the raw value bytes.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The value payload is untyped on disk; the producer may store UTF-8 text or arbitrary bytes depending on the tag's registered semantics.</description></item>
/// <item><description><see cref="AsString"/> decodes the payload as UTF-8; for tags documented as binary, that decode is lossy or meaningless.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">meta data map record</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MetaTable"/>
/// <seealso cref="MetaTable.Find(Tag)"/>
/// <seealso cref="MetaTable.GetText(Tag)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/meta">OpenType specification: meta data map record</seealso>
public sealed record MetaDataMap
{
    /// <summary>Gets the metadata tag.</summary>
    /// <value>The four-character identifier for this value.</value>
    /// <seealso cref="Data"/>
    public Tag Tag { get; init; }

    /// <summary>Gets the raw value bytes.</summary>
    /// <value>The value payload as stored in the table; not decoded or interpreted.</value>
    /// <seealso cref="Tag"/>
    /// <seealso cref="Length"/>
    /// <seealso cref="AsString"/>
    public byte[] Data { get; init; } = [];

    /// <summary>Gets the value length in bytes.</summary>
    /// <value>The size of the <see cref="Data"/> array.</value>
    /// <seealso cref="Data"/>
    public int Length => Data.Length;

    /// <summary>Interprets the value as a UTF-8 string.</summary>
    /// <returns>The payload decoded as UTF-8.</returns>
    /// <remarks>The method is only meaningful for tags whose values are documented as text; binary payloads decode to replacement characters or garbage.</remarks>
    /// <seealso cref="Data"/>
    /// <seealso cref="MetaTable.GetText(Tag)"/>
    public string AsString() => Encoding.UTF8.GetString(Data);

    /// <inheritdoc/>
    /// <returns>A diagnostic string of the form <c>'tag' (N bytes)</c>.</returns>
    /// <remarks>The format is intended for diagnostic output; it is not a stable serialization format.</remarks>
    /// <seealso cref="Tag"/>
    /// <seealso cref="Length"/>
    public override string ToString() => $"'{Tag}' ({Data.Length} bytes)";
}
