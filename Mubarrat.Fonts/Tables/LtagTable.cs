using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// ltag — Language Tag Table (Apple Advanced Typography)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>ltag</c> table: a mapping between the numeric language codes used by the <c>name</c> table's Unicode-platform strings and IETF language tags.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is an Apple Advanced Typography (AAT) table and is not part of OpenType. Apple documents it in the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual, <c>ltag</c> table</see>.</description></item>
/// <item><description>A language code is internal to the font in the same way a glyph ID is: the codes are indices into <see cref="Tags"/>. A font uses them in <c>name</c> table strings with the Unicode platform, and in the language code feature (feature type 39) of the <c>feat</c> and <c>morx</c> tables.</description></item>
/// <item><description>The tags themselves are IETF (BCP 47) language tags such as <c>en</c>, <c>es</c>, or <c>sr-Latn</c>. The manual states that they are ASCII on disk and can therefore be interpreted as UTF-8 or any other ASCII-compatible encoding; <see cref="LanguageTag.AsString"/> decodes them as UTF-8.</description></item>
/// <item><description>The 12-byte header is followed by one four-byte <see cref="TagStringRange"/> per tag, and then by the tag string bytes those ranges address. The string region has no declared count or length: the ranges are the only route to it.</description></item>
/// <item><description>The manual's worked example stores <c>1</c>, <c>0</c>, and <c>2</c> in the header, then the ranges <c>(0, 2)</c> and <c>(2, 2)</c>, then the four bytes <c>"ensp"</c>; that table maps language code 0 to <c>en</c> and code 1 to <c>es</c>.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// foreach (LanguageTag tag in ltag.Tags)
///     Console.WriteLine(tag.AsString());   // "en", "es", "sr", ...
/// </code>
/// </example>
/// <seealso cref="TagStringRange"/>
/// <seealso cref="LanguageTag"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
public sealed record LtagTable : IFontTable<LtagTable>
{
    /// <summary>Gets the AAT table tag <c>ltag</c>.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
    public static Tag Tag => "ltag";

    /// <summary>Gets the table version.</summary>
    /// <remarks>The only version Apple defines is <c>1</c>. The parser rejects <c>0</c> and accepts higher values, which the manual leaves room for by describing 1 as the current version rather than the only one.</remarks>
    /// <seealso cref="Flags"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
    public uint Version { get; init; }

    /// <summary>Gets the table flags.</summary>
    /// <remarks>No flags are defined; the manual requires <c>0</c>. The field is preserved so that a validator can flag a non-conforming writer.</remarks>
    /// <seealso cref="Version"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
    public uint Flags { get; init; }

    /// <summary>Gets the language tags, in the order the table declares them.</summary>
    /// <remarks>A tag's index in this list is the language code that the <c>name</c>, <c>feat</c>, and <c>morx</c> tables use for it. An empty list is valid and means the table declares no tags.</remarks>
    /// <seealso cref="Count"/>
    /// <seealso cref="LanguageTag"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
    public IReadOnlyList<LanguageTag> Tags { get; init; } = [];

    /// <summary>Gets the number of language tags.</summary>
    /// <seealso cref="Tags"/>
    public int Count => Tags.Count;

    /// <summary>Size, in bytes, of a <see cref="TagStringRange"/> record.</summary>
    private const int RangeSize = 4;

    /// <summary>The 12-byte fixed-layout <c>ltag</c> table header.</summary>
    /// <remarks>Fields are stored in big-endian order at the offsets defined by the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html"><c>ltag</c> table specification</see>. The header is followed immediately by <c>numTags</c> four-byte <see cref="TagStringRange"/> records.</remarks>
    /// <seealso cref="LtagTable"/>
    /// <seealso cref="TagStringRange"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0. Currently 1.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
        public uint Version;   // +0

        /// <summary>The table flags at byte offset 4. Currently none are defined.</summary>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
        public uint Flags;     // +4

        /// <summary>The number of language tags that follow at byte offset 8.</summary>
        /// <seealso cref="TagStringRange"/>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
        public uint NumTags;   // +8

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        /// <remarks>All three fields are <c>uint32</c> and are reversed independently; the header has no byte-only fields.</remarks>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
            NumTags = BinaryPrimitives.ReverseEndianness(v.NumTags),
        };
    }

    /// <summary>A four-byte fixed-layout tag string range: where one language tag's bytes live and how long they are.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The manual names this structure <c>FTStringRange</c>.</description></item>
    /// <item><description>Ranges are not required to be ordered, aligned, or contiguous, and two ranges may overlap. The only structural requirement is that each range lies inside the table.</description></item>
    /// <item><description>The tag bytes a range addresses are plain ASCII/UTF-8 text, without a length prefix and without a terminator: the string is exactly <see cref="Length"/> bytes long. The length is the sole delimiter, so a trailing <c>NUL</c> byte inside a range would be decoded as part of the tag.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LtagTable"/>
    /// <seealso cref="LanguageTag"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct TagStringRange : IEndianReversibleStruct<TagStringRange>
    {
        /// <summary>The offset from the start of the table to the first byte of the string at byte offset 0.</summary>
        /// <seealso cref="Length"/>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
        public ushort Offset;  // +0

        /// <summary>The string length in bytes at byte offset 2.</summary>
        /// <seealso cref="Offset"/>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
        public ushort Length;  // +2

        /// <summary>Reverses the byte order of both fields in a <see cref="TagStringRange"/>.</summary>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new range with each field reversed.</returns>
        public static TagStringRange ReverseEndianness(TagStringRange v) => new()
        {
            Offset = BinaryPrimitives.ReverseEndianness(v.Offset),
            Length = BinaryPrimitives.ReverseEndianness(v.Length),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the <c>ltag</c> table.</param>
    /// <param name="context">Unused. The table is self-contained.</param>
    /// <returns>The parsed <c>ltag</c> table with every tag's bytes resolved.</returns>
    /// <exception cref="InvalidDataException">The version is 0, the range array exceeds the table's extent, or a range addresses bytes past the end of the table.</exception>
    /// <exception cref="EndOfStreamException">The header or the range array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The range array is bounded against the table's declared extent before allocation, so an absurd <c>numTags</c> is rejected instead of producing a huge allocation.</description></item>
    /// <item><description>Tag bytes are read eagerly into byte arrays and stored on the corresponding <see cref="LanguageTag"/>.</description></item>
    /// <item><description>A table with <c>numTags</c> equal to 0 parses to an empty <see cref="Tags"/> list and is not an error.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="TagStringRange"/>
    /// <seealso cref="LanguageTag"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
    static LtagTable IRecord<LtagTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version == 0)
            throw new InvalidDataException($"'ltag'.version is {header.Version}, expected 1.");

        long tableExtent = cursor.Source.Length;
        long rangesEnd = cursor.Position + (long)header.NumTags * RangeSize;

        if (rangesEnd > tableExtent)
            throw new InvalidDataException(
                $"'ltag'.numTags is {header.NumTags}, which exceeds the table's extent.");

        // The bound above proves NumTags fits in an int: a table cannot be longer than int.MaxValue bytes.
        TagStringRange[] ranges = cursor.ReadBigEndianStructArray<TagStringRange>((int)header.NumTags);

        var tags = new LanguageTag[ranges.Length];
        for (int i = 0; i < ranges.Length; i++)
        {
            TagStringRange range = ranges[i];

            if ((long)range.Offset + range.Length > tableExtent)
                throw new InvalidDataException(
                    $"'ltag' tag {i} has offset {range.Offset} and length {range.Length}, which extends past the table end.");

            tags[i] = new LanguageTag
            {
                Offset = range.Offset,
                Length = range.Length,
                Bytes = cursor.Source.ReadBytesAt(range.Offset, range.Length),
            };
        }

        return new LtagTable
        {
            Version = header.Version,
            Flags = header.Flags,
            Tags = tags,
        };
    }
}

/// <summary>One language tag of an <c>ltag</c> table: the tag's raw bytes together with the range that located them.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The value is untyped on disk; a producer is expected to store an ASCII IETF language tag there, so <see cref="AsString"/> is the intended reading.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LtagTable"/>
/// <seealso cref="LtagTable.Tags"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6ltag.html">TrueType Reference Manual: The <c>ltag</c> table</seealso>
public sealed record LanguageTag
{
    /// <summary>Gets the offset of the tag's bytes from the start of the <c>ltag</c> table.</summary>
    /// <value>The byte offset of the first byte of <see cref="Bytes"/> within the table.</value>
    /// <seealso cref="Length"/>
    /// <seealso cref="Bytes"/>
    public ushort Offset { get; init; }

    /// <summary>Gets the length of the tag in bytes.</summary>
    /// <value>The size of the string as the table declared it; equal to the length of <see cref="Bytes"/>.</value>
    /// <seealso cref="Offset"/>
    /// <seealso cref="Bytes"/>
    public ushort Length { get; init; }

    /// <summary>Gets the raw tag bytes.</summary>
    /// <value>The tag payload as stored in the table, without a terminator; not decoded or interpreted.</value>
    /// <seealso cref="AsString"/>
    /// <seealso cref="Length"/>
    public byte[] Bytes { get; init; } = [];

    /// <summary>Interprets the tag as a UTF-8 string.</summary>
    /// <returns>The tag payload decoded as UTF-8.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The manual states that IETF language tags are in ASCII and can therefore be interpreted as UTF-8 or any other ASCII-compatible text encoding, so this decode is the intended one. A payload that is not valid UTF-8 decodes to replacement characters rather than throwing.</description></item>
    /// <item><description>The length is the only delimiter: no terminator is stripped, so a range that includes a trailing <c>NUL</c> byte yields a string ending in <c>'\0'</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Bytes"/>
    /// <seealso cref="ToString()"/>
    public string AsString() => Encoding.UTF8.GetString(Bytes);

    /// <inheritdoc/>
    /// <returns>The decoded language tag, as returned by <see cref="AsString"/>.</returns>
    /// <remarks>The format is intended for diagnostic output; it is not a stable serialization format.</remarks>
    /// <seealso cref="AsString"/>
    public override string ToString() => AsString();
}
