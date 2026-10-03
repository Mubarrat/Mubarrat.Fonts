using Mubarrat.Fonts.OpenType.Binary;
using Mubarrat.Fonts.OpenType.Primitives;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Mubarrat.Fonts.OpenType.Tables.Layout;

/// <summary>Enumerates the layout features a GSUB or GPOS table provides.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The feature list is a sorted array of <see cref="FeatureRecord"/> entries. Each record pairs a feature tag (e.g. <c>"liga"</c>, <c>"kern"</c>) with a <see cref="Feature"/> that names the lookups implementing it.</description></item>
/// <item><description>Records are sorted ascending by <see cref="FeatureRecord.Tag"/>, matching the tag-sorted convention used across the OpenType layout tables.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-list-table">Feature list table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatureRecord"/>
/// <seealso cref="Feature"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-list-table">OpenType specification: Feature list table</seealso>
public sealed record FeatureList : IRecord<FeatureList>
{
    /// <summary>Gets the feature records, sorted by tag.</summary>
    /// <value>The ordered list of <see cref="FeatureRecord"/> entries. Records are sorted ascending by <see cref="FeatureRecord.Tag"/> so a binary search can locate a feature.</value>
    /// <seealso cref="GetFeature(int)"/>
    /// <seealso cref="FeatureRecord"/>
    public IReadOnlyList<FeatureRecord> Features { get; init; } = [];

    /// <summary>Returns the Feature at an index, or <c>null</c> when the index is out of range.</summary>
    /// <param name="index">The zero-based index into <see cref="Features"/>.</param>
    /// <returns>The <see cref="Feature"/> at <paramref name="index"/>, or <c>null</c> when the index is out of range.</returns>
    /// <remarks>The lookup is constant-time; the index directly selects an entry from the sorted list.</remarks>
    /// <seealso cref="Features"/>
    /// <seealso cref="Feature"/>
    public Feature? GetFeature(int index) =>
        (uint)index < (uint)Features.Count ? Features[index].Feature : null;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the feature list.</param>
    /// <param name="context">Unused. The list resolves its records through its own source.</param>
    /// <returns>The parsed feature list with its records resolved.</returns>
    /// <exception cref="EndOfStreamException">The count, record array, or any referenced feature extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-list-table">Feature list table</see> in the OpenType specification.</remarks>
    /// <seealso cref="FeatureRecord"/>
    /// <seealso cref="TagOffsetRecord"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-list-table">OpenType specification: Feature list table</seealso>
    static FeatureList IRecord<FeatureList>.Parse(ref Cursor cursor, object? context)
    {
        int count = cursor.ReadUInt16();
        return new FeatureList
        {
            Features = cursor.ReadBigEndianHeaderRecordArray<FeatureRecord, TagOffsetRecord>(count, new ParentContext(cursor.Source)),
        };
    }
}

/// <summary>A feature entry in a <see cref="FeatureList"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The record pairs a feature tag with the <see cref="Feature"/> that names its lookups. The tag is also passed through to the feature's parse as a <see cref="TagContext"/>, so the feature can select its params layout.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-list-table">Feature list table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatureList"/>
/// <seealso cref="Feature"/>
/// <seealso cref="TagContext"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-list-table">OpenType specification: Feature list table</seealso>
public sealed record FeatureRecord : IBigEndianHeaderRecord<FeatureRecord, TagOffsetRecord>
{
    /// <summary>Gets the feature tag.</summary>
    /// <value>The four-character feature identifier, e.g. <c>"liga"</c>, <c>"kern"</c>, <c>"ss01"</c>. Also passed through to the feature's parse as the tag-context discriminator.</value>
    /// <seealso cref="Feature"/>
    /// <seealso cref="FeatureParams"/>
    public Tag Tag { get; init; }

    /// <summary>Gets the parsed Feature.</summary>
    /// <value>The <see cref="Feature"/> resolved from the record's offset.</value>
    /// <seealso cref="Tag"/>
    /// <seealso cref="Feature"/>
    public Feature Feature { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="header">The already-read <see cref="TagOffsetRecord"/> header.</param>
    /// <param name="context">A <see cref="ParentContext"/> whose <see cref="IParentContext.ParentSource"/> is the list-scoped source.</param>
    /// <returns>A new record with its feature resolved.</returns>
    /// <remarks>The <c>offset</c> is measured from the start of the enclosing <see cref="FeatureList"/>; the parent context carries the list-scoped source so the offset resolves correctly. The tag is passed through as a <see cref="TagContext"/> so <see cref="IRecord{T}.Parse"/> can select the right <see cref="FeatureParams"/> layout.</remarks>
    /// <seealso cref="TagOffsetRecord"/>
    /// <seealso cref="Feature"/>
    /// <seealso cref="TagContext"/>
    static FeatureRecord IHeaderRecord<FeatureRecord, TagOffsetRecord>.FromHeader(in TagOffsetRecord header, object? context) => new()
    {
        Tag = header.Tag,
        Feature = ((ParentContext)context!).ParentSource.ParseRecordAt<Feature>(header.Offset, new TagContext(header.Tag)),
    };
}

/// <summary>A feature: the set of lookups implementing one feature tag, plus optional feature-specific parameters.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <see cref="LookupIndices"/> array names the lookups that collectively implement the feature. Each index selects an entry in the enclosing GSUB or GPOS LookupList.</description></item>
/// <item><description>Some features carry additional parameters whose layout is determined by the feature tag. Only <c>size</c>, <c>ss01</c>–<c>ss20</c>, and <c>cv01</c>–<c>cv99</c> have defined params; other tags ignore the params offset even when non-zero.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-table">Feature table</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatureList"/>
/// <seealso cref="FeatureRecord"/>
/// <seealso cref="FeatureParams"/>
/// <seealso cref="HasKnownParams(Tag)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-table">OpenType specification: Feature table</seealso>
public sealed record Feature : IRecord<Feature>
{
    /// <summary>Gets the parsed feature params, or <c>null</c>.</summary>
    /// <value>The <see cref="FeatureParams"/> block for features that carry one, or <c>null</c> when the params offset is zero or the feature tag has no defined params layout.</value>
    /// <seealso cref="HasKnownParams(Tag)"/>
    /// <seealso cref="FeatureParams"/>
    public FeatureParams? FeatureParams { get; init; }

    /// <summary>Gets the indices into the enclosing LookupList for the lookups this feature applies.</summary>
    /// <value>The ordered list of lookup indices. Each index selects an entry in the LookupList of the table that owns this feature.</value>
    /// <seealso cref="FeatureParams"/>
    public IReadOnlyList<ushort> LookupIndices { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the feature record body (after the offset is resolved).</param>
    /// <param name="context">A <see cref="TagContext"/> whose <see cref="TagContext.Tag"/> identifies the feature.</param>
    /// <returns>The parsed feature.</returns>
    /// <exception cref="InvalidDataException"><paramref name="context"/> is not a <see cref="TagContext"/>.</exception>
    /// <exception cref="EndOfStreamException">The params offset, lookup-index array, or referenced params block extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The params block is resolved only when both the offset is non-zero and the feature tag is one that carries params. A non-zero offset on an unrecognised tag is silently ignored rather than throwing.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-table">Feature table</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="TagContext"/>
    /// <seealso cref="HasKnownParams(Tag)"/>
    /// <seealso cref="FeatureParams"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-table">OpenType specification: Feature table</seealso>
    static Feature IRecord<Feature>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not TagContext tagContext)
            throw new InvalidDataException("Feature.Parse requires a TagContext; the tag identifies the layout.");

        ushort paramsOffset = cursor.ReadOffset16();
        ushort[] indices = cursor.ReadUInt16Array(cursor.ReadUInt16());

        FeatureParams? featureParams = null;
        if (paramsOffset != 0 && HasKnownParams(tagContext.Tag))
            featureParams = cursor.Source.ParseRecordAt<FeatureParams>(paramsOffset, tagContext);

        return new Feature { FeatureParams = featureParams, LookupIndices = indices };
    }

    /// <summary>Returns <see langword="true"/> when the given feature tag has a defined params layout.</summary>
    /// <param name="tag">The feature tag to test.</param>
    /// <returns><see langword="true"/> when <paramref name="tag"/> is <c>"size"</c>, a stylistic set (<c>ss01</c>–<c>ss20</c>), or a character variant (<c>cv01</c>–<c>cv99</c>); otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Used by <see cref="IRecord{T}.Parse"/> to decide whether to resolve the feature-params block when the params offset is non-zero.</description></item>
    /// <item><description>Tags without a defined params layout have their params offset ignored even when set. This tolerates fonts that store unrelated data at that offset.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatureParams.IsStylisticSet(Tag)"/>
    /// <seealso cref="FeatureParams.IsCharacterVariant(Tag)"/>
    /// <seealso cref="FeatureParams"/>
    public static bool HasKnownParams(Tag tag) => FeatureParams.IsStylisticSet(tag) || FeatureParams.IsCharacterVariant(tag) || tag == "size";
}

// ═══════════════════════════════════════════════════════════════════════════
// Feature params
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Base type for a feature-params block. The concrete layout is chosen by the enclosing feature's tag, which is supplied through <see cref="TagContext"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Three params layouts exist: <c>size</c> (a 10-byte block for optical-size features), stylistic sets (<c>ss01</c>–<c>ss20</c>, a 4-byte block with a UI label name ID), and character variants (<c>cv01</c>–<c>cv99</c>, a 14-byte prefix plus a uint24 array of Unicode scalar values).</description></item>
/// <item><description>The dispatch is by tag rather than by a format discriminant, so <see cref="TagContext"/> must be supplied to <see cref="IRecord{T}.Parse"/>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-params-table">FeatureParams tables</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatureParamsSize"/>
/// <seealso cref="FeatureParamsStylisticSet"/>
/// <seealso cref="FeatureParamsCharacterVariants"/>
/// <seealso cref="TagContext"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-params-table">OpenType specification: FeatureParams tables</seealso>
public abstract record FeatureParams
    : IRecord<FeatureParams>, IBaseRecord<FeatureParams>
{
    /// <summary>Parses the feature-params block for the tag supplied in <paramref name="context"/>.</summary>
    /// <param name="cursor">Cursor positioned at the params block's first byte.</param>
    /// <param name="context">A <see cref="TagContext"/> whose <see cref="TagContext.Tag"/> identifies the feature. Any other context shape, including <c>null</c>, is rejected.</param>
    /// <returns>The decoded params block.</returns>
    /// <exception cref="InvalidDataException">The context is not a <see cref="TagContext"/>, or the tag is not one that carries feature parameters.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The dispatch is by tag: <c>size</c> → <see cref="FeatureParamsSize"/>, stylistic set tags → <see cref="FeatureParamsStylisticSet"/>, character variant tags → <see cref="FeatureParamsCharacterVariants"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-params-table">FeatureParams tables</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatureParamsSize"/>
    /// <seealso cref="FeatureParamsStylisticSet"/>
    /// <seealso cref="FeatureParamsCharacterVariants"/>
    /// <seealso cref="TagContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#feature-params-table">OpenType specification: FeatureParams tables</seealso>
    static FeatureParams IRecord<FeatureParams>.Parse(ref Cursor cursor, object? context)
    {
        if (context is not TagContext tagContext)
            throw new InvalidDataException(
                "FeatureParams.Parse requires a TagContext; the tag identifies the layout.");

        Tag tag = tagContext.Tag;

        if (IsStylisticSet(tag))
            return IBaseRecord<FeatureParams>.Parse<FeatureParamsStylisticSet>(ref cursor, context);
        if (IsCharacterVariant(tag))
            return IBaseRecord<FeatureParams>.Parse<FeatureParamsCharacterVariants>(ref cursor, context);
        if (tag == "size")
            return IBaseRecord<FeatureParams>.Parse<FeatureParamsSize>(ref cursor, context);

        throw new InvalidDataException($"Feature params tag '{tag}' is not recognized.");
    }

    /// <summary>True when <paramref name="tag"/> has the form <c>ssNN</c>.</summary>
    /// <param name="tag">The tag to test.</param>
    /// <returns><see langword="true"/> when the tag is <c>"ss"</c> followed by two ASCII digits (e.g. <c>"ss01"</c>, <c>"ss20"</c>); otherwise <see langword="false"/>.</returns>
    /// <remarks>The check uses a C# list pattern over the tag's indexer, testing each character position directly without allocating a string.</remarks>
    /// <seealso cref="IsCharacterVariant(Tag)"/>
    /// <seealso cref="Tag.this[int]"/>
    public static bool IsStylisticSet(Tag tag) => tag is ['s', 's', >= '0' and <= '9', >= '0' and <= '9'];

    /// <summary>True when <paramref name="tag"/> has the form <c>cvNN</c>.</summary>
    /// <param name="tag">The tag to test.</param>
    /// <returns><see langword="true"/> when the tag is <c>"cv"</c> followed by two ASCII digits (e.g. <c>"cv01"</c>, <c>"cv99"</c>); otherwise <see langword="false"/>.</returns>
    /// <remarks>The check uses a C# list pattern over the tag's indexer, testing each character position directly without allocating a string.</remarks>
    /// <seealso cref="IsStylisticSet(Tag)"/>
    /// <seealso cref="Tag.this[int]"/>
    public static bool IsCharacterVariant(Tag tag) => tag is ['c', 'v', >= '0' and <= '9', >= '0' and <= '9'];
}

/// <summary>Feature parameters for the <c>size</c> feature. All size values are in decipoints (720/inch units).</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The <c>size</c> feature declares an optical-size range for a font, optionally subdividing a family into subfamilies by design size.</description></item>
/// <item><description>All values are in decipoints: one point is 1/72 inch, so one decipoint is 1/720 inch. A design size of <c>1200</c> means 120 decipoints, or 12 points.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/features_pt#size">size feature</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatureParams"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/features_pt#size">OpenType specification: size feature</seealso>
public sealed record FeatureParamsSize : FeatureParams, IBigEndianHeaderDerivedRecord<FeatureParams, FeatureParamsSize, FeatureParamsSize.Header>
{
    /// <summary>Design size in decipoints. Non-zero.</summary>
    /// <value>The design size of the font in decipoints; a value of <c>1200</c> means 12 points.</value>
    /// <seealso cref="SubfamilyIdentifier"/>
    /// <seealso cref="RangeStart"/>
    /// <seealso cref="RangeEnd"/>
    public ushort DesignSize { get; init; }

    /// <summary>Identifier that associates fonts in a subfamily. Zero when not used.</summary>
    /// <value>A nonzero identifier shared by every font in the same optical subfamily, or <c>0</c> when the font is not part of a subdivided subfamily.</value>
    /// <seealso cref="DesignSize"/>
    /// <seealso cref="SubfamilyNameID"/>
    public ushort SubfamilyIdentifier { get; init; }

    /// <summary>Name table name ID that specifies the subfamily the font belongs to. Must be in the font-specific range (256–32767) when <see cref="SubfamilyIdentifier"/> is non-zero.</summary>
    /// <value>The name ID resolving to a human-readable subfamily label, or <c>0</c> when <see cref="SubfamilyIdentifier"/> is zero.</value>
    /// <seealso cref="SubfamilyIdentifier"/>
    public ushort SubfamilyNameID { get; init; }

    /// <summary>Large end of the recommended usage range (exclusive), in decipoints. Zero when no range is specified.</summary>
    /// <value>The exclusive upper bound of the size range over which the font should be selected, in decipoints. Zero when the range is unbounded above.</value>
    /// <seealso cref="RangeEnd"/>
    /// <seealso cref="DesignSize"/>
    public ushort RangeStart { get; init; }

    /// <summary>Small end of the recommended usage range (inclusive), in decipoints. Zero when no range is specified.</summary>
    /// <value>The inclusive lower bound of the size range over which the font should be selected, in decipoints. Zero when the range is unbounded below.</value>
    /// <seealso cref="RangeStart"/>
    /// <seealso cref="DesignSize"/>
    public ushort RangeEnd { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read header.</param>
    /// <param name="context">Unused.</param>
    /// <returns>A new instance populated from the header.</returns>
    static FeatureParamsSize IHeaderDerivedRecord<FeatureParams, FeatureParamsSize, Header>.FromHeader(in Header header, object? context) => new()
    {
        DesignSize = header.DesignSize,
        SubfamilyIdentifier = header.SubfamilyIdentifier,
        SubfamilyNameID = header.SubfamilyNameID,
        RangeStart = header.RangeStart,
        RangeEnd = header.RangeEnd,
    };

    /// <summary>The 10-byte <c>size</c> params block. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>All five fields are <c>uint16</c> values measured in decipoints, or zero when the corresponding field is unspecified.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/features_pt#size">size feature</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatureParamsSize"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/features_pt#size">OpenType specification: size feature</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the design size in decipoints.</summary>
        /// <value>The design size of the font in decipoints; a value of <c>1200</c> means 12 points.</value>
        /// <seealso cref="SubfamilyIdentifier"/>
        public ushort DesignSize;          // +0

        /// <summary>Gets the subfamily identifier.</summary>
        /// <value>A nonzero identifier shared by every font in the same optical subfamily, or <c>0</c> when the font is not part of a subdivided subfamily.</value>
        /// <seealso cref="DesignSize"/>
        /// <seealso cref="SubfamilyNameID"/>
        public ushort SubfamilyIdentifier; // +2

        /// <summary>Gets the name table name ID for the subfamily label.</summary>
        /// <value>The name ID resolving to a human-readable subfamily label, or <c>0</c> when <see cref="SubfamilyIdentifier"/> is zero.</value>
        /// <seealso cref="SubfamilyIdentifier"/>
        public ushort SubfamilyNameID;     // +4

        /// <summary>Gets the exclusive upper bound of the recommended usage range, in decipoints.</summary>
        /// <value>The exclusive upper bound of the size range over which the font should be selected, or <c>0</c> when the range is unbounded above.</value>
        /// <seealso cref="RangeEnd"/>
        public ushort RangeStart;          // +6

        /// <summary>Gets the inclusive lower bound of the recommended usage range, in decipoints.</summary>
        /// <value>The inclusive lower bound of the size range over which the font should be selected, or <c>0</c> when the range is unbounded below.</value>
        /// <seealso cref="RangeStart"/>
        public ushort RangeEnd;            // +8

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All five fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            DesignSize = BinaryPrimitives.ReverseEndianness(v.DesignSize),
            SubfamilyIdentifier = BinaryPrimitives.ReverseEndianness(v.SubfamilyIdentifier),
            SubfamilyNameID = BinaryPrimitives.ReverseEndianness(v.SubfamilyNameID),
            RangeStart = BinaryPrimitives.ReverseEndianness(v.RangeStart),
            RangeEnd = BinaryPrimitives.ReverseEndianness(v.RangeEnd),
        };
    }
}

/// <summary>Feature parameters for a stylistic-set feature (<c>ss01</c>–<c>ss20</c>). Additional data may be appended to the end of the table in the future; consumers should only rely on the first four bytes.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Stylistic sets provide alternate glyph substitutions that a user can enable by name; this params block supplies the label shown in a font menu.</description></item>
/// <item><description>The table is defined to be forward-compatible: later versions may append fields, so consumers must not assume the block ends after <see cref="UiNameID"/>.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/features_pt#ss">stylistic set feature</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatureParams"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/features_pt#ss">OpenType specification: stylistic set feature</seealso>
public sealed record FeatureParamsStylisticSet : FeatureParams, IBigEndianHeaderDerivedRecord<FeatureParams, FeatureParamsStylisticSet, FeatureParamsStylisticSet.Header>
{
    /// <summary>Minor version number. Set to 0.</summary>
    /// <value>The constant <c>0</c> for the current version of the stylistic-set params block.</value>
    /// <seealso cref="UiNameID"/>
    public ushort Version { get; init; }

    /// <summary>Name table name ID that specifies a user-interface label for this feature. Values are expected to be in the font-specific range (256–32767).</summary>
    /// <value>The name ID resolving to a human-readable label for the stylistic set.</value>
    /// <seealso cref="Version"/>
    public ushort UiNameID { get; init; }

    /// <inheritdoc/>
    /// <param name="header">The already-read header.</param>
    /// <param name="context">Unused.</param>
    /// <returns>A new instance populated from the header.</returns>
    static FeatureParamsStylisticSet IHeaderDerivedRecord<FeatureParams, FeatureParamsStylisticSet, Header>.FromHeader(in Header header, object? context) => new()
    {
        Version = header.Version,
        UiNameID = header.UiNameID,
    };

    /// <summary>The 4-byte stylistic-set params block. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Additional fields may be appended in future versions; the parser reads only the first four bytes and ignores any trailing data.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/features_pt#ss">stylistic set feature</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatureParamsStylisticSet"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/features_pt#ss">OpenType specification: stylistic set feature</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>Gets the minor version number.</summary>
        /// <value>The constant <c>0</c> for the current version.</value>
        /// <seealso cref="UiNameID"/>
        public ushort Version;   // +0

        /// <summary>Gets the name table name ID for the user-interface label.</summary>
        /// <value>The name ID resolving to a human-readable label for the stylistic set.</value>
        /// <seealso cref="Version"/>
        public ushort UiNameID;  // +2

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Version = BinaryPrimitives.ReverseEndianness(v.Version),
            UiNameID = BinaryPrimitives.ReverseEndianness(v.UiNameID),
        };
    }
}

/// <summary>Feature parameters for a character-variant feature (<c>cv01</c>–<c>cv99</c>).</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Character variants provide per-character substitutions where the user can select from a set of named variants for a character.</description></item>
/// <item><description>The fixed prefix is 14 bytes; the trailing <see cref="Characters"/> array is a sequence of uint24 Unicode scalar values. Total length is <c>14 + 3 * CharacterCount</c> bytes.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/features_pt#cv">character variant feature</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="FeatureParams"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/features_pt#cv">OpenType specification: character variant feature</seealso>
public sealed record FeatureParamsCharacterVariants : FeatureParams, IDerivedRecord<FeatureParams, FeatureParamsCharacterVariants>
{
    /// <summary>Gets the format of the feature-parameters block.</summary>
    /// <value>The constant <c>1</c> for the current version.</value>
    /// <seealso cref="Header.Format"/>
    public ushort Format { get; init; }

    /// <summary>Gets the name table name ID of the user-interface label for the feature.</summary>
    /// <value>The name ID resolving to a human-readable label for the character variant feature. Expected in the font-specific range (256–32767).</value>
    /// <seealso cref="FeatUiTooltipTextNameId"/>
    /// <seealso cref="SampleTextNameId"/>
    public ushort FeatUiLabelNameId { get; init; }

    /// <summary>Gets the name table name ID of the user-interface tooltip text for the feature.</summary>
    /// <value>The name ID resolving to a human-readable tooltip for the character variant feature, or <c>0</c> when no tooltip is defined.</value>
    /// <seealso cref="FeatUiLabelNameId"/>
    /// <seealso cref="SampleTextNameId"/>
    public ushort FeatUiTooltipTextNameId { get; init; }

    /// <summary>Gets the name table name ID of sample text that demonstrates the feature.</summary>
    /// <value>The name ID resolving to a short sample string, or <c>0</c> when no sample is defined.</value>
    /// <seealso cref="FeatUiLabelNameId"/>
    /// <seealso cref="FeatUiTooltipTextNameId"/>
    public ushort SampleTextNameId { get; init; }

    /// <summary>Gets the number of named parameters that follow the fixed-size header.</summary>
    /// <value>The count of parameter entries that the feature definition exposes. The parameter labels begin at <see cref="FirstParamUiLabelNameId"/> and run consecutively.</value>
    /// <seealso cref="FirstParamUiLabelNameId"/>
    public ushort NumNamedParameters { get; init; }

    /// <summary>Gets the name table name ID of the first parameter's user-interface label. Values are expected to be in the font-specific range (256–32767).</summary>
    /// <value>The name ID resolving to the label of the first parameter; subsequent parameters use consecutive name IDs.</value>
    /// <seealso cref="NumNamedParameters"/>
    public ushort FirstParamUiLabelNameId { get; init; }

    /// <summary>Gets the 24-bit Unicode scalar values that follow the fixed-size header.</summary>
    /// <value>The list of Unicode scalar values covered by this character variant feature, one per variant. Each value is a full 24-bit scalar, so codepoints above the BMP are supported.</value>
    /// <seealso cref="CharacterCount"/>
    /// <seealso cref="UInt24"/>
    public IReadOnlyList<int> Characters { get; init; } = [];

    /// <summary>Gets the number of 24-bit Unicode scalar values that follow the fixed-size header.</summary>
    /// <value>The size of the <see cref="Characters"/> array.</value>
    /// <seealso cref="Characters"/>
    public int CharacterCount => Characters.Count;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the character-variant params block.</param>
    /// <param name="context">Unused. The block is self-describing.</param>
    /// <returns>The parsed params block.</returns>
    /// <exception cref="EndOfStreamException">The header or the character array extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The character count is read from <see cref="Header.CharCount"/>; the parser then consumes exactly that many 24-bit values.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/features_pt#cv">character variant feature</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Characters"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/features_pt#cv">OpenType specification: character variant feature</seealso>
    static FeatureParamsCharacterVariants IDerivedRecord<FeatureParams, FeatureParamsCharacterVariants>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        UInt24[] raw = cursor.ReadUInt24Array(header.CharCount);

        var characters = new int[raw.Length];
        for (int i = 0; i < raw.Length; i++)
            characters[i] = (int)raw[i].Value;

        return new FeatureParamsCharacterVariants
        {
            Format = header.Format,
            FeatUiLabelNameId = header.FeatUiLabelNameId,
            FeatUiTooltipTextNameId = header.FeatUiTooltipTextNameId,
            SampleTextNameId = header.SampleTextNameId,
            NumNamedParameters = header.NumNamedParameters,
            FirstParamUiLabelNameId = header.FirstParamUiLabelNameId,
            Characters = characters,
        };
    }

    /// <summary>The 14-byte character-variant params block. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The header is followed by <c>charCount</c> 24-bit Unicode scalar values; the total block length is <c>14 + 3 * charCount</c> bytes.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/features_pt#cv">character variant feature</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FeatureParamsCharacterVariants"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/features_pt#cv">OpenType specification: character variant feature</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IBigEndianStruct<Header>
    {
        /// <summary>The format of the feature parameters. Must be 1.</summary>
        /// <value>The constant <c>1</c> for the current version.</value>
        /// <seealso cref="CharCount"/>
        public ushort Format;

        /// <summary>The name table name ID of the user-interface label for the feature.</summary>
        /// <value>The name ID resolving to a human-readable label for the character variant feature. Expected in the font-specific range (256–32767).</value>
        /// <seealso cref="FeatUiTooltipTextNameId"/>
        public ushort FeatUiLabelNameId;

        /// <summary>The name table name ID of the user-interface tooltip text for the feature.</summary>
        /// <value>The name ID resolving to a tooltip for the character variant feature, or <c>0</c> when no tooltip is defined.</value>
        /// <seealso cref="FeatUiLabelNameId"/>
        public ushort FeatUiTooltipTextNameId;

        /// <summary>The name table name ID of sample text that demonstrates the feature.</summary>
        /// <value>The name ID resolving to a sample string, or <c>0</c> when no sample is defined.</value>
        /// <seealso cref="FeatUiLabelNameId"/>
        public ushort SampleTextNameId;

        /// <summary>The number of named parameters that follow the fixed-size header.</summary>
        /// <value>The count of parameter entries exposed by the feature.</value>
        /// <seealso cref="FirstParamUiLabelNameId"/>
        public ushort NumNamedParameters;

        /// <summary>The name table name ID of the first parameter's user-interface label.</summary>
        /// <value>The name ID resolving to the label of the first parameter; subsequent parameters use consecutive name IDs.</value>
        /// <seealso cref="NumNamedParameters"/>
        public ushort FirstParamUiLabelNameId;

        /// <summary>The number of 24-bit Unicode scalar values that follow the fixed-size header.</summary>
        /// <value>The count of uint24 values in the trailing array.</value>
        /// <seealso cref="Format"/>
        /// <seealso cref="UInt24"/>
        public ushort CharCount;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All seven fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IBigEndianStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            FeatUiLabelNameId = BinaryPrimitives.ReverseEndianness(v.FeatUiLabelNameId),
            FeatUiTooltipTextNameId = BinaryPrimitives.ReverseEndianness(v.FeatUiTooltipTextNameId),
            SampleTextNameId = BinaryPrimitives.ReverseEndianness(v.SampleTextNameId),
            NumNamedParameters = BinaryPrimitives.ReverseEndianness(v.NumNamedParameters),
            FirstParamUiLabelNameId = BinaryPrimitives.ReverseEndianness(v.FirstParamUiLabelNameId),
            CharCount = BinaryPrimitives.ReverseEndianness(v.CharCount),
        };
    }
}
