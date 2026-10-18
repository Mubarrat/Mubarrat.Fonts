using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>fond</c> table: the Apple Advanced Typography font resource table. Provides the equivalent of classic Mac OS <c>FOND</c> and <c>NFNT</c> resource data within a TrueType font's data fork.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table lets a data-fork font behave as if it were packaged in a classic font suitcase with associated <c>FOND</c> and <c>NFNT</c> resources. It is a compatibility shim for legacy QuickDraw applications rather than a modern layout mechanism.</description></item>
/// <item><description>The table declares <c>FOND</c> entries (family-level metadata) and <c>NFNT</c> entries (individual bitmap resources). Each entry carries the equivalent of the Resource Manager data for its classic resource plus the offset and length of the resource data within this table.</description></item>
/// <item><description>Each <c>FOND</c> data block is preceded by a resource-ID-to-PostScript-name mapping table, because classic <c>FOND</c> resources identify their <c>sfnt</c> resources by resource ID, and a data-fork font has no resource table to resolve that ID against.</description></item>
/// <item><description>The table is supported in macOS starting with 10.6.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">font resource table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FondEntry"/>
/// <seealso cref="NfntEntry"/>
/// <seealso cref="ResourceMapping"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">TrueType Reference Manual: The <c>fond</c> table</seealso>
public sealed record FondTable : IFontTable<FondTable>, IRecord<FondTable>
{
    /// <summary>The version value for the only defined font resource table format, 2.</summary>
    /// <value>The constant <c>2</c>.</value>
    /// <remarks>The version field is a plain <c>uint16</c>, not a fixed-point value.</remarks>
    /// <seealso cref="Version"/>
    public const ushort CurrentVersion = 2;

    /// <summary>The fixed number of bytes reserved for a <c>FOND</c> or <c>NFNT</c> entry's name field.</summary>
    /// <value>The constant <c>255</c>.</value>
    /// <remarks>The name is stored as a C string padded to this length. The unused bytes should be set to <c>0xFF</c> or <c>0x00</c>.</remarks>
    public const int MaxNameLength = 255;

    /// <summary>The fixed number of bytes reserved for a PostScript name in a resource mapping.</summary>
    /// <value>The constant <c>62</c>.</value>
    /// <remarks>The name is stored as an ASCII C string padded to this length.</remarks>
    public const int MaxPostScriptNameLength = 62;

    /// <inheritdoc/>
    public static Tag Tag => "fond";

    /// <summary>Gets the table version.</summary>
    /// <value>The <see cref="CurrentVersion"/> value for a conforming table.</value>
    /// <seealso cref="CurrentVersion"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">TrueType Reference Manual: The <c>fond</c> table</seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the <c>FOND</c> entries, in table order.</summary>
    /// <value>A read-only view over the <see cref="FondEntry"/> records, one per classic <c>FOND</c> resource the table replaces.</value>
    /// <seealso cref="FondEntry"/>
    /// <seealso cref="NfntEntries"/>
    public required IReadOnlyList<FondEntry> FondEntries { get; init; }

    /// <summary>Gets the <c>NFNT</c> entries, in table order.</summary>
    /// <value>A read-only view over the <see cref="NfntEntry"/> records, one per classic <c>NFNT</c> resource the table replaces.</value>
    /// <seealso cref="NfntEntry"/>
    /// <seealso cref="FondEntries"/>
    public required IReadOnlyList<NfntEntry> NfntEntries { get; init; }

    /// <summary>Gets the number of <c>FOND</c> entries.</summary>
    /// <value>The count of <see cref="FondEntries"/>.</value>
    /// <seealso cref="FondEntries"/>
    public int FondCount => FondEntries.Count;

    /// <summary>Gets the number of <c>NFNT</c> entries.</summary>
    /// <value>The count of <see cref="NfntEntries"/>.</value>
    /// <seealso cref="NfntEntries"/>
    public int NfntCount => NfntEntries.Count;

    /// <summary>Tries to find the <c>FOND</c> entry with the given resource ID.</summary>
    /// <param name="resourceId">The resource ID to look up.</param>
    /// <param name="entry">When this method returns <see langword="true"/>, contains the entry; otherwise, <c>null</c>.</param>
    /// <returns><see langword="true"/> when an entry with the given resource ID is present; otherwise, <see langword="false"/>.</returns>
    /// <remarks>The lookup is linear over <see cref="FondEntries"/>. When duplicate resource IDs are present — which the specification does not forbid — the first match in table order is returned.</remarks>
    /// <seealso cref="FondEntries"/>
    /// <seealso cref="FondEntry"/>
    public bool TryGetFondEntry(short resourceId, [NotNullWhen(true)] out FondEntry? entry)
    {
        foreach (var candidate in FondEntries)
        {
            if (candidate.ResourceID == resourceId)
            {
                entry = candidate;
                return true;
            }
        }

        entry = null;
        return false;
    }

    /// <summary>Tries to find the <c>NFNT</c> entry with the given resource ID.</summary>
    /// <param name="resourceId">The resource ID to look up.</param>
    /// <param name="entry">When this method returns <see langword="true"/>, contains the entry; otherwise, <c>null</c>.</param>
    /// <returns><see langword="true"/> when an entry with the given resource ID is present; otherwise, <see langword="false"/>.</returns>
    /// <remarks>The lookup is linear over <see cref="NfntEntries"/>. When duplicate resource IDs are present, the first match in table order is returned.</remarks>
    /// <seealso cref="NfntEntries"/>
    /// <seealso cref="NfntEntry"/>
    public bool TryGetNfntEntry(short resourceId, [NotNullWhen(true)] out NfntEntry? entry)
    {
        foreach (var candidate in NfntEntries)
        {
            if (candidate.ResourceID == resourceId)
            {
                entry = candidate;
                return true;
            }
        }

        entry = null;
        return false;
    }

    static FondTable IRecord<FondTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadEndianReversibleStruct<Header>();

        if (header.Version != CurrentVersion)
            throw new InvalidDataException($"'fond'.version is {header.Version}, expected {CurrentVersion}.");

        var ctx = new ParentContext(cursor.Source);
        return new()
        {
            Version = header.Version,
            FondEntries = Array.AsReadOnly(cursor.ReadEndianReversibleHeaderRecordArray<FondEntry, FondEntry.Header>(header.FondCount, ctx)),
            NfntEntries = Array.AsReadOnly(cursor.ReadEndianReversibleHeaderRecordArray<NfntEntry, NfntEntry.Header>(header.NfntCount, ctx)),
        };
    }

    /// <summary>The 8-byte <c>fond</c> table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by the <c>FOND</c> entry array, then the <c>NFNT</c> entry array. The resource data blocks referenced by the entries are located by offset elsewhere in the table.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">font resource table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FondTable"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">TrueType Reference Manual: The <c>fond</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The table version at byte offset 0.</summary>
        /// <value>The <see cref="CurrentVersion"/> value for a conforming table.</value>
        public ushort Version;       // +0

        /// <summary>The number of <c>FOND</c> entries at byte offset 2.</summary>
        public ushort FondCount;     // +2

        /// <summary>The number of <c>NFNT</c> entries at byte offset 4.</summary>
        public ushort NfntCount;     // +4

        /// <summary>The unused field at byte offset 6.</summary>
        /// <value>The constant <c>0</c> for a conforming table. The field is reserved for future extensions and provides better data alignment.</value>
        public ushort Unused;        // +6

        /// <summary>Reverses the byte order of every field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each field reversed.</returns>
        public static Header ReverseEndianness(Header value)
        {
            Span<ushort> span = MemoryMarshal.CreateSpan(ref value.Version, 4);
            BinaryPrimitives.ReverseEndianness(span, span);
            return value;
        }
    }

    internal static string DecodeCString(ReadOnlySpan<byte> bytes, int length)
    {
        if (length <= 0) return string.Empty;

        int limit = Math.Min(length, bytes.Length);
        int end = 0;
        while (end < limit && bytes[end] != 0)
            end++;

        return end == 0 ? string.Empty : Encoding.Latin1.GetString(bytes[..end]);
    }
}

/// <summary>A parsed <c>fond</c> entry: the Resource Manager data for one classic <c>FOND</c> resource, together with the resolved resource mappings and raw FOND data.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record replaces the wire offsets with the resolved resource mappings and FOND bytes; the offsets are consumed during parse and are not exposed.</description></item>
/// <item><description>Implements <see cref="IEndianReversibleHeaderRecord{T, THeader}"/>, so the framework's default <c>Parse</c> reads the 276-byte wire header and forwards to <see cref="IHeaderRecord{T, THeader}.FromHeader"/>; the enclosing table drives the array read via <c>ReadEndianReversibleHeaderRecordArray</c>.</description></item>
/// <item><description>The <c>FromHeader</c> implementation resolves both the mapping array and the raw FOND bytes by absolute offsets within <see cref="ParentContext.ParentSource"/>; it never mutates a cursor.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">font resource table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FondTable"/>
/// <seealso cref="ResourceMapping"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">TrueType Reference Manual: The <c>fond</c> table</seealso>
public sealed record FondEntry : IEndianReversibleHeaderRecord<FondEntry, FondEntry.Header>
{
    /// <summary>Gets the resource ID of the <c>FOND</c> resource.</summary>
    /// <value>A signed 16-bit value. Usually equal to <see cref="FamilyID"/>.</value>
    /// <seealso cref="FamilyID"/>
    public required short ResourceID { get; init; }

    /// <summary>Gets the family ID field within the <c>FOND</c> resource.</summary>
    /// <value>A signed 16-bit value. The specification notes some exceptional cases where it differs from <see cref="ResourceID"/>.</value>
    /// <seealso cref="ResourceID"/>
    public required short FamilyID { get; init; }

    /// <summary>Gets the Resource Manager attributes.</summary>
    /// <value>The value returned by classic Mac OS <c>GetResAttrs()</c> for the <c>FOND</c> resource.</value>
    public required ushort Flags { get; init; }

    /// <summary>Gets the QuickDraw style value.</summary>
    /// <value>Zero for the plain style, one for bold, and so on.</value>
    public required ushort Style { get; init; }

    /// <summary>Gets the script code.</summary>
    /// <value>A signed 16-bit script code. Together with <see cref="Language"/> it determines how to convert <see cref="Name"/> to Unicode.</value>
    public required short Script { get; init; }

    /// <summary>Gets the language code.</summary>
    /// <value>A signed 16-bit language code.</value>
    public required short Language { get; init; }

    /// <summary>Gets the resource name.</summary>
    /// <value>The name decoded from the entry's fixed-size name buffer, stopping at the first null byte or the declared length.</value>
    /// <seealso cref="Script"/>
    /// <seealso cref="Language"/>
    public required string Name { get; init; }

    /// <summary>Gets the resource-ID-to-PostScript-name mappings for the associated TrueType fonts.</summary>
    /// <value>A read-only view over the <see cref="ResourceMapping"/> entries, or an empty list when the block declares no mappings.</value>
    /// <seealso cref="ResourceMapping"/>
    public required IReadOnlyList<ResourceMapping> ResourceMappings { get; init; }

    /// <summary>Gets the raw embedded <c>FOND</c> resource data.</summary>
    /// <value>A read-only view over the bytes that follow the resource mapping array within the entry's data block. The library does not parse the internal structure of a classic <c>FOND</c> resource; the bytes are exposed as-is.</value>
    public required IReadOnlyList<byte> FondData { get; init; }

    static FondEntry IHeaderRecord<FondEntry, Header>.FromHeader(in Header header, object? context)
    {
        if (context is not ParentContext ctx)
            throw new InvalidDataException("'fond' FOND entry requires a ParentContext carrying the table-scoped source.");

        Source source = ctx.ParentSource;
        long tableLength = source.Length;

        long dataStart = header.Offset;
        long dataEnd = dataStart + header.Length;
        if (dataStart < 0 || dataEnd > tableLength)
            throw new InvalidDataException(
                $"'fond' FOND entry resource ID {header.ResourceID} declares data range [{dataStart}, {dataEnd}), exceeding the table length {tableLength}.");

        uint mappingCount = source.ReadUInt32At(dataStart);
        if (mappingCount > int.MaxValue)
            throw new InvalidDataException(
                $"'fond' FOND entry resource ID {header.ResourceID} declares {mappingCount} mappings, which exceeds the supported maximum.");

        long mappingStart = dataStart + sizeof(uint);
        long mappingBytes = (long)mappingCount * Unsafe.SizeOf<ResourceMapping.Header>();
        if (mappingStart + mappingBytes > dataEnd)
            throw new InvalidDataException(
                $"'fond' FOND entry resource ID {header.ResourceID} declares {mappingCount} mappings ({mappingBytes} bytes) at offset {mappingStart}, exceeding its data block end {dataEnd}.");

        ResourceMapping[] mappings = source.ReadEndianReversibleHeaderRecordArrayAt<ResourceMapping, ResourceMapping.Header>(
            mappingStart, (int)mappingCount);

        long fondDataStart = mappingStart + mappingBytes;
        int fondDataLength = checked((int)(dataEnd - fondDataStart));
        byte[] fondData = fondDataLength > 0 ? source.ReadBytesAt(fondDataStart, fondDataLength) : [];

        return new()
        {
            ResourceID = header.ResourceID,
            FamilyID = header.FamilyID,
            Flags = header.Flags,
            Style = header.Style,
            Script = header.Script,
            Language = header.Language,
            Name = FondTable.DecodeCString(header.Name.AsSpan(), header.NameLength),
            ResourceMappings = Array.AsReadOnly(mappings),
            FondData = Array.AsReadOnly(fondData),
        };
    }

    /// <summary>The 276-byte wire form of a <c>FOND</c> entry: the Resource Manager data for one classic <c>FOND</c> resource plus the offset of its data block. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The name is stored in a fixed 255-byte field; <see cref="NameLength"/> gives the meaningful length. The unused bytes are conventionally set to <c>0xFF</c> or <c>0x00</c>.</description></item>
    /// <item><description>The <see cref="Offset"/> and <see cref="Length"/> fields locate the <c>Resource_fond</c> block and the following embedded <c>FOND</c> data within the table.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">font resource table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FondEntry"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">TrueType Reference Manual: The <c>fond</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The resource ID of the <c>FOND</c> resource at byte offset 0.</summary>
        /// <value>A signed 16-bit value. Usually equal to <see cref="FamilyID"/>; the specification notes some exceptional cases where they differ.</value>
        public short ResourceID;     // +0

        /// <summary>The family ID field within the <c>FOND</c> resource at byte offset 2.</summary>
        public short FamilyID;       // +2

        /// <summary>The Resource Manager attributes at byte offset 4.</summary>
        /// <value>The value returned by classic Mac OS <c>GetResAttrs()</c> for the <c>FOND</c> resource. The bit layout is defined by the legacy Resource Manager and is not enumerated by the TrueType specification.</value>
        public ushort Flags;         // +4

        /// <summary>The QuickDraw style value at byte offset 6.</summary>
        /// <value>Zero for the plain style, one for bold, and so on. The value identifies which classic style of the family this entry describes.</value>
        public ushort Style;         // +6

        /// <summary>The script code at byte offset 8.</summary>
        /// <value>A signed 16-bit script code. Together with <see cref="Language"/> it determines how to convert <see cref="Name"/> to Unicode.</value>
        public short Script;         // +8

        /// <summary>The language code at byte offset 10.</summary>
        /// <value>A signed 16-bit language code.</value>
        public short Language;       // +10

        /// <summary>The offset to the entry's <c>Resource_fond</c> block at byte offset 12.</summary>
        /// <value>A byte offset from the start of the <c>fond</c> table.</value>
        public uint Offset;          // +12

        /// <summary>The total length of the <c>Resource_fond</c> block and the following embedded <c>FOND</c> data at byte offset 16.</summary>
        public uint Length;          // +16

        /// <summary>The meaningful length of the name at byte offset 20.</summary>
        public byte NameLength;      // +20

        /// <summary>The name bytes at byte offset 21.</summary>
        /// <value>A fixed 255-byte field. The meaningful portion is the first <see cref="NameLength"/> bytes; the remainder is conventionally filled with <c>0xFF</c> or <c>0x00</c>.</value>
        public FixedBuffer255 Name;  // +21

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new record with the multi-byte fields reversed and the byte-only fields copied through.</returns>
        public static Header ReverseEndianness(Header value) => new()
        {
            ResourceID = BinaryPrimitives.ReverseEndianness(value.ResourceID),
            FamilyID = BinaryPrimitives.ReverseEndianness(value.FamilyID),
            Flags = BinaryPrimitives.ReverseEndianness(value.Flags),
            Style = BinaryPrimitives.ReverseEndianness(value.Style),
            Script = BinaryPrimitives.ReverseEndianness(value.Script),
            Language = BinaryPrimitives.ReverseEndianness(value.Language),
            Offset = BinaryPrimitives.ReverseEndianness(value.Offset),
            Length = BinaryPrimitives.ReverseEndianness(value.Length),
            NameLength = value.NameLength,
            Name = value.Name,
        };
    }
}

/// <summary>A parsed <c>fond</c> <c>NFNT</c> entry: the Resource Manager data for one classic <c>NFNT</c> resource, together with the raw NFNT data.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record replaces the wire offsets with the resolved NFNT bytes; the offsets are consumed during parse and are not exposed.</description></item>
/// <item><description>Implements <see cref="IEndianReversibleHeaderRecord{T, THeader}"/>, so the framework's default <c>Parse</c> reads the 274-byte wire header and forwards to <see cref="IHeaderRecord{T, THeader}.FromHeader"/>.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">font resource table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FondTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">TrueType Reference Manual: The <c>fond</c> table</seealso>
public sealed record NfntEntry : IEndianReversibleHeaderRecord<NfntEntry, NfntEntry.Header>
{
    /// <summary>Gets the resource ID of the <c>NFNT</c> resource.</summary>
    public required short ResourceID { get; init; }

    /// <summary>Gets the Resource Manager attributes.</summary>
    /// <value>The value returned by classic Mac OS <c>GetResAttrs()</c> for the <c>NFNT</c> resource.</value>
    public required ushort Flags { get; init; }

    /// <summary>Gets the QuickDraw style value.</summary>
    public required ushort Style { get; init; }

    /// <summary>Gets the script code.</summary>
    public required short Script { get; init; }

    /// <summary>Gets the language code.</summary>
    public required short Language { get; init; }

    /// <summary>Gets the resource name.</summary>
    /// <value>The name decoded from the entry's fixed-size name buffer, stopping at the first null byte or the declared length.</value>
    public required string Name { get; init; }

    /// <summary>Gets the raw embedded <c>NFNT</c> resource data.</summary>
    /// <value>A read-only view over the bytes at the entry's declared offset and length. The library does not parse the internal structure of a classic <c>NFNT</c> resource; the bytes are exposed as-is.</value>
    public required IReadOnlyList<byte> NfntData { get; init; }

    static NfntEntry IHeaderRecord<NfntEntry, Header>.FromHeader(in Header header, object? context)
    {
        if (context is not ParentContext ctx)
            throw new InvalidDataException("'fond' NFNT entry requires a ParentContext carrying the table-scoped source.");

        Source source = ctx.ParentSource;
        long tableLength = source.Length;

        long dataStart = header.Offset;
        long dataEnd = dataStart + header.Length;
        if (dataStart < 0 || dataEnd > tableLength)
            throw new InvalidDataException(
                $"'fond' NFNT entry resource ID {header.ResourceID} declares data range [{dataStart}, {dataEnd}), exceeding the table length {tableLength}.");

        int dataLength = checked((int)header.Length);
        byte[] data = dataLength > 0 ? source.ReadBytesAt(dataStart, dataLength) : [];

        return new()
        {
            ResourceID = header.ResourceID,
            Flags = header.Flags,
            Style = header.Style,
            Script = header.Script,
            Language = header.Language,
            Name = FondTable.DecodeCString(header.Name.AsSpan(), header.NameLength),
            NfntData = Array.AsReadOnly(data),
        };
    }

    /// <summary>The 274-byte wire form of an <c>NFNT</c> entry: the Resource Manager data for one classic <c>NFNT</c> resource plus the offset of its data block. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The name is stored in a fixed 255-byte field; <see cref="NameLength"/> gives the meaningful length.</description></item>
    /// <item><description>The <see cref="Offset"/> and <see cref="Length"/> fields locate the embedded <c>NFNT</c> data within the table. Because <c>NFNT</c> resources do not reference other resources, there is no <c>Resource_fond</c> block preceding them.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">font resource table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="NfntEntry"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">TrueType Reference Manual: The <c>fond</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The resource ID of the <c>NFNT</c> resource at byte offset 0.</summary>
        public short ResourceID;     // +0

        /// <summary>The Resource Manager attributes at byte offset 2.</summary>
        /// <value>The value returned by classic Mac OS <c>GetResAttrs()</c> for the <c>NFNT</c> resource.</value>
        public ushort Flags;         // +2

        /// <summary>The QuickDraw style value at byte offset 4.</summary>
        public ushort Style;         // +4

        /// <summary>The script code at byte offset 6.</summary>
        public short Script;         // +6

        /// <summary>The language code at byte offset 8.</summary>
        public short Language;       // +8

        /// <summary>The offset to the embedded <c>NFNT</c> data at byte offset 10.</summary>
        /// <value>A byte offset from the start of the <c>fond</c> table.</value>
        public uint Offset;          // +10

        /// <summary>The length of the embedded <c>NFNT</c> data at byte offset 14.</summary>
        public uint Length;          // +14

        /// <summary>The meaningful length of the name at byte offset 18.</summary>
        public byte NameLength;      // +18

        /// <summary>The name bytes at byte offset 19.</summary>
        /// <value>A fixed 255-byte field. The meaningful portion is the first <see cref="NameLength"/> bytes.</value>
        public FixedBuffer255 Name;  // +19

        /// <summary>Reverses the byte order of every multi-byte field in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new record with the multi-byte fields reversed and the byte-only fields copied through.</returns>
        public static Header ReverseEndianness(Header value) => new()
        {
            ResourceID = BinaryPrimitives.ReverseEndianness(value.ResourceID),
            Flags = BinaryPrimitives.ReverseEndianness(value.Flags),
            Style = BinaryPrimitives.ReverseEndianness(value.Style),
            Script = BinaryPrimitives.ReverseEndianness(value.Script),
            Language = BinaryPrimitives.ReverseEndianness(value.Language),
            Offset = BinaryPrimitives.ReverseEndianness(value.Offset),
            Length = BinaryPrimitives.ReverseEndianness(value.Length),
            NameLength = value.NameLength,
            Name = value.Name,
        };
    }
}

/// <summary>A parsed resource mapping from a <c>fond</c> entry's <c>Resource_fond</c> block: a resource ID and the PostScript name of the associated TrueType font.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Implements <see cref="IEndianReversibleHeaderRecord{T, THeader}"/>, so the framework's default <c>Parse</c> reads the 66-byte wire header and forwards to <see cref="IHeaderRecord{T, THeader}.FromHeader"/>; the enclosing <see cref="FondEntry"/> drives the bulk array read.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">font resource table</see> in the Apple TrueType Reference Manual.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FondTable"/>
/// <seealso cref="FondEntry"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">TrueType Reference Manual: The <c>fond</c> table</seealso>
public sealed record ResourceMapping : IEndianReversibleHeaderRecord<ResourceMapping, ResourceMapping.Header>
{
    /// <summary>Gets the resource ID.</summary>
    public required int ResourceID { get; init; }

    /// <summary>Gets the PostScript name of the associated TrueType font.</summary>
    /// <value>An ASCII string decoded from the mapping's fixed-size name buffer, stopping at the first null byte or the end of the buffer.</value>
    public required string PostScriptName { get; init; }

    static ResourceMapping IHeaderRecord<ResourceMapping, Header>.FromHeader(in Header header, object? context) => new()
    {
        ResourceID = header.ResourceID,
        PostScriptName = FondTable.DecodeCString(header.PostScriptName.AsSpan(), FondTable.MaxPostScriptNameLength),
    };

    /// <summary>The 66-byte wire form of a resource mapping: a resource ID and the PostScript name of the associated TrueType font. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The PostScript name is stored as an ASCII C string in a fixed 62-byte field.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically.</description></item>
    /// <item><description>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">font resource table</see> in the Apple TrueType Reference Manual.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ResourceMapping"/>
    /// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">TrueType Reference Manual: The <c>fond</c> table</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>The resource ID at byte offset 0.</summary>
        public int ResourceID;              // +0

        /// <summary>The PostScript name bytes at byte offset 4.</summary>
        /// <value>A fixed 62-byte field holding an ASCII C string. Unused bytes are conventionally filled with <c>0xFF</c> or <c>0x00</c>.</value>
        public FixedBuffer62 PostScriptName; // +4

        /// <summary>Reverses the byte order of the multi-byte fields in a <see cref="Header"/>.</summary>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new record with the multi-byte fields reversed and the byte-only fields copied through.</returns>
        public static Header ReverseEndianness(Header value) => new()
        {
            ResourceID = BinaryPrimitives.ReverseEndianness(value.ResourceID),
            PostScriptName = value.PostScriptName,
        };
    }
}

/// <summary>A 255-byte fixed buffer used for resource names in the <c>fond</c> table.</summary>
/// <remarks>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">font resource table</see> in the Apple TrueType Reference Manual.</remarks>
/// <seealso cref="FondTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">TrueType Reference Manual: The <c>fond</c> table</seealso>
[StructLayout(LayoutKind.Sequential, Size = 255, Pack = 1)]
public struct FixedBuffer255
{
    /// <summary>The first byte of the fixed buffer.</summary>
    public byte First;

    /// <summary>Gets the buffer contents as a span.</summary>
    /// <returns>A span covering the full 255 bytes.</returns>
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref First, 255);
}

/// <summary>A 62-byte fixed buffer used for PostScript names in the <c>fond</c> table's resource mappings.</summary>
/// <remarks>See the <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">font resource table</see> in the Apple TrueType Reference Manual.</remarks>
/// <seealso cref="FondTable"/>
/// <seealso href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6fond.html">TrueType Reference Manual: The <c>fond</c> table</seealso>
[StructLayout(LayoutKind.Sequential, Size = 62, Pack = 1)]
public struct FixedBuffer62
{
    /// <summary>The first byte of the fixed buffer.</summary>
    public byte First;

    /// <summary>Gets the buffer contents as a span.</summary>
    /// <returns>A span covering the full 62 bytes.</returns>
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref First, 62);
}
