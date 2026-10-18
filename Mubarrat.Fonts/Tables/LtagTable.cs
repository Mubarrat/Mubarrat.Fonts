using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using Mubarrat.Fonts.Sfnt;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// The 'ltag' table — AAT language tag list
//
// Self-contained; no glyph count, no external context required.
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>ltag</c> table: an ordered list of IETF language tags addressed by numeric language code.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each language code is a zero-based index into this table's tag list: code <c>0</c> selects the first tag, code <c>1</c> the second, and so on. Language code <c>-1</c> is a UI convention meaning "don't care" and is not stored here.</description></item>
/// <item><description>The tags are consumed by the <c>name</c> table's language field when the platform is Unicode, by the <c>feat</c> table through feature 39 (language code) selector type, and by the <c>morx</c> table's language-sensitive subtables.</description></item>
/// <item><description>Language tag IDs are internal to the font, exactly like glyph IDs. A consumer that needs the tag's IETF identity outside the font resolves it here; a consumer that needs a localized display string resolves it through the operating system's locale APIs.</description></item>
/// <item><description>Tag strings are ASCII by specification and safe to interpret as UTF-8. The parser decodes them as UTF-8, which produces identical results for the pure-ASCII content the format defines and treats any stray non-ASCII byte as a valid code point rather than a replacement character.</description></item>
/// <item><description>The specification's prose states that range offsets are measured from the beginning of the table; the sample table in the same document shows offsets that would only make sense if measured from the start of the string region. The parser follows the prose, and a diagnostics analyzer can flag ranges that overlap the header or range array.</description></item>
/// <item><description>All multi-byte reads use the sfnt default of big-endian.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html"><c>ltag</c> table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="TagRange"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
public sealed record LtagTable : IFontTable<LtagTable>, IRecord<LtagTable>
{
    /// <summary>The only version defined by the current specification.</summary>
    /// <value>The constant <c>1</c>.</value>
    /// <seealso cref="Version"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
    public const uint CurrentVersion = 1;

    /// <inheritdoc/>
    public static Tag Tag => "ltag";

    /// <summary>Gets the table version.</summary>
    /// <value>The version read from the header. Always <see cref="CurrentVersion"/>; the parser rejects any other value.</value>
    /// <seealso cref="CurrentVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
    public uint Version { get; init; }

    /// <summary>Gets the raw flags word from the header.</summary>
    /// <value>The <c>flags</c> field as stored. The specification defines it as all-zero; the parser preserves the value for round-tripping and diagnostics.</value>
    /// <seealso cref="Header.Flags"/>
    public uint Flags { get; init; }

    /// <summary>Gets the IETF language tags in language-code order.</summary>
    /// <value>A read-only view over the tags: entry <c>i</c> is the IETF language tag selected by language code <c>i</c>. The count equals the header's <c>numTags</c> field.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The list is not sorted; its position is the language code. Tag <c>0</c> is at index <c>0</c>, tag <c>1</c> at index <c>1</c>, and so on.</description></item>
    /// <item><description>Each element is decoded from the on-disk ASCII bytes once at parse time; the raw bytes are not retained.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Count"/>
    /// <seealso cref="GetTag(int)"/>
    /// <seealso cref="TryGetTag(int, out string?)"/>
    public required IReadOnlyList<string> Tags { get; init; }

    /// <summary>Gets the number of language tags in the table.</summary>
    /// <value>The count of <see cref="Tags"/>.</value>
    /// <seealso cref="Tags"/>
    public int Count => Tags.Count;

    /// <summary>Gets the IETF language tag selected by a language code.</summary>
    /// <param name="languageCode">The language code, a zero-based index into <see cref="Tags"/>.</param>
    /// <returns>The tag when <paramref name="languageCode"/> is within range; otherwise <see langword="null"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The method returns <see langword="null"/> for out-of-range codes rather than throwing; a consumer that needs to distinguish "not present" from "empty tag" should use <see cref="TryGetTag(int, out string?)"/>.</description></item>
    /// <item><description>Language code <c>-1</c> ("don't care") always returns <see langword="null"/>, because it is not a stored index.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="TryGetTag(int, out string?)"/>
    /// <seealso cref="Tags"/>
    public string? GetTag(int languageCode) => TryGetTag(languageCode, out var tag) ? tag : null;

    /// <summary>Attempts to get the IETF language tag selected by a language code.</summary>
    /// <param name="languageCode">The language code, a zero-based index into <see cref="Tags"/>.</param>
    /// <param name="tag">When this method returns <see langword="true"/>, contains the tag; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="languageCode"/> is within range; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Distinguishes an absent code from an empty tag: an empty tag would return <see langword="true"/> with <c>""</c>, though the specification does not define empty tags.</remarks>
    /// <seealso cref="GetTag(int)"/>
    /// <seealso cref="Tags"/>
    public bool TryGetTag(int languageCode, [NotNullWhen(true)] out string? tag)
    {
        if ((uint)languageCode < (uint)Tags.Count)
        {
            tag = Tags[languageCode];
            return true;
        }
        tag = null;
        return false;
    }

    static LtagTable IRecord<LtagTable>.Parse(ref Cursor cursor, object? context)
    {
        long tableLength = cursor.Length;

        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
            throw new InvalidDataException($"'ltag'.version is {header.Version}, expected {CurrentVersion}.");
        if (header.NumTags > int.MaxValue)
            throw new InvalidDataException($"'ltag'.numTags is {header.NumTags}, which exceeds the supported maximum.");

        int numTags = (int)header.NumTags;
        TagRange[] ranges = cursor.ReadEndianReversibleStructArray<TagRange>(numTags);

        var tags = new string[numTags];
        for (int i = 0; i < numTags; i++)
        {
            TagRange range = ranges[i];
            long rangeEnd = range.Offset + range.Length;
            if (rangeEnd > tableLength)
                throw new InvalidDataException($"'ltag' tag {i} range is [{range.Offset}, {rangeEnd}) but the table length is {tableLength}.");
            tags[i] = Encoding.UTF8.GetString(cursor.Source.ReadUInt8ArrayAt(range.Offset, range.Length));
        }

        return new()
        {
            Version = header.Version,
            Flags = header.Flags,
            Tags = Array.AsReadOnly(tags),
        };
    }

    /// <summary>The 12-byte <c>ltag</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All three fields are <c>UInt32</c>, so the reversal is a single pass over the struct reinterpreted as three 32-bit words.</description></item>
    /// <item><description>The <c>flags</c> field is defined as zero by the specification; the parser preserves the value for round-tripping and diagnostics.</description></item>
    /// <item><description>Consumed at parse time and not retained on <see cref="LtagTable"/>; its field values are exposed through the semantic record's properties.</description></item>
    /// <item><description>Declared as a mutable <c>record struct</c> with <see cref="StructLayoutAttribute"/> so its fields honour the wire layout; the <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>Public because a writer fills the struct in memory, reverses it, and writes the bytes by blittable cast.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LtagTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Table version at byte offset 0.</summary>
        /// <value>The constant <see cref="CurrentVersion"/>.</value>
        /// <seealso cref="CurrentVersion"/>
        /// <seealso cref="LtagTable.Version"/>
        public uint Version;   // +0

        /// <summary>Raw flags word at byte offset 4.</summary>
        /// <value>The on-disk <c>flags</c> field; defined as zero, preserved by the parser for round-tripping and diagnostics.</value>
        /// <seealso cref="LtagTable.Flags"/>
        public uint Flags;     // +4

        /// <summary>The number of language tags that follow at byte offset 8.</summary>
        /// <value>The count of <see cref="TagRange"/> entries that immediately follow this header.</value>
        /// <seealso cref="LtagTable.Tags"/>
        /// <seealso cref="LtagTable.Count"/>
        public uint NumTags;   // +8

        /// <summary>Reverses the byte order of all three 32-bit fields in one pass.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new header with all three fields reversed.</returns>
        /// <remarks>The header is entirely <c>UInt32</c> fields, so it is reinterpreted as a span of three 32-bit words and reversed with a single <see cref="BinaryPrimitives.ReverseEndianness(ReadOnlySpan{uint}, Span{uint})"/> call.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static Header ReverseEndianness(Header value)
        {
            var words = MemoryMarshal.CreateSpan(ref value.Version, 3);
            BinaryPrimitives.ReverseEndianness(words, words);
            return value;
        }
    }

    /// <summary>A 4-byte string range that locates one tag's bytes within the <c>ltag</c> table.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Corresponds to the <c>FTStringRange</c> type in the Apple specification; renamed here to avoid confusion with the codebase's <see cref="Primitives.Tag"/> (a four-character OpenType tag, unrelated to an IETF language tag string).</description></item>
    /// <item><description>The range's <see cref="Offset"/> is measured from the start of the enclosing table, per the specification's prose; the specification's sample table appears to measure it from the start of the string region instead. The parser follows the prose.</description></item>
    /// <item><description>Both fields are <c>UInt16</c>, so the reversal is a single pass over the struct reinterpreted as two 16-bit words.</description></item>
    /// <item><description>The type is a candidate for promotion to <c>AatShared.cs</c> if another AAT table adopts the same <c>(offset, length)</c> string-range encoding.</description></item>
    /// <item><description>Public because a writer fills the struct in memory, reverses it, and writes the bytes by blittable cast.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LtagTable"/>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct TagRange : IEndianReversibleStruct<TagRange>
    {
        /// <summary>Byte offset of the tag's string from the start of the table at byte offset 0.</summary>
        /// <value>The byte position of the tag's first character within the enclosing <c>ltag</c> table.</value>
        /// <seealso cref="Length"/>
        /// <seealso cref="LtagTable.Tags"/>
        public ushort Offset;   // +0

        /// <summary>The tag's length in bytes at byte offset 2.</summary>
        /// <value>The number of ASCII bytes that compose the tag.</value>
        /// <seealso cref="Offset"/>
        /// <seealso cref="LtagTable.Tags"/>
        public ushort Length;   // +2

        /// <summary>Reverses the byte order of both 16-bit fields in one pass.</summary>
        /// <param name="value">The value whose fields are to be reversed.</param>
        /// <returns>A new range with both fields reversed.</returns>
        /// <remarks>The range is entirely <c>UInt16</c> fields, so it is reinterpreted as a span of two 16-bit words and reversed with a single <see cref="BinaryPrimitives.ReverseEndianness(ReadOnlySpan{ushort}, Span{ushort})"/> call.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}(bool)"/>
        public static TagRange ReverseEndianness(TagRange value)
        {
            var words = MemoryMarshal.CreateSpan(ref value.Offset, 2);
            BinaryPrimitives.ReverseEndianness(words, words);
            return value;
        }
    }
}
