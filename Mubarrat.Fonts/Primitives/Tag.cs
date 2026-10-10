using Mubarrat.Fonts.Binary;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.Primitives;

/// <summary>A 4-byte OpenType tag: an ASCII identifier for a table, script, feature, or similar construct. Stored as a big-endian <see cref="uint"/> on disk.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The four bytes are stored most-significant-first, so the tag <c>"cmap"</c> reads as the <see cref="uint"/> <c>0x636D6170</c> on disk and on the wire.</description></item>
/// <item><description>Blittable, size 4. <see cref="ReverseEndianness(Tag)"/> converts between native and on-disk byte order; when the value is read from a big-endian source on a little-endian host, callers apply the reverse after the raw read.</description></item>
/// <item><description>Tags shorter than four characters are right-padded with spaces in the specification (e.g. <c>"cvt "</c> for the Control Value Table); this type does not pad automatically.</description></item>
/// <item><description>Comparisons and equality operate on the raw 32-bit value, which matches the byte-wise ordering used by the table directory.</description></item>
/// </list>
/// <para>For the four-byte tag representation and the table directory that indexes them, see <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification, Data Types</see> and <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#table-directory">OpenType specification, Table Directory</see>.</para>
/// </remarks>
/// <example>
/// <code>
/// var cmap = new Tag("cmap");
/// char first = cmap[0];           // 'c'
/// string text = cmap.ToString();  // "cmap"
/// uint raw = cmap;                // 0x636D6170
/// </code>
/// </example>
/// <seealso cref="IFontTable{T}"/>
/// <seealso cref="IEndianReversibleStruct{T}"/>
/// <seealso cref="Source.ReadTagAt(long)"/>
/// <seealso cref="Cursor.ReadTag()"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Tag</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#table-directory">OpenType specification: Table Directory</seealso>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Tag : IEquatable<Tag>, IComparable<Tag>, IComparable, IEndianReversibleStruct<Tag>
{
    private readonly uint _value;

    /// <summary>Creates a tag from its raw 32-bit value.</summary>
    /// <param name="value">The four bytes of the tag packed into a <see cref="uint"/>, most-significant byte first.</param>
    /// <remarks>Use this constructor when the raw value is already known; for a character or string form, use <see cref="Tag(char, char, char, char)"/> or <see cref="Tag(string)"/> instead.</remarks>
    /// <seealso cref="Value"/>
    /// <seealso cref="Tag(char, char, char, char)"/>
    /// <seealso cref="Tag(string)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Tag</seealso>
    public Tag(uint value) => _value = value;

    /// <summary>Creates a tag from four ASCII characters.</summary>
    /// <param name="c0">The most-significant byte of the tag.</param>
    /// <param name="c1">The second byte of the tag.</param>
    /// <param name="c2">The third byte of the tag.</param>
    /// <param name="c3">The least-significant byte of the tag.</param>
    /// <exception cref="ArgumentException">Any character is outside the ASCII range.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The check accepts code points up to <c>0xFF</c>, covering ISO-8859-1 as well as ASCII. This matches the spec's wording for tag bytes, which are not strictly limited to 7-bit ASCII.</description></item>
    /// <item><description>The characters are packed left-to-right into the <see cref="uint"/>: <paramref name="c0"/> occupies bits 31–24, <paramref name="c3"/> occupies bits 7–0.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var cmap = new Tag('c', 'm', 'a', 'p');  // raw 0x636D6170
    /// </code>
    /// </example>
    /// <seealso cref="Tag(string)"/>
    /// <seealso cref="Tag(uint)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Tag</seealso>
    public Tag(char c0, char c1, char c2, char c3)
    {
        if (c0 > 0xFF || c1 > 0xFF || c2 > 0xFF || c3 > 0xFF)
            throw new ArgumentException("Tag characters must be valid ASCII characters.");
        _value = ((uint)c0 << 24) | ((uint)c1 << 16) | ((uint)c2 << 8) | c3;
    }

    /// <summary>Creates a tag from a four-character string.</summary>
    /// <param name="tag">The four-character tag. Must be exactly four characters, each at most <c>0xFF</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tag"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="tag"/> is not exactly four characters, or a character is outside the ASCII range.</exception>
    /// <remarks>The string is not trimmed or padded; callers supplying a shorter tag must pad it with trailing spaces themselves to match the specification's convention.</remarks>
    /// <example>
    /// <code>
    /// var os2 = new Tag("OS/2");
    /// var cvt = new Tag("cvt ");  // note the trailing space
    /// </code>
    /// </example>
    /// <seealso cref="Tag(char, char, char, char)"/>
    /// <seealso cref="Tag(uint)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Tag</seealso>
    public Tag(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (tag.Length != 4)
            throw new ArgumentException("Tag must be exactly 4 characters long.", nameof(tag));
        if (tag[0] > 0xFF || tag[1] > 0xFF || tag[2] > 0xFF || tag[3] > 0xFF)
            throw new ArgumentException("Tag characters must fit within a single byte.", nameof(tag));
        _value = ((uint)tag[0] << 24) | ((uint)tag[1] << 16) | ((uint)tag[2] << 8) | tag[3];
    }

    /// <summary>Gets the raw 32-bit value.</summary>
    /// <value>The tag's four bytes packed most-significant-byte first.</value>
    /// <seealso cref="Tag(uint)"/>
    /// <seealso cref="Tag(char, char, char, char)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Tag</seealso>
    public uint Value => _value;

    /// <summary>Gets the number of characters in the tag, which is always 4.</summary>
    /// <value>The constant <c>4</c>.</value>
    /// <remarks>Provided so that consumers can iterate over the tag with a <c>for</c> loop without hard-coding the length.</remarks>
    /// <seealso cref="this[int]"/>
    public int Length => 4;

    /// <summary>Gets the ASCII character at the given index.</summary>
    /// <param name="index">The byte position within the tag: 0 for the most-significant byte, 3 for the least-significant.</param>
    /// <returns>The character at <paramref name="index"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not 0–3.</exception>
    /// <remarks>Index 0 corresponds to the first character of the tag as written; the indexing order is the reverse of the underlying byte positions within the <see cref="uint"/>.</remarks>
    /// <example>
    /// <code>
    /// var tag = new Tag("cmap");
    /// char first = tag[0];   // 'c'
    /// char last  = tag[3];   // 'p'
    /// </code>
    /// </example>
    /// <seealso cref="Length"/>
    /// <seealso cref="ToString()"/>
    public char this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => index switch
        {
            0 => (char)((_value >> 24) & 0xFF),
            1 => (char)((_value >> 16) & 0xFF),
            2 => (char)((_value >> 8) & 0xFF),
            3 => (char)(_value & 0xFF),
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
    }

    /// <inheritdoc/>
    /// <param name="value">The tag whose bytes are to be reversed.</param>
    /// <returns>A <see cref="Tag"/> whose four bytes are in reversed order.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The operation is unconditional. The host-endianness check is applied by the reader (<see cref="Source.ReadEndianReversibleStructAt{T}(long)"/>, <see cref="Cursor.ReadBigEndianStruct{T}"/>), not by this method.</description></item>
    /// <item><description>Symmetric: <c>ReverseEndianness(ReverseEndianness(x)) == x</c> for every <c>x</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">Tag data type</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IEndianReversibleStruct{T}"/>
    /// <seealso cref="Source.ReadEndianReversibleStructAt{T}(long)"/>
    /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/otff#data-types">OpenType specification: Tag</seealso>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tag ReverseEndianness(Tag value) =>
        new(BinaryPrimitives.ReverseEndianness(value._value));

    /// <inheritdoc/>
    /// <returns>The tag as a four-character string. Shorter tags retain any trailing spaces they were constructed with.</returns>
    /// <remarks>The string is constructed from the four indexed characters and does not allocate during iteration; only the final <see cref="string"/> allocation occurs.</remarks>
    /// <seealso cref="this[int]"/>
    /// <seealso cref="Tag(string)"/>
    public override string ToString() => new([this[0], this[1], this[2], this[3]]);

    /// <inheritdoc/>
    /// <param name="other">The tag to compare with.</param>
    /// <returns><see langword="true"/> when both tags have identical raw values.</returns>
    /// <seealso cref="operator ==(Tag, Tag)"/>
    /// <seealso cref="IEquatable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1">.NET API: <c>IEquatable&lt;T&gt;</c></seealso>
    public bool Equals(Tag other) => _value == other._value;

    /// <inheritdoc/>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is a <see cref="Tag"/> with an identical raw value.</returns>
    /// <seealso cref="Equals(Tag)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.object.equals">.NET API: <c>Object.Equals</c></seealso>
    public override bool Equals(object? obj) => obj is Tag other && Equals(other);

    /// <inheritdoc/>
    /// <returns>A hash code derived from the raw 32-bit value.</returns>
    /// <seealso cref="Value"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.object.gethashcode">.NET API: <c>Object.GetHashCode</c></seealso>
    public override int GetHashCode() => _value.GetHashCode();

    /// <inheritdoc/>
    /// <param name="other">The tag to compare with.</param>
    /// <returns>A signed comparison of the raw 32-bit values.</returns>
    /// <remarks>Comparison is on the raw value, which orders tags byte-wise in the same order they appear on disk.</remarks>
    /// <seealso cref="CompareTo(object?)"/>
    /// <seealso cref="IComparable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.icomparable-1">.NET API: <c>IComparable&lt;T&gt;</c></seealso>
    public int CompareTo(Tag other) => _value.CompareTo(other._value);

    /// <inheritdoc/>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>A positive value when this instance is greater; zero when equal; negative when less.</returns>
    /// <exception cref="ArgumentException"><paramref name="obj"/> is neither <c>null</c> nor a <see cref="Tag"/>.</exception>
    /// <remarks>A <c>null</c> argument is treated as less than any non-null value, per the <see cref="IComparable"/> contract.</remarks>
    /// <seealso cref="CompareTo(Tag)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.icomparable">.NET API: <c>IComparable</c></seealso>
    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        Tag other => CompareTo(other),
        _ => throw new ArgumentException($"Object must be of type {nameof(Tag)}.", nameof(obj)),
    };

    /// <summary>Equality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when both tags have identical raw values.</returns>
    /// <seealso cref="Equals(Tag)"/>
    /// <seealso cref="operator !=(Tag, Tag)"/>
    public static bool operator ==(Tag left, Tag right) => left._value == right._value;

    /// <summary>Inequality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when the tags have different raw values.</returns>
    /// <seealso cref="Equals(Tag)"/>
    /// <seealso cref="operator ==(Tag, Tag)"/>
    public static bool operator !=(Tag left, Tag right) => left._value != right._value;

    /// <summary>Less-than.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/>'s raw value is less than <paramref name="right"/>'s.</returns>
    /// <remarks>Comparison is on the raw 32-bit value, which orders tags byte-wise in the same order they appear on disk.</remarks>
    /// <seealso cref="CompareTo(Tag)"/>
    /// <seealso cref="operator >(Tag, Tag)"/>
    public static bool operator <(Tag left, Tag right) => left._value < right._value;

    /// <summary>Greater-than.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/>'s raw value is greater than <paramref name="right"/>'s.</returns>
    /// <seealso cref="CompareTo(Tag)"/>
    /// <seealso cref="operator &lt;(Tag, Tag)"/>
    public static bool operator >(Tag left, Tag right) => left._value > right._value;

    /// <summary>Less-than-or-equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/>'s raw value is less than or equal to <paramref name="right"/>'s.</returns>
    /// <seealso cref="CompareTo(Tag)"/>
    /// <seealso cref="operator >=(Tag, Tag)"/>
    public static bool operator <=(Tag left, Tag right) => left._value <= right._value;

    /// <summary>Greater-than-or-equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when <paramref name="left"/>'s raw value is greater than or equal to <paramref name="right"/>'s.</returns>
    /// <seealso cref="CompareTo(Tag)"/>
    /// <seealso cref="operator &lt;=(Tag, Tag)"/>
    public static bool operator >=(Tag left, Tag right) => left._value >= right._value;

    /// <summary>Widens to <see cref="uint"/>.</summary>
    /// <param name="tag">The tag to convert.</param>
    /// <returns>The tag's raw 32-bit value.</returns>
    /// <remarks>The conversion is lossless and is the basis for the equality and comparison operators.</remarks>
    /// <seealso cref="Value"/>
    /// <seealso cref="implicit operator Tag(uint)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static implicit operator uint(Tag tag) => tag._value;

    /// <summary>Creates a tag from a <see cref="uint"/>.</summary>
    /// <param name="value">The raw 32-bit value to wrap.</param>
    /// <returns>A <see cref="Tag"/> wrapping <paramref name="value"/> unchanged.</returns>
    /// <remarks>Implicit because every <see cref="uint"/> is a valid tag; the inverse conversion to <see cref="uint"/> is also implicit, so round-tripping through <see cref="uint"/> is transparent.</remarks>
    /// <seealso cref="Tag(uint)"/>
    /// <seealso cref="implicit operator uint(Tag)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static implicit operator Tag(uint value) => new(value);

    /// <summary>Converts to a four-character string.</summary>
    /// <param name="tag">The tag to convert.</param>
    /// <returns>The tag as a four-character string.</returns>
    /// <remarks>Shorthand for <see cref="ToString()"/>; useful in string interpolation and concatenation where the implicit form reads more naturally.</remarks>
    /// <seealso cref="ToString()"/>
    /// <seealso cref="implicit operator Tag(string)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static implicit operator string(Tag tag) => tag.ToString();

    /// <summary>Creates a tag from a four-character string.</summary>
    /// <param name="tag">The four-character string.</param>
    /// <returns>A <see cref="Tag"/> whose bytes are taken from <paramref name="tag"/> in order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tag"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="tag"/> is not exactly four characters, or a character is outside the ASCII range.</exception>
    /// <remarks>Implicit so that call sites can pass a string literal directly; the length and range checks are those of <see cref="Tag(string)"/>.</remarks>
    /// <example>
    /// <code>
    /// Tag head = "head";  // uses the implicit string → Tag conversion
    /// </code>
    /// </example>
    /// <seealso cref="Tag(string)"/>
    /// <seealso cref="implicit operator string(Tag)"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/user-defined-conversion-operators">C# reference: User-defined conversion operators</seealso>
    public static implicit operator Tag(string tag) => new(tag);
}
