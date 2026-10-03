using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;

namespace Mubarrat.Fonts.OpenType.Tables.Metadata;

/// <summary>The <c>DSIG</c> table: digital signature. Contains PKCS#7 signature blocks covering the font file.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Signature validation requires removing the <c>DSIG</c> table, adjusting table offsets, recalculating the <c>head</c> checksum adjustment, and hashing the result. The parser only exposes the signatures themselves; verification is out of scope.</description></item>
/// <item><description>Only format 1 is defined; other formats are preserved as raw payloads so a consumer can inspect them without the library needing to interpret them.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">DSIG table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="DsigSignature"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: DSIG table</seealso>
public sealed record DsigTable : IOpenTypeTable<DsigTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IOpenTypeTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: DSIG table</seealso>
    public static Tag Tag => "DSIG";

    /// <summary>Gets the table version. Always 1.</summary>
    /// <value>The constant <c>1</c> for a conforming DSIG table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig"><c>ulVersion</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Flags"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: <c>ulVersion</c></seealso>
    public uint Version { get; init; }

    /// <summary>Gets the permission flags. Bit 0 means the font cannot be re-signed.</summary>
    /// <value>A 16-bit flag word. Bit 0 is the only defined bit; all others are reserved.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig"><c>usFlag</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="CannotBeResigned"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: <c>usFlag</c></seealso>
    public ushort Flags { get; init; }

    /// <summary>Gets the signature records.</summary>
    /// <value>The ordered list of <see cref="DsigSignature"/> entries, one per signature record in the table.</value>
    /// <seealso cref="Count"/>
    /// <seealso cref="DsigSignature"/>
    public IReadOnlyList<DsigSignature> Signatures { get; init; } = [];

    /// <summary>Gets the number of signatures.</summary>
    /// <value>The size of the <see cref="Signatures"/> list.</value>
    /// <seealso cref="Signatures"/>
    public int Count => Signatures.Count;

    /// <summary>True when bit 0 of <see cref="Flags"/> is set.</summary>
    /// <value><see langword="true"/> when the font declares that it must not be re-signed.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig"><c>usFlag</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Flags"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: <c>usFlag</c></seealso>
    public bool CannotBeResigned => (Flags & 0x0001) != 0;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the DSIG table.</param>
    /// <param name="context">Unused. The table is self-contained.</param>
    /// <returns>The parsed DSIG table with its signature payloads materialized.</returns>
    /// <exception cref="InvalidDataException">The version is not 1, or any signature block extends past the table end.</exception>
    /// <exception cref="EndOfStreamException">The header, record array, or any referenced signature block extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Format 1 records are parsed through <see cref="SignatureBlockHeader"/>, and the PKCS#7 packet that follows the header is read into <see cref="DsigSignature.Signature"/>.</description></item>
    /// <item><description>Records with an unrecognised format are preserved as raw payloads, so a consumer can still inspect non-standard blocks.</description></item>
    /// <item><description>The source's full length is used as the bound for signature-block range checks; the table-scoped source is already limited to the declared DSIG extent.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">DSIG table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="SignatureRecord"/>
    /// <seealso cref="SignatureBlockHeader"/>
    /// <seealso cref="DsigSignature"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: DSIG table</seealso>
    public static DsigTable Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version != 1)
            throw new InvalidDataException($"'DSIG'.version is {header.Version}, expected 1.");

        SignatureRecord[] records =
            cursor.ReadBigEndianStructArray<SignatureRecord>(header.NumSignatures);

        Source source = cursor.Source;
        long tableExtent = source.Length;

        var signatures = new DsigSignature[records.Length];
        for (int i = 0; i < records.Length; i++)
        {
            SignatureRecord record = records[i];

            if (record.Format == 1)
            {
                // Format 1 block: reserved1 (2), reserved2 (2), signatureLength (4),
                // signature (signatureLength bytes). The block begins at
                // record.BlockOffset from the start of the DSIG table.
                SignatureBlockHeader blockHeader =
                    source.ReadBigEndianStructAt<SignatureBlockHeader>(record.BlockOffset);

                long signatureStart = (long)record.BlockOffset + Unsafe.SizeOf<SignatureBlockHeader>();
                long signatureLength = blockHeader.SignatureLength;

                if (signatureStart + signatureLength > tableExtent)
                    throw new InvalidDataException(
                        $"'DSIG' signature {i} extends past the table end.");

                byte[] sig = source.ReadBytesAt(signatureStart, checked((int)signatureLength));
                signatures[i] = new DsigSignature
                {
                    Format = record.Format,
                    Length = record.Length,
                    Signature = sig,
                };
            }
            else
            {
                // Unknown format: preserve the raw block so consumers can inspect it.
                long blockStart = record.BlockOffset;
                long blockLength = record.Length;

                if (blockStart + blockLength > tableExtent)
                    throw new InvalidDataException(
                        $"'DSIG' signature block {i} extends past the table end.");

                byte[] raw = source.ReadBytesAt(blockStart, checked((int)blockLength));
                signatures[i] = new DsigSignature
                {
                    Format = record.Format,
                    Length = record.Length,
                    Signature = raw,
                };
            }
        }

        return new DsigTable
        {
            Version = header.Version,
            Flags = header.Flags,
            Signatures = signatures,
        };
    }

    /// <summary>The 8-byte <c>DSIG</c> table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed immediately by <c>numSignatures</c> 12-byte <see cref="SignatureRecord"/> entries.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">DSIG header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="DsigTable"/>
    /// <seealso cref="SignatureRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: DSIG header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the table version. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming DSIG table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig"><c>ulVersion</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="NumSignatures"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: <c>ulVersion</c></seealso>
        public uint Version;   // +0

        /// <summary>Gets the number of signature records that follow the header.</summary>
        /// <value>The count of <see cref="SignatureRecord"/> entries.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig"><c>usNumSigs</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Version"/>
        /// <seealso cref="SignatureRecord"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: <c>usNumSigs</c></seealso>
        public ushort NumSignatures;   // +4

        /// <summary>Gets the permission flags. Bit 0 means the font cannot be re-signed.</summary>
        /// <value>A 16-bit flag word. Bit 0 is the only defined bit; all others are reserved.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig"><c>usFlag</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Version"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: <c>usFlag</c></seealso>
        public ushort Flags;   // +6

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            NumSignatures = BinaryPrimitives.ReverseEndianness(v.NumSignatures),
            Flags = BinaryPrimitives.ReverseEndianness(v.Flags),
        };
    }

    /// <summary>The 12-byte signature record. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Each record names a signature format and points at a block elsewhere in the DSIG table.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">DSIG signature record</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="DsigTable"/>
    /// <seealso cref="SignatureBlockHeader"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: DSIG signature record</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct SignatureRecord : IBigEndianStruct<SignatureRecord>
    {
        /// <summary>Gets the signature format. Format 1 is currently the only one defined.</summary>
        /// <value>The format discriminant from the first <c>uint32</c> of the record.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig"><c>ulFormat</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Length"/>
        /// <seealso cref="BlockOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: <c>ulFormat</c></seealso>
        public uint Format;       // +0

        /// <summary>Gets the byte length of the signature block.</summary>
        /// <value>The size of the signature block in bytes, including the format-1 header when applicable.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig"><c>ulLength</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Format"/>
        /// <seealso cref="BlockOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: <c>ulLength</c></seealso>
        public uint Length;       // +4

        /// <summary>Gets the offset to the signature block, measured from the DSIG table start.</summary>
        /// <value>The byte offset of the signature block within the DSIG table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig"><c>ulOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Format"/>
        /// <seealso cref="Length"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: <c>ulOffset</c></seealso>
        public uint BlockOffset;       // +8 (from start of DSIG table)

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new record with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are <c>uint32</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static SignatureRecord ReverseEndianness(SignatureRecord v) => new()
        {
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            Length = BinaryPrimitives.ReverseEndianness(v.Length),
            BlockOffset = BinaryPrimitives.ReverseEndianness(v.BlockOffset),
        };
    }

    /// <summary>The 8-byte format-1 signature block header. Blittable, no padding. The PKCS#7 packet follows immediately.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header's two reserved words exist to align the signature length and payload; both must be zero in conforming fonts.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">DSIG signature block</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="DsigTable"/>
    /// <seealso cref="SignatureRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: DSIG signature block</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct SignatureBlockHeader : IBigEndianStruct<SignatureBlockHeader>
    {
        /// <summary>Gets the first reserved word. Always zero.</summary>
        /// <value>Always <c>0</c> in conforming fonts; present to align the following fields.</value>
        /// <seealso cref="Reserved2"/>
        /// <seealso cref="SignatureLength"/>
        public ushort Reserved1 { get; init; }   // +0

        /// <summary>Gets the second reserved word. Always zero.</summary>
        /// <value>Always <c>0</c> in conforming fonts; present to align the following field.</value>
        /// <seealso cref="Reserved1"/>
        /// <seealso cref="SignatureLength"/>
        public ushort Reserved2 { get; init; }   // +2

        /// <summary>Gets the byte length of the PKCS#7 packet that follows.</summary>
        /// <value>The size of the PKCS#7 signature payload in bytes.</value>
        /// <seealso cref="Reserved1"/>
        /// <seealso cref="Reserved2"/>
        public uint SignatureLength;   // +4

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static SignatureBlockHeader ReverseEndianness(SignatureBlockHeader v) => new()
        {
            Reserved1 = BinaryPrimitives.ReverseEndianness(v.Reserved1),
            Reserved2 = BinaryPrimitives.ReverseEndianness(v.Reserved2),
            SignatureLength = BinaryPrimitives.ReverseEndianness(v.SignatureLength),
        };
    }
}

/// <summary>A single signature record from the <c>DSIG</c> table. For format 1, <see cref="Signature"/> holds the PKCS#7 packet; for unknown formats, it holds the raw block payload.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The signature payload is not verified by this library; the bytes are exposed as-is for a downstream validator.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">DSIG signature record</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="DsigTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/dsig">OpenType specification: DSIG signature record</seealso>
public sealed record DsigSignature
{
    /// <summary>Gets the signature format identifier. Format 1 is currently the only one defined.</summary>
    /// <value>The value of the corresponding <see cref="DsigTable.SignatureRecord.Format"/> field.</value>
    /// <seealso cref="Signature"/>
    /// <seealso cref="Length"/>
    public uint Format { get; init; }

    /// <summary>Gets the length declared in the signature record.</summary>
    /// <value>The length as recorded in the corresponding <see cref="DsigTable.SignatureRecord.Length"/> field.</value>
    /// <remarks>The declared length includes the format-1 block header when present; it is not necessarily equal to <see cref="SignatureLength"/>.</remarks>
    /// <seealso cref="Signature"/>
    /// <seealso cref="SignatureLength"/>
    public uint Length { get; init; }

    /// <summary>Gets the signature payload.</summary>
    /// <value>For format 1, the raw PKCS#7 packet; for unknown formats, the entire block including any format-specific header.</value>
    /// <seealso cref="SignatureLength"/>
    /// <seealso cref="Format"/>
    public byte[] Signature { get; init; } = [];

    /// <summary>Gets the payload length in bytes.</summary>
    /// <value>The size of the <see cref="Signature"/> array.</value>
    /// <seealso cref="Signature"/>
    public int SignatureLength => Signature.Length;
}
