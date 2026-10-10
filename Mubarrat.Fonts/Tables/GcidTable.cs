using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════════════════
// gcid — Glyph to CID Mapping Table (Apple Advanced Typography)
// ═══════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>gcid</c> table: maps the glyphs of the font to the character IDs (CIDs) of a named Adobe character collection.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table is an Apple Advanced Typography (AAT) table and is not part of OpenType. Apple documents it in the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual, <c>gcid</c> table</see>.</description></item>
/// <item><description>The table identifies a character collection by a registry, an order, and a supplement version — the three parts of a PostScript collection name such as <c>Adobe-CNS1-4</c>, which <see cref="Collection"/> composes.</description></item>
/// <item><description><see cref="Cids"/> is indexed by glyph index and starts at glyph 0. A glyph that has no CID in the identified collection is recorded as <see cref="NoCid"/> (<c>0xFFFF</c>).</description></item>
/// <item><description>Apple states that the declared CID count should not exceed the number of glyphs in the font, so a font may declare fewer CIDs than it has glyphs; <see cref="GetCid(int)"/> reports <see cref="NoCid"/> for the glyphs beyond the declared count.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Header"/>
/// <seealso cref="GetCid(int)"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual: The <c>gcid</c> table</seealso>
public sealed record GcidTable : IFontTable<GcidTable>
{
    /// <summary>Gets the AAT table tag <c>gcid</c>.</summary>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual: The <c>gcid</c> table</seealso>
    public static Tag Tag => "gcid";

    /// <summary>The CID recorded for a glyph that does not correspond to a CID in the identified collection, <c>0xFFFF</c>.</summary>
    /// <remarks>The same value is reported by <see cref="GetCid(int)"/> for a glyph index beyond the declared CID count.</remarks>
    /// <seealso cref="Cids"/>
    /// <seealso cref="GetCid(int)"/>
    public const ushort NoCid = 0xFFFF;

    /// <summary>The size, in bytes, of the fixed-layout <see cref="Header"/>: 144 bytes, dominated by the two 64-byte name fields.</summary>
    /// <seealso cref="Header"/>
    public const int HeaderSize = 144;

    /// <summary>Gets the table version.</summary>
    /// <remarks>The specification sets this field to 0. Parsing rejects any other value because no other version's layout is defined.</remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual: The <c>gcid</c> table</seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the data format of the table.</summary>
    /// <remarks>The specification sets this field to 0, the only format it defines. Parsing rejects any other value.</remarks>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual: The <c>gcid</c> table</seealso>
    public ushort Format { get; init; }

    /// <summary>Gets the size of the table, including the header, in bytes, as the font declares it.</summary>
    /// <remarks>The declared size is preserved for round-tripping and for diagnostics; parsing does not rely on it, because the CID array length comes from <see cref="Header.Count"/>.</remarks>
    /// <seealso cref="Cids"/>
    public uint Size { get; init; }

    /// <summary>Gets the registry ID of the character collection, the first part of a collection name.</summary>
    /// <remarks>For example, an Adobe collection uses registry ID 0, with <see cref="RegistryName"/> set to <c>"Adobe"</c>.</remarks>
    /// <seealso cref="RegistryName"/>
    /// <seealso cref="Order"/>
    /// <seealso cref="Collection"/>
    public ushort Registry { get; init; }

    /// <summary>Gets the registry name of the character collection.</summary>
    /// <remarks>The value is decoded from the header's fixed-width, NUL-padded ASCII field; unused bytes in that field are set to 0 by a conforming font.</remarks>
    /// <seealso cref="Registry"/>
    /// <seealso cref="Collection"/>
    public string RegistryName { get; init; } = string.Empty;

    /// <summary>Gets the order ID of the character collection, the second part of a collection name.</summary>
    /// <remarks>For example, the Adobe CNS1 collection uses order ID 2, with <see cref="OrderName"/> set to <c>"CNS1"</c>.</remarks>
    /// <seealso cref="OrderName"/>
    /// <seealso cref="Collection"/>
    public ushort Order { get; init; }

    /// <summary>Gets the order name of the character collection.</summary>
    /// <remarks>The value is decoded from the header's fixed-width, NUL-padded ASCII field; unused bytes in that field are set to 0 by a conforming font.</remarks>
    /// <seealso cref="Order"/>
    /// <seealso cref="Collection"/>
    public string OrderName { get; init; } = string.Empty;

    /// <summary>Gets the supplement version of the character collection.</summary>
    /// <remarks>The supplement version is the last part of a collection name; <c>Adobe-CNS1-4</c> has supplement version 4.</remarks>
    /// <seealso cref="Collection"/>
    public ushort SupplementVersion { get; init; }

    /// <summary>Gets the CIDs of the font's glyphs, in glyph index order.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Index <c>i</c> is the CID of glyph <c>i</c>, so the array starts at glyph 0 and need not cover every glyph of the font.</description></item>
    /// <item><description>A glyph with no CID in the identified collection is recorded as <see cref="NoCid"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetCid(int)"/>
    /// <seealso cref="Count"/>
    /// <seealso cref="NoCid"/>
    public IReadOnlyList<ushort> Cids { get; init; } = [];

    /// <summary>Gets the number of CIDs the table declares.</summary>
    /// <remarks>The size of <see cref="Cids"/>, taken from the header's <c>count</c> field. Apple states the value should not exceed the number of glyphs in the font.</remarks>
    /// <seealso cref="Cids"/>
    public int Count => Cids.Count;

    /// <summary>Gets the name of the character collection the table identifies, such as <c>Adobe-CNS1-4</c>.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The name is composed from <see cref="RegistryName"/>, <see cref="OrderName"/>, and <see cref="SupplementVersion"/>, which is the form Apple's example spells out.</description></item>
    /// <item><description>The numeric <see cref="Registry"/> and <see cref="Order"/> IDs identify the same collection by number; this property uses the names because those are what a collection name is written with.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// string collection = gcid.Collection; // "Adobe-CNS1-4"
    /// </code>
    /// </example>
    /// <seealso cref="RegistryName"/>
    /// <seealso cref="OrderName"/>
    /// <seealso cref="SupplementVersion"/>
    public string Collection => $"{RegistryName}-{OrderName}-{SupplementVersion}";

    /// <summary>Gets the CID of a glyph.</summary>
    /// <param name="glyphIndex">The glyph index to look up.</param>
    /// <returns>The glyph's CID, or <see cref="NoCid"/> when the glyph index is beyond the declared CID count.</returns>
    /// <remarks>Apple's <c>0xFFFF</c> value ("no CID in the identified collection") and this method's out-of-range answer are deliberately the same, so a caller that treats <see cref="NoCid"/> as "no CID" needs no separate bounds test.</remarks>
    /// <example>
    /// <code>
    /// ushort cid = gcid.GetCid(glyphId);
    /// bool unmapped = cid == GcidTable.NoCid;
    /// </code>
    /// </example>
    /// <seealso cref="Cids"/>
    /// <seealso cref="NoCid"/>
    public ushort GetCid(int glyphIndex) =>
        (uint)glyphIndex < (uint)Cids.Count ? Cids[glyphIndex] : NoCid;

    /// <summary>64-byte opaque storage for one of the header's fixed-width ASCII name fields.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>An inline-array struct used as a fixed-size byte buffer inside <see cref="Header"/>; a font pads the field with NUL bytes after the collection name.</description></item>
    /// <item><description>Exposed so that parsing can obtain a <see cref="ReadOnlySpan{T}"/> over the underlying bytes without unsafe code.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="MemoryMarshal.AsBytes{T}(ReadOnlySpan{T})"/>
    [InlineArray(64)]
    public struct Ascii64 { private byte _element0; }

    /// <summary>The 144-byte fixed-layout <c>gcid</c> table header.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Fields are stored in big-endian order at the offsets the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html"><c>gcid</c> table specification</see> defines. The header is followed by <see cref="Count"/> <c>uint16</c> CIDs.</description></item>
    /// <item><description>The two name fields are opaque byte buffers rather than strings, because the wire format fixes them at 64 bytes each; parsing decodes them into the <see cref="GcidTable.RegistryName"/> and <see cref="GcidTable.OrderName"/> strings.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GcidTable"/>
    /// <seealso cref="Ascii64"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual: The <c>gcid</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0.</summary>
        /// <remarks>Set to 0 by the specification.</remarks>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual: The <c>gcid</c> table</seealso>
        public ushort Version;           // +0

        /// <summary>The data format at byte offset 2.</summary>
        /// <remarks>Set to 0 by the specification.</remarks>
        /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual: The <c>gcid</c> table</seealso>
        public ushort Format;            // +2

        /// <summary>The size of the table, including the header, at byte offset 4.</summary>
        /// <seealso cref="Count"/>
        public uint Size;                // +4

        /// <summary>The registry ID of the character collection at byte offset 8.</summary>
        /// <seealso cref="RegistryName"/>
        public ushort Registry;          // +8

        /// <summary>The registry name of the character collection at byte offset 10.</summary>
        /// <remarks>A 64-byte ASCII field, NUL-padded when the name is shorter.</remarks>
        /// <seealso cref="Ascii64"/>
        /// <seealso cref="Registry"/>
        public Ascii64 RegistryName;     // +10  (64 bytes ASCII)

        /// <summary>The order ID of the character collection at byte offset 74.</summary>
        /// <seealso cref="OrderName"/>
        public ushort Order;             // +74

        /// <summary>The order name of the character collection at byte offset 76.</summary>
        /// <remarks>A 64-byte ASCII field, NUL-padded when the name is shorter.</remarks>
        /// <seealso cref="Ascii64"/>
        /// <seealso cref="Order"/>
        public Ascii64 OrderName;        // +76  (64 bytes ASCII)

        /// <summary>The supplement version of the character collection at byte offset 140.</summary>
        /// <seealso cref="Registry"/>
        /// <seealso cref="Order"/>
        public ushort SupplementVersion; // +140

        /// <summary>The number of CIDs that follow the header at byte offset 142.</summary>
        /// <remarks>Apple states this value should not exceed the number of glyphs in the font.</remarks>
        /// <seealso cref="GcidTable.Cids"/>
        public ushort Count;             // +142

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>The two 64-byte ASCII name fields are byte-sized and are copied through unchanged; only the numeric fields are reversed.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => v with
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            Size = BinaryPrimitives.ReverseEndianness(v.Size),
            Registry = BinaryPrimitives.ReverseEndianness(v.Registry),
            Order = BinaryPrimitives.ReverseEndianness(v.Order),
            SupplementVersion = BinaryPrimitives.ReverseEndianness(v.SupplementVersion),
            Count = BinaryPrimitives.ReverseEndianness(v.Count),
        };
    }

    /// <summary>Decodes a fixed-width ASCII name field, terminating at the first NUL byte.</summary>
    /// <param name="name">The raw fixed-width field, NUL-padded on the right when the name is shorter.</param>
    /// <returns>The decoded ASCII name, trimmed at the first NUL; the whole field when it holds no NUL.</returns>
    /// <remarks>The decode uses <see cref="Encoding.ASCII"/>, so bytes with the high bit set are replaced with <c>'?'</c> rather than being interpreted as UTF-8 or Latin-1. The specification calls for ASCII.</remarks>
    /// <seealso cref="Ascii64"/>
    /// <seealso cref="Encoding.ASCII"/>
    private static string DecodePaddedAscii(Ascii64 name)
    {
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(new ReadOnlySpan<Ascii64>(in name));

        int length = bytes.IndexOf((byte)0);
        if (length < 0) length = bytes.Length;

        return Encoding.ASCII.GetString(bytes[..length]);
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the gcid table.</param>
    /// <param name="context">Unused. The gcid table is self-contained and declares its own CID count.</param>
    /// <returns>The parsed gcid table.</returns>
    /// <exception cref="InvalidDataException">The version or format is not 0, or the declared CID count does not fit in the remaining bytes of the table.</exception>
    /// <exception cref="EndOfStreamException">The header or the CID array extends past the end of the table-scoped source.</exception>
    /// <remarks>The CID array length is taken from the header's <c>count</c> field; the reader consumes exactly that many two-byte values, and the table's declared <c>size</c> field is not consulted.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Cids"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6gcid.html">TrueType Reference Manual: The <c>gcid</c> table</seealso>
    static GcidTable IRecord<GcidTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version != 0)
            throw new InvalidDataException($"'gcid'.version is {header.Version}, expected 0.");

        if (header.Format != 0)
            throw new InvalidDataException($"'gcid'.format is {header.Format}, expected 0.");

        long fitting = cursor.Remaining / 2;
        if (header.Count > fitting)
        {
            throw new InvalidDataException(
                $"'gcid'.count is {header.Count}, but only {fitting} CIDs fit in the remaining " +
                $"{cursor.Remaining} bytes of the table.");
        }

        return new GcidTable
        {
            Version = header.Version,
            Format = header.Format,
            Size = header.Size,
            Registry = header.Registry,
            RegistryName = DecodePaddedAscii(header.RegistryName),
            Order = header.Order,
            OrderName = DecodePaddedAscii(header.OrderName),
            SupplementVersion = header.SupplementVersion,
            Cids = cursor.ReadUInt16Array(header.Count),
        };
    }
}
