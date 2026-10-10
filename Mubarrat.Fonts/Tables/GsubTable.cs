using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mubarrat.Fonts.Binary;
using Mubarrat.Fonts.Primitives;

namespace Mubarrat.Fonts.Tables;

// ═══════════════════════════════════════════════════════════════════════════
// GSUB table
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>The <c>GSUB</c> table: glyph substitution. Supplies ligature formation, contextual substitution, alternate selection, and reverse chaining, organized by script and language system.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The table's top-level structure is three parallel lists — ScriptList, FeatureList, LookupList — that together describe which lookups apply to which script/feature combinations.</description></item>
/// <item><description>Version 1.0 defines the base header; version 1.1 adds a <see cref="FeatureVariations"/> table for conditional feature substitution in variable fonts.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub">GSUB table</see> chapter in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ScriptList"/>
/// <seealso cref="FeatureList"/>
/// <seealso cref="LookupList{T}"/>
/// <seealso cref="FeatureVariations"/>
/// <seealso cref="GsubSubtable"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub">OpenType specification: GSUB table</seealso>
public sealed record GsubTable : IFontTable<GsubTable>
{
    /// <inheritdoc/>
    /// <seealso cref="IFontTable{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub">OpenType specification: GSUB table</seealso>
    public static Tag Tag => "GSUB";

    /// <summary>Gets the major version. Always 1.</summary>
    /// <value>The constant <c>1</c> for a conforming GSUB table.</value>
    /// <seealso cref="MinorVersion"/>
    /// <seealso cref="Header.Major"/>
    public ushort MajorVersion { get; init; }

    /// <summary>Gets the minor version: 0 or 1.</summary>
    /// <value><c>0</c> for the base version, or <c>1</c> when the table declares a feature-variations offset.</value>
    /// <seealso cref="MajorVersion"/>
    /// <seealso cref="FeatureVariations"/>
    public ushort MinorVersion { get; init; }

    /// <summary>Gets the script list.</summary>
    /// <value>The <see cref="ScriptList"/> resolved from the table's script-list offset.</value>
    /// <seealso cref="FeatureList"/>
    /// <seealso cref="LookupList"/>
    public ScriptList ScriptList { get; init; } = null!;

    /// <summary>Gets the feature list.</summary>
    /// <value>The <see cref="FeatureList"/> resolved from the table's feature-list offset.</value>
    /// <seealso cref="ScriptList"/>
    /// <seealso cref="LookupList"/>
    public FeatureList FeatureList { get; init; } = null!;

    /// <summary>Gets the lookups in processing order.</summary>
    /// <value>The <see cref="LookupList{T}"/> resolved from the table's lookup-list offset; its indices are referenced by <see cref="Feature.LookupIndices"/>.</value>
    /// <seealso cref="FeatureList"/>
    /// <seealso cref="GsubSubtable"/>
    public LookupList<GsubSubtable> LookupList { get; init; } = null!;

    /// <summary>Gets the feature variations, or <c>null</c> for version 1.0.</summary>
    /// <value>The <see cref="FeatureVariations"/> resolved from the version 1.1 trailing offset, or <c>null</c> when the table is version 1.0 or the offset was zero.</value>
    /// <seealso cref="MinorVersion"/>
    /// <seealso cref="FeatureVariations"/>
    public FeatureVariations? FeatureVariations { get; init; }

    /// <summary>The 10-byte version 1.0 <c>GSUB</c> header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The three offsets are measured from the start of the GSUB table.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub">GSUB header</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="GsubTable"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub">OpenType specification: GSUB header</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the major version. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming GSUB table.</value>
        /// <seealso cref="Minor"/>
        public ushort Major;

        /// <summary>Gets the minor version: 0 or 1.</summary>
        /// <value><c>0</c> for the base version, or <c>1</c> when a feature-variations offset follows.</value>
        /// <seealso cref="Major"/>
        public ushort Minor;

        /// <summary>Gets the offset to the script list.</summary>
        /// <value>The byte offset of the <see cref="ScriptList"/> from the GSUB table start.</value>
        /// <seealso cref="FeatureListOffset"/>
        /// <seealso cref="LookupListOffset"/>
        public ushort ScriptListOffset;

        /// <summary>Gets the offset to the feature list.</summary>
        /// <value>The byte offset of the <see cref="FeatureList"/> from the GSUB table start.</value>
        /// <seealso cref="ScriptListOffset"/>
        /// <seealso cref="LookupListOffset"/>
        public ushort FeatureListOffset;

        /// <summary>Gets the offset to the lookup list.</summary>
        /// <value>The byte offset of the <see cref="LookupList{T}"/> from the GSUB table start.</value>
        /// <seealso cref="ScriptListOffset"/>
        /// <seealso cref="FeatureListOffset"/>
        public ushort LookupListOffset;

        /// <inheritdoc/>
        /// <param name="value">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All five fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header value) => new()
        {
            Major = BinaryPrimitives.ReverseEndianness(value.Major),
            Minor = BinaryPrimitives.ReverseEndianness(value.Minor),
            ScriptListOffset = BinaryPrimitives.ReverseEndianness(value.ScriptListOffset),
            FeatureListOffset = BinaryPrimitives.ReverseEndianness(value.FeatureListOffset),
            LookupListOffset = BinaryPrimitives.ReverseEndianness(value.LookupListOffset),
        };
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the GSUB table.</param>
    /// <param name="context">Forwarded to the subtable parsers.</param>
    /// <returns>The parsed GSUB table.</returns>
    /// <exception cref="InvalidDataException">The major version is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header or any referenced subtable extends past the end of the table-scoped source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The version 1.1 feature-variations offset is read only when <see cref="MinorVersion"/> is at least 1; a version 1.0 table does not advance the cursor past its declared extent.</description></item>
    /// <item><description>Every offset is resolved against the table-scoped source, so the referenced subtables' internal offsets resolve correctly.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub">GSUB table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="ScriptList"/>
    /// <seealso cref="FeatureList"/>
    /// <seealso cref="LookupList{T}"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub">OpenType specification: GSUB table</seealso>
    public static GsubTable Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();

        if (header.Major != 1)
            throw new InvalidDataException($"'GSUB'.majorVersion is {header.Major}, expected 1.");

        uint featureVariationsOffset = header.Minor >= 1 ? cursor.ReadUInt32() : 0u;

        return new GsubTable
        {
            MajorVersion = header.Major,
            MinorVersion = header.Minor,
            ScriptList = cursor.Source.ParseRecordAt<ScriptList>(header.ScriptListOffset, context),
            FeatureList = cursor.Source.ParseRecordAt<FeatureList>(header.FeatureListOffset, context),
            LookupList = cursor.Source.ParseRecordAt<LookupList<GsubSubtable>>(header.LookupListOffset, context),
            FeatureVariations = featureVariationsOffset != 0
                ? cursor.Source.ParseRecordAt<FeatureVariations>(featureVariationsOffset, context)
                : null,
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// GsubSubtable — dispatcher
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Base class for all GSUB lookup subtables.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The dispatcher reads the lookup type from <c>context</c> and routes to the matching concrete subtable family. The context value passed by <c>Lookup&lt;GsubSubtable&gt;</c> must be the enclosing lookup's <c>LookupType</c>.</description></item>
/// <item><description>Lookup type 7 (extension substitution) is followed transparently: the dispatcher reads the extension record and re-dispatches to the wrapped subtable. Consumers never observe type 7 in the returned object graph.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub">GSUB table</see> chapter in the OpenType specification for the full list of lookup types.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SingleSubst"/>
/// <seealso cref="MultipleSubst"/>
/// <seealso cref="AlternateSubst"/>
/// <seealso cref="LigatureSubst"/>
/// <seealso cref="ContextualSubst"/>
/// <seealso cref="ChainContextSubst"/>
/// <seealso cref="ExtensionSubst"/>
/// <seealso cref="ReverseChainSingleSubst"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub">OpenType specification: GSUB table</seealso>
public abstract record GsubSubtable : IRecord<GsubSubtable>, IBaseRecord<GsubSubtable>
{
    /// <summary>Gets the subtable format number. Its meaning depends on the lookup type.</summary>
    /// <value>The format discriminant, interpreted in the context of the enclosing lookup's type.</value>
    public ushort Format { get; init; }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable.</param>
    /// <param name="context">A <see cref="SubtableContext"/> carrying the enclosing lookup's <see cref="SubtableContext.LookupType"/>.</param>
    /// <returns>The concrete subtable for the given lookup type.</returns>
    /// <exception cref="InvalidDataException">The lookup type is not in 1–8.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The lookup-type switch is exhaustive over the seven concrete families plus extension; type 7 is dispatched through <see cref="ExtensionSubst.ParseAndUnwrap(ref Cursor)"/> which returns the wrapped subtable directly.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub">GSUB table</see> chapter in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SubtableContext"/>
    /// <seealso cref="ExtensionSubst"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub">OpenType specification: GSUB table</seealso>
    public static GsubSubtable Parse(ref Cursor cursor, object? context)
    {
        ushort lookupType = ((SubtableContext)context!).LookupType;
        return lookupType switch
        {
            1 => IBaseRecord<GsubSubtable>.Parse<SingleSubst>(ref cursor),
            2 => IBaseRecord<GsubSubtable>.Parse<MultipleSubst>(ref cursor),
            3 => IBaseRecord<GsubSubtable>.Parse<AlternateSubst>(ref cursor),
            4 => IBaseRecord<GsubSubtable>.Parse<LigatureSubst>(ref cursor),
            5 => IBaseRecord<GsubSubtable>.Parse<ContextualSubst>(ref cursor),
            6 => IBaseRecord<GsubSubtable>.Parse<ChainContextSubst>(ref cursor),
            7 => ExtensionSubst.ParseAndUnwrap(ref cursor),
            8 => IBaseRecord<GsubSubtable>.Parse<ReverseChainSingleSubst>(ref cursor),
            _ => throw new InvalidDataException($"GSUB lookup type {lookupType} is not defined."),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Lookup 1: SingleSubst
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Lookup type 1: single substitution. Replaces one glyph with one other glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Both formats share the same coverage + format shape and differ in whether the substitute is computed from a signed delta (format 1) or looked up from an explicit array (format 2).</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">Lookup Type 1</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SingleSubstFormat1"/>
/// <seealso cref="SingleSubstFormat2"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">OpenType specification: Lookup Type 1</seealso>
public abstract record SingleSubst : GsubSubtable, IBaseRecord<SingleSubst>, IDerivedRecord<GsubSubtable, SingleSubst>
{
    /// <summary>Gets the coverage table listing substituted glyphs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects a substitute in the format-2 array, or applies the format-1 delta.</value>
    /// <seealso cref="GetSubstitute(int)"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Returns the substitute glyph for <paramref name="glyphId"/>, or <paramref name="glyphId"/> itself when the glyph is not covered.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The substitute glyph ID, or <paramref name="glyphId"/> when the glyph is not covered by the subtable.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Both format implementations return the input glyph unchanged when it is not covered, matching the specification's implicit identity behavior for uncovered glyphs.</description></item>
    /// <item><description>Format 1 computes the substitute arithmetically modulo 65536; format 2 looks it up from the coverage-indexed array.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SingleSubstFormat1.GetSubstitute(int)"/>
    /// <seealso cref="SingleSubstFormat2.GetSubstitute(int)"/>
    public abstract int GetSubstitute(int glyphId);

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable body (after the lookup-type dispatch).</param>
    /// <param name="context">Forwarded to the format-specific parser.</param>
    /// <returns>The format-specific SingleSubst subtable.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1 or 2.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">Lookup Type 1</see> in the OpenType specification.</remarks>
    /// <seealso cref="SingleSubstFormat1"/>
    /// <seealso cref="SingleSubstFormat2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">OpenType specification: Lookup Type 1</seealso>
    public static new SingleSubst Parse(ref Cursor cursor, object? context)
    {
        ushort format = cursor.ReadUInt16();
        return format switch
        {
            1 => IBaseRecord<SingleSubst>.Parse<SingleSubstFormat1>(ref cursor, context),
            2 => IBaseRecord<SingleSubst>.Parse<SingleSubstFormat2>(ref cursor, context),
            _ => throw new InvalidDataException($"SingleSubst format {format} is not defined."),
        };
    }
}

/// <summary>SingleSubst format 1: a constant delta applied modulo 65536.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The substitute is <c>(glyphId + DeltaGlyphId) mod 65536</c>; the specification requires the writer to choose a delta such that the result never wraps, but the implementation masks defensively.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">Lookup Type 1</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SingleSubst"/>
/// <seealso cref="SingleSubstFormat2"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">OpenType specification: Lookup Type 1</seealso>
public sealed record SingleSubstFormat1 : SingleSubst, IDerivedRecord<SingleSubst, SingleSubstFormat1>
{
    /// <summary>Gets the signed delta added to each covered glyph.</summary>
    /// <value>The signed 16-bit delta added to every covered glyph's ID to produce the substitute ID.</value>
    /// <seealso cref="GetSubstitute(int)"/>
    public short DeltaGlyphId { get; init; }

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The glyph ID plus <see cref="DeltaGlyphId"/> modulo 65536, or <paramref name="glyphId"/> when the glyph is not covered.</returns>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">Lookup Type 1</see> in the OpenType specification.</remarks>
    /// <seealso cref="DeltaGlyphId"/>
    /// <seealso cref="SingleSubst.Coverage"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">OpenType specification: Lookup Type 1</seealso>
    public override int GetSubstitute(int glyphId) =>
        Coverage.GetCoverageIndex(glyphId) >= 0 ? (glyphId + DeltaGlyphId) & 0xFFFF : glyphId;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-1 body (after the format discriminant).</param>
    /// <param name="context">Forwarded to the coverage parser.</param>
    /// <returns>The parsed format-1 subtable.</returns>
    /// <exception cref="EndOfStreamException">The header or coverage extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">Lookup Type 1</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">OpenType specification: Lookup Type 1</seealso>
    static SingleSubstFormat1 IDerivedRecord<SingleSubst, SingleSubstFormat1>.Parse(ref Cursor cursor, object? context)
    {
        // The format word was consumed by SingleSubst.Parse.
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new SingleSubstFormat1
        {
            Format = 1,
            Coverage = cursor.Source.ParseRecordAt<Coverage>(header.CoverageOffset, context),
            DeltaGlyphId = header.DeltaGlyphId,
        };
    }

    /// <summary>The 4-byte SingleSubst format 1 body. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The coverage offset is measured from the start of the SingleSubst subtable.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">Lookup Type 1</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SingleSubstFormat1"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">OpenType specification: Lookup Type 1</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the offset to the coverage table.</summary>
        /// <value>The byte offset of the <see cref="Coverage"/> from the subtable start.</value>
        /// <seealso cref="DeltaGlyphId"/>
        public ushort CoverageOffset;

        /// <summary>Gets the signed delta added to each covered glyph.</summary>
        /// <value>The signed 16-bit delta used by <see cref="SingleSubstFormat1.GetSubstitute(int)"/>.</value>
        /// <seealso cref="CoverageOffset"/>
        public short DeltaGlyphId;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>The coverage offset and the delta are each reversed via the corresponding <see cref="BinaryPrimitives"/> overload.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            CoverageOffset = BinaryPrimitives.ReverseEndianness(v.CoverageOffset),
            DeltaGlyphId = BinaryPrimitives.ReverseEndianness(v.DeltaGlyphId),
        };
    }
}

/// <summary>SingleSubst format 2: an explicit substitute glyph per covered glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The substitute array is index-aligned with the coverage table: entry <c>i</c> is the substitute for the glyph at coverage index <c>i</c>.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">Lookup Type 1</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SingleSubst"/>
/// <seealso cref="SingleSubstFormat1"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">OpenType specification: Lookup Type 1</seealso>
public sealed record SingleSubstFormat2 : SingleSubst, IDerivedRecord<SingleSubst, SingleSubstFormat2>
{
    /// <summary>Gets the substitutes in Coverage Index order.</summary>
    /// <value>The ordered list of substitute glyph IDs; length equals <see cref="SingleSubst.Coverage"/>'s glyph count.</value>
    /// <seealso cref="SingleSubst.Coverage"/>
    /// <seealso cref="GetSubstitute(int)"/>
    public IReadOnlyList<ushort> SubstituteGlyphIds { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The substitute at the glyph's coverage index, or <paramref name="glyphId"/> when the glyph is not covered.</returns>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">Lookup Type 1</see> in the OpenType specification.</remarks>
    /// <seealso cref="SubstituteGlyphIds"/>
    /// <seealso cref="SingleSubst.Coverage"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">OpenType specification: Lookup Type 1</seealso>
    public override int GetSubstitute(int glyphId)
    {
        int i = Coverage.GetCoverageIndex(glyphId);
        return i >= 0 && i < SubstituteGlyphIds.Count ? SubstituteGlyphIds[i] : glyphId;
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the format-2 body (after the format discriminant).</param>
    /// <param name="context">Forwarded to the coverage parser.</param>
    /// <returns>The parsed format-2 subtable.</returns>
    /// <exception cref="EndOfStreamException">The header, coverage, or substitute array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">Lookup Type 1</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">OpenType specification: Lookup Type 1</seealso>
    static SingleSubstFormat2 IDerivedRecord<SingleSubst, SingleSubstFormat2>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        return new SingleSubstFormat2
        {
            Format = 2,
            Coverage = cursor.Source.ParseRecordAt<Coverage>(header.CoverageOffset, context),
            SubstituteGlyphIds = cursor.ReadUInt16Array(header.GlyphCount),
        };
    }

    /// <summary>The 6-byte SingleSubst format 2 body. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The coverage offset is measured from the start of the SingleSubst subtable.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">Lookup Type 1</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SingleSubstFormat2"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-1-single-substitution-subtable">OpenType specification: Lookup Type 1</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the offset to the coverage table.</summary>
        /// <value>The byte offset of the <see cref="Coverage"/> from the subtable start.</value>
        /// <seealso cref="GlyphCount"/>
        public ushort CoverageOffset;

        /// <summary>Gets the number of substitute glyph IDs.</summary>
        /// <value>The count of substitutes in the array; must equal the coverage table's glyph count.</value>
        /// <seealso cref="CoverageOffset"/>
        public ushort GlyphCount;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with both fields reversed.</returns>
        /// <remarks>Both fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            CoverageOffset = BinaryPrimitives.ReverseEndianness(v.CoverageOffset),
            GlyphCount = BinaryPrimitives.ReverseEndianness(v.GlyphCount),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Lookup 2: MultipleSubst
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Lookup type 2: multiple substitution. Replaces one glyph with a sequence of glyphs.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Commonly used to decompose a precomposed character into a base glyph plus combining marks.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-2-multiple-substitution-subtable">Lookup Type 2</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Sequence"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-2-multiple-substitution-subtable">OpenType specification: Lookup Type 2</seealso>
public sealed record MultipleSubst : GsubSubtable, IDerivedRecord<GsubSubtable, MultipleSubst>
{
    /// <summary>Gets the coverage table listing substituted glyphs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>Sequences[i]</c>.</value>
    /// <seealso cref="Sequences"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets the Sequence tables in Coverage Index order.</summary>
    /// <value>The ordered list of <see cref="Sequence"/> entries; length equals <see cref="Coverage"/>'s glyph count.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="Sequence"/>
    public IReadOnlyList<Sequence> Sequences { get; init; } = [];

    /// <summary>Returns the substitute sequence for a glyph, or an empty list when uncovered.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The substitute sequence for the glyph, or an empty list when the glyph is not covered.</returns>
    /// <seealso cref="Sequences"/>
    /// <seealso cref="Sequence.SubstituteGlyphIds"/>
    public IReadOnlyList<ushort> GetSubstitutes(int glyphId)
    {
        int i = Coverage.GetCoverageIndex(glyphId);
        return i >= 0 && i < Sequences.Count ? Sequences[i].SubstituteGlyphIds : [];
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable body (after the lookup-type dispatch).</param>
    /// <param name="context">Forwarded to the coverage and sequence parsers.</param>
    /// <returns>The parsed MultipleSubst subtable.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header, coverage, or any sequence extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-2-multiple-substitution-subtable">Lookup Type 2</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Sequence"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-2-multiple-substitution-subtable">OpenType specification: Lookup Type 2</seealso>
    static MultipleSubst IDerivedRecord<GsubSubtable, MultipleSubst>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.Format != 1)
            throw new InvalidDataException($"MultipleSubst format {header.Format} is not defined.");
        return new MultipleSubst
        {
            Format = 1,
            Coverage = cursor.Source.ParseRecordAt<Coverage>(header.CoverageOffset, context),
            Sequences = cursor.ReadOffset16ArrayPeekRecord<Sequence>(header.SequenceCount, context),
        };
    }

    /// <summary>The 6-byte MultipleSubst header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The coverage offset and the sequence-array offsets are measured from the start of the MultipleSubst subtable.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-2-multiple-substitution-subtable">Lookup Type 2</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="MultipleSubst"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-2-multiple-substitution-subtable">OpenType specification: Lookup Type 2</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the format. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming MultipleSubst subtable.</value>
        /// <seealso cref="CoverageOffset"/>
        public ushort Format;

        /// <summary>Gets the offset to the coverage table.</summary>
        /// <value>The byte offset of the <see cref="Coverage"/> from the subtable start.</value>
        /// <seealso cref="SequenceCount"/>
        public ushort CoverageOffset;

        /// <summary>Gets the number of Sequence tables.</summary>
        /// <value>The count of Offset16 entries that follow the header; must equal the coverage table's glyph count.</value>
        /// <seealso cref="CoverageOffset"/>
        public ushort SequenceCount;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            CoverageOffset = BinaryPrimitives.ReverseEndianness(v.CoverageOffset),
            SequenceCount = BinaryPrimitives.ReverseEndianness(v.SequenceCount),
        };
    }
}

/// <summary>A Sequence of glyphs substituted for a single covered glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The sequence replaces one input glyph; the number of output glyphs can be zero (deletion) or many (decomposition).</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-2-multiple-substitution-subtable">Lookup Type 2</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="MultipleSubst"/>
/// <seealso cref="MultipleSubst.GetSubstitutes(int)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-2-multiple-substitution-subtable">OpenType specification: Lookup Type 2</seealso>
public sealed record Sequence : IRecord<Sequence>
{
    /// <summary>Gets the substitute glyph IDs.</summary>
    /// <value>The ordered list of substitute glyph IDs that replace the single input glyph.</value>
    /// <seealso cref="MultipleSubst.Sequences"/>
    public IReadOnlyList<ushort> SubstituteGlyphIds { get; init; } = [];

    /// <summary>Reads the substitute count and the corresponding glyph ID array from the cursor.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the sequence.</param>
    /// <param name="context">Unused. The sequence is self-describing.</param>
    /// <returns>The parsed sequence.</returns>
    /// <exception cref="EndOfStreamException">The count or glyph ID array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-2-multiple-substitution-subtable">Lookup Type 2</see> in the OpenType specification.</remarks>
    /// <seealso cref="SubstituteGlyphIds"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-2-multiple-substitution-subtable">OpenType specification: Lookup Type 2</seealso>
    public static Sequence Parse(ref Cursor cursor, object? context) => new() { SubstituteGlyphIds = cursor.ReadUInt16Array(cursor.ReadUInt16()) };
}

// ═══════════════════════════════════════════════════════════════════════════
// Lookup 3: AlternateSubst
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Lookup type 3: alternate substitution. Replaces one glyph with one of several alternates chosen by the shaper.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The shaper selects which alternate to use based on the feature's parameters and user preference; the subtable only enumerates the available alternates.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-3-alternate-substitution-subtable">Lookup Type 3</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AlternateSet"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-3-alternate-substitution-subtable">OpenType specification: Lookup Type 3</seealso>
public sealed record AlternateSubst : GsubSubtable, IDerivedRecord<GsubSubtable, AlternateSubst>
{
    /// <summary>Gets the coverage table listing substituted glyphs.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>AlternateSets[i]</c>.</value>
    /// <seealso cref="AlternateSets"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets the AlternateSets in Coverage Index order.</summary>
    /// <value>The ordered list of <see cref="AlternateSet"/> entries; length equals <see cref="Coverage"/>'s glyph count.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="AlternateSet"/>
    public IReadOnlyList<AlternateSet> AlternateSets { get; init; } = [];

    /// <summary>Returns the alternates for a glyph, or an empty list when uncovered.</summary>
    /// <param name="glyphId">The glyph ID to look up.</param>
    /// <returns>The list of alternate glyph IDs for the glyph, or an empty list when the glyph is not covered.</returns>
    /// <seealso cref="AlternateSets"/>
    /// <seealso cref="AlternateSet.AlternateGlyphIds"/>
    public IReadOnlyList<ushort> GetAlternates(int glyphId)
    {
        int i = Coverage.GetCoverageIndex(glyphId);
        return i >= 0 && i < AlternateSets.Count ? AlternateSets[i].AlternateGlyphIds : [];
    }

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable body (after the lookup-type dispatch).</param>
    /// <param name="context">Forwarded to the coverage and alternate-set parsers.</param>
    /// <returns>The parsed AlternateSubst subtable.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header, coverage, or any alternate set extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-3-alternate-substitution-subtable">Lookup Type 3</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="AlternateSet"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-3-alternate-substitution-subtable">OpenType specification: Lookup Type 3</seealso>
    static AlternateSubst IDerivedRecord<GsubSubtable, AlternateSubst>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.Format != 1)
            throw new InvalidDataException($"AlternateSubst format {header.Format} is not defined.");

        return new AlternateSubst
        {
            Format = 1,
            Coverage = cursor.Source.ParseRecordAt<Coverage>(header.CoverageOffset, context),
            AlternateSets = cursor.ReadOffset16ArrayPeekRecord<AlternateSet>(header.AlternateSetCount, context),
        };
    }

    /// <summary>The 6-byte AlternateSubst header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The coverage and alternate-set offsets are measured from the start of the AlternateSubst subtable.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-3-alternate-substitution-subtable">Lookup Type 3</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="AlternateSubst"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-3-alternate-substitution-subtable">OpenType specification: Lookup Type 3</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the format. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming AlternateSubst subtable.</value>
        /// <seealso cref="CoverageOffset"/>
        public ushort Format;

        /// <summary>Gets the offset to the coverage table.</summary>
        /// <value>The byte offset of the <see cref="Coverage"/> from the subtable start.</value>
        /// <seealso cref="AlternateSetCount"/>
        public ushort CoverageOffset;

        /// <summary>Gets the number of AlternateSet tables.</summary>
        /// <value>The count of Offset16 entries that follow the header; must equal the coverage table's glyph count.</value>
        /// <seealso cref="CoverageOffset"/>
        public ushort AlternateSetCount;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            CoverageOffset = BinaryPrimitives.ReverseEndianness(v.CoverageOffset),
            AlternateSetCount = BinaryPrimitives.ReverseEndianness(v.AlternateSetCount),
        };
    }
}

/// <summary>An AlternateSet: the alternates for one covered glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Alternate order is the order the font designer intends the shaper to try; the first is the preferred alternate.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-3-alternate-substitution-subtable">Lookup Type 3</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="AlternateSubst"/>
/// <seealso cref="AlternateSubst.GetAlternates(int)"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-3-alternate-substitution-subtable">OpenType specification: Lookup Type 3</seealso>
public sealed record AlternateSet : IRecord<AlternateSet>
{
    /// <summary>Gets the alternate glyph IDs, in arbitrary order.</summary>
    /// <value>The ordered list of alternate glyph IDs available for the covered glyph.</value>
    /// <seealso cref="AlternateSubst.AlternateSets"/>
    public IReadOnlyList<ushort> AlternateGlyphIds { get; init; } = [];

    /// <summary>Reads the alternate count and the corresponding glyph ID array from the cursor.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the alternate set.</param>
    /// <param name="context">Unused. The alternate set is self-describing.</param>
    /// <returns>The parsed alternate set.</returns>
    /// <exception cref="EndOfStreamException">The count or glyph ID array extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-3-alternate-substitution-subtable">Lookup Type 3</see> in the OpenType specification.</remarks>
    /// <seealso cref="AlternateGlyphIds"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-3-alternate-substitution-subtable">OpenType specification: Lookup Type 3</seealso>
    public static AlternateSet Parse(ref Cursor cursor, object? context) => new() { AlternateGlyphIds = cursor.ReadUInt16Array(cursor.ReadUInt16()) };
}

// ═══════════════════════════════════════════════════════════════════════════
// Lookup 4: LigatureSubst
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Lookup type 4: ligature substitution. Replaces a sequence of glyphs with a single ligature glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The coverage table identifies the first glyph of each ligature; the rest of the input sequence is stored in the ligature records.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-4-ligature-substitution-subtable">Lookup Type 4</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LigatureSet"/>
/// <seealso cref="Ligature"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-4-ligature-substitution-subtable">OpenType specification: Lookup Type 4</seealso>
public sealed record LigatureSubst : GsubSubtable, IDerivedRecord<GsubSubtable, LigatureSubst>
{
    /// <summary>Gets the coverage table listing the first glyph of each ligature.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>LigatureSets[i]</c>.</value>
    /// <seealso cref="LigatureSets"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets the LigatureSets in Coverage Index order.</summary>
    /// <value>The ordered list of <see cref="LigatureSet"/> entries; length equals <see cref="Coverage"/>'s glyph count.</value>
    /// <seealso cref="Coverage"/>
    /// <seealso cref="LigatureSet"/>
    public IReadOnlyList<LigatureSet> LigatureSets { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable body (after the lookup-type dispatch).</param>
    /// <param name="context">Forwarded to the coverage and ligature-set parsers.</param>
    /// <returns>The parsed LigatureSubst subtable.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header, coverage, or any ligature set extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-4-ligature-substitution-subtable">Lookup Type 4</see> in the OpenType specification.</remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="LigatureSet"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-4-ligature-substitution-subtable">OpenType specification: Lookup Type 4</seealso>
    static LigatureSubst IDerivedRecord<GsubSubtable, LigatureSubst>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.Format != 1)
            throw new InvalidDataException($"LigatureSubst format {header.Format} is not defined.");

        return new LigatureSubst
        {
            Format = 1,
            Coverage = cursor.Source.ParseRecordAt<Coverage>(header.CoverageOffset, context),
            LigatureSets = cursor.ReadOffset16ArrayPeekRecord<LigatureSet>(header.LigatureSetCount, context),
        };
    }

    /// <summary>The 6-byte LigatureSubst header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The coverage and ligature-set offsets are measured from the start of the LigatureSubst subtable.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-4-ligature-substitution-subtable">Lookup Type 4</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="LigatureSubst"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-4-ligature-substitution-subtable">OpenType specification: Lookup Type 4</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the format. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming LigatureSubst subtable.</value>
        /// <seealso cref="CoverageOffset"/>
        public ushort Format;

        /// <summary>Gets the offset to the coverage table.</summary>
        /// <value>The byte offset of the <see cref="Coverage"/> from the subtable start.</value>
        /// <seealso cref="LigatureSetCount"/>
        public ushort CoverageOffset;

        /// <summary>Gets the number of LigatureSet tables.</summary>
        /// <value>The count of Offset16 entries that follow the header; must equal the coverage table's glyph count.</value>
        /// <seealso cref="CoverageOffset"/>
        public ushort LigatureSetCount;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            CoverageOffset = BinaryPrimitives.ReverseEndianness(v.CoverageOffset),
            LigatureSetCount = BinaryPrimitives.ReverseEndianness(v.LigatureSetCount),
        };
    }
}

/// <summary>A LigatureSet: the ligature sequences that begin with one covered glyph.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Ligatures are evaluated in declaration order; the first whose input sequence matches is used.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-4-ligature-substitution-subtable">Lookup Type 4</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LigatureSubst"/>
/// <seealso cref="Ligature"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-4-ligature-substitution-subtable">OpenType specification: Lookup Type 4</seealso>
public sealed record LigatureSet : IRecord<LigatureSet>
{
    /// <summary>Gets the ligatures, ordered by preference (first match wins).</summary>
    /// <value>The ordered list of <see cref="Ligature"/> entries; all begin with the same first glyph but consume different input sequences.</value>
    /// <seealso cref="Ligature"/>
    public IReadOnlyList<Ligature> Ligatures { get; init; } = [];

    /// <summary>Reads the ligature count and the corresponding ligature offset array from the cursor.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the ligature set.</param>
    /// <param name="context">Forwarded to the ligature parsers.</param>
    /// <returns>The parsed ligature set.</returns>
    /// <exception cref="EndOfStreamException">The count, offset array, or any referenced ligature extends past the end of the source.</exception>
    /// <remarks>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-4-ligature-substitution-subtable">Lookup Type 4</see> in the OpenType specification.</remarks>
    /// <seealso cref="Ligatures"/>
    /// <seealso cref="Ligature"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-4-ligature-substitution-subtable">OpenType specification: Lookup Type 4</seealso>
    public static LigatureSet Parse(ref Cursor cursor, object? context) => new() { Ligatures = cursor.ReadOffset16ArrayPeekRecord<Ligature>(cursor.ReadUInt16(), context) };
}

/// <summary>A Ligature: the output glyph and the input component sequence it replaces. The first component is implied by the parent coverage table; the stored array begins at input index 1.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The first glyph of the input sequence is implicit through the enclosing coverage index; only the remaining components are stored here.</description></item>
/// <item><description>Declared as a <c>record</c>, so equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-4-ligature-substitution-subtable">Lookup Type 4</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="LigatureSet"/>
/// <seealso cref="LigatureSubst"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-4-ligature-substitution-subtable">OpenType specification: Lookup Type 4</seealso>
public sealed record Ligature : IRecord<Ligature>
{
    /// <summary>Gets the output ligature glyph ID.</summary>
    /// <value>The glyph ID that replaces the entire matched input sequence.</value>
    /// <seealso cref="ComponentGlyphIds"/>
    /// <seealso cref="ComponentCount"/>
    public ushort LigatureGlyph { get; init; }

    /// <summary>Gets the component glyph IDs in input order, starting from the second component. Component count is <c>ComponentGlyphIds.Count + 1</c>.</summary>
    /// <value>The ordered list of input component glyph IDs following the first; length is <c>ComponentCount - 1</c>.</value>
    /// <seealso cref="LigatureGlyph"/>
    /// <seealso cref="ComponentCount"/>
    public IReadOnlyList<ushort> ComponentGlyphIds { get; init; } = [];

    /// <summary>Gets the total number of glyphs consumed by this ligature.</summary>
    /// <value>The number of input glyphs the ligature replaces; equals <c><see cref="ComponentGlyphIds"/>.Count + 1</c>.</value>
    /// <seealso cref="ComponentGlyphIds"/>
    public int ComponentCount => ComponentGlyphIds.Count + 1;

    /// <summary>Reads the ligature glyph, the component count, and the component glyph ID array from the cursor.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the ligature.</param>
    /// <param name="context">Unused. The ligature is self-describing.</param>
    /// <returns>The parsed ligature.</returns>
    /// <exception cref="EndOfStreamException">The header or component array extends past the end of the source.</exception>
    /// <remarks>The component count on disk includes the implicit first component; the parser subtracts one before reading the array.</remarks>
    /// <seealso cref="ComponentGlyphIds"/>
    /// <seealso cref="ComponentCount"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-4-ligature-substitution-subtable">OpenType specification: Lookup Type 4</seealso>
    public static Ligature Parse(ref Cursor cursor, object? context) => new()
    {
        LigatureGlyph = cursor.ReadUInt16(),
        // componentGlyphIDs has (componentCount - 1) entries.
        ComponentGlyphIds = cursor.ReadUInt16Array(cursor.ReadUInt16() - 1),
    };
}

// ═══════════════════════════════════════════════════════════════════════════
// Lookup 5 & 6: Contextual substitution
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Lookup type 5: contextual substitution. Uses the shared <see cref="SequenceContext"/> formats from the layout common table definitions.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The contextual formats are shared with GPOS; only the nested lookup references differ, pointing at GSUB lookups instead of GPOS lookups.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-5-contextual-substitution-subtable">Lookup Type 5</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="SequenceContext"/>
/// <seealso cref="ChainContextSubst"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-5-contextual-substitution-subtable">OpenType specification: Lookup Type 5</seealso>
public sealed record ContextualSubst : GsubSubtable, IDerivedRecord<GsubSubtable, ContextualSubst>
{
    /// <summary>Gets the contextual subtable (format 1, 2, or 3).</summary>
    /// <value>The <see cref="SequenceContext"/> that describes the matched input sequence and the nested lookups to apply on a match.</value>
    /// <seealso cref="SequenceContext"/>
    public SequenceContext Context { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable body (after the lookup-type dispatch).</param>
    /// <param name="context">Forwarded to the contextual parser.</param>
    /// <returns>The parsed ContextualSubst subtable.</returns>
    /// <exception cref="EndOfStreamException">The contextual record or any referenced subtable extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The <see cref="GsubSubtable.Format"/> property is set to <c>1</c> as a placeholder; the real format is inside the <see cref="SequenceContext"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-5-contextual-substitution-subtable">Lookup Type 5</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Context"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-5-contextual-substitution-subtable">OpenType specification: Lookup Type 5</seealso>
    static ContextualSubst IDerivedRecord<GsubSubtable, ContextualSubst>.Parse(ref Cursor cursor, object? context) => new()
    {
        Format = 1,
        Context = cursor.ReadRecord<SequenceContext>(),
    };
}

/// <summary>Lookup type 6: chained contextual substitution. Uses the shared <see cref="ChainedSequenceContext"/> formats from the layout common table definitions.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>The chained contextual formats are shared with GPOS; only the nested lookup references differ, pointing at GSUB lookups instead of GPOS lookups.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-6-chained-contextual-substitution-subtable">Lookup Type 6</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="ChainedSequenceContext"/>
/// <seealso cref="ContextualSubst"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-6-chained-contextual-substitution-subtable">OpenType specification: Lookup Type 6</seealso>
public sealed record ChainContextSubst : GsubSubtable, IDerivedRecord<GsubSubtable, ChainContextSubst>
{
    /// <summary>Gets the chained contextual subtable (format 1, 2, or 3).</summary>
    /// <value>The <see cref="ChainedSequenceContext"/> that describes the matched backtrack/input/lookahead sequences and the nested lookups to apply on a match.</value>
    /// <seealso cref="ChainedSequenceContext"/>
    public ChainedSequenceContext Context { get; init; } = null!;

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable body (after the lookup-type dispatch).</param>
    /// <param name="context">Forwarded to the chained contextual parser.</param>
    /// <returns>The parsed ChainContextSubst subtable.</returns>
    /// <exception cref="EndOfStreamException">The chained contextual record or any referenced subtable extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The <see cref="GsubSubtable.Format"/> property is set to <c>1</c> as a placeholder; the real format is inside the <see cref="ChainedSequenceContext"/>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-6-chained-contextual-substitution-subtable">Lookup Type 6</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Context"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-6-chained-contextual-substitution-subtable">OpenType specification: Lookup Type 6</seealso>
    static ChainContextSubst IDerivedRecord<GsubSubtable, ChainContextSubst>.Parse(ref Cursor cursor, object? context) => new()
    {
        Format = 1,
        Context = cursor.ReadRecord<ChainedSequenceContext>(),
    };
}

// ═══════════════════════════════════════════════════════════════════════════
// Lookup 7: ExtensionSubst
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Lookup type 7: extension substitution. Followed transparently during parsing so consumers never see this type.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>ExtensionSubst is deliberately a static helper, not a derived record. The derived-record pattern is for formats whose parse produces an object of the format type. Extension format 7 exists only to carry a 32-bit offset; the object it eventually produces is some other format.</description></item>
/// <item><description>Making ExtensionSubst a derived record would either leak a wrapper into the object graph — contradicting the specification's "processed as if it were a subtable of the type it points at" — or produce a derived record that the dispatcher never invokes through the interface.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-7-extension-substitution-subtable">Lookup Type 7</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="GsubSubtable"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-7-extension-substitution-subtable">OpenType specification: Lookup Type 7</seealso>
public static class ExtensionSubst
{
    /// <summary>Reads an extension record, follows the 32-bit offset, and returns the wrapped subtable. Called by the lookup dispatcher in place of a direct parse.</summary>
    /// <param name="cursor">Cursor positioned at the first byte of the extension record.</param>
    /// <returns>The subtable identified by the extension's lookup type and offset.</returns>
    /// <exception cref="InvalidDataException">The format is not 1, or the extension's lookup type is itself 7 (recursive extension).</exception>
    /// <exception cref="EndOfStreamException">The extension header or the wrapped subtable extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The extension's offset is relative to the start of the extension subtable, which is <c>cursor.Source</c>'s base. The parser re-bases the source at that offset before dispatching so the wrapped subtable's internal offsets resolve against its own start rather than the extension's.</description></item>
    /// <item><description>Recursive extension indirection is rejected: an extension whose lookup type is 7 would loop indefinitely.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-7-extension-substitution-subtable">Lookup Type 7</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="SubtableContext"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-7-extension-substitution-subtable">OpenType specification: Lookup Type 7</seealso>
    public static GsubSubtable ParseAndUnwrap(ref Cursor cursor)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.Format != 1)
            throw new InvalidDataException($"ExtensionSubst format {header.Format} is not defined.");

        if (header.ExtensionLookupType == 7)
            throw new InvalidDataException("ExtensionSubst cannot reference another type 7 lookup.");

        // The extension's offset is relative to the start of the extension subtable,
        // which is cursor.Source's base. Re-base the source at that offset before
        // dispatching so the wrapped subtable's internal offsets resolve against its
        // own start rather than the extension's.
        return cursor.Source.ParseRecordAt<GsubSubtable>(header.ExtensionOffset, new SubtableContext(header.ExtensionLookupType));
    }

    /// <summary>The 8-byte ExtensionSubst header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The extension offset is a 32-bit value measured from the start of this record.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-7-extension-substitution-subtable">Lookup Type 7</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ExtensionSubst"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-7-extension-substitution-subtable">OpenType specification: Lookup Type 7</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the format. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming extension record.</value>
        /// <seealso cref="ExtensionLookupType"/>
        public ushort Format;

        /// <summary>Gets the lookup type of the wrapped subtable.</summary>
        /// <value>The lookup type from 1 to 6 or 8 that the extension's offset points at; must not be <c>7</c>.</value>
        /// <seealso cref="Format"/>
        /// <seealso cref="ExtensionOffset"/>
        public ushort ExtensionLookupType;

        /// <summary>Gets the offset from the start of this extension record to the wrapped subtable.</summary>
        /// <value>A 32-bit byte offset measured from the extension record start.</value>
        /// <seealso cref="ExtensionLookupType"/>
        public uint ExtensionOffset;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are multi-byte and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            ExtensionLookupType = BinaryPrimitives.ReverseEndianness(v.ExtensionLookupType),
            ExtensionOffset = BinaryPrimitives.ReverseEndianness(v.ExtensionOffset),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Lookup 8: ReverseChainSingleSubst
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Lookup type 8: reverse chaining contextual single substitution. Matches a single input glyph in a backtrack/lookahead context, then replaces it. Processing runs from the end of the glyph sequence backward, unlike every other lookup type. Designed for scripts such as Nastaliq where a glyph's form depends on the glyph that follows it.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>This is the only GSUB lookup type whose lookups are applied in reverse sequence order, so a shaper cannot simply iterate the glyph array forward.</description></item>
/// <item><description>Because only one input glyph is consumed per match, the coverage table has one entry per application of the rule.</description></item>
/// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-8-reverse-chaining-contextual-single-substitution-subtable">Lookup Type 8</see> in the OpenType specification.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Coverage"/>
/// <seealso cref="Header"/>
/// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-8-reverse-chaining-contextual-single-substitution-subtable">OpenType specification: Lookup Type 8</seealso>
public sealed record ReverseChainSingleSubst
    : GsubSubtable,
      IDerivedRecord<GsubSubtable, ReverseChainSingleSubst>
{
    /// <summary>Gets the coverage table for the single input glyph.</summary>
    /// <value>The <see cref="Coverage"/> whose index <c>i</c> selects <c>SubstituteGlyphIds[i]</c>.</value>
    /// <seealso cref="SubstituteGlyphIds"/>
    /// <seealso cref="BacktrackCoverages"/>
    public Coverage Coverage { get; init; } = null!;

    /// <summary>Gets the backtrack coverage tables in glyph sequence order: index 0 is the glyph immediately before the input glyph in logical order.</summary>
    /// <value>The ordered list of <see cref="Coverage"/> entries for the backtrack sequence, indexed outward from the input glyph.</value>
    /// <seealso cref="LookaheadCoverages"/>
    /// <seealso cref="Coverage"/>
    public IReadOnlyList<Coverage> BacktrackCoverages { get; init; } = [];

    /// <summary>Gets the lookahead coverage tables in glyph sequence order: index 0 is the glyph immediately after the input glyph in logical order.</summary>
    /// <value>The ordered list of <see cref="Coverage"/> entries for the lookahead sequence, indexed outward from the input glyph.</value>
    /// <seealso cref="BacktrackCoverages"/>
    /// <seealso cref="Coverage"/>
    public IReadOnlyList<Coverage> LookaheadCoverages { get; init; } = [];

    /// <summary>Gets the substitute glyphs in Coverage Index order.</summary>
    /// <value>The ordered list of substitute glyph IDs; length equals <see cref="Coverage"/>'s glyph count.</value>
    /// <seealso cref="Coverage"/>
    public IReadOnlyList<ushort> SubstituteGlyphIds { get; init; } = [];

    /// <inheritdoc/>
    /// <param name="cursor">Cursor positioned at the first byte of the subtable body (after the lookup-type dispatch).</param>
    /// <param name="context">Forwarded to the coverage parsers.</param>
    /// <returns>The parsed ReverseChainSingleSubst subtable.</returns>
    /// <exception cref="InvalidDataException">The format discriminant is not 1.</exception>
    /// <exception cref="EndOfStreamException">The header, coverage, or any coverage array extends past the end of the source.</exception>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>The on-disk layout interleaves counts and arrays: coverage offset, backtrack count, backtrack offsets, lookahead count, lookahead offsets, substitute count, substitutes. Each count is read immediately before its array; hoisting them would read the wrong bytes.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-8-reverse-chaining-contextual-single-substitution-subtable">Lookup Type 8</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Header"/>
    /// <seealso cref="Coverage"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-8-reverse-chaining-contextual-single-substitution-subtable">OpenType specification: Lookup Type 8</seealso>
    static ReverseChainSingleSubst IDerivedRecord<GsubSubtable, ReverseChainSingleSubst>.Parse(ref Cursor cursor, object? context)
    {
        Header header = cursor.ReadBigEndianStruct<Header>();
        if (header.Format != 1)
            throw new InvalidDataException($"ReverseChainSingleSubst format {header.Format} is not defined.");

        // Interleaved counts and arrays follow. Each count is read immediately before
        // its array; hoisting them would read the wrong bytes.
        return new ReverseChainSingleSubst
        {
            Format = 1,
            Coverage = cursor.Source.ParseRecordAt<Coverage>(header.CoverageOffset, context),
            BacktrackCoverages = cursor.ReadOffset16ArrayPeekRecord<Coverage>(header.BacktrackCount, context),
            LookaheadCoverages = cursor.ReadOffset16ArrayPeekRecord<Coverage>(cursor.ReadUInt16(), context),
            SubstituteGlyphIds = cursor.ReadUInt16Array(cursor.ReadUInt16()),
        };
    }

    /// <summary>The 6-byte ReverseChainSingleSubst header. Blittable, no padding.</summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Only the first three fields are in the header; the remaining counts and arrays follow inline in a fixed order.</description></item>
    /// <item><description>Declared as a <c>record struct</c>, so value equality, <c>ToString</c>, and a compiler-generated <c>&lt;Clone&gt;$()</c> are supplied automatically. The <c>&lt;Clone&gt;$()</c> method must be declared in the public-API baseline — see <c>PublicAPI.Shipped.txt</c>.</description></item>
    /// <item><description>See the <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-8-reverse-chaining-contextual-single-substitution-subtable">Lookup Type 8</see> in the OpenType specification.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ReverseChainSingleSubst"/>
    /// <seealso href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookup-type-8-reverse-chaining-contextual-single-substitution-subtable">OpenType specification: Lookup Type 8</seealso>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Header : IEndianReversibleStruct<Header>
    {
        /// <summary>Gets the format. Always 1.</summary>
        /// <value>The constant <c>1</c> for a conforming ReverseChainSingleSubst subtable.</value>
        /// <seealso cref="CoverageOffset"/>
        public ushort Format;

        /// <summary>Gets the offset to the coverage table.</summary>
        /// <value>The byte offset of the input <see cref="Coverage"/> from the subtable start.</value>
        /// <seealso cref="BacktrackCount"/>
        public ushort CoverageOffset;

        /// <summary>Gets the number of backtrack coverage tables.</summary>
        /// <value>The count of Offset16 entries for the backtrack sequence; the entries and their offsets follow this header.</value>
        /// <seealso cref="CoverageOffset"/>
        public ushort BacktrackCount;

        /// <inheritdoc/>
        /// <param name="v">The value whose multi-byte fields are to be reversed.</param>
        /// <returns>A new header with each multi-byte field reversed.</returns>
        /// <remarks>All three fields are <c>uint16</c> and are reversed independently.</remarks>
        /// <seealso cref="IEndianReversibleStruct{T}"/>
        /// <seealso cref="Cursor.ReadBigEndianStruct{T}"/>
        public static Header ReverseEndianness(Header v) => new()
        {
            Format = BinaryPrimitives.ReverseEndianness(v.Format),
            CoverageOffset = BinaryPrimitives.ReverseEndianness(v.CoverageOffset),
            BacktrackCount = BinaryPrimitives.ReverseEndianness(v.BacktrackCount),
        };
    }
}
