using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Mubarrat.Fonts.Tables;

/// <summary>The <c>SVG </c> table: Scalable Vector Graphics. Contains SVG descriptions for some or all of the glyphs in the font.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Each SVG document may cover a range of glyph IDs. Documents may be plain UTF-8 XML or gzip-compressed; the parser exposes the encoded bytes and, on demand, the decoded text.</description></item>
/// <item><description>Unlike <c>COLR</c>/<c>CPAL</c> which describe colour as layered outlines, <c>SVG </c> embeds full vector documents that a renderer interprets directly.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">SVG table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SvgDocumentRecord"/>
/// <seealso cref="ColrTable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: SVG table</seealso>
public sealed record SvgTable : IFontTable<SvgTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: SVG table</seealso>
    public static Tag Tag => "SVG ";

    /// <summary>Gets the table version. Always 0.</summary>
    /// <value>The constant <c>0</c> for a conforming SVG table.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg"><c>version</c> field</see> in the OpenType specification.</remarks>
    /// <seealso cref="Records"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: <c>version</c></seealso>
    public ushort Version { get; init; }

    /// <summary>Gets the document records, in table order.</summary>
    /// <value>The ordered list of <see cref="SvgDocumentRecord"/> entries declared by the table. Empty when the table declares no records.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The list order follows the on-disk order; it is not required to be sorted by glyph range.</description></item>
    /// <item><description>Each record's payload is read into a byte array eagerly during parse; the text and XML views are derived lazily on first access.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FindRecord(int)"/>
    /// <seealso cref="Count"/>
    /// <seealso cref="SvgDocumentRecord"/>
    public IReadOnlyList<SvgDocumentRecord> Records { get; init; } = [];

    /// <summary>Gets the number of document records.</summary>
    /// <value>The size of the <see cref="Records"/> list.</value>
    /// <seealso cref="Records"/>
    public int Count => Records.Count;

    /// <summary>Returns the record covering a glyph, or <c>null</c> when none does.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The first <see cref="SvgDocumentRecord"/> whose range includes <paramref name="glyphId"/>, or <c>null</c> when no record matches.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup is linear over <see cref="Records"/>. The table is typically small (one or a few records), so the linear scan is not usually a concern.</description></item>
    /// <item><description>The glyph ranges are not required to be sorted by <c>StartGlyphId</c>, so a binary search would not be safe without a sort step.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GetSvgDocument(int)"/>
    /// <seealso cref="Records"/>
    /// <seealso cref="SvgDocumentRecord.StartGlyphId"/>
    /// <seealso cref="SvgDocumentRecord.EndGlyphId"/>
    public SvgDocumentRecord? FindRecord(int glyphId)
    {
        foreach (var r in Records)
            if (glyphId >= r.StartGlyphId && glyphId <= r.EndGlyphId) return r;
        return null;
    }

    /// <summary>Returns the decoded SVG document string for a glyph, or <c>null</c> when no record covers the glyph. Gzip-compressed documents are transparently decompressed.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The decoded SVG text for the glyph, or <c>null</c> when no record covers it.</returns>
    /// <remarks>Convenience composition of <see cref="FindRecord(int)"/> and <see cref="SvgDocumentRecord.GetDecodedText"/>.</remarks>
    /// <seealso cref="FindRecord(int)"/>
    /// <seealso cref="SvgDocumentRecord.GetDecodedText"/>
    /// <seealso cref="SvgDocumentRecord.Text"/>
    public string? GetSvgDocument(int glyphId) => FindRecord(glyphId)?.GetDecodedText();

    /// <summary>The 10-byte <c>SVG </c> table header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed by an <c>SVGDocumentList</c> at <see cref="SvgDocumentListOffset"/>, which begins with a <c>uint16</c> record count.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">SVG header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SvgTable"/>
    /// <seealso cref="DocumentRecordHeader"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: SVG header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the table version. Always 0.</summary>
        /// <value>The constant <c>0</c> for a conforming SVG table.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg"><c>version</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="SvgDocumentListOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: <c>version</c></seealso>
        public ushort Version;                   // +0

        /// <summary>Gets the offset from the table start to the SVG document list.</summary>
        /// <value>The byte offset of the document list's record count, measured from the table start.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg"><c>svgDocumentListOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Version"/>
        /// <seealso cref="Reserved"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: <c>svgDocumentListOffset</c></seealso>
        public uint SvgDocumentListOffset;       // +2

        /// <summary>Gets the reserved field. Always 0.</summary>
        /// <value>Always <c>0</c> in conforming fonts.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg"><c>reserved</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="Version"/>
        /// <seealso cref="SvgDocumentListOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: <c>reserved</c></seealso>
        public uint Reserved;                    // +6

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: SVG header</seealso>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            SvgDocumentListOffset = BinaryPrimitives.ReverseEndianness(v.SvgDocumentListOffset),
            Reserved = BinaryPrimitives.ReverseEndianness(v.Reserved),
        };
    }

    /// <summary>A 12-byte SVG document record. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Each record names a contiguous glyph ID range and points at an SVG payload elsewhere in the table.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">SVG document record</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SvgTable"/>
    /// <seealso cref="SvgTable.Header"/>
    /// <seealso cref="SvgDocumentRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: SVG document record</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct DocumentRecordHeader : IEndianReversibleStruct<DocumentRecordHeader>
    {
        /// <summary>Gets the first glyph ID covered.</summary>
        /// <value>The inclusive lower bound of the glyph ID range that shares the document.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg"><c>startGlyphID</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="EndGlyphID"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: <c>startGlyphID</c></seealso>
        public ushort StartGlyphID;       // +0

        /// <summary>Gets the last glyph ID covered (inclusive).</summary>
        /// <value>The inclusive upper bound of the glyph ID range that shares the document.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg"><c>endGlyphID</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="StartGlyphID"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: <c>endGlyphID</c></seealso>
        public ushort EndGlyphID;         // +2

        /// <summary>Gets the offset from the SVG table start to the SVG payload.</summary>
        /// <value>The byte offset of the encoded document, measured from the table start.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg"><c>svgDocOffset</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="SvgDocLength"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: <c>svgDocOffset</c></seealso>
        public uint SvgDocOffset;         // +4  (from start of SVG table)

        /// <summary>Gets the byte length of the SVG payload.</summary>
        /// <value>The size of the encoded document in bytes. Zero is invalid per the specification.</value>
        /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg"><c>svgDocLength</c> field</see> in the OpenType specification.</remarks>
        /// <seealso cref="SvgDocOffset"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: <c>svgDocLength</c></seealso>
        public uint SvgDocLength;         // +8

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new record header with each multi-byte field reversed.</returns>
        /// <remarks>All four fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: SVG document record</seealso>
        public static DocumentRecordHeader ReverseEndianness(DocumentRecordHeader v) => new()
        {
            StartGlyphID = BinaryPrimitives.ReverseEndianness(v.StartGlyphID),
            EndGlyphID = BinaryPrimitives.ReverseEndianness(v.EndGlyphID),
            SvgDocOffset = BinaryPrimitives.ReverseEndianness(v.SvgDocOffset),
            SvgDocLength = BinaryPrimitives.ReverseEndianness(v.SvgDocLength),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the SVG table.</param>
    /// <param name="context">Unused. The SVG table is self-contained.</param>
    /// <returns>The parsed SVG table with all document payloads materialized.</returns>
    /// <exception cref="InvalidDataException">The version is not 0, the document-list offset is zero, the list is empty, or any record has an invalid offset, length, or glyph range.</exception>
    /// <exception cref="EndOfStreamException">The header, document list, or any referenced payload extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is read first; the document list is then located through <see cref="Header.SvgDocumentListOffset"/>, which is relative to the table start.</description></item>
    /// <item><description>Each document's payload is read eagerly into a byte array; the text and XML views are derived lazily on first access through <see cref="SvgDocumentRecord.Text"/> and <see cref="SvgDocumentRecord.Document"/>.</description></item>
    /// <item><description>Every record's glyph range is validated (start &lt;= end) during parse; overlapping ranges are tolerated and resolved by first-match in <see cref="FindRecord(int)"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">SVG table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="DocumentRecordHeader"/>
    /// <seealso cref="SvgDocumentRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: SVG table</seealso>
    static SvgTable IRecord<SvgTable>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Version != 0)
            throw new InvalidDataException($"'SVG '.version is {header.Version}, expected 0.");
        if (header.SvgDocumentListOffset == 0)
            throw new InvalidDataException("'SVG ' svgDocumentListOffset is 0.");

        Source source = cursor.Source;
        var listCursor = source.CreateCursor(header.SvgDocumentListOffset);

        int numEntries = listCursor.ReadUInt16();
        if (numEntries == 0)
            throw new InvalidDataException("'SVG ' document list is empty.");

        DocumentRecordHeader[] headers =
            listCursor.ReadBigEndianStructArray<DocumentRecordHeader>(numEntries);

        var records = new SvgDocumentRecord[numEntries];
        for (int i = 0; i < numEntries; i++)
        {
            DocumentRecordHeader h = headers[i];

            if (h.SvgDocOffset == 0)
                throw new InvalidDataException($"'SVG ' record {i} has zero svgDocOffset.");
            if (h.SvgDocLength == 0)
                throw new InvalidDataException($"'SVG ' record {i} has zero svgDocLength.");
            if (h.StartGlyphID > h.EndGlyphID)
                throw new InvalidDataException(
                    $"'SVG ' record {i} has startGlyphID {h.StartGlyphID} > endGlyphID {h.EndGlyphID}.");

            // svgDocOffset is relative to the start of the SVG table, which is the base
            // of source.
            byte[] data = source.ReadBytesAt(h.SvgDocOffset, checked((int)h.SvgDocLength));

            records[i] = new SvgDocumentRecord(data)
            {
                StartGlyphId = h.StartGlyphID,
                EndGlyphId = h.EndGlyphID,
            };
        }

        return new SvgTable { Version = header.Version, Records = records };
    }
}

/// <summary>A single <c>SVG </c> document record.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record holds the encoded payload bytes as its primary representation. Text and XML views are derived on first access and cached for the lifetime of the record.</description></item>
/// <item><description>A caller that only inspects the record's range or payload size pays nothing; a caller that reads text pays the UTF-8 decode once; a caller that wants the tree pays the DOM parse once.</description></item>
/// <item><description><see cref="EncodedData"/> is a <see cref="ReadOnlyMemory{T}"/>, so the caller cannot mutate the backing storage through the property.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>The lazy caches are not thread-safe; concurrent first access to <see cref="Document"/> from multiple threads may run the parse more than once.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SvgTable"/>
/// <seealso cref="SvgTable.FindRecord(int)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg">OpenType specification: SVG document record</seealso>
public sealed record SvgDocumentRecord
{
    private readonly byte[] _encodedData;
    private string? _text;
    private XDocument? _document;
    private bool _documentParsed;

    /// <summary>Creates a record from an encoded payload.</summary>
    /// <param name="encodedData">The raw document bytes. May be plain UTF-8 or gzip-compressed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="encodedData"/> is <c>null</c>.</exception>
    /// <remarks>The array is retained by reference; the caller must not mutate it after construction.</remarks>
    /// <seealso cref="EncodedData"/>
    /// <seealso cref="IsGzip"/>
    public SvgDocumentRecord(byte[] encodedData)
    {
        ArgumentNullException.ThrowIfNull(encodedData);
        _encodedData = encodedData;
    }

    /// <summary>Gets the first glyph ID in the range.</summary>
    /// <value>The inclusive lower bound of the glyph ID range that shares this document.</value>
    /// <seealso cref="EndGlyphId"/>
    /// <seealso cref="SvgTable.FindRecord(int)"/>
    public ushort StartGlyphId { get; init; }

    /// <summary>Gets the last glyph ID in the range (inclusive).</summary>
    /// <value>The inclusive upper bound of the glyph ID range that shares this document.</value>
    /// <seealso cref="StartGlyphId"/>
    /// <seealso cref="SvgTable.FindRecord(int)"/>
    public ushort EndGlyphId { get; init; }

    /// <summary>Gets the encoded document bytes. May be plain UTF-8 or gzip-compressed.</summary>
    /// <value>A read-only view over the payload as it was stored in the table.</value>
    /// <remarks>Use <see cref="IsGzip"/> to check the encoding; use <see cref="Text"/> or <see cref="Document"/> to obtain a decoded view.</remarks>
    /// <seealso cref="Length"/>
    /// <seealso cref="IsGzip"/>
    /// <seealso cref="Text"/>
    public ReadOnlyMemory<byte> EncodedData => _encodedData;

    /// <summary>Gets the encoded length in bytes.</summary>
    /// <value>The size of <see cref="EncodedData"/>, not the decoded text length.</value>
    /// <seealso cref="EncodedData"/>
    public int Length => _encodedData.Length;

    /// <summary>True when the payload begins with the gzip magic bytes 0x1F 0x8B.</summary>
    /// <value><see langword="true"/> when the first two bytes of <see cref="EncodedData"/> are the gzip signature.</value>
    /// <remarks>The check is a signature test only; it does not verify that the rest of the payload is a well-formed gzip stream.</remarks>
    /// <seealso cref="EncodedData"/>
    /// <seealso cref="Text"/>
    public bool IsGzip => _encodedData.Length >= 2
        && _encodedData[0] == 0x1F
        && _encodedData[1] == 0x8B;

    /// <summary>Gets the decoded SVG text. Plain UTF-8 payloads are returned as-is; gzip payloads are decompressed first. The result is cached after the first access. Returns <see cref="string.Empty"/> when decoding fails.</summary>
    /// <value>The decoded SVG document as a string, or <see cref="string.Empty"/> on decode failure.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The first access performs the decode; subsequent accesses return the cached string.</description></item>
    /// <item><description>Decode failures are silent — a malformed gzip stream, an oversized payload, or an internal exception all return <see cref="string.Empty"/>. Callers that need to distinguish success from failure should inspect <see cref="EncodedData"/> directly.</description></item>
    /// <item><description>Access is not thread-safe; see the type-level remarks.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Document"/>
    /// <seealso cref="DecodeText"/>
    /// <seealso cref="GetDecodedText"/>
    public string Text => _text ??= DecodeText();

    /// <summary>Gets the parsed XML document, or <c>null</c> when the payload is not valid XML. Parsed on first access and cached. Whitespace and comments are preserved so text content inside <c>&lt;text&gt;</c> elements survives round-tripping.</summary>
    /// <value>The parsed <see cref="XDocument"/>, or <c>null</c> when the payload fails to parse as XML.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>DTD processing and external entity resolution are disabled, so a payload cannot cause the parser to fetch external resources or expand recursive entity definitions.</description></item>
    /// <item><description>Whitespace and comments are preserved via <see cref="LoadOptions.PreserveWhitespace"/>; text content inside <c>&lt;text&gt;</c> elements therefore survives.</description></item>
    /// <item><description>Parse failures are silent — the getter returns <c>null</c> and caches the null result so the parse is attempted at most once.</description></item>
    /// <item><description>Access is not thread-safe; concurrent first access may run the parse more than once.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Text"/>
    /// <seealso cref="XDocument"/>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.xml.linq.xdocument">.NET API: <c>XDocument</c></seealso>
    public XDocument? Document
    {
        get
        {
            if (_documentParsed) return _document;
            _documentParsed = true;

            try
            {
                using var reader = XmlReader.Create(
                    new StringReader(Text),
                    new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Prohibit,
                        XmlResolver = null,
                        IgnoreWhitespace = false,
                        IgnoreComments = false,
                        CloseInput = true,
                    });
                _document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
            }
            catch (XmlException)
            {
                _document = null;
            }

            return _document;
        }
    }

    /// <summary>Returns the decoded SVG text. Retained for source compatibility with callers that invoked the method form; delegates to <see cref="Text"/>.</summary>
    /// <returns>The decoded SVG document as a string, or <see cref="string.Empty"/> on decode failure.</returns>
    /// <remarks>Equivalent to reading <see cref="Text"/>; provided as a method for callers whose call sites read more naturally that way.</remarks>
    /// <seealso cref="Text"/>
    /// <seealso cref="DecodeText"/>
    public string GetDecodedText() => Text;

    /// <summary>Decodes the payload into an SVG text string, decompressing gzip payloads first.</summary>
    /// <returns>The decoded text, or <see cref="string.Empty"/> on any decode failure.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>For plain UTF-8 payloads, decodes the bytes directly with <see cref="Encoding.UTF8"/>.</description></item>
    /// <item><description>For gzip payloads, reads the <c>ISIZE</c> trailer to size the output buffer, rents from <see cref="ArrayPool{T}.Shared"/>, decompresses, and decodes as UTF-8.</description></item>
    /// <item><description>Any exception during decoding is caught and reported as <see cref="string.Empty"/>; the method never throws.</description></item>
    /// <item><description>Prefer <see cref="Text"/> over calling this directly: <see cref="Text"/> caches the result, while this method re-decodes on every call.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Text"/>
    /// <seealso cref="IsGzip"/>
    public string DecodeText()
    {
        try
        {
            ReadOnlySpan<byte> span = _encodedData;
            if (!IsGzip)
                return Encoding.UTF8.GetString(span);

            // Gzip trailer: last 4 bytes are ISIZE, the uncompressed size mod 2^32, LE.
            if (span.Length < 18)
                return string.Empty;

            uint isize = BinaryPrimitives.ReadUInt32LittleEndian(span[^4..]);
            if (isize > int.MaxValue)
                return string.Empty;

            int decompressedLength = (int)isize;
            if (decompressedLength == 0)
                return string.Empty;

            byte[] rented = ArrayPool<byte>.Shared.Rent(decompressedLength);
            try
            {
                if (!GZipDecoder.TryDecompress(span, rented, out int bytesWritten))
                    return string.Empty;

                return Encoding.UTF8.GetString(rented.AsSpan(0, bytesWritten));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
        catch
        {
            return string.Empty;
        }
    }
}
