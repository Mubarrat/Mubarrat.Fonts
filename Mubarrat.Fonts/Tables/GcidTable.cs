using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>gcid</c> table: the Apple Advanced Typography glyph-to-CID mapping table. Maps each glyph in the font to a CID within a named Adobe character collection.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table identifies a character collection by its registry and order — for example, Adobe-CNS1 with supplement version 4 — and stores one <c>uint16</c> CID per glyph, in glyph index order. A glyph that has no CID in the identified collection stores <see cref="NoCid"/>.</description></item>
/// <item><description>The table is used by AAT when a font needs to interoperate with CID-keyed PostScript workflows. It is a mapping table, not a shaping table: it tells a consumer which CID corresponds to each glyph, but does not describe how glyphs are selected or positioned.</description></item>
/// <item><description>The specification states that the entry count "should not exceed the number of glyphs in the font". The parser does not enforce the constraint; a validator may flag a mismatch as a rule.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">glyph-to-CID mapping table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Header"/>
/// <seealso cref="MaxpTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual: The <c>gcid</c> table</seealso>
public sealed record GcidTable : IFontTable<GcidTable>, IRecord<GcidTable>
{
    /// <summary>The only value the specification permits in the <c>version</c> field.</summary>
    /// <value>The constant <c>0</c>.</value>
    /// <seealso cref="Version"/>
    public const ushort CurrentVersion = 0;

    /// <summary>The only value the specification permits in the <c>format</c> field.</summary>
    /// <value>The constant <c>0</c>.</value>
    /// <seealso cref="Format"/>
    public const ushort CurrentFormat = 0;

    /// <summary>The CID value that marks a glyph with no CID in the identified collection.</summary>
    /// <value>The constant <c>0xFFFF</c>.</value>
    /// <remarks>A glyph whose entry in <see cref="Cids"/> equals this value has no corresponding CID in the character collection named by <see cref="RegistryName"/> and <see cref="OrderName"/>.</remarks>
    /// <seealso cref="Cids"/>
    public const ushort NoCid = 0xFFFF;

    /// <summary>The fixed number of bytes reserved for each ASCII name field.</summary>
    /// <value>The constant <c>64</c>.</value>
    /// <remarks>Both <see cref="RegistryName"/> and <see cref="OrderName"/> occupy a fixed sixty-four-byte field. Unused bytes should be set to zero.</remarks>
    public const int MaxNameLength = 64;

    /// <inheritdoc/>
    public static Tag Tag => "gcid";

    /// <summary>Gets the table version. Always 0.</summary>
    /// <value>The constant <see cref="CurrentVersion"/>.</value>
    /// <remarks>The specification defines only one version and requires the field to be zero.</remarks>
    /// <seealso cref="CurrentVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual: The <c>gcid</c> table</seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the data format. Always 0.</summary>
    /// <value>The constant <see cref="CurrentFormat"/>.</value>
    /// <remarks>The specification defines only one format and requires the field to be zero.</remarks>
    /// <seealso cref="CurrentFormat"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual: The <c>gcid</c> table</seealso>
    public ushort Format { get; init; }

    /// <summary>Gets the table size in bytes, including the header.</summary>
    /// <value>The byte offset of the end of the CID array, as declared by the table. The value should equal the actual table length in bytes.</value>
    /// <seealso cref="Cids"/>
    public uint Size { get; init; }

    /// <summary>Gets the registry ID of the character collection.</summary>
    /// <value>The registry identifier. The specification's example uses <c>0</c> for the Adobe registry.</value>
    /// <seealso cref="RegistryName"/>
    /// <seealso cref="Order"/>
    /// <seealso cref="OrderName"/>
    public ushort Registry { get; init; }

    /// <summary>Gets the registry name of the character collection.</summary>
    /// <value>An ASCII string decoded from the table's fixed sixty-four-byte registry name field, stopping at the first null byte. The specification's example uses <c>"Adobe"</c>.</value>
    /// <seealso cref="Registry"/>
    /// <seealso cref="OrderName"/>
    public required string RegistryName { get; init; }

    /// <summary>Gets the order ID of the character collection.</summary>
    /// <value>The order identifier. The specification's example uses <c>2</c> for the CNS1 order.</value>
    /// <seealso cref="OrderName"/>
    /// <seealso cref="Registry"/>
    public ushort Order { get; init; }

    /// <summary>Gets the order name of the character collection.</summary>
    /// <value>An ASCII string decoded from the table's fixed sixty-four-byte order name field, stopping at the first null byte. The specification's example uses <c>"CNS1"</c>.</value>
    /// <seealso cref="Order"/>
    /// <seealso cref="RegistryName"/>
    public required string OrderName { get; init; }

    /// <summary>Gets the supplement version of the character collection.</summary>
    /// <value>The supplement version within the identified registry and order. The specification's example uses <c>4</c> for Adobe-CNS1-4.</value>
    /// <seealso cref="RegistryName"/>
    /// <seealso cref="OrderName"/>
    public ushort SupplementVersion { get; init; }

    /// <summary>Gets the CIDs for the glyphs in the font, in glyph index order.</summary>
    /// <value>An array whose entry <c>i</c> is the CID for glyph <c>i</c>, or <see cref="NoCid"/> when the glyph has no corresponding CID in the identified character collection.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The array length is the table's declared <see cref="Count"/>.</description></item>
    /// <item><description>The specification states that the count should not exceed the font's glyph count, but does not require it; the parser reads as many entries as the table declares.</description></item>
    /// <item><description>The array is indexed by glyph ID starting at glyph 0, so an entry's position within the array is the glyph ID it describes.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Count"/>
    /// <seealso cref="NoCid"/>
    /// <seealso cref="TryGetCid(int, out ushort)"/>
    public required IReadOnlyList<ushort> Cids { get; init; }

    /// <summary>Gets the number of CIDs in the table.</summary>
    /// <value>The length of <see cref="Cids"/>.</value>
    /// <seealso cref="Cids"/>
    public int Count => Cids.Count;

    /// <summary>Tries to find the CID for a glyph.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <param name="cid">When this method returns <see langword="true"/>, contains the glyph's CID; otherwise, <see cref="NoCid"/>.</param>
    /// <returns><see langword="true"/> when the table has an entry for <paramref name="glyphIndex"/>; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup is a bounds check followed by an indexed read, because the array is indexed directly by glyph ID.</description></item>
    /// <item><description>A glyph whose entry is <see cref="NoCid"/> is reported as <see langword="true"/> with <see cref="NoCid"/>, because the table does have an entry for that glyph; it simply maps to no CID. This is different from a glyph index beyond the array's length, which is reported as <see langword="false"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Cids"/>
    /// <seealso cref="NoCid"/>
    public bool TryGetCid(int glyphIndex, out ushort cid)
    {
        if ((uint)glyphIndex < (uint)Cids.Count)
        {
            cid = Cids[glyphIndex];
            return true;
        }

        cid = NoCid;
        return false;
    }

    /// <summary>Gets the CID for a glyph, or <see cref="NoCid"/> when the table has no entry for it.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <returns>The glyph's CID, or <see cref="NoCid"/> when the glyph index is beyond the table's array length.</returns>
    /// <seealso cref="TryGetCid(int, out ushort)"/>
    /// <seealso cref="NoCid"/>
    public ushort GetCid(int glyphIndex) => TryGetCid(glyphIndex, out var cid) ? cid : NoCid;

    /// <summary>The 144-byte <c>gcid</c> table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed by <see cref="Count"/> <c>uint16</c> CID values. The total table size is <see cref="Size"/> bytes.</description></item>
    /// <item><description>The two name fields are fixed sixty-four-byte buffers holding ASCII C strings. Unused bytes should be set to zero.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">glyph-to-CID mapping table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GcidTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual: The <c>gcid</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0. Always 0.</summary>
        /// <value>The constant <see cref="CurrentVersion"/>.</value>
        public ushort Version;              // +0

        /// <summary>The data format at byte offset 2. Always 0.</summary>
        /// <value>The constant <see cref="CurrentFormat"/>.</value>
        public ushort Format;               // +2

        /// <summary>The table size in bytes, including the header, at byte offset 4.</summary>
        public uint Size;                   // +4

        /// <summary>The registry ID at byte offset 8.</summary>
        public ushort Registry;             // +8

        /// <summary>The registry name bytes at byte offset 10.</summary>
        /// <value>A fixed sixty-four-byte field holding an ASCII C string. Unused bytes should be set to zero.</value>
        public FixedBuffer64 RegistryName;  // +10

        /// <summary>The order ID at byte offset 74.</summary>
        public ushort Order;                // +74

        /// <summary>The order name bytes at byte offset 76.</summary>
        /// <value>A fixed sixty-four-byte field holding an ASCII C string. Unused bytes should be set to zero.</value>
        public FixedBuffer64 OrderName;     // +76

        /// <summary>The supplement version at byte offset 140.</summary>
        public ushort SupplementVersion;    // +140

        /// <summary>The number of CIDs that follow at byte offset 142.</summary>
        public ushort Count;                // +142

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with the multi-byte fields reversed and the byte-only fields copied through.</returns>
        /// <remarks>The two name buffers are byte-only and do not participate in the reversal.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadEndianReversibleStruct{T}"/>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual: The <c>gcid</c> table</seealso>
        public static Header ReverseEndianness(Header value) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(value.Version),
            Format = BinaryPrimitives.ReverseEndianness(value.Format),
            Size = BinaryPrimitives.ReverseEndianness(value.Size),
            Registry = BinaryPrimitives.ReverseEndianness(value.Registry),
            RegistryName = value.RegistryName,
            Order = BinaryPrimitives.ReverseEndianness(value.Order),
            OrderName = value.OrderName,
            SupplementVersion = BinaryPrimitives.ReverseEndianness(value.SupplementVersion),
            Count = BinaryPrimitives.ReverseEndianness(value.Count),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">The cursor positioned at the first byte of the <c>gcid</c> table.</param>
    /// <param name="context">Unused. The table is self-contained.</param>
    /// <returns>The parsed <c>gcid</c> table.</returns>
    /// <exception cref="InvalidDataException">The version is not zero, or the format is not zero.</exception>
    /// <exception cref="EndOfStreamException">The header or the CID array extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is read as a single struct, then the CID array is read in one bulk call sized by <see cref="Header.Count"/>.</description></item>
    /// <item><description>The declared <see cref="Header.Size"/> is not validated against the actual table length. A consumer that wants to check the two agree can compare <see cref="Size"/> against the table's directory entry.</description></item>
    /// <item><description>The two ASCII name fields are decoded during parse and are not retained on the semantic record as raw buffers.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual: The <c>gcid</c> table</seealso>
    static GcidTable IRecord<GcidTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
            throw new InvalidDataException($"'gcid'.version is {header.Version}, expected {CurrentVersion}.");

        if (header.Format != CurrentFormat)
            throw new InvalidDataException($"'gcid'.format is {header.Format}, expected {CurrentFormat}.");

        return new()
        {
            Version = header.Version,
            Format = header.Format,
            Size = header.Size,
            Registry = header.Registry,
            RegistryName = DecodeAscii(header.RegistryName.AsSpan()),
            Order = header.Order,
            OrderName = DecodeAscii(header.OrderName.AsSpan()),
            SupplementVersion = header.SupplementVersion,
            Cids = cursor.ReadUInt16Array(header.Count),
        };
    }

    /// <summary>Decodes a fixed-size byte buffer as an ASCII C string, stopping at the first null byte.</summary>
    /// <param name="bytes">The fixed-size byte buffer.</param>
    /// <returns>The decoded string, or an empty string when the buffer begins with a null byte.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The specification states that unused bytes should be set to zero, so the first null byte marks the end of the meaningful content.</description></item>
    /// <item><description>The bytes are decoded as ASCII, which matches the specification's stated encoding for both name fields.</description></item>
    /// </list>
    /// </remarks>
    private static string DecodeAscii(ReadOnlySpan<byte> bytes)
    {
        int end = bytes.IndexOf((byte)0);
        if (end < 0) end = bytes.Length;
        return end == 0 ? string.Empty : Encoding.ASCII.GetString(bytes[..end]);
    }
}

/// <summary>A 64-byte fixed buffer used for the <c>registryName</c> and <c>orderName</c> fields of the <c>gcid</c> table.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The buffer holds an ASCII C string. Unused bytes should be set to zero.</description></item>
/// <item><description>The type exists because C# has no first-class fixed-size array in a <c>record struct</c> that composes with <see cref="StructLayoutAttribute"/> and <see cref="Cursor.ReadEndianReversibleStruct{T}"/>.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">glyph-to-CID mapping table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GcidTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual: The <c>gcid</c> table</seealso>
[StructLayout(LayoutKind.Sequential, Size = 64, Pack = 1)]
public struct FixedBuffer64
{
    /// <summary>The first byte of the fixed buffer.</summary>
    public byte First;

    /// <summary>Gets the buffer contents as a span.</summary>
    /// <returns>A span covering the full sixty-four bytes.</returns>
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref First, 64);
}
