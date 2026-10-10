using Mubarrat.Fonts.Binary;

namespace Mubarrat.Fonts.Tables;

/// <summary>A parsed glyph bitmap: metrics plus payload. Concrete subclasses correspond one-to-one with the EBDT/CBDT image formats.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The image format determines which fields are populated. Exactly one metrics source applies per format: <see cref="SmallMetrics"/> for formats 1, 2, 8, 17; <see cref="BigMetrics"/> for formats 6, 7, 9, 18; <see cref="MetricsInSubtable"/> for formats 5 and 19, where the metrics live in the enclosing EBLC/CBLC subtable rather than the bitmap record. The other two metrics properties are <c>null</c>.</description></item>
/// <item><description>The payload is similarly format-specific. Exactly one of <see cref="PixelData"/>, <see cref="Components"/>, or <see cref="PngData"/> is non-empty; the other two are the default empty list.</description></item>
/// <item><description>Dispatch is by the image format field of the enclosing index subtable, carried to the parser through <see cref="GlyphBitmapContext"/>. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyphBitmapFormat1"/>
/// <seealso cref="GlyphBitmapFormat2"/>
/// <seealso cref="GlyphBitmapFormat5"/>
/// <seealso cref="GlyphBitmapFormat6"/>
/// <seealso cref="GlyphBitmapFormat7"/>
/// <seealso cref="GlyphBitmapFormat8"/>
/// <seealso cref="GlyphBitmapFormat9"/>
/// <seealso cref="GlyphBitmapFormat17"/>
/// <seealso cref="GlyphBitmapFormat18"/>
/// <seealso cref="GlyphBitmapFormat19"/>
/// <seealso cref="GlyphBitmapContext"/>
/// <seealso cref="BigGlyphMetrics"/>
/// <seealso cref="SmallGlyphMetrics"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: CBDT table</seealso>
public abstract record GlyphBitmap : IRecord<GlyphBitmap>, IBaseRecord<GlyphBitmap>
{
    /// <summary>Gets the image format number that produced this bitmap.</summary>
    /// <value>The image format discriminant from the enclosing index subtable, ranging over the ten formats this hierarchy implements.</value>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphBitmapContext.ImageFormat"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
    public ushort ImageFormat { get; init; }

    /// <summary>Gets the big metrics, or <c>null</c> when small metrics or metrics-in-EBLC were used.</summary>
    /// <value>The <see cref="BigGlyphMetrics"/> for formats 6, 7, 9, and 18; <c>null</c> for all other formats.</value>
    /// <seealso cref="SmallMetrics"/>
    /// <seealso cref="MetricsInSubtable"/>
    /// <seealso cref="BigGlyphMetrics"/>
    public BigGlyphMetrics? BigMetrics { get; init; }

    /// <summary>Gets the small metrics, or <c>null</c> when big metrics or metrics-in-EBLC were used.</summary>
    /// <value>The <see cref="SmallGlyphMetrics"/> for formats 1, 2, 8, and 17; <c>null</c> for all other formats.</value>
    /// <seealso cref="BigMetrics"/>
    /// <seealso cref="MetricsInSubtable"/>
    /// <seealso cref="SmallGlyphMetrics"/>
    public SmallGlyphMetrics? SmallMetrics { get; init; }

    /// <summary>Gets the metrics supplied by the enclosing EBLC/CBLC index subtable, or <c>null</c> for formats that store metrics in the bitmap record itself.</summary>
    /// <value>The <see cref="BigGlyphMetrics"/> carried by the paired index subtable for formats 5 and 19; <c>null</c> for all other formats.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Bitmap format 5 is paired with an index subtable format 2; bitmap format 19 is paired with an index subtable format 5.</description></item>
    /// <item><description>The value is obtained through <see cref="GetMetricsFromSubtable(IndexSubtable)"/> during the enclosing strike's parse and threaded into the bitmap record via <see cref="GlyphBitmapContext.SubtableMetrics"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="BigMetrics"/>
    /// <seealso cref="SmallMetrics"/>
    /// <seealso cref="GetMetricsFromSubtable(IndexSubtable)"/>
    public BigGlyphMetrics? MetricsInSubtable { get; init; }

    /// <summary>Gets the raw pixel bytes for non-composite, non-PNG formats, or an empty list.</summary>
    /// <value>The packed bitmap bytes for formats 1, 2, 5, 6, and 7; an empty list for the composite and PNG formats.</value>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The byte count is derived from the record's declared length minus the metrics size: 5 bytes for small metrics (formats 1, 2), 8 bytes for big metrics (formats 6, 7), or the full record for format 5 where metrics are external.</description></item>
    /// <item><description>The packing is bit-aligned for formats 2, 5, and 7 and byte-aligned for formats 1 and 6.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Components"/>
    /// <seealso cref="PngData"/>
    public IReadOnlyList<byte> PixelData { get; init; } = [];

    /// <summary>Gets the composite components, or an empty list for non-composite formats.</summary>
    /// <value>The <see cref="EbdtComponent"/> array for formats 8 and 9; an empty list for all other formats.</value>
    /// <seealso cref="PixelData"/>
    /// <seealso cref="PngData"/>
    /// <seealso cref="IsComposite"/>
    /// <seealso cref="EbdtComponent"/>
    public IReadOnlyList<EbdtComponent> Components { get; init; } = [];

    /// <summary>Gets the embedded PNG data for EBDT format 17 or CBDT formats 17, 18, and 19, or an empty list for formats that do not carry PNG payload.</summary>
    /// <value>The raw PNG stream bytes for formats 17, 18, and 19; an empty list for all other formats.</value>
    /// <remarks>The data is the complete PNG file image, including its signature, IHDR, IDAT, and IEND chunks. It is not decoded by this library; the bytes are returned as-is for a downstream decoder.</remarks>
    /// <seealso cref="PixelData"/>
    /// <seealso cref="Components"/>
    /// <seealso cref="IsPng"/>
    public IReadOnlyList<byte> PngData { get; init; } = [];

    /// <summary>True when this glyph is a composite reference.</summary>
    /// <value><see langword="true"/> when <see cref="Components"/> is non-empty.</value>
    /// <remarks>Equivalent to <c><see cref="Components"/>.Count &gt; 0</c>. Provided as a named predicate for readability at call sites.</remarks>
    /// <seealso cref="Components"/>
    /// <seealso cref="IsPng"/>
    public bool IsComposite => Components.Count > 0;

    /// <summary>True when this bitmap carries PNG payload.</summary>
    /// <value><see langword="true"/> when <see cref="PngData"/> is non-empty.</value>
    /// <remarks>Equivalent to <c><see cref="PngData"/>.Count &gt; 0</c>. Provided as a named predicate for readability at call sites.</remarks>
    /// <seealso cref="PngData"/>
    /// <seealso cref="IsComposite"/>
    public bool IsPng => PngData.Count > 0;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the bitmap record.</param>
    /// <param name="context">A <see cref="GlyphBitmapContext"/> carrying the image format, the record's declared length, and the subtable metrics.</param>
    /// <returns>The format-specific bitmap record.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="context"/> is not a <see cref="GlyphBitmapContext"/>.</exception>
    /// <exception cref="InvalidDataException">The image format is not one of the ten defined formats.</exception>
    /// <remarks>The dispatcher reads only the context; it does not consume any cursor bytes itself. The format-specific parser consumes the record starting at the cursor's current position. See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphBitmapContext"/>
    /// <seealso cref="IBaseRecord{TBase}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
    static GlyphBitmap IRecord<GlyphBitmap>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not GlyphBitmapContext ctx)
            throw new InvalidOperationException(
                $"{nameof(GlyphBitmap)}.Parse requires a {nameof(GlyphBitmapContext)} context.");

        return ctx.ImageFormat switch
        {
            1 => IBaseRecord<GlyphBitmap>.Parse<GlyphBitmapFormat1>(ref cursor, ctx),
            2 => IBaseRecord<GlyphBitmap>.Parse<GlyphBitmapFormat2>(ref cursor, ctx),
            5 => IBaseRecord<GlyphBitmap>.Parse<GlyphBitmapFormat5>(ref cursor, ctx),
            6 => IBaseRecord<GlyphBitmap>.Parse<GlyphBitmapFormat6>(ref cursor, ctx),
            7 => IBaseRecord<GlyphBitmap>.Parse<GlyphBitmapFormat7>(ref cursor, ctx),
            8 => IBaseRecord<GlyphBitmap>.Parse<GlyphBitmapFormat8>(ref cursor, ctx),
            9 => IBaseRecord<GlyphBitmap>.Parse<GlyphBitmapFormat9>(ref cursor, ctx),
            17 => IBaseRecord<GlyphBitmap>.Parse<GlyphBitmapFormat17>(ref cursor, ctx),
            18 => IBaseRecord<GlyphBitmap>.Parse<GlyphBitmapFormat18>(ref cursor, ctx),
            19 => IBaseRecord<GlyphBitmap>.Parse<GlyphBitmapFormat19>(ref cursor, ctx),
            _ => throw new InvalidDataException(
                $"Bitmap image format {ctx.ImageFormat} is not defined."),
        };
    }

    /// <summary>Extracts the metrics stored in the enclosing index subtable, for formats 5 and 19. Returns <c>null</c> for any other subtable format.</summary>
    /// <param name="sub">The index subtable that locates this bitmap.</param>
    /// <returns>The <see cref="BigGlyphMetrics"/> carried by <paramref name="sub"/> when it is an <see cref="IndexSubtableFormat2"/> or <see cref="IndexSubtableFormat5"/>; <c>null</c> otherwise.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Bitmap format 5 uses metrics carried by an index subtable of format 2; bitmap format 19 uses metrics carried by an index subtable of format 5. The other index subtable formats carry no metrics, and bitmaps produced from them supply their own.</description></item>
    /// <item><description>The result is passed to the bitmap parser through <see cref="GlyphBitmapContext.SubtableMetrics"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="IndexSubtableFormat2.BigMetrics"/>
    /// <seealso cref="IndexSubtableFormat5.BigMetrics"/>
    /// <seealso cref="GlyphBitmapContext.SubtableMetrics"/>
    public static BigGlyphMetrics? GetMetricsFromSubtable(IndexSubtable sub) => sub switch
    {
        IndexSubtableFormat2 f2 => f2.BigMetrics,
        IndexSubtableFormat5 f5 => f5.BigMetrics,
        _ => null,
    };
}

/// <summary>Context supplied to <see cref="IRecord{T}.Parse"/>: the image format to dispatch on, the record's declared byte extent, and the metrics carried by the enclosing index subtable (used by formats 5 and 19).</summary>
/// <param name="ImageFormat">The image format from the enclosing index subtable.</param>
/// <param name="Length">The record's declared byte extent.</param>
/// <param name="SubtableMetrics">The metrics from the enclosing index subtable, or <c>null</c> when the subtable carries none. Only formats 5 and 19 read this value.</param>
/// <remarks>
/// <list type="bullet">
/// <item><description><see cref="Length"/> is used by formats 1, 2, 6, and 7 to derive the pixel-data byte count; formats that carry an explicit length field in the record itself (17, 18, 19) do not use it.</description></item>
/// <item><description>The context is a record type, so it is a reference type and passing it through <see cref="IRecord{T}.Parse"/> does not copy the fields.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="BigGlyphMetrics"/>
/// <seealso cref="IndexSubtable"/>
public record GlyphBitmapContext(
    ushort ImageFormat,
    int Length,
    BigGlyphMetrics? SubtableMetrics);

// ═══════════════════════════════════════════════════════════════════════════
// Format 1 — small metrics, byte-aligned data
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>EBDT/CBDT format 1: small metrics followed by byte-aligned bitmap data.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record begins with a 5-byte <see cref="SmallGlyphMetrics"/>, followed by <c>Length - 5</c> bytes of pixel data.</description></item>
/// <item><description>Pixel rows are byte-aligned; a row is padded to the next byte boundary if its bit width is not a multiple of 8.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="GlyphBitmapFormat2"/>
/// <seealso cref="GlyphBitmapFormat6"/>
/// <seealso cref="SmallGlyphMetrics"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
public sealed record GlyphBitmapFormat1
    : GlyphBitmap,
      IDerivedRecord<GlyphBitmap, GlyphBitmapFormat1>
{
    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the record.</param>
    /// <param name="context">A <see cref="GlyphBitmapContext"/> carrying the record's declared length.</param>
    /// <returns>The parsed format-1 bitmap.</returns>
    /// <exception cref="EndOfStreamException">The metrics or pixel data extend past the end of the table-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphBitmapContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
    static GlyphBitmapFormat1 IDerivedRecord<GlyphBitmap, GlyphBitmapFormat1>.Parse(
        ref Cursor cursor, object? context)
    {
        var ctx = (GlyphBitmapContext)context!;
        return new GlyphBitmapFormat1
        {
            ImageFormat = 1,
            SmallMetrics = cursor.ReadStruct<SmallGlyphMetrics>(),
            PixelData = cursor.ReadBytes(ctx.Length - 5),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 2 — small metrics, bit-aligned data
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>EBDT/CBDT format 2: small metrics followed by bit-aligned bitmap data.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record begins with a 5-byte <see cref="SmallGlyphMetrics"/>, followed by <c>Length - 5</c> bytes of pixel data.</description></item>
/// <item><description>Pixel rows are bit-packed without padding; rows may share bytes at their boundary. Use <see cref="SmallGlyphMetrics.Width"/> and <see cref="SmallGlyphMetrics.Height"/> to compute the bit count per row when decoding.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="GlyphBitmapFormat1"/>
/// <seealso cref="GlyphBitmapFormat7"/>
/// <seealso cref="SmallGlyphMetrics"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
public sealed record GlyphBitmapFormat2
    : GlyphBitmap,
      IDerivedRecord<GlyphBitmap, GlyphBitmapFormat2>
{
    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the record.</param>
    /// <param name="context">A <see cref="GlyphBitmapContext"/> carrying the record's declared length.</param>
    /// <returns>The parsed format-2 bitmap.</returns>
    /// <exception cref="EndOfStreamException">The metrics or pixel data extend past the end of the table-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphBitmapContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
    static GlyphBitmapFormat2 IDerivedRecord<GlyphBitmap, GlyphBitmapFormat2>.Parse(
        ref Cursor cursor, object? context)
    {
        var ctx = (GlyphBitmapContext)context!;
        return new GlyphBitmapFormat2
        {
            ImageFormat = 2,
            SmallMetrics = cursor.ReadStruct<SmallGlyphMetrics>(),
            PixelData = cursor.ReadBytes(ctx.Length - 5),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 5 — metrics in EBLC/CBLC, bit-aligned data only
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>EBDT/CBDT format 5: bitmap data only; metrics are supplied by the enclosing EBLC/CBLC index subtable.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record is entirely pixel data; <c>Length</c> bytes are consumed starting at the cursor's current position.</description></item>
/// <item><description>Metrics are read from the paired index subtable and exposed via <see cref="GlyphBitmap.MetricsInSubtable"/>; the bitmap record itself carries none.</description></item>
/// <item><description>Pixel rows are bit-packed, matching format 2's layout. The dimensions needed for decoding come from <see cref="GlyphBitmap.MetricsInSubtable"/>.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="GlyphBitmapFormat2"/>
/// <seealso cref="GlyphBitmapFormat19"/>
/// <seealso cref="IndexSubtableFormat2"/>
/// <seealso cref="BigGlyphMetrics"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
public sealed record GlyphBitmapFormat5
    : GlyphBitmap,
      IDerivedRecord<GlyphBitmap, GlyphBitmapFormat5>
{
    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the record.</param>
    /// <param name="context">A <see cref="GlyphBitmapContext"/> carrying the record's declared length and the metrics from the enclosing index subtable.</param>
    /// <returns>The parsed format-5 bitmap.</returns>
    /// <exception cref="EndOfStreamException">The pixel data extends past the end of the table-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphBitmapContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
    static GlyphBitmapFormat5 IDerivedRecord<GlyphBitmap, GlyphBitmapFormat5>.Parse(
        ref Cursor cursor, object? context)
    {
        var ctx = (GlyphBitmapContext)context!;
        return new GlyphBitmapFormat5
        {
            ImageFormat = 5,
            MetricsInSubtable = ctx.SubtableMetrics,
            PixelData = cursor.ReadBytes(ctx.Length),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 6 — big metrics, byte-aligned data
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>EBDT/CBDT format 6: big metrics followed by byte-aligned bitmap data.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record begins with an 8-byte <see cref="BigGlyphMetrics"/>, followed by <c>Length - 8</c> bytes of pixel data.</description></item>
/// <item><description>Pixel rows are byte-aligned; a row is padded to the next byte boundary if its bit width is not a multiple of 8.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="GlyphBitmapFormat1"/>
/// <seealso cref="GlyphBitmapFormat7"/>
/// <seealso cref="BigGlyphMetrics"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
public sealed record GlyphBitmapFormat6
    : GlyphBitmap,
      IDerivedRecord<GlyphBitmap, GlyphBitmapFormat6>
{
    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the record.</param>
    /// <param name="context">A <see cref="GlyphBitmapContext"/> carrying the record's declared length.</param>
    /// <returns>The parsed format-6 bitmap.</returns>
    /// <exception cref="EndOfStreamException">The metrics or pixel data extend past the end of the table-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphBitmapContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
    static GlyphBitmapFormat6 IDerivedRecord<GlyphBitmap, GlyphBitmapFormat6>.Parse(
        ref Cursor cursor, object? context)
    {
        var ctx = (GlyphBitmapContext)context!;
        return new GlyphBitmapFormat6
        {
            ImageFormat = 6,
            BigMetrics = cursor.ReadStruct<BigGlyphMetrics>(),
            PixelData = cursor.ReadBytes(ctx.Length - 8),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 7 — big metrics, bit-aligned data
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>EBDT/CBDT format 7: big metrics followed by bit-aligned bitmap data.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record begins with an 8-byte <see cref="BigGlyphMetrics"/>, followed by <c>Length - 8</c> bytes of pixel data.</description></item>
/// <item><description>Pixel rows are bit-packed without padding, matching format 2's layout but with the larger metrics header.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="GlyphBitmapFormat2"/>
/// <seealso cref="GlyphBitmapFormat6"/>
/// <seealso cref="BigGlyphMetrics"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
public sealed record GlyphBitmapFormat7
    : GlyphBitmap,
      IDerivedRecord<GlyphBitmap, GlyphBitmapFormat7>
{
    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the record.</param>
    /// <param name="context">A <see cref="GlyphBitmapContext"/> carrying the record's declared length.</param>
    /// <returns>The parsed format-7 bitmap.</returns>
    /// <exception cref="EndOfStreamException">The metrics or pixel data extend past the end of the table-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphBitmapContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
    static GlyphBitmapFormat7 IDerivedRecord<GlyphBitmap, GlyphBitmapFormat7>.Parse(
        ref Cursor cursor, object? context)
    {
        var ctx = (GlyphBitmapContext)context!;
        return new GlyphBitmapFormat7
        {
            ImageFormat = 7,
            BigMetrics = cursor.ReadStruct<BigGlyphMetrics>(),
            PixelData = cursor.ReadBytes(ctx.Length - 8),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 8 — small metrics, composite
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>EBDT/CBDT format 8: small metrics followed by composite components.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record begins with a 5-byte <see cref="SmallGlyphMetrics"/>, then a single padding byte, then a <c>uint16</c> component count, then that many <see cref="EbdtComponent"/> entries.</description></item>
/// <item><description>The padding byte aligns <c>numComponents</c> to a 16-bit boundary; its value is not meaningful and is discarded.</description></item>
/// <item><description>Components are drawn in array order; later components overlap earlier ones.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="GlyphBitmapFormat9"/>
/// <seealso cref="EbdtComponent"/>
/// <seealso cref="SmallGlyphMetrics"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
public sealed record GlyphBitmapFormat8
    : GlyphBitmap,
      IDerivedRecord<GlyphBitmap, GlyphBitmapFormat8>
{
    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the record.</param>
    /// <param name="context">A <see cref="GlyphBitmapContext"/>. The format-8 layout is entirely self-describing, so the context's length and metrics fields are not read.</param>
    /// <returns>The parsed format-8 composite bitmap.</returns>
    /// <exception cref="EndOfStreamException">The metrics or component array extend past the end of the table-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphBitmapContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
    static GlyphBitmapFormat8 IDerivedRecord<GlyphBitmap, GlyphBitmapFormat8>.Parse(
        ref Cursor cursor, object? context)
    {
        SmallGlyphMetrics metrics = cursor.ReadStruct<SmallGlyphMetrics>();
        cursor.ReadUInt8();   // pad: aligns numComponents to a 16-bit boundary
        ushort numComponents = cursor.ReadUInt16();
        return new GlyphBitmapFormat8
        {
            ImageFormat = 8,
            SmallMetrics = metrics,
            Components = cursor.ReadBigEndianStructArray<EbdtComponent>(numComponents),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 9 — big metrics, composite
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>EBDT/CBDT format 9: big metrics followed by composite components.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record begins with an 8-byte <see cref="BigGlyphMetrics"/>, then a <c>uint16</c> component count, then that many <see cref="EbdtComponent"/> entries.</description></item>
/// <item><description>Unlike format 8, no padding byte precedes the count; the larger metrics header leaves the count naturally 16-bit aligned.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="GlyphBitmapFormat8"/>
/// <seealso cref="EbdtComponent"/>
/// <seealso cref="BigGlyphMetrics"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
public sealed record GlyphBitmapFormat9
    : GlyphBitmap,
      IDerivedRecord<GlyphBitmap, GlyphBitmapFormat9>
{
    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the record.</param>
    /// <param name="context">A <see cref="GlyphBitmapContext"/>. The format-9 layout is entirely self-describing, so the context's length and metrics fields are not read.</param>
    /// <returns>The parsed format-9 composite bitmap.</returns>
    /// <exception cref="EndOfStreamException">The metrics or component array extend past the end of the table-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">EBDT subtable formats</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphBitmapContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/ebdt#ebdtsubtable-formats">OpenType specification: EBDT subtable formats</seealso>
    static GlyphBitmapFormat9 IDerivedRecord<GlyphBitmap, GlyphBitmapFormat9>.Parse(
        ref Cursor cursor, object? context)
    {
        BigGlyphMetrics metrics = cursor.ReadStruct<BigGlyphMetrics>();
        ushort numComponents = cursor.ReadUInt16();
        return new GlyphBitmapFormat9
        {
            ImageFormat = 9,
            BigMetrics = metrics,
            Components = cursor.ReadBigEndianStructArray<EbdtComponent>(numComponents),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 17 — small metrics, PNG image data
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>CBDT format 17: small metrics followed by embedded PNG image data.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record begins with a 5-byte <see cref="SmallGlyphMetrics"/>, then a <c>uint32</c> data length, then that many bytes of PNG data.</description></item>
/// <item><description>The PNG bytes are the complete file image, including signature and IEND; the library does not decode them.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">CBDT table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="GlyphBitmapFormat18"/>
/// <seealso cref="GlyphBitmapFormat19"/>
/// <seealso cref="SmallGlyphMetrics"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: CBDT table</seealso>
public sealed record GlyphBitmapFormat17
    : GlyphBitmap,
      IDerivedRecord<GlyphBitmap, GlyphBitmapFormat17>
{
    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the record.</param>
    /// <param name="context">A <see cref="GlyphBitmapContext"/>. The PNG length is stored in the record itself, so the context's length field is not read.</param>
    /// <returns>The parsed format-17 bitmap with its PNG bytes in <see cref="GlyphBitmap.PngData"/>.</returns>
    /// <exception cref="EndOfStreamException">The metrics or PNG data extend past the end of the table-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">CBDT table</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphBitmapContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: CBDT table</seealso>
    static GlyphBitmapFormat17 IDerivedRecord<GlyphBitmap, GlyphBitmapFormat17>.Parse(
        ref Cursor cursor, object? context)
    {
        SmallGlyphMetrics metrics = cursor.ReadStruct<SmallGlyphMetrics>();
        uint dataLen = cursor.ReadUInt32();
        return new GlyphBitmapFormat17
        {
            ImageFormat = 17,
            SmallMetrics = metrics,
            PngData = cursor.ReadBytes(checked((int)dataLen)),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 18 — big metrics, PNG image data
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>CBDT format 18: big metrics followed by embedded PNG image data.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record begins with an 8-byte <see cref="BigGlyphMetrics"/>, then a <c>uint32</c> data length, then that many bytes of PNG data.</description></item>
/// <item><description>The PNG bytes are the complete file image, including signature and IEND; the library does not decode them.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">CBDT table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="GlyphBitmapFormat17"/>
/// <seealso cref="GlyphBitmapFormat19"/>
/// <seealso cref="BigGlyphMetrics"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: CBDT table</seealso>
public sealed record GlyphBitmapFormat18
    : GlyphBitmap,
      IDerivedRecord<GlyphBitmap, GlyphBitmapFormat18>
{
    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the record.</param>
    /// <param name="context">A <see cref="GlyphBitmapContext"/>. The PNG length is stored in the record itself, so the context's length field is not read.</param>
    /// <returns>The parsed format-18 bitmap with its PNG bytes in <see cref="GlyphBitmap.PngData"/>.</returns>
    /// <exception cref="EndOfStreamException">The metrics or PNG data extend past the end of the table-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">CBDT table</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphBitmapContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: CBDT table</seealso>
    static GlyphBitmapFormat18 IDerivedRecord<GlyphBitmap, GlyphBitmapFormat18>.Parse(
        ref Cursor cursor, object? context)
    {
        BigGlyphMetrics metrics = cursor.ReadStruct<BigGlyphMetrics>();
        uint dataLen = cursor.ReadUInt32();
        return new GlyphBitmapFormat18
        {
            ImageFormat = 18,
            BigMetrics = metrics,
            PngData = cursor.ReadBytes(checked((int)dataLen)),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Format 19 — metrics in CBLC, PNG image data
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>CBDT format 19: embedded PNG image data; metrics are supplied by the enclosing CBLC index subtable.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record begins with a <c>uint32</c> data length, then that many bytes of PNG data; no metrics header is present.</description></item>
/// <item><description>Metrics are read from the paired index subtable (typically <see cref="IndexSubtableFormat5"/>) and exposed via <see cref="GlyphBitmap.MetricsInSubtable"/>.</description></item>
/// <item><description>Declared as a <c>sealed record</c>, so the compiler-generated <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">CBDT table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GlyphBitmap"/>
/// <seealso cref="GlyphBitmapFormat5"/>
/// <seealso cref="GlyphBitmapFormat17"/>
/// <seealso cref="GlyphBitmapFormat18"/>
/// <seealso cref="IndexSubtableFormat5"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: CBDT table</seealso>
public sealed record GlyphBitmapFormat19
    : GlyphBitmap,
      IDerivedRecord<GlyphBitmap, GlyphBitmapFormat19>
{
    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the record.</param>
    /// <param name="context">A <see cref="GlyphBitmapContext"/> carrying the subtable metrics. The PNG length is stored in the record itself, so the context's length field is not read.</param>
    /// <returns>The parsed format-19 bitmap with its PNG bytes in <see cref="GlyphBitmap.PngData"/> and metrics in <see cref="GlyphBitmap.MetricsInSubtable"/>.</returns>
    /// <exception cref="EndOfStreamException">The PNG data extends past the end of the table-scoped source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">CBDT table</see> in the OpenType specification.</remarks>
    /// <seealso cref="GlyphBitmapContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/cbdt">OpenType specification: CBDT table</seealso>
    static GlyphBitmapFormat19 IDerivedRecord<GlyphBitmap, GlyphBitmapFormat19>.Parse(
        ref Cursor cursor, object? context)
    {
        var ctx = (GlyphBitmapContext)context!;
        uint dataLen = cursor.ReadUInt32();
        return new GlyphBitmapFormat19
        {
            ImageFormat = 19,
            MetricsInSubtable = ctx.SubtableMetrics,
            PngData = cursor.ReadBytes(checked((int)dataLen)),
        };
    }
}
